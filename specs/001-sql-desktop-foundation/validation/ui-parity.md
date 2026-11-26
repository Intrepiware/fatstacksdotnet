# US3 desktop parity evidence

This report covers US3 and the shared controls used by its journal workflows.
The owner closed the manual test pass with the existing coverage.
Each row lists its evidence and any limits. See the [manual closeout](manual-us3.md#us3-manual-test-closeout)
and [focused Windows rerun](us3.md#focused-windows-rerun).

| Row | Required Windows check | Evidence/status |
|---|---|---|
| P07 | Show payee and optional italic description, correct Payment/Deposit and balances, full tooltip/accessibility text; select rows without a rendering exception. | J02 passed. The register and selected-entry summary showed payee and italic description. Portable summary and SQL mapping tests also passed. |
| P10 | Edit/delete then Reload/F5; retain valid account and entry IDs and refresh balances. | J07 passed. The focused Windows journey passed edit, delete, selection, and restart checks. |
| P11 | N/E/Delete and double-click operate on entries; typing in text fields remains editing; Ctrl+W closes only the active window through normal write guards. | J02 and J07 passed. J08 has an overall pass. The focused journey opened the editor and verified the delete flow. |
| P12 | Compare Date, bank posting date, Payee, Description, Account/C/Debit/Credit column order, right-aligned amounts, currency, and read-only blank/R/checkmark line states with the supplied editor baseline. | Revised J01 passed. The manual record includes a screenshot of the editor at its smallest size. |
| P13 | Tab/mouse focus selects account text; replacement typing searches; Up/Down/Enter/Tab/Escape and mouse acceptance work; date changes refresh eligible choices while preserving historical text. | J02 and J04 passed. J08 has an overall pass for long paths and popup navigation. |
| P14 | Add/remove postings, retain at least two, Tab from last Credit adds a row only when unbalanced; Ctrl+Shift+Delete removes the focused row and focuses an adjacent account. Check forward/reverse row focus. | J02 passed. J08 has an overall pass. The Windows journey also passed the split workflow. |
| P15 | Save/Ctrl+Enter/Continue/Cancel, mutually exclusive Debit/Credit input, live totals/difference, disabled Save/Continue while unequal, retained failures/conflicts, one pending write, Continue Date focus; show a yellow warning before a cleared line changes and identify reset lines in the register confirmation after Save or in the editor after Continue. | J02, J05, and revised J06 passed. The Windows journey passed the stale-save and Continue checks. Individual J08 totals and validation results remain unrecorded. The owner closed the remaining manual checks. |
| P16 | Fresh creation uses current book-local today; edits retain stored dates; Continue retains the saved date. Both date fields support minus/plus/T, including midnight and date limits. | J02 passed. The focused Windows date test passed typing, rejected input, and correction for both date fields. |
| P17 | Delete preview uses the source dialog's gray summary card and simple posting rows. Show the date, then payee and italic description on one wrapping line; omit the separator when either is empty. Account/C/Debit/Credit headings directly introduce the rows. C is blank for Uncleared, R for Reconciling, and a checkmark for Cleared; expose full names through tooltips and accessibility. Do not show a currency column or separate “Journal entry lines” heading. Use a red Delete button labeled Delete. Cancel starts focused; cancellation changes nothing and confirmation removes all entry effects. | J07 passed with delete preview evidence. The focused Windows journey passed cancellation and confirmed deletion. |
| P22 | Check screen-reader names, visible focus, popup focus, long paths/splits, scrolling and two-decimal amount blur formatting at 100%, 150%, and 200% scaling. | J08 records an overall pass and results at 100% and 200%. The owner closed the separate 150% and Payee/Description screen-reader checks without individual results. |

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

The [US3 manual test checklist](manual-us3.md) records setup, actions, and expected results for J01–J08. Use the overview below when these cases are run again.

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

## Phase 15 verification status

The posting rows, totals footing, inline errors, and amber reset markers are implemented.
The owner completed the manual review with the evidence summarized above.
The [US3 report](us3.md#manual-acceptance-closeout) records the closeout and the limits of the results.
