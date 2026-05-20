# Property Migration Toolkit - UI Concept
Date: 2026-05-20

## Main Dashboard
Metrics:
- Contracts discovered.
- Contracts migrated.
- Contracts excluded.
- Warnings count.
- AutoFix count.
- Suspense account count.
- Receipts migrated.
- Issues excluded/migrated.
- Journal count.
- Rejected journal count.
- Reconciliation status.
- ReadyToTest status.

## Setup Screen
Fields:
- Source Database.
- Target Clone Database.
- Customer Code.
- Cutoff Date.
- Migration Mode.
- Include Accounting.
- Include Historical Receipts.
- Include Issues.
- Include Advance Payments.
- Include Terminations.
- Exclude Unsafe Payments.
- AutoFix options.

## Stage Buttons
- Start Discovery.
- Start Diagnostics.
- Generate Mapping.
- Run Migration.
- Run Reconciliation.
- Generate ReadyToTest Report.
- Rollback Batch.

## Status Colors
| Status | Color |
|---|---|
| Pass | Green |
| Warning | Amber |
| Critical | Red |
| Needs Review | Blue |
| Blocked | Red |

## Future Admin Page
An Admin page can call the same runner service and render the database tables from the Exception Framework.
