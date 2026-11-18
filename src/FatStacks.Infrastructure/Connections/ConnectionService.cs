using FatStacks.Core.Connections;
using FatStacks.Core.Contracts;
using FatStacks.Infrastructure.Diagnostics;
using FatStacks.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace FatStacks.Infrastructure.Connections;

public sealed class ConnectionService : IBookConnectionService
{
    public required SqlTransport Transport { private get; init; }
    public Task<OperationResult<BookInfo>> TestAsync(ConnectionSettings settings, CancellationToken cancellationToken = default) => ReadAsync(settings, null, cancellationToken);
    public Task<OperationResult<BookInfo>> OpenAsync(ConnectionProfile profile, CancellationToken cancellationToken = default) => ReadAsync(profile.Settings, profile.ExpectedBookIdentifier, cancellationToken);
    private async Task<OperationResult<BookInfo>> ReadAsync(ConnectionSettings settings, Guid? expected, CancellationToken cancellationToken)
    {
        try
        {
            var opened = await Transport.OpenAsync(settings, cancellationToken);
            await using var connection = opened.Connection;
            await using var db = new BookDbContext(new DbContextOptionsBuilder<BookDbContext>().UseSqlServer(connection).Options);
            int? version;
            try { version = await db.SchemaVersions.Where(x => x.SchemaVersionId == 1).Select(x => (int?)x.Version).SingleOrDefaultAsync(cancellationToken); }
            catch (SqlException) { return OperationResult<BookInfo>.Fail("Fatal connection error: the schema version is missing or unreadable. Use explicit maintenance to prepare the selected database."); }
            if (!SchemaCompatibility.IsCompatible(version)) return OperationResult<BookInfo>.Fail("Fatal connection error: schema version does not match application version 1. Use the matching application or explicit maintenance.");
            var book = await (from b in db.Books.AsNoTracking()
                              join c in db.Currencies on b.CurrencyId equals c.CurrencyId
                              select new BookInfo(b.StableIdentifier, b.Name, c.Code, b.TimeZoneId, null)).SingleOrDefaultAsync(cancellationToken);
            if (book is null) return OperationResult<BookInfo>.Fail("The schema is prepared but no book exists. Complete explicit book setup.");
            if (expected.HasValue && expected.Value != book.StableIdentifier) return OperationResult<BookInfo>.Fail("The profile points to a different book. Confirm reassociation explicitly before opening it.", Outcome.IdentityMismatch);
            return OperationResult<BookInfo>.Success(book with { EffectiveSettings = opened.Settings }, opened.Warnings);
        }
        catch (Exception ex) when (ex is SqlException or ArgumentException or InvalidOperationException or OperationCanceledException)
        { return DatabaseErrors.Result<BookInfo>(ex); }
    }
}
