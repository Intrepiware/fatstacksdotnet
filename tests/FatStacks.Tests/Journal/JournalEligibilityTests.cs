using FatStacks.Core.Accounts;
using FatStacks.Core.Connections;
using FatStacks.Core.Contracts;
using FatStacks.Core.Journal;
using FatStacks.Desktop.ViewModels;
using FatStacks.WindowsChecks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.Tests.Journal;

[TestClass]
public sealed class JournalEligibilityTests
{
    private static readonly DateOnly Day = new(2026, 11, 19);
    private static readonly AccountRecord[] Catalog = [
        new(1,1,null,"Bank",1,1,new(2000,1,1),null),
        new(2,1,null,"Closed original",9,5,new(2000,1,1),new(2026,11,20)),
        new(3,1,null,"Closed replacement",9,5,new(2000,1,1),new(2026,11,20)),
        new(4,1,null,"Future account",11,4,new(2026,12,1),null)];
    private static JournalRequest Loaded() => new(1, 10, new byte[8], Day, null, null, "History", 0,
        [new(11, new byte[8], 2, 1, 25, 0, "USD"), new(12, new byte[8], 1, 2, 0, 25, "USD")], [new(11, new byte[8]), new(12, new byte[8])]);
    [TestMethod]
    public void NewLineSuggestionsExcludeClosedAccountsAndFollowAccountingDate()
    {
        var model = JournalEditorViewModelTests.Editor(new JournalEditorViewModelTests.FakeJournal());
        model.Initialize(null, Catalog, "USD", Day, 1);
        CollectionAssert.AreEqual(new[] { "Bank" }, model.Lines[0].AccountPaths.ToArray());
        model.AccountingDate = new DateTime(2026, 12, 1);
        CollectionAssert.AreEquivalent(new[] { "Bank", "Future account" }, model.Lines[0].AccountPaths.ToArray());
        model.AccountingDate = new DateTime(1999, 12, 31);
        Assert.IsEmpty(model.Lines[0].AccountPaths);
    }
    [TestMethod]
    public async Task HistoricalLineKeepsOnlyItsOwnClosedReferenceBeforeClosure()
    {
        var journal = new JournalEditorViewModelTests.FakeJournal();
        var model = JournalEditorViewModelTests.Editor(journal);
        model.Initialize(Loaded(), Catalog, "USD", Day);
        CollectionAssert.Contains(model.Lines[0].AccountPaths.ToArray(), "Closed original");
        CollectionAssert.DoesNotContain(model.Lines[0].AccountPaths.ToArray(), "Closed replacement");
        CollectionAssert.DoesNotContain(model.Lines[1].AccountPaths.ToArray(), "Closed original");
        model.AccountingDate = new DateTime(2026, 11, 20);
        Assert.AreEqual("Closed original", model.Lines[0].AccountText);
        Assert.AreEqual(2L, model.Lines[0].AccountId);
        CollectionAssert.DoesNotContain(model.Lines[0].AccountPaths.ToArray(), "Closed original");
        Assert.IsFalse(await model.SaveAsync(false));
        Assert.AreEqual(0, journal.Calls);
        Assert.IsTrue(model.Errors.Any(x => x.Field == "Account" && x.LineOrder == 1 && x.Message.Contains("closed", StringComparison.OrdinalIgnoreCase)));
    }
    [TestMethod]
    public async Task ServiceValidatesAgainstStoredOriginalAccountsAndPreservesConflicts()
    {
        var store = new Store { Original = Loaded() };
        using var session = Session();
        var service = new JournalService { Store = store, Accounts = new Accounts(), Session = session };
        Assert.IsTrue((await service.SaveAsync(Loaded())).IsSuccess);
        Assert.AreEqual(1, store.Writes);
        var changed = Loaded() with
        {
            Lines = [Loaded().Lines[0] with { AccountId = 3 }, Loaded().Lines[1]]
        };
        var invalid = await service.SaveAsync(changed);
        Assert.AreEqual(Outcome.ValidationFailure, invalid.Outcome);
        Assert.AreEqual(1, store.Writes);
        Assert.IsTrue(invalid.Errors!.Any(x => x.Field == "Account" && x.LineOrder == 1 && x.Message.Contains("closed", StringComparison.OrdinalIgnoreCase)));
        store.Original = Loaded() with
        {
            Version = new byte[] { 1, 0, 0, 0, 0, 0, 0, 0 }
        };
        Assert.AreEqual(Outcome.Conflict, (await service.SaveAsync(changed)).Outcome);
        Assert.AreEqual(1, store.Writes);
    }
    [TestMethod]
    public async Task ValidationDistinguishesMissingBeforeOpenAndClosedAccounts()
    {
        using var session = Session();
        var store = new Store();
        var service = new JournalService { Store = store, Accounts = new Accounts(), Session = session };
        foreach (var (id, reason) in new[] { (999L, "available"), (4L, "opens"), (2L, "closed") })
        {
            var draft = Loaded() with
            {
                EntryId = null,
                Version = null,
                OriginalLines = [],
                Lines = [new(null, null, id, 1, 25, 0, "USD"), new(null, null, 1, 2, 0, 25, "USD")]
            };
            var result = await service.SaveAsync(draft);
            Assert.AreEqual(Outcome.ValidationFailure, result.Outcome);
            Assert.IsTrue(result.Errors!.Any(x => x.Field == "Account" && x.LineOrder == 1 && x.Message.Contains(reason, StringComparison.OrdinalIgnoreCase)), reason);
        }
        Assert.AreEqual(0, store.Writes);
    }
    [TestMethod]
    public async Task CorrectingDateRechecksCurrentAccountEligibilityAndRetainsOtherErrors()
    {
        using var session = Session();
        var store = new Store();
        var service = new JournalService { Store = store, Accounts = new Accounts(), Session = session };
        var model = JournalEditorViewModelTests.Editor(service);
        var earlierCatalog = Catalog.Select(account => account with { OpenDate = Day, CloseDate = null }).ToArray();
        model.Initialize(null, earlierCatalog, "USD", Day, 4);
        model.Lines[0].Debit = "25";
        model.Lines[1].AccountText = "Closed replacement";
        model.Lines[1].Credit = "25";

        Assert.IsFalse(await model.SaveAsync(false));
        model.AccountingDate = new DateTime(2026, 11, 30);
        Assert.IsTrue(model.Lines[0].HasErrors);
        model.AccountingDate = new DateTime(2026, 12, 1);
        Assert.IsFalse(model.Lines[0].HasErrors);
        Assert.IsTrue(model.Lines[1].HasErrors);
        Assert.AreEqual("25", model.Lines[0].Debit);
        Assert.AreEqual(0, store.Writes);

        model.Lines[1].AccountText = "Bank";
        Assert.IsTrue(await model.SaveAsync(false));
        Assert.AreEqual(1, store.Writes);
    }
    private static BookSession Session() => new(new(Guid.NewGuid(), "Synthetic", new() { ConnectionString = "unused" }), new(Guid.NewGuid(), "Synthetic", "USD", "UTC"));
    private sealed class Accounts : IAccountService
    {
        public Task<IReadOnlyList<AccountRecord>> ListAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AccountRecord>>(Catalog);
        public Task<OperationResult<AccountRecord>> SaveAsync(AccountRequest request, CancellationToken ct = default) => throw new NotSupportedException();
    }
    private sealed class Store : IJournalStore
    {
        public JournalRequest? Original
        {
            get; set;
        }
        public int Writes
        {
            get; private set;
        }
        public Task<JournalRequest?> LoadAsync(long id, CancellationToken ct = default) => Task.FromResult(Original);
        public Task<OperationResult<JournalSaveResult>> SaveAsync(JournalRequest request, CancellationToken ct = default)
        {
            Writes++;
            return Task.FromResult(OperationResult<JournalSaveResult>.Success(new(request, [])));
        }
        public Task<OperationResult<bool>> DeleteAsync(DeleteJournalRequest request, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
