# Property Migration Toolkit - Exception Framework
Date: 2026-05-20

## Tables
| Table | Purpose |
|---|---|
| `PropertyMigrationWarning` | Non-blocking issues |
| `PropertyMigrationError` | Blocking/critical issues |
| `PropertyMigrationAutoFix` | Applied auto fixes |
| `PropertyMigrationFallback` | Default/fallback entity registry |
| `PropertyMigrationReviewQueue` | Human review workflow |
| `PropertyMigrationSuspenseMapping` | Unknown/mapped-to-suspense accounting items |
| `PropertyMigrationExcludedRecord` | Excluded records and reasons |
| `PropertyMigrationReconciliationResult` | Reconciliation metrics |
| `PropertyMigrationRunLog` | Step execution audit |

## Required Fields
Every exception record should capture:
- BatchId.
- CustomerCode.
- SourceDatabase.
- SourceTable.
- SourceId.
- EntityType.
- Severity.
- IssueType.
- OriginalValue.
- AppliedFix.
- FallbackEntity.
- RequiresManualReview.
- SuggestedAction.
- CreatedAt.

## Severity Classes
| Severity | Meaning | Blocks Migration | Blocks GoLive |
|---|---|---|---|
| Critical | Accounting/FK corruption risk | Usually yes | Yes |
| Warning | Data quality issue with fallback | No in tolerant/hybrid | Maybe until reviewed |
| Info | Optional/display data issue | No | No |

## Rule
Do not hide problems. The engine should move data forward, but make uncertainty visible.
