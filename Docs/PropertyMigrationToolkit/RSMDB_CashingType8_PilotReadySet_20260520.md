# RSMDB CashingType=8 Pilot ReadySet - 2026-05-20

## Current ReadySet

| Metric | Count / Value |
|---|---:|
| ReadyForAccountingPilot receipts | 0 |
| Ready journals | 0 |
| Ready journal lines | 0 |
| Ready receipt value | 0.0000 |

## Near-Ready Set
The practical first pilot candidate set after finance approval is:

| Metric | Count / Value |
|---|---:|
| Linked + balanced receipts needing finance approval | 505 |
| Receipt value | 14,115,520.2900 |
| Journal lines | 1,010 |
| Contracts | 229 |
| Renters | 227 |

## Why ReadySet Is Zero
No candidate currently has all journal accounts finance-approved. The previous Top 50 finance approvals were built around the earlier `CashingType=7` set and do not cover these true property receipt accounts sufficiently.

## Proposed Next Execute Scope After Approval
After finance approval, execute only:

- `NoteType=4`
- `CashingType=8`
- `ContracttBillInstallmentsDone` exists
- contract + installment + renter EntityMap exists
- balanced journals
- all accounts finance-approved
- no Issues
- no Owner Payments
- no Terminations
- no 9088
- no Suspense unless explicitly approved
