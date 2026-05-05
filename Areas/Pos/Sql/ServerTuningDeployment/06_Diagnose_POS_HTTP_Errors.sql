/*
    Diagnose recent POS HTTP/API errors from the POS system error log.
    Run against the POS database (Byte).
*/

USE [Byte];
GO

IF OBJECT_ID(N'dbo.POS_SystemErrorLog', N'U') IS NULL
BEGIN
    SELECT N'POS_SystemErrorLog table does not exist. Run Areas/Pos/Sql/37_POS_SystemErrorLog.sql first.' AS Message;
    RETURN;
END;
GO

DECLARE @FromDate DATETIME;
SET @FromDate = DATEADD(HOUR, -6, GETDATE());

SELECT TOP (100)
    CreatedAt,
    Severity,
    Status,
    UserId,
    UserName,
    BranchId,
    ScreenName,
    ActionName,
    OperationType,
    TransactionId,
    ErrorMessage,
    RequestSummary,
    IpAddress,
    LEFT(StackTrace, 2000) AS StackTraceStart
FROM dbo.POS_SystemErrorLog
WHERE CreatedAt >= @FromDate
ORDER BY CreatedAt DESC;
GO

SELECT
    ScreenName,
    ActionName,
    Status,
    Severity,
    COUNT(*) AS ErrorCount,
    MAX(CreatedAt) AS LastSeen
FROM dbo.POS_SystemErrorLog
WHERE CreatedAt >= DATEADD(HOUR, -6, GETDATE())
GROUP BY ScreenName, ActionName, Status, Severity
ORDER BY ErrorCount DESC, LastSeen DESC;
GO

