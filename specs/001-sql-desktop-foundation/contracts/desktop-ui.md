# Desktop UI Contract

The detailed baseline is [ui-parity.md](../ui-parity.md). All Retain/Adapt rows remain acceptance
requirements. Use the source app's layout, spacing, colors, fonts, icons, and keyboard behavior.

## Connection and Setup

Show the active friendly profile name and Book identity in the shell. On launch, attempt only
the last successful profile. Keep a busy indicator and Cancel available. Missing or unusable
profiles lead to the connection dialog. No fallback database or automatic setup is allowed.

The dialog supports named profiles, server/database names, integrated or SQL authentication,
Test, Open, and explicit Initialize. Selecting Windows integrated credentials disables the SQL
username and password fields in both connection and maintenance dialogs. Profile rename preserves its ID. Remove affects local
configuration only and requires an active profile to disconnect first. Reassociation to a
new Book identity is explicit. Resolve unsaved edits with Save, Discard, or Cancel before switching.

Show a visible non-blocking warning for plaintext SQL transport or unverified certificate trust,
including self-signed/expired certificate cases. Use TLS when available. These warnings need no
confirmation. The shell warning has a Dismiss button. Connection test warnings remain available
in the connection dialog. Authentication and unusable protocol errors still fail the connection. Schema
version failure is fatal to activation and leaves the dialog available (FR-030/032).

Setup collects book name, currency (USD default, EUR or JPY alternatives), and Windows time zone.
It requests separate maintenance credentials for schema work. After successful initialization,
replace Initialize and Cancel with Close. A failed initialization retains the retry controls. Minimal account setup supports
create, rename, group, parent selection, and close. Show the type-determined accounting class.
The account Type selector supports typing a name prefix while creating an account; its type
remains fixed after creation. Security setup collects name, symbol, and declared quantity precision.

## Window keyboard behavior

Ctrl+W closes the active application window, including the journal editor and other dialogs.
It must work while a text field has focus and use the same closing path as the title-bar Close
button. Preserve pending-write guards, cancellation, and main-window disconnect/shutdown behavior.
Closing a dialog must not close its owner or save its draft.

## Main Window

Retain the account rail, group tabs, split layout, dense alternating register rows, details area,
and default/minimum sizes from P01–P10. Show explicit currency. Preserve valid selection by
account and entry ID after refresh. Cancel obsolete searches and reject previous-session results.
Keep the UI responsive during reads and writes.

The register's Payee / Description column and selected-entry summary display payee in normal
text, followed by an em dash and italic description when both are present. Payee-only and
description-only entries display their available text without an empty separator. Keep dense
single-line rows, ellipsis for overflow, and a tooltip/accessibility name with the full summary.

Retain Payment and Deposit headings. Their tooltips explain selected-account credit and debit
movements. Details and editor use explicit Debit/Credit labels. Current and running balances use
complete applicable history; search changes visible rows only. Closed accounts remain reachable
from details and contribute to totals.

## Entry Editor and Delete

Keep N, E, Delete, F5, Ctrl+O, and Ctrl+Enter behavior with text-input typing guards. Keep account
suggestions and split-line keyboard workflows. Tab from Description focuses the first posting
account input, then traverses Account, C, Debit, Credit for each posting row. Shift+Tab reverses
this order. Keep add/remove posting and save actions keyboard accessible; noninteractive
layout and error areas must not interrupt data-entry focus. Ctrl+Shift+Delete removes the
focused posting when more than two lines remain, then focuses an adjacent posting account. Bind line view models to controls with stable
automation IDs. Give errors field and line context. Do not parse English messages to control Save.

Use a six-column posting grid: 1-based line number, Account, interactive C (white blank, amber R, or green checkmark
with full status in tooltip and accessibility name), centered Debit and Credit headings with
right-aligned amount inputs, and Remove (`×`). Share column alignment and scrollbar-gutter space
across the header, scrollable posting rows, and totals footing so columns and totals stay
vertically aligned whether or not the vertical scrollbar is visible. Shade even posting rows with
a continuous `#F0F8FC` register stripe and highlight the hovered or keyboard-focused posting row
in `#DDEBFA`, while keeping the Account, Debit, and Credit input boxes crisp white (`#FFFFFF`).

