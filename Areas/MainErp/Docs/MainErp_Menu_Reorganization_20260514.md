# MainErp Menu Reorganization - 2026-05-14

## 2026-05-14 architecture update

Final routing rule: MainErp menu items stay inside MainErp. Shared business screens may share repositories/view models with POS, but the route, shell, context, and permissions remain MainErp-specific.

Current top-level MainErp categories:

- الإدخالات
- المبيعات
- المشتريات
- المخزون
- الحسابات
- الموارد البشرية
- الإعدادات العامة
- مدير النظام

MainErp routing decisions:

| MainErp menu item | Final route | Decision |
|---|---|---|
| الموظفون | `/MainErp/EmployeePayroll/Employees` | Full MainErp HR administration wrapper |
| التأمين الطبي | `/MainErp/EmployeePayroll/MedicalInsurance` | Full MainErp insurance administration wrapper |
| تقارير التأمين | `/MainErp/EmployeePayroll/MedicalInsuranceReports` | MainErp reports |
| مسير الرواتب | `/MainErp/EmployeePayroll/SalaryRun` | MainErp payroll administration; protected posting tools stay MainErp-only |
| مفردات المرتب/السلف/الإجازات/المتغيرات/التوزيعات | `/MainErp/LegacyHrFinance/...` | MainErp legacy HR/finance authority |
| البنوك والخزن | `/MainErp/FinancialAdministration/Index` | Full MainErp finance administration |
| سندات القبض | `/MainErp/Cashing/Index` | MainErp payment/cashing wrapper using MainErp context |
| سندات الصرف | `/MainErp/Payments/Index` | MainErp payment wrapper using MainErp context |
| دليل الحسابات | `/MainErp/AccountCharts/Index` | MainErp-only accounting administration |
| المخازن | `/MainErp/StoreData/Index` | Full MainErp store administration over shared store core |
| الأصناف | `/MainErp/Items/Index` | MainErp inventory master data |
| الاعتمادات المستندية | `/MainErp/LC/Index` | MainErp finance administration |
| مستخلصات المشاريع | `/MainErp/ProjectExtracts/Index` | MainErp project administration |
| المستخدمون | `ERPUsers/Index` from MainErp system shell | MainErp/system user administration only |

Shared-screen boundary:

- MainErp `EmployeePayrollController` uses `MainErpDbConnectionFactory.ResolveActiveConnectionString()`.
- POS `EmployeePayrollController` uses `KishnyCashConnection` unless explicit demo override is enabled.
- MainErp `StoreDataController` injects `MainErpDbConnectionFactory`.
- POS `StoresController` injects `KishnyCashConnection`.
- MainErp sidebar does not need to link into POS for shared modules; shared code sits below the area controller layer.

## Final menu tree

- لوحة التحكم
- الموارد البشرية
  - الموظفون
  - التأمين الطبي
  - المسير
  - مفردات المرتب
  - السلف
  - استحقاقات الإجازات
  - الإجازات المرضية
  - المتغيرات
  - توزيعات الموظفين
- الإدارة المالية
  - البنوك والخزن
  - دليل الحسابات
  - الفروع وربط الحسابات
  - القيود اليومية
  - سندات الصرف
  - سندات القبض
  - إشعارات الخصم
  - المراجعة المالية
  - حركة حساب
  - الاعتمادات المستندية
  - الفواتير المالية
  - معاينة سند
- المخازن
  - المخازن
  - التحويلات المخزنية
  - الأصناف
  - الجرد
  - سند التجميع
- المشاريع
  - المشاريع
  - مستخلصات المشاريع
- المبيعات والمشتريات
  - العملاء والموردون
  - فاتورة الورشة
  - فاتورة المضخات
  - المشتريات
- التقارير والمتابعة
  - تقارير التأمين الطبي
  - تقارير المبيعات
  - ملخص المبيعات
  - التقارير الديناميكية
  - إدارة التقارير الديناميكية (Admin only)
- إدارة النظام (Admin only)
  - إعدادات النظام
  - تحديثات قاعدة البيانات
  - استيراد ملفات Excel

## Screens added or made visible

- Banks / البنوك and Boxes / الخزن through `FinancialAdministration/Index`.
- Employees / الموظفين through `EmployeePayroll/Employees`.
- Salary Run / المسير through `EmployeePayroll/SalaryRun`.
- Medical Insurance / التأمين الطبي through `EmployeePayroll/MedicalInsurance`.
- MOFRAD / مفردات المرتب through `LegacyHrFinance/Components`.
- Employee Advances / السلف through `LegacyHrFinance/Advances`.
- Vacation Entitlements / استحقاقات الإجازات through `LegacyHrFinance/LeaveEntitlements`.
- Sick Leave / الإجازات المرضية through `LegacyHrFinance/SickLeaves`.
- Changed Components / المتغيرات through `LegacyHrFinance/CompensationAdjustments`.
- Employee Allocations / توزيعات الموظفين through `LegacyHrFinance/EmployeeAllocations`.
- Stores / المخازن through `StoreData/Index`, with related operational screens for stock transfers, stocktaking, items, and assembly vouchers.
- Project Extracts / مستخلصات المشاريع through `ProjectExtracts/Index`.
- Letters of Credit / الاعتمادات المستندية through `LC/Index`.

