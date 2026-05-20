/* Phase3_EntityMigration_FIXED_SANDBOX_ONLY_20260520.sql */
DECLARE @MigrationBatchId UNIQUEIDENTIFIER;
DECLARE @CutoverDate DATETIME;
SET @MigrationBatchId = 'B7B0DA8D-1E0E-4A1D-A4AB-AD2026052001';
SET @CutoverDate = '20260520';

IF DB_NAME() = N'Alromaizan' OR (DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%Sandbox%')
BEGIN RAISERROR('Blocked: Sandbox/PropertyPilot database required.',16,1); RETURN; END;

IF OBJECT_ID('tempdb..#ActiveContracts') IS NOT NULL DROP TABLE #ActiveContracts;
IF OBJECT_ID('tempdb..#Tenants') IS NOT NULL DROP TABLE #Tenants;
IF OBJECT_ID('tempdb..#Props') IS NOT NULL DROP TABLE #Props;
IF OBJECT_ID('tempdb..#Units') IS NOT NULL DROP TABLE #Units;

;WITH Settled AS (
    SELECT DISTINCT CAST(ContNo AS int) AS ContNo FROM Adnan.dbo.TblFiterWaiver WHERE ContNo IS NOT NULL
)
SELECT c.*
INTO #ActiveContracts
FROM Adnan.dbo.TblContract c
LEFT JOIN Settled s ON s.ContNo=CAST(c.ContNo AS int)
WHERE ISNULL(c.EndContract,0)=0
  AND s.ContNo IS NULL
  AND (c.EndDate IS NULL OR c.EndDate >= @CutoverDate)
  AND c.Iqar IS NOT NULL AND c.UnitNo IS NOT NULL AND c.CusID IS NOT NULL;

SELECT DISTINCT r.CusID, r.CusName, r.CusNamee, r.RecordDate, r.NationalNo, r.ResponsibleContact, r.VATNO, r.Cus_Phone, r.Cus_mobile, r.Address, r.Account_Code
INTO #Tenants
FROM #ActiveContracts c
INNER JOIN Adnan.dbo.TblCustemers r ON r.CusID=c.CusID;

SELECT DISTINCT p.Aqarid, p.aqarNo, p.aqarname, p.aqartypeid, p.CountryID, p.cityid, p.heyid, p.streetname, p.floorcount, p.EntryCount, p.noofapartement, p.noofoffices, p.noofparking
INTO #Props
FROM #ActiveContracts c
INNER JOIN Adnan.dbo.TblAqar p ON p.Aqarid=c.Iqar;

SELECT DISTINCT u.Id, u.Aqarid, u.unitno, u.unittype, u.Floor, u.roomscount, u.LoungeCount, u.haveFurniture, u.rentType, u.length, u.meterPrice, u.RentValue, u.MiniRentValue, u.kithchencount
INTO #Units
FROM #ActiveContracts c
INNER JOIN Adnan.dbo.TblAqarDetai u ON u.Id=c.UnitNo;

BEGIN TRY
    BEGIN TRAN;

    INSERT INTO dbo.PropertyRenter
    (Code, ArName, EnName, IsActive, IsDeleted, UserId, Notes, RegistrationDate, NationalNo, ContactPerson, VATNo, Phone, Mobile, Address, AccountId)
    SELECT N'ADNAN-T-' + CAST(t.CusID AS NVARCHAR(50)),
           ISNULL(NULLIF(t.CusName,N''), N'Adnan Tenant ' + CAST(t.CusID AS NVARCHAR(30))),
           t.CusNamee,
           1,0,NULL,
           N'PropertyPilot Adnan CusID=' + CAST(t.CusID AS NVARCHAR(30)),
           t.RecordDate,t.NationalNo,t.ResponsibleContact,t.VATNO,t.Cus_Phone,t.Cus_mobile,t.Address,
           m.NewChartOfAccountId
    FROM #Tenants t
    LEFT JOIN dbo.PropertyPilotAccountMapping m ON m.MigrationBatchId=@MigrationBatchId AND m.OldDatabaseName=N'Adnan' AND m.OldAccountCode COLLATE DATABASE_DEFAULT=t.Account_Code COLLATE DATABASE_DEFAULT AND m.IsApproved=1
    LEFT JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.OldDatabaseName=N'Adnan' AND x.OldTableName=N'TblCustemers' AND x.OldId=CAST(t.CusID AS NVARCHAR(100)) AND x.EntityType=N'Tenant'
    WHERE x.Id IS NULL;

    INSERT INTO dbo.PropertyPilotCrossReference(MigrationBatchId,OldDatabaseName,OldTableName,OldId,NewTableName,NewId,EntityType,Notes)
    SELECT @MigrationBatchId,N'Adnan',N'TblCustemers',CAST(t.CusID AS NVARCHAR(100)),N'PropertyRenter',pr.Id,N'Tenant',N'Phase3 fixed tenant migration'
    FROM #Tenants t
    INNER JOIN dbo.PropertyRenter pr ON pr.Notes = N'PropertyPilot Adnan CusID=' + CAST(t.CusID AS NVARCHAR(30))
    LEFT JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.OldDatabaseName=N'Adnan' AND x.OldTableName=N'TblCustemers' AND x.OldId=CAST(t.CusID AS NVARCHAR(100)) AND x.EntityType=N'Tenant'
    WHERE x.Id IS NULL;

    INSERT INTO dbo.Property
    (Code, ArName, EnName, IsActive, IsDeleted, UserId, Notes, PropertySequence, PropertyTypeId, CountryId, CityId, NeighborhoodName, StreetName, FloorsNo, EntriesNo, HousingUnitsNo, CommercialUnitsNo, ParkingsNo, PropertyOwnerId)
    SELECT N'ADNAN-P-' + CAST(p.Aqarid AS NVARCHAR(30)),
           ISNULL(NULLIF(p.aqarname,N''), N'Adnan Property ' + CAST(p.Aqarid AS NVARCHAR(30))),
           NULL,1,0,NULL,
           N'PropertyPilot Adnan Aqarid=' + CAST(p.Aqarid AS NVARCHAR(30)),
           p.aqarNo,NULL,p.CountryID,p.cityid,CAST(p.heyid AS NVARCHAR(100)),p.streetname,p.floorcount,p.EntryCount,p.noofapartement,p.noofoffices,p.noofparking,NULL
    FROM #Props p
    LEFT JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.OldDatabaseName=N'Adnan' AND x.OldTableName=N'TblAqar' AND x.OldId=CAST(p.Aqarid AS NVARCHAR(100)) AND x.EntityType=N'Property'
    WHERE x.Id IS NULL;

    INSERT INTO dbo.PropertyPilotCrossReference(MigrationBatchId,OldDatabaseName,OldTableName,OldId,NewTableName,NewId,EntityType,Notes)
    SELECT @MigrationBatchId,N'Adnan',N'TblAqar',CAST(p.Aqarid AS NVARCHAR(100)),N'Property',np.Id,N'Property',N'Phase3 fixed property migration'
    FROM #Props p
    INNER JOIN dbo.Property np ON np.Notes = N'PropertyPilot Adnan Aqarid=' + CAST(p.Aqarid AS NVARCHAR(30))
    LEFT JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.OldDatabaseName=N'Adnan' AND x.OldTableName=N'TblAqar' AND x.OldId=CAST(p.Aqarid AS NVARCHAR(100)) AND x.EntityType=N'Property'
    WHERE x.Id IS NULL;

    INSERT INTO dbo.PropertyDetail
    (MainDocId, PropertyUnitNo, PropertyUnitTypeId, Floor, RoomsNo, HallsNo, IsFurnishing, StatusId, TypeId, IsApplyTax, RentMethod, Area, MeterPrice, RentalValue, LowestRentalValue, IsDeleted, UserId, Notes, KitchenCount)
    SELECT px.NewId, CAST(u.unitno AS NVARCHAR(100)), NULL, u.Floor, u.roomscount, u.LoungeCount,
           CASE WHEN ISNULL(u.haveFurniture,0)=0 THEN 0 ELSE 1 END,
           2, u.rentType, NULL, CAST(u.rentType AS NVARCHAR(100)), u.length, u.meterPrice, u.RentValue, u.MiniRentValue, 0, NULL,
           N'PropertyPilot Adnan UnitId=' + CAST(u.Id AS NVARCHAR(30)), u.kithchencount
    FROM #Units u
    INNER JOIN dbo.PropertyPilotCrossReference px ON px.MigrationBatchId=@MigrationBatchId AND px.OldDatabaseName=N'Adnan' AND px.OldTableName=N'TblAqar' AND px.OldId=CAST(u.Aqarid AS NVARCHAR(100)) AND px.EntityType=N'Property'
    LEFT JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.OldDatabaseName=N'Adnan' AND x.OldTableName=N'TblAqarDetai' AND x.OldId=CAST(u.Id AS NVARCHAR(100)) AND x.EntityType=N'Unit'
    WHERE x.Id IS NULL;

    INSERT INTO dbo.PropertyPilotCrossReference(MigrationBatchId,OldDatabaseName,OldTableName,OldId,NewTableName,NewId,EntityType,Notes)
    SELECT @MigrationBatchId,N'Adnan',N'TblAqarDetai',CAST(u.Id AS NVARCHAR(100)),N'PropertyDetail',pd.Id,N'Unit',N'Phase3 fixed unit migration'
    FROM #Units u
    INNER JOIN dbo.PropertyDetail pd ON pd.Notes = N'PropertyPilot Adnan UnitId=' + CAST(u.Id AS NVARCHAR(30))
    LEFT JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.OldDatabaseName=N'Adnan' AND x.OldTableName=N'TblAqarDetai' AND x.OldId=CAST(u.Id AS NVARCHAR(100)) AND x.EntityType=N'Unit'
    WHERE x.Id IS NULL;

    INSERT INTO dbo.PropertyContract
    (DocumentNumber, VoucherDate, ContractTypeId, PropertyId, RentTypeId, PropertyOwnerId, PropertyRenterId, RentValue, CommissionValue, ServicesValue, WaterValue, NetTotal, VATPercentage, VATValue, TotalAfterTaxes, ContractStartDate, ContractEndDate, ContractPeriodNum, ContractPeriodTypeId, IsDeleted, UserId, Notes, PropertyUnitTypeId, PropertyUnitId, IncludeRentValueInVAT, IncludeWaterValueInVAT, ElectricityValue, IncludeElectricityValueInVAT, NumberOfBatches, FirstBatchDate, PeriodBetweenBatchesNum, PeriodBetweenBatchesTypeId, UnifiedContractNumber, InsuranceValue, IncludeInsuranceValueInVAT, IncludeCommissionValueInVAT, IncludeServicesValueInVAT, IsRenewed)
    SELECT N'ADNAN-C-' + CAST(CAST(c.ContNo AS int) AS NVARCHAR(30)),
           ISNULL(c.ContDate, ISNULL(c.StrDate, @CutoverDate)), c.ContType, px.NewId, c.RentType, NULL, tx.NewId,
           c.TotalContract, c.CommiValue, c.Servce, c.Water, c.NetValue, c.FATYou, c.FATValue, c.TotalValue,
           c.StrDate, c.EndDate, c.Contract_period_no, c.Contract_period, 0, NULL,
           N'PropertyPilot Adnan ContNo=' + CAST(CAST(c.ContNo AS int) AS NVARCHAR(30)), NULL, ux.NewId,
           CASE WHEN ISNULL(c.CommiValueInVAT,0)=0 THEN 0 ELSE 1 END,
           CASE WHEN ISNULL(c.WaterElecValueInVAT,0)=0 THEN 0 ELSE 1 END,
           c.Electricity,
           CASE WHEN ISNULL(c.WaterElecValueInVAT,0)=0 THEN 0 ELSE 1 END,
           c.PaymentCount, c.FristPaymentDate, c.PeriodsID, c.Periods, CAST(c.NoteSerial1 AS NVARCHAR(100)), c.InsuranceValue,
           CASE WHEN ISNULL(c.InsurValueInVAT,0)=0 THEN 0 ELSE 1 END,
           CASE WHEN ISNULL(c.CommiValueInVAT,0)=0 THEN 0 ELSE 1 END,
           NULL,
           CASE WHEN ISNULL(c.Renew,0)=0 THEN 0 ELSE 1 END
    FROM #ActiveContracts c
    INNER JOIN dbo.PropertyPilotCrossReference px ON px.MigrationBatchId=@MigrationBatchId AND px.OldDatabaseName=N'Adnan' AND px.OldTableName=N'TblAqar' AND px.OldId=CAST(c.Iqar AS NVARCHAR(100)) AND px.EntityType=N'Property'
    INNER JOIN dbo.PropertyPilotCrossReference ux ON ux.MigrationBatchId=@MigrationBatchId AND ux.OldDatabaseName=N'Adnan' AND ux.OldTableName=N'TblAqarDetai' AND ux.OldId=CAST(c.UnitNo AS NVARCHAR(100)) AND ux.EntityType=N'Unit'
    INNER JOIN dbo.PropertyPilotCrossReference tx ON tx.MigrationBatchId=@MigrationBatchId AND tx.OldDatabaseName=N'Adnan' AND tx.OldTableName=N'TblCustemers' AND tx.OldId=CAST(c.CusID AS NVARCHAR(100)) AND tx.EntityType=N'Tenant'
    LEFT JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.OldDatabaseName=N'Adnan' AND x.OldTableName=N'TblContract' AND x.OldId=CAST(CAST(c.ContNo AS int) AS NVARCHAR(100)) AND x.EntityType=N'Contract'
    WHERE x.Id IS NULL;

    INSERT INTO dbo.PropertyPilotCrossReference(MigrationBatchId,OldDatabaseName,OldTableName,OldId,NewTableName,NewId,EntityType,Notes)
    SELECT @MigrationBatchId,N'Adnan',N'TblContract',CAST(CAST(c.ContNo AS int) AS NVARCHAR(100)),N'PropertyContract',pc.Id,N'Contract',N'Phase3 fixed contract migration'
    FROM #ActiveContracts c
    INNER JOIN dbo.PropertyContract pc ON pc.Notes = N'PropertyPilot Adnan ContNo=' + CAST(CAST(c.ContNo AS int) AS NVARCHAR(30))
    LEFT JOIN dbo.PropertyPilotCrossReference x ON x.MigrationBatchId=@MigrationBatchId AND x.OldDatabaseName=N'Adnan' AND x.OldTableName=N'TblContract' AND x.OldId=CAST(CAST(c.ContNo AS int) AS NVARCHAR(100)) AND x.EntityType=N'Contract'
    WHERE x.Id IS NULL;

    INSERT INTO dbo.PropertyContractBatch
    (MainDocId, BatchNo, BatchDate, BatchRentValue, BatchWaterValue, BatchElectricityValue, BatchCommissionValue, BatchTotal, IsDeleted, UserId, Notes, BatchServicesValue, BatchInsuranceValue)
    SELECT cx.NewId, i.InstallNo, i.Installdate, i.RentValue, i.Water, i.Electric, i.Commissions, i.installValue, 0, NULL,
           N'PropertyPilot Adnan InstallmentId=' + CAST(i.id AS NVARCHAR(30)), i.TelandNet, i.Insurance
    FROM Adnan.dbo.TblContractInstallments i
    INNER JOIN dbo.PropertyPilotCrossReference cx ON cx.MigrationBatchId=@MigrationBatchId AND cx.OldDatabaseName=N'Adnan' AND cx.OldTableName=N'TblContract' AND cx.OldId=CAST(CAST(i.ContNo AS int) AS NVARCHAR(100)) AND cx.EntityType=N'Contract'
    LEFT JOIN dbo.PropertyPilotCrossReference bx ON bx.MigrationBatchId=@MigrationBatchId AND bx.OldDatabaseName=N'Adnan' AND bx.OldTableName=N'TblContractInstallments' AND bx.OldId=CAST(i.id AS NVARCHAR(100)) AND bx.EntityType=N'ContractBatch'
    WHERE bx.Id IS NULL;

    INSERT INTO dbo.PropertyPilotCrossReference(MigrationBatchId,OldDatabaseName,OldTableName,OldId,NewTableName,NewId,EntityType,Notes)
    SELECT @MigrationBatchId,N'Adnan',N'TblContractInstallments',CAST(i.id AS NVARCHAR(100)),N'PropertyContractBatch',pcb.Id,N'ContractBatch',N'Phase3 fixed batch migration'
    FROM Adnan.dbo.TblContractInstallments i
    INNER JOIN dbo.PropertyContractBatch pcb ON pcb.Notes = N'PropertyPilot Adnan InstallmentId=' + CAST(i.id AS NVARCHAR(30))
    INNER JOIN dbo.PropertyPilotCrossReference cx ON cx.MigrationBatchId=@MigrationBatchId AND cx.OldDatabaseName=N'Adnan' AND cx.OldTableName=N'TblContract' AND cx.OldId=CAST(CAST(i.ContNo AS int) AS NVARCHAR(100)) AND cx.EntityType=N'Contract'
    LEFT JOIN dbo.PropertyPilotCrossReference bx ON bx.MigrationBatchId=@MigrationBatchId AND bx.OldDatabaseName=N'Adnan' AND bx.OldTableName=N'TblContractInstallments' AND bx.OldId=CAST(i.id AS NVARCHAR(100)) AND bx.EntityType=N'ContractBatch'
    WHERE bx.Id IS NULL;

    ;WITH Paid AS (
        SELECT d.istallid, SUM(ISNULL(d.RentValuePayed,0)+ISNULL(d.CommissionsPayed,0)+ISNULL(d.InsurancePayed,0)+ISNULL(d.WaterPayed,0)+ISNULL(d.ElectricPayed,0)+ISNULL(d.TelandNetPayed,0)+ISNULL(d.OldValuePayed,0)+ISNULL(d.VATPayed,0)) TruePaid
        FROM Adnan.dbo.ContracttBillInstallmentsDone d
        LEFT JOIN Adnan.dbo.Notes n ON n.NoteID=d.NoteID
        WHERE n.NoteType=4 OR n.NoteType IS NULL
        GROUP BY d.istallid
    ), ContractOB AS (
        SELECT i.ContNo,
               SUM(CASE WHEN i.Installdate <= @CutoverDate THEN ISNULL(i.installValue,0) ELSE 0 END) DueInstallmentTotal,
               SUM(CASE WHEN i.Installdate <= @CutoverDate THEN ISNULL(p.TruePaid,0) ELSE 0 END) TruePaid,
               SUM(CASE WHEN i.Installdate <= @CutoverDate THEN ISNULL(i.installValue,0)-ISNULL(p.TruePaid,0) ELSE 0 END) OpeningBalanceAmount
        FROM Adnan.dbo.TblContractInstallments i
        LEFT JOIN Paid p ON p.istallid=i.id
        INNER JOIN dbo.PropertyPilotCrossReference cx ON cx.MigrationBatchId=@MigrationBatchId AND cx.OldDatabaseName=N'Adnan' AND cx.OldTableName=N'TblContract' AND cx.OldId=CAST(CAST(i.ContNo AS int) AS NVARCHAR(100)) AND cx.EntityType=N'Contract'
        GROUP BY i.ContNo
    )
    INSERT INTO dbo.PropertyPilotOpeningBalanceStaging(MigrationBatchId,OldDatabaseName,OldContractNo,OldRenterId,OldAccountCode,DueInstallmentTotal,TruePaid,OpeningBalanceAmount,CutoverDate,NewPropertyContractId,NewPropertyRenterId,NewAccountId,Notes)
    SELECT @MigrationBatchId,N'Adnan',CAST(c.ContNo AS int),c.CusID,r.Account_Code,ob.DueInstallmentTotal,ob.TruePaid,ob.OpeningBalanceAmount,@CutoverDate,cx.NewId,tx.NewId,m.NewChartOfAccountId,N'Staging only. No accounting posted.'
    FROM ContractOB ob
    INNER JOIN #ActiveContracts c ON CAST(c.ContNo AS int)=CAST(ob.ContNo AS int)
    LEFT JOIN Adnan.dbo.TblCustemers r ON r.CusID=c.CusID
    INNER JOIN dbo.PropertyPilotCrossReference cx ON cx.MigrationBatchId=@MigrationBatchId AND cx.OldDatabaseName=N'Adnan' AND cx.OldTableName=N'TblContract' AND cx.OldId=CAST(CAST(c.ContNo AS int) AS NVARCHAR(100)) AND cx.EntityType=N'Contract'
    LEFT JOIN dbo.PropertyPilotCrossReference tx ON tx.MigrationBatchId=@MigrationBatchId AND tx.OldDatabaseName=N'Adnan' AND tx.OldTableName=N'TblCustemers' AND tx.OldId=CAST(c.CusID AS NVARCHAR(100)) AND tx.EntityType=N'Tenant'
    LEFT JOIN dbo.PropertyPilotAccountMapping m ON m.MigrationBatchId=@MigrationBatchId AND m.OldDatabaseName=N'Adnan' AND m.OldAccountCode COLLATE DATABASE_DEFAULT=r.Account_Code COLLATE DATABASE_DEFAULT AND m.IsApproved=1
    LEFT JOIN dbo.PropertyPilotOpeningBalanceStaging existing ON existing.MigrationBatchId=@MigrationBatchId AND existing.OldDatabaseName=N'Adnan' AND existing.OldContractNo=CAST(c.ContNo AS int)
    WHERE existing.Id IS NULL AND ob.OpeningBalanceAmount <> 0;

    UPDATE dbo.PropertyPilotMigrationBatch SET Status=N'Completed' WHERE MigrationBatchId=@MigrationBatchId;

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    DECLARE @Msg NVARCHAR(4000); SET @Msg = ERROR_MESSAGE();
    RAISERROR(@Msg,16,1);
END CATCH;

SELECT EntityType, COUNT(*) AS Cnt FROM dbo.PropertyPilotCrossReference WHERE MigrationBatchId=@MigrationBatchId GROUP BY EntityType ORDER BY EntityType;
SELECT COUNT(*) AS OpeningBalanceRows, SUM(OpeningBalanceAmount) AS OpeningBalance FROM dbo.PropertyPilotOpeningBalanceStaging WHERE MigrationBatchId=@MigrationBatchId;

