# Shared Enterprise Screens Audit - 2026-05-15

## Decision Rule

The best working version wins. MainErp is usually the source authority for generic ERP/master/accounting/HR/system screens, but it is not chosen blindly. POS, original web, Main Original VB6, and Kishny VB6 are compared where relevant.

Selection criteria:

1. completeness of business logic;
2. UI/UX quality;
3. runtime stability;
4. working save/load behavior;
5. permission support;
6. Arabic labels and operator usability;
7. fit with current architecture;
8. least risky integration path.

## Current Shared Architecture Found

| Shared core | Current users | Notes |
| --- | --- | --- |
| `Common/StoreData` | MainErp StoreData, POS Stores | Best current proof that shared core + two wrappers works. |
| `Common/EmployeePayroll` | MainErp EmployeePayroll, POS EmployeePayroll | Strong shared core; area shells differ by admin/operational use. |
| `Common/DiscountNotifications` | MainErp/POS discount notifications | Read-only/shared visibility pattern. |
| `Common/Users` | MainErp Users; POS user screen remains POS wrapper | New shared user listing core; full unification still pending. |

## Required Screen Audit

| # | Arabic screen | MainErp route | POS route | Legacy source | Versions compared | Best existing version | Recommendation | Menu path | Permission key / screen | Notes and risks |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | العملاء والموردين | `/MainErp/Customers` | POS has KYC/card customer flow, not ERP customer/vendor admin | `FrmCustemers` | MainErp + POS KYC + Main VB6 | MainErp | Keep MainErp as ERP source; POS can link only for admin users or needs wrapper later | MainErp: العملاء والموردون; POS: المشتريات | `FrmCustemers` | Do not mix POS card/KYC customer workflow with supplier/customer accounting master. |
| 2 | الأصناف | `/MainErp/Items` | no full POS item master; POS has item lookups inside sales/purchases | `FrmItems` | MainErp + POS lookups + Main VB6 | MainErp after latest UI repair | Use MainErp for full item master; POS menu exposes it as shared enterprise link until POS wrapper is built | المخزون | `FrmItems` | POS link currently points to MainErp route; proper POS shell wrapper remains future work. |
| 3 | الموظفين | `/MainErp/EmployeePayroll/Employees` | `/Pos/EmployeePayroll/Employees` | Main VB6 HR/payroll screens | MainErp + POS | Shared core | Keep `Common/EmployeePayroll`; MainErp full admin, POS operational visibility | الموارد البشرية | HR legacy permission + POS HR access | Strong candidate for continued shared partials. |
| 4 | مسير الرواتب | `/MainErp/EmployeePayroll/SalaryRun` | `/Pos/EmployeePayroll/SalaryRun` | Main VB6 payroll | MainErp + POS | Shared core with protected posting | Keep shared preview/replay core; posting remains protected | الموارد البشرية | payroll/admin permission | Do not enable unrestricted posting. |
| 5 | التأمين الطبي | `/MainErp/EmployeePayroll/MedicalInsurance` | `/Pos/EmployeePayroll/MedicalInsurance` | Main VB6 HR + product workflow | MainErp + POS | Shared core with POS operational UX | Keep both shells over shared core | الموارد البشرية | HR/medical access | POS has stronger operational presentation; MainErp owns administration. |
| 6 | المخازن | `/MainErp/StoreData` | `/Pos/Stores/Index` | `FrmStoreData` | MainErp + POS + common core | Shared `Common/StoreData` | Current best POC: one repository, two wrappers | المخزون | `FrmStoreData`; POS admin/change-defaults | Works from both areas; POS is operational read/search, MainErp handles full admin save/delete. |
| 7 | سند الجرد | `/MainErp/Stocktaking` | no POS full wrapper | `FrmNewGard`, `FrmNewGard1` | MainErp + Main VB6 | MainErp | POS menu can expose MainErp route for admin; wrapper later if needed | المخزون | stocktaking legacy screen | Needs POS wrapper only if POS operators must run stock count. |
| 8 | دليل الحسابات | `/MainErp/AccountCharts` | POS has account tree/read logic inside journal screens | Main VB6 chart menu | MainErp + POS account tree | MainErp for master admin | POS menu exposes MainErp chart route for admin; shared account lookup can be extracted later | الحسابات | chart/admin permission | POS account lookup is useful but not full chart admin. |
| 9 | اليومية العامة | `/MainErp/JournalEntries` | `/Pos/JournalEntries/Index` | Main VB6 accounting | MainErp + POS | POS for operational journal UI; MainErp for ERP accounting source | Keep both wrappers; move read/write service to shared accounting core later | الحسابات | journal-entry POS flags + MainErp accounting | Do not replace working POS journals with weaker MainErp blindly. |
| 10 | البنوك | `/MainErp/FinancialAdministration?scope=banks` | POS reports/link only | `FrmBanksData` | MainErp + POS reports + Main VB6 | MainErp | Keep MainErp as admin source; POS should link to MainErp/admin or read summary only | الحسابات | `FrmBanksData` | POS does not yet have full bank master save. |
| 11 | الخزن والعهد | `/MainErp/FinancialAdministration?scope=boxes`, `/MainErp/LegacyOperations` | `/Pos/Payments`, `/Pos/AccountingReports` | `FrmBoxesData` + custody screens | MainErp + POS | Split: MainErp for boxes, POS for custody operations | Shared core later; avoid merging custody/card funding into generic box master | الحسابات | `FrmBoxesData`, POS payment flags | POS payment/custody is operationally stronger; box master belongs MainErp. |
| 12 | سند الصرف | `/MainErp/Payments` | `/Pos/Payments/Vouchers` and `/Pos/Payments` | Main VB6 vouchers | MainErp + POS | Depends use: MainErp ERP voucher, POS custody/payment ops | Keep both; extract voucher read/write core later | الحسابات | `FrmPayments`, POS payment permissions | Avoid using POS funding screen as ERP payment voucher. |
| 13 | سند القبض | `/MainErp/Cashing` | `/Pos/Cashing/Index` | Main VB6 vouchers | MainErp + POS | MainErp for ERP receipt voucher; POS for POS receipts | Keep separate wrappers; shared voucher core later | الحسابات | `FrmCashing` | Routes open separately; no single winner for all contexts. |
| 14 | التحويل المخزني | `/MainErp/StockTransfers` | `/Pos/StockTransfer/Index` | Main VB6 inventory movement | MainErp + POS | POS currently better operational UI | Promote POS stock-transfer service toward shared inventory core; keep MainErp URL | المخزون | POS stock-transfer permissions | POS implementation has working item/store UX. |
| 15 | المشتريات | `/MainErp/Purchases` | `/Pos/PurchaseInvoice/Index` | Main VB6 purchase invoices | MainErp + POS | POS currently better working purchase invoice UI | Promote POS purchase invoice logic to shared purchasing core before replacing MainErp | المشتريات | purchase/admin permission | MainErp purchase page is still lightweight. |
| 16 | الصلاحيات على الشاشات | `/MainErp/Permissions`, `/Pos/PosPermissions/Index` | `/Pos/PosPermissions/Index` | Main VB6 authority matrix | MainErp + POS | Split: POS permissions for POS, MainErp permissions for ERP legacy screens | Keep both; Arabic display layer improved in POS | النظام / الإعدادات | POS permission keys, MainErp legacy screen permissions | Internal keys stay unchanged. |
| 17 | إعدادات النظام FRMOptions | `/MainErp/Options` | no complete POS options master | `F:\Source Code\SatriahMain\Frm\FrmOptions.frm` | MainErp + Main VB6 | MainErp | Use MainErp; map labels from Main VB6 captions | الإعدادات والإدارة | `FRMOptions` / options edit permission | See FRMOptions mapping section below. |
| 18 | الفروع | `/MainErp/Branches` | `/Pos/PosLegacyAdmin/BranchesData` | Main VB6 branch/account setup | MainErp + POS legacy admin | MainErp for branch/account master; POS for integration visibility | Keep both; POS menu now exposes MainErp branch route for admin | الإعدادات والإدارة | branch/admin permission | POS legacy branch screen is useful for POS integration but not complete ERP branch setup. |
| 19 | شاشة ربط الحسابات والفروع | `/MainErp/Branches` account definitions | `/Pos/PosLegacyAdmin/BranchesData` | Main VB6 branches/accounts | MainErp + POS | MainErp | Use MainErp branch account definitions as source | الحسابات / الإعدادات | branch/account permission | POS integration screen remains limited. |
| 20 | تمويل واستعاضة العهد | no full MainErp equivalent; partial finance expenses/legacy ops | `/Pos/Payments` | Kishny/POS operational workflow | POS + Kishny VB6 | POS | POS remains source; do not force MainErp | الإعدادات والإدارة / الحسابات | `CanOpenPayments`, `CanExecutePayments`, `CanEditPayments` | POS-specific custody/card workflow. |
| 21 | إشعارات الخصم | `/MainErp/DiscountNotifications` | `/Pos/DiscountNotifications/Index` | shared financial notice concept | MainErp + POS + common read core | Shared read core | Keep shared core; expand write only after accounting approval | الحسابات | discount permissions | Current screens are safe/read-only. |
| 22 | سجل أخطاء النظام | no MainErp equivalent | `/Pos/PosSystemErrorLog/Index` | web/POS operational | POS | POS | Promote to shared monitoring later if MainErp needs it | النظام والمراقبة | admin/system permission | POS version is working and useful. |
| 23 | تحديثات قاعدة البيانات | `/MainErp/DatabaseMigration` | `/Pos/PosSqlUpdates/Index` | web/POS admin | MainErp + POS | Split by database scope | Keep both; do not cross-run wrong DB scripts | النظام والمراقبة | migration/admin permission | High-risk actions; server-side checks required. |
| 24 | مراقبة النظام | no MainErp equivalent | `/Pos/PosSystemHealth/Index` | web/POS operational | POS | POS | Promote to shared monitoring later | النظام والمراقبة | admin/system permission | POS version is best working. |
| 25 | إدارة التقارير الديناميكية | `/MainErp/DynamicReportsAdmin` if present | `/Pos/DynamicReportsAdmin/Index` | web reports | MainErp + POS | POS currently visible/working | Keep POS; expose MainErp when route verified | التقارير / النظام | reports admin | Needs route smoke before MainErp menu expansion. |
| 26 | التقارير المالية | MainErp accounting/financial reports | `/Pos/FinancialIntelligence/Index` | web/POS dashboards | MainErp + POS | POS for dashboards; MainErp for accounting reports | Keep both under reports/accounts | التقارير / الحسابات | finance/report permissions | POS financial intelligence is stronger visually. |
| 27 | تقارير الحسابات | `/MainErp/AccountingReports` | `/Pos/AccountingReports/Index` | Main VB6 accounting reports | MainErp + POS | MainErp for ERP accounting; POS for POS-visible reports | Keep both, share read repositories later | التقارير / الحسابات | accounting report permissions | Avoid removing POS reports operators use. |
| 28 | مصمم التقارير الديناميكي | `/MainErp/DynamicReports` if present | `/Pos/DynamicReports/Index` | web reports | MainErp + POS | POS currently visible/working | Keep POS as current best; verify MainErp route before exposing | التقارير | dynamic reports permission | Needs full QA. |

