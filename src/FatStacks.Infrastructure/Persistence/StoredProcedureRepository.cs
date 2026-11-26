using System.Data;
using FatStacks.Core.Register;
using FatStacks.Core.Journal;
using FatStacks.Core.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using FatStacks.Infrastructure.Diagnostics;
namespace FatStacks.Infrastructure.Persistence;

public sealed class StoredProcedureRepository : IJournalStore
{
    public required BookDatabase Database
    {
        private get; init;
    }
    public async Task<RegisterResult> ReadAccountRegisterAsync(long accountId, DateOnly today, string search, CancellationToken ct = default)
    {
        await using var connection = Database.CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "dbo.ReadAccountRegister";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add("@AccountId", SqlDbType.BigInt).Value = accountId;
        command.Parameters.Add("@Today", SqlDbType.Date).Value = today.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@SearchText", SqlDbType.NVarChar, 1000).Value = search;
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        var balance = reader.GetDecimal(0);
        await reader.NextResultAsync(ct);
        var rows = new List<RegisterRow>();
        while (await reader.ReadAsync(ct))
            rows.Add(new(reader.GetInt64(0), DateOnly.FromDateTime(reader.GetDateTime(1)), reader.GetString(2), reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetDecimal(4), reader.IsDBNull(5) ? null : reader.GetDecimal(5), reader.GetDecimal(6), reader.GetString(7), reader.FieldCount > 8 && !reader.IsDBNull(8) ? reader.GetString(8) : null));
        return new(balance, rows);
    }

