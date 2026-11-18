using FatStacks.Core.Connections;
using FatStacks.Core.Contracts;
using FatStacks.Infrastructure.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FatStacks.Tests.Profiles;

[TestClass]
public sealed class ProfileConnectionEditTests
{
    [TestMethod]
    public async Task ConnectionEditRetainsStableProfileIdentityAndBookPinAfterFreshLoad()
    {
        var folder = Path.Combine(Path.GetTempPath(), "FatStacks_ProfileEdit_" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(folder, "profiles.json");
        try
        {
            var original = new ConnectionProfile(Guid.NewGuid(), "Personal", new() { ConnectionString = "Server=original;Database=First;Integrated Security=True" }, Guid.NewGuid());
            var store = new ProfileStore(path, new TestProtector());
            Assert.IsTrue((await store.SaveAsync(new(1, [original], original.ProfileId))).IsSuccess);
            var loaded = (await store.LoadAsync()).Value!;
            var edited = loaded.Profiles.Single() with
            {
                FriendlyName = "Renamed",
                Settings = new() { ConnectionString = "Server=changed;Database=Second;Integrated Security=True" }
            };
            Assert.IsTrue((await store.SaveAsync(loaded with { Profiles = [edited] })).IsSuccess);
            var reloaded = await new ProfileStore(path, new TestProtector()).LoadAsync();
            Assert.IsTrue(reloaded.IsSuccess);
            var profile = reloaded.Value!.Profiles.Single();
            Assert.AreEqual(original.ProfileId, profile.ProfileId);
            Assert.AreEqual(original.ExpectedBookIdentifier, profile.ExpectedBookIdentifier);
            Assert.AreEqual(original.ProfileId, reloaded.Value.LastSuccessfulProfileId);
            Assert.AreEqual(edited.Settings.ConnectionString, profile.Settings.ConnectionString);
            Assert.AreEqual("Renamed", profile.FriendlyName);
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    private sealed class TestProtector : ISecretProtector
    {
        public string Protect(string value) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));
        public string Unprotect(string value) => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value));
    }
}
