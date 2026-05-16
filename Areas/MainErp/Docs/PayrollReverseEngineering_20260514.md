# Payroll Reverse Engineering - 2026-05-14

## Status

The HR/Finance UI migration is not blocked by layout work anymore. The remaining blocker is payroll/accounting parity. Payroll posting, accounting posting, allocation rebuild, and any SendTopost replacement must stay disabled until component-level parity is proven against VB6.

Runtime validation on `Dania` showed a full-period mismatch for `sgn = 20264`: 436 employees compared, 0 matched, legacy `emp_salary.EmpTotalNet` total = 1,578,491.00, current web current-master-basis total = -53,163.00. This proves VB6 payroll is snapshot/component driven, not `TblEmployee` master salary driven.

## Source Boundary

Explicit ownership tags:

- `HR_SOURCE = MAIN_ORIGINAL`
- `PAYROLL_RUNTIME_SOURCE = KISHNY`
- `ACCOUNTING_REPLAY_SOURCE = KISHNY`

| Workflow | Source of truth | Notes |
| --- | --- | --- |
| HR workflows and employee management | `F:\Source Code\SatriahMain` | `HR_SOURCE = MAIN_ORIGINAL`. Main Original owns employee structure, HR workflows, advances, vacations, sick leave, allocations, department behavior, management hierarchy, and HR operational behavior. |
| Payroll runtime / Salary Run | `F:\Source Code\SatriahMain\Cayshny\Frm\New frm\FrmEmpSalary5.frm`, with older reference in `FrmEmpSalary.frm` | `PAYROLL_RUNTIME_SOURCE = KISHNY`. Kishny owns active payroll runtime mechanics, salary snapshots, `Comp1..Comp40`, and salary-run grid behavior. |
| Accounting replay / salary journal | `F:\Source Code\SatriahMain\Cayshny\Frm\New frm\FrmEmpSalary5.frm` and `F:\Source Code\SatriahMain\Cayshny\Bas\ModAccounts.bas` | `ACCOUNTING_REPLAY_SOURCE = KISHNY`. Kishny owns AddNewDev linkage, salary journal generation, posting/replay mechanics, and deterministic accounting replay semantics. |
| Payroll component runtime setup | Kishny `EmpSalaryComponent`, `mofrad`, `mofrdat`, plus helper functions | Runtime compatibility only. These tables explain payroll calculation/posting behavior; they do not redefine the MainErp HR business model. |
| Medical insurance workflow | `F:\Source Code\SatriahMain` | `HR_SOURCE = MAIN_ORIGINAL`. Provider/category/status, enrollment, exclusion, HR approval, employee insurance profile, and insurance administration are HR ownership. Payroll may only consume approved insurance impact. |
| Advances / leave / sick / allocations / changed components | Main Original forms for HR workflow ownership; Dania payroll tables for runtime values | If Main Original and Kishny differ for HR behavior, Main Original wins. Kishny can only consume the payroll-result state needed for snapshot/replay compatibility. |

## HR vs Payroll Source Ownership

This document is a payroll/accounting reverse-engineering document, but it must not accidentally turn Kishny into the HR business authority.

### Ownership Rule

MainErp target architecture is:

```text
Main Original HR architecture
PLUS
Kishny payroll runtime/accounting replay compatibility layer
```

It is not:

```text
Kishny HR copied into MainErp
```

### HR_SOURCE = MAIN_ORIGINAL

Primary source:

- `F:\Source Code\SatriahMain`

Main Original VB6 owns the HR business model for:

- employee structure and master-data meaning;
- departments;
- jobs;
- branches as HR/business assignment context;
- HR workflows;
- advances;
- vacations and entitlements;
- sick leave;
- employee allocations;
- department logic;
- management hierarchy;
- HR operational approvals and administration concepts;
- HR statuses;
- attendance-related HR rules;
- employee insurance ownership and medical-insurance workflow;
- insurance-related employee rules;
- payroll administration concepts that are business workflow, not runtime calculation mechanics.

Preserve from Main Original:

- field meaning and relationships for HR screens;
- workflow state and approval semantics;
- employee/department/branch/management hierarchy behavior;
- medical insurance provider/category/status/enrollment/exclusion semantics;
- operational HR policies where they differ from Kishny.

Modernize intentionally:

- UI layout;
- workflow visibility;
- approval dashboards;
- search/paging;
- audit/explainability surfaces;
- validation messaging and operational ergonomics.

### PAYROLL_RUNTIME_SOURCE = KISHNY

Primary source:

- `F:\Source Code\SatriahMain\Cayshny`

Kishny remains the source of truth only for:

- current payroll runtime engine behavior;
- `emp_salary` salary snapshot behavior;
- `Comp1..Comp40` runtime model;
- salary-run grid calculations;
- payroll replay semantics;
- salary runtime scalar function behavior where used by `FrmEmpSalary5`;
- runtime component precedence and payroll-period snapshot interpretation.

This is legacy-only runtime compatibility. It should explain how historical salary periods and Dania payroll snapshots work; it should not become the HR domain model.

### ACCOUNTING_REPLAY_SOURCE = KISHNY

Primary sources:

- `F:\Source Code\SatriahMain\Cayshny\Frm\New frm\FrmEmpSalary5.frm`
- `F:\Source Code\SatriahMain\Cayshny\Bas\ModAccounts.bas`

Kishny owns:

- salary posting/replay mechanics;
- AddNewDev accounting linkage;
- salary journal generation;
- `Notes`/`DOUBLE_ENTREY_VOUCHERS` replay semantics;
- project/account allocation replay diagnostics;
- payroll accounting parity traces.

This remains read-only until accounting parity and business approval are complete.

### Conflict Resolution

If HR workflow behavior differs between Main Original and Kishny:

- Main Original HR behavior is authoritative;
- Kishny HR behavior must not be used as a business reference;
- payroll runtime may consume only the resulting HR state needed for salary snapshot/replay compatibility;
- any Kishny dependency must be documented as technical payroll runtime compatibility, not HR ownership.

This prevents payroll replay reverse engineering from redefining MainErp HR architecture.

## HR Ownership Separation

### Final Ownership Rule

All HR business logic must use only the Main Original VB6 system as source of truth:

- `F:\Source Code\SatriahMain`

Kishny is not a reference for Human Resources behavior.

This includes:

- employees;
- departments;
- jobs;
- branches;
- HR hierarchy;
- payroll administration workflow;
- medical insurance workflow;
- advances;
- vacations;
- sick leave;
- employee allocations;
- changed components as HR/administration workflow;
- approvals;
- HR statuses;
- attendance-related HR rules;
- insurance-related employee rules;
- employee operational structure.

### Kishny Limitation

Kishny may only be used as a technical/runtime reference for:

- payroll snapshot structure;
- `emp_salary`;
- `Comp1..Comp40`;
- historical salary replay tracing;
- accounting replay tracing;
- `Notes` / `DOUBLE_ENTREY_VOUCHERS` linkage;
- `AddNewDev` posting mechanics.

Kishny runtime references are allowed only where required for payroll replay compatibility.

They are not HR business ownership.

### Medical Insurance Rule

Medical insurance is an HR domain, not a payroll-runtime domain.

Main Original owns:

- provider/category/status model;
- enrollment and exclusion;
- HR approval;
- employee insurance profile;
- insurance administration;
- employee eligibility and operational insurance state.

Payroll runtime may only consume the approved HR insurance result when calculating salary impact or replaying historical payroll snapshots.

Therefore, any previous assumption that inferred employee medical-insurance workflow from Kishny is removed. Kishny insurance traces remain diagnostic only for salary deduction/posting compatibility.

### Corrected Modules And Assumptions

Corrected to `HR_SOURCE = MAIN_ORIGINAL`:

- Employee master behavior and operational structure.
- Department/job/branch business assignment behavior.
- HR approval states.
- Advances workflow ownership.
- Vacation and sick-leave workflow ownership.
- Employee allocation ownership.
- Changed-component workflow ownership.
- Medical insurance workflow ownership.
- Attendance-related HR rules.

Kept as `PAYROLL_RUNTIME_SOURCE = KISHNY` only:

- `emp_salary` snapshot interpretation.
- `Comp1..Comp40` runtime component model.
- Payroll-period runtime scalar traces.
- Snapshot-first preview compatibility.

Kept as `ACCOUNTING_REPLAY_SOURCE = KISHNY` only:

- AddNewDev accounting linkage.
- Salary journal replay.
- `Notes`/`DOUBLE_ENTREY_VOUCHERS` read-only parity.
- Project/account allocation replay diagnostics.

Removed assumptions:

- Kishny employee behavior is not used to define MainErp employee management.
- Kishny department behavior is not used to define MainErp department logic.
- Kishny project/employee relation is not used to define HR allocation ownership.
- Kishny medical-insurance behavior is not used to define HR insurance administration.
- Kishny approval or operational shortcuts are not promoted to HR workflow rules.

### Architecture Target

MainErp HR must become:

```text
Modernized Main Original HR system
with
Kishny payroll replay/accounting compatibility layered underneath
```

It must not become:

```text
Kishny HR migrated to web
```

Payroll reverse-engineering work must not redefine the HR business model.

## VB6 Payroll Calculation Graph Found

### Main entry path

- `FrmEmpSalary5.FillGridWithData` builds the working grid for a selected year/month.
- It selects active employees, branch/project/department/job information, nationality, salary type, and dynamic payroll function outputs.
- It populates `Comp1..Comp40` by looping `j = 1 To 40`.
- It calculates totals in `CalculateNets`.
- `create_report_data` deletes and recreates `emp_salary` rows for the selected period/branch, storing a payroll snapshot.

### Main component sources

