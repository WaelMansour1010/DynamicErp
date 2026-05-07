# MainErp Implementation Log

## 2026-05-06

Created phase 1 MainErp foundation:

- New MVC area under `Areas\MainErp`.
- Area routes for `/MainErp`, `/MainErp/LC`, and `/MainErp/ProjectExtracts`.
- Authorized skeleton controllers and view models.
- Isolated MainErp layout, CSS, landing page, LC page, and Project Extracts page.
- Placeholder SQL scripts under `Areas\MainErp\Sql`.
- Migration documentation under `AI_Docs\MainErp`.

Kishny/POS isolation:

- No files under `Areas\Pos` were modified.
- No POS SQL scripts were modified.
- No Kishny legacy files under `SatriahMain\Cayshny` were modified.
- No `AllScripts.sql` changes were made.

Next step:

- Review LC and Project Extracts analysis with live schema confirmation before implementing business logic.

## 2026-05-06 Phase 3 Core Accounting Foundation

Created MainErp-local infrastructure only under `Areas\MainErp`:

- explicit DB connection factory and unit-of-work transaction wrapper;
- manual ID generation abstraction for known legacy manual-id targets;
- account code preview service;
- note/voucher numbering preview service;
- voucher models and posting engine with preview validation;
- posting preview demo route `/MainErp/Accounting/PreviewTest`;
- read-only LC list/details;
- read-only Project Extract list/details;
- lightweight audit logger hook.

Architecture decisions:

- ADO.NET was used instead of EF for transaction and SQL Server 2012 compatibility.
- Real manual ID allocation requires an active transaction and application lock.
- Voucher posting validates balance, debit/credit semantics, account presence, and zero-value rejection before writing.
- Read-only repositories catch missing legacy schema and show warnings rather than crashing against the current `MyErp` web database.

Unresolved risks and TODOs:

- Full VB6 `new_id`, `Notes_coding`, `Voucher_coding`, `sanad_numbering`, and `get_opening_balance_voucher_id` behavior still require final mapping.
- Account creation remains disabled; only dry-run child code preview exists.
- No final LC or Project Extract save/post workflow is implemented.
- External VB6 writers during migration can still affect manual ID allocation unless deployment locking is coordinated.

Safety:

- No MainErp code was placed under `Areas\Pos`.
- No POS SQL was modified.
- `AllScripts.sql` was not modified.

## 2026-05-06 Kishny Reuse Review And Safe First Import

Reviewed POS/Kishny web modules as reference only:

- purchases;
- stock transfers;
- journal entries;
- accounting reports;
- sales reports;
- dashboard and financial intelligence pages.

Created reuse planning documents:

- `20_KishnyReusableModulesInventory.md`
- `21_DashboardReusePlan.md`
- `22_ReportReusePlan.md`

Implemented MainErp-owned first wave:

- dashboard shell at `/MainErp/Dashboard`;
- accounting reports shell at `/MainErp/AccountingReports`;
- sales reports shell at `/MainErp/SalesReports`;
- purchases and stock-transfer reserved routes with no save/import behavior;
- journal-entry read-only search at `/MainErp/JournalEntries`.

Architecture decisions:

- No POS controller, repository, view, JavaScript, SQL script, or permission name was copied directly into MainErp.
- Journal entries use a new MainErp repository against `DOUBLE_ENTREY_VOUCHERS`, `Notes`, and `ACCOUNTS`.
- Purchase and stock-transfer write/import behavior remains excluded because the current POS implementation is coupled to POS session, branch defaults, serial imports, and POS permissions.
- Accounting and sales reports are shells until stored procedures are reviewed against `AllScripts.sql` and live schema.

Safety:

- No `Areas\Pos` files were modified for this import.
- No POS SQL was modified.
- No Kishny/Cayshny files were modified.
- `AllScripts.sql` was not modified.

## 2026-05-06 MainErp Connection Boundary

Formalized MainErp database separation:

