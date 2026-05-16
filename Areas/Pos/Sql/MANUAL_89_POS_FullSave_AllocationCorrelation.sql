/*
    Kishny POS - full save allocation/accounting correlation report.

    Read-only report. Run after a realistic POS save load test or after peak
    production incident collection.

    SQL Server compatibility: SQL Server 2012.
*/

SET NOCOUNT ON;

DECLARE @Since DATETIME;
SET @Since = DATEADD(HOUR, -6, GETDATE());

PRINT '=== POS save stage p95/p99 by stage ===';
;WITH x AS
(
    SELECT
        StageName,
        ServiceType,
        DurationMs,
        Success,
        ErrorNumber,
        ROW_NUMBER() OVER (PARTITION BY StageName, ServiceType ORDER BY DurationMs) AS rn,
        COUNT(*) OVER (PARTITION BY StageName, ServiceType) AS cnt
    FROM dbo.POS_SaveAllocationStageLog WITH (READCOMMITTEDLOCK)
    WHERE CreatedAt >= @Since
)
SELECT
    StageName,
    ServiceType,
    COUNT(*) AS samples,
    SUM(CASE WHEN Success = 1 THEN 1 ELSE 0 END) AS success_count,
    SUM(CASE WHEN ErrorNumber = 1205 THEN 1 ELSE 0 END) AS deadlock_count,
    AVG(DurationMs) AS avg_ms,
    MAX(DurationMs) AS max_ms,
    MIN(CASE WHEN rn >= CEILING(cnt * 0.95) THEN DurationMs END) AS p95_ms,
    MIN(CASE WHEN rn >= CEILING(cnt * 0.99) THEN DurationMs END) AS p99_ms
FROM x
GROUP BY StageName, ServiceType
ORDER BY p99_ms DESC, max_ms DESC;

PRINT '=== Slow saves > 2 seconds with slowest stage ===';
;WITH attempts AS
(
    SELECT
        SaveAttemptId,
        CreatedAt,
        UserID,
        BranchId,
        StoreID,
        BoxID,
        TransactionType,
        DurationMs AS SaveDurationMs,
        Transaction_ID,
        SqlErrorNumber,
        Status,
        RequestSummary
    FROM dbo.POS_SaveAttemptLog WITH (READCOMMITTEDLOCK)
    WHERE CreatedAt >= @Since
      AND EventName IN (N'Save.Success', N'Save.SqlException', N'Save.Exception')
      AND ISNULL(DurationMs, 0) > 2000
),
stage_rank AS
(
    SELECT
        a.SaveAttemptId,
        s.StageName,
        s.DurationMs,
        s.Detail,
        ROW_NUMBER() OVER (PARTITION BY a.SaveAttemptId ORDER BY s.DurationMs DESC, s.Id DESC) AS rn
    FROM attempts AS a
    LEFT JOIN dbo.POS_SaveAllocationStageLog AS s WITH (READCOMMITTEDLOCK)
        ON s.ClientRequestId = a.SaveAttemptId
        OR (s.Transaction_ID = a.Transaction_ID AND s.CreatedAt BETWEEN DATEADD(MINUTE, -2, a.CreatedAt) AND DATEADD(MINUTE, 2, a.CreatedAt))
)
SELECT
    a.CreatedAt,
    a.SaveAttemptId,
    a.Transaction_ID,
    a.TransactionType,
    IsCardFlow = CASE WHEN a.TransactionType = N'card' THEN 1 ELSE 0 END,
    a.BranchId,
    a.StoreID,
    a.BoxID,
    a.SaveDurationMs,
    SlowestStage = sr.StageName,
    SlowestStageMs = sr.DurationMs,
    sr.Detail,
    a.SqlErrorNumber,
    a.Status,
    a.RequestSummary
FROM attempts AS a
LEFT JOIN stage_rank AS sr ON sr.SaveAttemptId = a.SaveAttemptId AND sr.rn = 1
ORDER BY a.SaveDurationMs DESC;

PRINT '=== Slow saves grouped by service type and slowest stage ===';
;WITH attempts AS
(
    SELECT SaveAttemptId, TransactionType, DurationMs, Transaction_ID, CreatedAt
    FROM dbo.POS_SaveAttemptLog WITH (READCOMMITTEDLOCK)
    WHERE CreatedAt >= @Since
      AND EventName IN (N'Save.Success', N'Save.SqlException', N'Save.Exception')
      AND ISNULL(DurationMs, 0) > 2000
),
stage_rank AS
(
    SELECT
        a.SaveAttemptId,
        s.StageName,
        s.DurationMs,
        ROW_NUMBER() OVER (PARTITION BY a.SaveAttemptId ORDER BY s.DurationMs DESC, s.Id DESC) AS rn
    FROM attempts AS a
    LEFT JOIN dbo.POS_SaveAllocationStageLog AS s WITH (READCOMMITTEDLOCK)
        ON s.ClientRequestId = a.SaveAttemptId
        OR (s.Transaction_ID = a.Transaction_ID AND s.CreatedAt BETWEEN DATEADD(MINUTE, -2, a.CreatedAt) AND DATEADD(MINUTE, 2, a.CreatedAt))
)
SELECT
    a.TransactionType,
    SlowestStage = ISNULL(sr.StageName, N'<no stage log>'),
    COUNT(*) AS slow_saves,
    AVG(a.DurationMs) AS avg_save_ms,
    MAX(a.DurationMs) AS max_save_ms,
    AVG(ISNULL(sr.DurationMs, 0)) AS avg_slowest_stage_ms,
    MAX(ISNULL(sr.DurationMs, 0)) AS max_slowest_stage_ms
