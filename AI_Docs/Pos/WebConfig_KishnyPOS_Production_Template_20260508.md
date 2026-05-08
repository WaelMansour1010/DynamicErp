# Web.config Kishny POS Production Template - 2026-05-08

Use this as a checklist/snippet source. Do not paste real passwords into source control or docs.

```xml
<connectionStrings>
  <clear />
  <add name="KishnyCashConnection"
       connectionString="Data Source=<SQL_SERVER>;Initial Catalog=<KISHNY_POS_DB>;User ID=<POS_SQL_USER>;Password=<POS_SQL_PASSWORD>;MultipleActiveResultSets=False;TrustServerCertificate=True;Max Pool Size=200;Min Pool Size=10;Connect Timeout=30;Pooling=True"
       providerName="System.Data.SqlClient" />
  <add name="MyERP_ConnectionString"
       connectionString="Data Source=<SQL_SERVER>;Initial Catalog=<KISHNY_POS_DB>;User ID=<POS_SQL_USER>;Password=<POS_SQL_PASSWORD>;MultipleActiveResultSets=True;TrustServerCertificate=True;Max Pool Size=200;Min Pool Size=10;Connect Timeout=30;Pooling=True"
       providerName="System.Data.SqlClient" />
  <!-- MainErp must remain disabled/unused for Kishny POS production. -->
  <add name="MainErp_ConnectionString"
       connectionString="Data Source=<DISABLED>;Initial Catalog=<DISABLED>;Integrated Security=False"
       providerName="System.Data.SqlClient" />
</connectionStrings>

<appSettings>
  <add key="EnableKishnyPos" value="true" />
  <add key="EnableMainErpMigration" value="false" />
  <add key="EnableDevStart" value="false" />
  <add key="EnableDevMasterPassword" value="false" />
  <add key="DevMasterPassword" value="" />
  <add key="DebugKYC" value="false" />
  <add key="EnableDebugDatabaseSelector" value="false" />
  <add key="EnableRunModeSelector" value="false" />
  <add key="PosSessionRestoreEnabled" value="true" />
  <add key="PosHealthDashboardEnabled" value="false" />
</appSettings>

<system.web>
  <compilation targetFramework="4.8" debug="false" />
  <!-- Preserve the deployed customer machineKey unless intentionally rotating sessions. -->
  <machineKey validationKey="<KEEP_EXISTING_CUSTOMER_VALIDATION_KEY>"
              decryptionKey="<KEEP_EXISTING_CUSTOMER_DECRYPTION_KEY>"
              validation="HMACSHA256"
              decryption="AES" />
  <httpCookies httpOnlyCookies="true" requireSSL="true" sameSite="Lax" />
  <sessionState mode="InProc" timeout="60" cookieless="UseCookies" />
  <customErrors mode="RemoteOnly" />
</system.web>

<system.webServer>
  <modules runAllManagedModulesForAllRequests="true">
    <remove name="FormsAuthentication" />
  </modules>
  <!-- Preserve required binding redirects and handlers from current customer Web.config. -->
</system.webServer>
```

## Required Route/Access Behavior
- `/Pos/Login`, `/Pos`, and approved POS routes only.
- Block or remove `/DevStart`.
- Block or remove `/RunMode`.
- Block or remove `/MainErp`.
- Hide MainErp/sidebar/payment/cashing/excel import links unless explicitly approved.

## Binding Redirect Reminder
Copy binding redirects from the current working customer deployment and from the clean build output. Do not remove DevExpress/Crystal/runtime redirects without testing reports and POS print.
