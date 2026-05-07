# POS Sales Invoice Excel Import Discovery - 2026-05-05

## MD / Rules Read

- `F:\Source Code\SatriahMain\CODEX_RULES.md`
- `F:\Source Code\SatriahMain\DEVELOPMENT_GUIDELINES.md`
- `F:\Source Code\SatriahMain\PROJECT_REFERENCE.md`
- `F:\Source Code\SatriahMain\DATABASE_SCHEMA.md`
- DynamicErp POS docs discovered: `F:\Source Code\DynamicErp\Areas\Pos\Technical_Handover_Document.md`, `F:\Source Code\DynamicErp\Docs\POS_SYSTEM_DOCUMENTATION.md`

Important correction for POS work: despite the legacy `CODEX_RULES.md` saying SQL changes go to `SatriahMain\Main Script\AllScripts.sql`, POS web-module SQL must be kept only under `F:\Source Code\DynamicErp\Areas\Pos\Sql`.

## AllScripts.sql Check

- Checked: `F:\Source Code\SatriahMain\Main Script\AllScripts.sql`
- Current git status for this file: clean.
- POS/import search terms found no POS script blocks: `usp_POS_`, `POS_`, `POS_Save`, `POS_Import`, `StockTransfer`, `SalesInvoiceExcelImport`, `FinancialDiagnostics`, `ImportBatch`, `ImportInvoice`, `Kishny POS`, `Keshni`, `Cayshny`.
- One generic legacy match remains at line 1400: `WHERE T.Transaction_Type = 21`; this is part of an existing legacy stock/query block, not POS web import SQL.
- No POS SQL was removed from `AllScripts.sql` because no POS SQL block was found there in the current working tree.

## Excel Files Inspected

Folder: `F:\Source Code\DynamicErp\Areas\Pos\Doc`

1. `سنورس (2).xlsx`
2. `شيت المعاملات ميت غمر (1).xlsx`
3. `شيت المعاملات ميت غمر (2).xlsx`

All three workbooks have the same sheet pattern:

- Daily sheets: `1 `, `2`, `3`, ..., `31`
- Monthly summary sheet: `Total`
- Card sheet: `كــــــــروت`

Daily sheets use Excel Tables:

- Main service table: `B3:J226`
- Token/card table: usually `L11:M226`

Visible daily sheet headers:

- `B`: م
- `C`: IPN
- `D`: إسم العميل
- `E`: رقم التليفون
- `F`: قيمة الشحنه
- `G`: قيمة الرسوم / العموله
- `H`: الاجمــــــــــالي
- `I`: نوع الخدمة
- `J`: التاريخ
- `L:M`: token list headed by `#` / `التوكن`
- `O:P`: daily totals and reconciliation values
- `R:W`: targets / achievement percentages

Merged / formatted structure:

- Daily sheets contain merged cells such as `A4:A226`, `F1:H1`, `R10:W10`, plus several `O:P` merged labels.
- No hidden rows/columns were found in the sampled daily sheets.
- The `Total` sheet is a summary/report, not an import source for transaction rows.

## Workbook Data Counts

`سنورس (2).xlsx`:

- Transaction rows detected: 33
- Token rows detected: 6
- Services detected: all populated transaction rows are `كاش ان`
- Active days: sheet `2` has 6 transactions and 1 token, sheet `3` has 11 transactions and 2 tokens, sheet `4` has 16 transactions and 3 tokens.
- Example transaction: sheet `2`, row 4, IPN `2605020000561`, customer `محمد`, phone `01063691891`, amount `425`, fee `35`, total `460`, service `كاش ان`, date `2.5.2026`.
- Example token: sheet `2`, row 12, `M3484770000000009c`.

`شيت المعاملات ميت غمر (1).xlsx`:

- Transaction rows detected: 13
- Token rows detected: 6
- Active day: sheet `9`, but the row date is `4.5.2026`.
- Services detected: 10 rows `كاش ان`, 3 rows with missing service.
- Example transaction: sheet `9`, row 4, IPN `IPN2605040000571`, customer `الاء`, phone `0125545454`, amount `1820`, fee `72.8`, total `1892.8`, service `كاش ان`, date `4.5.2026`.
- Example token: sheet `9`, row 12, `He484770000000009a`.

