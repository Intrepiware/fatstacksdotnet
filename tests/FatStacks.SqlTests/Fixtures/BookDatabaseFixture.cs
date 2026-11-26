using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using FatStacks.Core.Connections;
using FatStacks.Infrastructure.Connections;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.SqlTests.Fixtures;

public sealed class BookDatabaseFixture : IAsyncDisposable
{
    public ConnectionSettings Settings
    {
        get;
    }
    private readonly string databaseName;
    private readonly string master;
    private BookDatabaseFixture(string name, string root)
    {
        databaseName = name;
        var builder = new SqlConnectionStringBuilder(root) { InitialCatalog = "master", TrustServerCertificate = true };
        master = builder.ConnectionString;
        builder.InitialCatalog = name;
        Settings = new()
        {
            ConnectionString = builder.ConnectionString
        };
    }
    public static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "FatStacks.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
    public static string? TestConnection() => OverrideServer(
        Environment.GetEnvironmentVariable("FATSTACKS_MAINTENANCE_CONNECTION")
        ?? ReadUdl("fatstacks_maintenance.udl")
        ?? Environment.GetEnvironmentVariable("FATSTACKS_TEST_CONNECTION")
        ?? ReadUdl("fatstacks_test.udl"));

    private static string? ReadUdl(string name)
    {
        var file = Path.Combine(RepositoryRoot(), ".credentials", name);
        if (!File.Exists(file))
            return null;
        var text = File.ReadAllText(file, Encoding.Unicode);
        if (!text.Contains("[oledb]", StringComparison.OrdinalIgnoreCase))
            text = File.ReadAllText(file);
        var line = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(x => !x.StartsWith(';') && !x.StartsWith('[') && x.Contains('='));
        return line is null ? null : Regex.Replace(line, @"(?i)(^|;)\s*Provider\s*=[^;]*;?", "$1");
    }
    private static string? OverrideServer(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return null;
        var builder = new SqlConnectionStringBuilder(source);
        var server = Environment.GetEnvironmentVariable("FATSTACKS_TEST_SERVER");
        if (!string.IsNullOrWhiteSpace(server))
            builder.DataSource = server;
        return builder.ConnectionString;
    }
    public async Task<ConnectionSettings> MapRuntimeLoginAsync()
    {
        var source = OverrideServer(Environment.GetEnvironmentVariable("FATSTACKS_RUNTIME_CONNECTION")
            ?? Environment.GetEnvironmentVariable("FATSTACKS_TEST_CONNECTION") ?? ReadUdl("fatstacks_test.udl"));
        if (source is null)
        {
            Assert.Inconclusive("Separate runtime SQL credentials are not configured.");
            return null!;
        }
        var runtime = new SqlConnectionStringBuilder(source) { InitialCatalog = databaseName, TrustServerCertificate = true };
        if (runtime.IntegratedSecurity)
        {
            Assert.Inconclusive("This runtime-login check requires a SQL login.");
            return null!;
        }
        await using var connection = new SqlConnection(Settings.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DECLARE @sql nvarchar(max) = N'CREATE USER [FatStacksRuntimeTest] FOR LOGIN ' + QUOTENAME(@login); EXEC sys.sp_executesql @sql; ALTER ROLE FatStacksRuntime ADD MEMBER FatStacksRuntimeTest;";
        command.Parameters.Add("@login", System.Data.SqlDbType.NVarChar, 128).Value = runtime.UserID;
        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch (SqlException ex) { Assert.Fail($"Runtime login mapping failed (SQL category {ex.Number})."); }
        return new()
        {
            ConnectionString = runtime.ConnectionString
        };
    }
    public static async Task<BookDatabaseFixture> CreateAsync()
    {
        var source = TestConnection();
        if (source is null)
        {
            Assert.Inconclusive("SQL credentials unavailable. Configure FATSTACKS_MAINTENANCE_CONNECTION or the ignored test UDL.");
            return null!;
        }
        var fixture = new BookDatabaseFixture("FatStacks_Test_" + Guid.NewGuid().ToString("N"), source);
        try
        {
            await using var connection = new SqlConnection(fixture.master);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE [{fixture.databaseName}]";
            await command.ExecuteNonQueryAsync();
            return fixture;
        }
        catch (SqlException ex)
        {
            Assert.Fail($"SQL test fixture failed (SQL category {ex.Number}).");
            return null!;
        }
    }
    public DatabaseSetup Setup() => new() { Transport = new(), ScriptDirectory = Path.Combine(RepositoryRoot(), "database") };
    public async Task InitializeAsync(string currency = "USD")
    {
        var result = await Setup().InitializeAsync(Settings, new("Synthetic test book", currency, "UTC"));
        Assert.IsTrue(result.IsSuccess, result.Message);
    }
    public async Task ExecuteAsync(string sql)
    {
        try
        {
            await using var connection = new SqlConnection(Settings.ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }
        catch (SqlException ex) { Assert.Fail($"SQL fixture statement failed (SQL category {ex.Number})."); }
    }
    public async ValueTask DisposeAsync()
    {
        SqlConnection.ClearAllPools();
        await using var connection = new SqlConnection(master);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}];";
        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch (SqlException ex) { Assert.Fail($"Isolated SQL fixture cleanup failed (SQL category {ex.Number}). Remove the generated FatStacks_Test_ database explicitly."); }
    }
}
