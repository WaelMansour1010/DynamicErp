# LC Live Schema Mapping

Scope: الاعتمادات المستندية / Letters of Credit. This document is based on live SQL Server inspection performed against `Wael\Sql2019`.

## Database Selection

The configured web database in `DynamicErp/Web.config` is `MyErp`; it contains only `transactionsVatDetails` from the requested legacy tables, so it is not a complete Main ERP schema specimen.

Full legacy table coverage was found in several local catalogs including `Eng`, `Adnan`, `Baltan`, `Cash`, `Dania`, `GhalinaERP`, `Henaki2026`, `Mass`, `Nagahat`, `RSMDB`, `Rwaby`, `SaryMas`, and `Zobir`. The representative schema used here is `Eng`.

Important naming correction: the live table is `TblLC`; legacy code also uses `TBLLC`/`tbllc` casing in SQL strings. SQL Server resolves this on case-insensitive databases, but the migration should standardize on `TblLC`.

## Live Row Counts in `Eng`

| Table | Rows |
| --- | ---: |
| `TblLC` | 191 |
| `ACCOUNTS` | 16652 |
| `DOUBLE_ENTREY_VOUCHERS1` | 11440 |
| `Notes` | 74420 |

## `TblLC`

Primary key: `PK_TblLC` on `TblLCID`.

Identity: `TblLCID` is not identity. VB6 assigns it with `new_id("TblLC", "TblLCiD", "", True)`.

Important columns:

| Column | Type | Null | Role |
| --- | --- | --- | --- |
| `TblLCID` | `int` | no | LC document id / manually assigned PK |
| `LCNO` | `nvarchar(255)` | yes | LC number |
| `LCTyperId` | `int` | yes | LC type lookup |
| `BankId`, `BankID2` | `int` | yes | issuing bank and payment bank |
| `BoxID` | `int` | yes | cashbox/payment source |
| `Value` | `money` | yes | LC amount |
| `OpenValue` | `float` | yes | opening/expense value used in note type `22010` |
| `CurrencyId`, `Currency_rate` | `int`, `float` | yes | currency and rate |
| `VendorId` | `int` | yes | supplier/vendor from `TblCustemers` |
| `CountryId` | `int` | yes | country lookup |
| `FromDate`, `Todate`, `CloseDate`, `LastParcilDate` | `datetime` | yes | lifecycle dates |
| `Locked` | `bit` | yes | lock/status flag |
| `BranchID` | `int` | yes | branch link |
| `userid` | `int` | yes | saved by user |
| `Remarks` | `nvarchar(4000)` | yes | notes |
| `Account_Code`, `LCAccount_Code` | `nvarchar(255)` | yes | LC account code |
| `Account_CodeMargin`, `MarginAccount_Code` | `nvarchar(255)` | yes | margin accounts |
| `AcceptAccount_Code` | `nvarchar(255)` | yes | acceptance account |
| `AccountExpensCode`, `AccountExpensParent` | `nvarchar(255)` | yes | LC expense account and parent |
| `AccountMarginParent`, `AccountLGParent`, `AccountAcceptanceParent` | `nvarchar(255)` | yes | selected parent accounts |
| `opening_balance_voucher_id` | `float` | yes | opening-balance voucher group id |
| `OpenBalanceDate`, `OpenBalance`, `OpenBalanceType` | `datetime`, `float`, `int` | yes | opening balance fields |
| `NoteID`, `NoteSerial`, `NoteID2`, `NoteSerial2`, `NoteIDOpen`, `NoteSerialOpen` | `int`/`nvarchar` | yes | generated note/voucher identifiers |
| `NoteIDRowId`, `NoteID2RowId`, `NoteIDOpenRowId` | `uniqueidentifier` | yes | duplicate-safe note row identifiers |
| `PaymentTypeID`, `ChequeNumber`, `ChequeDueDate` | `int`, `int`, `datetime` | yes | payment method details |
| `project_id`, `projectName`, `AccountExpProject` | `int`, `nvarchar(255)`, `nvarchar(250)` | yes | optional project link |

