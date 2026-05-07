# Re-analysis Gap Report - VB6 MainERP vs Current Web MainErp vs POS Reference

## Scope

This report is the required checkpoint before any further large implementation. It compares:

- Original VB6 MainERP sources.
- Current `Areas\MainErp` web implementation.
- Existing `Areas\Pos` web implementation as a reusable migration-style reference.

No database changes were made for this report.

## Active VB6 Files Confirmed

`F:\Source Code\SatriahMain\Account.vbp` is the active VB6 project map.

| Feature | Active source | Account.vbp line |
| --- | --- | ---: |
| LC main screen | `F:\Source Code\SatriahMain\Frm\New frm\FrmLC.frm` | 301 |
| LC types | `F:\Source Code\SatriahMain\Frm\New frm\FrmLCTypes.frm` | 305 |
| LC search | `F:\Source Code\SatriahMain\Frm\New frm\FrmLC_search.frm` | 669 |
| LC report | `F:\Source Code\SatriahMain\Frm\New frm\FrmLC_Report.frm` | 670 |
| Project bill main screen | `F:\Source Code\SatriahMain\Frm\New frm\projectsbill.frm` | 242 |
| Project reports | `F:\Source Code\SatriahMain\Frm\New frm\frmProjectsReports.frm` | 353 |
| Project bill search | `F:\Source Code\SatriahMain\Frm\New frm\projectsbill_search.frm` | 673 |
| Project bill alarms/reporting | `F:\Source Code\SatriahMain\Frm\New frm\ProjectsBillAlarm1.frm` | 826 |

## A. LC Re-analysis

### VB6 Screen Shape

`FrmLC.frm` is not a simple list/details screen. It is a dense RTL accounting screen using `C1Tab1`.

Actual tabs from the VB6 control:

1. `البيانات الاساسية`
2. `مصاريف الفتح`
3. `الفواتير المالية`
4. `revised bond amount`
5. `قروض الاعتمادات`
6. `Refinance`
7. `acceptance advice`

### Important LC Controls

| VB6 control | Purpose |
| --- | --- |
| `TXTTblLCID` | Manual id from `new_id("TblLC", "TblLCiD", "", True)` |
| `TXTLCNO` | LC number |
| `DCLC` | LC type lookup from `LCTypes` |
| `DCBank` | bank lookup |
| `DcBranch` | branch |
| `DBCboClientName` | vendor/customer |
| `DCCountry` | country |
| `DCCUrrency` | currency |
| `txt_Currency_rate` | exchange rate, default `1` |
| `TXTValue` | LC value |
| `txtOPenValue` | opening/expense value |
| `TXTBank2`, `DcboBankName`, `DcboBox`, `TxtChequeNumber`, `DtpChequeDueDate` | payment/opening expense controls |
| `cmbAccountLGParent` | LC account parent |
| `cmbAccountMarginParent` | margin account parent |
| `cmbAccountAcceptanceParent` | acceptance account parent |
| `cmbAccountExpensParent` | expense account parent |
| `cmbAccount`, `cmbAccountExpProject` | additional expense/project accounts |
| `TxtOpenBalance`, `OptType(0/1/2)`, `txtopening_balance_voucher_id` | opening balance amount/type/voucher id |
| `dbFromDate`, `DpCloseDate`, `DPLastParcilDate`, `txtGuaranteeDate`, `txtLGExpiryDate` | date workflow |
| `txtRemarks`, `txtProjectName`, `DataCombo2` | notes/project linkage |

### LC Grids

| VB6 grid | Table/meaning | Current MainErp status |
| --- | --- | --- |
| `Grid` | items/proforma support | Missing |
| `Fg` | tab support grid | Missing |
| `GrdMargin` | financial invoices/margin display, `TBLLCMargin` | Missing |
| `GrdBondHistory` | LC history, `TBLLCHistory` | Missing |
| `GrdMargin2` | margin payment/revised bond amount, `TBLLCMargin` | Missing |
| `GrdMargin3` | opening balance rows, `tblLCOpenB` | Missing |
| `GrdMargin4` | refinance/acceptance advice, `TBLLCMargin2` | Missing |

### LC Buttons And Events

