# Store Data POS Integration - 2026-05-14

## Source Ownership

- Business authority remains Main Original/MainErp `FrmStoreData`.
- POS does not own store administration rules.
- POS consumes operational store visibility only.

## POS Behavior Delivered

- Added read-only POS route: `/Pos/Stores/Index`.
- Added POS lookup API: `/Pos/Stores/Lookup`.
- Added POS dashboard/menu access for operational stores.
- Existing POS store dropdowns now reuse the shared store lookup through `PosSqlRepository.GetStoresByBranch`.
- POS view shows:
  - store code/name
  - branch
  - phone
  - linked-to-groups status
  - lab flag
  - no-production-entry flag
  - transaction count
  - last movement date

## Permission Rules

- POS requires an active POS session.
- Non-admin POS users are locked to their session branch.
- Admin/full-access/default-change users can inspect other branches.
- POS exposes no create/edit/delete actions for stores.
- Dangerous warehouse administration remains in MainErp only.

## Shared Logic

- POS uses `Common/StoreData/StoreDataRepository`.
- Existing POS `GetStoresByBranch` delegates to the shared operational lookup, avoiding duplicate SQL and duplicate store business rules.
- The POS controller uses `KishnyCashConnection` because existing POS operational data reads from that configured database, while preserving Main Original `TblStore` semantics.

## Runtime QA

- Shared repository smoke tested against realistic `Eng` data:
  - operational list loaded for branch `1`
  - search term filtering loaded expected rows
  - branch filtering returned branch-compatible stores
  - no null crash on last movement date, phone, branch, or flags
- POS `Cash` database operational smoke:
  - loaded 115 operational stores through the shared lookup
  - term search returned filtered rows
  - tolerated older `TblStore` variants that do not contain `IsNotCreateEntry`
- IIS Express route smoke returned the expected login redirect for `/Pos/Stores/Index` and `/Pos/Stores/Lookup`, confirming route/controller startup without an anonymous compile failure.
- Full solution MSBuild is still blocked because the workstation is missing `Microsoft.WebApplication.targets`.

## Remaining Minor Limitations

- The VB6/table model has no dedicated active/inactive column. POS therefore displays real operational flags rather than a synthetic active toggle.
- POS view is intentionally read-only; administration must be done in MainErp.
- Authenticated visual browser QA remains pending because no POS session was available in the route smoke.

## Files Changed

- `Common/StoreData/StoreDataModels.cs`
- `Common/StoreData/StoreDataRepository.cs`
- `Areas/Pos/Controllers/StoresController.cs`
- `Areas/Pos/Controllers/PosDashboardController.cs`
- `Areas/Pos/Data/PosSqlRepository.cs`
- `Areas/Pos/Views/Stores/Index.cshtml`
- `Areas/Pos/Views/PosDashboard/_Sidebar.cshtml`
- `MyERP.csproj`

## Screenshots Checklist

- POS dashboard menu with operational stores entry
- POS stores list desktop
- POS stores list mobile
- POS branch-locked user view
- POS admin/all-branch view
