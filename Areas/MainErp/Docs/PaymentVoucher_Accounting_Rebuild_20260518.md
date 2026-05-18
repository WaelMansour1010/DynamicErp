# Payment Voucher Accounting Rebuild - 2026-05-18

## Scope

Screen: Payment Voucher / سند الصرف.

SQL changed:

- `F:\Source Code\DynamicErp\Areas\MainErp\Sql\16_PaymentVoucher_VB6_SaveParity.sql`
- `F:\Source Code\DynamicErp\Areas\MainErp\Sql\17_PaymentVoucher_Accounting_Rebuild.sql`
- `F:\Source Code\DynamicErp\MyERP.csproj`

VB6 source traced:

- `F:\Source Code\SatriahMain\Frm\FrmPayments.frm`
- `F:\Source Code\SatriahMain\Bas\SalimNew.bas`
- `F:\Source Code\SatriahMain\Bas\registry.bas`

## VB6 Intent Implemented

`FrmPayments.SaveData` treats payment vouchers as `NoteType = 5`.

On edit, VB6 deletes existing `DOUBLE_ENTREY_VOUCHERS` rows for the note and rebuilds them. The web procedure now follows that intent atomically in one transaction.

General accounting direction:

- Debit paid party/account.
- Debit VAT when present.
- Debit transfer expense when present.
- Debit transfer expense VAT when present.
- Credit treasury or bank account for the total cash/bank movement.

VAT account source:

- Uses `TblSettsReqLimK.AccDep`.
- Date-bounded by voucher date.
- `TransType = 23`.
- `(AccOrTran = 1 OR AccOrTran IS NULL)`.

Serial behavior:

- Create only: calls `dbo.usp_DynamicErpVoucher_NextSerial` for both `NoteSerial` and `NoteSerial1`.
- Edit: preserves existing `NoteSerial` and `NoteSerial1`.
- Delete: does not decrement or reuse counters.
- Runtime allocation uses `SerialCounters_V2`, `sp_getapplock`, and `UPDLOCK/HOLDLOCK`; no `MAX(NoteSerial)+1` or `MAX(NoteSerial1)+1` remains in the save path.

## Supported Save Cases

The safe save currently supports:

- Cash payment source, `@paymentMethod = 0`, credited from `TblBoxesData.Account_Code`.
- Bank transfer payment source, `@paymentMethod = 2`, credited from `BanksData.Account_Code`.
- Voucher cashing types `0`, `1`, `2`, and `5`.
- Optional VAT.
- Optional transfer expense and transfer-expense VAT when the expense account is explicitly supplied.

The procedure blocks unsupported payment method/cashing-type cases with Arabic validation instead of writing incomplete accounting.

## Intentional Modernization

VB6 business meaning was preserved, but unsafe patterns were not copied:

- Replaced serial `MAX+1` behavior with transactional counter rows.
- Validates leaf, unblocked accounts before writing.
- Validates balanced journal before and after insert.
- Deletes and rebuilds journal rows inside the same transaction on edit.
- Blocks linked vouchers rather than partially editing allocations from the wrong workflow.
- Blocks cheque payment save for now because cheque table behavior needs the full `TblChecqueBoxContent1` rebuild path before production enablement.

## Linked-Table Cleanup

Added shared cleanup procedure:

- `dbo.usp_DynamicErpVoucher_CleanupLinkedRows`

It is called by:

- `dbo.usp_DynamicErpVoucher_Save` on edit before rebuilding journal rows.
- `dbo.usp_DynamicErpVoucher_Delete` before deleting the note.

Cleanup is transaction-scoped and covers:

- `DOUBLE_ENTREY_VOUCHERS`
- `TblNotesBillBuyPayment`
- `TblBillBuyPayment`
- `TblNotesBillProjectPayment`
- `TblBillProjectPayment`
- `TblNotesBillVindorPayment`
- `TblBillVindorPayment`
- `TblChecqueBoxContent1`
- `ReciveDetails`
- `TblSalaryNotesPayment`
- `marakes_taklefa_temp`
- `TblEmpAdvance`
- `TblEmpAdvanceDetails`
- `TblEmpAdvanceRequest.AccAproved`
- prepaid flags in `TblPripaidExpensesDet`
- supplier attribution flags in `TblAttributionInstallmentDivided`
- vendor installment flags in `TblQestFexed`
- vacation/end-service/VAT paid flags where the voucher type confirms the link