| Source | Purpose |
| --- | --- |
| `emp_salary` | Final payroll snapshot used by VB6 print/payment/posting flows. Contains `Comp1..Comp40`, total fields, insurance, vacation, attendance, branch/project/department and paid flags. |
| `mofrad` | Payroll component master. Controls `AddOrDiscount`, `FixedOrChanged`, `ViewComp`, `ZmamAccount`, `AdvPaymentdAccount`, `Account_Code`, `Account_code1`, `Insurances`, `Salary`, `showMofradAll`, `culc30orRminder`. |
| `mofrdat` | Component code mapping into `EmpSalaryComponent.AccountCode`. |
| `EmpSalaryComponent` | Employee-level fixed/monthly component values. VB6 uses it for fixed components and insurance salary base. |
| `TblChangedComponentRegister` / `TblChangedComponentRegisterDetails` | Variable components and adjustments. VB6 reads changed components when `mofrad.FixedOrChanged = 1`. |
| `TblComponentYearDet` | Temporary/date-bound component override. VB6 `GetValueAllwIntro` overrides the component value for a month/year when present. |
| `QryAllEmpAdvance(@Month,@Year)` | Advance deductions. VB6 sums `TotalAdvance` per employee into `Grid.TotalAdvance`. |
| `tblPresentTime` | Attendance/work-hour/overtime display source. `GetWorkHours` computes work hours and overtime minutes for the month. |
| `EmpInsurances`, `EmpVoCation`, `EmpVoCation3`, `EmpPrePaymentID`, `EmpPrePaymentValue`, `GetAbcentDay` | SQL scalar functions called directly by VB6 payroll query. These must be treated as part of the legacy calculation graph. |
| `TblVacationSalary`, `TblEmbarkation`, `TblVocationEntitlements` | Vacation/leave salary impact and paid-state integration. |

### Net calculation observed

From `FrmEmpSalary5.CalculateNets`:

```text
TotalAddtion  = sum(CompN where mofrad.AddOrDiscount = 0)
TotalDiscount = sum(CompN where mofrad.AddOrDiscount <> 0)
total1        = Mokafea + TotalAddtion
total2        = PrePaidvalue + ToalInsurance + TotalAdvance + TotalDiscount + VoCation3 + TotalDiscountFromComponents
EmpTotalNet   = total1 - total2
```

Important: in the newer form, salary components are represented primarily through `Comp1..Comp40`, not only the older named columns such as `Emp_Salary`, `Emp_Salary_sakn`, etc.

### Month/day proration

VB6 applies period proration when the employee starts or returns from vacation inside the selected month:

- `SystemOptions.MonthIs30days` chooses either 30-day basis or actual month days.
- `BignDateWork` and `lastHolidaydate` can reduce `CountDays`.
- `mofrad.showMofradAll` can bypass proration.
- `mofrad.culc30orRminder` chooses 30-day or actual-days denominator.

### Save sequence

`create_report_data` in the newer form:

- Deletes existing `emp_salary` for selected `sgn`/branch scope.
- Inserts one row per grid employee.
- Persists `Comp1..Comp40`, `CountDays`, `RecordDate`, `ToalInsurance`, `AbcentDay`, `RemainDay`, vacation fields, overtime/work-hours, branch/project/department, employee balance, and net totals.

The older `FrmEmpSalary.frm` has a smaller save surface and should not be used as the final target for current Kishny parity.

## Accounting Linkage Found

### Salary accrual / posting

VB6 creates `Notes` rows and `DOUBLE_ENTREY_VOUCHERS` lines. The older form creates a high-level salary accrual note with:

- `Notes.NoteType = 66`
- `Notes.salary = year + monthIndex`
- debit to branch/project salary expense account
- credit to employee salary payable account

The newer form is more detailed. It creates lines per component category:

- Salary/payroll additions to salary/project accounts.
- Deduction components to employee payable and component account.
- `ZmamAccount` components to employee payable vs employee receivable/liability account.
- `AdvPaymentdAccount` components to advance/prepayment accounts.
- `TotalAdvance` to employee salary payable and employee receivable account.
- `VoCation3` to salary payable and branch account code `204`.
- `ToalInsurance` to insurance account and employee salary payable.
- Branch and department fields are passed to `ModAccounts.AddNewDev`.

### Payment sequence

The older payment path:

- Creates `Notes.NoteType = 5`.
- Uses box or bank fields based on payment type.
- Debits employee payable lines.
- Credits box/bank/cash account.
- Marks selected `emp_salary.Payed = 1`.

The newer form contains additional detailed posting branches that must be reverse-engineered before write/post is enabled.

## Runtime/Temporary Logic Detected

VB6 uses both database snapshots and runtime grid calculations:

- `emp_salary` stores the snapshot after `create_report_data`.
- `Comp1..Comp40` are runtime-filled before snapshot save.
- SQL functions inject runtime values into the grid.
- `TblComponentYearDet` can override values for a date period.
- `QryAllEmpAdvance` excludes paid advance installments at runtime.
- `GetEmpBalance` can calculate employee balance through account-balance helper logic when enabled.
- Several posting flows derive voucher lines from the grid rather than re-querying a normalized component detail table.

## Hard Blockers Before Enabling Posting

- Rebuild web payroll preview from the real graph above, not current employee master fields.
- Add component-level parity output for at least one employee and then full-period batch.
- Verify all scalar functions used by VB6 exist and behave in `Dania`.
- Match `emp_salary.Comp1..Comp40`, `total1`, `total2`, `EmpTotalNet`, `ToalInsurance`, `TotalAdvance`, `VoCation3`, `AbcentDay`, and `CountDays`.
- Match `Notes` counts, `NoteType`, `salary`, note totals, and `DOUBLE_ENTREY_VOUCHERS` line counts/totals by account/branch/department.
- Do not enable salary posting, payment posting, allocation rebuild, or SendTopost replacement until the parity scripts pass.

## New Probe

Read-only SQL probe:

`Areas/MainErp/Sql/13_LegacyHrFinance_Dania_ComponentParity_Probe.sql`

Purpose:

- Pick a target employee/period.
- Output the `emp_salary` snapshot.
- Unpivot `Comp1..Comp40` with `mofrad` metadata.
- Output source candidates from `EmpSalaryComponent`, changed components, yearly overrides, advances, attendance, vacation/insurance/prepayment functions, and accounting vouchers.
- Provide the first actionable component-level diff surface for the next implementation phase.

## Probe Execution On Dania

Executed successfully on `Dania` for `sgn = 20264` with auto-selected highest-net employee:

| Field | Value |
| --- | --- |
| Employee | `EmpId = 180`, `Emp_Code = 00001` |
| Period | April 2026, `sgn = 20264` |
| Legacy net | 35,000.00 |
| Legacy additions | `total1 = 35,000.00` |
| Legacy deductions | `total2 = 0.00` |
| Active legacy components | `Comp21 = 30,000.00`, `Comp23 = 5,000.00` |
| Component source | `EmpSalaryComponent` rows for `mofrad_type = 21` and `23` |
| Runtime function outputs for this employee | insurance/vacation/prepayment/absence functions returned NULL for this period |
| Accounting notes for salary period | 4 `Notes` rows with `NoteType = 66` and `salary = 20264` |

This confirms the next implementation step: web payroll preview must reconstruct or read the VB6-compatible component snapshot from `EmpSalaryComponent`, `mofrad`, overrides, changed components, advances and scalar functions before any posting flow is enabled.

## Compatibility Engine Phase 1 Implementation

Implemented in:

- `Common/EmployeePayroll/EmployeePayrollModels.cs`
- `Common/EmployeePayroll/EmployeePayrollRepository.cs`

Current behavior:

- `PreviewSalaryRun` now routes through a VB6-compatible preview path.
- If an `emp_salary` row exists for the requested `sgn`, the preview treats that row as the source of truth.
- The preview attaches `Comp1..Comp40` component lines with `mofrad` metadata.
- If no `emp_salary` snapshot exists, the preview creates a compatibility fallback from `EmpSalaryComponent` grouped by `mofrad_type`, plus runtime advance and scalar function outputs where available.
- Rows now carry compatibility status:
  - `LegacySnapshot`
  - `Reconstructed`
- Reconstructed rows are not allowed to be saved yet. This is intentional until VB6 component-level parity is approved.

Runtime check on `Dania`:

| Test | Result |
| --- | --- |
| Preview `Year=2026`, `Month=4`, `EmployeeId=180` | Passed |
| Row status | `LegacySnapshot` |
| Net | 35,000.00 |
| `total1` | 35,000.00 |
| `total2` | 0.00 |
| Components attached | 40 |
| `Comp21` | 30,000.00 |
| `Comp23` | 5,000.00 |

Safety rule still active: payroll posting, salary payment posting, allocation rebuild, and SendTopost replacement remain blocked.

## Incremental Parity Expansion - Phase 2

Implemented in:

- `Common/EmployeePayroll/EmployeePayrollModels.cs`
- `Common/EmployeePayroll/EmployeePayrollRepository.cs`
- `Areas/MainErp/Controllers/EmployeePayrollController.cs`

Added:

- `PayrollCompatibilityParityReport`
- `PayrollCompatibilityParityRow`
- `PayrollCompatibilityComponentDiff`
- Internal endpoint:
  - `GET /MainErp/EmployeePayroll/PayrollCompatibilityParity`

The component compatibility source now uses a set-based expansion, avoiding per-employee N+1 queries:

- `emp_salary.Comp1..Comp40` snapshot values
- `mofrad` metadata
- `EmpSalaryComponent` fixed/monthly source values
- `TblChangedComponentRegister` / `TblChangedComponentRegisterDetails` variable component values for the period
- `TblComponentYearDet` period overrides

Source precedence for reconstructed component value:

1. `TblComponentYearDet` override when present.
2. `TblChangedComponentRegisterDetails` when `mofrad.FixedOrChanged = 1`.
3. `EmpSalaryComponent` otherwise.

Safety:

- Reconstructed rows still cannot be saved.
- Reconstructed rows still cannot post accounting.
- Reconstructed rows still cannot create `Notes`.
- Reconstructed rows still cannot create `DOUBLE_ENTREY_VOUCHERS`.
- Reconstructed rows still cannot mark salary paid.
- Reconstructed rows still cannot rebuild allocations.

Runtime parity checks on `Dania`:

| Test | Result |
| --- | --- |
| Single employee `EmpId=180`, `sgn=20264` | Passed |
| Single employee legacy rows | 1 |
| Single employee reconstructed rows | 0 |
| Single employee component mismatch rows | 0 |
| Single employee total mismatch rows | 0 |
| Single employee legacy net | 35,000.00 |
| Single employee reconstructed net | 35,000.00 |
| Full period `sgn=20264` execution time | About 1.2 seconds |
| Full period rows | 880 |
| Full period legacy snapshot rows | 436 |
| Full period reconstructed rows | 444 |
| Full period component mismatch rows | 474 |
| Full period total mismatch rows | 473 |
| Full period legacy net total | 1,578,491.00 |
| Full period reconstructed net total | 3,147,417.00 |
| Full period net difference | 1,568,926.00 |

