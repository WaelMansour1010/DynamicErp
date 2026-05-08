# POS Excel Import Defaults + Service Mapping - 2026-05-07

## Files Changed

- `Areas/Pos/Controllers/ExcelImportController.cs`
- `Areas/Pos/Data/PosSqlRepository.cs`
- `Areas/Pos/Models/PosExcelImportModels.cs`
- `Areas/Pos/Models/PosSaveTransactionRequest.cs`
- `Areas/Pos/Services/PosExcelImportParser.cs`
- `Areas/Pos/Services/PosExcelImportPreflightService.cs`
- `Areas/Pos/Views/ExcelImport/Index.cshtml`
- `Areas/Pos/Views/ExcelImport/Preview.cshtml`
- `MyERP.csproj`

No SQL script was required for this phase. No POS SQL was added outside `Areas/Pos/Sql`.

## Schema Inspected

Connection used: `KishnyCashConnection`, database `Cash`.

Tables/columns confirmed:

- `TblBranchesData`: `branch_id`, `branch_Code`, `branch_name`, `branch_namee`
- `TblUsers`: `UserID`, `UserName`, `BranchId`, `StoreID`, `BoxID`, `Empid`, `isDeactivated`
- `TblItems`: `ItemID`, `ItemName`, `ItemNamee`, `ItemType`, `ChkLot`, `IsPriceIsPerview`, `HaveGuarantee`, `TrafficViolations`
- Existing payment defaults are still loaded by `PosSqlRepository.GetPosUserDefaults()` and `GetDefaultCashPaymentForUser()`.

Sample confirmed data:

- `EC010` maps by branch code to branch `11 - سوق السيارات المستعملة الجديد`.
- Arabic branch name token `عبور` maps to branch `75 - مجمع مرور العبور`.
- User `30 / EC010` has branch/store/box/employee defaults for branch 11.
- Service items inspected include item `2 شحن كيشنى`, item `10 خدمة كاش أوت`, item `19 كارت البنك الاهلي`, item `20 رسوم سداد مخالفات النيابة`.

## Branch Detection Rules

Implemented in `PosExcelImportPreflightService`:

1. Extract file base name without extension.
2. Normalize Arabic lightly: trim spaces, remove tatweel, normalize hamza/alef and yeh forms.
3. Try exact `branch_Code` match first, case-insensitive for English.
4. Try `branch_name` contains the file token.
5. Try significant filename tokens against Arabic/English branch names.
6. Try filename contains branch name.
7. If multiple candidates are found, all rows are rejected with a visible preflight error and candidates are listed.
8. If no candidate is found, all rows are rejected with a visible preflight error.

## Defaults Rules

After one deterministic branch is found:

- The import asks `PosSqlRepository.GetDefaultPosUserContextForBranch(branchId)` for an active POS user in that branch.
- The selected user must have `StoreID`, `BoxID`, and `Empid`.
- The resulting context reuses `GetPosUserDefaults()`, including the existing payment/default cash logic.
- Preflight shows user/cashier, employee/salesman, store, cash box, payment type, and bank if any.
- Missing required defaults reject all rows before any commit path can be enabled.

## Service Mapping Rules

Excel service text is mapped to the internal POS service type:

- `كاش ان` / `كاش إن` -> `cash-in`
- `كاش اوت` / `كاش أوت` -> `cash-out`
- `مخالفات` -> `violations`
- `كارت كيشني` / `كارت كيشنى` -> `card`

For each internal type, the preflight calls the existing screen behavior:

- `PosSqlRepository.GetDefaultServiceItem(internalServiceType, null, branchId)`

The preview displays:

- Excel service text
- internal service type/name
- default `Item_ID / ItemName`

Unknown service text or missing default item rejects the affected rows.

## Validation / Preflight Rules

Preflight now blocks save readiness for:

- missing or ambiguous branch
- missing default POS user
- missing employee/salesman
- missing store
- missing cash box
- missing payment type
- unknown service text
- service type without default item
- existing parser validations: missing date, invalid amount/fees/total, duplicate IPN, duplicate token, total mismatch

The UI no longer asks production users for manual `BranchId`, `StoreId`, `PaymentTypeId`, or cash-in item id. Those values are detected or rejected.

## Test Cases

Build:

- Visual Studio MSBuild succeeded:
  `MSBuild.exe F:\Source Code\DynamicErp\MyERP.sln /m /p:RestorePackages=false /verbosity:minimal`

Database/schema checks:

- Confirmed `EC010` branch code exists.
- Confirmed Arabic `عبور` branch lookup returns one candidate.
- Confirmed active users with store/box/employee defaults exist.
- Confirmed service item source columns and representative items exist.

Manual preflight cases to run in UI:

