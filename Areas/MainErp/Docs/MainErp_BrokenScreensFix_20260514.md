# MainErp Broken Screens Fix - 2026-05-14

## Scope

Critical repair pass for shared-screen boundaries and the currently broken MainErp screens reported by manual QA.

Database used for MainErp runtime QA: `Eng`.

## Fixes Applied

### Letters of Credit

- Route tested: `/MainErp/LC/Edit/197`.
- Root cause: the LC edit view formatted legacy nullable dates through the current Arabic `ar-SA` culture. Some LC child rows contain dates outside the supported Umm al-Qura calendar range, so the Razor date helpers threw `ArgumentOutOfRangeException` while rendering the save result. This made the screen appear as if save failed or silently broke.
- Fix: LC date inputs now render with invariant `yyyy-MM-dd` strings before posting. The POST action and repository path remain unchanged.
- Runtime validation: edited LC `197` remarks to `QA save check 2026-05-15`, saved successfully, redirected to `/MainErp/LC?selectedId=197`, then reopened `/MainErp/LC/Edit/197` and confirmed the saved remark is visible.
- SQL proof: `Eng.dbo.TblLC` row `TblLCID = 197` contains the QA remark.
- Console/server status: no JavaScript errors and no raw server exception on the tested edit route.

### Projects and Project Extracts

- Routes tested:
  - `/MainErp/Projects`
  - `/MainErp/ProjectExtracts`
- Fix: restored the Projects menu link under the MainErp projects group. Project Extracts remains visible in the same group.
- Menu validation: MainErp sidebar now exposes `/MainErp/Projects` and `/MainErp/ProjectExtracts`; admin opens both routes successfully.
- Anonymous behavior: MainErp routes redirect to `/MainErp/Login` rather than leaking into POS.
- Console/server status: no JavaScript errors and no raw server exceptions on both tested routes.

### Items

- Route tested: `/MainErp/Items`.
- Source authority checked for screen identity: Main Original VB6 `F:\Source Code\SatriahMain\Frm\FrmItems.frm`.
- UI repair:
  - added item summary strip;
  - widened and stabilized the search/workbench panel;
  - added group tree navigation in the search side panel;
  - reorganized tabs into: `البيانات الأساسية`, `الوحدات والأسعار`, `الباركود`, `المخزون`, `إعدادات البيع/الشراء`, `ملاحظات/إضافي`, `المجموعات`;
  - moved prices beside units;
  - kept barcode, stock limits, warranty, and notes in a readable section;
  - made unit inputs wider and grids horizontally safe;
  - improved responsive behavior for tablet/mobile widths.
- Runtime validation:
  - route opened as admin on `Eng`;
  - search/list loaded 20 real item rows;
  - selected first existing item from the list;
  - unit grid displayed 1 unit row for the selected item;
  - required tabs and summary cards appeared;
  - no raw server exception and no browser console errors.

### Users

- Routes tested:
  - `/MainErp/Users?searchText=admin`
  - `/Pos/PosLegacyAdmin/Users`
- Fix: introduced a shared users core without shared routes:
  - `Common/Users/SharedUserModels.cs`
  - `Common/Users/SharedUsersRepository.cs`
- MainErp wrapper:
  - `/MainErp/Users` uses the shared repository with `MainErpDbConnectionFactory`.
  - The page shows the active MainErp context badge (`MainErp / Eng` during QA).
  - Admin search for `admin` loads users.
  - Page contains no `/Pos/` links.
- POS wrapper:
  - `/Pos/PosLegacyAdmin/Users` still opens inside POS shell after POS admin login.
  - Page contains no `/MainErp/` links.
- Context rule validated: shared repository receives the area-owned connection factory; it does not choose a database itself.

## Build

- Command: `MSBuild MyERP.sln /t:Build /p:Configuration=Debug /p:Platform="Any CPU" /m /v:minimal`
- Result: pass.
- Notes: existing compiler warnings remain, but no build errors.

## Tested Routes

| Route | Context | Result |
| --- | --- | --- |
| `/MainErp/LC/Edit/197` | MainErp / Eng | Pass |
| `/MainErp/LC?selectedId=197` | MainErp / Eng | Pass |
| `/MainErp/Projects` | MainErp / Eng | Pass |
| `/MainErp/ProjectExtracts` | MainErp / Eng | Pass |
| `/MainErp/Items` | MainErp / Eng | Pass |
| `/MainErp/Users?searchText=admin` | MainErp / Eng | Pass |
| `/Pos/PosLegacyAdmin/Users` | POS / Cash | Pass |

## Remaining Issues

- Items save was not forced with a business data mutation in this pass; the screen was loaded, selected, and inspected safely. A dedicated item edit should be done with a named QA item before changing operational item data.
- Shared architecture is now established for users. Banks, boxes, stores, items, and reports should keep moving to the same shared-core/two-wrapper pattern as future changes touch them.

## Screenshots Checklist

- LC edit after saved remark visible: checked in browser.
- Projects menu and routes visible: checked in browser.
- Items list, tabs, summary, and unit grid: checked in browser.
- MainErp Users context badge and list: checked in browser.
- POS Users shell boundary: checked in browser.

## Files Changed

- `Areas/MainErp/Views/LC/Edit.cshtml`
- `Areas/MainErp/Views/Shared/_MainErpSidebar.cshtml`
- `Areas/MainErp/Views/Items/Index.cshtml`
- `Areas/MainErp/Content/items.css`
- `Areas/MainErp/Controllers/UsersController.cs`
- `Areas/MainErp/Views/Users/Index.cshtml`
- `Common/Users/SharedUserModels.cs`
- `Common/Users/SharedUsersRepository.cs`
- `MyERP.csproj`