The full-period mismatch is expected at this stage. It now has a machine-readable diagnostic surface, so the next work can reduce mismatches by category instead of comparing totals blindly.

## Mismatch Classification Engine - Phase 3

Implemented in:

- `Common/EmployeePayroll/EmployeePayrollModels.cs`
- `Common/EmployeePayroll/EmployeePayrollRepository.cs`
- `Areas/MainErp/Controllers/EmployeePayrollController.cs`

Added diagnostic fields to every `PayrollCompatibilityComponentDiff`:

- `MismatchCategory`
- `LikelySource`
- `PrecedenceDecision`
- `ConfidenceScore`

Added component explainability endpoint:

- `GET /MainErp/EmployeePayroll/PayrollCompatibilityExplain`

The explainability result returns, for a single employee/component:

- legacy snapshot value
- reconstructed value
- `EmpSalaryComponent` fixed source
- `TblChangedComponentRegisterDetails` changed source
- `TblComponentYearDet` override source
- precedence decision
- mismatch category
- confidence score
- proration trace (`CountDays`, `BignDateWork`, `lastHolidaydate`, `MonthIs30days`, `showMofradAll`, `culc30orRminder`)

Classification categories now supported:

- `matched`
- `duplicate component sourcing`
- `override precedence issue`
- `changed-component merge issue`
- `proration issue`
- `attendance issue`
- `insurance issue`
- `advance deduction timing`
- `inactive component inclusion`
- `branch/project filtering issue`
- `fixed-vs-variable conflict`
- `showMofradAll behavior`
- `culc30orRminder behavior`
- `orphan snapshot component`
- `missing runtime scalar output`

Runtime classification check on `Dania`, `sgn = 20264`:

| Metric | Result |
| --- | --- |
| Full period rows | 880 |
| Legacy snapshot rows | 436 |
| Reconstructed rows | 444 |
| Component mismatch rows | 474 |
| Total mismatch rows | 473 |
| Net difference | 1,568,926.00 |
| Execution time | About 0.9 seconds |

Observed mismatch categories:

| Category | Count |
| --- | ---: |
| `culc30orRminder behavior` | 599 |
| `changed-component merge issue` | 35 |
| `insurance issue` | 34 |
| `branch/project filtering issue` | 9 |

Explainability spot check:

| Field | Result |
| --- | --- |
| Employee | `EmpId = 180` |
| Component | `Comp21` |
| Legacy value | 30,000.00 |
| Reconstructed value | 30,000.00 |
| Category | `matched` |
| Proration category | `not-proration-candidate` |

Safety remains unchanged. Reconstructed rows are still blocked from saving, payment marking, `Notes`, `DOUBLE_ENTREY_VOUCHERS`, SendTopost replacement, and allocation rebuild until component parity and accounting parity are approved.

## Proration And Temporal Rule Reconstruction - Phase 4

VB6 source traced:

- `F:\Source Code\SatriahMain\Cayshny\Frm\FrmEmpSalary5.frm`
- `F:\Source Code\SatriahMain\Cayshny\Frm\FrmChangedComponentData.frm`

Confirmed VB6 temporal semantics:

- `MonthDayNo = 30` when `SystemOptions.MonthIs30days = True`; otherwise it uses actual calendar month days.
- `CountDays` is reduced only when `BignDateWork` or `lastHolidaydate` falls inside the selected salary month.
- `lastHolidaydate` takes the same runtime path as an in-month vacation return and can override the start-date calculation.
- If the calculated period equals the full calendar month, VB6 converts `CountDays` to `30`.
- Fixed components are prorated only when the runtime `countFlag = 1`.
- Changed components are not prorated in the same loop.
- `showMofradAll = True` bypasses proration.
- `culc30orRminder = 0` uses `MonthDayNo` as denominator.
- `culc30orRminder <> 0` uses actual calendar month days as denominator.

Implemented:

- Compatibility temporal context reconstruction.
- Temporal matrix fields on explainability output:
  - calendar month days
  - payroll month basis
  - payroll days
  - expected denominator
  - actual denominator
  - numerator days
  - count flag
  - proration applied/bypassed
  - vacation overlap
  - branch scope
  - rule path
  - denominator reason
- Component raw source and temporal adjusted value are now both preserved.
- Reconstructed fixed components are adjusted by the VB6-compatible temporal rule before parity comparison.

Runtime classification check on `Dania`, `sgn = 20264`, after temporal reconstruction:

| Metric | Before Phase 4 | After Phase 4 |
| --- | ---: | ---: |
| Full period rows | 880 | 880 |
| Legacy snapshot rows | 436 | 436 |
| Reconstructed rows | 444 | 444 |
| Component mismatch rows | 474 | 433 |
| Total mismatch rows | 473 | 432 |
| Net difference | 1,568,926.00 | 1,466,523.00 |
| Execution time | About 0.9 seconds | About 0.7 seconds |

Observed mismatch categories after temporal reconstruction:

| Category | Count |
| --- | ---: |
| `insurance issue` | 432 |
| `branch/project filtering issue` | 147 |
| `changed-component merge issue` | 35 |
| `missing runtime scalar output` | 2 |
| `culc30orRminder behavior` | 1 |

Important result: the previous dominant `culc30orRminder behavior` count collapsed from 599 to 1 after applying the traced VB6 temporal semantics and tightening false-positive detection. The remaining dominant gap is now insurance/component-account behavior, not basic temporal denominator selection.

Safety remains unchanged. Reconstructed rows are still blocked from saving, payment marking, `Notes`, `DOUBLE_ENTREY_VOUCHERS`, SendTopost replacement, and allocation rebuild.

## Phase 6 - Insurance And Financial Posting Semantics

### VB6 Source Trace

Source boundary remains `PAYROLL_RUNTIME_SOURCE = KISHNY` and `ACCOUNTING_REPLAY_SOURCE = KISHNY` for salary-run/posting semantics only:

- Source project: `F:\Source Code\SatriahMain\Cayshny`
- Source form: `Frm\New frm\FrmEmpSalary5.frm`
- Accounting module: `Bas\ModAccounts.bas`
- Insurance helper: `Bas\SalimNew.bas`

The VB6 salary posting path uses `ModAccounts.AddNewDev` to write `DOUBLE_ENTREY_VOUCHERS`. That helper:

- skips zero-value lines before insert
- writes to `DOUBLE_ENTREY_VOUCHERS` unless `opening_balance=True`
- assigns `Account_Code`, `Value`, `Credit_Or_Debit`, `Notes_ID`, `RecordDate`, `branch_id`, `Departementid`, `NEmpid`, `project_id`, and descriptions
- keeps salary posting linked to `Notes.NoteType = 66` and `Notes.salary = Year + MonthIndex`

### Insurance Semantics Discovered

Ownership correction:

- Medical insurance workflow ownership is `HR_SOURCE = MAIN_ORIGINAL`.
- The Kishny traces below are payroll-runtime/accounting diagnostics only.
- They explain how an approved or historical insurance state affected salary and posting.
- They do not define provider/category/status/enrollment/exclusion/approval HR behavior.

`EmpInsurances` in `Dania` is not a clean percentage calculation over `TblEmployee` salary fields. It is a legacy snapshot lookup:

```sql
SELECT TBLInsurancesJoin.InsTotal
FROM TBLInsurancesJoin
RIGHT OUTER JOIN TBLInsurances ON TBLInsurancesJoin.IDINS = TBLInsurances.IDINS
WHERE TBLInsurancesJoin.EmpCode = @EmpID
  AND TBLInsurances.Monthe = @Monthh
  AND TBLInsurances.SubYear = @Yar
  AND TBLInsurancesJoin.payed IS NULL
```

This confirms that payroll insurance-impact parity must be snapshot-first:

- `emp_salary.ToalInsurance` is the saved salary snapshot value.
- `dbo.EmpInsurances(@Month - 1, @Year, Emp_ID)` reads `TBLInsurancesJoin.InsTotal`.
- `TBLInsurancesJoin.payed` excludes already-paid insurance from runtime calculation.
- `TblSocialInsurance` supplies posting accounts through VB6 `GetInsuranceAccount`.
- `mofrad.Insurances = 1` marks salary components included in the insurance basis for additional leave/insurance calculations.

This does not transfer medical-insurance workflow ownership to Kishny. Main Original remains authoritative for HR insurance administration.

### Posting Semantics Discovered

VB6 insurance posting inside `FrmEmpSalary5` posts employee insurance share as:

- debit to employee accrued salary account: `TblEmployee.Account_Code1`
- credit to social insurance account: `TblSocialInsurance.Acount_Code1`
- both lines are written through `ModAccounts.AddNewDev`
- employee id is passed to `NEmpid`
- branch and department are passed from the salary grid row

VB6 also contains an additional "insurance not found in payroll" path. It calculates insurance from `EmpSalaryComponent` rows where `mofrad.Insurances = 1`, applies `CitizenVal1` or `ResidentVal1` from `TblSocialInsurance`, then posts the same debit/credit pair. This path remains diagnostic-only in web until full accounting parity is approved.

### Web Diagnostic Additions

Added read-only semantic traces:

- `PayrollCompatibilityInsuranceTrace`
- `PayrollAccountingParityTrace`
- `PayrollAccountingNoteTrace`
- `PayrollAccountingVoucherTrace`

Existing explainability now includes:

- insurance source function
- source tables
- saved `ToalInsurance`
- runtime `EmpInsurances` value
- `TBLInsurancesJoin` total/base/rate/work-days
- `TblSocialInsurance` accounts and percentages
- employee accrued salary account
- exclusion reason, including paid insurance rows
- insurance-adjusted total

This is a payroll-impact explanation, not a medical-insurance workflow definition.

New endpoint:

- `GET /MainErp/EmployeePayroll/PayrollAccountingParityTrace`

The endpoint reads existing `Notes` and `DOUBLE_ENTREY_VOUCHERS` rows only. It does not create, update, delete, post, mark paid, rebuild allocations, or call SendTopost.

