# Payment Voucher Production Validation

Date: 2026-05-18

Scope: سند الصرف / Payment Voucher only. Databases inspected: `Eng`, `Cash`, `Dania`.

## Verdict

Not production-ready yet.

The web routes and shared architecture are in place, and voucher serial generation (`NoteSerial`, `NoteSerial1`) is using the counter procedure path. However, the installed save procedure still contains `MAX(...) + 1` allocation for `Notes.NoteID` and `DOUBLE_ENTREY_VOUCHERS.Double_Entry_Vouchers_ID`. Because the validation requirement explicitly includes “no MAX+1 remains,” I did not run create/edit/delete write tests against the databases.

## Tests Run

### Installed procedure audit

| DB | Save proc installed | Calls serial allocator | Uses app lock | MAX+1 NoteID | MAX+1 journal ID | Header proc |
| --- | --- | --- | --- | --- | --- | --- |
| Cash | yes | yes | yes | yes | yes | yes |
| Dania | yes | yes | yes | yes | yes | missing |
| Eng | yes | yes | yes | yes | yes | yes |

Result: fail for production write validation until `NoteID` and journal ID allocation are moved to a safe allocator or another proven concurrency-safe existing source.

### Numbering rollback smoke

Executed `dbo.usp_DynamicErpVoucher_NextSerial` inside an explicit transaction and rolled back.

| DB | Branch | Prefix | Return | Generated sample | Tail before | Tail inside tx | Tail after rollback | Result |
| --- | ---: | --- | ---: | --- | ---: | ---: | ---: | --- |
| Cash | 1 | `<NULL>` | 0 | `12605009` | 8 | 9 | 8 | pass |
| Dania | 10 | `<NULL>` | 0 | `1026050002` | 257 | 2 | 257 | pass |
| Eng | 1 | `AJV` | 0 | `12605001` | null | 1 | null | pass |

Notes:

- Rollback did not consume the serial counter.
- Dania showed a counter-scope anomaly during the transaction (`TailBefore=257`, `TailInside=2`) and needs a closer counter-scope review before write testing.
- Concurrent save was not run because the save procedure still uses MAX+1 for non-serial IDs.

### Existing data integrity

| DB | Payment vouchers | Duplicate `NoteSerial1` scoped | Orphan journal lines | Vouchers without journal | Unbalanced vouchers | Duplicate journal line numbers |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Cash | 720 | 1 | 0 | 0 | 1 | 1457 |
| Dania | 13762 | 1 | 0 | 0 | 4 | 2574 |
| Eng | 6162 | 0 | 0 | 421 | 0 | 1654 |

Sample findings:

- Cash duplicate scoped serial: branch `1`, prefix `<NULL>`, serial `0`, count `24`.
- Cash unbalanced voucher: `NoteID=414133`, serial `1.2501e+007`, debit `181199.36`, credit `181199.68`.
- Dania duplicate scoped serial: branch `12`, prefix `<NULL>`, serial `1.22602e+009`, count `2`.
- Dania unbalanced samples include `NoteID=190339`, `187945`, `188283`, `71505`.
- Eng has 421 payment vouchers without journal rows, sample `NoteID=219110` through `219106` dated `2026-04-06`.

### Linked table orphan checks

| DB | `TblNotesBillBuyPayment` | `TblNotesBillProjectPayment` | `TblNotesBillVindorPayment` | `TblChecqueBoxContent1` |
| --- | ---: | ---: | ---: | ---: |
| Cash | 0 | 0 | 0 | 0 |
| Dania | 1 | 0 | 0 | 0 |
| Eng | 0 | 0 | 0 | 0 |

Result: mostly clean, except Dania has one orphan buy-payment allocation.

### POS/MainERP routing and isolation

Code validation only; no browser runtime test was run because build/runtime is blocked by missing legacy web targets.

