namespace FatStacks.Infrastructure.Persistence.Entities;

public sealed class CurrencyEntity
{
    public byte CurrencyId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public byte FractionalDigits { get; set; }
}