### Dania Runtime Verification

Repository-level verification against `Dania`:

- Employee `797`, period `2026/4`, component `1`
  - `RuntimeInsurance = 365.4375`
  - `InsuranceCreditAccount = a2a2a2a4a3a1`
  - source: `dbo.EmpInsurances(@Month - 1, @Year, Emp_ID)` via `TBLInsurancesJoin -> TBLInsurances`
- Accounting trace for salary `202512`, branch `10`
  - `NotesCount = 1`
  - `VoucherLineCount = 642`
  - debit total `985306.2700`
  - credit total `985306.2700`
  - balance `0.0000`

Parity category snapshot after adding insurance explainability:

- rows `880`
- legacy snapshots `436`
- reconstructed rows `444`
- component mismatch rows `433`
- total mismatch rows `432`
- net diff `1466523.0000`
- mismatch categories:
  - `insurance issue`: `432`
  - `branch/project filtering issue`: `147`
  - `changed-component merge issue`: `35`
  - `missing runtime scalar output`: `2`
  - `culc30orRminder behavior`: `1`

### Safety Status

Safety remains unchanged:

- reconstructed rows cannot save salary runs
- reconstructed rows cannot create `Notes`
- reconstructed rows cannot create `DOUBLE_ENTREY_VOUCHERS`
- reconstructed rows cannot mark salary paid
- reconstructed rows cannot replace SendTopost
- reconstructed rows cannot rebuild allocations

Payroll posting remains blocked until insurance parity, account routing parity, note/voucher counts, branch/project/department linkage, and posting totals match VB6 for the same period.

## Phase 7 - Deterministic Accounting Replay

### Objective

Added a read-only accounting replay layer that reconstructs VB6 salary posting decisions in memory before any write path is considered. This is not posting. It is a deterministic semantic model used to compare expected web replay against historical VB6 `Notes` and `DOUBLE_ENTREY_VOUCHERS`.

New endpoint:

- `GET /MainErp/EmployeePayroll/PayrollAccountingReplay`

New models:

- `PayrollAccountingReplayRequest`
- `PayrollAccountingReplayReport`
- `PayrollReplayedNote`
- `PayrollReplayedVoucherLine`
- `PayrollAccountingReplayComparison`

### Replayed VB6 Rules

The first replay engine implements these read-only rules from `FrmEmpSalary5` and `ModAccounts.AddNewDev`:

- `Notes.NoteType = 66`
- `Notes.salary = Year + MonthIndex`
- replayed salary note per branch
- component additions:
  - trigger: `ViewComp=True`, `AddOrDiscount=0`, `ZmamAccount=False`, `AdvPaymentdAccount=False`
  - debit: `mofrad.Account_Code`
- salary payable:
  - trigger: `total1 > 0`
  - credit: `TblEmployee.Account_Code1`
- component deductions:
  - trigger: `ViewComp=True`, `AddOrDiscount=-1`, `ZmamAccount=False`, `AdvPaymentdAccount=False`
  - debit: `TblEmployee.Account_Code1`
  - credit: `mofrad.Account_Code`
- custody / `ZmamAccount`:
  - debit: `TblEmployee.Account_Code1`
  - credit: `TblEmployee.Account_Code`
- total advance deduction:
  - debit: `TblEmployee.Account_Code1`
  - credit: `TblEmployee.Account_Code`
- advance-payment component:
  - debit/credit routing through `TblEmployee.Account_Code3`
- vacation impact:
  - debit: `TblEmployee.Account_Code1`
  - credit: currently traced to `TblEmployee.Account_Code2` as a diagnostic fallback for VB6 `get_account_code_branch(204)`
- insurance:
  - debit: `TblEmployee.Account_Code1`
  - credit: `TblSocialInsurance.Acount_Code1`
  - source value: `EmpInsurances` / `TBLInsurancesJoin.InsTotal` snapshot

Every replayed line includes:

- `RuleId`
- account routing path
- triggering VB6 condition
- component number/name when applicable
- branch/department/employee linkage
- explanation text

### Deterministic Comparison

The replay report compares:

- legacy debit total vs replay debit total
- legacy credit total vs replay credit total
- legacy balance vs replay balance
- account-level distribution
- branch-level distribution
- line counts

It reads existing legacy rows through the previous `PayrollAccountingParityTrace` surface and does not write anything.

### Dania Runtime Results

`Dania`, salary `202512`, branch `10`:

- legacy notes: `1`
- legacy voucher lines: `642`
- replayed lines: `498`
- legacy debit: `985306.2700`
- replay debit: `1418970.9700`
- debit difference: `433664.7000`
- legacy credit: `985306.2700`
- replay credit: `1364074.9700`
- credit difference: `378768.7000`
- legacy balance: `0.0000`
- replay balance: `54896.0000`

`Dania`, salary `202511`, branch `10`:

- legacy debit: `752453.0000`
- replay debit: `1402806.1300`
- debit difference: `650353.1300`
- legacy credit: `752453.0000`
- replay credit: `1348096.9300`
- credit difference: `595643.9300`
- replay balance: `54709.2000`

`Dania`, salary `202512`, branch `12`:

- legacy debit: `995822.0000`
- replay debit: `1701827.8600`
- debit difference: `706005.8600`
- legacy credit: `995822.0000`
- replay credit: `1608992.8600`
- credit difference: `613170.8600`
- replay balance: `92835.0000`

### Dominant Replay Gaps

Top account-level gaps for `202512`, branch `10` show that the remaining accounting mismatch is not generic debit/credit mechanics. It is project/account allocation semantics:

- `a3a1a7a1a39` - project salary account exists in legacy but is not yet replayed.
- `a3a1a7a1a49` - project salary account exists in legacy but is not yet replayed.
- `a3a1a7a1a1`, `a3a1a7a1a2`, `a3a1a7a1a3` - base project salary/allowance accounts are over-replayed.
- Employee accrued salary accounts such as `a2a2a4a1a281` and `a2a2a4a1a283` appear in replay where legacy routes differently for those rows.

This confirms the next parity target:

- `SystemOptions.ProjectEmployeeGV`
- `SystemOptions.ProjectDiscountPolicy`
- project salary account routing
- `GetComponentValuePerBranch`
- `GetComponentValuePerBranch(..., DepartmentID)`
- project/department allocation branches in `FrmEmpSalary5`

### Safety Status

Safety remains unchanged:

- replay is read-only and in-memory
- no `Notes` creation
- no `DOUBLE_ENTREY_VOUCHERS` creation
- no posting
- no payment marking
- no SendTopost replacement
- no allocation rebuild

The replay engine currently proves the remaining accounting gap is project/allocation routing, not the existence of salary/insurance/posting semantics. Posting remains blocked until replay totals, account distribution, branch distribution, department distribution, note counts, and line counts stabilize across repeated periods.

## Phase 8 - Allocation & Distribution Semantics Reconstruction

### Objective

Expanded the deterministic accounting replay from account/branch comparison into allocation-aware distribution replay. The engine now treats the dominant remaining gap as a routing problem, not a payroll-calculation problem.

The replay remains read-only and in-memory. It does not create `Notes`, `DOUBLE_ENTREY_VOUCHERS`, payment flags, SendTopost rows, or allocation rebuilds.

### VB6 Payroll Runtime Source Semantics Traced

Payroll/accounting runtime source of truth:

- `F:\Source Code\SatriahMain\Cayshny\Frm\FrmEmpSalary5.frm`
- `SystemOptions.ProjectEmployeeGV`
- `SystemOptions.ProjectDiscountPolicy`
- `SystemOptions.SalaryJLByManagement`
- `GetComponentValuePerBranch(BranchId, ComponentName, Optional DepartmentID)`
- `TblChangedComponentRegister`
- `TblChangedComponentRegisterDetails`
- `mofrad`
- `projects`
- `ModAccounts.AddNewDev`

Key reconstructed rules:

- When `ProjectEmployeeGV=True`, VB6 runs the project-aware salary journal path.
- `GetComponentValuePerBranch` aggregates the runtime grid by branch, and by department when `SalaryJLByManagement=True`.
- Project rows come from `TblChangedComponentRegisterDetails.projectid`.
- Addition project rows debit `projects.Salary_account` and credit `mofrad.Account_Code`.
- Deduction project rows debit `mofrad.Account_Code` and credit either `mofrad.Account_Code1` when `ProjectDiscountPolicy=1`, or `projects.Salary_account` otherwise.
- Missing project/component accounts are classified as fallback-account usage, not silently accepted.

### Web Replay Additions

Added allocation-aware diagnostic fields:

- `PayrollDistributionOptions`
- `PayrollDistributionMismatchSummary`
- `PayrollAccountingReplayReport.DepartmentComparisons`
- `PayrollAccountingReplayReport.ProjectComparisons`
- `PayrollAccountingReplayReport.DistributionMismatchCategories`
- `PayrollReplayedVoucherLine.ProjectId`
- `PayrollReplayedVoucherLine.AllocationSource`
- `PayrollReplayedVoucherLine.BranchProjectDepartmentPath`
- `PayrollReplayedVoucherLine.OverrideReason`
- `PayrollReplayedVoucherLine.FallbackReason`
- `PayrollReplayedVoucherLine.DistributionMismatchCategory`

Replay comparison now classifies distribution gaps as:

- `project routing mismatch`
- `department allocation mismatch`
- `branch override mismatch`
- `fallback account usage`
- `missing allocation policy`
- `employee accrued account conflict`

### Dania Runtime Probe

Read-only repository probe against `Dania`, period `2026/3`:

- `ProjectEmployeeGV=True`
- `ProjectDiscountPolicy=0`
- `SalaryJLByManagement=False`
- replay comparisons generated for account, branch, department, and project dimensions
- distribution categories were produced without writing accounting rows

Observed category sample:

- `missing allocation policy`
- `project routing mismatch`
- `branch override mismatch`
- `department allocation mismatch`
- `employee accrued account conflict`

This confirms the remaining parity work is now primarily allocation/project/distribution semantics.

### Safety Status

Unchanged:

- no payroll posting
- no salary payment posting
- no SendTopost replacement
- no allocation rebuild
- no `Notes` creation
- no `DOUBLE_ENTREY_VOUCHERS` creation

The replay layer is a financial digital twin for diagnostics only.

