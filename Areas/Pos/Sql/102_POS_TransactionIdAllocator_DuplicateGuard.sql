/*
    102_POS_TransactionIdAllocator_DuplicateGuard.sql

    Purpose:
      Emergency guard for POS Transaction_ID allocation after field evidence of
      duplicate PK attempts from a stale/low allocator seed.

    Scope:
      - Transactions.Transaction_ID allocator only.
      - No voucher/invoice/accounting serial changes.
      - SQL Server 2008+/2012 compatible.

    Behavior:
      - Ensures dbo.POS_TransactionIdAllocator exists.
      - Reseeds the allocator above current MAX(Transactions.Transaction_ID).
      - Recreates dbo.usp_POS_AllocateTransactionId so it never returns an ID
        already present in dbo.Transactions. If it finds a stale candidate, it
        bumps the counter above MAX(Transaction_ID) and retries internally.
*/

SET NOCOUNT ON;
GO

IF DB_NAME() IN (N'master', N'model', N'msdb', N'tempdb')
BEGIN
    RAISERROR('Refusing to run POS Transaction_ID allocator guard in a system database.', 16, 1);
    SET NOEXEC ON;
END;
GO

IF OBJECT_ID(N'dbo.POS_TransactionIdAllocator', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_TransactionIdAllocator
    (
        CounterName SYSNAME NOT NULL CONSTRAINT PK_POS_TransactionIdAllocator PRIMARY KEY,
        NextValue BIGINT NOT NULL,
        SeededFromMax BIGINT NOT NULL,
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_POS_TransactionIdAllocator_UpdatedAt DEFAULT(GETDATE())
    );
END;
GO

DECLARE @TransactionIdSeed BIGINT;
DECLARE @SeedLockResult INT;

EXEC @SeedLockResult = sys.sp_getapplock
    @Resource = N'POS_TransactionIdAllocator.Seed.Transactions.Transaction_ID',
    @LockMode = 'Exclusive',
    @LockOwner = 'Session',
    @LockTimeout = 30000;

IF @SeedLockResult < 0
BEGIN
    RAISERROR('Unable to acquire Transaction_ID allocator reseed lock.', 16, 1);
END;
ELSE
BEGIN
    BEGIN TRANSACTION;

    SELECT @TransactionIdSeed = ISNULL(MAX(CONVERT(BIGINT, Transaction_ID)), 0)
    FROM dbo.Transactions WITH (HOLDLOCK, UPDLOCK);

    IF EXISTS (SELECT 1 FROM dbo.POS_TransactionIdAllocator WITH (UPDLOCK, HOLDLOCK) WHERE CounterName = N'Transactions.Transaction_ID')
    BEGIN
        UPDATE dbo.POS_TransactionIdAllocator WITH (UPDLOCK, HOLDLOCK)
        SET NextValue = CASE WHEN NextValue <= @TransactionIdSeed THEN @TransactionIdSeed + 1 ELSE NextValue END,
            SeededFromMax = CASE WHEN NextValue <= @TransactionIdSeed THEN @TransactionIdSeed ELSE SeededFromMax END,
            UpdatedAt = GETDATE()
        WHERE CounterName = N'Transactions.Transaction_ID';
    END
    ELSE
    BEGIN
        INSERT INTO dbo.POS_TransactionIdAllocator (CounterName, NextValue, SeededFromMax, UpdatedAt)
        VALUES (N'Transactions.Transaction_ID', @TransactionIdSeed + 1, @TransactionIdSeed, GETDATE());
    END;

    COMMIT TRANSACTION;

    EXEC sys.sp_releaseapplock
        @Resource = N'POS_TransactionIdAllocator.Seed.Transactions.Transaction_ID',
        @LockOwner = 'Session';
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_AllocateTransactionId', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_AllocateTransactionId;
GO

CREATE PROCEDURE dbo.usp_POS_AllocateTransactionId
    @NextValue BIGINT OUTPUT,
    @ErrorMsg NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT OFF;

    DECLARE @Attempt INT;
    DECLARE @MaxAttempts INT;
    DECLARE @DelayMs INT;
    DECLARE @Delay CHAR(12);
    DECLARE @Candidate BIGINT;
    DECLARE @CurrentMax BIGINT;

    SET @Attempt = 1;
    SET @MaxAttempts = 8;
    SET @NextValue = NULL;
    SET @ErrorMsg = NULL;

    WHILE @Attempt <= @MaxAttempts
    BEGIN
        BEGIN TRY
            BEGIN TRANSACTION;

            SET @Candidate = NULL;

            UPDATE dbo.POS_TransactionIdAllocator WITH (UPDLOCK, HOLDLOCK)
            SET @Candidate = NextValue,
                NextValue = NextValue + 1,
                UpdatedAt = GETDATE()
            WHERE CounterName = N'Transactions.Transaction_ID';

            IF @@ROWCOUNT <> 1 OR @Candidate IS NULL
            BEGIN
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                SET @ErrorMsg = N'Transactions.Transaction_ID allocator row is missing.';
                RETURN -1;
            END;

            IF @Candidate > 2147483647
            BEGIN
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                SET @ErrorMsg = N'Allocated Transaction_ID exceeds INT range.';
                RETURN -1;
            END;

            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.Transactions WITH (UPDLOCK, HOLDLOCK)
                WHERE Transaction_ID = CONVERT(INT, @Candidate)
            )
            BEGIN
                COMMIT TRANSACTION;
                SET @NextValue = @Candidate;
                SET @ErrorMsg = NULL;
                RETURN 0;
            END;

            SELECT @CurrentMax = ISNULL(MAX(CONVERT(BIGINT, Transaction_ID)), 0)
            FROM dbo.Transactions WITH (HOLDLOCK, UPDLOCK);

            UPDATE dbo.POS_TransactionIdAllocator WITH (UPDLOCK, HOLDLOCK)
            SET NextValue = CASE WHEN NextValue <= @CurrentMax THEN @CurrentMax + 1 ELSE NextValue END,
                SeededFromMax = CASE WHEN SeededFromMax < @CurrentMax THEN @CurrentMax ELSE SeededFromMax END,
                UpdatedAt = GETDATE()
            WHERE CounterName = N'Transactions.Transaction_ID';

            COMMIT TRANSACTION;

            SET @Attempt = @Attempt + 1;
            CONTINUE;
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

            IF ERROR_NUMBER() = 1205 AND @Attempt < @MaxAttempts
            BEGIN
                SET @DelayMs = 40 + (ABS(CHECKSUM(NEWID())) % 160);
                SET @Delay = '00:00:00.' + RIGHT('000' + CONVERT(VARCHAR(3), @DelayMs), 3);
                WAITFOR DELAY @Delay;
                SET @Attempt = @Attempt + 1;
                CONTINUE;
            END;

            SET @ErrorMsg = ERROR_MESSAGE();
            SET @NextValue = NULL;
            RETURN -1;
        END CATCH;
    END;

    SET @ErrorMsg = N'Unable to allocate a non-duplicate Transactions.Transaction_ID after reseed retries.';
    SET @NextValue = NULL;
    RETURN -1;
END;
GO

DECLARE @PostMax BIGINT;
DECLARE @PostNext BIGINT;

SELECT @PostMax = ISNULL(MAX(CONVERT(BIGINT, Transaction_ID)), 0)
FROM dbo.Transactions WITH (NOLOCK);

SELECT @PostNext = NextValue
FROM dbo.POS_TransactionIdAllocator WITH (NOLOCK)
WHERE CounterName = N'Transactions.Transaction_ID';

IF @PostNext IS NULL OR @PostNext <= @PostMax
BEGIN
    RAISERROR('Transaction_ID allocator duplicate guard verification failed: NextValue is not above MAX(Transaction_ID).', 16, 1);
END;
GO
