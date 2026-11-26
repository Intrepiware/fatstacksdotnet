using FatStacks.Core.Connections;
using FatStacks.Infrastructure.Configuration;
using FatStacks.Infrastructure.Connections;
using FatStacks.WindowsChecks.Fixtures;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.WindowsChecks;

[TestClass]
[DoNotParallelize]
public sealed class ConnectionJourneyTests
{
    [TestMethod]
    public async Task ProfileAutomationSelectsContainerWhenNameBelongsToDisplayText()
    {
        using var desktop = new DesktopFixture();
        var store = new ProfileStore(desktop.ProfilePath, new WindowsSecretProtector());
        var first = new ConnectionProfile(Guid.NewGuid(), "First", new()
        {
            ConnectionString = "Server=unused;Database=First;Integrated Security=True"
        });
        var second = new ConnectionProfile(Guid.NewGuid(), "Second", new()
        {
            ConnectionString = "Server=unused;Database=Second;Integrated Security=True"
        });
        Assert.IsTrue((await store.SaveAsync(new(1, [first, second], null))).IsSuccess);
        // No startup preference: inspect profile selection without opening a SQL connection.
        await desktop.StartAsync();
        foreach (var name in new[] { "Second", "First", "Second" })
        {
            await desktop.SelectProfileAsync(name);
            await desktop.WaitValueAsync("FriendlyName", name);
            await desktop.WaitValueAsync("Database", name);
        }
        Assert.IsNull((await store.LoadAsync()).Value!.LastSuccessfulProfileId);
    }

    [TestMethod]
    public async Task TestPreservesActiveBookAndStartupPreferenceAndActiveRemovalIsRejected()
    {
        await using var a = await SqlTests.Fixtures.BookDatabaseFixture.CreateAsync();
        await a.InitializeAsync();
        await using var b = await SqlTests.Fixtures.BookDatabaseFixture.CreateAsync();
        await b.InitializeAsync();
        using var desktop = new DesktopFixture();
        var store = new ProfileStore(desktop.ProfilePath, new WindowsSecretProtector());
        var active = new ConnectionProfile(Guid.NewGuid(), "Active", a.Settings);
        var other = new ConnectionProfile(Guid.NewGuid(), "Other", b.Settings);
        Assert.IsTrue((await store.SaveAsync(new(1, [active, other], active.ProfileId))).IsSuccess);
        await desktop.StartAsync();
        await desktop.WaitTextAsync("ActiveBook", "Active");
        await desktop.InvokeAsync("Connections");
        await desktop.SelectProfileAsync("Other");
        await desktop.InvokeAsync("TestConnection");
        await desktop.WaitTextAsync("ConnectionStatus", "Connection is usable");
        await desktop.WaitTextAsync("ActiveBook", "Active");
        Assert.AreEqual(active.ProfileId, (await store.LoadAsync()).Value!.LastSuccessfulProfileId);
        await desktop.SelectProfileAsync("Active");
        await desktop.InvokeAsync("RemoveProfile");
        await desktop.WaitTextAsync("ConnectionStatus", "Disconnect the active profile");
        var loaded = (await store.LoadAsync()).Value!;
        Assert.AreEqual(2, loaded.Profiles.Count);
        Assert.AreEqual(active.ProfileId, loaded.LastSuccessfulProfileId);
        await desktop.WaitTextAsync("ActiveBook", "Active");
    }

