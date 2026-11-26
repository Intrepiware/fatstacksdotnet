CREATE OR ALTER PROCEDURE dbo.DeleteJournalEntry
@BookId BIGINT, @JournalEntryId BIGINT, @ExpectedVersion BINARY (8), @OriginalLines dbo.JournalEntryLineVersions READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
    BEGIN TRY
        BEGIN TRANSACTION;
        IF NOT EXISTS (SELECT 1
                       FROM   dbo.Book WITH (UPDLOCK, HOLDLOCK)
                       WHERE  BookId = @BookId)
            THROW 51001, 'Invalid book.', 1;
        IF NOT EXISTS (SELECT 1
                       FROM   dbo.JournalEntry WITH (UPDLOCK, HOLDLOCK)
                       WHERE  JournalEntryId = @JournalEntryId
                              AND BookId = @BookId
                              AND Version = @ExpectedVersion)
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
        IF EXISTS (SELECT 1
                   FROM   dbo.JournalEntry
                   WHERE  JournalEntryId = @JournalEntryId
                          AND EntryKind <> 0)
           OR EXISTS (SELECT 1
                      FROM   dbo.JournalEntryLine
                      WHERE  JournalEntryId = @JournalEntryId
                             AND SecurityId IS NOT NULL)
            THROW 51001, 'Security entries require the security workflow.', 1;
        DELETE dbo.JournalEntryLine
        WHERE  JournalEntryId = @JournalEntryId;
        DELETE dbo.JournalEntry
        WHERE  JournalEntryId = @JournalEntryId;
        COMMIT TRANSACTION;
        SELECT CAST (1 AS BIT) AS Deleted;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK;
        THROW;
    END CATCH
END