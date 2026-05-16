# POS Save Deadlock Phase 1 Mitigation Plan - 2026-05-15

## Scope

This is a plan-only document for reducing Kishny POS invoice save deadlocks/contention without changing accounting meaning, voucher meaning, or the visible POS save flow.

Baseline inputs:

- `Areas/Pos/Docs/POS_Invoice_Save_Deadlock_Audit_20260515.md`
- `Areas/Pos/Docs/POS_Save_Deadlock_Investigation_20260514.md`
- `Areas/Pos/Docs/POS_SequenceAllocator_Contention_20260514.md`
- `Areas/Pos/Docs/POS_DoubleEntryVoucher_Deadlock_Investigation_20260514.md`
- `Areas/Pos/Docs/POS_FullSave_Allocator_Correlation_20260514.md`
- `Areas/Pos/Docs/POS_DEVSerial_LegacyRequirement_20260514.md`
- `Areas/Pos/Docs/POS_DEVSerial_RemoveFromHotPath_20260514.md`
- `Areas/Pos/Docs/POS_VoucherCoding_LegacyRequirement_20260514.md`
- `Areas/Pos/Docs/POS_VoucherCoding_Bottleneck_20260514.md`
- `Areas/Pos/Docs/POS_VoucherSerialScope_ProductionRolloutChecklist_20260514.md`

Phase 1 target:

- Reduce unnecessary serialization around voucher coding and legacy serial fields.
- Shorten the main `dbo.usp_POS_SaveTransaction` transaction where safe.
- Keep invoice posting, issue voucher creation, accounting rows, and stock effects synchronous.
- Preserve business-visible voucher behavior and existing POS save behavior.

## Baseline Conclusion

The strongest current suspect is not the raw `DOUBLE_ENTREY_VOUCHERS` insert. Yesterday's full-save stage evidence points more strongly at voucher coding / `NoteSerial1` allocation overlapping with a long `.NET` transaction and stricter lock usage than legacy VB6.

Important proven points from 2026-05-14:

- Isolated `DOUBLE_ENTREY_VOUCHERS` insert tests did not reproduce deadlocks/timeouts at 150 users.
- Full-save stage logs showed invoice voucher coding p99 reaching about 13-15 seconds.
- Old `DEV_Serial` allocation p99 reached about 4.6-4.7 seconds before the hot-path removal plan.
- `DEV_Serial` is not a durable accounting key and has no proven uniqueness/business-correctness requirement.
- `NoteSerial1` / voucher coding is business-visible and must be preserved.
- `99_POS_VoucherSerialScope.sql` already exists and is listed as `autoApply`.
- `97_POS_DEVSerial_RemoveFromHotPath.sql` already exists as a manual production rollout script.

## Exact Files And Scripts

### Existing scripts to verify/deploy

| File | Action | Reason |
| --- | --- | --- |
| `Areas/Pos/Sql/99_POS_VoucherSerialScope.sql` | Verify deployed; deploy if missing through the POS SQL updater. | Adds `TblOptions.POSVoucherSerialScope` and recreates `dbo.usp_GetNextSerial_V2` with Company/Branch/BranchStore effective scope. |
| `Areas/Pos/Sql/POS_SQL_AutoUpdate_Manifest.json` | Verify script `99` is still `autoApply: true`; no change expected. | Confirms normal deployment path includes voucher scope support. |
| `Areas/Pos/Sql/97_POS_DEVSerial_RemoveFromHotPath.sql` | Review and manually deploy only if production still has the old `POS_DEVSerialAllocator` / `DEV_Serial allocation` stage. | Removes avoidable legacy `DEV_Serial` serialization from `dbo.usp_POS_SaveTransaction`. |
| `Areas/Pos/Sql/MANUAL_98_POS_VoucherCoding_Diagnostics.sql` | Run read-only before and after. | Verifies effective scope, counter keys, duplicate risks, stage p95/p99, live waits, and deadlock XML indicators for voucher coding. |
| `Areas/Pos/Sql/MANUAL_89_POS_FullSave_AllocationCorrelation.sql` | Run read-only before and after realistic load. | Shows whether voucher coding remains the slowest full-save stage. |
| `Areas/Pos/Sql/MANUAL_86_POS_Save_Deadlock_Diagnostics.sql` | Run read-only during/after peak. | Captures application attempts, waits, index usage, and system_health deadlock XML. |
| `Areas/Pos/Sql/MANUAL_96_POS_DEVSerial_UsageAudit.sql` | Run read-only if there is doubt about current `DEV_Serial` usage. | Confirms no indexes/constraints require `DEV_Serial` uniqueness and no new `DEV_Serial allocation` stage is present. |

