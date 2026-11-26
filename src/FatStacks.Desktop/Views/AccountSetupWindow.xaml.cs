using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using FatStacks.Desktop.ViewModels;
using FatStacks.Infrastructure.Diagnostics;
namespace FatStacks.Desktop.Views;

public partial class AccountSetupWindow : Window
{
    private readonly AccountSetupViewModel model;
    public AccountSetupWindow(AccountSetupViewModel model)
    {
        InitializeComponent();
        this.model = model;
        DataContext = model;
        Loaded += async (_, _) => { try { await model.LoadAsync(); } catch (OperationCanceledException) { Close(); } catch (Microsoft.Data.SqlClient.SqlException ex) { DatabaseErrors.TraceFailure("AccountLoad", ex); MessageBox.Show(this, "Accounts could not be loaded. Check the connection."); Close(); } };
    }
    private void AccountSetupPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F || Keyboard.Modifiers != ModifierKeys.Control)
            return;

        e.Handled = true;
        AccountSearch.Focus();
    }
    private void TreeAccountChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (model.IsTreeView && e.NewValue is AccountBrowserItem item)
            model.SelectBrowserItem(item);
    }
    private void AccountViewClicked(object sender, RoutedEventArgs e) => model.IsTreeView = sender == TreeViewOption;
    private void NewClicked(object sender, RoutedEventArgs e)
    {
        model.New();
        AccountName.Focus();
    }
    private async void SaveClicked(object sender, RoutedEventArgs e) => await model.SaveAsync();
    private void CloseClicked(object sender, RoutedEventArgs e) => Close();
    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (model.Busy)
            e.Cancel = true;
    }
}
