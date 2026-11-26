using System.Collections.ObjectModel;
using FatStacks.Core.Accounts;
using FatStacks.Core.Calendar;
using FatStacks.Core.Connections;
using FatStacks.Core.Register;
using FatStacks.Core.Journal;
using FatStacks.Core.Contracts;
using FatStacks.Infrastructure.Diagnostics;
namespace FatStacks.Desktop.ViewModels;

public sealed record AccountGroup(byte TypeId, string Name, IReadOnlyList<AccountNode> Children)
{
    public bool IsAccount => false;
}
public enum RegisterReadState
{
    Idle, Loading, Loaded, Failed
}

public sealed class AccountNode(AccountRecord account, string path, decimal balance, string currency = "") : ObservableObject
{
    public AccountRecord Account { get; } = account;
    public bool IsAccount => true;
    public string Label => $"{Account.Name}{(Account.CloseDate is null ? "" : " (closed)")}";
    public string BalanceLabel => $"{Balance:N2} {currency}";
    public bool IsNegative => Balance < 0;
    private bool isSelected; public bool IsSelected
    {
        get => isSelected; set => Set(ref isSelected, value);
    }
    public string Path { get; } = path;
    public decimal Balance { get; } = balance;
    public ObservableCollection<AccountNode> Children { get; } = [];
}
public sealed class MainWindowViewModel : ObservableObject, IDisposable
{
    public IJournalService Journal { private get; init; } = null!;
    public IReadOnlyList<AccountRecord> AccountRecords => accounts;
    public async Task<JournalRequest?> LoadSelectedEntryAsync()
    {
        if (!IsCurrent || Selected is not { } selected)
            return null;
        try
        {
            var entry = await Journal.LoadAsync(selected.EntryId, Session.Reads.Token);
            if (!IsCurrent || Selected?.EntryId != selected.EntryId)
                return null;
            if (entry is null)
                Status = "The selected entry no longer exists. Refresh the register.";
            return entry;
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            DatabaseErrors.TraceFailure("JournalLoad", ex);
            if (IsCurrent && Selected?.EntryId == selected.EntryId)
                Status = "Entry could not be loaded. Refresh and retry.";
            return null;
        }
    }
    public async Task<OperationResult<bool>> DeleteEntryAsync(DeleteJournalRequest request)
    {
        if (!IsCurrent)
            return OperationResult<bool>.Fail("The book is no longer active.");
        var result = await Journal.DeleteAsync(request);
        if (result.IsSuccess && IsCurrent)
        {
            var keep = Rows.FirstOrDefault(x => x.EntryId != request.EntryId)?.EntryId;
            await RefreshAsync(keep);
        }
        return result;
    }
    public async Task JournalSavedAsync(JournalSaveResult saved)
    {
        if (!IsCurrent)
            return;
        await RefreshAsync(saved.Entry.EntryId);
        SaveNotice = saved.ResetLineIds.Count > 0 ? JournalSaveNotice.Describe(saved, accounts) : "";
    }
    public void DismissSaveNotice() => SaveNotice = "";
    public required IAccountService Accounts
    {
        private get; init;
    }
    public required IRegisterService Register
    {
        private get; init;
    }
    public required BookSession Session
    {
        private get; init;
    }
    public required BookSessionCoordinator Sessions
    {
        private get; init;
    }
    public string Currency => Session.Book.CurrencyCode;
    private const int OtherTab = 4;
    private static readonly string[] TabNames = ["Banks", "Credit Cards", "Investments", "Loans Payable", "Other"];
    private static readonly (byte TypeId, string Name)[] OtherSections =
    [
        (4, "Loans Receivable"), (7, "Other Assets"), (8, "Other Liabilities"),
        (9, "Equity"), (10, "Income"), (11, "Expenses"),
        (1, "Banks"), (2, "Cash"), (3, "Credit Cards"), (6, "Investments"), (5, "Loans Payable")
    ];
    public ObservableCollection<AccountNode> Tree { get; } = [];
    public IReadOnlyList<object> VisibleTree { get; private set; } = [];
    public string GroupTitle => TabNames[GroupIndex].ToUpperInvariant();
    public string AccountSummary => $"{accounts.Count:N0} accounts · {Currency}";
    public ObservableCollection<RegisterRow> Rows { get; } = [];
    public ObservableCollection<EntryLine> Details { get; } = [];
    private IReadOnlyList<AccountRecord> accounts = [];
    private IReadOnlyDictionary<long, decimal> balances = new Dictionary<long, decimal>();
    private CancellationTokenSource? load, searchRead, detailRead;
    private bool disposed;
    private long? accountId; public long? AccountId => accountId;
    private int group;
    public int GroupIndex
    {
        get => group;
        set
        {
            if (value < 0 || value >= TabNames.Length || !Set(ref group, value))
                return;
            var previous = accountId;
            BuildTree();
            Changed(nameof(GroupTitle));
            if (IsCurrent && previous != accountId)
                _ = ReadAsync();
        }
    }
    private string search = ""; public string Search
    {
        get => search; set
        {
            if (Set(ref search, value))
                _ = ReadRegisterAsync(null, "RegisterSearch");
        }
    }
    private string status = ""; public string Status
    {
        get => status; private set => Set(ref status, value);
    }
    private string saveNotice = ""; public string SaveNotice
    {
        get => saveNotice; private set
        {
            if (Set(ref saveNotice, value))
                Changed(nameof(HasSaveNotice));
        }
    }
    public bool HasSaveNotice => SaveNotice.Length > 0;
    private long? balanceAccountId;
    private decimal balance;
    public decimal Balance
    {
        get => balance;
        private set
        {
            balanceAccountId = accountId;
            Set(ref balance, value);
            Changed(nameof(BalanceLabel));
            Changed(nameof(IsBalanceNegative));
        }
    }
    public string BalanceLabel => accountId is not null && balanceAccountId == accountId ? $"{Currency} {Balance:N2}" : "—";
    public bool IsBalanceNegative => balanceAccountId == accountId && Balance < 0;
    private string title = "Transaction register";
    public string AccountTitle
    {
        get => title; private set => Set(ref title, value);
    }
    private string accountPath = "Select an account to view its transactions";
    public string AccountPath
    {
        get => accountPath; private set => Set(ref accountPath, value);
    }
    private DateOnly? registerToday;
    private RegisterReadState readState;
    public RegisterReadState ReadState
    {
        get => readState;
        private set
        {
            Set(ref readState, value);
            Changed(nameof(EmptyTitle));
            Changed(nameof(EmptyDescription));
        }
    }
    public string EmptyTitle => ReadState switch
    {
        RegisterReadState.Loading => "Loading transactions…",
        RegisterReadState.Failed => "Unable to load transactions",
        _ => accountId is null ? "Select an account" : "No transactions to show"
    };
    public string EmptyDescription
    {
        get
        {
            var today = registerToday ?? new BookCalendar(Session.Book.TimeZoneId).Today;
            return ReadState switch
            {
                RegisterReadState.Loading => "Reading the selected account's register.",
                RegisterReadState.Failed => "Check the connection and use Reload to try again.",
                _ => accountId is null ? "Choose an account in the left rail, or set up your accounts."
                    : $"No matching transactions from {today.AddDays(-89):MMM d, yyyy} through {today:MMM d, yyyy}."
            };
        }
    }
    private RegisterRow? selected; public RegisterRow? Selected
    {
        get => selected; set
        {
            if (Set(ref selected, value))
                _ = LoadDetailsAsync(value);
        }
    }
    private bool updatingRegisterRows;

