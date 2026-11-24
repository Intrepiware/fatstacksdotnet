# US3 validation

Date: 2026-11-21. Scope: monetary journal maintenance, T037–T047 and overlapping T073.
Runner: Linux x64, .NET SDK 10.0.115, SQL Server 2022 16.0.4325.4 in an isolated container.
US4 reconciliation controls and US5 security workflows are outside this implementation.

## Implemented

- Typed requests carry the full final lines, original line/version set including removed lines,
  and loaded header version. Core validates decimal values before SQL parameter conversion,
  book currency, eligible account dates, and balanced totals. Validation returns field/line errors.
- SaveJournalEntry and DeleteJournalEntry use serializable transactions and full version checks.
  Saves preserve retained line IDs, return refreshed versions, reset only financially changed
  reconciled lines, and permanently lock currency. Deletion removes lines and header physically.
  Security entry kinds are rejected until US5 supplies their holding-history workflow.
- StoredProcedureRepository binds all values, including typed TVPs, and returns Success only
  after the server confirms commit. Each service write holds its original session's write lease.
  Failures preserve drafts and report sanitized outcomes; no uncertain-outcome framework was added.
- The editor provides debit/credit split lines, account substring suggestions and keyboard
  acceptance, line errors, Save/Continue/Cancel, Ctrl+Enter, and pending-save exclusion.
  Continue preserves the accounting date; new dialogs start at book-local today. The separate
  bank posting date is optional. Delete previews all lines and their states with Cancel focused.
- The shell provides New/Edit/Delete, N/E/Delete typing guards, double-click editing, and
  refresh with selection restored by identity. The account picker is ported from the supplied
  baseline. The dialog adds space for separate debit/credit and bank-date fields.

## Executed evidence

| Check | Result |
|---|---|
| Requirements checklist | 16 checked, 0 unchecked; read-only gate passed |
| Solution restore and cross-target build | Passed; zero warnings/errors |
| Portable tests, including linked editor behavior tests | Latest run: 99 passed; 0 failed/skipped |
| US3 JournalWriteTests | Latest run: 9 passed; 0 failed/skipped |
| Broader SQL regression run excluding dedicated transport fixtures | 33 passed; 0 failed/skipped before adding three currency cases |
| Procedure Git attributes | Both definitions have text set and merge unset |
| Windows journal tests on this runner | Not executed: WindowsDesktop runtime unavailable; test host exit 150, zero tests ran |
| Windows visual, keyboard, focus, high-DPI, accessibility parity | Unverified |

Commands from the repository root; supply maintenance/runtime credentials externally:

```powershell
dotnet restore FatStacks.slnx
dotnet build FatStacks.slnx --no-restore
dotnet test --project tests/FatStacks.Tests/FatStacks.Tests.csproj --no-build
dotnet test --project tests/FatStacks.SqlTests/FatStacks.SqlTests.csproj --filter 'FullyQualifiedName~JournalWriteTests'
dotnet test --project tests/FatStacks.SqlTests/FatStacks.SqlTests.csproj --filter 'FullyQualifiedName!~TransportTests'
dotnet test --project tests/FatStacks.WindowsChecks/FatStacks.WindowsChecks.csproj --filter 'FullyQualifiedName~Journal'
```

The first validation run compiled with a permissive validator: three of four financial tests
failed on missing behavioral rejection, then passed after implementation. A portable editor test
also caught Continue clearing the draft without adding its next two lines while busy; the
regression now passes. The first configured host SQL attempt failed in fixture connection setup
with sanitized SQL category 0, before assertions. The isolated SQL deployment then exposed grate's
missing default types stage. Mapping RunFirstAfterUp to types made deployment and SQL tests pass.
Temporary deployment diagnostics were removed. No credentials or real financial data were used
in the fixtures. No package versions or applied one-time migrations changed.

## Exact accounting and write evidence

The SQL reference case uses these entries on the same date, ordered by entry identity:

