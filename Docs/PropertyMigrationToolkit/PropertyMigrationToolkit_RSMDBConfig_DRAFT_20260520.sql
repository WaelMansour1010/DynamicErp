/* PropertyMigrationToolkit_RSMDBConfig_DRAFT_20260520.sql
Draft only. Updated after read-only discovery on RSMDB. Do not approve until mapping review is complete.
*/
SET NOCOUNT ON;
IF DB_NAME() IN (N'RSMDB',N'Alromaizan',N'MyErp',N'Adnan') BEGIN RAISERROR('Blocked database.',16,1); RETURN; END;
IF DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%ReadyToTest%' AND DB_NAME() NOT LIKE N'%PilotClone%' AND DB_NAME() NOT LIKE N'%Sandbox%' AND DB_NAME() NOT LIKE N'%Migration%' BEGIN RAISERROR('Clone/sandbox target required.',16,1); RETURN; END;
IF OBJECT_ID(N'dbo.PropertyMigrationConfig',N'U') IS NULL BEGIN RAISERROR('Run 00_ToolkitCore_ConfigAndXref_Generic.sql first.',16,1); RETURN; END;

IF NOT EXISTS (SELECT 1 FROM dbo.PropertyMigrationConfig WHERE CustomerCode=N'RSMDB' AND SourceDatabaseName=N'RSMDB' AND TargetCloneDatabaseName=DB_NAME())
INSERT INTO dbo.PropertyMigrationConfig(
    CustomerCode,SourceDatabaseName,TargetCloneDatabaseName,ReferenceDatabaseName,CutoffDate,MigrationMode,DryRun,ActiveContractRule,
    IncludeHistoricalReceipts,IncludeHistoricalIssues,IncludeJournalEntries,IncludeAdvancePayments,IncludeTerminations,ExcludeBrokenContracts,
    AllowUnknownUnits,AllowUnknownProperties,AllowUnknownRenters,AllowSuspenseAccounts,AllowFallbackPaymentMethods,AllowDefaultCashBox,AllowDefaultBank,AllowTemporaryRenterAccounts,ExcludeUnsafeOwnerPayments,AutoCreateMissingAccounts,AutoCreateMissingLookups,
    BranchMappingMode,CashBoxMappingMode,BankMappingMode,AccountMappingMode,
    DefaultPilotBranchId,DefaultDepartmentId,DefaultCashBoxId,DefaultBankAccountId,
    CashReceiptPaymentMethodId,BankReceiptPaymentMethodId,CashIssuePaymentMethodId,BankIssuePaymentMethodId,
    PaymentMethodStrategy,OwnerPaymentStrategy,OpeningBalanceStrategy,ArchiveHistoryMode,IsApproved,Notes)
VALUES(
    N'RSMDB',N'RSMDB',DB_NAME(),N'MyErp','2026-05-20',N'Hybrid',1,
    N'DRAFT: candidate active contracts from TblContract/TblContractInstallments; confirm status/end-date rules using TblRentStatus/TblRentType and VB6 forms before migration.',
    0,0,0,1,0,1,
    1,1,1,0,1,1,1,1,1,0,1,
    N'PilotSingleBranch',N'PilotCashBox',N'PilotBankAccount',N'ManualThenSeed',
    NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,
    N'HybridResolver',N'ManualReview',N'ComputedAsOfCutoff',N'ArchiveUnsafeHistory',0,
    N'DISCOVERY ONLY: likely property tables TblAqar/TblAqarDetai/TblUnites; contracts TblContract/TblContractInstallments; receipts/payments Notes NoteType 4/5; contract journals NoteType 60; VAT/installments NoteType 9088; terminations NoteType -1; GL DOUBLE_ENTREY_VOUCHERS. Requires mapping review before execute.'
);
