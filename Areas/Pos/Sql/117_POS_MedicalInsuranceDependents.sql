/* POS Medical Insurance employee dependents
   SQL Server 2012 compatible.
   Creates the production table used by the employee insurance subscription screen.
*/

IF OBJECT_ID(N'dbo.MedicalInsuranceDependents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MedicalInsuranceDependents
    (
        DependentId INT IDENTITY(1,1) NOT NULL,
        EmpId INT NOT NULL,
        EmployeeInsuranceId INT NULL,
        DependentName NVARCHAR(200) NOT NULL,
        Relation NVARCHAR(30) NOT NULL,
        BirthDate DATETIME NULL,
        CoveragePercent MONEY NOT NULL CONSTRAINT DF_MedicalInsuranceDependents_CoveragePercent DEFAULT (100),
        IsActive BIT NOT NULL CONSTRAINT DF_MedicalInsuranceDependents_IsActive DEFAULT (1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_MedicalInsuranceDependents_CreatedAt DEFAULT (GETDATE()),
        CONSTRAINT PK_MedicalInsuranceDependents PRIMARY KEY CLUSTERED (DependentId ASC)
    );
END;

IF COL_LENGTH(N'dbo.MedicalInsuranceDependents', N'EmployeeInsuranceId') IS NULL
BEGIN
    ALTER TABLE dbo.MedicalInsuranceDependents ADD EmployeeInsuranceId INT NULL;
END;

IF COL_LENGTH(N'dbo.MedicalInsuranceDependents', N'CoveragePercent') IS NULL
BEGIN
    ALTER TABLE dbo.MedicalInsuranceDependents ADD CoveragePercent MONEY NOT NULL CONSTRAINT DF_MedicalInsuranceDependents_CoveragePercent_Added DEFAULT (100);
END;

IF COL_LENGTH(N'dbo.MedicalInsuranceDependents', N'IsActive') IS NULL
BEGIN
    ALTER TABLE dbo.MedicalInsuranceDependents ADD IsActive BIT NOT NULL CONSTRAINT DF_MedicalInsuranceDependents_IsActive_Added DEFAULT (1);
END;

IF COL_LENGTH(N'dbo.MedicalInsuranceDependents', N'CreatedAt') IS NULL
BEGIN
    ALTER TABLE dbo.MedicalInsuranceDependents ADD CreatedAt DATETIME NOT NULL CONSTRAINT DF_MedicalInsuranceDependents_CreatedAt_Added DEFAULT (GETDATE());
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.MedicalInsuranceDependents')
      AND name = N'IX_MedicalInsuranceDependents_Emp_Insurance_Active'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_MedicalInsuranceDependents_Emp_Insurance_Active
    ON dbo.MedicalInsuranceDependents (EmpId, EmployeeInsuranceId, IsActive)
    INCLUDE (Relation, DependentName, BirthDate, CoveragePercent);
END;
