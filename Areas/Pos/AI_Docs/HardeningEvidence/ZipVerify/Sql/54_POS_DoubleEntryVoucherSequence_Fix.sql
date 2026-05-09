/*
    POS DOUBLE_ENTREY_VOUCHERS sequence repair.
    Database target: POS production database.
    SQL Server 2012 compatible.

    Edit @AppDbUser when the production app uses a non-dbo SQL user.
    Leave it NULL to grant only to PUBLIC for sequence NEXT VALUE safety.
*/
SET NOCOUNT ON;

DECLARE @AppDbUser SYSNAME;
DECLARE @SequenceName SYSNAME;
DECLARE @SequenceObject NVARCHAR(300);
DECLARE @MaxDevID BIGINT;
DECLARE @CurrentSequenceValue BIGINT;
DECLARE @StartWith BIGINT;
DECLARE @Action NVARCHAR(50);
DECLARE @Sql NVARCHAR(MAX);

SET @AppDbUser = NULL;
SET @SequenceName = N'seq_DOUBLE_ENTREY_VOUCHERS_Double_Entry_Vouchers_ID';
SET @SequenceObject = N'dbo.' + QUOTENAME(@SequenceName);

BEGIN TRY
    BEGIN TRANSACTION;

    EXEC sys.sp_getapplock
        @Resource = N'POS:DOUBLE_ENTREY_VOUCHERS:SequenceRepair',
        @LockMode = 'Exclusive',
        @LockOwner = 'Transaction',
        @LockTimeout = 30000;

    SELECT @MaxDevID = ISNULL(MAX(CONVERT(BIGINT, Double_Entry_Vouchers_ID)), 0)
    FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (HOLDLOCK, UPDLOCK);

    IF OBJECT_ID(@SequenceObject, N'SO') IS NULL
    BEGIN
        SET @StartWith = @MaxDevID + 1;
        SET @Sql = N'CREATE SEQUENCE ' + @SequenceObject + N'
AS BIGINT
START WITH ' + CONVERT(NVARCHAR(30), @StartWith) + N'
INCREMENT BY 1
NO CACHE;';
        EXEC sys.sp_executesql @Sql;
        SET @Action = N'CREATE';
    END
    ELSE
    BEGIN
        SELECT @CurrentSequenceValue = CONVERT(BIGINT, current_value)
        FROM sys.sequences
        WHERE object_id = OBJECT_ID(@SequenceObject, N'SO');

        IF @CurrentSequenceValue <= @MaxDevID
        BEGIN
            SET @StartWith = @MaxDevID + 1;
            SET @Sql = N'ALTER SEQUENCE ' + @SequenceObject + N' RESTART WITH ' + CONVERT(NVARCHAR(30), @StartWith) + N';';
            EXEC sys.sp_executesql @Sql;
            SET @Action = N'RESTART';
        END
        ELSE
        BEGIN
            SET @StartWith = @CurrentSequenceValue + 1;
            SET @Action = N'NO_CHANGE_ALREADY_SAFE';
        END;
    END;

    IF @AppDbUser IS NOT NULL AND DATABASE_PRINCIPAL_ID(@AppDbUser) IS NOT NULL
    BEGIN
        SET @Sql = N'GRANT EXECUTE ON dbo.GetNextID_FromSequence TO ' + QUOTENAME(@AppDbUser) + N';
GRANT UPDATE ON OBJECT::dbo.seq_DOUBLE_ENTREY_VOUCHERS_Double_Entry_Vouchers_ID TO ' + QUOTENAME(@AppDbUser) + N';
GRANT REFERENCES ON OBJECT::dbo.seq_DOUBLE_ENTREY_VOUCHERS_Double_Entry_Vouchers_ID TO ' + QUOTENAME(@AppDbUser) + N';';
        EXEC sys.sp_executesql @Sql;
    END;

    COMMIT TRANSACTION;

    SELECT
        FixedSequence = @SequenceName,
        ActionTaken = @Action,
        MaxDoubleEntryVoucherId = @MaxDevID,
        PreviousSequenceCurrentValue = @CurrentSequenceValue,
        SafeNextValue = @StartWith,
        Note = N'SafeNextValue is greater than current MAX. Existing safe sequences are not restarted backwards.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @Err NVARCHAR(4000);
    SET @Err = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;