## Excel Module Decision

Professional Arabic module name selected: `إدارة Excel والاستيراد والمطابقة`.

Included now in POS:

- `/Pos/TokenInvoiceLookup/Index` - بحث التوكينات من Excel.
- `/Pos/ExcelImport/Index` - استيراد فواتير Excel.
- `/Pos/PosInvoiceReconciliation/Index` - مطابقة وتسوية فواتير Excel.
- `/Pos/PosSystemErrorLog/Index?source=excel` - تقارير أخطاء Excel.
- `/MainErp/MasterDataImport` - استيراد بيانات ERP من Excel for admin/shared ERP import.

Included now in MainErp:

- `/MainErp/MasterDataImport` - استيراد بيانات ERP من Excel.

POS token lookup, POS import, and POS reconciliation are the best current working versions. They were promoted into the shared menu strategy rather than being replaced by weaker MainErp screens.

## FRMOptions Arabic Caption Mapping

Source inspected: `F:\Source Code\SatriahMain\Frm\FrmOptions.frm` decoded as Windows-1256.

Mapped captions found:

| Field / area | VB6 Arabic caption |
| --- | --- |
| Form title | اعدادات النظام |
| Save button | حفظ |
| Cancel button | إلغاء |
| Company Arabic name | اسم الشركة عربي |
| Company English name | اسم الشركة انجليزي |
| Address | العنوان |
| Website | الموقع الالكتروني |
| Phone | الهاتف |
| Email | البريد الالكتروني |
| Mobile | الجوال |
| Commercial register | رقم السجل |
| Responsible person | اسم المسؤل |
| VAT register | رقم التسجيل VAT |
| VAT by activity | رقم ال VAT طبقا للنشاط |
| Show logo in reports | إظهار شعار الشركة فى التقارير |
| Show logo by branch | عرض اللوجو طبقا للفرع |
| Data location | موقع البيانات |
| Inventory options | خيارات المخازن |
| Revaluation from last stock count | اعتبار اخر جرد اعادة تقييم |
| Cost from last stock count | التكلفة تحسب من اخر جرد |
| Items by branch in POS | الاصناف طبقا للفرع فى نقاط البيع |
| Multi-store sales/purchase | التعامل باكثر من مخزن في المبيعات والمشتريات |
| All items VAT taxable | كل الاصناف خاضعة لل VAT |
| Open stock/warehouse overdraft | السحب على المكشوف من المخزن |
| Available quantity formula | الكمية المتاحة تساوي |