    public void SelectRegisterRow(RegisterRow? row)
    {
        if (!updatingRegisterRows && (row is null || Rows.Contains(row)))
            Selected = row;
    }

    private void UpdateRegisterRows(IEnumerable<RegisterRow> rows, long? entryId)
    {
        // The grid can report a transient selection while the collection is replaced.
        updatingRegisterRows = true;
        try
        {
            Selected = null;
            Rows.Clear();
            Details.Clear();
            foreach (var row in rows)
                Rows.Add(row);
            if (entryId is not null)
                Selected = Rows.FirstOrDefault(row => row.EntryId == entryId);
        }
        finally { updatingRegisterRows = false; }
    }

    public bool IsCurrent => !disposed && Sessions.IsCurrent(Session);
    private CancellationTokenSource NewRead(ref CancellationTokenSource? previous)
    {
        previous?.Cancel();
        var next = CancellationTokenSource.CreateLinkedTokenSource(Session.Reads.Token);
        previous = next;
        return next;
    }
    public async Task RefreshAsync(long? entryId = null)
    {
        if (!IsCurrent)
            return;
        using var read = NewRead(ref load);
        Status = "Loading accounts…";
        try
        {
            var today = new BookCalendar(Session.Book.TimeZoneId).Today;
            var loaded = await Accounts.ListAsync(read.Token);
            var totals = await Register.BalancesAsync(today, read.Token);
            if (!IsCurrent || read.IsCancellationRequested)
                return;
            var previousAccount = accounts.FirstOrDefault(account => account.AccountId == accountId);
            var refreshedAccount = loaded.FirstOrDefault(account => account.AccountId == accountId);
            var previousGroup = GroupIndex;
            if (previousAccount is not null && refreshedAccount is not null &&
                SidebarTab(previousAccount) != SidebarTab(refreshedAccount))
            {
                group = SidebarTab(refreshedAccount);
            }

            accounts = loaded;
            balances = totals;
            Changed(nameof(AccountSummary));
            accountId = refreshedAccount?.AccountId ?? accounts.FirstOrDefault()?.AccountId;
            Changed(nameof(AccountId));
            Changed(nameof(BalanceLabel));
            Changed(nameof(IsBalanceNegative));
            BuildTree();
            if (GroupIndex != previousGroup)
            {
                Changed(nameof(GroupIndex));
                Changed(nameof(GroupTitle));
            }

            await ReadRegisterAsync(entryId, "RegisterRefresh");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is Microsoft.Data.SqlClient.SqlException or InvalidOperationException)
        {
            if (IsCurrent && !read.IsCancellationRequested)
            {
                DatabaseErrors.TraceFailure("RegisterRefresh", ex);
                Status = "Accounts could not be loaded. Check the connection and refresh.";
            }
        }
        finally { if (ReferenceEquals(load, read)) load = null; }
    }
    private IReadOnlyList<AccountNode> BuildRoots(IEnumerable<AccountRecord> members)
    {
        var included = new HashSet<long>(members.Select(x => x.AccountId));
        foreach (var item in accounts.Where(x => included.Contains(x.AccountId)).ToArray())
        {
            var parent = item.ParentAccountId;
            while (parent is { } id && included.Add(id))
                parent = accounts.Single(x => x.AccountId == id).ParentAccountId;
        }
        var ordered = accounts.Where(x => included.Contains(x.AccountId)).OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        var nodes = ordered.ToDictionary(x => x.AccountId, x => new AccountNode(x, AccountCatalog.Path(x, accounts), balances.GetValueOrDefault(x.AccountId), Currency));
        var roots = new List<AccountNode>();
        foreach (var account in ordered)
        {
            if (account.ParentAccountId is { } id && nodes.TryGetValue(id, out var parent))
                parent.Children.Add(nodes[account.AccountId]);
            else
                roots.Add(nodes[account.AccountId]);
        }
        return roots;
    }
    private static int SidebarTab(AccountRecord account) => account.GroupId switch
    {
        >= 1 and <= 3 => account.GroupId - 1,
        5 when account.TypeId == 5 => 3, // Loan Payable previously defaulted to Other.
        _ => OtherTab
    };