- `F:\Source Code\DynamicErp\Excel\EC010.xlsx` should detect branch by `branch_Code`.
- `F:\Source Code\DynamicErp\Excel\العبور (2).xlsx` should detect branch by Arabic branch name token.
- `كاش ان`, `كاش اوت`, `مخالفات`, and `كارت كيشني/كيشنى` should resolve to default POS items or reject clearly if the database lacks a default.
- Unknown service text must reject the row.
- Ambiguous or missing branch must reject all rows.

## Risks / Notes

- This phase is still preview/preflight only. It does not create invoices.
- Token matching remains the existing visible sequential preview. The critical token-to-row problem is still intentionally surfaced before commit rather than hidden.
- If business wants a different default import cashier per branch, add a configuration table later; do not hardcode users.
- No changes were made to legacy `AllScripts.sql`.

## Addendum - IPN and KYC Rules

Date: 2026-05-07

Files changed for this addendum:

- `Areas/Pos/Controllers/PosTransactionController.cs`
- `Areas/Pos/Data/PosSqlRepository.cs`
- `Areas/Pos/Scripts/pos-transaction.js`
- `Areas/Pos/Services/PosExcelImportParser.cs`
- `Areas/Pos/Services/PosExcelImportPreflightService.cs`
- `Areas/Pos/Models/PosExcelImportModels.cs`
- `Areas/Pos/Views/ExcelImport/Preview.cshtml`
- `Areas/Pos/Sql/30_POS_SaveTransaction_UnicodeText.sql`

Implemented business rule:

- The screen IPN field, saved as `Transactions.ManualNO`, is important only for `cash-in` and Kishny card invoices.
- Duplicate `ManualNO` is allowed for `cash-out` and traffic violations.
- Duplicate `ManualNO` is blocked for `cash-in` and Kishny card invoices in the POS invoice screen, Excel preflight, repository validation, and the POS save stored procedure.
- Missing `ManualNO` is required only for `cash-in` and Kishny card invoices.

Excel validation changes:

- Parser no longer rejects duplicate IPN globally.
- Preflight rejects duplicate/missing IPN only when the resolved service type is `cash-in` or `card`.
- Duplicate IPN in `cash-out` and `violations` rows is allowed.
- Matched token is now retained on each parsed row for visible traceability.
- Sequential token matching now treats both `cash-in` and Kishny card rows as token-eligible rows.

KYC/token handling:

- Card rows require a deterministic matched token before save readiness.
- If the matched token is not found in KYC, preflight marks the row with `RequiresKycCreation` and warns that the customer/token must be created before invoice save.
- No fallback token guessing is used. A card row without a matched token is rejected in preflight.

SQL notes:

- The only SQL script changed is under the approved POS SQL folder:
  `Areas/Pos/Sql/30_POS_SaveTransaction_UnicodeText.sql`
- The script remains SQL Server 2012 compatible and keeps the DROP + CREATE stored procedure pattern.
- No POS SQL was added to legacy `AllScripts.sql`.

## Addendum - Admin Invoice Delete and Excel Source Flag

Date: 2026-05-07

Implemented:

- The POS sales screen now receives `CanAdminDeleteInvoice` from the POS context.
- The delete controls are rendered/enabled only for admin/full-access users.
- Deleting a single sales invoice requires re-entering the current admin POS password.
- Deleting an invoice runs inside a SQL transaction and removes the POS sale effects through the same cleanup path used by Excel rollback:
  - `Transactions` sale header
  - `Transaction_Details`
  - `TblSalesPayment`
  - linked issue voucher transaction/details when referenced by the sale
  - `Notes`
  - `DOUBLE_ENTREY_VOUCHERS`
- Deletion is blocked if the invoice has `Transactions.NoteIDClose > 0`.
- Imported Excel invoices are flagged in invoice lists by joining to `POS_ImportBatchRow` rows with `Status = N'Imported'`.
- The sales index and today's invoices list can filter to Excel-imported invoices only.
- Admin users can delete Excel-imported invoices for a selected date period, optionally scoped by branch for full-access users.
- Deleted Excel import rows are marked as `Status = N'Deleted'` and their `TransactionId` is cleared so they no longer appear as active Excel invoices.

SQL:

- No new SQL script was required for this change.
- No SQL was added to legacy `AllScripts.sql`.

## Addendum - Excel Import Execution Permission

Date: 2026-05-07

Implemented:

- Added POS temporary permission key `CanImportExcel` with title `استيراد العمليات من Excel`.
- Full-access/admin users receive the permission automatically.
- The POS dashboard Excel import menu item is shown only for admin/full-access users or users granted `CanImportExcel`.
- The Excel import screen blocks unauthorized users at the controller level.
- The commit/start import action requires the current authorized POS user's password before the async import job starts.
- Excel import rollback also requires the same permission and password because it deletes invoices and their accounting/stock effects.

SQL:

- No new SQL script was required; the existing `POS_UserPermissions` table stores the new permission key.
- No SQL was added to legacy `AllScripts.sql`.

## Addendum - Recharge Services and Violation Defaults

