# FrmPayments / FrmCashing Source Map

Primary source: `F:\Source Code\SatriahMain\Account.vbp` in the MAIN ERP project.

## Active forms registered in Account.vbp

- `FrmPayments`: `Frm\FrmPayments.frm`
- `FrmCashing`: `Frm\New frm\RealEstateMnag\FrmCashing1.frm`

Related registered variants found but not used as primary source:

- `Frm\FrmPayments1.frm`
- `Frm\New frm\RealEstateMnag\FrmPayments2.frm`
- `F:\Source Code\SatriahMain\Cayshny\Account.vbp` was not used as primary source.

## Related forms/helpers observed

- Search forms: `Account_search`, `FrmExpensesSearch`, `FrmBuySearch`
- Report/print: Crystal report calls via `OpenReport`, report names resolved from `BanksData.ReportName`, deposit report path under `REPORTS\Deposits`
- Accounting helpers: `Bas\ModAccounts.bas`, especially `AddNewDev`, note/opening-balance helpers, and `DOUBLE_ENTREY_VOUCHERS` creation
- Database/log helpers: `Bas\ModDataBase.bas`, `Bas\registry2.bas`

## Key routines

- `FrmPayments.frm`: `SaveData`, `payGl*` routines, VAT calculation routines, bill allocation delete/load routines, Crystal print routines
- `FrmCashing1.frm`: `SaveData`, contract/installment allocation routines, VAT payment routines, Excel import approval helpers, contract resolving helpers, Crystal print routines

## Main tables touched

- Voucher header: `Notes`
- Accounting trace: `DOUBLE_ENTREY_VOUCHERS`
- Account display: `ACCOUNTS`
- Parties: `TblCustemers`
- Cash/bank: `TblBoxesData`, `BanksData`
- Branch: `TblBranchesData`
- Payment allocations: `TblNotesBillBuyPayment`, `TblBillBuyPayment`, `TblNotesBillProjectPayment`, `TblBillProjectPayment`, `TblNotesBillVindorPayment`, `TblBillVindorPayment`
- Receipt allocations: `TblContractInstallments`, `ContracttBillInstallmentsDone`, `TblContract`, `Transactions`

## Permission signals

VB6 uses screen-level and action-level permission checks around open/save/edit/delete/print. Web first wave maps this to existing module session gates only and implements no writes.
