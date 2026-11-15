# Accounting Vocabulary and Model Notes

**Feature**: [Phase 1 specification](spec.md)
**Status**: Requirements interpretation and design recommendations; not an implemented schema.

## Naming

Use **Book**, **Account**, **JournalEntry**, and **JournalEntryLine**. A journal entry represents
one balanced accounting event; a line represents its effect on one account. “Transaction” can
remain a user-facing label. Avoid a second Transaction table describing the same event.

A register is a view of journal activity for an account, not a separate authoritative ledger.
A database transaction is the mechanism for committing an entire entry atomically.

## Complete Accounting-Class Catalog

Beancount's top-level accounting classes are the following five. Its account hierarchy and
commodity model allow more specific accounts and holdings; they do not define a sixth
“Security” accounting class.

| Class | Normal balance side | Examples to support |
|---|---|---|
| Assets | Debit | Bank/checking, savings, cash, receivables, brokerage cash, security holdings |
| Liabilities | Credit | Credit cards, loans, mortgages, other amounts owed |
| Equity | Credit | Opening balances, owner equity, equity adjustments |
| Income | Credit | Salary, interest, dividends, other income |
| Expenses | Debit | Food, housing, fees, taxes, other expenses |

Examples are optional user-created accounts, not a fixed exhaustive subtype list. Contra
accounts can have the opposite balance without changing the class's debit/credit mechanics.
Security holdings, including stock, fund, ETF, and bond units, belong to asset accounts in the
phase-1 scope. Detailed instrument valuation and trading behavior are deferred.

Keep the source UI's groups **Banks**, **Credit Cards**, **Investments**, **Expenses**, and
**Other** separate from class. **All** is a filter. Store explicit grouping rather than infer
it from account names. Income and Equity remain reachable through Other/All. Renaming an
account must not change its accounting class or display group.

## Account Types and Phase-1 UI Behavior

Account type is selected during account creation. It determines the accounting class and
initial display group. The account setup shows the resulting class; it is not an independently
editable value that can conflict with the type. A display-group override affects placement
only. Persist the chosen type rather than infer it from the account name.

| Account type | Accounting class | Default UI group | Phase-1 UI behavior |
|---|---|---|---|
| Bank | Assets | Banks | Monetary register, journal entry, and line reconciliation |
| Cash | Assets | Banks | Monetary register, journal entry, and line reconciliation |
| Credit Card | Liabilities | Credit Cards | Credit-card grouping/type label; monetary register and line reconciliation |
| Loan Receivable | Assets | Other | Receivable type label; monetary register and split-entry support |
| Loan Payable | Liabilities | Other | Payable type label; monetary register and split-entry support |
| Investment/Brokerage | Assets | Investments | Monetary register plus basic security holdings, opening quantities, and internal holding transfers |
| Other Asset | Assets | Other | General asset monetary register and journal entry |
| Other Liability | Liabilities | Other | General liability monetary register and journal entry |
| Equity | Equity | Other | Equity register, including opening-balance counterpart entries |
| Income | Income | Other | Income register and journal entry |
| Expense | Expenses | Expenses | Expense register and journal entry |

All types retain entry details, search, split entries, and line reconciliation. The table
highlights grouping and capability differences; it does not restrict common journal features.
Security quantity controls apply to Investment/Brokerage accounts. Security remains an instrument
held by an account, not an accounting class. A brokerage account can also contain cash activity.

Debit/credit meaning and the signed-balance convention remain consistent across types. The
register uses the Payment/Deposit presentation mapping defined below; account type does not
change stored debit/credit meaning. A loan payment can use an ordinary split entry between
principal, interest expense, and the paying bank account.

Phase 1 does not add amortization schedules, interest calculation, credit-card statement
management, or trading workflows. Dedicated workflows can use the explicit account types later.
Changing an existing account's type is deferred; renaming and display-group changes remain
available and preserve the type and class.

## Book Currency

Use one currency for each book.
USD is the default. Alternative books can select another supported currency, but all monetary
lines within a book must use its selected currency. Reject foreign-currency input; never
convert it automatically. Lock the currency after the first entry to avoid reinterpreting
history. Currency changes requiring historical conversion are outside phase 1.

