# MainErp Route And Menu QA - 2026-05-14

Database: `Eng`  
Login used: `admin`  
Runtime host: `http://localhost:51234`

## Final Menu Tree

| Group | Item | Route | Admin result | Permission behavior |
|---|---|---|---|---|
| العملاء والموردون | العملاء والموردون | `/MainErp/Customers` | PASS | MainErp session required; anonymous redirects to login. |
| المالية | البنوك | `/MainErp/FinancialAdministration?scope=banks` | PASS | `FrmBanksData` permission checked. |
| المالية | الخزن | `/MainErp/FinancialAdministration?scope=boxes` | PASS | `FrmBoxesData` permission checked. |
| المالية | سند قبض | `/MainErp/Cashing` | PASS | `FrmCashing` permission checked. |
| المالية | سند صرف | `/MainErp/Payments` | PASS | `FrmPayments` permission checked. |
| المالية | الاعتمادات المستندية | `/MainErp/LC` | PASS | MainErp permission/session guarded. |
| المخازن | الأصناف | `/MainErp/Items` | PASS | `FrmItems` permission checked. |
| المخازن | المخازن | `/MainErp/StoreData` | PASS | `FrmStoreData` permission checked. |
| المخازن | الجرد | `/MainErp/Stocktaking` | PASS | `FrmNewGard` / `FrmNewGard1` permission checked. |
| المخازن | سند التجميع | `/MainErp/DefinCompItem` | PASS | `FrmDefinCompItem` permission checked. |
| المشاريع | مستخلصات المشاريع | `/MainErp/ProjectExtracts` | PASS | Project extract permissions checked by known extract screens. |
| الموارد البشرية | الموظفين | `/MainErp/EmployeePayroll/Employees` | PASS | HR/payroll permission gate. |
| الموارد البشرية | المسير | `/MainErp/EmployeePayroll/SalaryRun` | PASS | HR/payroll permission gate; posting protected. |
| الموارد البشرية | التأمين الطبي | `/MainErp/EmployeePayroll/MedicalInsurance` | PASS | HR/payroll permission gate. |
| الموارد البشرية | السلف | `/MainErp/LegacyHrFinance/Advances` | PASS | Legacy HR finance permission gate. |
| الموارد البشرية | الإجازات | `/MainErp/LegacyHrFinance/LeaveEntitlements` | PASS | Legacy HR finance permission gate. |
| الموارد البشرية | المتغيرات | `/MainErp/LegacyHrFinance/CompensationAdjustments` | PASS | Legacy HR finance permission gate. |
| النظام | المستخدمين | `/MainErp/Users` | PASS | MainErp session required; reads `TblUsers`. |
| النظام | الصلاحيات | `/MainErp/Permissions` | PASS | Admin sees full matrix; non-admin scope limited to own user id. |
| النظام | الإعدادات | `/MainErp/Options` | PASS | `FrmOptions` permission checked. |

## Broken Links Closed

- `سند قبض`: fixed by applying MainErp voucher read procedures to `Eng`.
- `سند صرف`: fixed by applying MainErp voucher read procedures to `Eng`.
- `المستخدمين`: fixed with MainErp-native `UsersController`, view, and menu link.
- `الصلاحيات`: fixed with MainErp-native `PermissionsController`, view, and menu link.
- `المخازن`: removed POS-area operational link from StoreData screen.

## Unresolved Links

No broken links remain in the required MainErp menu tree after the final browser route pass.
