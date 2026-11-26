using FatStacks.Core.Register;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.Tests.Register;

[TestClass]
public sealed class RegisterPresentationTests
{
    [TestMethod]
    [DataRow("Store", "Groceries", "Store — Groceries")]
    [DataRow("American Express: Opening Balance", "", "American Express: Opening Balance")]
    [DataRow(null, "Description only", "Description only")]
    [DataRow("", "Description only", "Description only")]
    [DataRow("Store", "   ", "Store")]
    [DataRow("   ", "", "")]
    public void RegisterSummaryIncludesAvailablePayeeAndDescriptionWithoutEmptySeparators(string? payee, string description, string expected)
    {
        var row = new RegisterRow(1, new(2026, 11, 21), description, "Counter", null, 25, 25, "Uncleared", payee);
        Assert.AreEqual(expected, row.SummaryText);
        Assert.AreEqual(description, row.Description);
        Assert.AreEqual(payee, row.Payee);
    }
}
