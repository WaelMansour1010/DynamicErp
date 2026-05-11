# DynamicErp SQL Migration Audit

Audit date: 2026-05-09  
Scope: `F:\Source Code\DynamicErp`  
Excluded: `bin`, `obj`, `.git`, `.vs`, `.claude`, CodeMirror SQL mode assets, and `F:\Source Code\SatriahMain\Main Script\AllScripts.sql`.

## Executive Summary

The repository contains 152 SQL files after excluding generated and tool asset folders. The active source candidates are in `Areas/*/Sql`, `Scripts`, and a few root SQL files. The `Releases/*/Sql` and `Areas/Sync/Releases/*/Sql` folders contain packaged copies and should not become the canonical migration source.

Key risks:

- Duplicate numbers exist in POS: `39` and `45` are used by more than one active script.
- Backup scripts are mixed with deployable scripts, for example `30_POS_SaveTransaction_UnicodeText_Backup_20260507_1918.sql` and `45_POS_FinancialIntelligenceReports_Backup_*`.
- Server tuning scripts live beside database migrations and should be separated because they require server-level permissions and are not database migrations.
- `Scripts` contains many overlapping hotfix/final/all-in-one files from January 2026; these need human review before conversion.
- Existing scripts have no shared migration history, so repeated or out-of-order execution is possible.

## Recommended Canonical Structure

Use:

```text
Database/Migrations/
  POS/
  MainErp/
  Shared/
  Reports/
  Sync/
```

Keep these as historical sources until converted:

- `Areas/Pos/Sql`
- `Areas/MainErp/Sql`
- `Areas/Reports/Sql`
- `Areas/Sync/Sql`
- `Scripts`
- root SQL files

Treat these as release artifacts:

- `Releases/*/Sql`
- `Areas/Sync/Releases/*/Server/Sql`

## Current Active SQL Inventory

