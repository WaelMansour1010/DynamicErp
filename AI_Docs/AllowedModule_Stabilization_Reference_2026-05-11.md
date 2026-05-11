# AllowedModule Stabilization Reference (May 11, 2026)

## Original Problem
- Saving `AllowedModule` could remove module selections globally across users.
- Menu filtering (`GetNotActiveModules`) could return mixed/global data, causing cross-user leakage.
- Result: one user's module save could affect another user's menu visibility.

## Findings
- `POST /SystemSetting/AllowedModule` previously removed all rows from `AllowedModules` before insert.
- `GET /Helper/GetNotActiveModules` was not reliably scoped to current user in previous behavior.
- `_Layout.cshtml` uses `/Helper/GetNotActiveModules` response to show/hide side menu module items.

## How Menu Visibility Works Today
1. User opens any page using `[Views/Shared/_Layout.cshtml](f:\Source Code\DynamicErp\Views\Shared\_Layout.cshtml)`.
2. Layout JS calls `GET /Helper/GetNotActiveModules`.
3. Returned module IDs are matched against hidden input values in `#side-menu li`.
4. Matching module items are shown; non-matching are hidden (dashboard is excluded from hide logic).

## Component Relationship
- `AllowedModule`: storage table for allowed module rows (`UserId`, `SystemPageId`, `IsSelected`).
- `SystemSettingController.AllowedModule`:
  - GET renders editable list.
  - POST saves selected module rows.
- `HelperController.GetNotActiveModules`: returns module list consumed by layout JS.
- `_Layout.cshtml`: performs client-side menu show/hide based on returned module IDs.
- `ERPAuthorize` (`Utils/ERPAuthorize.cs`): enforces action-level authorization using `UserPrivilege` and fallback `RolePrivilege`.
- `RolePrivilege` / `UserPrivilege`: action permissions (controller/action), separate from menu visibility.
- `pv` attribute in layout anchors: page marker in markup; not enforced by server-side authorization logic.

## Root Cause of Broken/Incomplete Behavior
- Global delete behavior in AllowedModule POST (`RemoveRange` over all rows) created cross-user side effects.
- Missing/weak user scoping in module fetch allowed visibility leakage.
- Menu visibility is UI filtering only; authorization is separate, so state mismatches were possible.

## Exact Stabilization Patch Implemented
### 1) Save behavior fix
File: `[Controllers/SystemSettings/SystemSettingController.cs](f:\Source Code\DynamicErp\Controllers\SystemSettings\SystemSettingController.cs)`
- Scope delete to current user only:
```csharp
var prevRecord = db.AllowedModules.Where(a => a.UserId == userId).ToList();
```
- Keep transaction.
- Insert only selected + valid rows, deduplicated by `SystemPageId`:
```csharp
.GroupBy(a => a.SystemPageId.Value)
.Select(g => new AllowedModule { SystemPageId = g.Key, IsSelected = true, UserId = userId })
```

### 2) Filtering logic fix
File: `[Controllers/HelperController.cs](f:\Source Code\DynamicErp\Controllers\HelperController.cs)`
- First query current user rows:
```csharp
.Where(a => a.UserId == userId && a.SystemPageId.HasValue && a.IsSelected != false)
```
- Backward-compatible fallback when no user rows exist:
```csharp
.Where(a => a.UserId == null && a.SystemPageId.HasValue && a.IsSelected != false)
```
- Return distinct module IDs.

### 3) Admin/super-admin visibility safe-guard (final)
File: `[Controllers/HelperController.cs](f:\Source Code\DynamicErp\Controllers\HelperController.cs)`
- Detect admin/super-admin via:
```csharp
var isAdminOrSuperAdmin = userId == 1 || db.ERPUsers.Where(u => u.Id == userId).Select(u => u.SystemAdmin).FirstOrDefault() == true;
```
- If true, always union critical system modules into returned list (then distinct by ID), even if not selected accidentally:
  - `SystemSetting`
  - `ERPUsers`
  - `ERPRoles`

## Before / After Behavior
- Before:
  - User A save could delete User B module rows.
  - Module API could expose non-user-scoped rows.
- After:
  - User save affects only that user.
  - Module API is user-scoped with legacy fallback to `UserId IS NULL` rows.
  - Duplicate inserts for same `SystemPageId` in one save request are prevented.
  - Admin/super-admin users always keep critical system module visibility.

## Backward Compatibility
- If no per-user rows exist, API falls back to legacy global rows (`UserId IS NULL`).
- Endpoint contracts and response shape unchanged (`Id`, `ArName` JSON list; `"true"/"false"` on save).
- Admin/super-admin safeguard is additive only to menu visibility response; it does not alter privilege tables or authorization flow.

## Risks and Future Improvements
- If a user intentionally saves a very small module set, menu shrinks accordingly; this is expected but operationally sensitive for admins.
- Consider protecting critical admin modules via non-hide allowlist (future, not part of this patch).
- Consider DB unique index on `(UserId, SystemPageId)` to enforce dedupe at storage level.
- Consider server-side menu construction using permission model for stronger consistency.

## Test Scenarios Performed (This Run)
Environment limits:
- SQL instance `Pc2\Sql2019` was unreachable from this runtime.
- Authenticated browser login flow is not executable from this terminal session.

Executed verification:
1. Scenario 1 (User A save/reload): **PASS (code-path verification)**
- Save path now deletes and inserts rows only for current `userId`.
- Layout still consumes same API and shows only returned module IDs.

