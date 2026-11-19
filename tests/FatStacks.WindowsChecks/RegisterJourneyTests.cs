using FatStacks.Core.Accounts;
using FatStacks.Core.Connections;
using FatStacks.Core.Contracts;
using FatStacks.Core.Register;
using FatStacks.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.WindowsChecks;

[TestClass]
public sealed class RegisterJourneyTests
{
    [TestMethod]
    public async Task AccountCreationSearchClosedNavigationAndRefreshKeepStableIds()
    {
        var (sessions, session) = await OpenAsync();
        var accounts = new Accounts(); var register = new Register();
        var setup = new AccountSetupViewModel { Accounts = accounts, Session = session }; await setup.LoadAsync();
        setup.Name = "New bank"; setup.Type = AccountCatalog.Types[0]; setup.GroupIndex = 5;
        Assert.IsTrue(await setup.SaveAsync()); Assert.AreEqual((byte)5, accounts.Saved!.GroupId);
        using var model = new MainWindowViewModel { Accounts = accounts, Register = register, Session = session, Sessions = sessions };
        await model.RefreshAsync(); await model.SelectAccountAsync(1); model.Selected = model.Rows.Single();
        Assert.AreEqual(2, model.Details.Count); Assert.AreEqual(25m, model.Rows.Single().Payment);
        model.Search = "counter"; await model.ReadAsync(); Assert.AreEqual(975m, model.Rows.Single().Balance);
        await model.SelectAccountAsync(2, 10); Assert.AreEqual(10L, model.Selected!.EntryId); Assert.AreEqual("", model.Search);
        await model.RefreshAsync(); Assert.AreEqual(2L, model.AccountId); Assert.AreEqual(10L, model.Selected!.EntryId);
        Assert.AreEqual(2, model.Tree.Count); Assert.AreEqual(1, model.Tree[0].Children.Count); Assert.IsTrue(model.Tree[0].Children[0].Label.Contains("closed", StringComparison.Ordinal));
    }
    [TestMethod]
    public async Task ObsoleteSearchAndOldSessionCannotPublishDelayedResults()
    {
        var (sessions, session) = await OpenAsync(); var register = new Register();
        using var model = new MainWindowViewModel { Accounts = new Accounts(), Register = register, Session = session, Sessions = sessions }; await model.RefreshAsync();
        register.Pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var old = model.ReadAsync(); register.Pending = null;
        model.Search = "new"; await model.ReadAsync(); Assert.AreEqual(975m, model.Balance);
        register.Delayed!.SetResult(new(1, [])); await old; Assert.AreEqual(975m, model.Balance); Assert.AreEqual(1, model.Rows.Count);
        register.Pending = new(TaskCreationOptions.RunContinuationsAsynchronously); var disconnected = model.ReadAsync();
        await sessions.DisconnectAsync(() => Task.FromResult(UnsavedDecision.Discard), () => Task.FromResult(true));
        register.Delayed!.SetResult(new(999, [])); await disconnected; Assert.AreEqual(975m, model.Balance);
    }
    [TestMethod]
    public async Task EmptyBookCanRefreshAndSearchRepeatedly()
    {
        var (sessions, session) = await OpenAsync(); var accounts = new Accounts(); accounts.Clear();
        using var model = new MainWindowViewModel { Accounts = accounts, Register = new Register(), Session = session, Sessions = sessions };
        await model.RefreshAsync(); await model.ReadAsync(); model.Search = "empty"; await model.ReadAsync(); await model.RefreshAsync();
        Assert.AreEqual(0, model.Rows.Count); Assert.AreEqual(0m, model.Balance); Assert.IsNull(model.AccountId);
    }
    [TestMethod]
    public async Task AccountBrowserSortsFiltersHierarchyAndPreservesDraftAcrossViews()
    {
        var (_,session)=await OpenAsync();var accounts=new Accounts();
        await accounts.SaveAsync(new(null,null,"alpha",1,null,new(2020,1,1),null));
        var model=new AccountSetupViewModel {Accounts=accounts,Session=session};await model.LoadAsync();
        CollectionAssert.AreEqual(new[] {"alpha","Bank","Closed counter"},model.VisibleAccounts.Select(x=>x.Account.Name).ToArray());
        Assert.IsTrue(model.IsTreeView);
        Assert.AreEqual(2,model.AccountTree.Count);Assert.AreEqual(1,model.AccountTree[1].Children.Count);
        model.Search="  EQUITY  ";
        Assert.AreEqual(1,model.VisibleAccounts.Count);Assert.AreEqual(2L,model.VisibleAccounts[0].Account.AccountId);
        Assert.AreEqual("Bank",model.AccountTree.Single().Account.Name);
        model.SelectedBrowserItem=model.VisibleAccounts.Single();model.Name="Unsaved rename";
        model.IsTreeView=true;
        Assert.IsFalse(model.IsAlphabeticalView);Assert.IsTrue(model.AccountTree.Single().Children.Single().IsSelected);
        model.Search="nothing matches";
        Assert.AreEqual(0,model.VisibleAccounts.Count);Assert.AreEqual(0,model.AccountTree.Count);
        Assert.AreEqual(2L,model.Selected!.AccountId);Assert.AreEqual("Unsaved rename",model.Name);
        model.SelectedBrowserItem=null; // WPF clears the list selection when filtering hides an item.
        model.IsAlphabeticalView=true;model.Search="bank:closed";
        Assert.IsFalse(model.IsTreeView);Assert.AreEqual(2L,model.SelectedBrowserItem!.Account.AccountId);
        Assert.AreEqual("Unsaved rename",model.Name);
        model.Search="";Assert.AreEqual(3,model.VisibleAccounts.Count);
        session.Dispose();
    }
    [TestMethod]
    public async Task ReferenceAccountRailGroupsAllAccountsAndKeepsClosedDescendantBalances()
    {
        var (sessions,session)=await OpenAsync();
        using var model=new MainWindowViewModel {Accounts=new Accounts(),Register=new Register(),Session=session,Sessions=sessions};
        Assert.AreEqual(0,model.GroupIndex);Assert.AreEqual("BANKS",model.GroupTitle);
        model.GroupIndex=5;await model.RefreshAsync();
        Assert.AreEqual(5,model.GroupIndex);Assert.AreEqual("ALL ACCOUNTS",model.GroupTitle);
        var groups=model.VisibleTree.Cast<AccountGroup>().ToArray();
        CollectionAssert.AreEqual(new[] {"Banks","Other"},groups.Select(x=>x.Name).ToArray());
        Assert.AreEqual($"{975m:N2} USD",groups[0].Children.Single().BalanceLabel);
        Assert.IsTrue(groups[1].Children.Single().Children.Single().Label.Contains("closed",StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(1,groups.SelectMany(x=>x.Children).Count(x=>x.IsSelected));
        await model.SelectAccountAsync(2);
        Assert.AreEqual("Closed counter",model.AccountTitle);Assert.AreEqual("Bank:Closed counter",model.AccountPath);
        await model.ClearAccountSelectionAsync();
        Assert.IsNull(model.AccountId);Assert.AreEqual("—",model.BalanceLabel);Assert.AreEqual("Select an account",model.EmptyTitle);
        model.GroupIndex=3;
        Assert.AreEqual("EXPENSES",model.GroupTitle);Assert.AreEqual(0,model.Tree.Count);Assert.IsNull(model.AccountId);
        model.GroupIndex=0;
        Assert.AreEqual(1L,model.AccountId);Assert.AreEqual("Bank",model.AccountTitle);
        session.Dispose();
    }
    [TestMethod]
    public async Task RegisterEmptyStateDistinguishesLoadingNoMatchesAndReadFailure()
    {
        var (sessions,session)=await OpenAsync();var register=new Register();
        using var model=new MainWindowViewModel {Accounts=new Accounts(),Register=register,Session=session,Sessions=sessions};
        await model.RefreshAsync();
        register.Pending=new(TaskCreationOptions.RunContinuationsAsynchronously);
        var loading=model.ReadAsync();
        Assert.AreEqual(RegisterReadState.Loading,model.ReadState);Assert.AreEqual("Loading transactions…",model.EmptyTitle);
        register.Pending.SetResult(new(975,[]));await loading;register.Pending=null;
        Assert.AreEqual(RegisterReadState.Loaded,model.ReadState);Assert.AreEqual("No transactions to show",model.EmptyTitle);
        Assert.IsTrue(model.EmptyDescription.Contains("No matching transactions from",StringComparison.Ordinal));
        register.Fail=true;await model.ReadAsync();
        Assert.AreEqual(RegisterReadState.Failed,model.ReadState);Assert.AreEqual("Unable to load transactions",model.EmptyTitle);
        session.Dispose();
    }
    internal static async Task<(BookSessionCoordinator, BookSession)> OpenAsync()
    {
        var coordinator = new BookSessionCoordinator(new Connections(), new Store());
        var result = await coordinator.SwitchAsync(new(Guid.NewGuid(), "Synthetic", new() { ConnectionString = "unused" }), () => Task.FromResult(UnsavedDecision.Discard), () => Task.FromResult(true));
        return (coordinator, result.Value!);
    }
    internal sealed class Accounts : IAccountService
    {
        public AccountRecord? Saved;
        public void Clear() => records.Clear();
        private readonly List<AccountRecord> records = [new(1, 1, null, "Bank", 1, 1, new(2020, 1, 1), null), new(2, 1, 1, "Closed counter", 9, 5, new(2020, 1, 1), new(2026, 1, 1))];
        public Task<IReadOnlyList<AccountRecord>> ListAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AccountRecord>>(records.ToArray());
        public Task<OperationResult<AccountRecord>> SaveAsync(AccountRequest r, CancellationToken ct = default)
        { Saved = new(3, 1, r.ParentAccountId, r.Name, r.TypeId, r.GroupId ?? 1, r.OpenDate, r.CloseDate); records.Add(Saved); return Task.FromResult(OperationResult<AccountRecord>.Success(Saved)); }
    }
    private sealed class Register : IRegisterService
    {
        public TaskCompletionSource<RegisterResult>? Pending, Delayed;
        public bool Fail;
        public Task<RegisterResult> ReadAsync(long id, DateOnly today, string search, CancellationToken ct = default)
        { if(Fail) throw new InvalidOperationException("Synthetic read failure"); if (Pending is { } pending) { Delayed = pending; return pending.Task; } return Task.FromResult(new RegisterResult(975, [new(10, new(2026, 11, 17), "Synthetic", "Counter", 25, null, 975, "Uncleared")])); }
        public Task<IReadOnlyList<EntryLine>> DetailsAsync(long entry, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<EntryLine>>([new(1, 1, "Bank", 0, 25, 0, null, null), new(2, 2, "Closed counter", 25, 0, 0, null, null)]);
        public Task<IReadOnlyDictionary<long, decimal>> BalancesAsync(DateOnly today, CancellationToken ct = default) => Task.FromResult<IReadOnlyDictionary<long, decimal>>(new Dictionary<long, decimal> { { 1, 975 }, { 2, 25 } });
    }
    private sealed class Connections : IBookConnectionService
    {
        public Task<OperationResult<BookInfo>> TestAsync(ConnectionSettings settings, CancellationToken ct = default) => Task.FromResult(OperationResult<BookInfo>.Success(new(Guid.NewGuid(), "Synthetic", "USD", "UTC")));
        public Task<OperationResult<BookInfo>> OpenAsync(ConnectionProfile profile, CancellationToken ct = default) => TestAsync(profile.Settings, ct);
    }
    private sealed class Store : IProfileStore
    {
        public Task<OperationResult<ProfileConfiguration>> LoadAsync(CancellationToken ct = default) => Task.FromResult(OperationResult<ProfileConfiguration>.Success(ProfileConfiguration.Empty));
        public Task<OperationResult<bool>> SaveAsync(ProfileConfiguration value, CancellationToken ct = default) => Task.FromResult(OperationResult<bool>.Success(true));
    }
}
