# POS/MainErp Final Menu Delivery - 2026-05-14

## Product decision

The menu is organized as an ERP product menu, not as a technical migration list. The approved top-level modules are the only top-level modules in both POS and MainErp:

1. الإدخالات
2. المبيعات
3. المشتريات
4. المخزون
5. الحسابات
6. الموارد البشرية
7. الإعدادات العامة
8. مدير النظام

POS/Kishny stays operational. MainErp stays the business administration authority. Shared modules use shared services/repositories with separate area routes, context, permissions, and layout.

## Complete screen inventory and classification

| Area | Arabic screen | English/internal name | Route | Controller/action | Business purpose | Final category | POS exposure decision |
|---|---|---|---|---|---|---|---|
| POS | شاشة البيع | POS Transaction | `/Pos/PosTransaction/Index` | `PosTransaction/Index` | Teller sales, cards, invoices | المبيعات | POS-native |
| POS | العملاء / كروت كيشني | KYC/Card customers | `/Pos/PosTransaction/Index?openKyc=true` | `PosTransaction/Index` | Customer/card operational workflow | المبيعات | POS-native |
| POS | متابعة KYC والبنك | KYC Bank Follow-up | `/Pos/KycBankFollowUp/Index` | `KycBankFollowUp/Index` | Customer/card bank follow-up | المبيعات | POS-native; moved from system to sales |
| POS | تقارير المبيعات | POS Reports | `/Pos/PosReports/Index` | `PosReports/Index` | Operational sales reports | المبيعات | POS-native |
| POS | التقارير الذكية | HTML Reports | `/Pos/HtmlReports/Index` | `HtmlReports/Index` | Smart tabular reports | المبيعات | POS-native reporting |
| POS | أداء المناديب والمبيعات | Sales Representatives Performance | `/Pos/SalesRepresentativesPerformance/Index` | `SalesRepresentativesPerformance/Index` | Sales force performance | المبيعات | POS reporting |
| POS | تارجت المناديب | Sales Targets | `/Pos/SalesTargets/Index` | `SalesTargets/Index` | Sales target administration | المبيعات | POS admin/full-access |
| POS | تحليل المبيعات والتحصيل | Sales Collections | `/Pos/FinancialIntelligence/SalesCollections` | `FinancialIntelligence/SalesCollections` | Sales and collections analysis | المبيعات | POS reporting |
| POS | إغلاق اليومية | Daily Closing | `/Pos/PosClosing/Index` | `PosClosing/Index` | Teller/day closing | الإدخالات | POS-native |
| POS | استيراد العمليات من Excel | Excel Import | `/Pos/ExcelImport/Index` | `ExcelImport/Index` | Bulk operational import | الإدخالات | POS admin/operational |
| POS | فاتورة مشتريات | Purchase Invoice | `/Pos/PurchaseInvoice/Index` | `PurchaseInvoice/Index` | Operational purchase invoice | المشتريات | POS-native operational |
| POS | المخازن | Stores | `/Pos/Stores/Index` | `Stores/Index` | Branch-scoped warehouse visibility | المخزون | POS wrapper over shared store core |
| POS | التحويل المخزني | Stock Transfer | `/Pos/StockTransfer/Index` | `StockTransfer/Index` | Branch/store stock movement | المخزون | POS-native operational |
| POS | مؤشرات المخزون والربحية | Inventory Profitability | `/Pos/FinancialIntelligence/InventoryProfitability` | `FinancialIntelligence/InventoryProfitability` | Inventory profitability report | المخزون | POS reporting |
| POS | الخزن / سندات القبض | Cashboxes / Receipts | `/Pos/Cashing/Index` | `Cashing/Index` | Receipt vouchers and cashbox activity | الحسابات | POS wrapper with POS DB |
| POS | العهد / كيشني كارت | Custody / Kishny Card Funding | `/Pos/Payments/Index` | `Payments/Index` | Funding/custody operations | الحسابات | POS-native |
| POS | البنوك والخزن | Banks and Cashboxes Reports | `/Pos/AccountingReports/Index` | `AccountingReports/Index` | Bank/cashbox operational reporting | الحسابات | POS report, not MainErp admin |
| POS | سندات الصرف | Payment Vouchers | `/Pos/Payments/Vouchers` | `Payments/Vouchers` | Payment voucher list | الحسابات | POS wrapper with POS DB |
| POS | القيود اليومية | Journal Entries | `/Pos/JournalEntries/Index` | `JournalEntries/Index` | Manual/operational journal entries | الحسابات | POS finance permissions |
| POS | إشعارات الخصم | Discount Notifications | `/Pos/DiscountNotifications/Index` | `DiscountNotifications/Index` | Discount/debit notices | الحسابات | POS finance |
| POS | التقارير المالية | Financial Intelligence | `/Pos/FinancialIntelligence/Index` | `FinancialIntelligence/Index` | Finance dashboard | الحسابات | POS reporting |
| POS | مصمم التقارير الديناميكي | Dynamic Reports | `/Pos/DynamicReports/Index` | `DynamicReports/Index` | Scoped reporting | الحسابات | POS scoped |
| POS | أداء الفروع المالي | Branch Performance | `/Pos/FinancialIntelligence/BranchPerformance` | `FinancialIntelligence/BranchPerformance` | Branch financial performance | الحسابات | POS reporting |
| POS | تحليل التدفقات النقدية | Cash Flow | `/Pos/FinancialIntelligence/CashFlow` | `FinancialIntelligence/CashFlow` | Cash flow report | الحسابات | POS reporting |
| POS | تحليل المصروفات | Expenses | `/Pos/FinancialIntelligence/Expenses` | `FinancialIntelligence/Expenses` | Expense analysis | الحسابات | POS reporting |
| POS | العهد | Custody report | `/Pos/FinancialIntelligence/Custody` | `FinancialIntelligence/Custody` | Custody/advances analysis | الحسابات | POS reporting |
| POS | تنبيهات محاسبية | Abnormal Journals | `/Pos/FinancialIntelligence/AbnormalJournals` | `FinancialIntelligence/AbnormalJournals` | Accounting exception review | الحسابات | POS reporting |
| POS | تحليل سبب المشكلة | Root Cause | `/Pos/FinancialIntelligence/RootCause` | `FinancialIntelligence/RootCause` | Drilldown/root-cause analysis | الحسابات | POS reporting |
| POS | الموظفون | Employees | `/Pos/EmployeePayroll/Employees` | `EmployeePayroll/Employees` | Employee visibility | الموارد البشرية | Shared core; POS read-only |
| POS | التأمين الطبي | Medical Insurance | `/Pos/EmployeePayroll/MedicalInsurance` | `EmployeePayroll/MedicalInsurance` | Operational insurance visibility | الموارد البشرية | Shared core; POS read-only/admin saves blocked |
| POS | تقارير التأمين | Medical Insurance Reports | `/Pos/EmployeePayroll/MedicalInsuranceReports` | `EmployeePayroll/MedicalInsuranceReports` | Insurance reports | الموارد البشرية | Shared core; POS reports |
| POS | مسير الرواتب | Salary Run Preview | `/Pos/EmployeePayroll/SalaryRun` | `EmployeePayroll/SalaryRun` | Payroll preview | الموارد البشرية | Preview only; save/posting blocked |
| POS | ذمم الموظفين | Employee Receivables | `/Pos/FinancialIntelligence/EmployeeReceivables` | `FinancialIntelligence/EmployeeReceivables` | Employee receivables report | الموارد البشرية | POS reporting |
| POS | إعدادات الربط | Integration Settings | `/Pos/PosLegacyAdmin/BranchesData` | `PosLegacyAdmin/BranchesData` | POS branch/activity integration | الإعدادات العامة | POS admin wrapper |
| POS | إعدادات الطباعة | Print Settings | `/Pos/PrintTemplate/Index?name=KycCard` | `PrintTemplate/Index` | KYC/card print template | الإعدادات العامة | POS admin/print permission |
| POS | لوحة التحكم | Dashboard | `/Pos/PosDashboard/Index` | `PosDashboard/Index` | POS home shell | مدير النظام | POS shell |
| POS | المستخدمون | Users | `/Pos/PosLegacyAdmin/Users` | `PosLegacyAdmin/Users` | POS users | مدير النظام | POS admin; no `ERPUsers` |
| POS | الصلاحيات | Permissions | `/Pos/PosPermissions/Index` | `PosPermissions/Index` | POS permissions | مدير النظام | POS admin |
| POS | مراقبة النظام | System Health | `/Pos/PosSystemHealth/Index` | `PosSystemHealth/Index` | Runtime health | مدير النظام | POS admin |
| POS | سجل أخطاء النظام | Error Log | `/Pos/PosSystemErrorLog/Index` | `PosSystemErrorLog/Index` | Error diagnostics | مدير النظام | POS admin |
| POS | تحديثات قاعدة البيانات | SQL Updates | `/Pos/PosSqlUpdates/Index` | `PosSqlUpdates/Index` | POS DB update admin | مدير النظام | POS admin |
| POS | إدارة التقارير الديناميكية | Dynamic Reports Admin | `/Pos/DynamicReportsAdmin/Index` | `DynamicReportsAdmin/Index` | Report catalog/admin | مدير النظام | POS admin |
| MainErp | المشاريع | Projects | `/MainErp/Projects/Index` | `Projects/Index` | Project master/admin | الإدخالات | MainErp only |
| MainErp | مستخلصات المشاريع | Project Extracts | `/MainErp/ProjectExtracts/Index` | `ProjectExtracts/Index` | Project billing/extracts | الإدخالات | MainErp only |
| MainErp | العملاء | Customers | `/MainErp/Customers/Index` | `Customers/Index` | Customer/supplier master data | المبيعات | MainErp admin; POS uses KYC/customer ops |
| MainErp | فاتورة الورشة | Workshop Sales | `/MainErp/WorkshopSales/Index` | `WorkshopSales/Index` | Workshop sales invoice | المبيعات | MainErp only |
| MainErp | فاتورة المضخات | Pump Sales | `/MainErp/PumpSales/Index` | `PumpSales/Index` | Pump sales invoice | المبيعات | MainErp only |
| MainErp | تقارير المبيعات | Sales Reports | `/MainErp/SalesReports/Index` | `SalesReports/Index` | Sales reports | المبيعات | MainErp reports |
| MainErp | ملخص المبيعات | Sales Summary | `/MainErp/SalesReports/SalesSummary` | `SalesReports/SalesSummary` | Sales summary | المبيعات | MainErp reports |
| MainErp | المشتريات | Purchases | `/MainErp/Purchases/Index` | `Purchases/Index` | Purchase administration | المشتريات | MainErp only |
| MainErp | الاعتمادات المستندية | Letters of Credit | `/MainErp/LC/Index` | `LC/Index` | LC/import finance | المشتريات | MainErp only |
| MainErp | الأصناف | Items | `/MainErp/Items/Index` | `Items/Index` | Item master data | المخزون | MainErp admin |
| MainErp | المخازن | Store Data | `/MainErp/StoreData/Index` | `StoreData/Index` | Warehouse administration | المخزون | Shared core; MainErp admin |
| MainErp | التحويلات المخزنية | Stock Transfers | `/MainErp/StockTransfers/Index` | `StockTransfers/Index` | Inventory transfers | المخزون | MainErp admin |
| MainErp | الجرد | Stocktaking | `/MainErp/Stocktaking/Index` | `Stocktaking/Index` | Stock count | المخزون | MainErp admin |
| MainErp | سند التجميع | Assembly Voucher | `/MainErp/DefinCompItem/Index` | `DefinCompItem/Index` | Assembly/manufacturing voucher | المخزون | MainErp admin |
| MainErp | البنوك والخزن | Financial Administration | `/MainErp/FinancialAdministration/Index` | `FinancialAdministration/Index` | Bank/cashbox setup and admin | الحسابات | MainErp admin |
| MainErp | سندات القبض | Cashing | `/MainErp/Cashing/Index` | `Cashing/Index` | Receipt vouchers | الحسابات | Shared payment core; MainErp context |
| MainErp | سندات الصرف | Payments | `/MainErp/Payments/Index` | `Payments/Index` | Payment vouchers | الحسابات | Shared payment core; MainErp context |
| MainErp | القيود اليومية | Journal Entries | `/MainErp/JournalEntries/Index` | `JournalEntries/Index` | Journal entries | الحسابات | MainErp finance |
| MainErp | دليل الحسابات | Account Charts | `/MainErp/AccountCharts/Index` | `AccountCharts/Index` | Chart of accounts | الحسابات | MainErp admin |
| MainErp | إشعارات الخصم | Discount Notifications | `/MainErp/DiscountNotifications/Index` | `DiscountNotifications/Index` | Debit/discount notifications | الحسابات | MainErp finance |
| MainErp | التقارير المحاسبية | Accounting Reports | `/MainErp/AccountingReports/Index` | `AccountingReports/Index` | Financial reports | الحسابات | MainErp reports |
| MainErp | حركة حساب | Account Movement | `/MainErp/AccountingReports/AccountMovement` | `AccountingReports/AccountMovement` | Account movement report | الحسابات | MainErp reports |
| MainErp | معاينة سند | Voucher Preview | `/MainErp/Accounting/PreviewTest` | `Accounting/PreviewTest` | Voucher preview/test | الحسابات | MainErp finance |
| MainErp | الفواتير المالية | Financial Expenses | `/MainErp/FinancialExpenses/Index` | `FinancialExpenses/Index` | Expense invoices | الحسابات | MainErp finance |
| MainErp | شاشات VB6 المالية | Legacy Operations | `/MainErp/LegacyOperations/Index` | `LegacyOperations/Index` | Migrated finance operations | الحسابات | MainErp admin |
| MainErp | الموظفون | Employees | `/MainErp/EmployeePayroll/Employees` | `EmployeePayroll/Employees` | HR employee admin | الموارد البشرية | Shared core; MainErp admin |
| MainErp | التأمين الطبي | Medical Insurance | `/MainErp/EmployeePayroll/MedicalInsurance` | `EmployeePayroll/MedicalInsurance` | Insurance admin | الموارد البشرية | Shared core; MainErp admin |
| MainErp | تقارير التأمين | Medical Insurance Reports | `/MainErp/EmployeePayroll/MedicalInsuranceReports` | `EmployeePayroll/MedicalInsuranceReports` | HR/insurance reports | الموارد البشرية | MainErp reports |
| MainErp | مسير الرواتب | Salary Run | `/MainErp/EmployeePayroll/SalaryRun` | `EmployeePayroll/SalaryRun` | Payroll admin/posting | الموارد البشرية | MainErp only for saves/posting |
| MainErp | مفردات المرتب | Components/MOFRAD | `/MainErp/LegacyHrFinance/Components` | `LegacyHrFinance/Components` | Salary components | الموارد البشرية | MainErp HR |
| MainErp | العهد والسلف | Advances | `/MainErp/LegacyHrFinance/Advances` | `LegacyHrFinance/Advances` | Advances/custody | الموارد البشرية | MainErp HR |
| MainErp | استحقاقات الإجازات | Leave Entitlements | `/MainErp/LegacyHrFinance/LeaveEntitlements` | `LegacyHrFinance/LeaveEntitlements` | Vacation entitlement | الموارد البشرية | MainErp HR |
| MainErp | الإجازات المرضية | Sick Leaves | `/MainErp/LegacyHrFinance/SickLeaves` | `LegacyHrFinance/SickLeaves` | Sick leave | الموارد البشرية | MainErp HR |
| MainErp | المتغيرات | Compensation Adjustments | `/MainErp/LegacyHrFinance/CompensationAdjustments` | `LegacyHrFinance/CompensationAdjustments` | Payroll changes | الموارد البشرية | MainErp HR |
| MainErp | توزيعات الموظفين | Employee Allocations | `/MainErp/LegacyHrFinance/EmployeeAllocations` | `LegacyHrFinance/EmployeeAllocations` | Employee allocations | الموارد البشرية | MainErp HR |
| MainErp | الفروع وإعدادات الربط | Branches | `/MainErp/Branches/Index` | `Branches/Index` | Branch/integration setup | الإعدادات العامة | MainErp admin |
| MainErp | الخيارات | Options | `/MainErp/Options/Index` | `Options/Index` | ERP options | الإعدادات العامة | MainErp admin |
| MainErp | استيراد البيانات الأساسية | Master Data Import | `/MainErp/MasterDataImport/Index` | `MasterDataImport/Index` | Master-data import | الإعدادات العامة | MainErp admin |
| MainErp | لوحة التحكم | Dashboard | `/MainErp/Dashboard/Index` | `Dashboard/Index` | MainErp home | مدير النظام | MainErp shell |
| MainErp | المستخدمون | ERP Users | `/ERPUsers/Index` | root `ERPUsers` | System users | مدير النظام | MainErp/system admin |
| MainErp | تحديثات قاعدة البيانات | Database Migration | `/MainErp/DatabaseMigration/Index` | `DatabaseMigration/Index` | Sensitive DB migrations | مدير النظام | MainErp admin |
| MainErp | إدارة التقارير الديناميكية | Dynamic Reports Admin | `/MainErp/DynamicReportsAdmin/Index` | `DynamicReportsAdmin/Index` | Report administration | مدير النظام | MainErp admin |

