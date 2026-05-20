/*
RSMDB Apply Finance Approved Account Mapping - DRAFT ONLY
Purpose: after finance fills PropertyMigrationAccountFinanceApproval, apply approved resolutions to clone intelligence tables.
Safety: clone-only, no source writes, no journal migration, no posting.
*/

DECLARE @BatchId uniqueidentifier = '1b5d8eb5-dd7e-4b63-8ddb-e7b00aad558b';
DECLARE @CustomerCode nvarchar(100) = N'RSMDB-STAGING';

IF DB_NAME() NOT LIKE '%Clone%' AND DB_NAME() NOT LIKE '%Sandbox%' AND DB_NAME() NOT LIKE '%PropertyPilot%' AND DB_NAME() NOT LIKE '%ReadyToTest%' AND DB_NAME() NOT LIKE '%PilotClone%' AND DB_NAME() NOT LIKE '%Migration%'
BEGIN
    RAISERROR('Unsafe database. Apply mapping draft must run on clone/staging only.',16,1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.PropertyMigrationAccountFinanceApproval', N'U') IS NULL
BEGIN
    RAISERROR('PropertyMigrationAccountFinanceApproval does not exist. Create/fill approval table first.',16,1);
    RETURN;
END;

IF EXISTS (
    SELECT 1
    FROM dbo.PropertyMigrationAccountFinanceApproval a
    WHERE a.BatchId=@BatchId
      AND a.Decision IN (N'Approved',N'Changed',N'SuspenseApproved')
      AND a.Status=N'Approved'
      AND NULLIF(LTRIM(RTRIM(ISNULL(a.ApprovedBy,N''))),N'') IS NULL
)
BEGIN
    RAISERROR('Approved mapping rows require ApprovedBy.',16,1);
    RETURN;
END;

IF EXISTS (
    SELECT 1
    FROM dbo.PropertyMigrationAccountFinanceApproval a
    LEFT JOIN dbo.ChartOfAccount ca ON ca.Code COLLATE DATABASE_DEFAULT = a.ApprovedTargetAccountSerial COLLATE DATABASE_DEFAULT AND ISNULL(ca.IsDeleted,0)=0
    WHERE a.BatchId=@BatchId
      AND a.Decision IN (N'Approved',N'Changed')
      AND a.Status=N'Approved'
      AND ca.Id IS NULL
)
BEGIN
    RAISERROR('One or more approved target accounts do not exist in ChartOfAccount.',16,1);
    RETURN;
END;

BEGIN TRANSACTION;

UPDATE c
SET BestTargetAccountId = ca.Id,
    BestTargetAccountCode = ca.Code,
    Score = CASE WHEN a.Decision IN (N'Approved',N'Changed') THEN 100 WHEN a.Decision=N'SuspenseApproved' THEN 80 ELSE c.Score END,
    Band = CASE WHEN a.Decision IN (N'Approved',N'Changed') THEN N'FinanceApproved' WHEN a.Decision=N'SuspenseApproved' THEN N'SuspenseApproved' ELSE c.Band END,
    AutoApproved = CASE WHEN a.Decision IN (N'Approved',N'Changed') THEN 1 ELSE 0 END,
    RequiresFinanceReview = CASE WHEN a.Decision IN (N'Approved',N'Changed',N'SuspenseApproved') THEN 0 ELSE 1 END,
    BlockedReason = NULL
FROM dbo.PropertyMigrationAccountConfidence c
JOIN dbo.PropertyMigrationAccountFinanceApproval a ON a.BatchId=c.MigrationBatchId AND a.SourceAccountCode=c.SourceAccountCode
LEFT JOIN dbo.ChartOfAccount ca ON ca.Code COLLATE DATABASE_DEFAULT = a.ApprovedTargetAccountSerial COLLATE DATABASE_DEFAULT AND ISNULL(ca.IsDeleted,0)=0
WHERE c.MigrationBatchId=@BatchId
  AND a.Status=N'Approved'
  AND a.Decision IN (N'Approved',N'Changed',N'SuspenseApproved');

INSERT INTO dbo.PropertyMigrationAccountResolution(MigrationBatchId, CustomerCode, SourceAccountCode, TargetAccountId, TargetAccountCode, ResolutionStatus, ConfidenceScore, ApprovedBy, ApprovedAt, Notes)
SELECT @BatchId, @CustomerCode, a.SourceAccountCode, ca.Id, ca.Code,
       a.Decision, CASE WHEN a.Decision IN (N'Approved',N'Changed') THEN 100 ELSE 80 END,
       a.ApprovedBy, ISNULL(a.ApprovedAt, GETDATE()), a.Notes
FROM dbo.PropertyMigrationAccountFinanceApproval a
LEFT JOIN dbo.ChartOfAccount ca ON ca.Code COLLATE DATABASE_DEFAULT = a.ApprovedTargetAccountSerial COLLATE DATABASE_DEFAULT AND ISNULL(ca.IsDeleted,0)=0
WHERE a.BatchId=@BatchId
  AND a.Status=N'Approved'
  AND a.Decision IN (N'Approved',N'Changed',N'SuspenseApproved')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.PropertyMigrationAccountResolution r
      WHERE r.MigrationBatchId=@BatchId AND r.SourceAccountCode=a.SourceAccountCode AND r.ResolutionStatus=a.Decision
  );

COMMIT TRANSACTION;

SELECT AppliedApprovedMappings = COUNT(*)
FROM dbo.PropertyMigrationAccountResolution
WHERE MigrationBatchId=@BatchId AND ResolutionStatus IN (N'Approved',N'Changed',N'SuspenseApproved');
