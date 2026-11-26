namespace FatStacks.Infrastructure.Persistence.Entities;

public sealed class SchemaVersionEntity
{
    public long SchemaVersionId
    {
        get; set;
    }
    public int Version
    {
        get; set;
    }
}
