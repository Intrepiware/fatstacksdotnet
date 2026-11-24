using FatStacks.Core.Journal;
using FatStacks.Core.Register;
using FatStacks.Desktop.ViewModels;
using FatStacks.WindowsChecks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.Tests.Journal;

[TestClass]
public sealed class JournalResetNoticeTests
{
    [TestMethod]
    [DataRow(false,0)]
    [DataRow(false,1)]
    [DataRow(true,1)]
    [DataRow(false,2)]
    [DataRow(true,2)]
    public async Task EditorIdentifiesResetLinesByVisibleLabels(bool continueEntry,int resetCount)
    {
        var service=new JournalEditorViewModelTests.FakeJournal { ResetLineIds=resetCount==0 ? [] : resetCount==1 ? [101] : [101,202] };
        var model=JournalEditorViewModelTests.Editor(service);
        var accounts=JournalEditorViewModelTests.Accounts.Select(x => x.AccountId==1 ? x with { Name="Checking",ParentAccountId=900 } : x)
            .Append(new FatStacks.Core.Accounts.AccountRecord(900,1,null,"Assets",1,1,new(2000,1,1),null)).ToArray();
        model.Initialize(Entry(),accounts,"USD",new(2026,11,21));
        var pending=model.SaveAsync(continueEntry); service.Complete(FatStacks.Core.Contracts.Outcome.Success); Assert.IsTrue(await pending);
        if(resetCount==0) Assert.AreEqual("Entry saved.",model.Status);
        else
        {
            StringAssert.Contains(model.Status,"Line 1 (Assets:Checking)");
            if(resetCount==2) StringAssert.Contains(model.Status,"Line 2 (Equity)"); else Assert.IsFalse(model.Status.Contains("Equity",StringComparison.Ordinal));
            Assert.IsFalse(model.Status.Contains("101",StringComparison.Ordinal)); Assert.IsFalse(model.Status.Contains("202",StringComparison.Ordinal));
        }
    }
    [TestMethod]
    public async Task MainWindowUsesRefreshedAccountPathsInResetNotice()
    {
        var (sessions,session)=await RegisterJourneyTests.OpenAsync(); using var owned=session;
        using var model=new MainWindowViewModel { Session=session,Sessions=sessions,Accounts=new RegisterJourneyTests.Accounts(),Register=new Register() };
        await model.RefreshAsync(); await model.JournalSavedAsync(new(Entry(),[101,202]));
        StringAssert.Contains(model.SaveNotice,"Line 1 (Bank)"); StringAssert.Contains(model.SaveNotice,"Line 2 (Bank:Closed counter)");
        Assert.IsFalse(model.SaveNotice.Contains("101",StringComparison.Ordinal));
        Assert.IsTrue(model.HasSaveNotice);
        model.DismissSaveNotice(); Assert.IsFalse(model.HasSaveNotice); Assert.AreEqual("",model.SaveNotice);
    }
    [TestMethod]
    [DataRow((byte)1)]
    [DataRow((byte)2)]
    public void WarningTracksChangedReconciledLinesAndReverts(byte status)
    {
        var model=JournalEditorViewModelTests.Editor(new JournalEditorViewModelTests.FakeJournal());
        var entry=Entry();
        model.Initialize(entry with { Lines=entry.Lines.Select(line => line with { Status=status }).ToArray() },JournalEditorViewModelTests.Accounts,"USD",new(2026,11,21));
        Assert.IsFalse(model.HasResetWarning);
        model.Payee="Changed payee"; model.Description="Changed description";
        model.Lines[0].Debit="25.00";
        Assert.IsFalse(model.HasResetWarning,"Text changes and equivalent formatting must not trigger the warning.");
        model.Lines[0].Debit="26";
        StringAssert.Contains(model.ResetWarning,"Line 1 (Bank)");
        Assert.IsFalse(model.ResetWarning.Contains("Line 2",StringComparison.Ordinal));
        model.Lines[1].Credit="26";
        StringAssert.Contains(model.ResetWarning,"Line 2 (Equity)");
        model.Lines[0].Debit="25"; model.Lines[1].Credit="25";
        Assert.IsFalse(model.HasResetWarning);
        model.Lines[0].AccountText="Equity";
        Assert.IsTrue(model.HasResetWarning,"Changing the account must warn even when the amount is unchanged.");
        model.Lines[0].AccountText="Bank";
        Assert.IsFalse(model.HasResetWarning);
        model.Lines[0].Debit="invalid";
        Assert.IsFalse(model.HasResetWarning,"Invalid input has its own validation and cannot be saved.");
    }
    private static JournalRequest Entry() => new(1,10,new byte[8],new(2026,11,21),null,null,"Edited",0,
        [new(101,new byte[8],1,1,25,0,"USD",2),new(202,new byte[8],2,2,0,25,"USD",2)],[new(101,new byte[8]),new(202,new byte[8])]);
    private sealed class Register : IRegisterService
    {
        public Task<RegisterResult> ReadAsync(long id,DateOnly today,string search,CancellationToken ct=default) => Task.FromResult(new RegisterResult(0,[new(10,today,"Edited","Counter",25,null,0,"Uncleared")]));
        public Task<IReadOnlyList<EntryLine>> DetailsAsync(long id,CancellationToken ct=default) => Task.FromResult<IReadOnlyList<EntryLine>>([]);
        public Task<IReadOnlyDictionary<long,decimal>> BalancesAsync(DateOnly today,CancellationToken ct=default) => Task.FromResult<IReadOnlyDictionary<long,decimal>>(new Dictionary<long,decimal>());
    }
}
