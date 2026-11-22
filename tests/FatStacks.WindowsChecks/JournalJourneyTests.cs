using System.Windows.Automation;
using FatStacks.Core.Connections;
using FatStacks.Infrastructure.Configuration;
using FatStacks.Infrastructure.Persistence;
using FatStacks.SqlTests.Fixtures;
using FatStacks.WindowsChecks.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.WindowsChecks;

[TestClass]
[DoNotParallelize]
public sealed class JournalJourneyTests
{
    [TestMethod]
    public async Task OpeningExpenseSplitEditFailureContinueAndDelete()
    {
        if (!OperatingSystem.IsWindows() || !Environment.UserInteractive) Assert.Inconclusive("Interactive Windows desktop required.");
        await using var book = await BookDatabaseFixture.CreateAsync(); await book.InitializeAsync();
        await book.ExecuteAsync("INSERT dbo.Account(BookId,Name,AccountTypeId,DisplayGroupId,OpenDate) VALUES(1,N'Bank',1,1,'20000101'),(1,N'Equity',9,5,'20000101'),(1,N'Expense A',11,4,'20000101'),(1,N'Expense B',11,4,'20000101');");
        using var desktop = new DesktopFixture();
        var store = new ProfileStore(desktop.ProfilePath, new WindowsSecretProtector()); var profile = new ConnectionProfile(Guid.NewGuid(), "Journal walkthrough", book.Settings);
        Assert.IsTrue((await store.SaveAsync(new(1, [profile], profile.ProfileId))).IsSuccess);
        await desktop.StartAsync(); await desktop.WaitTextAsync("RegisterStatus", "0 transactions shown");
        await desktop.InvokeAsync("NewJournal"); await FillAsync([("Bank", "1000", ""), ("Equity", "", "1000")]);
        var date = Value(await desktop.WaitAsync("AccountingDate"));
        await desktop.InvokeAsync("ContinueJournal"); await desktop.WaitTextAsync("JournalStatus", "saved");
        Assert.AreEqual(date, Value(await desktop.WaitAsync("AccountingDate")));
        await FillAsync([("Expense A", "25", ""), ("Bank", "", "25")]);
        await desktop.SetAsync("JournalDescription", "Expense");
        await desktop.InvokeAsync("SaveJournal"); await desktop.WaitTextAsync("RegisterStatus", "2 transactions shown");
        await SelectAsync("Expense"); await desktop.InvokeAsync("EditJournal");
        await FillAsync([("Expense A", "30", ""), ("Bank", "", "30")]);
        // Stale editor detects even an independent line update and retains the draft with two-decimal amount formatting.
        await book.ExecuteAsync("UPDATE dbo.JournalEntryLine SET ReconciliationStatus=2 WHERE AccountId=3;");
        await desktop.InvokeAsync("SaveJournal"); await desktop.WaitTextAsync("JournalStatus", "changed");
        Assert.AreEqual(30m.ToString("F2", System.Globalization.CultureInfo.CurrentCulture), Value(Controls("JournalDebit_")[0]));
        await desktop.InvokeAsync("CancelJournal");
        await desktop.InvokeAsync("EditJournal"); await FillAsync([("Expense A", "30", ""), ("Bank", "", "30")]);
        await desktop.InvokeAsync("SaveJournal"); await desktop.WaitTextAsync("RegisterStatus", "2 transactions shown");
        await SelectAsync("Expense"); await desktop.InvokeAsync("DeleteJournal");
        Assert.IsTrue((await desktop.WaitAsync("CancelDelete")).Current.HasKeyboardFocus);
        await desktop.InvokeAsync("CancelDelete"); Assert.AreEqual(970m, await BankAsync());
        await desktop.InvokeAsync("DeleteJournal"); await desktop.InvokeAsync("ConfirmDelete"); await desktop.WaitTextAsync("RegisterStatus", "1 transactions shown"); Assert.AreEqual(1000m, await BankAsync());
        await desktop.InvokeAsync("NewJournal"); await desktop.InvokeAsync("AddJournalLine");
        await FillAsync([("Expense A", "20", ""), ("Expense B", "5", ""), ("Bank", "", "25")]);
        await desktop.SetAsync("JournalDescription", "Split"); await desktop.InvokeAsync("SaveJournal"); await desktop.WaitTextAsync("RegisterStatus", "2 transactions shown");
        Assert.AreEqual(975m, await BankAsync()); desktop.Stop(); await desktop.StartAsync(); await desktop.WaitTextAsync("RegisterStatus", "2 transactions shown"); Assert.AreEqual(975m, await BankAsync());

        static string Value(AutomationElement element) => ((ValuePattern)element.GetCurrentPattern(ValuePattern.Pattern)).Current.Value;
        List<AutomationElement> Controls(string prefix) => (desktop.Find("JournalEditor")?.FindAll(TreeScope.Descendants, Condition.TrueCondition).Cast<AutomationElement>() ?? []).Where(x => x.Current.AutomationId.StartsWith(prefix, StringComparison.Ordinal)).ToList();
        async Task FillAsync((string Account, string Debit, string Credit)[] values)
        {
            await desktop.WaitAsync("JournalEditor");
            for (var attempt = 0; attempt < 100 && Controls("JournalAccount_").Count != values.Length; attempt++) await Task.Delay(50);
            var accounts = Controls("JournalAccount_"); var debits = Controls("JournalDebit_"); var credits = Controls("JournalCredit_"); Assert.AreEqual(values.Length, accounts.Count);
            for (var i = 0; i < values.Length; i++)
            {
                ((ValuePattern)accounts[i].GetCurrentPattern(ValuePattern.Pattern)).SetValue(values[i].Account);
                ((ValuePattern)debits[i].GetCurrentPattern(ValuePattern.Pattern)).SetValue(values[i].Debit);
                ((ValuePattern)credits[i].GetCurrentPattern(ValuePattern.Pattern)).SetValue(values[i].Credit);
            }
        }
        async Task SelectAsync(string text)
        {
            var grid = await desktop.WaitAsync("RegisterGrid");
            for (var attempt = 0; attempt < 100; attempt++)
            {
                foreach (AutomationElement row in grid.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.DataItem)))
                    if (row.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text)).Cast<AutomationElement>().Any(x => x.Current.Name.Contains(text, StringComparison.Ordinal))) { ((SelectionItemPattern)row.GetCurrentPattern(SelectionItemPattern.Pattern)).Select(); return; }
                await Task.Delay(100);
            }
            Assert.Fail("Journal row was not found.");
        }
        async Task<decimal> BankAsync() { await using var db = new BookDatabase(book.Settings).CreateContext(); return await db.JournalEntryLines.Where(x => x.AccountId == 1).SumAsync(x => x.Debit - x.Credit); }
    }
}
