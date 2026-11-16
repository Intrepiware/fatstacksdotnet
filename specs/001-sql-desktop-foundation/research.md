# Research: SQL Desktop Foundation

**Status**: Design decisions complete. Implementation and target-environment checks remain pending.

Research decisions use D-001 through D-008. Architecture-review findings use R01 through R14.
Keep each decision ID when its wording or position changes. Assign the next unused ID to a new decision.

## D-001: Desktop and Application Boundaries

**Decision:** Keep WPF and use three production projects: Core, Infrastructure, and Desktop.
Use Autofac property injection and operation-scoped database resources.

**Rationale:** FR-009 preserves the existing interface. Principle I requires independent rules
and testable storage boundaries. The supplied projects already target .NET 10. Review R01
identifies direct window-to-storage calls that need separation.

**Alternatives considered:** A UI framework migration has no current requirement. Separate domain,
application, mediator, and generic repository projects add boundaries with no present benefit.

## D-002: Journal Persistence

**Decision:** Use `SaveJournalEntry` and `DeleteJournalEntry` procedures for whole-entry changes.
Use EF Core for line reconciliation and book/account/security setup. Keep all procedure calls
in `StoredProcedureRepository`.

**Rationale:** FR-015/017/018/025 require atomic multi-line changes, conflict detection, and valid
holding history. The owner supports procedures for this financially sensitive boundary. A line
status change needs only a small EF transaction. Metadata writes do not need journal controls.
The constitution permits this hybrid approach and requires one procedure-call repository.

**Alternatives considered:** EF can implement whole-entry atomic writes. Procedures keep the
selected journal checks beside the multi-row change without a larger persistence framework.
Procedure-only access, column permissions, and procedures for routine setup have no current need.

## D-003: Stale-Version Checks

**Decision:** Use SQL rowversion on journal headers and lines. Preserve line identity. Each
application line update also updates the header timestamp in the same transaction.

**Rationale:** FR-017 requires edits, deletes, and reconciliation updates to detect stale data.
Checking both header and line versions catches independent line changes. Updating the header
also lets an open whole-entry editor detect a reconciliation change.

**Alternatives considered:** A header-only check can miss an independent line change. File hashes
and offsets from the source application do not identify SQL records or their versions.

## D-004: Register Reads

**Decision:** Use `ReadAccountRegister` for account movements and running balances. Use EF for
ordinary details, catalogs, and grouped account totals.

**Rationale:** FR-010/011 require full-history running balances followed by date/search filters.
SQL window functions express that order directly. Review R09 identifies repeated source scans.
The constitution requires a hybrid EF/procedure approach.

**Alternatives considered:** Loading all history for each search repeats unnecessary work.
Caches and pagination need measured evidence before introduction.

## D-005: Precision, Dates, and Holdings

**Decision:** Use C# decimal, SQL money columns `decimal(10,2)`, and quantity `decimal(18,8)`.
Support USD/EUR with two fractional digits and JPY with zero. Use DateOnly/date for accounting
and bank posting dates. Use UTC datetime2 timestamps without Utc in column names.

**Rationale:** FR-014/019/020/025 and the constitution define these boundaries. Eight quantity
decimals preserve the supplied six-decimal example and allow an explicit per-security scale.
SQL aggregate results use `decimal(28,2)` for money and `decimal(28,8)` for quantity so totals
have more range than individual lines and fit C# decimal.

**Alternatives considered:** Floating point, two-decimal quantities, implicit monetary rounding,
and unrestricted currency claims lose required meaning. Extra precision in intermediate C#
calculations is allowed. No custom money type is needed.

Opening and transfer amounts are explicit. The application does not allocate lots or calculate
prices. Validate affected holding history after creates, edits, and deletes. Otherwise, deleting
an opening entry could leave a later transfer with insufficient holdings (FR-025).

## D-006: Schema and Local Configuration

**Decision:** Grate owns definitions and deployment. Runtime activation compares one exact schema
version and checks the profile's expected Book identity. Use a separate maintenance identity.
Store profiles in user-local JSON and protect secret content with Windows current-user DPAPI.

**Rationale:** FR-001–006/028/032 require these controls. Atomic file replacement preserves the
last readable settings when a write fails. A normal runtime role has DML and required EXECUTE
rights but no schema-change rights.

**Alternatives considered:** EF migrations violate the constitution. Schema manifests, automatic
repair, database provisioning, application backups, and encryption infrastructure exceed scope.

## D-007: SQL Transport and Diagnostics

**Decision:** Prefer a validated encrypted connection. Permit encrypted connections with an
unverified self-signed or expired certificate and permit plaintext when TLS is unavailable.
Show a persistent, non-blocking warning for the applicable condition. Never treat an authentication
failure as a reason to weaken transport settings.

**Rationale:** FR-030 explicitly requires these outcomes. FR-029 still forbids secret or financial
values in logs and exception output. Use SqlClient connection options and connection metadata;
report unverified certificate trust honestly rather than claiming that a bypass validates it.

**Alternatives considered:** Mandatory confirmation or rejection for these TLS conditions violates
FR-030. A custom certificate-validation framework is unnecessary. Verify the chosen SqlClient
options against the required server/certificate cases during implementation.

## D-008: Dependencies and Verification

**Decision:** Use EF Core SQL Server for mapped access, SqlClient for parameterized procedure
calls, Autofac for required injection, grate for required deployment, and MSTest for tests.
Use System.Text.Json, Windows DPAPI, WPF binding, and Windows UI Automation for platform tasks.

**Rationale:** Each choice supplies a required boundary. No logging framework, MVVM framework,
container-based test orchestrator, or UI automation wrapper is needed initially. Existing Fluent
assets retain their license. Principle VIII requires exact package/license/security review when
versions are pinned.

**Alternatives considered:** SQLite cannot verify SQL Server behavior. A new dependency is not
justified when a platform API meets the requirement.

## Evidence and Limits

Inputs are constitution 2.0.1, the spec, model notes, UI parity matrix, and architecture review.
Source spot checks confirm the .NET targets and full-history-before-filtering register behavior
in `.workspace/dotnet-beancount/src`. Delegated design research reviewed conflict handling and
holding-history risks. No database, Windows UI, or performance test was run during planning.
All design unknowns are resolved. Package patches, SQL transport behavior, and performance need
implementation verification; this document does not claim those checks passed.
