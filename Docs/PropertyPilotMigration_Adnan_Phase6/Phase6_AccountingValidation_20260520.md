# Phase 6 Accounting Validation

Date: 2026-05-20

## Receipt Accounting

Receipt Id: `37`  
DocumentNumber: `01-26-05-002`  
Amount: `125.0000`

| Line | Account | Debit | Credit | Notes |
|---|---|---:|---:|---|
| Debit | `110101001 - صندوق المكتب الرئيسى` | 125.0000 | 0.0000 | CashBox account `629`. |
| Credit | `110501001 - مستأجرين عقارات` | 0.0000 | 125.0000 | PartyType `4`, PartyId `1991`. |

| Check | Result |
|---|---|
| Journal created | PASS |
| Linked to receipt | PASS |
| Debit/Credit balanced | PASS, diff `0.0000` |
| Null account lines | PASS, `0` |
| Cash account correct | PASS after compatibility seed |
| Renter party link | PASS |

## Receipt Operational Impact

- `CashReceiptVoucherPropertyContractBatch` row created.
- Batch `3921` paid amount reflected as `125.0000` for receipt `37`.
- Remain reflected as `41,275.0000` for the tested receipt.

## Termination Accounting

Termination Id: `2`  
DocumentNumber: `01-26-05-001`  
TotalUnpaidAmount: `353,228.9800`

| Line | Account | Debit | Credit | Notes |
|---|---|---:|---:|---|
| Debit | `110501001 - مستأجرين عقارات` | 353,228.9800 | 0.0000 | PartyType `4`, PartyId `1991`. |
| Credit | `31041001 - ايراد ايجار عقارات` | 0.0000 | 353,228.9800 | Revenue account. |

| Check | Result |
|---|---|
| Termination created | PASS |
| Detail rows | PASS, `8` rows |
| Journal created | PASS |
| Debit/Credit balanced | PASS, diff `0.0000` |
| Null account lines | PASS, `0` |
| Renter account link | PASS |