Click C or press Space to cycle through the three states. R and C toggle their respective states.
U sets Uncleared. Disable the control during a pending save. Keep a new line’s chosen status
through account and amount edits. For an existing reconciled line, display Uncleared while a
financial edit requires a reset. Restore the saved status when the original values return.

Place the totals footing directly beneath the posting rows under a top border. In the Account
column of the footing, display a semi-bold `Totals:` prefix in `#23384E` followed by muted
`Balanced` when debits equal credits, or red (`#B42318`) `Credits short by <difference>` /
`Debits short by <difference>` when unequal (or muted `Enter valid amounts to check the balance.`
without totals when amount text is invalid). In the Debit and Credit footing cells, show only the
right-aligned formatted totals aligned to the right text edge of the inputs above, omitting
currency codes and repeated Debit/Credit labels; keep the book currency visible in the header.
Color only the over-side total red while the entry is unbalanced. Expose the full Debit, Credit,
and difference/balanced summary to assistive technology.

When a posting field fails validation or contains unparseable amount text, mark that input with a
2px red (`#B42318`) border, set its invalid accessibility state and field error tooltip, and
render that row's line number as a solid red pill badge with white text so error identification
does not rely on color alone. When an edit to a Cleared or Reconciling line would reset it to
Uncleared, style that row's line number in bold amber (`#A16207`) in addition to showing the
yellow reset warning banner. As the user edits and fixes invalid fields, dates, or unbalanced
totals, immediately clear the resolved error messages and inline error markers.

While either journal date input is focused, `-` subtracts one day, `+` adds one day, and
`T`/`t` sets current book-local today. Support both the main keyboard and numeric keypad signs.
Use the displayed date for stepping and book-local today when blank. Reject stepping invalid
text with field feedback; `T` can replace it. Keep dates in range and leave the other date alone.

New creation dialogs start at book-local today; editing retains the entry's saved date.
Continue saves once and starts another draft with the same date, then focuses and selects the
accounting-date text after controls become available. Failed saves retain the current draft.
Posting account inputs select all their text on keyboard or initial mouse focus; typing replaces
the selected name and searches suggestions. Amount inputs format exact valid values with two
decimal places on focus loss. Leave empty, invalid, and nonzero excess-precision text intact so
validation can report it; formatting must not round away financial errors. Cancel does not save.
Disable repeat submission while a journal write is pending.
A failed or conflicting save retains the draft. Show success only after commit is confirmed.
Show which reconciled lines reset after a financial edit.

Delete shows date, description, all lines, and reconciliation states. Cancel has initial focus.
After confirmed deletion, refresh balances and preserve any selection that is still valid.

## Reconciliation and Securities

Expose Uncleared, Reconciling, and Cleared on each line. Each selected-account posting has its own register row,
running balance, and C control. Up and Down select visible postings. Click or focused Space cycles
status. R and C toggle their states and U sets Uncleared. Lower details show a read-only C
column between Account and Debit using the same accessible markers.
A status-only change preserves all amounts and other lines.

Offer security opening and same-security transfer only. Show quantities at their declared scale
and explicit book-currency carrying amounts. Do not label carrying amounts as market value.
Do not offer prices, trades, lots, gains, short positions, or conversion as working features.

## Accessibility and Evidence

Retain keyboard access, meaningful names, visible focus, and grid virtualization. Check dynamic
line names, popup focus, high-DPI layout, and long splits on Windows. Automate connection switch,
create, edit, reconcile, and delete smoke journeys where practical. Record repeatable manual
steps and a reason for any automation gap. Compare screenshots for each visual Retain/Adapt row.
These are planned checks, not evidence that the UI already passes.