| Operation | Bank | Equity | Expenses | Income | Card |
|---|---:|---:|---:|---:|---:|
| Bank opening: debit Bank 1,000, credit Equity 1,000 | 1,000 | -1,000 | 0 | 0 | 0 |
| Expense: debit Expense 25, credit Bank 25 | 975 | -1,000 | 25 | 0 | 0 |
| Salary: debit Bank 500, credit Income 500 | 1,475 | -1,000 | 25 | -500 | 0 |
| Card payment: debit Card 100, credit Bank 100 | 1,375 | -1,000 | 25 | -500 | 100 |
| Split expense: debits 20 + 5, credit Bank 25 | 1,350 | -1,000 | 50 | -500 | 100 |
| Edit original expense from 25 to 30 | 1,345 | -1,000 | 55 | -500 | 100 |
| Physically delete the edited expense | 1,375 | -1,000 | 25 | -500 | 100 |

The test asserts the final five account balances and the retained split/edited identities.
It verifies literal apostrophes and SQL-like descriptions persist without executing commands.
Reordering preserves IDs and reconciliation states; changing one account resets only that line
and returns its ID. Removed lines are deleted and added lines receive new identities.

Validation tests cover 99,999,999.99, rejection of 100,000,000.00, zero/negative/two-sided amounts,
imbalance, foreign currencies, invalid/closed account dates, full original versions, and totals
larger than one line. SQL accepts a balanced total of 199,999,999.98 across four maximum-sized
lines. USD/EUR accept 1.2300 exactly and reject 1.001; JPY accepts 123 and rejects 1.23 at both
service and procedure boundaries.

Conflict tests cover stale headers, omitted original lines, and independent external line
changes without a header update. Failed writes preserve the newer data. SQL-trigger failure
injection runs only in randomly named fixture databases: an insert trigger aborts create, an
update trigger aborts edit after header work, and a header delete trigger aborts delete after
line removal. Tests verify no orphan/partial entries, unchanged stored content and loaded
versions after rollback, and no premature currency lock. Deleting the last entry leaves the
currency locked. A separate runtime login successfully calls both procedures and TVP types,
and CREATE TABLE is denied with SQL category 262.

Portable editor tests verify typed/text field failures without dispatch, retained failed and
conflicting drafts, a single pending submission, Continue date retention, Cancel without a
write, and no success for an unconfirmed commit. Journal saving has no profile-store dependency;
existing profile/session tests independently cover preference-write failure and usable activation.
These portable tests do not establish WPF binding, focus, or rendered keyboard behavior.

## Remaining Windows acceptance

T037–T046 are implemented. T047 and overlapping T073 stay unchecked until Windows execution and
required parity evidence are recorded. JournalJourneyTests automates opening, Continue, expense,
stale-save draft retention, edit, cancelled/confirmed delete, split, and restart. An STA window
check covers bound amount edits, account-popup keyboard routing, and initial delete focus.
Both compile, but neither was executed on Windows in this run.

On an interactive Windows 11 desktop, configure the isolated SQL fixture credentials described
in [testing.md](../../../docs/testing.md), build, and run the Windows journal command above.
Then record these repeatable manual checks and screenshots for P10–P17:

1. Create an opening of 1,000 using N. Use substring account search, Up/Down, Enter, Tab, Escape,
   and mouse acceptance. Ensure typing N/E/Delete in text controls edits text only.
2. Continue into an expense of 25 with the same date. Start a new dialog and check book-local
   today. Set a separate bank posting date and verify both dates after reopening the entry.
3. Add/remove split lines; retain at least two. Tab from the last Credit field while unbalanced
   must add a line and focus its account. Verify scrolling, stable focus, accessible names,
   long account paths, and a long split at 100%, 150%, and 200% scaling.
4. Use Ctrl+Enter to save. While a save is pending, repeat Save/Continue and attempt closing;
   only one write may occur. Fail a save and induce a conflict; retain all draft text and show
   the relevant errors. An unconfirmed response must never display success.
