# Property Migration Toolkit - Logging & Audit Trail
Date: 2026-05-20

## Required Audit Fields
- Start Time.
- End Time.
- Customer Code.
- Source DB.
- Target DB.
- BatchId.
- User.
- Step Name.
- Migration Mode.
- Warnings Count.
- Errors Count.
- AutoFix Count.
- Excluded Records Count.
- Reconciliation Result.
- ReadyToTest Status.

## Tables Used
| Table | Audit Purpose |
|---|---|
| `PropertyMigrationBatch` | Overall run status |
| `PropertyMigrationRunLog` | Step-level execution |
| `PropertyMigrationWarning` | Warning trail |
| `PropertyMigrationError` | Error trail |
| `PropertyMigrationAutoFix` | Every fallback/autofix |
| `PropertyMigrationReviewQueue` | Manual review lifecycle |
| `PropertyMigrationReconciliationResult` | Metrics and pass/fail |

## Report Outputs
- Technical execution log.
- Business reconciliation summary.
- Review queue export.
- AutoFix summary.
- GoLive blockers.
- ReadyToTest delivery report.
