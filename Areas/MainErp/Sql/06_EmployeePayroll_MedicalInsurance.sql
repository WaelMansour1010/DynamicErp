IF OBJECT_ID(N'dbo.MedicalInsuranceProviders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MedicalInsuranceProviders
    (
        ProviderId INT NOT NULL CONSTRAINT PK_MedicalInsuranceProviders PRIMARY KEY,
        ProviderNameAr NVARCHAR(200) NOT NULL,
        ProviderNameEn NVARCHAR(200) NULL,
        Phone NVARCHAR(50) NULL,
        Notes NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_MedicalInsuranceProviders_IsActive DEFAULT (1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_MedicalInsuranceProviders_CreatedAt DEFAULT (GETDATE()),
        CreatedBy INT NULL
    );
END
GO

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

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MedicalInsurancePlans_Provider' AND object_id = OBJECT_ID(N'dbo.MedicalInsurancePlans'))
BEGIN
    CREATE INDEX IX_MedicalInsurancePlans_Provider ON dbo.MedicalInsurancePlans (ProviderId, IsActive);
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

IF OBJECT_ID(N'dbo.EmployeeMedicalInsurance', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmployeeMedicalInsurance
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmployeeMedicalInsurance PRIMARY KEY,
        EmpId INT NOT NULL,
        PlanId INT NULL,
        Amount MONEY NOT NULL CONSTRAINT DF_EmployeeMedicalInsurance_Amount DEFAULT (0),
        PercentValue MONEY NOT NULL CONSTRAINT DF_EmployeeMedicalInsurance_PercentValue DEFAULT (0),
        DeductionType NVARCHAR(20) NOT NULL CONSTRAINT DF_EmployeeMedicalInsurance_DeductionType DEFAULT (N'Amount'),
        StartDate DATETIME NOT NULL,
        EndDate DATETIME NULL,
        IsMonthly BIT NOT NULL CONSTRAINT DF_EmployeeMedicalInsurance_IsMonthly DEFAULT (1),
        IsActive BIT NOT NULL CONSTRAINT DF_EmployeeMedicalInsurance_IsActive DEFAULT (1),
        MonthlyCost MONEY NOT NULL CONSTRAINT DF_EmployeeMedicalInsurance_MonthlyCost DEFAULT (0),
        EmployeeShareType NVARCHAR(20) NOT NULL CONSTRAINT DF_EmployeeMedicalInsurance_EmployeeShareType DEFAULT (N'Amount'),
        EmployeeShareValue MONEY NOT NULL CONSTRAINT DF_EmployeeMedicalInsurance_EmployeeShareValue DEFAULT (0),
        CompanyShareType NVARCHAR(20) NOT NULL CONSTRAINT DF_EmployeeMedicalInsurance_CompanyShareType DEFAULT (N'AutoBalance'),
        CompanyShareValue MONEY NOT NULL CONSTRAINT DF_EmployeeMedicalInsurance_CompanyShareValue DEFAULT (0),
        EmployeeMonthlyDeduction MONEY NOT NULL CONSTRAINT DF_EmployeeMedicalInsurance_EmployeeMonthlyDeduction DEFAULT (0),
        CompanyMonthlyCost MONEY NOT NULL CONSTRAINT DF_EmployeeMedicalInsurance_CompanyMonthlyCost DEFAULT (0),
        Notes NVARCHAR(500) NULL,
        CreatedBy INT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_EmployeeMedicalInsurance_CreatedAt DEFAULT (GETDATE()),
        UpdatedAt DATETIME NULL,
        UpdatedBy INT NULL
    );
END
GO

IF COL_LENGTH('dbo.EmployeeMedicalInsurance', 'PlanId') IS NULL ALTER TABLE dbo.EmployeeMedicalInsurance ADD PlanId INT NULL;
IF COL_LENGTH('dbo.EmployeeMedicalInsurance', 'MonthlyCost') IS NULL ALTER TABLE dbo.EmployeeMedicalInsurance ADD MonthlyCost MONEY NOT NULL CONSTRAINT DF_EmployeeMedicalInsurance_MonthlyCost DEFAULT (0);
IF COL_LENGTH('dbo.EmployeeMedicalInsurance', 'EmployeeShareType') IS NULL ALTER TABLE dbo.EmployeeMedicalInsurance ADD EmployeeShareType NVARCHAR(20) NOT NULL CONSTRAINT DF_EmployeeMedicalInsurance_EmployeeShareType DEFAULT (N'Amount');
IF COL_LENGTH('dbo.EmployeeMedicalInsurance', 'EmployeeShareValue') IS NULL ALTER TABLE dbo.EmployeeMedicalInsurance ADD EmployeeShareValue MONEY NOT NULL CONSTRAINT DF_EmployeeMedicalInsurance_EmployeeShareValue DEFAULT (0);
IF COL_LENGTH('dbo.EmployeeMedicalInsurance', 'CompanyShareType') IS NULL ALTER TABLE dbo.EmployeeMedicalInsurance ADD CompanyShareType NVARCHAR(20) NOT NULL CONSTRAINT DF_EmployeeMedicalInsurance_CompanyShareType DEFAULT (N'AutoBalance');
IF COL_LENGTH('dbo.EmployeeMedicalInsurance', 'CompanyShareValue') IS NULL ALTER TABLE dbo.EmployeeMedicalInsurance ADD CompanyShareValue MONEY NOT NULL CONSTRAINT DF_EmployeeMedicalInsurance_CompanyShareValue DEFAULT (0);
IF COL_LENGTH('dbo.EmployeeMedicalInsurance', 'EmployeeMonthlyDeduction') IS NULL ALTER TABLE dbo.EmployeeMedicalInsurance ADD EmployeeMonthlyDeduction MONEY NOT NULL CONSTRAINT DF_EmployeeMedicalInsurance_EmployeeMonthlyDeduction DEFAULT (0);
IF COL_LENGTH('dbo.EmployeeMedicalInsurance', 'CompanyMonthlyCost') IS NULL ALTER TABLE dbo.EmployeeMedicalInsurance ADD CompanyMonthlyCost MONEY NOT NULL CONSTRAINT DF_EmployeeMedicalInsurance_CompanyMonthlyCost DEFAULT (0);
IF COL_LENGTH('dbo.EmployeeMedicalInsurance', 'UpdatedAt') IS NULL ALTER TABLE dbo.EmployeeMedicalInsurance ADD UpdatedAt DATETIME NULL;
IF COL_LENGTH('dbo.EmployeeMedicalInsurance', 'UpdatedBy') IS NULL ALTER TABLE dbo.EmployeeMedicalInsurance ADD UpdatedBy INT NULL;
GO

UPDATE dbo.EmployeeMedicalInsurance
SET MonthlyCost = CASE WHEN MonthlyCost = 0 THEN Amount ELSE MonthlyCost END,
    EmployeeShareType = CASE WHEN ISNULL(EmployeeShareType, N'') = N'' THEN DeductionType ELSE EmployeeShareType END,
    EmployeeShareValue = CASE
        WHEN EmployeeShareValue = 0 AND DeductionType = N'Percent' THEN PercentValue
        WHEN EmployeeShareValue = 0 THEN Amount
        ELSE EmployeeShareValue
    END,
    EmployeeMonthlyDeduction = CASE
        WHEN EmployeeMonthlyDeduction = 0 AND DeductionType = N'Percent' THEN ROUND(ISNULL(Amount, 0) * ISNULL(PercentValue, 0) / 100.0, 2)
        WHEN EmployeeMonthlyDeduction = 0 THEN Amount
        ELSE EmployeeMonthlyDeduction
    END,
    CompanyMonthlyCost = CASE
        WHEN CompanyMonthlyCost = 0 AND MonthlyCost > EmployeeMonthlyDeduction THEN MonthlyCost - EmployeeMonthlyDeduction
        ELSE CompanyMonthlyCost
    END
WHERE MonthlyCost = 0 OR EmployeeMonthlyDeduction = 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EmployeeMedicalInsurance_Emp_Period' AND object_id = OBJECT_ID(N'dbo.EmployeeMedicalInsurance'))
BEGIN
    CREATE INDEX IX_EmployeeMedicalInsurance_Emp_Period
    ON dbo.EmployeeMedicalInsurance (EmpId, IsActive, IsMonthly, StartDate, EndDate);
END
GO

IF OBJECT_ID(N'dbo.PayrollMedicalInsuranceDeduction', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PayrollMedicalInsuranceDeduction
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PayrollMedicalInsuranceDeduction PRIMARY KEY,
        SalaryRunId INT NULL,
        EmpId INT NOT NULL,
        EmployeeInsuranceId INT NULL,
        [Year] INT NOT NULL,
        [Month] INT NOT NULL,
        PeriodFrom DATETIME NOT NULL,
        PeriodTo DATETIME NOT NULL,
        MonthlyCost MONEY NOT NULL CONSTRAINT DF_PayrollMedicalInsuranceDeduction_MonthlyCost DEFAULT (0),
        EmployeeDeduction MONEY NOT NULL CONSTRAINT DF_PayrollMedicalInsuranceDeduction_EmployeeDeduction DEFAULT (0),
        CompanyCost MONEY NOT NULL CONSTRAINT DF_PayrollMedicalInsuranceDeduction_CompanyCost DEFAULT (0),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_PayrollMedicalInsuranceDeduction_CreatedAt DEFAULT (GETDATE()),
        CreatedBy INT NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PayrollMedicalInsuranceDeduction_Emp_Period' AND object_id = OBJECT_ID(N'dbo.PayrollMedicalInsuranceDeduction'))
BEGIN
    CREATE UNIQUE INDEX UX_PayrollMedicalInsuranceDeduction_Emp_Period
    ON dbo.PayrollMedicalInsuranceDeduction (EmpId, [Year], [Month]);
END
GO

IF OBJECT_ID(N'dbo.SalaryRunMedicalInsuranceDeduction', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalaryRunMedicalInsuranceDeduction
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalaryRunMedicalInsuranceDeduction PRIMARY KEY,
        EmpId INT NOT NULL,
        [Year] INT NOT NULL,
        [Month] INT NOT NULL,
        Amount MONEY NOT NULL,
        MonthlyCost MONEY NOT NULL CONSTRAINT DF_SalaryRunMedicalInsuranceDeduction_MonthlyCost DEFAULT (0),
        CompanyCost MONEY NOT NULL CONSTRAINT DF_SalaryRunMedicalInsuranceDeduction_CompanyCost DEFAULT (0),
        EmployeeInsuranceId INT NULL,
        CreatedBy INT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_SalaryRunMedicalInsuranceDeduction_CreatedAt DEFAULT (GETDATE())
    );
END
GO

IF COL_LENGTH('dbo.SalaryRunMedicalInsuranceDeduction', 'MonthlyCost') IS NULL ALTER TABLE dbo.SalaryRunMedicalInsuranceDeduction ADD MonthlyCost MONEY NOT NULL CONSTRAINT DF_SalaryRunMedicalInsuranceDeduction_MonthlyCost DEFAULT (0);
IF COL_LENGTH('dbo.SalaryRunMedicalInsuranceDeduction', 'CompanyCost') IS NULL ALTER TABLE dbo.SalaryRunMedicalInsuranceDeduction ADD CompanyCost MONEY NOT NULL CONSTRAINT DF_SalaryRunMedicalInsuranceDeduction_CompanyCost DEFAULT (0);
IF COL_LENGTH('dbo.SalaryRunMedicalInsuranceDeduction', 'EmployeeInsuranceId') IS NULL ALTER TABLE dbo.SalaryRunMedicalInsuranceDeduction ADD EmployeeInsuranceId INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_SalaryRunMedicalInsuranceDeduction_Emp_Period' AND object_id = OBJECT_ID(N'dbo.SalaryRunMedicalInsuranceDeduction'))
BEGIN
    CREATE UNIQUE INDEX UX_SalaryRunMedicalInsuranceDeduction_Emp_Period
    ON dbo.SalaryRunMedicalInsuranceDeduction (EmpId, [Year], [Month]);
END
GO

IF OBJECT_ID(N'dbo.usp_EmployeePayroll_GetMedicalInsuranceDeduction', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_EmployeePayroll_GetMedicalInsuranceDeduction;
GO
CREATE PROCEDURE dbo.usp_EmployeePayroll_GetMedicalInsuranceDeduction
    @EmpId INT,
    @PeriodStart DATETIME,
    @PeriodEnd DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        mi.Id,
        mi.EmpId,
        mi.PlanId,
        pl.PlanNameAr,
        mi.MonthlyCost,
        mi.EmployeeMonthlyDeduction AS EmployeeDeduction,
        mi.CompanyMonthlyCost AS CompanyCost,
        mi.EmployeeShareType,
        mi.EmployeeShareValue,
        mi.CompanyShareType,
        mi.CompanyShareValue,
        mi.StartDate,
        mi.EndDate
    FROM dbo.EmployeeMedicalInsurance mi
    LEFT JOIN dbo.MedicalInsurancePlans pl ON pl.PlanId = mi.PlanId
    WHERE mi.EmpId = @EmpId
      AND mi.IsActive = 1
      AND mi.IsMonthly = 1
      AND mi.StartDate <= @PeriodEnd
      AND (mi.EndDate IS NULL OR mi.EndDate >= @PeriodStart)
    ORDER BY mi.StartDate DESC, mi.Id DESC;
END
GO
