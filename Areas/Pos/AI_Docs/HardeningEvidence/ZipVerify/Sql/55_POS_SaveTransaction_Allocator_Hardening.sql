/*
    Recreate dbo.GetNextID_FromSequence with serialized sequence allocation.
    Database target: POS production database.
    SQL Server 2012 compatible.
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'dbo.GetNextID_FromSequence', N'P') IS NOT NULL
    DROP PROCEDURE dbo.GetNextID_FromSequence;
GO

CREATE PROCEDURE dbo.GetNextID_FromSequence
    @TableName NVARCHAR(100),
    @FieldName NVARCHAR(100),
    @NextValue BIGINT OUTPUT,
    @ErrorMsg  NVARCHAR(500) OUTPUT
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
        @SchemaName SYSNAME,
        @SequenceName SYSNAME,
        @SequenceObject NVARCHAR(300),
        @TableObject NVARCHAR(300),
        @LockName NVARCHAR(255),
        @sql NVARCHAR(MAX),
        @StartWith BIGINT,
        @MaxExisting BIGINT,
        @CandidateExists BIT,
        @LockResult INT;

    SET @SchemaName = N'dbo';
    SET @NextValue = NULL;
    SET @ErrorMsg = NULL;
    SET @SequenceName = N'seq_' + @TableName + N'_' + @FieldName;
    SET @SequenceObject = QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@SequenceName);
    SET @TableObject = QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@TableName);
    SET @LockName = N'GetNextID_FromSequence:' + @SchemaName + N'.' + @TableName + N'.' + @FieldName;

    BEGIN TRY
        IF OBJECT_ID(@TableObject, N'U') IS NULL
        BEGIN
            SET @ErrorMsg = N'Unknown table for numbering: ' + @TableObject;
            RETURN -1;
        END;

        IF COL_LENGTH(@TableObject, @FieldName) IS NULL
        BEGIN
            SET @ErrorMsg = N'Unknown field for numbering: ' + @TableObject + N'.' + @FieldName;
            RETURN -1;
        END;

        EXEC @LockResult = sys.sp_getapplock
            @Resource = @LockName,
            @LockMode = 'Exclusive',
            @LockOwner = 'Session',
            @LockTimeout = 30000;

        IF @LockResult < 0
        BEGIN
            SET @ErrorMsg = N'Unable to acquire numbering lock for ' + @LockName;
            RETURN -1;
        END;

        IF OBJECT_ID(@SequenceObject, N'SO') IS NULL
        BEGIN
            SET @sql = N'
SELECT @MaxExisting = ISNULL(MAX(CONVERT(BIGINT,' + QUOTENAME(@FieldName) + N')), 0)
FROM ' + @TableObject + N' WITH (HOLDLOCK, UPDLOCK);';

            EXEC sys.sp_executesql
                @sql,
                N'@MaxExisting BIGINT OUTPUT',
                @MaxExisting = @MaxExisting OUTPUT;

            SET @StartWith = ISNULL(@MaxExisting, 0) + 1;
            SET @sql = N'
CREATE SEQUENCE ' + @SequenceObject + N'
AS BIGINT
START WITH ' + CAST(@StartWith AS NVARCHAR(30)) + N'
INCREMENT BY 1
NO CACHE;';

            EXEC sys.sp_executesql @sql;
        END;

        SET @sql = N'SELECT @NextValue = NEXT VALUE FOR ' + @SequenceObject + N';';
        EXEC sys.sp_executesql
            @sql,
            N'@NextValue BIGINT OUTPUT',
            @NextValue = @NextValue OUTPUT;

        SET @CandidateExists = 0;
        SET @sql = N'
SELECT @CandidateExists =
    CASE WHEN EXISTS
    (
        SELECT 1
        FROM ' + @TableObject + N' WITH (READCOMMITTEDLOCK)
        WHERE ' + QUOTENAME(@FieldName) + N' = @Candidate
    )
    THEN 1 ELSE 0 END;';

        EXEC sys.sp_executesql
            @sql,
            N'@Candidate BIGINT, @CandidateExists BIT OUTPUT',
            @Candidate = @NextValue,
            @CandidateExists = @CandidateExists OUTPUT;

        IF @CandidateExists = 1
        BEGIN
            SET @sql = N'
SELECT @MaxExisting = ISNULL(MAX(CONVERT(BIGINT,' + QUOTENAME(@FieldName) + N')), 0)
FROM ' + @TableObject + N' WITH (HOLDLOCK, UPDLOCK);';

            EXEC sys.sp_executesql
                @sql,
                N'@MaxExisting BIGINT OUTPUT',
                @MaxExisting = @MaxExisting OUTPUT;

            SET @StartWith = ISNULL(@MaxExisting, 0) + 1;
            SET @sql = N'ALTER SEQUENCE ' + @SequenceObject + N' RESTART WITH ' + CAST(@StartWith AS NVARCHAR(30)) + N';';
            EXEC sys.sp_executesql @sql;

            SET @sql = N'SELECT @NextValue = NEXT VALUE FOR ' + @SequenceObject + N';';
            EXEC sys.sp_executesql
                @sql,
                N'@NextValue BIGINT OUTPUT',
                @NextValue = @NextValue OUTPUT;
        END;

        EXEC sys.sp_releaseapplock @Resource = @LockName, @LockOwner = 'Session';
        SET @ErrorMsg = NULL;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @LockResult >= 0
            EXEC sys.sp_releaseapplock @Resource = @LockName, @LockOwner = 'Session';

        SET @NextValue = NULL;
        SET @ErrorMsg = ERROR_MESSAGE();
        RETURN -1;
    END CATCH;
END;
GO

