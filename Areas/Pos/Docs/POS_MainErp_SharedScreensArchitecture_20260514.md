# POS/MainErp Shared Screens Architecture - 2026-05-14

## Executive rule

Shared module does not mean shared route.

MainErp remains the ERP/business authority. POS/Kishny remains the operational shell for cards, teller work, branch workflows, and safe visibility. POS must never navigate directly to `/MainErp/...` or `ERPUsers` because that leaks database context, session, permissions, and user intent.

## Screen classification

| Screen/module | Classification | MainErp route | POS route | POS mode | Shared core/service | Context rule | Permission rule |
|---|---|---|---|---|---|---|---|
| POS sales screen | POS-native | N/A | `/Pos/PosTransaction/Index` | Operational write | POS repositories | `KishnyCashConnection` | POS teller/session permissions |
| KYC/cards | POS-native | N/A | `/Pos/PosTransaction/Index?openKyc=true`, `/Pos/KycBankFollowUp/Index` | Operational | POS repositories | `KishnyCashConnection` | POS KYC/customer-service permissions |
| POS closing | POS-native | N/A | `/Pos/PosClosing/Index` | Operational | POS closing repository | `KishnyCashConnection` | POS closing/teller permissions |
| Employees | Shared module with wrappers | `/MainErp/EmployeePayroll/Employees` | `/Pos/EmployeePayroll/Employees` | Read-only operational view | `Common/EmployeePayroll/EmployeePayrollRepository.cs`, `Common/EmployeePayroll/EmployeePayrollModels.cs` | MainErp uses `MainErpDbConnectionFactory`; POS uses `KishnyCashConnection` unless explicit demo override is enabled | MainErp legacy HR permission; POS `FrmEmployee`/`FrmEmpSalary5` or admin |
| Medical Insurance | Shared module with wrappers | `/MainErp/EmployeePayroll/MedicalInsurance` | `/Pos/EmployeePayroll/MedicalInsurance` | Read-only/operational visibility | `Common/EmployeePayroll/*` | Same as employees | MainErp can administer providers/plans; POS admin saves are blocked with 403 |
| Medical Insurance Reports | Shared module with wrappers | `/MainErp/EmployeePayroll/MedicalInsuranceReports` | `/Pos/EmployeePayroll/MedicalInsuranceReports` | Reporting | `Common/EmployeePayroll/*` | Same as employees | Area-specific HR/report permissions |
| Salary preview | Shared module with wrappers | `/MainErp/EmployeePayroll/SalaryRun` | `/Pos/EmployeePayroll/SalaryRun` | Preview only | `Common/EmployeePayroll/*` | Same as employees | POS salary save/posting blocked with 403 |
| Stores/Warehouses | Shared module with wrappers | `/MainErp/StoreData/Index` | `/Pos/Stores/Index` | Branch-scoped read/lookup | `Common/StoreData/StoreDataRepository.cs` | MainErp wrapper injects `MainErpDbConnectionFactory`; POS wrapper injects `KishnyCashConnection` | MainErp `FrmStoreData`; POS session with branch scope, admin can filter branch |
| Stock transfer | POS-native operational; MainErp has separate inventory admin route | `/MainErp/StockTransfers/Index` | `/Pos/StockTransfer/Index` | Operational | Separate area controllers | Area-specific DB | Area-specific permissions |
| Boxes/Cashboxes and receipt vouchers | Shared payment/cashing core with wrappers | `/MainErp/Cashing/Index` | `/Pos/Cashing/Index` | Operational finance | POS repositories derive from MainErp payment repositories but inject `KishnyCashConnection` | MainErp factory for MainErp; POS `KishnyPosConnectionFactory` | MainErp `FrmCashing`; POS `FrmCashing`/accounting access |
| Payment vouchers | Shared payment core with wrappers | `/MainErp/Payments/Index` | `/Pos/Payments/Vouchers` | Operational finance | `Areas/MainErp/Repositories/Payments/*` reused by POS wrappers with POS connection factory | MainErp factory vs POS `KishnyCashConnection` | MainErp `FrmPayments`; POS payment/accounting permissions |
| Banks/Boxes reports | Shared business concept, POS operational report | `/MainErp/FinancialAdministration/Index` | `/Pos/AccountingReports/Index` | Reporting/lookup | Area-specific repositories | Area-specific DB | MainErp banks/boxes permissions; POS `CanViewAccountingReports` |
| Financial administration | MainErp-only administration | `/MainErp/FinancialAdministration/Index` | No direct MainErp link from POS | Not exposed as admin | MainErp service/repository only | MainErp context only | MainErp finance permissions |
| Account chart | MainErp-only administration | `/MainErp/AccountCharts/Index` | No direct MainErp link from POS | Not exposed | MainErp repository | MainErp context only | MainErp accounting permissions |
| Items | MainErp administration; POS uses operational item lookups inside sales/purchase/stock screens | `/MainErp/Items/Index` | No direct admin route in POS menu | Lookup only through POS screens | Area-specific operational SQL | Area-specific DB | MainErp `FrmItems`; POS screen permissions |
| Branches/options/integration settings | Shared concept with separate shells | `/MainErp/Branches/Index`, `/MainErp/Options/Index` | `/Pos/PosLegacyAdmin/BranchesData` | POS admin/integration settings | POS legacy admin repository | POS `KishnyCashConnection` | MainErp admin vs POS full-access admin |
| Users | Separate system administration per product | `ERPUsers/Index` from MainErp system shell | `/Pos/PosLegacyAdmin/Users` | POS admin only | POS legacy admin repository | POS `KishnyCashConnection` | POS `IsFullAccess`; MainErp system permission |
| Dynamic reports | Shared idea, separate route/scope | `/MainErp/DynamicReports/Index` | `/Pos/DynamicReports/Index` | Reporting | Dynamic report infrastructure with scope | Scope-specific connection/context | Scope-specific permissions |
| Payroll posting/replay/test posting | MainErp-only administration | MainErp payroll endpoints | No POS route | Not exposed | MainErp payroll repository | MainErp context only | MainErp save/posting permissions |
| LC/project extracts/master import/database migration | MainErp-only administration | MainErp routes | No POS route | Not exposed | MainErp services | MainErp context only | MainErp permissions/admin |

