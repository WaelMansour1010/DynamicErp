/* RSMDB Operational Link Resolution + Minimal Operational Migration - Clone only, no accounting posting. */
SET NOCOUNT ON;
DECLARE @BatchId uniqueidentifier='$(MigrationBatchId)';
DECLARE @CustomerCode nvarchar(100)=N'$(CustomerCode)';

IF DB_NAME() NOT LIKE '%Clone%' AND DB_NAME() NOT LIKE '%Sandbox%' AND DB_NAME() NOT LIKE '%PropertyPilot%' AND DB_NAME() NOT LIKE '%ReadyToTest%' AND DB_NAME() NOT LIKE '%PilotClone%' AND DB_NAME() NOT LIKE '%Migration%'
BEGIN RAISERROR('Clone database required.',16,1); RETURN; END;
IF DB_NAME() IN ('RSMDB','Adnan','Alromaizan','MyErp') BEGIN RAISERROR('Blocked database.',16,1); RETURN; END;

IF OBJECT_ID(N'dbo.PropertyMigrationReceiptLinkCandidate', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyMigrationReceiptLinkCandidate(
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MigrationBatchId uniqueidentifier NOT NULL,
        CustomerCode nvarchar(100) NOT NULL,
        SourceReceiptId nvarchar(400) NOT NULL,
        SuggestedContractSourceId nvarchar(400) NULL,
        SuggestedInstallmentSourceId nvarchar(400) NULL,
        SuggestedRenterSourceId nvarchar(400) NULL,
        MatchStrategy nvarchar(200) NOT NULL,
        MatchConfidence int NOT NULL,
        LinkBand nvarchar(60) NOT NULL,
        Signals nvarchar(max) NULL,
        CreatedAt datetime NOT NULL DEFAULT(GETDATE())
    );
END;
DELETE FROM dbo.PropertyMigrationReceiptLinkCandidate WHERE MigrationBatchId=@BatchId;

/* Entity maps for existing rows accidentally/previously inserted by code. */
INSERT INTO dbo.PropertyMigrationEntityMap(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,TargetTableName,TargetId,EntityType,Status,UsedFallback,RequiresManualReview,Notes)
SELECT @BatchId,@CustomerCode,s.SourceDatabaseName,s.SourceTableName,s.SourceId,N'Property',p.Id,N'Property',N'MappedExisting',0,0,N'Operational link resolution matched existing property by code'
FROM dbo.PropertyMigrationSourceProperty s
JOIN dbo.Property p ON p.Code COLLATE DATABASE_DEFAULT=s.SourceCode COLLATE DATABASE_DEFAULT AND ISNULL(p.IsDeleted,0)=0
WHERE s.MigrationBatchId=@BatchId AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationEntityMap m WHERE m.MigrationBatchId=@BatchId AND m.EntityType=N'Property' AND m.SourceId=s.SourceId);

DECLARE @PropertyOut TABLE(SourceDatabaseName sysname,SourceTableName nvarchar(128),SourceId nvarchar(400),TargetId int);
MERGE dbo.Property AS tgt
USING (
    SELECT SourceDatabaseName,SourceTableName,SourceId,
           Code=ISNULL(NULLIF(SourceCode,N''),N'RSMDB-PROP-' + SourceId),
           ArName=ISNULL(NULLIF(CONVERT(nvarchar(500),ArName),N''),N'RSMDB Property ' + SourceId),
           EnName, Notes,
           PropertyTypeId=CASE WHEN EXISTS(SELECT 1 FROM dbo.PropertyType pt WHERE pt.Id=s.PropertyTypeId) THEN s.PropertyTypeId ELSE NULL END,
           DepartmentId=CASE WHEN EXISTS(SELECT 1 FROM dbo.Department d WHERE d.Id=s.DepartmentId) THEN s.DepartmentId ELSE NULL END
    FROM dbo.PropertyMigrationSourceProperty s
    WHERE s.MigrationBatchId=@BatchId AND s.IsValid=1
      AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationEntityMap m WHERE m.MigrationBatchId=@BatchId AND m.EntityType=N'Property' AND m.SourceId=s.SourceId)
) src ON 1=0
WHEN NOT MATCHED THEN INSERT(Code,ArName,EnName,IsActive,IsDeleted,Notes,PropertyTypeId,DepartmentId)
VALUES(src.Code,src.ArName,src.EnName,1,0,src.Notes,src.PropertyTypeId,src.DepartmentId)
OUTPUT src.SourceDatabaseName,src.SourceTableName,src.SourceId,inserted.Id INTO @PropertyOut;

