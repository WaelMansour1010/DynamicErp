/*
    119_POS_VoucherCoding_StaleCounterSelfHeal.sql

    Purpose:
      Recreate dbo.usp_GetNextSerial_V2 with the existing POS voucher serial scope
      behavior plus a stale-counter self-heal path.

      If SerialCounters_V2 is behind actual Transactions.NoteSerial1 data for the
      same branch/type/month scope, the procedure reseeds that counter forward and
      returns the next safe number instead of failing with:
        Unable to generate NoteSerial1 using dbo.usp_Voucher_coding_V2.

    Rules:
      - Preserve the visible serial format and configured Company/Branch/BranchStore scope.
      - Do not change accounting, voucher meaning, Sanad_No, NoteType, or invoice serial semantics.
      - Only move stale SerialCounters_V2.CurrentTail forward; never move it backward.
      - StoreID participates only when the configured scope is BranchStore and
        sanad_numbering.StoreCoding = 1.
      - Branch scope locks only SourceTable + BranchID + TypeCode + Prefix + Year + Month.
      - Serial format remains unchanged.
      - Includes an idempotent reseed pass for existing POS Transactions counters.

    SQL Server compatibility: SQL Server 2012+
*/

SET NOCOUNT ON;
GO

IF COL_LENGTH(N'dbo.TblOptions', N'POSVoucherSerialScope') IS NULL
BEGIN
    ALTER TABLE dbo.TblOptions
        ADD POSVoucherSerialScope NVARCHAR(20) NULL
            CONSTRAINT DF_TblOptions_POSVoucherSerialScope DEFAULT(N'Company');
END;
GO

