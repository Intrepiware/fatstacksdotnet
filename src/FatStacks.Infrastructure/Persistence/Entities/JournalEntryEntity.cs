namespace FatStacks.Infrastructure.Persistence.Entities;

public sealed class JournalEntryEntity
{
    public long JournalEntryId
    {
        get; set;
    }
    public long BookId
    {
        get; set;
    }
    public DateOnly AccountingDate
    {
        get; set;
    }
    public DateOnly? BankPostingDate
    {
        get; set;
    }
    public string? Payee
    {
        get; set;
    }
    public string Description { get; set; } = "";
    public byte EntryKind
    {
        get; set;
    }
    public byte[] Version { get; set; } = [];
    public DateTime CreateDate
    {
        get; set;
    }
    public DateTime UpdateDate
    {
        get; set;
    }
}
