# RSMDB CashingType=8 Journal Readiness - 2026-05-20

## Journal Rules
Each candidate was checked against `RSMDB.dbo.DOUBLE_ENTREY_VOUCHERS`.

Required:

- journal lines exist
- debit/credit direction is known
- debit total equals credit total
- every account is finance-approved
- no `AccountId=NULL` after mapping

## Results

| Metric | Count |
|---|---:|
| CashingType=8 candidates with installment evidence | 8,083 |
| Balanced journals | 8,083 |
| Unknown direction inside candidate set | 0 identified by classifier |
| Linked + balanced candidates | 505 |
| Linked + balanced + finance-approved candidates | 0 |

## Key Finding
The accounting structure is technically clean for the candidate set: journals are balanced. The blocking issue is finance-approved account mapping, not journal balance.