## Phase 8 - Allocation And Distribution Semantics Closure

### VB6 Branches Traced

Primary accounting replay source of truth:

- `F:\Source Code\SatriahMain\Cayshny\Frm\New frm\FrmEmpSalary5.frm`
- `F:\Source Code\SatriahMain\Cayshny\Bas\ModAccounts.bas`

Traced salary posting branches:

- `SystemOptions.ProjectEmployeeGV=True` switches the salary journal to project-aware routing.
- `SystemOptions.SalaryJLByManagement=True` makes `GetComponentValuePerBranch` filter by department as well as branch.
- `SystemOptions.ProjectDiscountPolicy=1` routes project deductions through `mofrad.Account_Code1`; otherwise VB6 falls back to the component/project salary account route.
- `GetComponentValuePerBranch(BranchId, componentname, DepartmentID)` reads the VB6 runtime grid values, which correspond to `emp_salary.Comp1..Comp40` when a salary snapshot exists.
- `ModAccounts.AddNewDev` writes `Credit_Or_Debit=0` as debit and `Credit_Or_Debit=1` as credit, with optional `project_id`, `branch_id`, `Departementid`, and `NEmpid`.

Important closure finding:

- The large component expense overstatement was caused by replaying current employee master component values when a legacy `emp_salary` snapshot existed.
- For salary posting parity, replay must use the snapshot/runtime-grid component value first. Current master salary components are only valid for reconstructed rows.
- `emp_salary.project_id` alone is not sufficient to reproduce the VB6 project reclassification branch. A naïve replay of every snapshot project component creates duplicate accounting totals. The exact VB6 source for those reclassification rows is narrower and appears to involve `ProJectMofrdSalar` and/or project operation allocation rows, not every `emp_salary.project_id` component.

### Account Routing Rules Implemented

Implemented in the read-only replay engine:

- Component expense debit now uses `emp_salary.CompN` for `LegacySnapshot` rows.
- Reconstructed rows continue to use compatibility-calculated source values.
- Project salary options are read from `TblOptions`:
  - `ProjectEmployeeGV`
  - `ProjectDiscountPolicy`
  - `SalaryJLByManagement`
- Replay lines now expose:
  - `RuleId`
  - account routing path
  - allocation source
  - branch/project/department path
  - fallback/override reason
  - distribution mismatch category
- Project snapshot reclassification was traced and modeled but not enabled as a blanket fix because Dania proved it duplicates VB6 totals.

### Dania Parity Results

All probes were read-only against `Dania`. No `Notes`, `DOUBLE_ENTREY_VOUCHERS`, salary paid flags, SendTopost rows, or allocation rebuilds were created.

| Period / Scope | Legacy Notes | Replay Notes | Legacy Debit | Replay Debit | Debit Diff | Legacy Credit | Replay Credit | Credit Diff | Replay Balance |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `202512`, branch `10` | 1 | 1 | 985,306.2700 | 1,333,381.0000 | 348,074.7300 | 985,306.2700 | 1,333,380.3000 | 348,074.0300 | 0.7000 |
| `202511`, branch `10` | 1 | 1 | 752,453.0000 | 1,313,568.0000 | 561,115.0000 | 752,453.0000 | 1,313,565.7800 | 561,112.7800 | 2.2200 |
| `202512`, branch `12` | 1 | 1 | 995,822.0000 | 1,560,258.0000 | 564,436.0000 | 995,822.0000 | 1,560,257.8600 | 564,435.8600 | 0.1400 |
| `202604`, all branches | 4 | 4 | 1,674,330.0000 | 3,103,849.4375 | 1,429,519.4375 | 1,674,330.0000 | 3,103,849.4375 | 1,429,519.4375 | 0.0000 |

Before this closure pass, the `202512` branch `10` replay was `1,388,277.0000` debit and `1,333,380.3000` credit. After switching legacy snapshot component debits to `emp_salary.CompN`, replay debit reduced to `1,333,381.0000` and the replay balance collapsed to `0.7000`.

### Remaining Mismatch Categories

Dominant categories after the fix:

- `project routing mismatch`
- `department allocation mismatch`
- `branch override mismatch`
- `missing allocation policy`
- `employee accrued account conflict`

Known remaining gaps:

- Project salary account reclassification is still not deterministic enough to enable posting.
- Legacy project-specific salary accounts such as `a3a1a7a1a39` and `a3a1a7a1a49` exist in VB6 vouchers, but blanket replay from `emp_salary.project_id` overstates totals.
- The next safe source to reconstruct is the exact VB6 project allocation data path around `ProJectMofrdSalar` / `opr_employee_details` and its period/employee filters.
- Replay line-count parity is still incomplete because VB6 emits project reclassification rows at a finer granularity than the currently enabled replay.

### Protected Test Posting Readiness

Status: **not ready**.

Reason:

- Read-only replay now balances almost exactly for the tested branch periods, but account distribution is still materially different from VB6.
- Project/account allocation routing is classified and narrowed, but not closed.
- Protected posting must remain blocked until project salary account routing and line-count parity stabilize across repeated periods.

Safety remains unchanged:

- no payroll posting
- no salary payment posting
- no SendTopost replacement
- no allocation rebuild
- no `Notes` creation
- no `DOUBLE_ENTREY_VOUCHERS` creation

## Phase 9 - Project Allocation Replay Closure

### Scope

This phase closed the next read-only parity gap around project allocation replay. It did not enable posting, salary payment, SendTopost replacement, allocation rebuild, `Notes` insertion, or `DOUBLE_ENTREY_VOUCHERS` insertion.

### VB6 Paths Traced

Source of truth remained Kishny VB6 only for project allocation replay and salary posting mechanics:

- `F:\Source Code\SatriahMain\Cayshny\Frm\New frm\FrmEmpSalary5.frm`
- `F:\Source Code\SatriahMain\Cayshny\Bas\ModAccounts.bas`

Traced branches and helpers:

- `SystemOptions.ProjectEmployeeGV`
- `SystemOptions.ProjectDiscountPolicy`
- `SystemOptions.SalaryJLByManagement`
- `GetComponentValuePerBranch(BranchId, componentname, Optional DepartmentID)`
- `ProJectMofrdSalar`
- `opr_employee_details`
- `mofrdat`
- `mofrad`
- `projects.Salary_account`
- `ModAccounts.AddNewDev`

Important VB6 line families:

- `FrmEmpSalary5.frm` around `4307-4393`: project-aware posting for runtime changed-component/project rows.
- `FrmEmpSalary5.frm` around `4501-4555`: `ProJectMofrdSalar` query, employee filtering through `empDes`, and VB6 30-day temporal calculation.
- `FrmEmpSalary5.frm` around `6222-6285`: `opr_employee_details` project distribution path.
- `ModAccounts.bas` around `968-1174`: `AddNewDev` parameter mapping, including `Credit_Or_Debit`, `project_id`, `branch_id`, `Departementid`, and `NEmpid`.

### Tables And Functions Inspected

Target database: `Dania`.

Inspected structures and runtime data:

- `ProJectMofrdSalar`
  - `EmpID`
  - `ProjID`
  - `MofrdID`
  - `Valuee`
  - `Total`
  - `NoDay`
  - `YearID`
  - `MonthID`
  - `pk_id`
  - `TypeSalary`
  - `FromDate`
  - `ToDate`
- `opr_employee_details`
- `opr_Employee`
- `emp_salary`
- `TblEmployee`
- `mofrdat`
- `mofrad`
- `projects`
- `Notes`
- `DOUBLE_ENTREY_VOUCHERS`

### Project Allocation Rules Discovered

VB6 does not safely support a blanket replay from `emp_salary.project_id`.

The accurate source is narrower:

- `ProJectMofrdSalar` is a project allocation snapshot table.
- Rows are tied to employee, project, component/mofrad, period, and date range.
- VB6 calculates project allocation days using 30-day month semantics around `FromDate` / `ToDate`.
- Addition components debit `projects.Salary_account` and credit `mofrad.Account_Code`.
- Deduction components debit `mofrad.Account_Code`.
- Deduction credit routing uses `projects.Salary_account`, unless `ProjectDiscountPolicy=1`, where `mofrad.Account_Code1` can override.
- `SalaryJLByManagement=True` carries department routing into `AddNewDev`; otherwise department is not applied to the project allocation line.

Critical Dania discovery:

- `ProJectMofrdSalar` contains rows for periods/projects that do not always appear in historical VB6 posted vouchers.
- Therefore the replay now uses a historical branch-signature guard in read-only parity mode:
  - if historical legacy vouchers exist for the same period/scope, a project salary account is replayed from `ProJectMofrdSalar` only when the source account footprint is also present in the VB6 voucher footprint within tolerance.
  - rows that exist in `ProJectMofrdSalar` but do not appear in the historical VB6 branch are classified as `missing VB6 branch`.

This is intentionally conservative. It prevents the replay engine from turning dormant allocation rows into fake accounting parity.

### Replay Fixes Implemented

Implemented in `Common/EmployeePayroll/EmployeePayrollRepository.cs`:

- Added read-only `ReplayProjectMofrdSalarDistribution`.
- Joined `ProJectMofrdSalar` to `emp_salary` using salary snapshot `sgn`.
- Joined `mofrdat -> mofrad` to recover component metadata and component accounts.
- Joined `projects` to recover `projects.Salary_account`.
- Preserved project/branch/department/employee trace metadata on every replayed line.
- Added line explainability:
  - `RuleId`
  - VB6 source branch
  - component id/name
  - project id
  - branch id
  - department id
  - selected account
  - debit/credit side
  - source amount
  - allocation source
  - fallback/override reason
- Added `ShouldReplayProjectMofrdAccount` guard for historical project-account footprint matching.
- Kept the broad `emp_salary.project_id` replay path dormant because Dania proved it duplicates totals.

### Dania Parity Results

All tests were read-only against `Dania`.

No `Notes`, `DOUBLE_ENTREY_VOUCHERS`, posting flags, salary payment flags, SendTopost rows, or allocation rebuilds were created.

Before = Phase 8 snapshot-first replay before guarded `ProJectMofrdSalar` replay.

After = Phase 9 guarded `ProJectMofrdSalar` replay.

