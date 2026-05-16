# POS Save Phase 1 Blockers - 2026-05-15

## Scope

This is a focused stabilization report for the blockers found after controlled script 100 testing on:

```text
SQL Server: Wael\Sql2019
Database: Cash
```

Current Cash state should remain unchanged for investigation:

- `TblOptions.POSVoucherSerialScope = Branch`
- Phase 1 `dbo.usp_POS_SaveTransaction` from script 100 is active
- rollback capture exists in `dbo.POS_SaveTransactionPhase1Rollback`
- backup is available: `Cash_Phase1_Pre100_20260515_223224.bak`

No rollback is recommended from the current evidence.

## Baseline Result

The primary Phase 1 objective appears successful in the low smoke run:

- No deadlocks observed.
- No successful-save SQL timeouts.
- No duplicate Branch-scope voucher numbers in the tested sample.
- Successful cash-in/cash-out accounting rows were balanced.
- Issue vouchers were created for the successful direct-harness saves.

Medium/peak concurrency should remain paused until the focused blockers below are resolved or classified.

## Blocker Summary

| Blocker | Current classification | Evidence | Next action |
|---|---|---|---|
| Violations accounting mapping failure `50000` | Likely Cash environment/configuration issue, not proven script 100 regression | Previous captured procedure already had the same mapping guard. Item `2` in Cash has missing accounting setup used by the violations path. | Fix or provide valid test accounting configuration for violations, then rerun only violations smoke. |
| Card candidate discovery timeout | Harness/data-query limitation, not a save-path timeout | Timeout happened before `dbo.usp_POS_SaveTransaction` was called. Candidate query scans large card/transaction/detail sets with expressions around card serial values. | Use a deterministic known-unused valid card token for smoke, or create a read-only candidate diagnostics query before any mutating card test. |
| Duplicate-submit validation incomplete | Harness/UI-path gap | Script 100 low smoke used `DoubleClickPercent = 0`; direct SQL harness is not equivalent to browser/repository duplicate-submit behavior. | Validate through application/repository path with same `NoID`/request fingerprint. |
| Receipt/print validation incomplete | UI/controller/reporting-path gap | Direct SQL harness only reads saved rows after save; it does not exercise MVC print action/templates/permissions. | Validate print through POS UI or controller route against a successful saved transaction. |

## 1. Violations Accounting Mapping Failure

### Observed Failure

Two low-smoke violations saves failed with:

```text
SqlErrorNumber: 50000
Invoice accounting account mapping is missing for one or more required rows.
```

Failed cases:

```text
BranchId=67, StoreID=64, UserID=88
BranchId=44, StoreID=43, UserID=65
```

### Procedure Behavior

The error is raised by `dbo.usp_POS_SaveTransaction` after accounting lines are built. For `TrafficViolations = 1`, the procedure creates lines using:

- `@BranchBoxAccount`
- `@ItemSupplierAccount`
- `@ItemRevenueAccount`
- `@PricePercent`

Then it rejects any positive-value accounting line where `AccountCode` is blank or `NO account`.

### Cash Configuration Evidence

Read-only diagnostics showed:

```text
ItemID=2
ItemName=<Kishny recharge item in Cash>
DefaultSupplier=NULL
SupplierAccount=NULL
ItemAccount=NULL
ItemRevenueAccount=NULL
PricePercent=0.0
```

For the failed branches:

```text
Branch 67: BranchBoxAccount=a1a2a1a1a87, ItemRevenueAccount=NULL
Branch 44: BranchBoxAccount=a1a2a1a1a45, ItemRevenueAccount=NULL
```

The immediate missing account for a positive violations line is `ItemRevenueAccount`.

### Regression Assessment

Current classification: **likely environment/configuration issue, not proven script 100 regression**.

Reason:

- The rollback capture taken immediately before script 100 already contains the same error guard:

```text
HasMappingGuard=1
HasPhase1Block=0
HasBranchScopeText=0
DefinitionBytes=113380
```

- Script 100 did move stable lookups before `BEGIN TRANSACTION`, but the failing account values are derived from the same item/branch/account setup data.
- Cash item `2` lacks the account setup needed by the violations accounting branch.

### Stabilization Task

Do not change accounting logic yet.

Recommended next checks:

1. Confirm the real production/service item used for violations. The harness currently uses `ItemIDService = 2`, which may be a poor test fixture.
2. Compare a known successful legacy violations invoice and identify its service item/account mappings.
3. Configure Cash test data only, if needed, so the violations service item has the same required accounts as the real production path.
4. Rerun only low-volume violations smoke after configuration is verified.

Stop if violations accounting remains unbalanced or uses unexpected accounts.

## 2. Card Candidate Discovery Timeout

### Observed Failure

The card-enabled smoke attempt timed out before any card save executed.

The timeout occurred in the harness candidate discovery query inside:

```text
F:\Source Code\DynamicErp\Areas\Pos\Tools\Invoke-PosSaveLoadTest.ps1
```

Relevant query behavior:

- Reads `dbo.TblCusCsh`.
- Excludes cards already used by `dbo.Transactions`.
- Cross-applies stock availability from `dbo.Transaction_Details`, `dbo.Transactions`, and `dbo.TransactionTypes`.
- Uses trimmed expressions around `CardNo`, `VisaNumber`, and `ItemSerial`.

### Data Size

Read-only row estimates on Cash:

```text
TblCusCsh: 149,061 rows
Transactions: 1,256,391 rows
Transaction_Details: 2,030,850 rows
```

### Index Evidence

Card-related indexes exist, including:

