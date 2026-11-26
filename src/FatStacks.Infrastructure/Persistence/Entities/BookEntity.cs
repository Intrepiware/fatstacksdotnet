namespace FatStacks.Infrastructure.Persistence.Entities;

public sealed class BookEntity
{
    public long BookId
    {
        get; set;
    }
    public Guid StableIdentifier
    {
        get; set;
    }
    public string Name { get; set; } = "";
    public byte CurrencyId
    {
        get; set;
    }
    public string TimeZoneId { get; set; } = "";
    public bool CurrencyLocked
    {
        get; set;
    }
    public DateTime CreateDate
    {
        get; set;
    }
    public DateTime UpdateDate
    {
        get; set;
    }
}
