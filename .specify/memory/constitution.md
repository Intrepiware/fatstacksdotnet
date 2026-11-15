<!--
Sync Impact Report (temporary review material; remove before committing)
Version change: 2.0.0 → 2.0.1 (PATCH: clarify monetary validation boundaries)
Modified guidance: Technology Constraints, monetary range and precision validation
Added guidance: validate service inputs and final persisted values; permit extra decimal
precision in intermediate calculations and value-preserving trailing zeros
No requirement for per-assignment checks, custom money types, or duplicate SQL numeric parsing
Related feature documents and safety review aligned; no template source changes
Compatibility: documentation only; no application or stored schema modified
Deferred placeholders: None
-->




# Fat Stacks.NET Constitution

Fat Stacks.NET is a personal accounting software package. It uses double-entry accounting
and may provide a web interface, a desktop interface, or both.

## Core Principles

### I. Interface-Independent Business Logic

Business rules and application workflows MUST remain independent of web and desktop UI
frameworks. Presentation code MUST delegate business operations to application or domain code;
UI state and rendering MUST remain in the presentation layer. Infrastructure dependencies MUST
be isolated at boundaries that allow business behavior to be tested without a running UI or
external service. Projects and abstractions MUST be introduced only for a documented boundary
or current requirement. This permits either interface without duplicating business rules or
requiring unnecessary architectural layers.

### II. Maintainable .NET Foundations

Production code MUST use .NET 10 while it remains supported, with compatible dependencies.
Before .NET 10 reaches end of support, the project MUST adopt a supported successor through
an explicit technology decision and constitution amendment. The repository
MUST pin its .NET SDK, declare target frameworks, and document required workloads and operating
systems. New C# projects MUST enable nullable reference types. Changes MUST follow repository
formatting and analyzer rules, resolve new compiler and analyzer warnings, and document any
necessary suppression. Dependencies MUST have a concrete purpose and compatible licenses.
Framework and package choices MUST favor the simplest solution that meets current requirements.

### III. Risk-Based Verification

Changes to business rules MUST include automated tests for their observable behavior. Defect
fixes MUST include a regression test when the failure can be reproduced automatically.
Changes to persistence, external integrations, or public contracts MUST include integration or
contract tests covering the affected boundary. Critical user journeys MUST have automated UI
coverage where practical; otherwise, the change MUST record repeatable manual verification
steps and the reason automation is impractical. Tests MUST be deterministic and isolated from
production data. Cosmetic or documentation-only changes MAY use focused review instead of
new automated tests.

Validation MUST target financial correctness and actual user journeys rather than mirror the
implementation. Reviews MUST distinguish document checks, synthetic tests, integration or
reference comparisons, and measurements on target environments. Unexecuted checks MUST be
reported as unverified. A build or schema check alone MUST NOT be presented as proof of
financial correctness or UI compatibility. Performance claims MUST be supported by measurements
on the specified environments with representative data. Performance fixes MUST NOT silently
drop required records, calculations, or behavior.

### IV. Security and Data Integrity

Secrets MUST remain outside source control and packaged application assets. Untrusted input
MUST be validated at trust boundaries, and privileged operations MUST enforce authorization
at the trusted execution boundary. Desktop clients MUST NOT be treated as trusted solely
because they are installed locally. Data access MUST use parameterized queries or equivalent
safe APIs. Sensitive data MUST be excluded from logs and error messages and protected in
transit and at rest according to its documented classification. Changes to stored data MUST
document migration, recovery, and compatibility effects before release. Dependency security
findings MUST be assessed, with fixes or explicitly accepted risks recorded before release.

While this desktop application remains single-user, application services MAY enforce business
validation through ordinary EF Core operations and transactions, together with database
constraints and normal runtime permissions. The desktop-client trust rule does not require
procedure-only writes, column-level permissions, or duplicate SQL validation for every
operation. Atomicity alone is not a reason to require a stored procedure; EF Core transactions
can provide atomic writes. Stored procedures MUST have a concrete reason consistent with the
hybrid data-access rule. Additional permission schemes, duplicate validation, and recovery
protocols MUST address a documented current requirement or risk. They MUST NOT be introduced
solely to cover hypothetical future multi-user access. These choices MUST preserve the
required accounting outcomes, parameterized access, and protection of credentials and data.

While this desktop application remains single-user, the following explicit exceptions apply
instead of mandatory transport/at-rest protection and formal data classification:

- SQL connections without TLS, or with a self-signed or expired certificate, MUST produce a
  visible non-blocking warning. These conditions alone MUST NOT prevent connection. Use TLS
  when available. Authentication failures, unsupported protocol negotiation, and other errors
  that prevent SQL access MUST still be reported. The warning policy is not a claim that an
  unverified certificate authenticates the server or that plaintext protects credentials.
