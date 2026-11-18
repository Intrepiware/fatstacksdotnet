using System.IO;
using FatStacks.Core.Connections;
using FatStacks.Core.Contracts;
using FatStacks.Infrastructure.Connections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.SqlTests.Connections;

[TestClass]
public sealed class ConnectionTests
{
    private static ConnectionService Service() => new() { Transport = new() };
    [TestMethod]
    public async Task FailedDeploymentDoesNotPublishVersionOrLeaveApplicationTables()
    {
        await using var db = await Fixtures.BookDatabaseFixture.CreateAsync();
        var scripts = Path.Combine(Path.GetTempPath(), "FatStacks_Scripts_" + Guid.NewGuid().ToString("N"));
        var source = Path.Combine(Fixtures.BookDatabaseFixture.RepositoryRoot(), "database");
        try
        {
            foreach (var file in Directory.EnumerateFiles(source, "*.sql", SearchOption.AllDirectories))
            {
                var target = Path.Combine(scripts, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target);
            }
            await File.WriteAllTextAsync(Path.Combine(scripts, "afterMigration", "998_Abort.sql"), "THROW 51002, 'Synthetic deployment failure', 1;");
            var setup = new DatabaseSetup { Transport = new(), ScriptDirectory = scripts };
            Assert.IsFalse((await setup.InitializeAsync(db.Settings, new("Test", "USD", "UTC"))).IsSuccess);
            await db.ExecuteAsync("IF OBJECT_ID('dbo.Book') IS NOT NULL OR OBJECT_ID('dbo.SchemaVersion') IS NOT NULL THROW 51003, 'Deployment did not roll back', 1;");
            Assert.IsFalse((await Service().TestAsync(db.Settings)).IsSuccess);
        }
        finally { if (Directory.Exists(scripts)) Directory.Delete(scripts, true); }
    }
    [TestMethod]
    public async Task EmptyDatabaseDoesNotOpenUntilExplicitSetupAndRepeatedSetupPreservesBook()
    {
        await using var db = await Fixtures.BookDatabaseFixture.CreateAsync();
        Assert.IsFalse((await Service().TestAsync(db.Settings)).IsSuccess);
        await db.InitializeAsync();
        var first = await Service().TestAsync(db.Settings); Assert.IsTrue(first.IsSuccess, first.Message);
        var repeated = await db.Setup().InitializeAsync(db.Settings, new("Different name", "EUR", "UTC"));
        Assert.IsTrue(repeated.IsSuccess, repeated.Message); Assert.AreEqual(first.Value!.StableIdentifier, repeated.Value!.StableIdentifier);
        Assert.AreEqual("USD", repeated.Value.CurrencyCode); Assert.AreEqual("Synthetic test book", repeated.Value.Name);
    }
    [TestMethod]
    public async Task DifferentBooksHaveDistinctIdentitiesAndMismatchIsRejected()
    {
        await using var a = await Fixtures.BookDatabaseFixture.CreateAsync(); await a.InitializeAsync();
        await using var b = await Fixtures.BookDatabaseFixture.CreateAsync(); await b.InitializeAsync("EUR");
        var first = (await Service().TestAsync(a.Settings)).Value!; var second = (await Service().TestAsync(b.Settings)).Value!;
        Assert.AreNotEqual(first.StableIdentifier, second.StableIdentifier);
        var profile = new ConnectionProfile(Guid.NewGuid(), "Test", b.Settings, first.StableIdentifier);
        Assert.AreEqual(Outcome.IdentityMismatch, (await Service().OpenAsync(profile)).Outcome);
        Assert.IsTrue((await Service().OpenAsync(profile with { ExpectedBookIdentifier = second.StableIdentifier })).IsSuccess);
    }
    [TestMethod]
    [DataRow("UPDATE dbo.SchemaVersion SET Version=2")]
    [DataRow("DELETE dbo.SchemaVersion")]
    [DataRow("DROP TABLE dbo.SchemaVersion")]
    public async Task IncompatibleMissingOrUnreadableSchemaNeverOpens(string change)
    {
        await using var db = await Fixtures.BookDatabaseFixture.CreateAsync(); await db.InitializeAsync(); await db.ExecuteAsync(change);
        Assert.IsFalse((await Service().TestAsync(db.Settings)).IsSuccess);
    }
    [TestMethod]
    public async Task UnrelatedSchemaIsNotModified()
    {
        await using var db = await Fixtures.BookDatabaseFixture.CreateAsync(); await db.ExecuteAsync("CREATE TABLE dbo.Unrelated(Id int NOT NULL)");
        var result = await db.Setup().InitializeAsync(db.Settings, new("Test", "USD", "UTC")); Assert.IsFalse(result.IsSuccess);
        await db.ExecuteAsync("IF OBJECT_ID('dbo.Book') IS NOT NULL THROW 51001,'Unexpected schema alteration',1;");
    }
    [TestMethod]
    public async Task CompatibleSchemaWithoutBookCanFinishSetup()
    {
        await using var db = await Fixtures.BookDatabaseFixture.CreateAsync(); await db.InitializeAsync(); await db.ExecuteAsync("DELETE dbo.Book");
        Assert.IsFalse((await Service().TestAsync(db.Settings)).IsSuccess);
        await db.InitializeAsync(); Assert.IsTrue((await Service().TestAsync(db.Settings)).IsSuccess);
    }
}
