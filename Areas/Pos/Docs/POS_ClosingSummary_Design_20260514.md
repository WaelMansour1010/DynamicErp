# POS Closing Summary Design - 20260514

Scope: `Areas\Pos` only. This document is design-only; it does not create tables, procedures, jobs, or UI changes.

## 1. Current Problem

The risky reports are the POS closing reports behind `/Pos/PosReports/Index`:

- `finance-closing`
- `finance-closing-discounts`

Both reports currently route through `dbo.usp_POS_Report_RunClosing` from `Areas\Pos\Sql\75_POS_ClosingReports_Performance.sql`. Phase 3 confirmed the procedure is already reasonably shaped: it uses explicit parameters and sargable date ranges for `TBLClosePos.OrderDate` and `Transactions.Transaction_Date`.

The remaining problem is not a bad single query pattern as much as repeated aggregation over large production tables, especially when admins run all-branch reports for a week, month, or older periods. The raw tables repeatedly read or joined by the closing/report flow are:

- `Transactions`
- `Transaction_Details`
- `TBLClosePos`
- `Notes`
- `DOUBLE_ENTREY_VOUCHERS`

Indexes alone are not the best next step because local audit already found broad index coverage for the current access paths. Adding more indexes without an actual execution plan would add write overhead to hot transaction/accounting tables and may still not solve the repeated all-branch/month aggregation cost. A daily summary table moves most repeated reads from millions of raw rows to a small number of daily branch/user/service rows.

## 2. Proposed Summary Table

Suggested table:

`dbo.POS_DailyClosingSummary`

Recommended grain:

- `SummaryDate` date-equivalent stored as `DATE` if compatibility level allows it, or `DATETIME` with midnight value if the target DB standard prefers it.
- `BranchId`
- `UserId` nullable or `0` for all-user total, decision required below.
- `ReportKey`, for example `finance-closing` or `finance-closing-discounts`.
- `ServiceType`, such as `card`, `cash-in`, `cash-out`, `violations`, or `all`.
- `PaymentType` nullable.
- `CashBoxId` nullable, useful if teller/cashbox closing is later required.
- `StoreId` nullable, useful only if card/store drilldown becomes part of closing.

Recommended columns:

| Column | Purpose |
| --- | --- |
| `SummaryDate` | Daily summary date. |
| `BranchId` | Branch being summarized. Do not store all branches as one row in first version. |
| `UserId` | User/cashier dimension when needed; otherwise `0` or nullable all-user row. |
| `ReportKey` | Keeps outputs aligned with existing report variants. |
| `ServiceType` | Allows service-level summaries without re-reading raw transactions. |
| `PaymentType` | Optional split when payment mode matters. |
| `CashBoxId` | Optional split for cashbox-aware closing. |
| `StoreId` | Optional split for store/card analysis. |
| `InvoiceCount` | Count of invoices/transactions included. |
| `TotalAmount` | Gross operational amount before returns/discounts where applicable. |
| `DiscountAmount` | Discounts/fees reductions, including closing discount logic where applicable. |
| `ReturnAmount` | Returns total using the same rules as `dbo.usp_POS_Report_RunClosing`. |
| `NetAmount` | Final net total used for report rollup. |
| `CashInAmount` | Cash-in/recharge amount. |
| `CashOutAmount` | Cash-out amount or wallet cash-out total. |
| `CardAmount` | Card issuance/sale amount. |
| `ViolationAmount` | Traffic/violation service amount if present. |
| `OpenBalance` | Opening balance if needed to match `finance-closing`. |
| `LastBalance` | Closing/end balance if needed to match `finance-closing`. |
| `TotalRechargeValue` | Matches existing report column. |
| `TotalRev` | Existing revenue/fees value. |
| `TotalVat` | VAT total where currently reported. |
| `TotalRevVat` | VAT on fees where currently reported. |
| `TotalSupply` | Existing closing supply total. |
| `BoxBalance` | Existing box/custody balance. |
| `NoteValue` | Accounting note value when `Notes` is part of the output. |
| `ClosingStatus` | Open/closed/partial state for branch/date. |
| `CreatedAt` | Row creation timestamp. |
| `UpdatedAt` | Last rebuild timestamp. |
| `SourceMinTransactionId` | Minimum source transaction id included. |
| `SourceMaxTransactionId` | Maximum source transaction id included. |
| `RebuildVersion` | Incrementing version or batch id for rebuild traceability. |

Recommended first key shape:

`SummaryDate, BranchId, UserId, ReportKey, ServiceType, PaymentType, CashBoxId, StoreId`

