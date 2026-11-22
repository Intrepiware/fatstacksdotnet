using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FatStacks.Desktop.ViewModels;
namespace FatStacks.Desktop.Views;

public partial class JournalEditorWindow : Window
{
    public JournalEditorViewModel Model { get; }
    public JournalEditorWindow(JournalEditorViewModel model)
    {
        Model = model; DataContext = model; InitializeComponent();
        Model.CloseRequested += CloseRequested;
        Closed += (_, _) => Model.CloseRequested -= CloseRequested;
        Loaded += (_, _) => FocusDateInput();
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
            var picker = new AccountComboBox(line.AccountPaths); picker.Bind(line);
            picker.SuggestionAccepted += (_, _) => picker.MoveToNextField();
            host.Content = picker;
        }
    }
    private async void SaveClicked(object sender, RoutedEventArgs e) { if (CanSave()) await Model.SaveAsync(false); }
    private async void ContinueClicked(object sender, RoutedEventArgs e) { if (CanSave() && await Model.SaveAsync(true)) FocusDateInput(); }
    private void FocusDateInput() => Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() =>
    {
        AccountingDateInput.ApplyTemplate();
        if (AccountingDateInput.Template.FindName("PART_TextBox", AccountingDateInput) is System.Windows.Controls.Primitives.DatePickerTextBox input)
        { input.Focus(); input.SelectAll(); }
        else AccountingDateInput.Focus();
    }));
    private void DebitLostFocus(object sender, KeyboardFocusChangedEventArgs e)
    { if (sender is FrameworkElement { DataContext: JournalLineViewModel line }) line.FormatAmount(false); }
    private void CreditLostFocus(object sender, KeyboardFocusChangedEventArgs e)
    { if (sender is FrameworkElement { DataContext: JournalLineViewModel line }) line.FormatAmount(true); }
    private bool CanSave()
    {
        foreach (var picker in new[] { AccountingDateInput, BankDateInput })
        {
            if (string.IsNullOrWhiteSpace(picker.Text))
            {
                if (ReferenceEquals(picker, AccountingDateInput)) Model.AccountingDate = null; else Model.BankPostingDate = null;
                continue;
            }
            if (!DateTime.TryParse(picker.Text, System.Globalization.CultureInfo.CurrentCulture, out var parsed))
            { Model.Errors.Clear(); Model.Errors.Add(new("Date", "Enter a valid date before saving.")); return false; }
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
            if (!string.IsNullOrWhiteSpace(picker.Text))
            {
                if (!DateTime.TryParse(picker.Text, System.Globalization.CultureInfo.CurrentCulture, out var parsed))
                { Model.Errors.Add(new(bankDate ? "BankPostingDate" : "AccountingDate", "Enter a valid date or press T for today.")); return; }
                current = parsed.Date;
            }
            if (bankDate) Model.BankPostingDate = current; else Model.AccountingDate = current;
        }
        if (Model.ApplyDateShortcut(e.Text[0], bankDate))
        {
            picker.SelectedDate = bankDate ? Model.BankPostingDate : Model.AccountingDate;
            // Also replace invalid or uncommitted text when the selected date did not change.
            picker.Text = picker.SelectedDate!.Value.ToString("d", System.Globalization.CultureInfo.CurrentCulture);
        }
    }
    private void DateInvalid(object? sender, DatePickerDateValidationErrorEventArgs e) { Model.Errors.Clear(); Model.Errors.Add(new("Date", "Enter a valid date before saving.")); e.ThrowException = false; }
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
