using FatStacks.Core.Accounts;
using FatStacks.Core.Connections;
using FatStacks.Core.Contracts;
namespace FatStacks.Core.Journal;

public sealed class JournalService : IJournalService
{
    public required IJournalStore Store { private get; init; }
    public required IAccountService Accounts { private get; init; }
    public required BookSession Session { private get; init; }
    public Task<JournalRequest?> LoadAsync(long entryId, CancellationToken ct = default) => Store.LoadAsync(entryId, ct);
    public async Task<OperationResult<JournalSaveResult>> SaveAsync(JournalRequest request, CancellationToken ct = default)
    {
        try
        {
            using var write = Session.BeginWrite();
            var accounts = await Accounts.ListAsync(ct);
            JournalRequest? original = null;
            if (request.EntryId is { } entryId)
            {
                original = await Store.LoadAsync(entryId, ct);
                if (original is null || original.BookId != request.BookId || original.EntryId != request.EntryId ||
                    request.Version is null || original.Version is null || !request.Version.SequenceEqual(original.Version) ||
                    request.OriginalLines.Count != original.OriginalLines.Count ||
                    original.OriginalLines.Any(old => !request.OriginalLines.Any(x => x.LineId == old.LineId && x.Version.SequenceEqual(old.Version))))
                    return OperationResult<JournalSaveResult>.Fail("This entry changed. Reload it before retrying; your draft has been retained.", Outcome.Conflict);
            }
            var errors = JournalValidation.Validate(request, Session.Book.CurrencyCode, accounts, original?.Lines);
            if (errors.Count > 0) return new(Outcome.ValidationFailure, Message: "Correct the indicated fields and lines.", Errors: errors);
            return await Store.SaveAsync(request, ct);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) { TraceFailure("JournalSave", ex); return OperationResult<JournalSaveResult>.Fail("Save was not confirmed. Retain this draft and reload the entry before retrying."); }
    }
    public async Task<OperationResult<bool>> DeleteAsync(DeleteJournalRequest request, CancellationToken ct = default)
    {
        try { using var write = Session.BeginWrite(); return await Store.DeleteAsync(request, ct); }
        catch (Exception ex) when (ex is not OutOfMemoryException) { TraceFailure("JournalDelete", ex); return OperationResult<bool>.Fail("Deletion was not confirmed. Reload before retrying."); }
    }
    private static void TraceFailure(string operation, Exception error)
    {
        if (error is OperationCanceledException) return;
        var category = error switch
        {
            System.Data.Common.DbException => "DatabaseFailure",
            InvalidOperationException => "InvalidOperation",
            _ => "UnexpectedFailure"
        };
        System.Diagnostics.Trace.TraceError($"operation={operation} category={category}");
    }
}
