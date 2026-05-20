/*
PropertyMigrationToolkit - Generic Source Staging Contract
SQL Server 2012 compatible. Run only on clone/sandbox/ReadyToTest database.
These tables are populated by customer-specific mapping SELECT scripts, then consumed by generic migration templates.
*/
SET NOCOUNT ON;

IF DB_NAME() IN (N'Alromaizan', N'MyErp', N'Adnan', N'RSMDB') BEGIN RAISERROR('Blocked database.',16,1); RETURN; END;
IF DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%ReadyToTest%' AND DB_NAME() NOT LIKE N'%PilotClone%' AND DB_NAME() NOT LIKE N'%Sandbox%' AND DB_NAME() NOT LIKE N'%Migration%'
BEGIN RAISERROR('Blocked: migration staging must run on clone/sandbox/ReadyToTest database.',16,1); RETURN; END;

IF OBJECT_ID(N'dbo.PropertyMigrationSourceProperty', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationSourceProperty(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    CustomerCode nvarchar(50) NOT NULL,
    SourceDatabaseName sysname NOT NULL,
    SourceTableName nvarchar(128) NOT NULL,
    SourceId nvarchar(200) NOT NULL,
    SourceCode nvarchar(200) NULL,
    ArName nvarchar(max) NULL,
    EnName nvarchar(max) NULL,
    PropertyTypeId int NULL,
    DepartmentId int NULL,
    Notes nvarchar(max) NULL,
    IsValid bit NOT NULL DEFAULT(1),
    ValidationMessage nvarchar(max) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE())
);

IF OBJECT_ID(N'dbo.PropertyMigrationSourceUnit', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationSourceUnit(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    CustomerCode nvarchar(50) NOT NULL,
    SourceDatabaseName sysname NOT NULL,
    SourceTableName nvarchar(128) NOT NULL,
    SourceId nvarchar(200) NOT NULL,
    SourcePropertyId nvarchar(200) NULL,
    SourceCode nvarchar(200) NULL,
    ArName nvarchar(max) NULL,
    EnName nvarchar(max) NULL,
    PropertyUnitTypeId int NULL,
    PropertyUnitStatusId int NULL,
    Notes nvarchar(max) NULL,
    IsValid bit NOT NULL DEFAULT(1),
    ValidationMessage nvarchar(max) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE())
);

IF OBJECT_ID(N'dbo.PropertyMigrationSourceRenter', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationSourceRenter(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    CustomerCode nvarchar(50) NOT NULL,
    SourceDatabaseName sysname NOT NULL,
    SourceTableName nvarchar(128) NOT NULL,
    SourceId nvarchar(200) NOT NULL,
    SourceCode nvarchar(200) NULL,
    ArName nvarchar(max) NULL,
    EnName nvarchar(max) NULL,
    AccountId int NULL,
    AccountCode nvarchar(100) NULL,
    Mobile nvarchar(100) NULL,
    Phone nvarchar(100) NULL,
    NationalNo nvarchar(100) NULL,
    VATNo nvarchar(100) NULL,
    DepartmentId int NULL,
    OpeningDebitBalance decimal(18,4) NULL,
    OpeningCreditBalance decimal(18,4) NULL,
    Notes nvarchar(max) NULL,
    IsValid bit NOT NULL DEFAULT(1),
    ValidationMessage nvarchar(max) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE())
);

IF OBJECT_ID(N'dbo.PropertyMigrationSourceOwner', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationSourceOwner(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    CustomerCode nvarchar(50) NOT NULL,
    SourceDatabaseName sysname NOT NULL,
    SourceTableName nvarchar(128) NOT NULL,
    SourceId nvarchar(200) NOT NULL,
    SourceCode nvarchar(200) NULL,
    ArName nvarchar(max) NULL,
    EnName nvarchar(max) NULL,
    AccountId int NULL,
    AccountCode nvarchar(100) NULL,
    Mobile nvarchar(100) NULL,
    Phone nvarchar(100) NULL,
    VATNo nvarchar(100) NULL,
    BankAccountNo nvarchar(200) NULL,
    BankName nvarchar(200) NULL,
    DepartmentId int NULL,
    Notes nvarchar(max) NULL,
    IsValid bit NOT NULL DEFAULT(1),
    ValidationMessage nvarchar(max) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE())
);

IF OBJECT_ID(N'dbo.PropertyMigrationSourcePropertyOwner', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationSourcePropertyOwner(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    CustomerCode nvarchar(50) NOT NULL,
    SourceDatabaseName sysname NOT NULL,
    SourceTableName nvarchar(128) NOT NULL,
    SourceId nvarchar(200) NOT NULL,
    SourcePropertyId nvarchar(200) NOT NULL,
    SourceOwnerId nvarchar(200) NOT NULL,
    OwnershipPercentage decimal(18,6) NULL,
    IsPrimaryOwner bit NOT NULL DEFAULT(1),
    Notes nvarchar(max) NULL,
    IsValid bit NOT NULL DEFAULT(1),
    ValidationMessage nvarchar(max) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE())
);

