# Manual test checklist: US3 journal entry

This checklist expands the eight manual checks in [ui-parity.md](ui-parity.md).
The constitution requires repeatable manual checks but does not prescribe a test-runner layout.
This document uses numbered actions, expected results, and a result field for each case.

The filtered WindowsChecks suite has a user-reported pass.
These manual checks verify physical keyboard use, rendered content, and desktop accessibility.
Automated checks cannot establish the visual comparison or the screen-reader experience.
Some financial checks overlap automation so that the same outcomes can be checked through the UI.

## Record this run

- Tester and date: 21 Nov 2026
- Application build or commit: b0402bb468f1f87b466760c12f6fdeed7c647b3d
- Windows version (`winver`): Windows 11 Pro
- .NET SDK version (`dotnet --version`):
- SQL Server version: 10.0.115
- Screen resolution and scaling: 2560x1440, 100% Scaling (both recommended)
- Test connection/profile and database name, without credentials: fatstacks_test
- Book currency and time zone: USD, CST
- Decimal separator and date format used by Windows: . MM/DD/YYYY
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

   | Account name | Type    |
   | ------------ | ------- |
   | U3 Bank      | Bank    |
   | U3 Equity    | Equity  |
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

## J01 — Compare the journal editor

- [x] **J01 editor layout passed.** Reference: P12. The delete-dialog comparison belongs to J07.

**Prerequisites:** Complete Preparation.
Have the accepted source editor reference image available.
Use the source application under `.workspace/dotnet-beancount/src` as the comparison baseline.
See the [source parity map](../ui-parity.md) for the referenced screens and approved adaptations.
Development screenshots in `.workspace/ph01-us03/screenshots/` can identify earlier defects.
They do not prove that the current build passes.
If the source baseline is unavailable, mark only the comparison part blocked.

| Step | Action                                                                                                                        | Expected result                                                                                                                                                                                                      |
| ---- | ----------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1    | Set Windows display scaling to 100% in Settings → System → Display. Relaunch the application if Windows requests it.          | The recorded scale is 100%.                                                                                                                                                                                          |
| 2    | Click **New entry**.                                                                                                          | The Journal entry dialog opens. The accounting date uses today in the book time zone. Focus is in Date.                                                                                                              |
| 3    | Inspect Date, Bank posting date, Payee, Description, Account, Debit, Credit, Status, Add posting, Cancel, Continue, and Save. | Labels and controls are readable. Fields and headings align. Status text is read-only.                                                                                                                               |
| 4    | Resize the dialog to its smallest permitted size.                                                                             | Fields remain usable. Scrolling makes content reachable. Buttons and errors do not overlap.                                                                                                                          |
| 5    | Capture the current editor. Compare it with the source editor at the same scale.                                              | Record differences in spacing, alignment, density, labels, and actions. The bank posting date and separate Debit/Credit columns are deliberate additions. Do not treat other differences as approved without review. |
| 6    | Cancel the empty draft.                                                                                                       | No transaction is added.                                                                                                                                                                                             |

**Result:** Passed on the earlier build. The delete-dialog change does not affect this editor check.
**Evidence and differences:** .workspace\ph01-us03\manual\J01 - Journal Entry Smallest Size.png (requested screenshot).

## J02 — Enter an opening, expense, and split with the keyboard

- [x] **J02 passed.** References: P07, P11, P13–P16.

**Prerequisites:** J01 editor inspection is complete.
U3 Bank has no transactions.

