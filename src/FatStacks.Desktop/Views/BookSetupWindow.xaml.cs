using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using FatStacks.Desktop.ViewModels;
namespace FatStacks.Desktop.Views;

public partial class BookSetupWindow : Window
{
    private readonly ConnectionViewModel model;
    public BookSetupWindow(ConnectionViewModel model)
    {
        InitializeComponent(); this.model = model; TimeZone.ItemsSource = TimeZoneInfo.GetSystemTimeZones(); TimeZone.SelectedValue = TimeZoneInfo.Local.Id;
    }
    private async void InitializeClicked(object sender, RoutedEventArgs e)
    {
        Initialize.IsEnabled = false; Feedback.Text = "Preparing the database. Once schema changes begin, wait for maintenance to finish.";
        try
        {
            var initialized = await model.InitializeAsync(BookName.Text, ((ComboBoxItem)Currency.SelectedItem).Content.ToString()!, TimeZone.SelectedValue?.ToString() ?? "", MaintenanceUser.Text, MaintenancePassword.Password, Integrated.IsChecked == true);
            MaintenancePassword.Clear(); Feedback.Text = model.Status;
            if (initialized)
            {
                Initialize.Visibility = Visibility.Collapsed;
                CancelInitialize.Visibility = Visibility.Collapsed;
                CloseSetup.Visibility = Visibility.Visible;
                CloseSetup.IsDefault = true;
                CloseSetup.IsCancel = true;
                CloseSetup.Focus();
            }
        }
        finally { Initialize.IsEnabled = true; }
    }
    private void CloseClicked(object sender, RoutedEventArgs e) => Close();
    private void CancelClicked(object sender, RoutedEventArgs e) { model.Cancel(); if (!model.Busy) Close(); }
    protected override void OnClosing(CancelEventArgs e) { if (model.Busy) { model.Cancel(); e.Cancel = true; } base.OnClosing(e); }
}