    [TestMethod]
    public async Task SwitchAndRestartOpenLastSuccessfulBookAfterProfileRename()
    {
        await using var a = await SqlTests.Fixtures.BookDatabaseFixture.CreateAsync();
        await a.InitializeAsync();
        await using var b = await SqlTests.Fixtures.BookDatabaseFixture.CreateAsync();
        await b.InitializeAsync("EUR");
        using var desktop = new DesktopFixture();
        var store = new ProfileStore(desktop.ProfilePath, new WindowsSecretProtector());
        var personal = new ConnectionProfile(Guid.NewGuid(), "Personal", a.Settings);
        var test = new ConnectionProfile(Guid.NewGuid(), "Test", b.Settings);
        Assert.IsTrue((await store.SaveAsync(new(1, [personal, test], personal.ProfileId))).IsSuccess);
        await desktop.StartAsync();
        await desktop.WaitTextAsync("ActiveBook", "Personal");
        await desktop.InvokeAsync("Connections");
        await desktop.SelectProfileAsync("Test");
        await desktop.InvokeAsync("OpenProfile");
        await desktop.WaitTextAsync("ActiveBook", "Test");
        desktop.Stop();
        var saved = (await store.LoadAsync()).Value!;
        Assert.AreEqual(test.ProfileId, saved.LastSuccessfulProfileId);
        Assert.IsTrue((await store.SaveAsync(saved with
        {
            Profiles = saved.Profiles.Select(x => x.ProfileId == test.ProfileId ? x with { FriendlyName = "Renamed Test" } : x).ToArray()
        })).IsSuccess);
        await desktop.StartAsync();
        await desktop.WaitTextAsync("ActiveBook", "Renamed Test");
        var text = await System.IO.File.ReadAllTextAsync(desktop.ProfilePath);
        var password = new SqlConnectionStringBuilder(a.Settings.ConnectionString).Password;
        if (!string.IsNullOrEmpty(password))
            Assert.IsFalse(text.Contains(password, StringComparison.Ordinal));
    }
    [TestMethod]
    public async Task MismatchRequiresExplicitReassociation()
    {
        await using var a = await SqlTests.Fixtures.BookDatabaseFixture.CreateAsync();
        await a.InitializeAsync();
        await using var b = await SqlTests.Fixtures.BookDatabaseFixture.CreateAsync();
        await b.InitializeAsync();
        var original = (await new ConnectionService { Transport = new() }.TestAsync(a.Settings)).Value!;
        using var desktop = new DesktopFixture();
        var store = new ProfileStore(desktop.ProfilePath, new WindowsSecretProtector());
        var profile = new ConnectionProfile(Guid.NewGuid(), "Personal", b.Settings, original.StableIdentifier);
        await store.SaveAsync(new(1, [profile], profile.ProfileId));
        await desktop.StartAsync();
        await desktop.WaitTextAsync("MainStatus", "different book");
        await desktop.SelectProfileAsync("Personal");
        Assert.IsNotNull(desktop.Find("ConnectionWindow"));
        await desktop.InvokeAsync("ReassociateProfile");
        // The explicit confirmation uses the Windows standard Yes button automation ID 6.
        await desktop.InvokeAsync("6");
        await desktop.WaitTextAsync("ConnectionStatus", "Profile saved");
        await desktop.InvokeAsync("OpenProfile");
        await desktop.WaitTextAsync("ActiveBook", "Personal");
    }
    [TestMethod]
    public async Task FailedSwitchKeepsDialogAndClearsPriorBook()
    {
        await using var db = await SqlTests.Fixtures.BookDatabaseFixture.CreateAsync();
        await db.InitializeAsync();
        using var desktop = new DesktopFixture();
        var store = new ProfileStore(desktop.ProfilePath, new WindowsSecretProtector());
        var personal = new ConnectionProfile(Guid.NewGuid(), "Personal", db.Settings);
        var unavailable = new ConnectionProfile(Guid.NewGuid(), "Unavailable", new()
        {
            ConnectionString = "Server=tcp:127.0.0.1,1;Database=Unavailable;User ID=invalid;Password=invalid;Connect Timeout=1;Encrypt=True;TrustServerCertificate=True"
        });
        await store.SaveAsync(new(1, [personal, unavailable], personal.ProfileId));
        await desktop.StartAsync();
        await desktop.WaitTextAsync("ActiveBook", "Personal");
        await desktop.InvokeAsync("Connections");
        await desktop.SelectProfileAsync("Unavailable");
        await desktop.InvokeAsync("OpenProfile");
        await desktop.WaitTextAsync("ActiveBook", "Connect to your book");
        Assert.IsNotNull(await desktop.WaitAsync("ConnectionWindow"));
        Assert.AreEqual(personal.ProfileId, (await store.LoadAsync()).Value!.LastSuccessfulProfileId);
    }
}
