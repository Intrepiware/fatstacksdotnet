using System.Security.Cryptography;
using System.Text.Json;
using FatStacks.Core.Connections;
using FatStacks.Core.Contracts;
namespace FatStacks.Infrastructure.Configuration;

public sealed class ProfileStore(string path, ISecretProtector protector) : IProfileStore
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool unreadable;
    private sealed record StoredProfile(Guid ProfileId, string FriendlyName, string ProtectedConnection, Guid? ExpectedBookIdentifier);
    private sealed record StoredConfiguration(int FormatVersion, List<StoredProfile?> Profiles, Guid? LastSuccessfulProfileId);
    public async Task<OperationResult<ProfileConfiguration>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(path))
            {
                unreadable = false;
                return OperationResult<ProfileConfiguration>.Success(ProfileConfiguration.Empty);
            }
            var stored = JsonSerializer.Deserialize<StoredConfiguration>(await File.ReadAllTextAsync(path, cancellationToken));
            if (stored is null || stored.FormatVersion != 1 || stored.Profiles is null ||
                stored.Profiles.Any(x => x is null || x.ProfileId == Guid.Empty ||
                    string.IsNullOrWhiteSpace(x.FriendlyName) || string.IsNullOrWhiteSpace(x.ProtectedConnection)))
                throw new JsonException();
            var profiles = stored.Profiles.Select(x => new ConnectionProfile(x!.ProfileId, x.FriendlyName,
                new()
                {
                    ConnectionString = protector.Unprotect(x.ProtectedConnection)
                }, x.ExpectedBookIdentifier)).ToArray();
            var configuration = new ProfileConfiguration(1, profiles, stored.LastSuccessfulProfileId);
            if (!Valid(configuration))
                throw new JsonException();
            unreadable = false;
            return OperationResult<ProfileConfiguration>.Success(configuration);
        }
        catch (Exception ex) when (IsStorageFailure(ex))
        {
            unreadable = true;
            return OperationResult<ProfileConfiguration>.Fail("Saved profiles could not be read or unlocked. The existing file was preserved. Restore the file or use its original Windows user.");
        }
        finally { gate.Release(); }
    }
    public async Task<OperationResult<bool>> SaveAsync(ProfileConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (!Valid(configuration))
            return OperationResult<bool>.Fail("Profile names and IDs must be unique and nonempty.", Outcome.ValidationFailure);
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (unreadable)
                return OperationResult<bool>.Fail("Resolve the unreadable saved profiles before replacing them.");
            var stored = new StoredConfiguration(1, configuration.Profiles.Select(x => new StoredProfile(x.ProfileId,
                x.FriendlyName.Trim(), protector.Protect(x.Settings.ConnectionString), x.ExpectedBookIdentifier)).Cast<StoredProfile?>().ToList(), configuration.LastSuccessfulProfileId);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            var temporary = path + ".tmp";
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(stored, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
            File.Move(temporary, path, true);
            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex) when (IsStorageFailure(ex))
        {
            return OperationResult<bool>.Fail("Profiles were not saved. Check the settings folder and Windows credential protection.");
        }
        finally { gate.Release(); }
    }
    private static bool Valid(ProfileConfiguration c) => c.FormatVersion == 1 &&
        c.Profiles.All(x => x.ProfileId != Guid.Empty && !string.IsNullOrWhiteSpace(x.FriendlyName)) &&
        c.Profiles.Select(x => x.ProfileId).Distinct().Count() == c.Profiles.Count &&
        c.Profiles.Select(x => x.FriendlyName.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == c.Profiles.Count;
    private static bool IsStorageFailure(Exception ex) => ex is IOException or UnauthorizedAccessException or JsonException or CryptographicException or FormatException or PlatformNotSupportedException or ArgumentException;
}
