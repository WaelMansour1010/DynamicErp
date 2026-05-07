# MainErp Kishny UI/Layout Reuse

Date: 2026-05-07

## Purpose

Correct MainErp from a temporary migration landing page into a real ERP application shell that reuses the proven Kishny web experience where it is generic:

- sidebar structure,
- accordion navigation,
- topbar behavior,
- dashboard panels,
- KPI card structure,
- chart containers,
- RTL/responsive shell.

This is UI/framework reuse only. MainErp still uses MainErp routes, MainErp login/session, and MainErp connection boundaries.

## Kishny Sources Reused/Adapted

- `Areas\Pos\Views\PosDashboard\Index.cshtml`
  - Reused dashboard shell pattern: executive panel, period filters, branch filter, KPI grid, insights panel, chart grid.
  - POS-specific operation filters and data endpoints were removed.

- `Areas\Pos\Views\PosDashboard\_Sidebar.cshtml`
  - Reused sidebar structure: brand header, user context block, accordion sections, icon navigation, active link behavior.
  - Rebuilt as `Areas\MainErp\Views\Shared\_MainErpSidebar.cshtml` with MainErp routes only.

- `Areas\Pos\Content\pos-transaction.css`
  - Adapted shell/sidebar/dashboard CSS into `Areas\MainErp\Content\mainerp\mainerp.css`.
  - Kept the shell class contract where useful, but under MainErp-owned CSS.

- `assets\libs\chart-js\Chart.bundle.min.js`
  - Reused for dashboard chart containers.

## POS/Kishny Logic Explicitly Removed

The MainErp UI does not include:

- POS sales invoice route,
- POS transaction save flow,
- card/token flows,
- KYC,
- commissions,
- cashier close,
- POS settlement/closing,
- POS session restore,
- POS dashboard summary endpoint,
- Kishny branding/logos,
- Kishny connection string or POS context.

## MainErp Navigation Structure

- لوحة التحكم
- الحسابات
  - القيود اليومية
  - التقارير المحاسبية
  - حركة حساب
  - معاينة القيود
- المبيعات
  - تقارير المبيعات
  - ملخص المبيعات
- المشتريات
  - فواتير المشتريات
- المخزون
  - التحويلات المخزنية
- المشاريع
  - مستخلصات المشاريع
- الاعتمادات المستندية
  - شاشة الاعتمادات

## Files Created/Changed

Created:

- `Areas\MainErp\Views\Shared\_MainErpSidebar.cshtml`
- `AI_Docs\MainErp\30_MainErp_KishnyUILayoutReuse.md`

Changed:

- `Areas\MainErp\Views\Shared\_MainErpLayout.cshtml`
- `Areas\MainErp\Views\Dashboard\Index.cshtml`
- `Areas\MainErp\Controllers\HomeController.cs`
- `Areas\MainErp\Views\Home\Index.cshtml`
- `Areas\MainErp\Content\mainerp\mainerp.css`
- MainErp controllers now set `ViewBag.ActiveScreen` for sidebar highlighting.
- `MyERP.csproj`

## Safety Confirmation

- No `Areas\Pos` file was modified.
- No `AllScripts.sql` change.
- No database schema change.
- No POS session class is referenced by the MainErp shell.
- No POS/Kishny connection is used by MainErp.

## Remaining Work

Dashboard charts currently use safe local/static placeholder series to preserve the real dashboard framework without calling POS endpoints. The next safe step is wiring each chart to MainErp read-only repositories using `MainErp_ConnectionString`.
