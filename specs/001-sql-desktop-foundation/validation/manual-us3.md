# Manual test checklist: US3 journal entry

This checklist expands the eight manual checks in [ui-parity.md](ui-parity.md).
The constitution requires repeatable manual checks but does not prescribe a test-runner layout.
This document uses numbered actions, expected results, and a result field for each case.

The filtered WindowsChecks suite has a user-reported pass.
These manual checks verify physical keyboard use, rendered content, and desktop accessibility.
Automated checks cannot establish the visual comparison or the screen-reader experience.
Some financial checks overlap automation so that the same outcomes can be checked through the UI.

## Record this run

- Tester and date:
- Application build or commit:
- Windows version (`winver`):
- .NET SDK version (`dotnet --version`):
- SQL Server version:
- Screen resolution and scaling:
- Test connection/profile and database name, without credentials:
- Book currency and time zone:
- Decimal separator and date format used by Windows:
- Screenshot folder: `.workspace/ph01-us03/manual/` or another recorded folder.

Leave a case unchecked until all its expected results pass.
Record **Pass**, **Fail**, or **Blocked** in its result field.
For a failure, give the step number, actual result, and screenshot name.
For a blocked step, name the missing prerequisite.
Keep earlier failure evidence when a later attempt passes.
If you already observed a result on this build, record that observation.

## Preparation

1. Close the application before building it.
2. Run `dotnet build FatStacks.slnx` from the repository root in PowerShell.
3. Start the application with `dotnet run --project src/FatStacks.Desktop`.
4. Open a dedicated synthetic test book through **Connections…**.
5. Use an empty book for the exact balances below.
   Do not use a personal book or the temporary database from an automated test.
   The automated fixtures remove their databases after the run.
6. Click **Accounts…**.
7. Use **New account** to create the following accounts.
   Leave Parent and Display group override at their defaults.
   Set every Open date to January 1, 2000.
   Leave Close date empty.

   | Account name | Type |
   |---|---|
   | U3 Bank | Bank |
   | U3 Equity | Equity |
   | U3 Expense A | Expense |
   | U3 Expense B | Expense |

8. Save each account before creating the next one.
9. Close Accounts and select **U3 Bank** in the main window.
10. Clear any register search text.
11. Keep the register date filter wide enough to include November 2026.

The instructions use November 10, 2026 as the accounting date.
Enter dates in the format accepted by your Windows settings.
Enter monetary values with your Windows decimal separator.
For example, use `25,5` instead of `25.5` if the decimal separator is a comma.
The expected numeric values are the same.

Run J01–J08 in order.
J02 creates the opening, expense, and split entries used by later cases.
J03–J06 must not change those three entries.
J07 deletes the expense entry.
J08 uses an unsaved draft.

## J01 — Compare the editor and delete dialog

- [ ] **J01 passed.** References: P12 and P17.

**Prerequisites:** Complete Preparation.
Have the accepted source editor and delete-dialog reference images available.
Use the source application under `.workspace/dotnet-beancount/src` as the comparison baseline.
See the [source parity map](../ui-parity.md) for the referenced screens and approved adaptations.
Development screenshots in `.workspace/ph01-us03/screenshots/` can identify earlier defects.
They do not prove that the current build passes.
If the source baseline is unavailable, mark only the comparison part blocked.

| Step | Action | Expected result |
|---|---|---|
| 1 | Set Windows display scaling to 100% in Settings → System → Display. Relaunch the application if Windows requests it. | The recorded scale is 100%. |
| 2 | Click **New entry**. | The Journal entry dialog opens. The accounting date uses today in the book time zone. Focus is in Date. |
| 3 | Inspect Date, Bank posting date, Payee, Description, Account, Debit, Credit, Status, Add posting, Cancel, Continue, and Save. | Labels and controls are readable. Fields and headings align. Status text is read-only. |
| 4 | Resize the dialog to its smallest permitted size. | Fields remain usable. Scrolling makes content reachable. Buttons and errors do not overlap. |
| 5 | Capture the current editor. Compare it with the source editor at the same scale. | Record differences in spacing, alignment, density, labels, and actions. The bank posting date and separate Debit/Credit columns are deliberate additions. Do not treat other differences as approved without review. |
| 6 | Cancel the empty draft. | No transaction is added. |

