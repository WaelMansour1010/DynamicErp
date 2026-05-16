# POS Closing Reports Phase 3 Notes - 20260514

Scope: `Areas\Pos` only.

## Current Implementation Trace

- Screen: `/Pos/PosReports/Index`
- Buttons: `finance-closing`, `finance-closing-discounts`
- Controller: `Areas\Pos\Controllers\PosReportsController.cs`
  - `Run` and `Export` validate the request through `ValidateReportRequest`.
  - `LoadReportTable` routes closing report keys to `RunPosClosingReport`.
- Repository: `Areas\Pos\Data\PosSqlRepository.cs`
  - `RunPosClosingReport` calls `dbo.usp_POS_Report_RunClosing`.
  - Parameters passed: `@reportKey`, `@fromDate`, `@toDate`, `@branchId`, `@userId`, `@canChangeDefaults`, `@branchFromId`, `@branchToId`, `@showEmptyBranches`, `@serviceSearch`, `@filterUserId`.
- SQL script: `Areas\Pos\Sql\75_POS_ClosingReports_Performance.sql`
  - Uses `DROP PROCEDURE dbo.usp_POS_Report_RunClosing` then `CREATE PROCEDURE`.
  - Report keys handled: `finance-closing`, `finance-closing-discounts`.

## Procedure Review

The current procedure already uses sargable date ranges for the large report reads:

- `c.OrderDate >= @from AND c.OrderDate < @toExclusive`
- `t.Transaction_Date >= @from AND t.Transaction_Date < @toExclusive`

No `FORMAT` calls or `CONVERT(date, large_table_column)` filters were found in `75_POS_ClosingReports_Performance.sql`. The procedure computes date parameters once from input parameters, not from large-table columns. No `SELECT *` was found in the current closing report script.

All-branch mode is supported by passing `@branchId = 0`. Passing `NULL` is not the intended all-branch mode for the current procedure because predicates use `@branchId <= 0`.

## Baseline Script

Created:

- `Areas\Pos\Sql\81_POS_ClosingReports_BaselineCompare.sql`

The script is read-only and is not added to `POS_SQL_AutoUpdate_Manifest.json` because it is a diagnostic/baseline helper, not a deployment change.

It runs both closing report keys over:

- Today: `2026-05-14` to `2026-05-14`
- Last 7 days: `2026-05-08` to `2026-05-14`
- Current month: `2026-05-01` to `2026-05-14`

It tests:

- Selected branch: branch `22`, selected from `TBLClosePos` activity.
- All branches: `@branchId = 0`.

## Observed Baseline On Local Cash DB

| Report | Range | Branch mode | Rows | Key totals | Duration |
| --- | --- | --- | ---: | --- | ---: |
| `finance-closing` | Today | Branch 22 | 0 | no rows | 60 ms |
| `finance-closing` | Today | All branches | 0 | no rows | 20 ms |
| `finance-closing` | Last 7 days | Branch 22 | 3 | `TotalSupply=66,781.619`, `NoteValue=69,157.699` | 120 ms |
| `finance-closing` | Last 7 days | All branches | 234 | `TotalSupply=6,529,652.632`, `NoteValue=7,799,012.866` | 276 ms |
| `finance-closing` | Current month | Branch 22 | 8 | `TotalSupply=307,293.299`, `NoteValue=344,325.379` | 36 ms |
| `finance-closing` | Current month | All branches | 624 | `TotalSupply=18,670,696.505`, `NoteValue=24,068,401.539` | 46 ms |
| `finance-closing-discounts` | Today | Branch 22 | 0 | no rows | 50 ms |
| `finance-closing-discounts` | Today | All branches | 0 | no rows | 66 ms |
| `finance-closing-discounts` | Last 7 days | Branch 22 | 1 | `TotalSupply=66,873.719`, `CountTransaction=44` | 53 ms |
| `finance-closing-discounts` | Last 7 days | All branches | 83 | `TotalSupply=6,525,554.149`, `CountTransaction=3,284` | 36 ms |
| `finance-closing-discounts` | Current month | Branch 22 | 1 | `TotalSupply=307,827.495`, `CountTransaction=154` | 40 ms |
| `finance-closing-discounts` | Current month | All branches | 86 | `TotalSupply=18,655,976.828`, `CountTransaction=9,121` | 36 ms |

Local durations are not a production SLA because the local database is smaller and has broad report indexes already. The row counts still show the expected production risk: admin all-branch and broad date ranges are the cases to watch.

## Existing Index Coverage From Audit

Relevant existing indexes observed on the local `Cash` database:

- `TBLClosePos`: `IX_POS_TBLClosePos_Report_Branch_Order_User`, `IX_POS_TBLClosePos_Report_Order_Branch_User`, `IX_TBLClosePos_BranchID_OrderDate`, `IX_TBLClosePos_OrderDate`
- `Transactions`: `IX_POS_Transactions_Report_Returns`, `IX_POS_Transactions_Report_Returns_Branch`, `IX_POS_Transactions_Report_ServiceSearch`, `IX_Transactions_CloseReport`, `IX_Trans_Branch_Type_Date`, `IX_Transactions_BranchId_Transaction_Date_Transaction_Type`
- `Notes`: `IX_Notes_Close`, `IX_Notes_NoteDate_NoteType`, `IX_Notes_NoteDate_NoteType_branch_no`

## Decision

No stored procedure rewrite was made. The current `dbo.usp_POS_Report_RunClosing` is already sargable for the closing reports reviewed, uses explicit parameters, and has stable output columns.

No index script was created. The current evidence does not prove a missing index from an actual execution plan. Adding another index without a plan would increase write overhead on large production tables.

Output columns were not changed because `75_POS_ClosingReports_Performance.sql` was not modified.

Before/after duration is not applicable in this pass because the procedure was not changed. The measured durations above are current-procedure baseline timings.

## Verification

- `MyERP.csproj` build passed with `MSBuild` and no compile errors.
- Local IIS Express started against `.vs\MyERP\config\applicationhost.config`.
- Opening `http://localhost:63735/Pos/PosReports/Index` returned HTTP `302` to POS login because there was no authenticated POS session. This confirms the route is reachable but UI run/export/print actions need a valid POS login session to test interactively.
- SQL-level execution tested both `finance-closing` and `finance-closing-discounts` through `dbo.usp_POS_Report_RunClosing` for today, last 7 days, and current month.
- Phase 2 duplicate-click prevention remains present in `Views\PosReports\Index.cshtml` through `beginAction`, `endAction`, and `setAllReportButtonsDisabled`.

## Next Recommended Improvement

The next real performance improvement should be Phase 5 design for a daily closing summary table. That would reduce all-branch/month reporting cost without changing invoice save or accounting logic, provided the design includes rebuild/validation procedures and a comparison path against raw `Transactions`/`TBLClosePos`.
