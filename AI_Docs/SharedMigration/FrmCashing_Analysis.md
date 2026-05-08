# FrmCashing Analysis

`FrmCashing1` is the MAIN ERP سند قبض / receipt voucher screen. It writes `Notes.NoteType = 4` and posts accounting rows through `ModAccounts.AddNewDev` into `DOUBLE_ENTREY_VOUCHERS`.

Header fields include note id/serial/date, customer/tenant/owner, contract number, property/building/unit fields, branch, cashbox/bank, payment method, cheque fields, VAT, amount, total, remarks, and report metadata.

Detail grids focus on real-estate contract installment allocation. The code loads `TblContractInstallments`, calculates already-paid values, allocates VAT/rent/commission/insurance/water/electric/service/old balances, and records completed rows in `ContracttBillInstallmentsDone`.

Partial payment logic is present: rows maintain paid and remaining amounts per component, including `VATPayed`, `RentValuePayed`, `CommissionsPayed`, `InsurancePayed`, `WaterPayed`, `ElectricPayed`, `TelandNetPayed`, and `OldValuePayed`.

Invoice/transaction links exist through `Transactions`, `TblMaintenece`, and `Notes` joins for prior debit notes and receipt sums.

Delete/edit/approval behavior is extensive, including Excel-import approval flags and contract resolution helpers. This migration wave intentionally excludes writes, approval, edit, delete, and post behavior.

Printing uses Crystal reports and bank/report metadata. First wave web behavior is read-only list, details, allocations, related notes, and accounting trace.
