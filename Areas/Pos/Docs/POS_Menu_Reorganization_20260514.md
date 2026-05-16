# POS Menu Reorganization - 2026-05-14

## 2026-05-14 architecture update

Final routing rule: POS menu items must point only to POS routes. Shared screens are exposed through POS wrappers, never by linking to `/MainErp/...` or `ERPUsers`.

Current top-level POS categories:

- الإدخالات
- المبيعات
- المشتريات
- المخزون
- الحسابات
- الموارد البشرية
- الإعدادات العامة
- مدير النظام

Routing decisions:

| POS menu item | Final route | Decision |
|---|---|---|
| شاشة البيع | `/Pos/PosTransaction/Index` | POS-native operational screen |
| العملاء / كروت كيشني | `/Pos/PosTransaction/Index?openKyc=true` | POS-native card/customer workflow |
| متابعة KYC والبنك | `/Pos/KycBankFollowUp/Index` | Customer/card bank follow-up; classified under sales, not system administration |
| إغلاق اليومية | `/Pos/PosClosing/Index` | POS-native operational closing |
| فاتورة مشتريات | `/Pos/PurchaseInvoice/Index` | POS operational purchase entry |
| المخازن | `/Pos/Stores/Index` | POS wrapper over shared store core, branch scoped |
| التحويل المخزني | `/Pos/StockTransfer/Index` | POS operational stock transfer |
| الخزن / سندات القبض | `/Pos/Cashing/Index` | POS wrapper using POS connection |
| العهد / كيشني كارت | `/Pos/Payments/Index` | POS-native funding/custody workflow |
| البنوك والخزن | `/Pos/AccountingReports/Index` | POS operational reports, not MainErp finance admin |
| سندات الصرف | `/Pos/Payments/Vouchers` | POS wrapper using POS connection |
| القيود اليومية | `/Pos/JournalEntries/Index` | POS finance operation with POS permissions |
| الموظفون | `/Pos/EmployeePayroll/Employees` | POS read-only/shared-core wrapper |
| التأمين الطبي | `/Pos/EmployeePayroll/MedicalInsurance` | POS operational/shared-core wrapper |
| تقارير التأمين | `/Pos/EmployeePayroll/MedicalInsuranceReports` | POS report/shared-core wrapper |
| مسير الرواتب | `/Pos/EmployeePayroll/SalaryRun` | POS preview only; save/posting blocked |
| إعدادات الربط | `/Pos/PosLegacyAdmin/BranchesData` | POS admin wrapper over POS DB |
| المستخدمون | `/Pos/PosLegacyAdmin/Users` | POS user administration; no `ERPUsers` redirect |
| الصلاحيات | `/Pos/PosPermissions/Index` | POS permissions only |

Removed cross-context links:

- All POS sidebar links to `~/MainErp/...`.
- POS sidebar link to `~/ERPUsers/Index`.
- POS medical-insurance `data-mainerp-admin-url`.

Database/context decision:

- POS menu routes stay inside `Areas/Pos`.
- POS shared wrappers use `KishnyCashConnection`.
- MainErp admin screens remain in `Areas/MainErp`.
- Dania/demo database is not active unless `PosEmployeePayrollDemoOverrideEnabled` is enabled and `PosEmployeePayrollDatabaseOverride` is explicitly set.

Delivery refinement:

- `متابعة KYC والبنك` was moved from `مدير النظام` to `المبيعات` because it is a customer/card workflow, not a system administration tool.
- Sidebar active/open state was expanded for report/drilldown routes so the correct business module remains open.

## Final menu tree

- لوحة التحكم
- التشغيل
  - شاشة البيع / العمليات
  - العملاء / الكروت
  - إغلاق اليومية
  - كيشني كارت
  - المخازن - تحويل وتشغيل
  - المخازن - عرض/اختيار تشغيلي
  - فاتورة مشتريات
  - استيراد العمليات من Excel
- المالية التشغيلية
  - الخزن / سندات القبض
  - البنوك والخزن
  - سندات الصرف
  - الحركات المالية التشغيلية
  - إشعارات الخصم
- التقارير
  - تقارير نقطة البيع / البيع والكروت
  - التقارير الذكية
  - مصمم التقارير الديناميكي
  - التقارير التشغيلية
  - لوحة المؤشرات المالية
  - أداء الفروع المالي
  - تحليل التدفقات النقدية
  - تحليل المبيعات والتحصيل
  - تحليل المصروفات
  - مؤشرات المخزون والربحية
  - ذمم الموظفين
  - العهد والسلف
  - تنبيهات محاسبية
  - تحليل سبب المشكلة
- الموارد التشغيلية
  - الموظفون - عرض تشغيلي
  - المسير - معاينة فقط
  - التأمين الطبي - عرض تشغيلي
  - تقارير التأمين
  - تارجت المناديب (Admin/full access only)
- الإدارة والإعدادات
  - متابعة KYC والبنك
  - مراقبة النظام
  - سجل أخطاء النظام
  - صلاحيات POS
  - مستخدمو POS
  - الأنشطة والفروع
  - تحديثات قاعدة البيانات
  - إدارة التقارير الديناميكية
  - نماذج الطباعة

## Screens added or made visible

