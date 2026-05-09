# FrmPayments Analysis

`FrmPayments` is the MAIN ERP سند صرف / payment voucher screen. It writes `Notes.NoteType = 5` and posts journal rows through `ModAccounts.AddNewDev` into `DOUBLE_ENTREY_VOUCHERS`.

Header fields include note id/serial/date, manual number, branch, party/customer/vendor/account, cashbox/bank, payment type, cheque fields, remarks, VAT, total, currency/rate, and report name. Detail grids allocate payment against purchase/vendor/project bills and other payment scenarios.

Business links include `TblCustemers`, direct account selection, suppliers/vendors/contractors, project/cost-center fields, boxes via `TblBoxesData`, and banks via `BanksData`.

VAT exists through `TxtVATValue`, `TxtVAt2`, `txtTotalWithVat`, `IncludVAT`, and `GetValueAddedAccount`/percentage helpers. Save/post routines may include VAT journal lines depending on payment type and system options.

Delete/edit behavior removes allocation rows such as `TblNotesBillBuyPayment`, `TblBillBuyPayment`, `TblNotesBillProjectPayment`, and `TblBillProjectPayment` before rebuilding. This migration wave does not implement delete/edit/save/post.

Printing uses Crystal reports and bank-specific `BanksData.ReportName` where available.

First wave web behavior: list, details, allocation readback, related notes, and `DOUBLE_ENTREY_VOUCHERS` trace only.

## VB6 comparison update - read-only wave

After comparing the MAIN ERP `F:\Source Code\SatriahMain\Frm\FrmPayments.frm` with the web screens, these VB6 header/detail fields were added to the read-only details surface in both POS and MainErp:

- Manual/reference data: `ManualNo`, `ORDER_NO`.
- Payment descriptions: `PayDes`, `PayDes1`, plus `Remark`.
- Cheque/bank data: `TxtChequeNumber1`, `DtpChequeDueDate1`, bank and box names.
- VAT/currency/project context: `IncludVAT`, `VAT`, `TotalValue`, `Currency_rate` where available, `general_cost_center`, `ProjectID`/`project_id`.
- Existing allocation grids represented from the VB6 grids:
  - `VSFlexGrid1` / purchase bill allocation: `TblNotesBillBuyPayment`.
  - `VSFlexGrid2` / project bill allocation: `TblNotesBillProjectPayment`.
  - `Grid1` / vendor bill allocation: `TblNotesBillVindorPayment`.

Still pending before save/post:

- Reconstruct the full VB6 edit-mode behavior for selecting bills and distributing `TransPayedValue`.
- Map all `DCboCashType` branches to explicit Arabic labels and expected visible sections.
- Replace report placeholders with the exact Crystal/report route selected by `ReportName` and bank report settings.
- Implement save/edit/delete/post only after stored procedures are approved.

## Production-readiness pass - 2026-05-09

Completed after the VB6 comparison:

- POS and MainErp list pages now hide balanced accounting status completely. Only unbalanced vouchers get a warning badge/column.
- Details pages now hide the normal balanced state and keep only an unbalanced warning when needed.
- Voucher lists use server-side paging through `dbo.usp_DynamicErpVoucher_Search` with `@pageNumber` and `@pageSize`.
- Details now display allocation sources using Arabic/business labels instead of raw table names such as `TblNotesBillBuyPayment`.
- Details include an allocation summary by source with paid/remaining totals.
- Header readback includes manual number, order/reference, payment descriptions, cheque data, VAT include flag, currency/rate, cost center, project, and VB6 report name where the live schema exposes them.
- POS UI was restyled with a scoped voucher workspace partial; MainErp uses the MainErp ERP styling.

Validation:

- `MyERP.sln` built successfully on 2026-05-09.
- POS real-session checks returned HTTP 200 for `/Pos/Payments/Vouchers`, `/Pos/Payments/Vouchers?page=2&pageSize=10`, `/Pos/Cashing/Index`, and a payment details page.
- MainErp real-session checks returned HTTP 200 for `/MainErp/Payments`, `/MainErp/Payments?page=2&pageSize=10`, `/MainErp/Cashing`, and a payment details page.
- HTML scans found no literal `Account_Code`, no scientific-notation voucher serials, no raw allocation table names, and no visible `القيد متوازن` text.
- No new repository/controller read path contains `INSERT`, `UPDATE`, `DELETE`, or `ExecuteNonQuery`.

## Write/print wave - 2026-05-09

Implemented for the production-safe direct voucher path:

- Add/Edit UI for `FrmPayments` in both modules:
  - MainErp: `/MainErp/Payments/Create`, `/MainErp/Payments/Edit/{id}`
  - POS: `/Pos/Payments/CreateVoucher`, `/Pos/Payments/EditVoucher/{id}`
- Save is done only through `dbo.usp_DynamicErpVoucher_Save`; MVC does not issue inline write SQL.
- The save procedure writes `Notes` and rebuilds the linked `DOUBLE_ENTREY_VOUCHERS` rows in one transaction.
- The direct payment-voucher accounting rule is:
  - debit: party account
  - credit: selected cashbox/bank account
- Post is done through `dbo.usp_DynamicErpVoucher_Post`.
- Delete is done through `dbo.usp_DynamicErpVoucher_Delete`, but the SP blocks posted vouchers and vouchers with legacy allocation rows.
- Print is implemented as an HTML print view using the read stored procedures:
  - MainErp: `/MainErp/Payments/Print/{id}`
  - POS: `/Pos/Payments/PrintVoucher/{id}`

Safety gates kept in place:

- Allocated payment vouchers from VB6 grids are not rewritten by the generic direct-voucher form. The SP raises an error if legacy allocation rows exist.
- Full allocation rebuild for `TblNotesBillBuyPayment`, `TblNotesBillProjectPayment`, and `TblNotesBillVindorPayment` remains a dedicated allocation workflow, not a generic direct save.
- VAT is persisted on `Notes`; the direct accounting entry posts the total value as a balanced two-line entry until the exact VAT-account branch is implemented for each VB6 cashing type.

Validation:

- Stored procedures deployed to MainErp `Eng` and POS/Kishny `Cash`.
- Transaction/rollback database tests executed `dbo.usp_DynamicErpVoucher_Save` for payment vouchers in both databases without leaving test rows.
- Real-session HTTP checks returned 200 for create, edit, and print pages in both modules.
