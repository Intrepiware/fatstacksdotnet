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

## Notes

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
