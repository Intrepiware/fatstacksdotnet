# Feature Specification: Phase 1 — SQL Desktop Foundation

**Feature Branch**: `001-sql-desktop-foundation`

**Created**: 2026-11-14

**Status**: Tasked — ready for implementation
**Governing constitution**: [Version 3.0.0](../../.specify/memory/constitution.md)
**Decision history**: [Decisions ledger](decisions-ledger.md)

**Input**: Establish Fat Stacks.NET on SQL Server, reuse the existing Windows UI with practical
feature and visual parity, save named database connections, use separate debit and credit
columns, reconcile journal entry lines, support all five accounting classes and security
holdings, and review the existing code. Start with new databases, defer Beancount import, and use one
currency per book. Reject other currencies at line entry.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Connect to the Right Book (Priority: P1)

As the owner, I want to save named database connections and open one book at a time so that I
can work with several sets of accounts without mixing their records.

**Why this priority**: Every financial workflow depends on a clearly identified, usable book.

**Independent Test**: Use two empty test databases and one unreachable connection. Initialize
one book in each database, switch between them, and restart the application.

**Acceptance Criteria**:

- Saving profiles named “Personal” and “Test” makes both available after restart (FR-001).
- After opening “Personal” and then successfully switching to “Test,” restarting automatically
  opens Test without another profile selection. Renaming Test preserves this behavior (FR-003).
- Testing Personal or failing to switch to an unreachable profile does not replace Test as
  the remembered startup profile (FR-003).
- On first launch, or when the remembered profile is removed or cannot be opened, the
  connection dialog appears. No other database opens and no schema is created automatically.
  The user can cancel a pending startup attempt and choose a connection (FR-003).
- The connection dialog accepts server, database, authentication details, and a friendly name;
  it reports whether the supplied connection can be used without exposing a password (FR-002).
- Opening “Personal” shows its name and book identity. Switching to “Test” clears Personal's
  accounts, selections, and details before displaying Test's records (FR-003).
- A failed switch leaves the application disconnected with a usable connection dialog and no
  writable records from the previous book. Cancelling before a switch begins leaves the
  current book open (FR-003, FR-004).
- Attempting to switch with unsaved edits offers save, discard, or cancel. A running write
  finishes or reports its outcome before a switch can proceed (FR-004).
- Initializing an empty database creates a book with a chosen currency and time zone. Repeating
  initialization preserves existing records. An unrelated or incompatible database is rejected
  without alteration (FR-005).
- Renaming or removing a saved profile changes only local configuration. Removing a profile
  does not delete its database; an active profile must be disconnected before removal (FR-001).
- Renaming Personal or changing its server details preserves its profile ID. Two independently
  initialized books have different Book IDs (FR-001, FR-005).
- Repointing Personal to Test's database is rejected because its expected Book ID differs.
  Personal can target Test only after explicit reassociation; no financial record is changed
  by that action (FR-005).
- A configuration save failure is shown as a failure to remember the profile, not a successful
  save. A valid current connection remains usable (FR-006).

### User Story 2 - Set Up Accounts and Browse the Familiar Register (Priority: P1)

As the owner, I want to create my accounts and use the familiar account rail, tabs, register,
search, and details to inspect my book.

**Why this priority**: A fresh database needs account setup, and the existing interface is the
accepted starting point for phase 1.

**Independent Test**: Use a book with all five account classes, nested accounts, closed accounts,
and known entries before and within the default 90-day window.

**Acceptance Criteria**:

- An empty book allows creation of accounts in Assets, Liabilities, Equity, Income, and Expenses.
  A security-holding asset account is available without creating a sixth accounting class
  (FR-007, FR-008).
- Renaming an account preserves its history. Closing it hides it from new-entry choices while
  retaining its recorded balances and historical detail links (FR-008).
- The account tree uses Banks, Credit Cards, Investments, Loans Payable, and Other tabs in
  that order. The Other tab groups accounts by type, including Expenses, Income, and Equity.
  A renamed account remains in its explicitly selected group (FR-007, FR-009).
- Creating a Bank account selects Assets/Banks; Credit Card selects Liabilities/Credit Cards;
  Loan Payable selects Liabilities/Other and appears in Loans Payable; Loan Receivable
  selects Assets/Other and appears in Other. The account
  setup shows the resulting class, and each remaining type follows the documented matrix
  (FR-007).
