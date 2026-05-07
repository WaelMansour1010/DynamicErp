# LC Real Screen Implementation

Date: 2026-05-07

## Scope

Implemented a real MainErp LC workspace foundation based on the active VB6 screen:

`F:\Source Code\SatriahMain\Frm\New frm\FrmLC.frm`

This is still a safe read-only shell. It is not a CRUD screen and does not save or post accounting.

## Routes

- `/MainErp/LC`
- `/MainErp/LC/Open/{id}`
- `/MainErp/LC/New`
- `/MainErp/LC/Edit/{id}`
- `/MainErp/LC/Details/{id}`
- `/MainErp/JournalEntries/DetailsByNote/{noteId}`
- `/MainErp/JournalEntries/DetailsByVoucher/{voucherId}`

## Implemented Screen Sections

The LC workspace now includes:

- `بيانات الاعتماد`
- `البنك والعملة والقيم`
- `الحسابات المرتبطة ومعاينة المخاطر`
- `التواريخ والرصيد الافتتاحي`
- `أرقام القيود الرئيسية`
- `هامش الاعتماد / دفعات الهامش / تاريخ الاعتماد / الرصيد الافتتاحي`
- `القيود والمستندات المرتبطة`
- `Trace القيود الفعلية`
- `معاينة محاسبية آمنة`

## Real Read-Only Data Now Loaded

From `TblLC`:

- LC header fields,
- bank/currency/vendor/project fields,
- value/open value,
- dates,
- opening balance fields,
- linked account codes,
- parent account codes,
- main note fields:
  - `NoteID`
  - `NoteSerial`
  - `NoteID2`
  - `NoteSerial2`
  - `NoteIDOpen`
  - `NoteSerialOpen`
  - `opening_balance_voucher_id`

From `ACCOUNTS`:

- account names,
- account serials,
- parent account names,
- parent last-account warning.

Account display rule:

- `ACCOUNTS.Account_Code` is an internal posting/join key only.
- User-visible account identity is:
  - `Account_Serial + " - " + Account_Name`
- If an account code stored on `TblLC` does not exist in `ACCOUNTS`, the UI shows:
  - `الحساب غير موجود`
- Raw account codes are not shown as primary LC UI labels.

From grid tables when present:

- `TBLLCHistory`
- `TBLLCMargin`
- `TBLLCMargin2`
- `tblLCOpenB`

From accounting tables:

- `Notes`
- `Notes1`
- `DOUBLE_ENTREY_VOUCHERS`
- `DOUBLE_ENTREY_VOUCHERS1`

## Voucher Opening

Implemented read-only journal details:

- `JournalEntriesController.DetailsByNote`
- `JournalEntriesController.DetailsByVoucher`
- `JournalEntryReadRepository.GetDetailsByNote`
- `JournalEntryReadRepository.GetDetailsByVoucher`
- `Views\JournalEntries\Details.cshtml`

The details view shows:

- note header,
- source table,
- note serial/type/date,
- linked LC id,
- debit/credit lines,
- account names,
- totals,
- balance difference.

## Disabled / Protected Actions

Visible but protected:

- حفظ فعلي
- ترحيل
- حذف
- إنشاء قيود فعلية
- إعادة بناء القيود
- طباعة / تقرير

They show:

`هذه الوظيفة لم يتم ترحيلها بعد - Read Only Mode`

## Not Implemented Yet

- `SaveData`
- account creation through `ModAccounts.AddNewAccount`
- account edit through `ModAccounts.EditAccount`
- `createVoucher`
- `CREATE_VOUCHER_GE`
- `createVoucher2`
- `CREATE_VOUCHER_GE2`
- close LC workflow
- deleting/rebuilding old vouchers
- report execution
- grid editing/saving

## Risk Notes

LC posting is high risk because one LC can create many notes and voucher rows. Edit mode in VB6 can delete and rebuild `Notes`, `Notes1`, `DOUBLE_ENTREY_VOUCHERS`, `DOUBLE_ENTREY_VOUCHERS1`, and grid rows. This must remain disabled until the transaction and rollback behavior is implemented and tested.

## Validation

Build succeeded after implementation.

Read-only SQL validation against `Eng` using `TblLCID = 195` found:

- LC header exists.
- Notes exist.
- Voucher rows exist in `DOUBLE_ENTREY_VOUCHERS`.
- `TBLLCMargin` rows exist.
- `TBLLCMargin2` rows exist.

No database writes were performed.
