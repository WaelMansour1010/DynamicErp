SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @FromDate datetime = '2026-05-16T00:00:00';
DECLARE @ToDate datetime = '2026-05-17T00:00:00';

DECLARE @TargetBatches TABLE
(
    BatchId int NOT NULL PRIMARY KEY,
    FileName nvarchar(260) NOT NULL,
    FirstFileName nvarchar(260) NOT NULL
);

INSERT INTO @TargetBatches (BatchId, FileName, FirstFileName)
SELECT b.BatchId,
       b.FileName,
       LTRIM(RTRIM(CASE WHEN CHARINDEX(',', b.FileName) > 0 THEN LEFT(b.FileName, CHARINDEX(',', b.FileName) - 1) ELSE b.FileName END))
FROM dbo.MasterDataImportBatch b
WHERE b.ImportStartedAt >= @FromDate
  AND b.ImportStartedAt < @ToDate;

DECLARE @TargetNotes TABLE
(
    BatchId int NOT NULL,
    NoteId bigint NOT NULL PRIMARY KEY,
    VoucherId bigint NULL,
    Remark nvarchar(500) NULL
);

INSERT INTO @TargetNotes (BatchId, NoteId, VoucherId, Remark)
SELECT b.BatchId,
       n.NoteID,
       n.Double_Entry_Vouchers_ID,
       n.Remark
FROM @TargetBatches b
JOIN dbo.Notes1 n
  ON n.NoteType = 101
 AND n.Remark LIKE N'%Excel%'
 AND n.Remark LIKE N'%' + b.FirstFileName + N'%';

SELECT N'Target batches' AS Section, *
FROM @TargetBatches
ORDER BY BatchId;

SELECT N'Target notes' AS Section, *
FROM @TargetNotes
ORDER BY BatchId, NoteId;

IF EXISTS (SELECT 1 FROM @TargetNotes)
BEGIN
    IF COL_LENGTH('dbo.ACCOUNTS', 'opening_balance') IS NOT NULL
    BEGIN
        ;WITH OpeningImpact AS
        (
            SELECT v.Account_Code,
                   SUM(CASE WHEN v.Credit_Or_Debit = 0 THEN v.Value ELSE -v.Value END) AS SignedDelta
            FROM dbo.DOUBLE_ENTREY_VOUCHERS1 v
            JOIN @TargetNotes t ON t.NoteId = v.Notes_ID
            GROUP BY v.Account_Code
        )
        UPDATE a
        SET a.opening_balance = ISNULL(a.opening_balance, 0) - i.SignedDelta,
            a.opening_balance_type =
                CASE
                    WHEN ISNULL(a.opening_balance, 0) - i.SignedDelta < 0 THEN 1
                    WHEN ISNULL(a.opening_balance, 0) - i.SignedDelta > 0 THEN 0
                    ELSE ISNULL(a.opening_balance_type, 0)
                END
        FROM dbo.ACCOUNTS a
        JOIN OpeningImpact i ON i.Account_Code = a.Account_Code;
    END;

    DELETE v
    FROM dbo.DOUBLE_ENTREY_VOUCHERS1 v
    JOIN @TargetNotes t ON t.NoteId = v.Notes_ID;

    DELETE n
    FROM dbo.Notes1 n
    JOIN @TargetNotes t ON t.NoteId = n.NoteID;
END;

IF OBJECT_ID('dbo.MasterDataImportJournalReview', 'U') IS NOT NULL
BEGIN
    DELETE r
    FROM dbo.MasterDataImportJournalReview r
    JOIN @TargetBatches b ON b.BatchId = r.BatchId;
END;

IF OBJECT_ID('dbo.MasterDataImportJournalFileLog', 'U') IS NOT NULL
BEGIN
    DELETE f
    FROM dbo.MasterDataImportJournalFileLog f
    JOIN @TargetBatches b ON b.BatchId = f.BatchId;
END;

DELETE b
FROM dbo.MasterDataImportBatch b
JOIN @TargetBatches t ON t.BatchId = b.BatchId;

SELECT N'Remaining batches on date' AS Section,
       COUNT(*) AS RemainingBatchCount
FROM dbo.MasterDataImportBatch
WHERE ImportStartedAt >= @FromDate
  AND ImportStartedAt < @ToDate;

SELECT N'Remaining opening notes by imported Excel remark' AS Section,
       COUNT(*) AS RemainingOpeningNotes
FROM dbo.Notes1
WHERE NoteType = 101
  AND Remark LIKE N'%Excel%';

COMMIT TRANSACTION;
