# FrmCashing Analysis

`FrmCashing1` is the MAIN ERP سند قبض / receipt voucher screen. It writes `Notes.NoteType = 4` and posts accounting rows through `ModAccounts.AddNewDev` into `DOUBLE_ENTREY_VOUCHERS`.

Header fields include note id/serial/date, customer/tenant/owner, contract number, property/building/unit fields, branch, cashbox/bank, payment method, cheque fields, VAT, amount, total, remarks, and report metadata.

Detail grids focus on real-estate contract installment allocation. The code loads `TblContractInstallments`, calculates already-paid values, allocates VAT/rent/commission/insurance/water/electric/service/old balances, and records completed rows in `ContracttBillInstallmentsDone`.

Partial payment logic is present: rows maintain paid and remaining amounts per component, including `VATPayed`, `RentValuePayed`, `CommissionsPayed`, `InsurancePayed`, `WaterPayed`, `ElectricPayed`, `TelandNetPayed`, and `OldValuePayed`.

Invoice/transaction links exist through `Transactions`, `TblMaintenece`, and `Notes` joins for prior debit notes and receipt sums.

Delete/edit/approval behavior is extensive, including Excel-import approval flags and contract resolution helpers. This migration wave intentionally excludes writes, approval, edit, delete, and post behavior.

Printing uses Crystal reports and bank/report metadata. First wave web behavior is read-only list, details, allocations, related notes, and accounting trace.

## Production-readiness pass - 2026-05-09

Completed after revisiting MAIN ERP `FrmCashing1` behavior:

- POS and MainErp list pages now hide balanced accounting status completely. Only unbalanced receipts get a warning badge/column.
- Details pages now hide the normal balanced state and show only an unbalanced warning when needed.
- Voucher lists use server-side paging through `dbo.usp_DynamicErpVoucher_Search` with `@pageNumber` and `@pageSize`.
- Receipt details now display allocation sources using Arabic/business labels instead of raw table names.
- Details include an allocation summary by source with paid/remaining totals.
- The allocation reader includes the earlier `ContracttBillInstallmentsDone` mapping plus additional real-estate related sources already documented in the module-differences file.
- Header readback includes manual number, order/reference, payment descriptions, cheque data, VAT include flag, currency/rate where present, cost center, project, and VB6 report name where the live schema exposes them.

Validation:

- `MyERP.sln` built successfully on 2026-05-09.
- POS real-session checks returned HTTP 200 for `/Pos/Cashing/Index`.
- MainErp real-session checks returned HTTP 200 for `/MainErp/Cashing`.
- HTML scans found no literal `Account_Code`, no scientific-notation voucher serials, and no visible `القيد متوازن` text.
- This line was true for the read-only wave. The 2026-05-09 write/print wave below adds direct save/edit/delete/post through stored procedures, with allocation safety gates.

## Write/print wave - 2026-05-09

Implemented for the production-safe direct receipt path:

- Add/Edit UI for `FrmCashing` in both modules:
  - MainErp: `/MainErp/Cashing/Create`, `/MainErp/Cashing/Edit/{id}`
  - POS: `/Pos/Cashing/Create`, `/Pos/Cashing/Edit/{id}`
- Save is done only through `dbo.usp_DynamicErpVoucher_Save`; MVC does not issue inline write SQL.
- The save procedure writes `Notes` and rebuilds the linked `DOUBLE_ENTREY_VOUCHERS` rows in one transaction.
- The direct receipt accounting rule is:
  - debit: selected cashbox/bank account
  - credit: party account
- Post is done through `dbo.usp_DynamicErpVoucher_Post`.
- Delete is done through `dbo.usp_DynamicErpVoucher_Delete`, but the SP blocks posted vouchers and receipts with legacy real-estate allocation rows.
- Print is implemented as an HTML print view using the read stored procedures:
  - MainErp: `/MainErp/Cashing/Print/{id}`
  - POS: `/Pos/Cashing/Print/{id}`

Safety gates kept in place:

- Receipts linked to `ContracttBillInstallmentsDone` are not rewritten by the generic direct-voucher form. They require the dedicated real-estate allocation workflow to avoid corrupting installment paid/remain fields.
- The full `TblContractInstallments` component allocation behavior remains a dedicated workflow because the VB6 form distributes rent, VAT, insurance, commissions, utilities, service, and old balances separately.

Validation:

- Stored procedures deployed to MainErp `Eng` and POS/Kishny `Cash`.
- Transaction/rollback database tests executed `dbo.usp_DynamicErpVoucher_Save` for receipt vouchers in both databases without leaving test rows.
- Real-session HTTP checks returned 200 for create pages in both modules.
