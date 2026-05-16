# Main Original VB6 Verification - 2026-05-14

Source authority: `F:\Source Code\SatriahMain`  
Target area: `F:\Source Code\DynamicErp\Areas\MainErp`  
Rule for this verification: Main Original VB6 only. Kishny/POS was not used as authority.

This is a migration-completeness audit, not a cosmetic QA pass. Runtime-open success does not mean a screen is complete.

## Evidence Method

- Read `Account.vbp` and the actual `.frm` files registered in Main Original.
- Checked form captions, control names, tabs/grids, button captions, event/sub names, save/search/report references.
- Compared against current MainErp controllers/views/scripts/routes.
- Flagged placeholder/disabled actions in migrated MainErp views.

## Verification Matrix

| Screen | Original VB6 form path | Migrated route | Completeness | Missing parts found | Parts added / already present | Remaining gaps | Screenshot checklist |
|---|---|---|---:|---|---|---|---|
| Customers / Suppliers `FrmCustemers` | `Frm\FrmMembers.frm` (`Attribute VB_Name = FrmCustemers`) | `/MainErp/Customers` | 55% | VB6 has Haj/Omrah flags, old contract action, customer/vendor combined flag, sales/purchase discount sections, tax exemption, vehicle/plate fields, many dependent lookup/change handlers, grid row add/remove sections, favorites/help, and external caller workflow. | MainErp has core customer/supplier identity, branch/account/opening-balance, search/list, save/delete actions, RTL layout. | Needs secondary tabs/sections for discounts, tax exemption, contracts, extra identifiers, grid row sections, and external caller return behavior. | Capture list search, existing record, new/edit, account lookup, customer-vendor toggle, mobile width. |
| Items `FrmItems` | `Frm\FrmItems.frm` | `/MainErp/Items` | 50% | VB6 is very large: `C1Tab1`, `VSFlexGrid2`, many grids, units/prices old/current, guarantees, product lines, locations, related/attached items, production flags, item making, source/spec/shelf-life/weight/overhead, calories, upload/file actions, many report/print paths. | MainErp has searchable item list, core item editor, groups, units grid, pricing/barcode fields, flags, save/delete buttons. | Needs full item detail tabs, price history, warranties/guarantees, product-line and location grids, attachment/upload, report/print actions, and deeper validation parity. | Capture item search, details, units grid, groups grid, price/barcode, flags, responsive grid. |
| Inventory count entry `FrmNewGard` | `Frm\FrmNewGard.frm` | `/MainErp/Stocktaking` | 65% | VB6 has quick item entry, color/size class, cost-from-Excel, big-cost detection, automatic items delete, input method options, store search, file import buttons, print, and stock document navigation. | MainErp has document search, item grid, totals, new/save/delete, store/item selection, basic flags and workflow strip. | Needs Excel/file import, color/size class handling, big-cost review, automatic-item cleanup, print parity, and original entry-mode behavior. | Capture search panel, item add row, totals, new/save buttons, flags, small viewport. |
| Inventory count execution `FrmNewGard1` | `Frm\FrmNewGard1.frm` | `/MainErp/Stocktaking` | 60% | VB6 has start count, start settlement, plus/minus vouchers, different account flags, debit/credit account lookup, auto detect/cost flags, print, user/count/current record status. | MainErp combines stocktaking route and exposes core workflow/totals/actions. | Needs explicit start-count/start-settlement workflow, plus/minus voucher trace, account-pair selection, print, and execution status parity. | Capture start/settlement controls, account fields, totals, item grid, voucher trace. |
| Assembly voucher `FrmDefinCompItem` | `Frm\New frm\FrmDefinCompItem.frm` | `/MainErp/DefinCompItem` | 60% | VB6 has 20 buttons and 149 subs: branch conversion, re-save by date range, production output-only, load/load2, recalculation, linked customer add, multiple print outputs: invoice, quote, receipt, estimated raw materials. | MainErp has final products grid, components grid, cost/totals, save/rebuild/delete, workflow explanation, responsive layout fix. | Print is still disabled; needs multiple print modes, branch conversion/resave workflow, production output-only behavior, customer linkage, and full voucher operation parity. | Capture final product -> components -> cost, rebuild, delete protection, print disabled state. |
| Stores `FrmStoreData` | `Frm\FrmStoreData.frm` | `/MainErp/StoreData` | 70% | VB6 tabs: warehouse data, locations, internal list, users, subaccounts. Has location grid add/remove, employees, branch, sales/purchase persons, four account codes, box linkage, lab/no-entry flags. | MainErp has store list/editor, branch filter, accounts, linked users, delete protection, no POS route leakage. | Needs full location grid editing, internal list tab, richer user/store permissions, full account-code parity and help/favorite behavior. | Capture search/list, editor, account section, users section, delete protection. |
| Banks `FrmBanksData` | `Frm\FrmBanksData.frm` | `/MainErp/FinancialAdministration?scope=banks` | 75% | VB6 includes shares/loans/LC flags, commission, report name/number, IBAN/account/branch/tel/email/address, opening balance voucher/date/type, attachment/help/favorite actions. | MainErp has list/search, summary cards, branch/account/currency/opening balance/editor, loan/approval flags, fixed JS. | Needs shares flag, LC flag parity, commission/report metadata, attachments/favorites/help, and full opening-balance voucher drilldown. | Capture banks list, editor, account lookup, opening balance, flags. |
| Boxes `FrmBoxesData` | `Frm\FrmBoxesData.frm` | `/MainErp/FinancialAdministration?scope=boxes` | 75% | VB6 includes custody type, permanent/temporary custody, max value, cheque cover, cashbox/custody type, employee, branch, account/parent account, opening balance voucher/date/type, attachment/help/favorite actions. | MainErp has list/search/editor, branch/account/employee/opening balance, cheque/wallet/collection badges. | Needs full custody-period/value behavior, attachments/favorites/help, opening voucher drilldown, and complete parent-account behavior. | Capture boxes list, editor, employee/branch/account, opening balance. |
| Receipt voucher `FrmCashing` | `Frm\FrmCashing.frm` | `/MainErp/Cashing` | 55% | VB6 has project mode, subcontractor/final customer options, extract/bill selection, FIFO/advance options, debit/credit accounting pane, cash/bank payment modes, cheque fields, cost center, transaction search, linked info panel. | MainErp route opens after procedure fix; read/search/detail/edit surfaces exist. | Needs bill/extract selection grids, FIFO/advance logic, project-party modes, full accounting side panel, cheque workflow, print/report parity, and save validation parity. | Capture search, voucher detail, payment mode, cheque fields, accounting trace. |
| Payment voucher `FrmPayments` | `Frm\FrmPayments.frm` | `/MainErp/Payments` | 50% | VB6 is much larger than current route: purchase invoice grids, financial invoice grids, project extract grids, select-all/cancel-payment actions, VAT totals, salary payment helpers, contractor/request payment, currency/month/duration fields, multiple retrieval workflows. | MainErp route opens after procedure fix; read/search/detail/edit surfaces exist. | Needs invoice/project/salary linked grids, VAT/payment allocation workflow, contractor/request payment panels, cancel-payment actions, and report/print parity. | Capture search, voucher detail, linked invoice panels, VAT totals, payment mode. |
| Users `TblUsers` user management | `Frm\FrmUsers.frm`, `Frm\dean.frm`, `Frm\New frm\frmUserSearch.frm` | `/MainErp/Users` | 45% | VB6 writes `TblUsers`, `TblUsersStores`, `TblUsersBranches`, `TblUsersBoxes`, `TblUsersProductLine`, handles activation, password, user type, branch/store/box/product-line permissions and many user ability flags. | MainErp-native list/search was added and reads real `TblUsers` with branch/store/box/employee joins. | Needs create/edit user, password/change policy, activation, branch/store/box/product-line assignment grids, and user ability flags. | Capture user search/list, status badges, admin, branch/store/box. |
| Permissions | `Frm\FrmPermission.frm`, `Frm\FrmPermissionScreen.frm`, `Frm\New frm\FrmGroupUsers.frm`, `Bas\ModPremis.bas` | `/MainErp/Permissions` | 40% | VB6 permission model includes screen permission rows, user groups, screen catalog, `CanAdd/CanEdit/CanDelete/CanPrint/CanSearch/CanShow/Attachments`, and `checkApility` runtime logic. | MainErp-native read-only matrix added from `ScreenJuncUser` and `TblUserScreen`. | Needs editable permission assignment, group user maintenance, attachments flag editing, screen catalog management, and audit/validation. | Capture screen matrix, user summary, search, non-admin scoped behavior. |
| Project extracts | `Frm\New frm\projectsbill.frm`, `Frm\New frm\projectsbill1.frm`, `Frm\New frm\projectsbill_search.frm`, `Frm\New frm\ProjectsBillselect.frm` | `/MainErp/ProjectExtracts` | 55% | VB6 project extract workflow includes project/customer/contractor context, previous extracts, deductions, VAT, retention, net payable, line items, selection/search forms, alarms, and accounting/report traces. | MainErp screen was expanded beyond summary with totals, previous/deductions/VAT/retention/net, status, report/accounting visibility. | Needs create/edit parity, line-level entry workflow, approval/state transitions, attachment/report parity, and accounting posting/rebuild parity. | Capture index, detail, line totals, deductions, report/accounting trace. |
| Letters of credit `FrmLC` | `Frm\New frm\FrmLC.frm`, `Frm\New frm\FrmLC_search.frm`, `Frm\New frm\FrmLC_Report.frm` | `/MainErp/LC`, `/MainErp/LC/Report/{id}` | 75% | VB6 has tabs for basic data, opening expenses, financial invoices, revised bond amount, LC loans, refinance, acceptance advice; has create/open/close vouchers, grid vouchers, LG calculations, report search, print entry. | MainErp has index/search/details/edit/report, controlled voucher creation/rebuild/delete, grid display, report route. | Some report/voucher preview tiles remain disabled by permission/read-only workflow; needs full search-report filter parity and all print/entry actions mapped to web reports. | Capture LC index, selected details, tabs, report route, voucher buttons. |
| Medical insurance | Main Original HR ownership; table/setup web surface, no single audited VB6 form found in this pass | `/MainErp/EmployeePayroll/MedicalInsurance` | 45% | Current source mapping still needs a Main Original form-level trace if medical insurance is a client-critical HR workflow. | Replaced POS partial with MainErp-native provider/plan read page; setup tables applied to `Eng`. | Needs exact VB6 source form identification, create/edit parity, reports, employee subscription workflow, and payroll deduction posting parity. | Capture provider/plan empty states, reports, salary-run impact. |
| Payroll salary run | Main Original HR/payroll forms plus salary runtime tables; exact form verification still incomplete | `/MainErp/EmployeePayroll/SalaryRun` | 50% | Runtime preview exists, but component-level VB6 parity and posting workflow remain protected. | Preview route opens and protected test posting panel is explicit. | Needs full Main Original payroll workflow verification before any production posting. | Capture preview, protected posting panel, no raw JSON errors. |

