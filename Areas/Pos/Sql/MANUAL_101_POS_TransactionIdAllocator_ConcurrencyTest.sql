/*
    MANUAL_101_POS_TransactionIdAllocator_ConcurrencyTest.sql

    Purpose:
      Manual concurrency probe for dbo.usp_POS_AllocateTransactionId.

    Important:
      - This script consumes Transaction_ID values by design.
      - Prefer running on Cash/test database, not production.
      - To test concurrency, open multiple SSMS/sqlcmd sessions and run the worker
        block at the same time with the same @BatchId.
*/

USE [Byte];
GO

IF OBJECT_ID(N'dbo.POS_TransactionIdAllocatorTestLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_TransactionIdAllocatorTestLog
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_TransactionIdAllocatorTestLog PRIMARY KEY,
        BatchId UNIQUEIDENTIFIER NOT NULL,
        SessionId INT NOT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_POS_TransactionIdAllocatorTestLog_CreatedAt DEFAULT(GETDATE()),
        AllocatedTransactionId BIGINT NULL,
        Success BIT NOT NULL,
        ErrorMessage NVARCHAR(1000) NULL
    );
END;
GO

/*
    Worker block:
    1. Set the same @BatchId in every parallel SSMS window.
    2. Run this block from 10-20 windows together.
*/
DECLARE @BatchId UNIQUEIDENTIFIER;
DECLARE @Iterations INT;
DECLARE @i INT;
DECLARE @NextValue BIGINT;
DECLARE @ErrorMsg NVARCHAR(500);
DECLARE @ReturnCode INT;

SET @BatchId = '00000000-0000-0000-0000-000000000101'; -- replace per test run
SET @Iterations = 100;
SET @i = 1;

WHILE @i <= @Iterations
BEGIN
    SET @NextValue = NULL;
    SET @ErrorMsg = NULL;

    BEGIN TRY
        EXEC @ReturnCode = dbo.usp_POS_AllocateTransactionId
            @NextValue = @NextValue OUTPUT,
            @ErrorMsg = @ErrorMsg OUTPUT;

        INSERT dbo.POS_TransactionIdAllocatorTestLog
        (
            BatchId,
            SessionId,
            AllocatedTransactionId,
            Success,
            ErrorMessage
        )
        VALUES
        (
            @BatchId,
            @@SPID,
            @NextValue,
            CASE WHEN @ReturnCode = 0 AND @NextValue IS NOT NULL AND @ErrorMsg IS NULL THEN 1 ELSE 0 END,
            @ErrorMsg
        );
    END TRY
    BEGIN CATCH
        INSERT dbo.POS_TransactionIdAllocatorTestLog
        (
            BatchId,
            SessionId,
            AllocatedTransactionId,
            Success,
            ErrorMessage
        )
        VALUES
        (
            @BatchId,
            @@SPID,
            @NextValue,
            0,
            ERROR_MESSAGE()
        );
    END CATCH;

    SET @i = @i + 1;
END;
GO

/*
    Result check:
*/
DECLARE @BatchId UNIQUEIDENTIFIER;
SET @BatchId = '00000000-0000-0000-0000-000000000101'; -- same value used above

SELECT
    BatchId,
    TotalRows = COUNT(1),
    SuccessRows = SUM(CASE WHEN Success = 1 THEN 1 ELSE 0 END),
    FailedRows = SUM(CASE WHEN Success = 0 THEN 1 ELSE 0 END),
    DistinctAllocatedIds = COUNT(DISTINCT AllocatedTransactionId),
    DuplicateAllocatedIds = COUNT(AllocatedTransactionId) - COUNT(DISTINCT AllocatedTransactionId)
FROM dbo.POS_TransactionIdAllocatorTestLog
WHERE BatchId = @BatchId
GROUP BY BatchId;

SELECT
    AllocatedTransactionId,
    DuplicateCount = COUNT(1)
FROM dbo.POS_TransactionIdAllocatorTestLog
WHERE BatchId = @BatchId
  AND Success = 1
GROUP BY AllocatedTransactionId
HAVING COUNT(1) > 1
ORDER BY DuplicateCount DESC, AllocatedTransactionId;

SELECT TOP (50)
    *
FROM dbo.POS_TransactionIdAllocatorTestLog
WHERE BatchId = @BatchId
  AND Success = 0
ORDER BY Id DESC;
GO