IF OBJECT_ID(N'dbo.PropertyMigrationSourceOwnerBalance', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationSourceOwnerBalance(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    CustomerCode nvarchar(50) NOT NULL,
    SourceDatabaseName sysname NOT NULL,
    SourceOwnerId nvarchar(200) NOT NULL,
    SourcePropertyId nvarchar(200) NULL,
    AccountId int NULL,
    AccountCode nvarchar(100) NULL,
    BalanceAmount money NOT NULL,
    BalanceDirection nvarchar(20) NULL,
    CutoffDate date NOT NULL,
    Notes nvarchar(max) NULL,
    IsValid bit NOT NULL DEFAULT(1),
    ValidationMessage nvarchar(max) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE())
);

IF OBJECT_ID(N'dbo.PropertyMigrationSourceOwnerPayment', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationSourceOwnerPayment(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    CustomerCode nvarchar(50) NOT NULL,
    SourceDatabaseName sysname NOT NULL,
    SourceTableName nvarchar(128) NOT NULL,
    SourceId nvarchar(200) NOT NULL,
    SourceOwnerId nvarchar(200) NULL,
    SourcePropertyId nvarchar(200) NULL,
    SourceContractId nvarchar(200) NULL,
    DocumentNumber nvarchar(200) NULL,
    PaymentDate datetime NOT NULL,
    MoneyAmount money NOT NULL,
    DebitAccountId int NULL,
    CashBoxId int NULL,
    BankAccountId int NULL,
    PaymentMethodId int NULL,
    BranchId int NULL,
    DepartmentId int NULL,
    LinkedNoteId nvarchar(200) NULL,
    RequiresManualReview bit NOT NULL DEFAULT(1),
    Notes nvarchar(max) NULL,
    IsValid bit NOT NULL DEFAULT(1),
    ValidationMessage nvarchar(max) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE())
);

IF OBJECT_ID(N'dbo.PropertyMigrationSourceContract', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationSourceContract(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    CustomerCode nvarchar(50) NOT NULL,
    SourceDatabaseName sysname NOT NULL,
    SourceTableName nvarchar(128) NOT NULL,
    SourceId nvarchar(200) NOT NULL,
    DocumentNumber nvarchar(200) NULL,
    VoucherDate datetime NOT NULL,
    SourcePropertyId nvarchar(200) NULL,
    SourceUnitId nvarchar(200) NULL,
    SourceRenterId nvarchar(200) NULL,
    ContractStartDate datetime NULL,
    ContractEndDate datetime NULL,
    RentValue money NULL,
    NetTotal money NULL,
    TotalAfterTaxes money NULL,
    VATPercentage float NULL,
    VATValue money NULL,
    PropertyUnitTypeId int NULL,
    DepartmentId int NULL,
    NumberOfBatches int NULL,
    FirstBatchDate datetime NULL,
    PeriodBetweenBatchesNum int NULL,
    PeriodBetweenBatchesTypeId int NULL,
    Notes nvarchar(max) NULL,
    IsActiveContract bit NOT NULL DEFAULT(1),
    IsValid bit NOT NULL DEFAULT(1),
    ValidationMessage nvarchar(max) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE())
);

IF OBJECT_ID(N'dbo.PropertyMigrationSourceInstallment', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationSourceInstallment(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    CustomerCode nvarchar(50) NOT NULL,
    SourceDatabaseName sysname NOT NULL,
    SourceTableName nvarchar(128) NOT NULL,
    SourceId nvarchar(200) NOT NULL,
    SourceContractId nvarchar(200) NOT NULL,
    BatchNo int NULL,
    BatchDate datetime NULL,
    BatchRentValue money NULL,
    BatchRentValueTaxes money NULL,
    BatchWaterValue money NULL,
    BatchWaterValueTaxes money NULL,
    BatchElectricityValue money NULL,
    BatchElectricityValueTaxes money NULL,
    BatchCommissionValue money NULL,
    BatchCommissionValueTaxes money NULL,
    BatchGasValue money NULL,
    BatchGasValueTaxes money NULL,
    BatchServicesValue money NULL,
    BatchServicesValueTaxes money NULL,
    BatchInsuranceValue money NULL,
    BatchInsuranceValueTaxes money NULL,
    BatchTotal money NULL,
    IsFuture bit NOT NULL DEFAULT(1),
    Notes nvarchar(max) NULL,
    IsValid bit NOT NULL DEFAULT(1),
    ValidationMessage nvarchar(max) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE())
);

IF OBJECT_ID(N'dbo.PropertyMigrationSourceOpeningBalance', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationSourceOpeningBalance(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    CustomerCode nvarchar(50) NOT NULL,
    SourceDatabaseName sysname NOT NULL,
    SourceContractId nvarchar(200) NULL,
    SourceRenterId nvarchar(200) NULL,
    AccountId int NULL,
    AccountCode nvarchar(100) NULL,
    OpeningBalanceAmount money NOT NULL,
    CutoffDate date NOT NULL,
    Notes nvarchar(max) NULL,
    IsValid bit NOT NULL DEFAULT(1),
    ValidationMessage nvarchar(max) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE())
);