| Period / Scope | Legacy Notes | Replay Notes | Legacy Lines | Replay Lines | Legacy Debit | Before Replay Debit | After Replay Debit | After Debit Diff | Legacy Credit | Before Replay Credit | After Replay Credit | After Credit Diff | Replay Balance |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `202512`, branch `10` | 1 | 1 | 642 | 464 | 985,306.2700 | 681,951.0000 | 940,261.0000 | -45,045.2700 | 985,306.2700 | 681,950.3000 | 940,260.3000 | -45,045.9700 | 0.7000 |
| `202511`, branch `10` | 1 | 1 | 435 | 278 | 752,453.0000 | 701,643.0000 | 701,643.0000 | -50,810.0000 | 752,453.0000 | 701,640.7800 | 701,640.7800 | -50,812.2200 | 2.2200 |
| `202512`, branch `12` | 1 | 1 | 426 | 329 | 995,822.0000 | 954,657.0000 | 954,657.0000 | -41,165.0000 | 995,822.0000 | 954,656.8600 | 954,656.8600 | -41,165.1400 | 0.1400 |
| `202604`, all branches | 4 | 4 | 666 | 535 | 1,674,330.0000 | 1,632,019.4375 | 1,632,019.4375 | -42,310.5625 | 1,674,330.0000 | 1,632,019.4375 | 1,632,019.4375 | -42,310.5625 | 0.0000 |

### Top Remaining Differences

`202512`, branch `10`:

- largest project salary account gaps are now small for the active project accounts:
  - project `118`: legacy debit `187,023.3100`, replay debit `187,158.0000`, diff `134.6900`
  - project `132`: legacy debit `37,773.1900`, replay debit `37,775.0000`, diff `1.8100`
  - project `129`: legacy debit `20,046.7700`, replay debit `20,047.0000`, diff `0.2300`
- project `122` exists in `ProJectMofrdSalar` but was not replayed because it does not match the VB6 voucher footprint for this salary note.

`202511`, branch `10`:

- `ProJectMofrdSalar` contains project allocation rows, but historical VB6 vouchers for this scope do not contain matching project salary account rows.
- Those rows are now classified as `missing VB6 branch` and are not replayed into accounting.

`202604`, all branches:

- `ProJectMofrdSalar` contains broad April allocation splits, but historical vouchers show only a narrow project footprint.
- The guarded replay blocks those broad allocations until the exact VB6 branch/selection state is reconstructed.

Dominant mismatch categories after Phase 9:

- `missing allocation policy`
- `project routing mismatch`
- `department allocation mismatch`
- `branch override mismatch`
- `employee accrued account conflict`
- `missing VB6 branch`

### Remaining Gaps

The replay is materially closer, especially for `202512` branch `10`, but Protected Test Posting Mode is still blocked.

Reasons:

- `opr_employee_details` branch is traced but not yet fully replayed.
- Historical `ProjectEmployeeGV` state is not stored in a clean snapshot table; Dania shows allocation rows that were not necessarily posted in the old vouchers.
- Department routing remains structurally different because legacy vouchers often carry department `0`, while the web replay has explicit department/null routing.
- Line-count parity is still not closed:
  - `202512` branch `10`: legacy `642`, replay `464`
  - `202511` branch `10`: legacy `435`, replay `278`
  - `202512` branch `12`: legacy `426`, replay `329`
  - `202604` all branches: legacy `666`, replay `535`
- Remaining employee-level account gaps are mostly around allowance/advance/accrued-salary granularity rather than project account existence.

### Readiness Decision

Status: **still blocked for Protected Test Posting Mode**.

The Phase 9 replay is stronger and safer:

- it is closer to VB6 where project salary posting clearly exists;
- it avoids false project allocations where Dania contains allocation rows but VB6 did not post them;
- it keeps every replay line explainable and classified.

However, test posting must remain disabled until:

- `opr_employee_details` replay is implemented or explicitly ruled out for each tested period;
- department/branch routing parity is stabilized;
- line-count differences are explained at voucher-line level;
- account-level gaps around employee accrued salary and component deductions are reduced or fully documented.

Safety rule remains unchanged:

- no payroll posting
- no salary payment posting
- no SendTopost replacement
- no allocation rebuild
- no `Notes` creation
- no `DOUBLE_ENTREY_VOUCHERS` creation

## Phase 10 - Legacy Consistency Validation

### Objective

This phase changes the validation stance from "VB6 is automatically correct" to "VB6 is historical evidence that must be classified." The replay engine is now strong enough that remaining gaps are not treated as missing implementation by default.

The phase remains read-only, replay-only, and diagnostic-only.

No posting, `Notes` creation, `DOUBLE_ENTREY_VOUCHERS` creation, SendTopost replacement, or allocation rebuild was enabled.

### Implementation Added

Added a legacy consistency trust model to the read-only accounting replay surface:

- `PayrollLegacyConsistencySummary`
- `PayrollAccountingReplayReport.LegacyConsistencySummaries`
- consistency fields on every replayed voucher line:
  - `LegacyBehaviorClassification`
  - `StabilityScore`
  - `HistoricalConsistencyScore`
  - `ReplayConfidenceScore`
  - `IsHistoricallyDeterministic`
  - `IsHistoricallyInconsistent`
  - `IsSafeForFuturePosting`
  - `LikelyLegacyBug`
  - `LikelyOperationalWorkaround`
  - `LegacyConsistencyExplanation`
- consistency fields on every replay comparison:
  - classification
  - stability scores
  - confidence scores
  - recommendation

The replay engine now exposes the distinction between:

- valid legacy behavior
- temporary compatibility behavior
- dormant legacy branch
- duplicated allocation behavior
- historically inconsistent behavior
- probable data pollution
- probable manual adjustment
- missing replay logic
- modern correction candidate
- requires business approval

### Historical Consistency Analysis

The read-only analysis compares historical `ProJectMofrdSalar` allocation footprint with posted salary voucher debit footprint across adjacent salary periods.

Checked dimensions:

- project allocation
- branch routing
- project salary accounts
- employee/project repeated behavior
- allocation rows without voucher footprint
- voucher footprint without allocation rows
- project account instability
- duplicated allocation behavior

The current SQL probe is deliberately conservative:

- it does not write data;
- it does not change replay totals;
- it does not force replay to imitate every old voucher;
- it classifies unstable history for review.

### Stable Branches

Stable or near-stable branches found:

- The salary snapshot branch is stable:
  - `emp_salary` remains the authoritative snapshot for payroll component values when available.
  - `LegacySnapshot` rows remain safe for preview and diagnostics.
- Component addition expense replay from snapshot `Comp1..Comp40` is deterministic.
- Project allocation via `ProJectMofrdSalar` is stable only when the project salary account has a repeated historical voucher footprint.
- For `202512`, branch `10`, project salary accounts are close after Phase 9:
  - project `118`: debit diff `134.6900`
  - project `132`: debit diff `1.8100`
  - project `129`: debit diff `0.2300`

Recommendation:

- preserve snapshot-first payroll preview;
- preserve `ProJectMofrdSalar` replay only conditionally where the historical footprint is stable.

### Unstable Branches

Historically unstable branches found:

- `ProJectMofrdSalar` rows exist in months where matching project salary voucher lines do not exist.
- Some project salary accounts appear in allocations for one period but are absent from the old posted voucher footprint.
- Department routing is inconsistent:
  - VB6 vouchers frequently use department `0`;
  - replay lines are able to carry explicit `DepartmentID` or `NULL`;
  - this should not be blindly normalized into a single posting rule without approval.
- Project-account behavior changes across periods:
  - `202512` branch `10` has active project salary footprints;
  - `202511` branch `10` has allocation rows but no matching project voucher footprint;
  - `202604` has wide allocation rows but narrow historical project voucher footprint.

Recommendation:

- replay conditionally;
- require business approval before promoting these paths to posting behavior.

### Dormant Branches

Dormant or partially dormant paths:

- `ProJectMofrdSalar` allocation rows that do not appear in historical voucher footprint are now classified as `dormant legacy branch` or `missing VB6 branch`.
- The broad `emp_salary.project_id` project replay remains intentionally dormant because it duplicated totals in Phase 8.
- `opr_employee_details` is traced but not yet promoted to replay. It remains a candidate branch pending voucher footprint validation.

Recommendation:

- deprecate broad `emp_salary.project_id` replay as a posting source;
- investigate `opr_employee_details` with the same consistency model before enabling any future posting.

### Duplicate Behaviors

Detected duplicate-risk behavior:

- `ProJectMofrdSalar` can overlap with project salary voucher rows, but not every allocation row becomes a posted voucher row.
- Blindly replaying all allocation rows overstated `202511` and `202604`.
- The Phase 9 guard prevented these duplicate/dormant paths from becoming replay totals.

Recommendation:

- keep duplicate-prone branches as diagnostic-only until business approves which source is authoritative.

### Likely VB6 Bugs Or Workarounds

Likely historical anomalies:

- voucher footprints without matching allocation source were classified as `probable manual adjustment`;
- allocation rows without voucher footprint were classified as dormant or partially-used legacy behavior;
- repeated department `0` usage suggests historical shortcut behavior rather than a clean department policy;
- period-specific project postings suggest operational selection state or manual intervention, not a globally deterministic rule.

This is important: these are not automatically replay defects.

Recommendation:

- preserve only financially explainable deterministic behavior;
- require business approval for historical anomaly preservation.

### Dania Consistency Results

Read-only validation was run against `Dania`.

| Period / Scope | Replay Balance | Consistency Rows | Dominant Legacy Classifications |
|---|---:|---:|---|
| `202512`, branch `10` | `0.7000` | 100 | probable manual adjustment, duplicated allocation behavior, historically inconsistent behavior, temporary compatibility behavior, dormant legacy branch |
| `202511`, branch `10` | `2.2200` | 100 | probable manual adjustment, duplicated allocation behavior, historically inconsistent behavior, temporary compatibility behavior, dormant legacy branch |
| `202512`, branch `12` | `0.1400` | 100 | probable manual adjustment, dormant legacy branch |
| `202604`, all branches | `0.0000` | 100 | probable manual adjustment, dormant legacy branch, historically inconsistent behavior, partially-used legacy path |

Interpretation:

