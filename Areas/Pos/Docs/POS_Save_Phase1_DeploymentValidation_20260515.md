# POS Save Phase 1 Deployment Validation - 2026-05-15

## Scope

This document is the controlled validation checklist for deploying the Phase 1 POS save deadlock mitigation.

Target database for rehearsal: `Cash` test database only, when explicitly approved.

Production rule: do not deploy automatically. Run the checks, capture evidence, then deploy script `100_POS_SaveTransaction_Phase1_LockWindow.sql` only during an approved production window.

## 1. Pre-Deployment Checks

Run these before script `100`.

```sql
SELECT DatabaseName = DB_NAME(), ServerName = @@SERVERNAME, CapturedAt = GETDATE();

SELECT HasPOSVoucherSerialScope =
    CASE WHEN COL_LENGTH(N'dbo.TblOptions', N'POSVoucherSerialScope') IS NULL THEN 0 ELSE 1 END;

SELECT GetNextSerialV2ObjectId = OBJECT_ID(N'dbo.usp_GetNextSerial_V2', N'P');
SELECT SaveTransactionObjectId = OBJECT_ID(N'dbo.usp_POS_SaveTransaction', N'P');

SELECT TOP (1) POSVoucherSerialScope
FROM dbo.TblOptions WITH (READCOMMITTEDLOCK);

SELECT
    sanad_no,
    MinStoreCoding = MIN(CONVERT(INT, ISNULL(StoreCoding, 0))),
    MaxStoreCoding = MAX(CONVERT(INT, ISNULL(StoreCoding, 0)))
FROM dbo.sanad_numbering WITH (READCOMMITTEDLOCK)
WHERE sanad_no IN (7, 10)
GROUP BY sanad_no;
```

Expected:

- `HasPOSVoucherSerialScope = 1`.
- `dbo.usp_GetNextSerial_V2` exists.
- `dbo.usp_POS_SaveTransaction` exists.
- Current production default may still be `Company` before rollout.
- For Kishny POS, `Sanad_No = 7` and `10` should have `StoreCoding = 0`; Branch scope is the safe target.

Also verify no old DEV serial hot path remains:

```sql
SELECT HasOldDevSerialAllocator =
    CASE WHEN OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_POS_SaveTransaction')) LIKE N'%POS_DEVSerialAllocator%' THEN 1 ELSE 0 END;

SELECT HasDevSerialAllocationStage =
    CASE WHEN OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_POS_SaveTransaction')) LIKE N'%DEV_Serial allocation%' THEN 1 ELSE 0 END;
```

Expected:

- `HasOldDevSerialAllocator = 0`.
- `HasDevSerialAllocationStage = 0`.

## 2. Backup Requirements

Before production deployment:

- Take a full database backup.
- Record backup file path, timestamp, database name, server name, and operator.
- Export current `dbo.usp_POS_SaveTransaction` definition.
- Confirm rollback script access from the deployment machine.
- Confirm a DBA or responsible engineer is available during the first peak period after deployment.

Minimum capture:

```sql
SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_POS_SaveTransaction')) AS ProcedureDefinitionBeforePhase1;
```

Script `100` also stores a rollback definition in `dbo.POS_SaveTransactionPhase1Rollback`, but external backup is still required.

## 3. Required Diagnostics Before Script 100

Run and save all result grids:

- `Areas/Pos/Sql/MANUAL_86_POS_Save_Deadlock_Diagnostics.sql`
- `Areas/Pos/Sql/MANUAL_89_POS_FullSave_AllocationCorrelation.sql`
- `Areas/Pos/Sql/MANUAL_98_POS_VoucherCoding_Diagnostics.sql`
- `Areas/Pos/Sql/MANUAL_96_POS_DEVSerial_UsageAudit.sql` if DEV serial evidence is needed

Capture:

- Recent deadlock/timeout counts.
- `Invoice voucher coding allocation` p95/p99.
- `Issue voucher coding allocation` p95/p99.
- Full save p95/p99.
- Current `POSVoucherSerialScope`.
- Effective scope for `Sanad_No = 7` and `10`.
- Any duplicate `NoteSerial1` warnings by branch/type/month.
- Recent system_health deadlock XML if available.

## 4. Branch-Scope Counter Warm-Up

Reason:

After switching from Company to Branch scope, first use of a branch/type/month counter can run the fallback discovery path. Do this off-peak or warm counters before peak usage.

Safe warm-up options:

- Preferred: after script `100`, perform one controlled real save per active branch/service path during the deployment window, then reverse/cancel only using the normal business process if required.
- Alternative: let the first few branches save off-peak naturally and monitor `SerialCounters_V2`.
- Avoid ad hoc serial consumption scripts unless separately reviewed, because they may consume visible invoice numbers.

