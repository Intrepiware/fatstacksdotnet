using FatStacks.Core.Accounts;
using FatStacks.Core.Contracts;
using FatStacks.Infrastructure.Persistence;
using FatStacks.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
namespace FatStacks.Infrastructure.Accounts;

public sealed class AccountService : IAccountService
{
    public required BookDatabase Database { private get; init; }
    private static AccountRecord Map(AccountEntity x) => new(x.AccountId, x.BookId, x.ParentAccountId, x.Name, x.AccountTypeId, x.DisplayGroupId, x.OpenDate, x.CloseDate);
    public async Task<IReadOnlyList<AccountRecord>> ListAsync(CancellationToken ct = default)
    {
        await using var db = Database.CreateContext();
        return (await db.Accounts.AsNoTracking().Where(x => x.BookId == 1).OrderBy(x => x.Name).ToListAsync(ct)).Select(Map).ToArray();
    }
    public async Task<OperationResult<AccountRecord>> SaveAsync(AccountRequest request, CancellationToken ct = default)
    {
        try
        {
            await using var db = Database.CreateContext();
            // Serializing hierarchy validation prevents two reparent operations from creating a cycle.
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var all = await db.Accounts.ToListAsync(ct);
            var error = AccountValidation.Validate(request, all.Select(Map).ToArray());
            if (error is not null) return OperationResult<AccountRecord>.Fail(error, Outcome.ValidationFailure);
            if (request.AccountId is { } id && await (from line in db.JournalEntryLines
                                                      join entry in db.JournalEntries on line.JournalEntryId equals entry.JournalEntryId
                                                      where line.AccountId == id && (entry.AccountingDate < request.OpenDate || request.CloseDate != null && entry.AccountingDate >= request.CloseDate)
                                                      select line.JournalEntryLineId).AnyAsync(ct))
                return OperationResult<AccountRecord>.Fail("Dates: existing journal lines fall outside these dates.", Outcome.ValidationFailure);
            var entity = request.AccountId is { } key ? all.Single(x => x.AccountId == key) : new AccountEntity { BookId = 1, CreateDate = DateTime.UtcNow };
            entity.Name = request.Name.Trim(); entity.ParentAccountId = request.ParentAccountId; entity.AccountTypeId = request.TypeId;
            entity.DisplayGroupId = AccountCatalog.Types.Single(x => x.Id == request.TypeId).WithGroup(request.GroupId);
            entity.OpenDate = request.OpenDate; entity.CloseDate = request.CloseDate; entity.UpdateDate = DateTime.UtcNow;
            if (request.AccountId is null) db.Accounts.Add(entity);
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
            return OperationResult<AccountRecord>.Success(Map(entity));
        }
        catch (OperationCanceledException) { return OperationResult<AccountRecord>.Fail("Account save cancelled.", Outcome.Cancelled); }
        catch (Exception ex) when (ex is DbUpdateException or Microsoft.Data.SqlClient.SqlException)
        { return OperationResult<AccountRecord>.Fail("Account could not be saved. Reload and check its name, parent, and dates."); }
    }
}