| Step | Action                                                                                                                                                                                                                            | Expected result                                                                                                                                                        |
| ---- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1    | Focus the register, outside an editable text field. Press `N`.                                                                                                                                                                    | A new Journal entry dialog opens.                                                                                                                                      |
| 2    | Set Date to November 10, 2026. Set Bank posting date to November 11, 2026. Enter Payee `U3 Opening` and Description `J02 opening`.                                                                                                | Both dates and both text fields retain their separate values.                                                                                                          |
| 3    | With focus in Description, press Tab.                                                                                                                                                                                             | Focus moves directly to the first Account input. It skips noninteractive areas.                                                                                        |
| 4    | Type `U3 B`. Use Down/Up to highlight U3 Bank. Press Enter.                                                                                                                                                                       | U3 Bank is accepted. Focus moves to that row's Debit field.                                                                                                            |
| 5    | Enter Debit `1000`. Press Tab through Credit to the second Account input.                                                                                                                                                         | Debit displays `1000.00` after it loses focus. The order is Account, Debit, Credit, then the next row's Account.                                                       |
| 6    | Select U3 Equity in the second row. Leave Debit empty. Enter Credit `1000`.                                                                                                                                                       | The entry has a 1000 debit to Bank and a 1000 credit to Equity.                                                                                                        |
| 7    | Press Shift+Tab through the row fields. Then return to an Account input that already contains text.                                                                                                                               | The order reverses correctly. All account text is selected when the input gains focus.                                                                                 |
| 8    | Type `U3 E`, select U3 Equity, and accept it with Tab.                                                                                                                                                                            | Typing replaces the selected text. It does not append to the old account name. Tab accepts the intended suggestion.                                                    |
| 9    | Click **Continue**.                                                                                                                                                                                                               | The opening saves once. A fresh draft appears. Date remains November 10, 2026. Focus returns to Date.                                                                  |
| 10   | Enter Payee `U3 Shop` and Description `J02 expense`. Enter U3 Expense A Debit `25` and U3 Bank Credit `25`. Click **Save**.                                                                                                       | The dialog closes after success. U3 Bank has two entries and a balance of 975.00.                                                                                      |
| 11   | Select the expense row and inspect its register text and selected-entry summary. Hover over truncated text.                                                                                                                       | Payee appears in normal text. Description appears in italic text. Full text is available through the tooltip and accessibility information. No rendering error occurs. |
| 12   | Open a new entry. Use Date November 10, 2026, Payee `U3 Split Shop`, and Description `J02 split`. Enter U3 Expense A Debit `20`. Enter U3 Expense B Debit `5` on row 2. Leave its Credit empty. Tab from that final Credit field. | Because debits exceed credits, a third posting is added. Focus moves to its Account input.                                                                             |
| 13   | Enter U3 Bank Credit `25` on row 3. Tab from that Credit field.                                                                                                                                                                   | The entry balances. Tab does not add an unnecessary row.                                                                                                               |
| 14   | Click **Add posting**. Focus the new row's Account input. Press Ctrl+Shift+Delete.                                                                                                                                                | The new row is removed. Focus moves to an adjacent Account input. Three valid rows remain.                                                                             |
| 15   | Press Ctrl+Enter.                                                                                                                                                                                                                 | The split saves once. U3 Bank has three entries and a balance of 950.00. Expense A totals 45.00. Expense B totals 5.00.                                                |
| 16   | Open another new entry. Focus a row and press Ctrl+Shift+Delete while only two rows exist.                                                                                                                                        | Two rows remain. The minimum of two postings is preserved. Cancel this draft.                                                                                          |

Check date shortcuts in a separate unsaved draft:

