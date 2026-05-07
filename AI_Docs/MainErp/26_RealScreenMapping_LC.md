# Real Screen Mapping - LC / FrmLC.frm

## Source Of Truth

- Active VB6 form: `F:\Source Code\SatriahMain\Frm\New frm\FrmLC.frm`
- Active project reference: `F:\Source Code\SatriahMain\Account.vbp`, line `301`: `Form=Frm\New frm\FrmLC.frm`
- Search form: `Frm\New frm\FrmLC_search.frm`
- Report form: `Frm\New frm\FrmLC_Report.frm`
- This mapping supersedes generic LC web layouts. The web screen must behave like a modernized `FrmLC.frm`, not a simplified ERP header page.

## Real VB6 Screen Shape

`FrmLC.frm` is a dense MDI child form, Arabic RTL, fixed-width operational screen. It uses a `C1Tab` control with seven tabs:

1. `البيانات الاساسية`
2. `مصاريف الفتح`
3. `الفواتير المالية`
4. `revised bond amount`
5. `قروض الاعتمادات`
6. `Refinance`
7. `acceptance advice`

The current MainErp LC page must therefore evolve into a tab-heavy operational screen. A simple details page is only acceptable as a temporary read-only bridge.

## Main Controls And Field Mapping

| VB6 control | Role | Web migration meaning |
| --- | --- | --- |
| `TXTTblLCID` | `TblLC.TblLCID` manual id | Primary key display and draft id allocation |
| `TXTLCNO` | LC number | Required business number |
| `DCLC` | LC type lookup | `LCTyperId` from `LCTypes` |
| `DCBank` | issuing bank | `BankId` from `BanksData` |
| `TXTBank2` | second/free bank text | `Bank2` / secondary bank text |
| `DcBranch` | branch | `BranchID` from `TblBranchesData` |
| `DBCboClientName` | vendor/customer | `VendorId` from `TblCustemers` |
| `DCCountry` | country | `CountryId` |
| `DCCUrrency` | currency | `CurrencyId` |
| `txt_Currency_rate` | exchange rate, default `1` | `Currency_rate`, required > 0 |
| `TXTValue` | LC value | `Value` |
| `txtOPenValue` | opening expenses/value | `OpenValue` and open-value voucher preview |
| `TxtOpenBalance` | opening balance | `OpenBalance` |
| `OptType(0/1/2)` | debit/credit/undefined opening balance | `OpenBalanceType`: `0 debit`, `1 credit`, null/undefined |
| `dbFromDate`, `DpCloseDate`, `DPLastParcilDate` | LC dates | `FromDate`, close date, last parcel date |
| `txtGuaranteeDate`, `txtLGExpiryDate` | guarantee/expiry dates | guarantee-specific date fields |
| `TxtNoOfParcil` | parcel count | `NoOfParcil` |
| `ChkLocked` | closed flag | `Locked` |
| `txtGuaranteeNo` | guarantee number | guarantee reference |
| `TXtPrimaryInvoiceNo` | primary invoice | proforma/primary invoice linkage |
| `txtProjectName`, `DataCombo2` | project text/id | `projectName`, `project_id` |
| `txtRemarks` | remarks | `Remarks` |

## Account Controls

| VB6 control | Role |
| --- | --- |
| `DboParentAccount` | legacy parent account, active in older/default LC account flow |
| `cmbAccountLGParent` | LC/bank guarantee parent account |
| `cmbAccountMarginParent` | cash margin parent account |
| `cmbAccountAcceptanceParent` | acceptance parent account, required when enabled |
| `cmbAccountExpensParent` | LC expense parent account |
| `cmbAccount`, `cmbAccountExpProject` | expense/project accounting selectors |

In real save, these controls are not just references. `SaveData` uses them to create/edit child accounts through `ModAccounts.AddNewAccount` and `ModAccounts.EditAccount`. MainErp must not auto-create accounts until this behavior is reproduced and tested.

## Real Grid Mapping

| VB6 grid | Tab / purpose | Related tables/flow |
| --- | --- | --- |
| `Grid` | proforma/items area on the basic screen | item/invoice support and lookup behavior |
| `Fg` | tab-level grid near `C1Tab1` | support grid loaded by form logic |
| `GrdMargin` | financial invoices / margin display | `TBLLCMargin`, read and aggregate `Amount`, `StillAmount`, `IsFullPayed` |
| `GrdBondHistory` | bond/history grid | `TBLLCHistory`, total `txtTotalBondHistory` |
| `GrdMargin2` | revised bond amount / margin payment | `TBLLCMargin` |
| `GrdMargin3` | opening-balance grid | `tblLCOpenB`, can post to `Notes1`/`DOUBLE_ENTREY_VOUCHERS1` |
| `GrdMargin4` | refinance / acceptance-related grid | `TBLLCMargin2` |

