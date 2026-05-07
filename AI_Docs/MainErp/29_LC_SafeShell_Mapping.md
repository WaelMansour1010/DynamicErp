# LC Safe Shell Mapping - Phase 2

Date: 2026-05-07

Scope: first safe correction of MainErp LC screen to resemble the active VB6 source `F:\Source Code\SatriahMain\Frm\New frm\FrmLC.frm`.

No database writes were added. No posting, account creation, Notes creation, `DOUBLE_ENTREY_VOUCHERS`, `DOUBLE_ENTREY_VOUCHERS1`, delete, close, or report execution is enabled.

## Screen Status

MainErp route:

- `/MainErp/LC`
- `/MainErp/LC/Details/{id}`

Current status:

- Real migrated from VB6 / Safe Shell.
- Read/search/view only.
- Dangerous actions are disabled and marked `Not migrated yet`.
- Dangerous action buttons show: `هذه الوظيفة لم يتم ترحيلها بعد - Read Only Mode`.
- Phase 2 added a status panel and more VB6 controls as read-only fields or `Not mapped yet` placeholders.

## Full VB6 Control Inventory

This inventory is extracted from the active `FrmLC.frm` file. Status values:

- `Present read-only`: visible in MainErp and bound to confirmed read-only data.
- `Present placeholder`: visible in MainErp, but source/table mapping still needs validation.
- `Grid shell`: visible as preview shell only; no grid editing or saving.
- `Disabled action`: visible but no write/post/delete behavior.
- `Not in web yet`: still absent from the current shell.

