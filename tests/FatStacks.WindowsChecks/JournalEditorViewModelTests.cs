using FatStacks.Core.Accounts;
using FatStacks.Core.Calendar;
using FatStacks.Core.Contracts;
using FatStacks.Core.Journal;
using FatStacks.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.WindowsChecks;

[TestClass]
public sealed class JournalEditorViewModelTests
{
    private static readonly DateOnly Day = new(2026, 11, 21);
    public static readonly AccountRecord[] Accounts = [new(1, 1, null, "Bank", 1, 1, new(2000, 1, 1), null), new(2, 1, null, "Equity", 9, 5, new(2000, 1, 1), null)];
    public static JournalEditorViewModel Editor(IJournalService service)
    {
        var model = new JournalEditorViewModel { Journal = service, Calendar = new("UTC", new FixedClock(new(2026, 11, 21, 12, 0, 0, TimeSpan.Zero))) };
        model.Initialize(null, Accounts, "USD", Day, 1);
        model.Lines[0].Debit = "1000"; model.Lines[1].AccountText = "Equity"; model.Lines[1].Credit = "1000";
        return model;
    }
    [TestMethod]
    public async Task SaveBusyExcludesDuplicateAndContinueRetainsDate()
    {
        var service = new FakeJournal(); var model = Editor(service); var pending = model.SaveAsync(true);
        await model.SaveAsync(false); Assert.AreEqual(1, service.Calls); Assert.IsTrue(model.IsBusy);
        Assert.IsFalse(model.ApplyDateShortcut('+')); Assert.AreEqual(Day.ToDateTime(TimeOnly.MinValue), model.AccountingDate);
        service.Complete(Outcome.Success); await pending;
        Assert.AreEqual(Day.ToDateTime(TimeOnly.MinValue), model.AccountingDate); Assert.AreEqual("", model.Lines[0].Debit); Assert.IsFalse(model.IsBusy);
    }
    [TestMethod]
    [DataRow(Outcome.Failed)]
    [DataRow(Outcome.Conflict)]
    public async Task InvalidFieldsAndUnconfirmedCommitRetainDraft(Outcome outcome)
    {
        var service = new FakeJournal(); var model = Editor(service); model.Lines[0].Debit = "invalid";
        await model.SaveAsync(false); Assert.AreEqual(0, service.Calls); Assert.IsNotEmpty(model.Errors);
        model.Lines[0].Debit = "1000"; var pending = model.SaveAsync(false); service.Complete(outcome); await pending;
        Assert.AreEqual("1000", model.Lines[0].Debit); Assert.IsNull(model.LastSaved); Assert.IsFalse(model.ShouldClose);
        model.Cancel(); Assert.IsTrue(model.ShouldClose); Assert.AreEqual(1, service.Calls);
    }
    [TestMethod]
    public async Task CommittedSaveDoesNotDependOnProfilePreferences()
    {
        var service = new FakeJournal(); var model = Editor(service); var pending = model.SaveAsync(false); service.Complete(Outcome.Success); await pending;
        Assert.IsNotNull(model.LastSaved); Assert.IsTrue(model.ShouldClose); // Journal has no profile-store dependency.
    }
    [TestMethod]
    public void DateShortcutsCrossMonthYearAndLeapDayWithoutChangingTheOtherDate()
    {
        var model = Editor(new FakeJournal());
        model.AccountingDate = new(2024, 3, 1); model.BankPostingDate = new(2026, 1, 1);
        Assert.IsTrue(model.ApplyDateShortcut('-')); Assert.AreEqual(new DateTime(2024, 2, 29), model.AccountingDate);
        Assert.IsTrue(model.ApplyDateShortcut('+')); Assert.AreEqual(new DateTime(2024, 3, 1), model.AccountingDate);
        Assert.IsTrue(model.ApplyDateShortcut('-', true)); Assert.AreEqual(new DateTime(2025, 12, 31), model.BankPostingDate);
        Assert.AreEqual(new DateTime(2024, 3, 1), model.AccountingDate);
        model.BankPostingDate = null;
        Assert.IsTrue(model.ApplyDateShortcut('+', true)); Assert.AreEqual(new DateTime(2026, 11, 22), model.BankPostingDate);
        model.AccountingDate = DateTime.MinValue;
        Assert.IsFalse(model.ApplyDateShortcut('-')); Assert.AreEqual(DateTime.MinValue, model.AccountingDate);
        model.AccountingDate = DateTime.MaxValue.Date;
        Assert.IsFalse(model.ApplyDateShortcut('+')); Assert.AreEqual(DateTime.MaxValue.Date, model.AccountingDate);
    }
    [TestMethod]
    public void TodayShortcutUsesCurrentBookDayEvenAfterContinueRetainsAnOlderDate()
    {
        var clock = new FixedClock(new(2026, 11, 22, 7, 30, 0, TimeSpan.Zero));
        var model = new JournalEditorViewModel { Journal = new FakeJournal(), Calendar = new("Pacific Standard Time", clock) };
        model.Initialize(null, Accounts, "USD", new(2020, 1, 1));
        Assert.IsTrue(model.ApplyDateShortcut('t')); Assert.AreEqual(new DateTime(2026, 11, 21), model.AccountingDate);
        clock.Utc = new(2026, 11, 22, 8, 30, 0, TimeSpan.Zero);
        Assert.IsTrue(model.ApplyDateShortcut('T', true)); Assert.AreEqual(new DateTime(2026, 11, 22), model.BankPostingDate);
        Assert.AreEqual(new DateTime(2026, 11, 21), model.AccountingDate);
    }
    [TestMethod]
    public void PostingNumbersFollowRowsWithoutChangingControlIdentity()
    {
        var model = Editor(new FakeJournal());
        model.AddLine(); model.AddLine();
        var retained = model.Lines[3]; var id = retained.AccountControlId;
        model.RemoveLine(model.Lines[1]);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, model.Lines.Select(x => x.LineNumber).ToArray());
        Assert.AreEqual(id, retained.AccountControlId);
        Assert.IsTrue(model.Lines[1].IsEvenRow); Assert.IsFalse(retained.IsEvenRow);
    }
    [TestMethod]
    public void FootingNamesShortSideAndHidesTotalsForInvalidText()
    {
        var model = Editor(new FakeJournal());
        Assert.AreEqual("Balanced", model.BalanceStatus);
        model.Lines[0].Debit = "1001";
        StringAssert.StartsWith(model.BalanceStatus, "Credits short by ");
        Assert.IsTrue(model.DebitOver); Assert.IsFalse(model.CreditOver);
        model.Lines[0].Debit = "999";
        StringAssert.StartsWith(model.BalanceStatus, "Debits short by ");
        Assert.IsTrue(model.CreditOver); Assert.IsFalse(model.DebitOver);
        model.Lines[0].Debit = "bad";
        Assert.AreEqual("", model.DebitTotal); Assert.AreEqual("", model.CreditTotal);
        Assert.IsFalse(model.DebitOver); Assert.IsFalse(model.CreditOver);
        Assert.AreEqual("Enter valid amounts to check the balance.", model.BalanceStatus);
        Assert.IsTrue(model.Lines[0].HasErrors);
    }
    [TestMethod]
    public async Task CorrectionsClearOnlyResolvedErrorsAndKeepTheirRowIdentity()
    {
        var model = Editor(new FakeJournal());
        model.AddLine(); model.Lines[2].AccountText = "missing"; model.Lines[2].Debit = "bad";
        model.Lines[0].Debit = "100000000";
        Assert.IsFalse(await model.SaveAsync(false));
        Assert.IsNotEmpty(model.Lines[0].DebitError);
        var invalid = model.Lines[2];
        model.Lines[0].Debit = "1000";
        Assert.AreEqual("", model.Lines[0].DebitError); Assert.IsTrue(invalid.HasErrors);
        model.RemoveLine(model.Lines[1]);
        Assert.AreEqual(2, invalid.LineNumber);
        Assert.IsTrue(model.Errors.Any(x => x.LineOrder == 2 && x.Field == "Account"));
        invalid.AccountText = "Equity"; invalid.Credit = "1000";
        Assert.IsEmpty(model.Errors); Assert.IsFalse(invalid.HasErrors);
        Assert.AreEqual("Balanced", model.BalanceStatus);
    }
    [TestMethod]
    public void CorrectingEitherDateClearsItsErrorWithoutClearingTheOther()
    {
        var model = Editor(new FakeJournal());
        model.SetDateError(false, "Invalid accounting date."); model.SetDateError(true, "Invalid bank date.");
        model.AccountingDate = model.AccountingDate;
        Assert.AreEqual("", model.AccountingDateError); Assert.IsNotEmpty(model.BankPostingDateError);
        model.BankPostingDate = null;
        Assert.AreEqual("", model.BankPostingDateError); Assert.IsEmpty(model.Errors);
    }
    [TestMethod]
    public void ResetMarkersTrackReconciledEditsAndReversion()
    {
        var model = Editor(new FakeJournal());
        var lines = new[] { new JournalLine(1, new byte[8], 1, 1, 1000, 0, "USD", 2), new JournalLine(2, new byte[8], 2, 2, 0, 1000, "USD", 1) };
        model.Initialize(new(1, 1, new byte[8], Day, null, "", "", 0, lines, lines.Select(x => new LineVersion(x.LineId!.Value, x.Version!)).ToArray()), Accounts, "USD", Day);
        model.Lines[0].Debit = "1001"; model.Lines[1].AccountText = "Bank";
        Assert.IsTrue(model.Lines.All(x => x.WillReset));
        model.Lines[0].Debit = "1000"; model.Lines[1].AccountText = "Equity";
        Assert.IsTrue(model.Lines.All(x => !x.WillReset)); Assert.IsFalse(model.HasResetWarning);
    }
    private sealed class FixedClock(DateTimeOffset utc) : TimeProvider
    {
        public DateTimeOffset Utc { get; set; } = utc;
        public override DateTimeOffset GetUtcNow() => Utc;
    }
    [TestMethod]
    [DataRow("en-US", "25", "25.00")]
    [DataRow("en-US", "25.5", "25.50")]
    [DataRow("en-US", "1.2300", "1.23")]
    [DataRow("en-US", "1.001", "1.001")]
    [DataRow("en-US", "1.000000000000000000000000000001", "1.000000000000000000000000000001")]
    [DataRow("en-US", "1.230000000000000000000000000000", "1.23")]
    [DataRow("en-US", "", "")]
    [DataRow("en-US", "invalid", "invalid")]
    [DataRow("de-DE", "25,5", "25,50")]
    public void AmountFormattingAddsTwoPlacesWithoutRoundingInvalidDrafts(string culture, string input, string expected)
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(culture);
            var line = Editor(new FakeJournal()).Lines[0];
            line.Credit = "other draft"; line.Debit = input; line.FormatAmount(false);
            Assert.AreEqual(expected, line.Debit);
            Assert.AreEqual(string.IsNullOrWhiteSpace(input) ? "other draft" : "", line.Credit);
            line.Debit = "other draft"; line.Credit = input; line.FormatAmount(true);
            Assert.AreEqual(expected, line.Credit);
            Assert.AreEqual(string.IsNullOrWhiteSpace(input) ? "other draft" : "", line.Debit);
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
    }
    [TestMethod]
    public void OppositeAmountClearsAndBalanceUpdatesAsRowsChange()
    {
        var model = Editor(new FakeJournal());
        Assert.IsTrue(model.CanSave); Assert.IsFalse(model.IsUnbalanced);
        model.Lines[0].Credit = "25";
        Assert.AreEqual("", model.Lines[0].Debit);
        Assert.IsTrue(model.IsUnbalanced); Assert.IsFalse(model.CanSave);
        StringAssert.Contains(model.BalanceSummary, "Out of balance:");
        model.Lines[0].Debit = "1000";
        Assert.AreEqual("", model.Lines[0].Credit);
        Assert.IsFalse(model.IsUnbalanced); Assert.IsTrue(model.CanSave);
        model.AddLine(); model.Lines[2].Debit = "25";
        Assert.IsFalse(model.CanSave);
        model.RemoveLine(model.Lines[2]); Assert.IsTrue(model.CanSave);
        model.Lines[0].Debit = "invalid";
        StringAssert.Contains(model.BalanceSummary, "Enter valid amounts");
    }
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task SaveAndContinueRejectLongFractionBeforeDispatch(bool continueEntry, bool formatBeforeSave)
    {
        var oldCulture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            var service = new FakeJournal(); var model = Editor(service);
            const string amount = "1.000000000000000000000000000001";
            model.Lines[0].Debit = amount; model.Lines[1].Credit = "1";
            if (formatBeforeSave) model.Lines[0].FormatAmount(false);
            var pending = model.SaveAsync(continueEntry);
            if (service.Calls > 0) service.Complete(Outcome.Failed);
            Assert.IsFalse(await pending);
            Assert.AreEqual(0, service.Calls, "Invalid precision must be rejected before any write.");
            Assert.AreEqual(amount, model.Lines[0].Debit);
            Assert.IsTrue(model.Errors.Any(x => x.Field == "Debit" && x.LineOrder == 1));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = oldCulture; }
    }
    [TestMethod]
    [DataRow("en-US", "1.230000000000000000000000000000", "1.23")]
    [DataRow("de-DE", "1,230000000000000000000000000000", "1,23")]
    public async Task ExactTrailingZerosReachServiceWithoutChangingValue(string culture, string input, string credit)
    {
        var oldCulture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(culture);
            var service = new FakeJournal(); var model = Editor(service);
            model.Lines[0].Debit = input; model.Lines[1].Credit = credit;
            var pending = model.SaveAsync(false);
            Assert.AreEqual(1, service.Calls);
            Assert.AreEqual(1.23m, service.SavedRequest!.Lines[0].Debit);
            Assert.AreEqual(1.23m, service.SavedRequest.Lines[1].Credit);
            service.Complete(Outcome.Success); Assert.IsTrue(await pending);
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = oldCulture; }
    }
