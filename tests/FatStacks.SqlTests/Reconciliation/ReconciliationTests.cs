using FatStacks.Core.Connections;
using FatStacks.Core.Contracts;
using FatStacks.Core.Reconciliation;
using FatStacks.Infrastructure.Persistence;
using FatStacks.Infrastructure.Reconciliation;
using FatStacks.SqlTests.Fixtures;
using FatStacks.SqlTests.Journal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FatStacks.SqlTests.Reconciliation;

[TestClass]
public sealed class ReconciliationTests
{
    [TestMethod]
    public async Task DistinctPairsSurviveReopenWithIndependentLinesAndUnchangedFinances()
    {
        await using var fixture = await BookDatabaseFixture.CreateAsync();
        await JournalWriteTests.SeedAsync(fixture);
        var journal = JournalWriteTests.Service(fixture.Settings);
        var draft = JournalWriteTests.Draft(1, 2, 25);
        var entry = (await journal.SaveAsync(draft)).Value!.Entry;
        using var session = new BookSession(new(Guid.NewGuid(), "Test", fixture.Settings), new(Guid.NewGuid(), "Test", "USD", "UTC"));
        var service = new ReconciliationService { Session = session, Database = new(fixture.Settings) };
        var repository = new StoredProcedureRepository { Database = new(fixture.Settings) };
        foreach (byte bank in new byte[] { 0, 1, 2 })
            foreach (byte counter in new byte[] { 0, 1, 2 })
            {
                if (bank == counter)
                    continue;
                foreach (var (account, target) in new[] { (1, bank), (2, counter) })
                {
                    var row = (await repository.ReadAccountRegisterAsync(account, draft.AccountingDate, "")).Rows.Single();
                    var result = await service.SetAsync(new(row.EntryId, row.LineId, row.EntryVersion!, row.LineVersion!, target));
                    Assert.IsTrue(result.IsSuccess, result.Message);
                    var current = (await repository.ReadAccountRegisterAsync(account, draft.AccountingDate, "")).Rows.Single();
                    CollectionAssert.AreEqual(current.EntryVersion, result.Value!.EntryVersion);
                    CollectionAssert.AreEqual(current.LineVersion, result.Value.LineVersion);
                    Assert.AreEqual(account == 1 ? 25m : -25m, current.Balance);
                }
                var loaded = (await JournalWriteTests.Service(fixture.Settings).LoadAsync(entry.EntryId!.Value))!;
                CollectionAssert.AreEqual(new byte[] { bank, counter }, loaded.Lines.Select(x => x.Status).ToArray());
                Assert.AreEqual(entry.AccountingDate, loaded.AccountingDate);
                Assert.AreEqual(entry.BankPostingDate, loaded.BankPostingDate);
                CollectionAssert.AreEqual(entry.Lines.Select(x => (x.LineId, x.AccountId, x.Debit, x.Credit)).ToArray(),
                    loaded.Lines.Select(x => (x.LineId, x.AccountId, x.Debit, x.Credit)).ToArray());
            }
    }

    [TestMethod]
    public async Task RejectedHeaderTouchLeavesBothVersionsAndStatusIntact()
    {
        await using var fixture = await BookDatabaseFixture.CreateAsync();
        await JournalWriteTests.SeedAsync(fixture);
        var journal = JournalWriteTests.Service(fixture.Settings);
        var entry = (await journal.SaveAsync(JournalWriteTests.Draft(1, 2, 25))).Value!.Entry;
        await fixture.ExecuteAsync("ALTER TABLE dbo.JournalEntry WITH NOCHECK ADD CONSTRAINT CK_TestRejectTouch CHECK (UpdateDate < '20000101');");
        using var session = new BookSession(new(Guid.NewGuid(), "Test", fixture.Settings), new(Guid.NewGuid(), "Test", "USD", "UTC"));
        var service = new ReconciliationService { Session = session, Database = new(fixture.Settings) };
        var line = entry.Lines[0];
        var result = await service.SetAsync(new(entry.EntryId!.Value, line.LineId!.Value, entry.Version!, line.Version!, 1));
        Assert.AreEqual(Outcome.Failed, result.Outcome);
        var loaded = (await journal.LoadAsync(entry.EntryId.Value))!;
        CollectionAssert.AreEqual(entry.Version, loaded.Version);
        CollectionAssert.AreEqual(line.Version, loaded.Lines[0].Version);
        Assert.AreEqual(line.Status, loaded.Lines[0].Status);
        Assert.AreEqual(line.Debit, loaded.Lines[0].Debit);
    }

    [TestMethod]
    public async Task WholeEntryAndLineWritesFromOneSnapshotCannotBothCommit()
    {
        await using var fixture = await BookDatabaseFixture.CreateAsync();
        await JournalWriteTests.SeedAsync(fixture);
        var journal = JournalWriteTests.Service(fixture.Settings);
        var entry = (await journal.SaveAsync(JournalWriteTests.Draft(1, 2, 25))).Value!.Entry;
        using var session = new BookSession(new(Guid.NewGuid(), "Test", fixture.Settings), new(Guid.NewGuid(), "Test", "USD", "UTC"));
        var service = new ReconciliationService { Session = session, Database = new(fixture.Settings) };
        var first = entry.Lines[0];
        var request = new ReconciliationRequest(entry.EntryId!.Value, first.LineId!.Value, entry.Version!, first.Version!, 2);
        var status = service.SetAsync(request);
        var edit = journal.SaveAsync(entry with
        {
            Description = "Edited"
        });
        await Task.WhenAll(status, edit);
        Assert.AreNotEqual(status.Result.IsSuccess, edit.Result.IsSuccess);
        var loaded = (await journal.LoadAsync(request.EntryId))!;
        Assert.AreEqual(status.Result.IsSuccess ? (byte)2 : (byte)0, loaded.Lines[0].Status);
        Assert.AreEqual(edit.Result.IsSuccess ? "Edited" : entry.Description, loaded.Description);
        Assert.AreEqual(Outcome.Conflict, (await service.SetAsync(request with
        {
            LineVersion = loaded.Lines[0].Version!
        })).Outcome);
        Assert.AreEqual(Outcome.Conflict, (await service.SetAsync(request with
        {
            EntryVersion = loaded.Version!
        })).Outcome);
        var after = (await journal.LoadAsync(request.EntryId))!;
        CollectionAssert.AreEqual(loaded.Version, after.Version);
        CollectionAssert.AreEqual(loaded.Lines[0].Version, after.Lines[0].Version);
        Assert.AreEqual(25m, after.Lines[0].Debit);
    }
}
