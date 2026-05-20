# RSMDB CashingType=8 Link Evidence - 2026-05-20

## Link Evidence Source
Primary evidence source:
`RSMDB.dbo.ContracttBillInstallmentsDone`

This table provides the operational link between receipt note and paid installment:

- `NoteID`
- `istallid`
- `InstallNo`
- component paid amounts
- payment/date fields

## Link Validation Results

| Validation | Count |
|---|---:|
| Candidate receipts with `ContracttBillInstallmentsDone` | 8,083 |
| With renter link | 8,083 |
| With contract link in current EntityMap | 505 |
| With installment link in current EntityMap | 505 |
| With contract + installment + renter link | 505 |
| Needs additional operational link/entity map review | 7,578 |

## Why 7,578 Need Link Review
The current clone EntityMap was generated for minimal active operational migration, not full historical RSMDB contract history. Many `CashingType=8` receipts belong to older or non-active contracts/installments that are not yet present in DynamicErp target entities.

## Pilot Link Rule
A receipt can enter the first accounting pilot only if all are true:

- `Notes.CashingType = 8`
- `ContracttBillInstallmentsDone.NoteID = Notes.NoteID`
- contract exists in `PropertyMigrationEntityMap`
- installment exists in `PropertyMigrationEntityMap`
- renter exists in `PropertyMigrationEntityMap`
