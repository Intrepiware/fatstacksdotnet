using System.Diagnostics;
using FatStacks.Core.Accounts;
using FatStacks.Core.Connections;
using FatStacks.Core.Contracts;
using FatStacks.Core.Journal;
using FatStacks.Core.Register;
using FatStacks.Desktop.ViewModels;
using FatStacks.Infrastructure.Diagnostics;
using FatStacks.WindowsChecks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.Tests.Journal;

[TestClass]
[DoNotParallelize]
public sealed class JournalDiagnosticsTests
{
    private const string Private = "PRIVATE_PASSWORD_ACCOUNT_FINANCIAL_TEXT_TOKEN_CONNECTION_PARAMETERS";
    [TestMethod]
    [DataRow("accounts")]
    [DataRow("save")]
    [DataRow("delete")]
    public async Task ServiceFailuresAreTracedOnceWithoutPrivateValues(string stage)
    {
        using var trace = new Capture(); using var session = Session();
        var service = new JournalService { Session = session, Accounts = new Accounts(stage == "accounts" ? new InvalidOperationException(Private) : null), Store = new Store { Error = new InvalidOperationException(Private) } };
        if (stage == "delete") Assert.AreEqual(Outcome.Failed, (await service.DeleteAsync(new(1, 1, new byte[8], []))).Outcome);
        else Assert.AreEqual(Outcome.Failed, (await service.SaveAsync(Draft())).Outcome);
        trace.AssertSingle(stage == "delete" ? "JournalDelete" : "JournalSave");
    }
    [TestMethod]
    public async Task AlreadyReportedRepositoryFailureDoesNotProduceAnotherEvent()
    {
        using var trace = new Capture(); using var session = Session();
        var service = new JournalService { Session = session, Accounts = new Accounts(null), Store = new Store { ReportedResult = true } };
        Assert.AreEqual(Outcome.Failed, (await service.SaveAsync(Draft())).Outcome); trace.AssertSingle("JournalSave");
    }
    [TestMethod]
    public async Task CancelledAccountReadDoesNotProduceFailureTrace()
    {
        using var trace = new Capture(); using var session = Session();
        var service = new JournalService { Session = session, Accounts = new Accounts(new OperationCanceledException(Private)), Store = new Store() };
        await service.SaveAsync(Draft()); Assert.AreEqual("", trace.Text);
    }
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task EditorFailureIsTracedButCancellationIsNot(bool cancelled)
    {
        using var trace = new Capture(); var editor = JournalEditorViewModelTests.Editor(new ThrowingJournal(cancelled));
        Assert.IsFalse(await editor.SaveAsync(false)); Assert.AreEqual("1000", editor.Lines[0].Debit);
        if (cancelled) Assert.AreEqual("", trace.Text); else trace.AssertSingle("JournalEditorSave");
    }
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task JournalLoadFailureIsTracedButCancellationIsNot(bool cancelled)
    {
        using var trace = new Capture(); var (sessions, session) = await RegisterJourneyTests.OpenAsync(); using var owned = session;
        using var model = new MainWindowViewModel { Session = session, Sessions = sessions, Accounts = new RegisterJourneyTests.Accounts(), Register = new Register(), Journal = new ThrowingJournal(cancelled) };
        await model.RefreshAsync(); model.Selected = model.Rows.Single();
        Assert.IsNull(await model.LoadSelectedEntryAsync());
        if (cancelled) Assert.AreEqual("", trace.Text); else trace.AssertSingle("JournalLoad");
    }
    [TestMethod]
    public async Task MissingSelectedEntryReportsHowToRefresh()
    {
        var (sessions, session) = await RegisterJourneyTests.OpenAsync();
        using var owned = session;
        using var model = new MainWindowViewModel
        {
            Session = session,
            Sessions = sessions,
            Accounts = new RegisterJourneyTests.Accounts(),
            Register = new Register(),
            Journal = new JournalEditorViewModelTests.FakeJournal()
        };
        await model.RefreshAsync();
        model.Selected = null;
        var status = model.Status;
        Assert.IsNull(await model.LoadSelectedEntryAsync());
        Assert.AreEqual(status, model.Status);
        model.Selected = model.Rows.Single();
        Assert.IsNull(await model.LoadSelectedEntryAsync());
        StringAssert.Contains(model.Status, "Refresh");
        StringAssert.Contains(model.Status, "no longer exists");
    }