INSERT INTO dbo.PropertyMigrationEntityMap(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,TargetTableName,TargetId,EntityType,Status,UsedFallback,RequiresManualReview,Notes)
SELECT @BatchId,@CustomerCode,SourceDatabaseName,SourceTableName,SourceId,N'Property',TargetId,N'Property',N'Inserted',0,0,N'RSMDB minimal operational migration'
FROM @PropertyOut;

INSERT INTO dbo.PropertyMigrationEntityMap(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,TargetTableName,TargetId,EntityType,Status,UsedFallback,RequiresManualReview,Notes)
SELECT @BatchId,@CustomerCode,s.SourceDatabaseName,s.SourceTableName,s.SourceId,N'PropertyRenter',r.Id,N'Renter',N'MappedExisting',0,0,N'Operational link resolution matched existing renter by code'
FROM dbo.PropertyMigrationSourceRenter s
JOIN dbo.PropertyRenter r ON r.Code COLLATE DATABASE_DEFAULT=s.SourceCode COLLATE DATABASE_DEFAULT AND ISNULL(r.IsDeleted,0)=0
WHERE s.MigrationBatchId=@BatchId AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationEntityMap m WHERE m.MigrationBatchId=@BatchId AND m.EntityType=N'Renter' AND m.SourceId=s.SourceId);

DECLARE @RenterOut TABLE(SourceDatabaseName sysname,SourceTableName nvarchar(128),SourceId nvarchar(400),TargetId int);
MERGE dbo.PropertyRenter AS tgt
USING (
    SELECT SourceDatabaseName,SourceTableName,SourceId,
           Code=ISNULL(NULLIF(SourceCode,N''),N'RSMDB-RENTER-' + SourceId),
           ArName=ISNULL(NULLIF(CONVERT(nvarchar(500),ArName),N''),N'RSMDB Renter ' + SourceId),
           EnName, Notes, Mobile, Phone, NationalNo, VATNo,
           DepartmentId=CASE WHEN EXISTS(SELECT 1 FROM dbo.Department d WHERE d.Id=s.DepartmentId) THEN s.DepartmentId ELSE NULL END,
           AccountId=CASE WHEN EXISTS(SELECT 1 FROM dbo.ChartOfAccount ca WHERE ca.Id=s.AccountId) THEN s.AccountId ELSE NULL END,
           OpeningDebitBalance, OpeningCreditBalance
    FROM dbo.PropertyMigrationSourceRenter s
    WHERE s.MigrationBatchId=@BatchId AND s.IsValid=1
      AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationEntityMap m WHERE m.MigrationBatchId=@BatchId AND m.EntityType=N'Renter' AND m.SourceId=s.SourceId)
) src ON 1=0
WHEN NOT MATCHED THEN INSERT(Code,ArName,EnName,IsActive,IsDeleted,DepartmentId,Mobile,Phone,NationalNo,VATNo,Notes,AccountId,OpeningDebitBalance,OpeningCreditBalance)
VALUES(src.Code,src.ArName,src.EnName,1,0,src.DepartmentId,src.Mobile,src.Phone,src.NationalNo,src.VATNo,src.Notes,src.AccountId,src.OpeningDebitBalance,src.OpeningCreditBalance)
OUTPUT src.SourceDatabaseName,src.SourceTableName,src.SourceId,inserted.Id INTO @RenterOut;

INSERT INTO dbo.PropertyMigrationEntityMap(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,TargetTableName,TargetId,EntityType,Status,UsedFallback,RequiresManualReview,Notes)
SELECT @BatchId,@CustomerCode,SourceDatabaseName,SourceTableName,SourceId,N'PropertyRenter',TargetId,N'Renter',N'Inserted',0,0,N'RSMDB minimal operational migration'
FROM @RenterOut;

