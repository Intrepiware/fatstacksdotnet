using System.Runtime.InteropServices;
using System.Windows.Automation;
using FatStacks.Core.Connections;
using FatStacks.Infrastructure.Configuration;
using FatStacks.SqlTests.Fixtures;
using FatStacks.WindowsChecks.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FatStacks.WindowsChecks;

[TestClass]
[DoNotParallelize]
public sealed class ReconciliationJourneyTests
{
    [TestMethod]
    public async Task RepeatedPostingsSupportIndependentMouseAndKeyboardChangesThroughRestart()
    {
        if (!OperatingSystem.IsWindows() || !Environment.UserInteractive)
            Assert.Inconclusive("Interactive Windows desktop required.");
        await using var book = await BookDatabaseFixture.CreateAsync();
        await book.InitializeAsync();
        await book.ExecuteAsync("""
            INSERT dbo.Account(BookId,Name,AccountTypeId,DisplayGroupId,OpenDate)
            VALUES(1,'Bank',1,1,'20000101'),(1,'Card',3,2,'20000101');
            INSERT dbo.JournalEntry(BookId,AccountingDate,Description,EntryKind)
            VALUES(1,CONVERT(date,SYSUTCDATETIME()),'Reconciliation',0);
            INSERT dbo.JournalEntryLine(JournalEntryId,AccountId,LineOrder,Debit,Credit)
            VALUES(1,1,1,10,0),(1,1,2,15,0),(1,2,3,0,25);
            """);

        using var desktop = new DesktopFixture();
        var profile = new ConnectionProfile(Guid.NewGuid(), "Reconciliation", book.Settings);
        var store = new ProfileStore(desktop.ProfilePath, new WindowsSecretProtector());
        Assert.IsTrue((await store.SaveAsync(new(1, [profile], profile.ProfileId))).IsSuccess);
        await desktop.StartAsync();
        const string first = "RegisterC_1", second = "RegisterC_2";
        await desktop.WaitTextAsync("RegisterStatus", "2 transactions shown");
        await desktop.InvokeAsync(first);
        await desktop.WaitTextAsync(first, "Reconciling");
        (await desktop.WaitAsync(first)).SetFocus();
        Press(0x20); // Space on the focused button.
        await desktop.WaitTextAsync(first, "Cleared");
        (await desktop.WaitAsync(first)).SetFocus();
        Press(0x28); // Down selects the second posting.
        Press(0x52); // R
        await desktop.WaitTextAsync(second, "Reconciling");
        var expectedStates = new[] { "Uncleared", "Reconciling", "Cleared" };
        string[] readOnlyStates = [];
        for (int attempt = 0; attempt < 150; attempt++)
        {
            var details = await desktop.WaitAsync("EntryDetails");
            readOnlyStates = details.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text))
                .Cast<AutomationElement>().Select(x => x.Current.Name).ToArray();
            if (expectedStates.All(readOnlyStates.Contains))
                break;
            await Task.Delay(100);
        }
        CollectionAssert.IsSubsetOf(expectedStates, readOnlyStates);
        (await desktop.WaitAsync(second)).SetFocus();
        Press(0x26); // Up selects the first posting.
        Press(0x55); // U
        await desktop.WaitTextAsync(first, "Uncleared");
        (await desktop.WaitAsync(first)).SetFocus();
        Press(0x43); // C
        await desktop.WaitTextAsync(first, "Cleared");

        await book.ExecuteAsync("UPDATE dbo.JournalEntry SET UpdateDate=SYSUTCDATETIME() WHERE JournalEntryId=1;");
        await desktop.InvokeAsync(second);
        await desktop.WaitTextAsync("ReconciliationError", "Refresh");
        await desktop.WaitTextAsync(second, "Reconciling");
        await desktop.InvokeAsync("RefreshRegister");
        await desktop.WaitTextAsync("RegisterStatus", "2 transactions shown");
        var grid = await desktop.WaitAsync("RegisterGrid");
        var selection = ((SelectionPattern)grid.GetCurrentPattern(SelectionPattern.Pattern)).Current.GetSelection();
        Assert.HasCount(1, selection);
        Assert.IsNotNull(selection[0].FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, second)));
        desktop.Stop();
        await desktop.StartAsync();
        await desktop.WaitTextAsync(first, "Cleared");
        await desktop.WaitTextAsync(second, "Reconciling");
        await desktop.WaitTextAsync("CurrentBalance", 25m.ToString("N2", System.Globalization.CultureInfo.CurrentCulture));
    }

    [TestMethod]
    [DataRow("Uncleared", "Reconciling")]
    [DataRow("Uncleared", "Cleared")]
    [DataRow("Reconciling", "Uncleared")]
    [DataRow("Reconciling", "Cleared")]
    [DataRow("Cleared", "Uncleared")]
    [DataRow("Cleared", "Reconciling")]
    public async Task TransferStatusPairSurvivesRefreshAndRestart(string bank, string card)
    {
        if (!OperatingSystem.IsWindows() || !Environment.UserInteractive)
            Assert.Inconclusive("Interactive Windows desktop required.");
        await using var book = await BookDatabaseFixture.CreateAsync();
        await book.InitializeAsync();
        await book.ExecuteAsync("""
            INSERT dbo.Account(BookId,Name,AccountTypeId,DisplayGroupId,OpenDate)
            VALUES(1,'Bank',1,1,'20000101'),(1,'Card',3,2,'20000101');
            INSERT dbo.JournalEntry(BookId,AccountingDate,Description,EntryKind)
            VALUES(1,CONVERT(date,SYSUTCDATETIME()),'Transfer',0);
            INSERT dbo.JournalEntryLine(JournalEntryId,AccountId,LineOrder,Debit,Credit)
            VALUES(1,1,1,25,0),(1,2,2,0,25);
            """);
        using var desktop = new DesktopFixture();
        var profile = new ConnectionProfile(Guid.NewGuid(), "Status pair", book.Settings);
        var store = new ProfileStore(desktop.ProfilePath, new WindowsSecretProtector());
        Assert.IsTrue((await store.SaveAsync(new(1, [profile], profile.ProfileId))).IsSuccess);
        await desktop.StartAsync();
        await ChooseAsync("RegisterC_1", bank);
        await OpenCounterAsync("Card");
        await ChooseAsync("RegisterC_2", card);
        await desktop.InvokeAsync("RefreshRegister");
        await desktop.WaitTextAsync("RegisterC_2", card);
        desktop.Stop();
        await desktop.StartAsync();
        await desktop.WaitTextAsync("RegisterC_1", bank);
        await desktop.WaitTextAsync("CurrentBalance", 25m.ToString("N2"));
        await OpenCounterAsync("Card");
        await desktop.WaitTextAsync("RegisterC_2", card);
        await desktop.WaitTextAsync("CurrentBalance", (-25m).ToString("N2"));

        async Task ChooseAsync(string id, string state)
        {
            await desktop.WaitTextAsync(id, "Uncleared");
            if (state == "Uncleared")
                return;
            var button = await desktop.WaitAsync(id);
            button.SetFocus();
            Press(state == "Reconciling" ? (byte)0x52 : (byte)0x43);
            await desktop.WaitTextAsync(id, state);
        }

        async Task OpenCounterAsync(string account)
        {
            var grid = await desktop.WaitAsync("RegisterGrid");
            var row = grid.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.DataItem));
            ((SelectionItemPattern)row.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
            var condition = new AndCondition(new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Hyperlink),
                new PropertyCondition(AutomationElement.NameProperty, account));
            for (var i = 0; i < 150; i++)
            {
                if (desktop.Find("EntryDetails")?.FindFirst(TreeScope.Descendants, condition) is { } link)
                {
                    ((InvokePattern)link.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
                    await desktop.WaitTextAsync("RegisterAccountTitle", account);
                    return;
                }
                await Task.Delay(100);
            }
            Assert.Fail($"The detail link for {account} did not appear.");
        }
    }

    private static void Press(byte key)
    {
        KeyEvent(key, 0, 0, UIntPtr.Zero);
        KeyEvent(key, 0, 2, UIntPtr.Zero);
    }

    [DllImport("user32.dll", EntryPoint = "keybd_event")]
    private static extern void KeyEvent(byte key, byte scanCode, uint flags, UIntPtr extraInfo);
}
