# Shared Enterprise HR Module Plan - 2026-05-15

## Architectural Decision

HR must be one shared enterprise module with two safe shells:

- MainErp shell for full ERP administration.
- POS/Kishny shell for operational visibility and permitted actions.

The business module is shared through:

- `Common/EmployeePayroll/EmployeePayrollRepository.cs`
- `Common/EmployeePayroll/EmployeePayrollModels.cs`
- shared HR view partials under `Views/Shared/EnterpriseHr`
- area wrapper controllers only for shell, permission, and database-context selection.

This keeps routes separate while avoiding duplicated business screens:

- MainErp routes stay `/MainErp/EmployeePayroll/...`.
- POS routes stay `/Pos/EmployeePayroll/...`.
- Shared screens do not require a second login.
- POS must not redirect into MainErp login.
- MainErp must not depend on POS routes.

## Feature Matrix

| Feature | POS/Kishny status | MainErp status | Working? | Backend dependency | Recommended shared implementation |
| --- | --- | --- | --- | --- | --- |
| Employees list/search | Exists as read-only HR screen | Exists as richer admin screen | Yes | `Common.EmployeePayroll.EmployeePayrollRepository.SearchEmployees/GetEmployee` | Use MainErp richer employee partial as shared base; POS uses it read-only unless permission policy allows write. |
| Employee create/edit | POS intentionally blocked | MainErp save/edit exists | MainErp yes, POS protected | `SaveEmployee`, `SetEmployeeActive`, `TblEmployee` and linked insurance tables | Shared UI with write URLs supplied only in MainErp shell. POS remains read-only by policy. |
| Medical insurance operational dashboard | Strong POS implementation | Weak/summary MainErp page | POS yes | `GetMedicalInsuranceOperationalDashboard` | Promote POS operational partial to shared module and expose from both shells. |
| Medical insurance providers/plans/settings | Strong POS UI, but POS save endpoints intentionally blocked | MainErp save endpoints exist | MainErp yes; POS read-only/protected | `MedicalInsuranceProviders`, `MedicalInsurancePlans`, save provider/plan methods | Use POS rich settings partial as shared UI. MainErp supplies save URLs; POS supplies empty/protected URLs unless explicitly authorized. |
| Medical insurance reports | POS report partial exists | MainErp report page exists but less shared | Yes | `GetMedicalInsuranceSubscriptions`, `GetMedicalInsuranceDeductions` | Promote POS report partial to shared partial with dynamic area URLs. |
| Payroll preview | POS preview exists, read-only | MainErp richer preview exists | Yes | `PreviewSalaryRun`, salary compatibility/replay methods | Use MainErp richer salary partial as shared base; POS supplies read-only preview only. |
| Payroll save/test posting | POS intentionally blocked | MainErp protected save/test-posting exists | MainErp yes | `SaveSalaryRun`, protected posting methods | Shared salary UI hides protected actions when wrapper does not provide URLs. |
| Leaves | Not found in POS EmployeePayroll area | Legacy/root HR controllers and MainErp legacy HR finance area exist | Existing legacy coverage likely | root `Controllers/HR/*Vacation*`, `LegacyHrFinance` | Phase 2 route into shared HR module as links/panels, without moving legacy controllers yet. |
| Advances | POS has employee receivable/custody reporting; MainErp payroll preview includes advance deduction | Partial | `DOUBLE_ENTREY_VOUCHERS.NEmpid`, payroll advance calculations | Phase 2 shared HR module should expose advances/receivables as read-only finance panel first. |
| Attendance | Root HR controllers exist | Not clearly exposed in current MainErp EmployeePayroll wrapper | Not validated in this pass | `EmployeesAttendAndLeave`, attendance controllers | Phase 2 add shared navigation entry to existing attendance screens after permission mapping. |
| HR settings | Existing root HR setting controllers | Not consolidated | Not validated in this pass | departments, jobs, salary items, vacation settings | Phase 2 catalog settings under shared HR admin, reusing existing controllers. |

## Proposed Shared Module Location

- Shared MVC partials: `Views/Shared/EnterpriseHr`
- Backend: existing `Common/EmployeePayroll`
- Area wrappers:
  - `Areas/MainErp/Controllers/EmployeePayrollController.cs`
  - `Areas/Pos/Controllers/EmployeePayrollController.cs`
- Shell views remain area-specific and render shared partials.

## Proposed Routes

MainErp:

- `/MainErp/EmployeePayroll/Employees`
- `/MainErp/EmployeePayroll/MedicalInsurance`
- `/MainErp/EmployeePayroll/MedicalInsuranceReports`
- `/MainErp/EmployeePayroll/SalaryRun`