- POS list/details/edit routes stay in `Areas/Pos/Controllers/PaymentsController.cs`.
- POS views use `area = "Pos"` for save, edit, details, print, post, and delete links.
- POS voucher views use `Layout = null` and POS styles/partials, not MainERP layout.
- MainERP routes stay in `Areas/MainErp/Controllers/PaymentsController.cs`.
- MainERP views use `area = "MainErp"` and `_MainErpLayout.cshtml`.
- Shared business/data logic is reused through `Areas/MainErp/Repositories/Payments/*` and POS wrapper repositories.
- POS voucher repositories use `FinanceVoucherDbConnectionFactory`, which resolves the MainERP active finance connection rather than the Kishny POS connection.

Result: route/layout isolation passes by code inspection. Live route test pending.

### Permissions

Code validation:

- MainERP checks legacy screen permission `FrmPayments` for view/add/edit/delete/print.
- POS checks POS legacy permissions for `FrmPayments`.
- Server-side save/delete/post/print actions are permission-gated.

Result: pass by code inspection. Runtime permission matrix test pending.

### Print flow

Validated by code inspection and previous report audit:

- Existing HTML print remains at MainERP `Print(int id)` and POS `PrintVoucher(int id)`.
- Legacy Crystal boundary endpoints exist:
  - MainERP: `/MainErp/Payments/LegacyCrystalPrint/{id}`
  - POS: `/Pos/Payments/LegacyCrystalPrintVoucher/{id}`
- Boundary endpoints resolve the VB6 report contract and return `501` while Crystal rendering is not implemented, avoiding fake report output.

Result: safe boundary exists. Crystal parity not ready.

### Build/runtime

`dotnet build MyERP.sln --no-restore` fails before compiling the web project because the machine lacks:

`Microsoft.WebApplication.targets`

This is an environment/tooling blocker, not evidence that the changed payment voucher code compiles.

## Tests Not Run

I did not run create/edit/delete, rollback save, or concurrent save against `Eng`, `Cash`, or `Dania` because installed `usp_DynamicErpVoucher_Save` still violates the no-MAX+1 validation requirement for `NoteID` and journal ID allocation.

Treasury effect and bank effect were validated only by SQL/code inspection:

- Cash payment credits `TblBoxesData.Account_Code`.
- Bank transfer credits `BanksData.Account_Code`.
- Party/account side is debited.
- VAT and transfer-expense branches exist in SQL, but transfer expense is not exposed by the current shared web UI/write repository.

## Unsupported / Blocked Payment Cases

Currently supported by the safe save procedure:

- Payment method `0`: cash.
- Payment method `2`: bank transfer.
- Cashing types `0`, `1`, `2`, `5`.

Blocked or incomplete:

- Cheque payment method `1`.
- Deferred cheque `3`.
- Account / later payment methods `4`, `5`.
- Salary, employee advance, vacation entitlement, end of service, VAT avowal, project-specific linked flows.
- Deposit transfer full beneficiary/remitter fields.
- Physical cheque print.
- Transfer expense UI/repository fields.

## Remaining Risks

- `MAX(NoteID)+1` and `MAX(Double_Entry_Vouchers_ID)+1` remain in installed save procedures.
- Existing data contains duplicates/unbalanced/no-journal cases that can affect reports and validation.
- Dania is missing `usp_DynamicErpVoucher_Header`, so its web read/detail flow is not compatible yet.
- Dania has one orphan `TblNotesBillBuyPayment` row.
- Crystal report parity is not implemented; only safe endpoint boundaries exist.
- Runtime POS/MainERP route and permission tests still need a working web host.
- Arabic messages in some older MainERP controller/source areas appear mojibake in the source/output and need a file-encoding cleanup pass before final production release.

## Required Next Step Before Write Testing

Replace the remaining `MAX+1` allocation in `usp_DynamicErpVoucher_Save`:

- `Notes.NoteID`
- `DOUBLE_ENTREY_VOUCHERS.Double_Entry_Vouchers_ID`
- `DOUBLE_ENTREY_VOUCHERS.DEV_ID_Line_No1` if it is treated as a business serial

After that, rerun:

- create cash voucher;
- create bank-transfer voucher;
- edit voucher and confirm old journal rebuild;
- delete voucher and confirm cleanup;
- forced rollback save;
- concurrent save;
- POS and MainERP browser route tests;
- print boundary tests.
