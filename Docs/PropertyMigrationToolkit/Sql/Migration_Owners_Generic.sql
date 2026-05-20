/* Generic owners migration from PropertyMigrationSourceOwner staging. */
SET NOCOUNT ON;
DECLARE @MigrationBatchId uniqueidentifier='$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(50)=N'$(CustomerCode)';
DECLARE @MigrationMode nvarchar(30)=N'$(MigrationMode)';
DECLARE @DefaultDepartmentId int=NULL,@AllowSuspenseAccounts bit=0,@SuspenseAccountId int=NULL;
IF DB_NAME() IN (N'Alromaizan',N'MyErp',N'Adnan',N'RSMDB') BEGIN RAISERROR('Blocked database.',16,1); RETURN; END;
IF DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%ReadyToTest%' AND DB_NAME() NOT LIKE N'%PilotClone%' AND DB_NAME() NOT LIKE N'%Sandbox%' AND DB_NAME() NOT LIKE N'%Migration%' BEGIN RAISERROR('Clone required.',16,1); RETURN; END;
IF OBJECT_ID(N'dbo.PropertyMigrationSourceOwner','U') IS NULL BEGIN RAISERROR('PropertyMigrationSourceOwner staging table is missing.',16,1); RETURN; END;

SELECT TOP 1 @MigrationMode=MigrationMode,@DefaultDepartmentId=DefaultDepartmentId,@AllowSuspenseAccounts=AllowSuspenseAccounts,@SuspenseAccountId=SuspenseAccountId
FROM dbo.PropertyMigrationConfig WHERE CustomerCode=@CustomerCode AND TargetCloneDatabaseName=DB_NAME() ORDER BY ConfigId DESC;
IF @MigrationMode IS NULL SET @MigrationMode=N'Hybrid';

INSERT INTO dbo.PropertyMigrationWarning(MigrationBatchId,CustomerCode,Severity,IssueType,EntityType,SourceDatabaseName,SourceTableName,SourceId,OriginalValue,AppliedFix,FallbackEntity,RequiresManualReview,SuggestedAction,Message)
SELECT @MigrationBatchId,@CustomerCode,N'Warning',N'OwnerMissingAccount',N'Owner',SourceDatabaseName,SourceTableName,SourceId,ISNULL(AccountCode,N''),
       CASE WHEN @AllowSuspenseAccounts=1 THEN N'Use configured suspense account for review only' ELSE N'No accounting migration for owner until account is mapped' END,
       CASE WHEN @AllowSuspenseAccounts=1 THEN N'MIGRATION_SUSPENSE_ACCOUNT' ELSE NULL END,1,N'Map owner account before owner payments/GoLive',N'Owner has no mapped account.'
FROM dbo.PropertyMigrationSourceOwner WHERE MigrationBatchId=@MigrationBatchId AND AccountId IS NULL;

DECLARE @Out TABLE(SourceDatabaseName sysname,SourceTableName nvarchar(128),SourceId nvarchar(200),TargetId int,UsedFallback bit);
MERGE dbo.PropertyOwner AS tgt
USING (
    SELECT SourceDatabaseName,SourceTableName,SourceId,
           CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(SourceCode,N''))),N'') IS NULL THEN N'MIG-OWNER-' + CAST(Id AS nvarchar(20)) ELSE SourceCode END Code,
           CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(ArName,N''))),N'') IS NULL THEN N'MIGRATION UNKNOWN OWNER ' + CAST(Id AS nvarchar(20)) ELSE ArName END ArName,
           EnName,ISNULL(DepartmentId,@DefaultDepartmentId) DepartmentId,Mobile,Phone,VATNo,BankAccountNo,BankName,Notes,
           CASE WHEN AccountId IS NULL AND @AllowSuspenseAccounts=1 THEN @SuspenseAccountId ELSE AccountId END AccountId,
           CASE WHEN AccountId IS NULL AND @AllowSuspenseAccounts=1 THEN 1 ELSE 0 END UsedFallback
    FROM dbo.PropertyMigrationSourceOwner
    WHERE MigrationBatchId=@MigrationBatchId AND IsValid=1
      AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationEntityMap m WHERE m.MigrationBatchId=@MigrationBatchId AND m.EntityType=N'Owner' AND m.SourceId=PropertyMigrationSourceOwner.SourceId AND m.SourceTableName=PropertyMigrationSourceOwner.SourceTableName)
) src ON 1=0
WHEN NOT MATCHED THEN INSERT(Code,ArName,EnName,IsActive,IsDeleted,DepartmentId,Mobile,Phone,VATNo,Notes,AccountId)
VALUES(src.Code,src.ArName,src.EnName,1,0,src.DepartmentId,src.Mobile,src.Phone,src.VATNo,
       ISNULL(src.Notes,N'') + CASE WHEN src.BankAccountNo IS NOT NULL OR src.BankName IS NOT NULL THEN N' | Bank=' + ISNULL(src.BankName,N'') + N' ' + ISNULL(src.BankAccountNo,N'') ELSE N'' END,
       src.AccountId)
OUTPUT src.SourceDatabaseName,src.SourceTableName,src.SourceId,inserted.Id,src.UsedFallback INTO @Out;

INSERT INTO dbo.PropertyMigrationEntityMap(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,TargetTableName,TargetId,EntityType,Status,UsedFallback,FallbackEntity,RequiresManualReview,Notes)
SELECT @MigrationBatchId,@CustomerCode,SourceDatabaseName,SourceTableName,SourceId,N'PropertyOwner',TargetId,N'Owner',N'Inserted',UsedFallback,CASE WHEN UsedFallback=1 THEN N'MIGRATION_SUSPENSE_ACCOUNT' ELSE NULL END,UsedFallback,N'Generic owner migration'
FROM @Out;

SELECT 'Owners' Stage, COUNT(*) InsertedOwners FROM @Out;
