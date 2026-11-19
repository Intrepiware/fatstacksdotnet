# US2 validation

Date: 2026-11-18. Scope: US2 only (T028–T036 and overlapping T071/T072).
Runner: Linux x64, .NET SDK 10.0.115; isolated SQL Server 2022 container.

## Implemented

- Account creation, rename, hierarchy, display-group override, and close through EF.
  The eleven type mappings preserve the five classes. Names are trimmed; invalid names,
  duplicate siblings, cycles, cross-book parents, type changes, and dates invalidating
  existing journal lines are rejected. Identity and closed history are retained.
- Book-local calendar through TimeProvider and Windows TimeZoneInfo IDs.
- ReadAccountRegister calculates widened full-history balances before applying the inclusive
  90-day window and literal case-insensitive search. Zero-net rows remain visible.
- Batch tree totals include closed descendants. Entry details include every line and account link.
- Desktop account setup, hierarchical tree, six ordered tabs with existing Fluent icons,
  register, search, details, currency, Payment/Deposit tooltips, F5, stable selection,
  cancellation, and rejection of obsolete/session results. Journal maintenance belongs to US3.

## Automated evidence

Commands from the repository root (SQL credentials are supplied externally):

```bash
dotnet restore FatStacks.slnx --force -p:RestoreFallbackFolders= -p:NuGetAudit=false
dotnet build FatStacks.slnx --no-restore
dotnet test --project tests/FatStacks.Tests --no-build
dotnet test --project tests/FatStacks.SqlTests --no-build -- --filter 'FullyQualifiedName~RegisterTests|FullyQualifiedName~AccountServiceTests'
```

Current recorded results (including subsequent follow-ups):

| Check | Result |
| --- | --- |
| Solution cross-target build, including Desktop and WindowsChecks | Passed; zero warnings/errors |
| FatStacks.Tests | Latest Linux run: 42 passed, zero failed/skipped |
| US2 SQL reference/account tests | 3 passed, zero failed/skipped |
| WindowsChecks on Windows x64 | User-reported full-suite pass: 33 passed, zero failed/skipped; see [run evidence](windowschecks-2026-11-18.md) |
| Visual comparison and full register UI Automation | Unverified |

The initial five account/calendar tests built and failed with NotImplementedException before
implementation. They subsequently passed. Initial configured SQL fixture access failed with
sanitized SQL category 0 before reaching any assertion; no SQL correctness claim follows from
that failure. Subsequent reference runs used a disposable local SQL Server, randomly named
fixture databases, and synthetic balanced entries. No journal-write procedure was needed.
A stale Windows NuGet fallback path required a fresh restore before cross-target compilation.
NuGet auditing was disabled for that restore; this run makes no dependency-audit claim.

The platform-neutral account/register view models and journey tests are linked into
FatStacks.Tests so their behavior executes on Linux as well as in the Windows project.
This does not substitute for WPF control or Windows UI Automation acceptance.

Reference cases:

| Case | Expected |
| --- | --- |
| Two old openings of 99,999,999.99 and a recent bank credit of 25 | Current/direct balance 199,999,974.98 |
| Closed child receives 5 in that split | Parent tree total 199,999,979.98; child 5.00 |
| Search `split`, `counter[%_]`, or `[%_]` | One split row; its full-history balance stays 199,999,974.98 |
| Equal-date zero-net entry after split | Ordered by entry ID; blank Payment/Deposit; Mixed summary |
| Split details | All three lines, including the closed child |
| Older opening 1,000; cutoff debit 100; today's credit 25 | Two visible rows, balances 1,100 and 1,075; current 1,075 |
| Future debit 100 | Excluded from current balance and visible register |
| Close date equal to an existing entry date | Rejected; following-day closure accepted |
| Rename/group override | Same account ID and history; unchanged account type |
| DST jumps/repeated hours and book/device zone differences | Correct book-local DateOnly; 90 inclusive days |
| Delayed obsolete search/session result | Cannot replace current visible results |
| Repeated empty-book search/refresh | No stale cancellation-source failure |

## Windows execution passed; visual acceptance pending

Use the [manual US1/US2 checklist](manual-us2.md) for the remaining hands-on steps and
expected results. Record Pass, Fail, or Blocked for each applicable case.

The user supplied a successful unfiltered WindowsChecks run: 33 succeeded, zero failed/skipped,
57s 170ms summary duration. The exact output and attribution are recorded in
[WindowsChecks evidence](windowschecks-2026-11-18.md). Earlier follow-up sections describe
what was executable on the Linux runner at the time; their Windows-suite execution gap is
superseded by this user-run result.

T036 and overlapping T072 remain unchecked for outstanding visual comparisons. The passing
suite includes account-dialog WPF tests and the platform-neutral register journey; it does not
establish full register UI Automation or a running-port screenshot comparison. The reference
source and supplied screenshot were inspected during the later reference-alignment follow-up.

To repeat the focused checks on an unlocked Windows desktop:

```powershell
dotnet test --project tests/FatStacks.WindowsChecks -- --filter 'FullyQualifiedName~RegisterJourneyTests|FullyQualifiedName~AccountSetupWindowTests'
```

Then record screenshots and outcomes for P01–P10 and P18–P22:

1. Initialize an isolated book; create all eleven account types, nested accounts, and group
   overrides. Verify Income/Equity under Other and All, rename identity, and closure boundaries.
2. Seed balanced synthetic history using the reference fixture; compare balances, literal search,
   zero-net rows, split details, and closed-account links. Follow a detail link and confirm the
   same entry stays selected. Refresh and confirm account/tree/entry selection.
3. Switch books while reads are pending. Confirm accounts, rows, details, and selections clear;
   no old read publishes into the new book. Try failed reads and a disconnected empty book.
