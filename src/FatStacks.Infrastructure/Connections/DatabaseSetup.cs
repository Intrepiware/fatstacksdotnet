using System.Data;
using FatStacks.Core.Connections;
using FatStacks.Core.Contracts;
using FatStacks.Infrastructure.Diagnostics;
using FatStacks.Infrastructure.Persistence;
using FatStacks.Infrastructure.Persistence.Entities;
using grate.Configuration;
using grate.DependencyInjection;
using grate.Migration;
using grate.SqlServer.Migration;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace FatStacks.Infrastructure.Connections;

public sealed class DatabaseSetup : IDatabaseSetup
{
    public required SqlTransport Transport
    {
        private get; init;
    }
    public string ScriptDirectory { get; init; } = Path.Combine(AppContext.BaseDirectory, "database");
    public async Task<OperationResult<BookInfo>> InitializeAsync(ConnectionSettings maintenance, BookSetup book, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(book.Name) || book.Name.Trim().Length > 200 || book.TimeZoneId.Length > 100 || !TimeZoneInfo.TryFindSystemTimeZoneById(book.TimeZoneId, out _))
            return OperationResult<BookInfo>.Fail("Supply a book name (up to 200 characters) and a valid Windows time zone.", Outcome.ValidationFailure);
        if (book.CurrencyCode is not ("USD" or "EUR" or "JPY"))
            return OperationResult<BookInfo>.Fail("Choose USD, EUR, or JPY.", Outcome.ValidationFailure);
        try
        {
            var opened = await Transport.OpenAsync(maintenance, cancellationToken);
            await using var connection = opened.Connection;
            await using (var inspect = connection.CreateCommand())
            {
                inspect.CommandText = "SELECT CASE WHEN OBJECT_ID('dbo.SchemaVersion','U') IS NOT NULL THEN 1 WHEN EXISTS (SELECT 1 FROM sys.objects WHERE is_ms_shipped=0 AND type IN ('U','V','P','FN','IF','TF')) THEN 2 ELSE 0 END";
                var state = Convert.ToInt32(await inspect.ExecuteScalarAsync(cancellationToken), System.Globalization.CultureInfo.InvariantCulture);
                if (state == 2)
                    return OperationResult<BookInfo>.Fail("The selected database contains an unrelated or incomplete schema. No changes were made. Inspect it before explicit maintenance.");
                if (state == 1)
                {
                    await using var existing = new BookDatabase(opened.Settings).CreateContext();
                    var version = await existing.SchemaVersions.Where(x => x.SchemaVersionId == 1).Select(x => (int?)x.Version).SingleOrDefaultAsync(cancellationToken);
                    if (!SchemaCompatibility.IsCompatible(version))
                        return OperationResult<BookInfo>.Fail("The existing schema is incompatible or incomplete. No changes were made.");
                    if (await existing.Books.AnyAsync(cancellationToken))
                        return await new ConnectionService { Transport = Transport }.TestAsync(opened.Settings, cancellationToken);
                }
                else
                {
                    // Grate's own DI container is confined to this maintenance adapter. No console/file logger is registered.
                    var services = new ServiceCollection();
                    services.AddLogging(x => x.ClearProviders());
                    services.AddGrate(x => x.WithConnectionString(opened.Settings.ConnectionString).WithSqlFilesDirectory(ScriptDirectory)
                        .DoNotCreateDatabase().WithTransaction().WithVersion("1")
                        .WithFolders(Folders.Create(Folders.Default.Values.OfType<MigrationsFolder>().Select(folder =>
                            // Grate has no default types folder; run TVP definitions before dependent procedures.
                            folder.Key == "RunFirstAfterUp" ? folder with
                            {
                                Path = "types",
                                Name = "Journal parameter types"
                            } :
                            folder.Key is "Permissions" or "AfterMigration"
                                ? folder with
                                {
                                    TransactionHandling = TransactionHandling.Default
                                }
                                : folder)))).UseDatabase(typeof(SqlServerDatabase));
                    await using var provider = services.BuildServiceProvider();
                    await using var migrator = provider.GetRequiredService<IGrateMigrator>();
                    // Grate has no cancellation token. Once migration starts, await its definite completion.
                    await migrator.Migrate();
                }
            }
            await using var db = new BookDatabase(opened.Settings).CreateContext();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            if (!await db.Books.AnyAsync(cancellationToken))
            {
                var currency = await db.Currencies.SingleAsync(x => x.Code == book.CurrencyCode, cancellationToken);
                db.Books.Add(new BookEntity
                {
                    BookId = 1,
                    StableIdentifier = Guid.NewGuid(),
                    Name = book.Name.Trim(),
                    CurrencyId = currency.CurrencyId,
                    TimeZoneId = book.TimeZoneId,
                    CreateDate = DateTime.UtcNow,
                    UpdateDate = DateTime.UtcNow
                });
                await db.SaveChangesAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
            return await new ConnectionService { Transport = Transport }.TestAsync(opened.Settings, cancellationToken);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return DatabaseErrors.Result<BookInfo>(ex);
        }
    }
}