Complete the delete-dialog comparison during J07, after a saved entry exists.
Capture that dialog before you cancel or confirm deletion.
Keep J01 open until that comparison is recorded.

**Result:** Unverified.
**Evidence and differences:**

## J02 — Enter an opening, expense, and split with the keyboard

- [ ] **J02 passed.** References: P07, P11, P13–P16.

**Prerequisites:** J01 editor inspection is complete.
U3 Bank has no transactions.

| Step | Action | Expected result |
|---|---|---|
| 1 | Focus the register, outside an editable text field. Press `N`. | A new Journal entry dialog opens. |
| 2 | Set Date to November 10, 2026. Set Bank posting date to November 11, 2026. Enter Payee `U3 Opening` and Description `J02 opening`. | Both dates and both text fields retain their separate values. |
| 3 | With focus in Description, press Tab. | Focus moves directly to the first Account input. It skips noninteractive areas. |
| 4 | Type `U3 B`. Use Down/Up to highlight U3 Bank. Press Enter. | U3 Bank is accepted. Focus moves to that row's Debit field. |
| 5 | Enter Debit `1000`. Press Tab through Credit to the second Account input. | Debit displays `1000.00` after it loses focus. The order is Account, Debit, Credit, then the next row's Account. |
| 6 | Select U3 Equity in the second row. Leave Debit empty. Enter Credit `1000`. | The entry has a 1000 debit to Bank and a 1000 credit to Equity. |
| 7 | Press Shift+Tab through the row fields. Then return to an Account input that already contains text. | The order reverses correctly. All account text is selected when the input gains focus. |
| 8 | Type `U3 E`, select U3 Equity, and accept it with Tab. | Typing replaces the selected text. It does not append to the old account name. Tab accepts the intended suggestion. |
| 9 | Click **Continue**. | The opening saves once. A fresh draft appears. Date remains November 10, 2026. Focus returns to Date. |
| 10 | Enter Payee `U3 Shop` and Description `J02 expense`. Enter U3 Expense A Debit `25` and U3 Bank Credit `25`. Click **Save**. | The dialog closes after success. U3 Bank has two entries and a balance of 975.00. |
| 11 | Select the expense row and inspect its register text and selected-entry summary. Hover over truncated text. | Payee appears in normal text. Description appears in italic text. Full text is available through the tooltip and accessibility information. No rendering error occurs. |
| 12 | Open a new entry. Use Date November 10, 2026, Payee `U3 Split Shop`, and Description `J02 split`. Enter U3 Expense A Debit `20`. Enter U3 Expense B Debit `5` on row 2. Leave its Credit empty. Tab from that final Credit field. | Because debits exceed credits, a third posting is added. Focus moves to its Account input. |
| 13 | Enter U3 Bank Credit `25` on row 3. Tab from that Credit field. | The entry balances. Tab does not add an unnecessary row. |
| 14 | Click **Add posting**. Focus the new row's Account input. Press Ctrl+Shift+Delete. | The new row is removed. Focus moves to an adjacent Account input. Three valid rows remain. |
| 15 | Press Ctrl+Enter. | The split saves once. U3 Bank has three entries and a balance of 950.00. Expense A totals 45.00. Expense B totals 5.00. |
| 16 | Open another new entry. Focus a row and press Ctrl+Shift+Delete while only two rows exist. | Two rows remain. The minimum of two postings is preserved. Cancel this draft. |

Check date shortcuts in a separate unsaved draft:

| Step | Action | Expected result |
|---|---|---|
| 17 | Set Date to November 10, 2026 and Bank posting date to November 11, 2026. Focus Date. Press `-`, then `+`. | Date changes to November 9, then November 10. Bank posting date stays November 11. |
| 18 | Focus Bank posting date. Press `+`, then `-`. Try both the main keyboard and numeric keypad if available. | Bank posting date changes to November 12, then November 11. Date stays November 10. Record unavailable keypad checks. |
| 19 | Press `t` in Date. Press `T` in Bank posting date. | Each field changes to today in the recorded book time zone. |
| 20 | Clear Bank posting date. Press `+`. | The empty field uses book-local today as its starting date and advances one day. |
| 21 | Enter invalid text in Date. Press `-`. Then press `T`. | The invalid date produces understandable feedback without a crash. T replaces it with book-local today. |
| 22 | Enter January 1, year 1 in Date and press `-`. Enter December 31, year 9999 and press `+`. | The control does not wrap or crash. The boundary date remains valid. Record any field-format limitation as blocked rather than passed. |
| 23 | Cancel the draft. | The three saved entries and the Bank balance of 950.00 remain unchanged. |

**Result:** Unverified.
**Evidence, actual balances, and keyboard observations:**

## J03 — Select an account type by typing its prefix

- [ ] **J03 passed.** References: shared Accounts control and P22.

**Prerequisites:** Complete Preparation.

| Step | Action | Expected result |
|---|---|---|
| 1 | Open **Accounts…**. Click **New account**. Enter name `U3 Prefix Test`. | The new-account form permits type selection. |
| 2 | Tab to **Type (fixed after creation)**. Type `exp` promptly. | Expense is selected by its name prefix. Selecting by mouse is not needed. |
| 3 | If typing with the dropdown closed does not select Expense, open it with Alt+Down and repeat. Record both outcomes. | Prefix selection must be usable through the keyboard. A failure in the supported closed/open behavior remains visible in the report. |
| 4 | Set Open date to January 1, 2000. Save the account. | The saved type is Expense. The displayed accounting class is Expenses. |
| 5 | Select U3 Bank, then select U3 Prefix Test again. Try to focus or change Type. | The saved account remains Expense. Type cannot be changed after creation. |
| 6 | Close Accounts with Ctrl+W. | Accounts closes. The main application remains open. |

**Result:** Unverified.
**Evidence:**

## J04 — Enforce open and close dates

- [ ] **J04 passed.** References: P13 and P15.

**Prerequisites:** Create two more accounts through Accounts.
Use type Expense for both.
Create `U3 Future` with Open date December 1, 2026.
Create `U3 Historical` with Open date January 1, 2000 and no Close date yet.

| Step | Action | Expected result |
|---|---|---|
| 1 | Create a balanced entry dated November 10, 2026. Use Payee `U3 History` and Description `J04 historical`. Enter U3 Historical Debit `10` and U3 Bank Credit `10`. Save. | The entry saves while Historical is open. Bank becomes 940.00. |
| 2 | In Accounts, set U3 Historical Close date to November 15, 2026. Save and close Accounts. Reload the register. | The account retains its history and its close date. |
| 3 | Open a new entry dated November 10, 2026. Search for U3 Historical in a posting Account input. | The closed account is excluded from choices for a new reference, even though the entry date is before closure. |
| 4 | Type its full name manually. Enter debit 10 and a matching U3 Bank credit. Try Save. | Saving is rejected. Feedback identifies an ineligible closed reference. The draft remains. No new transaction appears. |
| 5 | Replace Historical with U3 Future. Keep Date at November 10. Try Save. | Saving is rejected because the date is before the account opened. Feedback differs from the closed-reference reason. The draft remains. |
| 6 | Change Date to December 1, 2026. Search for U3 Future again. | Its eligible choice appears without reopening the dialog. Cancel this draft without saving. |
| 7 | Edit `J04 historical`. Keep Date November 10 and the original Historical posting. Change only its Payee to `U3 History Revised`. Save. | The original closed reference is retained on its original line. Its label remains readable. The metadata edit saves. Bank remains 940.00. |
| 8 | Edit that entry again. Change Date to November 15, 2026. Try Save. | Saving is rejected on the closure date. The draft remains available for correction. |
| 9 | Cancel. Reopen the saved entry. | The saved date is still November 10. Amounts, accounts, and line states have not changed. |
| 10 | Cancel the editor. Delete only `J04 historical` through the normal confirmation dialog. | Bank returns to 950.00. The three J02 entries remain. |

