# LC Safe Shell Mapping

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

## Grid Mapping

| VB6 grid | Intended table/behavior | Web status |
| --- | --- | --- |
| `GrdBondHistory` | LC history / `TBLLCHistory` | Placeholder grid only |
| `GrdMargin` | Margin/financial invoices / `TBLLCMargin` | Placeholder grid only |
| `GrdMargin2` | Margin payment / revised bond amount | Placeholder grid only |
| `GrdMargin3` | Opening balance rows / `tblLCOpenB` | Placeholder grid only |
| `GrdMargin4` | Refinance / acceptance advice / `TBLLCMargin2` | Placeholder grid only |

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
