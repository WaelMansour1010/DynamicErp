# POS Dashboard Daily Snapshots

This package implements the first approved KPI snapshot step only: **daily snapshots**.

## Apply Script

Run:

```sql
Areas/Pos/Sql/40_POS_Dashboard_DailySnapshots.sql
```

The script creates snapshot tables and two stored procedures:

```text
usp_POS_DashboardSnapshot_GenerateDaily
usp_POS_DashboardSnapshot_ReadDaily
```

## Generate One Day

```sql
EXEC dbo.usp_POS_DashboardSnapshot_GenerateDaily
    @snapshotDate = '2026-05-04',
    @branchId = NULL,
    @operationType = N'',
    @generatedByUserId = NULL;
```

## Read One Day

```sql
EXEC dbo.usp_POS_DashboardSnapshot_ReadDaily
    @snapshotDate = '2026-05-04',
    @branchId = NULL,
    @operationType = N'',
    @includeSmartMetrics = 1;
```

## Current Rules

- Daily snapshots only.
- Past daily dashboard periods read from the snapshot tables.
- If a snapshot is missing, the dashboard shows a missing-state message and the admin can refresh/generate it.
- Today/current open day remains live and is not precomputed automatically.
- Weekly/monthly/yearly are deferred and must later aggregate from verified daily snapshots.

## Verification Rule

Before approving daily snapshots in production, compare:

```sql
EXEC dbo.usp_POS_Dashboard_Summary
    @fromDate = 'YYYY-MM-DD',
    @toDate = 'YYYY-MM-DD',
    @previousFromDate = DATEADD(DAY, -1, 'YYYY-MM-DD'),
    @previousToDate = DATEADD(DAY, -1, 'YYYY-MM-DD'),
    @branchId = NULL,
    @operationType = N'';
```

against:

```sql
EXEC dbo.usp_POS_DashboardSnapshot_ReadDaily
    @snapshotDate = 'YYYY-MM-DD',
    @branchId = NULL,
    @operationType = N'',
    @includeSmartMetrics = 1;
```

The current and previous KPI resultsets must match before moving to weekly/monthly/yearly.
