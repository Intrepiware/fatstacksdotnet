using System.ComponentModel;
using System.Windows;
using FatStacks.Desktop.ViewModels;
namespace FatStacks.Desktop.Views;

public partial class AccountSetupWindow : Window
{
    private readonly AccountSetupViewModel model;
    public AccountSetupWindow(AccountSetupViewModel model)
    { InitializeComponent(); this.model = model; DataContext = model; Loaded += async (_, _) => { try { await model.LoadAsync(); } catch (OperationCanceledException) { Close(); } catch (Microsoft.Data.SqlClient.SqlException) { MessageBox.Show(this, "Accounts could not be loaded. Check the connection."); Close(); } }; }
    private void TreeAccountChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is AccountBrowserItem item) model.Selected = item.Account;
    }
    private void NewClicked(object sender, RoutedEventArgs e) { model.New(); AccountName.Focus(); }
    private async void SaveClicked(object sender, RoutedEventArgs e) => await model.SaveAsync();
    private void CloseClicked(object sender, RoutedEventArgs e) => Close();
    protected override void OnClosing(CancelEventArgs e) { base.OnClosing(e); if (model.Busy) e.Cancel = true; }
}
