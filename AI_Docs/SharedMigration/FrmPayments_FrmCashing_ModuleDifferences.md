# FrmPayments / FrmCashing Module Differences

## Identical business source

Both modules are based on MAIN ERP VB6:

- `Frm\FrmPayments.frm`
- `Frm\New frm\RealEstateMnag\FrmCashing1.frm`

Both read `Notes`, `DOUBLE_ENTREY_VOUCHERS`, `ACCOUNTS`, parties, branches, boxes, banks, and allocation tables. Both are first-wave read-only and expose no save/edit/delete/post behavior.

## MainErp web

- Area: `Areas\MainErp`
- URLs: `/MainErp/Payments`, `/MainErp/Cashing`
- Connection: `MainErp_ConnectionString` (`Eng`)
- Session/layout: MainErp login context and `_MainErpLayout`
- Sidebar: الحسابات / سندات الصرف / سندات القبض

## POS/Kishny web

- Area: `Areas\Pos`
- URLs implemented safely:
  - `/Pos/Payments/Vouchers` for migrated سند صرف reader
  - `/Pos/Cashing` opens the POS shell and loads `/Pos/Cashing/Index` for migrated سند قبض reader
- Existing `/Pos/Payments` shell and `/Pos/Payments/Index` funding/custody save workflow were preserved.
- Connection: `KishnyCashConnection` (`Cash`)
- Session/layout: POS login context and POS iframe pages
- Sidebar: accounting section links added for voucher readers where accounting-report permission exists.

## Excluded until a later wave

- Save/edit/delete/post
- Note numbering
- Crystal report rendering
- Write stored procedures and any save/post simulation that would depend on them
- Full VAT/accounting simulation beyond read-only trace
- Replacing POS funding/custody payment screen

## Follow-up validation on 2026-05-07

- MainErp was tested after a real MainErp login session against `MainErp_ConnectionString` / `Eng`:
  - `/MainErp/Payments`
  - `/MainErp/Cashing`
  - `/MainErp/Payments/Details/222080`
  - `/MainErp/Cashing/Details/221554`
- POS/Kishny was tested after a real POS login session against `KishnyCashConnection` / `Cash`:
  - `/Pos/Payments/Vouchers`
  - `/Pos/Cashing`
  - `/Pos/Payments/Details/1342703`
  - `/Pos/Cashing/Details/642067`
- All tested pages returned HTTP 200 without redirecting back to login.
- Page content scan did not expose the literal `Account_Code`.
- Legacy permissions now check `ScreenJuncUser` screen names:
  - `FrmPayments`
  - `FrmCashing`
- Header party account fallback now uses journal lines when the direct `Notes` account is empty:
  - payment vouchers prefer the debit party line
  - receipt vouchers prefer the credit party line
- Payment/cashing classification display now prefers `NoteCashingType` and maps the VB6 `CboPaymentType` / `DCboCashType` list indexes from MAIN ERP forms.
- Cashing allocations now include `ContracttBillInstallmentsDone` plus a first pass over `TblContractInstallments` joined by `Notes.ContNo`.

## Stored procedure conversion on 2026-05-07

- Read/display paths were moved out of inline repository SQL into stored procedures:
  - `dbo.usp_DynamicErpVoucher_Search`
  - `dbo.usp_DynamicErpVoucher_Header`
  - `dbo.usp_DynamicErpVoucher_Accounting`
  - `dbo.usp_DynamicErpVoucher_RelatedNotes`
  - `dbo.usp_DynamicErpVoucher_Allocations`
- Deployment scripts:
  - `Areas\MainErp\Sql\04_MainErp_PaymentCashing_ReadProcedures.sql`
  - `Areas\Pos\Sql\51_POS_PaymentCashing_ReadProcedures.sql`
- The procedures were applied to:
  - MainErp `Eng`
  - POS/Kishny `Cash`
- The C# repository now calls stored procedures with `CommandType.StoredProcedure`.
- The allocation procedure now has additional read-only sources from the MAIN ERP real-estate receipt form:
  - `TblAqarCommissions`
  - `TblNotesSales`
  - `TblUnitNoInformation`
  - `TblAqrEarnest`
  - `TblOtheExpensAqar`
  - `TblFiterWaiver`
- No save/edit/delete/post procedures were deployed in this slice.
