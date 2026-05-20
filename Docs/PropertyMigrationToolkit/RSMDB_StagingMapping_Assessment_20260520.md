# RSMDB Staging Mapping Assessment - 2026-05-20

## Scope

This assessment prepares RSMDB for the PropertyMigrationToolkit staging contract only. No migration was executed on RSMDB.

## Staging Contract Reviewed

The generic templates now expect customer-specific scripts to populate staging tables such as:

- `PropertyMigrationSourceProperty`
- `PropertyMigrationSourceUnit`
- `PropertyMigrationSourceRenter`
- `PropertyMigrationSourceContract`
- `PropertyMigrationSourceInstallment`
- `PropertyMigrationSourceOpeningBalance`
- `PropertyMigrationSourceAdvancePayment`
- `PropertyMigrationSourceReceipt`
- `PropertyMigrationSourceIssue`
- `PropertyMigrationSourceJournal`
- `PropertyMigrationSourceJournalLine`
- `PropertyMigrationSourceTermination`
- `PropertyMigrationSourceOwner`
- `PropertyMigrationSourcePropertyOwner`
- `PropertyMigrationSourceOwnerBalance`
- `PropertyMigrationSourceOwnerPayment`

## RSMDB Source Mapping Draft

| RSMDB Source | Staging Target | Status | Notes |
|---|---|---|---|
| `TblAqar` | `PropertyMigrationSourceProperty` | Draft mapped | Property master. |
| `TblAqar.ownerid -> TblCustemers.CusID` | `PropertyMigrationSourceOwner` | Draft mapped | Owner master comes from customer master by role. |
| `TblAqar.ownerid` | `PropertyMigrationSourcePropertyOwner` | Draft mapped | Property-level owner link. |
| `TblAqarDetai` | `PropertyMigrationSourceUnit` | Draft mapped | Actual unit rows. |
| `TblUnites` | Lookup/type candidate | Review | Appears to be unit type/category support, not unit master. |
| `TblContract.CusID -> TblCustemers.CusID` | `PropertyMigrationSourceRenter` | Draft mapped | Renter master by contract relationship. |
| `TblContract` | `PropertyMigrationSourceContract` | Draft mapped | Active rule still requires final business review. |
| `TblContractInstallments` | `PropertyMigrationSourceInstallment` | Draft mapped | Installment source. |
| `Notes` Type 4 | `PropertyMigrationSourceReceipt` | Draft staged | Only valid when linked through installment/contract payment relationship. |
| `Notes` Type 5 | `PropertyMigrationSourceIssue` | Review only | Payment meaning unsafe until source type is proven. |
| `DOUBLE_ENTREY_VOUCHERS` | `PropertyMigrationSourceJournal` / `PropertyMigrationSourceJournalLine` | Restricted draft | Only linked to approved staged receipts for now. |
| `TblAqrOwin` | `PropertyMigrationSourceOwnerBalance` | Review only | Owner payable candidate. |
| `Notes` Type -1 | `PropertyMigrationSourceTermination` | Review only | Termination candidate; requires VB6 confirmation. |
| `Notes` Type 9088 | Review Queue | Unclassified | Do not migrate until meaning is proven. |

## Current Read-Only Diagnostics

| Metric | Value |
|---|---:|
| Properties | 16 |
| Properties without owner | 0 |
| Distinct owners | 4 |
| Units from `TblAqarDetai` | 629 |
| `TblUnites` rows | 102 |
| Units without property | 105 |
| Contracts | 2,813 |
| Active contract candidates | 262 |
| Contracts without unit | 4 |
| Contracts without renter | 4 |
| Contracts without property | 4 |
| Installments | 7,478 |
| Installments without contract | 0 |
| Receipt notes Type 4 | 10,365 |
| Type 4 receipts without contract link | 1,587 |
| Issue notes Type 5 | 7,632 |
| Termination notes Type -1 | 754 |
| Unclassified Type 9088 notes | 64 |
| Journal lines | 139,769 |
| Journal lines missing account code | 0 |
| Owner payable candidates `TblAqrOwin` | 4 |

## Important Difference From Adnan

RSMDB has more unresolved accounting and relationship noise than Adnan:

- More contracts overall and a different active candidate count.
- 105 unit rows without property link.
- 1,587 receipt notes without safe contract linkage.
- 7,632 issue/payment notes that are not safe to migrate yet.
- 4 owner payable candidates in `TblAqrOwin`.
- Large journal table requiring direction/grouping validation.

## Decision

RSMDB is ready for staging-mapping review and diagnostics, not for migration execution. The next safe step is to populate staging on a clone only, inspect Review Queue and mapping gaps, then approve or adjust the mapping.
