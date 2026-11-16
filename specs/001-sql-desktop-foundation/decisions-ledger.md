# Decisions Ledger: Fat Stacks.NET — SQL Desktop Foundation

## 2026-11-16 02:16:19 +00:00: Generate implementation tasks

**Files Updated:** [tasks.md](tasks.md), [plan.md](plan.md), [spec.md](spec.md).

Create 68 dependency-ordered tasks across setup, shared foundation, the five user stories, and
release validation. Include the required financial, SQL Server, and Windows checks, exact schema
constraints, independent story checkpoints, and parallel work examples. Link the task list from
the plan and mark the spec ready for implementation.

Preserve the current scope: StoredProcedureRepository contains all procedure calls, EF handles
reconciliation and metadata, and schema compatibility uses a stored integer with a hardcoded
expected version. Securities remain required. The tasks add no journal outcome-verification
protocol. This workflow generates documents; implementation checks remain unexecuted.

---

## 2026-11-15 23:30:45 +00:00: Identify research decisions consistently

**Files Updated:** [research.md](research.md).

Assign D-001 through D-008 to the research decisions. Use D for decisions because R already
identifies architecture-review findings. Keep decision IDs stable when wording or order changes,
and give new decisions the next unused ID.

---

## 2026-11-15 23:28:16 +00:00: Name the shared stored-procedure repository

**Files Updated:** [plan.md](plan.md), [research.md](research.md), [application-services.md](contracts/application-services.md).

Rename JournalRepository to StoredProcedureRepository. The name reflects the constitution's
requirement that one service or repository contain all application stored-procedure calls.
Keep parameter binding, execution, and result mapping in this repository. Application services
own business workflows.

---

## 2026-11-15 23:18:19 +00:00: Remove journal outcome verification

**Files Updated:** [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md), [application-services.md](contracts/application-services.md), [desktop-ui.md](contracts/desktop-ui.md), [quickstart.md](quickstart.md), [accounting-model.md](accounting-model.md), [architecture-review.md](architecture-review.md).

Remove the FR-017 sentence that requires uncertain journal outcomes to be identified and
verified before retrying, as requested by the owner. Remove the planned JournalOperation table,
operation IDs, receipts, local pending marker, verification service, retry protocol, restart
blocking, and their validation scenarios. Align the related edge case, SC-004, and source-review
recommendation so they do not restore the removed requirement.

Keep stale-version detection, atomic journal changes, draft retention, and prevention of repeated
submissions while a save is pending. Whole-entry saves and deletes still use procedures;
reconciliation still uses an EF transaction. This changes planning documents only.

---

## 2026-11-15 22:27:39 +00:00: Plan the simpler single-user implementation

**Files Updated:** [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md), [quickstart.md](quickstart.md), [application-services.md](contracts/application-services.md), [desktop-ui.md](contracts/desktop-ui.md), [accounting-model.md](accounting-model.md), [architecture-review.md](architecture-review.md), [spec.md](spec.md).

Regenerate the plan against constitution 2.0.1 and the updated functional requirements. Use
three production projects, ordinary EF setup operations, grate deployment, and a simple exact
schema-version check. Record a requirement or concrete current risk for each mechanism. Align
the source review's transport advice with FR-030 and correct the spec's branch and planning status.

Use procedures for journal saves, deletes, and register reads, following the owner's support
for that boundary. Use EF for line reconciliation after the owner questioned the need for a
procedure. Limit versions and outcome receipts to FR-017 journal operations. A receipt can record
Applied or terminal NotApplied so a late command cannot run after negative verification. Keep
holding-history checks for FR-025 and omit general recovery, permission, and schema frameworks.

Add a runnable validation guide for the planned implementation and record the current evidence
limits. No application, database, UI, or performance test ran during this planning workflow.

---

## 2026-11-15 16:00:41 -06:00: Clarify monetary validation boundaries

**Files Updated:** [constitution.md](../../.specify/memory/constitution.md), [plan.md](plan.md), [quickstart.md](quickstart.md), [research.md](research.md), [data-model.md](data-model.md), [spec.md](spec.md), [requirements.md](checklists/requirements.md), [application-services.md](contracts/application-services.md), [accounting-model.md](accounting-model.md), [functional-requirements-safety-review.md](../../.workspace/functional-requirements-safety-review.md), [decisions-ledger.md](decisions-ledger.md).

