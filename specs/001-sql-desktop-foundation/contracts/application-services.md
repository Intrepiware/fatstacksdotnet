# Application Service Contracts

**Scope:** Internal desktop application contracts. No HTTP API or user command-line interface.
All operations are asynchronous where they perform I/O. Core contracts contain no WPF, EF, SQL,
or Autofac types. Requests use typed values and responses contain explicit outcomes.

## Common Outcomes

Use Success, ValidationFailure, Conflict, and Failed for journal operations. ValidationFailure
identifies the field and line. Conflict means the loaded version no longer matches. Failed
reports an operation error without assuming whether an unconfirmed commit occurred. Return
Success only after commit is confirmed. Retain the draft on failure. Never derive outcome types
from message text.

Return sanitized error categories and corrective actions. Do not expose provider exception text,
SQL values, financial descriptions, or connection strings in diagnostics (FR-029).

## Persistence Operation Matrix

| Operation | Access | Reason |
|---|---|---|
| Read book, catalogs, accounts, entry details | EF queries | Straightforward mapped reads |
| Read grouped account balances and holdings | EF aggregate queries | Simple grouping; preserve full history |
| Read account register | ReadAccountRegister procedure | FR-010/011: windowed balances before filters |
| Initialize or migrate schema | Explicit grate maintenance | FR-005/028/032; constitution |
| Create/update book, account, security definitions | EF saves; transaction when several rows change | Ordinary metadata validation; outside FR-017 |
| Create/edit monetary or security journal entry | SaveJournalEntry procedure | FR-015/017/025; atomic lines, versions, and holding-history checks |
| Delete journal entry | DeleteJournalEntry procedure | FR-017/018/025; remove all lines and validate resulting holdings |
| Set one line's reconciliation status | EF transaction | FR-017/021; ordinary status update with version checks |
| Save profiles or startup preference | User-local configuration store | FR-001/002/006; outside financial database |

Only `StoredProcedureRepository` calls stored procedures. No metadata procedure or procedure-only runtime
permission model is required. Use typed parameters for every supplied value, including table-valued
parameters for journal lines. Never concatenate values into SQL.

`StoredProcedureRepository` handles parameter binding, execution, and result mapping for all
application stored procedures. Application services own business workflows.

## Connection and Setup

`TestConnection(settings)` reports SQL reachability, schema compatibility, and transport warnings.
It neither activates a book nor changes the startup preference. Dispose its test connection.

`OpenProfile(profileId)` captures the profile settings, checks exact schema version, then reads
Book.StableIdentifier. Reject a mismatch with ExpectedBookIdentifier until the user explicitly
reassociates the profile. First association records the observed identity. Only a successful open
updates LastSuccessfulProfileId. Report a preference write failure separately from connection success.

`SwitchProfile(profileId)` resolves save/discard/cancel and outstanding writes first. Cancel before
switching leaves the current book open. Once switching begins, cancel old reads, clear UI data,
and dispose the old scope. A failed open leaves no active book. A delayed read may update the UI
only if its captured session is still active. Each write keeps its original connection target.

`InitializeDatabase(settings, bookName, currency, timeZone, maintenanceCredentials)` is an explicit
setup action. Distinguish an empty database, a compatible installation, and an unrelated or
incompatible schema. Initialize only the selected empty database through grate. Create the book
once in a transaction. Repeating setup preserves an existing compatible book. Never change its
identity or records to match new setup arguments. Normal OpenProfile never invokes this action.

Use a runtime role with SELECT/INSERT/UPDATE/DELETE on required application tables and EXECUTE
on the three journal procedures. It has no DDL, db_owner, or administrator rights. Maintenance
uses a separate explicit identity. Do not save maintenance credentials.

## Metadata Operations

Account requests include parent, name, type, display group, and open/close dates. Validate the
catalog mapping, unique paths, hierarchy, and date rules in [data-model.md](../data-model.md).
Use an EF transaction for changes that need multiple writes. Security requests include symbol,
name, and quantity precision. Book requests include name, supported currency, and time zone.
Reject a currency change after CurrencyLocked becomes true. Check the lock and save a currency
change in one serializable EF transaction so a first journal save cannot race that check.
FR-017 version checks do not apply to these operations.

## SaveJournalEntry

Inputs are expected BookId, optional JournalEntryId, expected header version,
AccountingDate, optional BankPostingDate, Payee, Description, EntryKind, and the complete final line
list. Each existing line carries its ID and loaded version. A separate original-line version list
covers removed lines too. New lines have no database ID yet.

Each final line supplies account ID, order, debit, credit, currency code, status, and optional
security/quantity. Validate typed service inputs and final decimal values before constructing SQL
parameters. CurrencyCode is checked against the active book before dispatch.

The procedure uses one serializable database transaction:

1. Load the target book, current entry/lines, referenced accounts, and affected holding history.
2. For edits, compare the expected header version, complete original line set, and line versions.
3. Validate the complete final entry, account dates, same-book references, and EntryKind rules.
4. Preserve existing line IDs. Apply additions/removals and reset financially changed statuses.
5. Validate the resulting affected security histories, including later entries.
6. Set CurrencyLocked on the first save and commit.

Return the entry ID, refreshed header/line versions, and IDs of lines whose statuses reset.
On validation or concurrency failure, roll back every change.

This procedure owns whole-entry accounting checks. The UI may show early feedback, but it must
not be the only check for balance or holding validity. SQL constraints own row shape and references.
No duplicate numeric-text parser or custom money representation is needed.

## DeleteJournalEntry

Inputs are entry ID, expected header version, and all loaded line IDs/versions.
The UI must first show the required confirmation; Cancel has initial focus.

Use one serializable transaction. Compare the entry and complete line set, and validate the
holding history that would remain after deletion. Delete all lines and the header, then commit.
Return Success only for a confirmed commit. Reject a delete that would leave an unsupported
negative security position.

## SetLineReconciliation

Inputs are entry ID, line ID, expected header/line versions, and target status.
All three statuses may transition to either other status. A request with no status change needs
no write. Never update other lines or financial columns.

Use one ordinary EF transaction. Update the selected line using its loaded rowversion and the
header timestamp using its loaded rowversion. Call SaveChanges and commit the transaction.
EF concurrency failures roll back both updates. A concurrent whole-entry edit
must therefore conflict rather than overwrite the status. Return the new versions and status.
A stored procedure is unnecessary for this operation.

## ReadAccountRegister

Inputs: AccountId, book-local Today, and SearchText. Output: current signed balance and ordered
register rows with entry ID, accounting date, description, counter-account names, Payment,
Deposit, running balance, and selected-account reconciliation summary.

Group all selected-account lines by entry. Sum Debit minus Credit. Calculate cumulative totals
over the complete history through Today, ordered by AccountingDate then JournalEntryId. Only then
filter to Today minus 89 days through Today. Apply case-insensitive description/counter-account
search after balance calculation. Escape LIKE metacharacters or use a literal substring operation.
Do not omit zero-net rows. Use parameters for dates, account IDs, and search text.

Positive net movement becomes Deposit; negative becomes positive Payment; zero leaves both blank.
Mixed is a display result when selected-account lines have different stored states. Details use
an EF read to return every line, including lines for closed accounts. Tree totals batch direct
account sums and add descendant contributions without dropping closed accounts.
