using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace FatStacks.Desktop.Views;

/// <summary>Account picker whose suggestion selection never overwrites an in-progress search.</summary>
internal sealed class AccountComboBox : UserControl
{
    private string[] accounts;
    public static readonly DependencyProperty AccountPathsProperty = DependencyProperty.Register(
        nameof(AccountPaths), typeof(IEnumerable<string>), typeof(AccountComboBox),
        new PropertyMetadata(null, (sender, args) => ((AccountComboBox)sender).UpdateAccounts((IEnumerable<string>?)args.NewValue)));
    public IEnumerable<string>? AccountPaths { get => (IEnumerable<string>?)GetValue(AccountPathsProperty); set => SetValue(AccountPathsProperty, value); }
    private void UpdateAccounts(IEnumerable<string>? paths)
    {
        accounts = (paths ?? []).Order(StringComparer.Ordinal).ToArray();
        if (popup.IsOpen) ShowSuggestions(showAll: editor.Text.Length == 0);
    }
    private readonly TextBox editor;
    private readonly ListBox suggestions;
    private readonly Popup popup;
    private bool committing;
    private Window? owner;
    public string Text { get => editor.Text; set => editor.SetCurrentValue(TextBox.TextProperty, value); }
    public event EventHandler? SuggestionAccepted;

    public AccountComboBox(IEnumerable<string> accounts)
    {
        this.accounts = accounts.Order(StringComparer.Ordinal).ToArray();
        Focusable = false;
        IsTabStop = false;
        var layout = new Grid();
        layout.ColumnDefinitions.Add(new ColumnDefinition());
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
        editor = new TextBox { VerticalContentAlignment = VerticalAlignment.Center };
        editor.SetBinding(Control.PaddingProperty, new Binding(nameof(Padding)) { Source = this });
        var toggle = new Button { Content = "▾", Padding = new Thickness(0), Margin = new Thickness(0), Focusable = false, IsTabStop = false, ToolTip = "Show accounts" };
        System.Windows.Automation.AutomationProperties.SetName(toggle, "Show account suggestions");
        Grid.SetColumn(toggle, 1);
        layout.Children.Add(editor);
        layout.Children.Add(toggle);
        suggestions = new ListBox { MaxHeight = 280, Focusable = false, IsTabStop = false };
        suggestions.SetValue(ScrollViewer.CanContentScrollProperty, true);
        suggestions.SetValue(VirtualizingPanel.IsVirtualizingProperty, true);
        suggestions.SetValue(VirtualizingPanel.VirtualizationModeProperty, VirtualizationMode.Recycling);
        popup = new Popup { Child = suggestions, PlacementTarget = this, Placement = PlacementMode.Bottom, StaysOpen = true, PopupAnimation = PopupAnimation.None };
        layout.Children.Add(popup);
        Content = layout;
        editor.TextChanged += (_, _) =>
        {
            if (!committing && editor.IsKeyboardFocused) ShowSuggestions(showAll: false);
        };
        editor.GotKeyboardFocus += (_, _) => editor.SelectAll();
        editor.PreviewMouseLeftButtonDown += (_, e) =>
        {
            if (editor.IsKeyboardFocused) return;
            e.Handled = true;
            editor.Focus();
        };
        editor.LostKeyboardFocus += (_, _) => popup.IsOpen = false;
        toggle.Click += (_, _) =>
        {
            bool open = popup.IsOpen;
            editor.Focus();
            if (open) popup.IsOpen = false; else ShowSuggestions(showAll: true);
        };
        suggestions.PreviewMouseLeftButtonDown += (_, e) =>
        {
            if (e.OriginalSource is DependencyObject source && ItemsControl.ContainerFromElement(suggestions, source) is ListBoxItem item)
            {
                suggestions.SelectedItem = item.Content;
                CommitSuggestion();
                SuggestionAccepted?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        };
        Loaded += (_, _) =>
        {
            owner = Window.GetWindow(this);
            if (owner is not null)
            {
                owner.PreviewMouseDown += OutsideClicked;
                owner.Deactivated += OwnerDeactivated;
            }
            System.Windows.Automation.AutomationProperties.SetName(editor, System.Windows.Automation.AutomationProperties.GetName(this));
        };
        Unloaded += (_, _) =>
        {
            popup.IsOpen = false;
            if (owner is not null)
            {
                owner.PreviewMouseDown -= OutsideClicked;
                owner.Deactivated -= OwnerDeactivated;
            }
            owner = null;
        };
    }

    public void Bind(FatStacks.Desktop.ViewModels.JournalLineViewModel line)
    {
        SetBinding(AccountPathsProperty, new Binding(nameof(line.AccountPaths)) { Source = line, Mode = BindingMode.OneWay });
        editor.SetBinding(TextBox.TextProperty, new Binding(nameof(line.AccountText)) { Source = line, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
        System.Windows.Automation.AutomationProperties.SetAutomationId(editor, line.AccountControlId);
        System.Windows.Automation.AutomationProperties.SetName(this, "Posting account");
    }
    public new bool Focus() => editor.Focus();
    public bool MoveToNextField() => editor.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

    private void OutsideClicked(object sender, MouseButtonEventArgs e)
    {
        if (!IsMouseOver && !suggestions.IsMouseOver) popup.IsOpen = false;
    }
    private void OwnerDeactivated(object? sender, EventArgs e) => popup.IsOpen = false;

    private void ShowSuggestions(bool showAll)
    {
        // Filtering is synchronous and does not change Text, caret, or account selection.
        string query = editor.Text;
        var matches = showAll ? accounts : accounts.Where(a => a.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
        suggestions.ItemsSource = matches;
        suggestions.SelectedIndex = matches.Length == 0 ? -1 : Math.Max(0, Array.IndexOf(matches, query));
        suggestions.Width = Math.Max(ActualWidth, 240);
        popup.IsOpen = matches.Length > 0 && (showAll || query.Length > 0);
    }

    private void CommitSuggestion()
    {
        if (suggestions.SelectedItem is not string account) return;
        committing = true;
        try
        {
            editor.SetCurrentValue(TextBox.TextProperty, account);
            editor.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            editor.CaretIndex = account.Length;
            popup.IsOpen = false;
        }
        finally { committing = false; }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.None)
        {
            if (e.Key is Key.Down or Key.Up)
            {
                if (!popup.IsOpen) ShowSuggestions(showAll: editor.Text.Length == 0);
                else if (suggestions.Items.Count > 0)
                    suggestions.SelectedIndex = Math.Clamp(suggestions.SelectedIndex + (e.Key == Key.Down ? 1 : -1), 0, suggestions.Items.Count - 1);
                if (suggestions.SelectedItem is { } item) suggestions.ScrollIntoView(item);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && popup.IsOpen && suggestions.SelectedItem is not null)
            {
                CommitSuggestion();
                e.Handled = true;
                SuggestionAccepted?.Invoke(this, EventArgs.Empty);
            }
            else if (e.Key == Key.Tab && popup.IsOpen)
            {
                // Accept the default/highlighted match, then let normal Tab navigation reach Amount.
                CommitSuggestion();
            }
            else if (e.Key == Key.Escape && popup.IsOpen)
            {
                popup.IsOpen = false;
                e.Handled = true;
            }
        }
        base.OnPreviewKeyDown(e);
    }
}
