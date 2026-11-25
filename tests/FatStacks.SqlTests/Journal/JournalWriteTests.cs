using FatStacks.Core.Connections;
using Microsoft.Data.SqlClient;
using FatStacks.Core.Contracts;
using FatStacks.Core.Journal;
using FatStacks.Infrastructure.Persistence;
using FatStacks.SqlTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.SqlTests.Journal;

[TestClass]
public sealed class JournalWriteTests
{
    private static readonly DateOnly Day = new(2026, 11, 21);
    public static async Task SeedAsync(BookDatabaseFixture fixture, string currency = "USD")
    {
        await fixture.InitializeAsync(currency);
        await fixture.ExecuteAsync("INSERT dbo.Account(BookId,Name,AccountTypeId,DisplayGroupId,OpenDate) VALUES(1,N'Bank',1,1,'20000101'),(1,N'Equity',9,5,'20000101'),(1,N'Expense',11,4,'20000101'),(1,N'Income',10,5,'20000101'),(1,N'Card',3,2,'20000101');");
    }
    public static JournalService Service(ConnectionSettings settings, string currency = "USD") => new() { Store = new StoredProcedureRepository { Database = new(settings) }, Accounts = new FatStacks.Infrastructure.Accounts.AccountService { Database = new(settings) }, Session = new(new(Guid.NewGuid(), "Synthetic", settings), new(Guid.NewGuid(), "Synthetic", currency, "UTC")) };
    public static JournalRequest Draft(long debit, long credit, decimal amount) => new(1, null, null, Day, null, "O'Brien", "'; DROP TABLE dbo.Book; --", 0, [new(null, null, debit, 1, amount, 0, "USD"), new(null, null, credit, 2, 0, amount, "USD")], []);
    private static JournalRequest Saved(OperationResult<JournalSaveResult> result) { Assert.IsTrue(result.IsSuccess, result.Message); return result.Value!.Entry; }
    [TestMethod]
    public async Task ExactReferenceSplitStableIdentityAndPhysicalDelete()
    {
        await using var fixture = await BookDatabaseFixture.CreateAsync(); await SeedAsync(fixture); var service = Service(fixture.Settings);
        var opening = Saved(await service.SaveAsync(Draft(1, 2, 1000)));
        var expense = Saved(await service.SaveAsync(Draft(3, 1, 25)));
        Saved(await service.SaveAsync(Draft(1, 4, 500))); Saved(await service.SaveAsync(Draft(5, 1, 100)));
        var split = Draft(3, 1, 25); Saved(await service.SaveAsync(split with { Lines = [split.Lines[0] with { Debit = 20 }, split.Lines[1], new(null, null, 3, 3, 5, 0, "USD")] }));
        var edited = Saved(await service.SaveAsync(expense with { Lines = expense.Lines.Select(x => x with { Debit = x.Debit == 0 ? 0 : 30, Credit = x.Credit == 0 ? 0 : 30 }).ToArray() }));
        CollectionAssert.AreEqual(expense.Lines.Select(x => x.LineId).ToArray(), edited.Lines.Select(x => x.LineId).ToArray());
        Assert.AreEqual(Outcome.Conflict, (await service.SaveAsync(expense)).Outcome);
        Assert.IsTrue((await service.DeleteAsync(new(1, edited.EntryId!.Value, edited.Version!, edited.OriginalLines))).IsSuccess);
        await using var db = new BookDatabase(fixture.Settings).CreateContext();
        Assert.IsFalse(await db.JournalEntryLines.AnyAsync(x => x.JournalEntryId == edited.EntryId));
        var bank = await db.JournalEntryLines.Where(x => x.AccountId == 1).SumAsync(x => x.Debit - x.Credit); Assert.AreEqual(1375m, bank);
        var amounts = await db.JournalEntryLines.GroupBy(x => x.AccountId).Select(g => new { AccountId = g.Key, Amount = g.Sum(x => x.Debit - x.Credit) }).ToDictionaryAsync(x => x.AccountId, x => x.Amount);
        Assert.AreEqual(-1000m, amounts[2]); Assert.AreEqual(25m, amounts[3]); Assert.AreEqual(-500m, amounts[4]); Assert.AreEqual(100m, amounts[5]);
        Assert.IsTrue((await db.Books.SingleAsync()).CurrencyLocked); Assert.AreEqual(opening.Description, (await service.LoadAsync(opening.EntryId!.Value))!.Description);
    }
    [TestMethod]
    [DataRow("USD")]
    [DataRow("EUR")]
    [DataRow("JPY")]
    public async Task SupportedCurrenciesRetainExactValuesAndRejectExcessPrecision(string currency)
    {
        await using var fixture = await BookDatabaseFixture.CreateAsync(); await SeedAsync(fixture, currency); var service = Service(fixture.Settings, currency);
        var amount = currency == "JPY" ? 123m : 1.2300m;
        JournalRequest InCurrency(decimal value) { var draft = Draft(1, 2, value); return draft with { Lines = draft.Lines.Select(x => x with { CurrencyCode = currency }).ToArray() }; }
        var saved = Saved(await service.SaveAsync(InCurrency(amount))); Assert.AreEqual(amount, saved.Lines[0].Debit);
        var invalid = InCurrency(currency == "JPY" ? 1.23m : 1.001m);
        Assert.AreEqual(Outcome.ValidationFailure, (await service.SaveAsync(invalid)).Outcome);
        Assert.AreEqual(Outcome.ValidationFailure, (await new StoredProcedureRepository { Database = new(fixture.Settings) }.SaveAsync(invalid)).Outcome);
    }
    [TestMethod]
    public async Task NullEntryKindRejectsCreateWithoutLockingCurrency()
    {
        await using var fixture = await BookDatabaseFixture.CreateAsync();
        await SeedAsync(fixture);
        await using var connection = new SqlConnection(fixture.Settings.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DECLARE @lines dbo.JournalEntryLines, @versions dbo.JournalEntryLineVersions;
            INSERT @lines (AccountId, LineOrder, Debit, Credit, CurrencyCode, ReconciliationStatus)
            VALUES (1, 1, 25, 0, N'USD', 0), (2, 2, 0, 25, N'USD', 0);
            EXEC dbo.SaveJournalEntry @BookId=1, @AccountingDate='20261121', @Description=N'Test',
                @EntryKind=NULL, @Lines=@lines, @OriginalLines=@versions;
            """;
        var error = await Assert.ThrowsExactlyAsync<SqlException>(() => command.ExecuteNonQueryAsync());
        Assert.AreEqual(51001, error.Number);
        await using var db = new BookDatabase(fixture.Settings).CreateContext();
        Assert.IsFalse(await db.JournalEntries.AnyAsync());
        Assert.IsFalse(await db.JournalEntryLines.AnyAsync());
        Assert.IsFalse((await db.Books.SingleAsync()).CurrencyLocked);
    }

    [TestMethod]
    public async Task ReorderRetainsIdsAndOnlyFinancialChangesResetStates()
    {
        await using var fixture = await BookDatabaseFixture.CreateAsync(); await SeedAsync(fixture); var service = Service(fixture.Settings);
        var created = Saved(await service.SaveAsync(Draft(1, 2, 1000)));
        await fixture.ExecuteAsync("UPDATE dbo.JournalEntryLine SET ReconciliationStatus=2;");
        var loaded = (await service.LoadAsync(created.EntryId!.Value))!;
        var reordered = Saved(await service.SaveAsync(loaded with { Description = "renamed", Lines = loaded.Lines.Reverse().Select((x, i) => x with { Order = i + 1 }).ToArray() }));
        Assert.IsTrue(reordered.Lines.All(x => x.Status == 2));
        var result = await service.SaveAsync(reordered with { Lines = reordered.Lines.Select(x => x.AccountId == 1 ? x with { AccountId = 3 } : x).ToArray() });
        Assert.IsTrue(result.IsSuccess, result.Message); Assert.HasCount(1, result.Value!.ResetLineIds);
        Assert.AreEqual((byte)2, result.Value.Entry.Lines.Single(x => x.AccountId == 2).Status);
        Assert.AreEqual((byte)0, result.Value.Entry.Lines.Single(x => x.AccountId == 3).Status);
        var entry = result.Value.Entry;
        var replaced = Saved(await service.SaveAsync(entry with { Lines = [entry.Lines[0], new(null, null, 1, 2, 1000, 0, "USD")] }));
        Assert.AreEqual(entry.Lines[0].LineId, replaced.Lines[0].LineId);
        Assert.AreNotEqual(entry.Lines[1].LineId, replaced.Lines[1].LineId);
    }
    [TestMethod]
    public async Task RuntimeRejectsDdlAndSqlRevalidatesPrecisionAndBalance()
    {
        await using var fixture = await BookDatabaseFixture.CreateAsync(); await SeedAsync(fixture); var runtime = await fixture.MapRuntimeLoginAsync();
        await using var connection = new SqlConnection(runtime.ConnectionString); await connection.OpenAsync();
        await using var command = connection.CreateCommand(); command.CommandText = "CREATE TABLE dbo.DeniedRuntimeTable(Id int)";
        try { await command.ExecuteNonQueryAsync(); Assert.Fail("Runtime DDL succeeded."); } catch (SqlException ex) { Assert.AreEqual(262, ex.Number); }
        var repository = new StoredProcedureRepository { Database = new(runtime) };
        foreach (var value in new[] { 1.001m, 100000000m, 0m, -1m }) Assert.AreEqual(Outcome.ValidationFailure, (await repository.SaveAsync(Draft(1, 2, value))).Outcome);
        var maximum = Draft(1, 2, 99999999.99m);
        var large = await repository.SaveAsync(maximum with { Lines = maximum.Lines.Concat(maximum.Lines.Select(x => x with { Order = x.Order + 2 })).ToArray() });
        Assert.IsTrue(large.IsSuccess, large.Message);
        var draft = Draft(1, 2, 1.2300m); Assert.IsTrue((await repository.SaveAsync(draft)).IsSuccess);
        Assert.AreEqual(Outcome.ValidationFailure, (await repository.SaveAsync(draft with { Lines = [draft.Lines[0] with { Debit = 2 }, draft.Lines[1]] })).Outcome);
    }
    [TestMethod]
    public async Task ClosedAccountRulesRejectReassignmentsWithoutChangingStoredEntry()
    {
        await using var fixture = await BookDatabaseFixture.CreateAsync(); await SeedAsync(fixture); var service = Service(fixture.Settings);
        var original = Saved(await service.SaveAsync(Draft(1, 2, 25) with { AccountingDate = new(2026, 11, 19) }));
        await fixture.ExecuteAsync("UPDATE dbo.Account SET CloseDate='20261120' WHERE AccountId=2; INSERT dbo.Account(BookId,Name,AccountTypeId,DisplayGroupId,OpenDate,CloseDate) VALUES(1,N'Closed replacement',9,5,'20000101','20261120');");
        var changed = original with { Lines = original.Lines.Select(x => x.AccountId == 2 ? x with { AccountId = 6 } : x).ToArray() };
        var result = await service.SaveAsync(changed);
        Assert.AreEqual(Outcome.ValidationFailure, result.Outcome);
        Assert.IsTrue(result.Errors!.Any(x => x.Field == "Account" && x.LineOrder == 2 && x.Message.Contains("closed", StringComparison.OrdinalIgnoreCase)));
        Assert.AreEqual(Outcome.ValidationFailure, (await new StoredProcedureRepository { Database = new(fixture.Settings) }.SaveAsync(changed)).Outcome);
        var unchanged = (await service.LoadAsync(original.EntryId!.Value))!;
        CollectionAssert.AreEqual(original.Version!, unchanged.Version!);
        foreach (var line in original.Lines) CollectionAssert.AreEqual(line.Version!, unchanged.Lines.Single(x => x.LineId == line.LineId).Version!);
        var retained = Saved(await service.SaveAsync(original with { Description = "Historical reference retained" }));
        Assert.AreEqual(2L, retained.Lines.Single(x => x.Order == 2).AccountId);
        Assert.AreEqual(Outcome.ValidationFailure, (await service.SaveAsync(retained with { AccountingDate = new(2026, 11, 20) })).Outcome);
        Assert.AreEqual(Outcome.ValidationFailure, (await service.SaveAsync(Draft(1, 2, 25) with { AccountingDate = new(2026, 11, 19) })).Outcome);
        await using var db = new BookDatabase(fixture.Settings).CreateContext();
        Assert.AreEqual(1, await db.JournalEntries.CountAsync()); Assert.AreEqual(2, await db.JournalEntryLines.CountAsync());
    }
    [TestMethod]
    public async Task CompleteLineVersionsAndExternalChangesConflict()
    {
        await using var fixture = await BookDatabaseFixture.CreateAsync(); await SeedAsync(fixture); var service = Service(fixture.Settings);
        var saved = Saved(await service.SaveAsync(Draft(1, 2, 1000)));
        Assert.AreEqual(Outcome.Conflict, (await service.DeleteAsync(new(1, saved.EntryId!.Value, saved.Version!, saved.OriginalLines.Take(1).ToArray()))).Outcome);
        await fixture.ExecuteAsync("UPDATE dbo.JournalEntryLine SET ReconciliationStatus=2 WHERE AccountId=1;");
        Assert.AreEqual(Outcome.Conflict, (await service.SaveAsync(saved)).Outcome);
        Assert.AreEqual(Outcome.Conflict, (await service.DeleteAsync(new(1, saved.EntryId.Value, saved.Version!, saved.OriginalLines))).Outcome);
    }
    [TestMethod]
    public async Task RuntimeCanWriteWithoutDdlAndFailuresRollback()
    {
        await using var fixture = await BookDatabaseFixture.CreateAsync(); await SeedAsync(fixture); var runtime = await fixture.MapRuntimeLoginAsync(); var service = Service(runtime);
        await fixture.ExecuteAsync("CREATE TRIGGER dbo.FailJournalInsert ON dbo.JournalEntryLine AFTER INSERT AS THROW 51099, 'Synthetic failure', 1;");
        Assert.AreEqual(Outcome.Failed, (await service.SaveAsync(Draft(1, 2, 1000))).Outcome);
        await using (var db = new BookDatabase(fixture.Settings).CreateContext()) { Assert.AreEqual(0, await db.JournalEntries.CountAsync()); Assert.IsFalse((await db.Books.SingleAsync()).CurrencyLocked); }
        await fixture.ExecuteAsync("DROP TRIGGER dbo.FailJournalInsert;");
        var saved = Saved(await service.SaveAsync(Draft(1, 2, 1000)));
        await fixture.ExecuteAsync("CREATE TRIGGER dbo.FailJournalUpdate ON dbo.JournalEntryLine AFTER UPDATE AS THROW 51099, 'Synthetic failure', 1;");
        Assert.AreEqual(Outcome.Failed, (await service.SaveAsync(saved with { Description = "changed" })).Outcome);
        Assert.AreEqual(saved.Description, (await service.LoadAsync(saved.EntryId!.Value))!.Description);
        await fixture.ExecuteAsync("DROP TRIGGER dbo.FailJournalUpdate;");
        await fixture.ExecuteAsync("CREATE TRIGGER dbo.FailJournalDelete ON dbo.JournalEntry AFTER DELETE AS THROW 51099, 'Synthetic failure', 1;");
        Assert.AreEqual(Outcome.Failed, (await service.DeleteAsync(new(1, saved.EntryId!.Value, saved.Version!, saved.OriginalLines))).Outcome);
        var retained = await service.LoadAsync(saved.EntryId.Value); Assert.IsNotNull(retained);
        CollectionAssert.AreEqual(saved.Version!, retained.Version!);
        foreach (var line in saved.Lines) CollectionAssert.AreEqual(line.Version!, retained.Lines.Single(x => x.LineId == line.LineId).Version!);
        await fixture.ExecuteAsync("DROP TRIGGER dbo.FailJournalDelete;");
        Assert.IsTrue((await service.DeleteAsync(new(1, saved.EntryId.Value, saved.Version!, saved.OriginalLines))).IsSuccess);
        await using var empty = new BookDatabase(fixture.Settings).CreateContext(); Assert.AreEqual(0, await empty.JournalEntries.CountAsync()); Assert.IsTrue((await empty.Books.SingleAsync()).CurrencyLocked);
    }
}
