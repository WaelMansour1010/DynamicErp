# VB6 Vacation Workflow Audit - 2026-05-18

Reference source: `F:\Source Code\SatriahMain`

This audit intentionally documents the original VB6 behavior before any web implementation. It does not define new accounting or payroll behavior.

## VB6 Files Inspected

- `Frm\salah\02 02 2014\formvocationl.frm`: employee vacation request, saves `TblVocation`.
- `Frm\New frm\FrmEmpVacationSearch.frm`: vacation request search and approval filters.
- `Frm\New frm\FrmVocationEntitlements.frm`: vacation entitlement / vacation settlement / payment document, saves `TblVocationEntitlements`.
- `Frm\New frm\FrmInstalVacation.frm`: opening vacation balances, unpaid vacation opening, absence opening.
- `Frm\New frm\FrmLastVacation.frm`: old/last vacation history and entitlements import.
- `Frm\New frm\FrmVacationSettings.frm`: vacation accrual/settings periods.
- `Frm\New frm\FrmHolidaydata.frm`: employee departure/holiday registration, updates employee status and `TblEmpHolidaysDetails`.
- `Frm\New frm\FrmEmbarkation.frm`: return-to-work / embarkation, updates vacation actual return data.
- `Frm\New frm\FrmApprovalTransactions.frm`: generic approval updater for `TblVocation` and `TblVocationEntitlements`.
- `Bas\Salim.bas`: `CreateVacationData`.
- `Bas\SalimNew.bas`: unpaid vacation helpers and vacation settings helpers.

## Main Workflow

1. Employee contract / setup
   - `frmEmpVacancy.frm` saves contract data and calls `CreateVacationData(EmpID)`.
   - `CreateVacationData` deletes pending generated vacation rows for the employee where `Status1 IS NULL AND InstVacaID IS NULL`, then generates up to 20 future entitlement rows in `tblVacationData`.

2. Opening vacation balances
   - `FrmInstalVacation.frm` writes a header in `TblInstalVacation` and employee rows in `TblInstalVacationDet`.
   - For each employee row:
     - `VacBalance` creates an opening vacation balance in `tblVacationData`.
     - `VacWithoutSal` creates `TblInforVacatiom` with `TypeVacation = 0`.
     - `Abcence` creates `TblInforVacatiom` with `TypeVacation = 1`.
     - Employee fields updated: `BignDateWork`, `IssueDateH`, `lastHolidaydate`, `lastHolidaydateH`, `balanceH3`.

3. Vacation request
   - `formvocationl.frm` creates a request in `TblVocation`.
   - It records employee, branch, department, manager, from/to dates, return-to-work date, paid/unpaid flags, trip/visa options, vacation type, calculated days and balance fields.
   - Sending for approval uses `SendTopost Me.Name, "Tblvocation", "Id", dept, branch, id`.

4. Approval
   - Approval rows are stored in `ApprovalData`.
   - Approval definition comes from `TblApprovalDef` and `TblApprovalDefDetails`.
   - `FrmApprovalTransactions.frm` sets `TblVocation.Approved = 1` and/or `TblVocationEntitlements.Approved = 1`.
   - Search screen filters approval-pending documents with:
     - `ScreenSendAparoved(TblVocation.ID, screen) > 0`
     - `ScreenIsAparoved(TblVocation.ID, screen) IS NULL`

5. Vacation entitlement / dues
   - `FrmVocationEntitlements.frm` can be based on a vacation request (`BasedOn = 1`, `NoOrder = TblVocation.ID`).
   - It refuses a duplicate entitlement for the same `NoOrder`.
   - On save, it writes `TblVocationEntitlements` and detail rows in `TblVocationEntitlementsDet`.
   - It updates `TblVocation.FlagPayed = 1`.
   - It marks linked unpaid-vacation/return records in `TblEmbarkation` as paid when applicable.

6. Return-to-work / attendance interaction
   - `FrmEmbarkation.frm` records actual return-to-work.
   - It updates `TblVocationEntitlements`:
     - `AcuDate`, `AcuDateH`
     - `NoVacation`
     - `NoDayAct`
     - `NoDayDelay`
   - Delete/reversal resets those values back to null/zero.

7. Payroll interaction
   - Vacation entitlements can touch payroll rows by updating `emp_salary`.
   - Deleting a vacation entitlement loops over linked salary rows and clears:
     - `emp_salary.Payed`
     - `emp_salary.VocEntitID`
   - Vacation/payroll columns in `emp_salary` include `TotalVacValue`, `vacDay`, `VoCation`, `VoCation2`, `VoCation3`, `VoCation4`.
   - Exact payroll posting/accounting should not be implemented until the salary-side consumption is traced separately.

## DB / Table Map Confirmed From Cash

