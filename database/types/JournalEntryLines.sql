IF TYPE_ID(N'dbo.JournalEntryLines') IS NULL
EXEC(N'CREATE TYPE dbo.JournalEntryLines AS TABLE (
    LineId bigint NULL,
    Version binary(8) NULL,
    AccountId bigint NOT NULL,
    LineOrder int NOT NULL,
    Debit decimal(38,28) NOT NULL,
    Credit decimal(38,28) NOT NULL,
    CurrencyCode nvarchar(3) NOT NULL,
    ReconciliationStatus tinyint NOT NULL,
    SecurityId bigint NULL,
    Quantity decimal(18,8) NULL
)');