Clarify the constitution as version 2.0.1 and align FR-014 and the planning guidance.
Validate monetary inputs and final persisted values before narrowing conversions. Permit extra
precision in intermediate decimal calculations and value-preserving trailing zeros. Use typed
decimal SQL parameters instead of a numeric-text parsing protocol. No per-assignment checks,
custom money types, or duplicate SQL parsing are required. Keep independent security quantity
scales, wider totals, explicit rounding policies, and range checks. Update C08 and the report's
source fingerprint. This changes documentation only; implementation remains pending.

---

## 2026-11-15 15:51:12 -06:00: Require explicit procedure/view definition merges

**Files Updated:** [spec.md](spec.md), [plan.md](plan.md), [quickstart.md](quickstart.md), [requirements.md](checklists/requirements.md), [research.md](research.md), [functional-requirements-safety-review.md](../../.workspace/functional-requirements-safety-review.md), [decisions-ledger.md](decisions-ledger.md).

Add FR-033 requiring one current SQL definition per procedure/view and Git -merge attributes
on their grate folders. Keep text diffs. Concurrent edits to one file require deliberate
resolution even when lines do not overlap. This addresses C07 merge review for the solo project,
without claiming to detect cross-file object redefinitions or replace grate ownership,
immutable applied migrations, or consistent current definitions. Configuration and fixture
validation remain implementation work; no Git attribute settings were changed here.

---

## 2026-11-15 15:45:42 -06:00: Keep schema compatibility to a fatal version comparison

**Files Updated:** [spec.md](spec.md), [data-model.md](data-model.md), [plan.md](plan.md), [contracts/application-services.md](contracts/application-services.md), [research.md](research.md), [quickstart.md](quickstart.md), [checklists/requirements.md](checklists/requirements.md), [safety review](../../.workspace/functional-requirements-safety-review.md), [decisions-ledger.md](decisions-ledger.md).

Add FR-032 for a lightweight exact schema-version comparison. A mismatch, missing version, or
failed version read is fatal to that connection attempt, with the app disconnected and dialog
available. Remove custom manifests, object inventories, schema-level locking, tightly coupled
runner/book bootstrap, and automatic migration recovery. Keep grate ownership, normal migration
history/checks, current definitions, safe explicit setup, and separate FR-017 journal recovery.

---

## 2026-11-15 15:39:09 -06:00: Adopt approved single-user security exceptions

**Files Updated:** [constitution.md](../../.specify/memory/constitution.md), [plan.md](plan.md), [quickstart.md](quickstart.md), [research.md](research.md), [data-model.md](data-model.md), [spec.md](spec.md), [requirements.md](checklists/requirements.md), [application-services.md](contracts/application-services.md), [desktop-ui.md](contracts/desktop-ui.md), [functional-requirements-safety-review.md](../../.workspace/functional-requirements-safety-review.md), [decisions-ledger.md](decisions-ledger.md).

Approve constitution 2.0.0 and FR-030/FR-031: warn without blocking for no TLS and self-signed
or expired SQL certificates. Use TLS when available and preserve ordinary connection failures.
Exclude application-required backup/encryption/classification/key-management/log-encryption
infrastructure while the desktop app remains single-user. Keep saved-credential protection,
FR-029, and migration/recovery documentation. Operator-managed backups/protection are optional.
Review these exceptions before introducing multi-user access.

---

## 2026-11-15 15:36:30 -06:00: Prohibit sensitive values in logs

**Files Updated:** [spec.md](spec.md), [plan.md](plan.md), [checklists/requirements.md](checklists/requirements.md), [decisions-ledger.md](decisions-ledger.md).

Add FR-029 to prohibit sensitive financial data, account numbers, passwords, tokens, and
secret-bearing connection strings in logs, SQL diagnostics, and exception output. Keep EF
sensitive-data logging disabled. This makes existing constitution protections explicit.
TLS-warning behavior and infrastructure exclusions are not adopted by this entry; their
conflict with Principle IV requires a separate approved constitutional adjustment.

---

## 2026-11-15 14:53:23 -06:00: Require parameterized access and a restricted runtime identity

**Files Updated:** [spec.md](spec.md), [plan.md](plan.md), [quickstart.md](quickstart.md), [checklists/requirements.md](checklists/requirements.md), [functional-requirements-safety-review.md](../../.workspace/functional-requirements-safety-review.md), [decisions-ledger.md](decisions-ledger.md).

Promote report recommendation R02 to FR-028. Require EF query APIs or parameterized SQL,
including parameter binding for raw SQL through EF. Prohibit concatenating user values into
SQL text. Normal access uses a non-administrator identity without schema-change permissions;
initialization and grate migrations use separate explicit maintenance privileges.

Add literal-input acceptance and validation coverage. Move the report item into its required
safeguards section. These are ordinary database practices, not a new recovery subsystem.

---

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
