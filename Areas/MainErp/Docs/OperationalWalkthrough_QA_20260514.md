# Operational Walkthrough QA - 2026-05-14

## Purpose

This report covers Phase 14 operational validation and workflow hardening. The goal was not to prove that screens technically load; that was covered in Phase 13. The goal here was to make realistic HR, payroll, finance, and POS users faster and safer.

## Tested Personas

| Persona | Focus |
| --- | --- |
| HR administrator | Employee search, profile edit surface, branch/department assignment, insurance visibility |
| Payroll accountant | Salary preview, month navigation, deductions, advances, insurance effect, explainability |
| Branch operator | Fast employee lookup, POS read-only access, route safety |
| Financial reviewer | Project extract totals, LC totals, accounting visibility, protected posting state |
| Manager/approver | Approval/status clarity, protected workflows, read-only messaging |
| POS operational user | Employee/insurance/payroll visibility without HR administration leakage |

## Tested Workflows

### Employee Management

Validated:

- quick search route;
- branch/department filters;
- employee profile panel;
- employee editor open/close;
- medical insurance state visibility;
- permission boundary for denied/add/edit users.

Hardening added:

- `Ctrl + K` focuses employee search.
- `Enter` from search executes search.
- Recent employees strip records recently opened profiles.
- `Escape` closes the employee editor.
- Employee screen now includes an operational shortcut/safety strip.

Remaining operator pain:

- Employee grid still needs formal server-side paging if deployed on a database much larger than Dania.
- Recent employees are browser-local only, which is appropriate for this phase but not an enterprise audit feature.

### Medical Insurance Workflow

Validated:

- insurance state is visible in employee rows and profile summary;
- provider/category visibility is available through MainErp medical insurance surfaces;
- POS opens reporting/visibility, not administration;
- payroll preview consumes insurance impact without enabling posting.

Remaining business questions:

- Dania does not contain `EmpInsurances`; fallback behavior remains required.
- Final HR policy for enrollment/exclusion approval needs business sign-off from Main Original HR rules.

### Payroll Preview Workflow

Validated:

- salary preview on Dania 2026/03 returned successfully;
- compatibility parity returns successfully;
- accounting replay returns successfully;
- `LegacySnapshot` versus `Reconstructed` remains visible;
- posting button remains protected.

Hardening added:

- previous/current/next month navigation buttons;
- `Ctrl + Enter` preview shortcut;
- `Alt + P` compatibility parity shortcut;
- `Alt + R` accounting replay shortcut;
- operator safety strip explaining protected posting.

Remaining operator pain:

- Explainability is still dense for non-technical managers.
- Large preview payloads are now supported, but long-term UX should introduce paging/virtualization for very large periods.

### Project Extracts Workflow

Validated:

- admin route opens on Dania;
- HR-only QA user is blocked with HTTP 403;
- sticky financial summary remains visible;
- financial totals, VAT, and net payable are readable;
- read-only/protected posture is explicit.

Hardening added:

- `Ctrl + K` focuses search;
- number keys switch tabs when not typing;
- operational hints strip;
- status strip for draft/review, financial review, approval clarity, and read-only safety.

Remaining operator pain:

- Approval transitions remain read-only/display-oriented.
- Business approval is required before enabling any workflow state writes.

### Letters Of Credit Workflow

Validated:

- admin route opens on Dania;
- HR-only QA user is blocked with HTTP 403;
- LC search workbench loads;
- posting/rebuild actions remain permission and confirmation gated;
- no browser console errors in route smoke test.

Hardening added:

- `Ctrl + K` focuses LC search;
- number keys switch tabs when a detail context is open;
- operational hints strip;
- selected-LC status strip for active/closed state, missing accounts, linked notes, and protected actions.

Remaining operator pain:

- The `/MainErp/LC` landing state does not always auto-select an LC, so the selected-LC status strip appears only after an LC is opened.
- This should be improved later with an explicit “select an LC” operational empty state.

### POS Operational Visibility

Validated:

- POS employee route redirects to POS login when not in POS context;
- POS salary route redirects to POS login when not in POS context;
- POS medical insurance route redirects to POS login when not in POS context;
- Phase 13 browser checks already confirmed POS employee/salary/insurance pages are read-only when context exists.

Hardening retained:

- POS write endpoints return protected operational-only responses.
- POS employee create/edit/activation controls are hidden/guarded.
- POS salary save is blocked.
- POS medical insurance administration is not exposed.

## Runtime Results

Environment:

- IIS Express: `http://localhost:63813`
- Database: `Dania`

Authenticated MainErp HR user:

| Route/API | Result |
| --- | --- |
| `/MainErp/EmployeePayroll/Employees` | Pass - HTTP 200 |
| `/MainErp/EmployeePayroll/SalaryRun` | Pass - HTTP 200 |
| `/MainErp/EmployeePayroll/Search` | Pass - HTTP 200, `success=true`, 177,664 bytes |
| `/MainErp/EmployeePayroll/PreviewSalaryRun?Year=2026&Month=3` | Pass - HTTP 200, `success=true`, 17,116,856 bytes |
| `/MainErp/EmployeePayroll/PayrollCompatibilityParity?Year=2026&Month=3` | Pass - HTTP 200, `success=true`, 488,932 bytes |
| `/MainErp/EmployeePayroll/PayrollAccountingReplay?Year=2026&Month=3` | Pass - HTTP 200, `success=true`, 851,749 bytes |

Authenticated admin:

| Route | Result |
| --- | --- |
| `/MainErp/ProjectExtracts` | Pass - HTTP 200, operational hints and ops strip present |
| `/MainErp/LC` | Pass - HTTP 200, operational hints present |

Permission boundary:

| User | Route | Result |
| --- | --- | --- |
| `QA_HRFIN_VIEW` | `/MainErp/ProjectExtracts` | Pass - HTTP 403 |
| `QA_HRFIN_VIEW` | `/MainErp/LC` | Pass - HTTP 403 |

Browser smoke:

| Screen | Result |
| --- | --- |
| Employees | Pass - no console errors |
| Salary Run | Pass - no console errors |
| Project Extracts | Pass - no console errors |
| LC | Pass - no console errors |

## Fixed Friction Points

- Faster employee search.
- Faster return to recently used employee profiles.
- Faster payroll month navigation.
- Faster payroll preview/parity/replay actions.
- Clearer project extract workflow state.
- Clearer LC route/search operation hints.
- Clearer safety messaging for read-only/protected workflows.

## Remaining Business Questions

- Which project extract approval states should become write-enabled, and who approves each transition?
- Which LC posting/rebuild operations should be available in production versus support-only?
- What wording should finance approve for payroll explainability shown to managers?
- Should recent employees become server-side per-user history later, or remain browser-local convenience?
- What POS employee/payroll visibility is required by branch operators beyond read-only lookup?

## Safety Confirmation

No Phase 14 action enabled:

- payroll posting;
- salary payment posting;
- `Notes` creation from payroll preview/replay;
- `DOUBLE_ENTREY_VOUCHERS` creation from payroll preview/replay;
- `SendTopost` replacement;
- allocation rebuild.

The module remains safe for operational walkthrough and controlled business validation.
