using FatStacks.Core.Accounts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.Tests.Accounts;

[TestClass]
public sealed class AccountTests
{
    [TestMethod]
    public void CatalogMapsEveryTypeAndAllowsGroupOverride()
    {
        CollectionAssert.AreEqual(new byte[] { 1, 1, 2, 1, 2, 1, 1, 2, 3, 4, 5 }, AccountCatalog.Types.Select(x => x.ClassId).ToArray());
        CollectionAssert.AreEqual(new byte[] { 1, 1, 2, 5, 5, 3, 5, 5, 5, 5, 4 }, AccountCatalog.Types.Select(x => x.DefaultGroupId).ToArray());
        Assert.AreEqual(5, AccountCatalog.Types.Single(x => x.Id == 1).WithGroup(5));
    }
    [TestMethod]
    public void HierarchyRejectsCyclesMissingAndCrossBookParents()
    {
        AccountRecord[] accounts = [new(1, 1, null, "Parent", 1, 1, new(2020, 1, 1), null), new(2, 1, 1, "Child", 1, 1, new(2020, 1, 1), null), new(3, 2, null, "Other", 1, 1, new(2020, 1, 1), null)];
        Assert.IsNotNull(AccountValidation.Validate(new(1, 2, "Parent", 1, null, new(2020, 1, 1), null), accounts));
        Assert.IsNotNull(AccountValidation.Validate(new(null, 3, "New", 1, null, new(2020, 1, 1), null), accounts));
        Assert.IsNotNull(AccountValidation.Validate(new(null, 99, "New", 1, null, new(2020, 1, 1), null), accounts));
        Assert.IsNull(AccountValidation.Validate(new(2, 1, " Renamed ", 1, 5, new(2020, 1, 1), null), accounts));
    }
    [TestMethod]
    public void NamesTypesAndClosureBoundariesAreValidated()
    {
        foreach (var name in new[] { "", " ", "A:B", new string('a', 201) })
            Assert.IsNotNull(AccountValidation.Validate(new(null, null, name, 1, null, new(2020, 1, 1), null), []));
        var a = new AccountRecord(1, 1, null, "Bank", 1, 1, new(2020, 1, 1), new(2026, 1, 2));
        Assert.IsTrue(a.CanPost(new(2026, 1, 1), true));
        Assert.IsFalse(a.CanPost(new(2026, 1, 2), true));
        Assert.IsFalse(a.CanPost(new(2026, 1, 1), false));
        Assert.IsFalse(a.CanPost(new(2019, 12, 31), true));
        Assert.IsNotNull(AccountValidation.Validate(new(1, null, "bank", 2, null, a.OpenDate, a.CloseDate), [a]));
        Assert.IsNotNull(AccountValidation.Validate(new(null, null, "BANK", 1, null, a.OpenDate, null), [a]));
    }
}
