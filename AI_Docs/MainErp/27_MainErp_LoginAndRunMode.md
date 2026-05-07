# MainErp Login And Run Mode

Date: 2026-05-07

## Purpose

MainErp now has its own login/start flow so it can load legacy ERP defaults from the MainErp database instead of relying only on the original web login.

## Routes

| Route | Purpose |
| --- | --- |
| `/DevStart` | DEBUG/local startup selector |
| `/RunMode` | alias for the DEBUG/local startup selector |
| `/` | DEBUG/local selector; outside DEBUG/local it redirects to the previous POS root behavior |
| `/MainErp/Login` | MainErp-specific login |
| `/MainErp` | MainErp home; redirects to `/MainErp/Login` when MainErp context is missing |
| `/Pos` | Kishny/POS unchanged |
| `/Home/Index` | original big MyErp web entry |

## MainErp Login

Controller:

- `Areas\MainErp\Controllers\LoginController.cs`

Service:

- `Areas\MainErp\Services\Security\MainErpLoginService.cs`

Connection:

- Always uses `MainErp_ConnectionString` through `MainErpDbConnectionFactory`.
- Does not use `KishnyCashConnection`.
- Does not use POS context or `POSCTX`.

## Loaded Defaults

MainErp login reads from `TblUsers` and stores:

- `UserId`
- `UserName`
- `EmpId`
- `BranchId`
- `StoreId`
- `BoxId`
- `PaymentNetId` if the column exists
- `UserType`
- `IsAdmin`

It also attempts safe read-only display lookups:

- employee name from `TblEmployee`
- branch name from `TblBranchesData`
- store name from `TblStore`
- box name from `TblBoxesData`

If optional lookup tables are missing, login does not fail because those names are not required for the first safe migration phase.

## Session Keys

Defined in:

- `Areas\MainErp\Security\MainErpSessionKeys.cs`

Keys:

- `MainErp.UserContext`
- `MainErp.UserId`
- `MainErp.UserName`
- `MainErp.EmpId`
- `MainErp.BranchId`
- `MainErp.StoreId`
- `MainErp.BoxId`
- `MainErp.Debug.DatabaseName`

## Master Password

Supported only through config:

```xml
<add key="EnableDevMasterPassword" value="true" />
<add key="DevMasterPassword" value="Alex2025" />
```

Rules:

- Existing active `TblUsers` user must be found first.
- Disabled/deactivated users do not login.
- Normal password still works.
- `Alex2025` works only when `EnableDevMasterPassword=true`.
- Wrong username plus master password fails.

## DEBUG Startup Selector

Controller:

- `Controllers\DevStartController.cs`

View:

- `Views\DevStart\Index.cshtml`

The selector is available only when:

- compiled in `DEBUG`,
- request is local.

It shows:

- Kishny/POS route and `KishnyCashConnection`
- original web route and `MyERP_ConnectionString`
- MainErp route and `MainErp_ConnectionString`

The original web card points to `/Home/Index` because `/` is now reserved for the DEBUG/local selector while preserving the previous POS root redirect outside DEBUG/local.

## Debug MainErp Database Override

Implementation:

- `Areas\MainErp\Infrastructure\MainErpDebugDatabaseOverride.cs`

Behavior:

- DEBUG/local only.
- Stores selected MainErp database in session.
- Does not write Web.config.
- Production ignores the override.
- Only `MainErp_ConnectionString` is affected.

Suggested local databases:

- `Eng`
- `MyErp`
- `Cash`

## Safety

- POS login remains unchanged.
- POS still uses `KishnyCashConnection`.
- Original web still uses `MyERP_ConnectionString`.
- MainErp uses `MainErp_ConnectionString` or the debug-only MainErp database override.
- No database schema changes were made.
- `AllScripts.sql` was not modified.