For steps 4, 5, and 8, capture the feedback and the retained draft.
After cancellation, verify the saved register again.
UI observations establish rejection and unchanged visible finances.
Use the existing SQL regression evidence for the stronger claim that no write was dispatched or stored versions changed.
Do not infer that claim solely from a screenshot.

**Result:** Unverified.
**Evidence and actual rejection messages:**

## J05 — Format amounts without accepting invalid precision

- [ ] **J05 passed.** References: P15 and P22.

**Prerequisites:** Bank balance is 950.00.
Open a new entry with Date November 10, 2026 and Description `J05 unsaved`.
Use U3 Expense A on row 1 and U3 Bank on row 2.

Repeat each row of this table in the first Debit field.
Use Ctrl+A before typing the next value.
Press Tab after entering the value.
Then repeat in a Credit field to verify both columns.
Do not save valid examples from this table.

| Input | Expected result after Tab |
|---|---|
| `25` | `25.00` |
| `25.5` | `25.50` |
| `25.5000` | `25.50`; value-preserving trailing zeros are accepted. |
| `abc` | The original invalid text remains available for correction. It is not replaced with zero. |
| `25.501` | The excess nonzero precision is not rounded away. |
| `1.000000000000000000000000000001` | The original invalid precision remains. It is not converted to `1.00`. |

| Step | Action | Expected result |
|---|---|---|
| 1 | Put the long fractional value above in row 1 Debit. Put Credit `1` on row 2. Click Save. | Saving is rejected with field/line feedback. The invalid amount remains available for correction. |
| 2 | Re-enter the long fractional value in Debit. Keep that field focused and press Ctrl+Enter directly. | Saving is rejected even without first using Tab to leave the field. |
| 3 | Try Continue with the same invalid draft. | No new draft replaces the invalid one. No success is reported. |
| 4 | Replace Debit with `1.000000000000000000000000000000`. Press Tab. | The exact value is accepted and displays `1.00`. Do not save this draft. |
| 5 | Cancel. Reload U3 Bank. | Bank remains 950.00. Only the three J02 entries remain. |

The editor-to-service automated regressions verify zero write dispatch for invalid precision.
These manual steps verify the visible error, retained text, and unchanged saved finances.

**Result:** Unverified.
**Evidence:**

## J06 — Identify the lines reset by a financial edit

- [ ] **J06 passed.** References: P15 and FR-023.

**Prerequisites:** Create a separate entry with Date November 10, 2026.
Use Payee `U3 Reset` and the unique Description `J06 reset fixture`.
Enter Expense A Debit 20, Expense B Debit 5, and Bank Credit 25, in that order.
Save and close the editor.
Bank is now 925.00.

US3 has no controls to set reconciliation states.
Use the following data update only in the dedicated synthetic test database.
In SQL Server Management Studio, connect to the test server and select that database.
Run `SELECT DB_NAME();` first and check the name against the run record.
Do not change connection permissions if the runtime identity rejects the update.
Use the synthetic fixture's maintenance identity instead.
If that identity is unavailable, mark this case blocked.

This script changes test data only.
It does not change the schema or require a migration.
It requires exactly one matching entry with three lines.

```sql
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF (SELECT COUNT(*) FROM dbo.JournalEntry
    WHERE Description = N'J06 reset fixture') <> 1
    THROW 51000, 'Expected one synthetic reset entry.', 1;

DECLARE @ResetEntryId bigint = (
    SELECT JournalEntryId FROM dbo.JournalEntry
    WHERE Description = N'J06 reset fixture'
);

IF (SELECT COUNT(*) FROM dbo.JournalEntryLine
    WHERE JournalEntryId = @ResetEntryId) <> 3
    THROW 51000, 'Expected three synthetic reset lines.', 1;

UPDATE dbo.JournalEntryLine
SET ReconciliationStatus = 2
WHERE JournalEntryId = @ResetEntryId;

COMMIT;

SELECT l.JournalEntryLineId, l.LineOrder, a.Name,
       l.Debit, l.Credit, l.ReconciliationStatus
FROM dbo.JournalEntryLine AS l
JOIN dbo.Account AS a ON a.AccountId = l.AccountId
WHERE l.JournalEntryId = @ResetEntryId
ORDER BY l.LineOrder;
```

