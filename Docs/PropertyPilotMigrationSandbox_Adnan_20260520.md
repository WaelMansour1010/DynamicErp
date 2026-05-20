# Adnan Property Pilot Migration Sandbox Assessment - 2026-05-20

## 1. Executive Summary

This document defines the first practical but safe Pilot Migration approach for moving the `Adnan` property module into the main DynamicErp Web architecture under:

`F:\Source Code\DynamicErp`

This is not a production migration and no production data was changed. The work performed in this phase is limited to:

- SELECT-only diagnostics.
- Dry Run logic.
- Mapping validation.
- Sandbox strategy design.
- Reconciliation design.
- Temporary report/artifact files under `Docs`.

The recommended strategy remains:

`Hybrid Migration = Active Contracts + Opening Balances + Historical Archive + Minimal Safe Accounting`

The current `Adnan` data can be migrated into a sandbox only after solving account mapping and blocking bad active-contract links. It should not be inserted into the current `Alromaizan` production/reference database directly.

## 2. Safety Boundaries

The following rules are mandatory for this Pilot:

| Area | Rule |
|---|---|
| Source DB `Adnan` | Read-only. No writes. |
| Reference DB `Alromaizan` | Read-only. No writes. |
| Sandbox DB | Plan only in this phase. Do not create without explicit approval. |
| DynamicErp project | Documentation and SELECT-only diagnostics only. |
| Accounting | No full historical journal migration. |
| Historical vouchers | Archive/reconciliation first, not native posting. |

No `INSERT`, `UPDATE`, `DELETE`, `ALTER`, `DROP`, `MERGE`, `TRUNCATE`, or stored-procedure execution was used for this phase.

## 3. Proposed Sandbox Architecture

### 3.1 Recommended Sandbox Database

Do not use `Alromaizan` directly.

Recommended database name for the next execution phase:

`Alromaizan_AdnanPilot_Sandbox_20260520`

This database should be a clone/restore of the current `Alromaizan` reference architecture, then migration inserts should be tested only there.

### 3.2 Sandbox Principles

1. Clone `Alromaizan` to a new isolated sandbox database.
2. Keep `Adnan` as read-only source.
3. Add/import only pilot records into the sandbox clone.
4. Preserve every old id through cross-reference mapping files or dedicated sandbox cross-reference tables.
5. Disable or bypass automatic posting that would duplicate opening balances.
6. Run DynamicErp Web against sandbox connection string only.
7. Compare results through reconciliation queries before any Go Live decision.

### 3.3 Do Not Execute Yet

This phase does not create the sandbox DB. Creation/restoration should be a separate approved step because it requires an actual database write operation, even though it would not touch production data.

## 4. Active Contract Definition For Pilot

Two possible definitions were tested:

| Definition | Rule | Count |
|---|---|---:|
| Operational active | `EndContract` is null/0 and no settlement exists in `TblFiterWaiver` | 1,231 |
| Strict active | Operational active and `EndDate >= 2026-05-20` or `EndDate IS NULL` | 293 |
| Operational active but date expired | Operational active but `EndDate < 2026-05-20` | 938 |

The Pilot should use strict active contracts only.

Reason: importing the 938 expired-but-not-closed contracts as active would make the web system show old expired contracts as live operational contracts. These should be handled later as archive, cleanup, or special opening-balance cases.

## 5. Strict Active Contract Population

As of `2026-05-20`, the strict active scope contains:

| Metric | Value |
|---|---:|
| Strict active contracts | 293 |
| Properties required | 26 |
| Units required | 258 |
| Renters required | 256 |
| Installment rows | 1,099 |
| Total installment value | 33,055,074.9365 |
| True paid from allocation/payment table | 12,719,724.4580 |
| True remaining total | 20,335,350.4785 |
| Negative true remaining rows | 0 |
| Overpaid true installment rows | 0 |

### 5.1 Blocking Link Issues