- `TblVocation`: vacation request header.
- `TblVocationEntitlements`: vacation settlement / dues document.
- `TblVocationEntitlementsDet`: entitlement component/detail rows and custody/asset rows.
- `tblVacationData`: generated vacation entitlement schedule and opening vacation balances.
- `TblInforVacatiom`: vacation/absence/unpaid day movements.
- `TblInstalVacation`: opening vacation balance header.
- `TblInstalVacationDet`: opening balance employee lines.
- `TblEmbarkation`: return-to-work / actual vacation return movement.
- `TblVacationSettings`, `TblVacationSettingsDet`: vacation accrual/settings periods.
- `tblHolidayData`, `TblEmpHolidaysDetails`: departure/holiday registration.
- `ApprovalData`, `TblApprovalDef`, `TblApprovalDefDetails`: approval workflow.
- `emp_salary`: payroll integration target.

## Balance Calculation Rules Found

1. Generated entitlement schedule
   - Starts from contract date.
   - Repeats up to 20 periods.
   - Due period:
     - `due_period = 0`: add months by `Due_period_no`.
     - `due_period = 1`: add years by `Due_period_no`.
     - `due_period = 2`: add days by `Due_period_no`.
   - Vacation value:
     - `Holiday_period = 0`: `Holiday_period_no`.
     - `Holiday_period = 1`: `Holiday_period_no * 30`.
     - `Holiday_period = 2`: legacy path is commented/unfinished.

2. Request/entitlement accrued days
   - If `TblVacationSettings.Typ = 1`, accrual is calculated from contract period rules.
   - If `TblVacationSettings.CommContract = 1`, previous remaining balance is included:
     - `NODiffDate = work-period-days + (last-balance-month * 30) - unpaid-deducted-days`
     - `ContDay = Round((holiday-days / period-days) * NODiffDate, 2)`
     - `LastBalanceMonth = Round((NODiffDate - period-days * completed-periods) / 30, 2)`
   - Otherwise:
     - `ContDay = Round((period-months / (period * 30)) * holiday-days, 2)`
   - If settings type is not active, system sums due rows from `tblVacationData` where:
     - `EmpID = employee`
     - `ExpectedacationDate <= vacation-start`
     - `Status1 IS NULL`

3. Unpaid vacation / absence balance
   - `TblInforVacatiom.TypeVacation = 0`: unpaid vacation/opening unpaid balance.
   - `TblInforVacatiom.TypeVacation = 1`: absence/deducted days.
   - Saved vacation entitlement writes negative rows into `TblInforVacatiom` for unpaid and absence deductions.

4. Unpaid vacation from return-to-work
   - `GetNoDayUnpadiVacation2` sums `TblEmbarkation.MoveVacBalance` where:
     - `TypeVacation = 1`
     - `VacationPaied IS NULL OR VacationPaied = 0`
     - `Emp_ID = employee`
     - optional `RdTypeVaction` split.

## Approval States

- Request/entitlement not sent: `posted IS NULL` or no `ApprovalData`.
- Sent for approval: `SendTopost` creates approval workflow rows.
- Current approval level: `ApprovalData.Currcursor = 1`.
- Approved: last approval row has `ApprovDate` and updater sets document `Approved = 1`.
- Search pending approval uses database functions `ScreenSendAparoved` and `ScreenIsAparoved`.

## Payroll Interaction Map

- Request alone does not post payroll.
- Entitlement/dues document is the payroll boundary:
  - `TblVocation.FlagPayed = 1` after entitlement save.
  - `TblEmbarkation.VacationPaied = 1` for linked unpaid vacation rows.
  - Delete reverses these flags.
  - `emp_salary` rows linked by `VocEntitID` are reset on delete.
- Payroll/accounting details require a separate salary-side trace before implementation.

## Employee Stop / Resign / Status Behavior

- Vacation request reads employee `jopstatusid`, `workstate`, `BignDateWork`, and `lastHolidaydate`.
- Return/holiday registration can set `TblEmployee.jopstatusid = 2` and update `lastHolidaydate`.
- Deleting entitlement in vacation mode sets `TblEmployee.jopstatusid = 1, workstate = 1`.
- Vacation alarm filters pending actual return where `TblVocationEntitlements.AcuDate IS NULL` and `TblEmployee.jopstatusid = GetHobStatus()`, where `GetHobStatus()` reads `jopstatus.Vacation = 1`.

## Holiday Overlap / Date Settings

- No strong overlap-prevention rule was confirmed in request save.
- Date validation confirms vacation end is not before start.
- `TblVacationSettingsDet` restricts allowed date windows:
  - `FrmDate <= RecDate <= ToDate`
  - `AlowDate >= RecDate` for allow checks.

## Branch / Security Filtering

- Request saves `BranchID` and `DeptID`.
- Approval sending includes department and branch.
- Approval definitions can include `BranchId` and `DepartmentID`.
- Search/filter screens support employee, department, date, approval state, and request type filters.

## Do Not Implement Yet

- Do not create payroll/accounting posting for vacations until salary-side posting is traced.
- Do not infer holiday overlap behavior beyond confirmed date/settings checks.
- Do not replace approval workflow with a new status enum without preserving `ApprovalData` and `SendTopost`.
