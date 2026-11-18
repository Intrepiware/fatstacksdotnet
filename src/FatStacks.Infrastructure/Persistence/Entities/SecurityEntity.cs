namespace FatStacks.Infrastructure.Persistence.Entities;

public sealed class SecurityEntity
{
    public long SecurityId { get; set; }
    public long BookId { get; set; }
    public string Symbol { get; set; } = "";
    public string Name { get; set; } = "";
    public byte QuantityPrecision { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }
}