### Proposed Phase 1 implementation script

| Proposed file | Action | Reason |
| --- | --- | --- |
| `Areas/Pos/Sql/100_POS_SaveTransaction_Phase1_LockWindow.sql` | New reviewed SQL script, not auto-applied until approved. | Recreate `dbo.usp_POS_SaveTransaction` with stable settings/account reads moved before `BEGIN TRANSACTION`; keep writes, validations needing current state, ID allocation, voucher coding, and accounting posting behavior intact. |

No C# or JavaScript changes are required for the first Phase 1 rollout unless diagnostics prove the app is not passing `ClientRequestId` / `@NoID` correctly. Current baseline already includes POS save attempt logging and idempotency support.

## SQL Procedures Affected

| Procedure | Phase 1 action | Notes |
| --- | --- | --- |
| `dbo.usp_GetNextSerial_V2` | Affected by existing script `99_POS_VoucherSerialScope.sql`. | Supports Company, Branch, BranchStore scope without changing serial format. Uses `SerialCounters_V2` with `UPDLOCK, SERIALIZABLE` on the effective counter row. |
| `dbo.usp_POS_SaveTransaction` | Affected by existing script `97_POS_DEVSerial_RemoveFromHotPath.sql` if not already applied; affected by proposed script `100_POS_SaveTransaction_Phase1_LockWindow.sql`. | Main transaction currently begins before transaction id allocation and voucher coding, then remains open through reads, inserts, accounting, and issue voucher creation. |
| `dbo.usp_Voucher_coding_V2` | Do not rewrite in Phase 1. | Keep public behavior; it benefits from `dbo.usp_GetNextSerial_V2` scope behavior. |
| `dbo.usp_Notes_coding_V2` | Do not rewrite in Phase 1. | Still allocates accounting note serials synchronously. |
| `dbo.GetNextID_FromSequence` | Do not rewrite in Phase 1. | Centralized allocator remains in place; full-save evidence does not make it the first safe target. |

## Step 1 - Verify `99_POS_VoucherSerialScope.sql`

Run these checks against production before changing configuration:

```sql
SELECT HasPOSVoucherSerialScope =
    CASE WHEN COL_LENGTH(N'dbo.TblOptions', N'POSVoucherSerialScope') IS NULL THEN 0 ELSE 1 END;

SELECT GetNextSerialV2ObjectId = OBJECT_ID(N'dbo.usp_GetNextSerial_V2', N'P');

SELECT TOP (1)
    POSVoucherSerialScope =
        CASE
            WHEN COL_LENGTH(N'dbo.TblOptions', N'POSVoucherSerialScope') IS NULL THEN N'<missing>'
            ELSE POSVoucherSerialScope
        END
FROM dbo.TblOptions;

SELECT branch_no, sanad_no, StoreCoding, numbering_id, no_of_digit, YearDigit, Prefix
FROM dbo.sanad_numbering WITH (READCOMMITTEDLOCK)
WHERE sanad_no IN (7, 10)
ORDER BY branch_no, sanad_no, Prefix;
```

Expected:

- `TblOptions.POSVoucherSerialScope` exists.
- `dbo.usp_GetNextSerial_V2` exists and its definition includes effective scope logic.
- `Sanad_No = 7` and `Sanad_No = 10` are reviewed for `StoreCoding`.
- For current Kishny evidence, `StoreCoding = 0` means `BranchStore` collapses to Branch; `Branch` is the intended production setting.

If script `99` is missing, deploy only `Areas/Pos/Sql/99_POS_VoucherSerialScope.sql` through the existing POS SQL deployment process, then rerun `MANUAL_98_POS_VoucherCoding_Diagnostics.sql`.

## Step 2 - Configure Kishny POS Voucher Scope To Branch

Recommended production setting:

```sql
UPDATE dbo.TblOptions
SET POSVoucherSerialScope = N'Branch';
```

Rationale:

- Current evidence says Kishny POS numbering is branch-visible and branch/year/month oriented.
- Company scope unnecessarily serializes all POS branches through the same `SerialCounters_V2` counter row for `Transactions` type 21 and issue voucher type 19.
- Branch scope preserves the serial format while splitting the lock key by branch.
- Do not use `BranchStore` unless `sanad_numbering.StoreCoding = 1` is a confirmed business requirement; current evidence says StoreCoding is 0 for the relevant Kishny POS sanad rows.