5. Edit expense 25 to 30; verify Bank 970 before other expenses. Verify changed reconciled
   lines disclose their reset while unchanged lines retain status. Status controls belong to US4.
6. Delete must preview date, description, all debit/credit lines and statuses. Verify Cancel has
   initial focus and changes nothing. Confirm deletion and verify Bank 1,000, no residual lines,
   updated balances, valid register selection, and identical values after restarting.
7. Compare the rendered dialog with the supplied NewTransactionWindow/DeleteTransactionWindow
   baseline and record visual, keyboard, screen-reader, and high-DPI results. Cross-build and
   portable tests do not substitute for this target-platform review.

## Deployment and constraints

See [database guidance](../../../database/README.md#us3-journal-package-compatibility).
Existing books need explicit maintenance deployment of the current types/procedures/grants.
Ordinary Open and repeated Initialize of an already compatible Book do not install updates.
No table migration or stored schema-version change is needed for this additive package.
The transaction boundary, physical deletion, decimal rules, UI-independent Core, operation-owned
connections, sanitized diagnostics, and absence of new dependencies follow constitution 3.0.0.
No exception or release acceptance is claimed. No extension hooks were registered.

## User review: 2026-11-21 journal editor screenshot

Source: `.workspace/ph01-us03/screenshots/Journal Entry Screen - 2026.11.21-16.30.png`.
The owner requires Tab from Description to focus the first posting account input; the explicit
keyboard sequence is now in FR-013, the desktop contract, and P14. T098 tracks implementation
and Windows focus regressions. The screenshot does not establish keyboard behavior passed.

The narrow boxes at row ends are Remove posting buttons. Their × labels are clipped by the
global button padding in the narrow column. The grey up/down arrows above Save are the error
area ScrollViewer's vertical scrollbar, which is visible even without errors. Both presentation defects have been corrected; Windows verification remains pending. The current read-only
Uncleared/Cleared display is expected for US3; editable reconciliation belongs to US4.

## Application-wide close shortcut

Ctrl+W is registered once for all WPF Window subclasses through ApplicationCommands.Close.
The handler calls the target window's normal Close method, preserving existing closing guards,
connection cancellation, and main-window disconnect. It does not save a draft or close a dialog's
owner. Added Windows regressions cover routing from a text field, keeping an owner open,
pending-journal-save exclusion without repeat submission, and normal main-window disconnect.
Windows execution remains unverified on the Linux runner. On Windows, run:

```powershell
dotnet test --project tests/FatStacks.WindowsChecks/FatStacks.WindowsChecks.csproj --filter 'FullyQualifiedName~CloseShortcut'
```

Manually press Ctrl+W from a text field in each dialog and the main window. Verify the active
window alone follows its title-bar close behavior; pending journal writes must keep their dialog
open until completion. This change does not complete T098's separate Tab-order follow-up.

## Journal keyboard and presentation fixes

The editor uses a three-row Grid with visual and keyboard content order matching the form:
header, posting rows, then actions. Tab from Description reaches the first account input;
posting fields follow Account, Debit, Credit by row, with reverse Shift+Tab navigation.
Scroll containers are excluded from Tab stops. Ctrl+Shift+Delete removes the focused posting
when at least two lines will remain and focuses the adjacent account. Tab-to-add and account
suggestion acceptance remain available. Reconciliation status remains read-only for US3.

Remove posting buttons use zero internal padding and a larger × glyph, and are disabled when
only two rows remain. The error-area scrollbar appears only when errors need scrolling.
Added a Windows regression for forward/reverse field focus, remove-glyph layout and minimum-line
behavior, and hidden/overflowing error scrollbars. T098 remains open for interactive Windows
execution; the implementation and regression source are complete.

Follow-up cross-build passed with zero warnings/errors; all 57 portable tests passed. The new
Windows focus, layout, and close-command tests compile but remain unexecuted on this Linux runner.

## Journal date shortcuts

Both date fields accept `-` (previous day), `+` (next day), and `T`/`t` (today in the active
book's time zone). Text-input routing recognizes the actual typed character, including shifted
main-keyboard plus and numeric-keypad signs. Shortcuts affect only the focused date field.
Stepping uses the currently displayed date, or book-local today if blank. Invalid text is retained
with field feedback for stepping; T replaces it with today. Date limits report an error without
wrapping or crashing. Journal busy-state exclusion still applies.

BookCalendar is injected from the captured book scope; today is evaluated on each shortcut,
including after midnight and while Continue retains an earlier accounting date. Portable tests
cover leap-day/year crossings, empty fields, independent dates, date limits, busy exclusion, and
book time-zone midnight transitions. A Windows regression raises actual preview text-input events
on the DatePicker textboxes. Its execution remains pending on Windows.

Manual Windows check: focus each date textbox and try main-keyboard `-`, shifted `+`, keypad
subtract/add, lowercase/uppercase T, a blank bank date, and invalid typed text. Verify focus stays
in the textbox, the other date does not change, and today follows the book's configured zone.

Date-shortcut verification: the solution cross-build passed with zero warnings/errors and
all 59 portable tests passed. The ordinary Desktop output was locked by a running Windows
application, so verification used isolated artifacts via
`dotnet build FatStacks.slnx --artifacts-path /tmp/fatstacks-date-shortcuts`, followed by
`dotnet /tmp/fatstacks-date-shortcuts/bin/FatStacks.Tests/debug/FatStacks.Tests.dll`.
Close the running app before rebuilding its normal output. The new Windows text-input
regression compiled; interactive Windows execution remains pending.

## Register payee and description display

Owner screenshot: `.workspace/ph01-us03/screenshots/Journal Entry Screen - 2026-11-21 172755.png`.
The previous register returned only Description, which left entries with only Payee visually
blank. ReadAccountRegister now returns Payee as an additional trailing result column, mapped
through StoredProcedureRepository into RegisterRow. The register column is Payee / Description,
and its shared template is also used for the selected-entry summary. Payee appears in normal
text, with an em dash and italic description when supplied. Either field can display alone;
empty/whitespace-only fields add no separator. Dense row heights, full-history balances, search
scope, and ordering remain as specified. Full summary text is available in tooltips and accessible
names when visual text is truncated.

Portable tests cover both fields, payee-only, description-only, and whitespace cases. A real SQL
regression covers literal payee text, null payees, blank descriptions, returned summaries and
unchanged balances after description filtering. A Windows template regression checks actual
inline text and italic styling; journal UI row selection accepts the combined display text.
Windows execution remains pending on this runner.

For an existing test book, deploy the updated `database/sprocs/ReadAccountRegister.sql` with
maintenance privileges (execute that CREATE OR ALTER definition in SSMS, or redeploy the grate
package) before running the new application build. No table migration or schema-version change
is required. Existing procedure grants are retained by CREATE OR ALTER. The new reader expects
the additional Payee column.

Payee-display verification: solution cross-build passed with zero warnings/errors, all 65
portable tests passed, and all 3 SQL register cases passed against isolated SQL Server 2022.
Windows inline-style/visual execution remains unverified.

## Register rendering regression: 2026-11-21

The owner reported `.workspace/ph01-us03/screenshots/Error 2026-11-21 173851.png`: selecting
an account with transactions triggered the global application-error dialog after rows loaded.
The new payee summary template bound Run.Text to read-only computed properties without specifying
a mode. WPF Run.Text metadata uses BindsTwoWayByDefault, causing rendering to throw when binding
to those properties. All three summary Run bindings now explicitly use Mode=OneWay.
This is a desktop-only fix; it needs no additional database deployment.

The existing real-template Windows regression now also checks that every summary Run uses a
one-way binding, alongside rendered payee/description text, italics, and changing row contexts.
Targeted Windows execution remains unverified on Linux. On Windows, run:

```powershell
dotnet test --project tests/FatStacks.WindowsChecks/FatStacks.WindowsChecks.csproj --filter 'FullyQualifiedName~RegisterSummaryRendersReadOnly'
```

Rebuild/reopen the app, select accounts with transactions, and verify payee-only, description-only,
and combined rows plus the selected-entry summary render without the global error dialog.

Rendering-fix verification: the complete solution cross-build, including the strengthened Windows
regression, passed with zero warnings/errors. Interactive Windows execution remains pending.

## Accounts and journal entry usability follow-up

- Account Type uses WPF name-prefix text search, including icon-template items, while remaining
  a controlled list whose type is fixed after creation. Focus the selector and type e.g. `exp`.
- Successful Continue keeps the saved accounting date and returns focus to the Date textbox
  after controls re-enable. New creation dialogs still start at book-local today; editing retains
  the stored date. Failed/unconfirmed saves do not reset the draft or trigger this focus change.
- Posting-account inputs select all text when entered by keyboard or the first mouse click.
  Typing replaces the current name and starts a fresh substring search. Clicking again while
  already focused permits normal caret positioning and selection.
- Debit/Credit format exact values with two decimal places on focus loss using the current
  culture (`25` becomes `25.00`). Empty/invalid text remains intact. Nonzero precision beyond
  two places remains intact for validation instead of silently rounding it away.

Added portable formatting cases for both amount fields, trailing zeros, excess precision,
empty/invalid text, and decimal-comma culture. Windows regressions cover type-prefix input,
initial/Continue date focus and retained dates, failed Continue, account keyboard/mouse selection,
replacement search, and formatting when focus moves off an amount field. These Windows cases
compile but require execution on Windows; no database deployment is needed for this follow-up.

Usability follow-up verification: solution cross-build passed with zero warnings/errors and all
74 portable tests passed. Build artifacts were isolated under `/tmp/fatstacks-entry-usability`
so the running Windows application did not lock verification outputs. The Windows prefix,
selection, focus, Continue, and blur-formatting regressions compile but remain unexecuted here.
Account suggestion acceptance also retains its text binding and advances from the inner account
textbox to the next amount field; the Windows focus regression covers that path.

## Phase 13 convergence implementation

Scope: T102–T106 and overlapping US3 validation. The requirements checklist remains 16 checked,
0 unchecked. No extensions.yml hooks are registered. Existing staged work was preserved.

| Check | Result |
|---|---|
| Solution cross-build, including WindowsChecks | Passed; 0 warnings/errors |
| Portable tests | 99 passed; 0 failed/skipped |
| Targeted journal, diagnostic, and register SQL tests | 13 passed; 0 failed/skipped against isolated SQL Server 2022 |
| Windows test-host attempt | Exit 150: Microsoft.WindowsDesktop.App is unavailable on Linux; no Windows cases executed |
| Windows visual/focus/accessibility evidence | Pending; see [US3 parity checklist](ui-parity.md) |

T102 now uses one focused input-precision check for formatting, Save/Continue conversion, and
unbalanced-entry navigation. Nonzero excess fractional digits are rejected before decimal parsing
can round them. Four regression cases first dispatched a write incorrectly, then passed with
zero dispatches after the fix; trailing-zero cases in decimal-point/comma cultures pass without
changing the value. Original input and field/line errors are retained.

T103 adds fixed operation/category trace events for failures caught before repository dispatch,
journal loads, editor fallback failures, and delete-dialog fallback failures. Core uses only
.NET trace/base exception categories; it has no Desktop/Infrastructure dependency. Routine
cancellation produces no failure event. Repository failures return outcomes, so outer layers do
not report them again. Five missing-event regressions failed before implementation; all nine
portable diagnostic cases now pass. A real SQL trigger raises synthetic private sentinel text;
the resulting operation emits one SqlFailure event with no provider text, supplied financial
values, or connection details. The delete-dialog listener case compiles but requires Windows.

T104 filters choices per posting and accounting date, preserves historical account labels, and
updates the popup's bound choices when the date changes. New references exclude closed accounts;
only the same loaded line/account may retain a closed reference before its closure. Core reads
the stored original entry and compares the header/full original version set before evaluating
historical-reference eligibility. This preserves Conflict for stale edits. SQL remains the final
transactional authority. Field errors distinguish unavailable accounts, dates before opening,
and closed references. All four new portable eligibility regressions first failed and now pass.
The SQL regression verifies invalid replacements leave header/line versions unchanged, both
Core and the direct procedure reject them, valid historical references remain editable, and
closure-date/new-reference rejections do not add records.

T106's notice implementation now maps reset IDs to returned line orders and account paths in
both editor and main-window notices. Five reset-label cases first failed; all six presentation
cases now pass, covering one/multiple resets, unchanged lines, no-reset success, Save/Continue,
and refreshed paths. The rendered Windows notice evidence remains pending, so T106 stays open
with T105. T047/T073/T098 also remain open for their target-platform evidence. All US4/US5 work
remains outside this pass.

Verification commands (isolated SQL credentials were supplied externally):

```bash
dotnet build FatStacks.slnx --artifacts-path /tmp/fatstacks-us3-convergence
dotnet /tmp/fatstacks-us3-convergence/bin/FatStacks.Tests/debug/FatStacks.Tests.dll
dotnet build tests/FatStacks.SqlTests/FatStacks.SqlTests.csproj --artifacts-path .workspace/us3-convergence-build
dotnet .workspace/us3-convergence-build/bin/FatStacks.SqlTests/debug/FatStacks.SqlTests.dll --filter 'FullyQualifiedName~Journal|FullyQualifiedName~RegisterTests'
```

The SQL fixture creates/drops only randomly named synthetic test databases. It uses separate
maintenance and runtime identities. No configured user database was modified. Production SQL
and stored schema version are unchanged; this pass requires an application rebuild only.

Constitution self-review: financial text is checked before conversion, journal rules remain in
Core/SQL, operation-owned contexts and original-session write leases remain intact, diagnostics
use safe categories, and no dependency/framework was added. Field errors and reset notices
provide user context without financial data in logs. The cross-build and synthetic test evidence
are distinct from pending Windows acceptance; no full US3 acceptance or constitution exception
is claimed.

## Reported Windows run: amount assertion correction

The user reported a successful Windows solution build and a filtered WindowsChecks run with
35 tests: 34 passed, 1 failed, and 0 skipped. `OpeningExpenseSplitEditFailureContinueAndDelete`
failed at the retained-draft assertion: it expected `30`, but the field displayed `30.00`.
The displayed value agrees with the required two-decimal amount formatting.

The journey assertion now expects 30 formatted with two decimal places in the current culture.
It still checks that the edited amount survives the save conflict. Application code is unchanged.
The initial failed run did not establish the later edit/delete/split/restart steps or the
remaining interactive parity checks.

The user subsequently reported that all tests passed when the same filtered WindowsChecks
command was rerun after the assertion correction. The preceding run selected 35 tests; the
rerun's full output and duration were not supplied. This records a user-reported pass for the
filtered automated suite, including the complete journal journey. It does not establish the
separate manual visual, screen-reader, long-split, or display-scaling checks in `ui-parity.md`.
Those checks remain unverified, and T107 remains open for the remaining acceptance evidence.

## J06 reset warning and Save confirmation follow-up

The user recorded J06 as failed: Continue showed the reset notice in the editor, but Save
closed the editor without a visible notice. The register status bar could clip that text.
The editor now shows a yellow warning before a financial edit resets a Cleared or Reconciling
line. The warning names affected line orders and account paths, updates as the draft changes,
and clears if the original values return. After Save, a separate yellow register banner
shows the actual reset result and has a Dismiss button. Continue still shows its result in
the editor. The manual J06 steps now check both messages and the saved line states. The
original failure remains recorded in `manual-us3.md` until a Windows retest passes.

The solution cross-build passed with zero warnings or errors. All 101 portable tests passed,
including the reset-warning and banner-state regressions. The new Windows editor-rendering
regression compiled but has not run here; the current runner is Linux. The changed UI and J06
manual steps remain unverified on Windows. No SQL procedure or schema changed.

The user then reported that all 36 WindowsChecks tests passed on Windows after this change.
This includes the new editor warning rendering test. The full run output, versions, and
duration were not supplied. J06's hands-on check of the warning, the register banner after
Save, and the saved line states remains unverified until its retest is recorded.

## J07 delete dialog layout follow-up

The user supplied screenshots of the source delete dialog and the J07 dialog. The source uses
a compact gray summary card, plain posting rows, and a red Delete button. The journal dialog
now uses that layout. It shows the date, payee when present, description, optional bank posting
date, and ordered account paths in the card. It keeps separate Debit and Credit columns and
each line's status. A zero amount leaves its column blank. Cancel remains the
focused, safe default. The Delete action is red and labeled Delete. The existing guarded,
atomic delete workflow is unchanged.

The solution cross-build passed with zero warnings or errors. A new Windows rendering check
for the card details, separate amounts, button label and color, and Cancel focus compiled.
It has not run on this Linux runner. The J01/J07 comparison and new Windows check remain
unverified on the revised build. The 36 previously reported WindowsChecks passes preceded
this dialog change. No SQL schema or procedure changed.

The requested status refinement places C between Account and Debit. Each line shows a blank
marker for Uncleared, R for Reconciling, or a checkmark for Cleared. The full status name is
available in the tooltip and accessibility name. The preview no longer has a currency column.
The revised Windows rendering check covers all three markers and separate amounts; it has
compiled but has not run on this Linux runner. J07 visual review remains open.

The delete preview now places the payee and italic description on one wrapping line below
the date. It inserts a separator only when both values exist. The “Journal entry lines”
heading is removed, so the column headings lead directly into the postings. J07 step 5/6
now checks this spacing. Windows rendering checks cover both values together and each value
alone; target-platform execution and visual review remain open.

## J08 Payee and Description accessible names

The user reported that focusing Payee or Description did not announce the field label.
Both text inputs now expose explicit accessible names. Their existing labels and access
keys remain connected to the inputs. A Windows regression checks each focused control's
automation name before and after text entry. J08 includes a focused Narrator retest;
the spoken announcements remain unverified until the Windows retest is recorded.

The WindowsChecks project and its dependencies cross-build with zero warnings or errors.
The new automation-name regression compiled but cannot execute on this Linux runner.

## J08 validation message spacing

The J08 step 7 screenshot showed a missing space after the middot in line errors and a
stray `Line ·` prefix on the entry-wide Balance error. The error template now preserves
the trailing space in the line prefix and omits that prefix when no line order exists.
J08 step 7 includes the corrected examples. A Windows rendering regression checks both
line errors and an entry-wide error. The WindowsChecks project cross-builds with zero
warnings or errors; the rendering check still requires Windows execution.

## Journal amount entry and balance feedback

Entering nonempty Debit text now clears Credit on that row, and vice versa. The editor
uses right-aligned amount inputs and Account/C/Debit/Credit columns. C is read-only and
uses blank, R, or a checkmark with the full state in a tooltip and accessibility name.
Live totals identify unequal debits/credits and show the difference in red text. Save and
Continue are disabled while totals differ; Ctrl+Enter also respects that condition.
Invalid numeric text remains available for correction and field validation. It does not
produce a misleading balanced total.

The solution cross-build passed with zero warnings or errors after removing obsolete
temporary build outputs that exhausted disk space on the first attempt. All 102 portable
tests passed. Windows checks for amount alignment and disabled save actions compiled but
need Windows execution. The J08 amount-entry retest records the remaining manual checks.
No SQL procedure or schema changed.


## Phase 15 editor implementation

Date: 2026-11-24. Scope: T109–T112 and US3 verification under T116/T107.

The editor now uses numbered posting rows with even-row stripes and full-row hover/focus bands.
The totals footing shares the posting viewport width, including its scrollbar allowance.
Numeric totals align with the amount text edges. Invalid amount text hides both totals.
The footing names the short side and colors only the larger total red.

Posting errors use red borders, tooltips, accessible status/help text, and white-on-red line badges.
Corrected errors clear during editing. Date errors remain separate for the two date fields.
The view model reuses Core journal validation after a validation attempt. Malformed amount text
gets feedback while typing. Errors follow the retained row objects after a row is removed.
Reconciled financial edits mark the affected line numbers in amber. Restoring the original
values clears those markers and the existing warning.

The existing Save, Continue, delete preview, reset confirmation, and SQL write paths remain in
place. No schema, public service contract, financial rule, dependency, or US4/US5 control changed.

| Check | Result |
| --- | --- |
| .NET 10 solution restore and cross-build | Passed; zero warnings and errors |
| Portable tests | 107 passed; zero failed or skipped |
| Focused portable editor tests | 27 passed; includes five new Phase 15 behavior cases |
| Changed C# whitespace check | Passed; the formatter reported workspace-load warnings |
| Diff whitespace check | Passed |
| Windows editor regression | Compiled; execution blocked before tests by missing Microsoft.WindowsDesktop.App; exit 150 |
| SQL journal/register regression | 13 failed during fixture creation with sanitized SQL category 0; zero assertions reached |
| Windows visual, keyboard, screen-reader, and DPI checks | Unverified; use the Phase 15 steps in manual-us3.md |

Build artifacts are isolated under the ignored `artifacts/us3-phase15` directory. The first
attempt used incompatible cached assets. A later temporary build ran out of space in `/tmp`.
The final build used the workspace artifacts directory and passed. The SQL fixture retry through
`host.docker.internal,1433` also failed during setup. These results do not establish a financial
regression or a successful SQL verification run.

Run the target checks from an interactive Windows session with isolated SQL fixtures:

```powershell
dotnet build FatStacks.slnx
dotnet test --project tests/FatStacks.WindowsChecks/FatStacks.WindowsChecks.csproj --filter 'FullyQualifiedName~JournalEditorViewModelTests|FullyQualifiedName~JournalJourneyTests|FullyQualifiedName~WindowBehaviorTests'
dotnet test --project tests/FatStacks.SqlTests/FatStacks.SqlTests.csproj --filter 'FullyQualifiedName~Journal|FullyQualifiedName~Register'
```

The new Windows case checks short/scrolling alignment, invalid-input borders and accessible
feedback, restored feedback after correction, and focused-row colors. Manual review is still
required for spoken output, visual comparison, mouse hover, and each requested display scale.

Self-review: presentation stays in Desktop; Core remains the authority for financial validation.
No SQL or persistence change is required. Source review checked exact contract colors, retained
keyboard handlers, stable control IDs, noninteractive line numbers, and pending-write guards.
The review does not substitute for Windows execution. T109–T112, T107, and T116 remain open
until the required target results are recorded. Earlier manual passes remain valid historical
evidence but do not verify this revised editor.


### Journal dialog startup binding correction

The owner reported application shutdown when pressing N in the ledger after the Phase 15 change.
Source review found a binding defect in the new totals status: WPF `Run.Text` defaults to a
two-way binding, but `JournalEditorViewModel.BalanceStatus` has no setter. Binding initialization
can therefore throw while opening the dialog. The status binding now explicitly uses OneWay.
This preserves live display updates without trying to write to the calculated property.

Added `NewJournalDialogOpensWithReadOnlyBalanceStatusAndUpdatesItsFooting` to the Windows tests.
It opens a fresh draft and checks balanced, unbalanced, and invalid-text status changes without
saving. The test compiles but still requires Windows execution. All 27 portable editor tests
pass. The solution cross-build passes with zero warnings or errors. The first build attempt
stopped with an internal CLR error; a retry with one build worker and shared compilation
disabled passed. No SQL or financial rule changed.

Rebuild and restart the Windows app, then press N in the ledger. Run the new regression on
Windows before recording the reported startup failure as resolved on the target platform.