    private bool publishingTree;

    private void BuildTree()
    {
        var members = accounts.Where(account => SidebarTab(account) == GroupIndex).ToArray();
        var roots = BuildRoots(members);
        VisibleTree = GroupIndex == OtherTab
            ? OtherSections.Select(section => new AccountGroup(section.TypeId, section.Name,
                    BuildRoots(members.Where(account => account.TypeId == section.TypeId))))
                .Where(section => section.Children.Count > 0).Cast<object>().ToArray()
            : roots.Cast<object>().ToArray();

        if (!roots.SelectMany(FlattenNode).Any(node => node.Account.AccountId == accountId))
        {
            accountId = roots.SelectMany(FlattenNode).FirstOrDefault(node => SidebarTab(node.Account) == GroupIndex)?.Account.AccountId;
            Changed(nameof(AccountId));
            Changed(nameof(BalanceLabel));
            Changed(nameof(IsBalanceNegative));
        }
        Tree.Clear();
        foreach (var root in roots)
            Tree.Add(root);
        SelectVisibleNode(accountId);
        // Replacing ItemsSource can select a group header while WPF rebuilds its containers.
        publishingTree = true;
        try
        {
            Changed(nameof(VisibleTree));
        }
        finally { publishingTree = false; }
    }

    public Task SelectTreeItemAsync(object? item)
    {
        if (publishingTree)
            return Task.CompletedTask;
        return item switch
        {
            AccountNode node => SelectAccountAsync(node.Account.AccountId),
            AccountGroup => ClearAccountSelectionAsync(),
            _ => Task.CompletedTask
        };
    }

    private static IEnumerable<AccountNode> FlattenNode(AccountNode node) => new[] { node }.Concat(node.Children.SelectMany(FlattenNode));

    private void SelectVisibleNode(long? id)
    {
        var nodes = VisibleTree.SelectMany(item => item is AccountGroup section
            ? section.Children.SelectMany(node => FlattenNode(node).Select(child => (Node: child, Canonical: child.Account.TypeId == section.TypeId)))
            : FlattenNode((AccountNode)item).Select(node => (Node: node, Canonical: true))).ToArray();
        var selectedNode = nodes.Where(item => item.Node.Account.AccountId == id)
            .OrderByDescending(item => item.Canonical).Select(item => item.Node).FirstOrDefault();
        foreach (var item in nodes)
            item.Node.IsSelected = ReferenceEquals(item.Node, selectedNode);
    }

