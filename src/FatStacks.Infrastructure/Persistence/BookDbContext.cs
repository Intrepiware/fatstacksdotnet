using FatStacks.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
namespace FatStacks.Infrastructure.Persistence;

public sealed class BookDbContext(DbContextOptions<BookDbContext> options) : DbContext(options)
{
    public DbSet<BookEntity> Books => Set<BookEntity>();
    public DbSet<SchemaVersionEntity> SchemaVersions => Set<SchemaVersionEntity>();
    public DbSet<CurrencyEntity> Currencies => Set<CurrencyEntity>();
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<JournalEntryEntity> JournalEntries => Set<JournalEntryEntity>();
    public DbSet<JournalEntryLineEntity> JournalEntryLines => Set<JournalEntryLineEntity>();
    public DbSet<SecurityEntity> Securities => Set<SecurityEntity>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<AccountClassEntity>().ToTable("AccountClass", "dbo").HasKey(x => x.AccountClassId);
        model.Entity<DisplayGroupEntity>().ToTable("DisplayGroup", "dbo").HasKey(x => x.DisplayGroupId);
        model.Entity<AccountTypeEntity>().ToTable("AccountType", "dbo").HasKey(x => x.AccountTypeId);
        model.Entity<CurrencyEntity>().ToTable("Currency", "dbo").HasKey(x => x.CurrencyId);
        model.Entity<SchemaVersionEntity>().ToTable("SchemaVersion", "dbo").HasKey(x => x.SchemaVersionId);
        model.Entity<BookEntity>().ToTable("Book", "dbo").HasKey(x => x.BookId);
        model.Entity<AccountEntity>().ToTable("Account", "dbo").HasKey(x => x.AccountId);
        model.Entity<JournalEntryEntity>().ToTable("JournalEntry", "dbo").HasKey(x => x.JournalEntryId);
        model.Entity<JournalEntryLineEntity>().ToTable("JournalEntryLine", "dbo").HasKey(x => x.JournalEntryLineId);
        model.Entity<SecurityEntity>().ToTable("Security", "dbo").HasKey(x => x.SecurityId);
        model.Entity<BookEntity>().Property(x => x.BookId).ValueGeneratedNever();
        model.Entity<SchemaVersionEntity>().Property(x => x.SchemaVersionId).ValueGeneratedNever();
        model.Entity<BookEntity>().HasIndex(x => x.StableIdentifier).IsUnique();
        model.Entity<BookEntity>().Property(x => x.Name).HasMaxLength(200);
        model.Entity<BookEntity>().Property(x => x.TimeZoneId).HasMaxLength(100);
        model.Entity<CurrencyEntity>().Property(x => x.Code).HasColumnType("char(3)");
        model.Entity<CurrencyEntity>().Property(x => x.Name).HasMaxLength(80);
        model.Entity<AccountClassEntity>().Property(x => x.Name).HasMaxLength(32);
        model.Entity<DisplayGroupEntity>().Property(x => x.Name).HasMaxLength(32);
        model.Entity<AccountTypeEntity>().Property(x => x.Name).HasMaxLength(40);
        model.Entity<AccountEntity>().Property(x => x.Name).HasMaxLength(200).UseCollation("Latin1_General_100_CI_AS");
        model.Entity<AccountEntity>().HasIndex(x => new { x.BookId, x.ParentAccountId, x.Name }).IsUnique();
        model.Entity<JournalEntryEntity>().Property(x => x.Payee).HasMaxLength(200);
        model.Entity<JournalEntryEntity>().Property(x => x.Description).HasMaxLength(1000);
        model.Entity<JournalEntryEntity>().Property(x => x.Version).IsRowVersion();
        model.Entity<JournalEntryLineEntity>().Property(x => x.Version).IsRowVersion();
        model.Entity<JournalEntryLineEntity>().Property(x => x.Debit).HasPrecision(10, 2);
        model.Entity<JournalEntryLineEntity>().Property(x => x.Credit).HasPrecision(10, 2);
        model.Entity<JournalEntryLineEntity>().Property(x => x.Quantity).HasPrecision(18, 8);
        model.Entity<JournalEntryLineEntity>().HasIndex(x => new { x.JournalEntryId, x.LineOrder }).IsUnique();
        model.Entity<SecurityEntity>().Property(x => x.Symbol).HasMaxLength(32);
        model.Entity<SecurityEntity>().Property(x => x.Name).HasMaxLength(200);
        model.Entity<SecurityEntity>().HasIndex(x => new { x.BookId, x.Symbol }).IsUnique();
        model.Entity<BookEntity>().HasOne<CurrencyEntity>().WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AccountTypeEntity>().HasOne<AccountClassEntity>().WithMany().HasForeignKey(x => x.AccountClassId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AccountTypeEntity>().HasOne<DisplayGroupEntity>().WithMany().HasForeignKey(x => x.DefaultDisplayGroupId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AccountEntity>().HasOne<BookEntity>().WithMany().HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AccountEntity>().HasOne<AccountEntity>().WithMany().HasForeignKey(x => x.ParentAccountId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AccountEntity>().HasOne<AccountTypeEntity>().WithMany().HasForeignKey(x => x.AccountTypeId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AccountEntity>().HasOne<DisplayGroupEntity>().WithMany().HasForeignKey(x => x.DisplayGroupId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<SecurityEntity>().HasOne<BookEntity>().WithMany().HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<JournalEntryEntity>().HasOne<BookEntity>().WithMany().HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<JournalEntryLineEntity>().HasOne<JournalEntryEntity>().WithMany().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<JournalEntryLineEntity>().HasOne<AccountEntity>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<JournalEntryLineEntity>().HasOne<SecurityEntity>().WithMany().HasForeignKey(x => x.SecurityId).OnDelete(DeleteBehavior.Restrict);
        foreach (var entity in model.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType == typeof(DateOnly) || property.ClrType == typeof(DateOnly?)) property.SetColumnType("date");
                if (property.ClrType == typeof(DateTime)) property.SetColumnType("datetime2(7)");
            }
        }
    }
}