    public async Task<JournalRequest?> LoadAsync(long entryId, CancellationToken ct = default)
    {
        await using var db = Database.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var header = await db.JournalEntries.AsNoTracking().SingleOrDefaultAsync(x => x.JournalEntryId == entryId && x.BookId == 1, ct);
        if (header is null)
            return null;
        var currency = await db.Books.Where(x => x.BookId == 1).Join(db.Currencies, b => b.CurrencyId, c => c.CurrencyId, (b, c) => c.Code).SingleAsync(ct);
        var lines = await db.JournalEntryLines.AsNoTracking().Where(x => x.JournalEntryId == entryId).OrderBy(x => x.LineOrder).ToArrayAsync(ct);
        await transaction.CommitAsync(ct);
        return new(1, entryId, header.Version, header.AccountingDate, header.BankPostingDate, header.Payee, header.Description, header.EntryKind,
            lines.Select(x => new JournalLine(x.JournalEntryLineId, x.Version, x.AccountId, x.LineOrder, x.Debit, x.Credit, currency, x.ReconciliationStatus, x.SecurityId, x.Quantity)).ToArray(),
            lines.Select(x => new LineVersion(x.JournalEntryLineId, x.Version)).ToArray());
    }
    private static DataTable Versions(IReadOnlyList<LineVersion> versions)
    {
        var table = new DataTable();
        table.Columns.Add("LineId", typeof(long));
        table.Columns.Add("Version", typeof(byte[]));
        foreach (var line in versions)
            table.Rows.Add(line.LineId, line.Version);
        return table;
    }
    private static void Originals(SqlCommand command, IReadOnlyList<LineVersion> versions) =>
        command.Parameters.Add(new SqlParameter("@OriginalLines", SqlDbType.Structured) { TypeName = "dbo.JournalEntryLineVersions", Value = Versions(versions) });
    private static OperationResult<T> WriteFailure<T>(Exception ex) => ex switch
    {
        SqlException { Number: 51002 } => OperationResult<T>.Fail("This entry changed. Reload it before retrying; your draft has been retained.", Outcome.Conflict),
        SqlException { Number: 51001 } => OperationResult<T>.Fail("The entry is invalid. Check amounts, currency, account eligibility, and dates.", Outcome.ValidationFailure),
        _ => OperationResult<T>.Fail("The write was not confirmed. Keep this draft and reload before retrying.")
    };
    public async Task<OperationResult<JournalSaveResult>> SaveAsync(JournalRequest request, CancellationToken ct = default)
    {
        try
        {
            await using var connection = Database.CreateConnection();
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.SaveJournalEntry";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add("@BookId", SqlDbType.BigInt).Value = request.BookId;
            command.Parameters.Add("@JournalEntryId", SqlDbType.BigInt).Value = (object?)request.EntryId ?? DBNull.Value;
            command.Parameters.Add("@ExpectedVersion", SqlDbType.Binary, 8).Value = (object?)request.Version ?? DBNull.Value;
            command.Parameters.Add("@AccountingDate", SqlDbType.Date).Value = request.AccountingDate.ToDateTime(TimeOnly.MinValue);
            command.Parameters.Add("@BankPostingDate", SqlDbType.Date).Value = (object?)request.BankPostingDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value;
            command.Parameters.Add("@Payee", SqlDbType.NVarChar, 200).Value = (object?)request.Payee ?? DBNull.Value;
            command.Parameters.Add("@Description", SqlDbType.NVarChar, 1000).Value = request.Description;
            command.Parameters.Add("@EntryKind", SqlDbType.TinyInt).Value = request.EntryKind;
            var table = new DataTable();
            foreach (var column in new (string Name, Type Type)[] { ("LineId", typeof(long)), ("Version", typeof(byte[])), ("AccountId", typeof(long)), ("LineOrder", typeof(int)), ("Debit", typeof(decimal)), ("Credit", typeof(decimal)), ("CurrencyCode", typeof(string)), ("ReconciliationStatus", typeof(byte)), ("SecurityId", typeof(long)), ("Quantity", typeof(decimal)) })
                table.Columns.Add(column.Name, column.Type);
            foreach (var line in request.Lines)
                table.Rows.Add((object?)line.LineId ?? DBNull.Value, (object?)line.Version ?? DBNull.Value, line.AccountId, line.Order, line.Debit, line.Credit, line.CurrencyCode, line.Status, (object?)line.SecurityId ?? DBNull.Value, (object?)line.Quantity ?? DBNull.Value);
            command.Parameters.Add(new SqlParameter("@Lines", SqlDbType.Structured) { TypeName = "dbo.JournalEntryLines", Value = table });
            Originals(command, request.OriginalLines);
            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return OperationResult<JournalSaveResult>.Fail("The save was not confirmed. Reload before retrying.");
            var id = reader.GetInt64(0);
            var version = (byte[])reader[1];
            await reader.NextResultAsync(ct);
            var lines = new List<JournalLine>();
            var reset = new List<long>();
            while (await reader.ReadAsync(ct))
            {
                lines.Add(new(reader.GetInt64(0), (byte[])reader[1], reader.GetInt64(2), reader.GetInt32(3), reader.GetDecimal(4), reader.GetDecimal(5), request.Lines[0].CurrencyCode, reader.GetByte(6)));
                if (reader.GetBoolean(7))
                    reset.Add(reader.GetInt64(0));
            }
            return OperationResult<JournalSaveResult>.Success(new(request with
            {
                EntryId = id,
                Version = version,
                Lines = lines,
                OriginalLines = lines.Select(x => new LineVersion(x.LineId!.Value, x.Version!)).ToArray()
            }, reset));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) { if (ex is not OperationCanceledException) DatabaseErrors.TraceFailure("JournalSave", ex); return WriteFailure<JournalSaveResult>(ex); }
    }
    public async Task<OperationResult<bool>> DeleteAsync(DeleteJournalRequest request, CancellationToken ct = default)
    {
        try
        {
            await using var connection = Database.CreateConnection();
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.DeleteJournalEntry";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add("@BookId", SqlDbType.BigInt).Value = request.BookId;
            command.Parameters.Add("@JournalEntryId", SqlDbType.BigInt).Value = request.EntryId;
            command.Parameters.Add("@ExpectedVersion", SqlDbType.Binary, 8).Value = request.Version;
            Originals(command, request.OriginalLines);
            return await command.ExecuteScalarAsync(ct) is bool confirmed && confirmed ? OperationResult<bool>.Success(true) : OperationResult<bool>.Fail("Deletion was not confirmed. Reload before retrying.");
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) { if (ex is not OperationCanceledException) DatabaseErrors.TraceFailure("JournalDelete", ex); return WriteFailure<bool>(ex); }
    }
}