- `MainErp_ConnectionString` is the preferred MainErp connection.
- `MyERP_ConnectionString` remains the original large web connection.
- POS/Kishny connection strings remain unchanged.
- `MainErpDbConnectionFactory` now logs a warning if `MainErp_ConnectionString` is missing and falls back to `MyERP_ConnectionString`.

Created:

- `25_ConnectionAndRunModes.md`

Validation:

- MainErp repositories and accounting services use `MainErpDbConnectionFactory` / `IMainErpDbConnectionFactory`.
- `Areas\Pos` has no references to `MainErp_ConnectionString`.
- `AllScripts.sql` was not modified.
- MainErp pages do not reference card, token, KYC, commission, cashier closing, or POS session restore behavior.

## 2026-05-06 Working Read-Only Reports And Eng Verification

Implemented the first working read-only report wave:

- `/MainErp/AccountingReports/JournalEntries`
- `/MainErp/AccountingReports/AccountMovement`
- `/MainErp/SalesReports/SalesSummary`

Created:

- `Areas\MainErp\Repositories\Reports\AccountingReportRepository.cs`
- `Areas\MainErp\Repositories\Reports\SalesReportRepository.cs`
- `Areas\MainErp\ViewModels\Reports\ReportRowViewModels.cs`
- report views under `Areas\MainErp\Views\AccountingReports` and `Areas\MainErp\Views\SalesReports`
- `23_WorkingReadOnlyReports.md`
- `24_Eng_TestResults_LC_ProjectExtracts.md`

Architecture decisions:

- Reports use MainErp-owned repositories and parameterized read-only SQL.
- No stored procedures were created or changed.
- MainErp now checks `MainErp_ConnectionString` first and falls back to `MyERP_ConnectionString`; this allows local testing against the representative `Eng` database without changing POS.
- Journal-entry direction was corrected to `0 = debit` and `1 = credit` after checking `Eng`.

Eng verification:

- LC list/details tested with sample `TblLCID = 197`.
- Project Extract list/details tested with sample `id = 3449`.
- Journal entries, account movement, and sales summary report SQL smoke-tested against `Eng`.

Remaining risks:

- Account movement opening balance is not calculated yet.
- Sales summary uses conservative `Transaction_Type IN (22, 29)` until final sales type mapping is approved.
- Browser route checks were unauthenticated and returned expected login redirects; authenticated UI testing is still needed.

Safety:

- No `Areas\Pos` files were modified for these reports.
- No POS SQL was modified.
- No Kishny/Cayshny files were modified.
- `AllScripts.sql` was not modified.

## 2026-05-07 Course Correction - Real VB6 Screen Migration

The migration direction was corrected:

- VB6 active forms are now the primary source of truth.
- Kishny/POS web work remains a reusable secondary reference only.
- Generic shells and theoretical architecture must not drive LC or Project Extract behavior.
- MainErp must feel like the original VB6 ERP modernized for web.

Created real screen mapping documents:

- `26_RealScreenMapping_LC.md`
- `27_RealScreenMapping_ProjectExtracts.md`

Real migrated behavior currently present:

- LC list reads real `TblLC` data.
- LC details reads real `TblLC` header/account values.
- Project Extract list reads real `project_billl` data.
- Project Extract details reads real `project_billl` header/totals/account values.
- MainErp uses a standalone layout and no longer visually inherits the old web layout.

Corrections made in MainErp UI:

- LC list/details labels were changed from generic English placeholders toward Arabic VB6 terminology.
- LC details now exposes the real `FrmLC.frm` tab structure:
  - `البيانات الاساسية`
  - `مصاريف الفتح`
  - `الفواتير المالية`
  - `revised bond amount`
  - `قروض الاعتمادات`
  - `Refinance`
  - `acceptance advice`
- LC details shows the real workflow buttons as disabled read-only controls: new/edit/save/delete/search/create voucher/print voucher.
- Project Extract details now exposes the real `projectsbill.frm` sections:
  - بيانات المستخلص
  - الإجماليات والضريبة
  - الحسابات
  - بنود المشروع `Fg_Journal`
  - الدفعات المقدمة `VSFlexGrid4`
  - ملاحظات
