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

| Check                                                            | Result                                                                                                         |
| ---------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| Solution cross-target build, including Desktop and WindowsChecks | Passed; zero warnings/errors                                                                                   |
| FatStacks.Tests                                                  | Latest Linux run: 42 passed, zero failed/skipped                                                               |
| US2 SQL reference/account tests                                  | 3 passed, zero failed/skipped                                                                                  |
| WindowsChecks on Windows x64                                     | User-reported full-suite pass: 33 passed, zero failed/skipped; see [run evidence](windowschecks-2026-11-18.md) |
| Visual comparison and full register UI Automation                | Unverified                                                                                                     |

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

| Case                                                             | Expected                                                     |
| ---------------------------------------------------------------- | ------------------------------------------------------------ |
| Two old openings of 99,999,999.99 and a recent bank credit of 25 | Current/direct balance 199,999,974.98                        |
| Closed child receives 5 in that split                            | Parent tree total 199,999,979.98; child 5.00                 |
| Search `split`, `counter[%_]`, or `[%_]`                         | One split row; its full-history balance stays 199,999,974.98 |
| Equal-date zero-net entry after split                            | Ordered by entry ID; blank Payment/Deposit; Mixed summary    |
| Split details                                                    | All three lines, including the closed child                  |
| Older opening 1,000; cutoff debit 100; today's credit 25         | Two visible rows, balances 1,100 and 1,075; current 1,075    |
| Future debit 100                                                 | Excluded from current balance and visible register           |
| Close date equal to an existing entry date                       | Rejected; following-day closure accepted                     |
| Rename/group override                                            | Same account ID and history; unchanged account type          |
| DST jumps/repeated hours and book/device zone differences        | Correct book-local DateOnly; 90 inclusive days               |
| Delayed obsolete search/session result                           | Cannot replace current visible results                       |
| Repeated empty-book search/refresh                               | No stale cancellation-source failure                         |

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
   overrides. Verify Income and Equity under Other, rename identity, and closure boundaries.
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

## Manual findings and retests

M02 exposed a mismatch between the Tree radio and the displayed hierarchy. M15 reported
loss of an unsaved account name after switching views and filtering. The Accounts changes
make the toggle explicit, guard the editor selection during browser updates, and sort parent
choices by displayed path. The Connections dialog collapses empty feedback, sorts saved
profiles, and uses the existing Open command on profile double-click. Ctrl+F focuses the main
register search.

