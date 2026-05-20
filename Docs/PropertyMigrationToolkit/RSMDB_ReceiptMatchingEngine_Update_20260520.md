# RSMDB Receipt Matching Engine Update - 2026-05-20

## SQL Added
`F:\Source Code\DynamicErp\Docs\PropertyMigrationToolkit\Sql\RSMDB_ReceiptAllocationDiscovery_20260520.sql`

## What It Does
- Runs on clone/sandbox only.
- Reads RSMDB through SELECT only.
- Creates/refreshes `dbo.PropertyMigrationReceiptAllocationEvidence` in the clone.
- Evaluates direct and indirect evidence:
  - `Notes.ContNo`, `CusID`, `akarid`, `UnitNo`
  - `ContracttBillInstallmentsDone`
  - `ReciveDetails`
  - allocation headers
  - previous amount/date candidates
  - text customer matches
- Classifies into:
  - `AutoApprovedLink`
  - `HighConfidence`
  - `MediumReview`
  - `WeakMatch`
  - `Blocked`

## Runner Update
The runner now supports an optional stage:
`ReceiptAllocationDiscovery`

Config flag:

```json
"IncludeReceiptAllocationDiscovery": true
```

DryRun confirms this stage appears in the execution plan and does not execute SQL in DryRun mode.

## Matching Rule Change
The engine must not promote amount/date-only matches to `HighConfidence`.

High confidence requires at least one direct operational signal:
- `CashingType=8` with `ContNo`, or
- `ContracttBillInstallmentsDone` rows, or
- another proven allocation table that includes receipt note and installment/contract link.

## Current Outcome
The new engine did not promote the current 58 because the discovered evidence proves they are not normal contract receipts.
