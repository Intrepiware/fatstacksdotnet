using FatStacks.Core.Accounts;
using FatStacks.Core.Journal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.Tests.Journal;

[TestClass]
public sealed class JournalValidationTests
{
    private static readonly DateOnly Day = new(2026, 11, 21);
    private static readonly AccountRecord[] Accounts = [new(1, 1, null, "Bank", 1, 1, Day.AddYears(-1), null), new(2, 1, null, "Expense", 11, 4, Day.AddYears(-1), null)];
    private static JournalRequest Draft(decimal amount = 25m) => new(1, null, null, Day, null, "Payee", "", 0, [new(null, null, 1, 1, amount, 0, "USD"), new(null, null, 2, 2, 0, amount, "USD")], []);
    [TestMethod]
    public void InvalidMoneyAndBalanceAreRejected()
    {
        foreach (var amount in new[] { 0m, -1m, 100000000m, 1.001m }) Assert.IsNotEmpty(JournalValidation.Validate(Draft(amount), "USD", Accounts), $"Amount {amount}");
        var draft = Draft();
        Assert.IsNotEmpty(JournalValidation.Validate(draft with { Lines = [draft.Lines[0] with { Credit = 25 }, draft.Lines[1]] }, "USD", Accounts));
        Assert.IsNotEmpty(JournalValidation.Validate(draft with { Lines = [draft.Lines[0] with { Debit = 26 }, draft.Lines[1]] }, "USD", Accounts));
    }
    [TestMethod]
    public void ExactValuesAndLargeTotalsAreAccepted()
    {
        foreach (var amount in new[] { 1.2300m, 99999999.99m }) Assert.IsEmpty(JournalValidation.Validate(Draft(amount), "USD", Accounts));
        var draft = Draft(99999999.99m);
        Assert.IsEmpty(JournalValidation.Validate(draft with { Lines = draft.Lines.Concat(draft.Lines.Select(x => x with { Order = x.Order + 2 })).ToArray() }, "USD", Accounts));
    }
    [TestMethod]
    public void AccountsDatesAndCurrencyAreChecked()
    {
        var draft = Draft();
        foreach (var line in new[] { draft.Lines[0] with { AccountId = 999 }, draft.Lines[0] with { CurrencyCode = "EUR" } })
            Assert.IsNotEmpty(JournalValidation.Validate(draft with { Lines = [line, draft.Lines[1]] }, "USD", Accounts));
        Assert.IsNotEmpty(JournalValidation.Validate(draft with { AccountingDate = Day.AddYears(-2) }, "USD", Accounts));
        Assert.IsNotEmpty(JournalValidation.Validate(draft, "USD", [Accounts[0] with { BookId = 2 }, Accounts[1]]));
        Assert.IsNotEmpty(JournalValidation.Validate(draft, "USD", [Accounts[0] with { CloseDate = Day.AddDays(1) }, Accounts[1]], draft.Lines));
        Assert.IsNotEmpty(JournalValidation.Validate(Draft(1.23m), "JPY", Accounts));
        Assert.IsNotEmpty(JournalValidation.Validate(draft, "GBP", Accounts));
        JournalRequest Currency(JournalRequest value, string currency) => value with { Lines = value.Lines.Select(x => x with { CurrencyCode = currency }).ToArray() };
        Assert.IsEmpty(JournalValidation.Validate(Currency(Draft(1), "JPY"), "JPY", Accounts));
        Assert.IsNotEmpty(JournalValidation.Validate(Currency(Draft(1.23m), "JPY"), "JPY", Accounts));
        Assert.IsEmpty(JournalValidation.Validate(Currency(Draft(1.2300m), "EUR"), "EUR", Accounts));
    }
    [TestMethod]
    public void ExistingClosedReferencesRequireDateBeforeClosureAndCompleteVersions()
    {
        var draft = Draft() with { EntryId = 10, Version = new byte[8], OriginalLines = [new(11, new byte[8])], Lines = [Draft().Lines[0] with { LineId = 11, Version = new byte[8] }, Draft().Lines[1]] };
        Assert.IsEmpty(JournalValidation.Validate(draft, "USD", [Accounts[0] with { CloseDate = Day.AddDays(1) }, Accounts[1]], draft.Lines));
        Assert.IsNotEmpty(JournalValidation.Validate(draft, "USD", [Accounts[0] with { CloseDate = Day }, Accounts[1]], draft.Lines));
        Assert.IsNotEmpty(JournalValidation.Validate(draft with { OriginalLines = [] }, "USD", Accounts));
    }
}