IF OBJECT_ID(N'dbo.SerialCounters_V2', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Transactions', N'U') IS NOT NULL
BEGIN
    ;WITH ExistingTails AS
    (
        SELECT
            CounterID = c.CounterID,
            MaxTail =
                MAX(
                    CASE
                        WHEN ISNUMERIC(RIGHT(CONVERT(VARCHAR(50), t.NoteSerial1), c.NoOfDigits)) = 1
                        THEN CAST(RIGHT(CONVERT(VARCHAR(50), t.NoteSerial1), c.NoOfDigits) AS BIGINT)
                        ELSE NULL
                    END
                )
        FROM dbo.SerialCounters_V2 c WITH (READCOMMITTEDLOCK)
        JOIN dbo.Transactions t WITH (READCOMMITTEDLOCK)
          ON c.SourceTable = 'Transactions'
         AND t.Transaction_Type = c.TypeCode
         AND t.NoteSerial1 IS NOT NULL
         AND (
                c.NumberingType = 1
             OR (
                    c.NumberingType = 2
                AND c.MonthNum BETWEEN 1 AND 12
                AND t.Transaction_Date >= DATEADD(MONTH, c.MonthNum - 1, DATEADD(YEAR, c.YearNum - 1900, 0))
                AND t.Transaction_Date < DATEADD(MONTH, c.MonthNum, DATEADD(YEAR, c.YearNum - 1900, 0))
                )
             OR (
                    c.NumberingType = 3
                AND t.Transaction_Date >= DATEADD(YEAR, c.YearNum - 1900, 0)
                AND t.Transaction_Date < DATEADD(YEAR, c.YearNum - 1899, 0)
                )
             )
         AND (c.BranchID = 0 OR t.BranchId = c.BranchID)
         AND (ISNULL(c.StoreID, 0) = 0 OR ISNULL(t.StoreID, 0) = ISNULL(c.StoreID, 0))
         AND (t.Prefix = c.Prefix OR (t.Prefix IS NULL AND c.Prefix IS NULL))
        WHERE c.SourceTable = 'Transactions'
          AND c.TypeCode IN (19, 21)
        GROUP BY c.CounterID
    )
    UPDATE c
    SET
        CurrentTail = e.MaxTail,
        LastUpdated = GETDATE(),
        UpdatedByUser = 'POS119;ReseedStale',
        UpdateCount = UpdateCount + 1
    FROM dbo.SerialCounters_V2 c WITH (UPDLOCK, HOLDLOCK)
    JOIN ExistingTails e ON e.CounterID = c.CounterID
    WHERE e.MaxTail IS NOT NULL
      AND e.MaxTail > c.CurrentTail;
END;
GO

UPDATE dbo.TblOptions
SET POSVoucherSerialScope = N'Company'
WHERE POSVoucherSerialScope IS NULL
   OR LTRIM(RTRIM(POSVoucherSerialScope)) = N'';
GO

IF OBJECT_ID(N'dbo.usp_GetNextSerial_V2', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetNextSerial_V2;
GO

CREATE PROCEDURE dbo.usp_GetNextSerial_V2
    @SourceTable VARCHAR(50),
    @BranchID INT,
    @TypeCode INT,
    @TransDate DATE,
    @Prefix VARCHAR(10) = NULL,
    @StoreID INT = NULL,
    @NumberingType TINYINT = NULL,
    @NoOfDigits TINYINT = NULL,
    @YearDigits TINYINT = NULL,
    @StartAt INT = NULL,
    @EndAt INT = NULL,
    @UserID INT = NULL,
    @SerialFormatted VARCHAR(50) OUTPUT,
    @SerialNumeric FLOAT OUTPUT,
    @TailNumber BIGINT OUTPUT,
    @ErrorMsg NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @YearNum INT = YEAR(@TransDate);
    DECLARE @MonthNum INT;
    DECLARE @NewTail BIGINT;
    DECLARE @Year4 VARCHAR(4) = CAST(YEAR(@TransDate) AS VARCHAR(4));
    DECLARE @Year2 VARCHAR(2) = RIGHT(CAST(YEAR(@TransDate) AS VARCHAR(4)), 2);
    DECLARE @Month2 VARCHAR(2) = RIGHT('0' + CAST(MONTH(@TransDate) AS VARCHAR(2)), 2);
    DECLARE @BranchDigit TINYINT;
    DECLARE @RowsAffected INT;
    DECLARE @SerialDataType VARCHAR(20) = 'VARCHAR';
    DECLARE @SanadNo INT;
    DECLARE @StoreCoding BIT = 0;
    DECLARE @ConfiguredScope NVARCHAR(20) = N'Company';
    DECLARE @EffectiveScope NVARCHAR(20) = N'Company';
    DECLARE @EffectiveBranchID INT;
    DECLARE @EffectiveStoreID INT;
    DECLARE @PrefixKey VARCHAR(10);
    DECLARE @StoreKey INT;
    DECLARE @BranchKey INT;

    BEGIN TRY
        SELECT @SerialDataType = ISNULL(SerialDataType, 'VARCHAR')
        FROM dbo.SerialTableMapping WITH (NOLOCK)
        WHERE SourceTable = @SourceTable;

        SELECT TOP (1)
            @ConfiguredScope =
                CASE
                    WHEN UPPER(LTRIM(RTRIM(ISNULL(POSVoucherSerialScope, N'Company')))) = N'BRANCH' THEN N'Branch'
                    WHEN UPPER(LTRIM(RTRIM(ISNULL(POSVoucherSerialScope, N'Company')))) = N'BRANCHSTORE' THEN N'BranchStore'
                    ELSE N'Company'
                END
        FROM dbo.TblOptions WITH (NOLOCK);

        SET @ConfiguredScope = ISNULL(@ConfiguredScope, N'Company');

        SELECT @SanadNo = ISNULL(SanadNo, @TypeCode)
        FROM dbo.SerialTableMapping WITH (NOLOCK)
        WHERE SourceTable = @SourceTable;

        IF @SanadNo IS NULL OR @SanadNo = 0
            SET @SanadNo = @TypeCode;

        SELECT TOP 1
            @NumberingType = ISNULL(@NumberingType, ISNULL(numbering_id, 2)),
            @StartAt = ISNULL(@StartAt, ISNULL(start_at, 1)),
            @EndAt = ISNULL(@EndAt, NULLIF(end_at, 0)),
            @NoOfDigits = ISNULL(@NoOfDigits, ISNULL(no_of_digit, 5)),
            @YearDigits = ISNULL(@YearDigits, ISNULL(YearDigit, 4)),
            @StoreCoding = ISNULL(StoreCoding, 0)
        FROM dbo.sanad_numbering WITH (NOLOCK)
        WHERE branch_no = @BranchID
          AND sanad_no = @SanadNo
          AND (Prefix = @Prefix OR (Prefix IS NULL AND @Prefix IS NULL));

        SET @NumberingType = ISNULL(@NumberingType, 2);
        SET @NoOfDigits = ISNULL(@NoOfDigits, 5);
        SET @YearDigits = ISNULL(@YearDigits, 4);
        SET @StartAt = ISNULL(@StartAt, 1);
        SET @StoreCoding = ISNULL(@StoreCoding, 0);

        IF @NoOfDigits = 0 SET @NoOfDigits = 5;
        IF @YearDigits = 0 SET @YearDigits = 4;

        /*
            Effective scope:
            - Company: one counter across branches.
            - Branch: one counter per branch.
            - BranchStore: store participates only if sanad_numbering.StoreCoding=1.
              If StoreCoding=0, do not create unnecessary per-store counters.
        */
        IF @ConfiguredScope = N'BranchStore' AND @StoreCoding = 1
            SET @EffectiveScope = N'BranchStore';
        ELSE IF @ConfiguredScope IN (N'Branch', N'BranchStore')
            SET @EffectiveScope = N'Branch';
        ELSE
            SET @EffectiveScope = N'Company';

        SET @EffectiveBranchID = CASE WHEN @EffectiveScope = N'Company' THEN 0 ELSE @BranchID END;
        SET @EffectiveStoreID = CASE WHEN @EffectiveScope = N'BranchStore' THEN ISNULL(@StoreID, 0) ELSE 0 END;
        SET @PrefixKey = ISNULL(@Prefix, '');
        SET @StoreKey = @EffectiveStoreID;
        SET @BranchKey = @EffectiveBranchID;

        SET @BranchDigit = CASE
            WHEN @BranchID >= 100 THEN 3
            WHEN @BranchID >= 10 THEN 2
            ELSE 1
        END;

        SET @MonthNum = CASE @NumberingType
            WHEN 1 THEN 0
            WHEN 2 THEN MONTH(@TransDate)
            WHEN 3 THEN 0
            ELSE MONTH(@TransDate)
        END;

        UPDATE dbo.SerialCounters_V2 WITH (UPDLOCK, SERIALIZABLE)
        SET
            CurrentTail = CurrentTail + 1,
            @NewTail = CurrentTail + 1,
            LastUpdated = GETDATE(),
            UpdatedByUser =
                LEFT(
                    ISNULL(CAST(@UserID AS VARCHAR(20)), '') +
                    ';Scope=' + CONVERT(VARCHAR(20), @EffectiveScope) +
                    ';BranchKey=' + CONVERT(VARCHAR(20), @BranchKey) +
                    ';StoreKey=' + CONVERT(VARCHAR(20), @StoreKey),
                    50
                ),
            UpdateCount = UpdateCount + 1
        WHERE SourceTable = @SourceTable
          AND BranchID = @BranchKey
          AND TypeCode = @TypeCode
          AND ISNULL(Prefix, '') = @PrefixKey
          AND ISNULL(StoreID, 0) = @StoreKey
          AND YearNum = @YearNum
          AND MonthNum = @MonthNum;

        SET @RowsAffected = @@ROWCOUNT;

        IF @RowsAffected = 0
        BEGIN
            DECLARE @LastTailFromData BIGINT = @StartAt - 1;
            DECLARE @SQL NVARCHAR(MAX);
            DECLARE @Params NVARCHAR(1000);
            DECLARE @BranchField VARCHAR(50), @DateField VARCHAR(50), @SerialField VARCHAR(50);

            SELECT
                @BranchField = BranchField,
                @DateField = DateField,
                @SerialField = SerialField,
                @SerialDataType = SerialDataType
            FROM dbo.SerialTableMapping WITH (NOLOCK)
            WHERE SourceTable = @SourceTable;

            IF @BranchField IS NULL
            BEGIN
                SET @BranchField = 'BranchId';
                SET @DateField = 'Transaction_Date';
                SET @SerialField = 'NoteSerial1';
            END;

            IF @NumberingType = 2
            BEGIN
                SET @SQL = N'SELECT @LastTail = ISNULL(MAX(' +
                    CASE @SerialDataType
                        WHEN 'FLOAT' THEN N'CAST(RIGHT(CAST(CAST(' + QUOTENAME(@SerialField) + N' AS BIGINT) AS VARCHAR(50)), @Digits) AS BIGINT)'
                        ELSE N'CAST(RIGHT(' + QUOTENAME(@SerialField) + N', @Digits) AS BIGINT)'
                    END +
                    N'), @StartAt - 1) FROM ' + QUOTENAME(@SourceTable) + N' WITH (NOLOCK) WHERE ' +
                    QUOTENAME(@DateField) + N' >= @PeriodFrom AND ' + QUOTENAME(@DateField) + N' < @PeriodTo AND ' +
                    QUOTENAME(@SerialField) + N' IS NOT NULL';
            END
            ELSE IF @NumberingType = 3
            BEGIN
                SET @SQL = N'SELECT @LastTail = ISNULL(MAX(' +
                    CASE @SerialDataType
                        WHEN 'FLOAT' THEN N'CAST(RIGHT(CAST(CAST(' + QUOTENAME(@SerialField) + N' AS BIGINT) AS VARCHAR(50)), @Digits) AS BIGINT)'
                        ELSE N'CAST(RIGHT(' + QUOTENAME(@SerialField) + N', @Digits) AS BIGINT)'
                    END +
                    N'), @StartAt - 1) FROM ' + QUOTENAME(@SourceTable) + N' WITH (NOLOCK) WHERE ' +
                    QUOTENAME(@DateField) + N' >= @PeriodFrom AND ' + QUOTENAME(@DateField) + N' < @PeriodTo AND ' +
                    QUOTENAME(@SerialField) + N' IS NOT NULL';
            END
            ELSE
            BEGIN
                SET @SQL = N'SELECT @LastTail = ISNULL(MAX(' +
                    CASE @SerialDataType
                        WHEN 'FLOAT' THEN N'CAST(' + QUOTENAME(@SerialField) + N' AS BIGINT)'
                        ELSE N'CAST(' + QUOTENAME(@SerialField) + N' AS BIGINT)'
                    END +
                    N'), @StartAt - 1) FROM ' + QUOTENAME(@SourceTable) + N' WITH (NOLOCK) WHERE ' +
                    QUOTENAME(@SerialField) + N' IS NOT NULL';
            END;

            IF @EffectiveScope IN (N'Branch', N'BranchStore')
                SET @SQL = @SQL + N' AND ' + QUOTENAME(@BranchField) + N' = @BranchID';

            IF @SourceTable = 'Transactions'
                SET @SQL = @SQL + N' AND Transaction_Type = @TypeCode';
            ELSE IF @SourceTable = 'Notes' OR @SourceTable = 'notes_all'
                SET @SQL = @SQL + N' AND NoteType = @TypeCode';

            IF @SourceTable = 'Transactions' AND @EffectiveScope = N'BranchStore'
                SET @SQL = @SQL + N' AND ISNULL(StoreID, 0) = @StoreID';

            SET @SQL = @SQL + N' AND (Prefix = @Prefix OR (Prefix IS NULL AND @Prefix IS NULL))';

            BEGIN TRY
                DECLARE @PeriodFrom DATE;
                DECLARE @PeriodTo DATE;

                SET @PeriodFrom = CASE @NumberingType
                    WHEN 2 THEN DATEADD(MONTH, DATEDIFF(MONTH, 0, @TransDate), 0)
                    WHEN 3 THEN DATEADD(YEAR, DATEDIFF(YEAR, 0, @TransDate), 0)
                    ELSE '19000101'
                END;
                SET @PeriodTo = CASE @NumberingType
                    WHEN 2 THEN DATEADD(MONTH, 1, @PeriodFrom)
                    WHEN 3 THEN DATEADD(YEAR, 1, @PeriodFrom)
                    ELSE '99991231'
                END;

                SET @Params = N'@LastTail BIGINT OUTPUT, @BranchID INT, @StoreID INT, @TypeCode INT, @PeriodFrom DATE, @PeriodTo DATE, @Digits INT, @StartAt INT, @Prefix VARCHAR(10)';

                EXEC sp_executesql @SQL, @Params,
                    @LastTail = @LastTailFromData OUTPUT,
                    @BranchID = @BranchID,
                    @StoreID = @StoreKey,
                    @TypeCode = @TypeCode,
                    @PeriodFrom = @PeriodFrom,
                    @PeriodTo = @PeriodTo,
                    @Digits = @NoOfDigits,
                    @StartAt = @StartAt,
                    @Prefix = @Prefix;
            END TRY
            BEGIN CATCH
                SET @LastTailFromData = @StartAt - 1;
            END CATCH;

            SET @NewTail = ISNULL(@LastTailFromData, @StartAt - 1) + 1;

            BEGIN TRY
                INSERT INTO dbo.SerialCounters_V2
                (
                    SourceTable, BranchID, TypeCode, Prefix, StoreID,
                    NumberingType, YearNum, MonthNum, CurrentTail,
                    NoOfDigits, YearDigits, StartAt, EndAt,
                    UpdatedByUser, UpdateCount
                )
                VALUES
                (
                    @SourceTable, @BranchKey, @TypeCode,
                    NULLIF(@PrefixKey, ''), NULLIF(@StoreKey, 0),
                    @NumberingType, @YearNum, @MonthNum, @NewTail,
                    @NoOfDigits, @YearDigits, @StartAt, @EndAt,
                    LEFT(ISNULL(CAST(@UserID AS VARCHAR(20)), '') + ';Scope=' + CONVERT(VARCHAR(20), @EffectiveScope), 50),
                    1
                );
            END TRY
            BEGIN CATCH
                IF ERROR_NUMBER() IN (2627, 2601)
                BEGIN
                    UPDATE dbo.SerialCounters_V2 WITH (UPDLOCK, SERIALIZABLE)
                    SET
                        CurrentTail = CurrentTail + 1,
                        @NewTail = CurrentTail + 1,
                        LastUpdated = GETDATE(),
                        UpdatedByUser =
                            LEFT(
                                ISNULL(CAST(@UserID AS VARCHAR(20)), '') +
                                ';Scope=' + CONVERT(VARCHAR(20), @EffectiveScope) +
                                ';BranchKey=' + CONVERT(VARCHAR(20), @BranchKey) +
                                ';StoreKey=' + CONVERT(VARCHAR(20), @StoreKey),
                                50
                            ),
                        UpdateCount = UpdateCount + 1
                    WHERE SourceTable = @SourceTable
                      AND BranchID = @BranchKey
                      AND TypeCode = @TypeCode
                      AND ISNULL(Prefix, '') = @PrefixKey
                      AND ISNULL(StoreID, 0) = @StoreKey
                      AND YearNum = @YearNum
                      AND MonthNum = @MonthNum;
                END
                ELSE
                    THROW;
            END CATCH;
        END;

        IF @EndAt IS NOT NULL AND @NewTail >= @EndAt
        BEGIN
            SET @ErrorMsg = N'تم تجاوز الحد الأقصى للترقيم: ' + CAST(@EndAt AS NVARCHAR(20));
            SET @SerialFormatted = NULL;
            SET @SerialNumeric = NULL;
            SET @TailNumber = NULL;
            RETURN -1;
        END;

        DECLARE @BranchCode VARCHAR(10) = RIGHT(REPLICATE('0', 3) + CAST(@BranchID AS VARCHAR(3)), @BranchDigit);
        DECLARE @TailFormatted VARCHAR(20) = RIGHT(REPLICATE('0', 10) + CAST(@NewTail AS VARCHAR(10)), @NoOfDigits);
        DECLARE @YearFormatted VARCHAR(4) = CASE WHEN @YearDigits = 2 THEN @Year2 ELSE @Year4 END;

        IF @NumberingType = 1
        BEGIN
            SET @SerialFormatted = @BranchCode + CAST(@NewTail AS VARCHAR(20));
            SET @SerialNumeric = CAST(@SerialFormatted AS FLOAT);
        END
        ELSE IF @NumberingType = 2
        BEGIN
            SET @SerialFormatted = @BranchCode + @YearFormatted + @Month2 + @TailFormatted;
            SET @SerialNumeric = CAST(@SerialFormatted AS FLOAT);
        END
        ELSE IF @NumberingType = 3
        BEGIN
            SET @SerialFormatted = @BranchCode + @YearFormatted + @TailFormatted;
            SET @SerialNumeric = CAST(@SerialFormatted AS FLOAT);
        END;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.Transactions WITH (READCOMMITTEDLOCK)
            WHERE Transaction_Type = @TypeCode
              AND NoteSerial1 = @SerialFormatted
              AND (@EffectiveScope = N'Company' OR BranchId = @BranchID)
              AND (@EffectiveScope <> N'BranchStore' OR ISNULL(StoreID, 0) = @StoreKey)
        )
        BEGIN
            DECLARE @RepairLastTail BIGINT;
            DECLARE @RepairPeriodFrom DATE;
            DECLARE @RepairPeriodTo DATE;

            SET @RepairPeriodFrom = CASE @NumberingType
                WHEN 2 THEN DATEADD(MONTH, DATEDIFF(MONTH, 0, @TransDate), 0)
                WHEN 3 THEN DATEADD(YEAR, DATEDIFF(YEAR, 0, @TransDate), 0)
                ELSE '19000101'
            END;
            SET @RepairPeriodTo = CASE @NumberingType
                WHEN 2 THEN DATEADD(MONTH, 1, @RepairPeriodFrom)
                WHEN 3 THEN DATEADD(YEAR, 1, @RepairPeriodFrom)
                ELSE '99991231'
            END;

            SELECT @RepairLastTail =
                ISNULL(MAX(CAST(RIGHT(CONVERT(VARCHAR(50), NoteSerial1), @NoOfDigits) AS BIGINT)), @StartAt - 1)
            FROM dbo.Transactions WITH (READCOMMITTEDLOCK)
            WHERE Transaction_Type = @TypeCode
              AND Transaction_Date >= @RepairPeriodFrom
              AND Transaction_Date < @RepairPeriodTo
              AND NoteSerial1 IS NOT NULL
              AND ISNUMERIC(RIGHT(CONVERT(VARCHAR(50), NoteSerial1), @NoOfDigits)) = 1
              AND (@EffectiveScope = N'Company' OR BranchId = @BranchID)
              AND (@EffectiveScope <> N'BranchStore' OR ISNULL(StoreID, 0) = @StoreKey)
              AND (Prefix = @Prefix OR (Prefix IS NULL AND @Prefix IS NULL));

            IF ISNULL(@RepairLastTail, @StartAt - 1) >= @NewTail
            BEGIN
                SET @NewTail = @RepairLastTail + 1;

                UPDATE dbo.SerialCounters_V2 WITH (UPDLOCK, HOLDLOCK)
                SET
                    CurrentTail = @NewTail,
                    LastUpdated = GETDATE(),
                    UpdatedByUser =
                        LEFT(
                            ISNULL(CAST(@UserID AS VARCHAR(20)), '') +
                            ';Scope=' + CONVERT(VARCHAR(20), @EffectiveScope) +
                            ';SelfHealStale',
                            50
                        ),
                    UpdateCount = UpdateCount + 1
                WHERE SourceTable = @SourceTable
                  AND BranchID = @BranchKey
                  AND TypeCode = @TypeCode
                  AND ISNULL(Prefix, '') = @PrefixKey
                  AND ISNULL(StoreID, 0) = @StoreKey
                  AND YearNum = @YearNum
                  AND MonthNum = @MonthNum;

                SET @TailFormatted = RIGHT(REPLICATE('0', 10) + CAST(@NewTail AS VARCHAR(10)), @NoOfDigits);

                IF @NumberingType = 1
                BEGIN
                    SET @SerialFormatted = @BranchCode + CAST(@NewTail AS VARCHAR(20));
                    SET @SerialNumeric = CAST(@SerialFormatted AS FLOAT);
                END
                ELSE IF @NumberingType = 2
                BEGIN
                    SET @SerialFormatted = @BranchCode + @YearFormatted + @Month2 + @TailFormatted;
                    SET @SerialNumeric = CAST(@SerialFormatted AS FLOAT);
                END
                ELSE IF @NumberingType = 3
                BEGIN
                    SET @SerialFormatted = @BranchCode + @YearFormatted + @TailFormatted;
                    SET @SerialNumeric = CAST(@SerialFormatted AS FLOAT);
                END;
            END;

            IF @EndAt IS NOT NULL AND @NewTail >= @EndAt
            BEGIN
                SET @ErrorMsg = N'طھظ… طھط¬ط§ظˆط² ط§ظ„ط­ط¯ ط§ظ„ط£ظ‚طµظ‰ ظ„ظ„طھط±ظ‚ظٹظ…: ' + CAST(@EndAt AS NVARCHAR(20));
                SET @SerialFormatted = NULL;
                SET @SerialNumeric = NULL;
                SET @TailNumber = NULL;
                RETURN -1;
            END;

            IF EXISTS
            (
                SELECT 1
                FROM dbo.Transactions WITH (READCOMMITTEDLOCK)
                WHERE Transaction_Type = @TypeCode
                  AND NoteSerial1 = @SerialFormatted
                  AND (@EffectiveScope = N'Company' OR BranchId = @BranchID)
                  AND (@EffectiveScope <> N'BranchStore' OR ISNULL(StoreID, 0) = @StoreKey)
            )
            BEGIN
            SET @ErrorMsg =
                N'تم توليد رقم فاتورة مستخدم سابقا داخل نطاق الترقيم. Scope=' + @EffectiveScope +
                N'; BranchID=' + CONVERT(NVARCHAR(20), @BranchID) +
                N'; StoreKey=' + CONVERT(NVARCHAR(20), @StoreKey) +
                N'; Serial=' + CONVERT(NVARCHAR(50), @SerialFormatted);
            SET @SerialFormatted = NULL;
            SET @SerialNumeric = NULL;
            SET @TailNumber = NULL;
            RETURN -1;
            END;
        END;

        SET @TailNumber = @NewTail;
        SET @ErrorMsg = NULL;
        RETURN 0;
    END TRY
    BEGIN CATCH
        SET @ErrorMsg = ERROR_MESSAGE();
        SET @SerialFormatted = NULL;
        SET @SerialNumeric = NULL;
        SET @TailNumber = NULL;
        RETURN -1;
    END CATCH
END;
GO
