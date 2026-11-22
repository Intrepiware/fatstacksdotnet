# Quickstart and Validation Guide

**Status:** Connections and US2 account/register functionality are implemented. See
[US1 validation](validation/us1.md), [US2 validation](validation/us2.md), and the
[user-reported WindowsChecks pass](validation/windowschecks-2026-11-18.md).
Use the [manual US1/US2 checklist](validation/manual-us2.md) for the current hands-on test pass.
Monetary journal maintenance is implemented; see [US3 validation](validation/us3.md) for SQL and portable results and pending Windows checks. Reconciliation and security workflows remain planned for US4–US5.

## Prerequisites

Use Windows 11 x64 with the pinned .NET 10 SDK and desktop targeting support. Visual Studio users
need the .NET desktop development workload. Obtain SQL Server 2022 or later, with two empty test
databases and a separate non-administrator runtime identity. SQL Server installation is outside
scope. Use only synthetic data. Never point fault tests at a real book.

The implementation must include a local grate tool manifest, restoreable package versions,
runtime-role setup instructions, and a Windows CI workflow. Credentials stay outside the repo,
command-line arguments, and test output. Use integrated authentication or protected local settings.

## Restore, Build, Test, and Run

Run from the repository root in PowerShell:

```powershell
dotnet tool restore
dotnet restore FatStacks.slnx
dotnet build FatStacks.slnx --no-restore
dotnet test --project tests/FatStacks.Tests/FatStacks.Tests.csproj
dotnet test --project tests/FatStacks.SqlTests/FatStacks.SqlTests.csproj
dotnet test --project tests/FatStacks.WindowsChecks/FatStacks.WindowsChecks.csproj
dotnet run --project src/FatStacks.Desktop/FatStacks.Desktop.csproj
```

Use the .NET 10 Microsoft.Testing.Platform runner configuration with MSTest for the shown test
syntax. Run Windows checks in an interactive desktop session. SQL tests must clearly report
unavailable SQL prerequisites; they must not claim a skipped database test passed.

Expected: restore/build succeed without new warnings; service tests compare exact financial
results; SQL tests verify procedures, EF reconciliation, permissions, and rollback on SQL Server.
Windows tests verify actual bindings and user workflows. Keep those evidence types separate.

## Initialize and Open Two Books

1. Launch the app and add profiles Personal and Test for the two empty databases.
2. Test either connection. Expect schema-not-initialized feedback and no automatic schema change.
3. Select Initialize for Personal. Supply explicit maintenance credentials, book name, USD, and
   a Windows time zone. The adapter runs the grate package against that selected database.
4. Open Personal with the runtime identity. Create Bank, Expense, Equity, and two Investment/
   Brokerage accounts. No manual financial record edits are needed.
5. Initialize Test with EUR. Switch to Test and restart. Expect Test to reopen automatically.
6. Rename Test and restart. Expect the same profile ID and book. Test an unreachable profile;
   it must not replace the startup preference.
7. Switch with a draft, a pending write, and a delayed read. Exercise Save, Discard, and Cancel.
   Verify no old record or response appears in the new session.
8. Repoint Personal at Test's database. Expect an identity error until explicit reassociation.
9. Repeat initialization of a compatible book. Expect unchanged identity and records. Try an
   unrelated schema and a mismatched/missing version. Expect no alteration and no active book.

This covers FR-001–008/028/032 and SC-001/006. The connection dialog remains usable after failure.

## Accounting Reference Run

Use the [data model](data-model.md) and [service contract](contracts/application-services.md).
Create each fixture in an isolated book or reset the test database between cases.

| Case | Expected result |
|---|---|
| Bank opening: debit Bank 1,000; credit Equity 1,000 | Bank +1,000; Equity -1,000 |
| Expense after opening: debit Expense 25; credit Bank 25 | Bank 975; Expense +25; Payment 25 in Bank |
| Salary after expense: debit Bank 500; credit Income 500 | Bank 1,475; Income -500; Deposit 500 |
| Card payment: debit Card 100; credit Bank 100 | Card movement +100; Bank movement -100 |
| Split: debit Expense A 20, Expense B 5; credit Bank 25 | All three lines retained; total debit = total credit |
| Edit expense 25 to 30, then delete it | Bank decreases by another 5, then regains 30; no leftover lines |
| Old opening outside 90 days plus recent expense | Recent running balance includes the old opening |
| Search hides an earlier row | Retained rows keep their original running balances |
| Multiple selected-account lines net to zero | Row remains visible; Payment/Deposit blank; all details retained |
| Equal dates across restart | Order remains AccountingDate then JournalEntryId |

