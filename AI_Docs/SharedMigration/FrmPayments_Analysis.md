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