Before lock behavior:

- `dbo.usp_GetNextSerial_V2` effective key can be `SourceTable=Transactions`, `BranchID=0`, `TypeCode=21`, year/month/prefix/store key.
- All branches can wait on the same counter row for POS invoice voucher coding.

After lock behavior:

- Effective key becomes `SourceTable=Transactions`, `BranchID=@BranchId`, `TypeCode=21`, year/month/prefix/store key.
- Issue voucher coding for `TypeCode=19` also uses branch-scoped counter rows.
- Lock duration per call is still strict, but fewer sessions compete for the same row.

Business-visible behavior:

- Serial format remains unchanged.
- Voucher number meaning remains branch/month/year based.
- Existing historical voucher numbers are not changed.

## Step 3 - Verify/Apply DEV_Serial Hot-Path Removal

Verify whether production still has the old allocator:

```sql
SELECT DefinitionText = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_POS_SaveTransaction'));

SELECT TOP (50)
    StageName, Samples = COUNT(*), MaxMs = MAX(DurationMs)
FROM dbo.POS_SaveAllocationStageLog WITH (READCOMMITTEDLOCK)
WHERE CreatedAt >= DATEADD(DAY, -7, GETDATE())
  AND StageName = N'DEV_Serial allocation'
GROUP BY StageName;
```

If `dbo.usp_POS_SaveTransaction` already has this style of assignment, no action is needed:

```sql
SET @DevSerial = CONVERT(CHAR(8), @TransactionDate, 112) + N'-' + CONVERT(NVARCHAR(20), @DevID);
```

If the old allocator is still present, manually apply:

```text
Areas/Pos/Sql/97_POS_DEVSerial_RemoveFromHotPath.sql
```

Before lock behavior:

- POS invoice accounting allocates `DEV_Serial` through a date-scoped allocator / lock path after `DOUBLE_ENTREY_VOUCHERS` ID allocation.
- Multiple invoice saves for the same date can serialize on a field that is not a durable accounting key.

After lock behavior:

- `DEV_Serial` is assigned as cheap display text from transaction date and `Double_Entry_Vouchers_ID`.
- No `POS_DEVSerialAllocator` row, date lock, or `DEV_Serial allocation` stage participates in the hot path.

Rollback:

```text
sqlcmd -S <server> -d <database> -E -v DevSerialAction=ROLLBACK -i Areas\Pos\Sql\97_POS_DEVSerial_RemoveFromHotPath.sql
```

## Step 4 - Move Safe Reads Before `BEGIN TRANSACTION`

Current `dbo.usp_POS_SaveTransaction` starts the main SQL transaction at the beginning of the save core and keeps it open through:

- `Transaction_ID` allocation.
- Invoice voucher coding.
- validation reads.
- `Transactions`, `Transaction_Details`, `TblSalesPayment`.
- account/settings lookups.
- `Notes` allocation/coding.
- `DOUBLE_ENTREY_VOUCHERS` ID allocation and insert.
- issue voucher allocation/coding/inserts.

The proposed `100_POS_SaveTransaction_Phase1_LockWindow.sql` should move only stable/reference reads before `BEGIN TRANSACTION`. It must not move writes or validations that require current transactional state.

Candidate reads to move before `BEGIN TRANSACTION`:

| Read / calculation | Current location | Phase 1 recommendation | Risk |
| --- | --- | --- | --- |
| `TblBranchesData` branch name lookup | Inside transaction after `@DevSerial`. | Move before transaction. | Low; display/account description only. |
| `dbo.GetMyAccountCode('TblCustemers', ...)` customer account lookup | Inside transaction. | Move before transaction. | Low-medium; if customer account changes during save, current behavior sees latest committed value. Acceptable only if business agrees account changes during peak are rare/admin-only. |
| `dbo.GetMyAccountCode('BanksData', ...)` cash/bank account lookup | Inside transaction. | Move before transaction. | Low-medium; reference data. |
| `TblUsers` -> `TblBoxesData` user second box account | Inside transaction. | Move before transaction. | Low-medium; user cashier setup should not change during invoice save. |
| `TblBoxesData` wallet, branch box, terminal POS accounts | Inside transaction. | Move before transaction. | Low-medium; setup data. |
| `TblItems` + `TblCustemers` supplier/item/revenue/price percent lookup | Inside transaction. | Move before transaction. | Medium; item accounting setup should be stable during save. Validate against item edit process. |
| `TblSettsReqLimK` VAT account by date/type | Inside transaction. | Move before transaction. | Low-medium; settings table. |
| `TblOptions` commission settings | Inside transaction. | Move before transaction. | Low-medium; settings table. |
| `dbo.CheckPriceRangeSales3(...)` wallet cost lookup | Inside transaction. | Move before transaction if deterministic and read-only on price tables. | Medium; confirm function has no side effects and tolerates same inputs. |
| `dbo.get_account_code_branch(...)` service/tax/sales account lookups | Mixed inside accounting line creation. | Compute before transaction for all branches/account IDs needed by the selected service type. | Medium; function must be read-only and stable. |

