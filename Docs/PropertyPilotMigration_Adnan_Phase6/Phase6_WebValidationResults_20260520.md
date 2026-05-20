# Phase 6 Web Validation Results

Date: 2026-05-20

## Page Validation

| Test | URL | Result | Notes |
|---|---|---|---|
| Properties list | `/Property?searchWord=ADNAN-P-` | PASS | HTTP 200, migrated Adnan property rows visible. |
| Units list | `/PropertyUnit?searchWord=ADNAN` | PASS | HTTP 200, migrated unit rows visible. |
| Contracts list | `/PropertyContract?searchWord=ADNAN-C-1096` | PASS | HTTP 200, contract `ADNAN-C-1096` visible. |
| Contract edit/view | `/PropertyContract/AddEdit/661` | PASS | HTTP 200, migrated contract opened. |
| Receipt page for contract | `/CashReceiptVoucher/AddEdit?cid=661` | PASS | HTTP 200, contract and renter loaded. |
| Termination page | `/PropertyContractTermination/AddEdit` | PASS | HTTP 200. |
| Termination calculation API | `/PropertyContractTermination/GetPropertyContractTerminationDetails?...ADNAN-C-1096...` | PASS | Returned contract, renter, unit, unpaid details. |

## Receipt Test

### First Attempt

- Receipt Id: `36`
- Payment method: `CASH-PILOT`, Id `5`
- Result: saved but accounting debit line had `AccountId = NULL`.
- Diagnosis: application logic treats payment method Id `1` as cash and Id `2` as bank. Phase4 seed created Ids `5` and `6`, so the accounting branch did not resolve cash account.

### Compatibility Fix Inside Sandbox

Added Sandbox-only payment methods with compatible ids:

| Table | Id | Code |
|---|---:|---|
| `CashReceiptPaymentMethod` | 1 | `CASH-COMPAT` |
| `CashReceiptPaymentMethod` | 2 | `BANK-COMPAT` |
| `CashIssuePaymentMethod` | 1 | `CASH-COMPAT` |
| `CashIssuePaymentMethod` | 2 | `BANK-COMPAT` |

### Second Attempt

- Receipt Id: `37`
- Contract: `661` / `ADNAN-C-1096`
- Batch: `3921`
- Amount: `125.0000`
- Result: PASS.
- Voucher created and linked to contract batch.
- Batch paid/remain updated in receipt detail: Paid `125.0000`, Remain `41,275.0000`.

## Termination Test

- Termination Id: `2`
- Contract: `661` / `ADNAN-C-1096`
- Total unpaid amount: `353,228.9800`
- Detail rows: `8`
- Result: PASS.
