# RSMDB Hidden Receipt Allocation - Schema Discovery - 2026-05-20

## Discovery Result
A hidden allocation/payment evidence table was found, but it does not support the current 58 finance-approved receipt candidates.

## Relevant Tables

| Table | Purpose discovered | Rows | Link fields |
|---|---|---:|---|
| `ContracttBillInstallmentsDone` | Actual paid installment evidence for contract receipts | Used by `CashingType=8` receipts | `NoteID`, `istallid`, paid components |
| `ReciveDetails` | Receipt payment method/details | 8,192 | `NoteSerial1`, `NoteSerial`, `CusID`; no contract/installment FK |
| `tblContractInsAllocations` | Contract revenue/accrual allocation header | 227 | `transID`, `NoteID`, `NoteSerial` |
| `tblContractInsAllocations1` | Contract revenue/accrual allocation header variant | 111 | `transID`, `NoteID`, `NoteSerial` |
| `tblContractInsAllocationsDetails` | Allocation detail linked to installment | 4,438 | `transID`, `Installid`, `ContNo`, `CusID` |
| `tblContractInsAllocationsDetails1` | Draft/contract installment allocation details | 8,671 | `ContractFlag`, `Installid`, `CusID` |
| `tblContractInsAllocationsDetails2` | Revenue allocation detail | 29,866 | `transID`, `Installid`, `CusID` |

## Current 58 Candidate Evidence

| Evidence check | Count |
|---|---:|
| Total current finance-approved Type 4 candidates | 58 |
| `CashingType=8` contract receipts | 0 |
| `CashingType=7` from-account receipts | 58 |
| Direct `Notes.ContNo` | 0 |
| Direct `Notes.CusID` | 0 |
| Direct `Notes.akarid` | 0 |
| `ContracttBillInstallmentsDone` rows | 0 |
| Allocation header matches | 0 |
| `ReciveDetails` matches | 49 |
| Text customer matches | 1 |

## Important Interpretation
`ReciveDetails` matches 49 receipts by serial, but it is not a contract allocation table. It stores payment instrument/payment detail information and does not provide a safe `Contract/Installment/Renter` link.

## Global RSMDB Type 4 Pattern

| CashingType | Meaning from VB6 | Total Type 4 receipts | With direct contract | With installment-done evidence |
|---:|---|---:|---:|---:|
| 7 | From Account | 1,530 | 0 | 0 |
| 8 | From Contract | 8,778 | 8,778 | 8,083 |

## Conclusion
The schema contains a reliable operational link for real property receipts, but it applies to `CashingType=8`, not to the current 58 `CashingType=7` candidates.
