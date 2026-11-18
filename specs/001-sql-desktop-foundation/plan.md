# Implementation Plan: SQL Desktop Foundation

**Branch**: `001-sql-desktop-foundation` | **Date**: 2026-11-15 | **Spec**: [spec.md](spec.md)

## Summary

Build the single-user Windows accounting application on SQL Server. Reuse the supplied WPF
layout and keyboard behavior. Keep accounting rules outside the UI. Use stored procedures for
whole journal saves and deletes. Use EF Core for setup and line reconciliation. Grate owns the
schema. Each mechanism below has a requirement or a current risk as its reason.

## Technical Context

| Area | Decision |
|---|---|
| Language/version | C# on .NET 10; nullable reference types enabled; pin a supported .NET 10 SDK patch at implementation |
| UI/platform | WPF, `net10.0-windows`, Windows 11 x64; .NET desktop runtime for framework-dependent releases |
| Core | `net10.0`; domain rules, typed requests, service contracts, and testable calculations |
| Infrastructure | EF Core 10 SQL Server provider, Microsoft.Data.SqlClient, Autofac, grate |
| Storage | SQL Server 2022 or later; one book per database; user-local JSON settings with Windows-protected saved credentials |
| Testing | MSTest; real SQL Server integration tests; Windows UI Automation for critical workflows |
| Performance | SC-008: selection, search, and refresh within two seconds in 19 of 20 runs |
| Scale | One active book; acceptance fixture has 100 accounts and 10,000 entries |
| Scope | Connections, setup, register, journal maintenance, line reconciliation, opening holdings, and internal security transfers |

Use built-in WPF binding and commands. Add no MVVM toolkit. Pin package and tool versions when
creating the projects. Review their licenses, support, security findings, and transitive packages
before adoption. These are implementation checks, not completed dependency verification.

## Constitution Check

The pre-design gate passes against constitution 3.0.0. The completed design also passes the
following document checks. Runtime evidence remains pending.

| Principle or constraint | Design evidence |
|---|---|
| I: UI-independent rules | Core contracts and calculations; Infrastructure implements storage; WPF owns rendering |
| II: .NET foundations | .NET 10, SDK pin, nullable analysis, compatible package review, and Windows build checks |
| III: Risk-based verification | Reference accounting cases, SQL boundary tests, and focused Windows journeys in quickstart |
| IV: Security and integrity | Runtime DML permissions, separate maintenance identity, parameterized access, protected credentials, redacted diagnostics |
| IV: Single-user exceptions | Non-blocking TLS warnings; no backup, encryption, or classification subsystem |
| V: Usable interfaces | Existing keyboard behavior, accessible line controls, asynchronous I/O, actionable errors |
| VI: Financial fidelity | Balanced atomic journal writes, separate debit/credit values, full-history balances |
| VII: Purposeful design | Requirement references in the mechanism table; three production projects with explicit boundaries |
| VIII: Dependencies | Platform features first; named package purposes in research; dependency review before adoption |
| Required data access | Hybrid EF/procedure access; all procedure calls in one repository; grate owns all DDL |
| Required types | bigint keys, tinyint catalogs, single-column keys, explicit decimals, date-only accounting dates, UTC timestamps |
| Writing and history | Short direct instructions; current decisions in these artifacts; changes in the decisions ledger |

No constitution exception is requested. No additional safety proposal is needed for this design.

## Project Structure

### Documentation

```text
specs/001-sql-desktop-foundation/
  plan.md
  tasks.md
  research.md
  data-model.md
  quickstart.md
  contracts/application-services.md
  contracts/desktop-ui.md
  accounting-model.md
  architecture-review.md
  ui-parity.md
  decisions-ledger.md
```

The [task list](tasks.md) defines the implementation order and acceptance checkpoints.

### Planned source layout

```text
FatStacks.slnx
global.json
Directory.Build.props
src/
  FatStacks.Core/
  FatStacks.Infrastructure/
  FatStacks.Desktop/
tests/
  FatStacks.Tests/
  FatStacks.SqlTests/
  FatStacks.WindowsChecks/
database/
  up/
  sprocs/
  views/
  types/
```

Core contains financial rules and narrow storage contracts. Infrastructure contains EF mappings,
the shared stored-procedure repository, configuration storage, and the grate maintenance adapter.
Desktop contains WPF views, view models, and the Autofac composition root. These boundaries
meet Principle I and review R01. They need no generic repository, mediator, or extra domain project.

Reuse the source under `.workspace/dotnet-beancount/src` as the UI baseline. Copy the required
assets and licenses into the delivered projects. The application must not depend on that local
reference folder at runtime. Exclude the Beancount parser and file writers.

## Required Mechanisms and Their Reasons

