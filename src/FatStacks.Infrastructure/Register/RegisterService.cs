using FatStacks.Core.Accounts;
using FatStacks.Core.Register;
using FatStacks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace FatStacks.Infrastructure.Register;

public sealed class RegisterService : IRegisterService
{
    public required BookDatabase Database
    {
        private get; init;
    }
    public required StoredProcedureRepository Procedures
    {
        private get; init;
    }
    public Task<RegisterResult> ReadAsync(long accountId, DateOnly today, string search, CancellationToken ct = default)
    {
        if (search.Length > 1000)
            throw new ArgumentOutOfRangeException(nameof(search));
        return Procedures.ReadAccountRegisterAsync(accountId, today, search, ct);
    }
    public async Task<IReadOnlyList<EntryLine>> DetailsAsync(long entryId, CancellationToken ct = default)
    {
        await using var db = Database.CreateContext();
        var accounts = await new FatStacks.Infrastructure.Accounts.AccountService { Database = Database }.ListAsync(ct);
        var lines = await db.JournalEntryLines.AsNoTracking().Where(x => x.JournalEntryId == entryId).OrderBy(x => x.LineOrder).ToListAsync(ct);
        return lines.Select(x => new EntryLine(x.JournalEntryLineId, x.AccountId, AccountCatalog.Path(accounts.Single(a => a.AccountId == x.AccountId), accounts), x.Debit, x.Credit, x.ReconciliationStatus, x.SecurityId, x.Quantity)).ToArray();
    }
    public async Task<IReadOnlyDictionary<long, decimal>> BalancesAsync(DateOnly today, CancellationToken ct = default)
    {
        await using var db = Database.CreateContext();
        var accounts = await db.Accounts.AsNoTracking().Where(x => x.BookId == 1).ToListAsync(ct);
        // SQL casts each operand before aggregation; never aggregate in decimal(10,2).
        var direct = await db.Database.SqlQuery<DirectBalance>($"SELECT l.AccountId, CAST(SUM(CAST(l.Debit AS decimal(28,2))-CAST(l.Credit AS decimal(28,2))) AS decimal(28,2)) AS Balance FROM dbo.JournalEntryLine l JOIN dbo.JournalEntry e ON e.JournalEntryId=l.JournalEntryId WHERE e.BookId=1 AND e.AccountingDate<={today.ToDateTime(TimeOnly.MinValue)} GROUP BY l.AccountId").ToDictionaryAsync(x => x.AccountId, x => x.Balance, ct);
        var totals = accounts.ToDictionary(x => x.AccountId, _ => 0m);
        foreach (var account in accounts)
        {
            var amount = direct.GetValueOrDefault(account.AccountId);
            var id = (long?)account.AccountId;
            var seen = new HashSet<long>();
            while (id is { } key && seen.Add(key))
            {
                totals[key] += amount;
                id = accounts.Single(x => x.AccountId == key).ParentAccountId;
            }
        }
        return totals;
    }
    private sealed class DirectBalance
    {
        public long AccountId
        {
            get; set;
        }
        public decimal Balance
        {
            get; set;
        }
    }
}