- The application MUST NOT implement or require backup management, disk/database/field
  encryption, formal data-classification infrastructure, encryption-key management, or an
  encrypted logging subsystem. Operators MAY independently manage backups and storage protection.
  Migration and recovery effects MUST still be documented without requiring those facilities.
- Platform protection of saved credentials remains required. Passwords, account numbers,
  sensitive financial data, tokens, and secret-bearing connection strings MUST NOT appear in
  logs or diagnostic exception output. These protections are not waived by the exceptions.

Before introducing multi-user access, the design MUST review trust boundaries, authorization,
concurrency, and write enforcement for that deployment. The single-user allowance MUST NOT
be carried forward without that review.

### V. Usable and Diagnosable Interfaces

Every delivered interface MUST provide keyboard access, meaningful accessible names, visible
focus, and understandable validation and error feedback for its supported workflows. Long
operations MUST keep the interface responsive, expose progress or busy state, and support
cancellation when the operation permits it. Asynchronous I/O MUST avoid blocking request or UI
threads. Operational failures MUST produce actionable diagnostics through structured logging
without exposing sensitive data. Performance-sensitive workflows MUST define measurable
acceptance criteria and verify them on representative environments before release.

### VI. Double-Entry Accounting and Financial Fidelity

The accounting model MUST include a book of accounts, journal entries, journal entry lines,
and explicit debit and credit effects. Every posted journal entry MUST balance: total debits
MUST equal total credits. Its lines MUST reference valid accounts in the intended book.
Posting MUST be atomic so that a failure cannot leave a partially posted entry.

Balances and reports MUST reflect account classifications, opening balances, and posted
journal activity consistently. Unknown amounts MUST NOT be replaced with zero. Required
records MUST NOT be silently omitted. Filtering, searching, and pagination MUST NOT alter the
underlying accounting balances. Financial calculations MUST be verified against known
reference results, including balanced and unbalanced entries and failed posting operations.
These rules preserve the integrity of the books regardless of interface or implementation.

### VII. Confirmed Scope and Purposeful Design

Scope and architecture MUST remain tied to confirmed requirements. Features, abstractions,
framework migrations, and operational machinery MUST have a documented current need.
When multiple approaches satisfy the requirements, the simplest maintainable option MUST be
chosen. Simplification MUST preserve required behavior. Once an interface is accepted, it
MUST remain the starting point for changes. Major redesigns MUST address a confirmed user
need or measured problem.

Plans and designs MUST remain revisable. When evidence contradicts a design assumption,
the finding and revised approach MUST be recorded while preserving confirmed requirements.
Changed requirements MUST be reconciled in active specifications, plans, and tasks before
dependent implementation proceeds.

### VIII. Restrained, Trusted Dependencies

Direct and transitive dependencies MUST be used sparingly. Each added dependency MUST have
a documented purpose and explain why existing platform capabilities or a simpler approach
are insufficient. Review MUST cover the transitive dependency footprint, license compatibility,
maintenance history, reliability, and handling of security issues. Popularity alone MUST NOT
be treated as evidence of trust. These requirements apply to runtime packages and development
tooling. Material dependency changes MUST be reviewed before adoption.

## Technology and Platform Constraints

- C# and .NET 10 are the required application stack while .NET 10 remains supported.
  Additional languages or runtimes MUST have a documented requirement and maintenance rationale.
- SQL Server MUST be the application database. Database integration tests MUST verify relevant
  SQL Server behavior; substitutes alone MUST NOT establish database compatibility.
- Autofac MUST provide property injection for service and view-model dependencies. Container
  registration and resolution MUST remain at composition boundaries. Core business contracts
  MUST NOT depend on Autofac. Mandatory dependencies MUST be supplied before a component starts
  work. Plans MUST define lifetimes and disposal responsibilities.
- Data access MUST use a hybrid EF Core and stored-procedure approach. Use EF Core for atomic
  writes when ordinary EF operations and transactions meet the requirements. Use stored
  procedures when EF would require awkward workarounds or complex machinery. Document the
  reason for each stored-procedure write path. Do not contort EF to avoid a procedure.
  All application stored-procedure calls MUST be contained in one service or repository.
- grate MUST own all database definitions, deployment, and schema changes. This includes
  tables, procedures, views, functions, types, indexes, and constraints. EF Core migrations
  and EF schema creation MUST NOT be used. This is a project-wide decision.
  Keep one current definition per object in the grate package so concurrent changes meet in
  the same file. Review combined changes even when Git reports no conflict. Applied one-time
  migration scripts MUST remain immutable. Table evolution MUST use new migration scripts;
  current table definitions MUST remain consistent with the migrated schema.