| Step | Action                                                                                                                                                                                                                                                       | Expected result                                                                                                                        |
| ---- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------- |
| 17   | Click **New entry** in the main window. In the Journal entry dialog, set the top-left **Date** field to November 10, 2026. Set **Bank posting date**, immediately to its right, to November 11, 2026. Click inside the Date text input. Press `-`, then `+`. | Date changes to November 9, then November 10. Bank posting date stays November 11.                                                     |
| 18   | Focus Bank posting date. Press `+`, then `-`. Try both the main keyboard and numeric keypad if available.                                                                                                                                                    | Bank posting date changes to November 12, then November 11. Date stays November 10. Record unavailable keypad checks.                  |
| 19   | Press `t` in Date. Press `T` in Bank posting date.                                                                                                                                                                                                           | Each field changes to today in the recorded book time zone.                                                                            |
| 20   | Clear Bank posting date. Press `+`.                                                                                                                                                                                                                          | The empty field uses book-local today as its starting date and advances one day.                                                       |
| 21   | Enter invalid text in Date. Press `-`. Then press `T`.                                                                                                                                                                                                       | The invalid date produces understandable feedback without a crash. T replaces it with book-local today.                                |
| 22   | Enter January 1, year 1 in Date and press `-`. Enter December 31, year 9999 and press `+`.                                                                                                                                                                   | The control does not wrap or crash. The boundary date remains valid. Record any field-format limitation as blocked rather than passed. |
| 23   | Cancel the draft.                                                                                                                                                                                                                                            | The three saved entries and the Bank balance of 950.00 remain unchanged.                                                               |

**Result:** Passed.
**Evidence, actual balances, and keyboard observations:**

## J03 — Select an account type by typing its prefix

- [x] **J03 passed.** References: shared Accounts control and P22.

**Prerequisites:** Complete Preparation.

| Step | Action                                                                                                              | Expected result                                                                                                                      |
| ---- | ------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| 1    | Open **Accounts…**. Click **New account**. Enter name `U3 Prefix Test`.                                             | The new-account form permits type selection.                                                                                         |
| 2    | Tab to **Type (fixed after creation)**. Type `exp` promptly.                                                        | Expense is selected by its name prefix. Selecting by mouse is not needed.                                                            |
| 3    | If typing with the dropdown closed does not select Expense, open it with Alt+Down and repeat. Record both outcomes. | Prefix selection must be usable through the keyboard. A failure in the supported closed/open behavior remains visible in the report. |
| 4    | Set Open date to January 1, 2000. Save the account.                                                                 | The saved type is Expense. The displayed accounting class is Expenses.                                                               |
| 5    | Select U3 Bank, then select U3 Prefix Test again. Try to focus or change Type.                                      | The saved account remains Expense. Type cannot be changed after creation.                                                            |
| 6    | Close Accounts with Ctrl+W.                                                                                         | Accounts closes. The main application remains open.                                                                                  |

**Result:** Passed.

**Evidence:**
**Notes:** Step 3: typing `exp` with the dropdown shown or hidden selected Expense (as expected)

## J04 — Enforce open and close dates

- [x] **J04 passed.** References: P13 and P15.

**Prerequisites:** Create two more accounts through Accounts.
Use type Expense for both.
Create `U3 Future` with Open date December 1, 2026.
Create `U3 Historical` with Open date January 1, 2000 and no Close date yet.

| Step | Action                                                                                                                                                                  | Expected result                                                                                                                           |
| ---- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| 1    | Create a balanced entry dated November 10, 2026. Use Payee `U3 History` and Description `J04 historical`. Enter U3 Historical Debit `10` and U3 Bank Credit `10`. Save. | The entry saves while Historical is open. Bank becomes 940.00.                                                                            |
| 2    | In Accounts, set U3 Historical Close date to November 15, 2026. Save and close Accounts. Reload the register.                                                           | The account retains its history and its close date.                                                                                       |
| 3    | Open a new entry dated November 10, 2026. Search for U3 Historical in a posting Account input.                                                                          | The closed account is excluded from choices for a new reference, even though the entry date is before closure.                            |
| 4    | Type its full name manually. Enter debit 10 and a matching U3 Bank credit. Try Save.                                                                                    | Saving is rejected. Feedback identifies an ineligible closed reference. The draft remains. No new transaction appears.                    |
| 5    | Replace Historical with U3 Future. Keep Date at November 10. Try Save.                                                                                                  | Saving is rejected because the date is before the account opened. Feedback differs from the closed-reference reason. The draft remains.   |
| 6    | Change Date to December 1, 2026. Search for U3 Future again.                                                                                                            | Its eligible choice appears without reopening the dialog. Cancel this draft without saving.                                               |
| 7    | Edit `J04 historical`. Keep Date November 10 and the original Historical posting. Change only its Payee to `U3 History Revised`. Save.                                  | The original closed reference is retained on its original line. Its label remains readable. The metadata edit saves. Bank remains 940.00. |
| 8    | Edit that entry again. Change Date to November 15, 2026. Try Save.                                                                                                      | Saving is rejected on the closure date. The draft remains available for correction.                                                       |
| 9    | Cancel. Reopen the saved entry.                                                                                                                                         | The saved date is still November 10. Amounts, accounts, and line states have not changed.                                                 |
| 10   | Cancel the editor. Delete only `J04 historical` through the normal confirmation dialog.                                                                                 | Bank returns to 950.00. The three J02 entries remain.                                                                                     |

