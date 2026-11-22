CREATE OR ALTER PROCEDURE dbo.SaveJournalEntry
@BookId BIGINT, @JournalEntryId BIGINT=NULL, @ExpectedVersion BINARY (8)=NULL, @AccountingDate DATE, @BankPostingDate DATE=NULL, @Payee NVARCHAR (200)=NULL, @Description NVARCHAR (1000), @EntryKind TINYINT, @Lines dbo.JournalEntryLines READONLY, @OriginalLines dbo.JournalEntryLineVersions READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @Currency AS NVARCHAR (3), @Digits AS TINYINT;
        SELECT @Currency = c.Code,
               @Digits = c.FractionalDigits
        FROM   dbo.Book AS b WITH (UPDLOCK, HOLDLOCK)
               INNER JOIN
               dbo.Currency AS c
               ON c.CurrencyId = b.CurrencyId
        WHERE  b.BookId = @BookId;
        IF @Currency IS NULL
           OR @EntryKind <> 0
           OR @AccountingDate IS NULL
           OR @Description IS NULL
            THROW 51001, 'Invalid journal header.', 1;
        IF @JournalEntryId IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1
                               FROM   dbo.JournalEntry WITH (UPDLOCK, HOLDLOCK)
                               WHERE  JournalEntryId = @JournalEntryId
                                      AND BookId = @BookId
                                      AND Version = @ExpectedVersion
                                      AND EntryKind = 0)
                    THROW 51002, 'Reload the journal entry.', 1;
                IF EXISTS (SELECT JournalEntryLineId,
                                  Version
                           FROM   dbo.JournalEntryLine WITH (UPDLOCK, HOLDLOCK)
                           WHERE  JournalEntryId = @JournalEntryId
                           EXCEPT
                           SELECT LineId,
                                  Version
                           FROM   @OriginalLines)
                   OR EXISTS (SELECT LineId,
                                     Version
                              FROM   @OriginalLines
                              EXCEPT
                              SELECT JournalEntryLineId,
                                     Version
                              FROM   dbo.JournalEntryLine
                              WHERE  JournalEntryId = @JournalEntryId)
                    THROW 51002, 'Reload the complete journal entry.', 1;
            END
        ELSE
            IF @ExpectedVersion IS NOT NULL
               OR EXISTS (SELECT 1
                          FROM   @OriginalLines)
                THROW 51001, 'Invalid original versions.', 1;
        IF (SELECT COUNT(*)
            FROM   @Lines) < 2
           OR EXISTS (SELECT   LineOrder
                      FROM     @Lines
                      GROUP BY LineOrder
                      HAVING   COUNT(*) > 1)
           OR EXISTS (SELECT   LineId
                      FROM     @Lines
                      WHERE    LineId IS NOT NULL
                      GROUP BY LineId
                      HAVING   COUNT(*) > 1)
           OR EXISTS (SELECT 1
                      FROM   @Lines
                      WHERE  LineOrder <= 0
                             OR Debit < 0
                             OR Credit < 0
                             OR NOT ((Debit > 0
                                      AND Credit = 0)
                                     OR (Credit > 0
                                         AND Debit = 0))
                             OR Debit > 99999999.99
                             OR Credit > 99999999.99
                             OR Debit <> ROUND(Debit, @Digits)
                             OR Credit <> ROUND(Credit, @Digits)
                             OR CurrencyCode <> @Currency
                             OR SecurityId IS NOT NULL
                             OR Quantity IS NOT NULL
                             OR ReconciliationStatus > 2
                             OR (LineId IS NULL
                                 AND (Version IS NOT NULL
                                      OR ReconciliationStatus <> 0)))
           OR (SELECT SUM(CAST (Debit AS DECIMAL (28, 2)))
               FROM   @Lines) <> (SELECT SUM(CAST (Credit AS DECIMAL (28, 2)))
                                  FROM   @Lines)
            THROW 51001, 'Invalid amounts, currency, or line shape.', 1;
        IF EXISTS (SELECT 1
                   FROM   @Lines AS l
                          LEFT OUTER JOIN
                          @OriginalLines AS o
                          ON o.LineId = l.LineId
                             AND o.Version = l.Version
                   WHERE  l.LineId IS NOT NULL
                          AND o.LineId IS NULL)
            THROW 51002, 'Reload original line versions.', 1;
        IF EXISTS (SELECT 1
                   FROM   @Lines AS l
                          LEFT OUTER JOIN
                          dbo.Account AS a WITH (UPDLOCK, HOLDLOCK)
                          ON a.AccountId = l.AccountId
                             AND a.BookId = @BookId
                          LEFT OUTER JOIN
                          dbo.JournalEntryLine AS old
                          ON old.JournalEntryLineId = l.LineId
                             AND old.JournalEntryId = @JournalEntryId
                   WHERE  a.AccountId IS NULL
                          OR @AccountingDate < a.OpenDate
                          OR (a.CloseDate IS NOT NULL
                              AND (old.AccountId IS NULL
                                   OR old.AccountId <> a.AccountId
                                   OR @AccountingDate >= a.CloseDate)))
            THROW 51001, 'Choose eligible accounts and dates.', 1;
        IF EXISTS (SELECT 1
                   FROM   @Lines AS l
                          INNER JOIN
                          dbo.JournalEntryLine AS old
                          ON old.JournalEntryLineId = l.LineId
                   WHERE  l.ReconciliationStatus <> old.ReconciliationStatus)
            THROW 51001, 'Use reconciliation to change line states.', 1;
        DECLARE @Reset TABLE (
            LineId BIGINT PRIMARY KEY);
        DECLARE @Now AS DATETIME2 (7) = SYSUTCDATETIME();
        IF @JournalEntryId IS NULL
            BEGIN
                INSERT  dbo.JournalEntry (
                    BookId,
                    AccountingDate,
                    BankPostingDate,
                    Payee,
                    Description,
                    EntryKind
                )
                VALUES                   (@BookId, @AccountingDate, @BankPostingDate, @Payee, @Description, 0);
                SET @JournalEntryId = SCOPE_IDENTITY();
            END
        ELSE
            BEGIN
                INSERT @Reset
                SELECT old.JournalEntryLineId
                FROM   dbo.JournalEntryLine AS old
                       INNER JOIN
                       @Lines AS l
                       ON old.JournalEntryLineId = l.LineId
                WHERE  old.ReconciliationStatus <> 0
                       AND (old.AccountId <> l.AccountId
                            OR old.Debit <> l.Debit
                            OR old.Credit <> l.Credit);
                UPDATE dbo.JournalEntry
                SET    AccountingDate  = @AccountingDate,
                       BankPostingDate = @BankPostingDate,
                       Payee           = @Payee,
                       Description     = @Description,
                       UpdateDate      = @Now
                WHERE  JournalEntryId = @JournalEntryId;
                DELETE old
                FROM   dbo.JournalEntryLine AS old
                WHERE  old.JournalEntryId = @JournalEntryId
                       AND NOT EXISTS (SELECT 1
                                       FROM   @Lines AS l
                                       WHERE  l.LineId = old.JournalEntryLineId);
                -- Move retained orders above both sets before reordering; keep keys and row identities.
                DECLARE @Offset AS BIGINT = (SELECT COALESCE (MAX(LineOrder), 0)
                                             FROM   dbo.JournalEntryLine
                                             WHERE  JournalEntryId = @JournalEntryId);
                SET @Offset = @Offset + (SELECT COALESCE (MAX(LineOrder), 0)
                                         FROM   @Lines) + 1;
                IF @Offset * 2 > 2147483647
                    THROW 51001, 'Line order is too large.', 1;
                UPDATE dbo.JournalEntryLine
                SET    LineOrder = LineOrder + CAST (@Offset AS INT)
                WHERE  JournalEntryId = @JournalEntryId;
                UPDATE old
                SET    AccountId            = l.AccountId,
                       LineOrder            = l.LineOrder,
                       Debit                = l.Debit,
                       Credit               = l.Credit,
                       ReconciliationStatus = CASE WHEN old.AccountId <> l.AccountId
                                                        OR old.Debit <> l.Debit
                                                        OR old.Credit <> l.Credit THEN 0 ELSE old.ReconciliationStatus END,
                       UpdateDate           = @Now
                FROM   dbo.JournalEntryLine AS old
                       INNER JOIN
                       @Lines AS l
                       ON l.LineId = old.JournalEntryLineId
                WHERE  old.JournalEntryId = @JournalEntryId;
            END
        INSERT dbo.JournalEntryLine (
            JournalEntryId,
            AccountId,
            LineOrder,
            Debit,
            Credit,
            ReconciliationStatus
        )
        SELECT @JournalEntryId,
               AccountId,
               LineOrder,
               CAST (Debit AS DECIMAL (10, 2)),
               CAST (Credit AS DECIMAL (10, 2)),
               0
        FROM   @Lines
        WHERE  LineId IS NULL;
        UPDATE dbo.Book
        SET    CurrencyLocked = 1,
               UpdateDate     = @Now
        WHERE  BookId = @BookId
               AND CurrencyLocked = 0;
        -- Capture committed values while holding the locks; return only after confirmed commit.
        SELECT JournalEntryId,
               Version
        INTO   #SavedHeader
        FROM   dbo.JournalEntry
        WHERE  JournalEntryId = @JournalEntryId;
        SELECT l.JournalEntryLineId,
               l.Version,
               l.AccountId,
               l.LineOrder,
               l.Debit,
               l.Credit,
               l.ReconciliationStatus,
               CAST (CASE WHEN r.LineId IS NULL THEN 0 ELSE 1 END AS BIT) AS WasReset
        INTO   #SavedLines
        FROM   dbo.JournalEntryLine AS l
               LEFT OUTER JOIN
               @Reset AS r
               ON r.LineId = l.JournalEntryLineId
        WHERE  l.JournalEntryId = @JournalEntryId;
        COMMIT TRANSACTION;
        SELECT *
        FROM   #SavedHeader;
        SELECT   *
        FROM     #SavedLines
        ORDER BY LineOrder;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK;
        THROW;
    END CATCH
END