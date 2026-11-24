using System.Collections.ObjectModel;
using System.Globalization;
using FatStacks.Core.Accounts;
using FatStacks.Core.Calendar;
using FatStacks.Core.Contracts;
using FatStacks.Core.Journal;
using FatStacks.Infrastructure.Diagnostics;
namespace FatStacks.Desktop.ViewModels;

// Validate the source precision before decimal parsing can discard fractional digits.
internal static class JournalAmountInput
{
    public static bool TryParse(string text, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(text)) return true;
        var separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        var point = text.LastIndexOf(separator, StringComparison.Ordinal);
        if (point >= 0 && text.Skip(point + separator.Length + 2).Any(c => c is >= '1' and <= '9')) return false;
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out amount);
    }
}

public sealed record AccountSuggestion(long Id, string Path);
public sealed class JournalLineViewModel : ObservableObject
{
    public required IReadOnlyList<AccountSuggestion> Accounts { private get; init; }
    public JournalLine? Original { get; init; }
    public string Key { get; } = Guid.NewGuid().ToString("N");
    private int lineNumber;
    public int LineNumber { get => lineNumber; internal set { if (Set(ref lineNumber, value)) { Changed(nameof(IsEvenRow)); Changed(nameof(Name)); } } }
    public bool IsEvenRow => LineNumber % 2 == 0;
    private bool willReset;
    public bool WillReset { get => willReset; internal set => Set(ref willReset, value); }
    private string accountError = "", debitError = "", creditError = "";
    public string AccountError => accountError;
    public string DebitError => debitError;
    public string CreditError => creditError;
    public bool HasErrors => accountError.Length + debitError.Length + creditError.Length > 0;
    internal void SetErrors(IEnumerable<JournalError> errors)
    {
        var list = errors.ToArray();
        string Message(params string[] fields) => string.Join(" ", list.Where(x => fields.Contains(x.Field)).Select(x => x.Message).Distinct());
        accountError = Message("Account"); debitError = Message("Debit", "Amount", "Currency"); creditError = Message("Credit", "Amount", "Currency");
        Changed(nameof(AccountError)); Changed(nameof(DebitError)); Changed(nameof(CreditError)); Changed(nameof(HasErrors));
    }
    private IReadOnlySet<long>? eligibleAccounts;
    private IEnumerable<AccountSuggestion> EligibleAccounts => Accounts.Where(x => eligibleAccounts is null || eligibleAccounts.Contains(x.Id));
    public IReadOnlyList<string> AccountPaths => EligibleAccounts.Select(x => x.Path).ToArray();
    public void SetEligibleAccounts(IReadOnlySet<long> eligible)
    {
        eligibleAccounts = eligible; Changed(nameof(AccountPaths)); Changed(nameof(Suggestions));
    }
    public string AccountControlId => $"JournalAccount_{Key}";
    public string DebitControlId => $"JournalDebit_{Key}";
    public string CreditControlId => $"JournalCredit_{Key}";
    private string accountText = ""; public string AccountText { get => accountText; set { if (Set(ref accountText, value)) { Changed(nameof(Suggestions)); Changed(nameof(Name)); } } }
    public IReadOnlyList<AccountSuggestion> Suggestions => EligibleAccounts.Where(x => x.Path.Contains(AccountText, StringComparison.OrdinalIgnoreCase)).ToArray();
    public long AccountId => Accounts.FirstOrDefault(x => x.Path.Equals(AccountText, StringComparison.OrdinalIgnoreCase))?.Id ?? 0;
    private string debit = ""; public string Debit { get => debit; set { if (Set(ref debit, value) && !string.IsNullOrWhiteSpace(value)) Credit = ""; } }
    private string credit = ""; public string Credit { get => credit; set { if (Set(ref credit, value) && !string.IsNullOrWhiteSpace(value)) Debit = ""; } }
    public void FormatAmount(bool isCredit)
    {
        var text = isCredit ? Credit : Debit;
        if (string.IsNullOrWhiteSpace(text) || !JournalAmountInput.TryParse(text, out var amount)) return;
        var formatted = amount.ToString("F2", CultureInfo.CurrentCulture);
        if (isCredit) Credit = formatted; else Debit = formatted;
    }
    public string StatusName => Original?.Status switch { 1 => "Reconciling", 2 => "Cleared", _ => "Uncleared" };
    public string StatusSymbol => Original?.Status switch { 1 => "R", 2 => "✓", _ => "" };
    public string Name => $"Line {LineNumber}: posting to {(AccountText.Length == 0 ? "unselected account" : AccountText)}";
}
public sealed class JournalEditorViewModel : ObservableObject
{
    public JournalEditorViewModel()
    {
        Lines.CollectionChanged += (_, _) =>
        {
            for (var i = 0; i < Lines.Count; i++) Lines[i].LineNumber = i + 1;
            Changed(nameof(CanRemoveLine)); UpdateResetWarning(); BalanceChanged(); RefreshValidation();
        };
        Errors.CollectionChanged += (_, _) => UpdateErrorPresentation();
    }
    public bool CanRemoveLine => Idle && Lines.Count > 2;
    public required BookCalendar Calendar { private get; init; }
    public required IJournalService Journal { private get; init; }
    private IReadOnlyList<AccountRecord> accounts = [];
    private IReadOnlyList<AccountSuggestion> suggestions = [];
    private JournalRequest? original;
    private bool validationRequested, initializing;
    private readonly Dictionary<string, string> dateErrors = [];
    private readonly List<(JournalLineViewModel? Line, JournalError Error)> serviceErrors = [];
    public string AccountingDateError => string.Join(" ", Errors.Where(x => x.Field == "AccountingDate").Select(x => x.Message).Distinct());
    public string BankPostingDateError => string.Join(" ", Errors.Where(x => x.Field == "BankPostingDate").Select(x => x.Message).Distinct());
    public void SetDateError(bool bank, string message)
    {
        var field = bank ? "BankPostingDate" : "AccountingDate";
        if (message.Length == 0) dateErrors.Remove(field); else dateErrors[field] = message;
        RefreshValidation();
    }
    public ObservableCollection<JournalLineViewModel> Lines { get; } = [];
    public ObservableCollection<JournalError> Errors { get; } = [];
    private string currency = ""; public string Currency => currency;
    private DateTime? date; public DateTime? AccountingDate { get => date; set { Set(ref date, value); dateErrors.Remove("AccountingDate"); foreach (var line in Lines) UpdateEligibility(line); RefreshValidation(); } }
    private DateTime? bankDate; public DateTime? BankPostingDate { get => bankDate; set { Set(ref bankDate, value); dateErrors.Remove("BankPostingDate"); RefreshValidation(); } }
    private string payee = ""; public string Payee { get => payee; set { if (Set(ref payee, value)) RefreshValidation(); } }
    private string description = ""; public string Description { get => description; set { if (Set(ref description, value)) RefreshValidation(); } }
    private string status = ""; public string Status { get => status; private set => Set(ref status, value); }
    private string resetWarning = ""; public string ResetWarning { get => resetWarning; private set { if (Set(ref resetWarning, value)) Changed(nameof(HasResetWarning)); } }
    public bool HasResetWarning => ResetWarning.Length > 0;
    private bool busy; public bool IsBusy { get => busy; private set { Set(ref busy, value); Changed(nameof(Idle)); Changed(nameof(CanRemoveLine)); Changed(nameof(CanSave)); } }
    public bool Idle => !IsBusy;
    public bool ShouldClose { get; private set; }
    public JournalSaveResult? LastSaved { get; private set; }
    public event EventHandler? CloseRequested;
    public void Initialize(JournalRequest? entry, IReadOnlyList<AccountRecord> available, string bookCurrency, DateOnly today, long? selectedAccount = null)
    {
        initializing = true; validationRequested = false; dateErrors.Clear(); serviceErrors.Clear();
        original = entry; accounts = available; currency = bookCurrency; Changed(nameof(Currency));
        suggestions = accounts.Select(x => new AccountSuggestion(x.AccountId, AccountCatalog.Path(x, accounts))).OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase).ToArray();
        AccountingDate = (entry?.AccountingDate ?? today).ToDateTime(TimeOnly.MinValue); BankPostingDate = entry?.BankPostingDate?.ToDateTime(TimeOnly.MinValue);
        Payee = entry?.Payee ?? ""; Description = entry?.Description ?? "";
        foreach (var line in Lines) line.PropertyChanged -= LineChanged;
        Lines.Clear(); Errors.Clear();
        if (entry is not null) foreach (var line in entry.Lines) Lines.Add(NewLine(line));
        else { Lines.Add(NewLine()); Lines.Add(NewLine()); if (selectedAccount is { } id) Lines[0].AccountText = suggestions.FirstOrDefault(x => x.Id == id)?.Path ?? ""; }
        initializing = false; UpdateResetWarning(); RefreshValidation();
    }
    public bool ApplyDateShortcut(char shortcut, bool bankPostingDate = false)
    {
        if (IsBusy || shortcut is not ('-' or '+' or 't' or 'T')) return false;
        var current = bankPostingDate ? BankPostingDate : AccountingDate;
        var day = current is { } value ? DateOnly.FromDateTime(value) : Calendar.Today;
        try
        {
            day = shortcut switch { '-' => day.AddDays(-1), '+' => day.AddDays(1), _ => Calendar.Today };
        }
        catch (ArgumentOutOfRangeException)
        {
            SetDateError(bankPostingDate, "The date must stay between January 1, 0001 and December 31, 9999.");
            return false;
        }
        if (bankPostingDate) BankPostingDate = day.ToDateTime(TimeOnly.MinValue);
        else AccountingDate = day.ToDateTime(TimeOnly.MinValue);
        return true;
    }
    private JournalLineViewModel NewLine(JournalLine? line = null)
    {
        var item = new JournalLineViewModel { Accounts = suggestions, Original = line, AccountText = suggestions.FirstOrDefault(x => x.Id == line?.AccountId)?.Path ?? "", Debit = line?.Debit > 0 ? line.Debit.ToString(CultureInfo.CurrentCulture) : "", Credit = line?.Credit > 0 ? line.Credit.ToString(CultureInfo.CurrentCulture) : "" };
        UpdateEligibility(item); item.PropertyChanged += LineChanged; return item;
    }
    private void LineChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(JournalLineViewModel.AccountText) or nameof(JournalLineViewModel.Debit) or nameof(JournalLineViewModel.Credit))
        {
            serviceErrors.RemoveAll(x => ReferenceEquals(x.Line, sender) && (x.Error.Field == "Account" && e.PropertyName == nameof(JournalLineViewModel.AccountText) || x.Error.Field is "Amount" or "Debit" or "Credit" && e.PropertyName is nameof(JournalLineViewModel.Debit) or nameof(JournalLineViewModel.Credit)));
            UpdateResetWarning(); RefreshValidation();
        }
        if (e.PropertyName is nameof(JournalLineViewModel.Debit) or nameof(JournalLineViewModel.Credit)) BalanceChanged();
    }
    private void BalanceChanged()
    {
        Changed(nameof(IsUnbalanced)); Changed(nameof(BalanceSummary)); Changed(nameof(CanSave));
        Changed(nameof(BalanceStatus)); Changed(nameof(DebitTotal)); Changed(nameof(CreditTotal)); Changed(nameof(DebitOver)); Changed(nameof(CreditOver));
    }
    private void UpdateResetWarning()
    {
        foreach (var line in Lines) line.WillReset = ChangesReconciledLine(line);
        var changed = Lines.Select((line, index) => (line, index)).Where(x => ChangesReconciledLine(x.line))
            .Select(x => $"Line {x.index + 1} ({(x.line.AccountText.Length > 0 ? x.line.AccountText : suggestions.FirstOrDefault(a => a.Id == x.line.Original!.AccountId)?.Path ?? "Unavailable account")})")
            .ToArray();
        ResetWarning = changed.Length == 0 ? "" : $"Saving will reset to Uncleared: {string.Join("; ", changed)}.";
    }
    private static bool ChangesReconciledLine(JournalLineViewModel line)
    {
        if (line.Original is not { Status: > 0 } original) return false;
        if (line.AccountId != original.AccountId) return true;
        return JournalAmountInput.TryParse(line.Debit, out var debit) &&
            JournalAmountInput.TryParse(line.Credit, out var credit) &&
            (debit != original.Debit || credit != original.Credit);
    }
    private void UpdateEligibility(JournalLineViewModel line)
    {
        var eligible = AccountingDate is { } day
            ? accounts.Where(x => x.BookId == 1 && x.CanPost(DateOnly.FromDateTime(day), line.Original?.LineId is not null && line.Original.AccountId == x.AccountId)).Select(x => x.AccountId).ToHashSet()
            : [];
        line.SetEligibleAccounts(eligible);
    }

    public void AddLine() { if (Idle) Lines.Add(NewLine()); }
    public void RemoveLine(JournalLineViewModel line) { if (Idle && Lines.Count > 2) { line.PropertyChanged -= LineChanged; Lines.Remove(line); } }
    private JournalRequest? Request(List<JournalError> errors)
    {
        if (AccountingDate is null) errors.Add(new("AccountingDate", "Choose an accounting date."));
        var lines = new List<JournalLine>();
        foreach (var line in Lines)
        {
            decimal Parse(string text, string field)
            {
                if (string.IsNullOrWhiteSpace(text)) return 0;
                if (JournalAmountInput.TryParse(text, out var value)) return value;
                errors.Add(new(field, "Enter a decimal amount with at most two decimal places; trailing zeros are allowed.", lines.Count + 1)); return 0;
            }
            var debit = Parse(line.Debit, "Debit"); var credit = Parse(line.Credit, "Credit");
            lines.Add(new(line.Original?.LineId, line.Original?.Version, line.AccountId, lines.Count + 1, debit, credit, Currency, line.Original?.Status ?? 0, line.Original?.SecurityId, line.Original?.Quantity));
        }
        var request = new JournalRequest(1, original?.EntryId, original?.Version, AccountingDate is { } day ? DateOnly.FromDateTime(day) : Calendar.Today, BankPostingDate is { } bank ? DateOnly.FromDateTime(bank) : null, Payee, Description, original?.EntryKind ?? 0, lines, original?.OriginalLines ?? []);
        foreach (var error in JournalValidation.Validate(request, Currency, accounts, original?.Lines)) errors.Add(error);
        return errors.Count == 0 ? request : null;
    }
    private void RefreshValidation()
    {
        if (initializing) return;
        var errors = new List<JournalError>();
        if (validationRequested) Request(errors);
        else
        {
            foreach (var line in Lines)
            {
                if (!JournalAmountInput.TryParse(line.Debit, out _)) errors.Add(new("Debit", "Enter a decimal amount with at most two decimal places; trailing zeros are allowed.", line.LineNumber));
                if (!JournalAmountInput.TryParse(line.Credit, out _)) errors.Add(new("Credit", "Enter a decimal amount with at most two decimal places; trailing zeros are allowed.", line.LineNumber));
            }
        }
        errors.AddRange(dateErrors.Select(x => new JournalError(x.Key, x.Value)));
        errors.AddRange(serviceErrors.Where(x => x.Line is null || Lines.Contains(x.Line)).Select(x => x.Error with { LineOrder = x.Line?.LineNumber }));
        Errors.Clear();
        foreach (var error in errors.Distinct()) Errors.Add(error);
    }
    private void UpdateErrorPresentation()
    {
        foreach (var line in Lines) line.SetErrors(Errors.Where(x => x.LineOrder == line.LineNumber));
        Changed(nameof(AccountingDateError)); Changed(nameof(BankPostingDateError));
    }
    private bool TryTotals(out decimal debits, out decimal credits)
    {
        debits = 0; credits = 0;
        foreach (var line in Lines)
        {
            if (!JournalAmountInput.TryParse(line.Debit, out var debit) || !JournalAmountInput.TryParse(line.Credit, out var credit) || debit < 0 || credit < 0) return false;
            try { debits += debit; credits += credit; } catch (OverflowException) { return false; }
        }
        return true;
    }
    public bool IsUnbalanced => TryTotals(out var debits, out var credits) && debits != credits;
    public bool CanSave => Idle && !IsUnbalanced;
    public string DebitTotal => TryTotals(out var debit, out _) ? debit.ToString("N2", CultureInfo.CurrentCulture) : "";
    public string CreditTotal => TryTotals(out _, out var credit) ? credit.ToString("N2", CultureInfo.CurrentCulture) : "";
    public bool DebitOver => TryTotals(out var debit, out var credit) && debit > credit;
    public bool CreditOver => TryTotals(out var debit, out var credit) && credit > debit;
    public string BalanceStatus => !TryTotals(out var debit, out var credit) ? "Enter valid amounts to check the balance."
        : debit == credit ? "Balanced" : $"{(debit > credit ? "Credits" : "Debits")} short by {Math.Abs(debit - credit):N2}";
    public string BalanceSummary
    {
        get
        {
            if (!TryTotals(out var debits, out var credits)) return "Enter valid amounts to check the balance.";
            var totals = $"Debit: {Currency} {debits:N2}    Credit: {Currency} {credits:N2}";
            return debits == credits ? $"{totals}    Balanced" : $"{totals}    Out of balance: {Currency} {Math.Abs(debits - credits):N2}";
        }
    }
    public async Task<bool> SaveAsync(bool continueEntry)
    {
        if (IsBusy) return false;
        serviceErrors.Clear(); validationRequested = true; RefreshValidation();
        var request = Request([]); if (request is null || Errors.Count > 0) { Status = "Correct the fields below."; return false; }
        IsBusy = true;
        try
        {
            var result = await Journal.SaveAsync(request);
            if (!result.IsSuccess || result.Value is null)
            {
                Status = result.Message;
                foreach (var error in result.Errors ?? [])
                    serviceErrors.Add((error.LineOrder is { } order ? Lines.ElementAtOrDefault(order - 1) : null, new(error.Field, error.Message, error.LineOrder)));
                RefreshValidation();
                return false;
            }
            LastSaved = result.Value;
            Status = JournalSaveNotice.Describe(result.Value, accounts);
            if (continueEntry) Initialize(null, accounts, Currency, request.AccountingDate);
            else { ShouldClose = true; CloseRequested?.Invoke(this, EventArgs.Empty); }
            return true;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) { DatabaseErrors.TraceFailure("JournalEditorSave", ex); Status = "Save was not confirmed. Keep this draft and reload before retrying."; return false; }
        finally { IsBusy = false; }
    }
    public void Cancel() { if (IsBusy) return; ShouldClose = true; CloseRequested?.Invoke(this, EventArgs.Empty); }
}