POS:

- `/Pos/EmployeePayroll/Employees`
- `/Pos/EmployeePayroll/MedicalInsurance`
- `/Pos/EmployeePayroll/MedicalInsuranceReports`
- `/Pos/EmployeePayroll/SalaryRun`

## Permission Mapping

| Business permission | MainErp check | POS check |
| --- | --- | --- |
| HR open/view | admin or `FrmEmployee` or `FrmEmpSalary5` view | full access/admin or POS legacy `FrmEmployee` or `FrmEmpSalary5` view |
| Employee add/edit | MainErp `FrmEmployee` add/edit | POS blocked in Phase 1 |
| Payroll preview | MainErp HR open | POS HR open |
| Payroll save/test posting | MainErp `FrmEmpSalary5` add/edit/admin | POS blocked in Phase 1 |
| Medical insurance settings save | MainErp HR open/admin save endpoints | POS blocked in Phase 1 |
| Medical insurance operational dashboard | HR open | HR open |

## Files To Keep, Move, Reuse

Keep:

- Existing area wrapper controllers for route/security boundaries.
- Existing POS and MainErp routes.
- Existing database context selection rules.

Move/promote to shared:

- MainErp employee partial.
- MainErp salary run partial.
- POS medical insurance operational partial.
- POS medical insurance settings partial.
- POS medical insurance reports partial.

Reuse:

- `Common/EmployeePayroll/EmployeePayrollRepository.cs`
- `Common/EmployeePayroll/EmployeePayrollModels.cs`
- Existing area scripts/CSS for now, until Phase 3 merges assets after browser verification.

## Implementation Status

Completed in this pass:

- POS and MainErp HR wrapper views now render the same shared partials from `Views/Shared/EnterpriseHr`.
- POS and MainErp controllers provide explicit shell context values: area, read-only mode, database name, branch/store/user labels, and environment badge.
- MainErp now exposes the medical insurance operational dashboard endpoint used by the promoted shared insurance UI.
- Shared partials receive URLs from the active wrapper area instead of hard-coding `/Pos/...` or `/MainErp/...`.
- POS HR remains read-only for employee save, provider/plan save, payroll save, and protected posting until a separate business decision grants write permissions.
- The accidental alternate `Common/EmployeePayroll/Views` Razor copy was removed so there is one shared UI source.
- The stale area-local HR partial copies under `Areas/MainErp/Views/EmployeePayroll` and `Areas/Pos/Views/EmployeePayroll` were removed after the shared wrappers were validated.

Runtime validation completed on local IIS Express over `https://localhost:44370` with an authenticated POS session:

- `/Pos/EmployeePayroll/Employees` returned 200.
- `/Pos/EmployeePayroll/MedicalInsurance` returned 200.
- `/Pos/EmployeePayroll/SalaryRun` returned 200.
- `/Pos/EmployeePayroll/MedicalInsuranceReports` returned 200.
- `/MainErp/EmployeePayroll/Employees?fromPos=1` returned 200 without redirecting to a login page.
- `/MainErp/EmployeePayroll/MedicalInsurance?fromPos=1` returned 200 without redirecting to a login page.
- `/MainErp/EmployeePayroll/SalaryRun?fromPos=1` returned 200 without redirecting to a login page.
- `/MainErp/EmployeePayroll/MedicalInsuranceReports?fromPos=1` returned 200 without redirecting to a login page.
- POS and MainErp HR lookup/search/dashboard JSON endpoints returned `success=true`.

Build validation:

- `MyERP.csproj` built successfully in Debug AnyCPU after the shared HR refactor.

## Risks

- POS and MainErp may point to different databases by design. A shared HR module must show context badges and must not silently switch contexts.
- POS write operations are intentionally protected today. Enabling HR write from POS requires a business decision and explicit permission mapping.
- Existing Arabic text in some views is mojibake from prior encoding history. Functional consolidation should happen first; text cleanup should follow in a controlled pass.
- Leaves, advances, and attendance exist mostly outside the current `EmployeePayroll` wrapper. They need route integration after mapping the legacy controller permissions.

## Implementation Phases

Phase 1:

- Render shared HR partials from both area shells.
- Keep POS and MainErp routes separate.
- Preserve the no-second-login behavior.
- Preserve POS read-only protections for sensitive writes.

Phase 2:

- Add shared HR module navigation for employees, insurance, payroll, leaves, advances, attendance, and settings.
- Route leaves/advances/attendance into the HR workbench using existing controllers first.

Phase 3:

- Merge duplicate CSS/JS only after route and behavior verification.
- Remove dead placeholders after confirming no route depends on them.