DECLARE @UnitOut TABLE(SourceDatabaseName sysname,SourceTableName nvarchar(128),SourceId nvarchar(400),TargetId int);
MERGE dbo.PropertyUnit AS tgt
USING (
    SELECT SourceDatabaseName,SourceTableName,SourceId,
           Code=N'RSMDB-UNIT-' + SourceId,
           ArName=COALESCE(NULLIF(CONVERT(nvarchar(500),ArName),N''), NULLIF(SourceCode,N''), N'RSMDB Unit ' + SourceId),
           EnName, Notes,
           PropertyUnitTypeId=CASE WHEN EXISTS(SELECT 1 FROM dbo.PropertyUnitType put WHERE put.Id=s.PropertyUnitTypeId) THEN s.PropertyUnitTypeId ELSE NULL END,
           PropertyUnitStatusId=NULL
    FROM dbo.PropertyMigrationSourceUnit s
    WHERE s.MigrationBatchId=@BatchId AND s.IsValid=1
      AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationEntityMap m WHERE m.MigrationBatchId=@BatchId AND m.EntityType=N'Unit' AND m.SourceId=s.SourceId)
) src ON 1=0
WHEN NOT MATCHED THEN INSERT(Code,ArName,EnName,IsActive,IsDeleted,Notes,PropertyUnitTypeId,PropertyUnitStatusId)
VALUES(src.Code,src.ArName,src.EnName,1,0,src.Notes,src.PropertyUnitTypeId,src.PropertyUnitStatusId)
OUTPUT src.SourceDatabaseName,src.SourceTableName,src.SourceId,inserted.Id INTO @UnitOut;

INSERT INTO dbo.PropertyMigrationEntityMap(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,TargetTableName,TargetId,EntityType,Status,UsedFallback,RequiresManualReview,Notes)
SELECT @BatchId,@CustomerCode,SourceDatabaseName,SourceTableName,SourceId,N'PropertyUnit',TargetId,N'Unit',N'Inserted',0,0,N'RSMDB minimal operational migration'
FROM @UnitOut;

DECLARE @ContractOut TABLE(SourceDatabaseName sysname,SourceTableName nvarchar(128),SourceId nvarchar(400),TargetId int);
MERGE dbo.PropertyContract AS tgt
USING (
    SELECT s.SourceDatabaseName,s.SourceTableName,s.SourceId,s.DocumentNumber,s.VoucherDate,pm.TargetId PropertyId,um.TargetId UnitId,rm.TargetId RenterId,
           s.RentValue,s.NetTotal,s.TotalAfterTaxes,s.VATPercentage,s.VATValue,s.ContractStartDate,s.ContractEndDate,PropertyUnitTypeId=CASE WHEN EXISTS(SELECT 1 FROM dbo.PropertyUnitType put WHERE put.Id=s.PropertyUnitTypeId) THEN s.PropertyUnitTypeId ELSE NULL END,
           DepartmentId=CASE WHEN EXISTS(SELECT 1 FROM dbo.Department d WHERE d.Id=s.DepartmentId) THEN s.DepartmentId ELSE NULL END,
           s.NumberOfBatches,s.FirstBatchDate,s.PeriodBetweenBatchesNum,s.PeriodBetweenBatchesTypeId,s.Notes
    FROM dbo.PropertyMigrationSourceContract s
    JOIN dbo.PropertyMigrationEntityMap pm ON pm.MigrationBatchId=@BatchId AND pm.EntityType=N'Property' AND pm.SourceId=s.SourcePropertyId
    JOIN dbo.PropertyMigrationEntityMap um ON um.MigrationBatchId=@BatchId AND um.EntityType=N'Unit' AND um.SourceId=s.SourceUnitId
    JOIN dbo.PropertyMigrationEntityMap rm ON rm.MigrationBatchId=@BatchId AND rm.EntityType=N'Renter' AND rm.SourceId=s.SourceRenterId
    WHERE s.MigrationBatchId=@BatchId AND s.IsValid=1 AND s.IsActiveContract=1
      AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationEntityMap m WHERE m.MigrationBatchId=@BatchId AND m.EntityType=N'Contract' AND m.SourceId=s.SourceId)
) src ON 1=0
WHEN NOT MATCHED THEN INSERT(DocumentNumber,VoucherDate,PropertyId,PropertyUnitId,PropertyRenterId,RentValue,NetTotal,TotalAfterTaxes,VATPercentage,VATValue,ContractStartDate,ContractEndDate,PropertyUnitTypeId,DepartmentId,NumberOfBatches,FirstBatchDate,PeriodBetweenBatchesNum,PeriodBetweenBatchesTypeId,IsDeleted,IsRenewed,Notes)
VALUES(src.DocumentNumber,src.VoucherDate,src.PropertyId,src.UnitId,src.RenterId,src.RentValue,src.NetTotal,src.TotalAfterTaxes,src.VATPercentage,src.VATValue,src.ContractStartDate,src.ContractEndDate,src.PropertyUnitTypeId,src.DepartmentId,src.NumberOfBatches,src.FirstBatchDate,src.PeriodBetweenBatchesNum,src.PeriodBetweenBatchesTypeId,0,0,src.Notes)
OUTPUT src.SourceDatabaseName,src.SourceTableName,src.SourceId,inserted.Id INTO @ContractOut;

