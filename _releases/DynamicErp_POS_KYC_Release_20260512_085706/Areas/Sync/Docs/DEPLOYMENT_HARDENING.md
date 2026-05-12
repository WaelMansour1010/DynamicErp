# Sync Enterprise Operations Platform - Deployment Hardening

## Scope

The MVC area is read-only for ApplyMode. It reads `Sync_*` tables and optional `Sync_Admin*` audit tables. It does not run PowerShell, does not start the sync runner, and does not perform batch apply.

## IIS Requirements

- ASP.NET MVC 5 on .NET Framework 4.8.
- HTTPS only for admin access.
- Windows Authentication or the ERP-approved authentication provider.
- Restrict `/sync` to admin roles mapped through `Sync.AdminRoles` and `Sync.Permission.*` app settings.

## Connection Strings

Set `Sync.ConnectionStringName` to a connection string that points to the test/central database containing the sync schema.

Preferred local/test example using Windows Integrated Security:

```xml
<connectionStrings>
  <add name="SyncAdminConnection"
       connectionString="Data Source=SERVER\INSTANCE;Initial Catalog=SyncAdminTest;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True"
       providerName="System.Data.SqlClient" />
</connectionStrings>

<appSettings>
<add key="Sync.ConnectionStringName" value="SyncAdminConnection" />
<add key="Sync.AdminRoles" value="SyncAdmin,Administrators" />
</appSettings>
```

Do not store plaintext production secrets in source control.

If SQL authentication is unavoidable, keep the real secret outside committed `Web.config`:

- use an environment-specific transform that is not committed with real credentials, or
- encrypt the deployed `connectionStrings` section using ASP.NET protected configuration, or
- inject the connection string during deployment from the release system secret store.

Never commit a real `sa` password. Prefer a least-privilege SQL login or, better, a Windows service/IIS identity granted only the required Sync Admin permissions.

## Permissions

The web identity needs read permission on:

- `Sync_Outbox`
- `Sync_Inbox`
- `Sync_Log`
- `Sync_Error`
- `Sync_ObjectMap`
- `Sync_Batch`
- `Sync_Config`
- optional `Sync_AdminOperation`, `Sync_AdminAudit`, `Sync_AdminApproval`

For queued-operation testing, the web identity also needs insert permission on `Sync_AdminOperation`, `Sync_AdminAudit`, and `Sync_AdminApproval`. No permission is required on invoice tables for this phase.

## Safety

- Apply POST returns HTTP 403.
- No browser endpoint can execute PowerShell.
- No batch apply endpoint exists.
- Admin audit SQL is a draft and must not be applied without approval.

## Backup Policy

Before any future pilot apply, complete `PILOT_APPROVAL_CHECKLIST.md`, verify backups with `RESTORE VERIFYONLY`, and confirm business and technical owner approval.
