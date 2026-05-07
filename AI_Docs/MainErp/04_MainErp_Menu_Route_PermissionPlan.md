# MainErp Menu, Route, And Permission Plan

## Routes

- `/MainErp`
- `/MainErp/Dashboard`
- `/MainErp/Purchases`
- `/MainErp/StockTransfers`
- `/MainErp/JournalEntries`
- `/MainErp/AccountingReports`
- `/MainErp/SalesReports`
- `/MainErp/WorkshopSales`
- `/MainErp/PumpSales`
- `/MainErp/LC`
- `/MainErp/ProjectExtracts`
- `/MainErp/Accounting/PreviewTest`

All routes are registered by `Areas\MainErp\MainErpAreaRegistration.cs`.

## Current Authorization

The current foundation uses `[Authorize]` on `MainErpControllerBase`. This keeps MainErp pages available only to authenticated users while avoiding changes to the existing global permission store.

## Menu Plan

MainErp has an internal isolated navigation in `_MainErpLayout.cshtml`. Global menu changes remain deferred until MainErp permissions are added to the shared permission store.

Allowed MainErp entries:

- الداشبورد
- المشتريات
- التحويلات المخزنية
- القيود اليومية
- التقارير المحاسبية
- تقارير المبيعات
- الاعتمادات المستندية
- مستخلصات المشاريع

Under-migration entries must hide write/post actions until each module is migrated and reviewed.

Do not include POS invoices, cards, KYC, commissions, Kishny reports, Kishny branding, cashier closing, or POS deployment assumptions.

Sales migration entries added from the active Main ERP `FrmSaleBill6.frm` source:

- فاتورة مبيعات الورشة
- فاتورة مبيعات المضخات

These are Main ERP invoice routes only. They do not reuse Kishny POS invoice logic.

## Proposed MainErp Permissions

- `MainErp.Dashboard.View`
- `MainErp.Purchases.View`
- `MainErp.Purchases.Create`
- `MainErp.StockTransfers.View`
- `MainErp.StockTransfers.Create`
- `MainErp.JournalEntries.View`
- `MainErp.JournalEntries.Create`
- `MainErp.AccountingReports.View`
- `MainErp.SalesReports.View`
- `MainErp.WorkshopSales.View`
- `MainErp.WorkshopSales.Create` later; disabled until save/post/inventory flow is mapped and approved.
- `MainErp.PumpSales.View`
- `MainErp.PumpSales.Create` later; disabled until pump-specific save/post/inventory flow is mapped and approved.
- `MainErp.LC.View`
- `MainErp.ProjectExtracts.View`

Do not reuse POS permission names. POS permissions such as `CanSave`, POS report permissions, teller flags, cashier flags, and POS full-access flags remain isolated to `Areas\Pos`.

## Optional Flags

Future reviewed settings:

- `EnableMainErpMigration`
- `EnableKishnyPos`

These flags are secondary controls only. Physical area isolation remains the primary separation mechanism.
