/* PropertyMigrationToolkit_AdnanConfig_20260520.sql
Template config for Adnan pilot case. Clone only. Do not run on source/production.
*/
SET NOCOUNT ON;
IF DB_NAME() IN (N'Adnan',N'Alromaizan',N'MyErp',N'RSMDB') BEGIN RAISERROR('Blocked database.',16,1); RETURN; END;
IF OBJECT_ID(N'dbo.PropertyMigrationConfig',N'U') IS NULL BEGIN RAISERROR('Run 00_ToolkitCore_ConfigAndXref_Generic.sql first.',16,1); RETURN; END;

IF NOT EXISTS (SELECT 1 FROM dbo.PropertyMigrationConfig WHERE CustomerCode=N'ADNAN' AND SourceDatabaseName=N'Adnan')
INSERT INTO dbo.PropertyMigrationConfig(
    CustomerCode,SourceDatabaseName,TargetCloneDatabaseName,ReferenceDatabaseName,CutoffDate,ActiveContractRule,
    IncludeHistoricalReceipts,IncludeHistoricalIssues,IncludeJournalEntries,IncludeAdvancePayments,IncludeTerminations,ExcludeBrokenContracts,
    BranchMappingMode,CashBoxMappingMode,BankMappingMode,AccountMappingMode,
    DefaultPilotBranchId,DefaultDepartmentId,DefaultCashBoxId,DefaultBankAccountId,
    CashReceiptPaymentMethodId,BankReceiptPaymentMethodId,CashIssuePaymentMethodId,BankIssuePaymentMethodId,
    PaymentMethodStrategy,OwnerPaymentStrategy,OpeningBalanceStrategy,ArchiveHistoryMode,IsApproved,Notes)
VALUES(
    N'ADNAN',N'Adnan',DB_NAME(),N'MyErp','2026-05-20',
    N'TblContract where EndContract=0, not in TblFiterWaiver, EndDate >= cutoff, and Iqar/UnitNo/CusID not null',
    1,0,1,1,0,1,
    N'PilotSingleBranch',N'PilotCashBox',N'PilotBankAccount',N'SeedMissing',
    1,44,1022,2024,5,6,5,6,
    N'HybridResolver',N'ManualReview',N'ComputedAsOfCutoff',N'ArchiveUnsafeHistory',1,
    N'Pilot-proven config. Historical receipts linked to migrated installments allowed; cash issues and owner payments excluded/manual review.'
);
