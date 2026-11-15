# Specification Quality Checklist: Phase 1 — SQL Desktop Foundation

**Purpose**: Validate specification completeness and quality before planning.
**Created**: 2026-11-14
**Feature**: [spec.md](../spec.md)
**Review ownership**: Requirements review performed by the specifying agent.
**Marker semantics**: Checked items indicate requirements quality, not implementation completion.

## Content Quality

- [x] No unrequested implementation choices in the behavioral specification; supplied technical
  constraints are isolated and identified (see qualification below).
- [x] Focused on user value and business needs.
- [x] Written for non-technical stakeholders, with accounting terms explained in the model notes.
- [x] All mandatory sections completed.

## Requirement Completeness

- [x] No unresolved clarification markers remain.
- [x] Requirements are testable and unambiguous within the documented phase-1 assumptions.
- [x] Success criteria are measurable.
- [x] Success criteria describe user-visible outcomes rather than implementation mechanisms.
- [x] Acceptance criteria are defined for every user story.
- [x] Edge cases are identified.
- [x] Scope is clearly bounded.
- [x] Dependencies and assumptions are identified.

## Feature Readiness

- [x] Functional requirements have acceptance coverage (mapping below).
- [x] User stories cover the primary workflows.
- [x] Measurable outcomes define how feature acceptance will be assessed.
- [x] Design recommendations remain in separate review/model documents; the specification
  identifies only owner/constitution technical constraints and an explicit WPF baseline assumption.

## Coverage Map

| Requirements | Acceptance coverage |
|---|---|
| FR-001–FR-006 | Story 1; SC-001, SC-006; configuration/connection edge cases |
| FR-007–FR-012 | Story 2; SC-002, SC-003, SC-008; hierarchy/history edge cases |
| FR-013–FR-019 | Story 3; SC-003, SC-004, SC-006; conflict/date/amount edge cases |
| FR-020 | Stories 3 and 5; SC-007 |
| FR-021–FR-023 | Stories 3 and 4; SC-004, SC-005 |
| FR-024–FR-026 | Stories 2 and 5; SC-003, SC-007; accounting-model catalog |
| FR-027 | Completed static architecture review with R01–R14; plan disposition remains future work |

| FR-028 | Story 3 literal-input acceptance; SQL parameterization review and runtime-role denial tests |

| FR-029 | Synthetic sensitive-value logging checks, provider/EF configuration and exception review |

| FR-030–FR-031 | Non-blocking transport warnings; ordinary connection errors; scope and credential/logging preservation |

| FR-032 | Exact-version success; fatal mismatch/missing/unreadable version; no startup changes |

| FR-033 | Git attribute review and disposable two-branch merge fixture; normal text diffs |

## Notes

- The governing [constitution is version 2.0.1](../../../.specify/memory/constitution.md).
  Its technology constraints are stated in the specification's Scope and Required Constraints.
  They include Autofac property injection, hybrid EF Core/procedure access, one procedure-call
  repository, grate ownership, database key conventions, date naming, decimal money, and UTC.
  The functional requirements remain separate from implementation design and validation.
- FR-001 and FR-005 distinguish local profile identity, logical Book.StableIdentifier, and the
  local bigint BookId. Formats are defined in the data model; API and CLI delivery remain deferred.
- FR-003 covers automatic opening of the last successfully activated profile, failed-switch
  and test exclusion, cancellation, and missing-profile fallback without schema creation.
- P07 and FR-009 preserve Payment/Deposit while stored lines and the editor use Debit/Credit.
  FR-007 defines explicit account types and grouping. Type changes and specialized card/loan
  workflows remain deferred.
- Each book has one currency. Mismatches are rejected, currency locks after first entry,
  and conversion is deferred. Phase 1 starts with new databases and excludes Beancount import.
- Acceptance Criteria are separate from functional requirements and state observable outcomes.
  The two-second/10,000-entry target is not a measured result.
- Basic security openings/transfers, one book per database, and minimal setup are phase-1 scope.
- Review covered all 33 functional requirements, 14 architecture findings, constitution 2.0.1,
  current rationale, and the [decisions ledger](../decisions-ledger.md). No blocking requirement
  clarification remains. The ledger contains change history; active documents state current rules.
- This is a document and static-source review. It does not establish passing application,
  database, Windows, UI, dependency-restore, or performance tests.
- Identity follow-up: FR-001 and FR-005 now distinguish stable local profile IDs from database
  Book IDs. Story 1 covers identity stability and wrong-book rejection with explicit
  reassociation. Future API/CLI targeting is bounded separately; identifier formats and
  interface implementation remain deferred.

- Startup follow-up: FR-003 and Story 1 explicitly require automatic opening of the last
  successfully used profile. Coverage includes rename stability, failed-switch/test exclusion,
  first-run and failed/missing-profile fallback, cancellation, and no automatic schema creation.
  The parity baseline and local-configuration model notes are aligned.

- P07 follow-up retains register Payment/Deposit labels using the source's net-credit/net-debit
  mapping. FR-009, Story 2, parity evidence, and model notes agree; storage and the journal
  editor retain explicit Debit/Credit. Card purchase/repayment examples clarify the labels.

- Follow-up review added explicit account types and their class/default-group/capability
  mappings to FR-007 and the model notes. Story 2 now verifies bank, card, and loan mappings
  and group overrides. Specialized lending/card workflows and existing-account type changes
  remain deferred. The parity baseline and architecture recommendation were aligned.

- FR-017 applies only to journal entries and their lines, including line reconciliation.
  FR-018 requires the entry and all lines to be deleted in one atomic transaction.
  Setup writes are outside the journal conflict and durable outcome-recovery protocol.

- FR-014 checks monetary service inputs and final persisted values before narrowing. Internal
  decimal precision and value-preserving trailing zeros are allowed; SQL numeric parsing is not required.
- Review pass 1 identified the need to replace the initial multicurrency assumption with the
  owner's later single-currency-per-book instruction. The specification and model now reject
  mismatches, lock currency after first entry, and defer conversion.
- The owner confirmed starting with new databases and deferring Beancount import.
- Review pass 2 verified story/requirement coverage, scope, supporting-document consistency,
  and constitution compliance. No blocking clarification remains.
- Qualification for the generic “no implementation details” checklist: the owner expressly
  requires SQL Server, .NET 10, debit/credit columns, and a three-state non-Boolean representation.
  The constitution supplies decimal/date/UTC constraints. These must remain visible under
  “Scope and Required Constraints”; deleting them to satisfy a generic checklist would lose
  requirements. No ORM, migration framework, repository pattern, or testing package is selected.
- Acceptance Criteria replaces the template's Acceptance Scenarios, as required by the
  constitution. Functional requirements remain separate from verifiable story outcomes.
- The two-second/10,000-entry performance target is explicitly proposed, not measured.
- Basic security opening/transfers, one book per database, and minimal setup screens are
  documented assumptions. They can be revised before planning if the owner wants a different scope.
- This is a documentation and static-source review. No application, database, device, or
  performance check has passed merely because this checklist is complete.
- No extension hooks are configured. No branch was created. The feature directory was saved
  in `.specify/feature.json` for downstream commands.
