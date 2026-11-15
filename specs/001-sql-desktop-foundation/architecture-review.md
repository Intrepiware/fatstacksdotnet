# Existing Desktop Application: Architecture and Code Review

**Reviewed**: 2026-11-14
**Feature**: [Phase 1](spec.md)
**Source root**: `.workspace/dotnet-beancount/src`
**Method**: Static review of all supplied C# files, application and window XAML, project files,
and resource references. The supplied copy has two projects. It has no test projects or build
configuration for the full solution. The referenced WindowsChecks assembly is not included.
No Windows launch, build, database test, UI automation, or performance measurement was done.

The application is a small WPF desktop application that targets .NET 10. Its layout and user
interactions are suitable for reuse. Its core reads and edits only part of the file format.
Replacing only the file input and output calls would leave gaps in accounting and state control.

## What Is Worth Preserving

- The UI is small. It has a clear main window and separate create, edit, and delete dialogs.
- Accounting-related code is already outside the WPF project. This gives a starting point
  for separate application services and domain code.
- The code reads monetary values as `decimal` and uses `DateOnly` for accounting dates.
- The register calculates balances before it filters recent rows and search results.
- The editor prevents unbalanced entries and repeated submissions while a save is in progress.
  It keeps drafts after a save failure and supports fast keyboard entry.
- The edit and delete code detects source changes made since loading. Keep this protection
  when the storage method changes.
- The delete dialog selects Cancel by default. The account suggestion control preserves
  partially typed text. It also removes subscriptions to owner events when the control unloads.
- Grids use virtualization. Several controls already have accessible names.
- Settings are written to a temporary file. A rename then replaces the previous settings file.
  The previous file is not truncated before the new file is written successfully.

## Findings and Recommendations

Paths in the findings are relative to the source root. `Core/` means
`BeancountViewer.Core/`. UI filenames without a directory are in `BeancountViewer/`.
Line numbers refer to the supplied copy.

Priorities show which items to address during the port. They do not indicate that a failure
was observed in production.

### R01 — Separate UI Control from Accounting and Data Storage (High)

**Evidence**: `BeancountViewer/MainWindow.xaml.cs:54`, `:87`, `:123`, and `:221` control several
operations. These include opening and closing dialogs, loading data, saving settings, building
the account tree, updating balances, and storing data.
`NewTransactionWindow.xaml.cs:154` calls static file writers directly and controls save state.

**Impact**: Direct replacement of these calls with SQL would give windows more responsibilities.
Windows would control database access lifetime, transaction integrity, and connection changes.
Tests for otherwise independent operations would need UI setup.

**Recommendation**: Keep WPF. Add separate view models for the main window, connection manager,
and journal editor. Use application services for journal and account operations. Keep SQL
access in a separate infrastructure component. Keep view-specific behavior in view code.
Examples include focus control and popup position. Use Autofac property injection at the App
composition boundary. Keep Core contracts free of Autofac types and service-location calls.
Add a generic repository hierarchy or
message bus only if a current need justifies it.

**Verification**: Test journal validation and save failures without WPF. Use a small set of
Windows UI workflows to check bindings, commands, focus, and dialogs.

### R02 — Replace the Partial Reader Model with Explicit Accounting Records (High)

**Evidence**: `Core/Ledger.cs:7` stores signed amounts that can be absent, plus raw annotations,
in Posting. At `:9`, Transaction uses a file and line number as its identity. The partial
reader starts at `:122`. `Core/TransactionEditor.cs:12` prevents some edits. These include
entries with prices, costs, unresolved values, or syntax that the editor cannot preserve.

**Impact**: These restrictions protect the source file format. They do not define the required
SQL accounting model. The reader explicitly does not provide a complete booking engine.
Using it as the authority for financial records would make debit, credit, and security
behavior less clear.

**Recommendation**: Use Book, Account, JournalEntry, and JournalEntryLine. Give them typed
amounts, separate Debit and Credit values, stable keys, and explicit security quantities.
Exclude the parser and serializer from the active phase-1 code path. Keep the reference source
for feature and design comparisons. See the [model notes](accounting-model.md) for the proposed
terms and constraints.

**Verification**: Check balanced reference entries for all five accounting classes. Test SQL
integrity for missing accounts, references to another book, invalid debit or credit sides,
and a failure halfway through a save.

