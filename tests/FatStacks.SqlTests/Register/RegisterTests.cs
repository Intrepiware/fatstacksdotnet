using FatStacks.Infrastructure.Persistence;
using FatStacks.Infrastructure.Persistence.Entities;
using FatStacks.Infrastructure.Register;
using FatStacks.SqlTests.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.SqlTests.Register;

[TestClass]
public sealed class RegisterTests
{
    [TestMethod]
    public async Task WindowIncludesCutoffAndTodayWithOlderBalanceAndExcludesFuture()
    {
        await using var fixture = await BookDatabaseFixture.CreateAsync(); await fixture.InitializeAsync();
        await fixture.ExecuteAsync("""
            BEGIN TRANSACTION;
            INSERT dbo.Account(BookId,Name,AccountTypeId,DisplayGroupId,OpenDate) VALUES(1,'Bank',1,1,'2020-01-01'),(1,'Equity',9,5,'2020-01-01');
            DECLARE @bank bigint=(SELECT AccountId FROM dbo.Account WHERE Name='Bank'), @equity bigint=(SELECT AccountId FROM dbo.Account WHERE Name='Equity');
            DECLARE @dates TABLE(D date,A decimal(10,2));
            INSERT @dates VALUES(DATEADD(day,-90,CONVERT(date,'2026-11-18')),1000),(DATEADD(day,-89,CONVERT(date,'2026-11-18')),100),('2026-11-18',-25),('2026-11-19',100);
            DECLARE @d date,@a decimal(10,2),@id bigint;
            DECLARE entries CURSOR LOCAL FAST_FORWARD FOR SELECT D,A FROM @dates ORDER BY D;
            OPEN entries; FETCH NEXT FROM entries INTO @d,@a;
            WHILE @@FETCH_STATUS=0
            BEGIN
                INSERT dbo.JournalEntry(BookId,AccountingDate,Description,EntryKind) VALUES(1,@d,'Synthetic',0); SET @id=SCOPE_IDENTITY();
                INSERT dbo.JournalEntryLine(JournalEntryId,AccountId,LineOrder,Debit,Credit)
                VALUES(@id,@bank,1,CASE WHEN @a>0 THEN @a ELSE 0 END,CASE WHEN @a<0 THEN -@a ELSE 0 END),
                      (@id,@equity,2,CASE WHEN @a<0 THEN -@a ELSE 0 END,CASE WHEN @a>0 THEN @a ELSE 0 END);
                FETCH NEXT FROM entries INTO @d,@a;
            END;
            CLOSE entries; DEALLOCATE entries; COMMIT;
            """);
        var database = new BookDatabase(fixture.Settings);
        await using var db = database.CreateContext();
        var bank = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(db.Accounts.Where(x => x.Name == "Bank"));
        var result = await new StoredProcedureRepository { Database = database }.ReadAccountRegisterAsync(bank.AccountId, new(2026, 11, 18), "");
        Assert.AreEqual(1075m, result.CurrentBalance); Assert.AreEqual(2, result.Rows.Count);
        Assert.AreEqual(new DateOnly(2026, 11, 18).AddDays(-89), result.Rows[0].Date);
        Assert.AreEqual(100m, result.Rows[0].Deposit); Assert.AreEqual(1100m, result.Rows[0].Balance);
        Assert.AreEqual(25m, result.Rows[1].Payment); Assert.AreEqual(1075m, result.Rows[1].Balance);
    }
    [TestMethod]
    public async Task FullHistoryBeforeLiteralSearchAndDateFiltersWithSplitsAndClosedDescendants()
    {
        await using var fixture = await BookDatabaseFixture.CreateAsync(); await fixture.InitializeAsync();
        var database = new BookDatabase(fixture.Settings);
        long bank, closed, first, zero;
        await using (var db = database.CreateContext())
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var parent = new AccountEntity { BookId = 1, Name = "Parent", AccountTypeId = 1, DisplayGroupId = 1, OpenDate = new(2020, 1, 1) };
            db.Accounts.Add(parent); await db.SaveChangesAsync();
            var child = new AccountEntity { BookId = 1, ParentAccountId = parent.AccountId, Name = "Closed child", AccountTypeId = 1, DisplayGroupId = 1, OpenDate = new(2020, 1, 1), CloseDate = new(2026, 11, 18) };
            var equity = new AccountEntity { BookId = 1, Name = "Counter[%_]", AccountTypeId = 9, DisplayGroupId = 5, OpenDate = new(2020, 1, 1) };
            db.Accounts.AddRange(child, equity); await db.SaveChangesAsync(); bank = parent.AccountId; closed = child.AccountId;
            async Task<long> Entry(DateOnly date, string description, params (long account, decimal debit, decimal credit)[] lines)
            {
                var entry = new JournalEntryEntity { BookId = 1, AccountingDate = date, Description = description }; db.JournalEntries.Add(entry); await db.SaveChangesAsync();
                db.JournalEntryLines.AddRange(lines.Select((x, i) => new JournalEntryLineEntity { JournalEntryId = entry.JournalEntryId, AccountId = x.account, LineOrder = i + 1, Debit = x.debit, Credit = x.credit, ReconciliationStatus = (byte)(i % 3) }));
                await db.SaveChangesAsync(); return entry.JournalEntryId;
            }
            await Entry(new(2020, 1, 1), "Old opening", (bank, 99999999.99m, 0), (equity.AccountId, 0, 99999999.99m));
            await Entry(new(2020, 1, 2), "Old opening", (bank, 99999999.99m, 0), (equity.AccountId, 0, 99999999.99m));
            first = await Entry(new(2026, 11, 17), "SPLIT literal[%_]", (bank, 0, 25), (equity.AccountId, 20, 0), (closed, 5, 0));
            zero = await Entry(new(2026, 11, 17), "Zero", (bank, 10, 0), (bank, 0, 10));
            await Entry(new(2026, 11, 19), "Future", (bank, 100, 0), (equity.AccountId, 0, 100));
            await transaction.CommitAsync();
        }
        var service = new RegisterService { Database = database, Procedures = new() { Database = database } };
        var all = await service.ReadAsync(bank, new(2026, 11, 18), "");
        Assert.AreEqual(199999974.98m, all.CurrentBalance); Assert.AreEqual(2, all.Rows.Count);
        Assert.AreEqual(first, all.Rows[0].EntryId); Assert.AreEqual(zero, all.Rows[1].EntryId);
        Assert.AreEqual(25m, all.Rows[0].Payment); Assert.IsNull(all.Rows[1].Payment); Assert.IsNull(all.Rows[1].Deposit); Assert.AreEqual("Mixed", all.Rows[1].Status);
        foreach (var search in new[] { "split", "counter[%_]", "[%_]" })
        {
            var filtered = await service.ReadAsync(bank, new(2026, 11, 18), search);
            Assert.AreEqual(1, filtered.Rows.Count); Assert.AreEqual(all.Rows[0].Balance, filtered.Rows[0].Balance); Assert.AreEqual(all.CurrentBalance, filtered.CurrentBalance);
        }
        Assert.AreEqual(0, (await service.ReadAsync(bank, new(2026, 11, 18), "absent")).Rows.Count);
        var details = await service.DetailsAsync(first); Assert.AreEqual(3, details.Count); Assert.IsTrue(details.Any(x => x.AccountId == closed));
        var totals = await service.BalancesAsync(new(2026, 11, 18)); Assert.AreEqual(199999979.98m, totals[bank]); Assert.AreEqual(5m, totals[closed]);
    }
}