The full solution cross-build succeeded with zero warnings and zero errors. All 42
FatStacks.Tests cases passed. WindowsChecks compiled. The interactive Windows results are
pending under [M27–M32](manual-us2.md#us2-fix-retests), including confirmation of the M02
and M15 fixes. M21–M26 remain blocked pending journal entry creation. Parent autocomplete
and empty-account deletion are separate feature requests.

The Windows retest log records M27–M31 as passed and M32 as failed. The later manual retests
M33 and M34 are marked passed in [the checklist](manual-us2.md#us2-fix-retests). M33 verifies
Ctrl+F in the main window and Accounts. M34 verifies the Connections Alt access keys.
M32 remains in the log as the original failure.

## Register convergence verification

- T091 adds redacted .NET trace events for handled account and register failures. The
  regression captures a failed register search through a trace listener and verifies
  that the event excludes the search text and raw exception message.
- T092 searches individual counter-account paths and retains the joined display text.
  The SQL regression checks that a term spanning two paths does not match. Both register
  SQL tests passed on a disposable SQL Server 2022 container. The configured fixture and
  the documented server override both returned SQL category 0 during fixture creation.
- T093 preserves the selected entry during overlapping refreshes for the same account.
  The delayed-read regression verifies restoration by entry ID and rejects obsolete
  selection after an account change or disconnect.
- T094 adds Windows automation for register search, closed-account links, refresh, and
  selected Connections access keys. The revised [M06](manual-us2.md#appearance-and-layout) is
  marked passed. The Connections access-key test passed on Windows in Run 1, and the
  register test passed on the targeted Windows rerun. Outstanding parity comparisons
  remain open. M21–M26 remain blocked pending journal entry creation.
- T095 passed SC-008 on the third [Windows benchmark run](performance.md#windowschecks-test-run-3---results).
  Account selection, search, and refresh each finished within two seconds on all 20 runs.

The solution cross-build passed with zero warnings or errors. The portable suite passed
all 44 tests, including the two new regressions. The Windows binaries were built on Linux.

## Reload after an account changes tabs

T096 follows the selected account to its new tab after a display-group change. Reload
preserves the active search and restores the selected transaction when the refreshed
register still contains it. The portable regression moves an account from Banks to Credit
Cards and checks the destination tab and both stable IDs. The regression failed before the
fix. The solution cross-build then passed with zero warnings or errors, and all 45 portable
tests passed.

## Outstanding Windows verification

- Run the populated-book register automation and Connections access-key test on an
  interactive Windows desktop with the SQL fixture configured. Build the solution first
  so the UI automation can launch the app from its default output location.

  ```powershell
  dotnet build FatStacks.slnx
  dotnet test --project tests/FatStacks.WindowsChecks/FatStacks.WindowsChecks.csproj --filter 'FullyQualifiedName~RegisterDesktopTests|FullyQualifiedName~ConnectionsAccessKeysRunNewAndClose'
  ```

- Record any outstanding visual, keyboard, scaling, and accessibility comparisons against
  the [UI parity baseline](../ui-parity.md). Retain the manual passes already recorded.
- Record the [populated-book manual walkthrough](manual-us2.md#register-walkthrough--requires-prepared-synthetic-history)
  when journal entry creation is available. Keep M21–M26 blocked until then.

## Windows Verification Test Run 1 - Results

```

>>   dotnet build FatStacks.slnx
>>   dotnet test --project tests/FatStacks.WindowsChecks/FatStacks.WindowsChecks.csproj --filter 'FullyQualifiedName~RegisterDesktopTests|FullyQualifiedName~ConnectionsAccessKeysRunNewAndClose'
Restore complete (1.2s)
  FatStacks.Core net10.0 succeeded (0.3s) → src\FatStacks.Core\bin\Debug\net10.0\FatStacks.Core.dll
  FatStacks.Infrastructure net10.0 succeeded (0.3s) → src\FatStacks.Infrastructure\bin\Debug\net10.0\FatStacks.Infrastructure.dll
  FatStacks.SqlTests net10.0 succeeded (0.5s) → tests\FatStacks.SqlTests\bin\Debug\net10.0\FatStacks.SqlTests.dll
  FatStacks.Tests net10.0 succeeded (4.2s) → tests\FatStacks.Tests\bin\Debug\net10.0\FatStacks.Tests.dll
  FatStacks.Desktop net10.0-windows succeeded (5.0s) → src\FatStacks.Desktop\bin\Debug\net10.0-windows\FatStacks.Desktop.dll
  FatStacks.WindowsChecks net10.0-windows succeeded (1.6s) → tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll

Build succeeded in 8.6s
Running tests from tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll (net10.0|x64)
failed SearchClosedAccountLinkAndRefreshWorkThroughTheRegister (33s 944ms)
  from tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll (net10.0|x64)
  Assertion failed.
  Desktop control RegisterAccountTitle did not show the expected state.
    at FatStacks.WindowsChecks.Fixtures.DesktopFixture.WaitTextAsync(String id, String expected) in tests\FatStacks.WindowsChecks\Fixtures\DesktopFixture.cs:34
    at FatStacks.WindowsChecks.RegisterDesktopTests.SearchClosedAccountLinkAndRefreshWorkThroughTheRegister() in tests\FatStacks.WindowsChecks\RegisterDesktopTests.cs:62
    at FatStacks.WindowsChecks.RegisterDesktopTests.SearchClosedAccountLinkAndRefreshWorkThroughTheRegister() in tests\FatStacks.WindowsChecks\RegisterDesktopTests.cs:64
    at Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.Execution.TestMethodInfo.ExecuteInternalAsync(Object[] arguments, CancellationTokenSource timeoutTokenSource, TestInvocationState invocationState)
tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll (net10.0|x64) failed with 1 error(s) (35s 474ms)
Exit code: 2

Test run summary: Failed!
  total: 2
  failed: 1
  succeeded: 1
  skipped: 0
  duration: 36s 128ms
Test run completed with non-success exit code: 2. At least one test failed. See: https://aka.ms/testingplatform/exitcodes
```

### Run 1 follow-up

The Connections access-key test passed. The register test reached the closed account and
timed out after Reload while waiting for the renamed account title. Reload now ignores
selection events raised while WPF replaces the account tree. A portable regression
simulates a group header selection during that update and verifies that the account and
transaction remain selected. The Windows test now waits for the closed account's entry
details before invoking Reload and reports the last observed title if the check times out.

All 46 portable tests passed. The Windows test project built with zero warnings or errors.
The register test still needs a Windows rerun. Use the command above with the filter
`FullyQualifiedName~RegisterDesktopTests`.

## Windows Verification Test Run 2 - Results

```
PS C:\Working\Personal\fat-stacks-net>
>>   dotnet build FatStacks.slnx
>>   dotnet test --project tests/FatStacks.WindowsChecks/FatStacks.WindowsChecks.csproj --filter 'FullyQualifiedName~RegisterDesktopTests|FullyQualifiedName~RegisterDesktopTests'
Restore complete (0.7s)
  FatStacks.Core net10.0 succeeded (0.2s) → src\FatStacks.Core\bin\Debug\net10.0\FatStacks.Core.dll
  FatStacks.Infrastructure net10.0 succeeded (0.2s) → src\FatStacks.Infrastructure\bin\Debug\net10.0\FatStacks.Infrastructure.dll
  FatStacks.Desktop net10.0-windows succeeded (0.2s) → src\FatStacks.Desktop\bin\Debug\net10.0-windows\FatStacks.Desktop.dll
  FatStacks.SqlTests net10.0 succeeded (0.3s) → tests\FatStacks.SqlTests\bin\Debug\net10.0\FatStacks.SqlTests.dll
  FatStacks.Tests net10.0 succeeded (0.3s) → tests\FatStacks.Tests\bin\Debug\net10.0\FatStacks.Tests.dll
  FatStacks.WindowsChecks net10.0-windows succeeded (0.3s) → tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll

Build succeeded in 1.8s
Running tests from tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll (net10.0|x64)
failed SearchClosedAccountLinkAndRefreshWorkThroughTheRegister (45s 616ms)
  from tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll (net10.0|x64)
  The expected control did not appear in EntryDetails.
    at FatStacks.WindowsChecks.RegisterDesktopTests.<>c__DisplayClass0_0.<<SearchClosedAccountLinkAndRefreshWorkThroughTheRegister>g__WaitForDescendantAsync|1>d.MoveNext() in tests\FatStacks.WindowsChecks\RegisterDesktopTests.cs:80
    --- End of stack trace from previous location ---
    at FatStacks.WindowsChecks.RegisterDesktopTests.SearchClosedAccountLinkAndRefreshWorkThroughTheRegister() in tests\FatStacks.WindowsChecks\RegisterDesktopTests.cs:60
    at FatStacks.WindowsChecks.RegisterDesktopTests.SearchClosedAccountLinkAndRefreshWorkThroughTheRegister() in tests\FatStacks.WindowsChecks\RegisterDesktopTests.cs:65
    at Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.Execution.TestMethodInfo.ExecuteInternalAsync(Object[] arguments, CancellationTokenSource timeoutTokenSource, TestInvocationState invocationState)
tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll (net10.0|x64) failed with 1 error(s) (45s 988ms)
Exit code: 2

Test run summary: Failed!
  total: 1
  failed: 1
  succeeded: 0
  skipped: 0
  duration: 46s 314ms
Test run completed with non-success exit code: 2. At least one test failed. See: https://aka.ms/testingplatform/exitcodes
```

### Run 2 follow-up

Run 2 reached the closed account and cleared the search. It timed out waiting for the
entry details before Reload was invoked. The register now keeps its selected entry while
the grid replaces its rows and ignores transient selection events during that update.
The Windows test checks the selected journal entry ID before and after Reload separately
from locating a detail link. If a detail link cannot be found, the test reports the
register status.

The solution cross-build succeeded with zero warnings or errors. All 47 portable tests
passed, including a regression that simulates grid deselection during entry restoration.
The user reported that the targeted Windows rerun of `RegisterDesktopTests` passed.

## Windows Verification Test Run 3 - Results

```

PS C:\Working\Personal\fat-stacks-net> dotnet build FatStacks.slnx
Restore complete (0.6s)
  FatStacks.Core net10.0 succeeded (0.2s) → src\FatStacks.Core\bin\Debug\net10.0\FatStacks.Core.dll
  FatStacks.Infrastructure net10.0 succeeded (0.1s) → src\FatStacks.Infrastructure\bin\Debug\net10.0\FatStacks.Infrastructure.dll
  FatStacks.SqlTests net10.0 succeeded (0.2s) → tests\FatStacks.SqlTests\bin\Debug\net10.0\FatStacks.SqlTests.dll
  FatStacks.Tests net10.0 succeeded (0.7s) → tests\FatStacks.Tests\bin\Debug\net10.0\FatStacks.Tests.dll
  FatStacks.Desktop net10.0-windows succeeded (1.3s) → src\FatStacks.Desktop\bin\Debug\net10.0-windows\FatStacks.Desktop.dll
  FatStacks.WindowsChecks net10.0-windows succeeded (0.7s) → tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll

Build succeeded in 3.0s
PS C:\Working\Personal\fat-stacks-net> dotnet test --project tests/FatStacks.WindowsChecks/FatStacks.WindowsChecks.csproj --filter 'FullyQualifiedName~RegisterDesktopTests' --output Detailed
Running tests from tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll (net10.0|x64)
passed SearchClosedAccountLinkAndRefreshWorkThroughTheRegister (7s 440ms)
  from tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll (net10.0|x64)
tests\FatStacks.WindowsChecks\bin\Debug\net10.0-windows\FatStacks.WindowsChecks.dll (net10.0|x64) passed (8s 016ms)

Test run summary: Passed!
  total: 1
  failed: 0
  succeeded: 1
  skipped: 0
  duration: 8s 464ms
```
