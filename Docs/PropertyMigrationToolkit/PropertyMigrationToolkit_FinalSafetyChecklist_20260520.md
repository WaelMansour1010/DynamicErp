# PropertyMigrationToolkit Final Safety Checklist - 2026-05-20

## Hard Blockers

| Rule | Status |
|---|---|
| Do not execute on RSMDB, Adnan, Alromaizan, MyErp | Enforced |
| Target must contain Clone/Sandbox/PropertyPilot/ReadyToTest/PilotClone/Migration | Enforced |
| Target must not contain Production/GoLive/Live markers | Enforced |
| Source and target cannot be the same database | Enforced |
| Execute requires BackupVerified=true | Enforced |
| Execute requires ExecutionPlanApproved=true | Enforced |
| Execute requires explicit BatchId | Enforced |
| AccountId=NULL blocks accounting | Enforced in validation/templates |
| Unbalanced journals block accounting | Enforced in validation/templates |
| Weak/Blocked records cannot be posted | Required by templates and pilot scope |
| Suspense requires explicit tracking/sign-off | Required |
| Owner payments require explicit review | Required |

## Before Execute

- Confirm clone name and source name.
- Confirm backup or clone creation evidence.
- Confirm BatchId and CustomerCode.
- Confirm finance approvals where accounting is included.
- Confirm no open critical diagnostics.
- Confirm rollback script exists for the target stage.

## After Execute

- Run reconciliation.
- Verify AccountId=NULL = 0.
- Verify unbalanced journals = 0.
- Verify no duplicate posting.
- Verify web screens for migrated entities.
- Decide keep ReadyToTest data or rollback.

## Production Rule

Production migration is not a default Runner capability. It requires a separate signed GoLive plan, final clone validation, finance sign-off, backup, rollback plan, and business approval.
