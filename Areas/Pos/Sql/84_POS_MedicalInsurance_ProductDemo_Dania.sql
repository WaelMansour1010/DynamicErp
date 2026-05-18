/*
    POS Medical Insurance product demo data for Dania
    SQL Server 2012 compatible.

    Prerequisite:
    Run Areas/Pos/Sql/52_POS_EmployeePayroll_MedicalInsurance.sql first.

    Safety:
    - Inserts demo providers, plans, dependents, installments, and employee subscriptions only when missing.
    - Does not create Notes or DOUBLE_ENTREY_VOUCHERS.
    - Does not mark payroll as posted or paid.
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.MedicalInsuranceProviders', N'U') IS NULL
BEGIN
    RAISERROR('MedicalInsuranceProviders is missing. Run Areas/Pos/Sql/52_POS_EmployeePayroll_MedicalInsurance.sql first.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.MedicalInsurancePlans', N'U') IS NULL
BEGIN
    RAISERROR('MedicalInsurancePlans is missing. Run Areas/Pos/Sql/52_POS_EmployeePayroll_MedicalInsurance.sql first.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.EmployeeMedicalInsurance', N'U') IS NULL
BEGIN
    RAISERROR('EmployeeMedicalInsurance is missing. Run Areas/Pos/Sql/52_POS_EmployeePayroll_MedicalInsurance.sql first.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.MedicalInsuranceDependents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MedicalInsuranceDependents
    (
        DependentId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MedicalInsuranceDependents PRIMARY KEY,
        EmpId INT NOT NULL,
        EmployeeInsuranceId INT NULL,
        DependentName NVARCHAR(200) NOT NULL,
        Relation NVARCHAR(30) NOT NULL,
        BirthDate DATETIME NULL,
        CoveragePercent MONEY NOT NULL CONSTRAINT DF_MedicalInsuranceDependents_CoveragePercent DEFAULT (100),
        IsActive BIT NOT NULL CONSTRAINT DF_MedicalInsuranceDependents_IsActive DEFAULT (1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_MedicalInsuranceDependents_CreatedAt DEFAULT (GETDATE())
    );
END;

IF OBJECT_ID(N'dbo.MedicalInsuranceInstallments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MedicalInsuranceInstallments
    (
        InstallmentId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MedicalInsuranceInstallments PRIMARY KEY,
        EmpId INT NOT NULL,
        EmployeeInsuranceId INT NULL,
        DueDate DATETIME NOT NULL,
        Amount MONEY NOT NULL CONSTRAINT DF_MedicalInsuranceInstallments_Amount DEFAULT (0),
        IsPaid BIT NOT NULL CONSTRAINT DF_MedicalInsuranceInstallments_IsPaid DEFAULT (0),
        PaidDate DATETIME NULL,
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_MedicalInsuranceInstallments_CreatedAt DEFAULT (GETDATE())
    );
END;

IF OBJECT_ID(N'dbo.MedicalInsuranceDocuments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MedicalInsuranceDocuments
    (
        DocumentId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MedicalInsuranceDocuments PRIMARY KEY,
        EmpId INT NOT NULL,
        EmployeeInsuranceId INT NULL,
        DocumentType NVARCHAR(50) NOT NULL,
        DocumentTitle NVARCHAR(200) NOT NULL,
        DocumentNo NVARCHAR(100) NULL,
        FilePath NVARCHAR(500) NULL,
        ExpiryDate DATETIME NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_MedicalInsuranceDocuments_IsActive DEFAULT (1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_MedicalInsuranceDocuments_CreatedAt DEFAULT (GETDATE())
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MedicalInsuranceDependents_Emp' AND object_id = OBJECT_ID(N'dbo.MedicalInsuranceDependents'))
    CREATE INDEX IX_MedicalInsuranceDependents_Emp ON dbo.MedicalInsuranceDependents(EmpId, IsActive);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MedicalInsuranceInstallments_EmpDue' AND object_id = OBJECT_ID(N'dbo.MedicalInsuranceInstallments'))
    CREATE INDEX IX_MedicalInsuranceInstallments_EmpDue ON dbo.MedicalInsuranceInstallments(EmpId, IsPaid, DueDate);

IF NOT EXISTS (SELECT 1 FROM dbo.MedicalInsuranceProviders WHERE ProviderId = 9101)
BEGIN
    INSERT INTO dbo.MedicalInsuranceProviders(ProviderId, ProviderNameAr, ProviderNameEn, Phone, Notes, IsActive, CreatedAt, CreatedBy)
    VALUES
    (9101, N'ظ…طµط± ظ„ظ„طھط£ظ…ظٹظ† ط§ظ„ط·ط¨ظٹ', N'Misr Medical Insurance', N'19600', N'Demo Egyptian provider for POS medical-insurance product story.', 1, GETDATE(), NULL),
    (9102, N'ط£ظƒط³ط§ ظ…طµط± ظ„ظ„ط±ط¹ط§ظٹط© ط§ظ„ط·ط¨ظٹط©', N'AXA Egypt Healthcare', N'16363', N'Demo provider with family plan visibility.', 1, GETDATE(), NULL),
    (9103, N'ظ…ظٹط¯ ط±ط§ظٹطھ ظ„ظ„ط®ط¯ظ…ط§طھ ط§ظ„ط·ط¨ظٹط©', N'Med Right Services', N'19988', N'Demo provider for branch operational follow-up.', 1, GETDATE(), NULL);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.MedicalInsurancePlans WHERE PlanId = 9201)
BEGIN
    INSERT INTO dbo.MedicalInsurancePlans
    (
        PlanId, ProviderId, PlanNameAr, PlanNameEn, DefaultMonthlyCost,
        DefaultEmployeeShareType, DefaultEmployeeShareValue,
        DefaultCompanyShareType, DefaultCompanyShareValue,
        EmployeeDeductionAccountCode, CompanyCostAccountCode,
        IsActive, Notes, CreatedAt, CreatedBy,
        LifecycleStatus, StartDate, EndDate, PayrollStartDate,
        PayrollDeductionType, IsMonthlyDeduction, AutoStopAtEndDate, ShowInPayroll,
        DistributeByDepartment, DistributeByCostCenter, TaxMode,
        MaxDependents, ChildrenMaxAge, SpouseAdditionalCost, ChildAdditionalCost,
        ParentAdditionalCost, DefaultCoveragePercent, AutoEnrollAfterDays, AutoEnrollCriteria
    )
    VALUES
    (9201, 9101, N'ط®ط·ط© ط§ظ„ظ‚ط§ظ‡ط±ط© ط§ظ„ط¹ط§ط¦ظ„ظٹط©', N'Cairo Family Plan', 1450, N'Amount', 450, N'AutoBalance', 0,
        N'EMP-INS-DEDUCT', N'MED-INS-EXP', 1, N'ط®ط·ط© ظ…ظ†ط§ط³ط¨ط© ظ„ظ„ظ…ظˆط¸ظپ ظˆط§ظ„ط£ط³ط±ط© ظ…ط¹ ط®طµظ… ط´ظ‡ط±ظٹ ظˆط§ط¶ط­.', GETDATE(), NULL,
        N'Active', '2026-01-01', '2026-12-31', '2026-01-01', N'Fixed', 1, 1, 1, 1, 0, N'AfterTax',
        4, 21, 520, 320, 650, 100, 30, N'Branches: Cairo/Giza; full-time employees'),
    (9202, 9102, N'ط®ط·ط© ط§ظ„ظ…ط­ط§ظپط¸ط§طھ ط§ظ„ط£ط³ط§ط³ظٹط©', N'Governorates Essential Plan', 850, N'Percent', 35, N'AutoBalance', 0,
        N'EMP-INS-DEDUCT', N'MED-INS-EXP', 1, N'ط®ط·ط© طھط´ط؛ظٹظ„ظٹط© ظ„ظ„ظپط±ظˆط¹ ط¨طھظƒظ„ظپط© ط£ظ‚ظ„ ظˆظ…طھط§ط¨ط¹ط© طھط¬ط¯ظٹط¯ط§طھ.', GETDATE(), NULL,
        N'Active', '2026-02-01', '2027-01-31', '2026-02-01', N'Fixed', 1, 1, 1, 1, 0, N'AfterTax',
        3, 21, 410, 240, 560, 90, 45, N'Branch operators can view status only'),
    (9203, 9103, N'ط®ط·ط© ط§ظ„ط¥ط¯ط§ط±ط© ط§ظ„طھظ†ظپظٹط°ظٹط©', N'Executive Care Plan', 2600, N'Amount', 600, N'AutoBalance', 0,
        N'EMP-INS-DEDUCT', N'MED-INS-EXP', 1, N'ط®ط·ط© ظ‚ظٹط§ط¯ظٹط© طھط¹ط±ط¶ طھط­ظ…ظ„ ط§ظ„ط´ط±ظƒط© ظˆط§ظ„طھط£ط«ظٹط± ط§ظ„ظ…ط§ظ„ظٹ ط¨ظˆط¶ظˆط­.', GETDATE(), NULL,
        N'Active', '2026-03-01', '2027-02-28', '2026-03-01', N'Fixed', 1, 1, 1, 0, 1, N'AfterTax',
        5, 24, 850, 500, 900, 100, 15, N'Management / executives');
END;

UPDATE dbo.MedicalInsuranceProviders
SET ProviderNameAr = CASE ProviderId
    WHEN 9101 THEN N'مصر للتأمين الطبي'
    WHEN 9102 THEN N'أكسا مصر للرعاية الطبية'
    WHEN 9103 THEN N'ميد رايت للخدمات الطبية'
    ELSE ProviderNameAr END,
    ProviderNameEn = CASE ProviderId
    WHEN 9101 THEN N'Misr Medical Insurance'
    WHEN 9102 THEN N'AXA Egypt Healthcare'
    WHEN 9103 THEN N'Med Right Services'
    ELSE ProviderNameEn END,
    Notes = CASE ProviderId
    WHEN 9101 THEN N'Demo Egyptian provider for POS medical-insurance product story.'
    WHEN 9102 THEN N'Demo provider with family plan visibility.'
    WHEN 9103 THEN N'Demo provider for branch operational follow-up.'
    ELSE Notes END
WHERE ProviderId IN (9101, 9102, 9103);

UPDATE dbo.MedicalInsurancePlans
SET PlanNameAr = CASE PlanId
    WHEN 9201 THEN N'خطة القاهرة العائلية'
    WHEN 9202 THEN N'خطة المحافظات الأساسية'
    WHEN 9203 THEN N'خطة الإدارة التنفيذية'
    ELSE PlanNameAr END,
    Notes = CASE PlanId
    WHEN 9201 THEN N'خطة مناسبة للموظف والأسرة مع خصم شهري واضح.'
    WHEN 9202 THEN N'خطة تشغيلية للفروع بتكلفة أقل ومتابعة تجديدات.'
    WHEN 9203 THEN N'خطة قيادية تعرض تحمل الشركة والتأثير المالي بوضوح.'
    ELSE Notes END
WHERE PlanId IN (9201, 9202, 9203);

;WITH DemoEmployees AS
(
    SELECT TOP (12)
        e.Emp_ID,
        ROW_NUMBER() OVER (ORDER BY ISNULL(e.chkStop, 0), e.Emp_ID) AS RowNo
    FROM dbo.TblEmployee e WITH (NOLOCK)
    WHERE ISNULL(e.Emp_Name, N'') <> N''
      AND ISNULL(e.chkStop, 0) = 0
    ORDER BY e.Emp_ID
),
Planned AS
(
    SELECT
        Emp_ID,
        CASE WHEN RowNo IN (1, 2, 3, 4, 5) THEN 9201 WHEN RowNo IN (6, 7, 8, 9) THEN 9202 ELSE 9203 END AS PlanId,
        CASE WHEN RowNo IN (4, 8) THEN DATEADD(DAY, -310, GETDATE()) ELSE DATEADD(DAY, -90 - (RowNo * 5), GETDATE()) END AS StartDate,
        CASE
            WHEN RowNo IN (3, 7) THEN DATEADD(DAY, 25, GETDATE())
            WHEN RowNo IN (5, 11) THEN DATEADD(DAY, -12, GETDATE())
            ELSE DATEADD(DAY, 180 + RowNo, GETDATE())
        END AS EndDate,
        CASE WHEN RowNo IN (6, 10) THEN 0 ELSE 1 END AS IsActive,
        RowNo
    FROM DemoEmployees
)
INSERT INTO dbo.EmployeeMedicalInsurance
(
    EmpId, PlanId, Amount, PercentValue, DeductionType, StartDate, EndDate, IsMonthly, IsActive,
    MonthlyCost, EmployeeShareType, EmployeeShareValue, CompanyShareType, CompanyShareValue,
    EmployeeMonthlyDeduction, CompanyMonthlyCost, Notes, CreatedBy, CreatedAt
)
SELECT
    p.Emp_ID,
    p.PlanId,
    CASE p.PlanId WHEN 9201 THEN 450 WHEN 9202 THEN 300 ELSE 600 END,
    CASE p.PlanId WHEN 9202 THEN 35 ELSE 0 END,
    CASE p.PlanId WHEN 9202 THEN N'Percent' ELSE N'Amount' END,
    p.StartDate,
    p.EndDate,
    1,
    p.IsActive,
    pl.DefaultMonthlyCost,
    pl.DefaultEmployeeShareType,
    pl.DefaultEmployeeShareValue,
    pl.DefaultCompanyShareType,
    pl.DefaultCompanyShareValue,
    CASE p.PlanId WHEN 9201 THEN 450 WHEN 9202 THEN 300 ELSE 600 END,
    pl.DefaultMonthlyCost - CASE p.PlanId WHEN 9201 THEN 450 WHEN 9202 THEN 300 ELSE 600 END,
    N'[DEMO] ط§ط´طھط±ط§ظƒ طھط£ظ…ظٹظ† ط·ط¨ظٹ طھط´ط؛ظٹظ„ظٹ ظ„ظ„ط¹ط±ط¶ ظپظٹ POS/MainErp.',
    NULL,
    GETDATE()
FROM Planned p
INNER JOIN dbo.MedicalInsurancePlans pl ON pl.PlanId = p.PlanId
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.EmployeeMedicalInsurance mi
    WHERE mi.EmpId = p.Emp_ID
      AND mi.PlanId = p.PlanId
      AND mi.Notes LIKE N'[[]DEMO]%' 
);

INSERT INTO dbo.MedicalInsuranceDependents(EmpId, EmployeeInsuranceId, DependentName, Relation, BirthDate, CoveragePercent, IsActive, CreatedAt)
SELECT mi.EmpId, mi.Id, N'طھط§ط¨ط¹ ط£ط³ط±ط© - ط²ظˆط¬/ط²ظˆط¬ط©', N'Spouse', DATEADD(YEAR, -32, GETDATE()), 100, 1, GETDATE()
FROM dbo.EmployeeMedicalInsurance mi
WHERE mi.Notes LIKE N'[[]DEMO]%' 
  AND NOT EXISTS (SELECT 1 FROM dbo.MedicalInsuranceDependents d WHERE d.EmployeeInsuranceId = mi.Id AND d.Relation = N'Spouse');

INSERT INTO dbo.MedicalInsuranceDependents(EmpId, EmployeeInsuranceId, DependentName, Relation, BirthDate, CoveragePercent, IsActive, CreatedAt)
SELECT TOP (8) mi.EmpId, mi.Id, N'طھط§ط¨ط¹ ط£ط³ط±ط© - ط§ط¨ظ†/ط§ط¨ظ†ط©', N'Child', DATEADD(YEAR, -9, GETDATE()), 90, 1, GETDATE()
FROM dbo.EmployeeMedicalInsurance mi
WHERE mi.Notes LIKE N'[[]DEMO]%' 
  AND NOT EXISTS (SELECT 1 FROM dbo.MedicalInsuranceDependents d WHERE d.EmployeeInsuranceId = mi.Id AND d.Relation = N'Child')
ORDER BY mi.Id;

INSERT INTO dbo.MedicalInsuranceInstallments(EmpId, EmployeeInsuranceId, DueDate, Amount, IsPaid, PaidDate, Notes, CreatedAt)
SELECT mi.EmpId, mi.Id, DATEADD(MONTH, -1, GETDATE()), mi.EmployeeMonthlyDeduction, 0, NULL, N'[DEMO] ظ‚ط³ط· ظ…طھط£ط®ط± ظ„ظ„ظ…طھط§ط¨ط¹ط© ط§ظ„طھط´ط؛ظٹظ„ظٹط©.', GETDATE()
FROM dbo.EmployeeMedicalInsurance mi
WHERE mi.Notes LIKE N'[[]DEMO]%' 
  AND mi.Id IN (SELECT TOP (4) Id FROM dbo.EmployeeMedicalInsurance WHERE Notes LIKE N'[[]DEMO]%'  ORDER BY Id)
  AND NOT EXISTS (SELECT 1 FROM dbo.MedicalInsuranceInstallments i WHERE i.EmployeeInsuranceId = mi.Id AND i.Notes LIKE N'[[]DEMO]%' );

INSERT INTO dbo.MedicalInsuranceInstallments(EmpId, EmployeeInsuranceId, DueDate, Amount, IsPaid, PaidDate, Notes, CreatedAt)
SELECT mi.EmpId, mi.Id, DATEADD(DAY, 10, GETDATE()), mi.EmployeeMonthlyDeduction, 0, NULL, N'[DEMO] ظ‚ط³ط· ط´ظ‡ط± ظ‚ط§ط¯ظ… ظ„ظ„ظ…طھط§ط¨ط¹ط©.', GETDATE()
FROM dbo.EmployeeMedicalInsurance mi
WHERE mi.Notes LIKE N'[[]DEMO]%' 
  AND NOT EXISTS (SELECT 1 FROM dbo.MedicalInsuranceInstallments i WHERE i.EmployeeInsuranceId = mi.Id AND i.Notes = N'[DEMO] ظ‚ط³ط· ط´ظ‡ط± ظ‚ط§ط¯ظ… ظ„ظ„ظ…طھط§ط¨ط¹ط©.');

SELECT
    Providers = (SELECT COUNT(1) FROM dbo.MedicalInsuranceProviders WHERE ProviderId BETWEEN 9101 AND 9103),
    Plans = (SELECT COUNT(1) FROM dbo.MedicalInsurancePlans WHERE PlanId BETWEEN 9201 AND 9203),
    DemoSubscriptions = (SELECT COUNT(1) FROM dbo.EmployeeMedicalInsurance WHERE Notes LIKE N'[[]DEMO]%' ),
    DemoDependents = (SELECT COUNT(1) FROM dbo.MedicalInsuranceDependents d INNER JOIN dbo.EmployeeMedicalInsurance mi ON mi.Id = d.EmployeeInsuranceId WHERE mi.Notes LIKE N'[[]DEMO]%' ),
    DemoInstallments = (SELECT COUNT(1) FROM dbo.MedicalInsuranceInstallments i INNER JOIN dbo.EmployeeMedicalInsurance mi ON mi.Id = i.EmployeeInsuranceId WHERE mi.Notes LIKE N'[[]DEMO]%' );

