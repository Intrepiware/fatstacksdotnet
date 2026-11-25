# Manual test checklist: US3 journal entry

Use these cases on an interactive Windows desktop with a synthetic book.
See the [parity checklist](ui-parity.md) for the related acceptance requirements.
Follow the actions in order and compare each result with its expected result.

The owner closed the US3 manual test pass after reviewing the existing evidence.
The recorded results apply to the builds identified in the evidence. Keep the cases for future regressions.
See [the manual closeout](#us3-manual-test-closeout) for checks without individual results.

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

For a new run, check a case when all its expected results pass.
Record **Pass**, **Fail**, or **Blocked** in its result field.
For a check closed by owner decision, keep that decision with the result record.
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
11. Check that the default 90-day register window includes November 10, 2026.
    If it does not, use the date substitutions below.

Use the same dates throughout a run. If the original accounting date is outside the
register window, record a replacement date 21 days before today in the book time zone.
Apply these substitutions in the actions and expected results.

| Date in the instructions | Replacement                                           |
| ------------------------ | ----------------------------------------------------- |
| November 9, 2026         | One day before the replacement accounting date        |
| November 10, 2026        | The replacement accounting date                       |
| November 11, 2026        | One day after the replacement accounting date         |
| November 12, 2026        | Two days after the replacement accounting date        |
| November 15, 2026        | Five days after the replacement accounting date       |
| December 1, 2026         | Twenty-one days after the replacement accounting date |

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

- [x] **Earlier J01 editor layout passed.** Reference P12.
- [x] **Revised J01 editor layout passed.** The delete-dialog comparison belongs to J07.

**Prerequisites:** Complete Preparation.
Have the accepted source editor reference image available.
Use the source application under `.workspace/dotnet-beancount/src` as the comparison baseline.
See the [source parity map](../ui-parity.md) for the referenced screens and approved adaptations.
Development screenshots in `.workspace/ph01-us03/screenshots/` can identify earlier defects.
They do not prove that the current build passes.
If the source baseline is unavailable, mark only the comparison part blocked.

| Step | Action                                                                                                                                                                  | Expected result                                                                                                                                                                                                                            |
| ---- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 1    | Set Windows display scaling to 100% in Settings → System → Display. Relaunch the application if Windows requests it.                                                    | The recorded scale is 100%.                                                                                                                                                                                                                |
| 2    | Click **New entry**.                                                                                                                                                    | The Journal entry dialog opens. The accounting date uses today in the book time zone. Focus is in Date.                                                                                                                                    |
| 3    | Inspect the editor header and posting grid.                                                                                                                             | The header shows the book currency. The six columns are line number, Account, C, Debit, Credit, and Remove (×). The line numbers start at 1. The C column is read-only. Debit and Credit headings are centered. Amount inputs align right. |
| 4    | Resize the dialog to its smallest permitted size.                                                                                                                       | Fields remain usable. Scrolling makes content reachable. Buttons and errors do not overlap.                                                                                                                                                |
| 5    | Capture the editor at the same scale as the source reference. Compare the revised posting grid and totals with the approved [journal mock-up](../mockups/journal.html). | Record differences in spacing, alignment, density, labels, and actions. Use the desktop UI contract to judge the approved changes.                                                                                                         |
| 6    | Cancel the empty draft.                                                                                                                                                 | No transaction is added.                                                                                                                                                                                                                   |

**Current result:** Passed. New screenshot: .workspace\ph01-us03\manual\J01 - Journal Entry Smallest Size (2).png

**Earlier result:** Passed on the earlier build.
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

- [x] **Earlier J06 retest passed.** References P15 and FR-023.
- [x] **Revised J06 reset markers and notices passed.**

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
SET ReconciliationStatus = CASE WHEN LineOrder = 3 THEN 1 ELSE 2 END
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
The fixture sets lines 1 and 2 to Cleared. It sets line 3 to Reconciling.
Run the script before opening the editor so that you do not create an unrelated version conflict.

| Step | Action                                                                                                                                                                                                             | Expected result                                                                                                                                                                                                                                       |
| ---- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1    | Reload the register. Edit `J06 reset fixture`.                                                                                                                                                                     | Lines 1 and 2 show checkmarks. Line 3 shows R. The tooltips and accessible names give the full status for each line.                                                                                                                                  |
| 2    | Change only Payee to `U3 Reset Revised`. Check for reset cues. Save and reopen the entry.                                                                                                                          | The editor shows no reset warning or amber marker. All three statuses remain as set by the fixture.                                                                                                                                                   |
| 3    | Change line 1 Debit from 20 to 21. Restore 20. Change line 1 Debit to 21 again. Change line 3 Credit to 26. Leave line 2 unchanged. Check the cues. Run the draft checks below before saving. Save the entry.      | The first change marks only line 1. Restoring 20 removes its marker and warning. The final draft shows bold amber (`#A16207`) numbers on lines 1 and 3. The yellow warning names both lines by number and account path. The editor closes after Save. |
| 4    | After Save closes the editor, capture the yellow confirmation directly below the register search/date controls. Confirm it names lines 1 and 3. Click Dismiss and verify that the banner closes. Reopen the entry. | Lines 1 and 3 are Uncleared. Line 2 is still Cleared. Bank is 924.00.                                                                                                                                                                                 |
| 5    | Close the editor. Rerun the fixture script. Reload and reopen the entry.                                                                                                                                           | Lines 1 and 2 are Cleared. Line 3 is Reconciling. The editor loads the current versions.                                                                                                                                                              |
| 6    | Change line 1 Debit from 21 to 22. Change line 3 Credit from 26 to 27. Check the yellow warning before saving. Click Continue.                                                                                     | The notice identifies reset lines 1 and 3 by order and path. A fresh draft opens with Date retained and focused.                                                                                                                                      |
| 7    | Capture the Continue notice. Cancel the fresh draft. Reopen the saved reset entry.                                                                                                                                 | Lines 1 and 3 are Uncleared. Line 2 remains Cleared. Bank is 923.00.                                                                                                                                                                                  |
| 8    | Close the editor. Delete only `J06 reset fixture` through the normal confirmation dialog.                                                                                                                          | Bank returns to 950.00. The J02 entries remain.                                                                                                                                                                                                       |

Before saving in step 3, also run these checks on the draft:

1. Change line 1 Account to U3 Expense B. Check that the warning still identifies line 1.
2. Enter `abc` in line 1 Debit. Check that the red line badge stays readable.
3. Check that the warning still explains the reset for the changed account.
4. Restore U3 Expense A and Debit 21. Check that the red badge clears and the amber marker returns.
5. Confirm that line 2 has neither marker. Complete step 3.

The warning must update as the draft changes. It clears when the original values are restored.
The post-save confirmation stays visible until Dismiss is used or a later save replaces it.
It must remain readable when the main window is narrow.
If either message disappears before it can be read, record that as a usability failure.
No reconciliation-status editing control is expected in US3.

**Current result:** PASSED.

**Earlier result:** FAILED.
**Evidence, notices, and actual line states:** Step 3 "expects" column indicates I should see a special message that line 1 and 3 reset to uncleared. I DO NOT see this when I press the save button (the dialog simply closes). I DO see it if I press the Continue button however.

**Retest after warning and register-banner change:** Passed. Keep the original failure above as history.

Earlier fixture query output:

| JournalEntryLineId | LineOrder | Name         | Debit | Credit | ReconciliationStatus |
| :----------------- | :-------- | :----------- | :---- | :----- | :------------------- |
| 12                 | 1         | U3 Expense A | 20.00 | 0.00   | 2                    |
| 13                 | 2         | U3 Expense B | 5.00  | 0.00   | 2                    |
| 14                 | 3         | U3 Bank      | 0.00  | 25.00  | 2                    |

Second run fixture query output:

# Result Set Batch 1 - Query 1

```
JournalEntryLineId LineOrder Name Debit Credit ReconciliationStatus

---

10 1 U3 Expense A 20.00 0.00 2
11 2 U3 Expense B 5.00 0.00 2
12 3 U3 Bank 0.00 25.00 1
(3 rows affected)
```

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

- [x] **J08 passed.** References: P11, P13–P15, and P22.

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
| 6    | Focus a middle row’s Account input. Press Ctrl+Shift+Delete.                                                                                                                            | Only that row is removed. Focus moves to an adjacent Account input. Line numbers remain consecutive. Even-numbered rows regain the stripe.                                                                                          |
| 7    | Run the totals and validation checks below.                                                                                                                                             | The inputs, totals, and error cues remain readable and reachable at this scale.                                                                                                                                                     |
| 8    | Start Windows Narrator with Ctrl+Windows+Enter, or start your normal screen reader. Navigate Date, Bank posting date, Payee, Description, posting accounts, Debit, Credit, and actions. | Each control has a meaningful spoken name and role. Row context distinguishes repeated amount fields. Focus and validation feedback can be found without relying on color. Record the actual announcements for any unclear control. |
| 9    | Inspect a long account choice and the saved J02 split register text with the screen reader. Cancel the draft first if needed.                                                           | Full account paths, payee, and description are accessible. Popup navigation does not trap focus.                                                                                                                                    |
| 10   | In a new unsaved draft, type `new entry`, `edit`, and ordinary digits in Payee and Description. Use Delete to remove characters.                                                        | Typing N or E does not open another entry. Delete edits text rather than deleting a transaction.                                                                                                                                    |
| 11   | Press Ctrl+W from a journal text field while no write is pending.                                                                                                                       | Only the journal window closes through its normal close behavior. No draft is saved. The main window stays open.                                                                                                                    |
| 12   | Open Accounts, focus its search field, and press Ctrl+W.                                                                                                                                | Only Accounts closes. The main window stays open.                                                                                                                                                                                   |
| 13   | Capture the long-path popup, a scrolled middle row, bottom actions, and readable validation feedback.                                                                                   | Evidence identifies the actual scale. No important content is clipped or unreachable.                                                                                                                                               |

### J08 posting layout, totals, and validation

Repeat these checks at each J08 display scale.
Use a new draft with U3 Expense A on row 1 and U3 Bank on row 2.
Enter amounts with the Windows decimal separator.
Use the 30-row draft to repeat the layout checks with the posting scrollbar visible.

| Step | Action                                                                                                                                                                                                   | Expected result                                                                                                                                                                                                                                                                                                                                                                                                                    |
| ---- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| A1   | Inspect rows 1 and 2. Hover over one row. Move keyboard focus into another row.                                                                                                                          | Even-numbered rows have a continuous `#F0F8FC` stripe. Hover and keyboard focus apply a full-row `#DDEBFA` band. Account, Debit, and Credit input backgrounds stay white (`#FFFFFF`).                                                                                                                                                                                                                                              |
| A2   | Enter Debit `25` on row 1 and Credit `25` on row 2. Tab away from both amounts. Inspect the footing. Repeat the alignment check with the scrollbar visible.                                              | Each input displays two decimal places. The footing starts below the posting rows with a top rule. The Account column shows semi-bold `Totals:` in `#23384E` followed by muted `Balanced` text. The header, rows, and footing share the same column edges. Each total is numeric and lines up with the right text edge above it. Currency appears in the editor header. The totals omit currency codes and repeated column labels. |
| A3   | Change row 2 Credit to `20`. Press Ctrl+Enter.                                                                                                                                                           | The footing shows `Credits short by 5.00` in red (`#B42318`). Only the Debit total is red. Save and Continue are disabled. Ctrl+Enter leaves the draft open.                                                                                                                                                                                                                                                                       |
| A4   | Change row 2 Credit to `30`. Inspect the footing with a screen reader.                                                                                                                                   | The footing shows `Debits short by 5.00` in red. Only the Credit total is red. The screen reader can read both totals and the short-side status.                                                                                                                                                                                                                                                                                   |
| A5   | Type `20` into row 2 Debit. Then enter `25` into its Credit field.                                                                                                                                       | The first nonempty input clears the opposite field on the same row. The second input restores 25.00 on both sides. `Balanced` replaces the short-side message and both totals lose red emphasis. Save and Continue are enabled when no save is pending.                                                                                                                                                                            |
| A6   | Replace row 1 Debit with `25.0000`. Tab away.                                                                                                                                                            | Debit displays `25.00`. The footing stays balanced.                                                                                                                                                                                                                                                                                                                                                                                |
| A7   | Replace row 1 Debit with `abc`. Hover over it. Focus it with a screen reader. Try Save.                                                                                                                  | The muted message reads `Enter valid amounts to check the balance.` Both numeric totals are blank. The field has a 2px red (`#B42318`) border and an error tooltip. The screen reader exposes the invalid state and the error. The line number appears in white on a solid red pill badge. Save leaves the draft and its invalid text open.                                                                                        |
| A8   | Replace row 1 Account with an unknown account. Try Save again. Correct Debit to `25`, then correct Account to U3 Expense A.                                                                              | Correcting Debit immediately removes its error message and border. The Account error and line badge remain until Account is corrected. The Account cue then clears and the footing shows balanced totals.                                                                                                                                                                                                                          |
| A9   | Change row 2 Credit to `20` in the corrected draft. Then restore it to `25`.                                                                                                                             | The balance error has no line prefix. Restoring `25` immediately clears the error and the red emphasis.                                                                                                                                                                                                                                                                                                                            |
| A10  | Set row 1 Debit to `100000000` and row 2 Credit to the same amount. Try Save.                                                                                                                            | The editor rejects the out-of-range amounts. Feedback identifies the affected posting lines. The draft stays open.                                                                                                                                                                                                                                                                                                                 |
| A11  | Cancel the draft. Open a new one with three rows. Enter `abc` in the Debit fields of rows 2 and 3. Remove row 2.                                                                                         | The removed row’s error disappears. The remaining invalid input keeps its border and badge. Its message now names line 2, with a space after the middot in the line prefix.                                                                                                                                                                                                                                                        |
| A12  | Cancel the draft. Open a balanced draft with Debit `25` and Credit `25`. Type a complete date in each date field, one character at a time. In a US date format, use `11/10/2026`. Tab out of each field. | Each character stays where it was typed. Separators do not trigger an error or move the caret while typing. Each field accepts the completed date when focus leaves it.                                                                                                                                                                                                                                                            |
| A13  | Replace Date with an incomplete date. Tab out. Try Save. Correct Date. Repeat with an incomplete Bank posting date.                                                                                      | Each unfinished date stays visible after focus leaves it. The field shows an error. Save leaves the draft open.                                                                                                                                                                                                                                                                                                                    |
| A14  | With both date fields invalid, type a complete date in Date. Press `T` in Bank posting date.                                                                                                             | Correcting Date clears its error and marker. Bank posting date keeps its error until `T` replaces the invalid text with book-local today. Date keeps the corrected value.                                                                                                                                                                                                                                                          |
| A15  | Cancel the draft. Check the register.                                                                                                                                                                    | The saved entries and balances match their values before J08.                                                                                                                                                                                                                                                                                                                                                                      |

For C markers and reset cues, use J06. For all date shortcuts, use the date steps in J02.

**Result:** Closed by owner decision. Individual results for the full subsection were not recorded. The focused Windows run passed the date typing regression.
**Evidence:** Record totals, borders, badge text, and spoken feedback.

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
**Retest result:** Closed by owner decision. No separate spoken result was recorded.

Stop Narrator with Ctrl+Windows+Enter when finished, if you started it for this test.
Cancel all remaining drafts.
Restore your preferred Windows scaling.
No J08 entry should be saved.

| Scale | Result: Pass / Fail / Blocked | Screenshot names and observations |
| ----- | ----------------------------- | --------------------------------- |
| 100%  | Passed                        |                                   |
| 150%  | Closed by owner decision      | No separate result recorded.      |
| 200%  | Passed                        |                                   |

**Overall result:** Passed.
**Screen reader and version:**
**Accessibility failures or unclear announcements:**

## Finish the run

1. Record a result for J01–J08.
2. Attach screenshots or give their local paths.
3. Record each failure with its case ID and step number.
4. Mark unavailable checks Blocked. Record why each check could not run.
5. Report the results before marking the corresponding parity rows or T107 complete.
6. Record the revised J01, J06, and J08 results for the Phase 15 build.

## Phase 15 editor retest

The revised [J01](#j01--compare-the-journal-editor), [J06](#j06--identify-the-lines-reset-by-a-financial-edit),
and [J08](#j08--test-long-splits-scaling-and-accessibility) cases are closed for this release.
Use Preparation and J02 to set up the fixture if these cases are run again.
Use J07 for the delete preview.

## US3 manual test closeout

The owner accepted the existing manual coverage and ended the US3 manual test pass.
The record includes passes for J01–J08, including the revised J01 and J06 checks.
J08 has recorded scaling passes at 100% and 200%.

The J08 totals and validation steps lack individual results for the full subsection.
The 150% display check and the separate Payee and Description screen-reader retest also lack
individual results. Close these checks by owner decision. The focused Windows run separately
passed the automated test for typing and correcting dates in both fields.