    private static BookSession Session() => new(new(Guid.NewGuid(), "Synthetic", new() { ConnectionString = Private }), new(Guid.NewGuid(), "Synthetic", "USD", "UTC"));
    private static JournalRequest Draft() => new(1, null, null, new(2026, 11, 21), null, Private, Private, 0, [new(null, null, 1, 1, 25, 0, "USD"), new(null, null, 2, 2, 0, 25, "USD")], []);
    private sealed class Accounts(Exception? error) : IAccountService
    {
        public Task<IReadOnlyList<AccountRecord>> ListAsync(CancellationToken ct = default) => error is null ? Task.FromResult<IReadOnlyList<AccountRecord>>(JournalEditorViewModelTests.Accounts) : Task.FromException<IReadOnlyList<AccountRecord>>(error);
        public Task<OperationResult<AccountRecord>> SaveAsync(AccountRequest request, CancellationToken ct = default) => throw new NotSupportedException();
    }
    private sealed class Store : IJournalStore
    {
        public Exception? Error { get; init; }
        public bool ReportedResult { get; init; }
        public Task<JournalRequest?> LoadAsync(long id, CancellationToken ct = default) => Task.FromResult<JournalRequest?>(null);
        public Task<OperationResult<JournalSaveResult>> SaveAsync(JournalRequest request, CancellationToken ct = default)
        {
            if (Error is not null) return Task.FromException<OperationResult<JournalSaveResult>>(Error);
            if (ReportedResult) DatabaseErrors.TraceFailure("JournalSave", new InvalidOperationException(Private));
            return Task.FromResult(OperationResult<JournalSaveResult>.Fail("Save was not confirmed."));
        }
        public Task<OperationResult<bool>> DeleteAsync(DeleteJournalRequest request, CancellationToken ct = default) => Task.FromException<OperationResult<bool>>(Error!);
    }
    private sealed class ThrowingJournal(bool cancelled) : IJournalService
    {
        private Exception Error => cancelled ? new OperationCanceledException(Private) : new InvalidOperationException(Private);
        public Task<JournalRequest?> LoadAsync(long id, CancellationToken ct = default) => Task.FromException<JournalRequest?>(Error);
        public Task<OperationResult<JournalSaveResult>> SaveAsync(JournalRequest request, CancellationToken ct = default) => Task.FromException<OperationResult<JournalSaveResult>>(Error);
        public Task<OperationResult<bool>> DeleteAsync(DeleteJournalRequest request, CancellationToken ct = default) => Task.FromException<OperationResult<bool>>(Error);
    }
    private sealed class Register : IRegisterService
    {
        public Task<RegisterResult> ReadAsync(long id, DateOnly today, string search, CancellationToken ct = default) => Task.FromResult(new RegisterResult(0, [new(1, today, Private, "Counter", null, 25, 25, "Uncleared")]));
        public Task<IReadOnlyList<EntryLine>> DetailsAsync(long id, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<EntryLine>>([]);
        public Task<IReadOnlyDictionary<long, decimal>> BalancesAsync(DateOnly today, CancellationToken ct = default) => Task.FromResult<IReadOnlyDictionary<long, decimal>>(new Dictionary<long, decimal>());
    }
    private sealed class Capture : IDisposable
    {
        private readonly StringWriter output = new();
        private readonly TextWriterTraceListener listener;
        public Capture() { listener = new(output); Trace.Listeners.Add(listener); }
        public string Text { get { listener.Flush(); return output.ToString(); } }
        public void AssertSingle(string operation)
        {
            StringAssert.Contains(Text, $"operation={operation} category=");
            Assert.AreEqual(1, Text.Split("operation=", StringSplitOptions.None).Length - 1);
            Assert.IsFalse(Text.Contains(Private, StringComparison.Ordinal));
        }
        public void Dispose() { Trace.Listeners.Remove(listener); listener.Dispose(); output.Dispose(); }
    }
}