Unmapped options should remain using safe existing labels until each `TblOptions` field is matched against the VB6 control name and save/load code.

## Proof-of-Concept Implementation Status

| Priority screen/module | Status |
| --- | --- |
| المخازن | Already shared via `Common/StoreData`; POS and MainErp wrappers remain. |
| الأصناف | MainErp best version after UI repair; POS menu exposes route as shared enterprise/admin link. POS shell wrapper still pending. |
| دليل الحسابات | MainErp route exposed from POS menu as admin/shared link. Shared account lookup extraction pending. |
| الصلاحيات على الشاشات | POS permission screen Arabic polish started; MainErp permissions route exposed separately. |
| إدارة Excel والاستيراد والمطابقة | New POS menu module created; MainErp menu gets ERP Excel import module. |

## Deprecation Notes

- Do not delete old POS screens yet.
- POS Excel/token/reconciliation screens are not deprecated; they are the best current working versions and should be promoted/shared.
- POS branch integration screen remains visible as POS-specific integration admin, while MainErp Branches is the ERP branch/account master.
- POS purchase invoice and stock transfer should not be replaced by lightweight MainErp screens until shared service extraction is done.

## Build/Test Notes

- Build command: `MSBuild MyERP.sln /t:Build /p:Configuration=Debug /p:Platform="Any CPU" /m /v:minimal`.
- Build result: pass. Existing warnings remain.
- Browser smoke completed:
  - POS dashboard menu contains `إدارة Excel والاستيراد والمطابقة`: pass.
  - POS dashboard no longer shows token lookup under sales: pass.
  - POS Stores opens: pass.
  - POS Token lookup opens under Excel module route: pass.
  - POS Invoice reconciliation opens under Excel module route: pass.
  - POS permissions screen shows Arabic labels for admin/bulk wording: pass.
  - MainErp dashboard shows the Excel module: pass.
  - MainErp MasterDataImport opens from MainErp Excel module: pass.
  - MainErp Items opens: pass.
  - MainErp StoreData opens: pass.
  - MainErp Permissions opens: pass.
  - MainErp AccountCharts reached the `دليل الحسابات` title, but browser DOM evaluation timed out because the page is heavy. Treat as route-open but performance risk until a lighter account tree smoke is added.

## Open Risks

- Direct POS menu links to MainErp routes are acceptable as a transitional Option A, but they still require MainErp authentication/session. A cleaner next step is wrapper routes that preserve POS shell while invoking shared services.
- Teller visibility still depends on current POS permission flags. Do not expose high-risk admin links to teller users without a finer menu visibility layer.
- MainErp dynamic report routes need separate smoke testing before being declared shared-ready.
