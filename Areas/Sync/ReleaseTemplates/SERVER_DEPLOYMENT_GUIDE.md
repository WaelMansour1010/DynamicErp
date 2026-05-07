# Data Sync Pilot - Server Deployment Guide

## Scope

This package deploys the central Sync Admin Area and branch ingestion API for a controlled customer pilot.

It does not enable ApplyMode, batch apply, automatic invoice insert, or production credentials.

## Copy Files

Copy `Server/Files/Areas/Sync` into the ERP web application under:

```text
<ERP Web Root>\Areas\Sync
```

Copy `Server/Files/bin/MyERP.dll` into:

```text
<ERP Web Root>\bin
```

Use the normal ERP deployment process if the site is compiled/published by CI.

## SQL

Run scripts manually, in order, only after approval:

1. `001_CreateSyncSchema.sql`
2. `002_Sync_AdminOperations.sql`
3. `003_Sync_BranchIngestion.sql`
4. `004_Sync_BranchAgentHardening.sql`
5. Optional verification: `005_CheckSyncSchema.sql`

Do not run scripts against production during this pilot unless explicitly approved.

## Web.config

Use `Server/Config/Server_Config_Template.config` as a safe example.

Requirements:

- `SyncAdminConnection` must point to the pilot/test Sync admin database.
- Prefer Windows Integrated Security.
- Do not store plaintext SQL passwords.
- Keep `Sync.BranchApiRequireSignature=true`.
- Configure branch API tokens through environment variables, not Web.config.

## IIS Requirements

- ASP.NET MVC application hosted under IIS.
- App Pool identity must have access to `SyncAdminConnection`.
- HTTPS is strongly recommended for branch API traffic.
- Do not expose central SQL credentials to branch machines.

## Verification

After deployment:

- `/sync` opens.
- `/sync/queue` opens.
- `/sync/diagnostics` opens.
- `/sync/logs` opens.
- `/sync/profiles` opens read-only.
- `/sync/pilot` opens.
- Signed `POST /sync/api/branch/heartbeat` is accepted.
- Signed `POST /sync/api/branch/outbox` inserts only `Sync_Outbox` rows.
- `POST /sync/apply/requestapply` returns HTTP 403.
- No destination invoice rows are inserted automatically.