For steps 4, 5, and 8, capture the feedback and the retained draft.
After cancellation, verify the saved register again.
UI observations establish rejection and unchanged visible finances.
Use the existing SQL regression evidence for the stronger claim that no write was dispatched or stored versions changed.
Do not infer that claim solely from a screenshot.

**Result:** Passed.
**Evidence and actual rejection messages:**

## J05 — Format amounts without accepting invalid precision

- [x] **J05 passed.** References: P15 and P22.

**Prerequisites:** Bank balance is 950.00.
Open a new entry with Date November 10, 2026 and Description `J05 unsaved`.
Use U3 Expense A on row 1 and U3 Bank on row 2.

Repeat each row of this table in the first Debit field.
Use Ctrl+A before typing the next value.
Press Tab after entering the value.
Then repeat in a Credit field to verify both columns.
Do not save valid examples from this table.

| Input                              | Expected result after Tab                                                                 |
| ---------------------------------- | ----------------------------------------------------------------------------------------- |
| `25`                               | `25.00`                                                                                   |
| `25.5`                             | `25.50`                                                                                   |
| `25.5000`                          | `25.50`; value-preserving trailing zeros are accepted.                                    |
| `abc`                              | The original invalid text remains available for correction. It is not replaced with zero. |
| `25.501`                           | The excess nonzero precision is not rounded away.                                         |
| `1.000000000000000000000000000001` | The original invalid precision remains. It is not converted to `1.00`.                    |

| Step | Action                                                                                              | Expected result                                                                                   |
| ---- | --------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------- |
| 1    | Put the long fractional value above in row 1 Debit. Put Credit `1` on row 2. Click Save.            | Saving is rejected with field/line feedback. The invalid amount remains available for correction. |
| 2    | Re-enter the long fractional value in Debit. Keep that field focused and press Ctrl+Enter directly. | Saving is rejected even without first using Tab to leave the field.                               |
| 3    | Try Continue with the same invalid draft.                                                           | No new draft replaces the invalid one. No success is reported.                                    |
| 4    | Replace Debit with `1.000000000000000000000000000000`. Press Tab.                                   | The exact value is accepted and displays `1.00`. Do not save this draft.                          |
| 5    | Cancel. Reload U3 Bank.                                                                             | Bank remains 950.00. Only the three J02 entries remain.                                           |

The editor-to-service automated regressions verify zero write dispatch for invalid precision.
These manual steps verify the visible error, retained text, and unchanged saved finances.

**Result:** Passed.
**Evidence:**

## J06 — Identify the lines reset by a financial edit

- [x] **J06 passed.** References: P15 and FR-023.

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
Status values are 0 = Uncleared, 1 = Reconciling, and 2 = Cleared.
The SQL fixture sets all three lines to Cleared.
Run the script before opening the editor so that you do not create an unrelated version conflict.

