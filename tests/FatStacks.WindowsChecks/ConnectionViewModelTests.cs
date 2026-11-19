using FatStacks.Core.Connections;
using FatStacks.Core.Contracts;
using FatStacks.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.WindowsChecks;

[TestClass]
public sealed class ConnectionViewModelTests
{
    [TestMethod]
    public async Task RepeatedNewProfileClearsPasswordPresentationAndSavedCredential()
    {
        var (model, store, _) = await ModelAsync();
        var displayedPassword = "";
        // Match ConnectionWindow's explicit PasswordBox synchronization (no text binding).
        model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ConnectionViewModel.Selected) && model.Selected is null)
                displayedPassword = "";
        };
        model.NewCommand.Execute(null);
        displayedPassword = "synthetic-old-password";
        model.Password = displayedPassword;
        model.NewCommand.Execute(null);
        Assert.AreEqual("", displayedPassword);
        Assert.IsNull(model.Selected);
        model.FriendlyName = "New profile";
        model.Server = "unused";
        model.Database = "Test";
        model.Integrated = false;
        model.SaveCommand.Execute(null);
        Assert.AreEqual("Profile saved.", model.Status);
        var saved = store.State.Profiles.Single(x => x.FriendlyName == "New profile");
        Assert.AreEqual("", new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(saved.Settings.ConnectionString).Password);
    }

    [TestMethod]
    public async Task ManualOpenRemainsUsableWhenProfileWritesFail()
    {
        var (model, store, connections) = await ModelAsync();
        store.FailWrites = true;
        var opened = 0;
        model.Opened += (_, _) => opened++;
        model.OpenCommand.Execute(null);
        Assert.IsFalse(model.Busy);
        Assert.AreEqual("Book opened.", model.Status);
        StringAssert.Contains(model.Warnings, "profile or startup preference was not saved");
        StringAssert.Contains(model.Warnings, "Transport warning.");
        Assert.AreEqual(1, opened);
        Assert.AreEqual(1, connections.OpenCount);
        var active = model.Sessions.Active;
        Assert.IsNotNull(active);
        Assert.IsTrue(model.Sessions.IsCurrent(active));
        Assert.IsFalse(active.Reads.IsCancellationRequested);
        using var write = active.BeginWrite();
        Assert.AreEqual(store.State.Profiles[0].ProfileId, active.Profile.ProfileId);
        Assert.AreEqual(connections.Book.StableIdentifier, active.Book.StableIdentifier);
        Assert.IsNull(store.State.LastSuccessfulProfileId);
        Assert.AreEqual(1, store.WriteCount);
    }

    [TestMethod]
    public async Task ProfileReloadFailureDoesNotHideSuccessfulActivation()
    {
        var (model, store, connections) = await ModelAsync();
        store.FailLoadAfterWrite = true;
        var opened = false;
        model.Opened += (_, _) => opened = true;
        model.OpenCommand.Execute(null);
        Assert.IsFalse(model.Busy);
        Assert.IsTrue(opened);
        Assert.AreEqual("Book opened.", model.Status);
        Assert.IsNotNull(model.Sessions.Active);
        Assert.AreEqual(1, connections.OpenCount);
        StringAssert.Contains(model.Warnings, "saved profiles could not be refreshed");
        StringAssert.Contains(model.Warnings, "Transport warning.");
    }

    [TestMethod]
    public async Task OpenWithFailedWriteThenSaveRetainsAssociatedIdentity()
    {
        var (model, store, _) = await ModelAsync();
        var profileId = model.Selected!.ProfileId;
        store.FailWrites = true;
        model.OpenCommand.Execute(null);
        var active = model.Sessions.Active;
        Assert.IsNotNull(active);
        store.FailWrites = false;
        model.FriendlyName = "Recovered";
        model.SaveCommand.Execute(null);
        Assert.AreEqual("Profile saved.", model.Status);
        Assert.AreEqual(profileId, model.Selected!.ProfileId);
        Assert.IsTrue(model.Selected.ExpectedBookIdentifier == active.Book.StableIdentifier);
        Assert.AreSame(active, model.Sessions.Active);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("oRiGiNaL")]
    public async Task OpenValidatesNamesBeforeSwitching(string name)
    {
        var (model, store, connections) = await ModelAsync();
        store.FailWrites = true;
        model.NewCommand.Execute(null);
        model.FriendlyName = name;
        model.Server = "unused";
        model.Database = "Test";
        model.OpenCommand.Execute(null);
        Assert.IsFalse(model.Busy);
        Assert.AreEqual(0, connections.OpenCount);
        Assert.AreEqual(0, store.WriteCount);
        Assert.IsNull(model.Sessions.Active);
        Assert.AreEqual(string.IsNullOrWhiteSpace(name) ? "Enter a nonempty profile name." : "Choose a unique profile name.", model.Status);
    }

    [TestMethod]
    public async Task ExplicitSaveReportsFailureAndKeepsSavedProfile()
    {
        var (model, store, connections) = await ModelAsync();
        store.FailWrites = true;
        model.FriendlyName = "Unsaved rename";
        model.SaveCommand.Execute(null);
        Assert.IsFalse(model.Busy);
        Assert.AreEqual("Settings could not be saved.", model.Status);
        Assert.AreEqual("Original", store.State.Profiles[0].FriendlyName);
        Assert.AreEqual("Original", model.Selected!.FriendlyName);
        Assert.AreEqual(0, connections.OpenCount);
        Assert.IsNull(model.Sessions.Active);
    }

    [TestMethod]
    public async Task SavingActiveRenameRefreshesDisplayWithoutReplacingSessionOrConnection()
    {
        var (model, store, connections) = await ModelAsync();
        model.OpenCommand.Execute(null);
        var active = model.Sessions.Active;
        Assert.IsNotNull(active);
        var settings = active.Profile.Settings;
        var bookIdentifier = active.Book.StableIdentifier;
        var changes = 0;
        model.Sessions.SessionChanged += (_, _) => changes++;
        model.FriendlyName = "Renamed";
        model.Server = "edited-server";
        model.SaveCommand.Execute(null);
        Assert.IsFalse(model.Busy);
        Assert.AreEqual("Profile saved.", model.Status);
        Assert.AreEqual("Renamed", model.ActiveProfileFriendlyName);
        Assert.AreSame(active, model.Sessions.Active);
        Assert.AreEqual("Original", active.Profile.FriendlyName);
        Assert.AreSame(settings, active.Profile.Settings);
        Assert.AreNotEqual(settings.ConnectionString, model.Selected!.Settings.ConnectionString);
        Assert.AreEqual(active.Profile.ProfileId, model.Selected!.ProfileId);
        Assert.IsTrue(model.Selected.ExpectedBookIdentifier == bookIdentifier);
        Assert.AreEqual(active.Profile.ProfileId, store.State.LastSuccessfulProfileId);
        Assert.IsFalse(active.Reads.IsCancellationRequested);
        Assert.AreEqual(1, connections.OpenCount);
        Assert.AreEqual(0, changes);
    }

    private static async Task<(ConnectionViewModel Model, MemoryStore Store, FakeConnections Connections)> ModelAsync()
    {
        var profile = new ConnectionProfile(Guid.NewGuid(), "Original", new() { ConnectionString = "Server=unused;Database=Test;Integrated Security=True" });
        var store = new MemoryStore { State = new(1, [profile], null) };
        var connections = new FakeConnections();
        var model = new ConnectionViewModel { Store = store, Connections = connections, Sessions = new(connections, store), Setup = new UnusedSetup() };
        await model.LoadAsync();
        model.Selected = model.Profiles.Single();
        return (model, store, connections);
    }

    private sealed class MemoryStore : IProfileStore
    {
        public ProfileConfiguration State { get; set; } = ProfileConfiguration.Empty;
        public bool FailWrites { get; set; }
        public bool FailLoadAfterWrite { get; set; }
        public int WriteCount { get; private set; }
        public Task<OperationResult<ProfileConfiguration>> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(FailLoadAfterWrite && WriteCount > 0
                ? OperationResult<ProfileConfiguration>.Fail("Settings could not be read.")
                : OperationResult<ProfileConfiguration>.Success(State));
        public Task<OperationResult<bool>> SaveAsync(ProfileConfiguration configuration, CancellationToken cancellationToken = default)
        {
            WriteCount++;
            if (FailWrites) return Task.FromResult(OperationResult<bool>.Fail("Settings could not be saved."));
            State = configuration;
            return Task.FromResult(OperationResult<bool>.Success(true));
        }
    }
    private sealed class FakeConnections : IBookConnectionService
    {
        public BookInfo Book { get; } = new(Guid.NewGuid(), "Test book", "USD", "UTC");
        public int OpenCount { get; private set; }
        public Task<OperationResult<BookInfo>> TestAsync(ConnectionSettings settings, CancellationToken cancellationToken = default)
            => Task.FromResult(OperationResult<BookInfo>.Success(Book));
        public Task<OperationResult<BookInfo>> OpenAsync(ConnectionProfile profile, CancellationToken cancellationToken = default)
        {
            OpenCount++;
            return Task.FromResult(OperationResult<BookInfo>.Success(Book, ["Transport warning."]));
        }
    }
    private sealed class UnusedSetup : IDatabaseSetup
    {
        public Task<OperationResult<BookInfo>> InitializeAsync(ConnectionSettings maintenance, BookSetup book, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
