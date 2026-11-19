namespace FatStacks.Infrastructure.Persistence.Entities;

public sealed class AccountTypeEntity
{
    public byte AccountTypeId { get; set; }
    public string Name { get; set; } = "";
    public byte AccountClassId { get; set; }
    public byte DefaultDisplayGroupId { get; set; }
}
