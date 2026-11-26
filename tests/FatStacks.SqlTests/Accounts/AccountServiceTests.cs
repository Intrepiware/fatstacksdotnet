using FatStacks.Core.Accounts;
using FatStacks.Infrastructure.Accounts;
using FatStacks.Infrastructure.Persistence;
using FatStacks.SqlTests.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.SqlTests.Accounts;

[TestClass]
public sealed class AccountServiceTests
{
    [TestMethod]
    public async Task CreatesEveryTypeRenamesWithoutChangingIdentityAndValidatesHistory()
    {
        await using var fixture = await BookDatabaseFixture.CreateAsync();
        await fixture.InitializeAsync();
        var service = new AccountService { Database = new(fixture.Settings) };
        foreach (var type in AccountCatalog.Types)
        {
            var result = await service.SaveAsync(new(null, null, type.Name, type.Id, null, new(2020, 1, 1), null));
            Assert.IsTrue(result.IsSuccess, result.Message);
            Assert.AreEqual(type.DefaultGroupId, result.Value!.GroupId);
        }
        var bank = (await service.ListAsync()).Single(x => x.TypeId == 1);
        var renamed = await service.SaveAsync(new(bank.AccountId, null, " Renamed ", 1, 5, bank.OpenDate, null));
        Assert.IsTrue(renamed.IsSuccess);
        Assert.AreEqual(bank.AccountId, renamed.Value!.AccountId);
        Assert.AreEqual("Renamed", renamed.Value.Name);
        Assert.AreEqual((byte)5, renamed.Value.GroupId);
        Assert.IsFalse((await service.SaveAsync(new(null, null, "RENAMED", 1, null, bank.OpenDate, null))).IsSuccess);
        var child = await service.SaveAsync(new(null, bank.AccountId, "Child", 1, null, bank.OpenDate, null));
        Assert.IsTrue(child.IsSuccess);
        Assert.IsFalse((await service.SaveAsync(new(bank.AccountId, child.Value!.AccountId, "Renamed", 1, 5, bank.OpenDate, null))).IsSuccess);
        await using (var db = new BookDatabase(fixture.Settings).CreateContext())
        {
            var e = new FatStacks.Infrastructure.Persistence.Entities.JournalEntryEntity { BookId = 1, AccountingDate = new(2026, 11, 17), Description = "Synthetic" };
            db.JournalEntries.Add(e);
            await db.SaveChangesAsync();
            var counter = (await service.ListAsync()).Single(x => x.TypeId == 9);
            db.JournalEntryLines.AddRange(new()
            {
                JournalEntryId = e.JournalEntryId,
                AccountId = bank.AccountId,
                LineOrder = 1,
                Debit = 25
            }, new()
            {
                JournalEntryId = e.JournalEntryId,
                AccountId = counter.AccountId,
                LineOrder = 2,
                Credit = 25
            });
            await db.SaveChangesAsync();
        }
        Assert.IsFalse((await service.SaveAsync(new(bank.AccountId, null, "Renamed", 1, 5, bank.OpenDate, new(2026, 11, 17)))).IsSuccess);
        Assert.IsFalse((await service.SaveAsync(new(bank.AccountId, null, "Renamed", 1, 5, new(2026, 11, 18), null))).IsSuccess);
        Assert.IsTrue((await service.SaveAsync(new(bank.AccountId, null, "Renamed", 1, 5, bank.OpenDate, new(2026, 11, 18)))).IsSuccess);
        Assert.AreEqual(25m, (await new FatStacks.Infrastructure.Register.RegisterService { Database = new(fixture.Settings), Procedures = new() { Database = new(fixture.Settings) } }.BalancesAsync(new(2026, 11, 18)))[bank.AccountId]);
    }
}
