using System.Reflection;
using FatStacks.Infrastructure.Connections;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FatStacks.SqlTests.Connections;

[TestClass]
public sealed class TransportPolicyTests
{
    [TestMethod]
    public async Task OptionalAttemptsTlsFirst()
    {
        var builder = new SqlConnectionStringBuilder { Encrypt = SqlConnectionEncryptOption.Optional };
        var attempts = 0;
        await using var result = await SqlTransport.OpenWithFallbackAsync(builder, (settings, _) =>
        {
            attempts++;
            Assert.AreEqual(SqlConnectionEncryptOption.Mandatory, settings.Encrypt);
            Assert.IsFalse(settings.TrustServerCertificate);
            return Task.FromResult(new SqlConnection());
        }, default);
        Assert.AreEqual(1, attempts);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task CertificateAndEncryptionFallbackCanOccurInEitherOrder(bool certificateFirst)
    {
        var builder = new SqlConnectionStringBuilder { Encrypt = SqlConnectionEncryptOption.Optional };
        var attempts = 0;
        await using var result = await SqlTransport.OpenWithFallbackAsync(builder, (settings, _) =>
        {
            attempts++;
            if (attempts == 1)
            {
                Assert.AreEqual(SqlConnectionEncryptOption.Mandatory, settings.Encrypt);
                Assert.IsFalse(settings.TrustServerCertificate);
                throw certificateFirst ? Error(-2146893019, "certificate") : Error(20, "Server does not support encryption");
            }
            if (attempts == 2)
            {
                Assert.AreEqual(certificateFirst, settings.TrustServerCertificate);
                Assert.AreEqual(certificateFirst ? SqlConnectionEncryptOption.Mandatory : SqlConnectionEncryptOption.Optional, settings.Encrypt);
                throw certificateFirst ? Error(20, "Server does not support encryption") : Error(-2146762495, "certificate");
            }
            Assert.AreEqual(SqlConnectionEncryptOption.Optional, settings.Encrypt);
            Assert.IsTrue(settings.TrustServerCertificate);
            return Task.FromResult(new SqlConnection());
        }, default);
        Assert.AreEqual(3, attempts);
    }

    [TestMethod]
    [DataRow(-2146893019)]
    [DataRow(-2146893016)]
    [DataRow(-2146762487)]
    [DataRow(-2146762495)]
    public async Task CertificateRetryKeepsTlsAndStopsAfterOneRetry(int number)
    {
        var builder = new SqlConnectionStringBuilder();
        var failure = Error(number, "certificate");
        var attempts = 0;
        var actual = await Assert.ThrowsExactlyAsync<SqlException>(() => SqlTransport.OpenWithFallbackAsync(builder, (settings, _) =>
        {
            Assert.AreEqual(SqlConnectionEncryptOption.Mandatory, settings.Encrypt);
            Assert.AreEqual(attempts > 0, settings.TrustServerCertificate);
            attempts++;
            throw failure;
        }, default));
        Assert.AreSame(failure, actual);
        Assert.AreEqual(2, attempts);
    }

    [TestMethod]
    [DataRow(18456, "Server does not support encryption")]
    [DataRow(18452, "Server does not support encryption")]
    [DataRow(0, "pre-login handshake protocol failure")]
    [DataRow(20, "TLS handshake failed")]
    public async Task AuthenticationAndProtocolFailuresArePreserved(int number, string message)
    {
        var builder = new SqlConnectionStringBuilder { Encrypt = SqlConnectionEncryptOption.Optional };
        var failure = Error(number, message);
        var attempts = 0;
        var actual = await Assert.ThrowsExactlyAsync<SqlException>(() => SqlTransport.OpenWithFallbackAsync(builder, (_, _) =>
        {
            attempts++;
            throw failure;
        }, default));
        Assert.AreSame(failure, actual);
        Assert.AreEqual(1, attempts);
        Assert.AreEqual(SqlConnectionEncryptOption.Mandatory, builder.Encrypt);
        Assert.IsFalse(builder.TrustServerCertificate);
    }

    [TestMethod]
    public async Task EncryptionUnavailableRetriesOnlyOnce()
    {
        var builder = new SqlConnectionStringBuilder();
        var failure = Error(20, "Server does not support encryption");
        var attempts = 0;
        var actual = await Assert.ThrowsExactlyAsync<SqlException>(() => SqlTransport.OpenWithFallbackAsync(builder, (settings, _) =>
        {
            Assert.AreEqual(attempts == 0 ? SqlConnectionEncryptOption.Mandatory : SqlConnectionEncryptOption.Optional, settings.Encrypt);
            Assert.IsFalse(settings.TrustServerCertificate);
            attempts++;
            throw failure;
        }, default));
        Assert.AreSame(failure, actual);
        Assert.AreEqual(2, attempts);
    }

    [TestMethod]
    public async Task StrictDoesNotDowngrade()
    {
        var builder = new SqlConnectionStringBuilder { Encrypt = SqlConnectionEncryptOption.Strict };
        var failure = Error(-2146893019, "Server does not support encryption");
        var attempts = 0;
        var actual = await Assert.ThrowsExactlyAsync<SqlException>(() => SqlTransport.OpenWithFallbackAsync(builder, (_, _) =>
        {
            attempts++;
            throw failure;
        }, default));
        Assert.AreSame(failure, actual);
        Assert.AreEqual(1, attempts);
        Assert.AreEqual(SqlConnectionEncryptOption.Strict, builder.Encrypt);
    }

    [TestMethod]
    public async Task CancellationPreventsAnotherAttempt()
    {
        using var cancellation = new CancellationTokenSource();
        var attempts = 0;
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => SqlTransport.OpenWithFallbackAsync(new(), (_, token) =>
        {
            Assert.AreEqual(cancellation.Token, token);
            attempts++;
            cancellation.Cancel();
            throw Error(-2146893019, "certificate");
        }, cancellation.Token));
        Assert.AreEqual(1, attempts);
    }

    // SqlClient exposes no public factory for provider errors. Keep reflection in this test helper.
    private static SqlException Error(int number, string message)
    {
        var constructor = typeof(SqlError).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .OrderBy(x => x.GetParameters().Length).First();
        var arguments = constructor.GetParameters().Select((parameter, index) => index == 0 ? (object)number
            : parameter.Name == "errorMessage" ? message
            : parameter.ParameterType == typeof(string) ? ""
            : parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null).ToArray();
        var error = (SqlError)constructor.Invoke(arguments);
        var errors = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)!;
        typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(errors, [error]);
        var factory = typeof(SqlException).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Single(x => x.Name == "CreateException" && x.GetParameters().Length == 2);
        return (SqlException)factory.Invoke(null, [errors, "test"])!;
    }
}