INSERT INTO dbo.PropertyMigrationEntityMap(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,TargetTableName,TargetId,EntityType,Status,UsedFallback,RequiresManualReview,Notes)
SELECT @BatchId,@CustomerCode,SourceDatabaseName,SourceTableName,SourceId,N'PropertyContract',TargetId,N'Contract',N'Inserted',0,0,N'RSMDB minimal operational migration'
FROM @ContractOut;

DECLARE @InstallmentOut TABLE(SourceDatabaseName sysname,SourceTableName nvarchar(128),SourceId nvarchar(400),TargetId int);
MERGE dbo.PropertyContractBatch AS tgt
USING (
    SELECT s.*, cm.TargetId ContractId
    FROM dbo.PropertyMigrationSourceInstallment s
    JOIN dbo.PropertyMigrationEntityMap cm ON cm.MigrationBatchId=@BatchId AND cm.EntityType=N'Contract' AND cm.SourceId=s.SourceContractId
    WHERE s.MigrationBatchId=@BatchId AND s.IsValid=1
      AND NOT EXISTS(SELECT 1 FROM dbo.PropertyMigrationEntityMap m WHERE m.MigrationBatchId=@BatchId AND m.EntityType=N'Installment' AND m.SourceId=s.SourceId)
) src ON 1=0
WHEN NOT MATCHED THEN INSERT(MainDocId,BatchNo,BatchDate,BatchRentValue,BatchRentValueTaxes,BatchWaterValue,BatchWaterValueTaxes,BatchElectricityValue,BatchElectricityValueTaxes,BatchCommissionValue,BatchTotal,IsDeleted,Notes,IsDelivered,IsRegisteredAsDue,BatchCommissionValueTaxes,BatchGasValue,BatchGasValueTaxes,BatchServicesValue,BatchServicesValueTaxes,BatchInsuranceValue,BatchInsuranceValueTaxes,IsRegisteredAsRevenue)
VALUES(src.ContractId,src.BatchNo,src.BatchDate,ISNULL(src.BatchRentValue,0),ISNULL(src.BatchRentValueTaxes,0),ISNULL(src.BatchWaterValue,0),ISNULL(src.BatchWaterValueTaxes,0),ISNULL(src.BatchElectricityValue,0),ISNULL(src.BatchElectricityValueTaxes,0),ISNULL(src.BatchCommissionValue,0),ISNULL(src.BatchTotal,0),0,src.Notes,0,0,ISNULL(src.BatchCommissionValueTaxes,0),ISNULL(src.BatchGasValue,0),ISNULL(src.BatchGasValueTaxes,0),ISNULL(src.BatchServicesValue,0),ISNULL(src.BatchServicesValueTaxes,0),ISNULL(src.BatchInsuranceValue,0),ISNULL(src.BatchInsuranceValueTaxes,0),0)
OUTPUT src.SourceDatabaseName,src.SourceTableName,src.SourceId,inserted.Id INTO @InstallmentOut;