| Step | Action                                                                                                                                                                                                             | Expected result                                                                                                                                          |
| ---- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1    | Reload the register and edit `J06 reset fixture`.                                                                                                                                                                  | All three C cells show checkmarks; their tooltips say Cleared.                                                                                                                         |
| 2    | Change only Payee to `U3 Reset Revised`. Check for a reset warning. Save. Reopen.                                                                                                                                  | All three lines remain Cleared. A text-only edit does not reset them. No reset warning appears before Save.                                              |
| 3    | Change line 1 Debit from 20 to 21. Confirm the warning names only line 1. Restore Debit 20 and confirm it disappears. Then change line 1 Debit to 21 and line 3 Credit to 26. Leave line 2 unchanged. Save.        | Before Save, a yellow warning names lines 1 and 3 by order and account path and says they will reset to Uncleared. The edit saves and the dialog closes. |
| 4    | After Save closes the editor, capture the yellow confirmation directly below the register search/date controls. Confirm it names lines 1 and 3. Click Dismiss and verify that the banner closes. Reopen the entry. | Lines 1 and 3 are Uncleared. Line 2 is still Cleared. Bank is 924.00.                                                                                    |
| 5    | Close the editor. Rerun the fixture script to set all three lines to Cleared again. Reload and reopen the entry.                                                                                                   | The editor loads the new versions and all three C cells show checkmarks for Cleared.                                                                                     |
| 6    | Change line 1 Debit from 21 to 22. Change line 3 Credit from 26 to 27. Check the yellow warning before saving. Click Continue.                                                                                     | The notice identifies reset lines 1 and 3 by order and path. A fresh draft opens with Date retained and focused.                                         |
| 7    | Capture the Continue notice. Cancel the fresh draft. Reopen the saved reset entry.                                                                                                                                 | Lines 1 and 3 are Uncleared. Line 2 remains Cleared. Bank is 923.00.                                                                                     |
| 8    | Close the editor. Delete only `J06 reset fixture` through the normal confirmation dialog.                                                                                                                          | Bank returns to 950.00. The J02 entries remain.                                                                                                          |

The warning must update as the draft changes and disappear when the original values are restored.
The post-save confirmation stays visible until Dismiss is used or a later save replaces it.
It must remain readable when the main window is narrow.
If either message disappears before it can be read, record that as a usability failure.
No reconciliation-status editing control is expected in US3.

**Result:** FAILED.
**Evidence, notices, and actual line states:** Step 3 "expects" column indicates I should see a special message that line 1 and 3 reset to uncleared. I DO NOT see this when I press the save button (the dialog simply closes). I DO see it if I press the Continue button however.

**Retest after warning and register-banner change:** Passed. Keep the original failure above as history.

Query output:

| JournalEntryLineId | LineOrder | Name         | Debit | Credit | ReconciliationStatus |
| :----------------- | :-------- | :----------- | :---- | :----- | :------------------- |
| 12                 | 1         | U3 Expense A | 20.00 | 0.00   | 2                    |
| 13                 | 2         | U3 Expense B | 5.00  | 0.00   | 2                    |
| 14                 | 3         | U3 Bank      | 0.00  | 25.00  | 2                    |

## J07 — Edit, cancel deletion, confirm deletion, and restart

- [x] **J07 passed.** References: P07, P10, P11, and P17.

**Prerequisites:** The three J02 entries remain.
Bank is 950.00.
The temporary J04 and J06 entries have been deleted.

