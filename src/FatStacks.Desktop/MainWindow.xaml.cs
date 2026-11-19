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
    private CancellationTokenSource? startup;
    private bool closingRequested;
    private bool shuttingDown;
    public MainWindow()
    {
        InitializeComponent(); Loaded += LoadedAsync; Closed += (_, _) => { if (Sessions is not null) Sessions.SessionChanged -= SessionChanged; };
        CommandBindings.Add(new(ApplicationCommands.Open, (_, _) => ShowConnections()));
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
    private void ConnectionsClicked(object sender, RoutedEventArgs e) => ShowConnections();
    private async void DisconnectClicked(object sender, RoutedEventArgs e)
    { await Sessions.DisconnectAsync(() => Task.FromResult(UnsavedDecision.Discard), () => Task.FromResult(true)); Render(); }
    private void SessionChanged(object? sender, EventArgs e) => Render();
    private void Render()
    {
        var current = Sessions.Active;
        BookTitle.Text = current is null ? "Connect to your book" : $"{Connections.ActiveProfileFriendlyName} — {current.Book.Name}";
        BookIdentity.Text = current is null ? "" : $"Book {current.Book.StableIdentifier:D} · {current.Book.CurrencyCode} · {current.Book.TimeZoneId}";
        DisconnectButton.IsEnabled = current is not null; Status.Text = current is null ? "Disconnected. Choose a connection." : "Book connected. Journal workflows are not yet implemented.";
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