## Final POS menu tree

- الإدخالات
  - إغلاق اليومية
  - استيراد العمليات من Excel
- المبيعات
  - شاشة البيع
  - العملاء / كروت كيشني
  - تقارير المبيعات
  - التقارير الذكية
  - أداء المناديب والمبيعات
  - تارجت المناديب
  - تحليل المبيعات والتحصيل
  - متابعة KYC والبنك
- المشتريات
  - فاتورة مشتريات
- المخزون
  - المخازن
  - التحويل المخزني
  - مؤشرات المخزون والربحية
- الحسابات
  - الخزن / سندات القبض
  - العهد / كيشني كارت
  - البنوك والخزن
  - سندات الصرف
  - القيود اليومية
  - إشعارات الخصم
  - التقارير المالية
  - مصمم التقارير الديناميكي
  - أداء الفروع المالي
  - تحليل التدفقات النقدية
  - تحليل المصروفات
  - العهد
  - تنبيهات محاسبية
  - تحليل سبب المشكلة
- الموارد البشرية
  - الموظفون
  - التأمين الطبي
  - تقارير التأمين
  - مسير الرواتب
  - ذمم الموظفين
- الإعدادات العامة
  - إعدادات الربط
  - إعدادات الطباعة
- مدير النظام
  - لوحة التحكم
  - المستخدمين
  - الصلاحيات
  - مراقبة النظام
  - سجل أخطاء النظام
  - تحديثات قاعدة البيانات
  - إدارة التقارير الديناميكية

