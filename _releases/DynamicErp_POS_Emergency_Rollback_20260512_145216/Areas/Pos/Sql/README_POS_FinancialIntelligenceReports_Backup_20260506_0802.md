# POS Financial Intelligence Reports

SQL location: `F:\Source Code\DynamicErp\Areas\Pos\Sql\45_POS_FinancialIntelligenceReports.sql`

Do not place these POS procedures in `F:\Source Code\SatriahMain\Main Script\AllScripts.sql`.

## Stored Procedures

- `dbo.usp_POS_FI_AccountingHealthDashboard`
- `dbo.usp_POS_FI_EmployeeReceivableDiagnostics`
- `dbo.usp_POS_FI_CustodyDiagnostics`
- `dbo.usp_POS_FI_AbnormalJournalDetection`
- `dbo.usp_POS_FI_RootCauseAnalyzer`

## Account Mapping

Employee receivables and custody/advance reports accept optional parent account serial filters. When a parent serial is not supplied, the procedures use conservative account-name heuristics with `DOUBLE_ENTREY_VOUCHERS.NEmpid`.

Configure parent mappings before relying on the diagnostics as an audit control.
