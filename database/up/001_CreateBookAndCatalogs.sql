CREATE TABLE dbo.AccountClass (
    AccountClassId TINYINT       NOT NULL PRIMARY KEY,
    Name           NVARCHAR (32) NOT NULL UNIQUE
);

CREATE TABLE dbo.DisplayGroup (
    DisplayGroupId TINYINT       NOT NULL PRIMARY KEY,
    Name           NVARCHAR (32) NOT NULL UNIQUE
);

CREATE TABLE dbo.AccountType (
    AccountTypeId         TINYINT       NOT NULL PRIMARY KEY,
    Name                  NVARCHAR (40) NOT NULL UNIQUE,
    AccountClassId        TINYINT       NOT NULL FOREIGN KEY REFERENCES dbo.AccountClass (AccountClassId),
    DefaultDisplayGroupId TINYINT       NOT NULL FOREIGN KEY REFERENCES dbo.DisplayGroup (DisplayGroupId)
);

CREATE TABLE dbo.Currency (
    CurrencyId       TINYINT       NOT NULL PRIMARY KEY,
    Code             CHAR (3)      NOT NULL UNIQUE,
    Name             NVARCHAR (80) NOT NULL,
    FractionalDigits TINYINT       NOT NULL CHECK (FractionalDigits BETWEEN 0 AND 2)
);

CREATE TABLE dbo.Book (
    BookId           BIGINT           NOT NULL PRIMARY KEY CHECK (BookId = 1),
    StableIdentifier UNIQUEIDENTIFIER NOT NULL UNIQUE,
    Name             NVARCHAR (200)   NOT NULL CHECK (LEN(TRIM(Name)) > 0),
    CurrencyId       TINYINT          NOT NULL FOREIGN KEY REFERENCES dbo.Currency (CurrencyId),
    TimeZoneId       NVARCHAR (100)   NOT NULL,
    CurrencyLocked   BIT              DEFAULT 0 NOT NULL,
    CreateDate       DATETIME2 (7)    DEFAULT SYSUTCDATETIME() NOT NULL,
    UpdateDate       DATETIME2 (7)    DEFAULT SYSUTCDATETIME() NOT NULL
);

CREATE TABLE dbo.SchemaVersion (
    SchemaVersionId BIGINT NOT NULL PRIMARY KEY CHECK (SchemaVersionId = 1),
    Version         INT    NOT NULL
);

INSERT  dbo.AccountClass
VALUES (1, 'Assets'),
       (2, 'Liabilities'),
       (3, 'Equity'),
       (4, 'Income'),
       (5, 'Expenses');

INSERT  dbo.DisplayGroup
VALUES (1, 'Banks'),
       (2, 'Credit Cards'),
       (3, 'Investments'),
       (4, 'Expenses'),
       (5, 'Other');

INSERT  dbo.AccountType
VALUES (1, 'Bank', 1, 1),
       (2, 'Cash', 1, 1),
       (3, 'Credit Card', 2, 2),
       (4, 'Loan Receivable', 1, 5),
       (5, 'Loan Payable', 2, 5),
       (6, 'Investment/Brokerage', 1, 3),
       (7, 'Other Asset', 1, 5),
       (8, 'Other Liability', 2, 5),
       (9, 'Equity', 3, 5),
       (10, 'Income', 4, 5),
       (11, 'Expense', 5, 4);

INSERT  dbo.Currency
VALUES (1, 'USD', 'US Dollar', 2),
       (2, 'EUR', 'Euro', 2),
       (3, 'JPY', 'Japanese Yen', 0);