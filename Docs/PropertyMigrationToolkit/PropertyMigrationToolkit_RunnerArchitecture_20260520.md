# Property Migration Toolkit - Runner Architecture
Date: 2026-05-20

## Goal
Move from manual SQL execution toward a controlled Runner that a Technical Support or Senior Implementer can operate safely.

## Recommended Phasing
| Phase | Tool Shape | Recommendation |
|---|---|---|
| 1 | SQL + Config + Runner | Build first |
| 2 | Console Tool | Add after RSMDB/second customer validation |
| 3 | Admin Page | Add after workflow stabilizes |

## Runner Inputs
- Source DB.
- Target Clone DB.
- Customer Code.
- BatchId.
- Migration Mode: Strict/Tolerant/Hybrid.
- Cutoff Date.
- Include Accounting.
- Include Historical Receipts.
- Include Issues.
- Include Advance Payments.
- Include Terminations.
- Exclude Unsafe Payments.
- AutoFix options.

## Runner Responsibilities
1. Validate connections.
2. Validate target is clone.
3. Validate backup exists.
4. Create/load config.
5. Execute stages in order.
6. Store logs.
7. Show progress metrics.
8. Generate report.
9. Support DryRun, ReadyToTest, Rollback modes.

## Technical Design
Current best form:
- Config Table or JSON.
- PowerShell or .NET Console Runner.
- SQL templates executed by stage.
- Output written to `PropertyMigrationRunLog`, warnings, review queue, reconciliation tables.

## Why Console Before Admin UI
Console is easier to secure, script, retry, and run by implementation staff. Admin UI can later call the same runner/service.
