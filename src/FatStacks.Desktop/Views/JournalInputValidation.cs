using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace FatStacks.Desktop.Views;

// Present typed view-model errors without interpreting their English messages.
public static class JournalInputValidation
{
    public static readonly DependencyProperty ErrorProperty = DependencyProperty.RegisterAttached(
        "Error", typeof(string), typeof(JournalInputValidation), new PropertyMetadata("", ErrorChanged));
    private static readonly DependencyProperty OriginalToolTipProperty = DependencyProperty.RegisterAttached(
        "OriginalToolTip", typeof(object), typeof(JournalInputValidation));
    public static string GetError(DependencyObject element) => (string)element.GetValue(ErrorProperty);
    public static void SetError(DependencyObject element, string value) => element.SetValue(ErrorProperty, value);

    private static void ErrorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not Control control) return;
        if (string.IsNullOrEmpty((string?)args.OldValue) && !string.IsNullOrEmpty((string?)args.NewValue))
            control.SetValue(OriginalToolTipProperty, control.ToolTip);
        Apply(control);
        if (!control.IsLoaded)
        {
            control.Loaded -= Loaded;
            control.Loaded += Loaded;
        }
    }
    private static void Loaded(object sender, RoutedEventArgs args)
    {
        var control = (Control)sender;
        control.Loaded -= Loaded;
        Apply(control);
    }
    private static void Apply(Control control)
    {
        var message = GetError(control);
        var invalid = message.Length > 0;
        AutomationProperties.SetItemStatus(control, invalid ? "Invalid" : "");
        AutomationProperties.SetHelpText(control, message);
        if (control is DatePicker picker)
        {
            picker.ApplyTemplate();
            if (picker.Template.FindName("PART_TextBox", picker) is TextBox input)
            {
                AutomationProperties.SetItemStatus(input, invalid ? "Invalid" : "");
                AutomationProperties.SetHelpText(input, message);
            }
        }
        if (invalid)
        {
            control.SetCurrentValue(Control.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18)));
            control.SetCurrentValue(Control.BorderThicknessProperty, new Thickness(2));
            control.SetCurrentValue(FrameworkElement.ToolTipProperty, message);
        }
        else
        {
            control.ClearValue(Control.BorderBrushProperty);
            control.ClearValue(Control.BorderThicknessProperty);
            if (control.ReadLocalValue(OriginalToolTipProperty) != DependencyProperty.UnsetValue)
                control.SetCurrentValue(FrameworkElement.ToolTipProperty, control.GetValue(OriginalToolTipProperty));
        }
        // Preserve the amount text edge when the error border becomes thicker.
        if (control is TextBox) control.SetCurrentValue(Control.PaddingProperty, new Thickness(invalid ? 5 : 6, invalid ? 3 : 4, invalid ? 5 : 6, invalid ? 3 : 4));
        var binding = control.GetBindingExpression(control is DatePicker ? DatePicker.SelectedDateProperty : TextBox.TextProperty);
        if (binding is null) return;
        Validation.SetErrorTemplate(control, null);
        if (invalid) Validation.MarkInvalid(binding, new ValidationError(new ExceptionValidationRule(), binding, message, null));
        else Validation.ClearInvalid(binding);
    }
}
