# FrmAccountCharts Migration Discovery

## Source

- Requested path was `F:\Source Code\SatriahMain\Frm\New frm\FrmAccountCharts.frm`, but that file was not present.
- Actual source used: `F:\Source Code\SatriahMain\Frm\FrmAccountCharts.frm`.
- Binary resources used by the old screen: `F:\Source Code\SatriahMain\Frm\FrmAccountCharts.frx`.
- Main supporting module: `F:\Source Code\SatriahMain\Bas\ModAccounts.bas`.
- Delete checks used by the old screen: `F:\Source Code\SatriahMain\Bas\Mod_DataBaseFunctions.bas`.

## Original Screen Behavior

- Screen title: `دليل الحسابات`.
- Tree source: `ACCOUNTS` hierarchy by `Account_Code` and `Parent_Account_Code`.
- Visible account number: `Account_Serial`.
- Visible names: `Account_Name`, `Account_NameEng`.
- Internal key: `Account_Code`.
- New account calls `ModAccounts.AddNewAccount`.
- Edit account calls `ModAccounts.EditAccount`.
- Delete blocks accounts with children, basic/cannot-delete flags, journal movement, or auto-generated references.
- Branch/user assignments are stored in `TblAccountBranch` and `TblAccountUser`.

## Database Objects Verified In `Eng`

- `dbo.ACCOUNTS`
- `dbo.AccountsLevelsDetails`
- `dbo.TblAccountBranch`
- `dbo.TblAccountUser`
- `dbo.TblBranchesData`
- `dbo.TblUsers`
- `dbo.Groups`
- `dbo.markaas_taklefa`
- `dbo.tblActivitesType`
- `dbo.currency`
- `dbo.DOUBLE_ENTREY_VOUCHERS`
- `dbo.DOUBLE_ENTREY_VOUCHERS1`

## Important `ACCOUNTS` Columns

- `Account_ID int`
- `Account_Code nvarchar(50)`
- `Account_Serial nvarchar(4000)`
- `Account_Name nvarchar(4000)`
- `Account_NameEng nvarchar(4000)`
- `Parent_Account_Code nvarchar(70)`
- `last_account bit`
- `cannot_del bit`
- `BasicAccount bit`
- `DateCreated smalldatetime`
- `mowazna bit`
- `currenct_code nvarchar(50)`
- `cost_center bit`
- `Sum_account bit`
- `cost_center_id nvarchar(255)`
- `cost_center_type int`
- `ActivityTypeId int`
- `AccountTypes int`
- `AccountTab int`
- `DepitOrCredit int`
- `Differenttype int`
- `Authority int`
- `Block bit`
- `UserGroupId int`
- `UserId int`
- `Level int`

## Account Number Generation

The web implementation mirrors the current VB6 logic:

- New `Account_Code` is generated as `{Parent_Account_Code}a{next child suffix}`.
- Existing child suffixes are scanned and the next unused suffix is selected.
- New `Account_Serial` is generated from the parent serial plus the next child serial segment.
- Segment length comes from `dbo.AccountsLevelsDetails.NoOfDigits` for the new level.
- If the user enters `Account_Serial`, the server validates uniqueness before insert/update.

## Delete Protection

Delete is blocked when:

- The account has children in `ACCOUNTS`.
- `cannot_del = 1` or `BasicAccount = 1`.
- The account is used in `DOUBLE_ENTREY_VOUCHERS` or `DOUBLE_ENTREY_VOUCHERS1`.
- The account appears in the auto-reference tables/columns used by the VB6 screen, when those tables/columns exist in the database.

## SQL Scripts

No schema changes or stored procedure changes were required. No SQL script must be run for this migration.

If future deployment needs explicit DB objects, place MainErp-only scripts under:

`F:\Source Code\DynamicErp\Areas\MainErp\Sql`

## Web Files

- `Areas\MainErp\Controllers\AccountChartsController.cs`
- `Areas\MainErp\Services\AccountCharts\AccountChartsService.cs`
- `Areas\MainErp\ViewModels\AccountCharts\AccountChartsViewModels.cs`
- `Areas\MainErp\Views\AccountCharts\Index.cshtml`
- `Areas\MainErp\Scripts\account-charts.js`
- `Areas\MainErp\Content\account-charts.css`
- `Areas\MainErp\Views\Shared\_MainErpSidebar.cshtml`
- `MyERP.csproj`

## Permissions

Server-side permission checks use `LegacyScreenPermissionService` with screen name `FrmAccountCharts`.

- View: `CanView`
- Add: `CanAdd`
- Edit: `CanEdit`
- Delete: `CanDelete`
- Print: `CanPrint`

Buttons are disabled in the UI when unauthorized, and each server endpoint checks permission again.