| File | Module | Purpose inferred | Rerunnable? | DROP/CREATE SP | ALTER TABLE | Data changes | Ordering notes |
|---|---|---|---:|---:|---:|---:|---|
| `Areas/MainErp/Sql/00_MainErp_Module_Setup.sql` | MainErp | Module setup | Review | No | No | No | MainErp first |
| `Areas/MainErp/Sql/01_LC_ReadModel.sql` | MainErp | LC read model | Review | No | No | No | After setup |
| `Areas/MainErp/Sql/02_ProjectExtracts_ReadModel.sql` | MainErp | Project extracts read model | Review | No | No | No | After setup |
| `Areas/MainErp/Sql/03_SalesInvoice_ReadWrite_Procedures.sql` | MainErp | Sales invoice procedures | Yes if DROP+CREATE only | Yes | Yes | Yes | Before dependent app release |
| `Areas/MainErp/Sql/04_MainErp_PaymentCashing_ReadProcedures.sql` | MainErp | Payment cashing read procedures | Yes if DROP+CREATE only | Yes | No | No | After shared objects |
| `Areas/MainErp/Sql/05_MainErp_AuditLog.sql` | MainErp | Audit log schema | Yes if guarded | No | Yes | No | Before audited code paths |
| `Areas/MainErp/Sql/06_EmployeePayroll_MedicalInsurance.sql` | MainErp | Payroll medical insurance | Review | Yes | Yes | Yes | Before POS equivalent wrapper |
| `Areas/Pos/Sql/25_POS_TemporaryPermissions.sql` | POS | Temporary permissions | Review | No | No | Yes | Early POS permissions |
| `Areas/Pos/Sql/26_POS_TellerPermission_ECUsers.sql` | POS | Teller permissions | Review | No | No | Yes | After permissions tables |
| `Areas/Pos/Sql/27_POS_ReportStoredProcedures.sql` | POS | POS report procedures | Yes if DROP+CREATE only | Yes | No | No | Before report UI |
| `Areas/Pos/Sql/28_POS_Payments_Audit.sql` | POS | Payment audit schema | Yes if guarded | No | Yes | No | Before payment audit code |
| `Areas/Pos/Sql/29_POS_UserCategory_BulkPermissions.sql` | POS | Bulk permission changes | Review | No | Yes | Yes | After permissions schema |
| `Areas/Pos/Sql/30_POS_SaveTransaction_UnicodeText.sql` | POS | Save transaction unicode fix | Yes if DROP+CREATE only | Yes | No | No | Supersedes backup copy |
| `Areas/Pos/Sql/30_POS_SaveTransaction_UnicodeText_Backup_20260507_1918.sql` | POS | Backup copy | No | Yes | No | No | Do not migrate |
| `Areas/Pos/Sql/31_POS_GetNextID_FromSequence_Concurrency.sql` | POS | Sequence concurrency procedure | Yes if DROP+CREATE only | Yes | No | No | Before transaction allocator |
| `Areas/Pos/Sql/32_POS_WebInvoiceAuditReport.sql` | POS | Web invoice audit report | Yes if DROP+CREATE only | Yes | No | No | After audit columns |
| `Areas/Pos/Sql/33_POS_JournalEntryAuditColumns.sql` | POS | Journal entry audit columns | Yes if guarded | No | Yes | No | Before audit reports |
| `Areas/Pos/Sql/34_POS_PerformanceStoredProcedures.sql` | POS | Performance procedures | Yes if DROP+CREATE only | Yes | No | No | Before performance deployment package |
| `Areas/Pos/Sql/35_POS_SystemHealthPermissions.sql` | POS | System health permissions | Review | No | No | Yes | After user/security objects |
| `Areas/Pos/Sql/36_POS_PerformanceIndexRollback.sql` | POS | Rollback experimental indexes | No | No | No | No | Manual rollback only |
| `Areas/Pos/Sql/37_POS_SystemErrorLog.sql` | POS | System error log setup | Review | No | No | Yes | Before error log UI |
| `Areas/Pos/Sql/38_POS_SystemHealthEncodingHotfix.sql` | POS | Encoding hotfix procedure | Yes if DROP+CREATE only | Yes | No | No | After system health setup |
| `Areas/Pos/Sql/39_POS_Deadlock_Diagnostics.sql` | POS | Deadlock diagnostics | Review | No | No | No | Duplicate number with report |
| `Areas/Pos/Sql/39_POS_NonWebLoginUsersReport.sql` | POS | Non-web login users report | Yes if DROP+CREATE only | Yes | No | No | Duplicate number with diagnostics |
| `Areas/Pos/Sql/40_POS_Dashboard_DailySnapshots.sql` | POS | Dashboard snapshots/procedures | Review | Yes | No | Yes | After core POS tables |
| `Areas/Pos/Sql/41_POS_PurchaseInvoice.sql` | POS | Purchase invoice procedures | Yes if DROP+CREATE only | Yes | No | No | Supersedes backup copy |
| `Areas/Pos/Sql/41_POS_PurchaseInvoice_Backup_20260505_1519.sql` | POS | Backup copy | No | Yes | No | No | Do not migrate |
| `Areas/Pos/Sql/42_POS_StockTransfer.sql` | POS | Stock transfer procedures | Yes if DROP+CREATE only | Yes | No | No | After item/warehouse objects |
| `Areas/Pos/Sql/45_POS_ExcelImport.sql` | POS | Excel import support | Review | No | No | Yes | Duplicate number with FI reports |
| `Areas/Pos/Sql/45_POS_FinancialIntelligenceReports.sql` | POS | Financial intelligence reports | Yes if DROP+CREATE only | Yes | No | No | Duplicate number with Excel import |
| `Areas/Pos/Sql/45_POS_FinancialIntelligenceReports_Backup_20260506_0753.sql` | POS | Backup copy | No | Yes | No | No | Do not migrate |
| `Areas/Pos/Sql/45_POS_FinancialIntelligenceReports_Backup_20260506_0802.sql` | POS | Backup copy | No | Yes | No | No | Do not migrate |
| `Areas/Pos/Sql/46_POS_SaveTransaction_ConcurrencyIndexes.sql` | POS | Concurrency indexes | Yes if guarded | No | No | No | After save transaction procedure |
| `Areas/Pos/Sql/47_POS_SaveAttemptLog.sql` | POS | Save attempt log | Review | No | No | Yes | Before allocator hardening |
| `Areas/Pos/Sql/48_POS_SalesRepresentativesPerformanceDashboard.sql` | POS | Sales rep performance dashboard | Yes if DROP+CREATE only | Yes | No | No | After sales targets if referenced |
| `Areas/Pos/Sql/49_POS_SalesRepresentativeTargets.sql` | POS | Sales representative targets | Review | Yes | No | Yes | Before performance dashboard |
| `Areas/Pos/Sql/50_POS_ExcelImportCommitAudit.sql` | POS | Excel import commit audit | Yes if guarded | No | Yes | No | After Excel import |
| `Areas/Pos/Sql/51_POS_PaymentCashing_ReadProcedures.sql` | POS | Payment cashing read procedures | Yes if DROP+CREATE only | Yes | No | No | Align with MainErp script 04 |
| `Areas/Pos/Sql/52_POS_EmployeePayroll_MedicalInsurance.sql` | POS | Payroll medical insurance bridge | Review | No | No | No | After MainErp 06 |
| `Areas/Pos/Sql/53_POS_DoubleEntryVoucherSequence_Diagnostics.sql` | POS | Voucher sequence diagnostics | Review | No | No | No | Before fix 54 |
| `Areas/Pos/Sql/54_POS_DoubleEntryVoucherSequence_Fix.sql` | POS | Voucher sequence fix | Review | No | No | Yes | After diagnostics 53 |
| `Areas/Pos/Sql/55_POS_SaveTransaction_Allocator_Hardening.sql` | POS | Save transaction allocator hardening | Yes if DROP+CREATE only | Yes | No | No | After 31 and 47 |
| `Areas/Pos/Sql/56_POS_RechargeValue_Validation.sql` | POS | Recharge validation | Review | No | No | Yes | After save transaction procedure |
| `Areas/Reports/Sql/01_DynamicReports_Schema.sql` | Reports | Dynamic reports schema | Yes if guarded | No | No | Yes | Reports first |
| `Areas/Reports/Sql/02_DynamicReports_StoredProcedures.sql` | Reports | Dynamic report procedures | Yes if DROP+CREATE only | Yes | No | No | After schema |
| `Areas/Reports/Sql/03_DynamicReports_SeedViews.sql` | Reports | Seed report views | Review | No | No | Yes | After procedures |
| `Areas/Reports/Sql/04_DynamicReports_LegacyPosMainErp_SeedViews.sql` | Reports | Legacy POS/MainErp seeded views | Review | No | No | Yes | After POS/MainErp report objects |
| `Areas/Sync/Sql/001_Sync_AdminAudit_Draft.sql` | Sync | Sync admin audit draft | Review | No | No | Yes | Sync first candidate |
| `Areas/Sync/Sql/002_Sync_AdminOperations.sql` | Sync | Sync admin operations | Review | No | No | Yes | After audit/schema |
| `Areas/Sync/Sql/003_Sync_BranchIngestion.sql` | Sync | Branch ingestion | Review | No | No | Yes | After operations |
| `Areas/Sync/Sql/004_Sync_BranchAgentHardening.sql` | Sync | Branch agent hardening | Yes if guarded | No | Yes | Yes | After ingestion |
| `Create_Cayshny_Pos_SQL_User.sql` | Shared | Create POS SQL user | No | No | No | No | Server/security task, not migration |
| `Create_Cayshny_Pos_SQL_User_Byte.sql` | Shared | Create Byte POS SQL user | No | No | No | No | Server/security task, not migration |
| `PropertyContract_Update_Fixed.sql` | Shared | Property contract procedure fix | Yes if DROP+CREATE only | Yes | No | No | Property module sequence needed |
| `SQL_Add_PaymentType_To_PurchaseOrder.sql` | Shared | Purchase order payment type | Review | Yes | Yes | Yes | Before related UI/code |
| `SQL_Add_Vehicle_Fields_Invoices.sql` | Shared | Vehicle fields in invoices | Yes if guarded | No | Yes | No | Before invoice UI/code |
| `Scripts/Add_CashTransferTypes.sql` | Shared | Cash transfer seed/types | Review | No | No | Yes | Before cash transfer procedures |
| `Scripts/ALL_IN_ONE_DEPLOYMENT.sql` | Shared | Bundled deployment | No | Yes | Yes | Yes | Do not migrate as-is |
| `Scripts/CashReceiptVoucher_AddColumns.sql` | Shared | Cash receipt columns | Yes if guarded | No | Yes | No | Before cash receipt procedures |
| `Scripts/CashReceiptVoucher_Insert_LATEST.sql` | Shared | Cash receipt insert procedure | Yes if DROP+CREATE only | Yes | No | No | After columns |
| `Scripts/CashReceiptVoucher_Update_LATEST.sql` | Shared | Cash receipt update procedure | Yes if DROP+CREATE only | Yes | No | No | After columns |
| `Scripts/CashTransfer_AddAccountOption.sql` | Shared | Cash transfer account option | Review | No | Yes | Yes | Before cash transfer procedures |
| `Scripts/CashTransfer_Complete_Enhancement.sql` | Shared | Cash transfer full enhancement | No | Yes | Yes | Yes | Split before migration |
| `Scripts/CashTransfer_Insert_Updated.sql` | Shared | Cash transfer insert procedure | Yes if DROP+CREATE only | Yes | No | No | After account option |
| `Scripts/CashTransfer_Update_Updated.sql` | Shared | Cash transfer update procedure | Yes if DROP+CREATE only | Yes | No | No | After account option |
| `Scripts/FIXED_CashIssueVoucher_Insert.sql` | Shared | Cash issue insert procedure | Yes if DROP+CREATE only | Yes | No | No | After document number safe proc |
| `Scripts/FIXED_Property_StoredProcedures.sql` | Shared | Property stored procedures | Yes if DROP+CREATE only | Yes | No | No | After property schema |
| `Scripts/FIXED_PropertyDueBatch_Insert.sql` | Shared | Property due batch insert procedure | Yes if DROP+CREATE only | Yes | No | No | After document number safe proc |
| `Scripts/FIXED_Remaining_StoredProcedures.sql` | Shared | Remaining fixed procedures | No | Yes | No | No | Split by procedure/module |
| `Scripts/GetNextDocumentNumber_Function.sql` | Shared | Document number function | Yes if DROP+CREATE only | Yes | No | No | Before safe procedure |
| `Scripts/GetNextDocumentNumberSafe_Procedure.sql` | Shared | Safe document number procedure | Yes if DROP+CREATE only | Yes | No | No | Before dependent voucher procs |
| `Scripts/HOTFIX_ALL_PROCEDURES.sql` | Shared | Hotfix bundle | No | Yes | No | No | Do not migrate as-is |
| `Scripts/HOTFIX_All_StoredProcedures.sql` | Shared | Hotfix bundle | No | Yes | No | No | Do not migrate as-is |
| `Scripts/HOTFIX_CashIssue_FINAL.sql` | Shared | Cash issue final hotfix | Review | Yes | No | No | Compare with fixed version |
| `Scripts/HOTFIX_CashReceiptVoucher_FINAL.sql` | Shared | Cash receipt final hotfix | Review | Yes | No | No | Compare with latest versions |
| `Scripts/HOTFIX_FINAL_ALL.sql` | Shared | Final all-in-one hotfix | No | Yes | No | No | Do not migrate as-is |
| `Scripts/HOTFIX_Property_Procedures_FINAL.sql` | Shared | Property procedures final hotfix | Review | Yes | No | No | Compare with fixed property scripts |
| `Scripts/HOTFIX_PropertyDueBatch_FINAL.sql` | Shared | Property due batch final hotfix | Review | Yes | No | No | Compare with fixed property due script |
| `Scripts/PropertyContract_UnitAvailability_Filter_Fix.sql` | Shared | Unit availability filter fix | Review | Yes | No | No | After property procedures |
| `Scripts/PropertyRenter_AddColumns.sql` | Shared | Property renter columns | Yes if guarded | No | Yes | No | Before property renter UI/code |
| `Scripts/SalesInvoice_LastModified_Audit.sql` | Shared | Sales invoice audit column | Yes if guarded | No | Yes | No | Before audit usage |
| `Scripts/SP_PropertyContractTotal_Report.sql` | Shared | Property contract total report SP | Yes if DROP+CREATE only | Yes | No | No | After property schema |
| `Scripts/SP_PropertyContractTotal_Report_Tests.sql` | Shared | Test script | No | No | No | Yes | Test only, not migration |
| `Scripts/TEST_SCRIPTS.sql` | Shared | Test script bundle | No | No | No | Yes | Test only, not migration |
| `Tools/Release/Templates/00_BACKUP_BEFORE_APPLY.sql` | Shared | Backup template | No | No | No | No | Release pre-step, not migration |