10 strict-active contracts have missing basic links. These must not be migrated until fixed or explicitly excluded.

| Contract Count | Issue |
|---:|---|
| 10 | Missing property/unit/renter relation indicators in the strict-active set |

Affected contract numbers found in the blocker export:

`331`, `689`, `690`, `721`, `805`, `945`, `946`, `947`, `1626`, `1716`

This means 283 of 293 strict-active contracts are structurally linkable immediately, before accounting checks.

## 6. Payment And Balance Logic

The Pilot must not depend on stored `TblContractInstallments.payed` and `TblContractInstallments.Remains` alone.

VB6 evidence shows paid values are recalculated through installment/payment allocation logic, especially through screens like `FrmCashing1.frm`, `RsCashing.frm`, and contract functions such as `getinsttPayedTocontract(...)`.

For the Pilot Dry Run, the safer paid calculation is:

`ContracttBillInstallmentsDone` joined to `Notes` where `Notes.NoteType = 4`, grouped by `istallid`.

Paid formula used:

```sql
RentValuePayed
+ CommissionsPayed
+ InsurancePayed
+ WaterPayed
+ ElectricPayed
+ TelandNetPayed
+ OldValuePayed
+ VATPayed
```

### 6.1 Opening Balance Split

| Bucket | Rows | Contracts | Installment Total | True Paid | True Remain |
|---|---:|---:|---:|---:|---:|
| Due/past installments through `2026-05-20` | 646 | 259 | 13,820,676.2280 | 12,664,131.5680 | 1,156,544.6600 |
| Future installments after `2026-05-20` | 453 | 199 | 19,234,398.7085 | 55,592.8900 | 19,178,805.8185 |

Recommended interpretation:

- `1,156,544.66` is the opening receivable candidate for due/past unpaid amounts.
- `19,178,805.8185` should remain as future `PropertyContractBatch` amounts, not opening balance.
- Any paid amount on future installments must be reviewed because it can represent advance payment.

### 6.2 Receipt Evidence

For strict active contracts, `Notes.NoteType = 4` contains:

| CashingType | NoteCashingType | Voucher Rows | Contracts | Total Note Value |
|---:|---:|---:|---:|---:|
| 8 | 0 | 3 | 3 | 24,417.0000 |
| 8 | 2 | 660 | 233 | 9,958,222.8652 |
| 8 | 4 | 90 | 49 | 2,750,774.1600 |

Total receipt note value:

`12,733,414.0252`

Allocated true paid from `ContracttBillInstallmentsDone`:

`12,719,724.4580`

Difference requiring reconciliation:

`13,689.5672`

This difference is not currently a blocker for sandbox design, but it is a blocker for Go Live unless explained by unapplied receipts, rounding, non-rent receipt components, or receipt detail behavior.

### 6.3 Accounting Evidence

Receipt-linked accounting lines for strict active receipt notes:

| Metric | Value |
|---|---:|
| Accounting rows | 2,360 |
| Notes with accounting rows | 753 |
| Debit value | 12,802,048.4785 |
| Credit value | 12,802,048.4785 |

The source accounting lines are balanced, but they should not be imported as historical native `JournalEntry` rows in the first Pilot. Doing so risks double-posting once opening balances are also created.

## 7. Account Mapping Result

Strict-active renters:

| Metric | Value |
|---|---:|
| Active renters | 256 |
| Renters with old `Account_Code` | 256 |
| Account code exists in `Adnan.ACCOUNTS` | 256 |
| Account code currently exists in `Alromaizan.ChartOfAccount` | 0 |

This is the most important Pilot blocker.

DynamicErp property screens and accounting flows depend on `PropertyRenter.AccountId`, `ChartOfAccount`, and department account mappings. A sandbox migration cannot be operationally valid unless renter accounts are seeded or mapped in the sandbox chart of accounts first.

## 8. Practical Migration Strategy

### 8.1 Data Transfer Order

