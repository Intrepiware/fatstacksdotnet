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
Security setup collects name, symbol, and declared quantity precision.

## Main Window

Retain the account rail, group tabs, split layout, dense alternating register rows, details area,
and default/minimum sizes from P01–P10. Show explicit currency. Preserve valid selection by
account and entry ID after refresh. Cancel obsolete searches and reject previous-session results.
Keep the UI responsive during reads and writes.

Retain Payment and Deposit headings. Their tooltips explain selected-account credit and debit
movements. Details and editor use explicit Debit/Credit labels. Current and running balances use
complete applicable history; search changes visible rows only. Closed accounts remain reachable
from details and contribute to totals.

## Entry Editor and Delete

Keep N, E, Delete, F5, Ctrl+O, and Ctrl+Enter behavior with text-input typing guards. Keep account
suggestions and split-line keyboard workflows. Bind line view models to controls with stable
automation IDs. Give errors field and line context. Do not parse English messages to control Save.

New sessions start at book-local today. Continue saves once and starts another draft with the
same date. Cancel does not save. Disable repeat submission while a journal write is pending.
A failed or conflicting save retains the draft. Show success only after commit is confirmed.
Show which reconciled lines reset after a financial edit.

Delete shows date, description, all lines, and reconciliation states. Cancel has initial focus.
After confirmed deletion, refresh balances and preserve any selection that is still valid.

## Reconciliation and Securities

Expose Uncleared, Reconciling, and Cleared on each line. A register summary covers only the
selected account's lines. Mixed exposes those individual values; it is not a stored status.
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