## Final MainErp menu tree

- الإدخالات
  - المشاريع
  - مستخلصات المشاريع
- المبيعات
  - العملاء
  - فاتورة الورشة
  - فاتورة المضخات
  - تقارير المبيعات
  - ملخص المبيعات
- المشتريات
  - المشتريات
  - الاعتمادات المستندية
- المخزون
  - الأصناف
  - المخازن
  - التحويلات المخزنية
  - الجرد
  - سند التجميع
- الحسابات
  - البنوك والخزن
  - الخزن / سندات القبض
  - سندات الصرف
  - القيود اليومية
  - دليل الحسابات
  - إشعارات الخصم
  - التقارير المحاسبية
  - حركة حساب
  - معاينة سند
  - الفواتير المالية
  - شاشات VB6 المالية
  - التقارير الديناميكية
- الموارد البشرية
  - الموظفون
  - التأمين الطبي
  - تقارير التأمين
  - مسير الرواتب
  - مفردات المرتب
  - العهد والسلف
  - استحقاقات الإجازات
  - الإجازات المرضية
  - المتغيرات
  - توزيعات الموظفين
- الإعدادات العامة
  - الفروع وإعدادات الربط
  - الخيارات
  - استيراد البيانات الأساسية
- مدير النظام
  - لوحة التحكم
  - المستخدمين
  - تحديثات قاعدة البيانات
  - إدارة التقارير الديناميكية

