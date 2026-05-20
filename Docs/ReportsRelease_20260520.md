# Reports Release - 2026-05-20

Scope: reporting changes only.

## MainErp database updates

MainErp update screen reads from `Database/Migrations` via `Web.config` key `DatabaseMigrationFolders`.

Apply through MainErp Database Migration screen:

- `Database/Migrations/MainErp/0008_MainErp_SharedAccountingReports_CrystalParity.sql`

Creates/recreates:

- `dbo.usp_Shared_AccountingReports_Branches`
- `dbo.usp_Shared_AccountingReports_AccountTree`
- `dbo.usp_Shared_AccountingReports_Run`

Applied locally to:

- Database: `Eng`

Verification query:

```sql
SELECT name
FROM sys.procedures
WHERE name LIKE 'usp_Shared_AccountingReports%'
ORDER BY name;
```

## POS database updates

POS update screen reads `Areas/Pos/Sql/POS_SQL_AutoUpdate_Manifest.json`.

Already registered in manifest as autoApply:

- `Areas/Pos/Sql/121_POS_ProjectStatus_Command8_9_10_Reports.sql`
- `Areas/Pos/Sql/122_POS_ProjectStatus_Command8_9_10_CrystalParityWrapper.sql`
- `Areas/Pos/Sql/123_POS_FrmReports_XPChk_CrystalParity.sql`

Creates/recreates key report procedures including:

- `dbo.usp_POS_ProjectStatus_Report_Run`
- `dbo.usp_POS_Report_RunOperationalSales`
- `dbo.usp_POS_FrmReports_XPChk90_93_DailySales`
- `dbo.usp_POS_FrmReports_XPChk2_ItemsSalesDetails`

Applied locally to:

- Database: `Cash`

Verification query:

```sql
SELECT name
FROM sys.procedures
WHERE name IN (
    'usp_POS_ProjectStatus_Report_Run',
    'usp_POS_Report_RunOperationalSales',
    'usp_POS_FrmReports_XPChk90_93_DailySales',
    'usp_POS_FrmReports_XPChk2_ItemsSalesDetails'
)
ORDER BY name;
```

## Web/report UI files in scope

- `Areas/Pos/Views/AccountingReports/Index.cshtml`
- `Areas/Pos/Content/html-reports.css`

If releasing POS operational report UI fixes too, include:

- `Areas/Pos/Controllers/PosReportsController.cs`
- `Areas/Pos/Views/PosReports/Index.cshtml`

## Do not include unrelated dirty files

Current working tree contains unrelated changes outside report release scope. Do not stage or commit all files blindly.
