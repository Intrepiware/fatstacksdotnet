# Decisions Ledger: Fat Stacks.NET — SQL Desktop Foundation

## 2026-11-15 13:20:23 -06:00: Limit FR-017 to journal entries and lines; clarify atomic deletion

**Files Updated:** [spec.md](spec.md), [contracts/application-services.md](contracts/application-services.md), [data-model.md](data-model.md), [research.md](research.md), [plan.md](plan.md), [contracts/desktop-ui.md](contracts/desktop-ui.md), [quickstart.md](quickstart.md), [accounting-model.md](accounting-model.md), [checklists/requirements.md](checklists/requirements.md), [decisions-ledger.md](decisions-ledger.md).

Limit stale-version checks and durable uncertain-outcome recovery to JournalEntry and
JournalEntryLine operations. Preserve journal receipts, pending-operation recovery, and line
reconciliation conflict handling. Remove setup receipts, setup expected-version requirements,
and protected EF metadata recovery records from the current design. Account, book, and security
setup keep ordinary validation, atomic transactions, and cross-record locks where needed.

FR-018 explicitly deletes the journal entry and every line in one atomic transaction. This
clarifies an existing atomic-delete requirement. No runtime implementation or test was changed.

---

## 2026-11-15 10:37:50 -06:00: Align active feature documents with constitution 1.1.0

**Files Updated:** [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md), [accounting-model.md](accounting-model.md), [contracts/application-services.md](contracts/application-services.md), [contracts/desktop-ui.md](contracts/desktop-ui.md), [architecture-review.md](architecture-review.md), [ui-parity.md](ui-parity.md), [quickstart.md](quickstart.md), [checklists/requirements.md](checklists/requirements.md), [decisions-ledger.md](decisions-ledger.md).

Apply the constitution's technology constraints, naming and key conventions, dependency
injection boundaries, and document rules to this feature. Refresh the compliance review and
validation guide. Document reasons for every stored-procedure write path. Clarify that logical
book identity is separate from the local numeric key and that identifier formats are defined
for phase 1, while API/CLI contract design remains deferred.

Keep current rationale in research and planning documents. Move requirement-change narratives
and follow-up notes into this ledger. Preserve source findings, migration guidance, and limits
of verification evidence. This is a documentation change; no runtime test result is claimed.

---

## 2026-11-15 10:37:50 -06:00: Backfill: use Autofac property injection

**Files Updated:** [plan.md](plan.md), [research.md](research.md), [quickstart.md](quickstart.md).

Backfill recorded at the datetime above. The original change datetime could not be
ascertained. File links identify the affected current documents, not an exact edit chronology.

Select Autofac property injection in place of manual dependency wiring. Keep registration and
resolution at the App composition boundary, with mandatory properties supplied before use.
Define application, workflow, and operation lifetimes and disposal. Core contracts remain plain
C# without Autofac types. This records the owner's dependency-injection preference.

---

## 2026-11-15 10:37:50 -06:00: Backfill: rename the intermediate reconciliation state

**Files Updated:** [spec.md](spec.md), [data-model.md](data-model.md), [accounting-model.md](accounting-model.md), [contracts/desktop-ui.md](contracts/desktop-ui.md), [architecture-review.md](architecture-review.md), [quickstart.md](quickstart.md).

Backfill recorded at the datetime above. The original change datetime could not be
ascertained. File links identify the affected current documents, not an exact edit chronology.

Rename Clearing to Reconciling. Keep Uncleared and Cleared unchanged. Preserve the non-null
tinyint mapping 0, 1, 2, the Uncleared default, and line-level reconciliation. This is a naming
change, not a fourth state or a change to accounting effects.

---

## 2026-11-15 10:37:50 -06:00: Backfill: use numeric relational keys and date names without Utc

**Files Updated:** [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md), [accounting-model.md](accounting-model.md), [contracts/application-services.md](contracts/application-services.md), [quickstart.md](quickstart.md).

Backfill recorded at the datetime above. The original change datetime could not be
ascertained. File links identify the affected current documents, not an exact edit chronology.

Replace general GUID record keys with bigint identities and use tinyint for small enum tables.
Use single-column primary and foreign keys with separate combined UNIQUE constraints. Name
UTC timestamp columns CreateDate, UpdateDate, and RecordDate. Retain GUIDs only for specific
uses: logical book identity, command correlation before key allocation, and local profile IDs.

Database-assigned IDs require create results and receipts to return assigned keys. Recover a
pending create by OperationId without requiring its unreceived target ID. JournalEntryId also
provides deterministic equal-date ordering, without a second entry sequence column.

---

## 2026-11-15 10:37:50 -06:00: Backfill: select hybrid EF Core and procedure access with grate ownership

**Files Updated:** [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md), [accounting-model.md](accounting-model.md), [contracts/application-services.md](contracts/application-services.md), [quickstart.md](quickstart.md), [checklists/requirements.md](checklists/requirements.md).

Backfill recorded at the datetime above. The original change datetime could not be
ascertained. File links identify the affected current documents, not an exact edit chronology.

Use EF Core for reads and ordinary atomic metadata writes. Use stored procedures for guarded
accounting writes without forcing complex operations through EF. Put all application procedure
calls in one repository. Financial procedures retain aggregate validation and operation receipts;
EF metadata updates use concurrency checks and read-back/review for uncertain outcomes.

