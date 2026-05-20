# Property Migration Toolkit - Safety Rules
Date: 2026-05-20

## Mandatory Rules
- No migration without clone.
- No migration without backup.
- No migration without approved config.
- No migration without `MigrationBatchId`.
- No migration without CrossReference / EntityMap.
- No Go Live without reconciliation.
- No Go Live if `AccountId=NULL` exists in any active journal detail.
- No Go Live if any active journal is unbalanced.
- No Go Live if migrated active contracts lack unit or tenant.
- No Go Live if payment methods are not mapped and validated.
- No Go Live if cashbox/bank account links are missing.
- No Go Live if owner payment semantics are unclear.
- Never modify source DB.
- Never migrate users/passwords from legacy VB6.
- Never run generic scripts on production unless explicitly reviewed and approved.

## Guard Requirements
Every write script must:
- Block known source/reference DB names.
- Require safe target DB name marker.
- Require BatchId.
- Use transaction and TRY/CATCH.
- Be idempotent as much as practical.
- Log exclusions and warnings.

## Accounting Gates
- Account mapping must be complete before posting journals.
- Journal debit and credit must balance per document.
- No detail line can have null account.
- Same debit/credit account pairs must be blocked unless a specific approved scenario exists.
- Historical journals are not default; only linked approved voucher journals may migrate.
