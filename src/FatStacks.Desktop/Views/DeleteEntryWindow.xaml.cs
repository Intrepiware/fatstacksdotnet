using System.ComponentModel;
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
        Preview.Text = $"{entry.AccountingDate:d} · {entry.Description}";
        Lines.ItemsSource = entry.Lines.Select(x => new { Account = accounts.FirstOrDefault(a => a.AccountId == x.AccountId) is { } account ? AccountCatalog.Path(account, accounts) : "Unavailable account", x.Debit, x.Credit, Status = x.Status switch { 1 => "Reconciling", 2 => "Cleared", _ => "Uncleared" } }).ToArray();
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
