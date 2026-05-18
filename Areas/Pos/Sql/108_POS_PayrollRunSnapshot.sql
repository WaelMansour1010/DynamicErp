SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.PayrollRunHeader', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PayrollRunHeader
    (
        PayrollRunId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PayrollRunHeader PRIMARY KEY,
        RunName nvarchar(200) NULL,
        PeriodYear int NOT NULL,
        PeriodMonth int NOT NULL,
        PeriodFrom datetime NOT NULL,
        PeriodTo datetime NOT NULL,
        BranchId int NULL,
        DepartmentId int NULL,
        EmployeeScope nvarchar(50) NULL,
        SelectionMode nvarchar(50) NULL,
        AllowDuplicateEmployees bit NOT NULL CONSTRAINT DF_PayrollRunHeader_AllowDuplicateEmployees DEFAULT(0),
        ExcludeAlreadyIncluded bit NOT NULL CONSTRAINT DF_PayrollRunHeader_ExcludeAlreadyIncluded DEFAULT(1),
        RebuildCount int NOT NULL CONSTRAINT DF_PayrollRunHeader_RebuildCount DEFAULT(0),
        IsPosted bit NOT NULL CONSTRAINT DF_PayrollRunHeader_IsPosted DEFAULT(0),
        PostedAt datetime NULL,
        PostedBy int NULL,
        NoteId int NULL,
        NoteSerial int NULL,
        VoucherLinesCount int NOT NULL CONSTRAINT DF_PayrollRunHeader_VoucherLinesCount DEFAULT(0),
        TotalBasic money NOT NULL CONSTRAINT DF_PayrollRunHeader_TotalBasic DEFAULT(0),
        TotalAllowances money NOT NULL CONSTRAINT DF_PayrollRunHeader_TotalAllowances DEFAULT(0),
        TotalDeductions money NOT NULL CONSTRAINT DF_PayrollRunHeader_TotalDeductions DEFAULT(0),
        TotalAdvance money NOT NULL CONSTRAINT DF_PayrollRunHeader_TotalAdvance DEFAULT(0),
        TotalMedicalInsurance money NOT NULL CONSTRAINT DF_PayrollRunHeader_TotalMedicalInsurance DEFAULT(0),
        TotalNet money NOT NULL CONSTRAINT DF_PayrollRunHeader_TotalNet DEFAULT(0),
        CreatedAt datetime NOT NULL CONSTRAINT DF_PayrollRunHeader_CreatedAt DEFAULT(GETDATE()),
        CreatedBy int NULL,
        UpdatedAt datetime NULL,
        UpdatedBy int NULL,
        IsCancelled bit NOT NULL CONSTRAINT DF_PayrollRunHeader_IsCancelled DEFAULT(0),
        CancelledAt datetime NULL,
        CancelledBy int NULL,
        CancelReason nvarchar(500) NULL
    );
END;

