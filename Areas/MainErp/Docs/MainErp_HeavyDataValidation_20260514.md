# MainErp Heavy Data Validation - 2026-05-14

Database: `Eng`  
Goal: validate MainErp behavior under real table sizes and reduce heavy grid/API pressure.

## Data Volume Observed

| Table | Rows |
|---|---:|
| `TblItems` | 40,012 |
| `TblCustemers` | 2,688 |
| `Transactions` | 17,421 |
| `Transaction_Details` | 53,136 |
| `emp_salary` | 23,831 |
| `DOUBLE_ENTREY_VOUCHERS` | 567,289 |
| `notes` | 75,973 |
| `TblLC` | 191 |

Payroll has several large periods. The largest sampled period was `sgn = 20257` with 1,671 salary rows and net total 8,679,469.00.

## Heavy Data Findings

- Customers and Project Extracts are already server-paged and stayed near 0.5 seconds in route smoke.
- Items is paged, but item/group metadata makes it heavier than most master-data pages.
- LC is functionally acceptable but has the largest initial HTML payload among measured pages.
- Stocktaking was the main inventory grid risk because each new row duplicated item options.
- Salary preview was the main API risk: it previously returned multi-megabyte JSON because every preview row carried detailed components.
- Payroll replay is now bounded for normal view mode, but server calculation remains expensive.

## API Payload Validation

| API | Result |
|---|---|
| Stocktaking item lookup | 6.2 KB, 134 ms, bounded to 40 matches |
| Salary preview after compacting | 105.8 KB, 6,348 ms |
| Salary preview before compacting | 5,774.8 KB, 9,814 ms |
| Payroll replay normal mode | 459.1 KB, 6,780 ms |

## Optimizations Applied

- Added async stocktaking item lookup endpoint.
- Replaced stocktaking per-row item dropdown payload with searchable input and datalist.
- Added client-side lookup cache and 250 ms debounce for stocktaking item search.
- Reduced initial stocktaking item seed to 25 rows.
- Added salary preview row/journal payload metadata: total rows, total journal rows, and truncation flag.
- Returned compact salary preview rows for the browser while leaving detailed explainability behind the explicit explain endpoint.
- Trimmed payroll replay detail collections when `IncludeLineDetails=false`.
- Batched payroll table HTML rendering.

## Browser Runtime Findings

- Focused authenticated browser smoke opened Stocktaking and Salary Run.
- Console errors: 0.
- Repeated route smoke through Playwright completed without raw server errors.
- No destructive test data writes were performed.

## Long Session and Multi-Tab Notes

- The changed screens avoid storing huge salary preview payloads in local storage.
- Payroll recent employee storage remains capped to 6 entries.
- Stocktaking item lookup cache is in-memory per tab and term-based; it will not grow from route load alone.
- Multi-tab behavior remains session-cookie based; no shared mutable browser cache was added.

## Remaining Heavy-Data Risks

- Payroll calculation speed still depends on Main Original-compatible SQL functions and legacy snapshot reconstruction.
- Payroll replay still needs deeper repository-level profiling before changing business logic.
- LC should eventually lazy-load secondary panels and report traces after the first paint.
- Reports should continue to use paged/summary endpoints and avoid sending full ledger/detail grids unless explicitly exported.
- Existing search patterns using leading wildcard `LIKE '%term%'` will remain limited by SQL scan behavior until full-text/search indexes or normalized search columns are introduced.

## Future Work

- Add SQL timing instrumentation around payroll compatibility phases: load rows, attach components, insurance trace, journal preview, replay comparisons.
- Add lightweight report summary endpoints before loading detailed ledger rows.
- Add database indexes listed in the performance hardening doc after DBA review.
- Add optional export-only endpoints for very large report result sets instead of rendering them into live browser grids.

## Client Trial Status

Heavy browser payload risk is reduced enough for controlled enterprise trial. The remaining concerns are server-side payroll/replay computation time and future SQL indexing, not route failure or browser instability.