Blocked cases:

- Posted payment voucher.
- Asset-linked payment voucher.
- Paid/settled cheque row.
- Employee advance that already has rows in `TblEmpAdvancePayedDet`.
- Prepaid expense rows that already have extinguishing rows in `TblPripaidExpChiled`.
- Malformed legacy CSV link fields, because those cannot be cleaned safely.

## Tests Run

Server: `Wael\Sql2019`

Installed successfully on:

- `Eng`
- `Cash`
- `Dania`

Rollback save tests:

| DB | Voucher ID | NoteSerial1 | Result |
| --- | ---: | --- | --- |
| Eng | 222119 | 12605001 | 3 lines, debit 115.00, credit 115.00, rollback removed note |
| Cash | 1457128 | 12605004 | 3 lines, debit 115.00, credit 115.00, rollback removed note |
| Dania | 197821 | 1226050014 | 3 lines, debit 115.00, credit 115.00, rollback removed note |

Create/edit/delete tests:

| DB | Created ID | Create total | Edit total | Cleanup |
| --- | ---: | ---: | ---: | --- |
| Eng | 222119 | 115.00 | 138.00 | `Notes`, `DOUBLE_ENTREY_VOUCHERS`, `TblChecqueBoxContent1` all removed |
| Cash | 1457128 | 115.00 | 138.00 | `Notes`, `DOUBLE_ENTREY_VOUCHERS`, `TblChecqueBoxContent1` all removed |

Concurrent save tests:

| DB | Sessions | NoteSerial1 values | Journal check | Cleanup |
| --- | ---: | --- | --- | --- |
| Eng | 4 | 12605002..12605005 | all balanced 10.00/10.00 | removed |
| Cash | 4 | 12605005..12605008 | all balanced 10.00/10.00 | removed |

Bank transfer with transfer expense rollback:

| DB | Voucher ID | Lines | Debit | Credit | Cleanup |
| --- | ---: | ---: | ---: | ---: | --- |
| Eng | 222119 | 5 | 120.75 | 120.75 | rollback removed note |

Unsupported cheque save:

- `@paymentMethod = 1` returned Arabic validation: "طريقة الدفع المختارة غير مدعومة في الحفظ الآمن الحالي. المدعوم حاليا: نقدي أو تحويل بنكي."
- No `Notes` row was inserted.

Linked cleanup tests:

| DB | Scenario | Result |
| --- | --- | --- |
| Eng | Created voucher, attached buy/project/vendor allocations, cheque row, `ReciveDetails`, salary row, advance rows, and cost-center temp row, then edited | all linked rows cleaned; source paid flags reset; journal rebuilt |
| Eng | Reattached cheque and `ReciveDetails`, then deleted | `Notes`, journal, cheque, and receipt rows removed |
| Cash | Created voucher with buy/vendor allocations, cheque row, `ReciveDetails`, and salary row, then edited/deleted | linked rows cleaned, source flags reset, note and journal removed |
| Eng | Delete with paid cheque | blocked with Arabic message and rows remained |
| Eng | Edit posted voucher | blocked with Arabic message |
| Eng | Delete posted voucher | blocked with Arabic message and note remained |

Post-test cleanup:

- `Eng`: zero `TEST-%` payment voucher notes remain.
- `Cash`: zero `TEST-%` payment voucher notes remain.
- `Dania`: zero `TEST-%` payment voucher notes remain.

## Remaining Unsupported Cases

These are intentionally blocked or not expanded in this pass because the linked-table workflow must be rebuilt from VB6 before enabling production writes:

- Cheque payment table lifecycle in `TblChecqueBoxContent1`.
- Bill allocation writes:
  - `TblNotesBillBuyPayment`
  - `TblNotesBillProjectPayment`
  - `TblNotesBillVindorPayment`
- `ReciveDetails` linked paths.
- Salary, prepaid, annual vacation, end-service, and employee advance link paths.
- Payment methods `4` and `5` account-settlement variants.

## UI Note

The accounting SQL uses internal `Account_Code` only for database linkage. User-facing lookup/display must continue to show `Account_Serial` and `Account_Name`, not raw `Account_Code`.
