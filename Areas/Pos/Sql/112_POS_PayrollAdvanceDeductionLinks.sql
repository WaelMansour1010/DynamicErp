SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.PayrollRunHeader', N'U') IS NULL
BEGIN
    RAISERROR('PayrollRunHeader is missing. Run 108_POS_PayrollRunSnapshot.sql before 112_POS_PayrollAdvanceDeductionLinks.sql.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.PayrollRunAdvanceDeductions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PayrollRunAdvanceDeductions
    (
        PayrollRunAdvanceDeductionId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PayrollRunAdvanceDeductions PRIMARY KEY,
        PayrollRunId int NOT NULL,
        EmployeeId int NOT NULL,
        SalaryRowId int NULL,
        AdvanceId int NOT NULL,
        AdvanceDetailTableId int NOT NULL,
        PartNo int NULL,
        PartDate datetime NULL,
        PartValue money NOT NULL CONSTRAINT DF_PayrollRunAdvanceDeductions_PartValue DEFAULT(0),
        IsPosted bit NOT NULL CONSTRAINT DF_PayrollRunAdvanceDeductions_IsPosted DEFAULT(0),
        PostedAt datetime NULL,
        PostedBy int NULL,
        NoteId int NULL,
        CreatedAt datetime NOT NULL CONSTRAINT DF_PayrollRunAdvanceDeductions_CreatedAt DEFAULT(GETDATE()),
        CreatedBy int NULL,
        CONSTRAINT FK_PayrollRunAdvanceDeductions_Header FOREIGN KEY (PayrollRunId) REFERENCES dbo.PayrollRunHeader(PayrollRunId)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PayrollRunAdvanceDeductions_RunDetail' AND object_id = OBJECT_ID(N'dbo.PayrollRunAdvanceDeductions'))
    CREATE UNIQUE INDEX UX_PayrollRunAdvanceDeductions_RunDetail ON dbo.PayrollRunAdvanceDeductions(PayrollRunId, AdvanceDetailTableId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PayrollRunAdvanceDeductions_Employee' AND object_id = OBJECT_ID(N'dbo.PayrollRunAdvanceDeductions'))
    CREATE INDEX IX_PayrollRunAdvanceDeductions_Employee ON dbo.PayrollRunAdvanceDeductions(EmployeeId, PayrollRunId) INCLUDE (PartValue, IsPosted, AdvanceDetailTableId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PayrollRunAdvanceDeductions_Detail' AND object_id = OBJECT_ID(N'dbo.PayrollRunAdvanceDeductions'))
    CREATE INDEX IX_PayrollRunAdvanceDeductions_Detail ON dbo.PayrollRunAdvanceDeductions(AdvanceDetailTableId) INCLUDE (PayrollRunId, IsPosted, NoteId);
