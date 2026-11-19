using FatStacks.Core.Connections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.Tests.Connections;

[TestClass]
public sealed class SchemaCompatibilityTests
{
    [TestMethod] public void ExpectedVersionOpens() => Assert.IsTrue(SchemaCompatibility.IsCompatible(1));
    [TestMethod]
    [DataRow(null)]
    [DataRow(0)]
    [DataRow(2)]
    public void MissingOrDifferentVersionDoesNotOpen(int? version) => Assert.IsFalse(SchemaCompatibility.IsCompatible(version));
}
