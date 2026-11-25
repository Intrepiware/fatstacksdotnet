using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using FatStacks.Desktop.ViewModels;
namespace FatStacks.Desktop.Views;

public partial class JournalEditorWindow : Window
{
    private readonly Dictionary<DatePicker, string> rejectedDateText = [];
    public JournalEditorViewModel Model { get; }
    public JournalEditorWindow(JournalEditorViewModel model)
    {
        Model = model; DataContext = model; InitializeComponent();
        Model.CloseRequested += CloseRequested;
        Closed += (_, _) => Model.CloseRequested -= CloseRequested;
        Loaded += (_, _) => FocusDateInput();
        foreach (var picker in new[] { AccountingDateInput, BankDateInput })
        {
            picker.Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
            picker.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler(DateTextChanged));
        }
    }
    private void CloseRequested(object? sender, EventArgs e)
    {
        if (!Model.IsBusy) Close();
        else Dispatcher.BeginInvoke(new Action(Close));
    }
    protected override void OnClosing(CancelEventArgs e) { if (Model.IsBusy) e.Cancel = true; base.OnClosing(e); }
    private void AccountHostLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ContentControl { DataContext: JournalLineViewModel line } host && host.Content is null)
        {
            var picker = new AccountComboBox(line.AccountPaths) { Padding = new Thickness(6, 4, 6, 4) }; picker.Bind(line);
            picker.SuggestionAccepted += (_, _) => picker.MoveToNextField();
            host.Content = picker;
        }
    }
    private async void SaveClicked(object sender, RoutedEventArgs e) { if (CanSave()) await Model.SaveAsync(false); }
    private async void ContinueClicked(object sender, RoutedEventArgs e) { if (CanSave() && await Model.SaveAsync(true)) FocusDateInput(); }
    private void FocusDateInput() => Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
    {
        AccountingDateInput.ApplyTemplate();
        if (AccountingDateInput.Template.FindName("PART_TextBox", AccountingDateInput) is DatePickerTextBox input)
        { input.Focus(); input.SelectAll(); }
        else AccountingDateInput.Focus();
    }));
    private void DebitLostFocus(object sender, KeyboardFocusChangedEventArgs e)
    { if (sender is FrameworkElement { DataContext: JournalLineViewModel line }) line.FormatAmount(false); }
    private void CreditLostFocus(object sender, KeyboardFocusChangedEventArgs e)
    { if (sender is FrameworkElement { DataContext: JournalLineViewModel line }) line.FormatAmount(true); }
    private bool CanSave()
    {
        if (!Model.CanSave) return false;
        foreach (var picker in new[] { AccountingDateInput, BankDateInput })
        {
            // WPF may briefly display the previous valid date while rejected text is restored.
            if (rejectedDateText.ContainsKey(picker)) return false;
            if (string.IsNullOrWhiteSpace(picker.Text))
            {
                if (ReferenceEquals(picker, AccountingDateInput)) Model.AccountingDate = null; else Model.BankPostingDate = null;
                continue;
            }
            if (!DateTime.TryParse(picker.Text, CultureInfo.CurrentCulture, out var parsed))
            { Model.SetDateError(ReferenceEquals(picker, BankDateInput), "Enter a valid date before saving."); return false; }
            if (ReferenceEquals(picker, AccountingDateInput)) Model.AccountingDate = parsed.Date; else Model.BankPostingDate = parsed.Date;
        }
        return true;
    }
    private void DateShortcutInput(object sender, TextCompositionEventArgs e)
    {
        if (e.Text.Length != 1 || e.Text[0] is not ('-' or '+' or 't' or 'T') ||
            (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows)) != 0 ||
            sender is not DatePicker picker || Model.IsBusy) return;
        e.Handled = true;
        var bankDate = ReferenceEquals(picker, BankDateInput);
        if (e.Text[0] is '-' or '+')
        {
            DateTime? current = null;
            var text = rejectedDateText.GetValueOrDefault(picker, picker.Text);
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (!DateTime.TryParse(text, CultureInfo.CurrentCulture, out var parsed))
                { Model.SetDateError(bankDate, "Enter a valid date or press T for today."); return; }
                current = parsed.Date;
            }
            if (bankDate) Model.BankPostingDate = current; else Model.AccountingDate = current;
        }
        if (Model.ApplyDateShortcut(e.Text[0], bankDate))
        {
            rejectedDateText.Remove(picker);
            picker.SetCurrentValue(DatePicker.SelectedDateProperty, bankDate ? Model.BankPostingDate : Model.AccountingDate);
            // Also replace invalid or uncommitted text when the selected date did not change.
            picker.Text = picker.SelectedDate!.Value.ToString("d", CultureInfo.CurrentCulture);
        }
    }
    private void DateInvalid(object? sender, DatePickerDateValidationErrorEventArgs e)
    {
        e.ThrowException = false;
        if (sender is not DatePicker picker) return;

        rejectedDateText[picker] = e.Text;
        Model.SetDateError(ReferenceEquals(picker, BankDateInput), "Enter a valid date or press T for today.");
        // DatePicker reverts invalid text on commit. Restore it in the text box without
        // assigning DatePicker.Text, which would immediately attempt another commit.
        Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
        {
            if (rejectedDateText.GetValueOrDefault(picker) != e.Text) return;
            try
            {
                if (picker.Template?.FindName("PART_TextBox", picker) is DatePickerTextBox input)
                    input.Text = e.Text;
            }
            finally { rejectedDateText.Remove(picker); }
        }));
    }
    private void DateTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not DatePicker picker || e.OriginalSource is not TextBox input ||
            !picker.IsLoaded || !input.IsKeyboardFocused || rejectedDateText.ContainsKey(picker)) return;

        // Let DatePicker commit typed text on Enter or focus loss. Updating SelectedDate
        // during typing replaces the draft with a formatted date and moves the caret.
        var bank = ReferenceEquals(picker, BankDateInput);
        var error = bank ? Model.BankPostingDateError : Model.AccountingDateError;
        if (error.Length == 0) return;
        if ((bank && string.IsNullOrWhiteSpace(input.Text)) ||
            DateTime.TryParse(input.Text, CultureInfo.CurrentCulture, out _))
            Model.SetDateError(bank, "");
    }
    private void CancelClicked(object sender, RoutedEventArgs e) => Model.Cancel();
    private void AddClicked(object sender, RoutedEventArgs e) { Model.AddLine(); FocusLastAccount(); }
    private void FocusLastAccount() => FocusAccount(Model.Lines.Count - 1);
    private void FocusAccount(int index) => Dispatcher.BeginInvoke(new Action(() =>
    {
        PostingRows.UpdateLayout();
        static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i); yield return child;
                foreach (var nested in Descendants(child)) yield return nested;
            }
        }
        Descendants(PostingRows).OfType<AccountComboBox>().ElementAtOrDefault(index)?.Focus();
    }));
    private void RemoveClicked(object sender, RoutedEventArgs e) { if (sender is FrameworkElement { DataContext: JournalLineViewModel line }) RemoveLine(line); }
    private void RemoveLine(JournalLineViewModel line)
    {
        if (!Model.CanRemoveLine) return;
        var index = Model.Lines.IndexOf(line);
        Model.RemoveLine(line);
        FocusAccount(Math.Min(index, Model.Lines.Count - 1));
    }
    private void CreditKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.None && sender is FrameworkElement { DataContext: JournalLineViewModel line } && ReferenceEquals(Model.Lines.LastOrDefault(), line) && Model.IsUnbalanced)
        {
            e.Handled = true; Model.AddLine(); FocusLastAccount();
        }
    }
    private async void DialogKeyDown(object sender, KeyEventArgs e)
    {
        // DatePicker may retain invalid text without changing SelectedDate. Force validation before Save.
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control) { e.Handled = true; Keyboard.ClearFocus(); if (CanSave()) await Model.SaveAsync(false); }
        else if (e.Key == Key.Delete && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && Keyboard.FocusedElement is FrameworkElement { DataContext: JournalLineViewModel line })
        { e.Handled = true; RemoveLine(line); }
        else if (e.Key == Key.Escape && !e.Handled) { e.Handled = true; Model.Cancel(); }
    }
}