- Application-owned tables MUST default to bigint primary keys. Small enum tables, such as
  AccountType and AccountClass, MUST use tinyint keys. A uniqueidentifier requires a documented
  specific purpose, such as a StableIdentifier or PublicId; it MUST NOT be the default key type.
  Primary and foreign keys MUST use single columns. Enforce combined uniqueness with separate
  UNIQUE constraints. Third-party internal schemas, including grate history, retain their
  own conventions.
- Application-owned date and timestamp column names MUST NOT contain Utc. Use names such as
  CreateDate and UpdateDate. This naming rule does not change the UTC storage requirement.
- Transaction dates, bank posting dates, and journal accounting dates MUST use .NET
  `DateOnly` and SQL Server `date`. They represent calendar dates without a time zone.
  Bank-supplied dates MUST be preserved without UTC or user-time-zone conversion. Transaction
  and posting dates MUST remain distinct when both are supplied. A date-only source MUST NOT
  be assigned an invented time, midnight timestamp, or time zone.
- Every date-time value persisted in the database MUST represent UTC, including creation,
  modification, and import timestamps. Input boundaries MUST resolve offsets or time zones
  before persistence. Local-time conversion MUST occur at display or input boundaries.
- When an import supplies only a timestamp, deriving an accounting date MUST follow an
  explicit, documented source or account time-zone policy. The application MUST NOT silently
  use the device's current time zone or assume an unknown source time zone.
- Each book MUST have a configured time zone for accounting defaults. Manual-entry defaults
  such as “today” MUST use that book time zone, and users MUST be able to override the default
  accounting date. Travel or a device time-zone change MUST NOT alter stored accounting dates.
  The user's display time zone MAY control the presentation of operational timestamps without
  changing accounting dates.
- Monetary amounts in the service layer MUST use C# `decimal`. Database monetary columns MUST
  use `decimal(10, 2)` by default. Exceptions MUST document the required range or precision and
  declare an explicit decimal precision and scale. Binary floating-point types MUST NOT be
  used for monetary amounts. Rounding rules MUST be explicit and tested. Out-of-range values
  and precision loss MUST NOT be silently accepted. Currency precision and range MUST be
  validated at application-service input boundaries and before final monetary values are
  persisted, before any conversion that could lose precision or range. Internal decimal
  variables and intermediate calculations MAY retain additional precision. Trailing zeros
  that do not change the value MUST NOT cause rejection. This rule does not require validation
  after every assignment, a custom money type, or duplicate numeric parsing in SQL.
- Feature plans MUST identify whether the interface is web, desktop, or both, and record the
  chosen framework and supported browsers or operating systems. This constitution does not
  mandate a particular UI framework or require both interfaces.
- Web applications MUST use HTTPS in deployed environments and apply applicable protections
  for authentication, session handling, cross-site scripting, and cross-site request forgery.
- Desktop applications MUST respect UI thread affinity, use platform-appropriate credential
  storage when credentials are retained, and document installation and update behavior.
- Configuration MUST be separated from code, validated at startup where required, and
  documented with safe defaults. Logs MUST identify failures without including secrets.
- Public contract or storage changes MUST state compatibility effects and provide a migration
  path when existing consumers or installations are affected.

## Development Workflow and Quality Gates

1. Specifications MUST state user outcomes, acceptance criteria, interface scope, and relevant
   security, accessibility, data, and performance requirements before implementation.
2. Plans MUST identify application boundaries, platform choices, verification steps, and any
   constitution exceptions. Technical decisions with lasting consequences MUST record their
   rationale in the plan or a linked decision record.
3. Changes MUST remain focused and include affected documentation, configuration guidance, and
   migration instructions. Setup documentation MUST provide reproducible restore, build, test,
   and run commands for each supported application target.
4. Before merge, the affected targets MUST restore, build, and pass relevant automated tests
   on compatible runners. Formatting and configured analyzers MUST pass, and new warnings MUST
   be resolved or explicitly justified. CI MUST enforce the automated checks once application
   code is introduced.
5. Every change MUST receive a recorded compliance review, through a pull request or a
   documented self-review for a solo maintainer. Review MUST cover acceptance criteria,
   applicable principles, verification evidence, and any approved exception.
6. Releases MUST identify the application version, summarize user-visible changes, and record
   deployment or installation steps and recovery instructions where applicable.

### Rule: Clear Document Writing

