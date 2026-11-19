namespace FatStacks.Core.Connections;

public static class SchemaCompatibility
{
    public const int ExpectedVersion = 1;
    public static bool IsCompatible(int? version) => version == ExpectedVersion;
}
