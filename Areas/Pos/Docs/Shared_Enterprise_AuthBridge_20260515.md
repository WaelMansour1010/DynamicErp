# Shared Enterprise Auth Bridge - 2026-05-15

## Issue

Opening shared enterprise screens from Kishny POS redirected the user to the MainErp login page, even when the POS user was already authenticated.

Affected examples:

- Items: `/MainErp/Items`
- StockTaking: `/MainErp/Stocktaking`
- Screen Permissions: `/MainErp/Permissions`
- System Options: `/MainErp/Options`
- Branches: `/MainErp/Branches`
- Master Data Import / Excel ERP import: `/MainErp/MasterDataImport`
- POS Integration Settings remains POS-native: `/Pos/PosLegacyAdmin/BranchesData`

## Root Cause

POS and MainErp were in the same web application, but they used different session keys:

- POS session: `PosUserContext`
- POS persistent auth cookie: `POSCTX`
- MainErp session: `MainErp.UserContext`

`MainErpControllerBase` only accepted `MainErp.UserContext`. When a POS-authenticated user navigated directly to a shared MainErp route, MainErp did not restore a MainErp context from the existing POS session/cookie and redirected to `/MainErp/Login`.

This was not an IIS virtual-directory or separate app-pool issue in the tested local app. The `POSCTX` cookie already uses `Path=/`, so it is readable by MainErp requests inside the same application.

## Fix Applied

Added a MainErp boundary bridge:

- Restores the active POS context using `PosLoginController.RestorePosContext`.
- Maps the POS user to an active MainErp user with the same `UserID` using `MainErpLoginService.GetUserDefaults`.
- Creates the normal MainErp session keys.
- Leaves all existing MainErp permission checks intact.
- Returns `403` for a POS user that cannot be mapped to an active MainErp user instead of redirecting to another login prompt.

POS shared menu links now add `fromPos=1` so MainErp deliberately refreshes the MainErp context from the active POS identity when navigation starts in POS.

## Auth And Session Flow

1. User logs in to POS.
2. POS stores `PosUserContext` and writes the protected `POSCTX` cookie.
3. User opens a shared enterprise link from POS, for example `/MainErp/Items?fromPos=1`.
4. `MainErpControllerBase` detects POS-origin navigation or missing MainErp context.
5. `MainErpPosSessionBridge` restores POS context.
6. MainErp loads the matching active MainErp user from the current MainErp database.
7. MainErp session keys are populated:
   - `MainErp.UserContext`
   - `MainErp.UserId`
   - `MainErp.UserName`
   - `MainErp.EmpId`
   - `MainErp.BranchId`
   - `MainErp.StoreId`
   - `MainErp.BoxId`
8. The requested controller action continues and applies its normal screen permission rules.

## Database Context

The bridge does not switch POS to MainErp or MainErp to POS.

- POS session restore uses POS repository/context.
- MainErp user mapping uses `MainErpLoginService`, which uses `MainErpDbConnectionFactory`.
- The tested MainErp connection is `MainErp_ConnectionString`, configured to `Eng`.
- The POS integration settings screen remains POS-native and uses the POS context.

## Files Changed

- `Areas/MainErp/Infrastructure/MainErpPosSessionBridge.cs`
- `Areas/MainErp/Controllers/MainErpControllerBase.cs`
- `Areas/Pos/Views/PosDashboard/_Sidebar.cshtml`
- `MyERP.csproj`

## Browser Smoke Test

Tested on local IIS Express:

- POS login: `admin`
- MainErp mapped user: `admin`
- MainErp database context: `Eng` through `MainErp_ConnectionString`

| Screen | URL tested | Result | Login prompt | Console errors |
| --- | --- | --- | --- | --- |
| Items | `/MainErp/Items?fromPos=1` | Opened | No | 0 |
| StockTaking | `/MainErp/Stocktaking?fromPos=1` | Opened | No | 0 |
| Screen Permissions | `/MainErp/Permissions?fromPos=1` | Opened | No | 0 |
| System Options | `/MainErp/Options?fromPos=1` | Opened | No | 0 |
| Branches | `/MainErp/Branches?fromPos=1` | Opened | No | 0 |
| Master Data Import | `/MainErp/MasterDataImport?fromPos=1` | Opened | No | 0 |
| POS Integration Settings | `/Pos/PosLegacyAdmin/BranchesData` | Opened | No | 0 |

## Build

Visual Studio MSBuild completed successfully:

`MSBuild.exe MyERP.csproj /p:Configuration=Debug /p:Platform="AnyCPU" /v:minimal`

Existing unrelated warnings remain.

## Remaining Notes

- POS users must have a matching active MainErp `TblUsers.UserID` to open shared MainErp screens.
- If a POS user is not mapped, the system now shows controlled `403` instead of sending the user to a second login page.
- The current integration fixes authentication/session continuity. A later UX pass can wrap more shared screens in a POS shell when full native visual continuity is required.
