# POS المؤشرات المالية الذكية

SQL location: `F:\Source Code\DynamicErp\Areas\Pos\Sql\45_POS_FinancialIntelligenceReports.sql`

Do not place these POS procedures in `F:\Source Code\SatriahMain\Main Script\AllScripts.sql`.

## Stored Procedures

- `dbo.usp_POS_FI_CfoDashboard`
- `dbo.usp_POS_FI_BranchPerformance`
- `dbo.usp_POS_FI_CashFlowAnalysis`
- `dbo.usp_POS_FI_SalesCollectionsAnalysis`
- `dbo.usp_POS_FI_ExpenseAnalysis`
- `dbo.usp_POS_FI_InventoryProfitability`
- `dbo.usp_POS_FI_AccountingHealthDashboard`
- `dbo.usp_POS_FI_EmployeeReceivableDiagnostics`
- `dbo.usp_POS_FI_CustodyDiagnostics`
- `dbo.usp_POS_FI_AbnormalJournalDetection`
- `dbo.usp_POS_FI_RootCauseAnalyzer`
- `dbo.usp_POS_FI_JournalDetails`

## Notes

- All procedures are read-only reporting procedures.
- Stored procedures use SQL Server 2012 compatible `DROP` + `CREATE`.
- Arabic report titles and alert labels use Unicode `N''` literals.
- Drill-down uses `Double_Entry_Vouchers_ID`, `Notes_ID`, and `Transaction_ID` where available.
- Employee receivables and custody/advance reports accept optional parent account serial filters. When a parent serial is not supplied, the procedures use conservative account-name heuristics with `DOUBLE_ENTREY_VOUCHERS.NEmpid`.
