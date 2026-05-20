# RSMDB Diagnostics Summary - 2026-05-20

## Scope

Diagnostics were run as SELECT-only discovery against `RSMDB`. No migration was executed.

Runner DryRun was also executed using:

- `F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner\Config\rsmdb-discovery.dryrun.json`

Runner report:

- `F:\Source Code\DynamicErp\Tools\DynamicErp.PropertyMigration.Runner\Reports\PropertyMigrationRunnerReport_RSMDB-DISCOVERY_20260520_105456.md`

## Counts

| Metric | Count |
|---|---:|
| Properties `TblAqar` | 16 |
| Properties without owner | 0 |
| Distinct owners | 4 |
| Actual units `TblAqarDetai` | 629 |
| Unit lookup/auxiliary `TblUnites` | 102 |
| Units without property | 105 |
| Contracts `TblContract` | 2813 |
| Active contract candidates | 262 |
| Contracts without unit | 4 |
| Contracts without renter | 4 |
| Contracts without property | 4 |
| Installments | 7478 |
| Installments without contract | 0 |
| Receipts `NoteType=4` | 10365 |
| Receipts Type 4 without contract link | 1587 |
| Issues `NoteType=5` | 7632 |
| Termination candidates `NoteType=-1` | 754 |
| Unclassified `NoteType=9088` | 64 |
| Journal lines | 139769 |
| Journal lines without account code | 0 |
| Potential unbalanced DEV groups by initial assumption | 51968 |
| Owner payable rows `TblAqrOwin` | 4 |
| Owner payment rows | 0 |
| Owner payment note rows | 0 |

## Critical Diagnostics

- `105` units in `TblAqarDetai` do not link to `TblAqar` by current `Aqarid` relationship.
- `4` contracts have missing unit/property/renter link candidates.
- `1587` receipt notes do not have a proven contract link by current simple rule.
- `51968` potential unbalanced journal groups indicate the journal grouping/direction rule is not ready.
- `NoteType=9088` remains unclassified.

## Decision

RSMDB requires additional mapping review before any clone migration execute.
