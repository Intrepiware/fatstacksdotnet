using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FatStacks.Core.Connections;
using FatStacks.Core.Contracts;
using FatStacks.Desktop;
using FatStacks.Desktop.ViewModels;
using FatStacks.Desktop.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.WindowsChecks;

[TestClass]
[DoNotParallelize]
public sealed class WindowBehaviorTests
{
    [STATestMethod]
    public void RepeatedNewProfileClearsPasswordBoxAndAcceptsReplacementCredential()
    {
        var model = Model();
        var window = new ConnectionWindow(model);
        try
        {
            window.Show();
            model.NewCommand.Execute(null);
            model.Integrated = false;
            var password = (PasswordBox)window.FindName("Password");
            password.Password = "synthetic-old-password";
            model.NewCommand.Execute(null);
            Pump();
            Assert.AreEqual("", password.Password);
            Assert.IsNull(model.Selected);
            model.FriendlyName = "New profile";
            model.Server = "unused";
            model.Database = "Test";
            model.Integrated = false;
            password.Password = "synthetic-replacement-password";
            model.SaveCommand.Execute(null);
            Pump();
            Assert.AreEqual("Profile saved.", model.Status);
            var saved = model.Profiles.Single();
            Assert.AreEqual("synthetic-replacement-password", new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(saved.Settings.ConnectionString).Password);
            Assert.AreEqual("synthetic-replacement-password", password.Password);
        }
        finally { window.Close(); }
    }

    [STATestMethod]
    public void ConnectionsAccessKeysRunNewAndClose()
    {
        var model = Model();
        var window = new ConnectionWindow(model);
        try
        {
            window.Show();
            Pump();
            model.FriendlyName = "Unsaved name";
            var scope = PresentationSource.FromVisual(window);
            System.Windows.Input.AccessKeyManager.ProcessKey(scope, "N", false);
            Pump();
            Assert.AreEqual("Enter a new connection.", model.Status);
            Assert.AreNotEqual("Unsaved name", model.FriendlyName);

            System.Windows.Input.AccessKeyManager.ProcessKey(scope, "C", false);
            Pump();
            Assert.IsFalse(window.IsVisible);
        }
        finally { if (window.IsVisible) window.Close(); }
    }

    [STATestMethod]
    public void IntegratedAuthenticationDisablesSqlCredentialsInBothDialogs()
    {
        var model = Model();
        var connections = new ConnectionWindow(model);
        var setup = new BookSetupWindow(model);
        try
        {
            connections.Show();
            AssertCredentials(connections, "IntegratedCredentials", "UserName", "Password");
            setup.Show();
            AssertCredentials(setup, "Integrated", "MaintenanceUser", "MaintenancePassword");
        }
        finally { setup.Close(); connections.Close(); }
    }

