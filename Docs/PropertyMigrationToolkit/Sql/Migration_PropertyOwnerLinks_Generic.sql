/* Generic property-owner link migration. DynamicErp currently stores primary owner in Property.PropertyOwnerId. */
SET NOCOUNT ON;
DECLARE @MigrationBatchId uniqueidentifier='$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(50)=N'$(CustomerCode)';
IF DB_NAME() IN (N'Alromaizan',N'MyErp',N'Adnan',N'RSMDB') BEGIN RAISERROR('Blocked database.',16,1); RETURN; END;
IF DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%ReadyToTest%' AND DB_NAME() NOT LIKE N'%PilotClone%' AND DB_NAME() NOT LIKE N'%Sandbox%' AND DB_NAME() NOT LIKE N'%Migration%' BEGIN RAISERROR('Clone required.',16,1); RETURN; END;
IF OBJECT_ID(N'dbo.PropertyMigrationSourcePropertyOwner','U') IS NULL BEGIN RAISERROR('PropertyMigrationSourcePropertyOwner staging table is missing.',16,1); RETURN; END;

INSERT INTO dbo.PropertyMigrationReviewQueue(MigrationBatchId,CustomerCode,Priority,Severity,IssueType,EntityType,SourceDatabaseName,SourceTableName,SourceId,OriginalValue,SuggestedAction,Status)
SELECT @MigrationBatchId,@CustomerCode,2,N'Warning',N'MultipleOrPercentageOwnerReview',N'PropertyOwnerLink',SourceDatabaseName,SourceTableName,SourceId,CAST(ISNULL(OwnershipPercentage,100) AS nvarchar(50)),N'Confirm if DynamicErp needs multi-owner extension before GoLive',N'Open'
FROM dbo.PropertyMigrationSourcePropertyOwner s
WHERE s.MigrationBatchId=@MigrationBatchId AND (ISNULL(OwnershipPercentage,100)<>100 OR IsPrimaryOwner=0)
  AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationReviewQueue q WHERE q.MigrationBatchId=@MigrationBatchId AND q.EntityType=N'PropertyOwnerLink' AND q.SourceId=s.SourceId);

UPDATE p SET p.PropertyOwnerId = om.TargetId
FROM dbo.Property p
JOIN dbo.PropertyMigrationEntityMap pm ON pm.MigrationBatchId=@MigrationBatchId AND pm.EntityType=N'Property' AND pm.TargetId=p.Id
JOIN dbo.PropertyMigrationSourcePropertyOwner l ON l.MigrationBatchId=@MigrationBatchId AND l.SourcePropertyId=pm.SourceId AND l.IsPrimaryOwner=1
JOIN dbo.PropertyMigrationEntityMap om ON om.MigrationBatchId=@MigrationBatchId AND om.EntityType=N'Owner' AND om.SourceId=l.SourceOwnerId
WHERE p.PropertyOwnerId IS NULL;

INSERT INTO dbo.PropertyMigrationEntityMap(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,TargetTableName,TargetId,EntityType,Status,UsedFallback,RequiresManualReview,Notes)
SELECT @MigrationBatchId,@CustomerCode,l.SourceDatabaseName,l.SourceTableName,l.SourceId,N'Property.PropertyOwnerId',pm.TargetId,N'PropertyOwnerLink',N'Linked',0,CASE WHEN ISNULL(l.OwnershipPercentage,100)<>100 OR l.IsPrimaryOwner=0 THEN 1 ELSE 0 END,N'Primary owner linked to Property.PropertyOwnerId'
FROM dbo.PropertyMigrationSourcePropertyOwner l
JOIN dbo.PropertyMigrationEntityMap pm ON pm.MigrationBatchId=@MigrationBatchId AND pm.EntityType=N'Property' AND pm.SourceId=l.SourcePropertyId
JOIN dbo.PropertyMigrationEntityMap om ON om.MigrationBatchId=@MigrationBatchId AND om.EntityType=N'Owner' AND om.SourceId=l.SourceOwnerId
WHERE l.MigrationBatchId=@MigrationBatchId
  AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationEntityMap m WHERE m.MigrationBatchId=@MigrationBatchId AND m.EntityType=N'PropertyOwnerLink' AND m.SourceId=l.SourceId);

SELECT 'PropertyOwnerLinks' Stage, COUNT(*) Links FROM dbo.PropertyMigrationEntityMap WHERE MigrationBatchId=@MigrationBatchId AND EntityType=N'PropertyOwnerLink';