- Overriding a loan account's display group changes its placement without changing its type,
  class, debit/credit behavior, or existing entries (FR-007, FR-024).
- The register opens to the last 90 calendar days through today in the book time zone. Its
  balances include earlier entries, including opening-balance entries (FR-010, FR-011).
- Searching by description or counter-account narrows visible rows without changing the
  balances attached to the remaining rows (FR-011).
- Selecting an entry displays every line, and following a counter-account link opens that
  account with the same entry selected when it falls in the current date window (FR-012).
- In a bank register, a 25.00 credit appears as Payment 25.00 and a 100.00 debit as Deposit
  100.00. A credit-card purchase appears as Payment in the card register; a card repayment
  appears as Deposit. Entry details retain the actual debit/credit lines (FR-009).
- Refreshing after a change updates balances while preserving the selected account and entry
  when those records still exist (FR-012).

**What Must Not Change**:

- The interface MUST retain the account rail, split layout, dense alternating-row register,
  details area, visual hierarchy, and keyboard workflows identified in the parity baseline.
- Search MUST continue to match descriptions and counter-account names without altering the
  selected account's underlying running balance.

### User Story 3 - Maintain Balanced Journal Entries (Priority: P1)

As the owner, I want to create, edit, and delete entries using the familiar dialogs while
keeping the book balanced and preserving entered work when a save fails.

**Why this priority**: Phase 1 must provide a usable accounting application, not only a viewer.

**Independent Test**: Enter an opening balance, an expense, and a split entry in a fresh book;
edit and delete them, restart, and compare balances with known expected results.

**Acceptance Criteria**:

- Saving a USD entry with a 25.00 debit to Expenses and a 25.00 credit to a bank asset produces
  two persisted lines and the expected account balances after reopening (FR-013–FR-015).
- A line with both a debit and credit, a negative amount, no nonzero amount, or a mismatched
  currency is rejected with a field-specific explanation (FR-014, FR-020).
- A 25.00 debit with a 24.99 credit cannot be saved. No partial entry is visible after a failed
  save, edit, or delete (FR-015, FR-017).
- A user can add and remove split lines, select accounts through keyboard suggestions, and
  save using Ctrl+Enter. Continue saves one entry and clears it for the next while retaining
  the entered date within that entry session (FR-013, FR-016).
- A failed save preserves the draft. A repeated Save action during a pending save creates no
  duplicate entry (FR-016, FR-017).
- Editing an existing entry preserves its identity and unaffected line statuses. A conflicting
  external change is reported without overwriting the newer version (FR-017).
- Delete shows the date, description, and affected lines with Cancel focused by default.
  Confirming removes the whole entry's effects; cancelling changes nothing (FR-018).
- A new entry session defaults to today in the book time zone. Changing the device time zone
  does not change stored accounting dates or the default book date (FR-019).
- Values outside the supported monetary range or precision are rejected before any write;
  they are not silently rounded or truncated (FR-014).

- Payee and description text containing apostrophes or SQL-like commands is saved as literal
  text without executing commands or changing unrelated records (FR-028).

### User Story 4 - Reconcile Each Account Line (Priority: P1)

As the owner, I want to mark individual account movements Uncleared, Reconciling, or Cleared
without changing the status of the other side of an entry.

**Why this priority**: Transfers can clear in different accounts at different times.

**Independent Test**: Use a transfer with two account lines and an entry with multiple lines
for the same account. Change their statuses independently and reopen the book.

**Acceptance Criteria**:

- New lines start Uncleared. Each line can be changed to Reconciling or Cleared and can be
  explicitly corrected back to either other state (FR-021).
- Marking the bank line Cleared leaves the credit-card line Uncleared after refresh and restart
  (FR-021, FR-022).
- An entry with multiple lines for the selected account shows each line's status in its details.
  A register summary identifies mixed statuses without introducing a fourth stored status
  (FR-022).
- Changing only reconciliation status leaves dates, amounts, account assignments, and balances
  unchanged (FR-021).
- Editing a Cleared line's account, monetary values, or security quantity resets that line to
  Uncleared and tells the user. Unchanged lines retain their statuses (FR-023).

