# POS Save Deadlock Investigation - 2026-05-14

Scope: `Areas/Pos` only.

## Executive Summary

Production deadlocks are most likely caused by the full POS save path under real cashier behavior, not by a simple direct 100-row insert test. The real path can include UI double-clicks, defaults/lookups before save, printing after save, card issue voucher creation, and accounting voucher generation inside the same SQL transaction.

This update does not change POS totals or accounting business logic. It adds duplicate-submit protection, request correlation/idempotency, stronger save diagnostics, a read-only deadlock evidence script, and a more realistic load test.

## Exact Save Path

1. UI:
   `Areas/Pos/Scripts/pos-transaction.js`

   Main functions:
   - `confirmSaveBtn` click handler
   - `submitConfirmedSave`
   - `saveTransaction`
   - print preview/request logic after successful save

2. Controller:
   `Areas/Pos/Controllers/PosTransactionController.cs`

   Main action:
   - `PosTransactionController.Save(PosSaveTransactionRequest request)`

3. Repository:
   `Areas/Pos/Data/PosSqlRepository.cs`

   Main methods:
   - `SaveTransaction`
   - `ExecuteSaveTransactionWithDeadlockRetry`
   - `ExecuteSaveTransactionProcedure`
   - `TryBeginPosSaveIdempotency`
   - `CompletePosSaveIdempotency`
   - `FailPosSaveIdempotency`

4. Stored procedure:
   `Areas/Pos/Sql/30_POS_SaveTransaction_UnicodeText.sql`

   Main SQL object:
   - `dbo.usp_POS_SaveTransaction`

5. Accounting and vouchers inside the procedure:
   - `Transactions`
   - `Transaction_Details`
   - `TblSalesPayment`
   - `Notes`
   - `DOUBLE_ENTREY_VOUCHERS`
   - `POS_DEVSerialAllocator`
   - sequence allocation through `dbo.GetNextID_FromSequence`
   - optional card issue voucher path using `Transaction_Type = 19`

## Likely Deadlock Sources Found

1. Duplicate browser submits during save:
   The UI already had a save-in-progress flag, but the confirmation button was not disabled immediately enough for very fast repeated clicks or keyboard-confirm flows. This can create two save requests close together during network delay.

2. Retry without a client correlation key:
   The repository had retry logic for SQL deadlocks, but repeated browser requests were not tied to a durable client request id. If the first request was still running and the second arrived, the server could not reliably tell whether it was the same cashier action.

3. Long SQL transaction scope:
   `dbo.usp_POS_SaveTransaction` starts one transaction and then performs invoice, details, payment, notes, journal, sequence allocation, voucher coding, and optional card issue voucher work inside it. This increases lock duration during peak use.

4. Shared sequence and voucher allocation:
   `dbo.GetNextID_FromSequence`, note serial generation, voucher coding, and DEV serial allocation are shared resources. Under many users, these can become blocking points even when each invoice touches a different branch.

5. Accounting generated inside the save transaction:
   The invoice save creates accounting rows in `DOUBLE_ENTREY_VOUCHERS` in the same SQL transaction. This is safer for consistency, but it means accounting contention can block invoice save.

6. Card transaction extra path:
   Kishny card mode can create an issue voucher with `Transaction_Type = 19`. That path writes additional transaction/note/accounting rows and can participate in a different lock pattern than cash-in/cash-out.

7. Production flow differs from direct insert test:
   The failed artificial test likely missed pre-save lookups, user think-time overlap, double-clicks, print reads, session/default reads, and a mixed service profile.

## Changes Made

### UI Duplicate Submit Protection

File:
`Areas/Pos/Scripts/pos-transaction.js`

Reason:
Prevent repeated save requests from fast double clicks and keyboard submit while the save is still in progress.

What changed:
- Added a per-save `ClientRequestId`.
- Disabled `confirmSaveBtn` immediately on confirmed save.
- Shows `جاري الحفظ...` while the request is in progress.
- Ignores repeated confirm clicks while save is busy.
- Clears the client request id only after success/failure.
- Print flow remains separate and does not trigger another save.

### Server Idempotency and Correlation

Files:
`Areas/Pos/Controllers/PosTransactionController.cs`
`Areas/Pos/Data/PosSqlRepository.cs`
`Areas/Pos/Models/PosSaveTransactionRequest.cs`

Reason:
Make duplicate browser requests safe and traceable under network delay or repeated clicks.

What changed:
- `ClientRequestId` is accepted on save.
- New requests are registered in `dbo.POS_SaveIdempotency`.
- A repeated completed request returns the original transaction id instead of creating another invoice.
- A repeated in-progress request returns HTTP 409 with a clear Arabic message.
- Logs now include `ClientRequestId`, user id, branch/store/box, service type, SQL error number, and duration where available.

### SQL Idempotency Table

File:
`Areas/Pos/Sql/85_POS_Save_Idempotency.sql`

Reason:
Durable duplicate-submit guard for POS invoice save.

SQL Server compatibility:
SQL Server 2012 compatible.

Object:
`dbo.POS_SaveIdempotency`

### Deadlock Evidence Script

File:
`Areas/Pos/Sql/MANUAL_86_POS_Save_Deadlock_Diagnostics.sql`

Reason:
The current summary screen was not enough to identify lock owners/victims. This script collects more evidence without destructive production changes.

