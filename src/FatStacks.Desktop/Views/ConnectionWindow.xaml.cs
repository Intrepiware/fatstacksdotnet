using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using FatStacks.Desktop.ViewModels;
namespace FatStacks.Desktop.Views;

public partial class ConnectionWindow : Window
{
    private readonly ConnectionViewModel model;
    public ConnectionWindow(ConnectionViewModel model)
    {
        InitializeComponent(); this.model = model; DataContext = model;
        model.Opened += Opened; model.PropertyChanged += ModelChanged;
        Closed += (_, _) => { model.Opened -= Opened; model.PropertyChanged -= ModelChanged; };
    }
    private void ModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ConnectionViewModel.Selected))
        {
            // PasswordBox cannot use a two-way text binding. The VM restores its protected-profile value.
            if (model.Selected is not null)
            {
                var b = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(model.Selected.Settings.ConnectionString);
                Password.Password = b.Password;
            }
            else Password.Clear();
        }
    }
    private void Opened(object? sender, EventArgs e) => Dispatcher.BeginInvoke(new Action(Close));
    private void PasswordChanged(object sender, RoutedEventArgs e) { if (DataContext is ConnectionViewModel vm) vm.Password = Password.Password; }
    private void CancelClicked(object sender, RoutedEventArgs e) => model.Cancel();
    private void CloseClicked(object sender, RoutedEventArgs e) { model.Cancel(); Close(); }
    protected override void OnClosing(CancelEventArgs e)
    {
        if (model.Busy) { model.Cancel(); e.Cancel = true; }
        base.OnClosing(e);
    }
    private void InitializeClicked(object sender, RoutedEventArgs e)
    {
        var setup = new BookSetupWindow(model) { Owner = this }; setup.ShowDialog();
    }
    private async void ReassociateClicked(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, "Associate this saved profile with the book at the current server and database? No financial records will change.", "Reassociate profile", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
            await model.ReassociateAsync();
    }
}