| VB6 event | Behavior |
| --- | --- |
| `Cmd_Click(0)` | New; allocates id, initializes grids and account defaults |
| `Cmd_Click(1)` | Edit; blocks if close voucher exists |
| `Cmd_Click(2)` | Save; calls `SaveData` |
| `Cmd_Click(4)` | Delete; permission + voucher safety then `Del_Trans` |
| `Cmd_Click(5)` | Search |
| `CmdCreateV_Click` | Create normal LC voucher |
| `CmdCreateV2_Click(Index)` | Create grid-driven voucher |
| `Command2_Click` | Delete normal voucher |
| `Command3_Click` | Create opening value voucher |
| `Command6_Click` | Delete opening voucher |
| `Command9_Click` / `cmdPrintEntryClose_Click` | Print voucher |
| `GrdMargin*_AfterEdit/BeforeEdit/CellButtonClick/KeyUp/StartEdit` | grid accounting and account lookup behavior |
| account combo `KeyUp` | F3-style lookup/clear behavior |

### LC SQL / Table Usage Observed

Important observed SQL/table usage:

- `TblLC` / `TBLLC`
- `LCTypes`
- `BanksData`
- `ACCOUNTS`
- `Expenses_accounts`, `Expenses_accounts_eng`
- `Notes`
- `Notes1`
- `DOUBLE_ENTREY_VOUCHERS`
- `DOUBLE_ENTREY_VOUCHERS1`
- `TBLLCHistory`
- `TBLLCMargin`
- `TBLLCMargin2`
- `tblLCOpenB`
- `Projects`
- `subjects_images`

Important functions:

- `new_id`
- `CreateNotes`
- `Notes_coding`
- `Voucher_coding`
- `get_account_code_branch`
- `get_opening_balance_voucher_id`
- `ModAccounts.AddNewAccount`
- `ModAccounts.EditAccount`
- `ModAccounts.AddNewDev`
- `saveGrid`
- `loadgrid`
- `DoPremis`

### LC Gap Report

| Gap | VB6 has | Current MainErp has | Decision |
| --- | --- | --- | --- |
| Real input/edit screen | Full tabbed screen with fields and grids | Mostly read/search plus visual shell | Must implement real read/write form matching tabs |
| Grid loading | 5+ grids with save/post behavior | Placeholder text only | Implement read-only grids first |
| Account generation | `AddNewAccount` and `EditAccount` side effects | Preview abstraction only | Rebuild exact logic before enabling |
| Voucher creation | `CreateNotes` + `AddNewDev` | Preview infrastructure only | Keep disabled until exact mapping |
| Delete behavior | conditional delete and rollback | None | Implement only after voucher safety mapping |
| Report/print | `FrmLC_Report`, `print_report`, voucher print | None | Inventory Crystal/report path |
| Permissions | `DoPremis(Do_New/Edit/Delete/Print)` | MVC `[Authorize]` only | Need MainErp permission names mapped to VB6 actions |
| Lookups | Dcombos + F3 behavior | simple text/id filters | Need real lookup endpoints |
| Current list | Search technical view | Exists | Keep as temporary technical test page only |

## B. Project Bills Re-analysis

### VB6 Screen Shape

`projectsbill.frm` is a project progress invoice/extract screen, not a generic invoice.

It contains:

- project header;
- customer/subcontractor party selection;
- main detail grid `Fg_Journal`;
- deductions/retention/performance bond;
- VAT;
- advance payment allocation;
- approval workflow;
- accounting posting;
- QR/e-invoice side effects.

### Important Project Bill Controls

| VB6 control | Purpose |
| --- | --- |
| `txtid` | `project_billl.id` |
| `note_id` | `Notes.NoteID` |
| `TxtNoteSerial` | accounting note serial |
| `TxtNoteSerial1` | project invoice/document serial |
| `txtManualNo` | manual number |
| `XPDtbTrans` | bill date |
| `Dcbranch` | branch |
| `DataCombo2`, `txtprojectname` | project |
| `billto` | end-user vs subcontractor flow |
| `DcbosubContractor` | subcontractor |
| `TXTOrDer_no`, `TXTOrDer_no2`, `CBoBasedON` | source project/order/contract basis |
| `Option7`, `Option6`, `Option8` | actual/estimated/under-implementation |
| `TxtAccountUnderImp` | under implementation account |
| `total`, `Results`, `TxtNetValue`, `TxtFATYou`, `TxtFATValue`, `TxtTotalValue` | totals/VAT |
| `txtDiscount`, `txtDiscount1/2/3/4`, `txtDiscountG`, `txtDiscountGMater` | deductions |
| `TxtPerforValue`, `txtPerformanceBond` | retention/performance values |
| `advancedPayment`, `TxtPreVAT` | advance payment/VAT |
| `AccountVat`, `DcDiscountAccount` | accounting accounts |
| `chkTaxExempt` | tax exempt flag |

### Project Bill Grids