It shows:
- POS save attempt summary.
- Deadlock/timeout rows grouped by branch, store, box, service, payment, and request fingerprint.
- Recent failed attempts with SQL error numbers.
- Idempotency records.
- Current blocking sessions and waits.
- Lock/wait statistics.
- Index usage for POS save tables.
- Recent `system_health` deadlock XML when SQL permissions allow it.

### Realistic Load Test

File:
`Areas/Pos/Tools/Invoke-PosSaveLoadTest.ps1`

Reason:
The old test inserted directly and did not reproduce real user behavior.

What changed:
- Can simulate 100 users/sessions.
- Adds random delay between steps.
- Adds pre-save reads similar to defaults/lookups.
- Mixes cash-in, cash-out, card, and optional violations.
- Includes card mode, which exercises the issue voucher path.
- Can simulate double-click duplicate save attempts.
- Reads print-related rows after save.
- Records deadlock, timeout, retry, duplicate submission, duplicate invoice risk, duration, and transaction id.

Note:
This test calls the SQL procedure directly, so it is for database lock reproduction. Browser/controller idempotency is tested through the web save endpoint.

## SQL Objects Involved In Save

Primary procedure:
- `dbo.usp_POS_SaveTransaction`

Primary tables:
- `Transactions`
- `Transaction_Details`
- `TblSalesPayment`
- `Notes`
- `DOUBLE_ENTREY_VOUCHERS`
- `POS_DEVSerialAllocator`
- `POS_SaveAttemptLog`
- `POS_SaveIdempotency`

Shared allocation/coding objects:
- `dbo.GetNextID_FromSequence`
- voucher coding and note serial logic used by the save procedure

Optional card issue path:
- `Transactions` with `Transaction_Type = 19`
- related `Notes`
- related `DOUBLE_ENTREY_VOUCHERS`

## Documented Lock Order In Current Procedure

Current effective order inside `dbo.usp_POS_SaveTransaction`:

1. Validate and prepare input.
2. Begin SQL transaction.
3. Allocate transaction id / serial values.
4. Insert invoice header into `Transactions`.
5. Insert invoice details into `Transaction_Details`.
6. Insert payment rows into `TblSalesPayment`.
7. Insert note rows into `Notes`.
8. Generate accounting/voucher rows in `DOUBLE_ENTREY_VOUCHERS`.
9. For card flow, create additional issue voucher transaction/note/accounting work.
10. Commit transaction.

Risk:
If any other POS path touches these objects in a different order, especially `Notes`, `DOUBLE_ENTREY_VOUCHERS`, voucher coding, or sequence tables, it can deadlock under load.

## Before And After Risk

Before:
- Same browser action could reach the server more than once.
- Server could not identify duplicate client save attempts.
- Deadlock reports had too little detail to identify exact lock owners.
- Load test was too artificial to match production.

After:
- UI blocks repeated save submits.
- Server stores and checks a save idempotency key.
- Completed duplicate requests return the existing transaction.
- In-progress duplicate requests return 409 and do not enter the stored procedure.
- Diagnostics can extract richer production evidence.
- Load test is closer to cashier behavior.

Remaining risk:
- The SQL transaction is still large because accounting is generated atomically with invoice save.
- No business/accounting lock order rewrite was applied without a real production deadlock graph.
- If deadlocks persist, the next safe step is to analyze the XML graph and then target the exact conflicting statement/index/order.

## How To Run The Realistic Concurrency Test

From an elevated PowerShell session on a test database:

```powershell
Set-Location 'F:\Source Code\DynamicErp'
.\Areas\Pos\Tools\Invoke-PosSaveLoadTest.ps1 `
  -ConnectionString 'Server=.;Database=cash;Trusted_Connection=True;' `
  -Users 100 `
  -AttemptsPerUser 5 `
  -IncludeCard `
  -IncludeViolations `
  -SimulateUserFlow `
  -DoubleClickPercent 15 `
  -MaxThinkTimeMs 1500
```

Review:
- Total attempts.
- Deadlock count.
- Timeout count.
- Retry success/failure.
- Duplicate submitted count.
- Duplicate created invoice count.
- Slowest attempts and SQL error numbers.

## How To Collect Production Deadlock Evidence

Run this read-only script on the production database during or soon after the incident:

```sql
:r .\Areas\Pos\Sql\MANUAL_86_POS_Save_Deadlock_Diagnostics.sql
```

Or open the file in SSMS and adjust:

```sql
DECLARE @FromDate datetime = DATEADD(HOUR, -6, GETDATE());
DECLARE @ToDate datetime = GETDATE();
DECLARE @BranchId int = NULL;
```

Important result sets:
- Recent failed `POS_SaveAttemptLog` rows.
- Blocking sessions.
- `system_health` deadlock XML.

Send the deadlock XML, not only the summary numbers. The XML identifies exact owners, victims, SQL statements, indexes, and lock modes.

## Recommended Next Step After Production Evidence

1. Capture the deadlock XML for a real peak incident.
2. Identify the exact two or more statements participating in the cycle.
3. If the cycle involves missing indexes, add the narrowest POS index.
4. If the cycle involves inconsistent object order, adjust only that POS path to match the documented order.
5. If the cycle involves sequence/voucher allocation, isolate and shorten only that allocation section.

No further accounting logic should be changed until the deadlock graph proves the conflicting statements.