### R03 — Define the Database Switching Sequence (High)

**Evidence**: `MainWindow.xaml.cs:27` holds a ledger and file path that can change.
`LoadFile` at `:87` uses a loading flag. It replaces state only after a successful load.
Selection and search handlers read these fields. The design supports one file. It does not
provide a separate database context.

**Impact**: An asynchronous port could display results from the previous connection.
It could also send a pending save to the newly selected connection. After a failed switch,
old records could appear to belong to the requested book.

**Recommendation**: Use a session service to own the active book. Before switching, resolve
unsaved edits and writes. Then release the old database access, clear loaded state, and open
the new context. Cancel obsolete reads or use session identity to reject their results.
Once a write starts, do not get its target from the global profile selection.

Keep connection tests separate from book activation. Close test connections promptly.
If only one connection can be open at a time, close the active book connection before testing
another profile.

**Verification**: Test delayed refresh and search, failed switches, an open editor, and a
disconnect during save. Check that no response or write is applied to a different book.

### R04 — Preserve Atomic Saves and Detect Conflicting Changes (High)

**Evidence**: `Core/TransactionEditor.cs:107` compares a source hash before replacing data.
`Core/TransactionDraft.cs:88` appends data and attempts rollback after a write failure.
`MainWindow.xaml.cs:211` restores selection through source file and line information.

**Impact**: File hashes and offsets cannot identify SQL rows. They also cannot protect a whole
journal entry from concurrent updates. Separate row writes can leave an incomplete,
unbalanced entry visible.

**Recommendation**: Commit the header, lines, and affected security movements in one database
transaction. Use stable entry and line IDs. Use version checks to detect conflicts during
edits, deletes, and status changes. This approach is called optimistic concurrency.
Validate the whole journal entry at the point where data is written.

Define how to check an uncertain write outcome before retrying. Do not automatically repeat
a write after a network error that could have occurred after commit.

**Verification**: Test two editors with old versions, failures during writes, simultaneous
line-status changes, and lost responses. These conditions must not create duplicates,
silently overwrite changes, or leave partial entries.

### R05 — Move Reconciliation to Lines and Preserve All Three States (High)

**Evidence**: `Core/TransactionDraft.cs:8` stores ReconciliationStatus on the entry.
`NewTransactionWindow.xaml.cs:69` maps both Uncleared and the intermediate reconciliation state to `!`.
The new application names that intermediate state Reconciling.
`MainWindow.xaml:75` displays this flag. The register therefore cannot distinguish these states.

**Recommendation**: Store a non-null value with three possible states on each line.
Show the status for the selected account. Calculate a Mixed indication only when the relevant
lines have different states. Add line controls to the details view and editor.
Keep statuses on unchanged line identities. Do not use the entry-level Flag as the authority
for reconciliation status.

**Verification**: Test every pair of states on a transfer after reopening the book.
Marking one account Cleared must not mark the other Cleared. Financial corrections must reset only the
affected reconciled lines.

<!-- Resume Reading -->

### R06 — Separate Accounting Class, Display Group, and Security (High)

**Evidence**: `Core/AccountCategory.cs:4` declares display groups as AccountCategory.
At `:14`, the code guesses groups from words in the account path.
`Core/Ledger.cs:46` inherits explicit groups from ancestor accounts.

**Impact**: Renaming “Checking” can change its display group. These groups are not the five
accounting classes. They do not define asset or liability accounting behavior.

**Recommendation**: Store account type and display group separately. Derive the accounting
class from the type through the agreed catalog. Keep the five accounting classes and support
nested accounts. Model securities as instruments held in asset accounts. Keep quantity separate
from carrying amount. Provide the minimum UI setup needed for a new book.

**Verification**: Renaming must preserve account identity, class, and group. Income and Equity
must remain accessible. Security units must not appear as an extra accounting class or a
foreign monetary balance.

### R07 — Define Monetary and Quantity Precision (High)

**Evidence**: `Core/Ledger.cs:17`, `:85`, and `MainWindow.xaml.cs:19` format values with `N2`.
`TransactionDraft.cs:10` accepts amounts with more than two decimal places. At `:57`, it checks
balance by commodity. `TransactionEditor.cs:16` prevents edits to annotated costs and prices.