The current web read-only page does not yet load these grids. That is a known placeholder and must be replaced with real grid repositories before save/post.

## Real Buttons And Workflow

| VB6 member/control | Behavior |
| --- | --- |
| `Cmd_Click(0)` | New LC. Sets `TxtModFlg="N"`, allocates `TblLCID` with `new_id("TblLC","TblLCiD","",True)`, initializes grids and default accounts. |
| `Cmd_Click(1)` | Edit. Blocks edit when close voucher `TxtNoteID2` exists. Sets `TxtModFlg="E"`, enables grids. |
| `Cmd_Click(2)` | Save. Calls `SaveData`. |
| `Cmd_Click(4)` | Delete. Blocks if main voucher exists, then calls `Del_Trans`. |
| `Cmd_Click(5)` | Search through LC search form. |
| `CmdCreateV` / `CmdCreateV_Click` | Creates normal LC voucher. |
| `Command3` | Creates opening value / opening voucher behavior. |
| `Command6` | Deletes opening voucher. |
| `Command2` | Deletes normal voucher. |
| `Command9` | Prints normal voucher. |
| `Command1`, `cmdPrintEntryClose` | Close-voucher delete/print. |
| `CmdCreateV2(Index)` | Creates grid-driven vouchers for margin/history/refinance/opening-balance rows. |
| `cmdAddLine(Index)` | Adds/removes rows from the relevant grid depending on tab/index. |

The web must keep these workflow concepts visible. Buttons may remain disabled until the matching VB6 behavior is implemented, but they should not be replaced by generic `Create/Edit/Delete` UX.

## Save Flow To Preserve

`SaveData` starts at line `8692` in the active form.

1. Validate branch-account configuration and required screen fields.
2. Start `Cn.BeginTrans`.
3. Resolve branch accounts with `get_account_code_branch(225, my_branch)` and `get_account_code_branch(226, my_branch)`.
4. Read bank account defaults from `BanksData`.
5. On new LC:
   - open `TblLC`;
   - assign `TblLCID`;
   - create child accounts for acceptance, margin, LC, and expense roles with `ModAccounts.AddNewAccount`;
   - link created accounts back to `TblLCID`.
6. On edit:
   - edit existing LC-linked accounts with `ModAccounts.EditAccount`;
   - create missing accounts when needed;
   - delete/rebuild `Notes`, `Notes1`, `DOUBLE_ENTREY_VOUCHERS1`, `TBLLCHistory`, `TBLLCMargin`, `TBLLCMargin2`, `tblLCOpenB`.
7. Save all `TblLC` header/account/note/date/project/opening-balance fields.
8. Save grids with `saveGrid`.
9. Reload grids and recalculate `StillAmount` / `IsFullPayed`.
10. Commit header/grid transaction.
11. Start a second transaction and call:
    - `CmdCreateV_Click`;
    - `Command3_Click` when `optTypeLCLG(0)` is selected.
12. Commit voucher transaction or rollback on error.

## Lookup Flow

`Form_Load` loads:

- banks: `Dcombos.GetBanks`
- countries: `Dcombos.GetCountriesNames`
- LC types: `Dcombos.GetLCTypesName`
- currency: `Dcombos.GetCUrrencyNames`
- branches: `Dcombos.GetBranches`
- boxes: `Dcombos.GetBoxes`
- projects from `Projects where Fullcode is not null`
- account selectors through `Dcombos.GetAccountingCodes`
- users through `LoadCombosData` / `Dcombos.GetUsers`

F3 key behavior clears or opens lookup search on several account/data combo controls. The web should support fast keyboard lookup, not only mouse dropdowns.

## Current MainErp Status After Correction

Real migrated:

- LC list reads actual `TblLC`.
- LC details reads actual `TblLC`, `BanksData`, `currency`, `TblCustemers`.
- LC details page is now shaped as a modernized VB6 screen with the real tab names and workflow buttons visible but disabled.

Still placeholder:

- all save/edit/delete behavior;
- account creation/editing;
- `Notes`, `Notes1`, `DOUBLE_ENTREY_VOUCHERS`, `DOUBLE_ENTREY_VOUCHERS1` writes;
- all LC grid load/save/posting flows;
- close voucher and print flows;
- proforma/item grid behavior.

## Next Implementation Order

1. Load real LC grid data read-only: `TBLLCHistory`, `TBLLCMargin`, `TBLLCMargin2`, `tblLCOpenB`.
2. Replace details placeholders with real grids.
3. Implement create/edit draft matching `Cmd_Click` and `SaveData` header behavior only.
4. Implement account creation preview using exact `ModAccounts.AddNewAccount` rules.
5. Implement voucher preview per `CREATE_VOUCHER_GE` and `CREATE_VOUCHER_GE2`.
6. Only after regression testing, enable actual Notes/voucher posting.
