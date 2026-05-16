# Shared Screen Hosting Fix - 2026-05-15

## Root cause

POS was loading shared enterprise menu entries as full `/MainErp/...` pages inside the POS dashboard iframe.

That meant the POS shell remained outside while the MainErp page rendered its own layout/sidebar/topbar inside the iframe. The visible result was two sidebars and a mixed host experience.

The same flow also allowed MainErp data resolution to use the default MainErp connection instead of the POS/Kishny connection for POS-hosted shared screens.

## Fix applied

- POS shared-screen links now pass explicit host intent with `fromPos=1&host=pos`.
- MainErp layout detects POS-hosted shared requests and renders embedded business content only.
- Embedded POS-hosted MainErp pages do not render `_MainErpSidebar`, the MainErp topbar, MainErp shell wrapper, or shell loader.
- MainErp direct/full-host pages keep the normal MainErp shell.
- MainErp DB connection resolution uses `KishnyCashConnection` for POS-hosted shared screens.
- MainErp login/logout clears POS bridge markers so a real MainErp login restores MainErp host context.
- StoreData read queries now tolerate POS/Cash schema differences for `linked`, `IsLab`, `IsNotCreateEntry`, and `BoxID`.

## Files changed

- `Areas/MainErp/Infrastructure/MainErpHostContext.cs`
- `Areas/MainErp/Infrastructure/MainErpDbConnectionFactory.cs`
- `Areas/MainErp/Infrastructure/MainErpPosSessionBridge.cs` - inspected, no behavior rewrite required.
- `Areas/MainErp/Controllers/LoginController.cs`
- `Areas/MainErp/Views/Shared/_MainErpLayout.cshtml`
- `Areas/Pos/Views/PosDashboard/_Sidebar.cshtml`
- `Common/StoreData/StoreDataRepository.cs`
- `Areas/Pos/Controllers/PosInvoiceReconciliationController.cs` - compile-only initialization fix for an unrelated existing build error.
- `MyERP.csproj`

## Auth/session flow

POS shared menu:

1. User logs in to POS.
2. POS opens `/MainErp/{Screen}?fromPos=1&host=pos` inside the POS content iframe.
3. `MainErpPosSessionBridge` restores MainErp-compatible user context from the existing POS session.
4. `MainErpHostContext` marks the request as POS-hosted.
5. `_MainErpLayout` renders embedded business content only.
6. `MainErpDbConnectionFactory` resolves POS-hosted shared content to `KishnyCashConnection`.

MainErp direct login:

1. User logs in through `/MainErp/Login`.
2. POS bridge session keys are cleared.
3. MainErp pages render the full MainErp shell and use the normal MainErp connection resolution.

## Route smoke results

Tested with a POS-authenticated session on `https://localhost:44370`.

| Route | Status | Login prompt | MainErp sidebar inside content | Embedded host |
| --- | ---: | --- | --- | --- |
| `/Pos/Dashboard` | 200 | No | No | No |
| `/MainErp/Items?fromPos=1&host=pos` | 200 | No | No | Yes |
| `/MainErp/Stocktaking?fromPos=1&host=pos` | 200 | No | No | Yes |
| `/MainErp/Permissions?fromPos=1&host=pos` | 200 | No | No | Yes |
| `/MainErp/Options?fromPos=1&host=pos` | 200 | No | No | Yes |
| `/MainErp/Branches?fromPos=1&host=pos` | 200 | No | No | Yes |
| `/MainErp/Users?fromPos=1&host=pos` | 200 | No | No | Yes |
| `/MainErp/StoreData?fromPos=1&host=pos` | 200 | No | No | Yes |
| `/MainErp/FinancialAdministration?fromPos=1&host=pos` | 200 | No | No | Yes |
| `/MainErp/EmployeePayroll/Employees?fromPos=1&host=pos` | 200 | No | No | Yes |

## Arabic and RTL integrity

- The functional changes were implemented with ASCII-only host/session code where possible.
- Existing Arabic labels in the POS menu and MainErp screens were not regenerated or machine-translated.
- No broad file conversion was performed.
- RTL attributes remain host-driven: POS shell keeps POS RTL/sidebar, and embedded MainErp content keeps `dir` on the business content host.
- Manual HTTP review confirmed Arabic pages return without raw server errors. The in-app browser automation timed out during local POS login entry, so final visual Arabic review should still be repeated manually on the running browser.

## Build

`MSBuild MyERP.csproj /p:Configuration=Debug /p:Platform="AnyCPU"` passed.

Existing warnings remain in unrelated legacy controllers and reports.

## Remaining risk

- StoreData POS-hosted read now opens against the POS/Cash schema. If POS users are allowed to create/edit stores through the shared MainErp StoreData screen, the write path should receive the same schema-compatibility review before enabling edits broadly.
- Browser automation could not complete the typed login flow because the in-app browser control timed out while interacting with the local login form. Server-authenticated route smoke passed.
