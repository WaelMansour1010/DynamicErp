# RSMDB Receipt Allocation Final Decision - 2026-05-20

## Direct Answers

| Question | Answer |
|---|---|
| Did we find an allocation table? | Yes: `ContracttBillInstallmentsDone` is the strongest receipt-to-installment evidence table for property receipts. Also `tblContractInsAllocations*` exist, but they are revenue/accrual allocation tables and did not match the 58 candidates. |
| Did we find a direct relation for the 58? | No. The 58 have no `ContNo`, no `CusID`, no installment rows, and no allocation header match. |
| Did VB6 confirm the link method? | Yes. VB6 confirms property contract receipts are `CashingType=8` (`From Contract`) and are applied through contract/installment grid logic. The 58 are `CashingType=7` (`From Account`). |
| AutoApproved after discovery | 0 |
| HighConfidence after discovery | 0 |
| MediumReview after discovery | 4 |
| Blocked/Weak not allowed for pilot | 54 total: 21 weak + 33 blocked |
| Can the first accounting pilot run on the current 58? | No. |

## Final Decision
Do not run Accounting Migration Pilot using the current 58 finance-approved Type 4 candidates.

They are financially approved at account level, but operationally they are not safe contract receipts.

## Required Next Step
Rebuild the first accounting pilot candidate set from operationally linked property receipts:

1. `Notes.NoteType = 4`.
2. `Notes.CashingType = 8`.
3. `Notes.ContNo IS NOT NULL`.
4. `ContracttBillInstallmentsDone.NoteID = Notes.NoteID` exists.
5. Source contract/installment/renter maps exist in `PropertyMigrationEntityMap`.
6. Account mapping is Finance Approved.
7. Journal is balanced and has no unknown accounts.

If finance approvals do not cover enough `CashingType=8` journals, create a new Finance Review Pack specifically for `CashingType=8` property receipts.
