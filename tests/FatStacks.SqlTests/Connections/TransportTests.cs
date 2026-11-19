using FatStacks.Core.Connections;
using FatStacks.Infrastructure.Connections;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.SqlTests.Connections;

[TestClass]
public sealed class TransportTests
{
    [TestMethod]
    public async Task UnverifiedCertificateConnectsWithWarning()
    {
        await using var fixture = await Fixtures.BookDatabaseFixture.CreateAsync();
        var result = await new SqlTransport().OpenAsync(fixture.Settings); await using var connection = result.Connection;
        Assert.IsTrue(result.Warnings.Any(x => x.Contains("certificate", StringComparison.OrdinalIgnoreCase)));
    }
    [TestMethod]
    public async Task AuthenticationFailureIsRedacted()
    {
        await using var fixture = await Fixtures.BookDatabaseFixture.CreateAsync();
        var b = new SqlConnectionStringBuilder(fixture.Settings.ConnectionString) { IntegratedSecurity = false, UserID = "FatStacks_Invalid_Test_Login", Password = "private-password-sentinel", ConnectTimeout = 3 };
        var result = await new ConnectionService { Transport = new() }.TestAsync(new() { ConnectionString = b.ConnectionString });
        Assert.IsFalse(result.IsSuccess); Assert.IsFalse(result.Message.Contains("private-password-sentinel", StringComparison.Ordinal));
    }
    [TestMethod]
    [DataRow("VALID_TLS", false)]
    [DataRow("VALID_TLS", true)]
    [DataRow("PLAINTEXT", false)]
    [DataRow("PLAINTEXT", true)]
    [DataRow("SELF_SIGNED", false)]
    [DataRow("SELF_SIGNED", true)]
    [DataRow("EXPIRED", false)]
    [DataRow("EXPIRED", true)]
    public async Task ConfiguredTransportMatrix(string scenario, bool permitPlaintext)
    {
        var builder = DedicatedFixture(scenario);
        builder.Encrypt = permitPlaintext ? SqlConnectionEncryptOption.Optional : SqlConnectionEncryptOption.Mandatory;

        // Prove the endpoint's prerequisite independently of the application's fallback policy.
        if (scenario != "VALID_TLS")
        {
            var probe = new SqlConnectionStringBuilder(builder.ConnectionString) { Encrypt = SqlConnectionEncryptOption.Mandatory };
            await using var direct = new SqlConnection(probe.ConnectionString);
            var failure = await Assert.ThrowsExactlyAsync<SqlException>(() => direct.OpenAsync());
            if (scenario == "PLAINTEXT")
                Assert.IsTrue(failure.Message.Contains("does not support encryption", StringComparison.OrdinalIgnoreCase), "Fixture must reject TLS as unavailable, rather than fail authentication or protocol negotiation.");
            else
                Assert.IsTrue(failure.Errors.Cast<SqlError>().Any(x => x.Number is -2146893019 or -2146893016 or -2146762487 or -2146762495), "Fixture must fail certificate validation before automatic retry.");
        }

        OpenSqlConnection opened;
        try { opened = await new SqlTransport().OpenAsync(new() { ConnectionString = builder.ConnectionString }); }
        catch (SqlException ex) { Assert.Fail($"Transport fixture {scenario} failed (SQL category {ex.Number})."); return; }
        await using var connection = opened.Connection;
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT encrypt_option FROM sys.dm_exec_connections WHERE session_id = @@SPID";
        var encrypted = Convert.ToString(await command.ExecuteScalarAsync());
        Assert.AreEqual(scenario == "PLAINTEXT" ? "FALSE" : "TRUE", encrypted, "Actual session encryption must match the dedicated fixture.");
        var effective = new SqlConnectionStringBuilder(opened.Settings.ConnectionString);
        Assert.AreEqual(scenario == "PLAINTEXT" ? SqlConnectionEncryptOption.Optional : SqlConnectionEncryptOption.Mandatory, effective.Encrypt);
        Assert.AreEqual(scenario is "SELF_SIGNED" or "EXPIRED", effective.TrustServerCertificate);
        if (scenario == "VALID_TLS") Assert.AreEqual(0, opened.Warnings.Count);
        else if (scenario == "PLAINTEXT")
        {
            CollectionAssert.AreEqual(new[] { "SQL transport is not encrypted." }, opened.Warnings.ToArray());
        }
        else
        {
            Assert.AreEqual(1, opened.Warnings.Count);
            Assert.IsTrue(opened.Warnings[0].Contains("certificate trust is not verified", StringComparison.Ordinal));
            Assert.IsFalse(opened.Warnings[0].Contains("not encrypted", StringComparison.Ordinal));
        }
    }

    [TestMethod]
    [DataRow("VALID_TLS")]
    [DataRow("PLAINTEXT")]
    [DataRow("SELF_SIGNED")]
    [DataRow("EXPIRED")]
    public async Task DedicatedFixtureAuthenticationFailureIsPreserved(string scenario)
    {
        var builder = DedicatedFixture(scenario);
        builder.Encrypt = SqlConnectionEncryptOption.Optional;
        builder.IntegratedSecurity = false;
        builder.UserID = "FatStacks_Invalid_Test_Login";
        builder.Password = "private-password-sentinel";
        var failure = await Assert.ThrowsExactlyAsync<SqlException>(() => new SqlTransport().OpenAsync(new() { ConnectionString = builder.ConnectionString }));
        Assert.IsTrue(failure.Errors.Cast<SqlError>().Any(x => x.Number == 18456), "Expected a preserved login failure after transport negotiation.");
        var result = await new ConnectionService { Transport = new() }.TestAsync(new() { ConnectionString = builder.ConnectionString });
        Assert.IsFalse(result.IsSuccess);
        Assert.IsFalse(result.Message.Contains("private-password-sentinel", StringComparison.Ordinal));
    }

    private static SqlConnectionStringBuilder DedicatedFixture(string scenario)
    {
        var value = Environment.GetEnvironmentVariable("FATSTACKS_SQL_" + scenario);
        if (string.IsNullOrWhiteSpace(value)) Assert.Inconclusive($"Transport fixture {scenario} was not configured.");
        var builder = new SqlConnectionStringBuilder(value);
        Assert.IsFalse(builder.TrustServerCertificate, "Dedicated fixtures must start with certificate checking enabled.");
        Assert.IsTrue(string.IsNullOrEmpty(builder.ServerCertificate), "Dedicated fixtures must not pin a certificate to bypass the intended validation condition.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(builder.InitialCatalog), "Dedicated fixtures require a database.");
        builder.Pooling = false;
        builder.ConnectTimeout = 5;
        return builder;
    }
}