INSERT INTO dbo.PropertyMigrationEntityMap(MigrationBatchId,CustomerCode,SourceDatabaseName,SourceTableName,SourceId,TargetTableName,TargetId,EntityType,Status,UsedFallback,RequiresManualReview,Notes)
SELECT @BatchId,@CustomerCode,SourceDatabaseName,SourceTableName,SourceId,N'PropertyContractBatch',TargetId,N'Installment',N'Inserted',0,0,N'RSMDB minimal operational migration'
FROM @InstallmentOut;

/* Link candidates for the current finance-approved Type4 set: source-to-source evidence only. */
;WITH Ready AS (
    SELECT CONVERT(int, rr.SourceId) NoteID
    FROM dbo.PropertyMigrationResolutionResult rr
    JOIN RSMDB.dbo.Notes n ON n.NoteID=CONVERT(int,rr.SourceId)
    WHERE rr.MigrationBatchId=@BatchId AND rr.EntityType=N'Journal' AND rr.ResolutionStatus=N'ReadyForMigration_FinanceApproved' AND n.NoteType=4
), Matches AS (
    SELECT r.NoteID,n.Note_Value,n.NoteDate,i.SourceId InstallmentId,i.SourceContractId,c.SourceRenterId,i.BatchTotal,i.BatchDate,
           rn=ROW_NUMBER() OVER(PARTITION BY r.NoteID ORDER BY ABS(DATEDIFF(day,i.BatchDate,n.NoteDate)), i.SourceId),
           cnt=COUNT(*) OVER(PARTITION BY r.NoteID)
    FROM Ready r
    JOIN RSMDB.dbo.Notes n ON n.NoteID=r.NoteID
    JOIN dbo.PropertyMigrationSourceInstallment i ON i.MigrationBatchId=@BatchId
      AND ABS(CAST(i.BatchTotal AS decimal(19,4))-CAST(n.Note_Value AS decimal(19,4)))<0.01
      AND ABS(DATEDIFF(day,i.BatchDate,n.NoteDate))<=31
    JOIN dbo.PropertyMigrationSourceContract c ON c.MigrationBatchId=@BatchId AND c.SourceId=i.SourceContractId
)
INSERT INTO dbo.PropertyMigrationReceiptLinkCandidate(MigrationBatchId,CustomerCode,SourceReceiptId,SuggestedContractSourceId,SuggestedInstallmentSourceId,SuggestedRenterSourceId,MatchStrategy,MatchConfidence,LinkBand,Signals)
SELECT @BatchId,@CustomerCode,CONVERT(nvarchar(400),NoteID),SourceContractId,InstallmentId,SourceRenterId,N'AmountExactDateProximityOnly',
       CASE WHEN cnt=1 AND ABS(DATEDIFF(day,BatchDate,NoteDate))<=7 THEN 75 WHEN cnt=1 THEN 65 WHEN cnt>1 THEN 45 ELSE 20 END,
       CASE WHEN cnt=1 AND ABS(DATEDIFF(day,BatchDate,NoteDate))<=7 THEN N'MediumReview' WHEN cnt=1 THEN N'MediumReview' WHEN cnt>1 THEN N'WeakMatch' ELSE N'Blocked' END,
       N'Amount exact; date delta=' + CONVERT(nvarchar(20),ABS(DATEDIFF(day,BatchDate,NoteDate))) + N'; candidate count=' + CONVERT(nvarchar(20),cnt)
FROM Matches WHERE rn=1;

SELECT 'OperationalMigration' Stage,
       (SELECT COUNT(*) FROM @PropertyOut) InsertedProperties,
       (SELECT COUNT(*) FROM @UnitOut) InsertedUnits,
       (SELECT COUNT(*) FROM @RenterOut) InsertedRenters,
       (SELECT COUNT(*) FROM @ContractOut) InsertedContracts,
       (SELECT COUNT(*) FROM @InstallmentOut) InsertedInstallments,
       (SELECT COUNT(*) FROM dbo.PropertyMigrationReceiptLinkCandidate WHERE MigrationBatchId=@BatchId) ReceiptLinkCandidates;


