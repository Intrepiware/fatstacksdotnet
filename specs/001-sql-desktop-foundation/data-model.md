# Data Model: SQL Desktop Foundation

**Status**: Planned storage contract. Grate creates the schema; EF only maps it.

## Shared Rules

Application record keys use bigint identity columns, except the two singleton rows described below.
Small catalogs use tinyint keys. Every
primary and foreign key has one column. Combined uniqueness uses a separate UNIQUE constraint.
Dates use SQL date and C# DateOnly. Operational timestamps use UTC datetime2(7), named CreateDate
and UpdateDate. SQL rowversion is a concurrency token, not a date or time.

Use uniqueidentifier only for Book.StableIdentifier, which identifies a book across profiles.
Local profile IDs are GUIDs for stable targeting after a rename.

## Catalogs

| Table | Fields and rules |
|---|---|
| AccountClass | AccountClassId tinyint PK; unique Name; five rows: Assets, Liabilities, Equity, Income, Expenses |
| DisplayGroup | DisplayGroupId tinyint PK; unique Name; Banks, Credit Cards, Investments, Expenses, Other |
| AccountType | AccountTypeId tinyint PK; unique Name; AccountClassId FK; DefaultDisplayGroupId FK |
| Currency | CurrencyId tinyint PK; unique Code char(3); Name; FractionalDigits tinyint |

Seed USD (2 digits), EUR (2), and JPY (0). Reject unsupported currencies. The Loans Payable
sidebar tab uses the Loan Payable account type and the stored Other display group. Account
type determines class through its foreign key. Store the class on AccountType.

| Account type | Class | Default group |
|---|---|---|
| Bank | Assets | Banks |
| Cash | Assets | Banks |
| Credit Card | Liabilities | Credit Cards |
| Loan Receivable | Assets | Other |
| Loan Payable | Liabilities | Other |
| Investment/Brokerage | Assets | Investments |
| Other Asset | Assets | Other |
| Other Liability | Liabilities | Other |
| Equity | Equity | Other |
| Income | Income | Other |
| Expense | Expenses | Expenses |

Securities are instruments held in Investment/Brokerage accounts. They are not a sixth class.

## Book

| Field | Type and meaning |
|---|---|
| BookId | bigint PK, assigned 1 without IDENTITY; CHECK BookId = 1 permits only one book |
| StableIdentifier | uniqueidentifier, NOT NULL, UNIQUE; permanent logical identity |
| Name | nvarchar(200), required |
| CurrencyId | tinyint FK, required; defaults to USD during setup |
| TimeZoneId | nvarchar(100), required; valid Windows TimeZoneInfo ID |
| CurrencyLocked | bit, default 0; becomes 1 with the first saved journal entry |
| CreateDate, UpdateDate | UTC datetime2(7) |

CurrencyLocked remains true after all entries are deleted (FR-020). Setup uses EF and validates
the selected currency and time zone.

## Account

| Field | Type and meaning |
|---|---|
| AccountId | bigint PK |
| BookId | bigint FK |
| ParentAccountId | nullable bigint FK to Account |
| Name | nvarchar(200), trimmed and nonempty |
| AccountTypeId | tinyint FK; fixed after creation in phase 1 |
| DisplayGroupId | tinyint FK; defaults from type, may be changed |
| OpenDate, CloseDate | date; CloseDate nullable |
| CreateDate, UpdateDate | UTC datetime2(7) |

UNIQUE(BookId, ParentAccountId, Name) under a case-insensitive collation prevents duplicate
siblings, including root names. Paths derive from parents and names. Disallow ':' in a name
because it separates path components. Renaming an ancestor does not change record identities.
CHECK ParentAccountId differs from AccountId. CHECK CloseDate is null or later than OpenDate.
The service rejects ancestor cycles and cross-book references in an ordinary EF transaction.

Closed accounts remain stored. Exclude them from new-line choices. An edit may keep the same
closed-account reference on the same line only when its accounting date precedes CloseDate.
Require dates on or after OpenDate. Closing an account must reject a date that would invalidate
its existing lines. This applies the date rules consistently (FR-008; review R08).

## JournalEntry

| Field | Type and meaning |
|---|---|
| JournalEntryId | bigint PK; stable identity and equal-date ordering key |
| BookId | bigint FK |
| AccountingDate | date, required |
| BankPostingDate | nullable date, independent of AccountingDate |
| Payee | nvarchar(200), optional |
| Description | nvarchar(1000), required; may be empty |
| EntryKind | tinyint: 0 ordinary, 1 security opening, 2 security transfer |
| Version | rowversion |
| CreateDate, UpdateDate | UTC datetime2(7) |

EntryKind expresses the limited security workflows in FR-025/026. It allows the save procedure
to reject an unsupported quantity operation without inferring intent from free text.

## JournalEntryLine

