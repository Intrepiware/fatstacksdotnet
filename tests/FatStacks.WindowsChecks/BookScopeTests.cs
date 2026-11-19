using Autofac;
using FatStacks.Core.Connections;
using FatStacks.Desktop.Composition;
using FatStacks.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.WindowsChecks;

[TestClass]
public sealed class BookScopeTests
{
    [TestMethod]
    public async Task ClosingResolvedBookScopeLeavesSessionDisposalWithCoordinator()
    {
        var (sessions, session) = await RegisterJourneyTests.OpenAsync();
        var reads = session.Reads.Token;
        var builder = new ContainerBuilder();
        builder.RegisterInstance(sessions);
        using var container = builder.Build();
        using var scope = container.BeginLifetimeScope(b => b.RegisterModule(new BookScopeModule(session)));
        var model = scope.Resolve<MainWindowViewModel>();
        scope.Resolve<AccountSetupViewModel>();
        sessions.SessionChanged += (_, _) => { if (sessions.Active is null) scope.Dispose(); };

        var result = await sessions.DisconnectAsync(() => Task.FromResult(UnsavedDecision.Discard), () => Task.FromResult(true));

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNull(sessions.Active);
        Assert.IsTrue(reads.IsCancellationRequested);
        Assert.IsFalse(model.IsCurrent);
        model.Dispose(); // MainWindow also releases its reference after scope cleanup.
    }

    [TestMethod]
    public async Task DisposingScopeDoesNotDisposeCoordinatorOwnedSession()
    {
        var (sessions, session) = await RegisterJourneyTests.OpenAsync();
        var reads = session.Reads.Token;
        var builder = new ContainerBuilder();
        builder.RegisterInstance(sessions);
        using var container = builder.Build();
        using (var scope = container.BeginLifetimeScope(b => b.RegisterModule(new BookScopeModule(session))))
            scope.Resolve<MainWindowViewModel>();

        Assert.IsFalse(reads.IsCancellationRequested);
        Assert.AreSame(session, sessions.Active);
        await sessions.DisconnectAsync(() => Task.FromResult(UnsavedDecision.Discard), () => Task.FromResult(true));
        Assert.IsTrue(reads.IsCancellationRequested);
    }
}
