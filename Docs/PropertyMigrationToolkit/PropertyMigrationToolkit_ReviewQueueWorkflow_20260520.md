# Property Migration Toolkit - Review Queue Workflow
Date: 2026-05-20

## Workflow
1. Engine detects issue.
2. If allowed, applies fallback/autofix.
3. Creates ReviewQueue item.
4. Reviewer opens queue.
5. Reviewer accepts fallback, changes mapping, excludes record, or requests source cleanup.
6. Mark reviewed/closed.
7. Optionally rerun mapping/migration for that record.

## Queue Fields
- Warning Type.
- Severity.
- Source Record.
- Target Record.
- Original Value.
- Applied Fix.
- Suggested Action.
- Assigned To.
- Status.
- Resolution Notes.

## Statuses
| Status | Meaning |
|---|---|
| Open | Needs review |
| InReview | Assigned/reviewing |
| Resolved | Action decided |
| AcceptedFallback | Fallback accepted |
| RemapRequired | Needs mapping change |
| Excluded | Excluded by decision |
| Closed | Done |

## Priority
Critical accounting items are priority 1 and block GoLive.
Master data placeholders are usually priority 2 or 3.
