CREATE TABLE dbo.JournalEntry (
    JournalEntryId  BIGINT          IDENTITY NOT NULL PRIMARY KEY,
    BookId          BIGINT          NOT NULL FOREIGN KEY REFERENCES dbo.Book (BookId),
    AccountingDate  DATE            NOT NULL,
    BankPostingDate DATE            NULL,
    Payee           NVARCHAR (200)  NULL,
    Description     NVARCHAR (1000) NOT NULL,
    EntryKind       TINYINT         NOT NULL CHECK (EntryKind IN (0, 1, 2)), -- 0 ordinary, 1 security opening, 2 security transfer
    Version         ROWVERSION      NOT NULL,
    CreateDate      DATETIME2 (7)   DEFAULT SYSUTCDATETIME() NOT NULL,
    UpdateDate      DATETIME2 (7)   DEFAULT SYSUTCDATETIME() NOT NULL
);

CREATE TABLE dbo.JournalEntryLine (
    JournalEntryLineId   BIGINT          IDENTITY NOT NULL PRIMARY KEY,
    JournalEntryId       BIGINT          NOT NULL FOREIGN KEY REFERENCES dbo.JournalEntry (JournalEntryId),
    AccountId            BIGINT          NOT NULL FOREIGN KEY REFERENCES dbo.Account (AccountId),
    LineOrder            INT             NOT NULL CHECK (LineOrder > 0),
    Debit                DECIMAL (10, 2) NOT NULL,
    Credit               DECIMAL (10, 2) NOT NULL,
    ReconciliationStatus TINYINT         DEFAULT 0 NOT NULL CHECK (ReconciliationStatus IN (0, 1, 2)),
    SecurityId           BIGINT          NULL,
    Quantity             DECIMAL (18, 8) NULL,
    Version              ROWVERSION      NOT NULL,
    CreateDate           DATETIME2 (7)   DEFAULT SYSUTCDATETIME() NOT NULL,
    UpdateDate           DATETIME2 (7)   DEFAULT SYSUTCDATETIME() NOT NULL,
    CONSTRAINT UQ_JournalEntryLine_Order UNIQUE (JournalEntryId, LineOrder),
    CHECK ((Debit > 0
            AND Credit = 0)
           OR (Credit > 0
               AND Debit = 0))
);

CREATE INDEX IX_JournalEntry_BookDate
    ON dbo.JournalEntry(BookId, AccountingDate, JournalEntryId);

CREATE INDEX IX_JournalEntryLine_Account
    ON dbo.JournalEntryLine(AccountId, JournalEntryId);

CREATE INDEX IX_JournalEntryLine_Holding
    ON dbo.JournalEntryLine(AccountId, SecurityId, JournalEntryId);