using FatStacks.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.SqlTests;

[TestClass]
public sealed class SchemaTests
{
    [TestMethod]
    public async Task SeparateRuntimeLoginCanOpenBookButCannotChangeSchema()
    {
        await using var fixture = await Fixtures.BookDatabaseFixture.CreateAsync();
        await fixture.InitializeAsync();
        var runtime = await fixture.MapRuntimeLoginAsync();
        var service = new FatStacks.Infrastructure.Connections.ConnectionService { Transport = new() };
        var result = await service.TestAsync(runtime);
        Assert.IsTrue(result.IsSuccess, result.Message);
        await using var connection = new SqlConnection(runtime.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE dbo.RuntimeMustNotCreate(Id int)";
        try
        {
            await command.ExecuteNonQueryAsync();
            Assert.Fail("Runtime DDL unexpectedly succeeded.");
        }
        catch (SqlException ex) { Assert.IsTrue(ex.Number is 262 or 2760 or 229, $"Unexpected permission category {ex.Number}."); }
    }
    [TestMethod]
    public async Task GrateSchemaMatchesEfAndRuntimeRoleCannotChangeSchema()
    {
        await using var fixture = await Fixtures.BookDatabaseFixture.CreateAsync();
        await fixture.InitializeAsync();
        await using var db = new BookDatabase(fixture.Settings).CreateContext();
        Assert.AreEqual(1, await db.Books.CountAsync());
        Assert.AreEqual(3, await db.Currencies.CountAsync());
        await fixture.ExecuteAsync("CREATE USER FatStacksTestRuntime WITHOUT LOGIN; ALTER ROLE FatStacksRuntime ADD MEMBER FatStacksTestRuntime;");
        await using var connection = new SqlConnection(fixture.Settings.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "EXECUTE AS USER='FatStacksTestRuntime'; SELECT COUNT(*) FROM dbo.Book";
        Assert.AreEqual(1, Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture));
        command.CommandText = "CREATE TABLE dbo.RuntimeMustNotCreate(Id int)";
        try
        {
            await command.ExecuteNonQueryAsync();
            Assert.Fail("Runtime DDL unexpectedly succeeded.");
        }
        catch (SqlException ex) { Assert.IsTrue(ex.Number is 262 or 2760 or 229, $"Unexpected permission category {ex.Number}."); }
    }
    [TestMethod]
    public async Task MoneyShapeAndStatusConstraintsRejectInvalidRows()
    {
        await using var fixture = await Fixtures.BookDatabaseFixture.CreateAsync();
        await fixture.InitializeAsync();
        await fixture.ExecuteAsync("INSERT dbo.Account(BookId,Name,AccountTypeId,DisplayGroupId,OpenDate) VALUES(1,'Bank',1,1,'2026-01-01'); INSERT dbo.JournalEntry(BookId,AccountingDate,Description,EntryKind) VALUES(1,'2026-01-01','Synthetic',0);");
        await using var connection = new SqlConnection(fixture.Settings.ConnectionString);
        await connection.OpenAsync();
        foreach (var values in new[] { "1,1,4", "0,0,0", "1,0,3", "-1,0,0" })
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "INSERT dbo.JournalEntryLine(JournalEntryId,AccountId,LineOrder,Debit,Credit,ReconciliationStatus) SELECT (SELECT TOP 1 JournalEntryId FROM dbo.JournalEntry),(SELECT TOP 1 AccountId FROM dbo.Account),1," + values;
            try
            {
                await command.ExecuteNonQueryAsync();
                Assert.Fail("Invalid line accepted.");
            }
            catch (SqlException ex) { Assert.AreEqual(547, ex.Number); }
        }
    }
}
