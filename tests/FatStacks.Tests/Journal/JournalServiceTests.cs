using FatStacks.Core.Accounts;
using FatStacks.Core.Connections;
using FatStacks.Core.Contracts;
using FatStacks.Core.Journal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace FatStacks.Tests.Journal;

[TestClass]
public sealed class JournalServiceTests
{
    [TestMethod]
    public async Task InvalidFinalValuesReturnTypedLineErrorsWithoutCallingStorage()
    {
        var store = new NoWriteStore();
        using var session = new BookSession(new(Guid.NewGuid(), "Synthetic", new()
        {
            ConnectionString = "unused"
        }), new(Guid.NewGuid(), "Book", "USD", "UTC"));
        var service = new JournalService { Store = store, Accounts = new Accounts(), Session = session };
        var draft = new JournalRequest(1, null, null, new(2026, 11, 21), null, null, "", 0, [new(null, null, 1, 1, 100000000, 0, "USD"), new(null, null, 2, 2, 0, 100000000, "USD")], []);
        var result = await service.SaveAsync(draft);
        Assert.AreEqual(Outcome.ValidationFailure, result.Outcome);
        Assert.AreEqual(0, store.Calls);
        Assert.IsTrue(result.Errors!.Any(x => x.Field == "Debit" && x.LineOrder == 1));
    }
    private sealed class Accounts : IAccountService
    {
        public Task<IReadOnlyList<AccountRecord>> ListAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AccountRecord>>([new(1, 1, null, "Bank", 1, 1, new(2000, 1, 1), null), new(2, 1, null, "Equity", 9, 5, new(2000, 1, 1), null)]);
        public Task<OperationResult<AccountRecord>> SaveAsync(AccountRequest request, CancellationToken ct = default) => throw new NotSupportedException();
    }
    private sealed class NoWriteStore : IJournalStore
    {
        public int Calls
        {
            get; private set;
        }
        public Task<JournalRequest?> LoadAsync(long entryId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<OperationResult<JournalSaveResult>> SaveAsync(JournalRequest request, CancellationToken ct = default)
        {
            Calls++;
            throw new AssertFailedException("Invalid values must not reach SQL parameter conversion.");
        }
        public Task<OperationResult<bool>> DeleteAsync(DeleteJournalRequest request, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
