/*
    Kishny POS - experimental allocator alternatives.

    TEST DATABASE ONLY. This script does not replace production POS save logic.
    It creates isolated objects to compare safer allocation strategies against
    the current global allocator pattern under concurrency.

    SQL Server compatibility: SQL Server 2012.

    Options covered:
      A. Current dbo.GetNextID_FromSequence behavior is benchmarked by MANUAL_87.
      B. Partitioned allocator by TableName + FieldName + BranchId + StoreID
         + TransactionType + YearNo + MonthNo.
      C. SQL Server SEQUENCE object evaluation. SQL Server 2012 supports SEQUENCE,
         but migration must account for gaps, deployment permissions, old code
         expecting branch/store/year semantics, and failover/cache behavior.
      D. Single-row update with OUTPUT inserted.NextValue, short transaction,
         row-level key, and no global applock.
*/

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.POS_SequenceAllocatorExperiment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_SequenceAllocatorExperiment
    (
        TableName SYSNAME NOT NULL,
        FieldName SYSNAME NOT NULL,
        BranchId INT NOT NULL,
        StoreID INT NOT NULL,
        TransactionType INT NOT NULL,
        YearNo INT NOT NULL,
        MonthNo INT NOT NULL,
        NextValue BIGINT NOT NULL,
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_POS_SequenceAllocatorExperiment_UpdatedAt DEFAULT(GETDATE()),
        CONSTRAINT PK_POS_SequenceAllocatorExperiment PRIMARY KEY
        (
            TableName,
            FieldName,
            BranchId,
            StoreID,
            TransactionType,
            YearNo,
            MonthNo
        )
    );
END;
GO

IF OBJECT_ID(N'dbo.POS_SequenceAllocatorExperiment_Global', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_SequenceAllocatorExperiment_Global
    (
        TableName SYSNAME NOT NULL,
        FieldName SYSNAME NOT NULL,
        NextValue BIGINT NOT NULL,
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_POS_SequenceAllocatorExperiment_Global_UpdatedAt DEFAULT(GETDATE()),
        CONSTRAINT PK_POS_SequenceAllocatorExperiment_Global PRIMARY KEY(TableName, FieldName)
    );
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_ExperimentalAllocator_Partitioned', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_ExperimentalAllocator_Partitioned;
GO

CREATE PROCEDURE dbo.usp_POS_ExperimentalAllocator_Partitioned
    @TableName SYSNAME,
    @FieldName SYSNAME,
    @BranchId INT,
    @StoreID INT,
    @TransactionType INT,
    @DateValue DATETIME,
    @NextValue BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @YearNo INT;
    DECLARE @MonthNo INT;
    DECLARE @out TABLE(NextValue BIGINT);

    SET @YearNo = YEAR(@DateValue);
    SET @MonthNo = MONTH(@DateValue);
    SET @NextValue = NULL;

    BEGIN TRANSACTION;

    UPDATE dbo.POS_SequenceAllocatorExperiment WITH (UPDLOCK, ROWLOCK)
        SET NextValue = NextValue + 1,
            UpdatedAt = GETDATE()
        OUTPUT inserted.NextValue INTO @out
    WHERE TableName = @TableName
      AND FieldName = @FieldName
      AND BranchId = @BranchId
      AND StoreID = @StoreID
      AND TransactionType = @TransactionType
      AND YearNo = @YearNo
      AND MonthNo = @MonthNo;

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO dbo.POS_SequenceAllocatorExperiment
        (
            TableName, FieldName, BranchId, StoreID,
            TransactionType, YearNo, MonthNo, NextValue
        )
        VALUES
        (
            @TableName, @FieldName, @BranchId, @StoreID,
            @TransactionType, @YearNo, @MonthNo, 1
        );

        SET @NextValue = 1;
    END
    ELSE
    BEGIN
        SELECT TOP (1) @NextValue = NextValue FROM @out;
    END;

    COMMIT TRANSACTION;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_ExperimentalAllocator_GlobalRowOutput', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_ExperimentalAllocator_GlobalRowOutput;
GO

CREATE PROCEDURE dbo.usp_POS_ExperimentalAllocator_GlobalRowOutput
    @TableName SYSNAME,
    @FieldName SYSNAME,
    @NextValue BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @out TABLE(NextValue BIGINT);
    SET @NextValue = NULL;

    BEGIN TRANSACTION;

    UPDATE dbo.POS_SequenceAllocatorExperiment_Global WITH (UPDLOCK, ROWLOCK)
        SET NextValue = NextValue + 1,
            UpdatedAt = GETDATE()
        OUTPUT inserted.NextValue INTO @out
    WHERE TableName = @TableName
      AND FieldName = @FieldName;

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO dbo.POS_SequenceAllocatorExperiment_Global(TableName, FieldName, NextValue)
        VALUES(@TableName, @FieldName, 1);

        SET @NextValue = 1;
    END
    ELSE
    BEGIN
        SELECT TOP (1) @NextValue = NextValue FROM @out;
    END;

    COMMIT TRANSACTION;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_ExperimentalAllocator_SequenceObject', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_ExperimentalAllocator_SequenceObject;
GO

IF OBJECT_ID(N'dbo.seq_POS_ExperimentalAllocator', N'SO') IS NULL
    EXEC(N'CREATE SEQUENCE dbo.seq_POS_ExperimentalAllocator AS BIGINT START WITH 1 INCREMENT BY 1 NO CACHE;');
GO

CREATE PROCEDURE dbo.usp_POS_ExperimentalAllocator_SequenceObject
    @NextValue BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT @NextValue = NEXT VALUE FOR dbo.seq_POS_ExperimentalAllocator;
END;
GO

/*
    Evidence matrix to fill after running the benchmark:

    Option | Concurrency safety | Uniqueness | Gaps risk | Deadlock risk | Blocking risk | Schema change | Fit
    A Current GetNextID_FromSequence | Safe unique values through SEQUENCE + applock | High | High, because SEQUENCE gaps are possible | App-lock serialization can become central choke point | High if all users share same table/field lock | Existing | Current production behavior
    B Partitioned allocator | Safe per partition if key matches numbering rule | Per branch/store/type/month | Low to medium | Lower if users spread over partitions | Lower | New allocator table + migration | Good if business accepts partitioned serial spaces
    C SQL Server SEQUENCE | Safe global uniqueness | Global | High by design, even with NO CACHE under failures | Low inside sequence call | Lower than app lock, but still global monotonic source | Sequence objects and migration | Works on SQL Server 2012, but semantics/gaps must be accepted
    D Single-row OUTPUT update | Safe unique values on one row | Global or chosen key | Low to medium | Lower than long app-lock if transaction is tiny | Still serializes same key | New allocator table | Good middle step if global serial must remain
*/