All project documents, including specifications, plans, and task lists, MUST use clear, direct
language and active verbs. Target a twelfth-grade reading level. Each sentence MUST express
one idea or instruction. Explanations MUST remain brief, with clarity taking priority over
brevity. Author-created shorthand MUST be defined unless its meaning is obvious.
Target approximately 80% of ASD-STE100 as editorial guidance, not a measured compliance score.
Documents MUST NOT claim formal ASD-STE100 compliance. Simplified wording MUST preserve the
full meaning, including conditions, qualifications, and failure behavior.

### Rule: Focused Acceptance Criteria

Specifications MUST use the heading **Acceptance Criteria** instead of **Acceptance Scenarios**.
Acceptance criteria MUST state observable conditions that demonstrate a user story is complete.
The first content MUST be a bulleted list of concrete behavior introduced or changed by the
story, without a subheading for that list.

Each criterion MUST state a specific outcome that a reviewer can verify. Criteria MUST exclude
background, rationale, implementation advice, summaries, and general statements of intent.
They MUST NOT merely restate the story or duplicate another criterion. Every criterion MUST
add a distinct acceptance condition and follow the Clear Document Writing rule.

Functional requirements MUST remain in their own section. Story acceptance criteria MUST
apply those requirements rather than repeat them verbatim. Requirement identifiers MAY be
referenced for traceability.

Include a **Non-Functional Requirements** section only when source requirements contain
relevant non-functional requirements. Its content MUST be a bulleted list. Authors MUST NOT
invent requirements to populate this section.

Include a **What Must Not Change** section only when source requirements identify existing
behavior the story must preserve. Its content MUST be a bulleted list. Each item MUST explicitly
state the behavior that must continue. Authors MUST NOT invent preservation requirements to
populate this section. Both optional sections MUST follow the Clear Document Writing rule
and MUST be omitted when inapplicable.

### Rule: Decisions Ledger and Current-State Documents

Changes to requirements, scope, design decisions, and acceptance conditions in specification
or planning documents MUST be recorded in `specs/<feature>/decisions-ledger.md`. This applies
regardless of who initiates the change. Each logical change MUST have one concise entry,
even when it affects several files. State what changed and why. Do not repeat the updated
documents or add filler. New entries MUST appear at the top, below the ledger title.

Ledger timestamps MUST use the current date and time in the user's configured time zone,
with the numeric UTC offset applicable at that instant. Use `YYYY-MM-DD HH:MM:SS ±HH:MM`,
for example `2026-11-15 10:24:36 -06:00`. Account for daylight-saving time. Do not treat a
fixed offset as a year-round time-zone rule. These are document timestamps; persisted
application database timestamps remain subject to the UTC requirement.

Use the following ledger form. Braced text is a template instruction or replacement field:

```markdown
# Decisions Ledger: {Project Name}

## {YYYY-MM-DD HH:MM:SS ±HH:MM}: {Brief Change Description}

**Files Updated:** {file1.md}, {file2.md}

{Describe what changed and why in 1–3 concise paragraphs. Preserve readability. Do not add filler.}

---
```

Specifications and planning documents MUST describe the present decided state. Keep obsolete
decisions out of active specifications. History of changed or superseded decisions MUST appear
only in the ledger. Do not add change-history asides such as “User revised decision on {date},”
“amended on {date},” or “superseded by {decision}” to active documents.

Keep the rationale for current decisions in research and planning documents, including
research.md. This rule does not override Spec Kit instructions to document technical rationale.
Required migration instructions, compatibility explanations, source findings, and verification
evidence MUST remain available where needed; they are not change-history asides.

## Governance

This constitution takes precedence over conflicting project conventions and workflow documents.
MUST denotes a requirement; MAY denotes an optional practice. Rules apply to the interfaces and
capabilities actually implemented, and do not require unused infrastructure or application types.

Amendments MUST describe the proposed change, its rationale, its effect on existing work, and
any migration needed. A project maintainer MUST approve the amendment through a recorded review
before adoption. Related specifications and guidance MUST be reviewed for consistency and any
required follow-up work recorded.

The constitution uses semantic versioning: MAJOR for incompatible principle removals or
redefinitions, MINOR for new principles or materially expanded guidance, and PATCH for
clarifications that do not change obligations. Amendments MUST update the version and last
amended date while preserving the original ratification date. Technology choices recorded
here remain binding unless changed through this amendment process.

Specifications, plans, and change reviews MUST check compliance with applicable principles.
Exceptions MUST record the affected rule, rationale, scope, compensating measures, approver,
and review or expiry condition. A maintainer MUST approve an exception before the affected
change is merged. Repeated exceptions MUST trigger a review of the governing rule.

**Version**: 2.0.1 | **Ratified**: 2026-11-14 | **Last Amended**: 2026-11-15