    [STATestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SetupShowsCloseOnlyAfterSuccessfulInitialization(bool succeeds)
    {
        var window = new BookSetupWindow(Model(succeeds));
        try
        {
            window.Show();
            ((TextBox)window.FindName("BookName")).Text = "Test book";
            ((Button)window.FindName("Initialize")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Pump();
            Assert.AreEqual(succeeds ? Visibility.Collapsed : Visibility.Visible, ((Button)window.FindName("Initialize")).Visibility);
            Assert.AreEqual(succeeds ? Visibility.Collapsed : Visibility.Visible, ((Button)window.FindName("CancelInitialize")).Visibility);
            Assert.AreEqual(succeeds ? Visibility.Visible : Visibility.Collapsed, ((Button)window.FindName("CloseSetup")).Visibility);
            Assert.IsTrue(((Button)window.FindName("Initialize")).IsEnabled);
        }
        finally { window.Close(); }
    }

    [STATestMethod]
    public void ClosingMainWindowWithAnActiveBookDoesNotCloseInsideTheClosingEvent()
    {
        var model = Model();
        var profile = new ConnectionProfile(Guid.NewGuid(), "Test", new() { ConnectionString = "Server=unused;Database=Test;Integrated Security=True" });
        model.Sessions.SwitchAsync(profile, () => Task.FromResult(UnsavedDecision.Discard), () => Task.FromResult(true)).GetAwaiter().GetResult();
        var startupStore = new MemoryStore();
        startupStore.SaveAsync(new(1, [profile], profile.ProfileId)).GetAwaiter().GetResult();
        var window = new MainWindow
        {
            Connections = model,
            Startup = new() { Store = startupStore, Sessions = model.Sessions },
            Sessions = model.Sessions
        };
        var closed = false;
        window.Closed += (_, _) => closed = true;
        window.Show();
        // A synchronous disconnect previously caused a recursive Close and a WPF exception here.
        window.Close();
        Pump();
        Assert.IsTrue(closed);
        Assert.IsNull(model.Sessions.Active);
    }

    [STATestMethod]
    public void SessionWarningCanBeDismissed()
    {
        var model = Model();
        var window = new MainWindow
        {
            Connections = model,
            Startup = new() { Store = new MemoryStore(), Sessions = model.Sessions },
            Sessions = model.Sessions
        };
        var warning = (TextBlock)window.FindName("Warning");
        warning.Text = "SQL certificate trust is not verified.";
        var dismiss = (Button)window.FindName("DismissWarning");
        dismiss.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.AreEqual("", warning.Text);
        window.Close();
        Pump();
    }

    [STATestMethod]
    public void StartupCancellationPreservesPreferenceAndLeavesConnectionsUsable()
    {
        var store = new MemoryStore();
        var profile = new ConnectionProfile(Guid.NewGuid(), "Pending", new() { ConnectionString = "Server=unused;Database=Test;Integrated Security=True" });
        store.SaveAsync(new(1, [profile], profile.ProfileId)).GetAwaiter().GetResult();
        var connections = new PendingConnections();
        var sessions = new BookSessionCoordinator(connections, store);
        var startup = new StartupViewModel { Store = store, Sessions = sessions };
        using var cancellation = new CancellationTokenSource();
        var open = startup.OpenLastAsync(cancellation.Token);
        Assert.IsTrue(connections.Started);
        cancellation.Cancel();
        PumpUntil(() => open.IsCompleted);
        Assert.AreEqual(Outcome.Cancelled, open.GetAwaiter().GetResult()!.Outcome);
        Assert.IsNull(sessions.Active);
        Assert.AreEqual(profile.ProfileId, store.LoadAsync().GetAwaiter().GetResult().Value!.LastSuccessfulProfileId);
        var model = new ConnectionViewModel { Store = store, Connections = connections, Sessions = sessions, Setup = new FakeSetup(true) };
        model.LoadAsync().GetAwaiter().GetResult();
        var window = new ConnectionWindow(model);
        try
        {
            window.Show(); Pump();
            Assert.IsTrue(model.Idle);
            Assert.IsTrue(model.TestCommand.CanExecute(null));
            Assert.IsTrue(model.OpenCommand.CanExecute(null));
            Assert.IsTrue(((TextBox)window.FindName("Server")).IsEnabled);
        }
        finally { window.Close(); }
    }

    [STATestMethod]
    public void ClosingDuringStartupCancelsOpenAndClosesWithoutConnectionDialog()
    {
        var store = new MemoryStore();
        var profile = new ConnectionProfile(Guid.NewGuid(), "Pending", new() { ConnectionString = "Server=unused;Database=Test;Integrated Security=True" });
        store.SaveAsync(new(1, [profile], profile.ProfileId)).GetAwaiter().GetResult();
        var connections = new PendingConnections();
        var sessions = new BookSessionCoordinator(connections, store);
        var model = new ConnectionViewModel { Store = store, Connections = connections, Sessions = sessions, Setup = new FakeSetup(true) };
        var window = new MainWindow { Connections = model, Startup = new() { Store = store, Sessions = sessions }, Sessions = sessions };
        var closed = false;
        window.Closed += (_, _) => closed = true;
        try
        {
            window.Show(); Pump();
            Assert.IsTrue(connections.Started);
            window.Close();
            PumpUntil(() => closed);
            Assert.IsTrue(connections.Cancelled);
            Assert.IsNull(sessions.Active);
            Assert.AreEqual(0, window.OwnedWindows.Count);
            Assert.AreEqual(profile.ProfileId, store.LoadAsync().GetAwaiter().GetResult().Value!.LastSuccessfulProfileId);
        }
        finally { if (!closed) { window.Close(); Pump(); } }
    }

    [STATestMethod]
    public void MainWindowCloseWaitsForOutstandingWrite()
    {
        var model = Model();
        var profile = new ConnectionProfile(Guid.NewGuid(), "Test", new() { ConnectionString = "Server=unused;Database=Test;Integrated Security=True" });
        model.Sessions.SwitchAsync(profile, () => Task.FromResult(UnsavedDecision.Discard), () => Task.FromResult(true)).GetAwaiter().GetResult();
        var active = model.Sessions.Active!;
        var reads = active.Reads.Token;
        var store = new MemoryStore();
        store.SaveAsync(new(1, [profile], profile.ProfileId)).GetAwaiter().GetResult();
        var window = new MainWindow { Connections = model, Startup = new() { Store = store, Sessions = model.Sessions }, Sessions = model.Sessions };
        var closed = false;
        window.Closed += (_, _) => closed = true;
        try
        {
            window.Show(); Pump();
            active = model.Sessions.Active!;
            using var displayedWrite = active.BeginWrite();
            reads = active.Reads.Token;
            window.Close(); Pump();
            Assert.IsFalse(closed);
            Assert.AreSame(active, model.Sessions.Active);
            displayedWrite.Dispose();
            PumpUntil(() => closed);
            Assert.IsNull(model.Sessions.Active);
            Assert.IsTrue(reads.IsCancellationRequested);
        }
        finally { if (!closed) { window.Close(); Pump(); } }
    }

    [STATestMethod]
    public void CloseShortcutCommandClosesOnlyItsWindowFromTextInput()
    {
        WindowShortcuts.Register();
        var owner = new Window();
        var input = new TextBox { Text = "Unsaved draft" };
        var dialog = new Window { Content = input };
        try
        {
            owner.Show(); dialog.Owner = owner; dialog.Show(); input.Focus(); Pump();
            System.Windows.Input.ApplicationCommands.Close.Execute(null, input);
            Pump();
            Assert.IsFalse(dialog.IsVisible);
            Assert.IsTrue(owner.IsVisible);
        }
        finally { if (dialog.IsVisible) dialog.Close(); owner.Close(); }
    }

    [STATestMethod]
    public void JournalCloseShortcutRespectsPendingSaveAndDoesNotSubmitAgain()
    {
        WindowShortcuts.Register();
        var service = new JournalEditorViewModelTests.FakeJournal();
        var model = JournalEditorViewModelTests.Editor(service);
        var window = new JournalEditorWindow(model);
        try
        {
            window.Show(); Pump();
            var pending = model.SaveAsync(false);
            System.Windows.Input.ApplicationCommands.Close.Execute(null, (TextBox)window.FindName("DescriptionInput"));
            Pump();
            Assert.IsTrue(window.IsVisible);
            Assert.AreEqual(1, service.Calls);
            service.Complete(Outcome.Failed);
            PumpUntil(() => pending.IsCompleted);
            Assert.IsFalse(model.ShouldClose);
            System.Windows.Input.ApplicationCommands.Close.Execute(null, window);
            Pump();
            Assert.IsFalse(window.IsVisible);
            Assert.AreEqual(1, service.Calls);
        }
        finally { if (window.IsVisible) window.Close(); }
    }

    [STATestMethod]
    public void MainWindowCloseShortcutUsesNormalDisconnect()
    {
        WindowShortcuts.Register();
        var model = Model();
        var profile = new ConnectionProfile(Guid.NewGuid(), "Test", new() { ConnectionString = "Server=unused;Database=Test;Integrated Security=True" });
        model.Sessions.SwitchAsync(profile, () => Task.FromResult(UnsavedDecision.Discard), () => Task.FromResult(true)).GetAwaiter().GetResult();
        var store = new MemoryStore(); store.SaveAsync(new(1, [profile], profile.ProfileId)).GetAwaiter().GetResult();
        var window = new MainWindow { Connections = model, Startup = new() { Store = store, Sessions = model.Sessions }, Sessions = model.Sessions };
        try
        {
            window.Show(); Pump();
            System.Windows.Input.ApplicationCommands.Close.Execute(null, window);
            PumpUntil(() => !window.IsVisible);
            Assert.IsNull(model.Sessions.Active);
        }
        finally { if (window.IsVisible) { window.Close(); Pump(); } }
    }

    [STATestMethod]
    public void RegisterSummaryRendersReadOnlyPayeeAndDescriptionWithoutWritingBack()
    {
        var model = Model();
        var shell = new MainWindow { Connections = model, Startup = new() { Store = new MemoryStore(), Sessions = model.Sessions }, Sessions = model.Sessions };
        var template = (DataTemplate)shell.FindResource("EntrySummaryTemplate");
        var text = (TextBlock)template.LoadContent();
        var host = new Window { Content = text };
        try
        {
            var row = new FatStacks.Core.Register.RegisterRow(1, new(2026, 11, 21), "Weekly groceries", "Expenses:Groceries", 25, null, 75, "Uncleared", "Synthetic Store");
            text.DataContext = row; host.Show(); Pump();
            var runs = text.Inlines.OfType<System.Windows.Documents.Run>().ToArray();
            foreach (var run in runs)
                Assert.AreEqual(System.Windows.Data.BindingMode.OneWay, System.Windows.Data.BindingOperations.GetBinding(run, System.Windows.Documents.Run.TextProperty)!.Mode);
            Assert.AreEqual("Synthetic Store — Weekly groceries", text.Text);
            Assert.AreEqual(FontStyles.Normal, runs[0].FontStyle);
            Assert.AreEqual(FontStyles.Italic, runs[2].FontStyle);
            Assert.AreEqual(row.SummaryText, text.ToolTip);
            text.DataContext = row with { Description = "" }; Pump(); Assert.AreEqual("Synthetic Store", text.Text);
            text.DataContext = row with { Payee = null }; Pump(); Assert.AreEqual("Weekly groceries", text.Text);
        }
        finally { host.Close(); shell.Close(); Pump(); }
    }

    [STATestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void DeleteFailureTraceIsSanitizedAndCancellationIsNotLogged(bool cancelled)
    {
        const string secret = "PRIVATE_DELETE_DESCRIPTION_PASSWORD_CONNECTION";
        var entry = new FatStacks.Core.Journal.JournalRequest(1, 1, new byte[8], new(2026, 11, 21), null, secret, secret, 0,
            [new(1, new byte[8], 1, 1, 25, 0, "USD"), new(2, new byte[8], 2, 2, 0, 25, "USD")], [new(1, new byte[8]), new(2, new byte[8])]);
        var window = new DeleteEntryWindow(entry, JournalEditorViewModelTests.Accounts, _ => Task.FromException<OperationResult<bool>>(cancelled ? new OperationCanceledException(secret) : new InvalidOperationException(secret)));
        using var output = new System.IO.StringWriter(); using var listener = new System.Diagnostics.TextWriterTraceListener(output);
        System.Diagnostics.Trace.Listeners.Add(listener);
        try
        {
            window.Show(); Pump();
            ((StackPanel)window.FindName("Actions")).Children.OfType<Button>().Single(x => System.Windows.Automation.AutomationProperties.GetAutomationId(x) == "ConfirmDelete").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Pump(); listener.Flush(); Assert.IsTrue(window.IsVisible);
            if (cancelled) Assert.AreEqual("", output.ToString());
            else StringAssert.Contains(output.ToString(), "operation=JournalDelete category=InvalidOperation");
            Assert.IsFalse(output.ToString().Contains(secret, StringComparison.Ordinal));
        }
        finally { window.Close(); System.Diagnostics.Trace.Listeners.Remove(listener); }
    }

    private static void PumpUntil(Func<bool> condition)
    {
        var timeout = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && timeout.Elapsed < TimeSpan.FromSeconds(5)) { Pump(); Thread.Sleep(1); }
        Assert.IsTrue(condition(), "Window did not finish closing before timeout.");
    }

    private sealed class PendingConnections : IBookConnectionService
    {
        public bool Started { get; private set; }
        public bool Cancelled { get; private set; }
        public Task<OperationResult<BookInfo>> TestAsync(ConnectionSettings settings, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public async Task<OperationResult<BookInfo>> OpenAsync(ConnectionProfile profile, CancellationToken cancellationToken = default)
        {
            Started = true;
            try { await Task.Delay(Timeout.Infinite, cancellationToken); }
            catch (OperationCanceledException) { Cancelled = true; throw; }
            throw new InvalidOperationException("Pending connection unexpectedly completed.");
        }
    }

    private static void AssertCredentials(Window window, string checkbox, string username, string password)
    {
        var integrated = (CheckBox)window.FindName(checkbox);
        foreach (var enabled in new[] { true, false, true })
        {
            integrated.IsChecked = !enabled;
            Pump();
            Assert.AreEqual(enabled, ((Control)window.FindName(username)).IsEnabled);
            Assert.AreEqual(enabled, ((Control)window.FindName(password)).IsEnabled);
        }
    }
    private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() => { }));
    private static ConnectionViewModel Model(bool setupSucceeds = true)
    {
        var store = new MemoryStore();
        var connections = new FakeConnections();
        return new()
        {
            Store = store,
            Connections = connections,
            Setup = new FakeSetup(setupSucceeds),
            Sessions = new(connections, store),
            Server = "unused",
            Database = "Test"
        };
    }
    private sealed class FakeSetup(bool succeeds) : IDatabaseSetup
    {
        public Task<OperationResult<BookInfo>> InitializeAsync(ConnectionSettings maintenance, BookSetup book, CancellationToken cancellationToken = default)
            => Task.FromResult(succeeds ? OperationResult<BookInfo>.Success(new(Guid.NewGuid(), book.Name, book.CurrencyCode, book.TimeZoneId)) : OperationResult<BookInfo>.Fail("Synthetic failure."));
    }
    private sealed class FakeConnections : IBookConnectionService
    {
        public Task<OperationResult<BookInfo>> TestAsync(ConnectionSettings settings, CancellationToken cancellationToken = default)
            => Task.FromResult(OperationResult<BookInfo>.Success(new(Guid.NewGuid(), "Test", "USD", "UTC")));
        public Task<OperationResult<BookInfo>> OpenAsync(ConnectionProfile profile, CancellationToken cancellationToken = default)
            => TestAsync(profile.Settings, cancellationToken);
    }
    private sealed class MemoryStore : IProfileStore
    {
        private ProfileConfiguration state = ProfileConfiguration.Empty;
        public Task<OperationResult<ProfileConfiguration>> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(OperationResult<ProfileConfiguration>.Success(state));
        public Task<OperationResult<bool>> SaveAsync(ProfileConfiguration configuration, CancellationToken cancellationToken = default)
        { state = configuration; return Task.FromResult(OperationResult<bool>.Success(true)); }
    }
}
