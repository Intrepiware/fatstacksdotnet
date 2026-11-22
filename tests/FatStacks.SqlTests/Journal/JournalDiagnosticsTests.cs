using System.Diagnostics;
using FatStacks.Core.Contracts;
using FatStacks.Infrastructure.Persistence;
using FatStacks.SqlTests.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.SqlTests.Journal;

[TestClass]
[DoNotParallelize]
public sealed class JournalDiagnosticsTests
{
    [TestMethod]
    public async Task SqlJournalFailureLogsOneSafeEventWithoutProviderTextOrValues()
    {
        const string sentinel="SYNTHETIC_PRIVATE_PASSWORD_FINANCIAL_TOKEN_PARAMETER";
        await using var fixture=await BookDatabaseFixture.CreateAsync(); await JournalWriteTests.SeedAsync(fixture);
        await fixture.ExecuteAsync("CREATE TRIGGER dbo.SyntheticPrivateFailure ON dbo.JournalEntryLine AFTER INSERT AS THROW 51099, 'SYNTHETIC_PRIVATE_PASSWORD_FINANCIAL_TOKEN_PARAMETER', 1;");
        using var output=new StringWriter(); using var listener=new TextWriterTraceListener(output); Trace.Listeners.Add(listener);
        try
        {
            var service=JournalWriteTests.Service(fixture.Settings);
            var request=JournalWriteTests.Draft(1,2,25) with { Payee=sentinel,Description=sentinel };
            Assert.AreEqual(Outcome.Failed,(await service.SaveAsync(request)).Outcome);
            listener.Flush(); var trace=output.ToString();
            StringAssert.Contains(trace,"operation=JournalSave category=SqlFailure");
            Assert.AreEqual(1,trace.Split("operation=",StringSplitOptions.None).Length-1);
            Assert.IsFalse(trace.Contains(sentinel,StringComparison.Ordinal));
            Assert.IsFalse(trace.Contains(fixture.Settings.ConnectionString,StringComparison.Ordinal));
        }
        finally { Trace.Listeners.Remove(listener); }
    }
}
