# US3 desktop parity evidence

Scope: US3 and its affected shared controls. This report does not complete the US2, US4, or
US5 parity work. Target: interactive Windows 11 x64 with the .NET 10 desktop runtime and isolated
SQL fixtures. Current runner: Linux x64; WindowsChecks cannot start (missing
Microsoft.WindowsDesktop.App; exit 150). Cross-build and portable/SQL results are recorded in
[US3 validation](us3.md#phase-13-convergence-implementation).

The user reported that all 36 WindowsChecks tests passed after the reset-warning change;
see [the Windows run record](us3.md#j06-reset-warning-and-save-confirmation-follow-up).
This supplies automated Windows evidence, including the new editor warning rendering test.
The rows below remain unverified as complete parity
checks because their separate manual requirements have not been recorded.

No target-platform row below is marked passed. Screenshots supplied during development show
individual observations, including earlier defects; they do not establish acceptance of the
current build.

| Row | Required Windows check | Evidence/status |
|---|---|---|
| P07 | Show payee and optional italic description, correct Payment/Deposit and balances, full tooltip/accessibility text; select rows without a rendering exception. | Unverified. Portable summaries and SQL mapping pass; real-template Windows regression compiles. |
| P10 | Edit/delete then Reload/F5; retain valid account and entry IDs and refresh balances. | Unverified. Journey and view-model regression source exist. |
| P11 | N/E/Delete and double-click operate on entries; typing in text fields remains editing; Ctrl+W closes only the active window through normal write guards. | Unverified. Close-command/window regression source exists; physical shortcuts require the interactive check. |
| P12 | Compare Date, bank posting date, Payee, Description, Account/C/Debit/Credit column order, right-aligned amounts, currency, and read-only blank/R/checkmark line states with the supplied editor baseline. | Unverified visual comparison. US4 state editing is outside this scope. |
| P13 | Tab/mouse focus selects account text; replacement typing searches; Up/Down/Enter/Tab/Escape and mouse acceptance work; date changes refresh eligible choices while preserving historical text. | Unverified. Model eligibility tests pass; Windows input/focus regressions compile. |
| P14 | Add/remove postings, retain at least two, Tab from last Credit adds a row only when unbalanced; Ctrl+Shift+Delete removes the focused row and focuses an adjacent account. Check forward/reverse row focus. | Unverified. Windows keyboard/layout regression source exists. |
| P15 | Save/Ctrl+Enter/Continue/Cancel, mutually exclusive Debit/Credit input, live totals/difference, disabled Save/Continue while unequal, retained failures/conflicts, one pending write, Continue Date focus; show a yellow warning before a cleared line changes and identify reset lines in the register confirmation after Save or in the editor after Continue. | Unverified as a complete parity check. The user reported 36 WindowsChecks passes, including editor warning rendering. The register banner and J06 line states still need a manual retest. |
| P16 | Fresh creation uses current book-local today; edits retain stored dates; Continue retains the saved date. Both date fields support minus/plus/T, including midnight and date limits. | Unverified. Book-calendar and date-shortcut model tests pass; Windows textbox input source exists. |
| P17 | Delete preview uses the source dialog's gray summary card and simple posting rows. Show the date, then payee and italic description on one wrapping line; omit the separator when either is empty. Account/C/Debit/Credit headings directly introduce the rows. C is blank for Uncleared, R for Reconciling, and a checkmark for Cleared; expose full names through tooltips and accessibility. Do not show a currency column or separate “Journal entry lines” heading. Use a red Delete button labeled Delete. Cancel starts focused; cancellation changes nothing and confirmation removes all entry effects. | Unverified for the revised layout. Real SQL physical-delete/rollback tests pass; Windows rendering regressions compile but need execution and visual review. |
| P22 | Check screen-reader names, visible focus, popup focus, long paths/splits, scrolling and two-decimal amount blur formatting at 100%, 150%, and 200% scaling. | Unverified. Requires an interactive Windows desktop and recorded review. |

## Run the automated cases

Close the running application before building its normal output. Supply the separate synthetic
maintenance/runtime SQL credentials described in [testing.md](../../../docs/testing.md).
From the repository root in PowerShell:

```powershell
dotnet build FatStacks.slnx
dotnet test --project tests/FatStacks.WindowsChecks/FatStacks.WindowsChecks.csproj --filter 'FullyQualifiedName~Journal|FullyQualifiedName~RegisterSummaryRendersReadOnly|FullyQualifiedName~CloseShortcut|FullyQualifiedName~AccountTypeCanBeSelectedByTypingItsNamePrefix|FullyQualifiedName~DeleteFailureTrace'
```

Record Windows/SDK/SQL versions, test totals and individual failures in this file or the US3
report. Correct observed failures and rerun relevant cases. Missing credentials or runner
prerequisites remain unverified and must not count as passing.

## Manual checks and visual evidence

Use the detailed [US3 manual test checklist](manual-us3.md) to execute these eight checks.
It specifies setup, actions, expected results, and result fields for J01–J08.
The list below is an overview.

1. Capture the source editor/delete baseline and the current dialogs at the same scale. Explain
   the added bank date and separate Debit/Credit fields; record approved visual departures.
2. Create a 1,000 Bank/Equity opening, Continue into a 25 expense, then create a split. Use the
   keyboard for account selection and posting changes. Verify Date receives focus after Continue.
3. Type `exp` in the Accounts Type selector while creating an account. Verify Expense is selected
   and existing account types remain fixed.
4. Try new references to closed accounts and accounts that open after the entry date. Edit a
   historical closed reference before and on its closure date. Check the displayed field/line
   reasons and that an invalid save preserves the draft and changes no stored data.
5. Enter `25`, `25.5`, trailing zeros, invalid text, and `1.000000000000000000000000000001` in
   an amount field. Tab away and save directly. Exact inputs format to two places; invalid
   precision is retained and must never dispatch a write.
6. In a synthetic fixture with reconciled lines, make a financial edit and confirm its notice
   names the reset posting(s), for example `Line 1 (Assets:Checking)`. Verify unchanged lines
   retain state. Status-editing controls belong to US4.
7. Edit expense 25 to 30; cancel and then confirm deletion. Verify exact balances, full delete
   preview, safe Cancel focus, stable remaining selection, and the same results after restart.
8. Repeat entry and popup navigation with long account paths and at least 30 postings at 100%,
   150%, and 200% display scaling. Inspect accessible names with a screen reader and retain
   screenshots and notes. Check ordinary typing is unaffected by window/journal shortcuts.

Physical keyboard routing, screen-reader output, native popup behavior, and rendered high-DPI
comparisons cannot execute on the current Linux runner. The listed Windows automated cases and
these manual steps provide repeatable checks for that remaining work.


## Phase 15 verification status

The new posting rows, totals footing, inline errors, and amber reset markers are implemented.
Portable tests and the WPF cross-build pass. Windows rendering and accessibility remain
unverified for this revision. Follow the Phase 15 retest in [manual-us3.md](manual-us3.md#phase-15-editor-retest).
The [US3 report](us3.md#phase-15-editor-implementation) records the blocked Windows and SQL runs.
Do not replace the earlier J01–J07 observations with a new pass until the revised UI is checked.
