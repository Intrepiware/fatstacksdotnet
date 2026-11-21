using FatStacks.Core.Accounts;
using FatStacks.Core.Calendar;
using FatStacks.Core.Connections;
using FatStacks.Infrastructure.Diagnostics;
namespace FatStacks.Desktop.ViewModels;

public sealed record ParentChoice(long? Id, string Name);
public sealed class AccountBrowserItem(AccountRecord account, string path) : ObservableObject
{
    public AccountRecord Account { get; } = account;
    public string Path { get; } = path;
    public string Name => Account.Name + (Account.CloseDate is null ? "" : " (closed)");
    public string TypeName => AccountCatalog.Types.Single(x => x.Id == Account.TypeId).Name;
    public List<AccountBrowserItem> Children { get; } = [];
    private bool isSelected;
    public bool IsSelected { get => isSelected; set => Set(ref isSelected, value); }
}

public sealed class AccountSetupViewModel : ObservableObject
{
    public required IAccountService Accounts { private get; init; }
    public required BookSession Session { private get; init; }
    // Presentation order is independent of the stored account type IDs.
    private static readonly byte[] TypeDisplayOrder = [1, 2, 3, 6, 5, 4, 7, 8, 9, 10, 11];

    public IReadOnlyList<AccountType> Types { get; } = Array.AsReadOnly(TypeDisplayOrder
        .Select(id => AccountCatalog.Types.Single(type => type.Id == id))
        .ToArray());
    public IReadOnlyList<string> Groups => ["Default from type", .. AccountCatalog.Groups.Take(5)];
    public IReadOnlyList<AccountRecord> Records { get; private set; } = [];
    public IReadOnlyList<ParentChoice> Parents { get; private set; } = [];
    public IReadOnlyList<AccountBrowserItem> VisibleAccounts { get; private set; } = [];
    public IReadOnlyList<AccountBrowserItem> AccountTree { get; private set; } = [];
    private string search = "";
    public string Search { get => search; set { if (Set(ref search, value)) RebuildBrowser(); } }
    private bool isTreeView = true;
    public bool IsTreeView
    {
        get => isTreeView;
        set => UpdateBrowser(() =>
        {
            if (Set(ref isTreeView, value)) Changed(nameof(IsAlphabeticalView));
        });
    }
    public bool IsAlphabeticalView { get => !IsTreeView; set => IsTreeView = !value; }
    private bool updatingBrowser;
    public AccountBrowserItem? SelectedBrowserItem
    {
        get => VisibleAccounts.FirstOrDefault(x => x.Account.AccountId == Selected?.AccountId);
        set { if (value is not null && VisibleAccounts.Contains(value)) SelectBrowserItem(value); }
    }
    public void SelectBrowserItem(AccountBrowserItem item)
    {
        if (!updatingBrowser && BrowserItems().Contains(item))
            Selected = item.Account;
    }

