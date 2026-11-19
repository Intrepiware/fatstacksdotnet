CREATE TABLE dbo.Account (
    AccountId       BIGINT         IDENTITY NOT NULL PRIMARY KEY,
    BookId          BIGINT         NOT NULL FOREIGN KEY REFERENCES dbo.Book (BookId),
    ParentAccountId BIGINT         NULL FOREIGN KEY REFERENCES dbo.Account (AccountId),
    Name            NVARCHAR (200) COLLATE Latin1_General_100_CI_AS NOT NULL CHECK (LEN(TRIM(Name)) > 0
                                                                                    AND Name NOT LIKE '%:%'),
    AccountTypeId   TINYINT        NOT NULL FOREIGN KEY REFERENCES dbo.AccountType (AccountTypeId),
    DisplayGroupId  TINYINT        NOT NULL FOREIGN KEY REFERENCES dbo.DisplayGroup (DisplayGroupId),
    OpenDate        DATE           NOT NULL,
    CloseDate       DATE           NULL,
    CreateDate      DATETIME2 (7)  DEFAULT SYSUTCDATETIME() NOT NULL,
    UpdateDate      DATETIME2 (7)  DEFAULT SYSUTCDATETIME() NOT NULL,
    CONSTRAINT UQ_Account_Path UNIQUE (BookId, ParentAccountId, Name),
    CHECK (ParentAccountId IS NULL
           OR ParentAccountId <> AccountId),
    CHECK (CloseDate IS NULL
           OR CloseDate > OpenDate)
);