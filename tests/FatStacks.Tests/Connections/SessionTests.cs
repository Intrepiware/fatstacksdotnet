using FatStacks.Core.Contracts;
using FatStacks.Core.Connections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.Tests.Connections;

[TestClass]
public sealed class SessionTests
{
    private static ConnectionProfile Profile(string name) => new(Guid.NewGuid(), name, new() { ConnectionString = "synthetic" });
    private static Task<UnsavedDecision> Discard() => Task.FromResult(UnsavedDecision.Discard);
    private static Task<bool> Saved() => Task.FromResult(true);
    [TestMethod]
    public async Task CancelPreservesActiveButFailedSwitchDisconnects()
    {
        var sql = new FakeConnection();
        var store = new MemoryStore();
        var session = new BookSessionCoordinator(sql, store);
        await session.SwitchAsync(Profile("A"), Discard, Saved);
        var first = session.Active!;
        await session.SwitchAsync(Profile("B"), () => Task.FromResult(UnsavedDecision.Cancel), Saved);
        Assert.AreSame(first, session.Active);
        sql.Fail = true;
        await session.SwitchAsync(Profile("B"), Discard, Saved);
        Assert.IsNull(session.Active);
        Assert.IsFalse(session.IsCurrent(first));
        Assert.AreEqual(first.Profile.ProfileId, store.State.LastSuccessfulProfileId);
    }
    [TestMethod]
    public async Task SwitchWaitsForCapturedWriteAndRejectsOldRead()
    {
        var session = new BookSessionCoordinator(new FakeConnection(), new MemoryStore());
        await session.SwitchAsync(Profile("A"), Discard, Saved);
        var first = session.Active!;
        using var write = first.BeginWrite();
        var switchTask = session.SwitchAsync(Profile("B"), Discard, Saved);
        Assert.IsFalse(switchTask.IsCompleted);
        Assert.AreSame(first, session.Active);
        write.Dispose();
        Assert.IsTrue((await switchTask).IsSuccess);
        Assert.IsFalse(session.IsCurrent(first));
        Assert.AreEqual("A", first.Profile.FriendlyName);
    }
    [TestMethod]
    public async Task FailedPreferenceWriteDoesNotUndoOpen()
    {
        var session = new BookSessionCoordinator(new FakeConnection(), new MemoryStore { FailSave = true });
        var result = await session.SwitchAsync(Profile("A"), Discard, Saved);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(session.Active);
        Assert.AreEqual(1, result.Warnings!.Count);
    }
    [TestMethod]
    public async Task CancelPendingOpenLeavesDisconnected()
    {
        var connection = new FakeConnection { Wait = true };
        var session = new BookSessionCoordinator(connection, new MemoryStore());
        using var cancel = new CancellationTokenSource();
        var open = session.SwitchAsync(Profile("A"), Discard, Saved, cancel.Token);
        cancel.Cancel();
        Assert.AreEqual(Outcome.Cancelled, (await open).Outcome);
        Assert.IsNull(session.Active);
    }
    [TestMethod]
    [DataRow(UnsavedDecision.Save, true, true, 1)]
    [DataRow(UnsavedDecision.Save, false, false, 1)]
    [DataRow(UnsavedDecision.Discard, false, true, 0)]
    [DataRow(UnsavedDecision.Cancel, true, false, 0)]
    public async Task DraftDecisionControlsSwitch(UnsavedDecision decision, bool saved, bool switches, int expectedSaves)
    {
        var store = new MemoryStore();
        var session = new BookSessionCoordinator(new FakeConnection(), store);
        await session.SwitchAsync(Profile("A"), Discard, Saved);
        var first = session.Active!;
        var reads = first.Reads.Token;
        var saves = 0;
        var next = Profile("B");
        var result = await session.SwitchAsync(next, () => Task.FromResult(decision), () => { saves++; return Task.FromResult(saved); });
        Assert.AreEqual(expectedSaves, saves);
        Assert.AreEqual(switches ? Outcome.Success : Outcome.Cancelled, result.Outcome);
        Assert.AreEqual(switches, reads.IsCancellationRequested);
        Assert.AreEqual(switches ? next.ProfileId : first.Profile.ProfileId, store.State.LastSuccessfulProfileId);
        if (switches)
            Assert.AreEqual(next.ProfileId, session.Active!.Profile.ProfileId);
        else
            Assert.AreSame(first, session.Active);
    }

    [TestMethod]
    [DataRow(UnsavedDecision.Save, true, true, 1)]
    [DataRow(UnsavedDecision.Save, false, false, 1)]
    [DataRow(UnsavedDecision.Discard, false, true, 0)]
    [DataRow(UnsavedDecision.Cancel, true, false, 0)]
    public async Task DraftDecisionControlsDisconnect(UnsavedDecision decision, bool saved, bool disconnects, int expectedSaves)
    {
        var store = new MemoryStore();
        var session = new BookSessionCoordinator(new FakeConnection(), store);
        await session.SwitchAsync(Profile("A"), Discard, Saved);
        var first = session.Active!;
        var reads = first.Reads.Token;
        var saves = 0;
        var result = await session.DisconnectAsync(() => Task.FromResult(decision), () => { saves++; return Task.FromResult(saved); });
        Assert.AreEqual(expectedSaves, saves);
        Assert.AreEqual(disconnects ? Outcome.Success : Outcome.Cancelled, result.Outcome);
        Assert.AreEqual(disconnects, reads.IsCancellationRequested);
        Assert.AreEqual(first.Profile.ProfileId, store.State.LastSuccessfulProfileId);
        if (disconnects)
            Assert.IsNull(session.Active);
        else
            Assert.AreSame(first, session.Active);
    }

    [TestMethod]
    public void SessionCleanupCanRepeatWithoutCancellingDisposedResources()
    {
        var session = new BookSession(Profile("A"), new(Guid.NewGuid(), "Synthetic", "USD", "UTC"));
        var reads = session.Reads.Token;
        var cancellations = 0;
        using var registration = reads.Register(() => cancellations++);
        session.Dispose();
        session.Dispose();
        Assert.IsTrue(reads.IsCancellationRequested);
        Assert.AreEqual(1, cancellations);
        Assert.Throws<InvalidOperationException>(() => session.BeginWrite());
    }

    private sealed class FakeConnection : IBookConnectionService
    {
        public bool Fail
        {
            get; set;
        }
        public bool Wait
        {
            get; set;
        }
        public Task<OperationResult<BookInfo>> TestAsync(ConnectionSettings settings, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public async Task<OperationResult<BookInfo>> OpenAsync(ConnectionProfile profile, CancellationToken cancellationToken = default)
        {
            if (Wait)
                await Task.Delay(Timeout.Infinite, cancellationToken);
            return Fail ? OperationResult<BookInfo>.Fail("Cannot open.") : OperationResult<BookInfo>.Success(new(Guid.NewGuid(), profile.FriendlyName, "USD", "UTC"));
        }
    }
    private sealed class MemoryStore : IProfileStore
    {
        public ProfileConfiguration State { get; private set; } = ProfileConfiguration.Empty;
        public bool FailSave
        {
            get; init;
        }
        public Task<OperationResult<ProfileConfiguration>> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(OperationResult<ProfileConfiguration>.Success(State));
        public Task<OperationResult<bool>> SaveAsync(ProfileConfiguration configuration, CancellationToken cancellationToken = default)
        {
            if (FailSave)
                return Task.FromResult(OperationResult<bool>.Fail("No write."));
            State = configuration;
            return Task.FromResult(OperationResult<bool>.Success(true));
        }
    }
}