2. Scenario 2 (User B isolation): **PASS (code-path verification)**
- No global delete remains; user-scoped query prevents cross-user leakage.

3. Scenario 3 (legacy global fallback): **PASS (code-path verification)**
- If user-scoped result set is empty, query falls back to `UserId == null` rows.

4. Duplicate row insertion check: **PASS (code-path verification)**
- Save request rows are grouped by `SystemPageId` before insert.

5. Admin critical menu access check: **PARTIAL PASS / OPERATIONAL RISK REMAINS**
- No accidental cross-user wipe now.
- Admin can still reduce own visible menu by own selection (expected behavior, not new regression).
 - Final safeguard now ensures critical system modules remain visible for admin/super-admin users.

## Important Paths
- `[Controllers/SystemSettings/SystemSettingController.cs](f:\Source Code\DynamicErp\Controllers\SystemSettings\SystemSettingController.cs)`
- `[Controllers/HelperController.cs](f:\Source Code\DynamicErp\Controllers\HelperController.cs)`
- `[Views/Shared/_Layout.cshtml](f:\Source Code\DynamicErp\Views\Shared\_Layout.cshtml)`
- `[Views/SystemSetting/AllowedModule.cshtml](f:\Source Code\DynamicErp\Views\SystemSetting\AllowedModule.cshtml)`
- `[Utils/ERPAuthorize.cs](f:\Source Code\DynamicErp\Utils\ERPAuthorize.cs)`
- `[Controllers/SystemSettings/RolePrivilegeController.cs](f:\Source Code\DynamicErp\Controllers\SystemSettings\RolePrivilegeController.cs)`
- `[Controllers/SystemSettings/UserPrivilegeController.cs](f:\Source Code\DynamicErp\Controllers\SystemSettings\UserPrivilegeController.cs)`

## Sync Module AllowedModule Integration (May 11, 2026)

### Problem
- `مركز التحكم بالمزامنة` appeared in ERP main menu but was not controlled by `AllowedModule`.
- Root reason: its menu item used hidden module input value `0`, so layout module-filter logic skipped it.

### Minimal Fix Applied
1. Updated Sync menu partial to use real module id lookup (no hardcoded random id):
- File: `[Areas/Sync/Views/Shared/_SyncErpMenuItem.cshtml](f:\Source Code\DynamicErp\Areas\Sync\Views\Shared\_SyncErpMenuItem.cshtml)`
- Change:
  - Added `MySoftERPEntity` lookup for a top-level module row matching Sync identity.
  - Hidden input now renders `@syncModuleId` instead of `0`.

2. Added idempotent SQL script to ensure Sync module row exists in `SystemPage`:
- File: `[Scripts/2026-05-11_Add_Sync_Module_SystemPage.sql](f:\Source Code\DynamicErp\Scripts\2026-05-11_Add_Sync_Module_SystemPage.sql)`
- Behavior:
  - Reuses existing row if found by one of: `ControllerName='Sync'`, `TableName='Sync'`, `Code='SYNC_MODULE'`, or Arabic name.
  - Inserts top-level module row only if none exists.

### Why This Works With Existing Mechanism
- `_Layout` menu filter reads hidden module id from each top-level `<li>`.
- `GetNotActiveModules` returns allowed module ids.
- Once Sync menu has a real `SystemPageId`, it participates in the same show/hide logic as other modules.

### Expected Result
- Unselect Sync module in `/SystemSetting/AllowedModule` => Sync menu hides.
- Select Sync module => Sync menu shows.

### Notes
- No Sync feature/controller logic changed.
- No permission model redesign and no `pv` activation were introduced.

## Page-Level Menu Visibility via Existing Privileges (May 11, 2026)

### Goal
- Add minimal screen/page-level menu hide/show in the original web layout using existing `UserPrivilege`/`RolePrivilege` data and existing `pv` attributes.

### Changes Implemented
1. New lightweight endpoint:
- File: `[Controllers/HelperController.cs](f:\Source Code\DynamicErp\Controllers\HelperController.cs)`
- Action: `GetAllowedPvKeys()`
- Behavior:
  - Reads current user and role from claims.
  - Admin/super-admin (`userId == 1` or `SystemAdmin == true`) returns `SkipFiltering = true`.
  - Computes effective action privilege with same precedence rule used by authorization logic:
    - `UserPrivilege` first
    - fallback to `RolePrivilege`
  - Builds allowed `pv` keys from `SystemPage.ControllerName`, `SystemPage.TableName`, and `PageAction.Action`.

2. Layout menu filter using existing `pv` attributes:
- File: `[Views/Shared/_Layout.cshtml](f:\Source Code\DynamicErp\Views\Shared\_Layout.cshtml)`
- Behavior:
  - Keeps existing module-level `AllowedModule` filtering unchanged.
  - After module filtering, calls `/Helper/GetAllowedPvKeys`.
  - Hides anchors with `pv` not in allowed keys.
  - Collapses empty `li`/`ul` groups created by hidden `pv` links.
  - Leaves links without `pv` untouched unless parent becomes an empty group.

### Preserved
- `AllowedModule` module-level behavior remains as-is.
- `/RolePrivilege` and `/UserPrivilege` assignment screens unchanged.
- `ERPAuthorize` behavior unchanged; direct URL access continues to be server-authorized.
