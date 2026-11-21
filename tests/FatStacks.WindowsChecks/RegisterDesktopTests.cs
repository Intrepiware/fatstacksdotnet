using System.Windows.Automation;
using FatStacks.Core.Connections;
using FatStacks.Infrastructure.Configuration;
using FatStacks.SqlTests.Fixtures;
using FatStacks.WindowsChecks.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FatStacks.WindowsChecks;

[TestClass]
[DoNotParallelize]
public sealed class RegisterDesktopTests
{
    [TestMethod]
    public async Task SearchClosedAccountLinkAndRefreshWorkThroughTheRegister()
    {
        if (!OperatingSystem.IsWindows() || !Environment.UserInteractive)
            Assert.Inconclusive("Interactive Windows desktop required.");

        await using var book = await BookDatabaseFixture.CreateAsync();
        await book.InitializeAsync();
        await book.ExecuteAsync("""
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;
            DECLARE @today date = CONVERT(date, SYSUTCDATETIME());
            INSERT dbo.Account(BookId, Name, AccountTypeId, DisplayGroupId, OpenDate, CloseDate)
            VALUES (1, N'Synthetic Bank', 1, 1, DATEADD(day, -120, @today), NULL),
                   (1, N'Closed counter', 9, 5, DATEADD(day, -120, @today), @today);
            INSERT dbo.JournalEntry(BookId, AccountingDate, Description, EntryKind)
            VALUES (1, DATEADD(day, -1, @today), N'Register walkthrough', 0);
            DECLARE @entry bigint = SCOPE_IDENTITY();
            INSERT dbo.JournalEntryLine(JournalEntryId, AccountId, LineOrder, Debit, Credit)
            SELECT @entry, AccountId, CASE WHEN AccountTypeId = 1 THEN 1 ELSE 2 END,
                   CASE WHEN AccountTypeId = 1 THEN 25 ELSE 0 END,
                   CASE WHEN AccountTypeId = 1 THEN 0 ELSE 25 END
            FROM dbo.Account;
            COMMIT;
            """);

        using var desktop = new DesktopFixture();
        var store = new ProfileStore(desktop.ProfilePath, new WindowsSecretProtector());
        var profile = new ConnectionProfile(Guid.NewGuid(), "Register walkthrough", book.Settings);
        Assert.IsTrue((await store.SaveAsync(new(1, [profile], profile.ProfileId))).IsSuccess);
        await desktop.StartAsync();
        await desktop.WaitTextAsync("RegisterStatus", "1 transactions shown");

        await desktop.SetAsync("RegisterSearch", "no match");
        await desktop.WaitTextAsync("RegisterStatus", "0 transactions shown");
        await desktop.SetAsync("RegisterSearch", "CLOSED COUNTER");
        await desktop.WaitTextAsync("RegisterStatus", "1 transactions shown");

        var row = await WaitForDescendantAsync("RegisterGrid", new PropertyCondition(
            AutomationElement.ControlTypeProperty, ControlType.DataItem));
        ((SelectionItemPattern)row.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
        var closedLink = await WaitForDetailLinkAsync("Closed counter");
        var selectedEntry = (await desktop.WaitAsync("SelectedJournalEntry")).Current.Name;
        Assert.IsFalse(string.IsNullOrWhiteSpace(selectedEntry));
        ((InvokePattern)closedLink.GetCurrentPattern(InvokePattern.Pattern)).Invoke();

        await desktop.WaitTextAsync("RegisterAccountTitle", "Closed counter");
        await desktop.WaitValueAsync("RegisterSearch", "");
        await desktop.WaitTextAsync("SelectedJournalEntry", selectedEntry);
        await WaitForDetailLinkAsync("Closed counter");
        await book.ExecuteAsync("UPDATE dbo.Account SET Name = N'Renamed counter' WHERE Name = N'Closed counter';");
        await desktop.InvokeAsync("RefreshRegister");
        await desktop.WaitTextAsync("RegisterAccountTitle", "Renamed counter");
        await desktop.WaitTextAsync("SelectedJournalEntry", selectedEntry);
        // The detail link reappears with the new name only if refresh restores the selected entry.
        await WaitForDetailLinkAsync("Renamed counter");

        Task<AutomationElement> WaitForDetailLinkAsync(string name) => WaitForDescendantAsync("EntryDetails",
            new AndCondition(new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Hyperlink),
                new PropertyCondition(AutomationElement.NameProperty, name)));

        async Task<AutomationElement> WaitForDescendantAsync(string parentId, Condition condition)
        {
            for (var i = 0; i < 150; i++)
            {
                var child = desktop.Find(parentId)?.FindFirst(TreeScope.Descendants, condition);
                if (child is not null) return child;
                await Task.Delay(100);
            }

            var status = desktop.Find("RegisterStatus")?.Current.Name;
            throw new AssertFailedException($"The expected control did not appear in {parentId}. Register status: '{status}'.");
        }
    }
}
