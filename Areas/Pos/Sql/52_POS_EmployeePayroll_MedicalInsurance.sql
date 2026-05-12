-- POS Employee Payroll / Medical Insurance enterprise extension.
-- Keep the base tables/procedures aligned with:
-- Areas\MainErp\Sql\06_EmployeePayroll_MedicalInsurance.sql
-- This POS script is executable and SQL Server 2012 compatible.

IF OBJECT_ID(N'dbo.MedicalInsurancePlans', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MedicalInsurancePlans
    (
        PlanId INT NOT NULL CONSTRAINT PK_MedicalInsurancePlans PRIMARY KEY,
        ProviderId INT NOT NULL,
        PlanNameAr NVARCHAR(200) NOT NULL,
        PlanNameEn NVARCHAR(200) NULL,
        DefaultMonthlyCost MONEY NOT NULL CONSTRAINT DF_MedicalInsurancePlans_DefaultMonthlyCost DEFAULT (0),
        DefaultEmployeeShareType NVARCHAR(20) NOT NULL CONSTRAINT DF_MedicalInsurancePlans_DefaultEmployeeShareType DEFAULT (N'Amount'),
        DefaultEmployeeShareValue MONEY NOT NULL CONSTRAINT DF_MedicalInsurancePlans_DefaultEmployeeShareValue DEFAULT (0),
        DefaultCompanyShareType NVARCHAR(20) NOT NULL CONSTRAINT DF_MedicalInsurancePlans_DefaultCompanyShareType DEFAULT (N'AutoBalance'),
        DefaultCompanyShareValue MONEY NOT NULL CONSTRAINT DF_MedicalInsurancePlans_DefaultCompanyShareValue DEFAULT (0),
        EmployeeDeductionAccountCode NVARCHAR(110) NULL,
        CompanyCostAccountCode NVARCHAR(110) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_MedicalInsurancePlans_IsActive DEFAULT (1),
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_MedicalInsurancePlans_CreatedAt DEFAULT (GETDATE()),
        CreatedBy INT NULL,
        UpdatedAt DATETIME NULL,
        UpdatedBy INT NULL
    );
END
GO

IF COL_LENGTH('dbo.MedicalInsurancePlans', 'LifecycleStatus') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD LifecycleStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_MedicalInsurancePlans_LifecycleStatus DEFAULT (N'Draft');
IF COL_LENGTH('dbo.MedicalInsurancePlans', 'StartDate') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD StartDate DATETIME NULL;
IF COL_LENGTH('dbo.MedicalInsurancePlans', 'EndDate') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD EndDate DATETIME NULL;
IF COL_LENGTH('dbo.MedicalInsurancePlans', 'PayrollStartDate') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD PayrollStartDate DATETIME NULL;
IF COL_LENGTH('dbo.MedicalInsurancePlans', 'SuspensionDate') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD SuspensionDate DATETIME NULL;
IF COL_LENGTH('dbo.MedicalInsurancePlans', 'CancellationDate') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD CancellationDate DATETIME NULL;
IF COL_LENGTH('dbo.MedicalInsurancePlans', 'CostCenterCode') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD CostCenterCode NVARCHAR(110) NULL;
IF COL_LENGTH('dbo.MedicalInsurancePlans', 'PayrollDeductionType') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD PayrollDeductionType NVARCHAR(20) NOT NULL CONSTRAINT DF_MedicalInsurancePlans_PayrollDeductionType DEFAULT (N'Fixed');
IF COL_LENGTH('dbo.MedicalInsurancePlans', 'IsMonthlyDeduction') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD IsMonthlyDeduction BIT NOT NULL CONSTRAINT DF_MedicalInsurancePlans_IsMonthlyDeduction DEFAULT (1);
IF COL_LENGTH('dbo.MedicalInsurancePlans', 'AutoStopAtEndDate') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD AutoStopAtEndDate BIT NOT NULL CONSTRAINT DF_MedicalInsurancePlans_AutoStopAtEndDate DEFAULT (1);
IF COL_LENGTH('dbo.MedicalInsurancePlans', 'ShowInPayroll') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD ShowInPayroll BIT NOT NULL CONSTRAINT DF_MedicalInsurancePlans_ShowInPayroll DEFAULT (1);
IF COL_LENGTH('dbo.MedicalInsurancePlans', 'DistributeByDepartment') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD DistributeByDepartment BIT NOT NULL CONSTRAINT DF_MedicalInsurancePlans_DistributeByDepartment DEFAULT (0);
IF COL_LENGTH('dbo.MedicalInsurancePlans', 'DistributeByCostCenter') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD DistributeByCostCenter BIT NOT NULL CONSTRAINT DF_MedicalInsurancePlans_DistributeByCostCenter DEFAULT (0);
IF COL_LENGTH('dbo.MedicalInsurancePlans', 'TaxMode') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD TaxMode NVARCHAR(20) NOT NULL CONSTRAINT DF_MedicalInsurancePlans_TaxMode DEFAULT (N'AfterTax');
IF COL_LENGTH('dbo.MedicalInsurancePlans', 'MaxDependents') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD MaxDependents INT NOT NULL CONSTRAINT DF_MedicalInsurancePlans_MaxDependents DEFAULT (4);
IF COL_LENGTH('dbo.MedicalInsurancePlans', 'ChildrenMaxAge') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD ChildrenMaxAge INT NOT NULL CONSTRAINT DF_MedicalInsurancePlans_ChildrenMaxAge DEFAULT (21);
IF COL_LENGTH('dbo.MedicalInsurancePlans', 'SpouseAdditionalCost') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD SpouseAdditionalCost MONEY NOT NULL CONSTRAINT DF_MedicalInsurancePlans_SpouseAdditionalCost DEFAULT (0);
IF COL_LENGTH('dbo.MedicalInsurancePlans', 'ChildAdditionalCost') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD ChildAdditionalCost MONEY NOT NULL CONSTRAINT DF_MedicalInsurancePlans_ChildAdditionalCost DEFAULT (0);
IF COL_LENGTH('dbo.MedicalInsurancePlans', 'ParentAdditionalCost') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD ParentAdditionalCost MONEY NOT NULL CONSTRAINT DF_MedicalInsurancePlans_ParentAdditionalCost DEFAULT (0);
IF COL_LENGTH('dbo.MedicalInsurancePlans', 'DefaultCoveragePercent') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD DefaultCoveragePercent MONEY NOT NULL CONSTRAINT DF_MedicalInsurancePlans_DefaultCoveragePercent DEFAULT (100);
IF COL_LENGTH('dbo.MedicalInsurancePlans', 'AutoEnrollAfterDays') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD AutoEnrollAfterDays INT NOT NULL CONSTRAINT DF_MedicalInsurancePlans_AutoEnrollAfterDays DEFAULT (30);
IF COL_LENGTH('dbo.MedicalInsurancePlans', 'AutoEnrollCriteria') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD AutoEnrollCriteria NVARCHAR(1000) NULL;
IF COL_LENGTH('dbo.MedicalInsurancePlans', 'RulesJson') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD RulesJson NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.MedicalInsurancePlans', 'DependentsTemplateJson') IS NULL ALTER TABLE dbo.MedicalInsurancePlans ADD DependentsTemplateJson NVARCHAR(MAX) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MedicalInsurancePlans_Lifecycle' AND object_id = OBJECT_ID(N'dbo.MedicalInsurancePlans'))
BEGIN
    CREATE INDEX IX_MedicalInsurancePlans_Lifecycle ON dbo.MedicalInsurancePlans (LifecycleStatus, StartDate, EndDate, IsActive);
END
GO
