using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using FatStacks.Core.Connections;
using FatStacks.Desktop.ViewModels;
using FatStacks.Desktop.Views;
namespace FatStacks.Desktop;

public partial class MainWindow : Window
{
    public required ConnectionViewModel Connections { private get; init; }
    public required StartupViewModel Startup { private get; init; }
    public required BookSessionCoordinator Sessions { private get; init; }
    private MainWindowViewModel? register;
    private CancellationTokenSource? startup;
    private bool closingRequested;
    private bool shuttingDown;
    public MainWindow()
    {
        InitializeComponent(); Loaded += LoadedAsync; Closed += (_, _) => { if (Sessions is not null) Sessions.SessionChanged -= SessionChanged; };
        CommandBindings.Add(new(ApplicationCommands.Open, (_, _) => ShowConnections()));
        PreviewKeyDown += async (_, e) => { if (e.Key == Key.F5) { e.Handled = true; if (register is { } model) await model.RefreshAsync(); } };
        InputBindings.Add(new KeyBinding(ApplicationCommands.Open, new KeyGesture(Key.O, ModifierKeys.Control)));
    }
    private async void LoadedAsync(object sender, RoutedEventArgs e)
    {
        Sessions.SessionChanged += SessionChanged;
        using var cancellation = new CancellationTokenSource();
        startup = cancellation;
        try
        {
            await Connections.LoadAsync();
            if (closingRequested) return;
            Status.Text = "Opening the last successful profile… Use Connections to cancel.";
            var result = await Startup.OpenLastAsync(cancellation.Token);
            if (closingRequested) return;
            await Connections.LoadAsync();
            if (closingRequested) return;
            Render();
            if (result is not null)
            {
                Status.Text = result.IsSuccess ? "Book opened." : result.Message;
                Warning.Text = string.Join(Environment.NewLine, result.Warnings ?? []);
            }
            if (Sessions.Active is null) ShowConnections();
        }
        finally { startup = null; }
    }
    private void ShowConnections()
    {
        if (closingRequested) return;
        startup?.Cancel();
        foreach (Window window in OwnedWindows) if (window is ConnectionWindow) { window.Activate(); return; }
        var dialog = new ConnectionWindow(Connections) { Owner = this }; dialog.ShowDialog(); Render();
        Warning.Text = Connections.Warnings;
    }
    private void ExitClicked(object sender, RoutedEventArgs e) => Close();
    private void ConnectionsClicked(object sender, RoutedEventArgs e) => ShowConnections();
    private async void DisconnectClicked(object sender, RoutedEventArgs e)
    { await Sessions.DisconnectAsync(() => Task.FromResult(UnsavedDecision.Discard), () => Task.FromResult(true)); Render(); }
    private async void SessionChanged(object? sender, EventArgs e)
    {
        register?.Dispose(); register = null; Workspace.DataContext = null; Render();
        if (Application.Current is App app && Sessions.Active is not null)
        { register = app.ResolveBook<MainWindowViewModel>(); Workspace.DataContext = register; if (register is not null) await register.RefreshAsync(); }
    }
    private async void RefreshClicked(object sender, RoutedEventArgs e) { if (register is { } model) await model.RefreshAsync(); }
    private async void AccountChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (register is not { } model) return;
        if (e.NewValue is AccountNode node) await model.SelectAccountAsync(node.Account.AccountId);
        else if (e.NewValue is AccountGroup) await model.ClearAccountSelectionAsync();
    }
    private async void DetailAccountClicked(object sender, RoutedEventArgs e) { if (register is { } model && sender is System.Windows.Documents.Hyperlink { Tag: long id }) await model.SelectAccountAsync(id, model.Selected?.EntryId); }
    private async void AccountsClicked(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app && app.ResolveBook<AccountSetupViewModel>() is { } model)
        { new AccountSetupWindow(model) { Owner = this }.ShowDialog(); if (register is { } current) await current.RefreshAsync(); }
    }
    private void Render()
    {
        var current = Sessions.Active;
        BookTitle.Text = current is null ? "Connect to your book" : $"{Connections.ActiveProfileFriendlyName} — {current.Book.Name}";
        BookIdentity.Text = current is null ? "" : $"Book {current.Book.StableIdentifier:D} · {current.Book.CurrencyCode} · {current.Book.TimeZoneId}";
        Title = current is null ? "Fat Stacks.NET" : $"{current.Book.Name} — Fat Stacks.NET";
        AccountSetupButton.IsEnabled = current is not null; ReloadButton.IsEnabled = current is not null;
        DisconnectButton.IsEnabled = current is not null; Status.Text = current is null ? "Disconnected. Choose a connection." : "Book connected.";
        if (current is null) Warning.Text = "";
    }
    private void DismissWarningClicked(object sender, RoutedEventArgs e) => Warning.Text = "";
    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (shuttingDown || e.Cancel) return;
        e.Cancel = true;
        if (closingRequested) return;
        closingRequested = true;
        startup?.Cancel();
        // WPF must finish this Closing event before Close can be called again.
        Dispatcher.BeginInvoke(new Action(CloseAfterDisconnect));
    }
    private async void CloseAfterDisconnect()
    {
        var result = await Sessions.DisconnectAsync(() => Task.FromResult(UnsavedDecision.Discard), () => Task.FromResult(true));
        if (!result.IsSuccess)
        {
            closingRequested = false;
            Status.Text = result.Message;
            return;
        }
        shuttingDown = true;
        Close();
    }
}