| VB6 grid | Purpose | Current MainErp status |
| --- | --- | --- |
| `Fg_Journal` | main project detail lines and calculations | Missing |
| `VSFlexGrid4` | advance/prepaid notes | Missing |
| `GrdBondHistory` | project bill history | Missing |
| `GRID2` | approval/status display | Missing |
| `VSFlexGrid1/2/3` | supporting source/employee/items/expense grids | Missing |

### Project Bill Events

| VB6 event/function | Behavior |
| --- | --- |
| `Search` / `maaRetrive` / `Retrive` | load header and details |
| `Cmd_Click(Index)` | new/edit/save/delete/print/search toolbar |
| `SaveData` | main save + note + posting transaction |
| `Savetemp` | save `project_bill_details`, line allocations, QR, advance payment |
| `Accredit_Click` | send to approval with `SendTopost` |
| `fillapprovData` | load `ApprovalData` into grid |
| `ALLButton1_Click` | load advance payment notes |
| `saveBillBuy` | save `TblPayPrePayed` / `TblProjePayPrePayed` links |
| `Del_Trans` | delete/reversal cleanup |
| `calcnet`, `ReLineGrid`, `CalcFormat`, change events | financial calculations |
| `savenewelectroncic` | post-commit e-invoice metadata behavior |

### Project Bill SQL / Table Usage Observed

Important tables:

- `project_billl`
- `project_bill_details`
- `TBLProjectBillHistory`
- `project_billl_Month`
- `projects`
- `projects_des`
- `ProjectMainDes`
- `SubcontractorContract`
- `SubcontractorContract2`
- `Notes`
- `DOUBLE_ENTREY_VOUCHERS`
- `TblPayPrePayed`
- `TblProjePayPrePayed`
- `ApprovalData`
- `TblCustemers`
- `TblBranchesData`
- `tblActivitesType`
- `transactionsVatDetails`
- `terms_operations_project_bill`
- `TblProcessUnites`

Important functions:

- `new_id`
- `Voucher_coding`
- `Notes_coding`
- `ModAccounts.AddNewDev`
- `SaveQRCode` / `SaveQRCode6`
- `SendTopost`
- `updateNotesValueAndNobytext`
- `saveGrid`
- `loadgrid`
- `DoPremis`

### Project Bills Gap Report

| Gap | VB6 has | Current MainErp has | Decision |
| --- | --- | --- | --- |
| Real detail grid | `Fg_Journal` with quantities, previous/current/cumulative values | Placeholder only | Implement `project_bill_details` read-only grid next |
| Advance payment grid | `VSFlexGrid4` + pay/prepay tables | Placeholder only | Implement read-only allocation view |
| Approval flow | `Accredit_Click`, `ApprovalData` | Button placeholder only | Load approval status first |
| Save/post | destructive rebuild transaction + accounting | None | Do not enable until exact migration |
| Calculations | UI event-driven totals | Header values only | Port calculation engine from VB6 |
| ZATCA/QR side effects | post-commit behavior | Not in MainErp | Defer, document separately |
| Reports | `print_report`, reports forms | None | Inventory/report mapping needed |
| Permissions | `DoPremis` | `[Authorize]` only | Map permissions |

## C. POS Web Reference Inventory

The following POS web modules are implemented and can guide MainErp migration style.

| POS module | Files | Reuse value | Risk |
| --- | --- | --- | --- |
| Dashboard/sidebar | `Views\PosDashboard\Index.cshtml`, `_Sidebar.cshtml`, `PosDashboardController.cs` | Strong layout/navigation/dashboard pattern | Contains POS/KYC/card/cashier widgets; must filter |
| Purchases | `PurchaseInvoiceController.cs`, `Views\PurchaseInvoice\Index.cshtml`, `Scripts\purchase-invoice.js`, repository methods | Working filters, entry UI, item grid, totals | Coupled to `PosUserContext`, POS permissions, POS repository |
| Stock transfers | `StockTransferController.cs`, `Views\StockTransfer\Index.cshtml`, repository methods | Working transfer UI and serial style | Coupled to POS branch/store/session |
| Journal entries | `JournalEntriesController.cs`, `Views\JournalEntries\Index.cshtml`, repository methods | Strong voucher UI, lines, totals, lookup concept | Save uses POS context/admin password; must split |
| Accounting reports | `AccountingReportsController.cs`, `Views\AccountingReports\Index.cshtml`, `html-reports.css` | Report tile/filter/export UX | Uses POS permissions and repository |
| Sales reports | `PosReportsController.cs`, `HtmlReportsController.cs`, report views | Generic sales report concepts | Mixed with POS-specific reports |

