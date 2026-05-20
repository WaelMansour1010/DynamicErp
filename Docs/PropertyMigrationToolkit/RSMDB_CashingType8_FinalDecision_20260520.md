# RSMDB CashingType=8 Final Decision - 2026-05-20

## Direct Answers

| Question | Answer |
|---|---|
| How many CashingType=8 receipts were found? | 8,778 |
| How many have `ContracttBillInstallmentsDone`? | 8,083 |
| How many have Contract/Installment/Renter links? | 505 |
| How many have finance-approved accounts? | 0 with all accounts approved |
| How many have balanced journal? | 8,083 |
| How many are ReadyForAccountingPilot now? | 0 |
| Ready value now | 0.0000 |
| Is additional finance approval needed? | Yes, 733 CashingType=8-specific accounts need review; priority is accounts that unlock the 505 linked balanced receipts. |
| Is first Accounting Pilot possible now? | Not yet. It becomes possible after finance approval for the linked balanced 505 set. |

## Final Decision
The new candidate build is correct and should replace the rejected `CashingType=7` scope. However, execute remains blocked until finance approves the required CashingType=8 account mappings.

## Next Recommended Phase
Run:
`RSMDB CashingType8 Finance Approval Phase`

Target the accounts that unlock the 505 linked balanced receipts first. After approval, rerun:

1. `CashingType8ReceiptCandidateBuild`
2. Accounting pilot prevalidation
3. Limited accounting pilot execute on clone only
4. Web/accounting validation
5. Rollback
