using System.ComponentModel;
using System.Globalization;
using System.Windows;
using FatStacks.Core.Accounts;
using FatStacks.Core.Journal;
using FatStacks.Infrastructure.Diagnostics;
namespace FatStacks.Desktop.Views;

public partial class DeleteEntryWindow : Window
{
    private readonly JournalRequest entry;
    private readonly Func<DeleteJournalRequest, Task<FatStacks.Core.Contracts.OperationResult<bool>>> delete;
    private bool busy;
    public DeleteEntryWindow(JournalRequest entry, IReadOnlyList<AccountRecord> accounts, Func<DeleteJournalRequest, Task<FatStacks.Core.Contracts.OperationResult<bool>>> delete)
    {
        this.entry = entry; this.delete = delete; InitializeComponent();
        JournalDate.Text = entry.AccountingDate.ToString("MMM d, yyyy", CultureInfo.CurrentCulture);
        var payee = string.IsNullOrWhiteSpace(entry.Payee) ? "" : entry.Payee.Trim();
        var description = string.IsNullOrWhiteSpace(entry.Description) ? "" : entry.Description.Trim();
        PayeeText.Text = payee.Length == 0 && description.Length == 0 ? "Journal entry" : payee;
        SeparatorText.Text = payee.Length > 0 && description.Length > 0 ? " — " : "";
        DescriptionText.Text = description;
        Preview.ToolTip = $"{PayeeText.Text}{SeparatorText.Text}{DescriptionText.Text}";
        System.Windows.Automation.AutomationProperties.SetName(Preview, (string)Preview.ToolTip);
        if (entry.BankPostingDate is { } bankDate) BankDate.Text = $"Bank posting date: {bankDate.ToString("MMM d, yyyy", CultureInfo.CurrentCulture)}";
        else BankDate.Visibility = Visibility.Collapsed;
        Lines.ItemsSource = entry.Lines.OrderBy(x => x.Order).Select(x => new
        {
            Account = accounts.FirstOrDefault(a => a.AccountId == x.AccountId) is { } account ? AccountCatalog.Path(account, accounts) : "Unavailable account",
            DebitText = x.Debit == 0 ? "" : x.Debit.ToString("N2", CultureInfo.CurrentCulture),
            CreditText = x.Credit == 0 ? "" : x.Credit.ToString("N2", CultureInfo.CurrentCulture),
            Status = x.Status switch { 1 => "Reconciling", 2 => "Cleared", _ => "Uncleared" },
            StatusSymbol = x.Status switch { 1 => "R", 2 => "✓", _ => "" }
        }).ToArray();
        Loaded += (_, _) => CancelButton.Focus();
    }
    private async void DeleteClicked(object sender, RoutedEventArgs e)
    {
        if (busy) return; busy = true; Actions.IsEnabled = false;
        try
        {
            var result = await delete(new(entry.BookId, entry.EntryId!.Value, entry.Version!, entry.OriginalLines));
            if (result.IsSuccess) { busy = false; DialogResult = true; }
            else Status.Text = result.Message;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) { DatabaseErrors.TraceFailure("JournalDelete", ex); Status.Text = "Deletion was not confirmed. Reload before retrying."; }
        finally { busy = false; Actions.IsEnabled = true; }
    }
    protected override void OnClosing(CancelEventArgs e) { if (busy) e.Cancel = true; base.OnClosing(e); }
}