## Duplicate and Overlapping Files

Release folders repeat many active POS files. Do not import both the source file and its packaged copy. Examples:

- `27_POS_ReportStoredProcedures.sql` appears in `Areas/Pos/Sql` and multiple `Releases/*/Sql`.
- `30_POS_SaveTransaction_UnicodeText.sql` appears in `Areas/Pos/Sql`, approved releases, deadlock fix release, and feature release.
- `31_POS_GetNextID_FromSequence_Concurrency.sql`, `39_POS_Deadlock_Diagnostics.sql`, `46_POS_SaveTransaction_ConcurrencyIndexes.sql`, and `47_POS_SaveAttemptLog.sql` appear in several release packages.
- Sync release packages duplicate `002_Sync_AdminOperations.sql`, `003_Sync_BranchIngestion.sql`, and `004_Sync_BranchAgentHardening.sql`.
- `Scripts` contains overlapping `HOTFIX_*`, `FIXED_*`, and `*_LATEST` files that must be compared before numbering.

## Files That Should Not Become Normal Migrations

- Backup copies with `_Backup_` in the name.
- Rollback scripts such as `36_POS_PerformanceIndexRollback.sql`.
- Server tuning scripts under `Areas/Pos/Sql/ServerTuningDeployment`.
- SQL user/security scripts at repository root.
- Backup templates under `Tools/Release/Templates`.
- Test-only scripts under `Scripts/*Tests.sql` and `Scripts/TEST_SCRIPTS.sql`.
- All-in-one hotfix bundles unless split into atomic migrations.