Record the query output.
Status values are 0 = Uncleared, 1 = Cleared, and 2 = Reconciled.
Run the script before opening the editor so that you do not create an unrelated version conflict.

| Step | Action | Expected result |
|---|---|---|
| 1 | Reload the register and edit `J06 reset fixture`. | All three lines display Reconciled. |
| 2 | Change only Payee to `U3 Reset Revised`. Save. Reopen. | All three lines remain Reconciled. A text-only edit does not reset them. |
| 3 | Change line 1 Debit from 20 to 21. Change line 3 Credit from 25 to 26. Leave line 2 unchanged. Save. | The financial edit saves. A notice identifies lines 1 and 3 by their displayed order and account paths. Database IDs alone are insufficient. |
| 4 | Capture the notice immediately. Reopen the entry. | Lines 1 and 3 are Uncleared. Line 2 is still Reconciled. Bank is 924.00. |
| 5 | Close the editor. Rerun the fixture script to set all three lines to Reconciled again. Reload and reopen the entry. | The editor loads the new versions and displays all three as Reconciled. |
| 6 | Change line 1 Debit from 21 to 22. Change line 3 Credit from 26 to 27. Click Continue. | The notice identifies reset lines 1 and 3 by order and path. A fresh draft opens with Date retained and focused. |
| 7 | Capture the Continue notice. Cancel the fresh draft. Reopen the saved reset entry. | Lines 1 and 3 are Uncleared. Line 2 remains Reconciled. Bank is 923.00. |
| 8 | Close the editor. Delete only `J06 reset fixture` through the normal confirmation dialog. | Bank returns to 950.00. The J02 entries remain. |

If a reset notice disappears before it can be read, record that as a usability failure.
No reconciliation-status editing control is expected in US3.

**Result:** Unverified.
**Evidence, notices, and actual line states:**

## J07 — Edit, cancel deletion, confirm deletion, and restart

- [ ] **J07 passed.** References: P07, P10, P11, and P17.

**Prerequisites:** The three J02 entries remain.
Bank is 950.00.
The temporary J04 and J06 entries have been deleted.

| Step | Action | Expected result |
|---|---|---|
| 1 | Select `J02 expense`. Focus the register and press `E`. | The saved entry opens with its original date, accounts, and amounts. |
| 2 | Change Expense A Debit from 25 to 30. Change Bank Credit from 25 to 30. Save. | Bank becomes 945.00. Expense A totals 50.00. Expense B remains 5.00. |
| 3 | Press F5 with the main window active. | The refreshed register retains U3 Bank and the selected expense entry. The balance remains 945.00. |
| 4 | Press Delete with the register focused. | The whole-entry delete preview opens. Cancel has initial keyboard focus. |
| 5 | Inspect the preview. | It shows the saved date, description, both account lines, debit/credit amounts of 30, and each line's state. |
| 6 | Capture the delete dialog. Compare it with the source delete baseline at the same scaling. | Record visual differences for J01. All lines and actions remain readable and reachable. |
| 7 | Press Enter while Cancel still has focus. | The dialog closes without deletion. Bank remains 945.00. The expense still exists. |
| 8 | Open Delete again. Explicitly select **Delete entry** and activate it. | The complete expense entry disappears. No orphan posting remains. Bank becomes 975.00. Expense A totals 20.00. Expense B totals 5.00. |
| 9 | Press F5. | U3 Bank remains selected. Any remaining selection refers to an existing entry. The deleted entry cannot remain selected as a live transaction. |
| 10 | Select `J02 split`. Double-click it. Inspect its lines and cancel. | The remaining entry opens correctly with its three original postings. |
| 11 | Close the main window with Ctrl+W. Relaunch and reopen the same test book if needed. Select U3 Bank. | The opening and split entries remain. The expense remains deleted. Bank is 975.00, Expense A is 20.00, and Expense B is 5.00. |

**Result:** Unverified.
**Evidence, selection after refresh, and actual balances:**

## J08 — Test long splits, scaling, and accessibility

- [ ] **J08 passed.** References: P11, P13–P15, and P22.

