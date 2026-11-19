using FatStacks.Core.Connections;
using FatStacks.Core.Contracts;
using FatStacks.Infrastructure.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.Tests.Profiles;

[TestClass]
public sealed class ProfileTests
{
    private string folder = null!;
    [TestInitialize] public void Initialize() => folder = Path.Combine(Path.GetTempPath(), "FatStacks", Guid.NewGuid().ToString());
    [TestCleanup] public void Cleanup() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    private ProfileStore Store(ISecretProtector? protector = null) => new(Path.Combine(folder, "profiles.json"), protector ?? new TestProtector());
    private static ConnectionProfile Profile(string name = "Personal") => new(Guid.NewGuid(), name, new() { ConnectionString = "Server=test;Password=private-sentinel" });
    [TestMethod]
    public async Task MissingConfigurationIsFirstUse()
    {
        var result = await Store().LoadAsync(); Assert.IsTrue(result.IsSuccess); Assert.AreEqual(0, result.Value!.Profiles.Count);
    }
    [TestMethod]
    public async Task ProfilesAndStartupIdentitySurviveRename()
    {
        var store = Store(); var original = Profile();
        Assert.IsTrue((await store.SaveAsync(new(1, [original], original.ProfileId))).IsSuccess);
        var renamed = original with { FriendlyName = "Renamed" };
        Assert.IsTrue((await store.SaveAsync(new(1, [renamed], original.ProfileId))).IsSuccess);
        var loaded = (await store.LoadAsync()).Value!;
        Assert.AreEqual(original.ProfileId, loaded.Profiles.Single().ProfileId);
        Assert.AreEqual(original.ProfileId, loaded.LastSuccessfulProfileId);
        Assert.AreEqual(original.Settings.ConnectionString, loaded.Profiles.Single().Settings.ConnectionString);
        Assert.IsFalse((await File.ReadAllTextAsync(Path.Combine(folder, "profiles.json"))).Contains("private-sentinel", StringComparison.Ordinal));
    }
    [TestMethod]
    public async Task DuplicateAndEmptyNamesAreRejectedWithoutOverwriting()
    {
        var store = Store(); var original = Profile(); await store.SaveAsync(new(1, [original], null));
        foreach (var profiles in new[] { new[] { original, Profile("personal") }, new[] { Profile(" ") } })
            Assert.AreEqual(Outcome.ValidationFailure, (await store.SaveAsync(new(1, profiles, null))).Outcome);
        Assert.AreEqual(original.ProfileId, (await store.LoadAsync()).Value!.Profiles.Single().ProfileId);
    }
    [TestMethod]
    public async Task CorruptionIsReportedAndPreserved()
    {
        Directory.CreateDirectory(folder); var path = Path.Combine(folder, "profiles.json"); await File.WriteAllTextAsync(path, "broken");
        Assert.IsFalse((await Store().LoadAsync()).IsSuccess); Assert.AreEqual("broken", await File.ReadAllTextAsync(path));
    }
    [TestMethod]
    public async Task ProtectionFailurePreservesLastSavedFile()
    {
        var original = Profile(); await Store().SaveAsync(new(1, [original], null));
        Assert.IsFalse((await Store(new BrokenProtector()).SaveAsync(new(1, [Profile("New")], null))).IsSuccess);
        Assert.AreEqual(original.ProfileId, (await Store().LoadAsync()).Value!.Profiles.Single().ProfileId);
    }
    [TestMethod]
    public async Task FailedReplacementPreservesLastSavedFile()
    {
        var original = Profile(); var store = Store(); await store.SaveAsync(new(1, [original], null));
        Directory.CreateDirectory(Path.Combine(folder, "profiles.json.tmp"));
        Assert.IsFalse((await store.SaveAsync(new(1, [Profile("New")], null))).IsSuccess);
        Assert.AreEqual(original.ProfileId, (await store.LoadAsync()).Value!.Profiles.Single().ProfileId);
    }
    [TestMethod]
    [DataRow("null")]
    [DataRow("{}")]
    [DataRow("{\"ProfileId\":\"11111111-1111-1111-1111-111111111111\",\"FriendlyName\":\"Personal\",\"ProtectedConnection\":null}")]
    public async Task MalformedRecordIsRedactedPreservedAndBlocksReplacement(string record)
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "profiles.json");
        var content = "{\"FormatVersion\":1,\"Profiles\":[" + record + "],\"PrivateSentinel\":\"private-sentinel\"}";
        await File.WriteAllTextAsync(path, content);
        var store = Store();
        var loaded = await store.LoadAsync();
        Assert.IsFalse(loaded.IsSuccess);
        Assert.IsFalse(loaded.Message.Contains("private-sentinel", StringComparison.Ordinal));
        Assert.IsFalse((await store.SaveAsync(new(1, [Profile()], null))).IsSuccess);
        Assert.AreEqual(content, await File.ReadAllTextAsync(path));
    }
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task ExplicitRecoveryClearsUnreadableState(bool removeFile)
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "profiles.json");
        await File.WriteAllTextAsync(path, "broken");
        var store = Store();
        Assert.IsFalse((await store.LoadAsync()).IsSuccess);
        if (removeFile) File.Delete(path);
        else await File.WriteAllTextAsync(path, "{\"FormatVersion\":1,\"Profiles\":[]}");
        Assert.IsTrue((await store.LoadAsync()).IsSuccess);
        Assert.IsTrue((await store.SaveAsync(new(1, [Profile()], null))).IsSuccess);
        Assert.AreEqual(1, (await store.LoadAsync()).Value!.Profiles.Count);
    }
    private sealed class TestProtector : ISecretProtector
    {
        public string Protect(string value) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));
        public string Unprotect(string value) => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value));
    }
    private sealed class BrokenProtector : ISecretProtector
    {
        public string Protect(string value) => throw new System.Security.Cryptography.CryptographicException("sentinel secret");
        public string Unprotect(string value) => throw new System.Security.Cryptography.CryptographicException("sentinel secret");
    }
}