- Project Extract details shows the real workflow buttons as disabled read-only controls: new/edit/save/delete/search/send to approval/print.

Placeholders still present and explicitly marked:

- LC grids are not loaded yet: `GrdMargin`, `GrdBondHistory`, `GrdMargin2`, `GrdMargin3`, `GrdMargin4`.
- Project Extract lines are not loaded yet from `project_bill_details`.
- Project advance/prepaid allocations are not loaded yet from `TblPayPrePayed` / `TblProjePayPrePayed`.
- Approval grid/status is not loaded yet from `ApprovalData`.
- No save/edit/delete/posting behavior is enabled.
- No account creation is enabled.
- No `Notes`, `Notes1`, `DOUBLE_ENTREY_VOUCHERS`, or `DOUBLE_ENTREY_VOUCHERS1` rows are created.

Kishny/POS reuse status:

- POS modules were previously analyzed but not imported deeply enough.
- Next reuse work must import/adapt actual working MainErp-safe modules, starting with read-only/reporting behavior that is generic and removing POS session/permission/branch forcing.
- POS-specific cards, KYC, commissions, cashier closing, POS receipt, POS session restore, and POS health widgets remain excluded.

Immediate next implementation targets:

1. LC: load the real read-only LC grids from `TBLLCHistory`, `TBLLCMargin`, `TBLLCMargin2`, and `tblLCOpenB`.
2. Project Extracts: load `project_bill_details` with `Fg_Journal` column names and previous/current/cumulative quantities.
3. Project Extracts: load advance-payment allocations from `TblPayPrePayed` and `TblProjePayPrePayed`.
4. Kishny reuse: select one safe working module, preferably accounting report/journal search behavior, and adapt the actual UI/query behavior into MainErp without POS dependencies.

## 2026-05-07 Critical Fix - Replace Generic Placeholder Pages

Created:

- `26_CourseCorrection_RealMigration.md`

Implemented immediate screen corrections:

- LC `/MainErp/LC` is no longer a primitive list-only page.
  - It is now labeled `Real migrated from VB6`.
  - It exposes the real `FrmLC.frm` workflow shell: basic LC data, accounts, dates/opening balance, related grids, original toolbar actions.
  - The old TblLC list is retained only as `Temporary technical test page` inside the screen.
- Project Extracts `/MainErp/ProjectExtracts` is no longer a primitive list-only page.
  - It is now labeled `Real migrated from VB6`.
  - It exposes the real `projectsbill.frm` workflow shell: header, `Fg_Journal`, deductions/VAT, advance-payment grid, approval/print toolbar actions.
  - The old `project_billl` list is retained only as `Temporary technical test page` inside the screen.
- Purchases `/MainErp/Purchases` is now labeled `Reused from Kishny generic module`.
  - It adapts the real Kishny/POS purchase invoice UI structure: filters, header fields, item grid, totals, toolbar.
  - Save/import remain disabled until `PosUserContext` and POS repository dependencies are replaced by MainErp services.
- Stock Transfers `/MainErp/StockTransfers` is now labeled `Reused from Kishny generic module`.
  - It adapts the real Kishny/POS stock transfer UI structure: search filters, transfer header, item/serial grid, toolbar.
  - Save/import remain disabled until MainErp repositories are implemented.
- Dashboard `/MainErp/Dashboard` is now labeled `Reused from Kishny generic module`.
  - It shows generic ERP KPI/report navigation and placeholders for generic branch/accounting/sales charts.
  - POS-only widgets are excluded.
- Accounting Reports `/MainErp/AccountingReports` is now labeled `Reused from Kishny generic module`.
  - It uses the Kishny report-tile/filter concept while linking to MainErp read-only reports.
- Sales Reports `/MainErp/SalesReports` is now labeled `Reused from Kishny generic module`.
  - It exposes generic sales reports and excludes POS/Kishny-specific reporting.

Still intentionally disabled:

- LC save/post/delete.
- Project Extract save/post/delete/approval.
- Purchase save/import.
- Stock transfer save/import.
- POS/Kishny-specific dashboards and reports.

Safety:

