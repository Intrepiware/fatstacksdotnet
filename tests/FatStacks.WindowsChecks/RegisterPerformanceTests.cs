using System.Diagnostics;
using System.Globalization;
using System.Windows.Automation;
using FatStacks.Core.Connections;
using FatStacks.Infrastructure.Configuration;
using FatStacks.SqlTests.Fixtures;
using FatStacks.WindowsChecks.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FatStacks.WindowsChecks;

[TestClass]
[DoNotParallelize]
public sealed class RegisterPerformanceTests
{
    public required TestContext TestContext { get; init; }

    [TestMethod]
    public async Task RepresentativeRegisterMeetsWindowsResponseTarget()
    {
        if (!OperatingSystem.IsWindows() || !Environment.UserInteractive ||
            Environment.GetEnvironmentVariable("FATSTACKS_RUN_REGISTER_BENCHMARK") != "1")
            Assert.Inconclusive("Set FATSTACKS_RUN_REGISTER_BENCHMARK=1 on the agreed interactive Windows machine.");

        await using var book = await BookDatabaseFixture.CreateAsync();
        await book.InitializeAsync();
        await RepresentativeBook.SeedAsync(book);
        using var desktop = new DesktopFixture();
        var store = new ProfileStore(desktop.ProfilePath, new WindowsSecretProtector());
        var profile = new ConnectionProfile(Guid.NewGuid(), "Register benchmark", book.Settings);
        Assert.IsTrue((await store.SaveAsync(new(1, [profile], profile.ProfileId))).IsSuccess);
        await desktop.StartAsync();

        const string bankA = "Benchmark Bank A";
        const string bankB = "Benchmark Bank B";
        var status = await desktop.WaitAsync("RegisterStatus");
        var title = await desktop.WaitAsync("RegisterAccountTitle");
        await WaitForRowsAsync(6000);
        var selection = new List<double>();
        var search = new List<double>();
        var refresh = new List<double>();

        for (var run = 0; run < 20; run++)
        {
            var next = run % 2 == 0 ? bankB : bankA;
            var expectedRows = next == bankB ? 4000 : 6000;
            var tree = await desktop.WaitAsync("AccountTree");
            var account = tree.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TreeItem),
                new PropertyCondition(AutomationElement.NameProperty, next)));
            Assert.IsNotNull(account, "The benchmark account must be selectable in the account tree.");
            var item = (SelectionItemPattern)account.GetCurrentPattern(SelectionItemPattern.Pattern);
            selection.Add(await MeasureAsync(() => item.Select(), () => WaitForRowsAsync(expectedRows)));
        }

        var searchBox = await desktop.WaitAsync("RegisterSearch");
        var searchValue = (ValuePattern)searchBox.GetCurrentPattern(ValuePattern.Pattern);
        for (var run = 0; run < 20; run++)
        {
            var term = run % 2 == 0 ? "Benchmark entry 6000" : "";
            search.Add(await MeasureAsync(() => searchValue.SetValue(term), () => WaitForRowsAsync(term.Length == 0 ? 6000 : 1)));
        }

        var reloadButton = await desktop.WaitAsync("RefreshRegister");
        var reload = (InvokePattern)reloadButton.GetCurrentPattern(InvokePattern.Pattern);
        var previousName = bankA;
        for (var run = 1; run <= 20; run++)
        {
            // Rename outside the timed interval to distinguish a new result from the old grid.
            var name = $"Benchmark Reload {run}";
            await book.ExecuteAsync($"UPDATE dbo.Account SET Name = N'{name}' WHERE Name = N'{previousName}';");
            refresh.Add(await MeasureAsync(reload.Invoke, async () =>
            {
                await WaitForNameAsync(title, name);
                await WaitForRowsAsync(6000);
            }));
            previousName = name;
        }

        var failures = new List<string>();
        Report("Account selection", selection);
        Report("Search", search);
        Report("Refresh", refresh);
        Assert.IsEmpty(failures, string.Join(", ", failures));

        Task WaitForRowsAsync(int rows) => WaitForNameAsync(
            status, $"{rows.ToString("N0", CultureInfo.CurrentCulture)} transactions shown");

        void Report(string operation, List<double> timings)
        {
            var withinTarget = timings.Count(milliseconds => milliseconds <= 2000);
            TestContext.WriteLine($"{operation} (ms): {string.Join(", ", timings.Select(value => value.ToString("F0", CultureInfo.InvariantCulture)))}");
            TestContext.WriteLine($"{operation} within two seconds: {withinTarget}/20");
            if (withinTarget < 19) failures.Add(operation);
        }
    }

    private static async Task WaitForNameAsync(AutomationElement control, string expected)
    {
        // Poll the same control so the measurement excludes desktop-wide UI Automation lookup.
        for (var attempt = 0; attempt < 150; attempt++)
        {
            if (string.Equals(control.Current.Name, expected, StringComparison.Ordinal))
                return;
            await Task.Delay(50);
        }

        Assert.Fail($"The register did not show '{expected}' before timeout.");
    }

    private static async Task<double> MeasureAsync(Action action, Func<Task> ready)
    {
        var timer = Stopwatch.StartNew();
        action();
        await ready();
        return timer.Elapsed.TotalMilliseconds;
    }
}