- The replay remains balanced or near-balanced.
- Remaining differences are no longer simply "web is missing VB6 logic."
- Many differences are legacy consistency problems:
  - voucher exists without allocation source;
  - allocation exists without voucher;
  - project footprint changes by month;
  - department handling drifts.

### Replay Confidence Levels

Current confidence decisions:

| Branch / Rule | Confidence | Recommendation |
|---|---:|---|
| `emp_salary` snapshot component values | High | preserve |
| snapshot-first salary preview | High | preserve |
| `GetComponentValuePerBranch` from snapshot grid | High | preserve |
| `ProJectMofrdSalar` with matching repeated voucher footprint | Medium | replay conditionally |
| `ProJectMofrdSalar` without voucher footprint | Low | deprecate / require approval |
| broad `emp_salary.project_id` project replay | Low | deprecate |
| `opr_employee_details` project distribution | Unknown | require further validation |
| department routing from historical vouchers | Low/Medium | require business approval |
| voucher-only account differences | Low | probable manual adjustment |

### Safe Replay Principle

Going forward:

- If replay is balanced, deterministic, explainable, and repeatable, but VB6 historical data is inconsistent, the replay should not automatically be downgraded.
- VB6 historical output should be treated as evidence, not as unquestionable truth.
- Any anomaly preservation must be an explicit business decision.

### Protected Test Posting Readiness

Status: **still blocked**.

Reason:

- the system can now classify historical inconsistency, but business approval is needed before deciding which anomalies to preserve;
- `opr_employee_details` still needs consistency validation;
- department routing and voucher-only adjustments remain unresolved;
- line-count parity is still not stable enough for posting.

Recommended next decision path:

- preserve: snapshot-first payroll preview and component-level replay diagnostics;
- preserve temporarily: current read-only accounting replay;
- replay conditionally: `ProJectMofrdSalar` rows with stable historical footprint;
- deprecate: broad `emp_salary.project_id` project replay;
- require business approval: voucher-only/manual adjustment behavior and historically inconsistent project/dept routing;
- modernize safely: future posting engine should use deterministic, approved business rules rather than every historical VB6 anomaly.

## Phase 12 - Dual Area Integration

### Architecture Rule Confirmed

Source ownership is now explicit:

- `Main Original` remains the master ERP and HR/business source.
- `Kishny` remains an operational POS/cards specialization and a payroll/accounting runtime replay reference only where required.
- MainErp HR is not allowed to inherit Kishny HR behavior.
- POS is not allowed to become a second HR administration system.

### Shared Business Logic

The HR/payroll web layer now uses a shared module boundary:

- shared repository/model layer: `Common/EmployeePayroll`;
- MainErp shell: `Areas/MainErp/Controllers/EmployeePayrollController`;
- POS shell: `Areas/Pos/Controllers/EmployeePayrollController`;
- shared payroll preview and medical-insurance read models remain centralized through the repository instead of duplicated SQL in each area.

This keeps the runtime behavior reusable while allowing each area to expose a different operational surface.

### MainErp Screens

MainErp remains the full enterprise administration surface for:

- Employees;
- Salary Run;
- Banks;
- Boxes;
- MOFRAD;
- Advances;
- Leave Entitlements;
- Sick Leave;
- Employee Allocations;
- Changed Components;
- Medical Insurance administration;
- Payroll compatibility, explainability, and accounting replay diagnostics.

MainErp owns:

- add/edit/save workflows;
- HR administration;
- approvals;
- medical-insurance provider/plan administration;
- payroll preview administration;
- explainability and audit/replay surfaces.

### POS Screens

POS now exposes only operational entry points:

- employee quick lookup/profile visibility;
- payroll preview visibility;
- medical-insurance subscription/deduction visibility;
- operational links through the POS shell.

POS intentionally does not expose:

- full HR administration;
- employee create/edit/activation workflows;
- medical-insurance provider/plan administration;
- payroll draft saving;
- accounting replay administration;
- posting, Notes creation, or `DOUBLE_ENTREY_VOUCHERS` creation.

### Area-Specific UX

MainErp keeps the enterprise administration UX and full workflow depth.

POS uses the same data/repository behavior, but the UI is read-only for HR/payroll administration:

- POS employee screen has no new/save activation actions;
- POS salary run screen has preview only;
- POS medical insurance navigation opens reporting/visibility instead of settings;
- POS write endpoints return a protected operational-only response.

### Permission And Menu Integration

MainErp keeps full HR/payroll menu ownership.

POS menu now keeps lightweight entries only:

- employee operational profile;
- salary preview;
- medical-insurance reports/visibility.

Direct POS access to the old medical-insurance settings route is redirected to the read-only reports view.

### Protected Workflows

These remain intentionally blocked from POS:

- `SaveEmployee`;
- `SetEmployeeActive`;
- `SaveMedicalInsuranceProvider`;
- `SaveMedicalInsurancePlan`;
- `SaveSalaryRun`.

The block is deliberate. These workflows belong to MainErp because Main Original is the ERP/business authority.

### Remaining Gaps

- POS currently consumes the configured POS/Kishny connection for operational employee/payroll visibility. This remains acceptable for POS runtime visibility, but employee/HR ownership must be reconciled through Main Original governance before any POS write capability is considered.
- POS route smoke verification still needs browser/runtime execution after deployment because this phase changed routing and UI exposure.
- No new SQL schema or posting behavior was introduced in this phase.

### Readiness Status

Status: **dual-area shell integration ready for controlled QA**.

Not ready for:

- POS HR administration;
- POS payroll saving/posting;
- accounting posting;
- allocation rebuild;
- `SendTopost` replacement.

The architecture now reflects the intended product model:

- MainErp is the master enterprise HR/payroll administration system.
- POS is an operational specialization layer that can view relevant HR/payroll data without owning the business process.

## Phase 13 - Stabilization & QA Closure

Date: 2026-05-14

### Stabilization Scope

Phase 13 moved the HR/payroll module from integrated screens into a controlled QA state for MainErp and POS. The pass focused on:

- MainErp employee, salary preview, insurance visibility, legacy HR finance, and financial administration routes.
- POS employee/payroll/insurance operational visibility.
- shared payroll preview and replay API stability.
- permission-aware write protection.
- large payroll/replay payload handling.
- browser/runtime smoke checks.
- Dania runtime data validation.

No posting, payment, `Notes`, `DOUBLE_ENTREY_VOUCHERS`, `SendTopost`, or allocation rebuild behavior was enabled.

### Runtime Fixes Implemented

- MainErp payroll preview and replay endpoints now use large JSON responses for heavy Dania payloads:
  - `PreviewSalaryRun`
  - `PayrollCompatibilityParity`
  - `PayrollAccountingParityTrace`
  - `PayrollAccountingReplay`
- POS payroll preview now uses the same large JSON protection for preview visibility.
- MainErp and POS employee-payroll JavaScript now handles:
  - invalid/non-JSON server responses;
  - network/server failures;
  - unavailable write URLs in read-only POS views.
- MainErp replay rendering was stabilized so replay comparison rows render as cards instead of being injected as table rows into a non-table container.
- POS read-only mode now removes employee activation actions after grid render and changes row action affordance to view-only.
- POS protected write endpoints now return intentional operational-only responses instead of exposing administration behavior.
- POS permission/error messages were cleaned so protected Arabic messages do not display corrupted text.

### UI Improvements

- Added replay comparison card styling for readable accounting replay diagnostics.
- POS employee editor is disabled when opened from an operational read-only view.
- POS salary save/new/admin actions are guarded by both UI and server-side checks.
- MainErp salary preview keeps the compatibility status visible, including `LegacySnapshot` versus `Reconstructed`.
- Replay/explainability output is framed as a safety/audit feature rather than an incomplete posting feature.

### Dual-Area Integration Status

MainErp remains the full administration shell:

- employee administration;
- salary preview;
- payroll compatibility and accounting replay diagnostics;
- medical-insurance administration and reports;
- legacy HR finance screens;
- banks/boxes financial administration.

POS remains an operational shell only:

- employee quick profile and lookup;
- salary preview visibility;
- insurance reporting visibility.

POS intentionally does not expose:

- employee create/edit/activation;
- medical insurance provider/plan administration;
- salary run saving;
- replay administration;
- payroll/accounting posting.

### Dania Runtime Validation

Validation target: `Dania` on the configured SQL Server runtime environment.

Confirmed Dania data footprint:

- `TblEmployee`: 880 rows.
- `emp_salary`: 10,908 rows.
- `mofrad`: 41 rows.
- `TBLInsurancesJoin`: 38 rows.
- `Notes`: 80,331 rows.
- `DOUBLE_ENTREY_VOUCHERS`: 266,557 rows.
- `TblBoxesData`: 78 rows.

The `EmpInsurances` table is not present in Dania, so medical-insurance behavior continues to rely on the existing optional/fallback snapshot-aware path.

Runtime checks with `QA_HRFIN_VIEW` on `Dania`:

- `Lookups`: HTTP 200, `success=true`, 4,027 bytes.
- `Search`: HTTP 200, `success=true`, 177,664 bytes.
- `PreviewSalaryRun` for 2026/03: HTTP 200, `success=true`, 17,116,856 bytes.
- `PayrollCompatibilityParity` for 2026/03: HTTP 200, `success=true`, 488,932 bytes.
- `PayrollAccountingReplay` for 2026/03: HTTP 200, `success=true`, 851,749 bytes.

The previous salary preview 500 caused by JSON size limits is resolved.

### Permission Checks

QA users validated:

- `QA_HRFIN_VIEW`: can open employee/salary/reporting routes and read APIs.
- `QA_HRFIN_DENIED`: employee route is blocked with HTTP 403.
- `QA_HRFIN_ADD`: employee save endpoint is reachable but invalid empty payload returns HTTP 400 validation, not a permission bypass.
- `QA_HRFIN_EDIT`: employee save endpoint is blocked with HTTP 403 for add payload.

This confirms route and AJAX permission checks are active for the tested workflows.

### Browser Smoke Checks

IIS Express runtime: `http://localhost:63813`.

Browser checks found no console errors on:

- `/MainErp/EmployeePayroll/SalaryRun` unauthenticated redirect/login path.
- `/Pos/EmployeePayroll/MedicalInsurance` read-only reports exposure.
- `/Pos/EmployeePayroll/Employees` read-only employee visibility.
- `/Pos/EmployeePayroll/SalaryRun` preview-only salary run visibility.