Verification query:

```sql
SELECT TOP (100)
    SourceTable,
    BranchID,
    TypeCode,
    Prefix,
    StoreID,
    YearNum,
    MonthNum,
    CurrentTail,
    UpdateCount,
    LastUpdated,
    UpdatedByUser
FROM dbo.SerialCounters_V2 WITH (READCOMMITTEDLOCK)
WHERE SourceTable = 'Transactions'
  AND TypeCode IN (19, 21)
ORDER BY LastUpdated DESC;
```

Expected after warm-up:

- Active branches show counter rows with branch-specific `BranchID`.
- `UpdatedByUser` contains scope hints such as `Scope=Branch`.
- `StoreID` remains `NULL`/0 when `StoreCoding = 0`.

## 5. Smoke Test Cases

Run through the real POS web flow after script `100`.

| Case | Validate |
| --- | --- |
| Cash in | Save succeeds, `Transaction_Type = 21`, voucher number visible, payments correct. |
| Cash out | Save succeeds, wallet/cash-out accounting lines balance. |
| Card | Card token validation still works; save succeeds for a valid card. |
| Violations | Save succeeds only if violations flow is enabled and data is valid. |
| Issue voucher path | Type 19 issue voucher is created when item/store flow requires it; sale `NOTS` links to issue transaction. |
| Accounting entry created | `Notes` row exists, `DOUBLE_ENTREY_VOUCHERS` rows exist, debit total equals credit total. |
| Receipt/print | Receipt/print preview opens after save and displays the same visible voucher number format. |

Suggested SQL verification for each saved transaction:

```sql
DECLARE @TransactionID INT = <saved_transaction_id>;

SELECT Transaction_ID, Transaction_Type, Transaction_Date, BranchId, StoreID, NoteSerial1, NoteSerial, NoteId, NOTS
FROM dbo.Transactions WITH (READCOMMITTEDLOCK)
WHERE Transaction_ID = @TransactionID;

SELECT COUNT(*) AS DetailRows, SUM(ISNULL(Quantity, 0) * ISNULL(Price, 0)) AS DetailTotal
FROM dbo.Transaction_Details WITH (READCOMMITTEDLOCK)
WHERE Transaction_ID = @TransactionID;

SELECT *
FROM dbo.TblSalesPayment WITH (READCOMMITTEDLOCK)
WHERE TransID = @TransactionID;

SELECT NoteID, NoteType, NoteSerial, NoteSerial1, Note_Value, Transaction_ID, Double_Entry_Vouchers_ID
FROM dbo.Notes WITH (READCOMMITTEDLOCK)
WHERE Transaction_ID = @TransactionID;

SELECT
    RowsCount = COUNT(*),
    DebitTotal = SUM(CASE WHEN Credit_Or_Debit = 0 THEN Value ELSE 0 END),
    CreditTotal = SUM(CASE WHEN Credit_Or_Debit = 1 THEN Value ELSE 0 END)
FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (READCOMMITTEDLOCK)
WHERE Transaction_ID = @TransactionID;
```

Expected:

- Save completes without deadlock/timeout.
- Debit and credit totals match.
- Voucher visible format is unchanged.
- Issue voucher exists when expected and is absent when not expected.
- Receipt uses the saved invoice number.

## 6. Peak Monitoring Checklist

Monitor during the first peak after deployment.

### Deadlock XML

Run `MANUAL_86_POS_Save_Deadlock_Diagnostics.sql` during or shortly after peak.

Capture:

- Victim process.
- Owner/waiter statements.
- Objects and indexes.
- Whether XML mentions `usp_Voucher_coding_V2`, `usp_GetNextSerial_V2`, `SerialCounters_V2`, `Transactions`, `Notes`, or `DOUBLE_ENTREY_VOUCHERS`.

### POS_SaveAttemptLog

Check:

- Deadlock count.
- Timeout count.
- Retry success/failure.
- Branch/store/service concentration.
- Longest save durations.

### POS_SaveAllocationStageLog

Check:

- `Invoice voucher coding allocation`.
- `Issue voucher coding allocation`.
- `Invoice NoteID allocation`.
- `Invoice NoteSerial allocation`.
- `Double entry voucher ID allocation`.
- `Accounting insert`.

Use:

