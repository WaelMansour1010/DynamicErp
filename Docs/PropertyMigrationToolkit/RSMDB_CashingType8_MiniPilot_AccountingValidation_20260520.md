# RSMDB CashingType=8 Mini Pilot Accounting Validation - 2026-05-20

## Validation Results Before Rollback

| Check | Result |
|---|---:|
| Receipts migrated | 32 |
| Journals migrated | 32 |
| Journal lines migrated | 64 |
| Debit total | 966,568.2500 |
| Credit total | 966,568.2500 |
| AccountId=NULL | 0 |
| Unbalanced journals | 0 |
| Duplicate receipt maps | 0 |
| Receipt batch allocation links | 51 |

## Paid/Remain

The pilot created CashReceiptVoucherPropertyContractBatch rows using the source allocation rows from ContracttBillInstallmentsDone:

- Paid = source allocation value.
- Remain = target batch total minus allocation value, capped at zero.
- The total paid across sample receipts matched the receipt amounts.

## Result

Pass for the Mini Pilot accounting scope. This does not approve broader RSMDB accounting migration yet.
