using System.Diagnostics;
using System.IO;
using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.WindowsChecks.Fixtures;

public sealed class DesktopFixture : IDisposable
{
    private Process? process;
    public string ProfilePath { get; } = Path.Combine(Path.GetTempPath(), "FatStacks_UI_" + Guid.NewGuid().ToString("N"), "profiles.json");
    public async Task StartAsync()
    {
        if (!OperatingSystem.IsWindows() || !Environment.UserInteractive) Assert.Inconclusive("Interactive Windows desktop required.");
        var root = SqlTests.Fixtures.BookDatabaseFixture.RepositoryRoot();
        var dll = Environment.GetEnvironmentVariable("FATSTACKS_DESKTOP_DLL") ?? Path.Combine(root, "src", "FatStacks.Desktop", "bin", "Debug", "net10.0-windows", "FatStacks.Desktop.dll");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false }; start.ArgumentList.Add(dll); start.Environment["FATSTACKS_PROFILE_PATH"] = ProfilePath;
        process = Process.Start(start) ?? throw new InvalidOperationException("Desktop process did not start.");
        await WaitAsync("MainWindow");
    }
    public AutomationElement? Find(string id)
    {
        if (process is null || process.HasExited) return null;
        return AutomationElement.RootElement.FindFirst(TreeScope.Descendants, new AndCondition(
            new PropertyCondition(AutomationElement.ProcessIdProperty, process.Id), new PropertyCondition(AutomationElement.AutomationIdProperty, id)));
    }
    public async Task<AutomationElement> WaitAsync(string id)
    {
        for (var i = 0; i < 150; i++) { var result = Find(id); if (result is not null) return result; await Task.Delay(100); }
        Assert.Fail($"Desktop control {id} was not found before timeout."); return null!;
    }
    public async Task WaitTextAsync(string id, string expected)
    {
        for (var i = 0; i < 150; i++) { if (Find(id)?.Current.Name.Contains(expected, StringComparison.Ordinal) == true) return; await Task.Delay(100); }
        Assert.Fail($"Desktop control {id} did not show the expected state.");
    }
    public async Task InvokeAsync(string id)
    {
        var control = await WaitAsync(id);
        for (var i = 0; i < 100 && !control.Current.IsEnabled; i++) await Task.Delay(100);
        Assert.IsTrue(control.Current.IsEnabled, $"Control {id} stayed disabled.");
        ((InvokePattern)control.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
    }
    public async Task SelectProfileAsync(string name)
    {
        var list = await WaitAsync("Profiles");
        var item = list.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, name));
        Assert.IsNotNull(item, "Saved profile not found."); ((SelectionItemPattern)item.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
    }
    public async Task SetAsync(string id, string value) => ((ValuePattern)(await WaitAsync(id)).GetCurrentPattern(ValuePattern.Pattern)).SetValue(value);
    public void Stop()
    {
        if (process is not null) { if (!process.HasExited) process.Kill(true); process.WaitForExit(); process.Dispose(); process = null; }
    }
    public void Dispose() { Stop(); var folder = Path.GetDirectoryName(ProfilePath)!; if (Directory.Exists(folder)) Directory.Delete(folder, true); }
}
