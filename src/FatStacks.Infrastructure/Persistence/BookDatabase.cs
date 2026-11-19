using FatStacks.Core.Connections;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace FatStacks.Infrastructure.Persistence;

public sealed class BookDatabase(ConnectionSettings settings)
{
    public BookDbContext CreateContext() => new(new DbContextOptionsBuilder<BookDbContext>()
        .UseSqlServer(settings.ConnectionString).EnableSensitiveDataLogging(false).EnableDetailedErrors(false).Options);
    public SqlConnection CreateConnection() => new(settings.ConnectionString);
}
