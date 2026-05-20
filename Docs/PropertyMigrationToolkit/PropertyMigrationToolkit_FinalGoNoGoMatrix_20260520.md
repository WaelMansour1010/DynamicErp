# PropertyMigrationToolkit Final Go/No-Go Matrix - 2026-05-20

| Case | Allowed | Forbidden | Notes |
|---|---|---|---|
| ReadyToTest | Yes |  | Clone only, after reconciliation |
| Clone Pilot | Yes |  | Preferred execution mode |
| Finance Approved Pilot | Yes |  | Accounting scope only after finance approval |
| Mini Accounting Pilot | Yes |  | Proven for RSMDB CashingType=8 scope |
| Full Production Migration | Conditional |  | Requires signed GoLive plan and final validation |
| No Backup |  | Yes | Execute blocked |
| No Execution Plan Approval |  | Yes | Execute blocked |
| Empty BatchId in Execute |  | Yes | Execute blocked |
| No Reconciliation |  | Yes | No delivery allowed |
| AccountId=NULL |  | Yes | Critical blocker |
| Unbalanced Journal |  | Yes | Critical blocker |
| Weak Match Posting |  | Yes | Must remain review/blocked |
| Blocked Record Posting |  | Yes | Must remain blocked |
| Suspense without tracking |  | Yes | Must be explicit and reported |
| Owner Payments without review |  | Yes | Requires separate finance/business review |
| 9088 without classification |  | Yes | Must be classified first |
| Terminations without validation |  | Yes | Needs dedicated termination pilot |
| Source DB modification |  | Yes | Source is read-only |
| Production-looking target name |  | Yes | Runner blocks live/production markers |
