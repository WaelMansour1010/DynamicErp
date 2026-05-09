# Kishny POS Release Hardening Report

Date: 2026-05-09
Scope: DynamicErp release readiness with primary focus on `Areas/Pos`, plus required integration checks for routing, shared layouts, bundles, publish configuration, and project file packaging.

## Executive Summary

The POS release was audited for visible debug/legacy wording, Area routing risk, shared asset dependency risk, Web.config release posture, missing publish files, and local/published runtime smoke checks.

Safe hardening changes were applied without changing business logic, SQL, database schema, stored procedures, binding names, element IDs, or POS route contracts.

## Issues Found

1. User-facing POS wording exposed internal migration/source-system terms:
   - References such as `VB6`, `FrmMoving`, `نفس منطق`, `تجريبي`, and internal report/source labels appeared in POS views and save messages.
   - These were replaced with professional Arabic production wording.

2. POS transaction JavaScript still had a client-side debug logger path:
   - The debug flag was disabled and `posDebugLog` is now a no-op.
   - Technical save details remain hidden unless explicitly enabled server-side for KYC diagnostics.

3. Publish simulation failed before hardening:
   - `MyERP.csproj` referenced missing files:
     - `Views\Log\Index.cshtml`
     - `Properties\PublishProfiles\FolderProfile.pubxml`
     - `Properties\PublishProfiles\FolderProfile1.pubxml`
     - `Properties\PublishProfiles\FolderProfile2.pubxml`
   - These missing project references blocked FileSystem/package publish and were removed from the project file.

4. Release transform posture was verified:
   - Published `Web.config` has `DebugKYC=false`, `EnableDevMasterPassword=false`, `DevMasterPassword=""`, `PosEmergencyAdminEnabled=false`.
   - Published `Web.config` has `customErrors="RemoteOnly"`.
   - Published `Web.config` uses `httpRuntime targetFramework="4.8"`.
   - Published compilation has no `debug="true"` attribute.

5. Remaining audit findings that need future review:
   - Internal code names such as `PosLegacyScreenPermissionService`, `TemporaryPermissions`, and Excel import statuses are still present because they are code/API concepts and changing them would be a risky refactor.
   - Vendor assets contain terms such as `legacy`, `fake`, and `todo` inside third-party DevExpress/Tablesaw files. These were not modified.
   - Test/byte Web.config variants still contain debug-style settings. They should not be used for client production deployment.
   - Several shared layouts load duplicate/global libraries and CDN assets. POS standalone screens mostly avoid them, but non-POS shared layouts remain a future hardening area.

## Files Modified

- `Areas/Pos/Controllers/PosTransactionController.cs`
- `Areas/Pos/Data/PosClosingSqlRepository.cs`
- `Areas/Pos/Data/PosSqlRepository.cs`
- `Areas/Pos/Scripts/pos-transaction.js`
- `Areas/Pos/Services/PosExcelImportPreflightService.cs`
- `Areas/Pos/Views/ExcelImport/Index.cshtml`
- `Areas/Pos/Views/SalesRepresentativesPerformance/Index.cshtml`
- `Areas/Pos/Views/SalesTargets/Index.cshtml`
- `Areas/Pos/Views/StockTransfer/Index.cshtml`
- `MyERP.csproj`

`MyERP.csproj` was modified outside `Areas/Pos` because missing file references prevented publish for the whole web application.

## Fixes Applied

1. Replaced visible internal wording:
   - Stock transfer title text no longer references `FrmMoving`.
   - Excel import note no longer says it follows the same screen logic.
   - Sales target/performance notes now describe the business calculation directly.
   - Save/deadlock messages no longer say `تجريبي` or `مؤقت`.

2. Hardened POS transaction diagnostics:
   - Removed browser console debug output from `pos-transaction.js`.
   - Kept transaction behavior intact and did not alter request/response contracts.

3. Cleaned publish blockers:
   - Removed missing publish-profile and missing view includes from `MyERP.csproj`.
   - Re-ran publish package simulation successfully after the change.

4. Verified release transform output:
   - Confirmed transformed config disables dev/debug POS switches.
   - Confirmed publish package does not include POS AI docs or diagnostic/performance deployment scripts.

## Area and Routing Audit

- `Areas/Pos/PosAreaRegistration.cs` uses the route prefix `Pos/{controller}/{action}/{id}` and POS controller namespace.
- `Areas/MainErp/MainErpAreaRegistration.cs` uses the route prefix `MainErp/{controller}/{action}/{id}` and disables namespace fallback.
- Root route in `App_Start/RouteConfig.cs` redirects to POS login when dev start is disabled.
- `DevStartController` only enables local dev behavior under `#if DEBUG`; Release builds return disabled.
- No circular Area route dependency was changed.