    private AccountRecord? selected;
    public AccountRecord? Selected
    {
        get => selected;
        set
        {
            if (!Set(ref selected, value)) return;
            UpdateBrowser(() =>
            {
                foreach (var item in BrowserItems()) item.IsSelected = item.Account.AccountId == value?.AccountId;
                Changed(nameof(SelectedBrowserItem));
            });
            Edit(value);
        }
    }
    private IEnumerable<AccountBrowserItem> BrowserItems() => AccountTree.SelectMany(Flatten);
    private static IEnumerable<AccountBrowserItem> Flatten(AccountBrowserItem item) => new[] { item }.Concat(item.Children.SelectMany(Flatten));
    private void RebuildBrowser()
    {
        var items = Records.Select(x => new AccountBrowserItem(x, AccountCatalog.Path(x, Records)))
            .OrderBy(x => x.Account.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Account.AccountId).ToArray();
        var query = Search.Trim();
        VisibleAccounts = items.Where(x => query.Length == 0 || x.Path.Contains(query, StringComparison.OrdinalIgnoreCase)
            || x.TypeName.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
        var included = new HashSet<long>(VisibleAccounts.Select(x => x.Account.AccountId));
        var byId = items.ToDictionary(x => x.Account.AccountId);
        foreach (var match in VisibleAccounts)
        {
            var parent = match.Account.ParentAccountId;
            while (parent is { } id && included.Add(id)) parent = byId[id].Account.ParentAccountId;
        }
        var roots = new List<AccountBrowserItem>();
        foreach (var item in items.Where(x => included.Contains(x.Account.AccountId)))
        {
            item.IsSelected = item.Account.AccountId == Selected?.AccountId;
            if (item.Account.ParentAccountId is { } id && byId.TryGetValue(id, out var parent)) parent.Children.Add(item);
            else roots.Add(item);
        }
        // A branch belongs to its root's type so mixed-type descendants stay with their parent.
        var rootsByType = roots.ToLookup(item => item.Account.TypeId);
        AccountTree = Types.SelectMany(type => rootsByType[type.Id]).ToArray();
        UpdateBrowser(() =>
        {
            Changed(nameof(VisibleAccounts));
            Changed(nameof(AccountTree));
            Changed(nameof(SelectedBrowserItem));
        });
    }

    private void UpdateBrowser(Action update)
    {
        // WPF can raise selection events while applying a new item source or highlight.
        updatingBrowser = true;
        try { update(); }
        finally { updatingBrowser = false; }
    }

    private string name = ""; public string Name { get => name; set => Set(ref name, value); }
    private AccountType type = AccountCatalog.Types[0]; public AccountType Type { get => type; set { if (Set(ref type, value)) Changed(nameof(ClassName)); } }
    public string ClassName => Type.ClassName;
    public bool CanChangeType => Selected is null;
    private long? parent; public long? ParentId { get => parent; set => Set(ref parent, value); }
    private int group; public int GroupIndex { get => group; set => Set(ref group, value); }
    private DateTime open; public DateTime Open { get => open; set => Set(ref open, value); }
    private DateTime? close; public DateTime? Close { get => close; set => Set(ref close, value); }
    private string status = ""; public string Status { get => status; private set => Set(ref status, value); }
    private bool busy; public bool Busy { get => busy; private set { Set(ref busy, value); Changed(nameof(Idle)); } }
    public bool Idle => !Busy;
    public void New() { Selected = null; Edit(null); }
    private void Edit(AccountRecord? value)
    {
        Name = value?.Name ?? ""; Type = Types.Single(x => x.Id == (value?.TypeId ?? 1)); ParentId = value?.ParentAccountId;
        GroupIndex = value?.GroupId ?? 0; Open = (value?.OpenDate ?? new BookCalendar(Session.Book.TimeZoneId).Today).ToDateTime(TimeOnly.MinValue);
        Close = value?.CloseDate?.ToDateTime(TimeOnly.MinValue); Changed(nameof(CanChangeType));
    }
    public async Task LoadAsync()
    {
        if (Busy) return;
        Busy = true;
        try
        {
            New();
            await ReadAccountsAsync();
            RebuildBrowser();
        }
        finally { Busy = false; }
    }

    private async Task ReadAccountsAsync()
    {
        Records = await Accounts.ListAsync(Session.Reads.Token);
        Parents = [new(null, "(No parent)"), .. Records
            .Select(x => new ParentChoice(x.AccountId, AccountCatalog.Path(x, Records)))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Id)];
        Changed(nameof(Records));
        Changed(nameof(Parents));
    }
    public async Task<bool> SaveAsync()
    {
        if (Busy) return false; Busy = true;
        try
        {
            using var write = Session.BeginWrite();
            var result = await Accounts.SaveAsync(new(Selected?.AccountId, ParentId, Name, Type.Id, GroupIndex == 0 ? null : (byte)GroupIndex, DateOnly.FromDateTime(Open), Close is { } date ? DateOnly.FromDateTime(date) : null));
            Status = result.Message;
            if (!result.IsSuccess) return false;
            try
            {
                await ReadAccountsAsync();
                New();
                RebuildBrowser();
                Selected = Records.Single(x => x.AccountId == result.Value!.AccountId);
                Status = "Account saved.";
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                DatabaseErrors.TraceFailure("AccountLoad", ex);
                Status = "Account saved, but the account list could not be refreshed. Reopen Accounts to retry.";
            }
            return true;
        }
        catch (OperationCanceledException) { Status = "Account load cancelled."; return false; }
        catch (InvalidOperationException ex)
        {
            if (!Session.Reads.IsCancellationRequested) DatabaseErrors.TraceFailure("AccountSave", ex);
            Status = "The book is disconnecting.";
            return false;
        }
        finally { Busy = false; }
    }
}