| Mechanism | Requirement or current risk | Chosen scope |
|---|---|---|
| One session object with captured book identity | FR-003/004; delayed reads could update a different book | Cancel old reads; reject results whose session object is no longer active; finish writes before switching |
| Three production projects | Principle I; review R01 | UI, business rules/contracts, and storage boundaries |
| Journal save/delete procedures | FR-015/017/018/025; a partial or incorrect multi-line change affects financial history; owner supports this boundary | Validate complete entries and affected security history within one SQL transaction |
| Register read procedure | FR-010/011; filtering before balance calculation gives wrong balances | Calculate full-history running balances before date/search filters |
| EF reconciliation transaction | FR-017/021; a line update must preserve money and detect stale data | Change one status and touch the header version atomically |
| Journal and line rowversions | FR-017; independent line changes must not be overwritten | Journal records only; compare loaded versions before edits/deletes/status changes |
| Serializable transaction for journal save/delete | FR-017/025; history checks can become stale during a write | Protect checked rows and affected holding ranges until commit; no custom lock service |
| SQL keys and row constraints | FR-008/014/021/024 | Foreign keys, uniqueness, allowed values, debit/credit shape |
| Protected credentials and atomic settings replacement | FR-002/006; failed writes must not destroy saved profiles | Windows current-user protection for secret content; temporary file then replace |
| Currency-change EF transaction | FR-020; first journal save could race a currency edit | Check CurrencyLocked and save the change in one serializable transaction |
| Exact schema version check | FR-032 | One stored version equals the application version; fail disconnected otherwise |
| Explicit grate maintenance | FR-005/028 | Separate maintenance credentials; no migration during normal connection |
| TLS warnings and redacted diagnostics | FR-029/030 | Show the specified warnings without blocking; log safe categories only |
| SQL definition merge attributes | FR-033 | `database/sprocs/**` and `database/views/**` use `text -merge` |

Metadata operations use ordinary validation and EF saves. Journal version checks apply only
to entries and lines under FR-017.

## Lifetimes and Dependency Injection

Register services and view models with Autofac property injection at application startup.
Resolve them only at composition boundaries. Supply required properties before use. Keep
Autofac types out of Core contracts.

The application owns the container, settings service, and session coordinator. Each active book
owns a child lifetime scope with immutable connection and book identity. Each operation owns
and disposes its DbContext or SqlConnection. Never share a DbContext between concurrent reads.
Views own their view models and cancellation sources. Dispose the old session scope after its
writes finish and its reads are cancelled. Apply UI changes on the WPF dispatcher.

All application stored-procedure calls go through `StoredProcedureRepository`. EF setup operations and
reconciliation use the same configured book session, but need no procedure wrapper.

## Delivery and Verification Order

1. Establish the solution, SDK pin, nullable settings, CI, and reused WPF assets.
2. Add the grate schema, runtime permissions, explicit initialization, and profile/session flow.
3. Add journal procedures and EF reconciliation with accounting and SQL integration tests.
4. Connect account setup, register, editor, and details to those services.
5. Add security setup and opening/transfer workflows with complete holding-history checks.
6. Validate UI parity and measure SC-008 on Windows with the representative book.

The [quickstart](quickstart.md) defines commands and acceptance runs. A build does not prove
financial correctness. SQL integration tests do not prove Windows usability.

## Source Review Disposition

| Findings | Planned response |
|---|---|
| R01, R02 | Separate view models and services; explicit accounting entities; exclude file parser |
| R03 | Captured session identity, write completion, and disconnected failed switches |
| R04 | Atomic journal operations and stale-version checks |
| R05, R06 | Per-line states; explicit types/classes/groups; minimal account and security setup |
| R07, R08 | Boundary precision checks; independent quantity scale; book-local calendar |
| R09 | Batch account totals; SQL windowed register; cancel obsolete search; retain virtualization |
| R10 | Typed requests and field errors; no message-prefix logic |
| R11 | Protected credentials, atomic configuration, redacted diagnostics, FR-030 warnings |
| R12 | Stable automation IDs; keyboard, focus, screen-reader, and high-DPI checks |
| R13, R14 | Explicit service outcomes; asynchronous SQL; Windows/SQL test evidence and CI |

All phase-1 recommendations have a planned response. Runtime verification is not yet complete.
See [architecture-review.md](architecture-review.md) for source references and priorities.

## Migration, Installation, and Recovery

Start with new databases. Use grate to initialize an explicitly selected empty database. Reject
unrelated schemas. A compatible initialized database remains unchanged when setup is repeated.
Publish the schema version only after the package completes. Create the Book in an ordinary
EF transaction after schema setup; repeat setup only if the compatible database has no Book.
Keep runtime activation disconnected until both version and book checks pass.

An interrupted grate run needs an explicit maintenance rerun after inspection of grate history.
Do not add automatic repair or an object-inventory protocol. Keep applied one-time scripts
immutable. Add table changes as new migrations. These scripts are authoritative for tables;
do not keep separate table-definition copies. Repeatable objects retain executable definitions.

Deliver a framework-dependent Windows x64 package. Users install the .NET 10 desktop runtime,
extract the package, and start the executable. Updates replace application files while the app
is closed. Settings remain under the current user's local application data folder. A schema
change requires explicit maintenance before the new application can connect. Document each
release's compatibility and recovery effects. Do not promise that replacing an executable can
reverse a schema change. Backup management is outside the application under FR-031.

## Complexity Tracking

No constitution violations. The mechanism table records the reason and scope of each design
choice that adds coordination or validation.
