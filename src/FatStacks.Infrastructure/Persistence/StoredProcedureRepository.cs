using FatStacks.Core.Connections;
namespace FatStacks.Infrastructure.Persistence;
// All application procedure calls will be added here by US2/US3. US1 has no procedure calls.
public sealed class StoredProcedureRepository
{
    public required BookDatabase Database { private get; init; }
}
