namespace FatStacks.Core.Connections;

// Deliberately excludes connection details from generated diagnostic text.
public sealed class ConnectionSettings
{
    public required string ConnectionString
    {
        get; init;
    }
    public override string ToString() => "SQL connection settings (redacted)";
}
public sealed record ConnectionProfile(Guid ProfileId, string FriendlyName, ConnectionSettings Settings, Guid? ExpectedBookIdentifier = null);
public sealed record ProfileConfiguration(int FormatVersion, IReadOnlyList<ConnectionProfile> Profiles, Guid? LastSuccessfulProfileId)
{
    public static ProfileConfiguration Empty => new(1, [], null);
}
public sealed record BookInfo(Guid StableIdentifier, string Name, string CurrencyCode, string TimeZoneId, ConnectionSettings? EffectiveSettings = null);
public sealed record BookSetup(string Name, string CurrencyCode, string TimeZoneId, ConnectionSettings? EffectiveSettings = null);
