using FatStacks.Core.Contracts;
namespace FatStacks.Core.Connections;

public enum UnsavedDecision
{
    Save, Discard, Cancel
}
public sealed class BookSession(ConnectionProfile profile, BookInfo book) : IDisposable
{
    public ConnectionProfile Profile { get; } = profile;
    public BookInfo Book { get; } = book;
    public CancellationTokenSource Reads { get; } = new();
    private readonly object gate = new();
    private readonly List<Task> writes = [];
    private bool closing;
    private bool disposed;
    public IDisposable BeginWrite()
    {
        lock (gate)
        {
            if (closing)
                throw new InvalidOperationException("The book is disconnecting.");
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            writes.Add(completion.Task);
            return new WriteLease(completion);
        }
    }
    internal Task CloseWritesAsync()
    {
        lock (gate)
        {
            closing = true;
            return Task.WhenAll(writes);
        }
    }
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed)
                return;
            disposed = true;
            closing = true;
        }
        Reads.Cancel();
        Reads.Dispose();
    }
    private sealed class WriteLease(TaskCompletionSource completion) : IDisposable
    {
        public void Dispose() => completion.TrySetResult();
    }
}

public sealed class BookSessionCoordinator(IBookConnectionService connections, IProfileStore profiles)
{
    public BookSession? Active
    {
        get; private set;
    }
    public event EventHandler? SessionChanged;
    private readonly SemaphoreSlim transition = new(1, 1);
    public bool IsCurrent(BookSession session) => ReferenceEquals(Active, session);
    public async Task<OperationResult<BookSession>> SwitchAsync(ConnectionProfile profile,
        Func<Task<UnsavedDecision>> resolveDraft, Func<Task<bool>> saveDraft, CancellationToken cancellationToken = default)
    {
        await transition.WaitAsync(cancellationToken);
        try
        {
            var decision = await resolveDraft();
            if (decision == UnsavedDecision.Cancel || (decision == UnsavedDecision.Save && !await saveDraft()))
                return OperationResult<BookSession>.Fail("Switch cancelled.", Outcome.Cancelled);
            await DisconnectCoreAsync();
            var opened = await connections.OpenAsync(profile, cancellationToken);
            if (!opened.IsSuccess || opened.Value is null)
                return new(opened.Outcome, Message: opened.Message, Warnings: opened.Warnings);
            cancellationToken.ThrowIfCancellationRequested();
            var associated = profile with
            {
                ExpectedBookIdentifier = opened.Value.StableIdentifier,
                Settings = opened.Value.EffectiveSettings ?? profile.Settings
            };
            Active = new(associated, opened.Value);
            SessionChanged?.Invoke(this, EventArgs.Empty);
            var loaded = await profiles.LoadAsync(CancellationToken.None);
            string warning = "";
            if (loaded.IsSuccess && loaded.Value is not null)
            {
                var updated = loaded.Value.Profiles.Where(x => x.ProfileId != profile.ProfileId).Append(associated).ToArray();
                var saved = await profiles.SaveAsync(new(1, updated, profile.ProfileId), CancellationToken.None);
                if (!saved.IsSuccess)
                    warning = "Book opened, but its profile or startup preference was not saved.";
            }
            else
                warning = "Book opened, but saved profiles could not be read. Startup preference was not saved.";
            var warnings = (opened.Warnings ?? []).Concat(string.IsNullOrEmpty(warning) ? Array.Empty<string>() : [warning]).ToArray();
            return OperationResult<BookSession>.Success(Active, warnings);
        }
        catch (OperationCanceledException) { return OperationResult<BookSession>.Fail("Connection cancelled.", Outcome.Cancelled); }
        finally { transition.Release(); }
    }
    public async Task<OperationResult<bool>> DisconnectAsync(Func<Task<UnsavedDecision>> resolveDraft, Func<Task<bool>> saveDraft)
    {
        await transition.WaitAsync();
        try
        {
            var choice = await resolveDraft();
            if (choice == UnsavedDecision.Cancel || (choice == UnsavedDecision.Save && !await saveDraft()))
                return OperationResult<bool>.Fail("Disconnect cancelled.", Outcome.Cancelled);
            await DisconnectCoreAsync();
            return OperationResult<bool>.Success(true);
        }
        finally { transition.Release(); }
    }
    private async Task DisconnectCoreAsync()
    {
        if (Active is not { } old)
            return;
        await old.CloseWritesAsync();
        old.Dispose();
        Active = null;
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }
}