**Prerequisites:** Create two Expense accounts with a shared long parent path.
For example, create an Expense parent named `U3 Household and Family Expenses`.
Create children named `Travel and Accommodation Domestic` and `Travel and Accommodation International`.
Use Open date January 1, 2000 throughout.
Record the resulting full paths.

Repeat the steps below at **100%, 150%, and 200%** Windows display scaling.
Use Settings → System → Display → Scale to change the setting.
Relaunch the application after each change.
If a scale is unavailable, record that scale as blocked.
Do not mark the whole case passed after testing only one scale.

| Step | Action | Expected result |
|---|---|---|
| 1 | Open a new entry. Inspect the normal-size dialog, then resize it to its minimum size. | Labels, fields, status text, errors, and buttons remain usable without overlap. |
| 2 | Enter Description `J08 unsaved`. Press Tab. Search for the shared long account name in the first Account input. | Focus goes directly to Account. The popup is visible and usable. The two long paths can be distinguished and their full values can be obtained. |
| 3 | Use Up/Down to choose a result and Enter to accept it. Reopen suggestions and press Escape. Repeat selection with Tab and then with the mouse. | Each acceptance method selects the intended account. Escape dismisses suggestions without unexpectedly closing the editor. Focus returns or advances predictably. |
| 4 | Create 30 posting rows with Add posting. Put Debit 1 in rows 1–29 and Credit 29 in row 30. Use the long-path accounts on debit rows and U3 Bank on the last row. | All 30 rows are reachable. The draft balances. Do not save it. |
| 5 | Tab forward through all rows. Use Shift+Tab to return through several rows. | Focus remains visible. The editor scrolls the focused row into view. Account, Debit, Credit order is preserved. |
| 6 | Focus a middle row's Account and press Ctrl+Shift+Delete. | Only that row is removed. Focus moves to an adjacent Account. Scrolling remains usable. |
| 7 | Enter `25.5` in an amount field and Tab away. Enter `abc` in another amount field and try Save. | The exact amount displays two decimal places. The invalid text remains available for correction. Feedback is readable and reachable at this scale. |
| 8 | Start Windows Narrator with Ctrl+Windows+Enter, or start your normal screen reader. Navigate Date, Bank posting date, Payee, Description, posting accounts, Debit, Credit, and actions. | Each control has a meaningful spoken name and role. Row context distinguishes repeated amount fields. Focus and validation feedback can be found without relying on color. Record the actual announcements for any unclear control. |
| 9 | Inspect a long account choice and the saved J02 split register text with the screen reader. Cancel the draft first if needed. | Full account paths, payee, and description are accessible. Popup navigation does not trap focus. |
| 10 | In a new unsaved draft, type `new entry`, `edit`, and ordinary digits in Payee and Description. Use Delete to remove characters. | Typing N or E does not open another entry. Delete edits text rather than deleting a transaction. |
| 11 | Press Ctrl+W from a journal text field while no write is pending. | Only the journal window closes through its normal close behavior. No draft is saved. The main window stays open. |
| 12 | Open Accounts, focus its search field, and press Ctrl+W. | Only Accounts closes. The main window stays open. |
| 13 | Capture the long-path popup, a scrolled middle row, bottom actions, and readable validation feedback. | Evidence identifies the actual scale. No important content is clipped or unreachable. |

Stop Narrator with Ctrl+Windows+Enter when finished, if you started it for this test.
Cancel all remaining drafts.
Restore your preferred Windows scaling.
No J08 entry should be saved.

| Scale | Result: Pass / Fail / Blocked | Screenshot names and observations |
|---|---|---|
| 100% | Unverified | |
| 150% | Unverified | |
| 200% | Unverified | |

**Overall result:** Unverified.
**Screen reader and version:**
**Accessibility failures or unclear announcements:**

## Finish the run

1. Record a result for J01–J08.
2. Attach screenshots or give their local paths.
3. Record each failure with its case ID and step number.
4. Keep unavailable checks marked Blocked and unverified.
5. Report the results before marking the corresponding parity rows or T107 complete.

Document review: These instructions preserve the eight original checks.
They separate manual observations from automated financial and write-dispatch evidence.
No test has been marked passed by this documentation change.