4. Compare source/port at 1360×860 and 960×600, plus high DPI. Check ordered icon tabs,
   split panes, alternating dense rows, numeric alignment, negative coloring, and currency.
5. Check Ctrl+O, F5, keyboard tab order, tree navigation, search typing, visible focus,
   accessible names, large splits, and virtualization. Record any accepted departures.

Existing development databases initialized before this increment need explicit grate maintenance
with the updated package and runtime-role grants. Normal Open and repeated Initialize do not
install missing procedures in an already initialized compatible book. Never deploy during Open.

## Hooks and boundaries

`.specify/extensions.yml` is absent; no before/after implementation hooks are registered.
Applied table migrations and checklist markers were left unchanged. US1/US3–US5 work remains
outside this implementation. Feature-wide acceptance and performance measurements are not claimed.

## Accounts dialog follow-up

Added Alt+N (New), Alt+S (Save), Alt+C (Close), Alt+F (Find), Alt+T (Tree),
and Alt+A (Alphabetical). New focuses the account-name field. Both browser views show
account-type icons, and the type picker uses the same icons. Icons reuse the retained
Fluent vector shapes with small vector badges for related account types.

Search matches names, full paths, and account types without regard to case. Alphabetical
results sort by account name, then full path. Tree results retain ancestors as context.
Filtering and switching views preserve the selected account and unsaved editor fields,
even when the selected account is hidden by a filter.

Validation: solution build passed with zero warnings/errors; FatStacks.Tests passed all
37 cases. The added regression covers sorting, matching descendants and types, ancestor
context, empty results, selection synchronization, and retained drafts. WPF tests for
access-key activation, focus, filtering, and view switching compile but remain unexecuted
on this Linux runner. The earlier SQL results are unchanged; this follow-up changes no
SQL behavior.

## Shutdown regression

The active BookSession was registered as Autofac-owned while BookSessionCoordinator also
owned its disposal. Closing a resolved book scope reproduced ObjectDisposedException from
CancellationTokenSource.Cancel in BookSession.Dispose: the coordinator disposed the session,
then scope cleanup disposed it again. The registration is now ExternallyOwned, and session
and register view-model disposal are idempotent. Disposing a session also prevents new writes.

The production book-scope registrations live in BookScopeModule so ownership can be exercised
without WPF. Before the fix, both ownership regression tests failed; after the fix all 40
FatStacks.Tests cases passed. A full solution build using an isolated artifacts directory passed
with zero warnings/errors. Windows end-to-end close execution remains unverified on Linux.

```bash
dotnet build FatStacks.slnx --artifacts-path /tmp/fatstacks-close-fix -p:RestoreFallbackFolders= -p:NuGetAudit=false
dotnet /tmp/fatstacks-close-fix/bin/FatStacks.Tests/debug/FatStacks.Tests.dll
```

The deterministic test project reuses the existing Autofac 9.3.4 dependency to exercise the
actual production scope registrations, including resolved account/register view models.

## Reference screenshot alignment

The supplied reference screenshot is
`.workspace/ph01-us02/screenshots/Screenshot 2026-11-18 205324.png`.
The reference source became available under
`.workspace/dotnet-beancount/src/FatStacks.Desktop/` for this follow-up.
The shell now follows its MainWindow.xaml and retained Fluent geometry resources:

- File menu, compact branded header and toolbar, 290-unit account rail, narrow splitter,
  default/minimum window sizes, Segoe UI 13, and the original palette and spacing.
- Six icon-only account tabs at the top of the rail, regular/filled icons, selected background,
  amber underline, hover/focus states, tooltips, accessible names, and All as the default.
- All groups accounts into the five stored display groups. Account names and balances occupy
  separate lines, including currency and negative coloring. Closed accounts remain accessible.
  Account hierarchy and names come from stored metadata; no synthetic accounting paths are saved.
- Register heading with a separate full path, right-aligned current balance, shaded 90-day/search
  toolbar, proportional description/category columns, narrow C column with full-status tooltips,
  right-aligned monetary headers and values, alternating rows, and the reference selection colors.
- Centered empty/loading/failure messages, with the actual book-local date range for no matches.
  Entry details appear only for a selected row and use a bounded scrollable grid. Connection
  warnings occupy the bottom of the register, above the status bar, as in the reference.

Adaptations retain the SQL application requirements: Accounts/Connections/Reload replace the
file/editor actions; the badge reads REGISTER; profile/book identity remains visible in the shell;
Debit/Credit, line statuses, and stable journal identity remain in details. Account-dialog search,
view toggles, icons, shortcuts, uniform fields, and the shutdown ownership fix remain in place.
No journal-editor or file-import capability is implied by the updated toolbar.

Added behavioral checks cover All grouping, one canonical selection for an account shared as an
ancestor across groups, closed descendant balances, leaf titles/full paths, group navigation,
empty group selection, and distinct loading/no-match/failure states. A Windows screenshot of the
updated application and high-DPI/keyboard visual acceptance still require an interactive runner;
static source/screenshot review is not reported as executed visual acceptance.

Final reference-alignment verification: full solution build passed with zero warnings/errors;
all 42 FatStacks.Tests cases passed, zero failed/skipped. Validation used isolated artifacts
under `/tmp/fatstacks-reference-parity` to avoid the running Windows app's shared output files.

## Requested startup defaults

The main register now opens on Banks. Each new Accounts dialog opens in tree view;
users can still switch to alphabetical view. These choices supersede the earlier default
of All for reference matching. The existing view-model regressions now assert both defaults.
Full solution build: zero warnings/errors. FatStacks.Tests: 42 passed, zero failed/skipped.
Raw WindowsChecks output may be pasted into the development conversation or saved under
`.workspace/ph01-us02/unit-test-logs/`; see docs/testing.md for the PowerShell capture command.