### User Story 5 - Keep One Currency per Book and Track Basic Securities (Priority: P2)

As the owner, I want a simple book currency and a place for security holdings without taking
on exchange-rate calculations or a trading system in phase 1.

**Why this priority**: The foundation must represent these assets correctly while keeping the
initial workflows small.

**Independent Test**: Use one USD book and one EUR book. In the USD book, record a security's
opening quantity and carrying amount and transfer part of that holding between accounts.

**Acceptance Criteria**:

- A new book defaults to USD and may instead be initialized in another supported currency.
  The chosen currency remains visible in entry and balance views (FR-020).
- Entering a EUR line in a USD book is rejected. No conversion or mixed-currency total is
  produced. Switching to a EUR book allows EUR entries (FR-020).
- After the first journal entry is saved, changing the book currency is rejected (FR-020).
- An opening holding of 1.234567 shares with a USD 100.00 carrying amount preserves both
  values distinctly. Its monetary offset is an Equity line, and its quantity is not presented
  as USD 1.23 (FR-024, FR-025).
- A transfer of 0.234567 shares with a USD 20.00 carrying amount between two holding accounts
  preserves total units and total carrying amount, and retains a balanced journal entry
  (FR-025).
- An unsupported trade, conversion, price lookup, or gain calculation is not offered as a
  working capability. Entered carrying amounts are not labelled market values (FR-026).

### Edge Cases

- Missing server, wrong credentials, insufficient permissions, unavailable database, and a
  disconnect during save: show actionable, redacted errors and retain the draft without
  claiming an unconfirmed save succeeded.
- Switching while search or refresh is pending: results from the previous book cannot appear
  in the new book or receive writes.
- Duplicate profile names, corrupt configuration, or unavailable credential protection:
  reject ambiguous names and preserve recoverable configuration; explain what was not saved.
- Existing unrelated schema, newer schema version, interrupted initialization, or a profile
  pointing to a different book than expected: refuse unsafe access and explain recovery.
- Multiple lines for one account, equal-date entries, split entries, and search hiding earlier
  activity: stable ordering and complete history determine balances.
- Closed accounts and renamed ancestors: retain valid historical references and prevent
  cyclic account trees or cross-book references.
- Zero, negative, excessive-precision, and out-of-range monetary amounts: reject invalid input.
- Foreign currency labels or security symbols entered into a monetary currency field:
  reject mismatches; security quantities are separate from money.
- Date boundaries, daylight-saving transitions, and travel: the book's calendar determines
  defaults and register cutoff; stored date-only values remain unchanged.
- Security transfers exceeding the available quantity or carrying amount: reject the transfer;
  short positions and automatic valuation are outside phase 1.
- Editing a line after reconciliation or deleting a reconciled entry: disclose affected statuses
  and apply the documented correction/delete behavior without leaving partial effects.

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: The application MUST let the user add, rename, edit, select, and remove saved
  database connection profiles with unique, nonempty friendly names. Profiles and their
  connection strings MUST persist in the current user's configuration. Removal MUST NOT
  delete a database.
  Each profile MUST have a unique, stable identifier separate from its friendly name.
  Renaming the profile or editing its connection details MUST NOT change that identifier.
  Identifier format is deferred to technical planning or implementation.
- **FR-002**: A connection dialog MUST support server and database selection by name, Windows
  integrated authentication or database username/password authentication, testing a connection,
  and opening it. Stored credentials MUST be protected for the current user and redacted from
  routine displays and logs.
- **FR-003**: Exactly one book/database context MAY be active at a time. The current friendly
  name and book identity MUST be visible. Switching MUST discard prior loaded data and prevent
  previous operations from updating the new context.
  After a profile successfully opens a compatible book, the application MUST remember its
  stable profile identity in the user's local configuration. On the next launch, it MUST
  automatically attempt to open that profile and book. Testing a connection or attempting
  an unsuccessful switch MUST NOT replace the remembered profile. Renaming a profile MUST
  preserve this preference. If no remembered profile exists, it was removed, credentials
  cannot be used, or opening fails, startup MUST show the connection dialog. It MUST NOT
  silently select another database or initialize a schema. The attempt MUST keep the UI
  responsive and permit cancellation; cancelling MUST leave the connection dialog available.