Support currencies with zero, one, or two fractional digits initially, such as JPY, USD,
and EUR. Validate currency-specific fractional precision even when the storage scale is two.
Do not silently accept a currency that requires more precision. The plan must choose an explicit
supported-currency catalog rather than claiming every ISO currency works with two decimals.

## Proposed Schema Responsibilities

| Record | Minimum responsibility | Design boundary |
|---|---|---|
| Book | Identity, name, currency, time-zone identifier | One per database in phase 1 |
| Account | Identity, book, parent, name, type, type-determined class, UI group, active/open/close information | Stable foreign keys; no cycles or cross-book parents |
| JournalEntry | Identity, book, accounting date, optional bank posting date, payee, description, version | Owns ordered lines; atomic save/edit/delete |
| JournalEntryLine | Entry, account, line order, Debit, Credit, reconciliation status, version coverage | Exactly one positive monetary side; other side zero |
| Currency | Code, name, allowed fractional digits | Book monetary unit; no conversion table required |
| Security | Identity, symbol, name, quantity precision | Independent from currency and account class |
| Security movement | Security, holding account, signed quantity change, related journal line | Quantity movement paired with explicit monetary carrying amount |
| Connection profile | Stable profile ID, friendly name, connection string, protected credential content, expected Book ID | User configuration, outside the financial database |
| Schema version | Installed schema compatibility | Repeatable initialization; no destructive automatic replacement |

The data model defines bigint record keys, tinyint enum keys, single-column primary and foreign
keys, separate UNIQUE constraints, indexes, and security fields on journal lines. The selected migration tool is grate, with its own script history and hashes.
Application schema compatibility remains a separate concern. These notes describe the information and invariants they must retain.

### Connection and Book Identities

A connection profile has its own stable ID in user-local configuration. Its friendly name
and connection details may change without changing that ID. The logical book identity is Book.StableIdentifier, stored in the financial database.
It is distinct from the local bigint BookId used for relationships. These identities serve different purposes.

A profile records its expected Book ID after the first successful association. On later opens,
verify the actual Book ID before activating the book. Reject a mismatch. Require explicit
reassociation to target a different book; editing a connection string is not reassociation.
The data model defines bigint record IDs and the specific GUID uses for logical book identity,
local profile identity, and operation correlation.

Future scripts can use stable profile IDs instead of changeable friendly names. A future API
will need its own host-configured connections and authorized Book ID resolution. Automated
writes must select their target explicitly rather than inherit the desktop's last connection.
API and command-line implementation remain outside phase 1.

### Local Startup Preference

Store the stable identity of the last profile that successfully opened a compatible book in
user-local configuration, alongside the saved connection profiles. It is not a database table
or a financial record. A profile rename preserves this identity. Connection tests and failed
switches do not change the preference. Report a failure to save it under FR-006.

On launch, attempt that profile automatically. If it is missing, unusable, or fails to open,
show the connection dialog. Allow cancellation while connecting. Do not silently open another
database or initialize a schema during this automatic attempt.

### Debit and Credit

Persist **Debit** and **Credit** as separate non-null decimal columns on each JournalEntryLine.
Use zero for the unused side. Require `Debit > 0 AND Credit = 0`, or `Credit > 0 AND Debit = 0`.
Each entry has at least two valid lines; total Debit equals total Credit in the book currency.
The database and service design must enforce same-book references and atomic posting.
An ordinary row constraint alone cannot enforce the sum across an entire journal entry.

The default `decimal(10, 2)` stores up to 99,999,999.99 per positive debit/credit field.
Validate monetary range and currency precision at service input boundaries and before final
values are persisted, before narrowing conversions. Reject excess nonzero fractional digits;
accept trailing zeros that do not change the value. Internal decimal calculations may retain
extra precision. Do not let implicit database rounding change a final value. Any intended
rounding of calculated amounts needs an explicit, tested policy.
Totals can exceed a single line's range: aggregation must use adequate decimal precision and
must never downcast a whole-account balance into a single-line amount field.

