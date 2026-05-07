# Data Sync Pilot Verification Checklist

## Server

- Server name:
- Web root:
- App pool identity:
- SyncAdminConnection server/database:
- SQL scripts applied manually in approved order: Yes / No
- HTTPS enabled or pilot exception documented: Yes / No
- Branch token environment variable configured: Yes / No

## Server UI

- `/sync` opens: Yes / No
- `/sync/queue` opens: Yes / No
- `/sync/diagnostics` opens: Yes / No
- `/sync/logs` opens: Yes / No
- `/sync/profiles` read-only: Yes / No
- `/sync/pilot` opens: Yes / No
- `/sync/apply/requestapply` returns HTTP 403: Yes / No

## Branch Agent

- BranchId:
- Branch machine:
- Local DB server/database:
- Service account:
- Token environment variable:
- Outbox folder:
- Log folder:
- Health command result attached: Yes / No

## Pilot Steps

- Phase 1 read-only scan completed: Yes / No
- Local outbox created: Yes / No
- Phase 2 heartbeat-only completed: Yes / No
- Branch shown online in dashboard: Yes / No
- Phase 3 one payload sent: Yes / No
- Central `Sync_Outbox` row pending: Yes / No
- No destination invoice insert: Yes / No
- No ApplyMode execution: Yes / No

## Approvals

- Business owner:
- Technical owner:
- Pilot status: Approved / Blocked / Needs review
