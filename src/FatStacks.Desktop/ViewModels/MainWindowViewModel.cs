using System.Collections.ObjectModel;
using FatStacks.Core.Accounts;
using FatStacks.Core.Calendar;
using FatStacks.Core.Connections;
using FatStacks.Core.Register;
namespace FatStacks.Desktop.ViewModels;

public sealed record AccountGroup(byte GroupId, string Name, IReadOnlyList<AccountNode> Children) { public bool IsAccount => false; }
public enum RegisterReadState { Idle, Loading, Loaded, Failed }

public sealed class AccountNode(AccountRecord account, string path, decimal balance, string currency = "") : ObservableObject
{
    public AccountRecord Account { get; } = account;
    public bool IsAccount => true;
    public string Label => $"{Account.Name}{(Account.CloseDate is null ? "" : " (closed)")}";
    public string BalanceLabel => $"{Balance:N2} {currency}";
    public bool IsNegative => Balance < 0;
    private bool isSelected; public bool IsSelected { get => isSelected; set => Set(ref isSelected, value); }
    public string Path { get; } = path;
    public decimal Balance { get; } = balance;
    public ObservableCollection<AccountNode> Children { get; } = [];
}
public sealed class MainWindowViewModel : ObservableObject, IDisposable
{
    public required IAccountService Accounts { private get; init; }
    public required IRegisterService Register { private get; init; }
    public required BookSession Session { private get; init; }
    public required BookSessionCoordinator Sessions { private get; init; }
    public string Currency => Session.Book.CurrencyCode;
    public IReadOnlyList<string> Groups => AccountCatalog.Groups;
    public ObservableCollection<AccountNode> Tree { get; } = [];
    public IReadOnlyList<object> VisibleTree { get; private set; } = [];
    public string GroupTitle => GroupIndex == 5 ? "ALL ACCOUNTS" : Groups[GroupIndex].ToUpperInvariant();
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
            if (value is < 0 or > 5 || !Set(ref group, value)) return;
            var previous = accountId;
            BuildTree();
            Changed(nameof(GroupTitle));
            if (IsCurrent && previous != accountId) _ = ReadAsync();
        }
    }
    private string search = ""; public string Search { get => search; set { if (Set(ref search, value)) _ = ReadAsync(); } }
    private string status = ""; public string Status { get => status; private set => Set(ref status, value); }
    private long? balanceAccountId;
    private decimal balance;
    public decimal Balance
    {
        get => balance;
        private set { balanceAccountId = accountId; Set(ref balance, value); Changed(nameof(BalanceLabel)); Changed(nameof(IsBalanceNegative)); }
    }
    public string BalanceLabel => accountId is not null && balanceAccountId == accountId ? $"{Currency} {Balance:N2}" : "—";
    public bool IsBalanceNegative => balanceAccountId == accountId && Balance < 0;
    private string title = "Transaction register";
    public string AccountTitle { get => title; private set => Set(ref title, value); }
    private string accountPath = "Select an account to view its transactions";
    public string AccountPath { get => accountPath; private set => Set(ref accountPath, value); }
    private DateOnly? registerToday;
    private RegisterReadState readState;
    public RegisterReadState ReadState
    {
        get => readState;
        private set { Set(ref readState, value); Changed(nameof(EmptyTitle)); Changed(nameof(EmptyDescription)); }
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
    private RegisterRow? selected; public RegisterRow? Selected { get => selected; set { if (Set(ref selected, value)) _ = LoadDetailsAsync(value); } }
    public bool IsCurrent => !disposed && Sessions.IsCurrent(Session);
    private CancellationTokenSource NewRead(ref CancellationTokenSource? previous)
    { previous?.Cancel(); var next = CancellationTokenSource.CreateLinkedTokenSource(Session.Reads.Token); previous = next; return next; }
    public async Task RefreshAsync()
    {
        if (!IsCurrent) return; using var read = NewRead(ref load); Status = "Loading accounts…";
        try
        {
            var today = new BookCalendar(Session.Book.TimeZoneId).Today;
            var loaded = await Accounts.ListAsync(read.Token); var totals = await Register.BalancesAsync(today, read.Token);
            if (!IsCurrent || read.IsCancellationRequested) return;
            accounts = loaded; balances = totals; Changed(nameof(AccountSummary));
            accountId = accounts.Any(x => x.AccountId == accountId) ? accountId : accounts.FirstOrDefault()?.AccountId;
            Changed(nameof(AccountId)); Changed(nameof(BalanceLabel)); Changed(nameof(IsBalanceNegative)); BuildTree(); await ReadAsync();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is Microsoft.Data.SqlClient.SqlException or InvalidOperationException) { if (IsCurrent) Status = "Accounts could not be loaded. Check the connection and refresh."; }
        finally { if (ReferenceEquals(load, read)) load = null; }
    }
    private IReadOnlyList<AccountNode> BuildRoots(byte? groupId)
    {
        var included = new HashSet<long>(accounts.Where(x => groupId is null || x.GroupId == groupId).Select(x => x.AccountId));
        foreach (var item in accounts.Where(x => included.Contains(x.AccountId)).ToArray())
        {
            var parent = item.ParentAccountId;
            while (parent is { } id && included.Add(id)) parent = accounts.Single(x => x.AccountId == id).ParentAccountId;
        }
        var ordered = accounts.Where(x => included.Contains(x.AccountId)).OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        var nodes = ordered.ToDictionary(x => x.AccountId, x => new AccountNode(x, AccountCatalog.Path(x, accounts), balances.GetValueOrDefault(x.AccountId), Currency));
        var roots = new List<AccountNode>();
        foreach (var account in ordered)
        {
            if (account.ParentAccountId is { } id && nodes.TryGetValue(id, out var parent)) parent.Children.Add(nodes[account.AccountId]);
            else roots.Add(nodes[account.AccountId]);
        }
        return roots;
    }
    private void BuildTree()
    {
        var roots = BuildRoots(GroupIndex == 5 ? null : (byte)(GroupIndex + 1));
        if (!roots.SelectMany(FlattenNode).Any(x => x.Account.AccountId == accountId))
        {
            accountId = roots.FirstOrDefault()?.Account.AccountId;
            Changed(nameof(AccountId)); Changed(nameof(BalanceLabel)); Changed(nameof(IsBalanceNegative));
        }
        foreach (var node in roots.SelectMany(FlattenNode)) node.IsSelected = node.Account.AccountId == accountId;
        Tree.Clear();
        foreach (var root in roots) Tree.Add(root);
        VisibleTree = GroupIndex == 5
            ? Enumerable.Range(1, 5).Select(id => new AccountGroup((byte)id, Groups[id - 1], BuildRoots((byte)id)))
                .Where(x => x.Children.Count > 0).Cast<object>().ToArray()
            : roots.Cast<object>().ToArray();
        SelectVisibleNode(accountId);
        Changed(nameof(VisibleTree));
    }
    private static IEnumerable<AccountNode> FlattenNode(AccountNode node) => new[] { node }.Concat(node.Children.SelectMany(FlattenNode));
    private void SelectVisibleNode(long? id)
    {
        foreach (var item in VisibleTree)
        {
            var section = item as AccountGroup;
            var nodes = section is null ? FlattenNode((AccountNode)item) : section.Children.SelectMany(FlattenNode);
            foreach (var node in nodes)
                node.IsSelected = node.Account.AccountId == id && (section is null || node.Account.GroupId == section.GroupId);
        }
    }
    public async Task ClearAccountSelectionAsync()
    {
        if (!IsCurrent) return;
        accountId = null; Changed(nameof(AccountId)); Changed(nameof(BalanceLabel)); Changed(nameof(IsBalanceNegative));
        foreach (var node in Flatten(Tree)) node.IsSelected = false;
        SelectVisibleNode(null);
        await ReadAsync();
    }
    public async Task SelectAccountAsync(long id, long? entryId = null)
    {
        if (!accounts.Any(x => x.AccountId == id) || !IsCurrent) return;
        if (accountId == id && entryId is null) return;
        accountId = id; Changed(nameof(AccountId)); Changed(nameof(BalanceLabel)); Changed(nameof(IsBalanceNegative));
        // Detail links must not leave an entry hidden by the previous search.
        if (entryId is not null) { search = ""; Changed(nameof(Search)); GroupIndex = 5; }
        foreach (var node in Flatten(Tree)) node.IsSelected = node.Account.AccountId == id;
        SelectVisibleNode(id);
        await ReadAsync(entryId);
    }
    private static IEnumerable<AccountNode> Flatten(IEnumerable<AccountNode> nodes) => nodes.SelectMany(x => new[] { x }.Concat(Flatten(x.Children)));
    public async Task ReadAsync(long? entryId = null)
    {
        if (!IsCurrent) return; using var read = NewRead(ref searchRead); detailRead?.Cancel();
        var keep = entryId ?? Selected?.EntryId; Rows.Clear(); Selected = null; Details.Clear();
        var today = new BookCalendar(Session.Book.TimeZoneId).Today;
        registerToday = today;
        if (accountId is not {} id)
        {
            searchRead = null; Balance = 0; AccountTitle = "Transaction register";
            AccountPath = "Select an account to view its transactions"; ReadState = RegisterReadState.Idle;
            Status = "Create an account to start."; return;
        }
        var account = accounts.Single(x => x.AccountId == id);
        AccountTitle = account.Name; AccountPath = AccountCatalog.Path(account, accounts);
        ReadState = RegisterReadState.Loading; Status = "Loading register…";
        try
        {
            var result = await Register.ReadAsync(id, today, Search, read.Token);
            if (!IsCurrent || read.IsCancellationRequested) return;
            Balance = result.CurrentBalance; foreach (var row in result.Rows) Rows.Add(row); ReadState = RegisterReadState.Loaded;
            Selected = Rows.FirstOrDefault(x => x.EntryId == keep); Status = $"{Rows.Count:N0} transactions shown";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is Microsoft.Data.SqlClient.SqlException or InvalidOperationException or ArgumentOutOfRangeException) { if (IsCurrent && !read.IsCancellationRequested) { ReadState = RegisterReadState.Failed; Status = "Register could not be loaded. Check the connection and search length, then refresh."; } }
        finally { if (ReferenceEquals(searchRead, read)) searchRead = null; }
    }
    public async Task LoadDetailsAsync(RegisterRow? row)
    {
        if (!IsCurrent) return; using var read = NewRead(ref detailRead); Details.Clear(); if (row is null) { detailRead = null; return; }
        try
        {
            var lines = await Register.DetailsAsync(row.EntryId, read.Token);
            if (!IsCurrent || read.IsCancellationRequested || Selected?.EntryId != row.EntryId) return;
            foreach (var line in lines) Details.Add(line);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is Microsoft.Data.SqlClient.SqlException or InvalidOperationException) { if (IsCurrent && !read.IsCancellationRequested) Status = "Entry details could not be loaded. Refresh to retry."; }
        finally { if (ReferenceEquals(detailRead, read)) detailRead = null; }
    }
    public void Dispose() { if (disposed) return; disposed = true; load?.Cancel(); searchRead?.Cancel(); detailRead?.Cancel(); Tree.Clear(); VisibleTree = []; Changed(nameof(VisibleTree)); Rows.Clear(); Details.Clear(); }
}