Date: 2026-05-07

Implemented rules:

- Excel service names starting with recharge service semantics are classified as `cash-in`.
- The following Arabic services are explicitly treated as cash-in:
  - `شحن ح بنكي`
  - `شحن محفظة`
  - `شحن بطاقة اخرى`
  - `شحن كيشنى`
- Cash-in recharge rows are not matched to token rows. Tokens are reserved for deterministic Kishny card rows only.
- A row is token-eligible only when the service text is Kishny card, for example `كارت كيشني`, and not a recharge service.
- For traffic violations, missing price values use the existing default of `50` EGP in preview validation so the row is not rejected just because the sheet omitted the price.
- Historical saved violations invoices were inspected. They use `TrafficViolations = 1`, `ItemIDService = 20`, detail item `20 - رسوم سداد مخالفات النيابة`, item/detail price `50`, `NetValue = 50`, and `PayedValue = 50`; `ViolationsValue` can hold the actual fine amount but is advisory for this import flow.
- The Excel preview no longer applies the `amount + fee = gross` equation to violations. Violations are treated as a fixed 50 EGP service-fee invoice.
- The default violations service item is normalized to price/show price/total price `50` even if the source item record is missing or has a different sales price.

Important boundary:

- `شحن كيشنى` is cash-in, not card.
- `كارت كيشني` is the card flow and can require KYC/token handling.

## Addendum - Commit Phase

Date: 2026-05-07

Implemented:

- The Excel preview is stored in the POS session after successful preflight.
- Commit now imports ready/warning rows by building `PosSaveTransactionRequest` and calling the existing `PosSqlRepository.SaveTransaction` -> `dbo.usp_POS_SaveTransaction` flow.
- No parallel accounting engine was added.
- Commit is blocked if the preview still has rejected rows, unmatched tokens, or missing detected POS defaults.
- Each imported row is audited in `POS_ImportBatch` / `POS_ImportBatchRow`.
- Duplicate source rows are blocked using source file hash + sheet + row once they were imported successfully.
- Runtime failure stops the file commit after recording the failed row. Already saved invoices remain traceable through the batch audit.

SQL:

- Added POS-only script:
  `Areas/Pos/Sql/50_POS_ExcelImportCommitAudit.sql`
- No SQL was added to legacy `AllScripts.sql`.

## Addendum - Partial Commit and Marked Workbook

Date: 2026-05-07

Implemented:

- Excel commit no longer requires the user to remove rejected rows before importing.
- Valid/import-ready rows are saved through the existing POS save flow.
- Rejected rows, rows without deterministic card token mapping, unknown services, and incomplete rows are skipped and left unposted.
- A previously imported source row is skipped instead of failing the whole import.
- The skipped/imported/failed row statuses are shown in the commit result.
- The uploaded workbook is saved under `App_Data/PosExcelImports` for the current preview session.
- After commit, the system creates a marked `.xlsx` copy with these columns:
  - `POS Import Status`
  - `POS Invoice No`
  - `POS Import Message`
  - `POS Marked At`
- Re-uploading a marked workbook skips rows marked as already imported.
- The commit screen now starts an asynchronous import job and polls server-side progress, showing processed/imported/skipped/failed counters and a progress bar while invoices are being saved.
- A rollback button is available after import completion. It deletes invoices created by the current Excel import batch only, including invoice details, payments, invoice journal notes, double-entry lines, and linked issue voucher transaction when present.
- Rollback is blocked per invoice if it is already linked to daily closing through `Transactions.NoteIDClose`.
- After rollback, the system creates a workbook copy with the POS import marker cells cleared for rolled-back rows so the sheet no longer carries the imported marker.
- Cash-in recharge services now resolve the POS item by matching the Excel service text against `TblItems` first, before falling back to the generic cash-in default. This prevents recharge rows such as bank/wallet/Kishny recharge from silently using a wrong cash-out-like item.
- Cash-in commit now explicitly forces `IsRecharg = 1`, `IsCashOut = 0`, `IsWallet = 0`, `IsPOS = 0`, and `TrafficViolations = 0` before calling the existing POS save flow.
- Commit now normalizes the service flags for every imported row from the preflight `InternalServiceType`: cash-in, cash-out, card, and violations are mutually exclusive before calling `SaveTransaction`.
- After `SaveTransaction` returns, the importer rereads the saved `Transactions` row using the same service classification used by invoice/list screens and also verifies `ItemIDService`. If the saved type or saved service item differs from Excel preflight, the just-created invoice and its POS effects are immediately removed and the row is recorded as failed. This prevents cash-in/cash-out flipped invoices or wrong service items from remaining in the system.

Safety:

- Only rows saved successfully are audited as `Imported` in `POS_ImportBatchRow`.
- Rejected/skipped rows are not converted into invoices and do not create accounting entries.
- No POS SQL was added to legacy `AllScripts.sql`.
