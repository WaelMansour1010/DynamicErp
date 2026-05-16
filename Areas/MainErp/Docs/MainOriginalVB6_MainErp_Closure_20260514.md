# Main Original VB6 -> MainErp Closure - 2026-05-14

## Scope

Source authority for this pass is Main Original VB6 under `F:\Source Code\SatriahMain`. Kishny/POS was not used as business authority for the prioritized MainErp screens.

Target project: `F:\Source Code\DynamicErp`  
Target area: `Areas/MainErp`  
Runtime database: `Eng`

## Screens Fixed

| Screen | Route | Closure result |
|---|---|---|
| `FrmCustemers` | `/MainErp/Customers` | Opens with enterprise RTL layout, side search/list, account/opening-balance surface, branch context, and no console errors. |
| `FrmBanksData` | `/MainErp/FinancialAdministration?scope=banks` | Removed Kishny wording, fixed editor JavaScript, account lookup opens, summary/list/editor usable. |
| `FrmBoxesData` | `/MainErp/FinancialAdministration?scope=boxes` | Removed POS labels from enterprise UI, fixed editor JavaScript, branch/account/box listing usable. |
| `FrmStoreData` | `/MainErp/StoreData` | MainErp route opens; removed link into `Areas/Pos`; list/editor/delete protection remain inside MainErp. |
| `FrmItems` | `/MainErp/Items` | Opens with searchable item workspace, groups/units grids, safe horizontal grid behavior, and cleaned operational flag wording. |
| `FrmNewGard` / `FrmNewGard1` | `/MainErp/Stocktaking` | Opens with document search, item grid, totals/workflow actions, no broken dropdowns observed. |
| `FrmDefinCompItem` | `/MainErp/DefinCompItem` | Opens with final-product/components workflow, cost panels, responsive layout fix, no console errors. |
| سند قبض | `/MainErp/Cashing` | Previously failed due missing voucher search procedure; applied read procedures to `Eng`; route now opens. |
| سند صرف | `/MainErp/Payments` | Previously failed due missing voucher search procedure; applied read procedures to `Eng`; route now opens. |
| المستخدمين | `/MainErp/Users` | Added MainErp-native controller/view reading `TblUsers` from `Eng`; route opens and searches/list users. |
| الصلاحيات | `/MainErp/Permissions` | Added MainErp-native permission matrix from `ScreenJuncUser` / `TblUserScreen`; menu no longer points to chart of accounts. |
| مستخلصات المشاريع | `/MainErp/ProjectExtracts` | Opens with operational detail: project/customer context, totals, previous/deductions/VAT/retention/net/report/accounting trace. |
| Payroll / Salaries | `/MainErp/EmployeePayroll/SalaryRun` | Opens, salary preview surface remains protected; no production posting enabled. |
| التأمين الطبي | `/MainErp/EmployeePayroll/MedicalInsurance` | Replaced POS partial/assets with MainErp-native page and real provider/plan reads. |
| تقارير التأمين الطبي | `/MainErp/EmployeePayroll/MedicalInsuranceReports` | Replaced POS partial/assets with MainErp-native report surface. |

## UI Issues Fixed

- Reorganized MainErp sidebar into the requested enterprise groups.
- Removed MainErp menu/screen link from `StoreData` into `Areas/Pos`.
- Removed visible Kishny/POS source wording from banks/boxes and medical-insurance MainErp pages.
- Added native MainErp users and permissions screens.
- Added responsive split-grid support used by permissions and medical-insurance screens.
- Fixed financial-administration editor save JavaScript syntax failure.
- Kept payroll posting protected and test-only wording isolated to the salary-run protection panel.

## Runtime / Data Work

- `Web.config` now points MainErp to `Eng` through `MainErp_ConnectionString`.
- Applied `Areas/MainErp/Sql/04_MainErp_PaymentCashing_ReadProcedures.sql` to `Eng`.
- Applied `Areas/MainErp/Sql/06_EmployeePayroll_MedicalInsurance.sql` to `Eng` so medical-insurance JSON endpoints no longer throw missing-table errors.
- Verified `Eng` data counts: customers `2688`, items `40012`, stores `12`, banks `23`, boxes `83`, users `34`, permission rows `16038`.

## Save/Search Validation

- Search/list route checks passed for customers, banks, boxes, stores, items, users, permissions, receipts, payments, project extracts, and HR/payroll pages.
- Existing record open/edit surfaces were exercised by route and editor wiring checks where safe.
- Destructive delete and production posting were not executed.
- Medical-insurance providers/plans currently load as empty operational tables after schema setup; page shows clean empty states.

## Screenshot Checklist

- Runtime screenshot captured: `Areas/MainErp/Docs/runtime-main-original-vb6-mainerp-qa.png`

## Files Changed In This Closure Pass

- `Web.config`
- `MyERP.csproj`
- `Areas/MainErp/Views/Shared/_MainErpSidebar.cshtml`
- `Areas/MainErp/Controllers/UsersController.cs`
- `Areas/MainErp/Controllers/PermissionsController.cs`
- `Areas/MainErp/ViewModels/Security/MainErpUsersViewModels.cs`
- `Areas/MainErp/ViewModels/Security/MainErpPermissionsViewModels.cs`
- `Areas/MainErp/Views/Users/Index.cshtml`
- `Areas/MainErp/Views/Permissions/Index.cshtml`
- `Areas/MainErp/Views/FinancialAdministration/Index.cshtml`
- `Areas/MainErp/Views/StoreData/Index.cshtml`
- `Areas/MainErp/Views/Items/Index.cshtml`
- `Areas/MainErp/Views/EmployeePayroll/MedicalInsurance.cshtml`
- `Areas/MainErp/Views/EmployeePayroll/MedicalInsuranceReports.cshtml`
- `Areas/MainErp/Scripts/financial-administration.js`
- `Areas/MainErp/Content/enterprise-ui.css`
- `Areas/MainErp/Content/mainerp/mainerp.css`
- `Areas/MainErp/Content/defin-comp-item.css`

## Remaining Issues

- Purchases and StockTransfers still contain older "reused from Kishny" wording, but they are outside the requested MainErp menu tree and were not part of the prioritized closure routes.
- Live create/update/delete mutations were limited to safe route/editor readiness checks; no destructive business data changes were performed in `Eng`.
