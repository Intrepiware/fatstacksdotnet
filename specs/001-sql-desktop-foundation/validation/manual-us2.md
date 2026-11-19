# Manual acceptance checklist — connections, accounts, and register

Created: 2026-11-19. Scope: the current US1/US2 application and its recent UI changes.
The user-reported [WindowsChecks run](windowschecks-2026-11-18.md) already passed all 33
cases with zero failures/skips. This checklist covers manual acceptance beyond that run.

This is a test-execution checklist. It is separate from the reviewer-owned requirements
checklists under `../checklists/`. No result below has been assumed from the automated run.

## Record this run

- Date and tester:
- Windows version and display scaling:
- Build/commit and whether there are uncommitted changes:
- Test book/profile and currency (omit credentials):
- Reference screenshot: `.workspace/ph01-us02/screenshots/Screenshot 2026-11-18 205324.png`
- Screenshots/results folder: `.workspace/ph01-us02/`

Check an item only after its expected result is observed. For a failure or missing prerequisite,
leave it unchecked and record **Fail** or **Blocked**, the case ID, and what happened.
An automated pass alone does not check off a manual observation. If you already observed a case
on this build, record that observation rather than repeating it unnecessarily.

## Preparation

Use an initialized synthetic test book for account changes, plus a second test book if running
M20. Rebuild/relaunch the current application first. An empty book is sufficient for M01–M09
and the account checks. A populated book is required for M21–M26 and the history-specific
parts of M13/M16.

The SQL test fixtures create temporary databases and remove them afterward; running those tests
does not leave a populated book for this checklist. If you have no prepared history, mark the
history-dependent cases **Blocked: needs sample transactions**. The current UI cannot create
journal entries, so an empty register is expected for a newly initialized book.

## Appearance and layout

- [ ] **M01 — Reference layout.** Open a book at the default 1360×860 window size and compare
  with the reference screenshot. Expect the compact header/File menu, left icon tabs above the
  account rail, name/path heading, right-aligned current balance, shaded search/date strip,
  and full-width register. Capture the current application's screenshot.
- [ ] **M02 — Defaults.** Restart the application and open Accounts. Expect **Banks** selected
  in the main window and **Tree** selected in a freshly opened Accounts dialog. Switch the
  dialog to Alphabetical, close it, and reopen it; expect Tree again.
- [ ] **M03 — Minimum size.** Resize the main window to its 960×600 minimum and Accounts to
  its minimum. Expect reachable buttons, readable headings, scrolling where needed, and no
  overlapping fields. Drag the account-rail splitter; both panes should remain usable.
- [ ] **M04 — Display scaling.** Repeat M01/M03 at your usual Windows scaling and another
  available setting such as 150% or 200%. Expect readable text/icons, usable dropdowns, and
  no clipped controls or unreachable buttons. Record the actual settings and screenshots.
- [ ] **M05 — Field sizing.** Inspect Accounts, Connections, and Initialize dialogs. Expect
  single-line textboxes, password fields, dropdowns, and date pickers to have matching heights
  and vertically aligned content. Account-type icons must fit inside the type picker.
- [ ] **M06 — Tabs and tree.** Visit Banks, Credit Cards, Investments, Expenses, Other, and All.
  Expect that order, correct tooltips/icons, a filled selected icon with amber underline,
  and visible keyboard focus. All shows stored display-group sections. Empty groups have a
  clear empty message; accounts appear according to their stored group overrides.
- [ ] **M07 — Long content.** Use a long account name and nested hierarchy in the test book.
  Expect the full path to be reachable through the tooltip or scrolling, without covering
  nearby controls. Alphabetical results with similar names should remain distinguishable.
- [ ] **M08 — Basic accessibility.** Use Windows Narrator or your normal screen reader on
  group tabs, search fields, account choices, and buttons. Expect meaningful names and a
  logical focus order; important information must remain available without relying on color.

## Keyboard and account operations against the real test book

- [ ] **M09 — Main-window keyboard.** Use Ctrl+O to open Connections and F5 to reload. Navigate
  tabs and accounts by keyboard. Type in both search boxes. Expect ordinary text entry with
  no accidental dialog/action activation, visible focus, and responsive controls.
- [ ] **M10 — Dialog shortcuts.** In Accounts, use Alt+N, Alt+S, Alt+C, Alt+F, Alt+T, and Alt+A.
  Expect New, Save, Close, Find, Tree, and Alphabetical respectively. New should focus the name
  field. Perform the save in the synthetic book and confirm it persists after reopening.
- [ ] **M11 — Types and placement.** Create representative accounts, checking every type
  against the matrix below. Expect the correct displayed class, default group, and recognizable
  icon. Reopen the account; its type must be fixed. Income and Equity must be reachable in Other/All.

  | Type | Class | Default group |
  | --- | --- | --- |
  | Bank / Cash | Assets | Banks |
  | Credit Card | Liabilities | Credit Cards |
  | Loan Receivable | Assets | Other |
  | Loan Payable | Liabilities | Other |
  | Investment/Brokerage | Assets | Investments |
  | Other Asset | Assets | Other |
  | Other Liability | Liabilities | Other |
  | Equity | Equity | Other |
  | Income | Income | Other |
  | Expense | Expenses | Expenses |

- [ ] **M12 — Hierarchy.** Create a parent and child; view both browser modes. Expect correct
  nesting in Tree and case-insensitive alphabetical ordering in Alphabetical. Parent selection
  must show full paths, including when sibling names exist under different parents.
- [ ] **M13 — Rename and override.** Rename an account and change its display group. Save,
  reopen Accounts, and reload the main window. Expect the new name/group to persist without
  changing the type or class. Existing history, if available, must remain attached to that account.
