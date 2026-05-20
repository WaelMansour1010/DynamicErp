/*
Draft only: creates finance approval table on clone/staging database.
Do not run on production. Does not apply mappings.
*/
IF DB_NAME() NOT LIKE '%Clone%' AND DB_NAME() NOT LIKE '%Sandbox%' AND DB_NAME() NOT LIKE '%PropertyPilot%' AND DB_NAME() NOT LIKE '%ReadyToTest%' AND DB_NAME() NOT LIKE '%PilotClone%' AND DB_NAME() NOT LIKE '%Migration%'
BEGIN
    RAISERROR('Unsafe database. Approval table is clone/staging only.',16,1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.PropertyMigrationAccountFinanceApproval', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyMigrationAccountFinanceApproval(
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        BatchId uniqueidentifier NOT NULL,
        CustomerCode nvarchar(100) NOT NULL,
        SourceAccountCode nvarchar(200) NOT NULL,
        SourceAccountName nvarchar(max) NULL,
        SuggestedTargetAccountSerial nvarchar(200) NULL,
        ApprovedTargetAccountSerial nvarchar(200) NULL,
        Decision nvarchar(60) NOT NULL,
        ApprovedBy nvarchar(200) NULL,
        ApprovedAt datetime NULL,
        Notes nvarchar(max) NULL,
        Status nvarchar(100) NOT NULL DEFAULT(N'Draft'),
        CreatedAt datetime NOT NULL DEFAULT(GETDATE()),
        CONSTRAINT CK_PropertyMigrationAccountFinanceApproval_Decision
            CHECK (Decision IN (N'Approved',N'Changed',N'SuspenseApproved',N'Blocked',N'NeedsMoreInfo'))
    );
END;
