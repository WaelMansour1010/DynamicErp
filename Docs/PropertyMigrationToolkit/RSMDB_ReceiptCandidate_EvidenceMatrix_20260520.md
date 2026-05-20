# RSMDB Receipt Candidate Evidence Matrix - 2026-05-20

## Matrix Source
The matrix was generated into the clone-only table:
`dbo.PropertyMigrationReceiptAllocationEvidence`

Target clone:
`Alromaizan_PropertyPilot_RSMDB_StagingClone_20260520`

Batch:
`1B5D8EB5-DD7E-4B63-8DDB-E7B00AAD558B`

## Classification Summary

| Decision | Count | Confidence |
|---|---:|---|
| AutoApprovedLink | 0 | Not reached |
| HighConfidence | 0 | Not reached |
| MediumReview | 4 | 65 |
| WeakMatch | 21 | 45 |
| Blocked | 33 | 20 |

## Evidence Signals Used

| Signal | Result |
|---|---|
| Direct FK in `Notes.ContNo` | 0/58 |
| Direct renter in `Notes.CusID` | 0/58 |
| Direct property in `Notes.akarid` | 0/58 |
| Contract receipt type `CashingType=8` | 0/58 |
| Installment paid rows in `ContracttBillInstallmentsDone` | 0/58 |
| Allocation header rows | 0/58 |
| Receipt detail rows in `ReciveDetails` | 49/58, but no contract evidence |
| Text/customer match | 1/58 |
| Amount/date candidate from previous engine | 25/58 |

## Why Medium/Weak Were Not Promoted
Amount/date candidates without direct contract/renter/installment evidence remain review-only. This is intentional because several RSMDB receipts have generic bank deposit text such as platform deposits, charge accounts, salary returns, bank reinforcements, and unidentified deposits.

## Query For Full Matrix
Use:

```sql
SELECT *
FROM dbo.PropertyMigrationReceiptAllocationEvidence
WHERE MigrationBatchId = '1B5D8EB5-DD7E-4B63-8DDB-E7B00AAD558B'
ORDER BY ReceiptNoteId;
```
