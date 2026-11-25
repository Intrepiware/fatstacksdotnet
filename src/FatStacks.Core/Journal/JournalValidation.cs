using FatStacks.Core.Accounts;
namespace FatStacks.Core.Journal;

public static class JournalValidation
{
    public const decimal MaximumAmount = 99999999.99m;
    public static IReadOnlyList<JournalError> Validate(JournalRequest request, string currency, IReadOnlyList<AccountRecord> accounts, IReadOnlyList<JournalLine>? originalLines = null)
    {
        var errors = new List<JournalError>();
        void Error(string field, string message, int? order = null) => errors.Add(new(field, message, order));
        if (request.BookId != 1) Error("Book", "Reload the active book.");
        if (currency is not ("USD" or "EUR" or "JPY")) Error("Currency", "Choose a supported book currency.");
        if (request.Payee?.Length > 200) Error("Payee", "Use at most 200 characters.");
        if (request.Description is null || request.Description.Length > 1000) Error("Description", "Use at most 1000 characters.");
        if (request.EntryKind != 0) Error("EntryKind", "Only ordinary monetary entries are available.");
        if (request.EntryId is <= 0 || request.EntryId is not null && request.Version?.Length != 8 || request.EntryId is null && (request.Version is not null || request.OriginalLines.Count != 0)) Error("Version", "Reload the complete entry before saving.");
        if (request.OriginalLines.Any(x => x.LineId <= 0 || x.Version.Length != 8) || request.OriginalLines.Select(x => x.LineId).Distinct().Count() != request.OriginalLines.Count) Error("Version", "Reload the complete original line set.");
        if (request.Lines.Count < 2) Error("Lines", "Supply at least two balanced lines.");
        if (request.Lines.Select(x => x.Order).Distinct().Count() != request.Lines.Count || request.Lines.Where(x => x.LineId is not null).Select(x => x.LineId).Distinct().Count() != request.Lines.Count(x => x.LineId is not null)) Error("Lines", "Line identities and orders must be unique.");
        decimal debit = 0, credit = 0;
        foreach (var line in request.Lines)
        {
            if (line.Order <= 0) Error("Order", "Choose a positive line order.", line.Order);
            if (line.LineId is { } id && (request.EntryId is null || line.Version?.Length != 8 || !request.OriginalLines.Any(x => x.LineId == id && x.Version.SequenceEqual(line.Version)))) Error("Version", "Reload this line's original version.", line.Order);
            if (line.LineId is null && line.Version is not null) Error("Version", "New lines cannot have stored versions.", line.Order);
            var digits = currency == "JPY" ? 0 : 2;
            void ValidateSide(decimal value, string field)
            {
                if (value < 0) Error(field, "Enter a nonnegative amount.", line.Order);
                else if (value > MaximumAmount || decimal.Round(value, digits) != value)
                    Error(field, $"Use at most {digits} decimal places and 99,999,999.99 per side.", line.Order);
            }
            ValidateSide(line.Debit, "Debit");
            ValidateSide(line.Credit, "Credit");
            if ((line.Debit == 0 && line.Credit == 0) || (line.Debit > 0 && line.Credit > 0))
                Error("Amount", "Enter one positive Debit or Credit.", line.Order);
            if (line.CurrencyCode != currency) Error("Currency", "Use the book currency.", line.Order);
            var account = accounts.FirstOrDefault(x => x.AccountId == line.AccountId && x.BookId == request.BookId);
            var retainedReference = line.LineId is { } lineId && originalLines?.Any(x => x.LineId == lineId && x.AccountId == line.AccountId) == true;
            if (account is null) Error("Account", "This account is not available in the active book.", line.Order);
            else if (request.AccountingDate < account.OpenDate) errors.Add(new JournalAccountDateError(account, $"This account opens on {account.OpenDate:yyyy-MM-dd}. Use that date or later.", line.Order));
            else if (account.CloseDate is { } close && (!retainedReference || request.AccountingDate >= close))
                errors.Add(new JournalAccountDateError(account, retainedReference ? $"This account closed on {close:yyyy-MM-dd}. Its historical reference must stay before this date." : "This account is closed. New references are not allowed.", line.Order));
            if (line.Status > 2 || line.LineId is null && line.Status != 0) Error("Status", "New lines start Uncleared; change existing states through reconciliation.", line.Order);
            if (line.SecurityId is not null || line.Quantity is not null) Error("Security", "Security entries are not available in this editor.", line.Order);
            try { debit += line.Debit; credit += line.Credit; }
            catch (OverflowException) { Error("Amount", "Entry totals exceed the supported decimal range.", line.Order); }
        }
        if (debit != credit) Error("Balance", "Total Debit must equal total Credit.");
        return errors;
    }
}
