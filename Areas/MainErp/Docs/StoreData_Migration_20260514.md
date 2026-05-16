# Store Data Migration Closure - 2026-05-14

## Source Analyzed

- Source authority: `F:\Source Code\SatriahMain\Frm\FrmStoreData.frm` from Main Original VB6.
- Requested root file `F:\Source Code\SatriahMain\FrmStoreData.frm` was not present; the actual source form was found under `Frm`.
- Screen: `FrmStoreData`, Arabic caption `بيانات المخازن`.

## VB6 Behavior Captured

- Maintains `dbo.TblStore`.
- Required fields: branch and Arabic store name.
- Duplicate guard: store name must be unique when adding/editing.
- Store fields captured: code, Arabic/English name, address, phone, remarks, branch, store keeper employee, sales person, purchase person, box, lab flag, no-production-entry flag, linked-to-groups flag.
- Accounting linkage captured:
  - `Account_Code` inventory account.
  - `Account_Code1` loss/damage account.
  - `Account_Code2` inventory settlement account.
  - `Account_Code3` gifts/samples account.
  - `ParetnAccount` parent store account when settlement-account grouping is enabled.
  - `Account_Code0`, `Account_Code11`, `Account_Code22`, `Account_Code33` store parent-account defaults.
- System options respected during account creation:
  - `StoreAccountHaveSettelment`
  - `eachStoreHaveLossAccount`
  - `eachStoreHaveGiftAccount`
- User-store access captured from `TblUsersStores`.
- Delete is guarded by:
  - existing `Transactions.StoreID`
  - existing `DOUBLE_ENTREY_VOUCHERS.Account_Code` rows for store-related accounts.
- VB6 active/inactive behavior: no dedicated inactive column was found in `TblStore`; the migrated UI exposes the real operational flags instead of inventing a schema flag.

## MainErp Behavior Delivered

- Added modern MainErp administration route: `/MainErp/StoreData`.
- Added full admin UI for:
  - search and branch filtering
  - linked/unlinked/lab/no-entry filters
  - create/edit/delete where permission allows
  - branch assignment
  - responsible employee
  - sales/purchase person
  - operational notes
  - account parent and generated account visibility
  - user-store authorization assignments
  - transaction/account-use delete safety indicators
- Added guarded account creation for new stores using the same branch-account/system-option semantics discovered in VB6.
- Edit flow preserves and relabels existing account records instead of rebuilding account trees.
- Delete flow does not expose dangerous deletion when movements or accounting rows exist.

## Shared Logic Decisions

- Created shared layer:
  - `Common/StoreData/StoreDataModels.cs`
  - `Common/StoreData/StoreDataRepository.cs`
- MainErp writes through the shared repository.
- POS reads operational store data through the same shared repository.
- Existing POS `GetStoresByBranch` now delegates to the shared operational lookup.
- No duplicate POS-specific warehouse business rules were introduced.

## Permissions

- MainErp screen permission: `FrmStoreData`.
- MainErp checks:
  - view: `CanView`
  - add: `CanAdd`
  - edit: `CanEdit`
  - delete: `CanDelete`
- Menu entry is visible only when the user can view `FrmStoreData`.

## Runtime QA

- Read-only DB smoke test against `Eng` on `Wael\Sql2019`:
  - loaded `TblStore` index successfully
  - loaded branch-filtered results successfully
  - loaded employees, users, accounts, and selected-store details successfully
  - loaded POS operational store list successfully
  - verified transaction/account-use delete counters load without null crashes
- Targeted C# compilation of the shared store repository succeeded.
- IIS Express route smoke returned login redirect for `/MainErp/StoreData/Index`, confirming route/controller startup without an anonymous compile failure.
- Full solution MSBuild could not run on this workstation because `Microsoft.WebApplication.targets` is missing from the installed Build Tools.

## Remaining Minor Limitations

- No physical active/inactive database column exists in VB6 `TblStore`; operational status is represented with existing flags (`linked`, `IsLab`, `IsNotCreateEntry`).
- Authenticated visual browser QA could not be completed in this session; unauthenticated route smoke reached the expected login redirect.

## Files Changed

- `Common/StoreData/StoreDataModels.cs`
- `Common/StoreData/StoreDataRepository.cs`
- `Areas/MainErp/Controllers/StoreDataController.cs`
- `Areas/MainErp/Views/StoreData/Index.cshtml`
- `Areas/MainErp/Content/store-data.css`
- `Areas/MainErp/Views/Shared/_MainErpSidebar.cshtml`
- `Areas/Pos/Controllers/StoresController.cs`
- `Areas/Pos/Controllers/PosDashboardController.cs`
- `Areas/Pos/Data/PosSqlRepository.cs`
- `Areas/Pos/Views/Stores/Index.cshtml`
- `Areas/Pos/Views/PosDashboard/_Sidebar.cshtml`
- `MyERP.csproj`

## Screenshots Checklist

- MainErp store list desktop
- MainErp store edit form desktop
- MainErp account/user assignment area
- MainErp narrow/mobile layout
- POS operational stores desktop
- POS operational stores mobile
