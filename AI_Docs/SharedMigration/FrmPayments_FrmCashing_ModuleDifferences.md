# FrmPayments / FrmCashing Module Differences

## Identical business source

Both modules are based on MAIN ERP VB6:

- `Frm\FrmPayments.frm`
- `Frm\New frm\RealEstateMnag\FrmCashing1.frm`

Both read `Notes`, `DOUBLE_ENTREY_VOUCHERS`, `ACCOUNTS`, parties, branches, boxes, banks, and allocation tables. The first wave was read-only; the 2026-05-09 wave adds direct save/edit/delete/post through stored procedures.

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

## Originally excluded from the first read-only wave

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
- No save/edit/delete/post procedures were deployed in that slice. They were added later in the 2026-05-09 write/print wave below.

## UI and paging hardening on 2026-05-09

- POS voucher screens were restyled with a scoped premium ERP workspace partial:
  - `Areas\Pos\Views\Shared\_VoucherScreenStyles.cshtml`
- MainErp voucher screens continue to use:
  - `Areas\MainErp\Content\mainerp\mainerp.css`
- Balanced accounting state is no longer shown to users. Only unbalanced vouchers display a warning.
- Allocation source names are displayed as Arabic/business labels in both modules; raw SQL table names remain an internal trace concern only.
- Search/list screens now page through the stored procedure instead of loading a fixed first batch only.
- Both modules keep separate controllers, repositories, views, sessions, layouts, and connection strings.
- Validation on 2026-05-09:
  - Build: `MyERP.sln` succeeded.
  - POS real-session pages returned HTTP 200 for list, paging, cashing list, and payment details.
  - MainErp real-session pages returned HTTP 200 for list, paging, cashing list, and payment details.
  - Content scans found no literal `Account_Code`, no scientific notation serials, no raw allocation table names, and no visible balanced-state label.

## Remaining gated work

- Generic direct save/edit/delete/post is now implemented through stored procedures in both modules.
- Crystal/DevExpress binary report rendering is still not used directly. HTML print views are implemented and expose the VB6 report name/bank report metadata as the reporting anchor.
- Full edit-mode allocation rebuilding from the VB6 grids remains gated behind dedicated allocation workflows because it touches note allocation and reversal behavior.

## Write/print implementation on 2026-05-09

New stored procedures, deployed to both `Eng` and `Cash`:

- `dbo.usp_DynamicErpVoucher_EditLookups`
- `dbo.usp_DynamicErpVoucher_EditHeader`
- `dbo.usp_DynamicErpVoucher_Save`
- `dbo.usp_DynamicErpVoucher_Post`
- `dbo.usp_DynamicErpVoucher_Delete`

Implemented routes:

- MainErp payment: `/MainErp/Payments/Create`, `/MainErp/Payments/Edit/{id}`, `/MainErp/Payments/Print/{id}`
- MainErp receipt: `/MainErp/Cashing/Create`, `/MainErp/Cashing/Edit/{id}`, `/MainErp/Cashing/Print/{id}`
- POS payment: `/Pos/Payments/CreateVoucher`, `/Pos/Payments/EditVoucher/{id}`, `/Pos/Payments/PrintVoucher/{id}`
- POS receipt: `/Pos/Cashing/Create`, `/Pos/Cashing/Edit/{id}`, `/Pos/Cashing/Print/{id}`

Module separation remains:

- MainErp uses `MainErp_ConnectionString`, MainErp session, MainErp layout, and MainErp legacy permissions.
- POS uses `KishnyCashConnection`, POS session, POS layout, and POS legacy permissions.
- Controllers and repositories are still separate at the area boundary; only neutral view models/read-write repository mechanics are reused where the connection factory is injected.

Current write coverage:

- Direct payment/receipt voucher save with two-line balanced journal entry.
- Edit of unposted, unallocated direct vouchers.
- Delete of unposted, unallocated direct vouchers.
- Post flag update for voucher and journal rows.
- HTML print from stored-procedure read data.

Protected exclusions:

- Allocated purchase/project/vendor payment vouchers are blocked from the generic edit/delete SP.
- Real-estate allocated receipts are blocked from the generic edit/delete SP.
- Exact Crystal report execution remains pending; the web print view is production usable but not a Crystal renderer.

## End-to-end validation on 2026-05-09

Full UI flows were tested after real login sessions:

- MainErp payments: create, details, edit, print, delete.
- MainErp cashing: create, details, edit, print, delete.
- POS payments: create, details, edit, print, delete.
- POS cashing: create, details, edit, print, delete.

Posting was tested separately for all four screens:

- MainErp payments post.
- MainErp cashing post.
- POS payments post.
- POS cashing post.

Database validation after posting:

- `Notes.NotePosted = 1`
- `DOUBLE_ENTREY_VOUCHERS.Posted = 1`
- two journal lines per direct voucher
- debit/credit difference = `0`

Cleanup:

- All `AUTO-*` and `POST-*` Codex test vouchers were removed after validation.
- Verification query returned zero Codex test rows in both `Eng` and `Cash`.
- `MyERP.sln` built successfully after the end-to-end tests.
