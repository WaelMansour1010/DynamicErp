# FrmCustemers / FrmMembers Migration Discovery

## Source Identity

- Physical VB6 source file: `F:\Source Code\SatriahMain\Frm\FrmMembers.frm`.
- Binary VB6 resource file: `F:\Source Code\SatriahMain\Frm\FrmMembers.frx`.
- Internal VB6 form/class identity: `FrmCustemers`.
- VB6 project reference: `Account.vbp` includes `Form=Frm\FrmMembers.frm`.
- Kishny/POS copies were not used as source behavior.

## References And Callers

- The legacy permission/menu key is `FrmCustemers`.
- `Bas\ModOpenScreen.bas` maps/open screens using `FrmCustemers`.
- Main menus and outbar check `checkApility("FrmCustemers")` before opening it.
- Sales and related screens open/retrieve this form by `FrmCustemers`, often passing an existing `CusID`.
- Search helpers such as `FrmMemberSearch.frm` call `FrmCustemers.Retrive`.

## Legacy Business Rules Observed

- Main table: `dbo.TblCustemers`.
- Main account table: `dbo.ACCOUNTS`.
- Required field: customer name.
- Save mode uses `TxtModFlg`: `N` for add, `E` for edit, `R` for read.
- New id is equivalent to `new_id("TblCustemers", "CusID", "", True)`.
- `Type = 1` is used for the customer master behavior in this form.
- Duplicate checks:
  - Customer name within `Type = 1`.
  - Full code (`prifix + code`) within `Type = 1`.
  - Commercial/record number (`CustGID`) when the system option requires it.
- Code behavior:
  - Legacy stores `prifix`, `code`, and `Fullcode`.
  - Legacy can auto-generate code via `get_coding`; MainErp stores the same fields and defaults the code to the new `CusID` when blank.
- Account behavior:
  - Default customer parent account comes from branch mapping `a8`; live DB value is `a1a2a2`.
  - The form creates/updates a linked final account in `ACCOUNTS`.
  - Optional legacy modes create extra accounts for checks/advance payments; the MainErp screen preserves the available table fields and primary linked account without enabling unrelated vertical account modes.
- Opening balance:
  - Fields include `OpenBalance`, `OpenBalanceType`, `OpenBalance1/2`, and `OpenBalanceType1/2`.
  - Legacy type values are `0 = مدين`, `1 = دائن`, `NULL = بدون`.
  - Branch opening-balance offset account is `a59`; live DB value is `a5a2a5`.
- Discount rules:
  - Sale discount fields: `Trans_DiscountType`, `Trans_Discount`.
  - Purchase discount fields: `Trans_DiscountTypePur`, `Trans_DiscountPur`.
  - Type `0 = بدون`, `1 = قيمة`, `2 = نسبة`; percent cannot exceed 100.
- Credit rules:
  - Credit limit, credit limit for credit side, debit/credit intervals, and credit lock are stored.
- E-invoice/tax fields:
  - VAT, tax exempt, commercial/record number, street, building number, postal zone, country code, extra address fields, and export flag are stored.
- Delete behavior:
  - Legacy blocks deletion when integrated data exists.
  - MainErp blocks deletion for transactions, notes, job orders, projects, and linked account vouchers.

## MainErp Mapping

- URL: `/MainErp/Customers`.
- Permission key: `FrmCustemers`.
- MVC files:
  - Controller: `Areas/MainErp/Controllers/CustomersController.cs`.
  - Service: `Areas/MainErp/Services/Customers/CustomerService.cs`.
  - Repository: `Areas/MainErp/Repositories/Customers/CustomerRepository.cs`.
  - View model: `Areas/MainErp/ViewModels/Customers/CustomersViewModels.cs`.
  - View: `Areas/MainErp/Views/Customers/Index.cshtml`.
  - Script/style: `Areas/MainErp/Scripts/customers.js`, `Areas/MainErp/Content/customers.css`.
- Menu entry is added to the MainErp sidebar and guarded by `LegacyScreenPermissionService.CanView(context, "FrmCustemers")`.
- No Kishny/POS behavior was changed.

## Database Notes

- Live schema inspected in `Henaki2026` before mapping fields.
- No table schema change was required.
- No stored procedure change was required.
- Permission helper script: `Areas/MainErp/Sql/08_MainErp_Customers_Permission.sql`.

## Deliberate Scope Decisions

- The VB6 file contains vertical grids for cars, sizes, and school/student-style details (`TblCusCar`, `TblCustomerSizes`, `TblCustomersLocations`). These are not included in the MainErp customer master screen because they are domain-specific child workflows, not required for a clean MainErp customer master migration, and adding them would change unrelated module behavior.
- MainErp preserves the main customer/account/tax/discount/opening-balance fields already present in `TblCustemers`.