Reusable POS style/patterns:

- modern RTL shell;
- sidebar with sections;
- iframe/screen area approach;
- dashboard filter period toggles;
- KPI cards and chart containers;
- report tiles;
- account tree selector;
- grid filters and searchable lookups;
- Excel export pattern.

Must exclude:

- KYC;
- cards/tokens;
- commissions;
- cashier closing;
- POS service flows;
- POS session restore;
- POS health/deadlock/save telemetry unless moved to neutral diagnostics;
- Kishny-only receipt/report templates.

## D. Current MainErp Comparison

Current MainErp files inspected:

- `Areas\MainErp\Controllers\LCController.cs`
- `Areas\MainErp\Controllers\ProjectExtractsController.cs`
- `Areas\MainErp\Controllers\PurchasesController.cs`
- `Areas\MainErp\Controllers\StockTransfersController.cs`
- `Areas\MainErp\Controllers\JournalEntriesController.cs`
- `Areas\MainErp\Controllers\AccountingReportsController.cs`
- `Areas\MainErp\Controllers\SalesReportsController.cs`
- `Areas\MainErp\Controllers\DashboardController.cs`
- repositories under `Areas\MainErp\Repositories`
- views under `Areas\MainErp\Views`

### What Exists And Is Useful

- `MainErp_ConnectionString` separation exists.
- LC and Project Extract list/details read from live legacy tables.
- Journal-entry read search exists against `DOUBLE_ENTREY_VOUCHERS`, `Notes`, `ACCOUNTS`.
- Basic accounting reports and sales summary read-only routes exist.
- Transaction/manual id/account/voucher preview infrastructure exists, but should stay subordinate to real VB6 migration.

### What Is Wrong / Too Generic

- LC still does not load real LC grids or implement real toolbar workflow.
- Project Extracts does not load `project_bill_details`.
- Purchases and stock transfers are UI-only shells, not real working modules.
- MainErp layout is not close enough to POS dashboard/sidebar experience.
- Dashboard is generic and not backed by reusable POS-style summary repository.
- MainErp permissions are not mapped to VB6 `DoPremis` behavior or POS-style menu checks.

## E. Debug Launcher Gap / Feasibility

Requirement:

Debug-only launch selection:

1. database target;
2. run mode:
   - original web;
   - MainERP;
   - POS.

Current behavior:

- RouteConfig maps empty URL `""` to `PosLogin.Root` under POS.
- Normal login redirects to old `Home/Index` when no `ReturnUrl`.
- `/MainErp` works only when explicitly opened.
- `/Pos` uses POS route/login.

Safe proposed implementation, not yet executed:

- Add a debug-only controller/view such as `DebugLauncherController`.
- Register route only inside `#if DEBUG` or guarded by `appSettings EnableDebugLauncher=true`.
- On local/debug only, root route can show launcher before default redirect.
- Selection should store a developer-only session value, not edit production connection strings.
- DB selection should choose from named configured connection strings such as:
  - `MyERP_ConnectionString`
  - `MainErp_ConnectionString`
  - POS/Kishny connection name
- Do not display or edit passwords.
- Do not allow this in production.

Open design question before implementation:

- Main web currently uses EF connection strings and static configuration. Runtime switching for original web may require a separate safe debug-only factory or restart-like selection; MainErp already has a factory and can switch more safely.

## F. Next Safe Implementation Order

No large code copy should happen until the following steps are approved:

1. MainErp shell/layout:
   - create MainErp dashboard/sidebar modeled on POS `_Sidebar.cshtml`;
   - strip POS-only links;
   - keep route isolation.
2. LC:
   - implement real read-only grids from `TBLLCHistory`, `TBLLCMargin`, `TBLLCMargin2`, `tblLCOpenB`;
   - then implement real edit shell fields;
   - keep save/post disabled.
3. Project Extracts:
   - load `project_bill_details` into `Fg_Journal` style grid;
   - load advance payments;
   - load approval status.
4. POS reuse:
   - extract UI patterns from purchases/stock/journal/reports;
   - create MainErp repositories using `MainErpDbConnectionFactory`;
   - only enable real search/details first.
5. Debug launcher:
   - implement debug-only launcher after confirming route behavior.

## G. Database Change Status

No database schema or stored procedure changes were made.

No `AllScripts.sql` change was made.

No POS SQL change was made.

Any future database change must be preceded by live schema inspection and must follow SQL Server 2012 compatibility and DROP + CREATE stored procedure rules.
