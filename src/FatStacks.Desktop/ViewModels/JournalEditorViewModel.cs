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
    private string debit = ""; public string Debit { get => debit; set => Set(ref debit, value); }
    private string credit = ""; public string Credit { get => credit; set => Set(ref credit, value); }
    public void FormatAmount(bool isCredit)
    {
        var text = isCredit ? Credit : Debit;
        if (string.IsNullOrWhiteSpace(text) || !JournalAmountInput.TryParse(text, out var amount)) return;
        var formatted = amount.ToString("F2", CultureInfo.CurrentCulture);
        if (isCredit) Credit = formatted; else Debit = formatted;
    }
    public string StatusName => Original?.Status switch { 1 => "Reconciling", 2 => "Cleared", _ => "Uncleared" };
    public string Name => $"Posting to {(AccountText.Length == 0 ? "unselected account" : AccountText)}";
}
public sealed class JournalEditorViewModel : ObservableObject
{
    public JournalEditorViewModel()
    {
        Lines.CollectionChanged += (_, _) => Changed(nameof(CanRemoveLine));
    }
    public bool CanRemoveLine => Idle && Lines.Count > 2;
    public required BookCalendar Calendar { private get; init; }
    public required IJournalService Journal { private get; init; }
    private IReadOnlyList<AccountRecord> accounts = [];
    private IReadOnlyList<AccountSuggestion> suggestions = [];
    private JournalRequest? original;
    public ObservableCollection<JournalLineViewModel> Lines { get; } = [];
    public ObservableCollection<JournalError> Errors { get; } = [];
    private string currency = ""; public string Currency => currency;
    private DateTime? date; public DateTime? AccountingDate { get => date; set { if (Set(ref date, value)) foreach (var line in Lines) UpdateEligibility(line); } }
    private DateTime? bankDate; public DateTime? BankPostingDate { get => bankDate; set => Set(ref bankDate, value); }
    private string payee = ""; public string Payee { get => payee; set => Set(ref payee, value); }
    private string description = ""; public string Description { get => description; set => Set(ref description, value); }
    private string status = ""; public string Status { get => status; private set => Set(ref status, value); }
    private bool busy; public bool IsBusy { get => busy; private set { Set(ref busy, value); Changed(nameof(Idle)); Changed(nameof(CanRemoveLine)); } }
    public bool Idle => !IsBusy;
    public bool ShouldClose { get; private set; }
    public JournalSaveResult? LastSaved { get; private set; }
    public event EventHandler? CloseRequested;
    public void Initialize(JournalRequest? entry, IReadOnlyList<AccountRecord> available, string bookCurrency, DateOnly today, long? selectedAccount = null)
    {
        original = entry; accounts = available; currency = bookCurrency; Changed(nameof(Currency));
        suggestions = accounts.Select(x => new AccountSuggestion(x.AccountId, AccountCatalog.Path(x, accounts))).OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase).ToArray();
        AccountingDate = (entry?.AccountingDate ?? today).ToDateTime(TimeOnly.MinValue); BankPostingDate = entry?.BankPostingDate?.ToDateTime(TimeOnly.MinValue);
        Payee = entry?.Payee ?? ""; Description = entry?.Description ?? ""; Lines.Clear(); Errors.Clear();
        if (entry is not null) foreach (var line in entry.Lines) Lines.Add(NewLine(line));
        else { Lines.Add(NewLine()); Lines.Add(NewLine()); if (selectedAccount is { } id) Lines[0].AccountText = suggestions.FirstOrDefault(x => x.Id == id)?.Path ?? ""; }
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
            Errors.Add(new(bankPostingDate ? "BankPostingDate" : "AccountingDate", "The date must stay between January 1, 0001 and December 31, 9999."));
            return false;
        }
        if (bankPostingDate) BankPostingDate = day.ToDateTime(TimeOnly.MinValue);
        else AccountingDate = day.ToDateTime(TimeOnly.MinValue);
        return true;
    }
    private JournalLineViewModel NewLine(JournalLine? line = null)
    {
        var item = new JournalLineViewModel { Accounts = suggestions, Original = line, AccountText = suggestions.FirstOrDefault(x => x.Id == line?.AccountId)?.Path ?? "", Debit = line?.Debit > 0 ? line.Debit.ToString(CultureInfo.CurrentCulture) : "", Credit = line?.Credit > 0 ? line.Credit.ToString(CultureInfo.CurrentCulture) : "" };
        UpdateEligibility(item); return item;
    }
    private void UpdateEligibility(JournalLineViewModel line)
    {
        var eligible = AccountingDate is { } day
            ? accounts.Where(x => x.BookId == 1 && x.CanPost(DateOnly.FromDateTime(day), line.Original?.LineId is not null && line.Original.AccountId == x.AccountId)).Select(x => x.AccountId).ToHashSet()
            : [];
        line.SetEligibleAccounts(eligible);
    }

    public void AddLine() { if (Idle) Lines.Add(NewLine()); }
    public void RemoveLine(JournalLineViewModel line) { if (Idle && Lines.Count > 2) Lines.Remove(line); }
    private JournalRequest? Request()
    {
        Errors.Clear();
        if (AccountingDate is null) { Errors.Add(new("AccountingDate", "Choose an accounting date.")); return null; }
        var lines = new List<JournalLine>();
        foreach (var line in Lines)
        {
            decimal Parse(string text, string field)
            {
                if (string.IsNullOrWhiteSpace(text)) return 0;
                if (JournalAmountInput.TryParse(text, out var value)) return value;
                Errors.Add(new(field, "Enter a decimal amount with at most two decimal places; trailing zeros are allowed.", lines.Count + 1)); return 0;
            }
            var debit = Parse(line.Debit, "Debit"); var credit = Parse(line.Credit, "Credit");
            lines.Add(new(line.Original?.LineId, line.Original?.Version, line.AccountId, lines.Count + 1, debit, credit, Currency, line.Original?.Status ?? 0, line.Original?.SecurityId, line.Original?.Quantity));
        }
        var request = new JournalRequest(1, original?.EntryId, original?.Version, DateOnly.FromDateTime(AccountingDate.Value), BankPostingDate is { } bank ? DateOnly.FromDateTime(bank) : null, Payee, Description, original?.EntryKind ?? 0, lines, original?.OriginalLines ?? []);
        foreach (var error in JournalValidation.Validate(request, Currency, accounts, original?.Lines)) Errors.Add(error);
        return Errors.Count == 0 ? request : null;
    }
    public bool IsUnbalanced
    {
        get
        {
            decimal total = 0;
            foreach (var line in Lines)
            {
                if (!JournalAmountInput.TryParse(line.Debit, out var debit) || !JournalAmountInput.TryParse(line.Credit, out var credit)) return false;
                try { total += debit - credit; } catch (OverflowException) { return true; }
            }
            return total != 0;
        }
    }
    public async Task<bool> SaveAsync(bool continueEntry)
    {
        if (IsBusy) return false;
        var request = Request(); if (request is null) { Status = "Correct the fields below."; return false; }
        IsBusy = true;
        try
        {
            var result = await Journal.SaveAsync(request);
            if (!result.IsSuccess || result.Value is null)
            {
                Status = result.Message;
                foreach (var error in result.Errors ?? []) Errors.Add(new(error.Field, error.Message, error.LineOrder));
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