Reads to keep inside the transaction in Phase 1:

| Operation | Reason |
| --- | --- |
| Existing invoice update cleanup/deletes. | Must remain atomic with replacement inserts. |
| `Transaction_ID`, `NoteID`, `Double_Entry_Vouchers_ID` allocations. | Moving outside the transaction may create wider gaps and different failure semantics; not Phase 1. |
| `dbo.usp_Voucher_coding_V2` invoice and issue voucher calls. | Business-visible numbering; preserve behavior. Scope change should reduce contention first. |
| `dbo.usp_Notes_coding_V2`. | Accounting note serial behavior is not fully audited for outside-transaction allocation. |
| Card token `sp_getapplock` and card availability checks. | Must protect current card issue/sale correctness. |
| ManualNO/IPN duplicate check. | Must remain current relative to concurrent saves. |
| Inserts/updates/deletes to `Transactions`, `Transaction_Details`, `TblSalesPayment`, `Notes`, `DOUBLE_ENTREY_VOUCHERS`. | Atomic save contract. |
| Issue voucher cursor and inserts. | Accounting/stock meaning must remain synchronous in Phase 1. |

Before lock behavior:

- Reference/account/settings reads execute while the transaction is already holding or about to hold locks from ID allocation, voucher coding, and invoice rows.
- Slow reads, blocked reads, or plan regressions lengthen the time serial/counter locks overlap with transaction writes.

After lock behavior:

- Stable lookup latency is paid before write locks and voucher/counter locks are acquired.
- Main transaction still performs the same writes and current-state validations.
- Accounting posting remains synchronous.

Implementation guardrails for the proposed script:

- Use a DROP + CREATE style consistent with POS SQL scripts.
- Keep SQL Server 2012 compatibility.
- Preserve all parameters and output contract of `dbo.usp_POS_SaveTransaction`.
- Add comments only around moved pre-transaction lookup block.
- Do not introduce `NOLOCK` to correctness checks.
- Do not change accounting line formulas.
- Do not reorder issue voucher creation relative to accounting in Phase 1.

## Step 5 - Shorten Voucher Coding Lock Duration

Phase 1 should shorten voucher coding contention by configuration and by reducing overlap, not by loosening correctness.

Safe actions:

- Confirm `99_POS_VoucherSerialScope.sql` is deployed.
- Set `TblOptions.POSVoucherSerialScope = N'Branch'`.
- Warm required `SerialCounters_V2` rows off-peak so first branch/month/type use does not need fallback `MAX(...)` discovery during teller load.
- Move unrelated stable reads before `BEGIN TRANSACTION` so voucher coding lock overlap is shorter.

Actions not approved in Phase 1:

- Do not move voucher coding outside the save transaction until duplicate/gap/failure semantics are explicitly accepted.
- Do not replace `dbo.usp_Voucher_coding_V2` with loose `MAX+1`.
- Do not remove duplicate checks around generated `NoteSerial1`.
- Do not change visible serial formatting, prefix, YearDigit, sanad settings, or transaction type mapping.

## MAX+1 / Strict Lock Patterns Still In Scope

Observed related patterns:

- `dbo.usp_GetNextSerial_V2` fallback uses `MAX(...)` with `NOLOCK` only when a counter row does not exist yet; normal path updates `SerialCounters_V2` with `UPDLOCK, SERIALIZABLE`.
- `dbo.GetNextID_FromSequence` creates/repairs sequences using `MAX(...) WITH (HOLDLOCK, UPDLOCK)` under a session applock. This remains in place for Phase 1.
- `Areas/Pos/Sql/41_POS_PurchaseInvoice.sql` and `Areas/Pos/Sql/42_POS_StockTransfer.sql` still contain `MAX+1 WITH (UPDLOCK,HOLDLOCK)` patterns, including `DOUBLE_ENTREY_VOUCHERS`, but they are outside Kishny POS invoice save Phase 1.