1. Sandbox preparation.
2. Lookup mapping: property type, unit type, rent type, branch, department, payment method, cash/bank mappings.
3. Account mapping/seeding in sandbox: `Adnan.ACCOUNTS` to `ChartOfAccount` for active renters and required property accounts.
4. Owners if needed for property ownership and reports.
5. Properties from `TblAqar` for strict-active contracts only.
6. Units from `TblAqarDetai` linked to strict-active contracts only.
7. Renters from `TblCustemers` for strict-active contracts only.
8. Active contracts from `TblContract` strict-active set only.
9. Contract batches from `TblContractInstallments`.
10. Opening balance layer for due/past unpaid amounts only.
11. Future batches remain as normal future installments.
12. Historical receipts/payments/journals/settlements as archive references only.
13. Reconciliation reports.
14. DynamicErp UI smoke test against sandbox.

### 8.2 What To Ignore Temporarily

| Area | Decision |
|---|---|
| Full historical `Notes` import | Ignore as native data; archive only. |
| Old `DOUBLE_ENTREY_VOUCHERS` as target journals | Ignore as native journals in Pilot. |
| Ended contracts | Archive only. |
| Operational-active but expired contracts | Do not migrate as active; review separately. |
| Settled contracts in `TblFiterWaiver` | Exclude from active Pilot. |
| Historical unit status rows | Archive/reconciliation only. |
| Full VAT historical reconstruction | Not in Pilot. |

### 8.3 What To Transform

| Source | Target | Transformation |
|---|---|---|
| `TblAqar` | `Property` | Property code/name/type/location/owner mapping. |
| `TblAqarDetai` | `PropertyDetail` | Unit id to target unit, derive status from active contract. |
| `TblCustemers` | `PropertyRenter` | Renter info plus account mapping. |
| `TblContract` | `PropertyContract` | Header, period, renter/property/unit, components, VAT flags. |
| `TblContractInstallments` | `PropertyContractBatch` | Batch dates/components/totals. |
| `ContracttBillInstallmentsDone` | Opening balance/reconciliation layer | Recompute true paid and true remain. |
| `Notes.NoteType=4` | Archive/evidence, optional modern receipt later | Classify and reconcile, do not full-import first. |

### 8.4 What To Archive Only

- `Notes` historical rows.
- `DOUBLE_ENTREY_VOUCHERS` historical rows.
- `TblFiterWaiver`, `TblFiterWaiverDe`, `TblFiterWaiverDet2` settlement history.
- `TblUnitNoInformation` status history.
- Old expired contracts not in strict-active scope.
- Old installment history tables.

## 9. Reconciliation Layer Design

The Reconciliation Layer should be implemented before any real migration insert.

### 9.1 Required Reconciliation Views/Reports

| Reconciliation Area | Source Calculation | Target Calculation | Pass Rule |
|---|---|---|---|
| Active contract count | Strict-active `TblContract` | Migrated `PropertyContract` | Same count after excluding blockers. |
| Property count | Distinct `TblContract.Iqar` | Migrated `Property` | Same mapped count. |
| Unit count | Distinct `TblContract.UnitNo` | Migrated `PropertyDetail` | Same mapped count. |
| Renter count | Distinct `TblContract.CusID` | Migrated `PropertyRenter` | Same mapped count. |
| Installment total | Sum `TblContractInstallments.installValue` | Sum `PropertyContractBatch.BatchTotal` | Difference <= approved rounding tolerance. |
| Due opening receivable | Due/past installments minus true paid | Opening balance entries / renter opening balances | Must match approved amount. |
| Future receivable schedule | Future installments minus true advance paid | Future batches/remain | Must match by contract and batch. |
| Receipt evidence | `Notes.NoteType=4` | Archive/evidence only | Must reconcile to paid allocation. |
| Journal safety | Source `DOUBLE_ENTREY_VOUCHERS` | No duplicate target journals | No duplicate posting. |
| Account mapping | `TblCustemers.Account_Code` | `PropertyRenter.AccountId` | 100% mapped for migrated renters. |

