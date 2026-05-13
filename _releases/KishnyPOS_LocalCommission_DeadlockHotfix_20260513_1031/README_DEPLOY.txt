Kishny POS Hotfix - Local Commission + Deadlock Indexes

Deploy contents:
1. bin\MyERP.dll
2. Areas\Pos\Scripts\pos-transaction.js
3. Areas\Pos\Sql\62_POS_SalesInvoice_LimitedEditPermissions.sql
4. Areas\Pos\Sql\63_POS_MigrateViolationWalletNumber.sql
5. Areas\Pos\Sql\64_POS_MigrateCashOutWalletNumber.sql
6. Areas\Pos\Sql\66_POS_SaveTransaction_DeadlockIndexes.sql
7. Areas\Pos\Sql\POS_SQL_AutoUpdate_Manifest.json

Notes:
- No extra DLL files are included; only the main MyERP.dll.
- Apply SQL scripts through the POS SQL updater/manifest, or run the listed SQL scripts in order if deploying manually.
- The commission preview for cash-in/cash-out now uses rules loaded in CommissionBootstrap and calculates locally in the browser. Save still performs server-side recalculation/validation.