For version 1, keep `CashBoxId`, `StoreId`, and `PaymentType` nullable and mostly `NULL` unless the existing report logic proves they are required. Avoid over-graining the first version.

## 3. Rebuild Stored Procedure Design

Design procedure:

`dbo.usp_POS_RebuildDailyClosingSummary`

Parameters:

- `@SummaryDate date`
- `@BranchId int = NULL`
- `@UserId int = NULL`
- `@ForceRebuild bit = 0`

SQL Server 2012 compatibility note: SQL Server 2012 supports the `date` type. Internally, the procedure should still compute:

- `@FromDate = CAST(@SummaryDate AS datetime)`
- `@ToExclusive = DATEADD(DAY, 1, @FromDate)`

Expected behavior:

- Rebuilds one day only.
- Optional `@BranchId` limits rebuild to one branch; `NULL` rebuilds all branches that have source data for that date.
- Optional `@UserId` limits rebuild to one user/cashier when user-level grain is enabled.
- Uses one explicit transaction.
- Deletes/rebuilds only the target day and optional branch/user/report scope.
- Reads raw tables once per target branch/day as much as possible.
- Inserts fresh summary rows, or performs update/insert if delete-first is not acceptable.
- Records a rebuild batch/version and timestamps.
- Logs result per day/branch in a future rebuild log table design, for example rows processed, duration, and status.

Handling cancelled invoices:

- The rebuild must copy the existing report rules exactly. If cancellation is represented by `Transactions.IsCancelled`, `Transaction_Type`, `NoteID`, or another field, the procedure must use the same include/exclude logic as `dbo.usp_POS_Report_RunClosing`.
- If cancellation happens after a summary row was built, the affected date/branch/user must be marked stale or rebuilt before the summary is trusted.

Handling returns and discounts:

- Returns must match the existing logic in `finance-closing-discounts`, including `Transaction_Type = 9` and date/branch/user filters.
- Discount and cash-out calculations must use the same definitions as `TBLClosePos` and the current closing report columns. Do not invent new business formulas in the summary layer.

Implementation outline for a later phase:

1. Validate parameters.
2. Resolve target branch list into a temp table.
3. Begin transaction.
4. Delete existing summary rows for `SummaryDate` and target branch/user scope when `@ForceRebuild = 1`, or when no successful current-version row exists.
5. Read `TBLClosePos` for closing rows once.
6. Read `Transactions` returns/operational rows once with sargable date range.
7. Optionally join `Notes` and `DOUBLE_ENTREY_VOUCHERS` only if accounting values are included in version 1.
8. Insert summary rows at the chosen grain.
9. Commit transaction.
10. Return rebuild status rows.

## 4. Query Stored Procedure Design

Design procedure:

`dbo.usp_POS_GetClosingSummary`

Parameters:

- `@FromDate date`
- `@ToDate date`
- `@BranchId int = NULL`
- `@UserId int = NULL`
- `@ReportKey nvarchar(50) = NULL`

Expected behavior:

- Reads `dbo.POS_DailyClosingSummary` only for dates already summarized.
- Aggregates summary rows over the requested date range.
- Supports selected branch and all-branch behavior without using `BranchId = 0` inside the summary table. In the query API, `@BranchId = NULL` should mean all branches; the existing report wrapper can translate old `0` behavior if needed.
- Returns output columns that match `dbo.usp_POS_Report_RunClosing` for the report key being requested.
- For `finance-closing`, produce branch/date/note-level columns only if summary stores enough note detail; otherwise use summary for totals and keep raw fallback for detailed rows until parity is complete.
- For `finance-closing-discounts`, aggregate daily branch summaries into the existing rollup columns: `TotalSupply`, `CountCards`, `CardValue`, `CountTransaction`, `WalletBalance`, `WalletSupply`, `BankBalanceCharge`, `TotalRechargeValue`, `TotalRev2`, `TotalRevWithVat`, `ReturnsCount`, `TotalReturns`, `NetCashOut`, `BoxValue`, and `ClosingStatus`.

Fallback recommendation:

- Use summary rows for dates with complete successful rebuilds.
- Fall back to `dbo.usp_POS_Report_RunClosing` for dates not yet summarized or marked stale.
- Keep the fallback inside a wrapper procedure rather than the UI. The web code should continue calling one repository method.

Output parity:

- The final wrapper output should match `dbo.usp_POS_Report_RunClosing` column names and data types as closely as possible.
- No UI change should be required initially.

## 5. Backfill Plan

Safe backfill order:

1. Backfill last 7 days.
2. Backfill current month.
3. Backfill previous 3 months.
4. Backfill older periods gradually, month by month or week by week depending on production load.

Operating rules:

- Run outside working hours.
- Rebuild one day/branch at a time.
- Log every day/branch rebuild result: start time, end time, duration, rows inserted, source min/max transaction id, and success/failure message.
- Stop on repeated failures for the same branch/date and leave raw report fallback enabled.
- Use lower transaction batch scope: one date/branch commit is safer than one large all-history transaction.

## 6. Validation Plan

Validation strategy:

- Reuse the approach from `Areas\Pos\Sql\81_POS_ClosingReports_BaselineCompare.sql`.
- Compare summary output vs `dbo.usp_POS_Report_RunClosing`.
- Test:
  - today
  - last 7 days
  - current month
  - one selected branch
  - all branches
  - both `finance-closing` and `finance-closing-discounts`

Compare fields:

- row counts
- `TotalSupply`
- `TotalRechargeValue`
- `TotalRev`
- `TotalVat`
- `CashOutTotal`
- `BoxBalance`
- `NoteValue`
- `CountCards`
- `CountTransaction`
- `TotalReturns`

Acceptance:

- Totals must match exactly where the old report uses the same precision.
- Tiny rounding differences may be accepted only if caused by deliberate rounding to the same final report scale, for example `DECIMAL(18,3)`, and must be documented.
- Any unexplained difference means the summary is not allowed to replace the raw report for that date/report key.

## 7. Integration Plan

Later code/SQL integration points:

- `Areas\Pos\Data\PosSqlRepository.cs`
  - `RunPosClosingReport` can continue to expose the same C# method.
  - The command target can later switch to a wrapper procedure, for example `dbo.usp_POS_Report_RunClosing_WithSummary`, after validation.
- `dbo.usp_POS_Report_RunClosing`
  - Option A: leave it as the raw fallback and create a new wrapper procedure that uses summary first.
  - Option B: change this procedure internally to use summary when available and raw logic when not.
- UI:
  - No UI change initially.
  - Existing Phase 2 protections remain useful because even summary queries should not run repeatedly from duplicate clicks.

Recommended integration path:

1. Add summary table and rebuild/query procedures in a future SQL script.
2. Run backfill and validation.
3. Add wrapper procedure.
4. Switch `RunPosClosingReport` to wrapper only after parity is proven.
5. Keep raw procedure available as fallback for at least one full close/reporting cycle.

## 8. Risks And Decisions Needed

`UserId` grain:

- Decision needed: include per-user rows, all-user rows, or both.
- Recommendation: store per-user rows plus an all-user row only if query performance requires it. Otherwise query can aggregate per-user rows.

All-branch representation:

- Existing `dbo.usp_POS_Report_RunClosing` treats `@branchId <= 0` as all branches.
- New summary table should not store `BranchId = 0` as an all-branch row in version 1. Store real branches only. Query procedures can interpret `@BranchId = NULL` or incoming `0` as all branches.

Edited/cancelled invoices after summary:

- Any invoice edit/cancellation affecting a summarized date must trigger a rebuild or stale marker.
- First version should avoid per-invoice summary maintenance and instead rebuild affected dates on a schedule or on demand.

Rebuild trigger:

- Options:
  - SQL Agent job after closing time.
  - On-demand rebuild when opening/running closing report for today.
  - Rebuild after each invoice save.
- Recommendation: do not rebuild per invoice in version 1. It increases save-path risk and touches business-critical invoice flow.

Accounting source decision:

- Decision needed: should summary include accounting values from `Notes` and `DOUBLE_ENTREY_VOUCHERS`, or only operational values from `TBLClosePos`/`Transactions`?
- Recommendation: version 1 should include the values needed to match current closing reports from `TBLClosePos` and `Notes`. Add `DOUBLE_ENTREY_VOUCHERS` detail only if a report output or validation requirement explicitly needs voucher line/account details.

Concurrency and locking:

- Rebuild should not block invoice saves for long periods.
- Use date/branch scoped transactions.
- Avoid holding locks while processing many days.

Data correction risk:

- Old data may have inconsistent closing rows or missing notes.
- Summary rows should carry rebuild version and source range so mismatches can be traced.

## 9. Recommendation

Start with a daily rebuild job after closing time. Also allow rebuilding today on demand when the closing report is opened or executed, but only for the requested branch/date and only outside invoice save/accounting logic.

Do not update the summary per invoice in the first version. Keep the old raw report as fallback until validation passes for today, last 7 days, current month, one branch, and all branches.

The first implementation phase after this design should create the table, rebuild procedure, query procedure, and validation script only. The web report should switch to summary-backed output only after totals match the existing `dbo.usp_POS_Report_RunClosing` for a representative production-like dataset.