| Step | Action                                                                                                                                                                                                                                                                  | Expected result                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |
| ---- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 1    | Select `J02 expense`. Focus the register and press `E`.                                                                                                                                                                                                                 | The saved entry opens with its original date, accounts, and amounts.                                                                                                                                                                                                                                                                                                                                                                                                                                         |
| 2    | Change Expense A Debit from 25 to 30. Change Bank Credit from 25 to 30. Save.                                                                                                                                                                                           | Bank becomes 945.00. Expense A totals 50.00. Expense B remains 5.00.                                                                                                                                                                                                                                                                                                                                                                                                                                         |
| 3    | Press F5 with the main window active.                                                                                                                                                                                                                                   | The refreshed register retains U3 Bank and the selected expense entry. The balance remains 945.00.                                                                                                                                                                                                                                                                                                                                                                                                           |
| 4    | Press Delete with the register focused.                                                                                                                                                                                                                                 | The whole-entry delete preview opens. Cancel has initial keyboard focus.                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| 5    | Inspect the gray summary card.                                                                                                                                                                                                                                          | It shows the saved date. The bold `U3 Shop` payee and italic `J02 expense` description share one wrapping line. The Account, C, Debit, and Credit headings follow directly, without a “Journal entry lines” heading. Both account paths are visible. The first line shows Debit 30.00, the second shows Credit 30.00, and the opposite cells are blank. Both C cells are blank because both lines are Uncleared. Hover over a C cell and check that its tooltip says Uncleared. There is no currency column. |
| 6    | Capture the revised delete dialog. Compare it with `.workspace/ph01-us03/manual/J07 Step 6 - Original Implementation.png` at the same scaling. Use `.workspace/ph01-us03/manual/J07 Step 6 - Delete journal entry dialog.png` as the earlier current-dialog comparison. | The summary card and posting rows match the source layout style. The payee and description share one line, the extra heading is absent, and the C, Debit, and Credit columns remain. The Delete button is red and labeled Delete. Cancel has visible initial focus. Record any remaining differences for J07. All lines and actions remain readable and reachable.                                                                                                                                           |
| 7    | Press Enter while Cancel still has focus.                                                                                                                                                                                                                               | The dialog closes without deletion. Bank remains 945.00. The expense still exists.                                                                                                                                                                                                                                                                                                                                                                                                                           |
| 8    | Open Delete again. Explicitly select **Delete** and activate it.                                                                                                                                                                                                        | The complete expense entry disappears. No orphan posting remains. Bank becomes 975.00. Expense A totals 20.00. Expense B totals 5.00.                                                                                                                                                                                                                                                                                                                                                                        |
| 9    | Press F5.                                                                                                                                                                                                                                                               | U3 Bank remains selected. Any remaining selection refers to an existing entry. The deleted entry cannot remain selected as a live transaction.                                                                                                                                                                                                                                                                                                                                                               |
| 10   | Select `J02 split`. Double-click it. Inspect its lines and cancel.                                                                                                                                                                                                      | The remaining entry opens correctly with its three original postings.                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| 11   | Close the main window with Ctrl+W. Relaunch and reopen the same test book if needed. Select U3 Bank.                                                                                                                                                                    | The opening and split entries remain. The expense remains deleted. Bank is 975.00, Expense A is 20.00, and Expense B is 5.00.                                                                                                                                                                                                                                                                                                                                                                                |

**Result:** Passed.

**Evidence, selection after refresh, and actual balances:** Final Delete dialog screenshot is .`workspace\ph01-us03\manual\J07 Step 6 - Delete journal entry dialog.png`

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