```sql
;WITH s AS
(
    SELECT
        StageName,
        ServiceType,
        DurationMs,
        rn = ROW_NUMBER() OVER (PARTITION BY StageName, ServiceType ORDER BY DurationMs),
        cnt = COUNT(*) OVER (PARTITION BY StageName, ServiceType)
    FROM dbo.POS_SaveAllocationStageLog WITH (READCOMMITTEDLOCK)
    WHERE CreatedAt >= DATEADD(HOUR, -2, GETDATE())
)
SELECT
    StageName,
    ServiceType,
    Samples = MAX(cnt),
    AvgMs = AVG(CONVERT(BIGINT, DurationMs)),
    MaxMs = MAX(DurationMs),
    P95Ms = MIN(CASE WHEN rn >= CEILING(cnt * 0.95) THEN DurationMs END),
    P99Ms = MIN(CASE WHEN rn >= CEILING(cnt * 0.99) THEN DurationMs END)
FROM s
GROUP BY StageName, ServiceType
ORDER BY P99Ms DESC, MaxMs DESC;
```

### Save Duration p95/p99

Use:

```sql
;WITH x AS
(
    SELECT
        TransactionType,
        DurationMs,
        rn = ROW_NUMBER() OVER (PARTITION BY TransactionType ORDER BY DurationMs),
        cnt = COUNT(*) OVER (PARTITION BY TransactionType)
    FROM dbo.POS_SaveAttemptLog WITH (READCOMMITTEDLOCK)
    WHERE CreatedAt >= DATEADD(HOUR, -2, GETDATE())
      AND EventName = N'Save.Success'
      AND DurationMs IS NOT NULL
)
SELECT
    TransactionType,
    Saves = COUNT(*),
    AvgMs = AVG(DurationMs),
    MaxMs = MAX(DurationMs),
    P95Ms = MIN(CASE WHEN rn >= CEILING(cnt * 0.95) THEN DurationMs END),
    P99Ms = MIN(CASE WHEN rn >= CEILING(cnt * 0.99) THEN DurationMs END)
FROM x
GROUP BY TransactionType
ORDER BY P99Ms DESC;
```

## 7. Rollback Steps

### Fast voucher-scope rollback

```sql
UPDATE dbo.TblOptions
SET POSVoucherSerialScope = N'Company';
```

Then rerun `MANUAL_98_POS_VoucherCoding_Diagnostics.sql` and confirm effective scope is Company.

### Procedure rollback

Preferred operational rollback:

- Reapply the previous approved `Areas/Pos/Sql/30_POS_SaveTransaction_UnicodeText.sql` from the release package used before Phase 1.

Rollback from captured definition:

```sql
SELECT TOP (1) DefinitionText
FROM dbo.POS_SaveTransactionPhase1Rollback WITH (READCOMMITTEDLOCK)
WHERE ProcedureName = N'usp_POS_SaveTransaction'
  AND ChangeName = N'Phase1_LockWindow_20260515'
ORDER BY BackupId DESC;
```

Use the captured definition to recreate the procedure in a controlled script if needed.

Rollback order during incident:

1. Stop or reduce new POS traffic if saves are failing.
2. Set `POSVoucherSerialScope = N'Company'`.
3. If failures continue or accounting parity is affected, roll back `dbo.usp_POS_SaveTransaction`.
4. Preserve all diagnostics and deadlock XML before cleanup.

## 8. Exact Success Criteria

Deployment is successful only if all are true:

- Build/release package is the approved one.
- Script `99` is deployed.
- Script `100` runs without SQL errors.
- `POSVoucherSerialScope = Branch`.
- `Sanad_No = 7` and `10` remain effectively Branch scoped with `StoreCoding = 0`.
- Cash in, cash out, card, violations, issue voucher, accounting, and receipt smoke tests pass.
- No accounting imbalance is created.
- No visible voucher format change is observed.
- No `POS_DEVSerialAllocator` or `DEV_Serial allocation` stage appears in current save procedure/stage logs.
- Peak `Invoice voucher coding allocation` p95/p99 is materially lower than the 2026-05-14 baseline or at least no worse while deadlocks/timeouts are reduced.
- POS save deadlocks/timeouts are lower than baseline during comparable peak volume.

## 9. Exact Stop Criteria

Stop deployment or roll back if any occur:

- Script `100` fails or only partially applies.
- `dbo.usp_POS_SaveTransaction` fails to compile.
- POS save fails for normal cash-in/cash-out scenarios.
- Debit and credit totals do not match for any smoke-test invoice.
- `NoteSerial1` format changes unexpectedly.
- Duplicate `NoteSerial1` is generated inside the intended branch/type/month scope for new saves.
- Card token validation allows an invalid duplicate/card sale.
- Issue voucher type 19 is missing when it should be created, or created when it should not be.
- Receipts/printing show wrong voucher numbers.
- Deadlocks/timeouts spike above baseline after the change.
- Deadlock XML shows a new repeated cycle caused by script `100`.
- Any unexpected accounting, stock, or customer-visible behavior appears.

