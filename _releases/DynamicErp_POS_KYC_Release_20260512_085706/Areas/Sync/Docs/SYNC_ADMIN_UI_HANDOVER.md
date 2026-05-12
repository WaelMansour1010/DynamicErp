# Sync Admin UI Phase - Final Handover

Date: 2026-05-06

## What Was Implemented

The Sync Admin UI was integrated into the DynamicErp MVC host as a read-only enterprise operations area under `/sync`.

Implemented modules:

- Dashboard for queue status, batches, profile usage, branch activity, conflict/retry indicators, and recent sensitive operations.
- Queue browser with filters for `SyncKey`, `BranchId`, `OldTransaction_ID`, status, profile, payload hash, and date range.
- Diagnostics screen for queue item review, object map preview, logs, errors, and safety checks.
- Logs screen for `Sync_Log` / `Sync_Error`.
- Profiles screen for read-only profile/config visibility.
- Pilot readiness screen with visual checklist and safety blockers.
- Admin audit screen prepared for future `Sync_Admin*` audit tables.
- Apply readiness screen that displays warnings only and keeps execution blocked.
- ERP sidebar integration under `مركز التحكم بالمزامنة`.
- Friendly Sync error page that does not expose connection strings, passwords, or raw exception text.
- Deployment, manual test, demo, and final readiness documentation.

## Available Routes

| Route | Purpose | Status |
|---|---|---|
| `/sync` | Dashboard | Read-only |
| `/sync/queue` | Queue browser and filters | Read-only |
| `/sync/diagnostics` | Diagnostics and object map preview | Read-only |
| `/sync/logs` | Logs and errors | Read-only |
| `/sync/profiles` | Profile/settings display | Read-only |
| `/sync/pilot` | Pilot readiness checklist | Read-only |
| `/sync/audit` | Admin operation history | Read-only |
| `/sync/apply` | Apply readiness warning screen | Execution blocked |
| `POST /sync/apply/requestapply` | Apply request endpoint | Always HTTP 403 |

## Safety Guarantees

- No ApplyMode execution exists in the UI.
- `POST /sync/apply/requestapply` returns HTTP `403 Forbidden`.
- No batch apply endpoint exists.
- No PowerShell execution is available from the browser.
- No direct runner invocation is available from the browser.
- No SQL scripts are applied by the UI.
- No invoice, payment, stock, VAT, notes, or accounting tables are modified by this UI.
- Queue retry/open/apply-style actions are diagnostics/readiness only or visually disabled.
- The SQL audit schema remains a draft until explicitly approved.

## Read-Only Status

The current Sync Admin UI is read-only for operational data. It reads from:

- `Sync_Outbox`
- `Sync_Inbox`
- `Sync_Log`
- `Sync_Error`
- `Sync_ObjectMap`
- `Sync_Batch`
- `Sync_Config`
- Optional future tables: `Sync_AdminOperation`, `Sync_AdminAudit`, `Sync_AdminApproval`

The UI does not write to these tables in this phase.

## SyncAdminConnection Configuration

By default, the UI looks for:

```xml
<add name="SyncAdminConnection"
     connectionString="Data Source=SERVER;Initial Catalog=DATABASE;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True"
     providerName="System.Data.SqlClient" />
```

Optional app setting:

```xml
<add key="Sync.ConnectionStringName" value="SyncAdminConnection" />
```

Recommended security settings:

```xml
<add key="Sync.AdminRoles" value="SyncAdmin,Administrators" />
<add key="Sync.Permission.Sync.View" value="SyncViewer,SyncAdmin,Administrators" />
<add key="Sync.Permission.Sync.Diagnostics" value="SyncDiagnostics,SyncAdmin,Administrators" />
<add key="Sync.Permission.Sync.Audit" value="SyncAuditor,SyncAdmin,Administrators" />
```

Do not store plaintext production passwords in source control. Use Windows Authentication or protected IIS/Windows secret management where possible.

## Known Limitations

- Admin audit tables are drafted but not applied.
- No audit writes are performed yet.
- No queued admin operation execution exists yet.
- No SignalR/live updates are enabled yet.
- Chart.js is optional; the built-in CSS bar charts are the current fallback.
- Production role mapping must be configured in the deployment environment.
- Local debug read-only access exists for development verification only; production must rely on real ERP authentication/authorization and HTTPS.

## Next Optional Phases

1. Audit tables approval/application
   - Review and approve `Areas/Sync/Sql/001_Sync_AdminAudit_Draft.sql`.
   - Apply only after explicit approval.
   - Confirm append-only behavior and retention policy.

2. Queued admin operation execution
   - Add a queue-only admin operation workflow.
   - Keep dangerous operations gated by permission, password confirmation, approval checkbox, and reason/comment.
   - Do not directly execute ApplyMode from the browser.

3. SignalR live monitoring
   - Add read-only live updates for dashboard counts, new conflicts, and batch status.
   - No execution through SignalR.
   - No automatic retry loops.

4. Controlled pilot execution after approvals
   - Requires completed pilot checklist, verified backups, business owner approval, and technical owner approval.
   - Keep `ApplySingleSyncKey` mandatory.
   - Keep `MaxInvoicesPerRun=1`.
   - Keep batch apply blocked until separately approved.
