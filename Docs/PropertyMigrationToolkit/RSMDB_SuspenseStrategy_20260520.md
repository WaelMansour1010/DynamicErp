# RSMDB Suspense Strategy - 2026-05-20

## Suspense Candidates
- Suspense/holding candidates identified: 1,595.

## Rules
- Suspense is never applied silently.
- Suspense requires finance sign-off.
- Suspense rows are tracked in PropertyMigrationSuspenseUsage.
- Suspense is not allowed for unbalanced journals or unknown debit/credit direction.
- For blocked accounts, preferred action is explicit mapping or exclusion before suspense.

## Recommended Use
Use suspense only for low-risk opening/holding differences after finance approval. Do not use suspense to force historical journal migration.
