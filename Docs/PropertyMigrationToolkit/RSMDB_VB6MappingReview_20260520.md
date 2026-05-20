# RSMDB VB6 Mapping Review - 2026-05-20

## Reviewed Path

`F:\Source Code\SatriahMain\Frm\New frm\RealEstateMnag`

## Confirmed By VB6 Code

### Properties

`TblAqar` is the property/building table. Forms such as `RSTradingCenter.frm`, `FrmAqarReport.frm`, and owner reports select from it and save into it.

### Units

`TblAqarDetai` is the actual unit/details table. It stores `Aqarid`, `unitno`, `unittype`, `RentValue`, `customerid`, and status fields.

`TblUnites` is not confirmed as actual unit master; it appears to be auxiliary/lookup in this context.

### Owners

`TblAqar.ownerid` links to `TblCustemers.CusID`. Owner reports and property screens use this link. `RSOwner.frm` exists as an owner-related form.

### Renters

`TblContract.CusID` links to `TblCustemers.CusID` for tenant/customer data.

### Contracts

`TblContract` is the property contract table. It uses:

- `ContNo`
- `Iqar`
- `UnitNo`
- `CusID`
- `ownerid`
- `StrDate`
- `EndDate`
- `EndContract`

### Installments

`TblContractInstallments` stores installments by `ContNo` with `InstallNo`, `Installdate`, `installValue`, `payed`, and `Remains`.

### Receipts

VB6 reports show receipts through `Notes.NoteType = 4`, with links through `ContracttBillInstallmentsDone.NoteID` and `TblContractInstallments.id`.

### Owner Payables

`TblAqrOwin` appears in owner installment/payment screens and uses `GetOwnerPayment(TblAqrOwin.ID)`.

### Owner GL

`RSTradingCenter.frm` contains `payGlPaymentOwner`, using owner account logic and `DOUBLE_ENTREY_VOUCHERS`.

## Not Fully Confirmed

- Exact semantics of `NoteType=9088`.
- Whether all `NoteType=-1` rows are contract terminations/settlements.
- Exact debit/credit direction in all `DOUBLE_ENTREY_VOUCHERS` groups.
- Whether `SourceTypeId=13` in DynamicErp maps one-to-one with old owner payments.

## Difference From Adnan

RSMDB has actual owner payable rows in `TblAqrOwin` (`4` rows), while Adnan discovery found zero. This makes owner/payable review more important for RSMDB than it was in the Adnan pilot.
