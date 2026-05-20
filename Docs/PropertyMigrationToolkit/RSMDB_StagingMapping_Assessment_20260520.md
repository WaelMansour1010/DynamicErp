# RSMDB Staging Mapping Assessment - 2026-05-20

## Scope

Created a draft staging mapping for RSMDB. This is not a migration script. It only prepares `PropertyMigrationSource*` staging data when executed later on a safe target clone.

Script:

- `F:\Source Code\DynamicErp\Docs\PropertyMigrationToolkit\Sql\RSMDB_StagingMapping_SELECT_TO_STAGING_DRAFT_20260520.sql`

## Staging Contract Tables

Reviewed and used:

- `PropertyMigrationSourceProperty`
- `PropertyMigrationSourceUnit`
- `PropertyMigrationSourceRenter`
- `PropertyMigrationSourceOwner`
- `PropertyMigrationSourcePropertyOwner`
- `PropertyMigrationSourceContract`
- `PropertyMigrationSourceInstallment`
- `PropertyMigrationSourceOpeningBalance`
- `PropertyMigrationSourceAdvancePayment`
- `PropertyMigrationSourceReceipt`
- `PropertyMigrationSourceIssue`
- `PropertyMigrationSourceOwnerBalance`
- `PropertyMigrationSourceOwnerPayment`
- `PropertyMigrationSourceJournal`
- `PropertyMigrationSourceJournalLine`
- `PropertyMigrationSourceTermination`

## RSMDB Mapping Draft

| RSMDB Source | Staging Target | Status |
|---|---|---|
| `TblAqar` | `PropertyMigrationSourceProperty` | Draft mapped |
| `TblAqar.ownerid -> TblCustemers` | `PropertyMigrationSourceOwner` | Draft mapped |
| `TblAqar.ownerid` | `PropertyMigrationSourcePropertyOwner` | Draft mapped |
| `TblAqarDetai` | `PropertyMigrationSourceUnit` | Draft mapped |
| `TblUnites` | Lookup/unit type candidate | Not migrated as unit master; VB6 shows actual units are `TblAqarDetai` |
| `TblContract.CusID -> TblCustemers` | `PropertyMigrationSourceRenter` | Draft mapped |
| `TblContract` | `PropertyMigrationSourceContract` | Draft active rule only |
| `TblContractInstallments` | `PropertyMigrationSourceInstallment` | Draft mapped |
| `Notes NoteType=4` | `PropertyMigrationSourceReceipt` | Draft mapped only with link indicators; unsafe rows marked invalid |
| `Notes NoteType=5` | `PropertyMigrationSourceIssue` | Review-only |
| `TblAqrOwin` | `PropertyMigrationSourceOwnerBalance` | Review-only owner payable staging |
| `DOUBLE_ENTREY_VOUCHERS` | `PropertyMigrationSourceJournal/Line` | Only voucher-linked and account-matched candidates |
| `Notes NoteType=-1` | `PropertyMigrationSourceTermination` | Review-only |
| `Notes NoteType=9088` | Review Queue | Not mapped until meaning confirmed |

## Important Findings

- RSMDB has `16` properties and `4` distinct property owners.
- RSMDB has `4` owner payable/schedule rows in `TblAqrOwin`.
- RSMDB has `0` rows in `TblOwnerPayment` and `TblNotesOwnerPayment` in current discovery.
- `TblUnites` appears not to be the actual unit master; actual units are in `TblAqarDetai` based on VB6 code and schema.

## Decision

RSMDB is not ready for migration execute. It is ready for mapping review and clone-only staging test.