`شيت المعاملات ميت غمر (2).xlsx`:

- Same detected data shape/counts as `(1)`: 13 transaction rows, 6 token rows, active sheet `9`, date `4.5.2026`.
- Treat `(1)` and `(2)` as likely duplicate source files until file hash/import policy confirms otherwise.

## Excel Import Interpretation

The files are not normal itemized invoices. They are daily operational transaction sheets:

- Each populated row in `B:J` appears to represent one cash-in service transaction, not a multi-line invoice.
- Token/card rows in `L:M` are separate one-token-per-row values, not embedded inside transaction rows.
- There is no explicit invoice number from POS. IPN is the strongest external transaction reference.
- The file name appears to contain branch/location text, for example `سنورس` or `ميت غمر`, but there is no branch ID column.
- There is no store column, cashier/user column, payment type column, POS item code column, unit column, VAT column, discount column, or explicit serial-to-transaction-row link.

## Existing POS Save Logic

Primary web save path:

- Controller: `F:\Source Code\DynamicErp\Areas\Pos\Controllers\PosTransactionController.cs`
- Save endpoint: `Save(PosSaveTransactionRequest request)`
- Repository: `F:\Source Code\DynamicErp\Areas\Pos\Data\PosSqlRepository.cs`
- SQL procedure: `dbo.usp_POS_SaveTransaction`
- SQL file: `F:\Source Code\DynamicErp\Areas\Pos\Sql\30_POS_SaveTransaction_UnicodeText.sql`

Key behavior:

- The controller requires a POS session context.
- It enforces save permissions.
- It applies branch/store/box/user defaults from the POS context unless the user can change defaults.
- It forces Kishny POS invoice `CustomerID = 2` while preserving KYC customer fields separately.
- It resolves default payment type from the logged-in POS user when needed.
- It calls `_repository.SaveTransaction(request)`.
- The repository builds table-valued parameters for items and sales payments.
- The repository calls `dbo.usp_POS_SaveTransaction`.
- The SQL procedure uses `DROP + CREATE`, SQL Server 2012-compatible syntax, `SET XACT_ABORT ON`, `BEGIN TRANSACTION`, `COMMIT`, and `ROLLBACK` in `CATCH`.
- The SQL procedure inserts `Transactions`, `Transaction_Details`, `TblSalesPayment`, `Notes`, `DOUBLE_ENTREY_VOUCHERS`, and issue voucher `Transaction_Type = 19` records where required.
- Existing issue voucher logic copies `Transaction_Details.ItemSerial` into the issue voucher detail rows.

## Legacy VB6 Logic Reviewed

Reviewed migration docs under:

- `F:\Source Code\SatriahMain\Cayshny\MigrationDocs`
- `F:\Source Code\SatriahMain\AI_Docs\Screens`

Relevant documented behavior:

- Main sales form: `FrmSaleBill6.frm`
- Sales transaction type: `21`
- Issue voucher transaction type: `19`
- Sales save creates `Transactions` and `Transaction_Details`.
- Stock deduction is handled by a generated issue voucher, not by the sales invoice alone.
- Accounting entries are written through `Notes` and `DOUBLE_ENTREY_VOUCHERS`.
- Card/Kishny card flow stores card serial/token in `Transactions.VisaNumber` and `Transaction_Details.ItemSerial`.
- Numbering uses `Voucher_coding` / sequence logic and branch/date/store/user context.

## Missing / Unclear Fields That Block Blind Import