**Impact**: The old viewer can show fractional security quantities rounded to two decimal places.
Its commodity parser does not prove full support for securities or currency conversion.
SQL `decimal(10, 2)` can round accepted input or reject it because it exceeds the allowed range.

**Recommendation**: Check book currency, monetary range, and precision before storing data.
Format security quantities separately with sufficient precision. Define explicit operations
for opening holdings and internal transfers. Defer trading and valuation features.
Use decimal totals with enough range. Do not combine monetary balances across books or currencies.

**Verification**: Test fractional shares, the maximum valid monetary amount, and excess decimal
places. Also test totals larger than one line's allowed range and rejection of foreign currency.

### R08 — Use One Book Calendar and Consistent Account-Date Rules (High)

**Evidence**: `MainWindow.xaml.cs:66`, `:128`, `:175`, and `:272` use device-local
`DateTime.Today`. `NewTransactionWindow.xaml.cs:43` does the same.
`Core/Ledger.cs:41` treats an account as closed when its close date equals the current date.
Historical validation in `TransactionDraft.cs:35` permits an entry on its account's close date.

**Impact**: Travel changes date cutoffs and defaults. Account selection and historical editing
use different rules for the close-date boundary.

**Recommendation**: Supply a clock and calendar policy based on the book time zone.
Permit posting dates before the close date. Do not permit dates on or after it.
Use this policy in setup, entry validation, and history views. Keep batch-entry dates only
within the current session.

**Verification**: Test midnight, daylight-saving boundaries, and device time-zone changes.
Test entry dates before, on, and after closure. Test historical edits that keep references
to closed accounts.

### R09 — Reduce Repeated Work and Preserve Full-History Balances (Medium)

**Evidence**: `Core/Ledger.cs:91` scans all entries for each requested account balance.
`MainWindow.xaml.cs:123` requests descendant balances for every account-tree node.
`SearchChanged` at `:171` refreshes synchronously after each keystroke.
`Register` at `Core/Ledger.cs:98` scans and sorts the full history each time.

**Impact**: Work increases with account count and history size. The same approach in SQL could
cause repeated queries and block the UI. This finding comes from source inspection.
No performance failure was measured.

**Recommendation**: Retrieve account balance totals in batches. Query the required register
with its opening balance. Use a stable order based on date and entry identity.
Delay asynchronous search briefly to combine rapid keystrokes, and cancel obsolete searches.
Keep virtualization. Measure performance before adding caches or paging.

**Verification**: Measure the agreed representative book. Check that filters and date windows
still produce the balances calculated from the full history.

### R10 — Use Typed Requests and Specific Validation Errors (Medium)

**Evidence**: `TransactionDraft.cs:8` stores dates and money as strings for serialization.
`NewTransactionWindow.xaml.cs:112` and `:162` identify imbalance messages by an English prefix.
Other field errors can disable Save while showing only a general error.

**Recommendation**: Keep editable text in the view. Convert it to requests with typed values
before passing it to application services. Return validation codes with line and field identity.
Do not determine error type from message text. Show account, currency, date, debit, and credit
errors next to the relevant input. Keep drafts after failure and preserve keyboard behavior.

**Verification**: Invalid accounts, dates, precision, amounts on both sides, and imbalance must
each give a specific error with a clear corrective action. Translated messages must not change
validation behavior.

### R11 — Protect Local Configuration and Connection Diagnostics (High)

**Evidence**: `ViewerSettings.cs:6` stores only LastFile and LastTransactionDate.
Reads at `:14` handle missing, corrupt, and unreadable configuration in the same way, without
notification. Writes replace the file atomically. This is a useful starting point.
`CrashLog.cs:11` writes raw exception text. `MainWindow.xaml.cs:269` logs account paths
synchronously through `CrashLog.RecordAction`.

**Impact**: Adding raw connection strings to settings could store credentials as plaintext.
Current diagnostics could expose private account names or connection details.
File writes run synchronously during selection and rendering.

