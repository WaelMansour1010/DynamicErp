# Cash Database Size Audit - 2026-05-20

## Executive Summary
- Scope executed: **READ-ONLY diagnostics only** on `Cash` (instance `WAEL\SQL2019`).
- Data file (`Cash.mdf`): **36,957.25 MB** total, **33,760.19 MB used**, **3,197.06 MB free**.
- Log file (`Cash_0.ldf`): **4,848.24 MB** total, **12.77 MB used** (**0.26%**), free ~**4,835.48 MB**.
- Largest growth drivers are business tables, especially:
  - `DOUBLE_ENTREY_VOUCHERS` (**13,667.77 MB reserved**)
  - `Transactions` (**7,019.31 MB reserved**)
  - `Transaction_Details` (**5,843.34 MB reserved**)
  - `Notes` (**4,302.13 MB reserved**)
- Core finding: the size is mostly from **actual data + very large nonclustered indexes**, not from log usage and not from large internal unused space.

## Phase 1 Diagnostics Status
- Completed all required checks using SELECT/DMV metadata only.
- No delete, no shrink, no recovery model change, no index drop/rebuild executed.

## 1) Top 30 Tables by Size (Key Highlights)
Top offenders by reserved size:

| Table | Rows (metadata) | Data MB | Index MB | Reserved MB | Index/Data Ratio |
|---|---:|---:|---:|---:|---:|
| `DOUBLE_ENTREY_VOUCHERS` | 14,505,894 | 4,662.00 | 8,954.05 | 13,667.77 | 1.92 |
| `Transactions` | 3,768,771 | 3,329.63 | 3,642.48 | 7,019.31 | 1.09 |
| `Transaction_Details` | 6,097,242 | 3,745.09 | 2,061.27 | 5,843.34 | 0.55 |
| `Notes` | 4,073,331 | 3,714.48 | 560.36 | 4,302.13 | 0.15 |
| `LogFile` | 2,022,282 | 365.84 | 11.58 | 815.23 | 0.03 |
| `TblCusCsh` | 299,484 | 221.04 | 515.93 | 738.82 | 2.33 |

Notes:
- `DOUBLE_ENTREY_VOUCHERS` has very high index overhead (index size nearly double data size).
- `Transactions` also has heavy index footprint.

## 2) Largest Indexes + Usage + Duplicate Pattern
Largest indexes include:
- `DOUBLE_ENTREY_VOUCHERS.PK_DOUBLE_ENTREY_VOUCHERS` (clustered): ~4,672.50 MB
- `Transaction_Details.IX_Transaction_Details` (clustered): ~3,772.05 MB
- `Notes.PK_Notes` (clustered): ~3,725.63 MB
- `Transactions.PK_Transactions` (clustered): ~3,344.56 MB
- Several very large NCI on `DOUBLE_ENTREY_VOUCHERS` between ~600 MB and ~2,273 MB each.

Duplicate/near-duplicate indicators:
- Duplicate-signature indexes detected: **2**
- Estimated space consumed by duplicates: **~42.20 MB**

Unused/low-used indexes from DMVs:
- Read-unseen indexes reported: **1,374**
- Space tied to these indexes: **~33,212.26 MB**
- Important caveat: DMV usage counters reset after restart; this metric is directional and must be validated over a full business cycle.

## 3) Target Tables (Growth Risk Scan)
Requested tables and related patterns found:
- Very large: `DOUBLE_ENTREY_VOUCHERS`, `Transactions`, `Transaction_Details`, `Notes`
- Log/audit/staging/import examples found:
  - `LogFile` (~815 MB)
  - `POS_SaveAttemptLog` (~92 MB)
  - `POS_SaveAllocationStageLog` (~33.64 MB)
  - `POS_SystemErrorLog` (~33.98 MB)
- Backup/test/old/temp style names found (mostly tiny/empty), e.g. `Transactions_Restore`, `Transaction_Details_Restore`, `TblContractInstallmentsOld`, `TblOLDContract`, `temp1`, `temp2`, `NewTempTable`.

## 4) Historical Data Distribution
Using real date columns discovered in schema:
- `Transactions.Transaction_Date`
- `DOUBLE_ENTREY_VOUCHERS.RecordDate`

`Transactions`:
- Date span: **2023-03-26** to **2026-05-18**
- Rows counted by date: **1,256,257**
- By year: 2023: 6,207 | 2024: 293,798 | 2025: 685,119 | 2026: 271,133