| Field | Type and meaning |
|---|---|
| JournalEntryLineId | bigint PK; preserved on edits |
| JournalEntryId | bigint FK |
| AccountId | bigint FK |
| LineOrder | int, positive; UNIQUE(JournalEntryId, LineOrder) |
| Debit, Credit | decimal(10,2), NOT NULL |
| ReconciliationStatus | tinyint, NOT NULL, default 0; CHECK value in (0,1,2) |
| SecurityId | nullable bigint FK |
| Quantity | nullable decimal(18,8), signed movement |
| Version | rowversion |
| CreateDate, UpdateDate | UTC datetime2(7) |

CHECK requires Debit > 0 and Credit = 0, or Credit > 0 and Debit = 0. CHECK requires SecurityId
and Quantity together, with nonzero Quantity. Positive quantity accompanies a debit; negative
quantity accompanies a credit. The entry's book supplies the stored currency. Service requests
still include CurrencyCode so a mismatch can be rejected rather than silently reinterpreted.

The save procedure verifies at least two lines, equal debit/credit totals, valid account dates,
and references to the intended book. Foreign keys reject nonexistent references. Since only one
Book can exist in the database, valid Book foreign keys also enforce the single-book boundary.
Cross-book profile mistakes are handled before activation (FR-005).

Keep all unchanged line IDs and statuses. Reset a changed financial line to Uncleared and return
its ID for the UI explanation. Financial fields are account, money, security, and quantity.
A requested currency mismatch is rejected. Explicit status updates affect only the chosen line.

Header and line versions cover FR-017. Save/delete requests carry the loaded header version and
versions for all existing lines. Compare the current line set too. An inserted or removed line
since loading is a conflict. Status requests carry the selected line version and header version.
Update the header timestamp on each successful application line change to advance its version.

Delete physically removes the header and all its lines in one transaction. Keep foreign keys
restrictive and delete lines before the header. Do not add soft-delete flags or retained line data.

## Security and Derived Holdings

| Field | Type and meaning |
|---|---|
| SecurityId | bigint PK |
| BookId | bigint FK |
| Symbol | nvarchar(32), required; UNIQUE(BookId, Symbol) |
| Name | nvarchar(200), required |
| QuantityPrecision | tinyint, CHECK 0 through 8 |
| CreateDate, UpdateDate | UTC datetime2(7) |

Validate quantity against the declared scale and the decimal(18,8) range. The maximum magnitude
is 9,999,999,999.99999999. Keep quantity precision fixed after first use to avoid reinterpreting
stored movements. Names and symbols may change while stable IDs preserve history.

SecurityHolding is a read result, not a table. Group security lines by AccountId and SecurityId.
Sum Quantity for units and Debit minus Credit for carrying amount. Exclude ordinary cash lines.

An opening entry debits one holding account and credits Equity. A transfer credits one holding
account and debits a different holding account for the same security, amount, and quantity.
These two security workflows use two lines. Ordinary monetary entries may have splits.

Save/delete must validate the resulting history for every affected account/security pair in
(AccountingDate, JournalEntryId) order. Include later entries and both old and new pairs on edit.
Group an entry's movements before testing a position. Neither units nor carrying amount may go
negative. A transfer cannot exceed either recorded value. If units become zero, carrying amount
must also be zero, and conversely; otherwise a later supported transfer cannot clear the holding.
This rule prevents stranded values in the limited FR-025 model. Users supply the transfer amount;
the application does not calculate an allocation or market value.

## SchemaVersion and Grate

SchemaVersion has SchemaVersionId bigint PK, assigned 1 without IDENTITY, with CHECK value = 1.
Its Version is int NOT NULL.
The initial application expects version 1. Missing, unreadable, or unequal version blocks
activation. This is independent of grate's own migration history.

Keep one current definition per procedure/view in grate's standard sprocs/views folders.
Use immutable scripts in up for table creation and evolution. These scripts are authoritative
for tables; keep no separate table-definition copies. Include SQL types, constraints, and
indexes in the deployment package. Use `text -merge` Git attributes on
sprocs/views. Do not use EF EnsureCreated, EnsureDeleted, or migrations.

## Indexes and Numeric Boundaries

Add indexes on JournalEntry(BookId, AccountingDate, JournalEntryId),
JournalEntryLine(AccountId, JournalEntryId), and
JournalEntryLine(AccountId, SecurityId, JournalEntryId). They support FR-010/011/025 reads.
Primary keys and UNIQUE constraints supply the remaining initial indexes. Measure before adding more.

Validate money at service input and before binding final SQL/EF values. The largest positive
line value is 99,999,999.99. Compare values to their permitted scale; accept trailing zeros.
Reject precision loss instead of rounding. Typed SQL decimal parameters do not replace these
checks. Intermediate decimal calculations may keep extra precision. Sum before narrowing;
return monetary totals as decimal(28,2) and quantity totals as decimal(28,8). Report aggregate
range errors explicitly. Do not present overflow or an unknown amount as zero.

## User-Local Configuration

Store FormatVersion, Profiles, and LastSuccessfulProfileId in a JSON
file under the current user's local application data folder. Each profile has ProfileId GUID,
unique case-insensitive FriendlyName, connection settings, protected secret content, and optional
ExpectedBookIdentifier GUID. Protect any secret-bearing connection-string content with Windows
current-user DPAPI. Never serialize plaintext passwords or maintenance credentials.