## Screens found but not added to POS menu

| Screen | Reason |
|---|---|
| `/MainErp/Items/Index` الأصناف | MainErp master-data administration. POS already consumes item lookups inside sales/purchase/stock transfer. A dedicated POS item browser should be a POS wrapper, not a direct MainErp link. |
| `/MainErp/Customers/Index` العملاء | MainErp customer/supplier administration. POS has customer/KYC/card operational workflows under sales. |
| `/MainErp/Branches/Index` الفروع | MainErp branch administration. POS exposes POS integration settings through `/Pos/PosLegacyAdmin/BranchesData`. |
| `/MainErp/Options/Index` الخيارات | MainErp system options. POS should not open MainErp options; POS operational settings remain under POS wrappers. |
| `/MainErp/FinancialAdministration/Index` البنوك والخزن | Full finance administration. POS exposes bank/cashbox visibility through reports and cashing/payment workflows. |
| `/MainErp/AccountCharts/Index` دليل الحسابات | Full chart administration. POS uses account lookups in finance screens and must not administer the chart. |
| `/MainErp/MasterDataImport/Index` استيراد البيانات الأساسية | MainErp master-data import/admin. POS has its own operational Excel import. |
| `/MainErp/DatabaseMigration/Index` تحديثات قاعدة البيانات | Sensitive MainErp system tool. POS has its own POS SQL update screen. |
| `/ERPUsers/Index` المستخدمون | Main/system user administration. POS exposes `/Pos/PosLegacyAdmin/Users` for POS users. |