    public async Task ClearAccountSelectionAsync()
    {
        if (!IsCurrent)
            return;
        accountId = null;
        Changed(nameof(AccountId));
        Changed(nameof(BalanceLabel));
        Changed(nameof(IsBalanceNegative));
        foreach (var node in Flatten(Tree))
            node.IsSelected = false;
        SelectVisibleNode(null);
        await ReadAsync();
    }
    public async Task SelectAccountAsync(long id, long? entryId = null)
    {
        var account = accounts.FirstOrDefault(x => x.AccountId == id);
        if (account is null || !IsCurrent)
            return;
        if (accountId == id && entryId is null)
            return;
        accountId = id;
        Changed(nameof(AccountId));
        Changed(nameof(BalanceLabel));
        Changed(nameof(IsBalanceNegative));
        // Detail links must not leave an entry hidden by the previous search.
        if (entryId is not null)
        {
            search = "";
            Changed(nameof(Search));
            GroupIndex = SidebarTab(account);
        }
        foreach (var node in Flatten(Tree))
            node.IsSelected = node.Account.AccountId == id;
        SelectVisibleNode(id);
        await ReadAsync(entryId);
    }
    private static IEnumerable<AccountNode> Flatten(IEnumerable<AccountNode> nodes) => nodes.SelectMany(x => new[] { x }.Concat(Flatten(x.Children)));
    private long? registerAccountId;
    private long? pendingEntryId;

    public Task ReadAsync(long? entryId = null) => ReadRegisterAsync(entryId, "RegisterRead");

    private async Task ReadRegisterAsync(long? entryId, string operation)
    {
        if (!IsCurrent)
            return;
        using var read = NewRead(ref searchRead);
        detailRead?.Cancel();
        // A replacement read may start after the prior read has cleared the grid.
        var keep = entryId ?? (registerAccountId == accountId ? Selected?.EntryId ?? pendingEntryId : null);
        registerAccountId = accountId;
        pendingEntryId = keep;
        UpdateRegisterRows([], null);
        var today = new BookCalendar(Session.Book.TimeZoneId).Today;
        registerToday = today;
        if (accountId is not { } id)
        {
            searchRead = null;
            pendingEntryId = null;
            Balance = 0;
            AccountTitle = "Transaction register";
            AccountPath = "Select an account to view its transactions";
            ReadState = RegisterReadState.Idle;
            Status = "Create an account to start.";
            return;
        }
        var account = accounts.Single(x => x.AccountId == id);
        AccountTitle = account.Name;
        AccountPath = AccountCatalog.Path(account, accounts);
        ReadState = RegisterReadState.Loading;
        Status = "Loading register…";
        try
        {
            var result = await Register.ReadAsync(id, today, Search, read.Token);
            if (!IsCurrent || read.IsCancellationRequested)
                return;
            Balance = result.CurrentBalance;
            UpdateRegisterRows(result.Rows, keep);
            ReadState = RegisterReadState.Loaded;
            Status = $"{Rows.Count:N0} transactions shown";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is Microsoft.Data.SqlClient.SqlException or InvalidOperationException or ArgumentOutOfRangeException)
        {
            if (IsCurrent && !read.IsCancellationRequested)
            {
                DatabaseErrors.TraceFailure(operation, ex);
                ReadState = RegisterReadState.Failed;
                Status = "Register could not be loaded. Check the connection and search length, then refresh.";
            }
        }
        finally
        {
            if (ReferenceEquals(searchRead, read))
            {
                searchRead = null;
                pendingEntryId = null;
            }
        }
    }
    public async Task LoadDetailsAsync(RegisterRow? row)
    {
        if (!IsCurrent)
            return;
        using var read = NewRead(ref detailRead);
        Details.Clear();
        if (row is null)
        {
            detailRead = null;
            return;
        }
        try
        {
            var lines = await Register.DetailsAsync(row.EntryId, read.Token);
            if (!IsCurrent || read.IsCancellationRequested || Selected?.EntryId != row.EntryId)
                return;
            foreach (var line in lines)
                Details.Add(line);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is Microsoft.Data.SqlClient.SqlException or InvalidOperationException)
        {
            if (IsCurrent && !read.IsCancellationRequested)
            {
                DatabaseErrors.TraceFailure("EntryDetailLoad", ex);
                Status = "Entry details could not be loaded. Refresh to retry.";
            }
        }
        finally { if (ReferenceEquals(detailRead, read)) detailRead = null; }
    }
    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        load?.Cancel();
        searchRead?.Cancel();
        detailRead?.Cancel();
        Tree.Clear();
        VisibleTree = [];
        Changed(nameof(VisibleTree));
        Rows.Clear();
        Details.Clear();
    }
}
