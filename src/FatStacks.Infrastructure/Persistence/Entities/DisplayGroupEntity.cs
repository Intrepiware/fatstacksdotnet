namespace FatStacks.Infrastructure.Persistence.Entities;

public sealed class DisplayGroupEntity
{
    public byte DisplayGroupId
    {
        get; set;
    }
    public string Name { get; set; } = "";
}