## Screens added from MainErp concepts into POS safely

- Users: POS route `/Pos/PosLegacyAdmin/Users`, not `ERPUsers`.
- Branches/integration: POS route `/Pos/PosLegacyAdmin/BranchesData`, not `/MainErp/Branches`.
- Stores: POS route `/Pos/Stores/Index`, shared store repository with POS connection.
- Employees/Medical Insurance/Salary preview: POS route `/Pos/EmployeePayroll/...`, shared payroll repository with POS connection and read-only/preview boundaries.
- Banks/Cashboxes: POS operational routes `/Pos/AccountingReports/Index`, `/Pos/Cashing/Index`, `/Pos/Payments/Vouchers`.

## Missing or broken routes

- No missing route was found in the current sidebar definitions.
- Browser route smoke could not complete while the local site on `localhost:61840` was not responding during the last run.
- Static validation confirmed that menu targets map to controllers/actions or explicit existing POS/MainErp URLs.

## Build result

- `MSBuild MyERP.sln /p:Configuration=Debug /p:Platform="Any CPU" /m /v:minimal` passed.
- Existing project warnings remain; no build errors were introduced by this menu delivery.

## Product-manager rationale

- Cashiers start in المبيعات and الإدخالات because their work is sales, KYC/cards, and daily close.
- Warehouse users start in المخزون because stock, stores, transfers, and inventory profitability belong together.
- Accountants start in الحسابات because vouchers, banks, cashboxes, journals, custody, and financial reports are financial controls.
- HR users start in الموارد البشرية because employees, payroll preview, insurance, and employee receivables are HR workflows.
- Admin users use الإعدادات العامة for business configuration and مدير النظام for sensitive security/runtime tools.
- MainErp keeps full administration. POS keeps operational visibility and branch/teller workflows without context leakage.
