# Adnan Property Pilot Migration - Phase 2 Plan - 2026-05-20

## 1. Scope

This phase prepares safe, repeatable Sandbox Migration tooling for `Adnan` into the main DynamicErp Web property module under:

`F:\Source Code\DynamicErp`

This phase does not execute real migration and does not modify `Adnan`, `Alromaizan`, or any production database.

Strategy:

`Hybrid Migration = Active Contracts + Opening Balance + Historical Archive + Minimal Safe Accounting`

## 2. Inputs

| File | Role |
|---|---|
| `Docs\PropertyMigrationAssessment_20260520.md` | Full architecture and VB6 logic assessment. |
| `Docs\PropertyPilotMigrationSandbox_Adnan_20260520.md` | Phase 1 Pilot analysis and Dry Run. |
| `Docs\PropertyPilotMigrationDiagnostics_Adnan_SELECT_ONLY_20260520.sql` | Existing read-only diagnostics. |
| `Docs\PropertyPilotMigrationMapping_Adnan_20260520.md` | Adnan-specific mapping. |
| `Docs\PropertyPilotMigration_Artifacts_20260520` | Dry Run CSV outputs. |

## 3. Phase 1 Confirmed Baseline

| Metric | Value |
|---|---:|
| Strict active contracts as of 2026-05-20 | 293 |
| Structurally linkable contracts | 283 |
| Contracts with missing critical links | 10 |
| Due/past opening balance candidate | 1,156,544.66 |
| Future installment candidate | 19,178,805.8185 |
| Active renters with `Account_Code` in Adnan | 256 |
| Active renter account codes already in `Alromaizan.ChartOfAccount` | 0 |

## 4. Sandbox DB Plan

Recommended sandbox database name:

`Alromaizan_PropertyPilot_Adnan_20260520`

The sandbox should be created as a clone/restore of the current `Alromaizan` reference database. Do not execute the setup script until approval is given.

Execution requirements:

1. Run backup/restore or copy-only restore manually after approval.
2. Verify the application connection string points to the sandbox DB only.
3. Run Cross Reference setup inside the sandbox only.
4. Run Account Mapping diagnostics before any migration procedure.
5. Run procedures against the sandbox only.
6. Run reconciliation before UI validation.

## 5. Cross Reference Layer

The Cross Reference layer is required because target property tables generally do not contain stable legacy id columns.

Entities to track:

| Entity | Old Key | Target Table |
|---|---|---|
| Property | `Adnan.TblAqar.Aqarid` | `Property` |
| Unit | `Adnan.TblAqarDetai.Id` | `PropertyDetail` |
| Tenant | `Adnan.TblCustemers.CusID` | `PropertyRenter` |
| Account | `Adnan.ACCOUNTS.Account_Code` | `ChartOfAccount` |
| Contract | `Adnan.TblContract.ContNo` | `PropertyContract` |
| Batch | `Adnan.TblContractInstallments.id` | `PropertyContractBatch` |
| Receipt evidence | `Adnan.Notes.NoteID` | archive / optional `CashReceiptVoucher` |
| Opening balance | Contract/cutover key | target OB document/journal if approved |

## 6. Account Mapping Recommendation

### Option A - Seed Accounts In Sandbox

Create needed renter accounts in sandbox `ChartOfAccount` under an approved parent account using the legacy `Account_Code` and `Account_Name` from `Adnan.ACCOUNTS`.

Pros:

- Preserves renter-level balances.
- Enables `PropertyRenter.AccountId` to be correctly populated.
- Best for testing DynamicErp screens and statements.
- Repeatable through Cross Reference.

Cons:

- Requires approval of parent account/type/classification in the target chart.
- Must be sandbox-only until finance approves the structure.

### Option B - Manual Mapping To Existing Accounts

Map each old renter account to an already existing target `ChartOfAccount` row.

Pros:

- Avoids creating chart accounts.
- Useful if finance wants consolidation.

Cons:

- Current diagnostics show 0 direct matches.
- Manual mapping for 256 active renters is slow and error-prone.
- Consolidation can hide renter balances and break statements.

### Recommendation

Use Option A for Sandbox Pilot.

Reason: the Pilot goal is to test real property operation and renter balances. Without seeded renter accounts, the screens may load but accounting and balances will be invalid. Option B can be revisited before production if finance wants a different chart design.

## 7. Missing Contract Link Policy

The 10 problematic contracts have blank/null critical fields in the source contract rows: property, unit, renter, and dates.

Default decision:

`Exclude From Pilot + Archive Only`

Do not auto-fix these contracts. They should not be included in the first Dry Run because they cannot prove operational behavior in DynamicErp.

## 8. Migration Procedure Design

Draft procedures are created in:

`05_MigrationProcedures_DRAFT_SANDBOX_ONLY_20260520.sql`

Design rules:

- SQL Server 2012 compatible.
- `DROP PROCEDURE` + `CREATE PROCEDURE` style.
- Guarded by database name: target DB must contain `PropertyPilot` or `Sandbox`.
- Explicitly blocks `Alromaizan`.
- Uses `MigrationBatchId`.
- Uses Cross Reference to prevent duplicates.
- Uses transactions and `TRY/CATCH`.
- Designed for review before execution.

## 9. Reconciliation Framework

Reconciliation must compare `Adnan` against the sandbox after migration:

- Property count.
- Unit count.
- Tenant count.
- Contract count.
- Installment totals.
- Opening balance total.
- Future batch total.
- Missing contracts.
- Unmapped accounts.
- Financial differences by contract.
- Date differences.
- Unit/renter differences.

All count differences should be zero after approved exclusions. Financial tolerance should be `0.01`.

## 10. Rollback Strategy

Rollback is batch-based only.

Rules:

1. Roll back only rows listed in `PropertyPilotCrossReference` for the current `MigrationBatchId`.
2. Delete child rows before parent rows.
3. Never delete rows without cross-reference evidence.
4. Never run if the database name is not Sandbox/PropertyPilot.
5. Never run on `Alromaizan`.
6. Keep migration batch and validation logs unless explicitly approved to purge.

## 11. Approval Needed Before First Dry Run

1. Permission to create/restore `Alromaizan_PropertyPilot_Adnan_20260520`.
2. Confirm account parent/type/classification for seeded renter accounts.
3. Confirm excluding the 10 bad-link contracts from first Dry Run.
4. Confirm cutover date: currently assumed `2026-05-20`.
5. Confirm whether opening balances should be stored on renter opening fields, journal entries, or a dedicated migration opening-balance document.
6. Confirm whether old receipts/journals remain archive-only in the first Dry Run.

## 12. Recommendation

The tools are suitable for review and can become executable against Sandbox after approval. The safest first Dry Run should migrate the 283 structurally valid strict-active contracts, not the 10 bad-link contracts.

Most critical risk before first Dry Run:

`Account Mapping / Account Seeding`

Without resolving target accounts, the migration may create visible property records but will not be financially valid.
