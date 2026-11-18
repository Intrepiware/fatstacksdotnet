namespace FatStacks.Infrastructure.Persistence.Entities;

public sealed class JournalEntryLineEntity
{
    public long JournalEntryLineId { get; set; }
    public long JournalEntryId { get; set; }
    public long AccountId { get; set; }
    public int LineOrder { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public byte ReconciliationStatus { get; set; }
    public long? SecurityId { get; set; }
    public decimal? Quantity { get; set; }
    public byte[] Version { get; set; } = [];
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }
}