| Step | Action                                                                                                                                                                                  | Expected result                                                                                                                                                                                                                     |
| ---- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1    | Open a new entry. Inspect the normal-size dialog, then resize it to its minimum size.                                                                                                   | Labels, fields, status text, errors, and buttons remain usable without overlap.                                                                                                                                                     |
| 2    | Enter Description `J08 unsaved`. Press Tab. Search for the shared long account name in the first Account input.                                                                         | Focus goes directly to Account. The popup is visible and usable. The two long paths can be distinguished and their full values can be obtained.                                                                                     |
| 3    | Use Up/Down to choose a result and Enter to accept it. Reopen suggestions and press Escape. Repeat selection with Tab and then with the mouse.                                          | Each acceptance method selects the intended account. Escape dismisses suggestions without unexpectedly closing the editor. Focus returns or advances predictably.                                                                   |
| 4    | Create 30 posting rows with Add posting. Put Debit 1 in rows 1–29 and Credit 29 in row 30. Use the long-path accounts on debit rows and U3 Bank on the last row.                        | All 30 rows are reachable. The draft balances. Do not save it.                                                                                                                                                                      |
| 5    | Tab forward through all rows. Use Shift+Tab to return through several rows.                                                                                                             | Focus remains visible. The editor scrolls the focused row into view. Account, Debit, Credit order is preserved.                                                                                                                     |
| 6    | Focus a middle row's Account and press Ctrl+Shift+Delete.                                                                                                                               | Only that row is removed. Focus moves to an adjacent Account. Scrolling remains usable.                                                                                                                                             |
| 7    | Enter `25.5` in an amount field and Tab away. Enter `abc` in another amount field and try Save.                                                                                         | The exact amount displays two decimal places. The invalid text remains available for correction. Feedback is readable and reachable at this scale. Line errors use the affected row number, such as `Line 4 · Debit: …` or `Line 4 · Amount: …`, with a space after the middot. The entry-wide error reads `Balance: Total Debit must equal total Credit.` without a line prefix or middot.                                                                                  |
| 8    | Start Windows Narrator with Ctrl+Windows+Enter, or start your normal screen reader. Navigate Date, Bank posting date, Payee, Description, posting accounts, Debit, Credit, and actions. | Each control has a meaningful spoken name and role. Row context distinguishes repeated amount fields. Focus and validation feedback can be found without relying on color. Record the actual announcements for any unclear control. |
| 9    | Inspect a long account choice and the saved J02 split register text with the screen reader. Cancel the draft first if needed.                                                           | Full account paths, payee, and description are accessible. Popup navigation does not trap focus.                                                                                                                                    |
| 10   | In a new unsaved draft, type `new entry`, `edit`, and ordinary digits in Payee and Description. Use Delete to remove characters.                                                        | Typing N or E does not open another entry. Delete edits text rather than deleting a transaction.                                                                                                                                    |
| 11   | Press Ctrl+W from a journal text field while no write is pending.                                                                                                                       | Only the journal window closes through its normal close behavior. No draft is saved. The main window stays open.                                                                                                                    |
| 12   | Open Accounts, focus its search field, and press Ctrl+W.                                                                                                                                | Only Accounts closes. The main window stays open.                                                                                                                                                                                   |
| 13   | Capture the long-path popup, a scrolled middle row, bottom actions, and readable validation feedback.                                                                                   | Evidence identifies the actual scale. No important content is clipped or unreachable.                                                                                                                                               |

### J08 amount entry, status, and balance retest

Use a separate two-row draft for these checks.
They do not require recreating the 30-row draft.

1. Open **New entry**. Select U3 Expense A on row 1 and U3 Bank on row 2.
2. Check the column order: **Account, C, Debit, Credit**. Both C cells are blank for new
   Uncleared lines. Hover over a C cell to check the full status name.
3. Enter Debit `25` on row 1 and Credit `20` on row 2. Tab away from the last amount.
   Both values are right-aligned and show two decimal places. The summary shows total debits
   25.00, total credits 20.00, and an out-of-balance amount of 5.00.
4. Check that Save and Continue are disabled. Press Ctrl+Enter. The draft must remain open
   and no transaction must be created.
5. On row 2, enter Debit `20`. Its Credit must clear as soon as nonempty Debit text is entered.
   The summary now shows debits 45.00 and credits 0.00. Save and Continue remain disabled.
6. On row 2, enter Credit `25`. Its Debit must clear. The totals become 25.00 each.
   The summary says Balanced. Save and Continue become enabled when no save is pending.
7. On row 1, replace Debit `25` with `25.0000`. Tab away. It becomes `25.00`.
   The summary remains Balanced.
8. Replace that Debit with `abc`. The summary must ask for valid amounts instead of claiming
   the entry balances. Save remains available to show field validation for invalid text.
   Try Save. The invalid draft must remain open with an understandable field error.
9. Cancel the draft. Confirm the saved register and balances have not changed.
10. If a saved entry has Cleared or Reconciling lines, open it and inspect C.
    Expect a checkmark for Cleared and R for Reconciling. Hover for the full state name.
    This column is read-only. Cancel without saving.

