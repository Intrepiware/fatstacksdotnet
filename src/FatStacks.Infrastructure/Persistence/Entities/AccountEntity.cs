namespace FatStacks.Infrastructure.Persistence.Entities;

public sealed class AccountEntity
{
    public long AccountId { get; set; }
    public long BookId { get; set; }
    public long? ParentAccountId { get; set; }
    public string Name { get; set; } = "";
    public byte AccountTypeId { get; set; }
    public byte DisplayGroupId { get; set; }
    public DateOnly OpenDate { get; set; }
    public DateOnly? CloseDate { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }
}