## Routing decisions

- POS sidebar items point only to `/Pos/...` routes.
- MainErp sidebar items point only to `/MainErp/...` or approved MainErp system routes such as `ERPUsers`.
- Removed POS direct navigation to `/MainErp/...`, `ERPUsers`, and `data-mainerp-admin-url`.
- Where a screen exists in both products, the routes are separate and the shared code sits below the controller layer.
- POS operational wrappers must pass POS URLs into views/partials. Shared partials must not hard-code `/MainErp/...`.

## Shared services used

- Employee, salary preview, and medical insurance use `Common/EmployeePayroll/EmployeePayrollRepository.cs` and `Common/EmployeePayroll/EmployeePayrollModels.cs`.
- Stores/warehouses use `Common/StoreData/StoreDataRepository.cs`.
- Payment/cashing wrappers reuse MainErp payment voucher repository logic through POS-specific repository subclasses that inject `KishnyCashConnection`.
- POS users/branches/options-style integration screens use `Areas/Pos/Data/PosLegacyAdminRepository.cs`, not `ERPUsers` or MainErp routes.

## Context/database resolution

MainErp:

- `MainErpControllerBase` restores MainErp session.
- MainErp repositories use `MainErpDbConnectionFactory` and the active MainErp connection setting.
- MainErp debug/database override is MainErp-scoped only.

POS:

- POS controllers restore `PosUserContext` through `PosLoginController.RestorePosContext`.
- POS operational data uses `KishnyCashConnection`.
- `EmployeePayrollController` supports `PosEmployeePayrollDatabaseOverride` only when `PosEmployeePayrollDemoOverrideEnabled` is truthy.
- `PosEmployeePayrollDatabaseOverride` is empty in `Web.config` at this audit, so Dania is not active by default.
- POS Employee/Insurance screens expose context badges for database, branch, store, user, and demo state.
- POS Stores now exposes a `DB: <database>` badge from `KishnyCashConnection`.

## Permissions by area

- MainErp permissions are checked through MainErp session and `LegacyScreenPermissionService`.
- POS permissions are checked through `PosUserContext`, POS flags, and `PosLegacyScreenPermissionService`.
- POS read-only HR endpoints allow search/preview/reporting and block save/admin/posting endpoints with 403 through `PosOperationalOnly`.
- POS admin/settings screens require POS full access and do not redirect to MainErp system administration.

## Links removed or blocked

Removed from POS sidebar:

- `~/MainErp/Customers/Index`
- `~/MainErp/Purchases/Index`
- `~/MainErp/Items/Index`
- `~/MainErp/Stocktaking/Index`
- `~/MainErp/DefinCompItem/Index`
- `~/MainErp/FinancialAdministration/Index`
- `~/MainErp/AccountCharts/Index`
- `~/MainErp/Branches/Index`
- `~/MainErp/Options/Index`
- `~/MainErp/MasterDataImport/Index`
- `~/ERPUsers/Index`

Removed from POS medical insurance partial:

- `data-mainerp-admin-url`

## Wrappers created or fixed

- `Areas/Pos/Controllers/EmployeePayrollController.cs` is the POS wrapper for shared payroll/insurance visibility and uses POS context plus read-only/operational boundaries.
- `Areas/MainErp/Controllers/EmployeePayrollController.cs` is the MainErp wrapper for full HR/payroll/insurance administration.
- `Areas/Pos/Controllers/StoresController.cs` is the POS wrapper for branch-scoped store visibility and now surfaces the POS active DB.
- `Areas/MainErp/Controllers/StoreDataController.cs` is the MainErp wrapper for full store administration.
- `Areas/Pos/Repositories/Payments/PaymentVoucherReadRepository.cs` and `PaymentVoucherWriteRepository.cs` reuse MainErp payment repository logic with POS connection injection.

## QA results

Static QA:

- POS sidebar contains no `MainErp`, `ERPUsers`, or `mainerp` navigation targets.
- POS views/scripts contain no hard-coded MainErp navigation URL after removing `data-mainerp-admin-url`.
- Remaining `Areas/MainErp/Content/...` references in POS are CSS asset references only; they are not routes and do not switch session/database.
- `Web.config` has `PosEmployeePayrollDatabaseOverride` empty, so POS does not load Dania unless demo override is explicitly enabled and configured.

Build QA:

- `MSBuild MyERP.sln /p:Configuration=Debug /p:Platform="Any CPU" /m /v:minimal` passed after the routing/context changes.

Browser QA:

- Attempted smoke test against `http://localhost:61840/Pos/PosDashboard/Index`.
- Browser could not complete because the local server did not respond on that port during this run.
- Previous static and build checks confirm no Razor parse error and no POS sidebar cross-area links.

## Remaining risks

- Some POS screens reuse CSS from `Areas/MainErp/Content`. This is acceptable for styling, but a later UI asset cleanup could move shared CSS to a neutral shared content path.
- Some payment/cashing classes still live under `Areas/MainErp.Repositories` while POS subclasses inject POS context. This is functionally safe, but the long-term naming target should be a neutral shared namespace when there is time.
- Browser smoke testing needs a running IIS Express/local site. Static route and build checks passed, but runtime 200 checks were blocked by the unavailable local server.
