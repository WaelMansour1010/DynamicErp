USE [MyErp]
GO
/****** Object:  StoredProcedure [dbo].[PropertyContract_Update]    Script Date: 2026/01/01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[PropertyContract_Update]
	-- Add the parameters for the stored procedure here
@Id int,
@DocumentNumber nvarchar(max),
@VoucherDate datetime,
@ContractTypeId	int	,
@PropertyId	int	,
@RentTypeId	int	,
@PropertyOwnerId	int	,
@PropertyRenterId	int	,
@RepId	int	,
@RentValue	money	,
@CommissionValue	money	,
@ServicesValue	money	,
@WaterValue	money	,
@NetTotal	money	,
@VATPercentage	float	,
@VATValue	money	,
@TotalAfterTaxes	money	,
@ContractStartDate	datetime	,
@ContractEndDate	datetime	,
@ContractPeriodNum	int	,
@ContractPeriodTypeId	int	,
@IsDeleted	bit	,
@UserId	int	,
@Notes	nvarchar(MAX)	,
@Image	nvarchar(MAX)	,
@PropertyUnitTypeId	int	,
@PropertyUnitId	int	,
@IncludeRentValueInVAT	bit	,
@IncludeWaterValueInVAT	bit	,
@ElectricityValue	money	,
@IncludeElectricityValueInVAT	bit	,
@GasValue	money	,
@GracePeriodDay	int	,
@GracePeriodMonth	int	,
@NumberOfBatches	int	,
@FirstBatchDate	datetime	,
@PeriodBetweenBatchesNum	int	,
@PeriodBetweenBatchesTypeId	int	,
@IsDivideWaterIntoBatches	bit	,
@IsDivideElectricityIntoBatches	bit	,
@UnifiedContractNumber	nvarchar(MAX),
@IsAddedValue	bit	,
@ContractSpecialTerms	nvarchar(MAX),
@JournalEntryId	int	,
@DepartmentId int,
@InsuranceValue	money	,
@IncludeInsuranceValueInVAT	bit	,
@IncludeGasValueInVAT	bit	,
@IncludeCommissionValueInVAT	bit	,
@IncludeServicesValueInVAT	bit	,
@GovernmentalOrPrivateContract int,
@Batches NTEXT,
@Reps NTEXT,
@Images NTEXT,
@MergedUnits NTEXT

AS
begin
set nocount on;
    declare @trancount int;
    set @trancount = @@trancount;
    begin try
        if @trancount = 0
            begin transaction
        else
            save transaction PropertyContract_Update;

--//-------- حفظ الوحدة القديمة لإرجاع حالتها إلى متاحة --------//--
DECLARE @OldPropertyUnitId INT
DECLARE @OldMergedUnits TABLE (PropertyUnitId INT)

-- حفظ الوحدة الأساسية القديمة
SELECT @OldPropertyUnitId = PropertyUnitId
FROM PropertyContract
WHERE Id = @Id

-- حفظ الوحدات المدمجة القديمة
INSERT INTO @OldMergedUnits (PropertyUnitId)
SELECT PropertyUnitId
FROM PropertyContractMergedUnit
WHERE PropertyContractId = @Id
--//----------------------------------------------------------------//--

    -- Insert statements for procedure here
	UPDATE [dbo].[PropertyContract]
   SET [DocumentNumber] = @DocumentNumber,
VoucherDate=@VoucherDate,
ContractTypeId=@ContractTypeId,
PropertyId=@PropertyId,
RentTypeId=@RentTypeId,
PropertyOwnerId=@PropertyOwnerId,
PropertyRenterId=@PropertyRenterId,
RepId=@RepId,
RentValue=@RentValue,
CommissionValue=@CommissionValue,
ServicesValue=@ServicesValue,
WaterValue=@WaterValue,
NetTotal=@NetTotal,
VATPercentage=@VATPercentage,
VATValue=@VATValue,
TotalAfterTaxes=@TotalAfterTaxes,
ContractStartDate=@ContractStartDate,
ContractEndDate=@ContractEndDate,
ContractPeriodNum=@ContractPeriodNum,
ContractPeriodTypeId=@ContractPeriodTypeId,
IsDeleted=@IsDeleted,
UserId=@UserId,
Notes=@Notes,
Image=@Image,
PropertyUnitTypeId=@PropertyUnitTypeId,
PropertyUnitId=@PropertyUnitId ,
IncludeRentValueInVAT=@IncludeRentValueInVAT,
IncludeWaterValueInVAT=@IncludeWaterValueInVAT,
ElectricityValue=@ElectricityValue,
IncludeElectricityValueInVAT=@IncludeElectricityValueInVAT,
GasValue=@GasValue,
GracePeriodDay=@GracePeriodDay,
GracePeriodMonth=@GracePeriodMonth,
NumberOfBatches=@NumberOfBatches,
FirstBatchDate=@FirstBatchDate,
PeriodBetweenBatchesNum=@PeriodBetweenBatchesNum ,
PeriodBetweenBatchesTypeId=@PeriodBetweenBatchesTypeId,
IsDivideWaterIntoBatches=@IsDivideWaterIntoBatches,
IsDivideElectricityIntoBatches=@IsDivideElectricityIntoBatches,
UnifiedContractNumber=@UnifiedContractNumber,
IsAddedValue=@IsAddedValue,
ContractSpecialTerms=@ContractSpecialTerms,
JournalEntryId=@JournalEntryId,
DepartmentId=@DepartmentId,
InsuranceValue=@InsuranceValue	,
IncludeInsuranceValueInVAT=@IncludeInsuranceValueInVAT,
IncludeGasValueInVAT=@IncludeGasValueInVAT,
IncludeCommissionValueInVAT=@IncludeCommissionValueInVAT,
IncludeServicesValueInVAT=@IncludeServicesValueInVAT,
GovernmentalOrPrivateContract=@GovernmentalOrPrivateContract

 WHERE [Id]=@Id

--//--------------------------------------------//----------------------------------------------------//--
--//------------------ Batches ----------------------//--
DECLARE @BatchesOut int
Declare @BatchesCount int=(select count(*) from PropertyContractBatch where MainDocId=@Id and IsDelivered = 1 and Id in (select PropertyContractBatchId from CashReceiptVoucherPropertyContractBatch ))
if(@BatchesCount=0) -- مش مربوطة بسند قبض
begin
DELETE FROM [PropertyContractBatch] WHERE [MainDocId]=@Id
EXEC sp_xml_preparedocument @BatchesOut OUT, @Batches
INSERT INTO [dbo].[PropertyContractBatch]
(
MainDocId,
BatchNo,
BatchDate,
BatchRentValue,
BatchRentValueTaxes,
BatchWaterValue,
BatchWaterValueTaxes,
BatchElectricityValue,
BatchElectricityValueTaxes,
BatchCommissionValue,
BatchTotal,
JournalEntryId,
IsDeleted,
UserId,
Notes,
Image,
IsDelivered,
IsRegisteredAsDue,
BatchCommissionValueTaxes,
BatchGasValue,
BatchGasValueTaxes,
BatchServicesValue,
BatchServicesValueTaxes,
BatchInsuranceValue,
BatchInsuranceValueTaxes,
IsRegisteredAsRevenue
)
SELECT @Id,
BatchNo,
BatchDate,
BatchRentValue,
BatchRentValueTaxes,
BatchWaterValue,
BatchWaterValueTaxes,
BatchElectricityValue,
BatchElectricityValueTaxes,
BatchCommissionValue,
BatchTotal,
JournalEntryId,
IsDeleted,
UserId,
Notes,
Image,
IsDelivered,
IsRegisteredAsDue,
BatchCommissionValueTaxes,
BatchGasValue,
BatchGasValueTaxes,
BatchServicesValue,
BatchServicesValueTaxes,
BatchInsuranceValue,
BatchInsuranceValueTaxes,
IsRegisteredAsRevenue

FROM OPENXML (@BatchesOut, '/DocumentElement/Batches', 2)
with(MainDocId int ,
BatchNo	int	,
BatchDate	datetime	,
BatchRentValue	money	,
BatchRentValueTaxes	money	,
BatchWaterValue	money	,
BatchWaterValueTaxes	money	,
BatchElectricityValue	money	,
BatchElectricityValueTaxes	money	,
BatchCommissionValue	money	,
BatchTotal	money	,
JournalEntryId	int	,
IsDeleted	bit	,
UserId	int	,
Notes	nvarchar(MAX)	,
Image	nvarchar(MAX)	,
IsDelivered bit,
IsRegisteredAsDue bit,
BatchCommissionValueTaxes money,
BatchGasValue money,
BatchGasValueTaxes	money,
BatchServicesValue	money,
BatchServicesValueTaxes	money,
BatchInsuranceValue	money,
BatchInsuranceValueTaxes money,
IsRegisteredAsRevenue bit
)
EXEC sp_xml_removedocument @BatchesOut
end
--//-------------------------------------//----------------------------------------//--

--//------------------ Reps ----------------------//--
 DECLARE @RepsOut int
DELETE FROM [PropertyContractRep] WHERE [MainDocId]=@Id

EXEC sp_xml_preparedocument @RepsOut OUT, @Reps
INSERT INTO [dbo].[PropertyContractRep]
(
MainDocId,
RepId,
RepPercentage,
IsDeleted,
UserId,
Notes,
Image
)
SELECT @Id,
RepId,
RepPercentage,
IsDeleted,
UserId,
Notes,
Image

FROM OPENXML (@RepsOut, '/DocumentElement/Reps', 2)
with(
MainDocId int ,
RepId int	,
RepPercentage float	,
IsDeleted bit	,
UserId	int	,
Notes	nvarchar(MAX),
Image	nvarchar(MAX)
)
EXEC sp_xml_removedocument @RepsOut
--//-------------------------------------//----------------------------------------//--
--// --------------------- Images ------------------------- //--

DECLARE @ImagesOut int
DELETE FROM [PropertyContractImage] WHERE [MainDocId] = @Id

EXEC sp_xml_preparedocument @ImagesOut OUT, @Images

INSERT INTO [dbo].[PropertyContractImage]
(
    MainDocId,
    Image
)
SELECT
    @Id,
    Image
FROM OPENXML (@ImagesOut, '/DocumentElement/Images', 2)
WITH
(
    MainDocId int,
    Image nvarchar(MAX)
)

EXEC sp_xml_removedocument @ImagesOut

--//-------------------------------------//----------------------------------------//--

--//------------------ MergedUnits ----------------------//--

DECLARE @MergedUnitsOut int

-- مسح الوحدات المدمجة القديمة
DELETE FROM [PropertyContractMergedUnit]
WHERE [PropertyContractId] = @Id

EXEC sp_xml_preparedocument @MergedUnitsOut OUT, @MergedUnits

INSERT INTO [dbo].[PropertyContractMergedUnit]
(
    PropertyContractId,
    PropertyUnitId
)
SELECT
    @Id,
    PropertyUnitId
FROM OPENXML (@MergedUnitsOut, '/DocumentElement/MergedUnits', 2)
WITH
(
    PropertyContractId int,
    PropertyUnitId int
)

EXEC sp_xml_removedocument @MergedUnitsOut

--//-------- إرجاع الوحدات القديمة إلى متاحة (StatusId = 0) --------//--
-- إرجاع الوحدة الأساسية القديمة إلى متاحة (إذا تم تغييرها)
IF @OldPropertyUnitId IS NOT NULL AND @OldPropertyUnitId != @PropertyUnitId
BEGIN
    UPDATE PropertyDetail
    SET StatusId = 0
    WHERE Id = @OldPropertyUnitId
END

-- إرجاع الوحدات المدمجة القديمة التي تم إزالتها إلى متاحة
UPDATE PropertyDetail
SET StatusId = 0
WHERE Id IN (
    SELECT PropertyUnitId
    FROM @OldMergedUnits
    WHERE PropertyUnitId NOT IN (
        SELECT PropertyUnitId
        FROM PropertyContractMergedUnit
        WHERE PropertyContractId = @Id
    )
)
--//------------------------------------------------------------------//--

--//-------- تحديث الوحدات الجديدة إلى مشغولة (StatusId = 1) --------//--
-- تحديث حالة الوحدة الأساسية + كل المدمجة إلى مشغولة = 1
UPDATE PropertyDetail
SET StatusId = 1
WHERE Id = @PropertyUnitId
   OR Id IN (
        SELECT PropertyUnitId
        FROM PropertyContractMergedUnit
        WHERE PropertyContractId = @Id
   );
---//-----------------------------------------------------------------------------------------------------------------//--

--//--------------- Journal Entry -------------------------------//--
DECLARE @SourcePageId int = (select [Id] from [SystemPage] where [TableName] = 'PropertyContract')
-- عشان مش بيعمل قيد اتوماتيك فبشوف الاول لو له قيد ولا لأ عشان مايحصلشerror
Declare @PreviousJE_Id int =(select top (1)[Id] from JournalEntry where SourcePageId=@SourcePageId and SourceId=@Id)

if(@PreviousJE_Id is not null)
begin
    DELETE FROM [dbo].[JournalEntryDetail] WHERE [JournalEntryId]=@PreviousJE_Id;

    declare @CurrencyId int =(select top(1)Id from Currency where IsDeleted=0 and IsActive=1);
    declare @CurrencyEquivalent float=(select Equivalent from Currency where Id=@CurrencyId);

    -- تعريف الحسابات
    DECLARE @RenterAndBuyerAccountId int=(select RenterAndBuyerAccountId from Department where [Id]=@DepartmentId);
    DECLARE @DueRentId int=(select DueRentId from [Department] where [Id]=@DepartmentId);
    DECLARE @FirstBatchValue money=(select Top(1) BatchTotal from PropertyContractBatch where [MainDocId]=@Id);
    DECLARE @ElectricityDueRevenueId int=(select ElectricityDueRevenueId from [Department] where [Id]=@DepartmentId);
    DECLARE @WaterDueRevenueId int=(select WaterDueRevenueId from [Department] where [Id]=@DepartmentId);
    DECLARE @ServicesDueRevenueId int=(select ServicesDueRevenueId from [Department] where [Id]=@DepartmentId);
    DECLARE @GasDueRevenueId int=(select GasDueRevenueId from [Department] where [Id]=@DepartmentId);
    DECLARE @CompanyDueCommissionId int=(select CompanyDueCommissionId from [Department] where [Id]=@DepartmentId);
    DECLARE @PropertyRefundInsuranceId int=(select PropertyRefundInsuranceId from [Department] where [Id]=@DepartmentId);

    Declare @SumValues money = isnull(@WaterValue,0)+isnull(@ServicesValue,0)+isnull(@CommissionValue,0)+isnull(@ElectricityValue,0)+isnull(@GasValue,0);
    Declare @NetFirstBatchValue money = isnull(@FirstBatchValue,0) - isnull(@SumValues,0) - isnull(@InsuranceValue,0);

    UPDATE [dbo].[JournalEntry]
    SET [Date] = @VoucherDate, [Notes] = @Notes, [IsActive] = 1, [IsPosted] = 0, [IsDeleted] = @IsDeleted,
        [Image] = @Image, [UserId] = @UserId, [DepartmentId] = @DepartmentId, [CurrencyId] = @CurrencyId, [Equivalent] = @CurrencyEquivalent
    WHERE [Id] = @PreviousJE_Id;

    -- إعادة استخدام متغير @JournalEntryId بنفس القيمة للتوافق
    set @JournalEntryId = @PreviousJE_Id;

    DECLARE @JournalEntryDetail table(
        [JournalEntryId] [int] NULL, [Debit] [money] NULL, [Credit] [money] NULL, [CurrencyId] [int] NULL, [Equivalent] [float] NULL,
        [AccountId] [int] NULL, [SourcePageId] [int] NULL, [SourceId] [int] NULL, [IsPosted] [bit] NULL, [IsDeleted] [bit] NULL,
        [IsActive] [bit] NULL, CostCenterId int null,
        PartyType INT NULL, PartyId INT NULL -- << إضافة الحقلين
    );

    Insert into @JournalEntryDetail (JournalEntryId, Debit, Credit, CurrencyId, Equivalent, AccountId, SourcePageId, SourceId, IsPosted, IsDeleted, IsActive, CostCenterId, PartyType, PartyId)
    VALUES
    -- Debit " From " - السطر الخاص بالمستأجر
    (@JournalEntryId, ISNULL(@NetFirstBatchValue,0), 0, @CurrencyId, @CurrencyEquivalent, @RenterAndBuyerAccountId, @SourcePageId, @Id, 0,0,1, null, 4, @PropertyRenterId),
    -- Credit " To "
    (@JournalEntryId, 0, ISNULL(@NetFirstBatchValue,0), @CurrencyId, @CurrencyEquivalent, @DueRentId, @SourcePageId, @Id, 0,0,1, null, null, null);

    Insert into @JournalEntryDetail (JournalEntryId, Debit, Credit, CurrencyId, Equivalent, AccountId, SourcePageId, SourceId, IsPosted, IsDeleted, IsActive, CostCenterId, PartyType, PartyId)
    VALUES
    -- Debit " From " - السطر الخاص بالمستأجر
    (@JournalEntryId, ISNULL(@SumValues,0), 0, @CurrencyId, @CurrencyEquivalent, @RenterAndBuyerAccountId, @SourcePageId, @Id, 0,0,1, null, 4, @PropertyRenterId),
    -- Credit " To "
    (@JournalEntryId, 0 , ISNULL(@ServicesValue,0), @CurrencyId, @CurrencyEquivalent, @ServicesDueRevenueId, @SourcePageId, @Id, 0,0,1, null, null, null),
    (@JournalEntryId, 0, ISNULL(@ElectricityValue,0), @CurrencyId, @CurrencyEquivalent, @ElectricityDueRevenueId, @SourcePageId, @Id, 0,0,1, null, null, null),
    (@JournalEntryId, 0, ISNULL(@WaterValue,0), @CurrencyId, @CurrencyEquivalent, @WaterDueRevenueId, @SourcePageId, @Id, 0,0,1, null, null, null),
    (@JournalEntryId, 0, ISNULL(@CommissionValue,0), @CurrencyId, @CurrencyEquivalent, @CompanyDueCommissionId, @SourcePageId, @Id, 0,0,1, null, null, null),
    (@JournalEntryId, 0, ISNULL(@GasValue,0), @CurrencyId, @CurrencyEquivalent, @GasDueRevenueId, @SourcePageId, @Id, 0,0,1, null, null, null);

    Insert into @JournalEntryDetail (JournalEntryId, Debit, Credit, CurrencyId, Equivalent, AccountId, SourcePageId, SourceId, IsPosted, IsDeleted, IsActive, CostCenterId, PartyType, PartyId)
    VALUES
    -- Debit " From "
    (@JournalEntryId, ISNULL(@InsuranceValue,0), 0, @CurrencyId, @CurrencyEquivalent, @PropertyRefundInsuranceId, @SourcePageId, @Id, 0,0,1, null, null, null),
    -- Credit " To " - السطر الخاص بالمستأجر
    (@JournalEntryId, 0, ISNULL(@InsuranceValue,0), @CurrencyId, @CurrencyEquivalent, @RenterAndBuyerAccountId, @SourcePageId, @Id, 0,0,1, null, 4, @PropertyRenterId);

    Insert into [dbo].[JournalEntryDetail] (JournalEntryId, Debit, Credit, CurrencyId, Equivalent, AccountId, SourcePageId, SourceId, IsPosted, IsDeleted, IsActive, CostCenterId, PartyType, PartyId)
    select
        JournalEntryId, Debit, Credit, CurrencyId, Equivalent, AccountId, SourcePageId, SourceId, IsPosted, IsDeleted, IsActive, CostCenterId, PartyType, PartyId
    from @JournalEntryDetail where Debit>0 or Credit>0;
end
--//---------------------------------------------------------------------------------------------------------//--

lbexit:
        if @trancount = 0
            commit;
    end try
    begin catch
        declare @error int, @message varchar(4000), @xstate int;
        select @error = ERROR_NUMBER(), @message = ERROR_MESSAGE()+ ' at ' + cast(ERROR_LINE() as VARCHAR(50)), @xstate = XACT_STATE();
        if @xstate = -1
            rollback;
        if @xstate = 1 and @trancount = 0
            rollback
        if @xstate = 1 and @trancount > 0
            rollback transaction PropertyContract_Update;

        raiserror ('PropertyContract_Update: %d: %s', 16, 1, @error, @message ) ;
    end catch
end