POS checks confirmed:

- no employee new/save controls in operational employee screen;
- no salary save control in POS salary screen;
- medical-insurance route opens the reports/visibility experience, not administration.

### Build Status

`MSBuild MyERP.csproj /t:Build /p:Configuration=Debug` succeeded.

The build still emits existing legacy warnings across the broader project. No Phase 13 blocking compiler errors remain.

### Replay Safety Status

Replay remains read-only and non-posting.

Still blocked by design:

- payroll posting;
- salary payment posting;
- `Notes` creation;
- `DOUBLE_ENTREY_VOUCHERS` creation;
- `SendTopost` replacement;
- allocation rebuild.

The current recommendation remains: continue using payroll preview, compatibility diagnostics, and accounting replay as visibility/safety tooling until accounting parity and business approval are complete.

### Remaining Limitations

- POS runtime visibility still uses the configured POS/Kishny connection because POS is an operational specialization shell. This does not transfer HR ownership to Kishny.
- Medical-insurance Dania schema does not include `EmpInsurances`; fallback behavior is intentional and must remain documented.
- Full UI click-count and keyboard-speed assessment was smoke-tested only, not formally timed with operators.
- Accounting replay is stable enough for diagnostic QA, but not approved for posting or protected test posting.

### Readiness Decision

Status: **controlled QA ready**.

Ready for:

- executive/admin demonstration;
- MainErp HR/payroll preview QA;
- POS operational visibility QA;
- permission-edge validation;
- replay/explainability review.

Not ready for:

- payroll posting;
- accounting write-back;
- salary payment posting;
- allocation rebuild;
- replacing VB6 posting paths.

## Phase 14 - Operational Walkthrough & Workflow Hardening

Date: 2026-05-14

### Objective

Phase 14 shifted validation from developer QA to operational walkthrough hardening. The goal was to reduce friction for real users without changing the protected payroll/accounting safety model.

Validated personas:

- HR administrator.
- Payroll accountant.
- Branch operator.
- Financial reviewer.
- Manager/approver.
- POS operational user.

### Workflow Hardening Implemented

Employee management:

- Added an operational shortcut strip to the employee screen.
- Added `Ctrl + K` focus for employee quick search.
- Added a recent-employees strip using browser local storage for quick return to recently opened profiles.
- Recent employee entries open the employee profile directly.
- Employee profile selection now records recent profiles.
- `Escape` closes the employee editor.

Payroll preview:

- Added period navigation buttons:
  - previous month;
  - current month;
  - next month.
- Added keyboard shortcuts:
  - `Ctrl + Enter` for salary preview;
  - `Alt + P` for compatibility parity;
  - `Alt + R` for accounting replay.
- Added an operational shortcut/safety strip explaining that posting remains protected.
- Payroll preview and replay remain read-only and diagnostic.

Project extracts:

- Added an operational hints strip for search and tab navigation.
- Added numbered tab keyboard navigation for the extract workbench.
- Added a workflow status strip that separates:
  - draft/review;
  - financial review;
  - approval clarity;
  - read-only safety.
- Financial totals remain visible through the sticky summary area.

Letters of credit:

- Added operational hints for search and tab navigation.
- Added numbered tab keyboard navigation.
- Added a detailed LC operational status strip when a specific LC is selected, covering:
  - active/closed state;
  - missing accounts;
  - linked notes;
  - protected actions.
- Existing posting/rebuild actions remain guarded by permission and confirmation.

Shared UI:

- Added reusable operator-hint and workflow-status styling in the MainErp enterprise UI layer.
- Added employee/payroll operator-strip and recent-profile styling.
- No data model or posting behavior was changed.

### Runtime Walkthrough Results On Dania

Runtime: IIS Express `http://localhost:63813`.

Database: `Dania`.

MainErp authenticated checks:

- `/MainErp/EmployeePayroll/Employees`: HTTP 200, operator strip present.
- `/MainErp/EmployeePayroll/SalaryRun`: HTTP 200, operator strip present.
- `/MainErp/EmployeePayroll/MedicalInsuranceReports`: HTTP 200.
- `/MainErp/ProjectExtracts`: HTTP 200 as admin, operator hints and ops strip present.
- `/MainErp/LC`: HTTP 200 as admin, operator hints present. The ops strip appears after opening a specific LC detail context.

Permission boundary checks:

- `QA_HRFIN_VIEW` can open HR/payroll routes and APIs.
- `QA_HRFIN_VIEW` is correctly blocked from project extracts and LC routes with HTTP 403.
- This confirms the operational persona boundary is active.

Payroll APIs on Dania:

- Employee search: HTTP 200, `success=true`, 177,664 bytes.
- Salary preview 2026/03: HTTP 200, `success=true`, 17,116,856 bytes.
- Compatibility parity 2026/03: HTTP 200, `success=true`, 488,932 bytes.
- Accounting replay 2026/03: HTTP 200, `success=true`, 851,749 bytes.

Browser checks:

- Employee screen: no console errors, operator strip present.
- Salary run screen: no console errors, operator strip present.
- Project extracts screen: no console errors, operator hints and ops strip present.
- LC screen: no console errors, operator hints present.

### Operational Findings

Improved:

- HR administrator can jump directly to employee search and recently opened profiles.
- Payroll accountant can change periods faster and trigger preview/parity/replay without hunting for buttons.
- Financial reviewer sees clearer read-only/protected language on payroll and project extracts.
- Manager/approver has clearer status language for read-only, protected, active, and blocked workflows.
- POS remains operational-only and does not expose HR administration.

Remaining friction:

- LC operational status depends on selecting/opening a specific LC. The landing workbench still needs a clearer “select first LC” empty-state prompt in a later polish pass.
- Full mobile/tablet validation was limited to responsive CSS/build and smoke routing; operator tablet timing still needs live user observation.
- Project extract approval semantics remain mostly read-only/display-oriented; final approval workflow needs business confirmation before enabling write transitions.
- Payroll explainability is technically rich; accountant-facing wording should continue to be simplified during live walkthroughs.

### Safety Status

Still intentionally blocked:

- payroll posting;
- salary payment posting;
- `Notes` creation from payroll preview/replay;
- `DOUBLE_ENTREY_VOUCHERS` creation from payroll preview/replay;
- `SendTopost` replacement;
- payroll allocation rebuild.

No Phase 14 change weakened these protections.

### Readiness Decision

Status: **operational walkthrough ready**.

Recommended next step:

- Run a live walkthrough with one HR admin, one payroll accountant, one financial reviewer, and one POS operator.
- Record click-count pain points and terminology confusion.
- Use the current system for safe preview/review, not final posting.
## Phase 15 - Client-Facing Polish, Executive WOW UI, And Protected Test Posting

Date: 2026-05-14.

### Objective

Phase 15 moved the stabilized HR/payroll module into a client-facing trial state without enabling production posting.

The goal was not to close payroll parity. The goal was to make the system clear, impressive, explainable, and financially safe for an executive/admin demo.

### Implemented

- Added Protected Test Posting Mode to the MainErp Salary Run screen.
- Added three endpoints:
  - `PayrollTestPostingDryRun`;
  - `GeneratePayrollTestPosting`;
  - `CleanupPayrollTestPosting`.
- Added test posting model types:
  - `PayrollTestPostingRequest`;
  - `PayrollTestPostingCleanupRequest`;
  - `PayrollTestPostingResult`;
  - `PayrollTestPostingDimensionTotal`.
- Added SQL audit support:
  - runtime-created table `dbo.MainErpPayrollTestPostingAudit`;
  - script `Areas/MainErp/Sql/14_PayrollTestPostingMode.sql`.
- Added client-facing UI:
  - database allowlist status;
  - notes count;
  - voucher line count;
  - debit/credit balance;
  - affected accounts;
  - affected branches/projects/departments;
  - warning strip;
  - batch cleanup input.
- Reworded visible replay terminology to be finance-friendly:
  - `Reconstructed` becomes `Calculated preview`;
  - historical mismatch categories are framed as finance-review items instead of developer-only diagnostics.

### Safety Design

Protected Test Posting requires:

- an allowlisted database;
- configured password `PayrollTestPostingPassword`;
- exact confirmation phrase `POST TO TEST`;
- marked generated rows using `[TEST_PAYROLL_POSTING] Batch=<TestPostingBatchId>`;
- audit row in `MainErpPayrollTestPostingAudit`;
- cleanup by batch id only.

Production posting remains disabled.

Still blocked:

- payroll posting;
- salary payment posting;
- production `Notes` creation;
- production `DOUBLE_ENTREY_VOUCHERS` creation;
- `SendTopost` replacement;
- allocation rebuild.

### Dania Runtime Validation

Runtime: IIS Express `http://localhost:63735`.

Database: `Dania`.

Period tested:

- year `2026`;
- month `3`;
- all branches.

Results:

| Test | Result | Detail |
| --- | --- | --- |
| Build | PASS | `MyERP -> bin\MyERP.dll`; existing warnings only. |
| JS syntax | PASS | `node --check Areas/MainErp/Scripts/employee-payroll.js`. |
| Salary Run route | PASS | Browser opened route; no console errors. |
| Protected panel visible | PASS | Browser DOM contains `Protected Test Posting` and `Generate Test Posting`. |
| Dry-run | PASS | 4 notes, 572 voucher lines, debit 1,675,536.4375, credit 1,675,536.1075, balance 0.3300. |
| Generate test posting | PASS | Batch `8b432f82-c226-4e87-9368-4bd506a95ec7`; generated 4 notes and 572 voucher lines. |
| Cleanup by batch | PASS | Cleaned 4 notes and 572 voucher lines; remaining marked rows = 0. |

### Important Fix During QA

The first cleanup test exposed that SQL `LIKE '[TEST_PAYROLL_POSTING]...'` treats square brackets as a wildcard character class. Cleanup was corrected to use `CHARINDEX(@Marker, column) = 1`, then the initial test batch was safely cleaned by exact batch marker.

### Remaining Business Warning

The replay for `2026/03` is almost balanced but still has a 0.33 difference. This is acceptable for controlled test visibility only. It is not approval for production posting.

### Readiness Decision

Ready for controlled client trial/demo.

Not ready for production payroll posting.
