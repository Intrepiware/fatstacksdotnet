using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using FatStacks.Core.Connections;
using FatStacks.Core.Calendar;
using FatStacks.Core.Register;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using FatStacks.Desktop.ViewModels;
using FatStacks.Desktop.Views;
namespace FatStacks.Desktop;

public partial class MainWindow : Window
{
    public required ConnectionViewModel Connections
    {
        private get; init;
    }
    public required StartupViewModel Startup
    {
        private get; init;
    }
    public required BookSessionCoordinator Sessions
    {
        private get; init;
    }
    private MainWindowViewModel? register;
    private CancellationTokenSource? startup;
    private bool closingRequested;
    private bool shuttingDown;
    private bool journalActionBusy;
    public MainWindow()
    {
        InitializeComponent();
        Loaded += LoadedAsync;
        Closed += (_, _) => { if (Sessions is not null) Sessions.SessionChanged -= SessionChanged; };
        CommandBindings.Add(new(ApplicationCommands.Open, (_, _) => ShowConnections()));
        PreviewKeyDown += MainWindowPreviewKeyDown;
        InputBindings.Add(new KeyBinding(ApplicationCommands.Open, new KeyGesture(Key.O, ModifierKeys.Control)));
    }
    private async void MainWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.None && PostingRegister.IsKeyboardFocusWithin && !IsTyping())
        {
            if (e.Key is Key.Up or Key.Down && PostingRegister.Items.Count > 0)
            {
                e.Handled = true;
                var next = Math.Clamp(PostingRegister.SelectedIndex + (e.Key == Key.Up ? -1 : 1), 0, PostingRegister.Items.Count - 1);
                var nextRow = (RegisterRow)PostingRegister.Items[next];
                PostingRegister.SetCurrentValue(Selector.SelectedItemProperty, nextRow);
                FocusPosting(nextRow);
                return;
            }
            if (e.Key is Key.R or Key.C or Key.U && PostingRegister.SelectedItem is RegisterRow row && register is { } model)
            {
                e.Handled = true;
                byte target = e.Key switch
                {
                    Key.R when row.Status != "Reconciling" => 1,
                    Key.C when row.Status != "Cleared" => 2,
                    _ => 0
                };
                await ChangeStatusAsync(model, row, target);
                return;
            }
        }
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            RegisterSearch.Focus();
        }
        else if (Keyboard.Modifiers == ModifierKeys.None && e.Key is Key.N or Key.E or Key.Delete && !IsTyping())
        {
            e.Handled = true;
            if (e.Key == Key.Delete)
                await DeleteJournalAsync();
            else
                await ShowJournalAsync(e.Key == Key.E);
        }
        else if (e.Key == Key.F5)
        {
            e.Handled = true;
            if (register is { } model)
                await model.RefreshAsync();
        }
    }
    private static bool IsTyping() => Keyboard.FocusedElement is TextBoxBase or PasswordBox or ComboBox;
    private async Task ShowJournalAsync(bool edit)
    {
        if (journalActionBusy || register is not { IsCurrent: true } current || Application.Current is not App app)
            return;
        journalActionBusy = true;
        try
        {
            var entry = edit ? await current.LoadSelectedEntryAsync() : null;
            if (edit && entry is null || !current.IsCurrent)
                return;
            if (app.ResolveBook<JournalEditorViewModel>() is not { } editor)
                return;
            editor.Initialize(entry, current.AccountRecords, current.Currency, new BookCalendar(Sessions.Active!.Book.TimeZoneId).Today, current.AccountId);
            new JournalEditorWindow(editor) { Owner = this }.ShowDialog();
            if (editor.LastSaved is { } saved)
                await current.JournalSavedAsync(saved);
        }
        finally { journalActionBusy = false; }
    }
    private async Task DeleteJournalAsync()
    {
        if (journalActionBusy || register is not { IsCurrent: true } current)
            return;
        journalActionBusy = true;
        try
        {
            var entry = await current.LoadSelectedEntryAsync();
            if (entry is null || !current.IsCurrent)
                return;
            new DeleteEntryWindow(entry, current.AccountRecords, current.DeleteEntryAsync) { Owner = this }.ShowDialog();
        }
        finally { journalActionBusy = false; }
    }
    private async void NewJournalClicked(object sender, RoutedEventArgs e) => await ShowJournalAsync(false);
    private void DismissSaveNoticeClicked(object sender, RoutedEventArgs e) => register?.DismissSaveNotice();
    private async void EditJournalClicked(object sender, RoutedEventArgs e) => await ShowJournalAsync(true);
    private async void DeleteJournalClicked(object sender, RoutedEventArgs e) => await DeleteJournalAsync();
    private void StatusFocused(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is Button { DataContext: RegisterRow row })
            PostingRegister.SetCurrentValue(Selector.SelectedItemProperty, row);
    }

    private async void RegisterStatusClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: RegisterRow row } && register is { } model)
        {
            PostingRegister.SetCurrentValue(Selector.SelectedItemProperty, row);
            byte target = row.Status switch
            {
                "Uncleared" => 1,
                "Reconciling" => 2,
                _ => 0
            };
            await ChangeStatusAsync(model, row, target);
        }
    }

    private async Task ChangeStatusAsync(MainWindowViewModel model, RegisterRow row, byte target)
    {
        await model.ReconcileAsync(row, target);
        if (!IsActive || model.Selected?.LineId != row.LineId ||
            Keyboard.FocusedElement is not null && !PostingRegister.IsKeyboardFocusWithin)
            return;
        FocusPosting(model.Selected);
    }

    private void FocusPosting(RegisterRow row)
    {
        PostingRegister.ScrollIntoView(row);
        PostingRegister.UpdateLayout();
        if (PostingRegister.ItemContainerGenerator.ContainerFromItem(row) is DataGridRow container)
        {
            if (RegisterStatusColumn.GetCellContent(container) is ContentPresenter presenter)
            {
                presenter.ApplyTemplate();
                if (presenter.ContentTemplate?.FindName("LineStatusButton", presenter) is Button button)
                {
                    button.Focus();
                    return;
                }
            }
            container.Focus();
        }
    }

    private async void RegisterDoubleClicked(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && ItemsControl.ContainerFromElement((ItemsControl)sender, source) is DataGridRow row &&
            RegisterStatusColumn.GetCellContent(row)?.IsMouseOver != true)
            await ShowJournalAsync(true);
    }
    private async void LoadedAsync(object sender, RoutedEventArgs e)
    {
        Sessions.SessionChanged += SessionChanged;
        using var cancellation = new CancellationTokenSource();
        startup = cancellation;
        try
        {
            await Connections.LoadAsync();
            if (closingRequested)
                return;
            Status.Text = "Opening the last successful profile… Use Connections to cancel.";
            var result = await Startup.OpenLastAsync(cancellation.Token);
            if (closingRequested)
                return;
            await Connections.LoadAsync();
            if (closingRequested)
                return;
            Render();
            if (result is not null)
            {
                Status.Text = result.IsSuccess ? "Book opened." : result.Message;
                Warning.Text = string.Join(Environment.NewLine, result.Warnings ?? []);
            }
            if (Sessions.Active is null)
                ShowConnections();
        }
        finally { startup = null; }
    }
    private void ShowConnections()
    {
        if (closingRequested)
            return;
        startup?.Cancel();
        foreach (Window window in OwnedWindows)
            if (window is ConnectionWindow)
            {
                window.Activate();
                return;
            }
        var dialog = new ConnectionWindow(Connections) { Owner = this };
        dialog.ShowDialog();
        Render();
        Warning.Text = Connections.Warnings;
    }
    private void ExitClicked(object sender, RoutedEventArgs e) => Close();
    private void ConnectionsClicked(object sender, RoutedEventArgs e) => ShowConnections();
    private async void DisconnectClicked(object sender, RoutedEventArgs e)
    {
        await Sessions.DisconnectAsync(() => Task.FromResult(UnsavedDecision.Discard), () => Task.FromResult(true));
        Render();
    }
    private async void SessionChanged(object? sender, EventArgs e)
    {
        register?.Dispose();
        register = null;
        Workspace.DataContext = null;
        Render();
        if (Application.Current is App app && Sessions.Active is not null)
        {
            register = app.ResolveBook<MainWindowViewModel>();
            Workspace.DataContext = register;
            if (register is not null)
                await register.RefreshAsync();
        }
    }
    private async void RefreshClicked(object sender, RoutedEventArgs e)
    {
        if (register is { } model)
            await model.RefreshAsync();
    }
    private async void AccountChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (register is { } model)
            await model.SelectTreeItemAsync(e.NewValue);
    }
    private void RegisterSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, sender) && sender is System.Windows.Controls.DataGrid grid)
            register?.SelectRegisterRow(grid.SelectedItem as FatStacks.Core.Register.RegisterRow);
    }
    private async void DetailAccountClicked(object sender, RoutedEventArgs e)
    {
        if (register is { } model && sender is System.Windows.Documents.Hyperlink { Tag: long id })
            await model.SelectAccountAsync(id, model.Selected?.EntryId);
    }
    private async void AccountsClicked(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app && app.ResolveBook<AccountSetupViewModel>() is { } model)
        {
            new AccountSetupWindow(model) { Owner = this }.ShowDialog();
            if (register is { } current)
                await current.RefreshAsync();
        }
    }
    private void Render()
    {
        var current = Sessions.Active;
        BookTitle.Text = current is null ? "Connect to your book" : $"{Connections.ActiveProfileFriendlyName} — {current.Book.Name}";
        BookIdentity.Text = current is null ? "" : $"Book {current.Book.StableIdentifier:D} · {current.Book.CurrencyCode} · {current.Book.TimeZoneId}";
        Title = current is null ? "Fat Stacks.NET" : $"{current.Book.Name} — Fat Stacks.NET";
        NewJournalButton.IsEnabled = current is not null;
        EditJournalButton.IsEnabled = current is not null;
        DeleteJournalButton.IsEnabled = current is not null;
        AccountSetupButton.IsEnabled = current is not null;
        ReloadButton.IsEnabled = current is not null;
        DisconnectButton.IsEnabled = current is not null;
        Status.Text = current is null ? "Disconnected. Choose a connection." : "Book connected.";
        if (current is null)
            Warning.Text = "";
    }
    private void DismissWarningClicked(object sender, RoutedEventArgs e) => Warning.Text = "";
    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (shuttingDown || e.Cancel)
            return;
        e.Cancel = true;
        if (closingRequested)
            return;
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