**Result:** Unverified.
**Amount replacement, balance, and status observations:**

### J08 step 8: Payee and Description retest

The user reported that the screen reader did not announce these two field names.
The text inputs now expose explicit accessible names.
This fix still needs a manual retest on Windows.

1. Rebuild and reopen the application.
2. Start Narrator with Ctrl+Windows+Enter.
3. Open **New entry**.
4. Press Alt+P to focus Payee. Expect Narrator to announce **Payee** and its editable role.
5. Type `U3 Reader Test`. Press Tab to focus Description. Expect **Description** and its editable role.
6. Type `J08 reader retest`. Press Shift+Tab to return to Payee. Expect **Payee** again, even with text present.
7. Press Tab to return to Description. Expect **Description** again, even with text present.
8. Cancel the draft. Record the actual announcements and the screen reader used.

**Original observation:** Payee and Description labels were not announced on focus.
**Retest result:** Unverified.

Stop Narrator with Ctrl+Windows+Enter when finished, if you started it for this test.
Cancel all remaining drafts.
Restore your preferred Windows scaling.
No J08 entry should be saved.

| Scale | Result: Pass / Fail / Blocked | Screenshot names and observations |
| ----- | ----------------------------- | --------------------------------- |
| 100%  | Unverified                    |                                   |
| 150%  | Unverified                    |                                   |
| 200%  | Unverified                    |                                   |

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


## Phase 15 editor retest

Status: Unverified. Run after building the Phase 15 editor on interactive Windows.
Use a synthetic book and retain the existing J01–J08 results as test history.
Repeat the visual checks at 100%, 150%, and 200% display scaling.

1. Open a new entry. Check the six columns: line number, Account, C, Debit, Credit, and Remove.
   Check centered amount headings and right-aligned amount inputs.
2. Add enough rows to show the posting scrollbar. Remove a middle row.
   Check consecutive 1-based numbers and stripes on even-numbered rows (`#F0F8FC`).
3. Hover a row, then move keyboard focus through another row.
   Check a continuous `#DDEBFA` band on the active row and white posting text inputs.
   Check that Tab and Shift+Tab retain their existing order.
4. Enter matching debit and credit amounts. Check the semi-bold `Totals:` prefix (`#23384E`)
   and muted Balanced text. Check numeric totals directly under the input text edges.
   Repeat with the scrollbar visible and hidden. Check that the footing stays below the rows.
5. Increase Debit, then increase Credit beyond Debit. Check that the footing names the short
   side and difference in `#B42318`. Check that only the larger numeric total turns red.
   Check that Save and Continue stay disabled until totals match.
6. Enter malformed amount text. Check the muted invalid-amount message and hidden numeric totals.
   Check the 2px red input border, error tooltip, and solid red line badge with white text.
   With a screen reader, check the invalid state and error explanation on the input.
7. Attempt to save an invalid account or an out-of-range amount. Correct one field at a time.
   Check immediate removal of each resolved message and marker. Other errors must remain.
   Remove an invalid split row and verify that remaining errors identify the correct rows.
8. Enter invalid text in both date fields. Correct the accounting date, then the bank posting date.
   Check that each field clears only its own error. Check T replacement and the existing date shortcuts.
9. Edit a synthetic entry with Cleared and Reconciling lines. Change an account and an amount.
   Check bold amber (`#A16207`) line numbers and the yellow warning with affected line names.
   Restore the original values and check that both cues clear. Also check a row with a reset
   warning and a field error: its red error badge must stay readable.
10. Verify Save and Continue reset notices, the existing delete preview, pending-write guards,
    and retained failure drafts using J06–J08. Record actual results and screenshots.

Record screen-reader/version details and the actual spoken messages. Keep any unavailable
check marked Unverified. Do not mark T107 or T116 complete from a build or portable test run.