`Transaction_Details` joined to `Transactions` (`Transaction_ID`):
- Date span: **2023-03-26** to **2026-05-18**
- Detail rows: **2,032,414**
- By year: 2023: 19,285 | 2024: 527,800 | 2025: 1,072,885 | 2026: 412,444

`DOUBLE_ENTREY_VOUCHERS`:
- Date span: **2003-01-08** to **2026-09-10**
- Rows counted by date: **7,252,947**
- By year: 2003: 2 | 2023: 53,006 | 2024: 1,740,582 | 2025: 3,932,294 | 2026: 1,527,063

Interpretation:
- Growth is largely **natural from accumulated transactional history**, especially 2024-2026.
- Additional inflation is from **aggressive indexing** on hottest financial tables.

## 5) LDF Diagnostics
- Recovery model: **SIMPLE**
- `log_reuse_wait_desc`: **NOTHING**
- Log total size: **4,848.24 MB**
- Log used: **12.77 MB** (0.26%)

Conclusion:
- Current log file is **over-allocated relative to active use**.
- No active blocker preventing log reuse at capture time.

## 6) Unused Space (Reserved vs Used)
- Major tables show low internal unused pages versus reserved size:
  - `DOUBLE_ENTREY_VOUCHERS`: unused ~41.22 MB
  - `Transactions`: unused ~32.27 MB
  - `Transaction_Details`: unused ~10.03 MB
  - `Notes`: unused ~16.13 MB
- Therefore, bloat is not primarily empty pages inside these objects; it is mostly allocated to active data/index structures.

## 7) Fragmentation (Large Indexes Only)
Top fragmented large indexes include:
- `Transaction_Details.IX_Transaction_Details` page_count 479,371 frag 54.09% -> Rebuild recommended (later window)
- `TblCusCsh` clustered/nonclustered indexes frag 46%-85% -> Rebuild recommended
- `POS_SaveAttemptLog` indexes frag ~53%-60% -> Rebuild recommended
- Several `Transactions`/`Notes` indexes in 10%-27% range -> Reorganize recommended

No maintenance action executed in this phase.

## Root Cause Assessment
Most likely reasons for size growth:
1. High-volume financial/history retention (especially `DOUBLE_ENTREY_VOUCHERS`, `Transactions`, `Transaction_Details`, `Notes`).
2. Heavy nonclustered indexing footprint (notably on `DOUBLE_ENTREY_VOUCHERS` and `Transactions`).
3. Secondary contributor: oversized log file allocation (space reserved but mostly unused).

Is current size normal?
- **Partially normal** for this transaction volume and retention horizon.
- **Partially non-optimal** due to index sprawl and oversized LDF allocation compared with real usage.

## Safe Recommendations (No execution yet)
1. Validate index usefulness over 2-4 weeks of uptime (DMV counters), then remove truly redundant/unused indexes in controlled change windows.
2. Tune/merge overlapping nonclustered indexes on `DOUBLE_ENTREY_VOUCHERS` and `Transactions`.
3. Run planned index maintenance for large fragmented indexes (Rebuild/Reorganize per thresholds).
4. Define archive policy by fiscal period for heavy tables (move cold years to archive DB/partitioned archive table).
5. Keep `LogFile`/system logs under retention controls at app level.
6. Consider controlled one-time log right-sizing **only after** maintenance and growth settings review.

## Dangerous / Not Recommended
- Random `DBCC SHRINKDATABASE`.
- Repeated periodic shrink cycles.
- Dropping indexes based on a single DMV snapshot.
- Immediate recovery model changes without backup/RPO design.

## Phased Plan
- **Phase 1 Diagnostics**: Completed (this report + select-only script).
- **Phase 2 Index Cleanup/Rebuild**:
  - confirm duplicate/overlapping indexes
  - execute maintenance in off-hours with rollback plan
- **Phase 3 Archive Strategy**:
  - archive cold years for `DOUBLE_ENTREY_VOUCHERS`, `Transactions`, `Transaction_Details`, `Notes`
- **Phase 4 Log Maintenance**:
  - validate autogrowth settings, backup cadence (if model changes later), right-size only when justified
- **Phase 5 Optional Controlled Shrink**:
  - only once, only post-cleanup/archive, only with capacity evidence and monitoring

## Artifacts
- Diagnostic SQL (SELECT-only): `F:\Source Code\DynamicErp\Docs\CashDatabaseSizeAudit_20260520_SELECT_ONLY.sql`
- Raw output snapshot: `F:\Source Code\DynamicErp\Docs\CashDatabaseSizeAudit_20260520_output.txt`
