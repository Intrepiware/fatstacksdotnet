# Manual acceptance checklist — connections, accounts, and register

Created: 2026-11-19. Scope: the current US1/US2 application and its recent UI changes.
The user-reported [WindowsChecks run](windowschecks-2026-11-18.md) already passed all 33
cases with zero failures/skips. This checklist covers manual acceptance beyond that run.

This is a test-execution checklist. It is separate from the reviewer-owned requirements
checklists under `../checklists/`. No result below has been assumed from the automated run.

## Record this run

- Date and tester: 2026-11-19, Mike
- Windows version and display scaling: Windows 11, 3840x2160
- Build/commit and whether there are uncommitted changes: 555bc94edb8b9ee3453aa583a48965f765c8ed2f
- Test book/profile and currency (omit credentials): UAT
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

- [x] **M01 — Reference layout.** Open a book at the default 1360×860 window size and compare
      with the reference screenshot. Expect the compact header/File menu, left icon tabs above the
      account rail, name/path heading, right-aligned current balance, shaded search/date strip,
      and full-width register. Capture the current application's screenshot.
      **Result:** Screenshot: .workspace\ph01-us02\screenshots\fatstacks-screenshot-01.png
- [ ] **M02 — Defaults.** Restart the application and open Accounts. Expect **Banks** selected
      in the main window and **Tree** selected in a freshly opened Accounts dialog. Switch the
      dialog to Alphabetical, close it, and reopen it; expect Tree again.
- [x] **M03 — Minimum size.** Resize the main window to its 960×600 minimum and Accounts to
      its minimum. Expect reachable buttons, readable headings, scrolling where needed, and no
      overlapping fields. Drag the account-rail splitter; both panes should remain usable.
- [x] **M04 — Display scaling.** Repeat M01/M03 at your usual Windows scaling and another
      available setting such as 150% or 200%. Expect readable text/icons, usable dropdowns, and
      no clipped controls or unreachable buttons. Record the actual settings and screenshots.
- [x] **M05 — Field sizing.** Inspect Accounts, Connections, and Initialize dialogs. Expect
      single-line textboxes, password fields, dropdowns, and date pickers to have matching heights
      and vertically aligned content. Account-type icons must fit inside the type picker.
- [x] **M06 — Tabs and tree.** Visit Banks, Credit Cards, Investments, Loans Payable, and Other.
      Expect that order, matching tooltips and icons, a filled selected icon with amber underline,
      and visible keyboard focus. Verify that a Loan Payable account with the default Other
      group appears in Loans Payable. Verify that the Other tab groups the remaining Other and
      Expenses accounts by type. Change a test account's group to Banks and verify it moves to
      Banks without changing type or class. Check the empty message on a tab with no accounts.
- [x] **M07 — Long content.** Use a long account name and nested hierarchy in the test book.
      Expect the full path to be reachable through the tooltip or scrolling, without covering
      nearby controls. Alphabetical results with similar names should remain distinguishable.
- [x] **M08 — Basic accessibility.** Use Windows Narrator or your normal screen reader on
      group tabs, search fields, account choices, and buttons. Expect meaningful names and a
      logical focus order; important information must remain available without relying on color.

**Results/Observations:**

- M02: **Failure**. The Tree/Alphabetical radio does not have a selected option (`.workspace\ph01-us02\screenshots\fatstacks-screenshot-02.png`), but the accounts are displayed with tree format.
- M02: Parent dropdown displays accounts in the order they were entered, which is unexpected.
- M02: An autocomplete on any part of the account name would be nice
- M02: We definitely need a way to delete empty accounts
- M04: Screenshots skipped for time; everything looks OK though.
- M05: The connection screen seems to reserve space for the error, which is odd (`.workspace\ph01-us02\screenshots\fatstacks-screenshot-03.png`)
- M05: Please display the connections on the left side in alphabetical order (`.workspace\ph01-us02\screenshots\fatstacks-screenshot-04.png`)
- M05: Also, open the connection when I double-click it in the left sidebar

## Keyboard and account operations against the real test book

- [x] **M09 — Main-window keyboard.** Use Ctrl+O to open Connections and F5 to reload. Navigate
      tabs and accounts by keyboard. Type in both search boxes. Expect ordinary text entry with
      no accidental dialog/action activation, visible focus, and responsive controls.