- No `Areas\Pos` file was edited for this correction.
- No POS SQL was edited.
- No `AllScripts.sql` change was made.

Safety:

- No `Areas\Pos` files were modified by this correction.
- No POS SQL was modified.
- No Kishny/Cayshny legacy files were modified.
- `AllScripts.sql` was not modified.

## 2026-05-07 LC Safe Shell Correction

Created:

- `29_LC_SafeShell_Mapping.md`

Modified:

- `Areas\MainErp\Controllers\LCController.cs`
- `Areas\MainErp\Repositories\LC\LcReadRepository.cs`
- `Areas\MainErp\ViewModels\LC\LCIndexViewModel.cs`
- `Areas\MainErp\Views\LC\Index.cshtml`
- `Areas\MainErp\Views\LC\Details.cshtml`
- `Areas\MainErp\Content\mainerp\mainerp.css`

Implemented:

- Replaced the primitive LC list page with a real `FrmLC.frm`-style safe shell.
- `/MainErp/LC` now shows:
  - original-style toolbar,
  - LC search panel,
  - selected LC read-only workbench,
  - basic LC data,
  - bank/currency/payment data,
  - linked account fields,
  - dates/opening balance/note serial fields,
  - placeholder shells for the real VB6 grids.
- `/MainErp/LC/Details/{id}` now uses the same Arabic-first LC terminology and safe read-only field grouping.
- Data reads remain limited to `TblLC` and safe lookup joins.

Disabled / not migrated yet:

- New/edit/save/delete.
- LC posting.
- Close voucher.
- Voucher creation/deletion/printing.
- Notes/Notes1 creation.
- `DOUBLE_ENTREY_VOUCHERS` or `DOUBLE_ENTREY_VOUCHERS1` writes.
- Account auto-creation.
- LC grid detail persistence or edit events.

Safety:

- No database schema changes.
- No `AllScripts.sql` changes.

## 2026-05-07 MainErp Kishny UI Shell Reuse Correction

Created:

- `Areas\MainErp\Views\Shared\_MainErpSidebar.cshtml`
- `AI_Docs\MainErp\30_MainErp_KishnyUILayoutReuse.md`

Modified:

- `Areas\MainErp\Views\Shared\_MainErpLayout.cshtml`
- `Areas\MainErp\Views\Dashboard\Index.cshtml`
- `Areas\MainErp\Controllers\HomeController.cs`
- `Areas\MainErp\Views\Home\Index.cshtml`
- `Areas\MainErp\Content\mainerp\mainerp.css`
- `Areas\MainErp\Controllers\AccountingController.cs`
- `Areas\MainErp\Controllers\AccountingReportsController.cs`
- `Areas\MainErp\Controllers\DashboardController.cs`
- `Areas\MainErp\Controllers\JournalEntriesController.cs`
- `Areas\MainErp\Controllers\LCController.cs`
- `Areas\MainErp\Controllers\ProjectExtractsController.cs`
- `Areas\MainErp\Controllers\PurchasesController.cs`
- `Areas\MainErp\Controllers\SalesReportsController.cs`
- `Areas\MainErp\Controllers\StockTransfersController.cs`
- `MyERP.csproj`

Implemented:

- Replaced the simplified MainErp header/link landing experience with a real shell adapted from the Kishny dashboard layout.
- Added MainErp-specific sidebar with Kishny-style accordion navigation and MainErp-only routes.
- Replaced the old temporary dashboard cards with a Kishny-style executive dashboard structure: period filters, branch/scope filters, KPI cards, insight panel, and chart containers.
- `/MainErp` now redirects to `/MainErp/Dashboard`.
- Controllers set `ViewBag.ActiveScreen` so the MainErp sidebar can highlight the active module.

Excluded from MainErp:

- POS sales invoice flow,
- card/token logic,
- KYC,
- commissions,
- cashier close,
- POS session restore,
- Kishny branding,
- POS dashboard data endpoints.

Safety:

- No `Areas\Pos` files modified.
- No `AllScripts.sql` changes.
- No database changes.
- MainErp still uses MainErp login/session/connection boundaries.