| VB6 control | Type | Expected source / behavior | Web status | Save later? |
| --- | --- | --- | --- | --- |
| `TXTTblLCID` | TextBox | `TblLC.TblLCID` | Present read-only | Yes, create allocation later |
| `TXTLCNO` | TextBox | `TblLC.LCNO` | Present read-only | Yes |
| `txtName` | TextBox | `TblLC.Name` | Present read-only | Yes |
| `txtNameE` | TextBox | English LC name, column not confirmed in live doc | Present placeholder | Yes if column found |
| `TXTBank2` | TextBox | Secondary bank text / legacy helper, source unclear | Not in web yet | Needs source validation |
| `txtProjectName` | TextBox | `TblLC.projectName` | Present read-only | Yes |
| `txt_Currency_rate` | TextBox | `TblLC.Currency_rate` | Present read-only | Yes |
| `TXTValue` | TextBox | `TblLC.Value` | Present read-only | Yes |
| `txtOPenValue` | TextBox | `TblLC.OpenValue` | Present read-only | Yes |
| `txtOPenValue2` | TextBox | LG/open value helper, source not confirmed | Present placeholder | Needs source validation |
| `txtBondAmt` | TextBox | bond amount, source not confirmed | Present placeholder | Needs source validation |
| `txtPercentV` | TextBox | percentage value, source not confirmed | Present placeholder | Needs source validation |
| `txtAcceptianPeriod` | TextBox | acceptance period, source not confirmed | Present placeholder | Needs source validation |
| `TxtNoOfParcil` | TextBox | parcels count, source not confirmed | Present placeholder | Needs source validation |
| `txtGuaranteeNo` | TextBox | guarantee number, source not confirmed | Present placeholder | Needs source validation |
| `TXtPrimaryInvoiceNo` | TextBox | primary invoice number, source not confirmed | Present placeholder | Needs source validation |
| `txtRemarks` | TextBox | `TblLC.Remarks` | Present read-only | Yes |
| `TxtOpenBalance` | TextBox | `TblLC.OpenBalance` | Present read-only | Yes |
| `txtopening_balance_voucher_id` | TextBox | `TblLC.opening_balance_voucher_id` | Present read-only | No direct user save; generated later |
| `TxtNoteSerial` | TextBox | `TblLC.NoteSerial` | Present read-only | Generated later |
| `TxtNoteSerial2` | TextBox | `TblLC.NoteSerial2` | Present read-only | Generated later |
| `txtNoteSerialOpen` | TextBox | `TblLC.NoteSerialOpen` | Present read-only | Generated later |
| `TxtNoteID`, `TxtNoteID2`, `txtNoteIDOpen` | TextBox | note IDs | Not in web yet | Generated later |
| `txtNoteIDRowId`, `txtNoteID2RowId`, `txtNoteIDOpenRowId` | TextBox | note row IDs | Not in web yet | Generated later |
| `txtMarginTotal`, `txtMarginTotal2`, `txtMarginTotal3`, `txtMarginTotal4` | TextBox | grid totals | Grid shell | Calculated later |
| `txtTotalBondHistory` | TextBox | bond history total | Grid shell | Calculated later |
| `txtLGExpPeriod` | TextBox | LG expense period | Present placeholder | Needs source validation |
| `txtLGExpPeriodEnd` | TextBox | LG expense period end | Not in web yet | Needs source validation |
| `txtLGExpPeriodLast` | TextBox | LG expense period last | Present placeholder | Needs source validation |
| `txtCostDay` | TextBox | daily LG cost | Present placeholder | Needs source validation |
| `txtCostLGYear` | TextBox | yearly LG cost | Present placeholder | Needs source validation |
| `txtCostLGYearLast` | TextBox | last yearly LG cost | Not in web yet | Needs source validation |
| `TxtItemQty` | TextBox | item quantity / repeated in VB6 | Present placeholder | Needs source validation |
| `TxtItemPrice` | TextBox | item price / repeated in VB6 | Present placeholder | Needs source validation |
| `txtType`, `txtid`, `TxtModFlg`, `Text1` | TextBox | internal mode/helper fields | Not in web yet | Internal only |
| `DCLC` | DataCombo | `TblLC.LCTyperId` / `LCTypes` | Present read-only by id | Yes |
| `DCBank` | DataCombo | `TblLC.BankId` / `BanksData` | Present read-only | Yes |
| `DcBranch` | DataCombo | `TblLC.BranchID` / `TblBranchesData` | Present read-only by id | Yes |
| `DBCboClientName` | DataCombo | `TblLC.VendorId` / `TblCustemers` | Present read-only | Yes |
| `DCCountry` | DataCombo | `TblLC.CountryId` / `TblCountriesData` | Present read-only by id | Yes |
| `DCCUrrency` | DataCombo | `TblLC.CurrencyId` / `currency` | Present read-only | Yes |
| `DcboBankName` | DataCombo | `TblLC.BankID2` | Present read-only by id | Yes |
| `DcboBox` | DataCombo | `TblLC.BoxID` | Present read-only by id | Yes |
| `CboPaymentType` | ComboBox | `TblLC.PaymentTypeID` | Present read-only by id | Yes |
| `DboParentAccount` | DataCombo | parent LC account | Present placeholder | Yes |
| `cmbAccountLGParent` | DataCombo | `TblLC.AccountLGParent` | Present read-only | Yes |
| `cmbAccountMarginParent` | DataCombo | `TblLC.AccountMarginParent` | Present read-only | Yes |
| `cmbAccountAcceptanceParent` | DataCombo | `TblLC.AccountAcceptanceParent` | Present read-only | Yes |
| `cmbAccountExpensParent` | DataCombo | `TblLC.AccountExpensParent` | Present read-only | Yes |
| `cmbAccountExpProject` | DataCombo | `TblLC.AccountExpProject` | Present read-only | Yes |
| `cmbAccount` | DataCombo | account helper | Present placeholder | Needs source validation |
| `DCPreFix` | DataCombo | prefix | Present placeholder | Needs source validation |
| `dcopr` | DataCombo | operator/operation helper | Present placeholder | Needs source validation |
| `dcproject` | DataCombo | `TblLC.project_id` | Present read-only | Yes |
| `Dcterm` | DataCombo | term lookup | Present placeholder | Needs source validation |
| `dcitems` | DataCombo | item lookup | Present placeholder | Needs source validation |
| `DataCombo1`, `DataCombo2`, `DCboUserName` | DataCombo | misc/user filters/helpers | Not in web yet | Needs source validation |
| `dbFromDate` | DTPicker | `TblLC.FromDate` | Present read-only | Yes |
| `dbTodate` | DTPicker | `TblLC.Todate` | Present read-only | Yes |
| `DpCloseDate` | DTPicker | `TblLC.CloseDate` | Present read-only | Yes |
| `DPLastParcilDate` | DTPicker | `TblLC.LastParcilDate` | Present read-only | Yes |
| `DtpChequeDueDate` | DTPicker | `TblLC.ChequeDueDate` | Present read-only | Yes |
| `Dtp` | DTPicker | opening balance date | Not in web yet | Needs source validation |
| `txtGuaranteeDate` | DTPicker | guarantee date | Present placeholder | Needs source validation |
| `txtLGExpiryDate` | DTPicker | LG expiry date | Present placeholder | Needs source validation |
| `ChkLocked` | CheckBox | `TblLC.Locked` | Present read-only as status | Yes |
| `ChKauto`, `Check1` | CheckBox | internal flags | Not in web yet | Needs source validation |
| `OptType(0/1/2)` | OptionButton | `TblLC.OpenBalanceType` | Present read-only | Yes |
| `Option1`, `Option2` | OptionButton | opening/account behavior | Not in web yet | Needs source validation |
| `optTypeLCLG(0/1)` | OptionButton | LC/LG type behavior | Present placeholder | Yes |
| `Fg` | VSFlexGrid | one of LC detail grids; source needs column trace | Grid shell | Yes, after mapping |
| `Grid` | VSFlexGrid | item/details grid | Not in web yet | Yes, after mapping |
| `GrdMargin` | VSFlexGrid | `TBLLCMargin` | Grid shell | Yes |
| `GrdBondHistory` | VSFlexGrid | `TBLLCHistory` | Grid shell | Yes |
| `GrdMargin2` | VSFlexGrid | `TBLLCMargin` payment/revised amount | Grid shell | Yes |
| `GrdMargin3` | VSFlexGrid | `tblLCOpenB` | Grid shell | Yes |
| `GrdMargin4` | VSFlexGrid | `TBLLCMargin2` | Grid shell | Yes |
| `cmdAddLine` | CommandButton | add grid rows | Disabled action / grid shell | Yes |
| `CmdCreateV`, `CmdCreateV2` | CommandButton | voucher creation | Disabled action | Yes, after accounting migration |
| `Command2`, `Command3`, `Command4`, `Command5`, `Command6`, `Command7`, `Command9`, `Command1` | CommandButton | voucher/opening/print/grid helper actions | Disabled or not in web yet | Needs per-event migration |
| `cmdCloseLC` | CommandButton | close LC | Disabled action | Yes, after close flow migration |
| `cmdPrintEntryClose` | CommandButton | print close voucher | Disabled action | Yes |
| `btnQuery` | ISButton | search | Present read-only | No write |
| `BtnUpdate` | ISButton | update/edit | Disabled action | Yes |
| `BtnPrint` | ISButton | print/report | Disabled action | Yes |
| `Cmd(Index)` | ISButton array | new/edit/save/delete/print/search/navigation | Search active; dangerous actions disabled | Yes |
| `XPBtnMove` | ISButton | record navigation | Not in web yet | Optional later |
| `ISButton1` | ISButton | close/exit helper | Not in web yet | Optional later |

