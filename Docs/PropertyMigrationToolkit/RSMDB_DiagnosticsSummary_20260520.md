# RSMDB Diagnostics Summary - 2026-05-20

## Execution Scope

Diagnostics were read-only. No migration was executed on RSMDB.

## Core Discovery Counts

| Area | Count |
|---|---:|
| Properties | 16 |
| Distinct owners | 4 |
| Unit rows | 629 |
| Unit type/lookup candidates | 102 |
| Contracts | 2,813 |
| Active contract candidates | 262 |
| Installments | 7,478 |
| Receipts Type 4 | 10,365 |
| Issues Type 5 | 7,632 |
| Terminations Type -1 | 754 |
| Unclassified Type 9088 | 64 |
| Journal lines | 139,769 |

## Relationship Diagnostics

| Check | Count | Severity |
|---|---:|---|
| Properties without owner | 0 | Pass |
| Units without property | 105 | Warning |
| Contracts without unit | 4 | Warning / AutoFix candidate |
| Contracts without renter | 4 | Warning / AutoFix candidate |
| Contracts without property | 4 | Warning / AutoFix candidate |
| Installments without contract | 0 | Pass |
| Receipts Type 4 without safe contract link | 1,587 | High / Review |
| Owner payable candidates | 4 | Review |

## Accounting Diagnostics

| Check | Count | Severity |
|---|---:|---|
| Journal lines missing account code | 0 | Pass |
| Potential unbalanced voucher groups using provisional logic | 51,968 | Critical until grouping/direction is confirmed |
| Issue/payment notes requiring manual classification | 7,632 | High |
| Unclassified 9088 notes | 64 | High |

## Interpretation

The journal imbalance count is provisional because `DOUBLE_ENTREY_VOUCHERS` grouping and debit/credit direction semantics must be confirmed before final accounting diagnostics. It is still a blocker for accounting migration until resolved.

## DryRun Runner Result

Runner DryRun was executed for configuration validation only. It did not run migration templates and did not modify RSMDB. The detailed Runner report is under the Runner `Reports` folder.

## Decision

RSMDB needs additional mapping review before Clone Migration. The safest next step is staging population on a clone only, then diagnostics and mapping review.