- [x] **M10 — Dialog shortcuts.** In Accounts, use Alt+N, Alt+S, Alt+C, Alt+F, Alt+T, and Alt+A.
      Expect New, Save, Close, Find, Tree, and Alphabetical respectively. New should focus the name
      field. Perform the save in the synthetic book and confirm it persists after reopening.
- [ ] **M11 — Types and placement.** Create representative accounts, checking every type
      against the matrix below. Expect the correct displayed class, default group, and recognizable
      icon. Reopen the account; its type must be fixed. Income and Equity must be reachable in Other.

  | Type                 | Class       | Default group |
  | -------------------- | ----------- | ------------- |
  | Bank / Cash          | Assets      | Banks         |
  | Credit Card          | Liabilities | Credit Cards  |
  | Loan Receivable      | Assets      | Other         |
  | Loan Payable         | Liabilities | Other         |
  | Investment/Brokerage | Assets      | Investments   |
  | Other Asset          | Assets      | Other         |
  | Other Liability      | Liabilities | Other         |
  | Equity               | Equity      | Other         |
  | Income               | Income      | Other         |
  | Expense              | Expenses    | Expenses      |

- [x] **M12 — Hierarchy.** Create a parent and child; view both browser modes. Expect correct
      nesting in Tree and case-insensitive alphabetical ordering in Alphabetical. Parent selection
      must show full paths, including when sibling names exist under different parents.
- [x] **M13 — Rename and override.** Rename an account and change its display group. Save,
      reopen Accounts, and reload the main window. Expect the new name/group to persist without
      changing the type or class. Existing history, if available, must remain attached to that account.
- [x] **M14 — Validation.** Try a blank name, a colon in the name, a duplicate sibling name
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

**Results/Observations:**

- M09: Can we add a Ctrl+F shortcut to focus on the main search textbox?
- M15: **Failure**. When I toggled between tree and alphabetical, then typed a search that did not match the current account, the current account disappeared (including my changes)

## Real application sessions and shutdown

- [x] **M17 — Connected exit.** Use one instance at a time.
  1. Launch the app and open a test book. Select an account, search, and open and close Accounts.
  2. Click the **X** on the main window. Wait for the app to exit. Expect no unhandled-error dialog.
  3. Relaunch the app. Confirm the remembered profile reopens with **Banks** selected.
  4. With the book open, choose **File → Exit**. Expect the same clean exit.

- [x] **M18 — Disconnected/startup exit.** Disconnect and close the app. Then start it and
      close during a pending startup connection if you can observe one. Expect a clean exit,
      no late connection dialog, and no unhandled error. Record an unobservable timing case as Blocked.
- [x] **M19 — Failures and warnings.** In Connections, create a separate profile named
      **Unreachable test** with server `tcp:127.0.0.1,1`, database `Unavailable`, and Windows
      integrated credentials. Click **Test**. Expect a connection failure and a Connections dialog
      that still responds. If a working connection shows a transport warning, confirm it is
      readable in the bottom register banner and **Dismiss** hides it. The dedicated transport
      matrix covers specific TLS and certificate scenarios.
- [x] **M20 — Book switch.** With two distinguishable synthetic books, switch between them.
      Expect the correct profile/book identity, currency, accounts, selections, rows, and details.
      Old results must not reappear in the new book. Repeat while a reload/search is pending if
      observable, and confirm a failed switch clears the old book from the screen.

## Register walkthrough — requires prepared synthetic history

Use an existing prepared test book with known expected balances. For an exact small reference,
the following balanced dataset works; each accounting date is relative to today in the book's
time zone. This is a fixture specification, not instructions to enter transactions through the
unfinished journal editor. All accounts must have suitable open dates.

| Order | Date               | Debit                           | Credit       |
| ----- | ------------------ | ------------------------------- | ------------ |
| 1     | Today − 120 days   | Bank 1,000                      | Equity 1,000 |
| 2     | Yesterday          | Food 20; Closed child of Bank 5 | Bank 25      |
| 3     | Today              | Bank 100                        | Income 100   |
| 4     | Today, after row 3 | Bank 10                         | Bank 10      |
| 5     | Tomorrow           | Bank 50                         | Income 50    |

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