## Field Mapping

| VB6 control / field | Web safe shell field | Source |
| --- | --- | --- |
| `TXTTblLCID` | `TblLCID` strip/detail | `TblLC.TblLCID` |
| `TXTLCNO` | رقم الاعتماد | `TblLC.LCNO` |
| `txtName` | اسم الاعتماد | `TblLC.Name` |
| `DCLC` | نوع الاعتماد | `TblLC.LCTyperId` |
| `optTypeLCLG` | اعتماد / ضمان status placeholder | Requires VB6 option mapping validation |
| `DCBank` | البنك | `TblLC.BankId` + `BanksData.BankName` |
| `DcboBankName` | بنك السداد | `TblLC.BankID2` |
| `DcboBox` | الصندوق | `TblLC.BoxID` |
| `DBCboClientName` | المورد | `TblLC.VendorId` + `TblCustemers.CusName` |
| `DCCountry` | الدولة | `TblLC.CountryId` |
| `DCCUrrency` | العملة | `TblLC.CurrencyId` + `currency.name` |
| `txt_Currency_rate` | سعر الصرف | `TblLC.Currency_rate` |
| `TXTValue` | قيمة الاعتماد | `TblLC.Value` |
| `txtOPenValue` | قيمة الفتح | `TblLC.OpenValue` |
| `CboPaymentType` | نوع السداد | `TblLC.PaymentTypeID` |
| `TxtChequeNumber` | رقم الشيك | `TblLC.ChequeNumber` |
| `DtpChequeDueDate` | تاريخ الشيك | `TblLC.ChequeDueDate` |
| `dcproject` / `txtProjectName` | المشروع | `TblLC.project_id`, `TblLC.projectName` |
| `DcBranch` | الفرع | `TblLC.BranchID` |
| `cmbAccountLGParent` | الحساب الرئيسي للاعتماد | `TblLC.AccountLGParent` |
| `LCAccount_Code` | حساب الاعتماد | `TblLC.Account_Code` |
| `cmbAccountMarginParent` | الحساب الرئيسي للهامش | `TblLC.AccountMarginParent` |
| `Account_CodeMargin` | حساب الهامش | `TblLC.Account_CodeMargin` |
| `cmbAccountAcceptanceParent` | الحساب الرئيسي للقبول | `TblLC.AccountAcceptanceParent` |
| `AcceptAccount_Code` | حساب القبول | `TblLC.AcceptAccount_Code` |
| `cmbAccountExpensParent` | الحساب الرئيسي للمصروفات | `TblLC.AccountExpensParent` |
| `AccountExpensCode` | حساب المصروفات | `TblLC.AccountExpensCode` |
| `dbFromDate` | من تاريخ | `TblLC.FromDate` |
| `dbTodate` | إلى تاريخ | `TblLC.Todate` |
| `DpCloseDate` | تاريخ الإغلاق | `TblLC.CloseDate` |
| `DPLastParcilDate` | آخر تاريخ شحن | `TblLC.LastParcilDate` |
| `TxtOpenBalance` | الرصيد الافتتاحي | `TblLC.OpenBalance` |
| `OptType` | نوع الرصيد | `TblLC.OpenBalanceType` |
| `TxtNoteSerial` | رقم قيد LC | `TblLC.NoteSerial` |
| `TxtNoteSerial2` | رقم قيد الهامش | `TblLC.NoteSerial2` |
| `txtNoteSerialOpen` | رقم قيد الافتتاح | `TblLC.NoteSerialOpen` |
| `txtRemarks` | ملاحظات | `TblLC.Remarks` |
| `opening_balance_voucher_id` | Opening voucher id | `TblLC.opening_balance_voucher_id` |
| `AccountExpProject` | حساب مصروف المشروع | `TblLC.AccountExpProject` |