Phase 1 should not touch purchase invoice, stock transfer, or unrelated modules. Capture them as Phase 2/backlog risks.

## Rollback Plan

### Voucher scope rollback

Fast rollback:

```sql
UPDATE dbo.TblOptions
SET POSVoucherSerialScope = N'Company';
```

Then rerun `MANUAL_98_POS_VoucherCoding_Diagnostics.sql` to confirm effective scope is Company.

### DEV_Serial rollback

Use the built-in rollback path in script `97`:

```text
sqlcmd -S <server> -d <database> -E -v DevSerialAction=ROLLBACK -i Areas\Pos\Sql\97_POS_DEVSerial_RemoveFromHotPath.sql
```

### `dbo.usp_POS_SaveTransaction` lock-window rollback

Before applying proposed script `100`, save the current production definition:

```sql
SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_POS_SaveTransaction')) AS ProcedureDefinition;
```

Rollback options:

- Reapply the prior approved `Areas/Pos/Sql/30_POS_SaveTransaction_UnicodeText.sql`.
- Or apply the saved production definition from the rollback capture.
- Keep database backup and deployment package from immediately before rollout.

### Production operational rollback

- Stop new POS traffic briefly if severe failures appear.
- Revert `POSVoucherSerialScope` to Company first because it is the lowest-risk toggle.
- Roll back `dbo.usp_POS_SaveTransaction` only if save correctness or accounting parity fails.
- Keep all diagnostic result sets and deadlock XML for post-rollback analysis.

## Test Plan

### Pre-deployment verification on a copied production database

1. Restore recent production backup to a test database.
2. Run `MANUAL_98_POS_VoucherCoding_Diagnostics.sql`.
3. Run `MANUAL_96_POS_DEVSerial_UsageAudit.sql`.
4. Run `MANUAL_89_POS_FullSave_AllocationCorrelation.sql` after a baseline load.
5. Confirm `POSVoucherSerialScope` current value and `sanad_numbering.StoreCoding` for `Sanad_No IN (7,10)`.

### Functional smoke tests

Create/save invoices through the real POS path for:

- Normal POS card sale.
- Cash-in/recharge sale.
- Cash-out/wallet sale.
- Traffic violations if enabled in the tested environment.
- Invoice edit path if `@ExistingTransactionID` is supported for the scenario.
- Sale that creates issue voucher type 19.

For each saved invoice, compare:

- `Transactions.Transaction_ID`, `Transaction_Type`, `NoteSerial1`, `NoteSerial`, `NoteId`, `NOTS`.
- `Transaction_Details` row count, quantities, prices, `ItemSerial`, `StoreID2`.
- `TblSalesPayment` rows and totals.
- `Notes` row count, `NoteType`, `NoteSerial`, `NoteSerial1`, `Double_Entry_Vouchers_ID`.
- `DOUBLE_ENTREY_VOUCHERS` line count, debit total, credit total, account codes, `Transaction_ID`, `Notes_ID`.
- Printed/search-visible voucher number format.

### Concurrency tests

Run realistic full-save load test on the copied database:

```powershell
& "F:\Source Code\DynamicErp\Areas\Pos\Tools\Invoke-PosSaveLoadTest.ps1" `
  -BaseUrl "http://localhost:<port>" `
  -SqlServer "<server>" `
  -Database "<test_db>" `
  -Concurrency 100 `
  -RequestsPerUser 3 `
  -IncludeCard `
  -IncludeViolations `
  -SimulateUserFlow `
  -DoubleClickPercent 20
