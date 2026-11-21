namespace FatStacks.Core.Register;

public sealed record RegisterRow(long EntryId, DateOnly Date, string Description, string CounterAccounts, decimal? Payment, decimal? Deposit, decimal Balance, string Status) { public bool IsNegative => Balance < 0; }
public sealed record RegisterResult(decimal CurrentBalance, IReadOnlyList<RegisterRow> Rows);
public sealed record EntryLine(long LineId, long AccountId, string AccountName, decimal Debit, decimal Credit, byte Status, long? SecurityId, decimal? Quantity) { public string StatusName => Status switch { 0 => "Uncleared", 1 => "Reconciling", 2 => "Cleared", _ => "Unknown" }; }
public interface IRegisterService
{
    Task<RegisterResult> ReadAsync(long accountId, DateOnly today, string search, CancellationToken ct = default);
    Task<IReadOnlyList<EntryLine>> DetailsAsync(long entryId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<long, decimal>> BalancesAsync(DateOnly today, CancellationToken ct = default);
}
