using System.Collections.ObjectModel;
using FatStacks.Core.Connections;
using FatStacks.Core.Contracts;
using Microsoft.Data.SqlClient;
namespace FatStacks.Desktop.ViewModels;

public sealed class ConnectionViewModel : ObservableObject
{
    public required IProfileStore Store
    {
        private get; init;
    }
    public required IBookConnectionService Connections
    {
        private get; init;
    }
    public required IDatabaseSetup Setup
    {
        private get; init;
    }
    public required BookSessionCoordinator Sessions
    {
        get; init;
    }
    public ObservableCollection<ConnectionProfile> Profiles { get; } = [];
    public event EventHandler? Opened;
    public string? ActiveProfileFriendlyName => Sessions.Active is { } active
        ? Profiles.FirstOrDefault(x => x.ProfileId == active.Profile.ProfileId)?.FriendlyName ?? active.Profile.FriendlyName
        : null;
    private ProfileConfiguration configuration = ProfileConfiguration.Empty;
    private CancellationTokenSource? operation;
    private ConnectionProfile? selected;
    public ConnectionProfile? Selected
    {
        get => selected; set
        {
            if (Set(ref selected, value) && value is not null)
                Populate(value);
        }
    }
    private string friendlyName = "", server = "", database = "", userName = "", status = "Select or create a named connection.", warnings = "";
    private bool integrated = true, busy, trustCertificate, optionalEncryption;
    public string FriendlyName
    {
        get => friendlyName; set => Set(ref friendlyName, value);
    }
    public string Server
    {
        get => server; set => Set(ref server, value);
    }
    public string Database
    {
        get => database; set => Set(ref database, value);
    }
    public string UserName
    {
        get => userName; set => Set(ref userName, value);
    }
    public string Password { private get; set; } = "";
    public bool Integrated
    {
        get => integrated; set => Set(ref integrated, value);
    }
    public bool TrustCertificate
    {
        get => trustCertificate; set => Set(ref trustCertificate, value);
    }
    public bool OptionalEncryption
    {
        get => optionalEncryption; set => Set(ref optionalEncryption, value);
    }
    public string Status
    {
        get => status; private set => Set(ref status, value);
    }
    public string Warnings
    {
        get => warnings; private set => Set(ref warnings, value);
    }
    public bool Busy
    {
        get => busy; private set
        {
            Set(ref busy, value);
            Changed(nameof(Idle));
            foreach (var command in commands)
                command.Refresh();
        }
    }
    public bool Idle => !Busy;
    public AsyncCommand SaveCommand
    {
        get;
    }
    public AsyncCommand TestCommand
    {
        get;
    }
    public AsyncCommand OpenCommand
    {
        get;
    }
    public AsyncCommand RemoveCommand
    {
        get;
    }
    public AsyncCommand NewCommand
    {
        get;
    }
    private readonly AsyncCommand[] commands;
    public ConnectionViewModel()
    {
        SaveCommand = new(() => RunAsync(SaveAsync), () => Idle);
        TestCommand = new(() => RunAsync(TestAsync), () => Idle);
        OpenCommand = new(() => RunAsync(OpenAsync), () => Idle);
        RemoveCommand = new(() => RunAsync(RemoveAsync), () => Idle);
        NewCommand = new(() =>
        {
            // PasswordBox synchronization must also run when no saved profile was selected.
            if (Selected is null)
                Changed(nameof(Selected));
            else
                Selected = null;
            FriendlyName = "";
            Server = "";
            Database = "";
            UserName = "";
            Password = "";
            Integrated = true;
            TrustCertificate = false;
            OptionalEncryption = false;
            Status = "Enter a new connection.";
            return Task.CompletedTask;
        }, () => Idle);
        commands = [SaveCommand, TestCommand, OpenCommand, RemoveCommand, NewCommand];
    }
    public async Task<bool> LoadAsync()
    {
        var loaded = await Store.LoadAsync();
        if (!loaded.IsSuccess || loaded.Value is null)
        {
            Status = loaded.Message;
            return false;
        }
        var selectedId = Selected?.ProfileId;
        configuration = loaded.Value;
        Profiles.Clear();
        foreach (var profile in configuration.Profiles)
            Profiles.Add(profile);
        Selected = Profiles.FirstOrDefault(x => x.ProfileId == selectedId);
        return true;
    }
    public void Cancel() => operation?.Cancel();
    private ConnectionSettings Settings()
    {
        if (string.IsNullOrWhiteSpace(Server) || string.IsNullOrWhiteSpace(Database))
            throw new ArgumentException();
        var b = new SqlConnectionStringBuilder
        {
            DataSource = Server.Trim(),
            InitialCatalog = Database.Trim(),
            IntegratedSecurity = Integrated,
            Encrypt = OptionalEncryption ? SqlConnectionEncryptOption.Optional : SqlConnectionEncryptOption.Mandatory,
            TrustServerCertificate = TrustCertificate
        };
        if (!Integrated)
        {
            b.UserID = UserName;
            b.Password = Password;
        }
        return new()
        {
            ConnectionString = b.ConnectionString
        };
    }
    private ConnectionProfile Profile() => new(Selected?.ProfileId ?? Guid.NewGuid(), FriendlyName.Trim(), Settings(), Selected?.ExpectedBookIdentifier);
    private void Populate(ConnectionProfile p)
    {
        var b = new SqlConnectionStringBuilder(p.Settings.ConnectionString);
        FriendlyName = p.FriendlyName;
        Server = b.DataSource;
        Database = b.InitialCatalog;
        Integrated = b.IntegratedSecurity;
        UserName = b.UserID;
        Password = b.Password;
        TrustCertificate = b.TrustServerCertificate;
        OptionalEncryption = b.Encrypt == SqlConnectionEncryptOption.Optional;
    }
    private async Task<bool> SaveProfileAsync(ConnectionProfile profile, CancellationToken token)
    {
        var updated = configuration with
        {
            Profiles = configuration.Profiles.Where(x => x.ProfileId != profile.ProfileId).Append(profile).ToArray()
        };
        var result = await Store.SaveAsync(updated, token);
        Status = result.IsSuccess ? "Profile saved." : result.Message;
        if (result.IsSuccess)
        {
            await LoadAsync();
            Selected = Profiles.Single(x => x.ProfileId == profile.ProfileId);
        }
        return result.IsSuccess;
    }
    private async Task SaveAsync(CancellationToken token) => _ = await SaveProfileAsync(Profile(), token);
    private async Task TestAsync(CancellationToken token)
    {
        var result = await Connections.TestAsync(Settings(), token);
        Status = result.IsSuccess ? "Connection is usable. No book was activated." : result.Message;
        Warnings = string.Join(Environment.NewLine, result.Warnings ?? []);
    }
    private async Task OpenAsync(CancellationToken token)
    {
        var profile = Profile();
        if (string.IsNullOrWhiteSpace(profile.FriendlyName))
        {
            Status = "Enter a nonempty profile name.";
            return;
        }
        if (configuration.Profiles.Any(x => x.ProfileId != profile.ProfileId && string.Equals(x.FriendlyName, profile.FriendlyName, StringComparison.OrdinalIgnoreCase)))
        {
            Status = "Choose a unique profile name.";
            return;
        }
        var result = await Sessions.SwitchAsync(profile, () => Task.FromResult(UnsavedDecision.Discard), () => Task.FromResult(true), token);
        Status = result.IsSuccess ? "Book opened." : result.Message;
        Warnings = string.Join(Environment.NewLine, result.Warnings ?? []);
        if (result.IsSuccess)
        {
            if (!await LoadAsync())
                Warnings = string.Join(Environment.NewLine, new[] { Warnings, "Book opened, but saved profiles could not be refreshed." }.Where(x => !string.IsNullOrEmpty(x)));
            var associated = Sessions.Active!.Profile;
            var existing = Profiles.FirstOrDefault(x => x.ProfileId == associated.ProfileId);
            if (existing is not null)
                Profiles[Profiles.IndexOf(existing)] = associated;
            else
                Profiles.Add(associated);
            Selected = associated;
            Status = "Book opened.";
            Opened?.Invoke(this, EventArgs.Empty);
        }
    }
    private async Task RemoveAsync(CancellationToken token)
    {
        if (Selected is null)
            return;
        if (Sessions.Active?.Profile.ProfileId == Selected.ProfileId)
        {
            Status = "Disconnect the active profile before removing it.";
            return;
        }
        var remaining = configuration.Profiles.Where(x => x.ProfileId != Selected.ProfileId).ToArray();
        var result = await Store.SaveAsync(configuration with
        {
            Profiles = remaining
        }, token);
        Status = result.IsSuccess ? "Profile removed. Its database was not changed." : result.Message;
        if (result.IsSuccess)
        {
            Selected = null;
            await LoadAsync();
        }
    }
    public async Task<bool> InitializeAsync(string bookName, string currency, string zone, string maintenanceUser, string maintenancePassword, bool maintenanceIntegrated)
    {
        var initialized = false;
        await RunAsync(async token =>
        {
            var b = new SqlConnectionStringBuilder(Settings().ConnectionString) { IntegratedSecurity = maintenanceIntegrated };
            b.UserID = maintenanceIntegrated ? "" : maintenanceUser;
            b.Password = maintenanceIntegrated ? "" : maintenancePassword;
            var result = await Setup.InitializeAsync(new()
            {
                ConnectionString = b.ConnectionString
            }, new(bookName, currency, zone), token);
            initialized = result.IsSuccess;
            Status = result.IsSuccess ? "Database initialized. Open it with the runtime identity." : result.Message;
            Warnings = string.Join(Environment.NewLine, result.Warnings ?? []);
        });
        return initialized;
    }
    public async Task ReassociateAsync() => await RunAsync(async token =>
    {
        if (Selected is null)
        {
            Status = "Save and select a profile first.";
            return;
        }
        var tested = await Connections.TestAsync(Settings(), token);
        if (!tested.IsSuccess || tested.Value is null)
        {
            Status = tested.Message;
            return;
        }
        await SaveProfileAsync(Profile() with
        {
            ExpectedBookIdentifier = tested.Value.StableIdentifier
        }, token);
    });
    private async Task RunAsync(Func<CancellationToken, Task> action)
    {
        if (Busy)
            return;
        using var source = new CancellationTokenSource();
        operation = source;
        Busy = true;
        Warnings = "";
        Status = "Working…";
        try
        {
            await action(source.Token);
        }
        catch (OperationCanceledException) { Status = "Operation cancelled."; }
        catch (Exception ex) when (ex is not OutOfMemoryException) { Status = "The operation failed. Check the supplied fields, settings folder, and database access."; }
        finally { operation = null; Busy = false; }
    }
}
