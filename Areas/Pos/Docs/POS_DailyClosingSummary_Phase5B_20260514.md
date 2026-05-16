# POS Daily Closing Summary Phase 5B - 20260514

Scope: `Areas\Pos` only.

## What Was Added

SQL script:

- `Areas\Pos\Sql\82_POS_DailyClosingSummary_Prototype.sql`

Objects created by the script:

- `dbo.POS_DailyClosingSummary`
- `dbo.POS_DailyClosingSummary_RebuildLog`
- `dbo.usp_POS_RebuildDailyClosingSummary`
- `dbo.usp_POS_GetClosingSummary`

Manifest update:

- `Areas\Pos\Sql\POS_SQL_AutoUpdate_Manifest.json` includes script `82_POS_DailyClosingSummary_Prototype.sql`.

## Why This Is SQL-Only

This is a limited prototype. It does not change the POS report UI, `PosReportsController`, `PosSqlRepository`, invoice save logic, accounting logic, or `dbo.usp_POS_Report_RunClosing`.

The summary rebuild procedure uses the existing raw report procedure as the source of truth. This preserves current business totals while proving whether a daily summary table can safely reduce repeated report aggregation later.

## How To Rebuild Branch 22 For Last 7 Days

Run this manually on a test database first:

```sql
DECLARE @d DATE;
SET @d = DATEADD(DAY, -6, CAST(GETDATE() AS DATE));

WHILE @d <= CAST(GETDATE() AS DATE)
BEGIN
    EXEC dbo.usp_POS_RebuildDailyClosingSummary
        @SummaryDate = @d,
        @BranchId = 22,
        @UserId = NULL,
        @ForceRebuild = 1,
        @SummaryEngineVersion = 1;

    SET @d = DATEADD(DAY, 1, @d);
END;
```

This rebuilds only one branch and one day per procedure call. It does not backfill all branches or all history.

## How To Query The Summary

```sql
EXEC dbo.usp_POS_GetClosingSummary
    @FromDate = DATEADD(DAY, -6, CAST(GETDATE() AS DATE)),
    @ToDate = CAST(GETDATE() AS DATE),
    @BranchId = 22,
    @UserId = NULL,
    @ReportKey = NULL,
    @SummaryEngineVersion = 1;
```

## How To Compare Against The Raw Report

For `finance-closing`:

```sql
EXEC dbo.usp_POS_Report_RunClosing
    @reportKey = N'finance-closing',
    @fromDate = DATEADD(DAY, -6, CAST(GETDATE() AS DATE)),
    @toDate = CAST(GETDATE() AS DATE),
    @branchId = 22,
    @userId = 0,
    @canChangeDefaults = 1;
```

For `finance-closing-discounts`:

```sql
EXEC dbo.usp_POS_Report_RunClosing
    @reportKey = N'finance-closing-discounts',
    @fromDate = DATEADD(DAY, -6, CAST(GETDATE() AS DATE)),
    @toDate = CAST(GETDATE() AS DATE),
    @branchId = 22,
    @userId = 0,
    @canChangeDefaults = 1;
```

Use the summary totals from `dbo.usp_POS_GetClosingSummary` and compare:

- `TotalSupply`
- `TotalRechargeValue`
- `TotalRev`
- `TotalVat`
- `CashOutAmount` / raw cash-out equivalent
- `BoxBalance`
- `NoteValue`
- `CountCards`
- `CountTransaction`
- `TotalReturns`
- `ReturnsCount`

## Columns Fully Matched By Source

For `finance-closing`, these columns are populated directly from the current raw report output:

- `OpenBalance`
- `LastBalance`
- `TotalRechargeValue`
- `TotalRev`
- `TotalVat`
- `TotalSupply`
- `BoxBalance`
- `NoteValue`
- `CashOutAmount` from raw `CashOutTotal`

For `finance-closing-discounts`, these columns are populated directly from the current raw report output:

- `TotalSupply`
- `TotalRechargeValue`
- `TotalRevVat` from raw `TotalRevWithVat`
- `CashOutAmount` from raw `NetCashOut`
- `CardAmount` from raw `CardValue`
- `BoxBalance` from raw `BoxValue`
- `CountCards`
- `CountTransaction`
- `TotalReturns`
- `ReturnsCount`
- `ClosingStatus`

## Placeholder Columns

The prototype intentionally keeps some columns as placeholders because the raw closing report does not expose them directly or they need a separate business decision:

- `ViolationAmount = 0`
- `PaymentType = NULL`
- `CashBoxId = NULL`
- `StoreId = NULL`
- `TotalRevVat = 0` for `finance-closing`
- `TotalRev = 0` for `finance-closing-discounts`
- `NoteValue = 0` for `finance-closing-discounts`

These placeholders must not be used for UI replacement until a later phase confirms the desired formulas.

## Known Risks

- The rebuild procedure uses `INSERT EXEC` from `dbo.usp_POS_Report_RunClosing`. This is acceptable for prototype parity, but a future production version may prefer direct set-based summary logic.
- `finance-closing` source output does not include `BranchId`, so the prototype runs the raw procedure branch by branch and stores the known requested branch id.
- The summary table stores real branches only. It does not store a `BranchId = 0` all-branches row.
- User-level grain is limited. If `@UserId` is supplied, it stores that user id; otherwise `UserId` remains `NULL`.
- The prototype does not update after invoice save, cancellation, or accounting changes. A date/branch must be rebuilt to refresh values.
- The summary query does not fall back to raw reports. It reads only `dbo.POS_DailyClosingSummary`.

## Local SQL Test Result

Tested against local `Cash` database on 2026-05-14.

- `82_POS_DailyClosingSummary_Prototype.sql` applied successfully.
- Limited rebuild was run only for `BranchId = 22` and the last 7 days.
- Days without closing data inserted `0` rows.
- Days with closing data inserted 2 rows per day: one for `finance-closing`, one for `finance-closing-discounts`.
- Rebuild log status was `Succeeded` for each tested day.

Observed summary totals for BranchId `22`, last 7 days:

| ReportKey | Summary rows | TotalSupply | TotalRechargeValue | Net/Note value | CountCards | CountTransaction | TotalReturns |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| `finance-closing` | 3 | `66,781.619` | `61,300.000` | `NoteValue=69,157.699` | `0` | `0` | `0.000` |
| `finance-closing-discounts` | 3 | `66,873.719` | `61,300.000` | `NetAmount=66,873.719` | `5` | `44` | `0.000` |

These totals match the Phase 3 baseline for BranchId `22` / last 7 days for the comparable fields.

## Next Step Before UI Switch

Before any UI or repository switch:

1. Run limited rebuild for BranchId `22` and last 7 days.
2. Compare summary totals against `dbo.usp_POS_Report_RunClosing`.
3. Repeat for today and current month.
4. Repeat on a production-like copy with actual execution plans.
5. Decide placeholder formulas and user/all-user grain.
6. Only then introduce a wrapper procedure or repository switch, keeping the raw report as fallback.
