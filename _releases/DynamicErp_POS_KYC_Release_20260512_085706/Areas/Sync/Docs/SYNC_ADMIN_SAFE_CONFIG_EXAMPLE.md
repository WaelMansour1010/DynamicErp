# Sync Admin Safe Configuration Example

Use this shape for local/test deployments. It contains no SQL password.

```xml
<connectionStrings>
  <add name="SyncAdminConnection"
       connectionString="Data Source=SERVER\INSTANCE;Initial Catalog=SyncAdminTest;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True"
       providerName="System.Data.SqlClient" />
</connectionStrings>

<appSettings>
  <add key="Sync.ConnectionStringName" value="SyncAdminConnection" />
  <add key="Sync.AdminRoles" value="SyncAdmin,Administrators" />
  <add key="Sync.Permission.Sync.View" value="SyncViewer,SyncAdmin,Administrators" />
  <add key="Sync.Permission.Sync.Diagnostics" value="SyncDiagnostics,SyncAdmin,Administrators" />
  <add key="Sync.Permission.Sync.AdminOperations" value="SyncOperator,SyncAdmin,Administrators" />
</appSettings>
```

## Production Credential Rule

Never commit real credentials. Prefer Windows Authentication with a dedicated IIS application pool identity or Windows Service identity. Grant only the Sync Admin permissions needed for the current phase.

If SQL authentication is unavoidable:

- use a least-privilege SQL login, not `sa`
- keep the password in an environment-specific transform that is not committed, or
- inject it from the deployment secret store, or
- encrypt the deployed `connectionStrings` section with ASP.NET protected configuration

The browser UI must never expose connection strings or passwords.