## 2026-05-07 LC Real Accounting Trace Screen

Created:

- `Areas\MainErp\ViewModels\JournalEntries\JournalEntryDetailsViewModel.cs`
- `Areas\MainErp\Views\JournalEntries\Details.cshtml`
- `AI_Docs\MainErp\31_LC_GridVoucherMapping.md`
- `AI_Docs\MainErp\32_LC_RealScreenImplementation.md`

Modified:

- `Areas\MainErp\Repositories\LC\LcReadRepository.cs`
- `Areas\MainErp\ViewModels\LC\LCIndexViewModel.cs`
- `Areas\MainErp\Views\LC\Index.cshtml`
- `Areas\MainErp\Repositories\JournalEntries\JournalEntryReadRepository.cs`
- `Areas\MainErp\Controllers\LCController.cs`
- `Areas\MainErp\Controllers\JournalEntriesController.cs`
- `Areas\MainErp\MainErpAreaRegistration.cs`
- `Areas\MainErp\Content\mainerp\mainerp.css`
- `MyERP.csproj`

Implemented:

- Real LC workspace replacing the primitive list/details behavior.
- LC routes:
  - `/MainErp/LC/Open/{id}`,
  - `/MainErp/LC/New`,
  - `/MainErp/LC/Edit/{id}`.
- Safe read-only sections matching the VB6 workflow:
  - LC header,
  - bank/currency/value,
  - accounts,
  - dates/opening balance,
  - main note ids/serials,
  - related grids,
  - Notes/Notes1,
  - voucher trace.
- Account link preview from `ACCOUNTS`, including missing account and parent-last-account warnings.
- Dynamic read-only grid loading for:
  - `TBLLCHistory`,
  - `TBLLCMargin`,
  - `TBLLCMargin2`,
  - `tblLCOpenB`.
- Voucher trace from:
  - `Notes`,
  - `Notes1`,
  - `DOUBLE_ENTREY_VOUCHERS`,
  - `DOUBLE_ENTREY_VOUCHERS1`.
- Read-only journal detail routes:
  - `/MainErp/JournalEntries/DetailsByNote/{noteId}`,
  - `/MainErp/JournalEntries/DetailsByVoucher/{voucherId}`.

Validation:

- Build succeeded.
- Read-only SQL validation against `Wael\Sql2019 / Eng` for `TblLCID = 195` confirmed LC header, notes, normal voucher rows, `TBLLCMargin`, and `TBLLCMargin2`.

Safety:

- No save/post/delete/account creation implemented.
- No `AllScripts.sql` change.
- No database schema change.
- No `Areas\Pos` files modified.
- No `Areas\Pos` files modified.
- No Cayshny legacy files modified.

## 2026-05-07 LC Workbench Phase 2 - Read-only Enhancement

Scope:

- Improve LC Workbench only.
- Keep the screen read-only.
- Add missing VB6 controls as read-only fields or clearly marked placeholders.
- Add read-only previews where safe.

Modified:

- `Areas\MainErp\Repositories\LC\LcReadRepository.cs`
- `Areas\MainErp\ViewModels\LC\LCIndexViewModel.cs`
- `Areas\MainErp\Views\LC\Index.cshtml`
- `Areas\MainErp\Views\LC\Details.cshtml`
- `Areas\MainErp\Content\mainerp\mainerp.css`
- `AI_Docs\MainErp\29_LC_SafeShell_Mapping.md`

Added:

- Full `FrmLC.frm` control inventory in `29_LC_SafeShell_Mapping.md`.
- Status panel at the top of the LC workbench:
  - LC number,
  - bank,
  - currency,
  - status,
  - value/open value,
  - opening voucher indicator,
  - closed indicator.
- More read-only fields from `TblLC`:
  - `opening_balance_voucher_id`,
  - `AccountExpProject`.
