using System.Globalization;
using FatStacks.Core.Connections;
using Microsoft.Data.SqlClient;
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("FatStacks.SqlTests")]
namespace FatStacks.Infrastructure.Connections;

public sealed record OpenSqlConnection(SqlConnection Connection, ConnectionSettings Settings, IReadOnlyList<string> Warnings);
public sealed class SqlTransport
{
    public async Task<OpenSqlConnection> OpenAsync(ConnectionSettings settings, CancellationToken cancellationToken = default)
    {
        var builder = new SqlConnectionStringBuilder(settings.ConnectionString);
        if (string.IsNullOrWhiteSpace(builder.InitialCatalog)) throw new ArgumentException("A database is required.");
        builder.ConnectTimeout = Math.Min(builder.ConnectTimeout, 15);
        var connection = await OpenWithFallbackAsync(builder, ConnectAsync, cancellationToken);
        try
        {
            var warnings = new List<string>();
            if (builder.TrustServerCertificate) warnings.Add("SQL certificate trust is not verified. Self-signed or expired certificates are permitted; the server identity is not authenticated.");
            if (builder.Encrypt == SqlConnectionEncryptOption.Optional)
            {
                bool? encrypted = null;
                try
                {
                    await using var command = connection.CreateCommand();
                    command.CommandText = "SELECT encrypt_option FROM sys.dm_exec_connections WHERE session_id = @@SPID";
                    var value = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
                    encrypted = string.Equals(value, "TRUE", StringComparison.OrdinalIgnoreCase) ? true
                        : string.Equals(value, "FALSE", StringComparison.OrdinalIgnoreCase) ? false : null;
                }
                catch (SqlException) { /* The runtime identity need not have server diagnostic privileges. */ }
                if (encrypted != true) warnings.Add(encrypted == false ? "SQL transport is not encrypted." : "SQL encryption is optional. Encrypted transport could not be confirmed.");
            }
            return new(connection, new() { ConnectionString = builder.ConnectionString }, warnings);
        }
        catch { await connection.DisposeAsync(); throw; }
    }
    internal static async Task<SqlConnection> OpenWithFallbackAsync(SqlConnectionStringBuilder builder,
        Func<SqlConnectionStringBuilder, CancellationToken, Task<SqlConnection>> connect, CancellationToken cancellationToken)
    {
        // Optional permits fallback; it must not prevent the first attempt from using TLS.
        if (builder.Encrypt == SqlConnectionEncryptOption.Optional) builder.Encrypt = SqlConnectionEncryptOption.Mandatory;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try { return await connect(builder, cancellationToken); }
            catch (SqlException ex) when (builder.Encrypt != SqlConnectionEncryptOption.Strict && !builder.TrustServerCertificate && CertificateFailure(ex))
            {
                builder.TrustServerCertificate = true;
            }
            catch (SqlException ex) when (builder.Encrypt == SqlConnectionEncryptOption.Mandatory && EncryptionUnavailable(ex))
            {
                builder.Encrypt = SqlConnectionEncryptOption.Optional;
            }
        }
    }
    private static bool AuthenticationFailure(SqlException ex) => ex.Errors.Cast<SqlError>().Any(x => x.Number is 18456 or 18452 or 18470 or 18486 or 18487 or 18488);
    private static bool CertificateFailure(SqlException ex) => !AuthenticationFailure(ex) && ex.Errors.Cast<SqlError>().Any(x => x.Number is -2146893019 or -2146893016 or -2146762487 or -2146762495);
    private static bool EncryptionUnavailable(SqlException ex) => !AuthenticationFailure(ex) && !CertificateFailure(ex) && ex.Message.Contains("does not support encryption", StringComparison.OrdinalIgnoreCase);
    private static async Task<SqlConnection> ConnectAsync(SqlConnectionStringBuilder builder, CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(builder.ConnectionString);
        try { await connection.OpenAsync(cancellationToken); return connection; }
        catch { await connection.DisposeAsync(); throw; }
    }
}