```

Repeat at 150 users if the 100-user run is clean.

Success indicators:

- Deadlocks reduced versus baseline.
- Timeouts reduced versus baseline.
- `Invoice voucher coding allocation` p95/p99 materially lower after Branch scope and pre-transaction reads.
- `Issue voucher coding allocation` does not become the new dominant p99.
- No `DEV_Serial allocation` stage appears after script `97`.
- `Accounting insert` remains low and does not become a new deadlock source.
- No duplicate `NoteSerial1` within the intended effective scope for new test rows.

### Diagnostics after tests

Run:

- `Areas/Pos/Sql/MANUAL_89_POS_FullSave_AllocationCorrelation.sql`
- `Areas/Pos/Sql/MANUAL_98_POS_VoucherCoding_Diagnostics.sql`
- `Areas/Pos/Sql/MANUAL_86_POS_Save_Deadlock_Diagnostics.sql`
- `Areas/Pos/Sql/MANUAL_90_POS_DoubleEntryVoucher_Diagnostics.sql` only if deadlock XML names `DOUBLE_ENTREY_VOUCHERS`.

## Production Deployment Checklist

1. Confirm there is an approved backup and rollback window.
2. Confirm no unrelated POS SQL deployment is bundled into this rollout.
3. Run read-only diagnostics `MANUAL_86`, `MANUAL_89`, `MANUAL_98`, and optionally `MANUAL_96`.
4. Verify `99_POS_VoucherSerialScope.sql` is deployed.
5. Set `TblOptions.POSVoucherSerialScope = N'Branch'`.
6. Warm counters for active branches/month/type 21 and type 19 off-peak, or run the first post-change saves branch-by-branch before peak.
7. Verify `dbo.usp_POS_SaveTransaction` no longer has old `DEV_Serial` allocator; apply `97` only if needed and approved.
8. Apply proposed `100_POS_SaveTransaction_Phase1_LockWindow.sql` only after separate review/approval.
9. Recycle app pool only if deployment process requires it; SQL-only config changes should not require application code changes.
10. Run one real save per high-value POS scenario.
11. Monitor `POS_SaveAttemptLog`, `POS_SaveAllocationStageLog`, SQL waits, and system_health deadlock XML during the next peak.
12. Keep rollback commands ready for `POSVoucherSerialScope`, `97`, and `100`.

## Risks

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Branch scope exposes unexpected historical duplicate assumptions. | New saves may hit duplicate checks or reporting ambiguity. | Run duplicate diagnostics by branch/type/month before production switch. |
| Cold branch-scoped counters use fallback `MAX(...)` discovery. | First save per branch/type/month can be slower. | Warm counters off-peak. |
| Moving account/settings reads before the transaction can read a value that changes before commit. | Rare mismatch if admin changes setup during active save. | Limit to stable reference tables; deploy during controlled window; document operational rule not to edit accounting setup during peak. |
| `GetNextID_FromSequence` remains centralized. | Some serialization remains. | Accepted for Phase 1; not the top current suspect. |
| Issue voucher type 19 coding remains inside transaction. | Card/stock-heavy flows can still overlap locks. | Monitor `Issue voucher coding allocation`; defer redesign to Phase 2. |
| Real production deadlock graph may name a different object/index. | Phase 1 may reduce latency but not eliminate all deadlocks. | Capture XML and keep changes small/rollbackable. |

## What Not To Change In Phase 1

- Do not redesign accounting.
- Do not make accounting posting asynchronous.
- Do not remove invoice-level `Notes` or `DOUBLE_ENTREY_VOUCHERS` posting.
- Do not change debit/credit line formulas or account mapping meaning.
- Do not change visible voucher number format, prefix, sanad, year/month logic, or transaction type mapping.
- Do not drop or loosen constraints/indexes on `DOUBLE_ENTREY_VOUCHERS`.
- Do not rewrite `dbo.GetNextID_FromSequence`.
- Do not replace serial allocation with VB6-style loose `MAX+1`.
- Do not touch purchase invoice, stock transfer, closing vouchers, payroll, MainErp, or unrelated POS modules.
- Do not add broad `NOLOCK` to correctness checks.
- Do not remove the card token transaction applock in Phase 1.

## Phase 1 Recommended Sequence

1. Read-only production diagnostics and duplicate/scope check.
2. Deploy/verify `99_POS_VoucherSerialScope.sql`.
3. Set `POSVoucherSerialScope = N'Branch'`.
4. Warm branch counters off-peak.
5. Verify/apply `97_POS_DEVSerial_RemoveFromHotPath.sql` only if production still has the old `DEV_Serial` allocator.
6. Load test and monitor stage p95/p99.
7. If contention remains, implement reviewed `100_POS_SaveTransaction_Phase1_LockWindow.sql` to move stable reads before `BEGIN TRANSACTION`.
8. Re-test full-save behavior and accounting parity.

## Phase 2 Candidates, Not Phase 1

- Redesign voucher/serial allocation to reserve numbers in shorter independent units with explicit accepted gap semantics.
- Review `dbo.GetNextID_FromSequence` locking and sequence repair behavior.
- Rework issue voucher creation to reduce cursor duration while preserving stock/accounting meaning.
- Tune `DOUBLE_ENTREY_VOUCHERS` indexes only from real production deadlock XML and query evidence.
- Address MAX+1 patterns in purchase invoice and stock transfer scripts separately.
- Consider snapshot/read-committed snapshot strategy only after full database-wide compatibility review.

