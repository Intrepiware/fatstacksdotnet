# US4 desktop mock-up

Open `index.html` in a browser. The files also work through a static HTTP server.
From the repository root, run:

```bash
python3 -m http.server 8765 --directory specs/001-sql-desktop-foundation/mockups
```

Then visit `http://localhost:8765/index.html`.

The main window is `main.html`. The journal editor is `journal.html`.
States use query parameters. Shared fixtures and styling live in `assets/`.
The collapsible mock controls switch states and simulated save and status outcomes.
All financial text is synthetic. Operations do not call the application or SQL Server.
The fixed review date is November 23, 2026 in a UTC book.

US4 replaces wide status dropdowns with a compact, color-coded 3-state **C** control (neutral
blank for Uncleared, amber `R` for Reconciling, and green `✓` for Cleared, with full status names
in tooltips and accessibility text). On the main window, each `JournalEntryLine` for the selected
account renders as its own row in the upper account register so the owner can reconcile one
account at a time: use `↑`/`↓` to navigate between rows and press `R` (Reconciling), `C`
(Cleared), or `U` (Uncleared), or click **C** on a row to cycle
`Uncleared → Reconciling → Cleared → Uncleared`. Because same-account lines are not combined into
a single register row, no `Mixed` summary state is needed and the lower entry-details pane's
compact **C** column between Account and Debit is purely read-only. On the journal entry screen,
each posting row's **C** column is an interactive 3-state control included in the
`Account → C → Debit → Credit` Tab sequence. New lines default to Uncleared and never auto-clear
their **C** state when the user edits values on the line before initial save, whereas editing an
already-saved Cleared or Reconciling line's account or amount auto-clears its **C** state to
Uncleared with the FR-023 reset warning.

The current spec and WPF views define the layout. Earlier screenshots do not override the
approved US3 changes. The journal totals footing, row-tracking cues, and accessible inline
validation refinements have been backported into `spec.md`, `contracts/desktop-ui.md`, and
`ui-parity.md`. The footing puts a bold `Totals:` prefix and `Balanced` (or the short side in
red) on the left, aligns the numeric debit and credit totals directly under their columns
without repeated column labels or currency codes, shares scrollbar-gutter alignment across
header/rows/footing, and colors the over-side total red while the totals are unequal. The
USD badge beside the heading keeps the book currency visible (FR-020). Each posting row shows
a line number, alternate rows carry a continuous register stripe, and the hovered or focused
row is highlighted in the register's selected-row color while keeping text inputs crisp white.
A line named in the reset warning shows a bold amber line number. A line with a field validation
error marks the invalid input with a 2px red border and tooltip/accessibility state, styles the
line number as a red pill badge, and clears the error live as soon as the input is fixed. The
WPF editor implements these additions; its Windows acceptance checks remain pending.
Material Symbols load from `fonts.googleapis.com` and `fonts.gstatic.com`; the browser needs
network access to those hosts. The WPF source uses Fluent icons. Material Symbols approximate
the same account-group concepts for this HTML review, as confirmed for this mock-up.

The review index lists the included states and deliberate omissions. Native date inputs,
desktop shortcut routing, actual screen-reader output, and database behavior still require
Windows acceptance. Do not use this mock-up as proof that those checks passed.

This folder is a review artifact. Remove it or reset it through Git when no longer needed.

## Review verification

The earlier US3 check opened all 16 then-current screen/state links from the index without page
errors or failed requests. Main-window and journal-editor screenshots were inspected. The smoke check
also covered search with unchanged balances, New entry and Tab navigation, disabled save
actions while unbalanced, opposite-amount clearing, reset warning and reversion, Save and
banner dismissal, Continue date retention, retained conflict drafts, long-split row changes,
and opening the journal page directly from the filesystem.

This verifies only the HTML review artifact. It does not change the Windows acceptance results.

The 2026-11-23 footing recheck covered the balanced, unbalanced, and invalid-text journal
states in Chromium. The totals aligned under the Debit and Credit columns, the over-side
total turned red with the short side named, and the balanced and invalid-text footings stayed
neutral. Typing updated the footing live, and the status region kept the
Debit/Credit/difference wording for assistive technology, without the currency code. The
row-tracking recheck covered line numbers in the long split, the continuous register stripe on
alternate rows, the hover and keyboard-focus bands in the selected-row color, and the amber
line numbers for lines named by
the reset warning. This recheck verifies only the HTML review artifact.

The 2026-11-27 US4 check opened all 21 review links in Chromium without page errors or failed
requests. It verified the compact, color-coded 3-state **C** control in the upper account
register, Up/Down Arrow transaction navigation, row keyboard shortcuts (`R`, `C`, `U`), amber
and green status colors, full status names in tooltips and accessibility text, unchanged
amounts and balances, separate register rows for entries with multiple lines on the selected
account, read-only **C** markers in the lower entry-details pane, conflict and
unconfirmed-failure retention, suppression of the amber reset warning on status-only changes,
Reload recovery, the journal editor's `Account → C → Debit → Credit` Tab sequence, **C**
preservation when editing unsaved new lines, and auto-clearing **C** to Uncleared when editing
already-saved Cleared or Reconciling lines. Screenshots of the register, same-account split
rows, and journal editor states were inspected. This verifies only the HTML review artifact; it
does not provide SQL Server, durable-restart, native WPF, or Windows accessibility evidence.
