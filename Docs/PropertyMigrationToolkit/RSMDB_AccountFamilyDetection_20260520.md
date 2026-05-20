# RSMDB Account Family Detection - 2026-05-20

## Families Detected
| Family | Accounts | Usage Count |
|---|---:|---:|
| Unknown | 813 | 4,715 |
| Payables | 444 | 1,623 |
| Receivables | 204 | 1,162 |
| Expense | 78 | 2,186 |
| Banks | 27 | 1,369 |
| Revenue | 15 | 1,602 |
| OwnerPayable | 7 | 10 |
| VAT | 7 | 201 |
| Cash | 2 | 523 |
| RenterReceivable | 2 | 10 |

## Examples
- 1a2a1a2a7a1 - البنك السعودى للاستثمار -> Banks -> suggested target family 110102001.
- 4a1a1a1a1 - ايراد ايجار -> Revenue -> suggested target family 31041001.
- 1a2a1a1a3a1 - النقديه بصندوق المركز الرئيسى -> Cash -> suggested target family 110101001.

## Decision
Family detection is useful for batch review, but not enough for automatic posting due to code-system mismatch.
