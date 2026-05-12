/*
    POS KYC/Card activation protection
    Date: 2026-05-11

    Apply after reviewing 60_POS_KYC_CardActivation_Diagnostics.sql.
    The unique indexes are created only if the existing data is clean.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.usp_POS_ValidateKeshniCardActivation', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_ValidateKeshniCardActivation;
GO

CREATE PROCEDURE dbo.usp_POS_ValidateKeshniCardActivation
    @CardNo NVARCHAR(255),
    @NationalId NVARCHAR(255) = NULL,
    @Mobile NVARCHAR(50) = NULL,
    @ExcludeCustomerId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Token NVARCHAR(255) = NULLIF(LTRIM(RTRIM(@CardNo)), N'');
    DECLARE @CleanNationalId NVARCHAR(255) = NULLIF(LTRIM(RTRIM(@NationalId)), N'');
    DECLARE @CleanMobile NVARCHAR(50) = NULLIF(LTRIM(RTRIM(@Mobile)), N'');
    DECLARE @CardType INT = CASE WHEN LEN(@Token) = 18 THEN 18 ELSE 0 END;
    DECLARE @SameExisting BIT = 0;
    DECLARE @LockResult INT;
    DECLARE @LockResource NVARCHAR(255);

    IF @Token IS NULL
        RAISERROR(N'رقم الكارت مطلوب', 16, 1);

    SET @LockResource = N'POS.KYC.CardActivation.' + @Token;
    EXEC @LockResult = sys.sp_getapplock
        @Resource = @LockResource,
        @LockMode = 'Exclusive',
        @LockOwner = 'Transaction',
        @LockTimeout = 10000;

    IF @LockResult < 0
        RAISERROR(N'تعذر قفل التوكن أثناء التفعيل. برجاء المحاولة مرة أخرى.', 16, 1);

    IF @ExcludeCustomerId IS NOT NULL
       AND EXISTS
       (
           SELECT 1
           FROM dbo.TblCusCsh WITH (UPDLOCK, HOLDLOCK)
           WHERE Id = @ExcludeCustomerId
             AND ISNULL(EasyCashType, 0) = 0
             AND LTRIM(RTRIM(ISNULL(CardNo, N''))) = @Token
       )
        SET @SameExisting = 1;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.TblCusCsh WITH (UPDLOCK, HOLDLOCK)
        WHERE ISNULL(EasyCashType, 0) = 0
          AND LTRIM(RTRIM(ISNULL(CardNo, N''))) = @Token
          AND (@ExcludeCustomerId IS NULL OR Id <> @ExcludeCustomerId)
    )
        RAISERROR(N'هذا الكارت/التوكن تم تفعيله من قبل ولا يمكن استخدامه مرة أخرى.', 16, 1);

    IF @CleanNationalId IS NOT NULL
       AND EXISTS
       (
           SELECT 1
           FROM dbo.TblCusCsh WITH (UPDLOCK, HOLDLOCK)
           WHERE ISNULL(EasyCashType, 0) = 0
             AND LTRIM(RTRIM(ISNULL(Tet_NumPoket, N''))) = @CleanNationalId
             AND (@ExcludeCustomerId IS NULL OR Id <> @ExcludeCustomerId)
             AND ((@CardType = 18 AND LEN(LTRIM(RTRIM(ISNULL(CardNo, N'')))) = 18)
                  OR (@CardType <> 18 AND LEN(LTRIM(RTRIM(ISNULL(CardNo, N'')))) <> 18))
       )
        RAISERROR(N'هذا العميل لديه كارت مفعل بالفعل من نفس النوع. مسموح فقط بكارت واحد من كل نوع.', 16, 1);

    IF @CleanNationalId IS NULL
       AND @CleanMobile IS NOT NULL
       AND EXISTS
       (
           SELECT 1
           FROM dbo.TblCusCsh WITH (UPDLOCK, HOLDLOCK)
           WHERE ISNULL(EasyCashType, 0) = 0
             AND LTRIM(RTRIM(ISNULL(PhoneNo2, N''))) = @CleanMobile
             AND (@ExcludeCustomerId IS NULL OR Id <> @ExcludeCustomerId)
             AND ((@CardType = 18 AND LEN(LTRIM(RTRIM(ISNULL(CardNo, N'')))) = 18)
                  OR (@CardType <> 18 AND LEN(LTRIM(RTRIM(ISNULL(CardNo, N'')))) <> 18))
       )
        RAISERROR(N'هذا العميل لديه كارت مفعل بالفعل من نفس النوع. مسموح فقط بكارت واحد من كل نوع.', 16, 1);

    IF @SameExisting = 0
       AND NOT EXISTS
       (
           SELECT 1
           FROM dbo.Transaction_Details td WITH (UPDLOCK, HOLDLOCK)
           INNER JOIN dbo.Transactions t WITH (UPDLOCK, HOLDLOCK)
               ON t.Transaction_ID = td.Transaction_ID
           WHERE t.Transaction_Type = 20
             AND LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))) = @Token
       )
        RAISERROR(N'هذا الكارت غير متاح بالمخزون أو تم صرفه/استخدامه من قبل.', 16, 1);

    IF @SameExisting = 0
       AND EXISTS
       (
           SELECT 1
           FROM dbo.Transaction_Details td WITH (UPDLOCK, HOLDLOCK)
           INNER JOIN dbo.Transactions t WITH (UPDLOCK, HOLDLOCK)
               ON t.Transaction_ID = td.Transaction_ID
           WHERE t.Transaction_Type = 19
             AND LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))) = @Token
       )
        RAISERROR(N'هذا الكارت غير متاح بالمخزون أو تم صرفه/استخدامه من قبل.', 16, 1);
END;
GO

IF COL_LENGTH(N'dbo.TblCusCsh', N'KycCardType') IS NULL
BEGIN
    ALTER TABLE dbo.TblCusCsh
    ADD KycCardType AS
        CASE
            WHEN LEN(LTRIM(RTRIM(ISNULL(CardNo, N'')))) = 18 THEN CONVERT(TINYINT, 18)
            ELSE CONVERT(TINYINT, 0)
        END;
END;
GO

IF COL_LENGTH(N'dbo.TblCusCsh', N'KycCardToken') IS NULL
BEGIN
    ALTER TABLE dbo.TblCusCsh
    ADD KycCardToken AS NULLIF(LTRIM(RTRIM(ISNULL(CardNo, N''))), N'');
END;
GO

IF COL_LENGTH(N'dbo.TblCusCsh', N'KycNationalId') IS NULL
BEGIN
    ALTER TABLE dbo.TblCusCsh
    ADD KycNationalId AS NULLIF(LTRIM(RTRIM(ISNULL(Tet_NumPoket, N''))), N'');
END;
GO

IF COL_LENGTH(N'dbo.TblCusCsh', N'KycIsActive') IS NULL
BEGIN
    ALTER TABLE dbo.TblCusCsh
    ADD KycIsActive AS CASE WHEN ISNULL(EasyCashType, 0) = 0 THEN CONVERT(BIT, 1) ELSE CONVERT(BIT, 0) END;
END;
GO

DECLARE @DirtyRows INT = 0;

;WITH Kyc AS
(
    SELECT
        Id,
        Token = LTRIM(RTRIM(ISNULL(CardNo, N''))),
        NationalId = LTRIM(RTRIM(ISNULL(Tet_NumPoket, N''))),
        CardType = CASE WHEN LEN(LTRIM(RTRIM(ISNULL(CardNo, N'')))) = 18 THEN 18 ELSE 0 END
    FROM dbo.TblCusCsh
    WHERE ISNULL(EasyCashType, 0) = 0
      AND NULLIF(LTRIM(RTRIM(ISNULL(CardNo, N''))), N'') IS NOT NULL
)
SELECT @DirtyRows = @DirtyRows + COUNT(*)
FROM
(
    SELECT Token
    FROM Kyc
    GROUP BY Token
    HAVING COUNT(*) > 1
) d;

;WITH Kyc AS
(
    SELECT
        Id,
        Token = LTRIM(RTRIM(ISNULL(CardNo, N''))),
        NationalId = LTRIM(RTRIM(ISNULL(Tet_NumPoket, N''))),
        CardType = CASE WHEN LEN(LTRIM(RTRIM(ISNULL(CardNo, N'')))) = 18 THEN 18 ELSE 0 END
    FROM dbo.TblCusCsh
    WHERE ISNULL(EasyCashType, 0) = 0
      AND NULLIF(LTRIM(RTRIM(ISNULL(CardNo, N''))), N'') IS NOT NULL
      AND NULLIF(LTRIM(RTRIM(ISNULL(Tet_NumPoket, N''))), N'') IS NOT NULL
)
SELECT @DirtyRows = @DirtyRows + COUNT(*)
FROM
(
    SELECT NationalId, CardType
    FROM Kyc
    GROUP BY NationalId, CardType
    HAVING COUNT(*) > 1
) d;

IF @DirtyRows > 0
BEGIN
    RAISERROR(N'KYC/card duplicate data exists. Run 60_POS_KYC_CardActivation_Diagnostics.sql and clean duplicates before creating unique indexes.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_POS_TblCusCsh_KycActiveToken' AND object_id = OBJECT_ID(N'dbo.TblCusCsh'))
BEGIN
    CREATE UNIQUE INDEX UX_POS_TblCusCsh_KycActiveToken
    ON dbo.TblCusCsh(KycCardToken)
    WHERE KycIsActive = 1
      AND KycCardToken IS NOT NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_POS_TblCusCsh_KycNationalCardType' AND object_id = OBJECT_ID(N'dbo.TblCusCsh'))
BEGIN
    CREATE UNIQUE INDEX UX_POS_TblCusCsh_KycNationalCardType
    ON dbo.TblCusCsh(KycNationalId, KycCardType)
    WHERE KycIsActive = 1
      AND KycNationalId IS NOT NULL
      AND KycCardToken IS NOT NULL;
END;
GO
