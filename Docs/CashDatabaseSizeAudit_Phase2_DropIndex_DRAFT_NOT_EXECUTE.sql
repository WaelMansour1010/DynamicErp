/*
Cash Phase 2 - DROP INDEX DRAFT (DO NOT EXECUTE)
Date: 2026-05-20
SQL Server 2012 compatible

IMPORTANT:
- This file is intentionally fully commented.
- Review + benchmark + pre/post execution plan checks are mandatory.
- Validate over full business cycle due DMV reset risks.
*/

/*
========================================================
Candidate Group 1 (LOW-MEDIUM RISK): Exact duplicate
========================================================
Finding:
- dbo.Notes has exact duplicate signature:
  IX_Notes_Transaction_ID and IX_POS_Notes_Transaction_ID
  same key: Transaction_ID
  same include: NoteID

Estimated space from smaller duplicate: ~19.89 MB

-- Proposed draft (choose one only after validation):
-- DROP INDEX [IX_POS_Notes_Transaction_ID] ON [dbo].[Notes];
*/

/*
========================================================
Candidate Group 2 (MEDIUM RISK): DEV overlap (Transaction_ID)
========================================================
Finding:
- dbo.DOUBLE_ENTREY_VOUCHERS has two overlapping indexes on Transaction_ID:
  IX_DOUBLE_ENTREY_VOUCHERS_Transaction_ID (~170.49 MB)
  IX_POS_DEV_Transaction_ID (~164.25 MB)

Keep strategy draft:
- Keep broader/shared index according to real plans
- Consider dropping POS variant if not required by critical queries

-- DROP INDEX [IX_POS_DEV_Transaction_ID] ON [dbo].[DOUBLE_ENTREY_VOUCHERS];
*/

/*
========================================================
Candidate Group 3 (MEDIUM RISK): DEV overlap (Notes_ID)
========================================================
Finding:
- dbo.DOUBLE_ENTREY_VOUCHERS has two overlapping indexes on Notes_ID:
  IX_DOUBLE_ENTREY_VOUCHERS_Notes_ID (~166.38 MB)
  IX_POS_DEV_Notes_ID (~163.63 MB)

-- DROP INDEX [IX_POS_DEV_Notes_ID] ON [dbo].[DOUBLE_ENTREY_VOUCHERS];
*/

/*
========================================================
Candidate Group 4 (MEDIUM-HIGH RISK): RecordDate overlap
========================================================
Finding:
- RecordDate access covered by multiple indexes:
  IX_DEV_RecordDate (~612.51 MB)
  <IndexRecordDate, sysname,> (~207.46 MB)
  IX_POS_DEV_RecordDate (~163.70 MB)

Potential conservative draft:
- Keep IX_DEV_RecordDate as main broad index.
- Consider retiring legacy single-key RecordDate index only after workload replay.

-- DROP INDEX [<IndexRecordDate, sysname,>] ON [dbo].[DOUBLE_ENTREY_VOUCHERS];
*/

/*
========================================================
Candidate Group 5 (HIGH RISK): Transaction_Details fan-out
========================================================
Finding:
- Multiple overlapping indexes around Transaction_ID and Item_ID.
- <IndxTT, sysname,> (~118.74 MB) appears broadly overlapped,
  but table is heavily used by sales/stock reports and save flows.

-- DROP INDEX [<IndxTT, sysname,>] ON [dbo].[Transaction_Details];
-- HIGH RISK: do not proceed without replay tests and report SLA validation.
*/

/*
========================================================
Very High Risk / Do NOT touch in early wave
========================================================
- Clustered PK/CI on all target tables
- Filtered search/report indexes used by POS search/report modules
- Large composite indexes on Transactions until report workload is profiled

Examples to keep initially:
- [PK_DOUBLE_ENTREY_VOUCHERS]
- [PK_Transactions]
- [IX_Transaction_Details] (clustered)
- [PK_Notes]
- [IX_POS_Transactions_Search_ManualNO]
- [IX_POS_Transactions_Search_NoteSerial1]
- [IX_POS_Transactions_Search_VisaNumber]
*/
