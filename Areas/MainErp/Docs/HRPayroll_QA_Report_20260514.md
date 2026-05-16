# HR/Payroll QA Report - 2026-05-14

## Scope

Phase 13 QA covered the stabilized HR/payroll preview, medical insurance visibility, replay diagnostics, and dual-area integration across:

- `Areas/MainErp`
- `Areas/Pos`

This was a controlled QA pass, not production posting validation.

## Environment

- Project: `F:\Source Code\DynamicErp`
- Runtime: IIS Express, `http://localhost:63813`
- Database: `Dania`
- SQL Server: configured local SQL Server runtime environment
- Build target: `MyERP.csproj`, Debug

## Tested Data Footprint

Read-only Dania row counts:

| Table | Rows |
| --- | ---: |
| `TblEmployee` | 880 |
| `emp_salary` | 10,908 |
| `mofrad` | 41 |
| `TBLInsurancesJoin` | 38 |
| `Notes` | 80,331 |
| `DOUBLE_ENTREY_VOUCHERS` | 266,557 |
| `TblBoxesData` | 78 |

Schema note: `EmpInsurances` was not found in Dania. The medical-insurance runtime continues to use optional/fallback behavior.

## MainErp Route QA

Authenticated as `QA_HRFIN_VIEW` against `Dania`.

| Route | Result |
| --- | --- |
| `/MainErp/EmployeePayroll/Employees` | Pass - HTTP 200 |
| `/MainErp/EmployeePayroll/SalaryRun` | Pass - HTTP 200 |
| `/MainErp/EmployeePayroll/MedicalInsuranceReports` | Pass - HTTP 200 |
| `/MainErp/LegacyHrFinance/Components` | Pass - HTTP 200 |
| `/MainErp/FinancialAdministration/Index` | Pass - HTTP 200 |

Unauthenticated MainErp routes redirect to the MainErp login shell as expected.

## POS Route QA

Browser smoke-tested:

| Route | Result |
| --- | --- |
| `/Pos/EmployeePayroll/Employees` | Pass - read-only employee visibility, no new/save controls |
| `/Pos/EmployeePayroll/SalaryRun` | Pass - preview visible, no save control |
| `/Pos/EmployeePayroll/MedicalInsurance` | Pass - opens read-only reports/visibility experience |
| `/Pos/EmployeePayroll/MedicalInsuranceReports` | Pass - reports route available |

POS does not expose HR administration, insurance administration, replay administration, or payroll posting.

## API QA

Authenticated as `QA_HRFIN_VIEW` against `Dania`.

| API | Result | Payload |
| --- | --- | ---: |
| `/MainErp/EmployeePayroll/Lookups` | Pass - HTTP 200, `success=true` | 4,027 bytes |
| `/MainErp/EmployeePayroll/Search` | Pass - HTTP 200, `success=true` | 177,664 bytes |
| `/MainErp/EmployeePayroll/PreviewSalaryRun?Year=2026&Month=3` | Pass - HTTP 200, `success=true` | 17,116,856 bytes |
| `/MainErp/EmployeePayroll/PayrollCompatibilityParity?Year=2026&Month=3` | Pass - HTTP 200, `success=true` | 488,932 bytes |
| `/MainErp/EmployeePayroll/PayrollAccountingReplay?Year=2026&Month=3` | Pass - HTTP 200, `success=true` | 851,749 bytes |

Fix verified: large payroll preview no longer fails because of MVC JSON length limits.

## Permission QA

| User | Scenario | Result |
| --- | --- | --- |
| `QA_HRFIN_VIEW` | Open employee/payroll routes and read APIs | Pass |
| `QA_HRFIN_DENIED` | Open employee screen | Pass - HTTP 403 |
| `QA_HRFIN_ADD` | Empty employee save payload | Pass - HTTP 400 validation, not permission bypass |
| `QA_HRFIN_EDIT` | Add-style employee save payload | Pass - HTTP 403 |

Permission coverage is active on routes and AJAX endpoints tested in this phase.

## JavaScript QA

Syntax checks:

| File | Result |
| --- | --- |
| `Areas/MainErp/Scripts/employee-payroll.js` | Pass |
| `Areas/Pos/Scripts/employee-payroll.js` | Pass |

Browser console smoke checks showed no JavaScript console errors on the tested MainErp and POS screens.

## Build Result

Command:

```powershell
MSBuild.exe MyERP.csproj /t:Build /p:Configuration=Debug /p:VSToolsPath="%ProgramFiles(x86)%\MSBuild\Microsoft\VisualStudio\v10.0" /v:minimal
```

Result: Pass.

Known existing warnings remain in unrelated legacy project files. No build-breaking Phase 13 errors remain.

## Fixed Issues

- Fixed large JSON payload failure for salary preview and replay endpoints.
- Hardened MainErp/POS payroll JavaScript against non-JSON error pages and network failures.
- Stabilized replay comparison rendering in MainErp.
- Enforced POS read-only employee/payroll behavior in UI and server endpoints.
- Corrected POS protected-route Arabic messages.
- Verified Dania runtime lookup/search/preview/replay APIs with real row counts.

## Remaining Warnings

- Payroll/accounting replay remains diagnostic only.
- Medical-insurance administration belongs to MainErp HR; POS only exposes operational visibility.
- Dania does not contain `EmpInsurances`; optional fallback remains required.
- Full operator timing for keyboard workflow/click count still needs live user observation.

## Safety Summary

Still intentionally blocked:

- payroll posting;
- salary payment posting;
- `Notes` insertion;
- `DOUBLE_ENTREY_VOUCHERS` insertion;
- `SendTopost` replacement;
- allocation rebuild.

Replay remains read-only and financially safe.

## Readiness

MainErp readiness: controlled QA ready.

POS readiness: operational visibility QA ready.

Recommended next phase:

- operator walkthrough on real HR/payroll scenarios;
- replay parity review with finance;
- business approval for any future protected test posting plan;
- continue preserving Main Original HR ownership and Kishny-only runtime replay isolation.
