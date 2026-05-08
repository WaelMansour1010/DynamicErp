# POS Deadlock / Save Hardening Release Notes - 2026-05-08

## Summary
The POS save hardening work targets deadlocks and ID allocation collisions around `PosSqlRepository.SaveTransaction` and `dbo.usp_POS_SaveTransaction`.

Release status: **candidate, needs isolation from unrelated POS feature work before customer deployment**.

## Code Changes Reviewed
- `Areas/Pos/Data/PosSqlRepository.cs`
  - Defines SQL deadlock number `1205`.
  - Uses retry delays `{ 150, 300, 600 }` ms.
  - Wraps `SaveTransaction` execution in `ExecuteSaveTransactionWithDeadlockRetry`.
  - Retries only when `SqlException.Errors[*].Number == 1205`.
  - Logs save start, deadlock retry, retry success, failed, and retried-failed events.
  - Writes a text log under `App_Data/Logs/pos-deadlock-retry-yyyyMMdd.log`.
  - Writes structured rows to `dbo.POS_SaveAttemptLog`.
- `Areas/Pos/Models/PosSystemErrorLogModels.cs`
  - Models save attempt log rows, timeline entries, summary counts, and search result.
- `Areas/Pos/Controllers/PosSystemErrorLogController.cs`
  - Adds/searches save attempt logs through `SearchSaveAttempts`.
- `Areas/Pos/Views/PosSystemErrorLog/Index.cshtml`
  - Adds a UI tab for POS save attempts, retry counts, deadlock affected attempts, retry success/failure, and timeline.

## SQL Changes Reviewed
- `Areas/Pos/Sql/30_POS_SaveTransaction_UnicodeText.sql`
  - `dbo.usp_POS_SaveTransaction`.
  - Uses `dbo.GetNextID_FromSequence` for `Transactions.Transaction_ID`, `Notes.NoteID`, and `DOUBLE_ENTREY_VOUCHERS.Double_Entry_Vouchers_ID`.
  - Adds allocation failure messages containing source table/field and next value.
  - Uses `sys.sp_getapplock` around invoice accounting `DEV_Serial` allocation.
  - Adds duplicate IPN prevention for Cash In and Keshni Card saves.
- `Areas/Pos/Sql/31_POS_GetNextID_FromSequence_Concurrency.sql`
  - Recreates `dbo.GetNextID_FromSequence`.
  - Uses `sys.sp_getapplock` with lock resource `GetNextID_FromSequence:<schema>.<table>.<field>`.
  - Uses SQL Server sequence objects and restarts them if needed.
- `Areas/Pos/Sql/46_POS_SaveTransaction_ConcurrencyIndexes.sql`
  - Adds narrow nonclustered indexes on save hot paths such as `DOUBLE_ENTREY_VOUCHERS.Transaction_ID`, `Notes.Transaction_ID`, `Transaction_Details.Transaction_ID`, and `TblSalesPayment.TransID`.
- `Areas/Pos/Sql/47_POS_SaveAttemptLog.sql`
  - Creates `dbo.POS_SaveAttemptLog` and indexes for date, attempt id, branch, user, event, and status.
- `Areas/Pos/Sql/39_POS_Deadlock_Diagnostics.sql`
  - Diagnostic/read script for recent application deadlock logs and SQL deadlock graph guidance.

## Retry Behavior
- One original attempt plus up to three retries for SQL error `1205`.
- Delay sequence: 150 ms, 300 ms, 600 ms.
- Non-deadlock SQL errors are not retried.
- After retry exhaustion, the original SQL exception is surfaced and logged as `RetriedFailed`.
- If a retry succeeds, the final transaction id is returned and logged as `Save.Retry.Success`.

## Failed Save Handling
- Failed deadlock attempts are retried automatically by the repository.
- Failed non-deadlock attempts are not retried.
- The user should see the normal save error if all retry attempts fail.
- Structured logging should make customer impact auditable after the fact.

## Duplicate Invoice Prevention
- The stored procedure is expected to run in a database transaction. A deadlock victim rolls back its partial work before retry.
- New `Transaction_ID` values are allocated through `GetNextID_FromSequence`.
- Duplicate IPN validation is added for Cash In and Keshni Card saves in both controller/repository and stored procedure areas.
- Risk remains: because unrelated admin delete and Excel import changes are mixed into the same files, the release package must isolate only the retry/save hardening code before deployment.

## Transaction ID Allocation Safety
- `GetNextID_FromSequence` serializes allocation per table/field using `sp_getapplock`.
- Sequence object use avoids `MAX(id)+1` races.
- The save procedure checks for null/non-positive/out-of-range allocations and raises explicit errors.
- SQL Server compatibility note: `CREATE SEQUENCE` requires SQL Server 2012 or later.

## User Impact Logging
- Text file: `App_Data/Logs/pos-deadlock-retry-yyyyMMdd.log`.
- Database table: `dbo.POS_SaveAttemptLog`.
- UI: POS system error log screen, save attempts tab.

## Release Recommendation
Ship only after creating a clean package with:
- retry/logging code,
- log UI if desired,
- required SQL scripts `31`, `30`, `46`, `47`,
- no MainErp, Excel import, payment/cashing, admin delete, or debug route exposure.
