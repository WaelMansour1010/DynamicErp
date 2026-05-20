# Property Migration Toolkit - Execution Safety
Date: 2026-05-20

## Runner Safety Rules
The Runner must block execution when:
- Target DB name does not look like a clone/sandbox/ReadyToTest.
- Target DB equals source/reference/production names.
- Backup evidence is missing.
- BatchId is missing.
- Config is not approved for migration stages.
- Previous stage has critical unresolved errors.
- Accounting reconciliation fails.

## Modes
- Dry Run: discovery/diagnostics/mapping simulation only.
- ReadyToTest: writes to clone with warnings/review queue.
- Rollback: removes selected BatchId artifacts with explicit confirmation.
- Production: future only, requires separate approvals and strict gates.

## Production Protection
Production execution should require:
- Explicit production flag.
- Fresh backup timestamp.
- Signed reconciliation from clone.
- Strict mode or approved Hybrid final run.
- User confirmation typed phrase.