## Other Migrated MainErp Views Found With Incomplete VB6 Parity

| Target view | Current status | Source authority note | Gap |
|---|---|---|---|
| `/MainErp/Purchases` | Visible placeholder/read-only style | Likely `Frm\FrmBillBuy.frm`, `Frm\FrmBuySearch.frm`, related purchase forms | Contains disabled save/import/print and old reuse notes; not client-ready. |
| `/MainErp/StockTransfers` | Visible placeholder/read-only style | Likely `Frm\New frm\FRMTRansferData.frm` and store-transfer forms | Save/import/print disabled; needs full transfer workflow migration. |
| `/MainErp/WorkshopSales` | Visible migrated details with disabled actions | Maintenance/financial sales forms under Main Original maintenance folders | Save/post/delete/print placeholders remain. |
| `/MainErp/JournalEntries` | Read-only/disabled buttons | `Frm\Frm_General_Journal.frm`, `Frm\Frm_JournalSearch.frm`, `Frm\FrmAccEditJournal*.frm` | New/save/report disabled; not a full journal migration. |
| Pump sales / workshop/pump routes | Partial operational migration | Main Original pump/workshop sales forms require separate source audit | Some posting/print actions are intentionally disabled or protected. |

## Important Missing Sections Requiring Migration

1. **Voucher allocation grids:** `FrmPayments` and `FrmCashing` contain linked invoice/extract/project/payment allocation grids that are only partially represented in web.
2. **Items detail tabs:** `FrmItems` has many operational tabs/grids beyond the current core item editor.
3. **Assembly print/re-save workflows:** `FrmDefinCompItem` has multiple print modes and production/re-save workflows not fully migrated.
4. **User administration editing:** `/MainErp/Users` is currently read-only list/search; VB6 user management is much broader.
5. **Permission maintenance editing:** `/MainErp/Permissions` is currently a read-only matrix; VB6 supports maintaining permissions and groups.
6. **Stock count execution workflow:** `FrmNewGard1` start settlement/plus-minus voucher/account behavior needs explicit web parity.
7. **LC report/search parity:** LC web is strong but still lacks full `FrmLC_Report` search/filter and print parity.
8. **Placeholder screens:** Purchases, StockTransfers, WorkshopSales, JournalEntries should not be considered complete migrations.

## Parts Added During This Verification Phase

No broad UI polish was added in this phase. The purpose was to verify source completeness first. Prior closure work already added/fixed:

- MainErp users list from `TblUsers`.
- MainErp permissions matrix from `ScreenJuncUser`.
- Receipts/payments read procedures in `Eng`.
- MainErp-native medical insurance pages and setup tables.
- Menu route corrections.

## Recommended Migration Order For Phase B

1. Payment/receipt allocation grids and voucher save parity.
2. Users + permissions editing workflows.
3. Items secondary tabs/grids.
4. Stocktaking execution workflow (`FrmNewGard1`).
5. Assembly print/re-save/production actions.
6. LC report/search print parity.
7. Placeholder screens outside the current required menu tree.

## Verification Conclusion

MainErp routes are runtime-stable, but multiple screens are not complete migrations of the Main Original VB6 workflows. The next work should migrate the missing sections above from the listed VB6 forms before any further visual polish is treated as client-ready closure.
