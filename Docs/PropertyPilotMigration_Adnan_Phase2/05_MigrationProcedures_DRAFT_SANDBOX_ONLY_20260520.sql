/*
05_MigrationProcedures_DRAFT_SANDBOX_ONLY_20260520.sql
Purpose: Draft stored procedures for Adnan Property Pilot Migration into a sandbox clone of DynamicErp.
Status: DRAFT SANDBOX ONLY. Do not execute on production.
Compatibility: SQL Server 2012.

Safety:
- Every procedure blocks execution on [Alromaizan].
- Every procedure requires DB_NAME() to contain PropertyPilot or Sandbox.
- Procedures are idempotent through dbo.PropertyPilotCrossReference.
- Review target lookup mappings before execution.
*/

IF DB_NAME() = N'Alromaizan' OR (DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%Sandbox%')
BEGIN
    RAISERROR('Blocked: procedure deployment can run only in a PropertyPilot/Sandbox database, never Alromaizan.', 16, 1);
    RETURN;
END;
GO

IF OBJECT_ID('dbo.usp_PropertyPilot_SeedTenantAccounts_Adnan', 'P') IS NOT NULL DROP PROCEDURE dbo.usp_PropertyPilot_SeedTenantAccounts_Adnan;
GO
CREATE PROCEDURE dbo.usp_PropertyPilot_SeedTenantAccounts_Adnan
    @MigrationBatchId UNIQUEIDENTIFIER,
    @CutoverDate DATETIME,
    @ParentAccountId INT,
    @TypeId INT = NULL,
    @ClassificationId INT = NULL,
    @CategoryId INT = NULL,
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF DB_NAME() = N'Alromaizan' OR (DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%Sandbox%')
    BEGIN
        RAISERROR('Blocked: Sandbox/PropertyPilot database required.', 16, 1);
        RETURN;
    END;

    IF @ParentAccountId IS NULL
    BEGIN
        RAISERROR('Parent account is required for sandbox account seeding.', 16, 1);
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRAN;

        ;WITH Settled AS (
            SELECT DISTINCT CAST(ContNo AS int) AS ContNo FROM Adnan.dbo.TblFiterWaiver WHERE ContNo IS NOT NULL
        ), ActiveContracts AS (
            SELECT c.* FROM Adnan.dbo.TblContract c
            LEFT JOIN Settled s ON s.ContNo = CAST(c.ContNo AS int)
            WHERE ISNULL(c.EndContract,0)=0 AND s.ContNo IS NULL AND (c.EndDate IS NULL OR c.EndDate >= @CutoverDate)
              AND c.Iqar IS NOT NULL AND c.UnitNo IS NOT NULL AND c.CusID IS NOT NULL
        ), RequiredAccounts AS (
            SELECT DISTINCT r.Account_Code, a.Account_ID, a.Account_Name
            FROM ActiveContracts c
            INNER JOIN Adnan.dbo.TblCustemers r ON r.CusID = c.CusID
            INNER JOIN Adnan.dbo.ACCOUNTS a ON a.Account_Code = r.Account_Code
            WHERE ISNULL(r.Account_Code,'') <> ''
        )
        INSERT INTO dbo.ChartOfAccount
        (
            Code, ArName, EnName, ParentId, TypeId, ClassificationId, CategoryId,
            IsActive, IsDeleted, UserId, Notes
        )
        SELECT ra.Account_Code,
               ISNULL(ra.Account_Name, N'Adnan renter account ' + ra.Account_Code),
               NULL,
               @ParentAccountId,
               @TypeId,
               @ClassificationId,
               @CategoryId,
               1,
               0,
               @UserId,
               N'PropertyPilot Adnan seeded renter account. OldAccountId=' + CAST(ra.Account_ID AS NVARCHAR(30))
        FROM RequiredAccounts ra
        LEFT JOIN dbo.ChartOfAccount ca ON ca.Code = ra.Account_Code
        LEFT JOIN dbo.PropertyPilotCrossReference x
               ON x.MigrationBatchId = @MigrationBatchId
              AND x.OldDatabaseName = N'Adnan'
              AND x.OldTableName = N'ACCOUNTS'
              AND x.OldId = ra.Account_Code
              AND x.NewTableName = N'ChartOfAccount'
              AND x.EntityType = N'Account'
        WHERE ca.Id IS NULL AND x.Id IS NULL;

        INSERT INTO dbo.PropertyPilotCrossReference
        (MigrationBatchId, OldDatabaseName, OldTableName, OldId, NewTableName, NewId, EntityType, Notes)
        SELECT @MigrationBatchId, N'Adnan', N'ACCOUNTS', ra.Account_Code, N'ChartOfAccount', ca.Id, N'Account', N'Seeded or matched tenant account'
        FROM RequiredAccounts ra
        INNER JOIN dbo.ChartOfAccount ca ON ca.Code = ra.Account_Code
        LEFT JOIN dbo.PropertyPilotCrossReference x
               ON x.MigrationBatchId = @MigrationBatchId
              AND x.OldDatabaseName = N'Adnan'
              AND x.OldTableName = N'ACCOUNTS'
              AND x.OldId = ra.Account_Code
              AND x.NewTableName = N'ChartOfAccount'
              AND x.EntityType = N'Account'
        WHERE x.Id IS NULL;

        INSERT INTO dbo.PropertyPilotAccountMapping
        (MigrationBatchId, OldDatabaseName, OldAccountCode, OldAccountId, OldAccountName, NewChartOfAccountId, NewAccountCode, MappingMode, IsApproved, Notes)
        SELECT @MigrationBatchId, N'Adnan', ra.Account_Code, ra.Account_ID, ra.Account_Name, ca.Id, ca.Code, N'Seed', 1, N'Sandbox seeded account'
        FROM RequiredAccounts ra
        INNER JOIN dbo.ChartOfAccount ca ON ca.Code = ra.Account_Code
        LEFT JOIN dbo.PropertyPilotAccountMapping m
               ON m.MigrationBatchId = @MigrationBatchId
              AND m.OldDatabaseName = N'Adnan'
              AND m.OldAccountCode = ra.Account_Code
        WHERE m.Id IS NULL;

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        DECLARE @Msg NVARCHAR(4000); SET @Msg = ERROR_MESSAGE();
        RAISERROR(@Msg, 16, 1);
    END CATCH;
END;
GO

IF OBJECT_ID('dbo.usp_PropertyPilot_MigrateTenants_Adnan', 'P') IS NOT NULL DROP PROCEDURE dbo.usp_PropertyPilot_MigrateTenants_Adnan;
GO
CREATE PROCEDURE dbo.usp_PropertyPilot_MigrateTenants_Adnan
    @MigrationBatchId UNIQUEIDENTIFIER,
    @CutoverDate DATETIME,
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF DB_NAME() = N'Alromaizan' OR (DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%Sandbox%') BEGIN RAISERROR('Blocked: Sandbox/PropertyPilot database required.',16,1); RETURN; END;

    BEGIN TRY
        BEGIN TRAN;

        ;WITH Settled AS (SELECT DISTINCT CAST(ContNo AS int) ContNo FROM Adnan.dbo.TblFiterWaiver WHERE ContNo IS NOT NULL),
        ActiveContracts AS (
            SELECT c.* FROM Adnan.dbo.TblContract c LEFT JOIN Settled s ON s.ContNo=CAST(c.ContNo AS int)
            WHERE ISNULL(c.EndContract,0)=0 AND s.ContNo IS NULL AND (c.EndDate IS NULL OR c.EndDate>=@CutoverDate)
              AND c.Iqar IS NOT NULL AND c.UnitNo IS NOT NULL AND c.CusID IS NOT NULL
        ), Tenants AS (
            SELECT DISTINCT r.* FROM ActiveContracts c INNER JOIN Adnan.dbo.TblCustemers r ON r.CusID=c.CusID
        )
        INSERT INTO dbo.PropertyRenter
        (Code, ArName, EnName, IsActive, IsDeleted, UserId, Notes, RegistrationDate, NationalNo, ContactPerson, VATNo, Phone, Mobile, Address, AccountId)
        SELECT CAST(t.CusID AS NVARCHAR(50)),
               ISNULL(NULLIF(t.CusName,N''), N'Adnan Tenant ' + CAST(t.CusID AS NVARCHAR(30))),
               t.CusNamee,
               1,
               0,
               @UserId,
               N'PropertyPilot Adnan CusID=' + CAST(t.CusID AS NVARCHAR(30)),
               t.RecordDate,
               t.NationalNo,
               t.ResponsibleContact,
               t.VATNO,
               t.Cus_Phone,
               t.Cus_mobile,
               t.Address,
               m.NewChartOfAccountId
        FROM Tenants t
        LEFT JOIN dbo.PropertyPilotAccountMapping m
               ON m.MigrationBatchId=@MigrationBatchId AND m.OldDatabaseName=N'Adnan' AND m.OldAccountCode=t.Account_Code AND m.IsApproved=1
        LEFT JOIN dbo.PropertyPilotCrossReference x
               ON x.MigrationBatchId=@MigrationBatchId AND x.OldDatabaseName=N'Adnan' AND x.OldTableName=N'TblCustemers' AND x.OldId=CAST(t.CusID AS NVARCHAR(100)) AND x.EntityType=N'Tenant'
        WHERE x.Id IS NULL;

        INSERT INTO dbo.PropertyPilotCrossReference
        (MigrationBatchId, OldDatabaseName, OldTableName, OldId, NewTableName, NewId, EntityType, Notes)
        SELECT @MigrationBatchId, N'Adnan', N'TblCustemers', CAST(t.CusID AS NVARCHAR(100)), N'PropertyRenter', pr.Id, N'Tenant', N'Migrated tenant'
        FROM Tenants t
        INNER JOIN dbo.PropertyRenter pr ON pr.Code = CAST(t.CusID AS NVARCHAR(50)) AND pr.Notes LIKE N'%PropertyPilot Adnan CusID=%'
        LEFT JOIN dbo.PropertyPilotCrossReference x
               ON x.MigrationBatchId=@MigrationBatchId AND x.OldDatabaseName=N'Adnan' AND x.OldTableName=N'TblCustemers' AND x.OldId=CAST(t.CusID AS NVARCHAR(100)) AND x.EntityType=N'Tenant'
        WHERE x.Id IS NULL;

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        DECLARE @Msg NVARCHAR(4000); SET @Msg = ERROR_MESSAGE(); RAISERROR(@Msg,16,1);
    END CATCH;
END;
GO

IF OBJECT_ID('dbo.usp_PropertyPilot_MigrateProperties_Adnan', 'P') IS NOT NULL DROP PROCEDURE dbo.usp_PropertyPilot_MigrateProperties_Adnan;
GO
CREATE PROCEDURE dbo.usp_PropertyPilot_MigrateProperties_Adnan
    @MigrationBatchId UNIQUEIDENTIFIER,
    @CutoverDate DATETIME,
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF DB_NAME() = N'Alromaizan' OR (DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%Sandbox%') BEGIN RAISERROR('Blocked: Sandbox/PropertyPilot database required.',16,1); RETURN; END;
    BEGIN TRY
        BEGIN TRAN;

        ;WITH Settled AS (SELECT DISTINCT CAST(ContNo AS int) ContNo FROM Adnan.dbo.TblFiterWaiver WHERE ContNo IS NOT NULL),
        ActiveContracts AS (
            SELECT c.* FROM Adnan.dbo.TblContract c LEFT JOIN Settled s ON s.ContNo=CAST(c.ContNo AS int)
            WHERE ISNULL(c.EndContract,0)=0 AND s.ContNo IS NULL AND (c.EndDate IS NULL OR c.EndDate>=@CutoverDate)
              AND c.Iqar IS NOT NULL AND c.UnitNo IS NOT NULL AND c.CusID IS NOT NULL
        ), Props AS (
            SELECT DISTINCT p.* FROM ActiveContracts c INNER JOIN Adnan.dbo.TblAqar p ON p.Aqarid=c.Iqar
        )
        INSERT INTO dbo.Property
        (Code, ArName, EnName, IsActive, IsDeleted, UserId, Notes, PropertySequence, PropertyTypeId, CountryId, CityId, NeighborhoodName, StreetName, FloorsNo, EntriesNo, HousingUnitsNo, CommercialUnitsNo, ParkingsNo, PropertyOwnerId)
        SELECT ISNULL(NULLIF(p.aqarNo,N''), N'ADNAN-P-' + CAST(p.Aqarid AS NVARCHAR(30))),
               ISNULL(NULLIF(p.aqarname,N''), N'Adnan Property ' + CAST(p.Aqarid AS NVARCHAR(30))),
               NULL,
               1,
               0,
               @UserId,
               N'PropertyPilot Adnan Aqarid=' + CAST(p.Aqarid AS NVARCHAR(30)),
               p.aqarNo,
               p.aqartypeid,
               p.CountryID,
               p.cityid,
               CAST(p.heyid AS NVARCHAR(100)),
               p.streetname,
               p.floorcount,
               p.EntryCount,
               p.noofapartement,
               p.noofoffices,
               p.noofparking,
               NULL
        FROM Props p
        LEFT JOIN dbo.PropertyPilotCrossReference x
               ON x.MigrationBatchId=@MigrationBatchId AND x.OldDatabaseName=N'Adnan' AND x.OldTableName=N'TblAqar' AND x.OldId=CAST(p.Aqarid AS NVARCHAR(100)) AND x.EntityType=N'Property'
        WHERE x.Id IS NULL;

        INSERT INTO dbo.PropertyPilotCrossReference
        (MigrationBatchId, OldDatabaseName, OldTableName, OldId, NewTableName, NewId, EntityType, Notes)
        SELECT @MigrationBatchId, N'Adnan', N'TblAqar', CAST(p.Aqarid AS NVARCHAR(100)), N'Property', np.Id, N'Property', N'Migrated property'
        FROM Props p
        INNER JOIN dbo.Property np ON np.Notes LIKE N'%PropertyPilot Adnan Aqarid=' + CAST(p.Aqarid AS NVARCHAR(30)) + N'%'
        LEFT JOIN dbo.PropertyPilotCrossReference x
               ON x.MigrationBatchId=@MigrationBatchId AND x.OldDatabaseName=N'Adnan' AND x.OldTableName=N'TblAqar' AND x.OldId=CAST(p.Aqarid AS NVARCHAR(100)) AND x.EntityType=N'Property'
        WHERE x.Id IS NULL;

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        DECLARE @Msg NVARCHAR(4000); SET @Msg = ERROR_MESSAGE(); RAISERROR(@Msg,16,1);
    END CATCH;
END;
GO

IF OBJECT_ID('dbo.usp_PropertyPilot_MigrateUnits_Adnan', 'P') IS NOT NULL DROP PROCEDURE dbo.usp_PropertyPilot_MigrateUnits_Adnan;
GO
CREATE PROCEDURE dbo.usp_PropertyPilot_MigrateUnits_Adnan
    @MigrationBatchId UNIQUEIDENTIFIER,
    @CutoverDate DATETIME,
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF DB_NAME() = N'Alromaizan' OR (DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%Sandbox%') BEGIN RAISERROR('Blocked: Sandbox/PropertyPilot database required.',16,1); RETURN; END;
    BEGIN TRY
        BEGIN TRAN;

        ;WITH Settled AS (SELECT DISTINCT CAST(ContNo AS int) ContNo FROM Adnan.dbo.TblFiterWaiver WHERE ContNo IS NOT NULL),
        ActiveContracts AS (
            SELECT c.* FROM Adnan.dbo.TblContract c LEFT JOIN Settled s ON s.ContNo=CAST(c.ContNo AS int)
            WHERE ISNULL(c.EndContract,0)=0 AND s.ContNo IS NULL AND (c.EndDate IS NULL OR c.EndDate>=@CutoverDate)
              AND c.Iqar IS NOT NULL AND c.UnitNo IS NOT NULL AND c.CusID IS NOT NULL
        ), Units AS (
            SELECT DISTINCT u.* FROM ActiveContracts c INNER JOIN Adnan.dbo.TblAqarDetai u ON u.Id=c.UnitNo
        )
        INSERT INTO dbo.PropertyDetail
        (MainDocId, PropertyUnitNo, PropertyUnitTypeId, Floor, RoomsNo, HallsNo, IsFurnishing, StatusId, TypeId, IsApplyTax, RentMethod, Area, MeterPrice, RentalValue, LowestRentalValue, IsDeleted, UserId, Notes, KitchenCount)
        SELECT px.NewId,
               CAST(u.unitno AS NVARCHAR(100)),
               u.unittype,
               CAST(u.Floor AS NVARCHAR(100)),
               u.roomscount,
               u.LoungeCount,
               u.haveFurniture,
               2,
               u.rentType,
               NULL,
               CAST(u.rentType AS NVARCHAR(100)),
               u.length,
               u.meterPrice,
               u.RentValue,
               u.MiniRentValue,
               0,
               @UserId,
               N'PropertyPilot Adnan UnitId=' + CAST(u.Id AS NVARCHAR(30)),
               u.kithchencount
        FROM Units u
        INNER JOIN dbo.PropertyPilotCrossReference px ON px.MigrationBatchId=@MigrationBatchId AND px.OldDatabaseName=N'Adnan' AND px.OldTableName=N'TblAqar' AND px.OldId=CAST(u.Aqarid AS NVARCHAR(100)) AND px.EntityType=N'Property'
        LEFT JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.OldDatabaseName=N'Adnan' AND x.OldTableName=N'TblAqarDetai' AND x.OldId=CAST(u.Id AS NVARCHAR(100)) AND x.EntityType=N'Unit'
        WHERE x.Id IS NULL;

        INSERT INTO dbo.PropertyPilotCrossReference
        (MigrationBatchId, OldDatabaseName, OldTableName, OldId, NewTableName, NewId, EntityType, Notes)
        SELECT @MigrationBatchId, N'Adnan', N'TblAqarDetai', CAST(u.Id AS NVARCHAR(100)), N'PropertyDetail', pd.Id, N'Unit', N'Migrated unit'
        FROM Units u
        INNER JOIN dbo.PropertyDetail pd ON pd.Notes LIKE N'%PropertyPilot Adnan UnitId=' + CAST(u.Id AS NVARCHAR(30)) + N'%'
        LEFT JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.OldDatabaseName=N'Adnan' AND x.OldTableName=N'TblAqarDetai' AND x.OldId=CAST(u.Id AS NVARCHAR(100)) AND x.EntityType=N'Unit'
        WHERE x.Id IS NULL;

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        DECLARE @Msg NVARCHAR(4000); SET @Msg = ERROR_MESSAGE(); RAISERROR(@Msg,16,1);
    END CATCH;
END;
GO

IF OBJECT_ID('dbo.usp_PropertyPilot_MigrateActiveContracts_Adnan', 'P') IS NOT NULL DROP PROCEDURE dbo.usp_PropertyPilot_MigrateActiveContracts_Adnan;
GO
CREATE PROCEDURE dbo.usp_PropertyPilot_MigrateActiveContracts_Adnan
    @MigrationBatchId UNIQUEIDENTIFIER,
    @CutoverDate DATETIME,
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF DB_NAME() = N'Alromaizan' OR (DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%Sandbox%') BEGIN RAISERROR('Blocked: Sandbox/PropertyPilot database required.',16,1); RETURN; END;
    BEGIN TRY
        BEGIN TRAN;

        ;WITH Settled AS (SELECT DISTINCT CAST(ContNo AS int) ContNo FROM Adnan.dbo.TblFiterWaiver WHERE ContNo IS NOT NULL),
        ActiveContracts AS (
            SELECT c.* FROM Adnan.dbo.TblContract c LEFT JOIN Settled s ON s.ContNo=CAST(c.ContNo AS int)
            WHERE ISNULL(c.EndContract,0)=0 AND s.ContNo IS NULL AND (c.EndDate IS NULL OR c.EndDate>=@CutoverDate)
              AND c.Iqar IS NOT NULL AND c.UnitNo IS NOT NULL AND c.CusID IS NOT NULL
        )
        INSERT INTO dbo.PropertyContract
        (DocumentNumber, VoucherDate, ContractTypeId, PropertyId, RentTypeId, PropertyOwnerId, PropertyRenterId, RentValue, CommissionValue, ServicesValue, WaterValue, NetTotal, VATPercentage, VATValue, TotalAfterTaxes, ContractStartDate, ContractEndDate, ContractPeriodNum, ContractPeriodTypeId, IsDeleted, UserId, Notes, PropertyUnitTypeId, PropertyUnitId, IncludeRentValueInVAT, IncludeWaterValueInVAT, ElectricityValue, IncludeElectricityValueInVAT, NumberOfBatches, FirstBatchDate, PeriodBetweenBatchesNum, PeriodBetweenBatchesTypeId, UnifiedContractNumber, InsuranceValue, IncludeInsuranceValueInVAT, IncludeCommissionValueInVAT, IncludeServicesValueInVAT, IsRenewed)
        SELECT ISNULL(CAST(c.NoteSerial1 AS NVARCHAR(100)), N'ADNAN-C-' + CAST(CAST(c.ContNo AS int) AS NVARCHAR(30))),
               ISNULL(c.ContDate, ISNULL(c.StrDate, @CutoverDate)),
               c.ContType,
               px.NewId,
               c.RentType,
               NULL,
               tx.NewId,
               c.TotalContract,
               c.CommiValue,
               c.Servce,
               c.Water,
               c.NetValue,
               c.FATYou,
               c.FATValue,
               c.TotalValue,
               c.StrDate,
               c.EndDate,
               c.Contract_period_no,
               c.Contract_period,
               0,
               @UserId,
               N'PropertyPilot Adnan ContNo=' + CAST(CAST(c.ContNo AS int) AS NVARCHAR(30)),
               c.UnitType,
               ux.NewId,
               c.CommiValueInVAT,
               c.WaterElecValueInVAT,
               c.Electricity,
               c.WaterElecValueInVAT,
               c.PaymentCount,
               c.FristPaymentDate,
               c.PeriodsID,
               c.Periods,
               c.NoteSerial1,
               c.InsuranceValue,
               c.InsurValueInVAT,
               c.CommiValueInVAT,
               NULL,
               ISNULL(c.Renew,0)
        FROM ActiveContracts c
        INNER JOIN dbo.PropertyPilotCrossReference px ON px.MigrationBatchId=@MigrationBatchId AND px.OldDatabaseName=N'Adnan' AND px.OldTableName=N'TblAqar' AND px.OldId=CAST(c.Iqar AS NVARCHAR(100)) AND px.EntityType=N'Property'
        INNER JOIN dbo.PropertyPilotCrossReference ux ON ux.MigrationBatchId=@MigrationBatchId AND ux.OldDatabaseName=N'Adnan' AND ux.OldTableName=N'TblAqarDetai' AND ux.OldId=CAST(c.UnitNo AS NVARCHAR(100)) AND ux.EntityType=N'Unit'
        INNER JOIN dbo.PropertyPilotCrossReference tx ON tx.MigrationBatchId=@MigrationBatchId AND tx.OldDatabaseName=N'Adnan' AND tx.OldTableName=N'TblCustemers' AND tx.OldId=CAST(c.CusID AS NVARCHAR(100)) AND tx.EntityType=N'Tenant'
        LEFT JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.OldDatabaseName=N'Adnan' AND x.OldTableName=N'TblContract' AND x.OldId=CAST(CAST(c.ContNo AS int) AS NVARCHAR(100)) AND x.EntityType=N'Contract'
        WHERE x.Id IS NULL;

        INSERT INTO dbo.PropertyPilotCrossReference
        (MigrationBatchId, OldDatabaseName, OldTableName, OldId, NewTableName, NewId, EntityType, Notes)
        SELECT @MigrationBatchId, N'Adnan', N'TblContract', CAST(CAST(c.ContNo AS int) AS NVARCHAR(100)), N'PropertyContract', pc.Id, N'Contract', N'Migrated strict-active contract'
        FROM ActiveContracts c
        INNER JOIN dbo.PropertyContract pc ON pc.Notes LIKE N'%PropertyPilot Adnan ContNo=' + CAST(CAST(c.ContNo AS int) AS NVARCHAR(30)) + N'%'
        LEFT JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.OldDatabaseName=N'Adnan' AND x.OldTableName=N'TblContract' AND x.OldId=CAST(CAST(c.ContNo AS int) AS NVARCHAR(100)) AND x.EntityType=N'Contract'
        WHERE x.Id IS NULL;

        INSERT INTO dbo.PropertyContractBatch
        (MainDocId, BatchNo, BatchDate, BatchRentValue, BatchWaterValue, BatchElectricityValue, BatchCommissionValue, BatchTotal, IsDeleted, UserId, Notes, BatchServicesValue, BatchInsuranceValue)
        SELECT cx.NewId,
               i.InstallNo,
               i.Installdate,
               i.RentValue,
               i.Water,
               i.Electric,
               i.Commissions,
               i.installValue,
               0,
               @UserId,
               N'PropertyPilot Adnan InstallmentId=' + CAST(i.id AS NVARCHAR(30)),
               i.TelandNet,
               i.Insurance
        FROM Adnan.dbo.TblContractInstallments i
        INNER JOIN dbo.PropertyPilotCrossReference cx ON cx.MigrationBatchId=@MigrationBatchId AND cx.OldDatabaseName=N'Adnan' AND cx.OldTableName=N'TblContract' AND cx.OldId=CAST(CAST(i.ContNo AS int) AS NVARCHAR(100)) AND cx.EntityType=N'Contract'
        LEFT JOIN dbo.PropertyPilotCrossReference bx ON bx.MigrationBatchId=@MigrationBatchId AND bx.OldDatabaseName=N'Adnan' AND bx.OldTableName=N'TblContractInstallments' AND bx.OldId=CAST(i.id AS NVARCHAR(100)) AND bx.EntityType=N'ContractBatch'
        WHERE bx.Id IS NULL;

        INSERT INTO dbo.PropertyPilotCrossReference
        (MigrationBatchId, OldDatabaseName, OldTableName, OldId, NewTableName, NewId, EntityType, Notes)
        SELECT @MigrationBatchId, N'Adnan', N'TblContractInstallments', CAST(i.id AS NVARCHAR(100)), N'PropertyContractBatch', pcb.Id, N'ContractBatch', N'Migrated contract batch'
        FROM Adnan.dbo.TblContractInstallments i
        INNER JOIN dbo.PropertyContractBatch pcb ON pcb.Notes LIKE N'%PropertyPilot Adnan InstallmentId=' + CAST(i.id AS NVARCHAR(30)) + N'%'
        INNER JOIN dbo.PropertyPilotCrossReference cx ON cx.MigrationBatchId=@MigrationBatchId AND cx.OldDatabaseName=N'Adnan' AND cx.OldTableName=N'TblContract' AND cx.OldId=CAST(CAST(i.ContNo AS int) AS NVARCHAR(100)) AND cx.EntityType=N'Contract'
        LEFT JOIN dbo.PropertyPilotCrossReference bx ON bx.MigrationBatchId=@MigrationBatchId AND bx.OldDatabaseName=N'Adnan' AND bx.OldTableName=N'TblContractInstallments' AND bx.OldId=CAST(i.id AS NVARCHAR(100)) AND bx.EntityType=N'ContractBatch'
        WHERE bx.Id IS NULL;

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        DECLARE @Msg NVARCHAR(4000); SET @Msg = ERROR_MESSAGE(); RAISERROR(@Msg,16,1);
    END CATCH;
END;
GO

IF OBJECT_ID('dbo.usp_PropertyPilot_CreateOpeningBalances_Adnan', 'P') IS NOT NULL DROP PROCEDURE dbo.usp_PropertyPilot_CreateOpeningBalances_Adnan;
GO
CREATE PROCEDURE dbo.usp_PropertyPilot_CreateOpeningBalances_Adnan
    @MigrationBatchId UNIQUEIDENTIFIER,
    @CutoverDate DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    IF DB_NAME() = N'Alromaizan' OR (DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%Sandbox%') BEGIN RAISERROR('Blocked: Sandbox/PropertyPilot database required.',16,1); RETURN; END;
    BEGIN TRY
        BEGIN TRAN;

        ;WITH Paid AS (
            SELECT d.istallid,
                   SUM(ISNULL(d.RentValuePayed,0)+ISNULL(d.CommissionsPayed,0)+ISNULL(d.InsurancePayed,0)+ISNULL(d.WaterPayed,0)+ISNULL(d.ElectricPayed,0)+ISNULL(d.TelandNetPayed,0)+ISNULL(d.OldValuePayed,0)+ISNULL(d.VATPayed,0)) TruePaid
            FROM Adnan.dbo.ContracttBillInstallmentsDone d
            LEFT JOIN Adnan.dbo.Notes n ON n.NoteID=d.NoteID
            WHERE n.NoteType=4 OR n.NoteType IS NULL
            GROUP BY d.istallid
        ), ContractOB AS (
            SELECT i.ContNo,
                   SUM(CASE WHEN i.Installdate <= @CutoverDate THEN ISNULL(i.installValue,0) ELSE 0 END) AS DueInstallmentTotal,
                   SUM(CASE WHEN i.Installdate <= @CutoverDate THEN ISNULL(p.TruePaid,0) ELSE 0 END) AS TruePaid,
                   SUM(CASE WHEN i.Installdate <= @CutoverDate THEN ISNULL(i.installValue,0)-ISNULL(p.TruePaid,0) ELSE 0 END) AS OpeningBalanceAmount
            FROM Adnan.dbo.TblContractInstallments i
            LEFT JOIN Paid p ON p.istallid=i.id
            INNER JOIN dbo.PropertyPilotCrossReference cx ON cx.MigrationBatchId=@MigrationBatchId AND cx.OldDatabaseName=N'Adnan' AND cx.OldTableName=N'TblContract' AND cx.OldId=CAST(CAST(i.ContNo AS int) AS NVARCHAR(100)) AND cx.EntityType=N'Contract'
            GROUP BY i.ContNo
        )
        INSERT INTO dbo.PropertyPilotOpeningBalanceStaging
        (MigrationBatchId, OldDatabaseName, OldContractNo, OldRenterId, OldAccountCode, DueInstallmentTotal, TruePaid, OpeningBalanceAmount, CutoverDate, NewPropertyContractId, NewPropertyRenterId, NewAccountId, Notes)
        SELECT @MigrationBatchId,
               N'Adnan',
               CAST(c.ContNo AS int),
               c.CusID,
               r.Account_Code,
               ob.DueInstallmentTotal,
               ob.TruePaid,
               ob.OpeningBalanceAmount,
               @CutoverDate,
               cx.NewId,
               tx.NewId,
               m.NewChartOfAccountId,
               N'Staging only. Accounting treatment requires approval before posting.'
        FROM ContractOB ob
        INNER JOIN Adnan.dbo.TblContract c ON CAST(c.ContNo AS int)=CAST(ob.ContNo AS int)
        LEFT JOIN Adnan.dbo.TblCustemers r ON r.CusID=c.CusID
        INNER JOIN dbo.PropertyPilotCrossReference cx ON cx.MigrationBatchId=@MigrationBatchId AND cx.OldDatabaseName=N'Adnan' AND cx.OldTableName=N'TblContract' AND cx.OldId=CAST(CAST(c.ContNo AS int) AS NVARCHAR(100)) AND cx.EntityType=N'Contract'
        LEFT JOIN dbo.PropertyPilotCrossReference tx ON tx.MigrationBatchId=@MigrationBatchId AND tx.OldDatabaseName=N'Adnan' AND tx.OldTableName=N'TblCustemers' AND tx.OldId=CAST(c.CusID AS NVARCHAR(100)) AND tx.EntityType=N'Tenant'
        LEFT JOIN dbo.PropertyPilotAccountMapping m ON m.MigrationBatchId=@MigrationBatchId AND m.OldDatabaseName=N'Adnan' AND m.OldAccountCode=r.Account_Code AND m.IsApproved=1
        LEFT JOIN dbo.PropertyPilotOpeningBalanceStaging existing ON existing.MigrationBatchId=@MigrationBatchId AND existing.OldDatabaseName=N'Adnan' AND existing.OldContractNo=CAST(c.ContNo AS int)
        WHERE existing.Id IS NULL AND ob.OpeningBalanceAmount <> 0;

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        DECLARE @Msg NVARCHAR(4000); SET @Msg = ERROR_MESSAGE(); RAISERROR(@Msg,16,1);
    END CATCH;
END;
GO

IF OBJECT_ID('dbo.usp_PropertyPilot_Reconcile_Adnan', 'P') IS NOT NULL DROP PROCEDURE dbo.usp_PropertyPilot_Reconcile_Adnan;
GO
CREATE PROCEDURE dbo.usp_PropertyPilot_Reconcile_Adnan
    @MigrationBatchId UNIQUEIDENTIFIER,
    @CutoverDate DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    IF DB_NAME() = N'Alromaizan' OR (DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%Sandbox%') BEGIN RAISERROR('Blocked: Sandbox/PropertyPilot database required.',16,1); RETURN; END;

    SELECT EntityType, OldTableName, NewTableName, COUNT(*) AS CrossReferenceRows
    FROM dbo.PropertyPilotCrossReference
    WHERE MigrationBatchId=@MigrationBatchId
    GROUP BY EntityType, OldTableName, NewTableName
    ORDER BY EntityType;

    SELECT SUM(OpeningBalanceAmount) AS SandboxOpeningBalanceStaging
    FROM dbo.PropertyPilotOpeningBalanceStaging
    WHERE MigrationBatchId=@MigrationBatchId;

    SELECT Severity, IssueType, COUNT(*) AS IssueCount
    FROM dbo.PropertyPilotValidationIssue
    WHERE MigrationBatchId=@MigrationBatchId AND IsResolved=0
    GROUP BY Severity, IssueType
    ORDER BY Severity, IssueType;
END;
GO

IF OBJECT_ID('dbo.usp_PropertyPilot_RollbackBatch_Adnan', 'P') IS NOT NULL DROP PROCEDURE dbo.usp_PropertyPilot_RollbackBatch_Adnan;
GO
CREATE PROCEDURE dbo.usp_PropertyPilot_RollbackBatch_Adnan
    @MigrationBatchId UNIQUEIDENTIFIER,
    @ConfirmSandboxRollback NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    IF DB_NAME() = N'Alromaizan' OR (DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%Sandbox%') BEGIN RAISERROR('Blocked: Sandbox/PropertyPilot database required.',16,1); RETURN; END;
    IF @ConfirmSandboxRollback <> N'ROLLBACK SANDBOX BATCH' BEGIN RAISERROR('Confirmation phrase required.',16,1); RETURN; END;

    BEGIN TRY
        BEGIN TRAN;

        DELETE pcb
        FROM dbo.PropertyContractBatch pcb
        INNER JOIN dbo.PropertyPilotCrossReference x ON x.NewTableName=N'PropertyContractBatch' AND x.NewId=pcb.Id
        WHERE x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'ContractBatch';

        DELETE pc
        FROM dbo.PropertyContract pc
        INNER JOIN dbo.PropertyPilotCrossReference x ON x.NewTableName=N'PropertyContract' AND x.NewId=pc.Id
        WHERE x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'Contract';

        DELETE pd
        FROM dbo.PropertyDetail pd
        INNER JOIN dbo.PropertyPilotCrossReference x ON x.NewTableName=N'PropertyDetail' AND x.NewId=pd.Id
        WHERE x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'Unit';

        DELETE pr
        FROM dbo.PropertyRenter pr
        INNER JOIN dbo.PropertyPilotCrossReference x ON x.NewTableName=N'PropertyRenter' AND x.NewId=pr.Id
        WHERE x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'Tenant';

        DELETE p
        FROM dbo.Property p
        INNER JOIN dbo.PropertyPilotCrossReference x ON x.NewTableName=N'Property' AND x.NewId=p.Id
        WHERE x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'Property';

        DELETE ca
        FROM dbo.ChartOfAccount ca
        INNER JOIN dbo.PropertyPilotCrossReference x ON x.NewTableName=N'ChartOfAccount' AND x.NewId=ca.Id
        WHERE x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'Account';

        DELETE FROM dbo.PropertyPilotOpeningBalanceStaging WHERE MigrationBatchId=@MigrationBatchId;
        DELETE FROM dbo.PropertyPilotAccountMapping WHERE MigrationBatchId=@MigrationBatchId;
        DELETE FROM dbo.PropertyPilotValidationIssue WHERE MigrationBatchId=@MigrationBatchId;
        DELETE FROM dbo.PropertyPilotCrossReference WHERE MigrationBatchId=@MigrationBatchId;

        UPDATE dbo.PropertyPilotMigrationBatch
        SET Status=N'RolledBack', Notes=ISNULL(Notes,N'') + N' Rolled back at ' + CONVERT(NVARCHAR(30), GETDATE(), 120)
        WHERE MigrationBatchId=@MigrationBatchId;

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        DECLARE @Msg NVARCHAR(4000); SET @Msg = ERROR_MESSAGE(); RAISERROR(@Msg,16,1);
    END CATCH;
END;
GO
