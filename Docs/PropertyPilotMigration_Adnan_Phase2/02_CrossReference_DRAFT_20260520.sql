/*
02_CrossReference_DRAFT_20260520.sql
Purpose: Create Sandbox-only migration metadata and cross-reference tables.
Status: DRAFT SANDBOX ONLY. Do not run on production.
SQL Server 2012 compatible.
*/

IF DB_NAME() = N'Alromaizan' OR (DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%Sandbox%')
BEGIN
    RAISERROR('Blocked: Cross Reference setup can run only inside a PropertyPilot/Sandbox database, never Alromaizan.', 16, 1);
    RETURN;
END;
GO

IF OBJECT_ID('dbo.PropertyPilotMigrationBatch', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyPilotMigrationBatch
    (
        MigrationBatchId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PropertyPilotMigrationBatch PRIMARY KEY,
        BatchName NVARCHAR(200) NOT NULL,
        SourceDatabaseName NVARCHAR(128) NOT NULL,
        TargetDatabaseName NVARCHAR(128) NOT NULL,
        CutoverDate DATETIME NOT NULL,
        Strategy NVARCHAR(100) NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_PropertyPilotMigrationBatch_CreatedAt DEFAULT (GETDATE()),
        CreatedBy NVARCHAR(128) NULL,
        Notes NVARCHAR(MAX) NULL
    );
END;
GO

IF OBJECT_ID('dbo.PropertyPilotCrossReference', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyPilotCrossReference
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PropertyPilotCrossReference PRIMARY KEY,
        MigrationBatchId UNIQUEIDENTIFIER NOT NULL,
        OldDatabaseName NVARCHAR(128) NOT NULL,
        OldTableName NVARCHAR(128) NOT NULL,
        OldId NVARCHAR(100) NOT NULL,
        NewTableName NVARCHAR(128) NOT NULL,
        NewId INT NULL,
        EntityType NVARCHAR(100) NOT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_PropertyPilotCrossReference_CreatedAt DEFAULT (GETDATE()),
        Notes NVARCHAR(MAX) NULL,
        CONSTRAINT FK_PropertyPilotCrossReference_Batch FOREIGN KEY (MigrationBatchId)
            REFERENCES dbo.PropertyPilotMigrationBatch(MigrationBatchId)
    );

    CREATE UNIQUE INDEX UX_PropertyPilotCrossReference_SourceEntity
    ON dbo.PropertyPilotCrossReference(MigrationBatchId, OldDatabaseName, OldTableName, OldId, NewTableName, EntityType);
END;
GO

IF OBJECT_ID('dbo.PropertyPilotAccountMapping', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyPilotAccountMapping
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PropertyPilotAccountMapping PRIMARY KEY,
        MigrationBatchId UNIQUEIDENTIFIER NOT NULL,
        OldDatabaseName NVARCHAR(128) NOT NULL,
        OldAccountCode NVARCHAR(100) NOT NULL,
        OldAccountId INT NULL,
        OldAccountName NVARCHAR(300) NULL,
        NewChartOfAccountId INT NULL,
        NewAccountCode NVARCHAR(100) NULL,
        MappingMode NVARCHAR(50) NOT NULL, -- Seed, ManualMap, Exclude
        IsApproved BIT NOT NULL CONSTRAINT DF_PropertyPilotAccountMapping_IsApproved DEFAULT (0),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_PropertyPilotAccountMapping_CreatedAt DEFAULT (GETDATE()),
        Notes NVARCHAR(MAX) NULL,
        CONSTRAINT FK_PropertyPilotAccountMapping_Batch FOREIGN KEY (MigrationBatchId)
            REFERENCES dbo.PropertyPilotMigrationBatch(MigrationBatchId)
    );

    CREATE UNIQUE INDEX UX_PropertyPilotAccountMapping_Code
    ON dbo.PropertyPilotAccountMapping(MigrationBatchId, OldDatabaseName, OldAccountCode);
END;
GO

IF OBJECT_ID('dbo.PropertyPilotValidationIssue', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyPilotValidationIssue
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PropertyPilotValidationIssue PRIMARY KEY,
        MigrationBatchId UNIQUEIDENTIFIER NOT NULL,
        Severity NVARCHAR(30) NOT NULL,
        IssueType NVARCHAR(100) NOT NULL,
        EntityType NVARCHAR(100) NULL,
        OldDatabaseName NVARCHAR(128) NULL,
        OldTableName NVARCHAR(128) NULL,
        OldId NVARCHAR(100) NULL,
        Message NVARCHAR(MAX) NOT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_PropertyPilotValidationIssue_CreatedAt DEFAULT (GETDATE()),
        IsResolved BIT NOT NULL CONSTRAINT DF_PropertyPilotValidationIssue_IsResolved DEFAULT (0),
        ResolutionNotes NVARCHAR(MAX) NULL,
        CONSTRAINT FK_PropertyPilotValidationIssue_Batch FOREIGN KEY (MigrationBatchId)
            REFERENCES dbo.PropertyPilotMigrationBatch(MigrationBatchId)
    );
END;
GO

IF OBJECT_ID('dbo.PropertyPilotOpeningBalanceStaging', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PropertyPilotOpeningBalanceStaging
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PropertyPilotOpeningBalanceStaging PRIMARY KEY,
        MigrationBatchId UNIQUEIDENTIFIER NOT NULL,
        OldDatabaseName NVARCHAR(128) NOT NULL,
        OldContractNo INT NOT NULL,
        OldRenterId INT NULL,
        OldAccountCode NVARCHAR(100) NULL,
        DueInstallmentTotal MONEY NOT NULL DEFAULT (0),
        TruePaid MONEY NOT NULL DEFAULT (0),
        OpeningBalanceAmount MONEY NOT NULL DEFAULT (0),
        CutoverDate DATETIME NOT NULL,
        NewPropertyContractId INT NULL,
        NewPropertyRenterId INT NULL,
        NewAccountId INT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_PropertyPilotOpeningBalanceStaging_CreatedAt DEFAULT (GETDATE()),
        Notes NVARCHAR(MAX) NULL,
        CONSTRAINT FK_PropertyPilotOpeningBalanceStaging_Batch FOREIGN KEY (MigrationBatchId)
            REFERENCES dbo.PropertyPilotMigrationBatch(MigrationBatchId)
    );

    CREATE UNIQUE INDEX UX_PropertyPilotOpeningBalanceStaging_Contract
    ON dbo.PropertyPilotOpeningBalanceStaging(MigrationBatchId, OldDatabaseName, OldContractNo);
END;
GO
