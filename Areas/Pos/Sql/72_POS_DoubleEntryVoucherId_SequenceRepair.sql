/*
    Kishny POS - DOUBLE_ENTREY_VOUCHERS ID sequence repair
    SQL Server 2012 compatible.

    Run with db_owner/DDL-capable login when a save error reports:
    Violation of PRIMARY KEY constraint 'PK_DOUBLE_ENTREY_VOUCHERS'
    duplicate key (Double_Entry_Vouchers_ID, DEV_ID_Line_No).

    POS save no longer relies on this sequence for the voucher header ID, but
    repairing it protects other modules/shared code paths that still call
    dbo.GetNextID_FromSequence for DOUBLE_ENTREY_VOUCHERS.
*/

SET NOCOUNT ON;

DECLARE @SequenceName SYSNAME;
DECLARE @SequenceObject NVARCHAR(300);
DECLARE @MaxDevID BIGINT;
DECLARE @CurrentSequenceValue BIGINT;
DECLARE @RestartWith BIGINT;
DECLARE @Sql NVARCHAR(MAX);
DECLARE @Action NVARCHAR(50);
DECLARE @LockResult INT;

SET @SequenceName = N'seq_DOUBLE_ENTREY_VOUCHERS_Double_Entry_Vouchers_ID';
SET @SequenceObject = N'dbo.' + QUOTENAME(@SequenceName);

BEGIN TRY
    BEGIN TRANSACTION;

    EXEC @LockResult = sys.sp_getapplock
        @Resource = N'POS.DOUBLE_ENTREY_VOUCHERS.ID.SequenceRepair',
        @LockMode = 'Exclusive',
        @LockOwner = 'Transaction',
        @LockTimeout = 30000;

    IF @LockResult < 0
        RAISERROR(N'Unable to acquire DOUBLE_ENTREY_VOUCHERS sequence repair lock.', 16, 1);

    SELECT @MaxDevID = ISNULL(MAX(CONVERT(BIGINT, Double_Entry_Vouchers_ID)), 0)
    FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (TABLOCKX, HOLDLOCK);

    SET @RestartWith = @MaxDevID + 1;

    IF OBJECT_ID(@SequenceObject, N'SO') IS NULL
    BEGIN
        SET @Sql = N'CREATE SEQUENCE ' + @SequenceObject + N'
AS BIGINT
START WITH ' + CONVERT(NVARCHAR(30), @RestartWith) + N'
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

        IF @CurrentSequenceValue < @RestartWith
        BEGIN
            SET @Sql = N'ALTER SEQUENCE ' + @SequenceObject + N' RESTART WITH ' + CONVERT(NVARCHAR(30), @RestartWith) + N';';
            EXEC sys.sp_executesql @Sql;
            SET @Action = N'RESTART';
        END
        ELSE
        BEGIN
            SET @Action = N'NO_CHANGE_ALREADY_SAFE';
        END;
    END;

    COMMIT TRANSACTION;

    SELECT
        SequenceName = @SequenceName,
        ActionTaken = @Action,
        MaxDoubleEntryVoucherId = @MaxDevID,
        PreviousSequenceCurrentValue = @CurrentSequenceValue,
        SafeNextValue = @RestartWith;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @Err NVARCHAR(4000);
    SET @Err = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;