- `TblCusCsh`: `IX_POS_TblCusCsh_CardNo_EasyCashType`, `IX_POS_TblCusCsh_KycSearch_CardNo`
- `Transaction_Details`: `IX_Transaction_Details` on `ItemSerial`, plus normalized serial indexes
- `Transactions`: `IDX_Transactions_VisaNumber`, plus transaction type/date/store indexes

However, the harness query wraps serial fields with:

```sql
LTRIM(RTRIM(ISNULL(..., N'')))
```

Those expressions can prevent simple index seeks on the raw serial columns and make the query expensive before the save path starts.

### Regression Assessment

Current classification: **harness/data-query limitation, not a script 100 save regression**.

Reason:

- The failure happened before `dbo.usp_POS_SaveTransaction` was called.
- No card save duration, voucher allocation duration, accounting insert, or save error was captured for a card transaction.
- Script 100 changes are in the save procedure and voucher scope, not in the harness card-candidate discovery query.

### Stabilization Task

Do not tune production card search yet as part of Phase 1 blocker triage.

Recommended safe validation path:

1. Pick a deterministic known-unused valid card token with available stock using a separate read-only diagnostic.
2. Run one low-volume card smoke save using that fixed token.
3. If fixed-token card save passes, classify the timeout as harness candidate discovery only.
4. If fixed-token card save fails, capture the exact save-stage diagnostics before any further load.

## 3. Duplicate-Submit Validation

### Current Gap

Duplicate-submit validation is incomplete.

The low smoke run used:

```text
DoubleClickPercent=0
```

So no duplicate save attempt was intentionally submitted.

The direct SQL harness can resend the same stored procedure parameters, but it still does not fully represent the UI/repository duplicate-submit path.

### Regression Assessment

Current classification: **not tested; UI/repository validation gap**.

No evidence currently suggests script 100 broke duplicate-submit protection, but it has not been validated under the Phase 1 state.

### Stabilization Task

Recommended validation:

1. Use the application/repository save path, not only direct SQL.
2. Submit the same POS save request twice with the same `NoID`/client request identifier.
3. Confirm only one transaction is created.
4. Confirm the second response is idempotent or rejected according to current expected behavior.
5. Verify no duplicate `Transactions`, `Notes`, `Payments`, or `DOUBLE_ENTREY_VOUCHERS` rows are produced.

Do not proceed to concurrency testing until this is checked at least once for cash-in and cash-out.

## 4. Receipt / Print Validation

### Current Gap

Receipt/print validation is incomplete.

The direct harness has an `Invoke-PrintAfterSave` helper, but it only performs read queries:

```text
Transactions
Transaction_Details
Notes
```

It does not exercise:

- POS controller print action
- print permissions
- print template selection
- rendered receipt view/report
- browser/UI print behavior

### Regression Assessment

Current classification: **UI/controller validation gap**.

No evidence currently suggests script 100 broke receipt/print output. It was not validated through the real print path.

### Stabilization Task

Recommended validation:

1. Use one successful saved cash-in transaction from the low smoke.
2. Use one successful saved cash-out transaction from the low smoke.
3. Open the POS receipt/print action through the application path.
4. Confirm the receipt renders.
5. Confirm voucher number, amount, customer/IPN fields, and issue voucher references still display correctly.
6. Confirm normal print permissions still apply.

Successful test transaction candidates:

```text
1299494 cash-out
1299497 cash-in
1299496 cash-in
1299500 cash-out
1299503 cash-in
1299505 cash-in
```

## Focused Stabilization Sequence

Run these in order, staying below load-test intensity:

1. **Violations config triage**
   - Confirm the correct service item for violations.
   - Verify required account mappings.
   - Rerun one controlled violations smoke only after configuration is valid.

2. **Card fixed-token smoke**
   - Avoid the broad candidate discovery query.
   - Use a known valid card token with available stock.
   - Run one controlled card save.

3. **Duplicate-submit app-path smoke**
   - Use the application/repository path.
   - Same request identifier twice.
   - Verify single transaction result.

4. **Receipt/print UI smoke**
   - Use existing successful transaction IDs.
   - Verify rendered receipt correctness.

Only after all four pass should medium concurrency be reconsidered.

## What Not To Change

Do not change these as part of blocker triage:

- Do not rollback script 100 based on current evidence.
- Do not change voucher serial meaning.
- Do not change Branch-scope voucher behavior.
- Do not redesign accounting posting.
- Do not remove the accounting mapping guard.
- Do not make posting asynchronous.
- Do not run medium/peak concurrency until the four blockers are resolved or explicitly classified as harmless test limitations.
- Do not tune broad card search/indexing as part of this focused task unless a separate plan is approved.

## Supporting Logs

```text
F:\Source Code\DynamicErp\Areas\Pos\Logs\Phase1CashTest_20260515\05_after_100_state.txt
F:\Source Code\DynamicErp\Areas\Pos\Logs\Phase1CashTest_20260515\06_post_smoke_diagnostics.txt
F:\Source Code\DynamicErp\Areas\Pos\Logs\Phase1CashTest_20260515\08_deadlock_after_smoke_recheck.txt
F:\Source Code\DynamicErp\Areas\Pos\Logs\Phase1CashTest_20260515\10_blockers_readonly_diagnostics.txt
F:\Source Code\DynamicErp\Areas\Pos\Logs\Phase1CashTest_20260515\11_card_index_diagnostics.txt
F:\Source Code\DynamicErp\Areas\Pos\Logs\Phase1CashTest_20260515\12_rollback_definition_check.txt
```
