using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FatStacks.Desktop.ViewModels;
using FatStacks.Desktop.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.WindowsChecks;

[TestClass]
[DoNotParallelize]
public sealed class AccountSetupWindowTests
{
    [STATestMethod]
    public void AccountControlsCreateWithExplicitGroupAndRetainStableIdentityOnRename()
    {
        var (_, session) = RegisterJourneyTests.OpenAsync().GetAwaiter().GetResult();
        var accounts = new RegisterJourneyTests.Accounts();
        var model = new AccountSetupViewModel { Accounts = accounts, Session = session };
        var window = new AccountSetupWindow(model);
        try
        {
            window.Show(); Pump();
            System.Windows.Input.AccessKeyManager.ProcessKey(PresentationSource.FromVisual(window), "N", false);
            Find<TextBox>(window, "AccountName").Text = "Synthetic bank";
            Find<ComboBox>(window, "AccountGroup").SelectedIndex = 5;
            System.Windows.Input.AccessKeyManager.ProcessKey(PresentationSource.FromVisual(window), "S", false); Pump();
            Assert.AreEqual("Account saved.", model.Status); Assert.AreEqual((byte)5, accounts.Saved!.GroupId);
            Assert.IsFalse(Find<ComboBox>(window, "AccountType").IsEnabled);
            Assert.AreEqual("Assets", model.ClassName);
            Find<TextBox>(window, "AccountName").Text = "Renamed";
            Assert.AreEqual(3L, model.Selected!.AccountId);
            Assert.AreEqual("Renamed", model.Name);
        }
        finally { window.Close(); session.Dispose(); }
    }
    [STATestMethod]
    public void SearchAndTreeToggleUseTheSameAccountSelection()
    {
        var (_,session)=RegisterJourneyTests.OpenAsync().GetAwaiter().GetResult();
        var model=new AccountSetupViewModel {Accounts=new RegisterJourneyTests.Accounts(),Session=session};
        var window=new AccountSetupWindow(model);
        try
        {
            window.Show();Pump();
            Assert.IsTrue(Find<TreeView>(window,"AccountHierarchy").IsVisible);
            System.Windows.Input.AccessKeyManager.ProcessKey(PresentationSource.FromVisual(window),"A",false);Pump();
            System.Windows.Input.AccessKeyManager.ProcessKey(PresentationSource.FromVisual(window),"F",false);Pump();
            Assert.IsTrue(Find<TextBox>(window,"AccountSearch").IsKeyboardFocused);
            Find<TextBox>(window,"AccountSearch").Text="closed";Pump();
            var list=Find<ListBox>(window,"AccountList");Assert.AreEqual(1,list.Items.Count);list.SelectedIndex=0;Pump();
            model.Name="Draft";
            System.Windows.Input.AccessKeyManager.ProcessKey(PresentationSource.FromVisual(window),"T",false);Pump();
            var tree=Find<TreeView>(window,"AccountHierarchy");Assert.IsTrue(tree.IsVisible);Assert.AreEqual(1,tree.Items.Count);
            Assert.AreEqual(2L,((AccountBrowserItem)tree.SelectedItem).Account.AccountId);
            System.Windows.Input.AccessKeyManager.ProcessKey(PresentationSource.FromVisual(window),"A",false);Pump();
            Assert.IsTrue(list.IsVisible);Assert.AreEqual(2L,((AccountBrowserItem)list.SelectedItem).Account.AccountId);Assert.AreEqual("Draft",model.Name);
            System.Windows.Input.AccessKeyManager.ProcessKey(PresentationSource.FromVisual(window),"C",false);Pump();
            Assert.IsFalse(window.IsVisible);
        }
        finally {if(window.IsVisible)window.Close();session.Dispose();}
    }
    private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() => { }));
    private static T Find<T>(Window window, string id) where T : DependencyObject
    {
        return Descendants(window).OfType<T>().Single(x => System.Windows.Automation.AutomationProperties.GetAutomationId(x) == id);
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i); yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}
