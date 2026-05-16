/*
Phase 15 - Protected Payroll Test Posting Mode
SQL Server 2012 compatible.

This script creates only the audit table used by the protected test-posting
experience. Test Notes and DOUBLE_ENTREY_VOUCHERS rows are marked in their
legacy text columns with:

    [TEST_PAYROLL_POSTING] Batch=<TestPostingBatchId>

Cleanup is always batch-scoped and marker-scoped.
*/

IF OBJECT_ID(N'dbo.MainErpPayrollTestPostingAudit', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MainErpPayrollTestPostingAudit
    (
        TestPostingBatchId uniqueidentifier NOT NULL CONSTRAINT PK_MainErpPayrollTestPostingAudit PRIMARY KEY,
        CreatedAt datetime NOT NULL CONSTRAINT DF_MainErpPayrollTestPostingAudit_CreatedAt DEFAULT(GETDATE()),
        CreatedBy int NULL,
        CreatedByName nvarchar(255) NULL,
        DatabaseName sysname NOT NULL,
        [Year] int NOT NULL,
        [Month] int NOT NULL,
        BranchId int NULL,
        DepartmentId int NULL,
        EmployeeId int NULL,
        NotesCount int NOT NULL CONSTRAINT DF_MainErpPayrollTestPostingAudit_NotesCount DEFAULT(0),
        VoucherLinesCount int NOT NULL CONSTRAINT DF_MainErpPayrollTestPostingAudit_VoucherLinesCount DEFAULT(0),
        DebitTotal decimal(18,2) NOT NULL CONSTRAINT DF_MainErpPayrollTestPostingAudit_Debit DEFAULT(0),
        CreditTotal decimal(18,2) NOT NULL CONSTRAINT DF_MainErpPayrollTestPostingAudit_Credit DEFAULT(0),
        Balance decimal(18,2) NOT NULL CONSTRAINT DF_MainErpPayrollTestPostingAudit_Balance DEFAULT(0),
        CleanupStatus nvarchar(50) NOT NULL CONSTRAINT DF_MainErpPayrollTestPostingAudit_CleanupStatus DEFAULT(N'Active'),
        CleanedAt datetime NULL,
        CleanedBy int NULL,
        CleanedNotesCount int NULL,
        CleanedVoucherLinesCount int NULL,
        Warning nvarchar(4000) NULL
    );
END
GO
