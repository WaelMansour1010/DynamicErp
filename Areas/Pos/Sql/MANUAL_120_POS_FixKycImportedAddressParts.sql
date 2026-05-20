/*
    Manual repair script: split KYC imported English address into bank address fields.
    SQL Server 2012 compatible.

    What it fixes:
    - Old Excel/Kishny token imports that stored the full English address in TblCusCsh.EnglishName5
      while EnglishName6 and EnglishName7 stayed empty.
    - Future bank export reads Address1/2/3 from EnglishName5/6/7, so this distributes the text
      into 35-character chunks without touching rows that are already distributed.

    Safety:
    - Review the SELECT output first.
    - Set @Apply = 1 to update.
*/

SET NOCOUNT ON;

DECLARE @Apply BIT;
SET @Apply = 0;

IF OBJECT_ID('tempdb..#KycAddressRepair') IS NOT NULL
    DROP TABLE #KycAddressRepair;

SELECT
    c.Id,
    c.CardNo,
    c.Tet_NumPoket,
    c.name AS CustomerName,
    OldEnglishName5 = LTRIM(RTRIM(ISNULL(c.EnglishName5, N''))),
    OldEnglishName6 = LTRIM(RTRIM(ISNULL(c.EnglishName6, N''))),
    OldEnglishName7 = LTRIM(RTRIM(ISNULL(c.EnglishName7, N''))),
    NewEnglishName5 = LEFT(LTRIM(RTRIM(ISNULL(c.EnglishName5, N''))), 35),
    NewEnglishName6 = NULLIF(LTRIM(RTRIM(SUBSTRING(LTRIM(RTRIM(ISNULL(c.EnglishName5, N''))), 36, 35))), N''),
    NewEnglishName7 = NULLIF(LTRIM(RTRIM(SUBSTRING(LTRIM(RTRIM(ISNULL(c.EnglishName5, N''))), 71, 35))), N''),
    c.CardSource,
    c.SaveDate
INTO #KycAddressRepair
FROM dbo.TblCusCsh c
WHERE
    LEN(LTRIM(RTRIM(ISNULL(c.EnglishName5, N'')))) > 35
    AND NULLIF(LTRIM(RTRIM(ISNULL(c.EnglishName6, N''))), N'') IS NULL
    AND NULLIF(LTRIM(RTRIM(ISNULL(c.EnglishName7, N''))), N'') IS NULL
    AND (
        ISNULL(c.CardSource, N'') LIKE N'%Excel%'
        OR EXISTS
        (
            SELECT 1
            FROM dbo.POS_ImportBatchRow r
            WHERE r.Token = c.CardNo
        )
    );

SELECT *
FROM #KycAddressRepair
ORDER BY Id;

IF @Apply = 1
BEGIN
    BEGIN TRANSACTION;

    UPDATE c
    SET
        EnglishName5 = r.NewEnglishName5,
        EnglishName6 = r.NewEnglishName6,
        EnglishName7 = r.NewEnglishName7,
        MailAdress = LEFT(
            LTRIM(RTRIM(
                ISNULL(r.NewEnglishName5, N'') + N' ' +
                ISNULL(r.NewEnglishName6, N'') + N' ' +
                ISNULL(r.NewEnglishName7, N'')
            )),
            255
        )
    FROM dbo.TblCusCsh c
    INNER JOIN #KycAddressRepair r ON r.Id = c.Id;

    SELECT @@ROWCOUNT AS UpdatedRows;

    COMMIT TRANSACTION;
END
ELSE
BEGIN
    SELECT COUNT(1) AS RowsThatWillBeUpdated_WhenApplyEquals1
    FROM #KycAddressRepair;
END
