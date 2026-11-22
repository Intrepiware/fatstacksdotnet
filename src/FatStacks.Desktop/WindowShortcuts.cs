using System.Windows;
using System.Windows.Input;
namespace FatStacks.Desktop;

public static class WindowShortcuts
{
    private static int registered;

    public static void Register()
    {
        if (Interlocked.Exchange(ref registered, 1) != 0) return;
        var shortcut = new KeyBinding(ApplicationCommands.Close, Key.W, ModifierKeys.Control);
        shortcut.Freeze();
        CommandManager.RegisterClassInputBinding(typeof(Window), shortcut);
        CommandManager.RegisterClassCommandBinding(typeof(Window), new CommandBinding(
            ApplicationCommands.Close,
            (sender, e) =>
            {
                e.Handled = true;
                ((Window)sender).Close();
            },
            (_, e) => { e.CanExecute = true; e.Handled = true; }));
    }
}
