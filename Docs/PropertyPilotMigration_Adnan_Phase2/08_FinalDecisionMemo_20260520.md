# Adnan Property Pilot Migration - Phase 2 Final Decision Memo - 2026-05-20

## 1. Decision Summary

Phase 2 tooling has been prepared for review, but no real migration has been executed.

The tools are now ready for controlled review and can be used for the first Sandbox Dry Run only after explicit approval to create/restore a sandbox database and approve account seeding/mapping.

Recommended first execution scope:

`283 structurally valid strict-active contracts only`

Do not include the 10 bad-link contract rows in the first Dry Run.

## 2. Files Created

| File | Purpose |
|---|---|
| `01_SandboxSetup_DRAFT_20260520.sql` | Draft-only sandbox clone instructions. |
| `02_CrossReference_DRAFT_20260520.sql` | Sandbox-only Cross Reference and metadata tables. |
| `03_AccountMappingDiagnostics_SELECT_ONLY_20260520.sql` | Read-only diagnostics for active renter account mapping. |
| `04_MissingContractLinks_SELECT_ONLY_20260520.sql` | Read-only diagnostics for the 10 missing-link contracts. |
| `05_MigrationProcedures_DRAFT_SANDBOX_ONLY_20260520.sql` | Draft stored procedures for sandbox migration. |
| `06_Reconciliation_SELECT_ONLY_20260520.sql` | Read-only post-migration reconciliation framework. |
| `07_Rollback_DRAFT_SANDBOX_ONLY_20260520.sql` | Batch-based sandbox rollback draft. |
| `PropertyPilotMigration_Adnan_Phase2_Plan_20260520.md` | Full Phase 2 plan. |

## 3. Safety Status

| Item | Status |
|---|---|
| `Adnan` writes | Not performed. |
| `Alromaizan` writes | Not performed. |
| Sandbox DB creation | Not performed. Draft only. |
| DDL/DML scripts | Written as files only, not executed. |
| SELECT diagnostics | Safe/read-only. |
| Production guard | Included in DRAFT scripts. |

Draft scripts that contain `CREATE`, `DROP`, `INSERT`, `UPDATE`, or `DELETE` are explicitly marked as Sandbox-only and contain database-name guards.

## 4. Account Mapping Decision

Recommended option:

`Option A - Seed missing renter accounts inside the Sandbox chart of accounts`

Reason:

- 256 active renters have valid old account codes in `Adnan`.
- 0 of those account codes currently exist in `Alromaizan.ChartOfAccount`.
- DynamicErp property accounting depends on `PropertyRenter.AccountId`.
- Manual mapping 256 accounts to unrelated existing accounts would be slower and riskier.
- Sandbox seeding preserves renter-level statements and enables reliable reconciliation.

Approval required:

- Parent account in `ChartOfAccount` for seeded renter accounts.
- Account `TypeId`, `ClassificationId`, and `CategoryId` policy.
- Whether seeded codes should preserve old `Account_Code` exactly or receive a prefix.

## 5. Missing Contract Link Decision

The 10 problematic contract rows have blank/null critical fields including property, unit, renter, and dates.

Affected old contract numbers:

`331`, `689`, `690`, `721`, `805`, `945`, `946`, `947`, `1626`, `1716`

Recommended decision for all 10:

`Exclude From Pilot + Archive Only`

Reason:

- They cannot test normal property workflow.
- They cannot be mapped safely without manual business evidence.
- Auto-fixing them would create artificial contracts in the target system.
- They do not have enough core data to support accounting confidence.

## 6. First Dry Run Recommendation

Start with:

`283 structurally valid strict-active contracts`

Do not wait to fix the 10 bad-link contracts before the first Dry Run. They should be handled in a separate exception-resolution pass after the core pipeline proves itself.

Why:

- The 283 contracts are enough to test properties, units, tenants, contracts, batches, opening balances, and reconciliation.
- The 10 bad-link rows are not representative and will slow down the first proof of migration quality.
- Excluding them gives a cleaner first signal.

## 7. Most Dangerous Point Before First Dry Run

The highest-risk item is:

`Account Mapping / Account Seeding`

If accounts are wrong, then contracts may appear operational but renter balances, opening balances, receipts, statements, and future accounting will be unreliable.

Second highest risk:

`Opening Balance posting design`

The prepared procedure stages opening balances only. It does not create final accounting postings because that requires approval of the target accounting treatment.

## 8. What Needs Approval From You

1. Approve creating/restoring sandbox DB: `Alromaizan_PropertyPilot_Adnan_20260520`.
2. Approve Option A account seeding in sandbox.
3. Provide or approve the parent account and account metadata for seeded renter accounts.
4. Approve excluding the 10 bad-link contracts from first Dry Run.
5. Confirm cutover date remains `2026-05-20`.
6. Decide opening balance target treatment:
   - renter opening fields,
   - journal entry,
   - dedicated migration opening-balance staging only for first test.
7. Confirm old `Notes`, `DOUBLE_ENTREY_VOUCHERS`, and `TblFiterWaiver` remain archive-only in the first Dry Run.

## 9. Go / No-Go

Current decision:

`Go for Sandbox Tool Review`

Not yet Go for execution.

Execution should start only after the sandbox DB exists and the account-seeding decision is approved.

## 10. Practical Next Step

Recommended next step after approval:

1. Create/restore sandbox clone.
2. Run `02_CrossReference_DRAFT_20260520.sql` inside sandbox.
3. Run `03_AccountMappingDiagnostics_SELECT_ONLY_20260520.sql` and approve parent account.
4. Run account seeding procedure in sandbox only.
5. Run tenant/property/unit/contract migration procedures in order.
6. Run opening balance staging procedure.
7. Run `06_Reconciliation_SELECT_ONLY_20260520.sql`.
8. Open DynamicErp against sandbox connection string and smoke-test property screens.

## 11. Final Recommendation

The tools are ready as a controlled Sandbox migration kit, not as production scripts.

Start with the 283 valid contracts. Keep the 10 broken contract rows excluded/archived until after the first successful Dry Run and reconciliation.
