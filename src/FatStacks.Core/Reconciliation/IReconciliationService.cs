using FatStacks.Core.Contracts;

namespace FatStacks.Core.Reconciliation;

public sealed record ReconciliationRequest(long EntryId, long LineId, byte[] EntryVersion, byte[] LineVersion, byte Status);
public sealed record ReconciliationResult(byte[] EntryVersion, byte[] LineVersion, byte Status);

public interface IReconciliationService
{
    Task<OperationResult<ReconciliationResult>> SetAsync(ReconciliationRequest request);
}