- **FR-004**: Switching or disconnecting MUST resolve unsaved edits and outstanding writes first.
  A failed switch MUST end disconnected; cancelling before the switch MUST preserve the
  current context. Failed connections MUST remain recoverable through the dialog.
- **FR-005**: Phase 1 MUST provide a repeatable schema initialization process for an explicitly
  selected empty database and one book within it. Initialization MUST set the book name,
  currency, and time zone. It MUST detect existing compatible, incompatible, or unrelated
  schemas and MUST NOT destroy or silently replace existing data.
  Give each book a permanent logical identifier. When a saved connection profile first opens a book successfully, save that identifier in the user’s configuration. On later opens, verify that the profile still points to the same book before displaying financial records.
  If the identifier differs, refuse to open the book and explain why. Allow the user to explicitly confirm that the profile should target the different book. Changing the connection string alone must not count as that confirmation.
- **FR-006**: Configuration failures MUST be reported without claiming profiles were saved.
  A failed preferences write MUST NOT undo a successful accounting operation.
- **FR-007**: Accounts MUST support Assets, Liabilities, Equity, Income, and Expenses.
  UI groups MUST be separate from these accounting classes. All classes MUST be reachable
  through the sidebar, with Income and Equity available under Other. The sidebar routes
  stored Banks, Credit Cards, and Investments groups to their matching tabs. It routes
  Loan Payable accounts in the stored Other group to Loans Payable. The Other tab shows
  the remaining stored Other and Expenses accounts in sections by account type.
  At creation, users MUST select an account type: Bank, Cash, Credit Card, Loan Receivable,
  Loan Payable, Investment/Brokerage, Other Asset, Other Liability, Equity, Income, or Expense.
  The type MUST determine the accounting class and default UI group according to the
  [account-type behavior matrix](accounting-model.md#account-types-and-phase-1-ui-behavior).
  Users MAY override the display group without changing the type or accounting class.
  Names MUST NOT determine account type or behavior.
- **FR-008**: Users MUST be able to create, rename, group, and close hierarchical accounts.
  Accounts MUST have stable identity, unique paths within a book, no hierarchy cycles, and a
  defined accounting class. Closed accounts MUST retain history and be excluded from new-line
  selection. Historical edits MAY retain an already referenced closed account when the entry
  date precedes its closure; they MUST NOT add new references to it.
- **FR-009**: Phase 1 MUST retain the source UI capabilities and design elements marked Retain
  or Adapt in [the parity baseline](ui-parity.md). Each material departure MUST be documented
  with its reason and verification consequence before acceptance.
  The account register MUST retain Payment and Deposit columns: net credits to the selected
  account appear as positive Payments and net debits as positive Deposits. Zero net movements
  leave both blank. Tooltips MUST explain this mapping. Entry details and the editor MUST
  retain explicit Debit/Credit fields; persisted accounting values MUST NOT change.
- **FR-010**: The default register MUST show the selected account's entries from today minus
  89 days through today in the book time zone. Current and running balances MUST include all
  earlier applicable journal activity. Tree totals MUST include descendants without losing
  closed accounts' financial contributions.
- **FR-011**: Search MUST match descriptions and counter-account names case-insensitively.
  Filtering MUST NOT change the running balances of retained rows. Equal-date entries MUST
  have a deterministic order that survives refresh and restart.
- **FR-012**: Entry details MUST show every line and allow navigation to its account, including
  historical closed accounts. Refresh MUST preserve valid selection by stable identity.
- **FR-013**: Users MUST be able to create and edit balanced entries with an accounting date,
  payee, description, and two or more lines. The editor MUST support adding/removing split
  lines, account suggestions, and separate debit and credit fields.
- **FR-014**: Every monetary line MUST contain nonnegative debit and credit amounts with exactly
  one positive side and the other zero. Invalid range, currency precision, or column precision
  MUST be rejected explicitly at application-service input boundaries and before final values
  are persisted, before conversion can lose precision or range. Value-preserving trailing
  zeros MUST be accepted. Internal decimal calculations MAY retain extra precision; checks
  after every assignment, custom money types, and duplicate SQL numeric parsing are not
  required. Account references MUST belong to the active book.
- **FR-015**: Saving an entry MUST require equal total debits and credits in the book currency.
  Creation and amendment MUST persist the whole balanced entry atomically. Opening monetary
  balances MUST be represented by balanced journal entries, not independent balance overrides.
- **FR-016**: The editor MUST retain drafts on failures and prevent duplicate submissions during
  a pending save. Continue MUST save and start another entry with the date retained for that
  session. Cancel MUST NOT save the current draft.
- **FR-017**: This requirement applies only to JournalEntry and JournalEntryLine operations.
  Edits, deletes, and reconciliation updates to these records MUST detect stale versions and
  report conflicts without overwriting newer data. Failed journal operations MUST leave no
  partial effects. Account, book, security-definition, and connection-profile operations are
  outside this requirement.
- **FR-018**: Deleting an entry MUST require a confirmation showing its date, description, and
  lines. Cancel MUST be the safe default. Successful deletion MUST remove the JournalEntry and all its
  JournalEntryLine records, including all balance effects, in one atomic database transaction. The exact physical retention mechanism belongs in the implementation plan.
- **FR-019**: Accounting dates MUST preserve date-only meaning. New-session dates and the
  register cutoff MUST use the configured book time zone. Operational timestamps MUST use UTC.
  An optional bank posting date MUST remain separate from the journal accounting date.
- **FR-020**: Each book MUST have exactly one currency, defaulting to USD at setup. All monetary
  lines MUST use that currency; mismatches MUST produce an error. The currency MUST be locked
  after the first saved entry. Separate books MAY use different supported currencies, but no
  exchange conversion or mixed-currency journal entry is included in phase 1.
- **FR-021**: Each journal entry line MUST have exactly one reconciliation status: Uncleared,
  Reconciling, or Cleared. New lines MUST default to Uncleared. Explicit status changes MUST be
  independent of other lines and MUST NOT change financial values.
- **FR-022**: The UI MUST expose line reconciliation and show a summary based only on the
  selected account's lines. When those statuses differ, the summary MUST indicate mixed
  statuses and expose the individual values; mixed is not a stored reconciliation state.
- **FR-023**: Changing a reconciled line's account, money, currency, or security quantity MUST
  reset that line to Uncleared with a visible explanation. Unchanged lines MUST retain status.
- **FR-024**: The application’s code and database MUST represent accounting class, account type, UI group, currency, and security as separate concepts. The documentation MUST list all five accounting classes and the supported account types. It MUST explain that securities are instruments held within accounts, not a sixth accounting class.
- **FR-025**: Phase 1 MUST support security definitions, opening quantities with explicit book-
  currency carrying amounts, and same-security transfers between holding accounts in the
  same book. Fractional quantities MUST retain their declared precision independently of
  monetary precision. Transfers MUST preserve total quantity and carrying amount and MUST
  NOT exceed the recorded holding. Entries involving securities MUST have equal monetary debits and credits in the book currency. Security quantities MUST be recorded separately.
- **FR-026**: Automated security pricing, trades, lot selection, realized/unrealized gains,
  corporate actions, short positions, and currency conversion MUST remain outside phase 1.
  The UI MUST NOT imply these features exist or label carrying amounts as market valuations.
- **FR-027**: The supplied UI and core source MUST receive a documented architecture/code review
  with source references, priorities, practical recommendations, and validation limits. The
  plan MUST address the phase-1 recommendations or record justified deferrals.

- **FR-028**: Runtime database access MUST use EF Core query APIs or parameterized SQL commands,
  including stored-procedure calls. User-supplied values MUST NOT be concatenated into SQL text.
  Raw SQL issued through EF Core MUST also use parameters for user-supplied values. Normal
  application access MUST use a non-administrator database identity with no schema-change
  permissions. Schema initialization and migration MUST use separate explicit maintenance
  privileges, not the normal runtime identity's permissions.

- **FR-029**: Logs MUST NOT contain sensitive financial data, passwords, account numbers,
  authentication tokens, or connection strings containing secrets. This prohibition applies
  to application diagnostics, database command/parameter logging, and exception output.
  Diagnostic identifiers and error categories MAY be logged only when they do not expose
  sensitive values. EF Core sensitive-data logging MUST remain disabled.

- **FR-030**: While the desktop application remains single-user, connecting to SQL Server
  without TLS encryption, or with a self-signed or expired certificate, MUST show a visible
  non-blocking warning identifying the condition. These conditions alone MUST NOT prevent
  connection or require confirmation. Use TLS when available. Authentication failures and
  other errors that prevent SQL access MUST still be reported; this policy does not guarantee
  that an otherwise unusable connection can succeed.
- **FR-031**: While the desktop application remains single-user, it MUST NOT implement or
  require backup management, disk/database/field encryption, formal data-classification
  infrastructure, encryption-key management, or an encrypted logging subsystem. Operators
  MAY manage backups and storage protection independently. Saved-credential protection,
  FR-029 log restrictions, and documentation of migration/recovery effects remain required.
  Before multi-user access is introduced, these exceptions MUST receive a security review.

- **FR-032**: Before activating a database, the application MUST compare its expected schema
  version with the stored database schema version. A mismatch or missing/unreadable version
  MUST produce a fatal connection error and leave the application disconnected. The connection
  dialog MUST remain available. This check MUST NOT initialize, upgrade, or select another
  database automatically. A lightweight exact-version comparison is sufficient; custom
  manifests, object inventories, schema-lock protocols, and automatic migration recovery
  are outside scope.

- **FR-033**: Each stored procedure and SQL view MUST have one current SQL definition file
  in the grate package. Git attributes MUST disable automatic content merging for these files
  using `-merge`, while preserving readable text diffs. If both branches change the same file,
  Git MUST leave a conflict requiring deliberate resolution, including non-overlapping edits.
  Review the combined definition before accepting it. This does not detect separate files
  that redefine the same object or replace grate ownership, immutable applied one-time
  migrations, or consistency between current definitions and the migrated schema.

### Key Entities _(include if feature involves data)_

| Entity            | Meaning and principal relationships                                                                                                               |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| ConnectionProfile | Stable profile ID, user-local friendly name, database connection string, protected credentials, and expected Book ID; not a financial record.     |
| Book              | One set of accounts in one database, with stable identity, name, currency, and time zone.                                                         |
| Account           | A stable record within a book, with a parent, name, selected account type, type-determined accounting class, display group, and open/close dates. |
| JournalEntry      | One balanced accounting event with a stable identity, accounting date, optional bank posting date, payee, and description.                        |
| JournalEntryLine  | One account's debit or credit within an entry, ordered within it, with independent reconciliation status and optional security movement.          |
| Currency          | The book's monetary unit and permitted fractional precision; USD is the default.                                                                  |
| Security          | A named instrument with a symbol and quantity precision; its units are distinct from money.                                                       |
| SecurityHolding   | An account/security position derived from recorded quantity movements and monetary carrying amounts; not an unrelated balance override.           |

Book ID in user-facing requirements means the logical book identity, Book.StableIdentifier.
It is separate from the database-local bigint BookId. Profiles pin the logical identity.

Use `JournalEntry` and `JournalEntryLine` as the proposed domain names. Keep “transaction”
in familiar UI labels where useful. Reserve “database transaction” for the atomic persistence
operation. A second business entity named Transaction is unnecessary in phase 1.

### Scope and Required Constraints

The phase-1 deliverable includes schema creation, a usable Windows application wired to that
schema, named connection management, minimal book/account/security setup, journal maintenance,
line reconciliation, and evidence of UI parity. This command defines that work; it does not
implement the schema or application.

The owner requires .NET 10 and SQL Server, separate persisted debit/credit columns on journal
entry lines, and a non-null three-state reconciliation representation. The proposed storage
mapping is `tinyint`: 0 = Uncleared, 1 = Reconciling, 2 = Cleared; a nullable Boolean is prohibited.
The constitution requires service-layer `decimal`, default database money `decimal(10, 2)`,
accounting dates as `DateOnly`/`date`, and UTC operational timestamps. These are supplied
constraints, not implementation choices inferred by this specification. Detailed schema design
and justified precision exceptions belong in the plan; [model notes](accounting-model.md)
record the implications for review.

The constitution also requires Autofac property injection and hybrid EF Core/stored-procedure
access. All application procedure calls belong in one service or repository. Stored-procedure
write paths need documented reasons. Grate owns all database definitions and schema changes;
EF migrations and EF schema creation are prohibited. Application-owned primary keys default
to bigint, small enum keys use tinyint, and primary/foreign keys use single columns. Combined
uniqueness uses separate UNIQUE constraints. A uniqueidentifier needs a specific documented
purpose. Date and timestamp column names omit Utc; persisted timestamps remain UTC. Grate
internal history keeps its own conventions. The plan defines mappings and dependency lifetimes.

Documents must follow the constitution's clear-writing and approximate ASD-STE100 guidance
without losing meaning. Record changes in the feature decisions ledger. Active documents
describe the current state and retain its rationale; change history belongs in the ledger.

Scope excludes Beancount import/export and file editing, a web UI, automatic bank feeds,
statement-matching workflows, loan amortization schedules, credit-card statement management,
consolidated books, multiuser administration, and server
installation. Selecting the three reconciliation states is included; a full statement
reconciliation wizard is not. Existing databases must be reachable with sufficient permissions;
application schema initialization is included, SQL Server provisioning is not.

### Future API and Command-Line Targeting

Phase 1 establishes the profile and book identities needed by later automated access.
It does not implement an API or command-line database operations.

- A future command-line interface can accept a friendly profile name for interactive use
  and the stable profile ID for scripts. Renaming a profile must not break ID-based targeting.
  Connection strings and passwords must not be required as command-line arguments.
- A future API must resolve authorized Book IDs through connections configured on its own host.
  Desktop user-local profiles are not automatically available to the API host.
- Future automated writes must require an explicit target. They must not default to the last
  connection used by the desktop application. Target selection does not replace authorization.
- Phase-1 profile and book identifier formats are defined in the data model. Command syntax,
  API routes, public API identifiers, host configuration, and authorization design are deferred
  until those interfaces are planned.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: In acceptance runs with two books, every switch and restart opens the selected
  book with zero records or writes leaking from the other book.
- **SC-002**: Every Retain/Adapt row in the parity baseline has passing behavioral evidence
  and, where visual, a recorded desktop comparison or an explicitly accepted departure.
- **SC-003**: All reference cases for opening balances, expenses, income, liabilities, splits,
  edits, deletion, and security opening/transfers produce exact expected amounts and quantities.
- **SC-004**: Every invalid or fault-injected write case leaves zero partial entries and zero
  unintended duplicates from repeated submissions during a pending save; stale-version
  conflicts are distinguishable from other failures.
- **SC-005**: Every pair of different reconciliation states on a two-account entry survives
  refresh and restart independently, with no balance change caused by status-only updates.
- **SC-006**: The owner can create a connection profile, open a prepared empty database,
  create accounts, and save a balanced entry using the application and setup guide without
  manually editing configuration or financial records.
- **SC-007**: Across the supported currencies used in acceptance, 100% of mismatched-currency
  lines are rejected and no quantity or monetary precision is silently lost.
- **SC-008**: Main-window account selection, search, and register refresh each finish within
  two seconds in at least 19 of 20 runs on an agreed Windows test machine with 100 accounts
  and 10,000 entries. This is a proposed acceptance target, not a measured result; record
  hardware and database placement with the measurements.

## Assumptions

- Phase 1 starts with new databases and does not import Beancount data.
- Each book uses one currency.
  USD is the default; alternative currencies with up to two fractional digits are the initial
  supported scope. Higher-precision currencies require a documented schema extension.
- One database contains one book in phase 1. Multiple saved profiles provide access to multiple
  books; simultaneous cross-book work is excluded.
- WPF is the practical UI baseline because the supplied application already uses it on .NET 10.
  A framework migration is not needed to meet the requested phase-1 behavior.
- Basic security support means explicit opening quantities/carrying amounts and internal
  transfers. General trading and automatic valuation are deferred.
- Entries save directly as posted balanced entries. A separate persisted draft/approval workflow
  and formal period closing are deferred; edit and delete remain available for UI parity.
- The prior application's 90-day register scope is retained; balances use full stored history.
  A configurable history-range selector is deferred.
- Remember the last-entered date within the active entry session. New sessions default to
  book-local today.
- Minimal account/security setup is included because a fresh book cannot otherwise be used
  without manual database edits. Broad administration screens are outside scope.
- The source review is static. Windows execution, screenshots, UI automation, database testing,
  and performance measurements remain implementation validation work.
