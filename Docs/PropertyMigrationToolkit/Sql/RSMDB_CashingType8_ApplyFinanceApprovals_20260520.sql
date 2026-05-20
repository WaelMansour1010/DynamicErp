/*
RSMDB CashingType=8 Apply Finance Approvals - 2026-05-20
Clone only. Applies scoped Score>=60 approvals for the CashingType=8 mini pilot.
No receipts, no journals, no posting, no RSMDB modification.
*/
SET NOCOUNT ON;
DECLARE @BatchId uniqueidentifier = '$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(50) = N'$(CustomerCode)';
DECLARE @ScopeName nvarchar(100) = N'RSMDB_CashingType8';
DECLARE @ApprovalBatchId uniqueidentifier = NEWID();

IF DB_NAME() NOT LIKE '%Clone%' AND DB_NAME() NOT LIKE '%Sandbox%' AND DB_NAME() NOT LIKE '%PropertyPilot%' AND DB_NAME() NOT LIKE '%ReadyToTest%' AND DB_NAME() NOT LIKE '%PilotClone%' AND DB_NAME() NOT LIKE '%Migration%'
BEGIN RAISERROR('Apply finance approvals requires clone/sandbox target database.',16,1); RETURN; END;
IF DB_NAME() IN ('RSMDB','Adnan','Alromaizan','MyErp')
BEGIN RAISERROR('Blocked database for finance approval apply.',16,1); RETURN; END;

IF OBJECT_ID('dbo.PropertyMigrationCashingType8FinanceApprovalPack','U') IS NULL
BEGIN RAISERROR('Finance approval pack is missing. Run RSMDB_CashingType8_FinanceApproval first.',16,1); RETURN; END;
IF OBJECT_ID('dbo.PropertyMigrationAccountFinanceApproval','U') IS NULL
BEGIN RAISERROR('PropertyMigrationAccountFinanceApproval table is missing.',16,1); RETURN; END;
IF OBJECT_ID('dbo.PropertyMigrationAccountResolution','U') IS NULL
BEGIN RAISERROR('PropertyMigrationAccountResolution table is missing.',16,1); RETURN; END;

IF COL_LENGTH('dbo.PropertyMigrationAccountFinanceApproval','ScopeName') IS NULL
    ALTER TABLE dbo.PropertyMigrationAccountFinanceApproval ADD ScopeName nvarchar(100) NULL;
IF COL_LENGTH('dbo.PropertyMigrationAccountFinanceApproval','ApprovalBatchId') IS NULL
    ALTER TABLE dbo.PropertyMigrationAccountFinanceApproval ADD ApprovalBatchId uniqueidentifier NULL;

IF EXISTS (
    SELECT 1
    FROM dbo.PropertyMigrationCashingType8FinanceApprovalPack p
    LEFT JOIN dbo.ChartOfAccount coa ON coa.Id=p.SuggestedTargetAccountId
    WHERE p.MigrationBatchId=@BatchId AND p.ScopeName=@ScopeName
      AND p.ConfidenceScore >= 60
      AND (p.SuggestedTargetAccountId IS NULL OR coa.Id IS NULL)
)
BEGIN RAISERROR('Score>=60 approval set contains missing target accounts.',16,1); RETURN; END;

IF OBJECT_ID('tempdb..#Approved') IS NOT NULL DROP TABLE #Approved;
SELECT p.SourceAccountCode,p.SourceAccountName,p.SuggestedTargetAccountId,p.SuggestedTargetAccountSerial,p.SuggestedTargetAccountName,p.ConfidenceScore
INTO #Approved
FROM dbo.PropertyMigrationCashingType8FinanceApprovalPack p
WHERE p.MigrationBatchId=@BatchId AND p.ScopeName=@ScopeName
  AND p.ConfidenceScore >= 60
  AND p.SuggestedTargetAccountId IS NOT NULL;

EXEC sp_executesql N'
INSERT INTO dbo.PropertyMigrationAccountFinanceApproval
(BatchId,CustomerCode,SourceAccountCode,SourceAccountName,SuggestedTargetAccountSerial,ApprovedTargetAccountSerial,Decision,ApprovedBy,ApprovedAt,Notes,Status,ScopeName,ApprovalBatchId)
SELECT @BatchId,@CustomerCode,a.SourceAccountCode,a.SourceAccountName,a.SuggestedTargetAccountSerial,a.SuggestedTargetAccountSerial,N''Approved'',N''MigrationPilot'',GETDATE(),N''Score>=60 Mini Pilot Approval'',N''Approved'',@ScopeName,@ApprovalBatchId
FROM #Approved a
WHERE NOT EXISTS (SELECT 1 FROM dbo.PropertyMigrationAccountFinanceApproval ex WHERE ex.BatchId=@BatchId AND ex.ScopeName=@ScopeName AND ex.SourceAccountCode=a.SourceAccountCode COLLATE DATABASE_DEFAULT AND ex.Status=N''Approved'');',
N'@BatchId uniqueidentifier,@CustomerCode nvarchar(50),@ScopeName nvarchar(100),@ApprovalBatchId uniqueidentifier',
@BatchId,@CustomerCode,@ScopeName,@ApprovalBatchId;

INSERT INTO dbo.PropertyMigrationAccountResolution
(MigrationBatchId,CustomerCode,SourceAccountCode,TargetAccountId,TargetAccountCode,ResolutionStatus,ConfidenceScore,ApprovedBy,ApprovedAt,Notes)
SELECT @BatchId,@CustomerCode,a.SourceAccountCode,a.SuggestedTargetAccountId,a.SuggestedTargetAccountSerial,N'Approved',100,N'MigrationPilot',GETDATE(),N'RSMDB_CashingType8 Score>=60 Mini Pilot Approval; ApprovalBatchId=' + CONVERT(nvarchar(36),@ApprovalBatchId)
FROM #Approved a
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PropertyMigrationAccountResolution r
    WHERE r.MigrationBatchId=@BatchId
      AND r.SourceAccountCode=a.SourceAccountCode COLLATE DATABASE_DEFAULT
      AND r.ResolutionStatus=N'Approved'
      AND r.TargetAccountId IS NOT NULL
);

SELECT ApprovalBatchId=@ApprovalBatchId, ApprovedAccounts=COUNT(*) FROM #Approved;
SELECT AppliedApprovalRows=COUNT(*) FROM dbo.PropertyMigrationAccountFinanceApproval WHERE BatchId=@BatchId AND ScopeName=@ScopeName AND ApprovalBatchId=@ApprovalBatchId;
SELECT TotalScopedApprovedAccounts=COUNT(*) FROM dbo.PropertyMigrationAccountResolution r JOIN #Approved a ON a.SourceAccountCode=r.SourceAccountCode COLLATE DATABASE_DEFAULT WHERE r.MigrationBatchId=@BatchId AND r.ResolutionStatus=N'Approved' AND r.TargetAccountId IS NOT NULL;
