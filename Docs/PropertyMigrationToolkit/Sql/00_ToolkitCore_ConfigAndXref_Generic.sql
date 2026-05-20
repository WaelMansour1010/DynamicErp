/*
PropertyMigrationToolkit - Enterprise Core Config, Modes, Exception, AutoFix, Review Queue Framework
SQL Server 2012 compatible. Template only.
Run only on a clone/sandbox/ReadyToTest database.
*/
SET NOCOUNT ON;

IF DB_NAME() IN (N'Alromaizan', N'MyErp', N'Adnan', N'RSMDB')
BEGIN RAISERROR('Blocked: source/reference/production database name.',16,1); RETURN; END;

IF DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%ReadyToTest%' AND DB_NAME() NOT LIKE N'%PilotClone%' AND DB_NAME() NOT LIKE N'%Sandbox%' AND DB_NAME() NOT LIKE N'%Migration%'
BEGIN RAISERROR('Blocked: migration must run on clone/sandbox/ReadyToTest database.',16,1); RETURN; END;

IF OBJECT_ID(N'dbo.PropertyMigrationConfig', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationConfig(
    ConfigId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CustomerCode nvarchar(50) NOT NULL,
    SourceDatabaseName sysname NOT NULL,
    TargetCloneDatabaseName sysname NOT NULL,
    ReferenceDatabaseName sysname NULL,
    CutoffDate date NOT NULL,
    MigrationMode nvarchar(30) NOT NULL DEFAULT(N'Hybrid'), -- Strict, Tolerant, Hybrid
    DryRun bit NOT NULL DEFAULT(1),
    ActiveContractRule nvarchar(4000) NULL,
    IncludeHistoricalReceipts bit NOT NULL DEFAULT(0),
    IncludeHistoricalIssues bit NOT NULL DEFAULT(0),
    IncludeJournalEntries bit NOT NULL DEFAULT(0),
    IncludeAdvancePayments bit NOT NULL DEFAULT(1),
    IncludeTerminations bit NOT NULL DEFAULT(0),
    ExcludeBrokenContracts bit NOT NULL DEFAULT(1),
    AllowUnknownUnits bit NOT NULL DEFAULT(0),
    AllowUnknownProperties bit NOT NULL DEFAULT(0),
    AllowUnknownRenters bit NOT NULL DEFAULT(0),
    AllowSuspenseAccounts bit NOT NULL DEFAULT(0),
    AllowFallbackPaymentMethods bit NOT NULL DEFAULT(0),
    AllowDefaultCashBox bit NOT NULL DEFAULT(0),
    AllowDefaultBank bit NOT NULL DEFAULT(0),
    AllowTemporaryRenterAccounts bit NOT NULL DEFAULT(0),
    ExcludeUnsafeOwnerPayments bit NOT NULL DEFAULT(1),
    AutoCreateMissingAccounts bit NOT NULL DEFAULT(0),
    AutoCreateMissingLookups bit NOT NULL DEFAULT(0),
    BranchMappingMode nvarchar(50) NOT NULL DEFAULT(N'PilotSingleBranch'),
    CashBoxMappingMode nvarchar(50) NOT NULL DEFAULT(N'PilotCashBox'),
    BankMappingMode nvarchar(50) NOT NULL DEFAULT(N'PilotBankAccount'),
    AccountMappingMode nvarchar(50) NOT NULL DEFAULT(N'SeedMissing'),
    DefaultPilotBranchId int NULL,
    DefaultDepartmentId int NULL,
    DefaultCashBoxId int NULL,
    DefaultBankAccountId int NULL,
    CashReceiptPaymentMethodId int NULL,
    BankReceiptPaymentMethodId int NULL,
    CashIssuePaymentMethodId int NULL,
    BankIssuePaymentMethodId int NULL,
    UnknownPropertyId int NULL,
    UnknownUnitId int NULL,
    UnknownRenterId int NULL,
    UnknownPropertyTypeId int NULL,
    UnknownUnitTypeId int NULL,
    SuspenseAccountId int NULL,
    HoldingAccountId int NULL,
    TempRenterAccountId int NULL,
    PaymentMethodStrategy nvarchar(100) NOT NULL DEFAULT(N'HybridResolver'),
    OwnerPaymentStrategy nvarchar(100) NOT NULL DEFAULT(N'ManualReview'),
    OpeningBalanceStrategy nvarchar(100) NOT NULL DEFAULT(N'ComputedAsOfCutoff'),
    ArchiveHistoryMode nvarchar(100) NOT NULL DEFAULT(N'ArchiveOnlyForUnsafeHistory'),
    IsApproved bit NOT NULL DEFAULT(0),
    CreatedAt datetime NOT NULL DEFAULT(GETDATE()),
    Notes nvarchar(max) NULL
);

IF OBJECT_ID(N'dbo.PropertyMigrationBatch', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationBatch(
    MigrationBatchId uniqueidentifier NOT NULL PRIMARY KEY,
    CustomerCode nvarchar(50) NOT NULL,
    ConfigId int NULL,
    SourceDatabaseName sysname NOT NULL,
    TargetDatabaseName sysname NOT NULL,
    CutoffDate date NOT NULL,
    MigrationMode nvarchar(30) NOT NULL,
    Stage nvarchar(50) NULL,
    Status nvarchar(50) NOT NULL,
    StartedAt datetime NOT NULL DEFAULT(GETDATE()),
    CompletedAt datetime NULL,
    CreatedBy nvarchar(128) NULL,
    WarningsCount int NOT NULL DEFAULT(0),
    ErrorsCount int NOT NULL DEFAULT(0),
    AutoFixCount int NOT NULL DEFAULT(0),
    ExcludedCount int NOT NULL DEFAULT(0),
    ReconciliationStatus nvarchar(50) NULL,
    ReadyToTestStatus nvarchar(50) NULL,
    Notes nvarchar(max) NULL
);

IF OBJECT_ID(N'dbo.PropertyMigrationEntityMap', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationEntityMap(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    CustomerCode nvarchar(50) NOT NULL,
    SourceDatabaseName sysname NOT NULL,
    SourceTableName nvarchar(128) NOT NULL,
    SourceId nvarchar(200) NOT NULL,
    TargetTableName nvarchar(128) NOT NULL,
    TargetId int NULL,
    EntityType nvarchar(100) NOT NULL,
    Status nvarchar(50) NOT NULL DEFAULT(N'Mapped'),
    UsedFallback bit NOT NULL DEFAULT(0),
    FallbackEntity nvarchar(100) NULL,
    RequiresManualReview bit NOT NULL DEFAULT(0),
    CreatedAt datetime NOT NULL DEFAULT(GETDATE()),
    Notes nvarchar(max) NULL
);

IF OBJECT_ID(N'dbo.PropertyMigrationRunLog', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationRunLog(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NULL,
    CustomerCode nvarchar(50) NULL,
    SourceDatabaseName sysname NULL,
    TargetDatabaseName sysname NULL,
    Stage nvarchar(50) NOT NULL,
    StepName nvarchar(200) NOT NULL,
    Status nvarchar(50) NOT NULL,
    StartedAt datetime NULL,
    EndedAt datetime NULL,
    WarningsCount int NULL,
    ErrorsCount int NULL,
    AutoFixCount int NULL,
    ExcludedCount int NULL,
    Message nvarchar(max) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE())
);

IF OBJECT_ID(N'dbo.PropertyMigrationWarning', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationWarning(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NULL,
    CustomerCode nvarchar(50) NULL,
    Severity nvarchar(30) NOT NULL DEFAULT(N'Warning'),
    IssueType nvarchar(100) NOT NULL,
    EntityType nvarchar(100) NULL,
    SourceDatabaseName sysname NULL,
    SourceTableName nvarchar(128) NULL,
    SourceId nvarchar(200) NULL,
    OriginalValue nvarchar(max) NULL,
    AppliedFix nvarchar(max) NULL,
    FallbackEntity nvarchar(100) NULL,
    RequiresManualReview bit NOT NULL DEFAULT(0),
    SuggestedAction nvarchar(max) NULL,
    Message nvarchar(max) NOT NULL,
    IsResolved bit NOT NULL DEFAULT(0),
    ResolutionNotes nvarchar(max) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE())
);

IF OBJECT_ID(N'dbo.PropertyMigrationError', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationError(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NULL,
    CustomerCode nvarchar(50) NULL,
    Severity nvarchar(30) NOT NULL DEFAULT(N'Critical'),
    Stage nvarchar(50) NULL,
    StepName nvarchar(200) NULL,
    IssueType nvarchar(100) NULL,
    EntityType nvarchar(100) NULL,
    SourceTableName nvarchar(128) NULL,
    SourceId nvarchar(200) NULL,
    OriginalValue nvarchar(max) NULL,
    ErrorMessage nvarchar(max) NOT NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE())
);

IF OBJECT_ID(N'dbo.PropertyMigrationAutoFix', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationAutoFix(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    CustomerCode nvarchar(50) NOT NULL,
    SourceDatabaseName sysname NOT NULL,
    SourceTableName nvarchar(128) NOT NULL,
    SourceId nvarchar(200) NOT NULL,
    EntityType nvarchar(100) NOT NULL,
    IssueType nvarchar(100) NOT NULL,
    OriginalValue nvarchar(max) NULL,
    AppliedFix nvarchar(max) NOT NULL,
    FallbackEntity nvarchar(100) NULL,
    RequiresManualReview bit NOT NULL DEFAULT(1),
    SuggestedAction nvarchar(max) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE())
);

IF OBJECT_ID(N'dbo.PropertyMigrationFallback', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationFallback(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CustomerCode nvarchar(50) NOT NULL,
    FallbackCode nvarchar(100) NOT NULL,
    EntityType nvarchar(100) NOT NULL,
    TargetTableName nvarchar(128) NOT NULL,
    TargetId int NULL,
    IsActive bit NOT NULL DEFAULT(1),
    RequiresManualReview bit NOT NULL DEFAULT(1),
    Notes nvarchar(max) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE())
);

IF OBJECT_ID(N'dbo.PropertyMigrationReviewQueue', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationReviewQueue(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    CustomerCode nvarchar(50) NOT NULL,
    Priority int NOT NULL DEFAULT(3),
    Severity nvarchar(30) NOT NULL,
    IssueType nvarchar(100) NOT NULL,
    EntityType nvarchar(100) NULL,
    SourceDatabaseName sysname NULL,
    SourceTableName nvarchar(128) NULL,
    SourceId nvarchar(200) NULL,
    TargetTableName nvarchar(128) NULL,
    TargetId int NULL,
    OriginalValue nvarchar(max) NULL,
    AppliedFix nvarchar(max) NULL,
    SuggestedAction nvarchar(max) NULL,
    AssignedTo nvarchar(128) NULL,
    Status nvarchar(50) NOT NULL DEFAULT(N'Open'),
    ResolutionNotes nvarchar(max) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE()),
    ReviewedAt datetime NULL,
    ReviewedBy nvarchar(128) NULL
);

IF OBJECT_ID(N'dbo.PropertyMigrationSuspenseMapping', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationSuspenseMapping(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    CustomerCode nvarchar(50) NOT NULL,
    SourceDatabaseName sysname NOT NULL,
    SourceAccountCode nvarchar(100) NULL,
    SourceAccountId nvarchar(100) NULL,
    SourceAccountName nvarchar(500) NULL,
    SuspenseAccountId int NOT NULL,
    Amount decimal(28,6) NULL,
    Direction nvarchar(20) NULL,
    Reason nvarchar(max) NOT NULL,
    RequiresManualReview bit NOT NULL DEFAULT(1),
    Status nvarchar(50) NOT NULL DEFAULT(N'Open'),
    CreatedAt datetime NOT NULL DEFAULT(GETDATE()),
    ResolvedAt datetime NULL,
    ResolutionNotes nvarchar(max) NULL
);

IF OBJECT_ID(N'dbo.PropertyMigrationExcludedRecord', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationExcludedRecord(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    CustomerCode nvarchar(50) NOT NULL,
    EntityType nvarchar(100) NOT NULL,
    SourceDatabaseName sysname NOT NULL,
    SourceTableName nvarchar(128) NOT NULL,
    SourceId nvarchar(200) NOT NULL,
    Severity nvarchar(30) NOT NULL DEFAULT(N'Warning'),
    ExclusionReason nvarchar(max) NOT NULL,
    Decision nvarchar(100) NOT NULL DEFAULT(N'Exclude'),
    SuggestedAction nvarchar(max) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE())
);

IF OBJECT_ID(N'dbo.PropertyMigrationReconciliationResult', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationReconciliationResult(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    CustomerCode nvarchar(50) NOT NULL,
    MetricName nvarchar(200) NOT NULL,
    SourceValue decimal(28,6) NULL,
    TargetValue decimal(28,6) NULL,
    Difference decimal(28,6) NULL,
    Status nvarchar(50) NOT NULL,
    BlocksGoLive bit NOT NULL DEFAULT(0),
    Notes nvarchar(max) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE())
);