- More VB6 fields as explicit `Not mapped yet` placeholders:
  - `txtNameE`,
  - `DCPreFix`,
  - `dcopr`,
  - `Dcterm`,
  - `dcitems`,
  - `TxtNoOfParcil`,
  - `txtGuaranteeNo`,
  - `TXtPrimaryInvoiceNo`,
  - `txtOPenValue2`,
  - `txtBondAmt`,
  - `txtPercentV`,
  - `txtAcceptianPeriod`,
  - `TxtItemQty`,
  - `TxtItemPrice`,
  - LG period/cost fields.
- Preview panels for:
  - LC expenses,
  - documents/shipments/messages placeholders,
  - linked `Notes` rows by `TblLCID` when the schema supports it.
- Dangerous action buttons now show:
  - `هذه الوظيفة لم يتم ترحيلها بعد - Read Only Mode`.

Still read-only:

- No `SaveData`.
- No delete.
- No post.
- No `Notes` insert/update/delete.
- No `DOUBLE_ENTREY_VOUCHERS` or `DOUBLE_ENTREY_VOUCHERS1` insert/update/delete.
- No account creation.
- No grid edit/save behavior.

Fields still without confirmed source:

- `txtNameE`
- `TXTBank2`
- `txtOPenValue2`
- `txtBondAmt`
- `txtPercentV`
- `txtAcceptianPeriod`
- `TxtNoOfParcil`
- `txtGuaranteeNo`
- `TXtPrimaryInvoiceNo`
- `txtGuaranteeDate`
- `txtLGExpiryDate`
- LG cost/period helper fields
- `DCPreFix`, `dcopr`, `Dcterm`, `dcitems`, `DataCombo1`, `DataCombo2`

Safety:

- No database changes.
- No `AllScripts.sql` changes.
- No `Areas\Pos` files modified.
- No POS/Kishny logic introduced.

## 2026-05-07 MainErp Login And Debug Run Mode

Created:

- `Areas\MainErp\Controllers\LoginController.cs`
- `Areas\MainErp\Models\Security\MainErpUserContext.cs`
- `Areas\MainErp\Security\MainErpSessionKeys.cs`
- `Areas\MainErp\Services\Security\MainErpLoginService.cs`
- `Areas\MainErp\ViewModels\Security\MainErpLoginViewModel.cs`
- `Areas\MainErp\Views\Login\Index.cshtml`
- `Areas\MainErp\Infrastructure\MainErpDebugDatabaseOverride.cs`
- `Controllers\DevStartController.cs`
- `Views\DevStart\Index.cshtml`
- `AI_Docs\MainErp\27_MainErp_LoginAndRunMode.md`

Modified:

- `Areas\MainErp\Controllers\MainErpControllerBase.cs`
- `Areas\MainErp\Infrastructure\MainErpDbConnectionFactory.cs`
- `Areas\MainErp\Views\Shared\_MainErpLayout.cshtml`
- `Areas\MainErp\Content\mainerp\mainerp.css`
- `App_Start\RouteConfig.cs`
- `MyERP.csproj`
- `AI_Docs\MainErp\25_ConnectionAndRunModes.md`

Implemented:

- MainErp-specific login route: `/MainErp/Login`.
- MainErp-specific session context instead of POS context.
- MainErp base controller now requires `MainErp.UserContext` and redirects missing sessions to `/MainErp/Login`.
- MainErp login reads active users from `TblUsers` using `MainErp_ConnectionString`.
- Loaded defaults:
  - user id,
  - username,
  - employee id/name,
  - branch id/name,
  - store id/name,
  - box id/name,
  - optional `PaymentNetid`/`PaymentNetID`,
  - user type/admin flag.
- Development master password support through:
  - `EnableDevMasterPassword`,
  - `DevMasterPassword`.
- DEBUG/local startup selector:
  - `/`,
  - `/DevStart`,
  - `/RunMode`.
- Outside DEBUG/local, `/` redirects to the previous POS root behavior.
- DEBUG/local MainErp database override stored only in Session and applied only to `MainErp_ConnectionString`.

Safety:

- POS login was not modified.
- POS context and `POSCTX` are not used by MainErp.
- MainErp does not use `KishnyCashConnection`.
- Original web still uses `MyERP_ConnectionString`.
- No database schema changes.
- No `AllScripts.sql` changes.