Potential follow-up:
- POS route registration does not explicitly set `UseNamespaceFallback=false`. It is namespaced, but adding the flag later would further reduce controller collision risk after a focused route regression test.

## Shared Layouts and Assets

- POS standalone views mostly use `Layout = null` and direct POS-local assets.
- Shared layouts outside POS still load broad global libraries, CDN jQuery UI, OneSignal, and repeated plugins. They were audited but not refactored to avoid destabilizing MainErp.
- BundleConfig remains generic and is not the primary loader for POS standalone screens.

Potential follow-up:
- Create a dedicated shared POS layout only if future POS screens must stop using standalone page shells.
- Review CDN dependencies for offline/client-server deployments.

## Build and Publish Results

Commands executed:

- `git status --short`
- `dotnet build MyERP.csproj --no-restore -c Release`
- `MSBuild MyERP.sln /t:Clean,Build /p:Configuration=Release /p:Platform="Any CPU"`
- `MSBuild MyERP.csproj /p:Configuration=Release /p:Platform=AnyCPU /p:DeployOnBuild=true /p:WebPublishMethod=FileSystem /p:DeleteExistingFiles=true`
- `git diff --check -- Web.Release.config MyERP.csproj Areas/Pos`

Results:

- `dotnet build` failed because this legacy ASP.NET MVC project requires `Microsoft.WebApplication.targets` from Visual Studio/MSBuild, not the .NET SDK web target path.
- Visual Studio MSBuild Release build succeeded.
- Publish/package simulation initially failed because of missing project file includes.
- Publish/package simulation succeeded after removing the missing includes.
- `git diff --check` passed; only line-ending normalization warnings were reported.

## Runtime Smoke Checks

Published package path tested:

- `F:\Source Code\DynamicErp\obj\Release\Package\PackageTmp`

IIS Express smoke test:

- Published POS login opened successfully:
  - `http://localhost:55220/Pos/PosLogin/Index` returned HTTP 200.
- Main POS screens redirected to login/session gate as expected when unauthenticated:
  - `/Pos/PosDashboard/Index`
  - `/Pos/PosTransaction/Index`
  - `/Pos/PosReports/Index`
  - `/Pos/EmployeePayroll/MedicalInsurance`
  - `/Pos/PosPermissions/Index`
  - `/Pos/ExcelImport/Index`
  - `/Pos/StockTransfer/Index`
  - `/Pos/SalesTargets/Index`
  - `/Pos/SalesRepresentativesPerformance/Index`

Limitations:

- Full admin login and end-to-end POS transaction flows were not executed because valid production-like credentials/session/database workflow were not available in the automated audit context.
- Full browser DevTools console capture could not be completed through Playwright in the available Node REPL because the Playwright module was not installed in the accessible runtime. HTTP and IIS Express checks were completed.

## Release Risks Remaining

1. Production transform usage is critical:
   - Do not deploy with `Web.config` directly because it has local/dev settings.
   - Use Release transform or Kishny production transform values.

2. Connection strings/secrets:
   - Root `Web.config` contains real-looking connection strings and dev/admin toggles.
   - Client deployment should use transform-managed values and server secrets handling.

3. Shared layouts:
   - Non-POS shared layouts still include CDN and duplicate broad assets.
   - This can cause client network dependency or duplicate-library risk for MainErp pages.

4. Existing warnings:
   - Release build succeeds, but the solution has many pre-existing warnings outside POS.
   - These warnings are not new from this hardening pass.

5. Internal code naming:
   - Some internal classes and properties still use `Legacy`, `Temporary`, and `Todo`.
   - They were kept because they are not visible UI text and may be tied to permissions/import contracts.

## SQL and Database Notes

- No SQL scripts were added or modified.
- No database schema, stored procedure, or data migration change was made.
- The earlier medical insurance Arabic `?????` issue remains data/query dependent and was not changed in this release-hardening pass.

## Recommended Next Steps

1. Run a manual admin login on the target database and verify:
   - POS dashboard
   - POS transaction save
   - Reports filters
   - Medical insurance settings
   - Excel import preview
   - Stock transfer

2. Validate deployed server settings:
   - App pool: .NET CLR v4 integrated.
   - Release transformed `Web.config`.
   - Correct machine key if multiple IIS nodes are used.
   - Static content enabled for fonts, CSS, JS, and images.

3. Future hardening:
   - Move MainErp/shared layout CDN dependencies to local assets or documented fallbacks.
   - Add `UseNamespaceFallback=false` to POS route after route regression testing.
   - Clean non-POS visible legacy/test wording in a separate MainErp release-hardening task.