## Screens moved

- HR and payroll screens were moved out of the financial/accounting group into `الموارد البشرية`.
- Letters of Credit moved into `الإدارة المالية`.
- Medical insurance reports moved into `التقارير والمتابعة`.
- Inventory screens were consolidated under `المخازن`.

## Screens hidden

- No migrated screen was intentionally removed from MainErp.
- Items are hidden only when the current MainErp user lacks the same legacy permission used by the target controller.
- Admin-only system tools remain hidden for non-admin users.

## Permission mapping

| Menu item | Route | Permission/visibility |
|---|---|---|
| الموظفون | `/MainErp/EmployeePayroll/Employees` | `FrmEmployee` or `FrmEmpSalary5` |
| التأمين الطبي | `/MainErp/EmployeePayroll/MedicalInsurance` | `FrmEmployee` or `FrmEmpSalary5` |
| المسير | `/MainErp/EmployeePayroll/SalaryRun` | `FrmEmployee` or `FrmEmpSalary5` |
| مفردات المرتب | `/MainErp/LegacyHrFinance/Components` | `MOFRAD` |
| السلف | `/MainErp/LegacyHrFinance/Advances` | `FrmEmpsAdvanceRequest` |
| استحقاقات الإجازات | `/MainErp/LegacyHrFinance/LeaveEntitlements` | `FrmVocationEntitlements` |
| الإجازات المرضية | `/MainErp/LegacyHrFinance/SickLeaves` | `FrmRegsterSickleave` |
| المتغيرات | `/MainErp/LegacyHrFinance/CompensationAdjustments` | `FrmChangedComponentData` |
| توزيعات الموظفين | `/MainErp/LegacyHrFinance/EmployeeAllocations` | `FrmChangedComponentData1` |
| البنوك والخزن | `/MainErp/FinancialAdministration/Index` | `FrmBanksData` or `FrmBoxesData` |
| سندات الصرف | `/MainErp/Payments/Index` | `FrmPayments` |
| سندات القبض | `/MainErp/Cashing/Index` | `FrmCashing` |
| دليل الحسابات | `/MainErp/AccountCharts/Index` | `FrmAccountCharts` |
| الفروع وربط الحسابات | `/MainErp/Branches/Index` | `baranches` |
| الأصناف | `/MainErp/Items/Index` | `FrmItems` |
| المخازن | `/MainErp/StoreData/Index` | `FrmStoreData` |
| الجرد | `/MainErp/Stocktaking/Index` | `FrmNewGard` or `FrmNewGard1` |
| سند التجميع | `/MainErp/DefinCompItem/Index` | `FrmDefinCompItem` |
| مستخلصات المشاريع | `/MainErp/ProjectExtracts/Index` | `projectsbill`, `FrmProjectsBill`, `FrmProjectBill`, or `ProjectExtracts` |
| الاعتمادات المستندية | `/MainErp/LC/Index` | `FrmLC` |
| إدارة النظام | `/MainErp/Options`, `/MainErp/DatabaseMigration`, `/MainErp/MasterDataImport` | MainErp admin only |

## Tested routes

Static route/controller validation completed for the menu targets listed above. The targets exist in `Areas/MainErp/Controllers` and are linked through `Url.Action` where MVC routes are available.

Smoke-tested on local IIS Express `http://localhost:61840`:

- `/MainErp/StoreData/Index` redirects anonymous users to the MainErp login page with no browser console errors.

Runtime expectations:

- Anonymous users are redirected by `MainErpControllerBase` to `/MainErp/Login/Index`.
- Unauthorized users do not see permission-gated menu entries.
- Admin users bypass legacy permission checks through `LegacyScreenPermissionService`.

## Unresolved links

- None identified in the rebuilt MainErp sidebar.

## Screenshots checklist

- MainErp desktop sidebar expanded.
- MainErp collapsed sidebar.
- MainErp mobile/responsive menu.
- HR group with all migrated HR screens visible for an authorized user.
- Financial group with banks/boxes and LC visible for an authorized user.
- Inventory group with stores-related screens visible.
- Unauthorized user check: permission-gated items are hidden.

## Files changed

- `Areas/MainErp/Views/Shared/_MainErpSidebar.cshtml`
- `Areas/MainErp/Controllers/StoreDataController.cs`
- `Common/StoreData/StoreDataRepository.cs`
- `MyERP.csproj`
- `Areas/MainErp/Docs/MainErp_Menu_Reorganization_20260514.md`