#if WINDOWS
    [STATestMethod]
    public void NewJournalDialogOpensWithReadOnlyBalanceStatusAndUpdatesItsFooting()
    {
        var service = new FakeJournal();
        var model = Editor(service);
        model.Initialize(null, Accounts, "USD", Day, 1);
        var window = new FatStacks.Desktop.Views.JournalEditorWindow(model);
        try
        {
            window.Show(); Pump();
            var footing = (System.Windows.Controls.Grid)window.FindName("TotalsFooting");
            var status = Descendants(footing).OfType<System.Windows.Controls.TextBlock>()
                .SelectMany(x => x.Inlines.OfType<System.Windows.Documents.Run>())
                .Single(x => System.Windows.Data.BindingOperations.GetBinding(x, System.Windows.Documents.Run.TextProperty)?.Path.Path == nameof(model.BalanceStatus));
            Assert.AreEqual(System.Windows.Data.BindingMode.OneWay,
                System.Windows.Data.BindingOperations.GetBinding(status, System.Windows.Documents.Run.TextProperty)!.Mode);
            Assert.AreEqual("Balanced", status.Text);
            model.Lines[0].Debit = "25"; Pump();
            StringAssert.StartsWith(status.Text, "Credits short by ");
            model.Lines[1].Credit = "25"; Pump();
            Assert.AreEqual("Balanced", status.Text);
            model.Lines[0].Debit = "invalid"; Pump();
            Assert.AreEqual("Enter valid amounts to check the balance.", status.Text);
            Assert.AreEqual(0, service.Calls);
        }
        finally { window.Close(); }
    }
    [STATestMethod]
    public void PostingFootingKeepsInputEdgesAndErrorsExposeAccessibleFeedback()
    {
        var model = Editor(new FakeJournal());
        var window = new FatStacks.Desktop.Views.JournalEditorWindow(model);
        try
        {
            window.Show(); Pump();
            System.Windows.Controls.TextBox Input(string id) => Descendants(window).OfType<System.Windows.Controls.TextBox>().Single(x => System.Windows.Automation.AutomationProperties.GetAutomationId(x) == id);
            var debit = Input(model.Lines[0].DebitControlId);
            var total = (System.Windows.Controls.TextBlock)window.FindName("DebitFooting");
            void CheckEdge()
            {
                var edge = debit.TranslatePoint(new(debit.ActualWidth - debit.BorderThickness.Right - debit.Padding.Right, 0), window).X;
                var totalEdge = total.TranslatePoint(new(total.ActualWidth, 0), window).X;
                Assert.AreEqual(edge, totalEdge, 1.0, "Totals must share the input text edge.");
            }
            CheckEdge();
            for (var i = 0; i < 20; i++) model.AddLine();
            Pump();
            var scroll = (System.Windows.Controls.ScrollViewer)window.FindName("PostingScroll");
            Assert.AreEqual(System.Windows.Visibility.Visible, scroll.ComputedVerticalScrollBarVisibility);
            CheckEdge();
            debit.Text = "invalid"; Pump();
            Assert.AreEqual("", total.Text);
            Assert.AreEqual(new System.Windows.Thickness(2), debit.BorderThickness);
            Assert.AreEqual("Invalid", System.Windows.Automation.AutomationProperties.GetItemStatus(debit));
            Assert.IsNotEmpty(System.Windows.Automation.AutomationProperties.GetHelpText(debit));
            Assert.AreEqual(model.Lines[0].DebitError, debit.ToolTip);
            Assert.IsTrue(System.Windows.Controls.Validation.GetHasError(debit));
            CheckEdge();
            debit.Text = "1000"; Pump();
            Assert.AreEqual("", System.Windows.Automation.AutomationProperties.GetItemStatus(debit));
            Assert.IsFalse(System.Windows.Controls.Validation.GetHasError(debit));
            Assert.IsFalse(model.Lines[0].HasErrors);
            debit.Focus(); Pump();
            var row = Descendants(window).OfType<System.Windows.Controls.Grid>().Single(x => ReferenceEquals(x.DataContext, model.Lines[0]) && x.ColumnDefinitions.Count == 6);
            Assert.AreEqual("#FFDDEBFA", row.Background.ToString());
            Assert.AreEqual("#FFFFFFFF", debit.Background.ToString());
        }
        finally { window.Close(); }
    }

    [STATestMethod]
    public void AmountColumnsAlignRightAndUnbalancedDraftDisablesSaveActions()
    {
        var model = Editor(new FakeJournal());
        var window = new FatStacks.Desktop.Views.JournalEditorWindow(model);
        try
        {
            window.Show(); Pump();
            var controls = Descendants(window).OfType<System.Windows.FrameworkElement>().ToArray();
            var debit = controls.OfType<System.Windows.Controls.TextBox>().Single(x => System.Windows.Automation.AutomationProperties.GetAutomationId(x) == model.Lines[0].DebitControlId);
            var credit = controls.OfType<System.Windows.Controls.TextBox>().Single(x => System.Windows.Automation.AutomationProperties.GetAutomationId(x) == model.Lines[0].CreditControlId);
            Assert.AreEqual(System.Windows.TextAlignment.Right, debit.TextAlignment);
            Assert.AreEqual(System.Windows.TextAlignment.Right, credit.TextAlignment);
            var actions = controls.OfType<System.Windows.Controls.Button>().Where(x => System.Windows.Automation.AutomationProperties.GetAutomationId(x) is "SaveJournal" or "ContinueJournal").ToArray();
            Assert.HasCount(2, actions); Assert.IsTrue(actions.All(x => x.IsEnabled));
            debit.Text = "25"; Pump();
            Assert.IsTrue(actions.All(x => !x.IsEnabled));
            model.Lines[1].Credit = "25"; Pump();
            Assert.IsTrue(actions.All(x => x.IsEnabled));
            var summary = controls.Single(x => System.Windows.Automation.AutomationProperties.GetAutomationId(x) == "JournalBalanceSummary");
            StringAssert.Contains(System.Windows.Automation.AutomationProperties.GetName(summary), "Balanced");
        }
        finally { window.Close(); }
    }
    [STATestMethod]
    public void ValidationMessagesSeparateLineFieldsAndOmitPrefixForEntryErrors()
    {
        var model = Editor(new FakeJournal());
        model.Errors.Add(new("Debit", "Enter a valid amount.", 4));
        model.Errors.Add(new("Amount", "Enter one positive Debit or Credit.", 4));
        model.Errors.Add(new("Balance", "Total Debit must equal total Credit."));
        var window = new FatStacks.Desktop.Views.JournalEditorWindow(model);
        try
        {
            window.Show();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            var messages = Descendants(window).OfType<System.Windows.Controls.TextBlock>()
                .Where(x => x.DataContext is JournalError)
                .Select(x => new System.Windows.Documents.TextRange(x.ContentStart, x.ContentEnd).Text).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                "Line 4 · Debit: Enter a valid amount.",
                "Line 4 · Amount: Enter one positive Debit or Credit.",
                "Balance: Total Debit must equal total Credit."
            }, messages);
        }
        finally { window.Close(); }
    }
    [STATestMethod]
    public void PayeeAndDescriptionExposeNamesWhenFocused()
    {
        var model = Editor(new FakeJournal());
        var window = new FatStacks.Desktop.Views.JournalEditorWindow(model);
        try
        {
            window.Show(); Pump();
            foreach (var (control, expectedName) in new[] { ("PayeeInput", "Payee"), ("DescriptionInput", "Description") })
            {
                var input = (System.Windows.Controls.TextBox)window.FindName(control);
                input.Focus(); Pump();
                Assert.IsTrue(input.IsKeyboardFocused);
                var peer = System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(input);
                Assert.IsNotNull(peer);
                Assert.AreEqual(expectedName, peer.GetName());
                input.Text = "Synthetic text"; Pump();
                Assert.AreEqual(expectedName, peer.GetName(), "Entered text must not replace the field name.");
            }
        }
        finally { window.Close(); }
        static void Pump() => System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }
    [STATestMethod]
    public void ReconciledEditWarningIsVisibleAndClearsWhenRestored()
    {
        var model = Editor(new FakeJournal());
        var entry = new JournalRequest(1, 10, new byte[8], Day, null, null, "Existing", 0,
            [new(101, new byte[8], 1, 1, 25, 0, "USD", 2), new(202, new byte[8], 2, 2, 0, 25, "USD", 2)],
            [new(101, new byte[8]), new(202, new byte[8])]);
        model.Initialize(entry, Accounts, "USD", Day);
        var window = new FatStacks.Desktop.Views.JournalEditorWindow(model);
        try
        {
            window.Show(); Pump();
            var warning = Descendants(window).OfType<System.Windows.Controls.TextBlock>()
                .Single(x => System.Windows.Automation.AutomationProperties.GetAutomationId(x) == "JournalResetWarning");
            Assert.IsFalse(warning.IsVisible);
            model.Lines[0].Debit = "26"; Pump();
            Assert.IsTrue(warning.IsVisible);
            StringAssert.Contains(warning.Text, "Line 1 (Bank)");
            model.Lines[0].Debit = "25"; Pump();
            Assert.IsFalse(warning.IsVisible);
        }
        finally { window.Close(); }
        static IEnumerable<System.Windows.DependencyObject> Descendants(System.Windows.DependencyObject parent)
        {
            for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i); yield return child;
                foreach (var nested in Descendants(child)) yield return nested;
            }
        }
        static void Pump() => System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }
    [STATestMethod]
    [DataRow(Outcome.Success)]
    [DataRow(Outcome.Failed)]
    public void ContinueFocusesDateOnlyAfterSuccessfulSaveAndKeepsEnteredDate(Outcome outcome)
    {
        var service = new FakeJournal(); var model = Editor(service);
        var window = new FatStacks.Desktop.Views.JournalEditorWindow(model);
        try
        {
            window.Show(); Pump();
            var date = (System.Windows.Controls.DatePicker)window.FindName("AccountingDateInput");
            Assert.AreEqual(Day.ToDateTime(TimeOnly.MinValue), model.AccountingDate);
            Assert.IsTrue(date.IsKeyboardFocusWithin);
            model.AccountingDate = new DateTime(2026, 10, 23); Pump();
            var description = (System.Windows.Controls.TextBox)window.FindName("DescriptionInput");
            description.Focus();
            Descendants(window).OfType<System.Windows.Controls.Button>().Single(x => System.Windows.Automation.AutomationProperties.GetAutomationId(x) == "ContinueJournal").RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Assert.IsTrue(model.IsBusy); service.Complete(outcome);
            var timer = System.Diagnostics.Stopwatch.StartNew();
            while (model.IsBusy && timer.Elapsed < TimeSpan.FromSeconds(5)) { Pump(); Thread.Sleep(1); }
            Pump(); Assert.IsFalse(model.IsBusy);
            Assert.AreEqual(new DateTime(2026, 10, 23), model.AccountingDate);
            Assert.AreEqual(outcome == Outcome.Success, date.IsKeyboardFocusWithin);
            Assert.AreEqual(outcome == Outcome.Success ? "" : "1000", model.Lines[0].Debit);
        }
        finally { window.Close(); }
    }
    [STATestMethod]
    public void AccountFocusSelectsItsTextAndAmountFocusLossFormatsOnlyExactValues()
    {
        var model = Editor(new FakeJournal());
        var window = new FatStacks.Desktop.Views.JournalEditorWindow(model);
        try
        {
            window.Show(); Pump();
            var fields = Descendants(window).OfType<System.Windows.Controls.TextBox>().ToArray();
            var account = fields.Single(x => System.Windows.Automation.AutomationProperties.GetAutomationId(x) == model.Lines[0].AccountControlId);
            var debit = fields.Single(x => System.Windows.Automation.AutomationProperties.GetAutomationId(x) == model.Lines[0].DebitControlId);
            var credit = fields.Single(x => System.Windows.Automation.AutomationProperties.GetAutomationId(x) == model.Lines[0].CreditControlId);
            var description = (System.Windows.Controls.TextBox)window.FindName("DescriptionInput");
            description.Focus(); description.MoveFocus(new(System.Windows.Input.FocusNavigationDirection.Next)); Pump();
            Assert.AreSame(account, System.Windows.Input.Keyboard.FocusedElement); Assert.AreEqual(account.Text, account.SelectedText);
            account.SelectedText = "Equ"; Assert.AreEqual("Equ", model.Lines[0].AccountText);
            Assert.IsTrue(model.Lines[0].Suggestions.Any(x => x.Path == "Equity"));
            var enter = new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice, System.Windows.PresentationSource.FromVisual(window), 0, System.Windows.Input.Key.Enter) { RoutedEvent = System.Windows.Input.Keyboard.PreviewKeyDownEvent };
            account.RaiseEvent(enter); Pump(); Assert.IsTrue(enter.Handled);
            Assert.AreEqual("Equity", model.Lines[0].AccountText); Assert.AreSame(debit, System.Windows.Input.Keyboard.FocusedElement);
            Assert.IsNotNull(account.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty));
            description.Focus();
            var click = new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0, System.Windows.Input.MouseButton.Left) { RoutedEvent = System.Windows.UIElement.PreviewMouseLeftButtonDownEvent };
            account.RaiseEvent(click); Assert.IsTrue(click.Handled); Assert.AreEqual(account.Text, account.SelectedText);
            debit.Focus(); debit.Text = "25"; credit.Focus(); Pump(); Assert.AreEqual("25.00", debit.Text); Assert.AreEqual("25.00", model.Lines[0].Debit);
            debit.Focus(); debit.Text = "1.001"; credit.Focus(); Pump(); Assert.AreEqual("1.001", debit.Text);
            credit.Text = "4.5"; description.Focus(); Pump(); Assert.AreEqual("4.50", credit.Text);
        }
        finally { window.Close(); }
    }
    private static IEnumerable<System.Windows.DependencyObject> Descendants(System.Windows.DependencyObject parent)
    {
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i); yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
    private static void Pump() => System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    [STATestMethod]
    public void DateShortcutTextInputUpdatesBothPickersAndRecoversInvalidText()
    {
        var model = Editor(new FakeJournal());
        var window = new FatStacks.Desktop.Views.JournalEditorWindow(model);
        try
        {
            window.Show();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            var accounting = (System.Windows.Controls.DatePicker)window.FindName("AccountingDateInput");
            var bank = (System.Windows.Controls.DatePicker)window.FindName("BankDateInput");
            accounting.Text = new DateTime(2024, 2, 28).ToString("d", System.Globalization.CultureInfo.CurrentCulture);
            Input(accounting, "+"); Assert.AreEqual(new DateTime(2024, 2, 29), model.AccountingDate);
            Input(accounting, "-"); Assert.AreEqual(new DateTime(2024, 2, 28), model.AccountingDate);
            Input(bank, "t"); Assert.AreEqual(new DateTime(2026, 11, 21), model.BankPostingDate);
            accounting.Text = "invalid";
            Input(accounting, "T"); Assert.AreEqual(new DateTime(2026, 11, 21), model.AccountingDate);
            Assert.AreEqual(new DateTime(2026, 11, 21).ToString("d", System.Globalization.CultureInfo.CurrentCulture), accounting.Text);
        }
        finally { window.Close(); }
        static void Input(System.Windows.Controls.DatePicker picker, string text)
        {
            picker.ApplyTemplate();
            var input = (System.Windows.Controls.Primitives.DatePickerTextBox)picker.Template.FindName("PART_TextBox", picker);
            input.Focus();
            var composition = new System.Windows.Input.TextComposition(System.Windows.Input.InputManager.Current, input, text);
            var args = new System.Windows.Input.TextCompositionEventArgs(System.Windows.Input.Keyboard.PrimaryDevice, composition) { RoutedEvent = System.Windows.Input.TextCompositionManager.PreviewTextInputEvent };
            input.RaiseEvent(args); Assert.IsTrue(args.Handled);
        }
    }
    [STATestMethod]
    public void EditorBindsAmountsAndDeleteInitiallyFocusesCancel()
    {
        var service=new FakeJournal(); var model=Editor(service);
        var window=new FatStacks.Desktop.Views.JournalEditorWindow(model);
        try
        {
            window.Show(); Pump();
            var amount=Descendants(window).OfType<System.Windows.Controls.TextBox>().Single(x => System.Windows.Automation.AutomationProperties.GetAutomationId(x)==model.Lines[0].DebitControlId);
            amount.Text="25"; Assert.AreEqual("25",model.Lines[0].Debit);
            var picker=Descendants(window).OfType<System.Windows.Controls.UserControl>().First(x => x.GetType().Name=="AccountComboBox"); picker.Focus();
            var escape=new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice,System.Windows.PresentationSource.FromVisual(window),0,System.Windows.Input.Key.Down) { RoutedEvent=System.Windows.Input.Keyboard.PreviewKeyDownEvent };
            picker.RaiseEvent(escape); Assert.IsTrue(escape.Handled);
        }
        finally { window.Close(); }
        var entry=new JournalRequest(1,1,new byte[8],Day,null,null,"Preview",0,[new(1,new byte[8],1,1,25,0,"USD"),new(2,new byte[8],2,2,0,25,"USD",2)],[new(1,new byte[8]),new(2,new byte[8])]);
        var delete=new FatStacks.Desktop.Views.DeleteEntryWindow(entry,Accounts,_ => throw new AssertFailedException("Cancel must not write."));
        try { delete.Show(); Pump(); Assert.IsTrue(((System.Windows.Controls.Button)delete.FindName("CancelButton")).IsKeyboardFocused); }
        finally { delete.Close(); }
        static IEnumerable<System.Windows.DependencyObject> Descendants(System.Windows.DependencyObject parent)
        {
            for(var i=0;i<System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);i++)
            {
                var child=System.Windows.Media.VisualTreeHelper.GetChild(parent,i); yield return child;
                foreach(var nested in Descendants(child)) yield return nested;
            }
        }
        static void Pump() => System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { },System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }
    [STATestMethod]
    public void DescriptionTabsThroughPostingFieldsAndErrorAreaDoesNotShowAnEmptyScrollbar()
    {
        var model = Editor(new FakeJournal());
        var window = new FatStacks.Desktop.Views.JournalEditorWindow(model);
        try
        {
            window.Show(); Pump();
            var description = (System.Windows.Controls.TextBox)window.FindName("DescriptionInput");
            var controls = Descendants(window).OfType<System.Windows.Controls.TextBox>().ToArray();
            System.Windows.Controls.TextBox Input(string id) => controls.Single(x => System.Windows.Automation.AutomationProperties.GetAutomationId(x) == id);
            var firstAccount = Input(model.Lines[0].AccountControlId);
            var firstDebit = Input(model.Lines[0].DebitControlId);
            var firstCredit = Input(model.Lines[0].CreditControlId);
            var secondAccount = Input(model.Lines[1].AccountControlId);
            description.Focus(); Pump();
            foreach (var expected in new[] { firstAccount, firstDebit, firstCredit, secondAccount })
            {
                Assert.IsTrue(((System.Windows.UIElement)System.Windows.Input.Keyboard.FocusedElement).MoveFocus(new(System.Windows.Input.FocusNavigationDirection.Next)));
                Pump(); Assert.AreSame(expected, System.Windows.Input.Keyboard.FocusedElement);
            }
            foreach (var expected in new[] { firstCredit, firstDebit, firstAccount, description })
            {
                Assert.IsTrue(((System.Windows.UIElement)System.Windows.Input.Keyboard.FocusedElement).MoveFocus(new(System.Windows.Input.FocusNavigationDirection.Previous)));
                Pump(); Assert.AreSame(expected, System.Windows.Input.Keyboard.FocusedElement);
            }
            var errors = (System.Windows.Controls.ScrollViewer)window.FindName("ErrorScroll");
            Assert.AreEqual(System.Windows.Visibility.Collapsed, errors.ComputedVerticalScrollBarVisibility);
            Assert.IsFalse(errors.IsTabStop);
            var remove = Descendants(window).OfType<System.Windows.Controls.Button>().First(x => System.Windows.Automation.AutomationProperties.GetName(x) == "Remove posting");
            Assert.IsFalse(remove.IsEnabled);
            var glyph = Descendants(remove).OfType<System.Windows.Controls.TextBlock>().Single(x => x.Text == "×");
            Assert.IsTrue(glyph.ActualWidth > 0 && glyph.ActualWidth >= glyph.DesiredSize.Width);
            model.AddLine(); Pump(); Assert.IsTrue(remove.IsEnabled);
            remove.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Pump(); Assert.HasCount(2, model.Lines);
            for (var i = 0; i < 30; i++) model.Errors.Add(new("Amount", "Enter a valid amount.", i + 1));
            Pump(); Assert.AreEqual(System.Windows.Visibility.Visible, errors.ComputedVerticalScrollBarVisibility);
        }
        finally { window.Close(); }
        static IEnumerable<System.Windows.DependencyObject> Descendants(System.Windows.DependencyObject parent)
        {
            for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i); yield return child;
                foreach (var nested in Descendants(child)) yield return nested;
            }
        }
        static void Pump() => System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }
#endif
    public sealed class FakeJournal : IJournalService
    {
        public int Calls { get; private set; }
        private JournalRequest? request;
        public JournalRequest? SavedRequest => request;
        public IReadOnlyList<long> ResetLineIds { get; set; } = [];
        private readonly TaskCompletionSource<OperationResult<JournalSaveResult>> completion = new();
        public void Complete(Outcome outcome) => completion.SetResult(outcome == Outcome.Success ? OperationResult<JournalSaveResult>.Success(new(request! with { EntryId = 1, Version = new byte[8] }, ResetLineIds)) : OperationResult<JournalSaveResult>.Fail("Save unconfirmed.", outcome));
        public Task<OperationResult<JournalSaveResult>> SaveAsync(JournalRequest value, CancellationToken ct = default) { Calls++; request = value; return completion.Task; }
        public Task<JournalRequest?> LoadAsync(long id, CancellationToken ct = default) => Task.FromResult<JournalRequest?>(null);
        public Task<OperationResult<bool>> DeleteAsync(DeleteJournalRequest value, CancellationToken ct = default) => Task.FromResult(OperationResult<bool>.Success(true));
    }
}
