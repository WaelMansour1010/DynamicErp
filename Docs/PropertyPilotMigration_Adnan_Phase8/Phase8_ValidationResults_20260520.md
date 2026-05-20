# Phase 8 - Validation Results
Date: 2026-05-20

## Sandbox Configuration Before Fix
| Item | Value |
|---|---:|
| Department 44 DirectExpensesAccountId | 629 |
| CashBox 1022 AccountId | 629 |
| DirectExpenses 4 AccountId | 805 |

This configuration explains the Phase7 cash issue journal: debit and credit both used account 629.

## Sandbox Configuration Fix
Sandbox-only script aligned `Department 44.DirectExpensesAccountId` to account `805` (`410502003 - تحميل و تنزيل`).

## Validation Tests
| Test | Result | Details |
|---|---|---|
| Unsafe cash issue before config fix | Blocked | Same debit/credit account was rejected |
| Cash direct expense after config fix | Pass | Voucher 22, JE 2926, debit 805, credit 629 |
| Bank direct expense after config fix | Pass | Voucher 23, JE 2927, debit 805, credit 631 |
| Deliberate same-account SourceType=6 | Blocked | Returned JSON failure message |

## Journal Checks
| Voucher | Debit | Credit | Diff | NullAccountLines | SameAccountPairs |
|---:|---:|---:|---:|---:|---:|
| 22 | 77.0000 | 77.0000 | 0.0000 | 0 | 0 |
| 23 | 88.0000 | 88.0000 | 0.0000 | 0 | 0 |

## Result
The critical Phase7 risk is now controlled by both configuration and validation.