IF OBJECT_ID(N'dbo.PayrollRunEmployees', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PayrollRunEmployees
    (
        PayrollRunEmployeeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PayrollRunEmployees PRIMARY KEY,
        PayrollRunId int NOT NULL,
        EmployeeId int NOT NULL,
        EmployeeCode varchar(50) NULL,
        EmployeeName nvarchar(250) NULL,
        BranchId int NULL,
        BranchName nvarchar(250) NULL,
        DepartmentId int NULL,
        DepartmentName nvarchar(250) NULL,
        ProjectId int NULL,
        BasicSalary money NOT NULL CONSTRAINT DF_PayrollRunEmployees_BasicSalary DEFAULT(0),
        Allowances money NOT NULL CONSTRAINT DF_PayrollRunEmployees_Allowances DEFAULT(0),
        VariableAdditions money NOT NULL CONSTRAINT DF_PayrollRunEmployees_VariableAdditions DEFAULT(0),
        Deductions money NOT NULL CONSTRAINT DF_PayrollRunEmployees_Deductions DEFAULT(0),
        Advances money NOT NULL CONSTRAINT DF_PayrollRunEmployees_Advances DEFAULT(0),
        MedicalInsurance money NOT NULL CONSTRAINT DF_PayrollRunEmployees_MedicalInsurance DEFAULT(0),
        MedicalInsuranceCompanyCost money NOT NULL CONSTRAINT DF_PayrollRunEmployees_MedicalInsuranceCompanyCost DEFAULT(0),
        TotalBeforeDeductions money NOT NULL CONSTRAINT DF_PayrollRunEmployees_TotalBeforeDeductions DEFAULT(0),
        TotalDeductions money NOT NULL CONSTRAINT DF_PayrollRunEmployees_TotalDeductions DEFAULT(0),
        NetSalary money NOT NULL CONSTRAINT DF_PayrollRunEmployees_NetSalary DEFAULT(0),
        EmployeeStatusAtRunTime nvarchar(100) NULL,
        ExistingSalaryRowId int NULL,
        IsPosted bit NOT NULL CONSTRAINT DF_PayrollRunEmployees_IsPosted DEFAULT(0),
        VoucherId int NULL,
        NoteSerial int NULL,
        AccountCode nvarchar(50) NULL,
        AccruedSalaryAccountCode nvarchar(50) NULL,
        AdvancePaymentAccountCode nvarchar(50) NULL,
        MedicalInsuranceEmployeeAccountCode nvarchar(50) NULL,
        MedicalInsuranceCompanyAccountCode nvarchar(50) NULL,
        CreatedAt datetime NOT NULL CONSTRAINT DF_PayrollRunEmployees_CreatedAt DEFAULT(GETDATE()),
        CreatedBy int NULL,
        UpdatedAt datetime NULL,
        UpdatedBy int NULL,
        CONSTRAINT FK_PayrollRunEmployees_Header FOREIGN KEY (PayrollRunId) REFERENCES dbo.PayrollRunHeader(PayrollRunId)
    );
END;

IF OBJECT_ID(N'dbo.PayrollRunJournalLinks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PayrollRunJournalLinks
    (
        PayrollRunJournalLinkId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PayrollRunJournalLinks PRIMARY KEY,
        PayrollRunId int NOT NULL,
        NoteId int NOT NULL,
        NoteSerial int NULL,
        VoucherLinesCount int NOT NULL CONSTRAINT DF_PayrollRunJournalLinks_VoucherLinesCount DEFAULT(0),
        DebitTotal money NOT NULL CONSTRAINT DF_PayrollRunJournalLinks_DebitTotal DEFAULT(0),
        CreditTotal money NOT NULL CONSTRAINT DF_PayrollRunJournalLinks_CreditTotal DEFAULT(0),
        CreatedAt datetime NOT NULL CONSTRAINT DF_PayrollRunJournalLinks_CreatedAt DEFAULT(GETDATE()),
        CreatedBy int NULL,
        CONSTRAINT FK_PayrollRunJournalLinks_Header FOREIGN KEY (PayrollRunId) REFERENCES dbo.PayrollRunHeader(PayrollRunId)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PayrollRunHeader_Period' AND object_id = OBJECT_ID(N'dbo.PayrollRunHeader'))
    CREATE INDEX IX_PayrollRunHeader_Period ON dbo.PayrollRunHeader(PeriodYear, PeriodMonth, BranchId, DepartmentId, IsCancelled);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PayrollRunEmployees_PeriodEmployee' AND object_id = OBJECT_ID(N'dbo.PayrollRunEmployees'))
    CREATE INDEX IX_PayrollRunEmployees_PeriodEmployee ON dbo.PayrollRunEmployees(EmployeeId, PayrollRunId) INCLUDE (NetSalary, IsPosted);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PayrollRunJournalLinks_Run' AND object_id = OBJECT_ID(N'dbo.PayrollRunJournalLinks'))
    CREATE INDEX IX_PayrollRunJournalLinks_Run ON dbo.PayrollRunJournalLinks(PayrollRunId, NoteId);