## Proposed Renumbering Plan

Do not rename in place yet. Convert by copying reviewed scripts into `Database/Migrations/{Module}` with new stable numbers.

Suggested first sequence:

| New range | Module | Source |
|---|---|---|
| `0001-0099` | Shared | Root SQL and `Scripts` after de-duplication |
| `0100-0199` | MainErp | `Areas/MainErp/Sql` |
| `0200-0299` | POS core | `Areas/Pos/Sql/25-37` |
| `0300-0399` | POS reports/performance | `Areas/Pos/Sql/38-56` |
| `0400-0499` | Reports | `Areas/Reports/Sql` |
| `0500-0599` | Sync | `Areas/Sync/Sql` |
| `9000-9099` | Manual DBA tasks | Backup, server tuning, login/security scripts |

Resolve duplicate current numbers before conversion:

- Split current POS `39` into two new migrations, for example `0310_POS_DeadlockDiagnostics.sql` and `0311_POS_NonWebLoginUsersReport.sql`.
- Split current POS `45` into two new migrations, for example `0320_POS_ExcelImport.sql` and `0321_POS_FinancialIntelligenceReports.sql`.
- Exclude backup versions unless a backup file is confirmed as the only correct final version.

## Conversion Plan

1. Freeze old SQL folders as historical inputs.
2. Create a test database clone.
3. Pick one module at a time, starting with `Shared`, then `MainErp`, then `POS`.
4. Compare overlapping hotfix/fixed/latest scripts and keep only the final intended object definition.
5. Copy each reviewed script into `Database/Migrations/{Module}` with the required header.
6. Make table and data changes idempotent.
7. Keep stored procedures as SQL Server 2012 compatible `DROP` + `CREATE`.
8. Run `DryRun` and attach output to the release ticket.
9. Run `Apply` on test only.
10. Re-run `Apply` to verify skips.
11. After approval, run `DryRun` then `Apply` for production.

