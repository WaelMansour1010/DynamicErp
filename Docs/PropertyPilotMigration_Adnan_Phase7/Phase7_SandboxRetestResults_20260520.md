# Phase 7 - Sandbox Retest Results
Date: 2026-05-20
Sandbox: Alromaizan_PropertyPilot_Adnan_20260520
BatchId: E7EAD000-0000-4000-9000-202605200007

## Dry Run
| Metric | Result |
|---|---:|
| Accounts | 256 |
| Tenants | 256 |
| Properties | 26 |
| Units | 258 |
| Contracts | 283 |
| Contract batches | 1099 |
| Opening balance rows | 68 |
| Opening balance total | 1,156,544.6600 |
| Advance payment rows | 14 |
| Advance payments total | 55,592.8900 |

## Web Retest
| Test | Result | Notes |
|---|---|---|
| Login with ErpAdmin/dev local password | Pass | No Adnan password used |
| Open PropertyContract search | Pass | `ADNAN-C-1096` visible |
| Open receipt screen for migrated contract | Pass | `/CashReceiptVoucher/AddEdit?cid=944` HTTP 200 |
| Cash receipt via `CASH-PILOT` Id=5 | Pass | Saved voucher 38; stored as legacy method 1 after normalization |
| Bank receipt via `BANK-PILOT` Id=6 | Pass | Saved voucher 39; stored as legacy method 2 after normalization |
| Cash issue via `CASH-PILOT` Id=5 | Pass with warning | Saved voucher 20; balanced/no NULL; direct-expense account selection questionable |
| Bank issue via `BANK-PILOT` Id=6 | Pass | Saved voucher 21; balanced/no NULL |
| Termination | Pass | Saved termination 3; JE balanced/no NULL |

## Receipt Accounting Details
- Cash receipt: debit cashbox account `629`, credit renter account `764`, amount `125.0000`.
- Bank receipt: debit bank account `631`, credit renter account `764`, amount `130.0000`.

## Termination Accounting Details
- Debit renter account `764`, credit property revenue account `740`, amount `353,198.9800`.

## Note About Termination Amount
Termination total became `353,198.9800` in Phase7 because the two test receipts reduced outstanding balance before termination calculation. Phase6 termination before Phase7 receipts was `353,228.9800`.
