# Discount Notifications Migration - Safe Read-Only Phase

Date: 2026-05-07

## Scope

This phase migrates the legacy VB6 discounts/debit-credit notification screen into both web modules as a safe read-only workspace:

- MainErp route: `/MainErp/DiscountNotifications`
- Kishny/POS route: `/Pos/DiscountNotifications`

No financial write behavior is enabled in this phase.

## Legacy Source

Primary source:

- `F:\Source Code\SatriahMain\Frm\FrmDiscounts.frm`

Reference copies found:

- `F:\Source Code\SatriahMain\Cayshny\Frm\FrmDiscounts.frm`
- `F:\Source Code\SatriahMain\Cayshny\_backup_frm\Frm\FrmDiscounts.frm`

The VB6 screen caption is `الاشعارات`. It is an accounting notification screen that works with discount/debit-credit note types and generated journal voucher rows.

## Legacy Behavior Mapped

The active VB6 screen reads and edits `Notes` rows where `NoteType` is one of:

- `9`
- `10`
- `8034`
- `9082`
- `9083`
- `9089`
- `9090`
- `9099`

The screen also uses:

- `DOUBLE_ENTREY_VOUCHERS`
- `TblCustemers`
- `TblBranchesData`
- `TblNotesBillBuyPayment2`
- `TblBillBuyPayment2`
- `Transactions`

Important VB6 functions/events reviewed:

- `Form_Load`
- `SaveData`
- `WriteDev`
- `Del_Trans`
- `print_report`
- `CboDiscountType_Change`
- `cmdImportExcel_Click`
- `ShowGL_cc`

## Implemented Web Behavior

Implemented as read-only:

- Search/list latest matching notifications.
- Filter by text, branch, date range.
- Open one selected notification in a cockpit-style details workspace.
- Show notification header values from `Notes`.
- Show customer and branch names.
- Show VAT/total/order/e-invoice fields when available.
- Show voucher lines from `DOUBLE_ENTREY_VOUCHERS`.
- Show debit/credit totals and balance difference.
- Display account identity as `Account_Serial - Account_Name`, not raw `Account_Code`.
- Show schema warnings instead of crashing when a legacy table/column is missing.

## Disabled Legacy Behavior

The following VB6 behavior remains disabled and must not be enabled until a later reviewed write/posting phase:

- `SaveData`
- `Del_Trans`
- Note serial allocation
- `Notes` INSERT/UPDATE/DELETE
- `DOUBLE_ENTREY_VOUCHERS` INSERT/UPDATE/DELETE
- FIFO bill payment allocation
- `TblNotesBillBuyPayment2` writes
- `TblBillBuyPayment2` writes
- Excel import and customer upsert
- QR regeneration
- e-invoice/ZATCA send or status update
- Crystal report execution
- Attachments migration

Dangerous buttons in the web UI display:

`هذه الوظيفة لم يتم ترحيلها بعد - Read Only Mode`

## Web Implementation Files

Shared neutral read layer:

- `F:\Source Code\DynamicErp\Common\DiscountNotifications\DiscountNotificationModels.cs`
- `F:\Source Code\DynamicErp\Common\DiscountNotifications\DiscountNotificationReadRepository.cs`

MainErp:

- `F:\Source Code\DynamicErp\Areas\MainErp\Controllers\DiscountNotificationsController.cs`
- `F:\Source Code\DynamicErp\Areas\MainErp\Views\DiscountNotifications\Index.cshtml`
- `F:\Source Code\DynamicErp\Areas\MainErp\Views\Shared\_MainErpSidebar.cshtml`
- `F:\Source Code\DynamicErp\Areas\MainErp\Resources\MainErp.resx`
- `F:\Source Code\DynamicErp\Areas\MainErp\Resources\MainErp.ar.resx`

Kishny/POS:

- `F:\Source Code\DynamicErp\Areas\Pos\Controllers\DiscountNotificationsController.cs`
- `F:\Source Code\DynamicErp\Areas\Pos\Views\DiscountNotifications\Index.cshtml`
- `F:\Source Code\DynamicErp\Areas\Pos\Views\PosDashboard\_Sidebar.cshtml`

Project file:

- `F:\Source Code\DynamicErp\MyERP.csproj`

## Connection Boundaries

MainErp uses:

- `MainErp_ConnectionString`

POS/Kishny uses:

- `KishnyCashConnection`

The shared repository is business-neutral and receives an already-open connection factory from each module. It does not choose a connection string itself.

## Safety Confirmation

- No database schema changes.
- No stored procedures created or modified.
- No `AllScripts.sql` changes.
- No `Areas\Pos\Sql` changes.
- POS changes are limited to the explicitly requested read-only controller/view/sidebar entry.
- Build succeeded.

## Remaining Work

Before enabling write behavior, the following must be mapped and reviewed:

- Exact note type meanings and serial rules.
- `SaveData` write order and transaction boundaries.
- Manual ID allocation strategy for `Notes`.
- `WriteDev` account direction logic for all discount types.
- VAT account selection logic.
- FIFO bill payment allocation rules.
- Delete/rebuild rollback behavior.
- Report templates and parameters.
- E-invoice/QR lifecycle and status protection.
