# Legacy HR/Finance Migration Closure Report

Date: 2026-05-14  
Target: `Areas/MainErp`  
Control document: `Areas/MainErp/Docs/LegacyHrFinanceMigrationPlan_20260514.md`

## Completed Screens

| Screen | Source of truth | Target route | Status |
| --- | --- | --- | --- |
| `FrmBanksData` | Kishny VB6, `F:\Source Code\SatriahMain\Cayshny` | `/MainErp/FinancialAdministration/Index` | Editable: search, dashboard, details, add, edit, validation |
| `FrmBoxesData` | Kishny VB6, `F:\Source Code\SatriahMain\Cayshny` | `/MainErp/FinancialAdministration/Index` | Editable: search, dashboard, details, add, edit, validation |
| Employees | Main Original VB6, `F:\Source Code\SatriahMain` | `/MainErp/EmployeePayroll/Employees` | Existing MainErp enterprise screen retained; HR behavior must be validated against Main Original |
| Payroll / Salary Run | Main Original for workflow; Kishny only for runtime/replay mechanics | `/MainErp/EmployeePayroll/SalaryRun` | Usable MainErp-native salary-run screen, no POS partial dependency; posting remains protected |
| `MOFRAD` | Main Original VB6, `F:\Source Code\SatriahMain` | `/MainErp/LegacyHrFinance/Components` | Editable payroll component catalog |
| `FrmEmpsAdvanceRequest` | Main Original VB6, `F:\Source Code\SatriahMain` | `/MainErp/LegacyHrFinance/Advances` | Read-only workflow dashboard |
| `FrmVocationEntitlements` | Main Original VB6, `F:\Source Code\SatriahMain` | `/MainErp/LegacyHrFinance/LeaveEntitlements` | Read-only workflow dashboard |
| `FrmRegsterSickleave` | Main Original VB6, `F:\Source Code\SatriahMain` | `/MainErp/LegacyHrFinance/SickLeaves` | Read-only workflow dashboard |
| `FrmChangedComponentData` | Main Original VB6, `F:\Source Code\SatriahMain` | `/MainErp/LegacyHrFinance/CompensationAdjustments` | Read-only workflow dashboard |
| `FrmChangedComponentData1` | Main Original VB6, `F:\Source Code\SatriahMain` | `/MainErp/LegacyHrFinance/EmployeeAllocations` | Read-only workflow dashboard |

## Menus Added

Added MainErp sidebar entries for:

- Financial Administration: banks and boxes.
- Payroll components.
- Employee advances.
- Leave entitlements.
- Sick leaves.
- Salary component adjustments.
- Employee allocations.
- Salary run remains under the payroll area but now loads a MainErp partial and MainErp assets.

## Permissions

All new controller actions use `LegacyScreenPermissionService` and legacy screen names:

- `FrmBanksData`: view/add/edit.
- `FrmBoxesData`: view/add/edit.
- `MOFRAD`: view/add/edit.
- `FrmEmpsAdvanceRequest`: view.
- `FrmVocationEntitlements`: view.
- `FrmRegsterSickleave`: view.
- `FrmChangedComponentData`: view.
- `FrmChangedComponentData1`: view.

## Source Mapping and Preserved Logic

### Kishny: Banks and Boxes

Repository logic was mapped from `FrmBanksData` and `FrmBoxesData`:

- Source tables: `BanksData`, `tblBoxesData`, `TblBranchesData`, `ACCOUNTS`, `currency`, `TblEmployee`.
- Preserved fields include bank/box names, branch, account links, parent accounts, opening balance, balance type/date, currency, bank IBAN/account/contact fields, commission, approval/loan flags, employee custodian, cheque box, box value, and period fields.
- Preserved behavior: required name/account validation, duplicate name prevention, manual numeric key allocation compatible with legacy tables, and server-side search/paging.
- Redesigned behavior: the UI is a financial administration workspace with summary cards, lookup-backed editors, details panels, and modern action flow instead of VB6 tab clutter.

### Employee Management And Salary Run Ownership

- Employee management is Main Original ownership, not Kishny.
- Payroll administration workflow is Main Original ownership.
- Kishny is used only for payroll runtime/replay mechanics where required: `emp_salary`, `Comp1..Comp40`, AddNewDev, `Notes`, and `DOUBLE_ENTREY_VOUCHERS`.
- Existing MainErp employee/payroll implementation remains the integration point.
- Salary Run was separated from POS partial/CSS/JS and now uses `Areas/MainErp/Views/EmployeePayroll/_EmployeePayrollSalaryRun.cshtml` plus MainErp assets.
- Preserved payroll runtime compatibility follows the current MainErp/Kishny payroll repository flow: period/scope selection, preview, salary rows, journal preview, and save draft.

### Main Original: MOFRAD and HR Finance Workflows

Repository logic was mapped from Main Original VB6 forms:

- `MOFRAD` -> `mofrad`: editable component catalog with duplicate name prevention and legacy flags for add/discount, fixed/changed, salary, absence, late, overtime, insurance, reward, visibility, unit, and account fields.
- `FrmEmpsAdvanceRequest` -> `TblEmpAdvanceRequest`: server-side read/search with employee join and approval/posting indicators.
- `FrmVocationEntitlements` -> `TblVocationEntitlements`: server-side read/search with employee join and leave entitlement values.
- `FrmRegsterSickleave` -> `TblRegsterSickleave`: server-side read/search with employee join and sick-leave dates.
- `FrmChangedComponentData` -> `TblChangedComponentRegister`, `TblChangedComponentRegisterDetails`, `mofrad`: server-side read/search with aggregate detail count and total value.
- `FrmChangedComponentData1` -> `TblEmpAllocations`, `TblEmpAllocationsDetails`, `notes`, `DOUBLE_ENTREY_VOUCHERS`: server-side read/search with aggregate detail count and total value.

## Database Changes

- No schema changes were added.
- No stored procedures were added or changed.
- No `UpdateDatabase` / `Update30Follow` migration script was required.
- All implemented writes use the existing legacy tables directly and remain SQL Server 2012 compatible.

## Known Limitations

The following screens are intentionally read-only until the final accounting/payroll posting behavior is approved against VB6:

- `FrmEmpsAdvanceRequest`: write/posting depends on the legacy `SendTopost` flow and accounting approval flags.
- `FrmVocationEntitlements`: final payout/posting formulas need full VB6 parity approval.
- `FrmRegsterSickleave`: write flow needs overlap detection and payroll-impact confirmation.
- `FrmChangedComponentData`: posting/delete/rebuild behavior requires full approval trail and payroll period locking.
- `FrmChangedComponentData1`: legacy deletes/recreates `notes` and `DOUBLE_ENTREY_VOUCHERS`; write flow remains protected until posting rules are signed off.

Read-only screens show an on-screen warning instead of dead/fake buttons.

## Test Checklist

| Check | Result |
| --- | --- |
| Build `MyERP.csproj` Debug | PASS |
| `/MainErp/FinancialAdministration/Index` opens | PASS |
| `/MainErp/LegacyHrFinance/Components` opens | PASS |
| `/MainErp/LegacyHrFinance/Advances` opens | PASS |
| `/MainErp/LegacyHrFinance/LeaveEntitlements` opens | PASS |
| `/MainErp/LegacyHrFinance/SickLeaves` opens | PASS |
| `/MainErp/LegacyHrFinance/CompensationAdjustments` opens | PASS |
| `/MainErp/LegacyHrFinance/EmployeeAllocations` opens | PASS |
| `/MainErp/EmployeePayroll/SalaryRun` opens without POS partial | PASS |
| MainErp CSS/JS assets return 200 | PASS |
| MainErp JavaScript syntax check with `node --check` | PASS |
| Bank validation rejects missing required fields | PASS |
| Box validation rejects missing required fields | PASS |
| Payroll component validation rejects missing name | PASS |
| Add/edit bank through web endpoint | PASS |
| Add/edit box through web endpoint | PASS |
| Add/edit payroll component through web endpoint | PASS |
| Test rows cleaned from database | PASS |

## Build Result

Command:

```powershell
MSBuild.exe MyERP.csproj /t:Build /p:Configuration=Debug /p:VSToolsPath="C:\Program Files (x86)\MSBuild\Microsoft\VisualStudio\v10.0" /v:minimal
```

Result: PASS. Existing project warnings remain, but no migration compile errors were produced.

Runtime smoke test database: `MainErp_ConnectionString`, database `Toger` on `Wael\Sql2019`.

Browser console automation note: Playwright was not installed in the available Node runtime, so a full browser-console capture could not be completed from the agent. HTTP route checks, asset checks, and JavaScript syntax checks passed.

Follow-up runtime verification on `Dania` is documented in `Areas/MainErp/Docs/LegacyHrFinanceMigration_RuntimeVerification_Dania_20260514.md`.

## Files Changed

- `Areas/MainErp/Controllers/FinancialAdministrationController.cs`
- `Areas/MainErp/Controllers/LegacyHrFinanceController.cs`
- `Areas/MainErp/Repositories/FinancialAdministration/FinancialAdministrationRepository.cs`
- `Areas/MainErp/Repositories/LegacyHrFinance/LegacyHrFinanceRepository.cs`
- `Areas/MainErp/Services/FinancialAdministration/FinancialAdministrationService.cs`
- `Areas/MainErp/Services/LegacyHrFinance/LegacyHrFinanceService.cs`
- `Areas/MainErp/ViewModels/FinancialAdministration/FinancialAdministrationViewModels.cs`
- `Areas/MainErp/ViewModels/LegacyHrFinance/LegacyHrFinanceViewModels.cs`
- `Areas/MainErp/Views/FinancialAdministration/Index.cshtml`
- `Areas/MainErp/Views/LegacyHrFinance/Index.cshtml`
- `Areas/MainErp/Views/EmployeePayroll/SalaryRun.cshtml`
- `Areas/MainErp/Views/EmployeePayroll/_EmployeePayrollSalaryRun.cshtml`
- `Areas/MainErp/Views/Shared/_MainErpSidebar.cshtml`
- `Areas/MainErp/Content/financial-administration.css`
- `Areas/MainErp/Content/legacy-hr-finance.css`
- `Areas/MainErp/Scripts/financial-administration.js`
- `Areas/MainErp/Scripts/legacy-hr-finance.js`
- `Areas/MainErp/Docs/LegacyHrFinanceMigrationPlan_20260514.md`
- `Areas/MainErp/Docs/LegacyHrFinanceMigration_ClosureReport_20260514.md`
- `MyERP.csproj`
