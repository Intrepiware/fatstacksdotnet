namespace FatStacks.SqlTests.Fixtures;

public static class RepresentativeBook
{
    public static Task SeedAsync(BookDatabaseFixture fixture) => fixture.ExecuteAsync("""
        SET XACT_ABORT ON;
        BEGIN TRANSACTION;

        DECLARE @today date = CONVERT(date, SYSUTCDATETIME());
        INSERT dbo.Account(BookId, Name, AccountTypeId, DisplayGroupId, OpenDate)
        VALUES (1, N'Benchmark Bank A', 1, 1, DATEADD(day, -120, @today)),
               (1, N'Benchmark Bank B', 1, 1, DATEADD(day, -120, @today));

        ;WITH Numbers AS (
            SELECT 1 AS Number
            UNION ALL SELECT Number + 1 FROM Numbers WHERE Number < 98
        )
        INSERT dbo.Account(BookId, Name, AccountTypeId, DisplayGroupId, OpenDate)
        SELECT 1, CONCAT(N'Benchmark Expense ', Number), 11, 4, DATEADD(day, -120, @today)
        FROM Numbers;

        DECLARE @entries TABLE (JournalEntryId bigint, Description nvarchar(1000));
        ;WITH Digits AS (
            SELECT Number FROM (VALUES (0), (1), (2), (3), (4), (5), (6), (7), (8), (9)) d(Number)
        ), Numbers AS (
            SELECT a.Number * 1000 + b.Number * 100 + c.Number * 10 + d.Number + 1 AS Number
            FROM Digits a CROSS JOIN Digits b CROSS JOIN Digits c CROSS JOIN Digits d
        )
        INSERT dbo.JournalEntry(BookId, AccountingDate, Description, EntryKind)
        OUTPUT inserted.JournalEntryId, inserted.Description INTO @entries
        SELECT 1, DATEADD(day, -((Number - 1) % 80), @today), CONCAT(N'Benchmark entry ', Number), 0
        FROM Numbers;

        ;WITH EntryNumbers AS (
            SELECT JournalEntryId, CONVERT(int, SUBSTRING(Description, 17, 10)) AS Number
            FROM @entries
        )
        INSERT dbo.JournalEntryLine(JournalEntryId, AccountId, LineOrder, Debit, Credit)
        SELECT e.JournalEntryId, a.AccountId, lines.LineOrder, lines.Debit, lines.Credit
        FROM EntryNumbers e
        CROSS APPLY (VALUES
            (1, CASE WHEN e.Number <= 6000 THEN N'Benchmark Bank A' ELSE N'Benchmark Bank B' END, 0, 1),
            (2, CONCAT(N'Benchmark Expense ', (e.Number - 1) % 98 + 1), 1, 0)
        ) lines(LineOrder, AccountName, Debit, Credit)
        JOIN dbo.Account a ON a.BookId = 1 AND a.Name = lines.AccountName;

        COMMIT;
        """);
}