| Field | Why required | Used by | Suggested strategy |
| --- | --- | --- | --- |
| Branch | Required for POS context, numbering, accounts, serial availability | Controller defaults, `usp_POS_SaveTransaction`, `usp_Voucher_coding_V2`, accounting branch accounts | Map from file name/sheet context to branch; reject if ambiguous |
| Store | Required for stock issue voucher and serial availability | `Transaction_Details.StoreID2`, issue voucher Type 19 | Default from import user POS context or mapping table; reject serial items if unavailable |
| User/Cashier | Required for permissions, numbering, accounting user, branch defaults | Controller context, SQL `@UserID`, notes/dev rows | Use logged-in import user or configured POS import user |
| Payment type | Required for POS save/accounting | Controller default payment logic, `TblSalesPayment`, cash/bank account mapping | Default from POS user payment setup or mapping screen; show in preflight |
| Item/service mapping | Required to build `PosTransactionItemDto` | `Transaction_Details`, accounting item/supplier/revenue accounts | Map service `كاش ان` to configured service item; reject unknown service types |
| Unit | Required in transaction detail rows | `Transaction_Details.UnitId`, quantity/unit conversion | Use item default unit from item lookup |
| VAT | Needed for `Vat`, `Vatyo`, accounting lines | SQL line totals/accounting | Calculate using existing commission/service logic if matching manual save; otherwise reject until mapped |
| Serial-token link | Required for exact serial-to-item traceability | `VisaNumber`, `Transaction_Details.ItemSerial`, issue voucher details | Current Excel has tokens in separate table; require deterministic pairing rule or manual mapping before commit |
| Invoice number | Duplicate prevention and traceability | `ManualNO`, audit/import tables | Use IPN as external reference; generate POS invoice number via normal save |
| Customer ID/KYC | POS uses fixed cash customer plus optional KYC data | Controller forces `CustomerID = 2`, KYC fields | Use customer name/phone from row; create/link cash customer only if current POS flow requires it |
| Discount | Affects totals/accounting | `discountvalue`, `TotalDiscountPerLine` | Default zero; show as default in preflight |
| Branch/store serial ownership | Required to prevent selling wrong branch/store serial | Existing reports/procedures around serials and issue vouchers | Add validation query using current `Transaction_Details` + `Transactions.TransactionTypes.StockEffect` model; do not invent a status column |

## Proposed Import Design

Step 1 - Preview / Validate:

- Upload Excel.
- Detect workbook type from table headers `B3:J226` and token table `L11:M226`.
- Parse one logical transaction per populated main row.
- Parse token rows separately.
- Apply branch/store/user/payment/item mappings.
- Validate IPN uniqueness in file and prior import audit table.
- Validate totals: `F + G = H` where all values exist.
- Validate service type is mapped.
- Validate missing service rows are rejected.
- Validate token count and pairing rule before allowing card-item commit.
- Show preflight checklist: fields found, defaults used, mappings applied, warnings, rejected rows, invoices ready.

Step 2 - Commit:

- Commit only valid invoices.
- Reuse normal `PosSaveTransactionRequest -> PosSqlRepository.SaveTransaction -> dbo.usp_POS_SaveTransaction`.
- Store import audit data after successful save in POS import tables under POS SQL only.
- Use one SQL transaction per invoice or a file-level mode; never leave half invoice / half journal / half serial state.

## Immediate Implementation Blockers

1. Serial-token pairing is not explicit in Excel. Tokens are one per row in `L:M`, while cash-in rows are in `B:J`; no row-level link exists.
2. Branch/store/user/payment are absent from the workbook and need mapping/defaults.
3. Service rows with missing `نوع الخدمة` exist in `ميت غمر` files and must be rejected unless a business rule explains them.
4. Item/service mapping is not in Excel. `كاش ان` must map to a configured POS service item.
5. `(1)` and `(2)` Mيت غمر files appear to duplicate the same source data and must be protected by source hash/IPN duplicate checks.

## Build Verification

- `dotnet build F:\Source Code\DynamicErp\MyERP.sln --no-restore` failed because the .NET SDK path does not include `Microsoft.WebApplication.targets`.
- Visual Studio MSBuild succeeded:
  - Command: `C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe F:\Source Code\DynamicErp\MyERP.sln /m /p:RestorePackages=false /verbosity:minimal`
  - Output: `MyERP -> F:\Source Code\DynamicErp\bin\MyERP.dll`