**Recommendation**: Use versioned user settings, unique friendly names, and credential
protection for the current user. Keep atomic settings updates. Distinguish recoverable file
corruption from a missing settings file on first use. Remove secrets and financial descriptions
before logging information or showing errors. Use short, structured diagnostics.
Avoid disk writes for every UI selection. Do not disable connection encryption or certificate
validation to hide a connection error.

**Verification**: Check that logs and configuration contain no plaintext credentials.
Unavailable credential protection and corrupt settings must produce recoverable errors.
A settings failure must not undo saved entries.

### R12 — Keep Controls Testable and Accessible (Medium)

**Evidence**: `NewTransactionWindow.xaml.cs:72` creates posting rows and keeps controls in a list.
Accessible row names use the row count at creation. Removing and adding rows can therefore
reuse labels. `AccountComboBox.cs:21` creates a custom popup with explicit focus handling.
`MainWindow.xaml:31` provides tab focus cues. The posting-account button template at `:92`
contains only a TextBlock. Its keyboard focus needs a runtime check.

**Recommendation**: Use a template to bind line view models to controls. Give controls stable
automation IDs. Update accessible row numbers consistently after row changes.
Keep the useful account-picker behavior. Before replacing it with a dependency, check focus,
popup behavior, screen-reader support, and keyboard interaction. Check high-DPI sizing and
long split entries.

**Verification**: Run automated Windows smoke tests for create, edit, reconcile, and switch
workflows. Also run manual keyboard, high-DPI, and focus checks. Domain tests without a UI
do not establish desktop usability.

### R13 — Classify Recoverable Database Errors (Medium)

**Evidence**: `App.xaml.cs:12` logs unhandled dispatcher exceptions and shuts down.
`NewTransactionWindow.xaml.cs:183` catches a list of file-related exceptions.

**Impact**: Unchanged handlers could send expected database errors to the global fatal-error
handler. Examples include timeouts, authentication failures, and concurrent-update conflicts.

**Recommendation**: At the service boundary, convert expected infrastructure errors into
application outcomes. Keep drafts and provide recovery for known failures.
Keep the global handler for unexpected failures, with sensitive details removed.
Use asynchronous database operations. Limit resource lifetime and release resources when
operations finish. Do not wrap blocking SQL calls in Task.Run as a substitute.

**Verification**: Connection loss, denied writes, timeouts, conflicts, and unexpected exceptions
must follow distinct handling paths. Each path must accurately report save status.

### R14 — Establish Build and Test Evidence Missing from the Supplied Copy (Medium)

**Evidence**: Both supplied projects target .NET 10. The WPF project references Core and declares
`BeancountViewer.WindowsChecks` as a friend assembly. The supplied copy has no tests, SDK pin,
nullable settings, or settings for the full solution.

**Recommendation**: During implementation, add the SDK and build settings required by the
constitution. Add nullable analysis, deterministic service tests, real SQL Server integration
tests, and a small Windows automation suite. Review the dependencies introduced by test tools
and packages. Keep the Fluent icon license with redistributed assets. The project already
copies that license to its output.

**Verification**: Run restore, build, and automated checks on compatible runners.
Identify Windows runtime checks separately. Missing tests in this copy do not prove that
the original project has no tests.

## Suggested Structure for the Implementation Plan

Start with the existing two-project separation. A practical target has a WPF presentation
project, a UI-independent domain and application project, and a SQL and settings infrastructure
project. Add separate test projects. Split domain and application code into further projects
only if that provides a concrete benefit at this scale. This structure is a recommendation.
It does not require a fixed number of projects.

Start implementation with connection and session control, then schema initialization.
Add journal services and real persistence tests before connecting the editor to them.
Move the account tree and register to view models. Reuse the XAML styles.
Add line reconciliation, minimum account and security setup, and focused Windows UI tests.
Use the parity matrix throughout the port to preserve existing features.

Avoid a framework rewrite, distributed services, and a generic plug-in layer.
Do not import the full file parser only to support the SQL UI.
The source is small enough for a focused refactor. A second application built around
incompatible models is unnecessary.

## Review Disposition

R01–R08 and R11 address phase-1 integrity and separation of responsibilities.
R09, R10, R12, R13, and R14 address performance, usability, recovery, and acceptance evidence.
The implementation plan must address each finding. For a deferred finding, record the reason
and remaining risk. These findings do not imply that all source behavior is defective or
needs replacement.