- Banks / البنوك and Boxes / الخزن are exposed operationally through `AccountingReports/Index` and cash/bank reporting shortcuts.
- Boxes / الخزن are exposed through `Cashing/Index` when `FrmCashing` is visible.
- Stores / المخازن are exposed through `Stores/Index`, `StockTransfer/Index`, and inventory profitability reports.
- Employees / الموظفين are exposed as POS operational read-only views through `EmployeePayroll/Employees`.
- Salary Run / المسير is exposed as preview-only through `EmployeePayroll/SalaryRun`; save endpoints remain blocked by `PosOperationalOnly`.
- Medical Insurance / التأمين الطبي is exposed through `EmployeePayroll/MedicalInsurance`.
- Medical Insurance reports remain available through `EmployeePayroll/MedicalInsuranceReports`.

## Screens moved

- POS daily operations were renamed and grouped under `التشغيل`.
- POS accounting links were regrouped under `المالية التشغيلية`.
- POS HR/payroll links were moved under `الموارد التشغيلية`.
- POS medical insurance now opens the operational page, not only the report page.

## Screens hidden

- Full HR administration remains unavailable from POS. Employee saves, activation changes, provider/plan saves, and salary-run saves return 403 through `PosOperationalOnly`.
- Full payroll posting is not exposed in POS.
- Dangerous system/database actions remain admin-only.
- Sales target administration remains admin/full-access only.

## Permission mapping

| Menu item | Route | Permission/visibility |
|---|---|---|
| شاشة البيع / العمليات | `/Pos/PosTransaction/Index` | POS sales shell/session defaults and teller rules |
| العملاء / الكروت | `/Pos/PosTransaction/Index?openKyc=true` | POS shell access; KYC actions still use POS context permissions |
| إغلاق اليومية | `/Pos/PosClosing/Index` | POS shell/teller closing rules |
| كيشني كارت | `/Pos/Payments/Index` | Admin or `CanOpenPayments` |
| المخازن - تحويل وتشغيل | `/Pos/StockTransfer/Index` | Admin or non-teller user with `CanSave` |
| المخازن - عرض/اختيار تشغيلي | `/Pos/Stores/Index` | POS session; branch-limited for non-admin users |
| الخزن / سندات القبض | `/Pos/Cashing/Index` | Admin or `FrmCashing` |
| البنوك والخزن | `/Pos/AccountingReports/Index` | Admin or `CanViewAccountingReports` |
| الحركات المالية التشغيلية | `/Pos/JournalEntries/Index` | Admin or journal entry permissions |
| الموظفون - عرض تشغيلي | `/Pos/EmployeePayroll/Employees` | Admin, `FrmEmployee`, or `FrmEmpSalary5` |
| المسير - معاينة فقط | `/Pos/EmployeePayroll/SalaryRun` | Admin, `FrmEmployee`, or `FrmEmpSalary5`; save blocked |
| التأمين الطبي - عرض تشغيلي | `/Pos/EmployeePayroll/MedicalInsurance` | Admin, `FrmEmployee`, or `FrmEmpSalary5`; administration saves blocked |
| تقارير التأمين | `/Pos/EmployeePayroll/MedicalInsuranceReports` | Admin, `FrmEmployee`, or `FrmEmpSalary5` |
| تارجت المناديب | `/Pos/SalesTargets/Index` | Admin/full access only |
| متابعة KYC والبنك | `/Pos/KycBankFollowUp/Index` | Admin or customer-service full access |
| نظام/SQL/permissions/admin reports | System admin routes | Admin only |
| نماذج الطباعة | `/Pos/PrintTemplate/Index?name=KycCard` | Admin or `CanManagePrintTemplates` |

## Tested routes

Static route/controller validation completed for all POS sidebar targets. `PosDashboardController` shell routes exist for `Sales`, `Closing`, `Kyc`, `Reports`, `AccountingReports`, `FinancialIntelligence`, `JournalEntries`, `Payments`, `Cashing`, `PurchaseInvoice`, `StockTransfer`, `ExcelImport`, `EmployeePayroll`, `SalaryRun`, `MedicalInsurance`, `MedicalInsuranceReports`, `SystemHealth`, `SqlUpdates`, and `PrintTemplates`.

Smoke-tested on local IIS Express `http://localhost:61840`:

- `/Pos/Stores/Index` opens the POS operational stores page for the active POS session with no server error and no browser console errors.
- `/Pos/Stores/Index` redirects anonymous HTTP requests to the POS login page.
- `/Pos/EmployeePayroll/MedicalInsurance` opens the operational medical insurance page with no browser console errors.
- `/Pos/PosDashboard/Index` redirects anonymous HTTP requests to the POS login page.

Runtime expectations:

- Anonymous users are redirected to `/Pos/PosLogin/Index`.
- Unauthorized POS users receive 403 from the dashboard shell and direct controllers.
- Menu visibility now follows the same POS context and legacy permission keys used by route-level checks.

## Unresolved links

- None identified in the rebuilt POS sidebar.

## Screenshots checklist

- POS desktop sidebar expanded.
- POS collapsed sidebar.
- POS mobile/responsive shell.
- Teller user: only safe operational entries are visible.
- Accounting/report user: financial/report links appear without opening 403 pages.
- HR operational user: employee, salary preview, medical insurance, and insurance reports appear.
- Admin user: admin tools and target management appear.

## Files changed

- `Areas/Pos/Views/PosDashboard/_Sidebar.cshtml`
- `Areas/Pos/Views/Stores/Index.cshtml`
- `Areas/Pos/Controllers/PosDashboardController.cs`
- `Areas/Pos/Controllers/StoresController.cs`
- `Common/StoreData/StoreDataRepository.cs`
- `MyERP.csproj`
- `Areas/Pos/Docs/POS_Menu_Reorganization_20260514.md`
