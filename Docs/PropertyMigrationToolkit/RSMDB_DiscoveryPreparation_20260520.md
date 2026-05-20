# RSMDB Discovery Preparation - 2026-05-20

## Scope

Discovery only. No migration was executed on `RSMDB`, and no write operation was performed against `RSMDB`.

Raw discovery output:

- `F:\Source Code\DynamicErp\Docs\PropertyMigrationToolkit\RSMDB_Discovery_RAW_20260520.txt`
- `F:\Source Code\DynamicErp\Docs\PropertyMigrationToolkit\RSMDB_Discovery_Columns_20260520.txt`

## Candidate Property Tables

| Table | Rows | Initial Meaning |
|---|---:|---|
| `TblAqar` | 16 | Properties/buildings candidate |
| `TblAqarDetai` | 629 | Property/unit/detail candidate |
| `TblUnites` | 102 | Unit lookup/detail candidate |
| `TblContract` | 2813 | Property contracts candidate |
| `TblContractInstallments` | 7478 | Contract installments candidate |
| `TblContractInstallmentsHist` | 803 | Historical installments candidate |
| `TblContractInstallmentsOld` | 1346 | Older installments candidate |
| `tblContractInsAllocations` | 227 | Allocation/payment distribution candidate |
| `tblContractInsAllocationsDetails` | 4438 | Allocation details candidate |
| `tblContractInsAllocationsDetails2` | 29866 | Additional allocation details candidate |
| `Notes` | 39257 | Mixed vouchers/receipts/issues/terminations/journals |
| `DOUBLE_ENTREY_VOUCHERS` | 139769 | GL journal lines candidate |

## Candidate NoteTypes

Important discovered counts:

| NoteType | Count | Candidate Meaning |
|---:|---:|---|
| `-1` | 754 | Termination/settlement candidate, requires VB6 confirmation |
| `4` | 10365 | Receipt candidate |
| `5` | 7632 | Issue/payment candidate |
| `60` | 2426 | Contract journal candidate |
| `9088` | 64 | VAT/installment candidate from previous VB6 pattern |
| `3` | 12332 | Needs classification before accounting migration |
| `57` | 2314 | Needs classification |
| `9090` | 361 | Needs classification |

## Key Accounting Columns

`DOUBLE_ENTREY_VOUCHERS` contains:

- `Double_Entry_Vouchers_ID`
- `DEV_ID_Line_No`
- `Account_Code`
- `Value`
- `Credit_Or_Debit`
- `Notes_ID`
- `ReceiptID`
- `Transaction_ID`
- `Aqarid`
- `unitno`
- `RelatedEntityType`
- `RelatedEntityID`

These columns are enough to design an accounting mapping, but not enough to approve migration without verifying direction semantics and voucher linkage.

## Active Contract Rule

Not approved yet. Candidate sources are:

- `TblContract`
- `TblContractInstallments`
- `TblRentStatus`
- `TblRentType`
- Contract dates/status columns from the detailed column discovery.

The rule must be confirmed from VB6 business logic before RSMDB migration execute.

## Config Update

`PropertyMigrationToolkit_RSMDBConfig_DRAFT_20260520.sql` was updated with discovered candidate tables and NoteTypes. It remains unapproved with `IsApproved=0`.

## Decision

RSMDB is ready for mapping design, not migration. The next step is a customer-specific staging population script for RSMDB.