No foreign keys were returned for `TblLC`; account and lookup relationships are maintained by convention in VB6.

## `ACCOUNTS`

Primary key: `PK_ACCOUNTS` on `Account_ID`.

Identity: `Account_ID` is identity.

Unique indexes: `IX_ACCOUNTS` and `UX_ACCOUNTS_Code` on `Account_Code`.

Important columns: `Account_Code nvarchar(50) not null`, `Account_Name nvarchar(4000)`, `Account_NameEng nvarchar(4000)`, `Parent_Account_Code nvarchar(70)`, `last_account bit not null`, `cannot_del bit not null`, `BasicAccount bit not null`, `Account_Serial nvarchar(4000)`, `opening_balance money default 0`, `opening_balance_type nvarchar(50)`, `DateCreated smalldatetime`, `Branch varchar(50)`, `BranchID int`, `UserId int`, `TblLCID int`.

LC creates child accounts through `ModAccounts.AddNewAccount`; generated account rows may store `TblLCID`.

## `DOUBLE_ENTREY_VOUCHERS1`

Purpose: opening-balance journal rows. Used by `ModAccounts.AddNewDev` when `opening_balance=True`, and by LC grid voucher helper `CREATE_VOUCHER_GE2` when `mIsOpenBalance=True`.

Primary key: no PK was returned in the live key query.

Foreign keys:

| Column | References |
| --- | --- |
| `Account_Code` | `ACCOUNTS.Account_Code` |
| `Notes_ID` | `Notes1.NoteID` |

Important columns: `Double_Entry_Vouchers_ID int`, `DEV_ID_Line_No int`, `Account_Code nvarchar(50)`, `Value money`, `Credit_Or_Debit smallint`, `Double_Entry_Vouchers_Description nvarchar(4000)`, `RecordDate datetime`, `Notes_ID int`, `Transaction_ID int`, `UserID int`, `Posted int`, `PostedDate datetime`, `PostedUserID int`, `Account_Interval_ID int`, `credit_value money`, `depet_value money`, `currency nvarchar(50)`, `rate float`, `valuee money`, `project_bill_no int`, `project_id int`, `bill_id int`, `opening_balance_voucher_id float`, `branch_id int`, `DueDate datetime`, `totalPayed float`, `IsHiddenInv bit`.

Debit/credit convention from VB6: `Credit_Or_Debit=0` means debit, `1` means credit.

## `Notes`

Primary key: `PK_Notes` on `NoteID`.

Identity: `NoteID` is not identity; VB6 assigns with `new_id("Notes", "NoteID", "", True)`.

Foreign keys found include `CusID -> TblCustemers.CusID`, `UserID -> TblUsers.UserID`, `Transaction_ID -> Transactions.Transaction_ID`, and `BankID -> BanksData.BankID`.

Important LC fields: `NoteID`, `NoteDate`, `NoteType`, `NoteSerial`, `NoteSerial1`, `Note_Value`, `BankID`, `DueDate`, `UserID`, `Remark`, `branch_no`, `Double_Entry_Vouchers_ID`, `TblLCID`, `RowId`, `TableName`, `Currency_id`, `Currency_rate`, `VAT`, `VATYou`, `TotalValue`, `PreVAT`, `AccountCodeVat`, `last_changed`, `RecTime`, e-invoice status fields.

LC save logic also deletes from `Notes1` and `DOUBLE_ENTREY_VOUCHERS1` by `TblLCID` during edit, so opening-balance note behavior must be checked against `Notes1` before implementation.

## Lookup Tables Observed in LC Code

The live schema and VB6 flow reference `BanksData`, `TblBoxesData`, `currency`, `LCTypes`, `TblCountriesData`, `TblCustemers`, `TblBranchesData`, `branches`, and possibly `Notes1`. These were not fully mapped in this phase except where required for accounting linkage.

## Migration Notes

Do not create SQL against `TBLLC`; use `TblLC`. Before implementation, confirm which live catalog is the target production database because `MyErp` is not a complete Main ERP schema.
