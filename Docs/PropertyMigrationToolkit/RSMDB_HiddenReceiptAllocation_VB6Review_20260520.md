# RSMDB Hidden Receipt Allocation - VB6 Review - 2026-05-20

## Scope
This review focused on VB6 real-estate receipt logic under:
`F:\Source Code\SatriahMain\Frm\New frm\RealEstateMnag`.

No migration, posting, receipt creation, or journal creation was performed.

## Key VB6 Evidence

### Receipt type selector
In `FrmCashing1.frm`, the `DCboCashType` list defines the operational receipt source:

| ListIndex | Arabic label | English label | Meaning |
|---:|---|---|---|
| 7 | من حساب | From Account | General account receipt, not contract-linked by design. |
| 8 | من عقد | From Contract | Property contract receipt. |
| 9 | دفعه حجز | Reservation/earnest payment | Property-related, but not normal contract receipt. |
| 10 | تصفيه | Termination/settlement | Property settlement path. |

Evidence:
- `DCboCashType.AddItem "من حساب"` at index 7.
- `DCboCashType.AddItem "من عقد"` at index 8.
- Excel/property receipt import explicitly sets `DCboCashType.ListIndex = 8`.
- Approval routine rejects non-contract receipts: `If DCboCashType.ListIndex <> 8 Then Reason = "السند ليس سند قبض أملاك"`.

### Contract receipt save path
The property receipt path does not rely on amount/date only. It loads a contract, loads open installments into `Grid3`, then applies payment columns such as:
- `VATPayed`
- `RentValuePayed`
- `CommissionsPayed`
- `InsurancePayed`
- `WaterPayed`
- `ElectricPayed`
- `TelandNetPayed`
- `OldValuePayed`

Key functions:
- `CashingImportOneRow`
- `CashingResolveContract`
- `CashingAllocateInstallmentsByAmount`
- `CashingSelectSingleInstallment`

### Operational installment payment evidence
The strongest operational payment evidence table is:
`ContracttBillInstallmentsDone`

It stores:
- `NoteID`
- `istallid`
- `InstallNo`
- component-level paid amounts
- `RecordDate`

For normal RSMDB property receipts (`NoteType=4`, `CashingType=8`), this table has direct evidence in many rows.

### Allocation tables found
VB6 also uses:
- `tblContractInsAllocations`
- `tblContractInsAllocations1`
- `tblContractInsAllocationsDetails`
- `tblContractInsAllocationsDetails1`
- `tblContractInsAllocationsDetails2`

But the reviewed code shows these are used for contract installment allocation/revenue recognition and edit blocking, not for the current 58 receipt candidates. They did not match those candidates by `NoteID` or `NoteSerial`.

## Conclusion
VB6 confirms that safe property receipt linking requires `CashingType=8` plus direct contract/installment evidence. The 58 finance-approved pilot receipts are all `CashingType=7`, which means they are general "From Account" receipts, not normal contract receipts.
