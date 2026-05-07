# MainErp Connection And Run Modes

## Purpose

DynamicErp now hosts three separate runtime areas that must keep clear database boundaries:

- the original large web application;
- Kishny/POS under `Areas\Pos`;
- MainErp migration under `Areas\MainErp`.

MainErp must be able to run against a full legacy database such as `Eng` without changing the original web database connection and without changing POS/Kishny behavior.

## Connection Strings

| Connection name | Module | Purpose |
| --- | --- | --- |
| `MyERP_ConnectionString` | Original large web application | Existing default web database connection. Keep unchanged. |
| `KishnyCashConnection` and existing POS/Kishny settings | Kishny/POS | Existing POS/Kishny database behavior. Keep unchanged. |
| `MainErp_ConnectionString` | MainErp migration | Dedicated MainErp legacy database connection. Local test database is `Eng`. |

## Current Web.config Shape

```xml
<connectionStrings>
  <add name="KishnyCashConnection"
       connectionString="Data Source=Wael\Sql2019;Initial Catalog=Cash;User ID=sa;Password=Admin@123;MultipleActiveResultSets=False;TrustServerCertificate=True"
       providerName="System.Data.SqlClient" />

  <add name="MainErp_ConnectionString"
       connectionString="Data Source=Wael\Sql2019;Initial Catalog=Eng;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True"
       providerName="System.Data.SqlClient" />

  <add name="MyERP_ConnectionString"
       connectionString="Data Source=Wael\Sql2019;Initial Catalog=MyErp;User ID=sa;Password=Admin@123;MultipleActiveResultSets=True;TrustServerCertificate=True"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

## MainErp Connection Factory

MainErp uses:

- `Areas\MainErp\Infrastructure\MainErpDbConnectionFactory.cs`

Behavior:

1. Read `MainErp_ConnectionString`.
2. If it is missing or empty, write the warning:
   `MainErp_ConnectionString not found; falling back to MyERP_ConnectionString.`
3. Fall back to `MyERP_ConnectionString`.
4. If both are missing, throw a configuration error.

This fallback exists only to avoid breaking older local setups. The preferred and documented MainErp mode is always `MainErp_ConnectionString`.

## Running Modes

### A) Original Big Web

- Uses `MyERP_ConnectionString`.
- Default routes remain unchanged.
- Does not use `MainErp_ConnectionString`.

Test:

- Run the site normally.
- Open existing non-MainErp routes.

### B) Kishny/POS

- Uses existing POS/Kishny connection configuration.
- Route area: `/Pos`
- No MainErp connection string is used by `Areas\Pos`.

Test:

- Open `/Pos`.
- Confirm existing POS login and POS pages behave as before.

### C) MainErp Migration

- Uses `MainErp_ConnectionString`.
- Route area: `/MainErp`
- Local test database should be `Eng`.

Test:

- Open `/MainErp`.
- Open `/MainErp/LC`.
- Open `/MainErp/ProjectExtracts`.
- Open `/MainErp/AccountingReports/JournalEntries`.
- Open `/MainErp/AccountingReports/AccountMovement`.
- Open `/MainErp/SalesReports/SalesSummary`.

Authenticated UI testing requires a valid web login session because MainErp controllers inherit `[Authorize]`.

## Switching MainErp To Another Legacy Database

Change only `MainErp_ConnectionString`:

```xml
<add name="MainErp_ConnectionString"
     connectionString="Data Source=SERVER_NAME;Initial Catalog=LEGACY_DB_NAME;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True"
     providerName="System.Data.SqlClient" />
```

Do not change:

- `MyERP_ConnectionString` unless testing the original web;
- POS/Kishny connection strings;
- POS SQL scripts;
- `AllScripts.sql`.

## Safety Notes

- MainErp code must use `MainErpDbConnectionFactory` or services/repositories that receive `IMainErpDbConnectionFactory`.
- `Areas\Pos` must not reference `MainErp_ConnectionString`.
- Original web controllers must not be changed to use `MainErp_ConnectionString`.
- Shared helpers must not hide connection selection; module-specific boundaries must stay visible.
- `AllScripts.sql` remains untouched unless a future database migration explicitly requires it and documents the change.
