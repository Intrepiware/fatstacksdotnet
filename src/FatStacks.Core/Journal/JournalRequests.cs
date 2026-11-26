using FatStacks.Core.Accounts;
using FatStacks.Core.Contracts;
namespace FatStacks.Core.Journal;

public sealed record LineVersion(long LineId, byte[] Version);
public sealed record JournalLine(long? LineId, byte[]? Version, long AccountId, int Order, decimal Debit, decimal Credit, string CurrencyCode, byte Status = 0, long? SecurityId = null, decimal? Quantity = null);
public sealed record JournalRequest(long BookId, long? EntryId, byte[]? Version, DateOnly AccountingDate, DateOnly? BankPostingDate, string? Payee, string Description, byte EntryKind, IReadOnlyList<JournalLine> Lines, IReadOnlyList<LineVersion> OriginalLines);
public sealed record DeleteJournalRequest(long BookId, long EntryId, byte[] Version, IReadOnlyList<LineVersion> OriginalLines);
public record JournalError(string Field, string Message, int? LineOrder = null) : FieldError(Field, Message, LineOrder);
public sealed record JournalAccountDateError(AccountRecord Account, string Message, int Order)
    : JournalError("Account", Message, Order);
public sealed record JournalSaveResult(JournalRequest Entry, IReadOnlyList<long> ResetLineIds);
public interface IJournalStore
{
    Task<JournalRequest?> LoadAsync(long entryId, CancellationToken ct = default);
    Task<OperationResult<JournalSaveResult>> SaveAsync(JournalRequest request, CancellationToken ct = default);
    Task<OperationResult<bool>> DeleteAsync(DeleteJournalRequest request, CancellationToken ct = default);
}
public interface IJournalService : IJournalStore
{
}