Test all five classes and all account types. Rename parents and close accounts. Verify history,
group overrides, closure-date boundaries, and descendant totals including closed accounts.

## Invalid Inputs, Conflicts, and Failure Outcomes

Automate the following with SQL Server and isolated data:

- Reject negative money, two positive sides, both sides zero, imbalance, invalid accounts,
  unsupported currency, 100,000,000.00 on one side, and nonzero excess decimal places.
- Accept 1.2300 USD and reject 1.001 USD. Accept whole JPY amounts and reject fractional JPY.
  Check multi-line totals above 99,999,999.99 without narrowing them to a line field.
- Save apostrophes and SQL-like payee/description text literally. Verify no command executes.
- Inject a failure between line changes and commit in save/delete. Verify zero partial effects.
- Edit or delete using stale header and line versions. Include an independent line change.
  Verify Conflict and unchanged newer data. Keep the failed editor draft.
- Reconcile a line while another editor saves. Verify the EF line/header transaction
  either succeeds as a unit or reports a conflict without overwriting newer data.
- Repeat Save while a save is pending. Verify only one submission is sent and one entry is saved.
- Fail a preferences write after a committed journal save. Verify the accounting result stays
  committed and the preference failure is reported separately.

These cases target FR-014–018/028 and SC-003/004/007. Synthetic fault injection proves only the
observed cases. It is not evidence of every possible network failure.

## Reconciliation and Security Run

Test every pair of the three reconciliation states across a two-account entry. Refresh and
restart after each change. Verify no amount changes. Test Mixed for multiple selected-account
lines. A financial edit resets only affected lines; unchanged lines keep identity and status.

Open 1.234567 units with carrying amount 100.00 against Equity. Transfer 0.234567 units and 20.00
from holding A to holding B. Expect A = 1.000000 units/80.00 and B = 0.234567 units/20.00.
Reject transfers beyond quantity or carrying amount and reject excess declared quantity precision.
Try an edit/delete of the opening that would invalidate the later transfer. Expect no write.
Test backdated transfers and edits moving a security between holding accounts. Compare the whole
resulting affected history. Confirm that the UI exposes no trading or valuation capability.

This covers FR-020–026 and SC-005/007. USD and EUR remain in separate books.

## Permissions, Transport, and Schema Checks

- Verify the runtime identity can perform supported operations but cannot alter schema.
  Confirm maintenance succeeds only with the separate explicit privileges.
- Test valid TLS, no TLS, self-signed certificate, and expired certificate. The latter three
  show the correct visible warning and permit usable connections without confirmation.
  Authentication failures still fail. Capture warnings without recording secrets.
- Seed diagnostic sentinel values in passwords, account numbers, and financial descriptions.
  Exercise SQL failures and global exception handling. Inspect output for leaks. Confirm EF
  sensitive-data logging stays disabled and parameter/exception logs expose no private values.
- Interrupt grate initialization in an isolated database. Verify normal opening refuses an
  incomplete installation. Inspect grate history and rerun maintenance explicitly.
- Use `git check-attr merge diff text -- database/sprocs/SaveJournalEntry.sql` after implementation.
  Expect merge unset and text set, with normal readable text diffs. Repeat for a view fixture.
  In a temporary Git repository, change non-overlapping lines of one procedure on two branches.
  Expect a conflict that requires deliberate resolution. Do not alter the working branch for this test.

## UI, Performance, and Release Evidence

Run every Retain/Adapt row in [ui-parity.md](ui-parity.md). Record source and port screenshots for
visual rows. Check keyboard-only entry, popup focus, accessible names, high-DPI layout, and long
splits. Automate the critical journeys. Record a reason and exact manual steps for any automation
gap; visual comparison still requires recorded review.

For SC-008, use 100 accounts and 10,000 entries with older history, splits, and securities.
Measure account selection, search, and refresh for 20 runs each. Each operation must finish within
two seconds in at least 19 runs. Record hardware, Windows version, SQL version, database placement,
and cache conditions. Do not improve timings by omitting history or required calculations.

Before release, record SDK/package/tool versions, license/security review, analyzer results, SQL
results, Windows parity evidence, and measured timings. Document installation and schema upgrade
steps. State recovery and compatibility effects for each migration. Operator backups are optional;
the application does not implement backup management or storage encryption.

## Planning Review Evidence

This planning run checked requirement coverage, document links, stale guidance, and constitution
alignment. It did not execute the proposed application commands, accounting tests, SQL scenarios,
Windows checks, or performance measurements. Those results remain unverified.
