/* Phase7_AccountSeed_FIXED_SANDBOX_ONLY_20260520.sql */
DECLARE @MigrationBatchId UNIQUEIDENTIFIER;
DECLARE @CutoverDate DATETIME;
DECLARE @ParentAccountId INT;
SET @MigrationBatchId = 'E7EAD000-0000-4000-9000-202605200007';
SET @CutoverDate = '20260520';
SET @ParentAccountId = 764;

IF DB_NAME() = N'Alromaizan' OR (DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%Sandbox%')
BEGIN RAISERROR('Blocked: Sandbox/PropertyPilot database required.',16,1); RETURN; END;

BEGIN TRY
    BEGIN TRAN;

    ;WITH Settled AS (
        SELECT DISTINCT CAST(ContNo AS int) AS ContNo FROM Adnan.dbo.TblFiterWaiver WHERE ContNo IS NOT NULL
    ), ActiveContracts AS (
        SELECT c.* FROM Adnan.dbo.TblContract c
        LEFT JOIN Settled s ON s.ContNo=CAST(c.ContNo AS int)
        WHERE ISNULL(c.EndContract,0)=0 AND s.ContNo IS NULL AND (c.EndDate IS NULL OR c.EndDate >= @CutoverDate)
          AND c.Iqar IS NOT NULL AND c.UnitNo IS NOT NULL AND c.CusID IS NOT NULL
    ), RequiredAccounts AS (
        SELECT DISTINCT r.Account_Code, a.Account_ID, a.Account_Name
        FROM ActiveContracts c
        INNER JOIN Adnan.dbo.TblCustemers r ON r.CusID=c.CusID
        INNER JOIN Adnan.dbo.ACCOUNTS a ON a.Account_Code COLLATE DATABASE_DEFAULT = r.Account_Code COLLATE DATABASE_DEFAULT
        WHERE ISNULL(r.Account_Code COLLATE DATABASE_DEFAULT,'') <> ''
    )
    INSERT INTO dbo.ChartOfAccount(Code,ArName,EnName,ParentId,TypeId,ClassificationId,CategoryId,IsActive,IsDeleted,UserId,Notes)
    SELECT ra.Account_Code,
           ISNULL(ra.Account_Name,N'Adnan renter account '+ra.Account_Code),
           NULL,
           @ParentAccountId,
           1,
           3,
           1,
           1,
           0,
           NULL,
           N'PropertyPilot Adnan seeded renter account. OldAccountId=' + CAST(ra.Account_ID AS NVARCHAR(30))
    FROM RequiredAccounts ra
    LEFT JOIN dbo.ChartOfAccount ca ON ca.Code COLLATE DATABASE_DEFAULT = ra.Account_Code COLLATE DATABASE_DEFAULT
    LEFT JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.OldDatabaseName=N'Adnan' AND x.OldTableName=N'ACCOUNTS' AND x.OldId COLLATE DATABASE_DEFAULT=ra.Account_Code COLLATE DATABASE_DEFAULT AND x.EntityType=N'Account'
    WHERE ca.Id IS NULL AND x.Id IS NULL;

    ;WITH Settled AS (
        SELECT DISTINCT CAST(ContNo AS int) AS ContNo FROM Adnan.dbo.TblFiterWaiver WHERE ContNo IS NOT NULL
    ), ActiveContracts AS (
        SELECT c.* FROM Adnan.dbo.TblContract c
        LEFT JOIN Settled s ON s.ContNo=CAST(c.ContNo AS int)
        WHERE ISNULL(c.EndContract,0)=0 AND s.ContNo IS NULL AND (c.EndDate IS NULL OR c.EndDate >= @CutoverDate)
          AND c.Iqar IS NOT NULL AND c.UnitNo IS NOT NULL AND c.CusID IS NOT NULL
    ), RequiredAccounts AS (
        SELECT DISTINCT r.Account_Code, a.Account_ID, a.Account_Name
        FROM ActiveContracts c
        INNER JOIN Adnan.dbo.TblCustemers r ON r.CusID=c.CusID
        INNER JOIN Adnan.dbo.ACCOUNTS a ON a.Account_Code COLLATE DATABASE_DEFAULT = r.Account_Code COLLATE DATABASE_DEFAULT
        WHERE ISNULL(r.Account_Code COLLATE DATABASE_DEFAULT,'') <> ''
    )
    INSERT INTO dbo.PropertyPilotCrossReference(MigrationBatchId,OldDatabaseName,OldTableName,OldId,NewTableName,NewId,EntityType,Notes)
    SELECT @MigrationBatchId,N'Adnan',N'ACCOUNTS',ra.Account_Code,N'ChartOfAccount',ca.Id,N'Account',N'Phase7 fixed account seed'
    FROM RequiredAccounts ra
    INNER JOIN dbo.ChartOfAccount ca ON ca.Code COLLATE DATABASE_DEFAULT = ra.Account_Code COLLATE DATABASE_DEFAULT
    LEFT JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.OldDatabaseName=N'Adnan' AND x.OldTableName=N'ACCOUNTS' AND x.OldId COLLATE DATABASE_DEFAULT=ra.Account_Code COLLATE DATABASE_DEFAULT AND x.EntityType=N'Account'
    WHERE x.Id IS NULL;

    ;WITH Settled AS (
        SELECT DISTINCT CAST(ContNo AS int) AS ContNo FROM Adnan.dbo.TblFiterWaiver WHERE ContNo IS NOT NULL
    ), ActiveContracts AS (
        SELECT c.* FROM Adnan.dbo.TblContract c
        LEFT JOIN Settled s ON s.ContNo=CAST(c.ContNo AS int)
        WHERE ISNULL(c.EndContract,0)=0 AND s.ContNo IS NULL AND (c.EndDate IS NULL OR c.EndDate >= @CutoverDate)
          AND c.Iqar IS NOT NULL AND c.UnitNo IS NOT NULL AND c.CusID IS NOT NULL
    ), RequiredAccounts AS (
        SELECT DISTINCT r.Account_Code, a.Account_ID, a.Account_Name
        FROM ActiveContracts c
        INNER JOIN Adnan.dbo.TblCustemers r ON r.CusID=c.CusID
        INNER JOIN Adnan.dbo.ACCOUNTS a ON a.Account_Code COLLATE DATABASE_DEFAULT = r.Account_Code COLLATE DATABASE_DEFAULT
        WHERE ISNULL(r.Account_Code COLLATE DATABASE_DEFAULT,'') <> ''
    )
    INSERT INTO dbo.PropertyPilotAccountMapping(MigrationBatchId,OldDatabaseName,OldAccountCode,OldAccountId,OldAccountName,NewChartOfAccountId,NewAccountCode,MappingMode,IsApproved,Notes)
    SELECT @MigrationBatchId,N'Adnan',ra.Account_Code,ra.Account_ID,ra.Account_Name,ca.Id,ca.Code,N'Seed',1,N'Phase7 fixed account seed'
    FROM RequiredAccounts ra
    INNER JOIN dbo.ChartOfAccount ca ON ca.Code COLLATE DATABASE_DEFAULT = ra.Account_Code COLLATE DATABASE_DEFAULT
    LEFT JOIN dbo.PropertyPilotAccountMapping m ON m.MigrationBatchId=@MigrationBatchId AND m.OldDatabaseName=N'Adnan' AND m.OldAccountCode COLLATE DATABASE_DEFAULT=ra.Account_Code COLLATE DATABASE_DEFAULT
    WHERE m.Id IS NULL;

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    DECLARE @Msg NVARCHAR(4000); SET @Msg = ERROR_MESSAGE();
    RAISERROR(@Msg,16,1);
END CATCH;

SELECT 'AccountXref' AS Metric, COUNT(*) AS Value FROM dbo.PropertyPilotCrossReference WHERE MigrationBatchId=@MigrationBatchId AND EntityType=N'Account'
UNION ALL SELECT 'AccountMapping', COUNT(*) FROM dbo.PropertyPilotAccountMapping WHERE MigrationBatchId=@MigrationBatchId
UNION ALL SELECT 'SeededChartAccounts', COUNT(*) FROM dbo.ChartOfAccount WHERE Notes LIKE N'%PropertyPilot Adnan seeded renter account%';



