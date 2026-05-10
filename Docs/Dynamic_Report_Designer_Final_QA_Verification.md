# Dynamic Report Designer - Final QA Verification After Phase 4B

Date: 2026-05-10

Scope: QA verification only after Phase 4B. No features, no Phase 5, no lookups, no PDF/XLSX, no refactor, and no SQL schema changes were performed.

## Environment

- App URL: `http://localhost:63735`
- Web DB: `MyErp`
- POS DB: `Cash`
- MainErp DB: `Eng`
- SQL Server: `Wael\Sql2019`
- Authentication: real authenticated dev sessions. Passwords are intentionally not recorded.

## Users Used

| Scope | Purpose | User |
| --- | --- | --- |
| Web | Admin QA | `ErpAdmin` |
| POS | Creator/Admin | `admin` / UserId 1 |
| POS | Reviewer | `Nasr` / UserId 18 |
| POS | Certifier | `Fatma Al.Zahraa` / UserId 94 |
| MainErp | Admin QA | `admin` |

## 1. Audit Log Verification

Executed:

```sql
SELECT COUNT(*), MAX(PerformedAt) FROM dbo.DynamicReportAuditLog;
```

| DB | Count | MAX(PerformedAt) | Result |
| --- | ---: | --- | --- |
| `MyErp` | 9 | 2026-05-10 08:45:07 | Pass |
| `Cash` | 9 | 2026-05-10 08:45:07 | Pass |
| `Eng` | 1 | 2026-05-10 08:22:31 | Pass |

Additional POS certification audit check:

| ReportId | ActionType | PerformedBy | Result |
| ---: | --- | ---: | --- |
| 4 | Active | 1 | Pass |
| 4 | MarkReviewed | 18 | Pass |
| 4 | ProductionReady | 18 | Pass |
| 4 | Certified | 94 | Pass |

Final POS report 4 state:

`LifecycleStatus=Active`, `CertificationLevel=Certified`, `CreatedBy=1`, `ReviewedBy=18`.

## 2. Legacy Regression

| Scope | URL / Action | Report | Expected | Actual | Result |
| --- | --- | --- | --- | --- | --- |
| Web | `/Reports/Viewer/Index` | legacy viewer page | Viewer opens | 500: layout did not render `scripts` section | Fail |
| Web | `/Reports/Viewer/Execute?scope=Web` | ReportId 1 `WEB_USERS_SAMPLE` | Execute succeeds | 200, `Success=true` | Pass |
| Web | `/Reports/Viewer/Print` | ReportId 1 | Print opens | 200 | Pass |
| POS | `/Pos/DynamicReports/Index` | legacy viewer page | Viewer opens | 200 | Pass |
| POS | `/Pos/DynamicReports/Execute?scope=POS` | ReportId 2 `POS_SALES_SAMPLE` | Execute succeeds | 200, max rows message | Pass |
| POS | `/Pos/DynamicReports/Print` | ReportId 2 | Print opens | 200 | Pass |
| MainErp | `/MainErp/DynamicReports/Index` | legacy viewer page | Viewer opens | 200 | Pass |
| MainErp | `/MainErp/DynamicReports/Execute?scope=MainErp` | ReportId 3 `MAINERP_JOURNAL_SAMPLE` | Execute succeeds | 200, max rows message | Pass |
| MainErp | `/MainErp/DynamicReports/Print` | ReportId 3 | Print opens | 200 | Pass |

Web Viewer page error detail:

`The following sections have been defined but have not been rendered for the layout page "~/Views/Shared/_Layout.cshtml": "scripts".`

This affects the Web viewer page shell only. The Web execution and print endpoints still work.

## 3. Certified Flow

Scope used: POS, because the dev POS DB has three distinct active admin users.

Flow:

1. Creator `admin` (UserId 1) activated imported report 4.
2. Reviewer `Nasr` (UserId 18) marked report 4 as Reviewed.
3. Reviewer `Nasr` moved report 4 to ProductionReady.
4. Certifier `Fatma Al.Zahraa` (UserId 94) marked report 4 as Certified.

| Step | URL / Action | Expected | Actual | Result |
| --- | --- | --- | --- | --- |
| Activate | `/Pos/DynamicReportsAdmin/TransitionStatus?id=4&scope=POS&toStatus=Active` | Active | 200, Active | Pass |
| Mark Reviewed | `/Pos/DynamicReportsAdmin/MarkReviewed?id=4&scope=POS` | Reviewed by user2 | 200, Reviewed | Pass |
| ProductionReady | `/Pos/DynamicReportsAdmin/MarkProductionReady?id=4&scope=POS` | ProductionReady after clean validation | 200, ProductionReady | Pass |
| Certified | `/Pos/DynamicReportsAdmin/MarkCertified?id=4&scope=POS` | Certified by user3 | 200, Certified | Pass |

## 4. T1-T10 Short Verification

| ID | Test | URL / Action | Result | Notes |
| --- | --- | --- | --- | --- |
| T1 | Admin Web opens | `/Reports/Admin/Index` | Pass | Authenticated 200 |
| T2 | Admin POS opens | `/Pos/DynamicReportsAdmin/Index` | Pass | Authenticated 200 |
| T3 | Admin MainErp opens | `/MainErp/DynamicReportsAdmin/Index` | Pass | Authenticated 200 |
| T4 | Catalog import Approved | `/Reports/Admin/CatalogImport?catalogId=142&scope=Web` | Pass | Imported draft ReportId 6 with Review link |
| T5 | Pending import rejected | `/Reports/Admin/CatalogImport?catalogId=14&scope=Web` | Pass | 400 with message: `لا يمكن الاستيراد إلا بعد الاعتماد اليدوي.` |
| T6 | Imported row shows Open Review | `/Reports/Admin/CatalogImport?catalogId=153&scope=Web` | Pass | `AlreadyImported=true`, Review URL returned |
| T7 | Review opens | `/Reports/Admin/Review?id=5&scope=Web` | Pass | Authenticated 200 |
| T8 | Validation runs | `/Reports/Admin/ValidateReport?id=5&scope=Web` | Pass | 200, no errors; row-count warning only |
| T9 | Activation works | `/Reports/Admin/TransitionStatus?id=5&scope=Web&toStatus=Active` | Pass | 200, Active |
| T10 | Viewer + Print works | `/Reports/Viewer/Print` for ReportId 5 with `techId=0` | Pass | 200 print preview |

## Bugs Remaining

### High

1. Web Viewer shell returns 500.
   - URL: `/Reports/Viewer/Index`
   - Expected: Viewer page opens.
   - Actual: 500.
   - Error: layout `~/Views/Shared/_Layout.cshtml` does not render the `scripts` section defined by the viewer page.
   - Impact: Web viewer page is blocked, although Web `Execute` and `Print` endpoints still work.
   - Recommendation: hotfix the Web viewer Razor section/layout wiring.

### Medium

1. Web legacy report 1 print title still shows legacy mojibake text from old seed data.
   - URL: `/Reports/Viewer/Print`, ReportId 1.
   - This is existing data content, not a Phase 4B rendering failure.

### Low

1. Some HTTP 400 responses are captured by PowerShell without body unless `ErrorDetails.Message` is read. The actual Pending import response body is present and clear.

## Recommendation

Accept with hotfixes.

Reason: Phase 4B audit logging, suggestions persistence, certification flow, POS/MainErp viewer regression, imported row handling, and Review/Validation/Activation all passed. However, Web Viewer page shell has a real 500 due Razor layout section wiring and should be fixed before final acceptance.
