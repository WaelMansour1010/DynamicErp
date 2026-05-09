# Dynamic Report Designer Smoke Baseline

Date: 2026-05-09

Branch: `claude/improve-report-designer-iAq77`

Environment:

- Repo root: `F:\Source Code\DynamicErp`
- Runtime: IIS Express, `http://localhost:8123`
- Web DB connection: `MyERP_ConnectionString` -> `Wael\Sql2019 / MyErp`
- POS DB connection: `KishnyCashConnection` -> `Wael\Sql2019 / Cash`
- MainErp DB connection: `MainErp_ConnectionString` -> `Wael\Sql2019 / Eng`

SQL baseline:

| Database | Dynamic Reports tables | Action |
| --- | ---: | --- |
| MyErp | 5/5 | Already applied |
| Cash | 5/5 | Already applied |
| Eng | 5/5 | Already applied |

HTTP smoke:

| Step | URL | Result | Notes |
| --- | --- | --- | --- |
| Web admin | `/Reports/Admin/Index` | Pass | HTTP 302 to `/Login?ReturnUrl=%2fReports%2fAdmin%2fIndex`; expected without authenticated Web session |
| Web viewer | `/Reports/Viewer/Index` | Pass | HTTP 302 to `/Login?ReturnUrl=%2fReports%2fViewer%2fIndex`; expected without authenticated Web session |
| Web viewer list | `/Reports/Viewer/List?scope=Web` | Pass | HTTP 200, unauthenticated response body length 26 |
| POS viewer | `/Pos/DynamicReports/Index` | Pass | HTTP 302 to `/Pos/Login?returnUrl=%2FPos%2FDynamicReports%2FIndex`; correct POS login context |
| POS admin | `/Pos/DynamicReportsAdmin/Index` | Pass | HTTP 302 to `/Pos/Login?returnUrl=%2FPos%2FDynamicReportsAdmin%2FIndex`; correct POS login context |
| MainErp viewer | `/MainErp/DynamicReports/Index` | Pass | HTTP 302 to `/MainErp/Login?returnUrl=%2FMainErp%2FDynamicReports%2FIndex`; correct MainErp login context |
| MainErp admin | `/MainErp/DynamicReportsAdmin/Index` | Pass | HTTP 302 to `/MainErp/Login?returnUrl=%2FMainErp%2FDynamicReportsAdmin%2FIndex`; correct MainErp login context |

Manual authenticated smoke:

| Step | Result | Notes |
| --- | --- | --- |
| Run seed report | Blocked | No authenticated browser session was available in this execution context |
| Save layout `smoke_baseline` | Blocked | Requires authenticated user context |
| Reload layout `smoke_baseline` | Blocked | Requires authenticated user context |

Console errors:

- Not available. The smoke was performed through HTTP requests without an authenticated interactive browser session.

Server/HTTP errors:

- No 500 responses observed in the unauthenticated HTTP smoke.
- No 404 responses observed for the required routes.

Execute result snapshot:

- Blocked by missing authenticated session. SQL tables were present, but Phase 0 does not repair or bypass authentication.
