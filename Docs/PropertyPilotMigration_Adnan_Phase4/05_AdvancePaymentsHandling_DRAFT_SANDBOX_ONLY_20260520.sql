/*
05_AdvancePaymentsHandling_DRAFT_SANDBOX_ONLY_20260520.sql
Purpose: Stage advance payments against future installments for review.
Status: DRAFT ONLY. Does not post accounting. Review before execution.
*/

IF DB_NAME() IN (N'Adnan', N'Alromaizan') OR (DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%Sandbox%')
BEGIN
    RAISERROR('Blocked: this script can run only inside a PropertyPilot/Sandbox database, never Adnan or Alromaizan.', 16, 1);
    RETURN;
END;

IF OBJECT_ID('dbo.PropertyPilotAdvancePaymentStaging','U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyPilotAdvancePaymentStaging
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PropertyPilotAdvancePaymentStaging PRIMARY KEY,
        MigrationBatchId UNIQUEIDENTIFIER NOT NULL,
        OldDatabaseName NVARCHAR(128) NOT NULL,
        OldContractNo INT NOT NULL,
        OldInstallmentId INT NOT NULL,
        OldRenterId INT NULL,
        FutureInstallmentValue MONEY NOT NULL DEFAULT(0),
        AdvancePaidAmount MONEY NOT NULL DEFAULT(0),
        FutureRemainAfterAdvance MONEY NOT NULL DEFAULT(0),
        NewPropertyContractId INT NULL,
        NewPropertyContractBatchId INT NULL,
        NewPropertyRenterId INT NULL,
        NewAccountId INT NULL,
        TreatmentDecision NVARCHAR(100) NOT NULL DEFAULT(N'StageOnly_NoPosting'),
        CreatedAt DATETIME NOT NULL DEFAULT(GETDATE()),
        Notes NVARCHAR(MAX) NULL
    );
END;

DECLARE @MigrationBatchId UNIQUEIDENTIFIER;
DECLARE @CutoverDate DATETIME;
SET @MigrationBatchId = 'B7B0DA8D-1E0E-4A1D-A4AB-AD2026052001';
SET @CutoverDate = '20260520';

;WITH Paid AS (
    SELECT d.istallid,
           SUM(ISNULL(d.RentValuePayed,0)+ISNULL(d.CommissionsPayed,0)+ISNULL(d.InsurancePayed,0)+ISNULL(d.WaterPayed,0)+ISNULL(d.ElectricPayed,0)+ISNULL(d.TelandNetPayed,0)+ISNULL(d.OldValuePayed,0)+ISNULL(d.VATPayed,0)) TruePaid
    FROM Adnan.dbo.ContracttBillInstallmentsDone d
    LEFT JOIN Adnan.dbo.Notes n ON n.NoteID=d.NoteID
    WHERE n.NoteType=4 OR n.NoteType IS NULL
    GROUP BY d.istallid
)
INSERT INTO dbo.PropertyPilotAdvancePaymentStaging
(MigrationBatchId, OldDatabaseName, OldContractNo, OldInstallmentId, OldRenterId, FutureInstallmentValue, AdvancePaidAmount, FutureRemainAfterAdvance, NewPropertyContractId, NewPropertyContractBatchId, NewPropertyRenterId, NewAccountId, Notes)
SELECT @MigrationBatchId,
       N'Adnan',
       CAST(i.ContNo AS int),
       i.id,
       c.CusID,
       ISNULL(i.installValue,0),
       ISNULL(p.TruePaid,0),
       ISNULL(i.installValue,0)-ISNULL(p.TruePaid,0),
       cx.NewId,
       bx.NewId,
       tx.NewId,
       m.NewChartOfAccountId,
       N'Staged future advance only; no receipt/journal posting.'
FROM Adnan.dbo.TblContractInstallments i
INNER JOIN Adnan.dbo.TblContract c ON CAST(c.ContNo AS int)=CAST(i.ContNo AS int)
INNER JOIN dbo.PropertyPilotCrossReference cx ON cx.MigrationBatchId=@MigrationBatchId AND cx.OldDatabaseName=N'Adnan' AND cx.OldTableName=N'TblContract' AND cx.OldId=CAST(CAST(i.ContNo AS int) AS NVARCHAR(100)) AND cx.EntityType=N'Contract'
INNER JOIN dbo.PropertyPilotCrossReference bx ON bx.MigrationBatchId=@MigrationBatchId AND bx.OldDatabaseName=N'Adnan' AND bx.OldTableName=N'TblContractInstallments' AND bx.OldId=CAST(i.id AS NVARCHAR(100)) AND bx.EntityType=N'ContractBatch'
LEFT JOIN dbo.PropertyPilotCrossReference tx ON tx.MigrationBatchId=@MigrationBatchId AND tx.OldDatabaseName=N'Adnan' AND tx.OldTableName=N'TblCustemers' AND tx.OldId=CAST(c.CusID AS NVARCHAR(100)) AND tx.EntityType=N'Tenant'
LEFT JOIN Adnan.dbo.TblCustemers r ON r.CusID=c.CusID
LEFT JOIN dbo.PropertyPilotAccountMapping m ON m.MigrationBatchId=@MigrationBatchId AND m.OldDatabaseName=N'Adnan' AND m.OldAccountCode COLLATE DATABASE_DEFAULT=r.Account_Code COLLATE DATABASE_DEFAULT
LEFT JOIN Paid p ON p.istallid=i.id
LEFT JOIN dbo.PropertyPilotAdvancePaymentStaging existing ON existing.MigrationBatchId=@MigrationBatchId AND existing.OldDatabaseName=N'Adnan' AND existing.OldInstallmentId=i.id
WHERE i.Installdate > @CutoverDate
  AND ISNULL(p.TruePaid,0) > 0
  AND existing.Id IS NULL;

SELECT COUNT(*) AdvanceRows, SUM(AdvancePaidAmount) AdvancePaidAmount, SUM(FutureRemainAfterAdvance) FutureRemainAfterAdvance
FROM dbo.PropertyPilotAdvancePaymentStaging
WHERE MigrationBatchId=@MigrationBatchId;