Select grate instead of the earlier DbUp proposal. Require grate for all database definitions
and schema changes, including tables. The owner's final decision removes the earlier option
of EF table migrations. Keep current definitions alongside immutable migration scripts so
concurrent changes meet in the same object definition and deployed schemas can be checked.

---

## 2026-11-15 10:37:50 -06:00: Backfill: protect uncertain operations and immutable session routes

**Files Updated:** [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/application-services.md](contracts/application-services.md), [quickstart.md](quickstart.md).

Backfill recorded at the datetime above. The original change datetime could not be
ascertained. File links identify the affected current documents, not an exact edit chronology.

Require terminal NotCommitted records when outcome resolution overtakes a delayed command.
Receipt absence alone does not prove non-commit. Keep immutable connection snapshots for active
operations and protect each pending operation's original recovery route outside main settings.
These rules address delayed writes, profile edits during operations, and settings corruption
without silently redirecting or replaying financial writes.

---

## 2026-11-15 10:37:50 -06:00: Backfill: separate connection-profile and logical-book identities

**Files Updated:** [spec.md](spec.md), [accounting-model.md](accounting-model.md), [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/application-services.md](contracts/application-services.md), [checklists/requirements.md](checklists/requirements.md).

Backfill recorded at the datetime above. The original change datetime could not be
ascertained. File links identify the affected current documents, not an exact edit chronology.

Give saved profiles stable identities independent of friendly names and connection details.
Pin the expected logical book identity and require explicit reassociation for a different book.
Prepare stable targeting for future API/CLI use without implementing those interfaces. Future
automated writes require an explicit target, rather than the desktop's last-opened profile.

---

## 2026-11-15 10:37:50 -06:00: Backfill: reopen the last successfully activated connection

**Files Updated:** [spec.md](spec.md), [accounting-model.md](accounting-model.md), [ui-parity.md](ui-parity.md), [contracts/desktop-ui.md](contracts/desktop-ui.md), [checklists/requirements.md](checklists/requirements.md).

Backfill recorded at the datetime above. The original change datetime could not be
ascertained. File links identify the affected current documents, not an exact edit chronology.

Automatically open the last successfully activated profile on startup. Renames preserve that
preference; failed switches and connection tests do not replace it. First use, cancellation,
and missing or failed connections lead to the connection dialog without opening another book
or creating a schema automatically.

---

## 2026-11-15 10:37:50 -06:00: Backfill: preserve Payment and Deposit register labels

**Files Updated:** [spec.md](spec.md), [accounting-model.md](accounting-model.md), [ui-parity.md](ui-parity.md), [contracts/desktop-ui.md](contracts/desktop-ui.md), [checklists/requirements.md](checklists/requirements.md).

Backfill recorded at the datetime above. The original change datetime could not be
ascertained. File links identify the affected current documents, not an exact edit chronology.

Retain Payment/Deposit in the register while storing and editing explicit Debit/Credit values.
A selected account's net credit appears as Payment and net debit as Deposit, including credit
cards. Keep signed balances as Debit minus Credit. This preserves the preferred UI behavior
without changing the double-entry model.

---

## 2026-11-15 10:37:50 -06:00: Backfill: define account types separately from accounting classes

**Files Updated:** [spec.md](spec.md), [accounting-model.md](accounting-model.md), [architecture-review.md](architecture-review.md), [checklists/requirements.md](checklists/requirements.md).

Backfill recorded at the datetime above. The original change datetime could not be
ascertained. File links identify the affected current documents, not an exact edit chronology.

Add an explicit eleven-type catalog, including Bank, Credit Card, Loan Receivable, Loan Payable,
and Investment/Brokerage. Derive class and default display group from type and allow a stored
group override. Preserve all five accounting classes. Securities remain holdings, not a sixth
class. Specialized card/loan workflows and existing-account type changes remain deferred.

---

## 2026-11-15 10:37:50 -06:00: Backfill: start with new books and one currency per book

**Files Updated:** [spec.md](spec.md), [accounting-model.md](accounting-model.md), [ui-parity.md](ui-parity.md), [checklists/requirements.md](checklists/requirements.md).

Backfill recorded at the datetime above. The original change datetime could not be
ascertained. File links identify the affected current documents, not an exact edit chronology.

Defer Beancount import and start with new databases. Replace the initial general multicurrency
scope with one currency per book, defaulting to USD. Reject mismatched line currencies, lock
currency after the first saved entry, and defer conversion. This follows the owner's preference
for the simplest useful phase-1 scope.

---

## 2026-11-15 10:37:50 -06:00: Backfill: preserve meaning while simplifying documents

**Files Updated:** [architecture-review.md](architecture-review.md), [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md), [quickstart.md](quickstart.md), [contracts/application-services.md](contracts/application-services.md), [contracts/desktop-ui.md](contracts/desktop-ui.md).

Backfill recorded at the datetime above. The original change datetime could not be
ascertained. File links identify the affected current documents, not an exact edit chronology.

Target approximately 80% of ASD-STE100 through direct sentences and consistent terminology,
without claiming formal compliance. Preserve conditions, qualifications, and failure behavior.
Keep acceptance criteria focused on distinct observable outcomes, and distinguish document
review from unexecuted runtime checks. The constitution incorporates this writing guidance.

---