IF OBJECT_ID(N'dbo.PropertyMigrationSourceAdvancePayment', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationSourceAdvancePayment(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    CustomerCode nvarchar(50) NOT NULL,
    SourceDatabaseName sysname NOT NULL,
    SourceContractId nvarchar(200) NULL,
    SourceInstallmentId nvarchar(200) NULL,
    SourceRenterId nvarchar(200) NULL,
    AdvancePaidAmount money NOT NULL,
    FutureInstallmentValue money NULL,
    FutureRemainAfterAdvance money NULL,
    Notes nvarchar(max) NULL,
    IsValid bit NOT NULL DEFAULT(1),
    ValidationMessage nvarchar(max) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE())
);

IF OBJECT_ID(N'dbo.PropertyMigrationSourceReceipt', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationSourceReceipt(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    CustomerCode nvarchar(50) NOT NULL,
    SourceDatabaseName sysname NOT NULL,
    SourceTableName nvarchar(128) NOT NULL,
    SourceId nvarchar(200) NOT NULL,
    SourceContractId nvarchar(200) NULL,
    SourceInstallmentId nvarchar(200) NULL,
    SourceRenterId nvarchar(200) NULL,
    DocumentNumber nvarchar(200) NULL,
    ReceiptDate datetime NOT NULL,
    MoneyAmount money NOT NULL,
    AccountId int NULL,
    CashBoxId int NULL,
    BankAccountId int NULL,
    PaymentMethodId int NULL,
    BranchId int NULL,
    DepartmentId int NULL,
    Notes nvarchar(max) NULL,
    IsValid bit NOT NULL DEFAULT(1),
    ValidationMessage nvarchar(max) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE())
);

IF OBJECT_ID(N'dbo.PropertyMigrationSourceIssue', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationSourceIssue(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    CustomerCode nvarchar(50) NOT NULL,
    SourceDatabaseName sysname NOT NULL,
    SourceTableName nvarchar(128) NOT NULL,
    SourceId nvarchar(200) NOT NULL,
    DocumentNumber nvarchar(200) NULL,
    IssueDate datetime NOT NULL,
    MoneyAmount money NOT NULL,
    DebitAccountId int NULL,
    CashBoxId int NULL,
    BankAccountId int NULL,
    PaymentMethodId int NULL,
    BranchId int NULL,
    DepartmentId int NULL,
    SourceTypeId int NULL,
    RequiresManualReview bit NOT NULL DEFAULT(1),
    Notes nvarchar(max) NULL,
    IsValid bit NOT NULL DEFAULT(1),
    ValidationMessage nvarchar(max) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE())
);

IF OBJECT_ID(N'dbo.PropertyMigrationSourceJournal', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationSourceJournal(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    CustomerCode nvarchar(50) NOT NULL,
    SourceDatabaseName sysname NOT NULL,
    SourceTableName nvarchar(128) NOT NULL,
    SourceId nvarchar(200) NOT NULL,
    LinkedReceiptSourceId nvarchar(200) NULL,
    LinkedIssueSourceId nvarchar(200) NULL,
    DocumentNumber nvarchar(200) NULL,
    JournalDate datetime NOT NULL,
    BranchId int NULL,
    DepartmentId int NULL,
    Notes nvarchar(max) NULL,
    IsValid bit NOT NULL DEFAULT(1),
    ValidationMessage nvarchar(max) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE())
);

IF OBJECT_ID(N'dbo.PropertyMigrationSourceJournalLine', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationSourceJournalLine(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    CustomerCode nvarchar(50) NOT NULL,
    SourceDatabaseName sysname NOT NULL,
    SourceJournalId nvarchar(200) NOT NULL,
    SourceLineId nvarchar(200) NOT NULL,
    AccountId int NOT NULL,
    Debit money NOT NULL DEFAULT(0),
    Credit money NOT NULL DEFAULT(0),
    DepartmentId int NULL,
    Notes nvarchar(max) NULL,
    IsValid bit NOT NULL DEFAULT(1),
    ValidationMessage nvarchar(max) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE())
);

IF OBJECT_ID(N'dbo.PropertyMigrationSourceTermination', N'U') IS NULL
CREATE TABLE dbo.PropertyMigrationSourceTermination(
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MigrationBatchId uniqueidentifier NOT NULL,
    CustomerCode nvarchar(50) NOT NULL,
    SourceDatabaseName sysname NOT NULL,
    SourceTableName nvarchar(128) NOT NULL,
    SourceId nvarchar(200) NOT NULL,
    SourceContractId nvarchar(200) NOT NULL,
    TerminationDate datetime NOT NULL,
    Amount money NULL,
    AccountId int NULL,
    Notes nvarchar(max) NULL,
    RequiresManualReview bit NOT NULL DEFAULT(1),
    IsValid bit NOT NULL DEFAULT(1),
    ValidationMessage nvarchar(max) NULL,
    CreatedAt datetime NOT NULL DEFAULT(GETDATE())
);