Preserve the source register's signed running balance convention (debits minus credits) and
its **Payment** and **Deposit** headings. For each register row, net the selected account's
lines: a net credit appears as a positive magnitude under Payment; a net debit appears as a
positive magnitude under Deposit. A zero net movement leaves both movement columns blank.
The entry details and journal editor expose the actual Debit/Credit lines, including offsetting
lines that net to zero. This presentation does not change stored values or balance calculations.

Apply this mapping consistently across account classes, matching the source UI's signed
movement convention. For a credit card, a purchase credits the liability and appears under
Payment; paying down the card debits it and appears under Deposit. Explain that the labels
refer to movement in the selected account, not universally to a purchase or a card repayment.
Credit-normal balances for Liabilities, Equity, and Income remain negative under the retained
signed-balance convention. Payment/Deposit tooltips MUST identify the corresponding credit/debit
movement so the presentation is unambiguous.

### Reconciliation

Required persisted mapping:

| Value | State | Meaning |
|---|---|---|
| 0 | Uncleared | Not yet marked as in the reconciliation process |
| 1 | Reconciling | In the reconciliation process; not yet confirmed cleared |
| 2 | Cleared | Explicitly confirmed cleared |

Use non-null `tinyint` with a default of 0 and a constraint limiting values to 0–2.
Statuses belong to JournalEntryLine. One transfer's two sides may differ. A register row that
summarizes several selected-account lines may show “Mixed,” but that is a display summary,
not a fourth stored state. An entry-wide checkbox must not overwrite distinct line states.

Status-only updates do not alter money. Financial changes reset the affected reconciled line
to Uncleared; preserve other line identities/statuses. Include those changes in the same
atomic update with concurrency checks.

### Dates and Time

Use `DateOnly`/SQL `date` for accounting and bank posting dates. Store operational timestamps
in UTC. Resolve today using the book time zone. Keep the time-zone policy out of the machine's
current location. The source's persisted last-date behavior is adapted to an in-session batch
entry convenience so a new session starts at book-local today.

### Securities Without a Trading Engine

Security units are quantities, not foreign currencies or two-decimal monetary values. Store
and display fractional quantities independently. A candidate quantity type is `decimal(18, 8)`;
the plan must confirm range, supported precision, and validation before committing that choice.
This does not replace the default money type. Any monetary precision exception still needs
the justification required by the constitution.

For an opening holding, debit the holding account with its explicit book-currency carrying
amount, credit Equity, and record the opening units against the holding line. For an internal
transfer, credit the source holding account, debit the destination with the same carrying
amount, and move equal quantities of the same security. Validate available quantities and
carrying amounts. Do not infer market values or compute cost lots.

Zero-value quantity adjustments, security purchases/sales, corporate actions, and short
positions are deferred. This bounds the initial model without pretending to implement all
Beancount booking behavior. The owner may prune the catalog or expand these workflows later.

## Reference Examples for Planning

- Opening bank balance: debit Bank 1,000.00 USD; credit Opening Equity 1,000.00 USD.
- Expense: debit Food 25.00 USD; credit Bank 25.00 USD.
- Salary: debit Bank 500.00 USD; credit Salary Income 500.00 USD.
- Card payment: debit Credit Card 100.00 USD; credit Bank 100.00 USD.
- Opening holding: debit Security Asset 100.00 USD with +1.234567 units; credit Equity 100.00 USD.
- Holding transfer: credit Security A 20.00 USD with −0.234567 units; debit Security B 20.00 USD
  with +0.234567 units of the same security.

These examples are synthetic acceptance fixtures, not seed data for a real user's book.


## Persistence Design

The technical plan uses EF Core for reads and simple metadata updates. Stored procedures handle
writes with complex accounting or history checks. One Infrastructure repository contains all
application procedure calls. The operation matrix documents why guarded writes use procedures.
Grate owns all database definitions and schema changes, including tables. EF mappings do not create or migrate the database. See the
[operation matrix](contracts/application-services.md#persistence-operation-matrix).

FR-017 conflict checks and durable outcome recovery apply only to journal entries and lines.
Account, book, and security-definition setup retain ordinary validation and atomic writes,
without expected-version checks, CommandReceipt records, or protected pending-operation files.
