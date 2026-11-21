using FatStacks.Core.Contracts;
namespace FatStacks.Core.Accounts;

public sealed record AccountRequest(long? AccountId, long? ParentAccountId, string Name, byte TypeId, byte? GroupId, DateOnly OpenDate, DateOnly? CloseDate);
public sealed record AccountRecord(long AccountId, long BookId, long? ParentAccountId, string Name, byte TypeId, byte GroupId, DateOnly OpenDate, DateOnly? CloseDate)
{
    public bool CanPost(DateOnly date, bool existingReference) => date >= OpenDate && (CloseDate is null || existingReference && date < CloseDate);
}
public interface IAccountService
{
    Task<IReadOnlyList<AccountRecord>> ListAsync(CancellationToken ct = default);
    Task<OperationResult<AccountRecord>> SaveAsync(AccountRequest request, CancellationToken ct = default);
}
public sealed record AccountType(byte Id, string Name, byte ClassId, byte DefaultGroupId)
{
    public byte WithGroup(byte? group) => group is null ? DefaultGroupId : group is >= 1 and <= 5 ? group.Value : throw new ArgumentOutOfRangeException(nameof(group));
    public string ClassName => AccountCatalog.Classes[ClassId - 1];
}
public static class AccountCatalog
{
    public static readonly IReadOnlyList<AccountType> Types = Array.AsReadOnly<AccountType>([
        new(1,"Bank",1,1),new(2,"Cash",1,1),new(3,"Credit Card",2,2),new(4,"Loan Receivable",1,5),new(5,"Loan Payable",2,5),
        new(6,"Investment/Brokerage",1,3),new(7,"Other Asset",1,5),new(8,"Other Liability",2,5),new(9,"Equity",3,5),new(10,"Income",4,5),new(11,"Expense",5,4)]);
    public static readonly string[] Classes = ["Assets", "Liabilities", "Equity", "Income", "Expenses"];
    public static readonly string[] Groups = ["Banks", "Credit Cards", "Investments", "Expenses", "Other", "All"];
    public static string Path(AccountRecord account, IReadOnlyList<AccountRecord> accounts)
    {
        var names = new List<string> { account.Name }; var seen = new HashSet<long> { account.AccountId };
        while (account.ParentAccountId is { } parent && seen.Add(parent))
        { var next = accounts.FirstOrDefault(x => x.AccountId == parent); if (next is null) break; account = next; names.Insert(0, account.Name); }
        return string.Join(":", names);
    }
}
public static class AccountValidation
{
    public static string? Validate(AccountRequest request, IReadOnlyList<AccountRecord> accounts, long bookId = 1)
    {
        var name = request.Name.Trim();
        if (name.Length is 0 or > 200 || name.Contains(':')) return "Name: enter 1–200 characters without colons.";
        if (!AccountCatalog.Types.Any(x => x.Id == request.TypeId) || request.GroupId is < 1 or > 5) return "Type/group: choose a supported value.";
        var current = accounts.FirstOrDefault(x => x.AccountId == request.AccountId);
        if (request.AccountId is not null && (current is null || current.BookId != bookId)) return "Account: reload this book.";
        if (current is not null && current.TypeId != request.TypeId) return "Type: account type cannot change after creation.";
        if (request.CloseDate <= request.OpenDate) return "Close date: must be later than open date.";
        if (accounts.Any(x => x.BookId == bookId && x.ParentAccountId == request.ParentAccountId && x.AccountId != request.AccountId && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))) return "Name: another sibling has this name.";
        var parent = request.ParentAccountId; var seen = new HashSet<long>();
        while (parent is { } id)
        {
            if (id == request.AccountId || !seen.Add(id)) return "Parent: an account cannot be its own ancestor.";
            var ancestor = accounts.FirstOrDefault(x => x.AccountId == id);
            if (ancestor is null || ancestor.BookId != bookId) return "Parent: choose an account in this book.";
            parent = ancestor.ParentAccountId;
        }
        return null;
    }
}