### 9.2 Reconciliation Tolerances

Recommended tolerances:

| Item | Tolerance |
|---|---:|
| Counts | 0 difference |
| Contract totals | 0.01 per contract |
| Batch totals | 0.01 per batch |
| Opening balance total | 0.01 total and per renter |
| Debit/Credit equality | 0.01 per document group |
| Account mapping | 0 unmapped |

### 9.3 Required Reconciliation Keys

Every migrated row must have a legacy reference key:

| Entity | Legacy Key |
|---|---|
| Property | `Adnan.TblAqar.Aqarid` |
| Unit | `Adnan.TblAqarDetai.Id` |
| Renter | `Adnan.TblCustemers.CusID` |
| Contract | `Adnan.TblContract.ContNo` |
| Batch | `Adnan.TblContractInstallments.id` |
| Receipt evidence | `Adnan.Notes.NoteID` |
| Paid allocation | `Adnan.ContracttBillInstallmentsDone.id` |

Because several target property tables do not have dedicated legacy-id columns, the safest implementation should add a sandbox-only cross-reference table or migration metadata layer before real migration. Using free-text `Notes` alone is possible for a quick pilot, but it is weaker for reconciliation and rollback.

## 10. Dry Run Logic

### 10.1 Candidate Contract Set

Candidate set:

```sql
TblContract
WHERE ISNULL(EndContract,0)=0
AND NOT EXISTS settlement in TblFiterWaiver
AND (EndDate IS NULL OR EndDate >= '20260520')
```

### 10.2 True Paid Calculation

Candidate source:

```sql
ContracttBillInstallmentsDone
LEFT JOIN Notes ON Notes.NoteID = ContracttBillInstallmentsDone.NoteID
WHERE Notes.NoteType = 4 OR Notes.NoteType IS NULL
```

True paid is the sum of paid component fields, not `TblContractInstallments.payed`.

### 10.3 Opening Balance Candidate

For each strict-active contract:

```text
DueOpeningRemain = SUM(installValue - TruePaid)
where Installdate <= CutoverDate
```

For the current assessment date:

`DueOpeningRemain = 1,156,544.66`

### 10.4 Future Batch Candidate

For each strict-active contract:

```text
FutureRemain = SUM(installValue - TruePaid)
where Installdate > CutoverDate
```

For the current assessment date:

`FutureRemain = 19,178,805.8185`

Future installments should be migrated as contract batches, not as opening balance.

## 11. DynamicErp Changes Needed Before Real Pilot Inserts

The following changes or configuration decisions are needed before execution:

| Requirement | Why |
|---|---|
| Dedicated sandbox connection profile | Prevent accidental writes to `Alromaizan`. |
| Cross-reference mechanism | Required for old-to-new ids and reconciliation. |
| Account seeding/mapping for active renters | Currently 0 active renter account codes match target chart. |
| Department/account configuration validation | Property procedures depend on department revenue/due accounts. |
| Import mode that suppresses duplicate journal posting | Avoid double-posting old receipts and opening balances. |
| Opening balance source type or flag | Needed to distinguish migration balance from normal receipt. |
| Archive access design for old `Notes`/settlements | Users will need historical visibility without native posting. |
| Validation screen/report | Compare VB6 totals to DynamicErp totals before Go Live. |
| Blocker handling policy for 10 bad-link contracts | Exclude, fix in source, or map manually. |

## 12. Failure Points

