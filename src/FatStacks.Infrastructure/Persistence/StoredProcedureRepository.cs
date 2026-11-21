using System.Data;
using FatStacks.Core.Register;
namespace FatStacks.Infrastructure.Persistence;

public sealed class StoredProcedureRepository
{
    public required BookDatabase Database { private get; init; }
    public async Task<RegisterResult> ReadAccountRegisterAsync(long accountId, DateOnly today, string search, CancellationToken ct = default)
    {
        await using var connection = Database.CreateConnection(); await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand(); command.CommandText = "dbo.ReadAccountRegister"; command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add("@AccountId", SqlDbType.BigInt).Value = accountId;
        command.Parameters.Add("@Today", SqlDbType.Date).Value = today.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@SearchText", SqlDbType.NVarChar, 1000).Value = search;
        await using var reader = await command.ExecuteReaderAsync(ct); await reader.ReadAsync(ct); var balance = reader.GetDecimal(0);
        await reader.NextResultAsync(ct); var rows = new List<RegisterRow>();
        while (await reader.ReadAsync(ct)) rows.Add(new(reader.GetInt64(0), DateOnly.FromDateTime(reader.GetDateTime(1)), reader.GetString(2), reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetDecimal(4), reader.IsDBNull(5) ? null : reader.GetDecimal(5), reader.GetDecimal(6), reader.GetString(7)));
        return new(balance, rows);
    }
}
