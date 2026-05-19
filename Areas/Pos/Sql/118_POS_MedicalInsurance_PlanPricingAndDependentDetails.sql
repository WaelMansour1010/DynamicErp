/* POS Medical Insurance plan pricing tiers and employee dependent detail fields.
   SQL Server 2012 compatible.
*/

IF OBJECT_ID(N'dbo.MedicalInsurancePlanAgePricing', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MedicalInsurancePlanAgePricing
    (
        TierId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MedicalInsurancePlanAgePricing PRIMARY KEY,
        PlanId INT NOT NULL,
        BeneficiaryType NVARCHAR(30) NOT NULL,
        Relation NVARCHAR(30) NULL,
        AgeFrom INT NOT NULL CONSTRAINT DF_MedicalInsurancePlanAgePricing_AgeFrom DEFAULT (0),
        AgeTo INT NOT NULL CONSTRAINT DF_MedicalInsurancePlanAgePricing_AgeTo DEFAULT (120),
        PremiumAmount MONEY NOT NULL CONSTRAINT DF_MedicalInsurancePlanAgePricing_Premium DEFAULT (0),
        EmployeeSharePercent MONEY NOT NULL CONSTRAINT DF_MedicalInsurancePlanAgePricing_EmpPercent DEFAULT (0),
        CompanySharePercent MONEY NOT NULL CONSTRAINT DF_MedicalInsurancePlanAgePricing_CoPercent DEFAULT (0),
        PriceMode NVARCHAR(20) NOT NULL CONSTRAINT DF_MedicalInsurancePlanAgePricing_Mode DEFAULT (N'Monthly'),
        EffectiveFrom DATETIME NULL,
        EffectiveTo DATETIME NULL,
        Notes NVARCHAR(MAX) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_MedicalInsurancePlanAgePricing_Active DEFAULT (1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_MedicalInsurancePlanAgePricing_CreatedAt DEFAULT (GETDATE())
    );
END;

IF COL_LENGTH(N'dbo.MedicalInsuranceDependents', N'NationalId') IS NULL
    ALTER TABLE dbo.MedicalInsuranceDependents ADD NationalId NVARCHAR(50) NULL;

IF COL_LENGTH(N'dbo.MedicalInsuranceDependents', N'IsCovered') IS NULL
    ALTER TABLE dbo.MedicalInsuranceDependents ADD IsCovered BIT NOT NULL CONSTRAINT DF_MedicalInsuranceDependents_IsCovered DEFAULT (1);

IF COL_LENGTH(N'dbo.MedicalInsuranceDependents', N'CoverageStartDate') IS NULL
    ALTER TABLE dbo.MedicalInsuranceDependents ADD CoverageStartDate DATETIME NULL;

IF COL_LENGTH(N'dbo.MedicalInsuranceDependents', N'CoverageEndDate') IS NULL
    ALTER TABLE dbo.MedicalInsuranceDependents ADD CoverageEndDate DATETIME NULL;

IF COL_LENGTH(N'dbo.MedicalInsuranceDependents', N'Status') IS NULL
    ALTER TABLE dbo.MedicalInsuranceDependents ADD Status NVARCHAR(30) NOT NULL CONSTRAINT DF_MedicalInsuranceDependents_Status DEFAULT (N'Active');

IF COL_LENGTH(N'dbo.MedicalInsuranceDependents', N'CalculatedPremium') IS NULL
    ALTER TABLE dbo.MedicalInsuranceDependents ADD CalculatedPremium MONEY NOT NULL CONSTRAINT DF_MedicalInsuranceDependents_CalculatedPremium DEFAULT (0);

IF COL_LENGTH(N'dbo.MedicalInsuranceDependents', N'ManualPremium') IS NULL
    ALTER TABLE dbo.MedicalInsuranceDependents ADD ManualPremium MONEY NOT NULL CONSTRAINT DF_MedicalInsuranceDependents_ManualPremium DEFAULT (0);

IF COL_LENGTH(N'dbo.MedicalInsuranceDependents', N'ExceptionReason') IS NULL
    ALTER TABLE dbo.MedicalInsuranceDependents ADD ExceptionReason NVARCHAR(500) NULL;

IF COL_LENGTH(N'dbo.MedicalInsuranceDependents', N'Notes') IS NULL
    ALTER TABLE dbo.MedicalInsuranceDependents ADD Notes NVARCHAR(MAX) NULL;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.MedicalInsurancePlanAgePricing')
      AND name = N'IX_MedicalInsurancePlanAgePricing_Plan_Type_Relation'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_MedicalInsurancePlanAgePricing_Plan_Type_Relation
    ON dbo.MedicalInsurancePlanAgePricing (PlanId, BeneficiaryType, Relation, IsActive, AgeFrom, AgeTo, EffectiveFrom, EffectiveTo);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.MedicalInsurancePlanAgePricing')
      AND name = N'CK_MedicalInsurancePlanAgePricing_AgeRange'
)
BEGIN
    ALTER TABLE dbo.MedicalInsurancePlanAgePricing
    ADD CONSTRAINT CK_MedicalInsurancePlanAgePricing_AgeRange CHECK (AgeFrom >= 0 AND AgeTo >= AgeFrom);
END;
