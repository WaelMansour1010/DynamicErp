# RSMDB CashingType=8 Approval Impact Simulation - 2026-05-20

## Scope

Simulation only. No approval rows were applied and no migration/posting occurred.

## Simulation Results

| Scenario | Accounts With Safe Suggested Target | Ready Receipts | Ready Journals | Ready Journal Lines | Ready Value | Accounts Still Review | Blocked Receipts |
|---|---:|---:|---:|---:|---:|---:|---:|
| Top 25 by impact | 5 | 4 | 4 | 8 | 49,168.2500 | 728 | 501 |
| Top 50 by impact | 5 | 4 | 4 | 8 | 49,168.2500 | 728 | 501 |
| Top 100 by impact | 6 | 5 | 5 | 10 | 73,168.2500 | 727 | 500 |
| All accounts with score >= 60 | 97 | 32 | 32 | 64 | 966,568.2500 | 636 | 473 |

## Interpretation

Top 25 and Top 50 have low operational impact because most high-frequency accounts are tenant/customer-like accounts without a suggested target account yet. The most useful immediate finance path is not simply approving Top 50; it is mapping the missing tenant/customer account family first.

## Recommendation

- Do not execute an Accounting Pilot yet.
- Review and map the 636 NeedsMoreInfo accounts, starting with the top tenant/customer accounts.
- If finance approves all score >= 60 mappings, the first small pilot can include 32 receipts / 32 journals / 64 lines with value 966,568.2500.
