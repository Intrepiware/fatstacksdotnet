CREATE TABLE dbo.Security (
    SecurityId        BIGINT         IDENTITY NOT NULL PRIMARY KEY,
    BookId            BIGINT         NOT NULL FOREIGN KEY REFERENCES dbo.Book (BookId),
    Symbol            NVARCHAR (32)  NOT NULL CHECK (LEN(TRIM(Symbol)) > 0),
    Name              NVARCHAR (200) NOT NULL CHECK (LEN(TRIM(Name)) > 0),
    QuantityPrecision TINYINT        NOT NULL CHECK (QuantityPrecision BETWEEN 0 AND 8),
    CreateDate        DATETIME2 (7)  DEFAULT SYSUTCDATETIME() NOT NULL,
    UpdateDate        DATETIME2 (7)  DEFAULT SYSUTCDATETIME() NOT NULL,
    CONSTRAINT UQ_Security_Symbol UNIQUE (BookId, Symbol)
);

ALTER TABLE dbo.JournalEntryLine
    ADD CONSTRAINT FK_JournalEntryLine_Security FOREIGN KEY (SecurityId) REFERENCES dbo.Security (SecurityId);

ALTER TABLE dbo.JournalEntryLine
    ADD CONSTRAINT CK_JournalEntryLine_Quantity CHECK ((SecurityId IS NULL
                                                        AND Quantity IS NULL)
                                                       OR (SecurityId IS NOT NULL
                                                           AND Quantity IS NOT NULL
                                                           AND ((Quantity > 0
                                                                 AND Debit > 0)
                                                                OR (Quantity < 0
                                                                    AND Credit > 0))));