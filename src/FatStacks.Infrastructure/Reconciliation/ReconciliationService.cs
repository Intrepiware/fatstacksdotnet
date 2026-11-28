using FatStacks.Core.Connections;
using FatStacks.Core.Contracts;
using FatStacks.Core.Reconciliation;
using FatStacks.Infrastructure.Diagnostics;
using FatStacks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FatStacks.Infrastructure.Reconciliation;

public sealed class ReconciliationService : IReconciliationService
{
    public required BookDatabase Database
    {
        private get; init;
    }
    public required BookSession Session
    {
        private get; init;
    }

    public async Task<OperationResult<ReconciliationResult>> SetAsync(ReconciliationRequest request)
    {
        if (request.Status > 2 || request.EntryVersion?.Length != 8 || request.LineVersion?.Length != 8)
            return OperationResult<ReconciliationResult>.Fail("Refresh the register and choose a valid status.", Outcome.ValidationFailure);

        try
        {
            using var write = Session.BeginWrite();
            await using var db = Database.CreateContext();
            await using var transaction = await db.Database.BeginTransactionAsync();
            var entry = await db.JournalEntries.SingleOrDefaultAsync(x => x.BookId == 1 && x.JournalEntryId == request.EntryId);
            var line = await db.JournalEntryLines.SingleOrDefaultAsync(x => x.JournalEntryId == request.EntryId && x.JournalEntryLineId == request.LineId);
            if (entry is null || line is null || !entry.Version.SequenceEqual(request.EntryVersion) || !line.Version.SequenceEqual(request.LineVersion))
                return Conflict();
            if (line.ReconciliationStatus != request.Status)
            {
                line.ReconciliationStatus = request.Status;
                line.UpdateDate = entry.UpdateDate = DateTime.UtcNow;
                db.Entry(entry).Property(x => x.UpdateDate).IsModified = true;
                await db.SaveChangesAsync();
            }
            await transaction.CommitAsync();
            return OperationResult<ReconciliationResult>.Success(new(entry.Version, line.Version, line.ReconciliationStatus));
        }
        catch (DbUpdateConcurrencyException) { return Conflict(); }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            DatabaseErrors.TraceFailure("SetLineReconciliation", ex);
            return OperationResult<ReconciliationResult>.Fail("Status change was not confirmed. Refresh the register before retrying.");
        }
    }

    private static OperationResult<ReconciliationResult> Conflict() =>
        OperationResult<ReconciliationResult>.Fail("This entry changed. Refresh the register before retrying.", Outcome.Conflict);
}
