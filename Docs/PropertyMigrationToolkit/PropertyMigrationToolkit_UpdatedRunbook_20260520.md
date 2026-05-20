# Property Migration Toolkit - Updated Runbook
Date: 2026-05-20

## Mode Selection
| Use Case | Mode |
|---|---|
| First messy legacy import | Tolerant |
| Normal ReadyToTest build | Hybrid |
| Final GoLive rehearsal | Strict |

## Tolerant Flow
1. Run Preflight.
2. Enable fallback flags.
3. Seed default entities.
4. Run discovery and diagnostics.
5. Run migration with AutoFix logging.
6. Review queue.
7. Reconcile.
8. Deliver ReadyToTest if critical accounting checks pass.

## Hybrid Flow
1. Master data can use fallback placeholders.
2. Accounting requires strict validation.
3. Receipts migrate only if safely linked.
4. Issues/owner payments remain review-first.
5. Open critical accounting issues block delivery.

## Strict Flow
1. No new fallback defaults.
2. No open suspense.
3. No open critical/warning items that affect operation.
4. All reconciliation must pass.
5. Use for final rehearsal only.

## Review Queue Closure
Before GoLive:
- Critical items must be closed.
- Suspense items must be resolved or finance-approved.
- Unknown units/properties/renters must be accepted or remapped.
- Excluded records must be signed off.

## ReadyToTest Acceptance
Allowed with open warnings if:
- No accounting criticals.
- No unbalanced journals.
- No `AccountId=NULL`.
- Warnings are visible in the report.

## GoLive Acceptance
Allowed only if:
- Strict mode or approved Hybrid final run.
- No critical errors.
- No unresolved suspense without sign-off.
- No hidden fallback counts.
- Business signs off reconciliation.