FROM attempts AS a
LEFT JOIN stage_rank AS sr ON sr.SaveAttemptId = a.SaveAttemptId AND sr.rn = 1
GROUP BY a.TransactionType, ISNULL(sr.StageName, N'<no stage log>')
ORDER BY slow_saves DESC, max_save_ms DESC;

PRINT '=== Accounting insert vs allocation correlation ===';
;WITH per_save AS
(
    SELECT
        ClientRequestId,
        Transaction_ID,
        ServiceType,
        MaxAccountingInsertMs = MAX(CASE WHEN StageName = N'Accounting insert' THEN DurationMs END),
        MaxDoubleEntryIdMs = MAX(CASE WHEN StageName = N'Double entry voucher ID allocation' THEN DurationMs END),
        MaxTransactionIdMs = MAX(CASE WHEN StageName = N'Transaction_ID allocation' THEN DurationMs END),
        MaxIssueVoucherMs = MAX(CASE WHEN StageName LIKE N'Issue voucher%' THEN DurationMs END),
        StageTotalMs = SUM(DurationMs)
    FROM dbo.POS_SaveAllocationStageLog WITH (READCOMMITTEDLOCK)
    WHERE CreatedAt >= @Since
    GROUP BY ClientRequestId, Transaction_ID, ServiceType
)
SELECT
    ServiceType,
    COUNT(*) AS saves,
    AVG(ISNULL(MaxAccountingInsertMs, 0)) AS avg_accounting_insert_ms,
    MAX(ISNULL(MaxAccountingInsertMs, 0)) AS max_accounting_insert_ms,
    AVG(ISNULL(MaxDoubleEntryIdMs, 0)) AS avg_double_entry_id_ms,
    MAX(ISNULL(MaxDoubleEntryIdMs, 0)) AS max_double_entry_id_ms,
    AVG(ISNULL(MaxTransactionIdMs, 0)) AS avg_transaction_id_ms,
    MAX(ISNULL(MaxTransactionIdMs, 0)) AS max_transaction_id_ms,
    SUM(CASE WHEN MaxIssueVoucherMs IS NOT NULL THEN 1 ELSE 0 END) AS issue_voucher_saves
FROM per_save
GROUP BY ServiceType
ORDER BY max_accounting_insert_ms DESC, max_double_entry_id_ms DESC;

PRINT '=== Duplicate submit / idempotency indicators ===';
SELECT
    SaveAttemptId,
    COUNT(*) AS attempt_rows,
    SUM(CASE WHEN EventName = N'Save.Success' THEN 1 ELSE 0 END) AS success_rows,
    SUM(CASE WHEN EventName = N'Save.SqlException' THEN 1 ELSE 0 END) AS sql_error_rows,
    MIN(CreatedAt) AS first_seen,
    MAX(CreatedAt) AS last_seen,
    MAX(DurationMs) AS max_duration_ms,
    MAX(Transaction_ID) AS transaction_id
FROM dbo.POS_SaveAttemptLog WITH (READCOMMITTEDLOCK)
WHERE CreatedAt >= @Since
GROUP BY SaveAttemptId
HAVING COUNT(*) > 1
ORDER BY max_duration_ms DESC, last_seen DESC;

PRINT '=== Full save p95/p99 versus stage p95/p99 ===';
;WITH save_ordered AS
(
    SELECT
        TransactionType,
        DurationMs,
        ROW_NUMBER() OVER (PARTITION BY TransactionType ORDER BY DurationMs) AS rn,
        COUNT(*) OVER (PARTITION BY TransactionType) AS cnt
    FROM dbo.POS_SaveAttemptLog WITH (READCOMMITTEDLOCK)
    WHERE CreatedAt >= @Since
      AND EventName = N'Save.Success'
      AND DurationMs IS NOT NULL
)
SELECT
    TransactionType,
    COUNT(*) AS saves,
    AVG(DurationMs) AS avg_save_ms,
    MAX(DurationMs) AS max_save_ms,
    MIN(CASE WHEN rn >= CEILING(cnt * 0.95) THEN DurationMs END) AS p95_save_ms,
    MIN(CASE WHEN rn >= CEILING(cnt * 0.99) THEN DurationMs END) AS p99_save_ms
FROM save_ordered
GROUP BY TransactionType
ORDER BY p99_save_ms DESC;