| Failure Point | Probability | Impact | Mitigation |
|---|---|---|---|
| Missing target accounts | High | Critical | Seed/map accounts in sandbox first. |
| Duplicate unit display numbers | Medium | High | Use old `TblAqarDetai.Id`, not `unitno`, as identity. |
| Importing expired unresolved contracts as active | High if not filtered | High | Use strict active definition only. |
| Double-posting accounting | Medium | Critical | Opening balance only, archive old journals. |
| Receipt allocation mismatch | Medium | High | Reconcile `Notes` vs `ContracttBillInstallmentsDone`. |
| Missing links in 10 contracts | Certain | Medium | Block or manually fix before pilot. |
| VAT component mismatch | Medium | High | Use batch totals and VAT fields as validation; do not reconstruct full VAT history now. |
| Target procedures auto-create journal entries | Medium | Critical | Use import-safe mode or direct controlled sandbox scripts after design approval. |

## 13. Pilot Scenarios

### Scenario A - Recommended Safe Pilot

- Create sandbox clone of `Alromaizan`.
- Seed/map accounts for the 256 active renters.
- Exclude 10 missing-link contracts initially.
- Import 283 structurally valid strict-active contracts.
- Create opening balance for due/past unpaid only.
- Keep old receipts and journals archive-only.
- Run reconciliation and UI smoke tests.

Expected success after prerequisites: `82% - 88%`.

### Scenario B - Strict Active Including Manual Fixes

- Same as Scenario A.
- Manually resolve the 10 missing-link contracts before import.
- Import all 293 strict-active contracts.

Expected success after prerequisites and manual fixes: `85% - 90%`.

### Scenario C - Include 1,231 Operational Active Contracts

Not recommended now.

This would include 938 date-expired contracts and would likely distort operational screens and reports.

Expected success without major cleanup: below `60%`.

## 14. Final Recommendation

Do not start actual migration inserts yet.

Start the next implementation phase only after approving these prerequisites:

1. Create/restore a sandbox clone of `Alromaizan` under a separate DB name.
2. Build account mapping/seeding for the 256 active renters from `Adnan.ACCOUNTS` to sandbox `ChartOfAccount`.
3. Add or approve a cross-reference design for legacy ids.
4. Decide how to handle the 10 strict-active contracts with missing links.
5. Approve opening balance accounting treatment so old receipt/journal history is not double-posted.
6. Approve archive-only handling for historical `Notes`, `DOUBLE_ENTREY_VOUCHERS`, and settlements.

### Decision

We can begin building the sandbox migration tooling, but we should not run data inserts until the sandbox database and account mapping strategy are approved.

Practical confidence:

| Condition | Confidence |
|---|---:|
| Without account mapping and cross-reference changes | 55% |
| With sandbox, account mapping, blocker exclusions, and reconciliation | 82% |
| With manual fixes for all 10 missing-link contracts | 88% |

The data is suitable for a controlled Pilot Migration, but only under the Hybrid strategy and only in a sandbox clone. The current blockers are manageable; the accounting strategy is the part that must be protected most carefully.

## 15. Generated Artifacts

| File | Purpose |
|---|---|
| `Docs\PropertyPilotMigrationDiagnostics_Adnan_SELECT_ONLY_20260520.sql` | SELECT-only diagnostics for Adnan pilot. |
| `Docs\PropertyPilotMigrationMapping_Adnan_20260520.md` | Final Adnan-specific mapping table. |
| `Docs\PropertyPilotMigration_Artifacts_20260520\AdnanPilot_01_active_contract_summary.csv` | Active-contract classification summary. |
| `Docs\PropertyPilotMigration_Artifacts_20260520\AdnanPilot_02_opening_balance_by_bucket.csv` | Opening balance/future split. |
| `Docs\PropertyPilotMigration_Artifacts_20260520\AdnanPilot_03_active_contract_balances.csv` | Contract-level dry-run balances. |
| `Docs\PropertyPilotMigration_Artifacts_20260520\AdnanPilot_04_blockers_missing_links.csv` | Missing-link blockers. |
| `Docs\PropertyPilotMigration_Artifacts_20260520\AdnanPilot_05_account_mapping_validation.csv` | Account mapping validation against target chart. |