- [ ] **M14 — Validation.** Try a blank name, a colon in the name, a duplicate sibling name
  with different casing, making an account its own parent, and making a parent a child of its
  descendant. Expect an actionable error and no saved invalid change. Correct each attempt
  before trying the next so the observed error is unambiguous.
- [ ] **M15 — Search and draft preservation.** Start editing an account name without saving.
  Search for a different account, for its type, and for text matching nothing; toggle Tree and
  Alphabetical. Expect filtering to work, ancestors to remain as context in Tree, and the
  original unsaved editor values to survive. Clear the search; the selected account should
  be highlighted when visible. Close without saving, reopen, and confirm no unintended save.
- [ ] **M16 — Closure.** Try a close date equal to the open date; expect rejection. Save a
  valid later close date on a synthetic account. Expect the closed indication and retained
  account/history after reopening. With prepared history, a close date on/before an existing
  entry or an open date after it must be rejected. There is no account-delete action to test.

## Real application sessions and shutdown

- [ ] **M17 — Connected exit.** Open the actual application and a test book, select an account,
  search, and open/close Accounts. Close the main window with X, then repeat using File → Exit.
  Expect a normal exit without the unhandled-error dialog. Restart and confirm the remembered
  profile reopens, with Banks selected.
- [ ] **M18 — Disconnected/startup exit.** Disconnect and close the app. Then start it and
  close during a pending startup connection if you can observe one. Expect a clean exit,
  no late connection dialog, and no unhandled error. Record an unobservable timing case as Blocked.
- [ ] **M19 — Failures and warnings.** Use an unreachable test profile; expect a usable
  Connections dialog and clear failure feedback. If a transport warning occurs, confirm it
  is readable in the bottom register banner and Dismiss works. Do not count an unobserved
  TLS/certificate scenario as passed; the dedicated transport matrix is a separate test run.
- [ ] **M20 — Book switch.** With two distinguishable synthetic books, switch between them.
  Expect the correct profile/book identity, currency, accounts, selections, rows, and details.
  Old results must not reappear in the new book. Repeat while a reload/search is pending if
  observable, and confirm a failed switch clears the old book from the screen.

## Register walkthrough — requires prepared synthetic history

Use an existing prepared test book with known expected balances. For an exact small reference,
the following balanced dataset works; each accounting date is relative to today in the book's
time zone. This is a fixture specification, not instructions to enter transactions through the
unfinished journal editor. All accounts must have suitable open dates.

| Order | Date | Debit | Credit |
| --- | --- | --- | --- |
| 1 | Today − 120 days | Bank 1,000 | Equity 1,000 |
| 2 | Yesterday | Food 20; Closed child of Bank 5 | Bank 25 |
| 3 | Today | Bank 100 | Income 100 |
| 4 | Today, after row 3 | Bank 10 | Bank 10 |
| 5 | Tomorrow | Bank 50 | Income 50 |

Close the child account as of today. Give row 2 a description and/or counter-account name with
literal characters `[%_]`. For a negative-color case, also include a separate Card credit of
30 balanced by an Expense debit of 30. Include different line statuses in row 4 for Mixed.

- [ ] **M21 — Full-history balances and window.** Select Bank. With this dataset expect three
  visible rows: yesterday's Payment 25 / balance 975; today's Deposit 100 / balance 1,075;
  today's zero-net row / balance 1,075 with blank Payment/Deposit. The old opening contributes
  to balances but is hidden. Tomorrow's row is hidden and excluded from the current balance.
- [ ] **M22 — Descendant totals.** Expect Bank's tree total to be 1,080, including the closed
  child's 5. The selected Bank's direct current balance remains 1,075. Select the child through
  its historical detail link; its history and balance must still be reachable.
- [ ] **M23 — Register search.** Search a description, a counter-account name with different
  casing, the literal text `[%_]`, and an absent term. Expect only matching visible rows;
  retained rows keep their original running balances. Clear search to restore the full window.
- [ ] **M24 — Details and navigation.** Select the split row. Expect all three lines with
  explicit Debit/Credit and individual statuses; follow a counter-account link and confirm
  the same entry stays selected when it is in the current window. Details should be hidden
  with no selected row, and long splits should scroll rather than cover the register.
- [ ] **M25 — Presentation.** Confirm right-aligned monetary headings/values, alternating
  row colors, visible selection, readable currency, negative balance coloring, and a Mixed
  summary with full status information in details/tooltips. Hover Payment/Deposit to check
  their credit/debit explanations.
- [ ] **M26 — Reload stability.** Select an account and entry, press F5, and use Reload.
  Expect the same valid account/entry selection and exact balances. Restart and compare the
  order of today's entries; equal-date ordering must remain stable.

## Outside this manual pass

Journal create/edit/delete, Save/Continue, reconciliation changes, and security opening/transfer
workflows belong to US3–US5. Do not report their absence as a US2 regression. The full SQL suite,
dedicated TLS/certificate matrix, security/redaction checks, representative performance benchmark,
and release acceptance remain separate from these manual checks and the 33-case Windows run.

## Return results

Paste a short report into the conversation, or save it as
`.workspace/ph01-us02/manual-test-results-YYYY.MM.dd.md` and send its path. Screenshots can go in
`.workspace/ph01-us02/screenshots/`. Example:

```text
Build: <commit / working-tree version>
Windows/scaling: <version>, <scale settings tested>
Passed: M01, M02, ...
Failed: M03 — <expected versus actual>; screenshot: <path>
Blocked: M21–M26 — no populated synthetic book
Notes: <other observations>
```

Reviewed outcomes should then be summarized in [US2 validation](us2.md). The full parity
baseline is [ui-parity.md](../ui-parity.md); this checklist covers its currently applicable
US1/US2 behavior, not the unfinished editor workflows.