**Result:** Blocked - requires synthetic transaction history

## Outside this manual pass

Journal create/edit/delete, Save/Continue, reconciliation changes, and security opening/transfer
workflows belong to US3–US5. Do not report their absence as a US2 regression. The full SQL suite,
dedicated TLS/certificate matrix, security/redaction checks, representative performance benchmark,
and release acceptance remain separate from these manual checks and the 33-case Windows run.

## US2 fix retests

Rebuild and relaunch on Windows. Use a disposable book. These checks work without journal
entries. Record each result here or in the results report below.

- [x] **M27 — Accounts view selection (M02).** Open Accounts. Expect Tree checked and the
      hierarchy visible. Click Alphabetical, then Tree. Expect exactly one radio button checked
      at each step and the corresponding view visible. Close while Alphabetical is selected,
      reopen, and verify Tree is checked again.
- [x] **M28 — Parent dropdown order (M02).** Create root accounts named Zebra and alpha in
      that order. Add a child under alpha. Reopen Accounts and open the Parent dropdown.
      Expect (No parent) first and the remaining choices sorted case-insensitively by full
      displayed path. Verify alpha and its child appear before Zebra.
- [x] **M29 — Draft survives a nonmatching search (M15).** In Tree, select Zebra and change
      its editor name without saving. Switch to Alphabetical, then back to Tree. Search for
      alpha so Zebra is hidden while another account still matches. Expect the unsaved name
      to remain in the editor. Switch to Alphabetical with the filter active and check again.
      Clear the filter and verify Zebra is highlighted and the editor still shows the draft.
      Switch back to Tree and verify the same. Close without saving, reopen, and confirm
      Zebra still has its original name.
- [x] **M30 — Connection feedback space (M05).** Open Connections and confirm there is no
      blank warning panel or idle progress strip. Test the unreachable profile from M19
      and verify the failure is still readable. If a profile produces a transport warning,
      verify the warning panel appears with readable text.
- [x] **M31 — Saved connection order and double-click (M05).** Use two working profiles
      pointing to distinguishable test books. Create or save Zebra first and alpha second.
      Reopen Connections and verify alpha appears before Zebra. Single-click the profile
      for the other book and verify the dialog stays open. Double-click empty space below the
      profiles and verify it stays open. Double-click the other profile and expect the dialog
      to close and the corresponding book to open.
- [ ] **M32 — Register search shortcut (M09).** Open a book and focus the account tree or date
      picker. Press Ctrl+F, then type. Expect focus to move to the main register search and the
      typed characters to appear there.

**Result** FAILED (M32) - clicking the account tree or date-picker then pressing ctrl+f has weird results. The first few times I did it, it focused on the radio button, switched to alphabetical during my few few keystrokes, then focused on the search box for the last few keystrokes in my query. The last few times I tried it, ctrl+f had no effect at all.

- [x] **M33 — Search focus from both windows (M32 retest).** With Accounts closed, click the
      main account tree, press Ctrl+F, and type a few characters. Expect the entire query in
      the register search. Clear it, use Tab to focus the main window's Reload button, and
      repeat. Then open Accounts and repeat with the account tree, either view radio button,
      and the Open date field focused.
      Expect the Accounts search to receive the entire query each time and the radio selection
      to stay unchanged. Verify Alt+F still focuses the Accounts search.
- [x] **M34 — Connections access keys.** Use a disposable profile. With a text field or the
      saved-profile list focused, verify each Alt shortcut invokes the corresponding button
      and honors the same enabled state and confirmation dialogs. Reopen Connections as needed.

      | Shortcut | Action |
      | -------- | ------ |
      | Alt+N | New profile |
      | Alt+S | Save profile |
      | Alt+T | Test |
      | Alt+O | Open |
      | Alt+R | Remove |
      | Alt+I | Initialize database… |
      | Alt+B | Reassociate book… |
      | Alt+A | Cancel operation |
      | Alt+C | Close |

      Cancel the Initialize and Reassociate dialogs without applying changes. If a pending
      connection is observable, use Alt+A and verify the operation cancels.

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