## Grid Mapping

| VB6 grid | Intended table/behavior | Web status |
| --- | --- | --- |
| `GrdBondHistory` | LC history / `TBLLCHistory` | Placeholder grid only |
| `GrdMargin` | Margin/financial invoices / `TBLLCMargin` | Placeholder grid only |
| `GrdMargin2` | Margin payment / revised bond amount | Placeholder grid only |
| `GrdMargin3` | Opening balance rows / `tblLCOpenB` | Placeholder grid only |
| `GrdMargin4` | Refinance / acceptance advice / `TBLLCMargin2` | Placeholder grid only |
| Notes preview | linked vouchers/movements | Read-only preview from `Notes` by `TblLCID` when schema allows |

## Button Mapping

| VB6 button/event | Web label | Status |
| --- | --- | --- |
| `Cmd_Click(0)` | جديد | Disabled / Not migrated yet |
| `Cmd_Click(1)` | تعديل | Disabled / Not migrated yet |
| `Cmd_Click(2)` | حفظ | Disabled / Not migrated yet |
| `Cmd_Click(4)` | حذف | Disabled / Not migrated yet |
| `Cmd_Click(5)` / `btnQuery` | بحث | Read-only active |
| `BtnPrint` / `print_report` | طباعة / تقرير | Disabled / Not migrated yet |
| `CmdCreateV_Click` | إنشاء القيد | Disabled / Not migrated yet |
| `Command2_Click` | حذف القيد | Disabled / Not migrated yet |
| `cmdCloseLC_Click` | إقفال الاعتماد | Disabled / Not migrated yet |
| `Command9_Click` / `cmdPrintEntryClose_Click` | طباعة القيد | Disabled / Not migrated yet |

## Deferred Financial Logic

The following VB6 behavior is intentionally not enabled:

- `SaveData` transaction and manual `TblLCID` allocation.
- Account auto-generation through `get_account_code_branch`, `GetNewAcountCode`, `CHECK_LAST_ACCOUNT`, and `ModAccounts.AddNewAccount`.
- LC voucher creation through `CmdCreateV_Click`, `createVoucher`, and `CREATE_VOUCHER_GE`.
- Opening balance voucher creation through `Command3_Click`, `createVoucher2`, and `CREATE_VOUCHER_GE2`.
- Deleting or rebuilding related `Notes`, `Notes1`, `DOUBLE_ENTREY_VOUCHERS`, or `DOUBLE_ENTREY_VOUCHERS1`.
- Grid edit behavior for `GrdMargin`, `GrdMargin2`, `GrdMargin3`, `GrdMargin4`, and `GrdBondHistory`.
- Close voucher behavior in `cmdCloseLC_Click`.
- Report/Crystal execution from `FrmLC_Report`.

## Test Method

1. Configure `MainErp_ConnectionString` to the representative legacy database.
2. Open `/MainErp/LC`.
3. Search by LC number, bank id, vendor id, or branch id.
4. Select an LC from the result list.
5. Confirm header, bank/currency, account, date, and note serial fields render read-only.
6. Confirm dangerous buttons are disabled.
7. Confirm no database write occurs.
