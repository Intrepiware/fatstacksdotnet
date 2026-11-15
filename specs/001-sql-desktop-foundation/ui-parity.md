# Existing UI Parity Baseline

**Feature**: [Phase 1 specification](spec.md)
**Review date**: 2026-11-14
**Evidence**: Static inspection of the supplied source, not a running Windows application.

Source paths below are relative to `.workspace/dotnet-beancount/src/`. `Core/` abbreviates
`BeancountViewer.Core/`; unqualified UI filenames are in `BeancountViewer/`. “Retain” preserves
behavior/design, “Adapt” changes it for the requested accounting model, and “Replace” removes
a file-specific interaction. No row is an assertion that the new application has been built.

| ID | Source capability and evidence | Phase-1 disposition | Acceptance evidence needed |
|---|---|---|---|
| P01 | WPF shell, Segoe UI, light palette, 1360×860 default, 960×600 minimum; `BeancountViewer/MainWindow.xaml:1` | Retain visual hierarchy and sizing baseline; rebrand Fat Stacks.NET | Screenshots at default/minimum dimensions and usable high-DPI layout |
| P02 | Header, primary actions, bottom status, split account rail and register; `MainWindow.xaml:40` | Retain layout; show profile/book identity instead of file identity | Compare shell and verify current database context is always clear |
| P03 | Banks, Credit Cards, Investments, Expenses, Other, All tabs; `MainWindow.xaml.cs:33` | Retain order, icons, selected styling, and keyboard reachability | Navigate every tab and inspect account placement |
| P04 | Hierarchical account tree, expanded groups, descendant balances; `MainWindow.xaml.cs:123` | Retain; default group from explicit account type, with stored group override | Parent/child fixture with hidden closed contributions |
| P05 | Current selected-account balance and default 90-day register; `MainWindow.xaml.cs:172`, `Core/Ledger.cs:98` | Retain; use book-local today and deterministic order | Older opening entry plus current entries with expected balances |
| P06 | Dense alternating rows, numeric alignment, negative coloring; `MainWindow.xaml:66`, `App.xaml:7` | Retain visual density; display explicit currency | Side-by-side visual review with signed reference balances |
| P07 | Date, Description, Category, C, Payment, Deposit, Balance columns; `MainWindow.xaml:72` | Retain Payment/Deposit as presentation labels over debit/credit; use selected account's Uncleared/Reconciling/Cleared line statuses for C | Verify credit movements appear as Payment, debit movements as Deposit, and mixed statuses are identifiable |
| P08 | Case-insensitive description/counter-account search; `Core/Ledger.cs:111` | Retain exact scope; full-history balance remains independent of filter | Search a later entry and compare its balance before/after filtering |
| P09 | Entry selection shows all postings and clickable accounts; `MainWindow.xaml.cs:250` | Adapt to stable journal/line IDs, debit/credit, and line reconciliation | Follow both sides of a transfer and a closed account link |
| P10 | Reload/F5, selection restored after edits/deletion; `MainWindow.xaml.cs:85`, `201` | Retain intent; restore by stable identity rather than file offset or row index | Refresh after adding, renaming, and deleting records |
| P11 | New entry via button/menu/N; edit by double-click/E; delete by Delete; `MainWindow.xaml.cs:48`, `187` | Retain shortcuts with typing guards | Keyboard-only walkthrough including text-input focus |
| P12 | Date, status, payee, narration, dynamic postings; `NewTransactionWindow.xaml:6` | Retain layout style; move status to each line; separate debit/credit | Balanced split and independent reconciliation in editor |
| P13 | Account substring suggestions, Up/Down/Enter/Tab/Escape, mouse selection; `AccountComboBox.cs:94` | Retain tested interaction; validate refreshed account eligibility | Type without caret jumps, accept suggestion, advance to amount |
| P14 | Add/remove lines; Tab adds a line when unbalanced; minimum two lines; `NewTransactionWindow.xaml.cs:72`, `120` | Retain intent for debit/credit editor | Keyboard entry of two-line and split entries |
| P15 | Save, Continue, Ctrl+Enter, Cancel; draft retained on save failure; `NewTransactionWindow.xaml.cs:143` | Retain; show precise field errors and prevent duplicate writes | Failed save and successful save-and-continue cases |
| P16 | Remember date between entries and launches; `ViewerSettings.cs:6` | Adapt: retain during batch-entry session; new session defaults to book-local today | Entry continuation, new session, and device-zone change |
| P17 | Delete confirmation previews entry, Cancel initially focused; `DeleteTransactionWindow.xaml.cs:8` | Retain; disclose reconciled lines and delete whole entry atomically | Cancel and confirm with balances checked afterward |
| P18 | File picker/Ctrl+O, file title, last file, startup file argument, drag/drop; `MainWindow.xaml.cs:42`, `80`, `297` | Replace picker/Ctrl+O with connection dialog and saved profiles; automatically reopen last successful profile; omit file argument/drop | Restart into last successful book; verify failed/missing-profile fallback and cancellation; no accidental file-edit/import capability |
| P19 | Reader warning banner, details, dismiss; `MainWindow.xaml.cs:290` | Adapt feedback style for connection/schema/data errors; omit parser-specific notices | Actionable errors without credentials or false success |
| P20 | Multiple commodities and foreign-currency icons; `Core/Ledger.cs:15` | Adapt to one currency per book; show security quantities separately | USD and EUR in separate books; reject within-book mismatch |
| P21 | Source file/line label and unsupported-syntax edit restrictions; `MainWindow.xaml.cs:257`, `Core/TransactionEditor.cs:12` | Replace with stable entry identity; remove text-format restrictions | Reopen same entry; no file-path dependencies |
| P22 | Accessibility names, tab focus styling, virtualized grids; `MainWindow.xaml:9`, `31`, `66` | Retain and complete gaps during port | UI automation plus manual keyboard/focus review |

The supplied source has no account or security management screen. Minimal setup screens are
phase-1 additions, not claims of existing parity. A full statement reconciliation screen is
also absent and remains deferred.

Visual acceptance must use the source app as the reference on Windows with synthetic data.
Source inspection establishes intended layout and handlers but does not prove their runtime
behavior. Record any baseline defect separately so parity does not require reproducing it.
