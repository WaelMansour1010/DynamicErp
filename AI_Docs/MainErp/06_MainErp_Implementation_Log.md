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

## 2026-05-07 MainErp Bilingual Architecture

Created:

- `Areas\MainErp\Resources\MainErp.resx`
- `Areas\MainErp\Resources\MainErp.en.resx`
- `Areas\MainErp\Resources\MainErp.ar.resx`
- `Areas\MainErp\Infrastructure\Localization\MainErpCultureManager.cs`
- `Areas\MainErp\Infrastructure\Localization\MainErpLocalizationService.cs`
- `Areas\MainErp\Infrastructure\Localization\MainErpEntityLocalization.cs`
- `Areas\MainErp\Controllers\LocalizationController.cs`
- `AI_Docs\MainErp\33_BilingualArchitecture.md`

Modified:

- `Areas\MainErp\Controllers\MainErpControllerBase.cs`
- `Areas\MainErp\Views\Shared\_MainErpLayout.cshtml`
- `Areas\MainErp\Views\Shared\_MainErpSidebar.cshtml`
- `Areas\MainErp\Views\Dashboard\Index.cshtml`
- `Areas\MainErp\Views\LC\Index.cshtml`
- `Areas\MainErp\Views\AccountingReports\Index.cshtml`
- `Areas\MainErp\Views\SalesReports\Index.cshtml`
- `Areas\MainErp\Repositories\LC\LcReadRepository.cs`
- `Areas\MainErp\Content\mainerp\mainerp.css`
- `MyERP.csproj`

Implemented:

- Resource-based UI localization for MainErp foundation.
- Central culture manager with Session/Cookie language preference.
- Topbar language switch without changing routes.
- Dynamic `dir=rtl/ltr` and CSS direction class.
- Entity localization helper with Arabic/English fallback.
- Account display standard using:
  - `Account_Serial + localized account name`.
- LC linked accounts now use `Account_NameEng` in English mode and `Account_Name` in Arabic mode.

Safety:

- No duplicated Arabic/English views or controllers.
- No database changes.
- No `AllScripts.sql` changes.
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

## 2026-05-07 MainErp Bilingual Architecture

Created:

- `Areas\MainErp\Resources\MainErp.resx`
- `Areas\MainErp\Resources\MainErp.en.resx`
- `Areas\MainErp\Resources\MainErp.ar.resx`
- `Areas\MainErp\Infrastructure\Localization\MainErpCultureManager.cs`
- `Areas\MainErp\Infrastructure\Localization\MainErpLocalizationService.cs`
- `Areas\MainErp\Infrastructure\Localization\MainErpEntityLocalization.cs`
- `Areas\MainErp\Controllers\LocalizationController.cs`
- `AI_Docs\MainErp\33_BilingualArchitecture.md`

Modified:

- `Areas\MainErp\Controllers\MainErpControllerBase.cs`
- `Areas\MainErp\Views\Shared\_MainErpLayout.cshtml`
- `Areas\MainErp\Views\Shared\_MainErpSidebar.cshtml`
- `Areas\MainErp\Views\Dashboard\Index.cshtml`
- `Areas\MainErp\Views\LC\Index.cshtml`
- `Areas\MainErp\Views\LC\Details.cshtml`
- `Areas\MainErp\Views\JournalEntries\Index.cshtml`
- `Areas\MainErp\Views\JournalEntries\Details.cshtml`
- `Areas\MainErp\Views\AccountingReports\Index.cshtml`
- `Areas\MainErp\Views\AccountingReports\JournalEntries.cshtml`
- `Areas\MainErp\Views\AccountingReports\AccountMovement.cshtml`
- `Areas\MainErp\Views\SalesReports\Index.cshtml`
- `Areas\MainErp\Views\SalesReports\SalesSummary.cshtml`
- `Areas\MainErp\Repositories\LC\LcReadRepository.cs`
- `Areas\MainErp\Repositories\JournalEntries\JournalEntryReadRepository.cs`
- `Areas\MainErp\Repositories\Reports\AccountingReportRepository.cs`
- `Areas\MainErp\Repositories\Reports\SalesReportRepository.cs`
- `Areas\MainErp\ViewModels\LC\LCIndexViewModel.cs`
- `Areas\MainErp\ViewModels\JournalEntries\JournalEntriesIndexViewModel.cs`
- `Areas\MainErp\ViewModels\JournalEntries\JournalEntryDetailsViewModel.cs`
- `Areas\MainErp\ViewModels\Reports\ReportRowViewModels.cs`
- `Areas\MainErp\Content\mainerp\mainerp.css`
- `MyERP.csproj`

Implemented:

- Same MainErp routes now support Arabic and English through resources; no duplicated views/controllers.
- MainErp culture preference is stored in Session and cookie.
- Layout switches `lang`, `dir`, and RTL/LTR CSS class dynamically.
- Sidebar, dashboard, LC, journal entries, accounting reports, and sales reports now use resource keys for visible labels.
- Database entity localization helper supports Arabic-first and English-first fallback.
- Account display standard is now centralized: `Account_Serial + " - " + localized account name`.
- LC linked accounts and voucher trace rows, journal rows, and accounting report rows no longer use raw `Account_Code` as primary display.

Safety:

- Build succeeded.
- No `Areas\Pos` files modified.
- No `AllScripts.sql` changes.
- No database schema changes.
- MainErp still uses MainErp-specific connection/context boundaries.

## 2026-05-07 LC and Project Extracts Premium UX Upgrade

Created:

- `AI_Docs\MainErp\34_LC_PremiumUXPlan.md`
- `AI_Docs\MainErp\35_ProjectExtracts_PremiumUXPlan.md`

Modified:

- `Areas\MainErp\Views\LC\Index.cshtml`
- `Areas\MainErp\Views\ProjectExtracts\Index.cshtml`
- `Areas\MainErp\Content\mainerp\mainerp.css`
- `Areas\MainErp\Resources\MainErp.resx`
- `Areas\MainErp\Resources\MainErp.ar.resx`

Implemented:

- LC is now a premium accounting cockpit instead of a raw data dump.
- LC includes search rail, cockpit header, sticky KPI band, account health cards, accounting timeline, voucher expanders, margin/opening balance tabs, and report placeholders.
- Project Extracts is now a project financial control center with executive KPIs, progress bars, execution/deduction/cash-flow tabs, and read-only voucher entry points.
- Dangerous operations remain disabled with the existing read-only message.
- New UX components are reusable for future accounting-heavy MainErp screens.

Safety:

- Build succeeded.
- No database changes.
- No `AllScripts.sql` changes.
- No `Areas\Pos` changes.
- No save/post/delete logic enabled.

## 2026-05-07 Discount Notifications Safe Read-Only Migration

Created:

- `Common\DiscountNotifications\DiscountNotificationModels.cs`
- `Common\DiscountNotifications\DiscountNotificationReadRepository.cs`
- `Areas\MainErp\Controllers\DiscountNotificationsController.cs`
- `Areas\MainErp\Views\DiscountNotifications\Index.cshtml`
- `Areas\Pos\Controllers\DiscountNotificationsController.cs`
- `Areas\Pos\Views\DiscountNotifications\Index.cshtml`
- `AI_Docs\MainErp\36_DiscountNotifications_Migration.md`

Modified:

- `MyERP.csproj`
- `Areas\MainErp\Views\Shared\_MainErpSidebar.cshtml`
- `Areas\MainErp\Resources\MainErp.resx`
- `Areas\MainErp\Resources\MainErp.ar.resx`
- `Areas\Pos\Views\PosDashboard\_Sidebar.cshtml`

Implemented:

- Migrated the legacy VB6 `FrmDiscounts.frm` screen as a safe read-only notification cockpit for both MainErp and Kishny/POS.
- MainErp route: `/MainErp/DiscountNotifications`.
- POS route: `/Pos/DiscountNotifications`.
- Reads `Notes` discount/debit-credit note types `9, 10, 8034, 9082, 9083, 9089, 9090, 9099`.
- Shows linked customer, branch, VAT/total fields, e-invoice metadata, and `DOUBLE_ENTREY_VOUCHERS` read-only lines.
- Uses the accounting display rule `Account_Serial - Account_Name`; raw `Account_Code` remains internal.
- Shared code is limited to a neutral read repository and view models; each module supplies its own connection factory.

Safety:

- MainErp uses `MainErp_ConnectionString`.
- POS/Kishny uses `KishnyCashConnection`.
- No save/post/delete/import behavior enabled.
- No `Notes` or voucher writes.
- No database changes.
- No `AllScripts.sql` changes.
- No `Areas\Pos\Sql` changes.
- Build succeeded.

Pending:

- Map and approve `SaveData`, `WriteDev`, `Del_Trans`, FIFO bill payment allocation, VAT account selection, QR/e-invoice lifecycle, reports, and attachments before any write phase.

## 2026-05-07 Project Extract Details Real Lines Fix

Created:

- `AI_Docs\MainErp\36_ProjectExtract_DetailLines_Implementation.md`

Modified:

- `Areas\MainErp\ViewModels\ProjectExtracts\ProjectExtractsIndexViewModel.cs`
- `Areas\MainErp\Repositories\ProjectExtracts\ProjectExtractReadRepository.cs`
- `Areas\MainErp\Views\ProjectExtracts\Details.cshtml`

Implemented:

- `/MainErp/ProjectExtracts/Details/{id}` now loads real `project_bill_details` rows where `bill_id = project_billl.id`.
- Added a real `بنود المستخلص` grid with quantity, previous/current/cumulative values, percentages, VAT, deductions, final value, and linked account display.
- Replaced placeholder messages for detail lines and advance payments with real data loading and real empty states.
- Added read-only advance payment sections for `TblPayPrePayed` and `TblProjePayPrePayed`.
- Added real voucher section from `DOUBLE_ENTREY_VOUCHERS` linked by `Notes_ID`, `project_bill_no`, or `bill_id`.
- Added journal entry links to `/MainErp/JournalEntries/DetailsByNote/{noteId}`.
- Applied account display rule: `Account_Serial - Account_Name`; raw account codes remain internal.

Validation:

- Tested against Eng sample `project_billl.note_id = 222097`, `project_billl.id = 3499`.
- Detail rows found: `1`.
- Linked voucher rows found: `4`.
- Linked advance payment rows found: `0`, and the page shows a real empty state.
- Build succeeded.

Safety:

- No save/edit/post/delete logic enabled.
- No database writes.
- No `AllScripts.sql` changes.
- No `Areas\Pos` changes in this fix.

## 2026-05-07 Main ERP FrmSaleBill6 Read-Only First Wave

Created:

- `AI_Docs\MainErp\37_FrmSaleBill6_MainERP_SourceMap.md`
- `AI_Docs\MainErp\38_SalesInvoice_SplitDesign.md`
- `AI_Docs\MainErp\39_SalesInvoice_LiveSchemaMapping.md`
- `AI_Docs\MainErp\40_SalesInvoice_AccountingInventoryFlow.md`
- `Areas\MainErp\ViewModels\SalesInvoices\SalesInvoiceViewModels.cs`
- `Areas\MainErp\Repositories\SalesInvoices\SalesInvoiceReadRepository.cs`
- `Areas\MainErp\Controllers\WorkshopSalesController.cs`
- `Areas\MainErp\Controllers\PumpSalesController.cs`
- `Areas\MainErp\Views\WorkshopSales\Index.cshtml`
- `Areas\MainErp\Views\WorkshopSales\Details.cshtml`

Modified:

- `MyERP.csproj`
- `Areas\MainErp\Views\Shared\_MainErpSidebar.cshtml`
- `Areas\MainErp\Resources\MainErp.resx`
- `Areas\MainErp\Resources\MainErp.ar.resx`
- `AI_Docs\MainErp\04_MainErp_Menu_Route_PermissionPlan.md`

Implemented:

- Confirmed the active Main ERP source from `F:\Source Code\SatriahMain\Account.vbp`: `Form=Frm\FrmSaleBill6.frm`.
- Explicitly excluded Kishny/Cayshny `FrmSaleBill6` copies as migration sources.
- Split the VB6 `C1Tab1` behavior into two MainErp routes:
  - `/MainErp/WorkshopSales`
  - `/MainErp/PumpSales`
- Added read-only list/search by date, invoice/customer text, and branch.
- Added read-only invoice details from `Transactions` and `Transaction_Details`.
- Added payment display from `TblSalesPayment` when present.
- Added voucher display from `DOUBLE_ENTREY_VOUCHERS` when linked by transaction/note fields.
- Added accounting balance indicators and inventory impact summary.
- Added MainErp sidebar entries and permissions plan for workshop and pump sales.

Validation:

- Eng schema was inspected for `Transactions`, `Transaction_Details`, `TblItems`, `TblUnites`, `tblPumpType`, `TblSalesPayment`, `Notes`, and `DOUBLE_ENTREY_VOUCHERS`.
- Current Eng sample data for `Transaction_Type IN (21,42,38,9)` returned one workshop-like row: `TypeInvoice=0`, `Transaction_Type=38`, `Max(Transaction_ID)=3832`.
- No pump sample row with `TypeInvoice=2` was found during this validation pass; the PumpSales route uses a real empty state until matching data is available.

Safety:

- Read-only only.
- No save/edit/post/delete behavior enabled.
- No accounting voucher creation.
- No inventory posting.
- No `AllScripts.sql` changes.
- No database schema changes.
- No `Areas\Pos` changes for this sales migration.

## 2026-05-07 Sales Invoice And Project Extract Premium Read-Only Preview

Created:

- `AI_Docs\MainErp\41_SalesInvoice_PremiumReadOnlyAndPreview.md`
- `AI_Docs\MainErp\42_ProjectExtracts_PremiumWorkspace.md`

Modified:

- `Areas\MainErp\ViewModels\PagedReadResult.cs`
- `Areas\MainErp\ViewModels\SalesInvoices\SalesInvoiceViewModels.cs`
- `Areas\MainErp\Repositories\SalesInvoices\SalesInvoiceReadRepository.cs`
- `Areas\MainErp\Controllers\WorkshopSalesController.cs`
- `Areas\MainErp\Controllers\PumpSalesController.cs`
- `Areas\MainErp\Views\WorkshopSales\Index.cshtml`
- `Areas\MainErp\Views\WorkshopSales\Details.cshtml`
- `Areas\MainErp\Views\ProjectExtracts\Details.cshtml`
- `AI_Docs\MainErp\40_SalesInvoice_AccountingInventoryFlow.md`

Implemented:

- Reworked workshop/pump invoice list and detail screens into premium operational workspaces.
- Added KPI cockpit, detail tabs, payment tab, inventory impact tab, accounting trace tab, print placeholder, and save-preview tab.
- Added read-only save preview based on existing invoice data; no write method is called.
- Added PumpSales diagnostics panel showing database, filters, row count, type breakdown, and close candidates with `PumpId`/`DetailsPump`.
- Re-inspected active `FrmSaleBill6.frm` and documented that pump report SQL confirms `Transaction_Type=21` and `TypeInvoice=2`.
- Enhanced Project Extract details with a read-only save-preview summary.

Validation:

- Build succeeded.
- No `INSERT`, `UPDATE`, `DELETE`, `ExecuteNonQuery`, or `SaveChanges` added to the new MainErp sales/project extract read paths.

Safety:

- No real save/post/delete behavior.
- No accounting voucher creation.
- No inventory posting.
- No database schema changes.
- No `AllScripts.sql` changes.
- No `Areas\Pos` changes for this phase.

## 2026-05-07 PumpSales Inventory Cost Preview And Audit UI

Created:

- `AI_Docs\MainErp\55_PumpSales_InventoryCostAndAudit.md`

Modified:

- `Areas\MainErp\Sql\03_SalesInvoice_ReadWrite_Procedures.sql`
- `Areas\MainErp\ViewModels\SalesInvoices\SalesInvoiceViewModels.cs`
- `Areas\MainErp\Repositories\SalesInvoices\SalesInvoiceReadRepository.cs`
- `Areas\MainErp\Controllers\PumpSalesController.cs`
- `Areas\MainErp\Views\WorkshopSales\Details.cshtml`

Implemented:

- Added controlled `@IncludeInventoryCost` support to `dbo.MainErp_PumpSales_Post`.
- Cost preview creates two balanced entries when enabled:
  - Debit `branches.a1` cost-of-sales account.
  - Credit `branches.a0` inventory account.
- Default actual posting remains unchanged with `IncludeInventoryCost = 0`.
- Added UI button `معاينة قيد التكلفة` for dry-run only.
- Added read-only Audit tab/section for `MainErp_AuditLog`.

Nagahat validation:

- Applied the MainErp sales SQL procedure script to `Wael\Sql2019 / Nagahat`.
- Dry-run on pump invoice `74004`:
  - Without inventory cost: 11 lines, debit/credit `431.5201`.
  - With inventory cost: 13 lines, inventory cost `407.0136`, debit/credit `838.5337`.

Safety:

- Cost posting is preview/controlled only from the UI.
- No `Areas\Pos` changes.
- No `AllScripts.sql` changes.
- No production configuration changes.

## 2026-05-07 PumpSales Cost Posting And Posted Cancellation

Created:

- `AI_Docs\MainErp\56_PumpSales_CostPostingCancelReceive.md`

Modified:

- `Areas\MainErp\Sql\03_SalesInvoice_ReadWrite_Procedures.sql`
- `Areas\MainErp\Controllers\PumpSalesController.cs`
- `Areas\MainErp\Repositories\SalesInvoices\SalesInvoiceReadRepository.cs`
- `Areas\MainErp\ViewModels\SalesInvoices\SalesInvoiceViewModels.cs`
- `Areas\MainErp\Views\WorkshopSales\Details.cshtml`

Implemented:

- Activated actual `post-cost` path for pump invoices using `@IncludeInventoryCost = 1`.
- Added actual posted cancellation workflow through `dbo.MainErp_PumpSales_CancelPosted`.
- Cancellation creates a reversal note, reversal voucher rows, and a `Transaction_Type = 20` receive/reversal inventory document.
- Source posted invoice is closed instead of deleted.
- Audit UI now includes user display plus before/after snapshots.

Nagahat validation:

- Applied updated SQL to `Wael\Sql2019 / Nagahat`.
- Rebuilt sample pump invoice `74004` with inventory cost posting.
- Cancelled sample pump invoice `74004`.
- Created reversal note `79418`.
- Created reversal voucher group `156010`.
- Created receive/reversal transaction `109513`.
- Verified source invoice `74004` is `Closed = 1`, has one linked receive doc, and audit rows were written.

Safety:

- Original invoice was not deleted.
- Original voucher was not deleted during cancellation.
- Reversal was additive and traceable.
- No `Areas\Pos` changes in this phase.
- No `AllScripts.sql` changes.

## 2026-05-07 Pump Sales Full Draft Save Start

Created:

- `AI_Docs\MainErp\51_PumpSales_FullDraftSave.md`
- `Areas\MainErp\Views\PumpSales\Edit.cshtml`

Modified:

- `Areas\MainErp\Controllers\PumpSalesController.cs`
- `Areas\MainErp\Repositories\SalesInvoices\SalesInvoiceReadRepository.cs`
- `Areas\MainErp\ViewModels\SalesInvoices\SalesInvoiceViewModels.cs`
- `Areas\MainErp\Views\WorkshopSales\Details.cshtml`
- `Areas\MainErp\Sql\03_SalesInvoice_ReadWrite_Procedures.sql`

Implemented:

- Added `/MainErp/PumpSales/New` and `/MainErp/PumpSales/Edit/{id}`.
- Added full draft save for pump invoice header in `Transactions`.
- Added full draft save for pump lines in `Transaction_Details`.
- Added full draft save for payment rows in `TblSalesPayment`.
- Rebuilt pump deferred customer allocations in `Transaction_DetailsPump` from the VB6 `DetailsPump` row format.
- Updated `tblPumpType.PercentV` from saved pump current quantities.
- Added full pump quantity validation matching VB6:
  `CurrentQty - PrevQty - CashQty - MadaQty - VisaQty - DeferredQty = 0`.
- Added lock protection for closed, posted, approved, and `IsPosted` invoices.
- Applied `dbo.MainErp_PumpSales_SaveDraftFull` to `Nagahat`.
- Validated dry-run and actual draft save on `Nagahat` transaction `74004`.
- Verified `Nagahat` transaction `95484` correctly fails preview because its existing pump quantity distribution is incomplete.

Still pending:

- `CreateIssueVoucher2`
- `CreateRecieveVoucher`
- `PG`
- `Notes`
- `DOUBLE_ENTREY_VOUCHERS`
- `DailyPumpR.Rpt`
- final post/approve/delete workflows
- final audit persistence

Safety:

- No `Areas\Pos` changes for this phase.
- No `AllScripts.sql` changes.
- Real financial posting and inventory documents are still disabled.

## 2026-05-07 Pump Sales Posting, Issue Voucher, Report, and Audit

Created:

- `AI_Docs\MainErp\52_PumpSales_PostingInventoryAudit.md`
- `Areas\MainErp\Views\PumpSales\DailyReport.cshtml`

Modified:

- `Areas\MainErp\Controllers\PumpSalesController.cs`
- `Areas\MainErp\Repositories\SalesInvoices\SalesInvoiceReadRepository.cs`
- `Areas\MainErp\ViewModels\SalesInvoices\SalesInvoiceViewModels.cs`
- `Areas\MainErp\Views\WorkshopSales\Details.cshtml`
- `Areas\MainErp\Sql\03_SalesInvoice_ReadWrite_Procedures.sql`

Implemented:

- Added `dbo.MainErp_PumpSales_Post`.
- Added `dbo.MainErp_AuditLog`.
- Added posting preview, actual post, and explicit rebuild path from the Pump details screen.
- Posting creates/updates `Notes`.
- Posting deletes/rebuilds `DOUBLE_ENTREY_VOUCHERS` for the pump invoice note.
- Posting creates a linked `Transaction_Type = 19` issue voucher if one does not already exist.
- Posting writes audit row with a correlation id.
- Added a web report route equivalent to the `DailyPumpR.Rpt` datasets:
  `/MainErp/PumpSales/DailyReport/{id}`.
- Strengthened draft save protection so posted/closed/approved invoices are blocked even in preview.

Nagahat validation:

- `74004` posting preview generated 11 balanced voucher rows: debit `431.5201`, credit `431.5201`.
- `74004` actual rebuild/post succeeded and created audit row `PumpSales.Post`.
- Existing linked issue voucher count for `74004` remained `1`; no duplicate issue voucher was created.
- `95484` posting preview was correctly blocked because pump quantities are not fully distributed.
- Draft save preview for posted `74004` is now blocked.

Still pending:

- Full `CreateRecieveVoucher` return-flow behavior.
- Full inventory cost accounting voucher if required by the legacy options.
- Crystal `.rpt` engine execution; the web route currently renders equivalent report data.
- Delete invoice workflow with reversal/audit rules.
- MainErp permission enforcement for post/rebuild/delete.

Build note:

- Full solution build is currently blocked by an unrelated existing POS compile error in `Areas\Pos\Controllers\PaymentsController.cs` referencing `MyERP.Areas.Pos.Repositories.PaymentVoucherReadRepository`.
- MainErp SQL deployment and Nagahat stored procedure validation succeeded.

## 2026-05-07 Sales Invoice Stored Procedures Performance Foundation

Created:

- `Areas\MainErp\Sql\03_SalesInvoice_ReadWrite_Procedures.sql`
- `AI_Docs\MainErp\46_SalesInvoice_StoredProcedures_PerformanceAndDraftSave.md`

Modified:

- `Areas\MainErp\Repositories\SalesInvoices\SalesInvoiceReadRepository.cs`
- `MyERP.csproj`

Implemented:

- Added `dbo.MainErp_SalesInvoice_Search` for paged workshop/pump invoice listing.
- Added `dbo.MainErp_SalesInvoice_GetDetails` for multi-result invoice details in one database round trip.
- Added `dbo.MainErp_SalesInvoice_SaveDraft` as a draft-save gate with dry-run enabled by default.
- Repository now prefers stored procedures and falls back to inline SQL if the procedures are missing in older test databases.
- Added the SQL script to the web project content list.

Validation:

- Applied the script to `Nagahat`.
- `MainErp_SalesInvoice_Search @TypeInvoice=2` returned `279` pump invoices.
- `MainErp_SalesInvoice_GetDetails @TypeInvoice=2, @TransactionId=95484` returned header, 24 detail rows, 4 payment rows, 0 voucher rows, and 1 related inventory row.
- `MainErp_SalesInvoice_SaveDraft` dry-run returned the payload and did not write.

Safety:

- No `AllScripts.sql` change.
- No POS/Kishny change.
- No active UI save/post/delete button enabled.
- No accounting voucher creation.
- No inventory posting.
- Draft save stored procedure is disabled by default through `@DryRun=1` and `@EnableDraftWrite=0`.

## 2026-05-07 Pump Deferred Customer Distribution

Created:

- `AI_Docs\MainErp\47_PumpDeferredCustomerDistribution.md`

Modified:

- `Areas\MainErp\ViewModels\SalesInvoices\SalesInvoiceViewModels.cs`
- `Areas\MainErp\Repositories\SalesInvoices\SalesInvoiceReadRepository.cs`
- `Areas\MainErp\Views\WorkshopSales\Details.cshtml`
- `Areas\MainErp\Sql\03_SalesInvoice_ReadWrite_Procedures.sql`
- `AI_Docs\MainErp\46_SalesInvoice_StoredProcedures_PerformanceAndDraftSave.md`

Implemented:

- Analyzed `FrmItemShowDet.frm` and confirmed `DetailsPump` format for deferred customer distribution.
- Added parser for `CustomerId#UnitId#Amount#CustomerName#Qty#UnitPrice#RecNo#@`.
- Pump invoice details now show the deferred customer distribution table under each pump line.
- Added total deferred distribution amount, quantity, and distinct customer count.
- Enriched parsed customer rows with customer account display from `TblCustemers` + `ACCOUNTS`.
- Added `dbo.MainErp_SalesInvoice_SavePumpDeferredDistribution` as a dry-run/default-disabled save gate for `DetailsPump`, `Deferred`, and `DeferredQty`.
- Applied updated SQL script to `Nagahat`.
- Dry-run validation succeeded for sample `Transaction_ID=95484`, line `ID=845857`.

Safety:

- UI remains read-only.
- No real distribution save was executed.
- No Notes/vouchers/inventory posting.
- No `AllScripts.sql` change.
- No POS/Kishny change.

## 2026-05-07 Pump Sales Nagahat Validation

Created:

- `AI_Docs\MainErp\45_PumpSales_NagahatValidation.md`

Modified:

- `Areas\MainErp\Repositories\SalesInvoices\SalesInvoiceReadRepository.cs`
- `Areas\MainErp\Views\WorkshopSales\Index.cshtml`
- `Views\DevStart\Index.cshtml`

Implemented:

- Confirmed that `Nagahat` is the useful local database for pump invoice testing.
- Found `279` pump invoices in `Nagahat` using `Transaction_Type=21` and `TypeInvoice=2`.
- Confirmed sample invoice `Transaction_ID=95484` with `24` pump detail rows.
- Added `Nagahat` to the debug-only MainErp database selector.
- Fixed PumpSales diagnostics SQL alias from `RowCount` to `RowsFound`.
- Added open links for diagnostic pump candidate rows.
- Updated pump empty state to direct local testing toward `Nagahat` instead of hardcoding `Eng`.

Safety:

- Read-only validation and display only.
- No save/post/delete.
- No database schema changes.
- No `AllScripts.sql` changes.
- No `Areas\Pos` changes for this phase.

## 2026-05-07 Pump Deferred Customer Distribution Edit Screen

Created:

- `AI_Docs\MainErp\48_PumpDeferredDistribution_EditScreen.md`
- `Areas\MainErp\Views\PumpSales\DeferredDistribution.cshtml`

Modified:

- `Areas\MainErp\Controllers\PumpSalesController.cs`
- `Areas\MainErp\ViewModels\SalesInvoices\SalesInvoiceViewModels.cs`
- `Areas\MainErp\Repositories\SalesInvoices\SalesInvoiceReadRepository.cs`
- `Areas\MainErp\Views\WorkshopSales\Details.cshtml`
- `AI_Docs\MainErp\47_PumpDeferredCustomerDistribution.md`
- `MyERP.csproj`

Implemented:

- Added the MainErp pump deferred distribution editor based on `FrmitemShowDet.frm`.
- Preserved the legacy `DetailsPump` serialization format: `CustomerId#UnitId#Amount#CustomerName#Qty#UnitPrice#RecNo#@`.
- Added a link from pump invoice detail lines to the distribution editor.
- Added repository save through `dbo.MainErp_SalesInvoice_SavePumpDeferredDistribution`.
- Save is intentionally limited to `Transaction_Details.DetailsPump`, `Deferred`, and `DeferredQty`.

Safety:

- No full invoice save.
- No posting.
- No `Notes` writes.
- No `DOUBLE_ENTREY_VOUCHERS` writes.
- No inventory issue/receive generation.
- The stored procedure refuses closed, posted, or approved invoices.
- No `AllScripts.sql` changes.
- No `Areas\Pos` changes for this phase.

## 2026-05-07 LC Save/Edit and Opening Voucher

Created:

- `AI_Docs\MainErp\49_LC_SaveEditAndOpeningVoucher.md`
- `Areas\MainErp\Repositories\LC\LcWriteRepository.cs`
- `Areas\MainErp\Views\LC\Edit.cshtml`

Modified:

- `Areas\MainErp\Controllers\LCController.cs`
- `Areas\MainErp\Repositories\LC\LcReadRepository.cs`
- `Areas\MainErp\ViewModels\LC\LCIndexViewModel.cs`
- `Areas\MainErp\Views\LC\Index.cshtml`
- `MyERP.csproj`

Implemented:

- Enabled LC create and edit against `TblLC`.
- Added account generation for missing LC linked accounts under selected parent accounts.
- Added guarded creation of the main LC opening voucher `NoteType=22001`.
- Voucher amount follows the confirmed FrmLC pattern: `Value * PercentV / 100 * Currency_rate`.
- Voucher posts debit to margin account and credit to `BanksData.Account_Code`.
- Existing voucher rows prevent duplicate posting.

Deferred:

- Delete.
- LC grid posting.
- Opening-balance posting.
- Destructive rebuild of existing vouchers.

Safety:

- All write operations use MainErp connection and explicit SQL transactions.
- No `AllScripts.sql` changes.
- No `Areas\Pos` changes for this phase.

Follow-up implemented in the same phase:

- Added guarded `22010` open-expense voucher creation.
- Added guarded `22005` close voucher creation.
- `22010` posts expense + VAT input debit against bank credit, following the observed Eng/VB6 VAT-inclusive behavior.
- `22005` posts bank debit against margin account credit and locks/closes the LC.
- Existing voucher rows prevent duplicate creation.

Eng smoke test:

- Created `TblLCID=198`, `LCNO=WEBTEST-LC-20260507184202`.
- Generated notes `222099` (`22001`), `222100` (`22010`), and `222101` (`22005`).
- Generated vouchers `393525`, `393526`, and `393527`.
- Verified all three voucher groups are balanced.
- No `AllScripts.sql` changes.
- No `Areas\Pos` changes.

## 2026-05-07 LC Premium UI Completion

Modified:

- `Areas\MainErp\Views\LC\Edit.cshtml`
- `Areas\MainErp\Views\LC\Index.cshtml`
- `Areas\MainErp\Content\mainerp\mainerp.css`
- `Areas\MainErp\Views\Shared\_MainErpLayout.cshtml`

Implemented:

- Reworked LC create/edit into a premium operational workspace instead of a raw technical form.
- Added an executive LC summary bar with LC number, value, expected opening voucher, expected open-expense voucher, and status.
- Added real workflow tabs for basic data, bank/currency, linked accounts, dates/payment, and notes/control.
- Added protected/disabled buttons for destructive or unmapped actions: delete, voucher rebuild, grid posting, opening-balance posting, and report execution.
- Added confirmation prompts before creating actual LC vouchers from the Workbench.
- Replaced ambiguous action labels with explicit business labels: opening voucher, open-expense voucher, close LC.
- Added responsive CSS for dense desktop ERP use while keeping tablet/mobile stacking.
- Bumped the MainErp CSS cache version to load the upgraded LC styling.

Still pending:

- Lookup dropdowns for banks, vendors, currencies, branches, countries, boxes, and accounts.
- Save/post logic for LC grids: `TBLLCHistory`, `TBLLCMargin`, `TBLLCMargin2`, and `tblLCOpenB`.
- Opening-balance posting into `DOUBLE_ENTREY_VOUCHERS1`.
- Safe destructive rebuild workflow with audit and confirmation.
- LC reports and attachments execution.

Validation:

- Build succeeded.
- No `Areas\Pos` changes.
- No `AllScripts.sql` changes.

## 2026-05-07 LC Lookups and Grid Save Phase

Modified:

- `Areas\MainErp\ViewModels\LC\LCIndexViewModel.cs`
- `Areas\MainErp\Repositories\LC\LcWriteRepository.cs`
- `Areas\MainErp\Controllers\LCController.cs`
- `Areas\MainErp\Views\LC\Edit.cshtml`
- `Areas\MainErp\Content\mainerp\mainerp.css`

Implemented:

- Replaced manual ID/code entry with real MainErp lookups for:
  - LC types from `LCTypes`.
  - bank/payment bank from `BanksData`.
  - boxes from `TblBoxesData`.
  - currencies from `currency`.
  - countries from `TblCountriesData`.
  - vendors from `TblCustemers`.
  - branches from `TblBranchesData`.
  - linked accounts from `ACCOUNTS`, displayed as `Account_Serial - Account_Name`.
- Added editable LC grid sections to the LC edit screen:
  - `TBLLCHistory`
  - `TBLLCMargin`
  - `TBLLCMargin2`
  - `tblLCOpenB`
- Grid save supports insert/update of the mapped operational fields.
- Grid save intentionally does not generate or rebuild grid accounting notes/vouchers yet.

Eng validation:

- Loaded LC `197` through `LcWriteRepository.GetForEdit`.
- Confirmed lookups loaded: banks, vendors, accounts, and existing `TBLLCMargin2` rows.
- Saved a test `TBLLCMargin2` row for test LC `198`.
- Verified inserted row: `TBLLCMargin2.ID = 50858`, `TblLCID = 198`, `Amount = 321.45`.

Still pending:

- Delete row workflow for LC grids.
- Voucher posting for grid rows through the exact VB6 `createVoucher2 / CREATE_VOUCHER_GE2` paths.
- Opening balance posting into `DOUBLE_ENTREY_VOUCHERS1`.
- Lookup paging/search for very large vendor/account lists.

Safety:

- No `AllScripts.sql` changes.
- No MainErp SQL migrations added.
- No intentional `Areas\Pos` edits in this phase. The worktree already contains unrelated POS changes that were left untouched.

## 2026-05-07 LC Opening Balance, Rebuild, Delete, and UI Control Phase

Modified:

- `Areas\MainErp\Infrastructure\ManualIdTarget.cs`
- `Areas\MainErp\ViewModels\LC\LCIndexViewModel.cs`
- `Areas\MainErp\Repositories\LC\LcWriteRepository.cs`
- `Areas\MainErp\Controllers\LCController.cs`
- `Areas\MainErp\Views\LC\Index.cshtml`
- `Areas\MainErp\Views\LC\Edit.cshtml`
- `Areas\MainErp\Content\mainerp\mainerp.css`
- `AI_Docs\MainErp\49_LC_SaveEditAndOpeningVoucher.md`

Implemented:

- Added `Notes1.NoteID` as a controlled manual-id target.
- Added LC header save/load fields for:
  - `OpenBalance`
  - `OpenBalanceType`
  - `OpenBalanceDate`
  - `opening_balance_voucher_id`
- Implemented protected opening-balance posting into:
  - `Notes1`
  - `DOUBLE_ENTREY_VOUCHERS1`
- Added protected LC rebuild:
  - requires `REBUILD-LC-{TblLCID}`.
  - deletes and recreates core LC normal/open-expense/opening-balance vouchers.
  - does not recreate detailed grid vouchers yet.
- Added protected LC delete:
  - requires `DELETE-LC-{TblLCID}`.
  - deletes LC notes, opening notes, normal/opening voucher rows, LC grids, LC-generated accounts, and `TblLC`.
- Improved LC workbench control zone with professional warning cards and typed confirmation fields.
- Added opening-balance fields to the LC edit screen.

Eng validation:

- Prepared test LC `198` with `OpenBalance = 222.22`, `OpenBalanceType = 0`.
- Posted opening balance successfully:
  - `Notes1.NoteID = 716`
  - `DOUBLE_ENTREY_VOUCHERS1.Double_Entry_Vouchers_ID = 6577`
  - debit `222.2200`
  - credit `222.2200`
  - 2 voucher lines
- Rebuilt LC `198`; normal vouchers and opening-balance vouchers remained balanced.
- Tested delete on temporary LC `999198`; final `TblLC` count for that id was `0`.
- Build succeeded.

## 2026-05-07 Pump/Workshop Sales Customer Deferred Distribution

Created:

- `AI_Docs\MainErp\50_PumpWorkshopSales_CurrentGapsAndCustomerDeferred.md`

Modified:

- `Areas\MainErp\ViewModels\SalesInvoices\SalesInvoiceViewModels.cs`
- `Areas\MainErp\Repositories\SalesInvoices\SalesInvoiceReadRepository.cs`
- `Areas\MainErp\Views\PumpSales\DeferredDistribution.cshtml`

Implemented:

- Pump deferred customer distribution now reads persisted `Transaction_DetailsPump` rows, not only `Transaction_Details.DetailsPump`.
- Distribution rows are matched to invoice detail lines by `Transaction_Details.LineID`, which matches the VB6 grid line behavior, with a safe fallback to `Transaction_Details.ID`.
- Added a customer lookup from `TblCustemers` in the pump deferred distribution screen.
- Existing selected customers are included in the lookup even when outside the first lookup page.
- Save/preview hydrates customer names and account displays from `TblCustemers` and `ACCOUNTS` before rebuilding the VB6 `DetailsPump` string.
- Customer account display uses `Account_Serial - Account_Name`; raw `Account_Code` remains internal.
- Added quantity safety validation so deferred quantity plus non-deferred quantity cannot exceed the line quantity limit.

Validation:

- Built `MyERP.sln` successfully.
- Tested against `Nagahat`, pump invoice `Transaction_ID = 95484`.
- Loaded `24` detail lines, `1` line with persisted pump deferred distribution, total `116.50`, quantity `50`.
- Dry-run preview rebuilt: `4#1#116.5#نجاحات للتجارة والنقل#50#2.33#255#@`.

Safety:

- No `Areas\Pos` changes.
- No `AllScripts.sql` changes.
- No Notes, voucher, accounting, or inventory posting writes were added.
- Full invoice save/posting remains pending.

Still pending:

- Exact row-level voucher posting for:
  - `TBLLCHistory`
  - `TBLLCMargin`
  - `TBLLCMargin2`
  - detailed `tblLCOpenB`
- Database audit-table persistence for rebuild/delete actions.
- Permission-gating destructive LC actions by role.

Safety:

- No `Areas\Pos` changes.
- No `AllScripts.sql` changes.
- No schema changes or stored procedure changes.

## 2026-05-07 Pump Sales Permissions and Cancellation Preview

Created:

- `AI_Docs\MainErp\54_PumpSales_PermissionsCancelReceiveCostPG.md`

Modified:

- `Areas\MainErp\Models\Security\MainErpUserContext.cs`
- `Areas\MainErp\Services\Security\MainErpLoginService.cs`
- `Areas\MainErp\Infrastructure\LegacyScreenPermissionService.cs`
- `Areas\MainErp\Controllers\PumpSalesController.cs`
- `Areas\MainErp\Repositories\SalesInvoices\SalesInvoiceReadRepository.cs`
- `Areas\MainErp\Views\WorkshopSales\Details.cshtml`
- `Areas\MainErp\Sql\03_SalesInvoice_ReadWrite_Procedures.sql`

Implemented:

- Loaded `TblUsers.CanPostPumpInv` into the MainErp user context.
- Added legacy action permission checks for add/edit/delete/print through `ScreenJuncUser`.
- Gated pump edit, post, rebuild, draft delete, and deferred distribution actions.
- Added `dbo.MainErp_PumpSales_CancelPreview` for posted invoice cancellation/reversal preview only.
- Added a UI button for `معاينة إلغاء فاتورة مرحلة`.
- Reviewed `CreateRecieveVoucher` and confirmed it is a sales-return receive-voucher path, not a normal pump sale posting path.
- Reviewed `PG` and documented the remaining cost/inventory parity risk.

Safety:

- No actual cancellation/reversal writes yet.
- No posted invoice delete.
- No `Areas\Pos` changes.
- No `AllScripts.sql` changes.

## 2026-05-07 LC Completion Pass

Created:

- `AI_Docs\MainErp\57_LC_CompletionPass.md`

Modified:

- `Areas\MainErp\Controllers\LCController.cs`
- `Areas\MainErp\Repositories\LC\LcReadRepository.cs`
- `Areas\MainErp\Repositories\LC\LcWriteRepository.cs`
- `Areas\MainErp\ViewModels\LC\LCIndexViewModel.cs`
- `Areas\MainErp\Views\LC\Index.cshtml`
- `Areas\MainErp\Content\mainerp\mainerp.css`

Implemented:

- Replaced manual code entry for LC workbench bank/supplier/branch filters with live lookups.
- Added editable placeholder rows for all LC grids on new and existing LC edit screens.
- Added legacy permission checks around LC add/edit/save/delete/post/rebuild routes.
- Styled LC select controls consistently with the premium editor inputs.

Safety:

- No `Areas\Pos` changes in this LC pass.
- No `AllScripts.sql` changes.
- No database schema changes.
- Row-level voucher posting for LC grids moved to `58_LC_GridVoucherPosting.md`.

## 2026-05-07 Pump Sales Draft Delete Safety

Created:

- `AI_Docs\MainErp\53_PumpSales_DraftDeleteSafety.md`

Modified:

- `Areas\MainErp\Sql\03_SalesInvoice_ReadWrite_Procedures.sql`
- `Areas\MainErp\Controllers\PumpSalesController.cs`
- `Areas\MainErp\Repositories\SalesInvoices\SalesInvoiceReadRepository.cs`
- `Areas\MainErp\Views\WorkshopSales\Details.cshtml`
- `Areas\MainErp\Content\mainerp\mainerp.css`

Implemented:

- Added `dbo.MainErp_PumpSales_DeleteDraft`.
- Added dry-run preview for draft delete.
- Added actual delete only for unposted, open, unapproved pump drafts.
- Blocked delete if `Notes`, `DOUBLE_ENTREY_VOUCHERS`, or linked inventory issue/receive documents already exist.
- Deleted draft rows from `Transaction_DetailsPump`, `TblSalesPayment`, `Transaction_Details`, and `Transactions`.
- Added guarded `tblPumpType.PercentV` rollback to previous reading only when the current reading still matches the deleted draft current reading.
- Added audit logging through `MainErp_AuditLog`.

Safety:

- Posted/approved/closed invoices are not deleted.
- Existing vouchers are not deleted.
- No posted cancellation/reversal is implemented yet.
- No `Areas\Pos` changes.
- No `AllScripts.sql` changes.

## 2026-05-07 FrmSaleBill6 Operational Workbench Phase 2

Created:

- `AI_Docs\MainErp\44_FrmSaleBill6_OperationalWorkbenchPhase2.md`

Modified:

- `Areas\MainErp\ViewModels\SalesInvoices\SalesInvoiceViewModels.cs`
- `Areas\MainErp\Repositories\SalesInvoices\SalesInvoiceReadRepository.cs`
- `Areas\MainErp\Views\WorkshopSales\Details.cshtml`
- `AI_Docs\MainErp\37_FrmSaleBill6_MainERP_SourceMap.md`

Implemented:

- Added a VB6 routine-stage panel in the invoice details screen.
- Added payment comparison between `TblSalesPayment` totals and header `PayedValue`.
- Added pump line fields: `Account_CodeComm`, `IsOther`, `ColorID`, and `DetailsPump` where present.
- Added commission account display using `Account_Serial - Account_Name`.
- Added print/report mapping for `PrintReport`, `PrintCash`, `cmdPrint*`, and `DailyPumpR.Rpt` as read-only documentation inside the screen.
- Extended save preview text with payment row totals.

Safety:

- Read-only only.
- No save/post/delete.
- No payment creation.
- No inventory voucher creation.
- No Crystal report execution.
- No database schema changes.
- No `AllScripts.sql` changes.
- No `Areas\Pos` changes for this phase.

## 2026-05-07 FrmSaleBill6 Read-Only Trace Expansion

Created:

- `AI_Docs\MainErp\43_FrmSaleBill6_ReadOnlyTraceExpansion.md`

Modified:

- `Areas\MainErp\ViewModels\SalesInvoices\SalesInvoiceViewModels.cs`
- `Areas\MainErp\Repositories\SalesInvoices\SalesInvoiceReadRepository.cs`
- `Areas\MainErp\Views\WorkshopSales\Details.cshtml`
- `AI_Docs\MainErp\40_SalesInvoice_AccountingInventoryFlow.md`

Implemented:

- Added operational header trace fields from `Transactions`: `Posted`, `Approved`, `Prefix`, `Fullcode`, `CBoBasedON`, `POSBillType`, `Transaction_NetValue`, `SumValueLine`, `SumVATLine`, and `DateRec`.
- Added read-only related inventory transaction trace for generated issue/receive vouchers.
- Related inventory lookup follows VB6 linkage through `Transactions.nots = source Transaction_ID` and `Transactions.nots2 = source NoteSerial1`.
- Displayed `Transaction_Type=19` as an issue voucher trace and `Transaction_Type=20` as a receive voucher trace.
- Added journal links for related inventory transactions when `NoteId` is available.
- Updated save preview warnings and inventory effects to include linked inventory document counts.

Safety:

- Read-only only.
- No `INSERT`, `UPDATE`, or `DELETE`.
- No call to `CreateIssueVoucher2` or `CreateRecieveVoucher`.
- No database schema changes.
- No `AllScripts.sql` changes.
- No `Areas\Pos` changes for this phase.
## 2026-05-07 LC Grid Voucher Posting Phase

Created:

- `AI_Docs\MainErp\58_LC_GridVoucherPosting.md`

Modified:

- `Areas\MainErp\Repositories\LC\LcWriteRepository.cs`
- `Areas\MainErp\Repositories\LC\LcReadRepository.cs`
- `Areas\MainErp\Controllers\LCController.cs`
- `Areas\MainErp\ViewModels\LC\LCIndexViewModel.cs`
- `Areas\MainErp\Views\LC\Index.cshtml`
- `Areas\MainErp\Sql\05_MainErp_AuditLog.sql`
- `MyERP.csproj`
- `AI_Docs\MainErp\57_LC_CompletionPass.md`
- `AI_Docs\MainErp\31_LC_GridVoucherMapping.md`

Implemented:

- Added guarded creation of missing LC grid vouchers for `TBLLCHistory`, `TBLLCMargin`, `TBLLCMargin2`, and `tblLCOpenB`.
- Mapped VB6 `createVoucher2` / `CREATE_VOUCHER_GE2` paths for `TypeGrid=1`, `TypeGrid=3`, `TypeGrid=4`, and `TypeGrid=6`.
- Added support for `TBLLCMargin2.IsOpenBalance` rows using `Notes1` and `DOUBLE_ENTREY_VOUCHERS1`.
- Added the LC workbench action `إنشاء قيود الجريدات`.
- Kept grid voucher posting separate from core LC voucher rebuild.
- Updated core rebuild safety so it does not delete grid notes when rebuilding header/opening/close vouchers.
- Added `MainErp_AuditLog` setup script under MainErp SQL.
- Added LC Audit UI inside the `تاريخ العمليات` tab.
- Added safe audit writes for LC header posting, open-expense posting, close, opening balance, grid voucher posting, rebuild, and delete.

Eng validation:

- Applied `Areas\MainErp\Sql\05_MainErp_AuditLog.sql` to `Wael\Sql2019 / Eng`.
- Verified `dbo.MainErp_AuditLog` exists.
- Verified initial Audit row count is `0`.
- Direct PowerShell repository invocation was not possible outside the web app because the full `MyERP.dll` load requires DevExpress runtime assemblies. The action is available for runtime testing through `/MainErp/LC`.

Still pending:

- Individual grid-row delete/rebuild with linked note cleanup.
- Runtime validation for `TBLLCHistory` once a real Eng sample row exists.
- Deeper audit UI for LC grid posting operations.

Safety:

- No `Areas\Pos` changes in this LC phase.
- No `AllScripts.sql` changes.
- MainErp-local audit table setup was applied to the `Eng` test database only.
- The grid posting action creates missing grid vouchers only and does not rebuild existing grid vouchers.

## 2026-05-07 LC Grid Voucher UI Test, Row Delete, and Grid Rebuild

Modified:

- `Areas\MainErp\Repositories\LC\LcWriteRepository.cs`
- `Areas\MainErp\Repositories\LC\LcReadRepository.cs`
- `Areas\MainErp\Controllers\LCController.cs`
- `Areas\MainErp\ViewModels\LC\LCIndexViewModel.cs`
- `Areas\MainErp\Views\LC\Index.cshtml`
- `AI_Docs\MainErp\58_LC_GridVoucherPosting.md`

Runtime UI validation on `Wael\Sql2019 / Eng`:

- Opened `/MainErp/LC?selectedId=198`.
- Logged in with `admin` and the dev master password.
- Clicked `إنشاء قيود الجريدات`.
- Generated `Notes.NoteID=222101`, `NoteSerial=202604465` for `TBLLCMargin2.ID=50858`.
- Verified `DOUBLE_ENTREY_VOUCHERS` has 2 lines, debit `321.45`, credit `321.45`, difference `0.00`.
- Verified `MainErp_AuditLog` has `LC.PostGridVouchers`.
- Opened `/MainErp/JournalEntries/DetailsByNote/222101` from the LC accounting timeline successfully.

Implemented:

- Safe row-level delete for LC grid rows.
- UI delete controls inside the LC grid tab with strong confirmation.
- Confirmation pattern: `DELETE-LC-GRID-{TblLCID}-{SourceTable}-{RowID}`.
- Whitelisted source tables only:
  - `TBLLCHistory`
  - `TBLLCMargin`
  - `TBLLCMargin2`
  - `tblLCOpenB`
- Row delete removes only the row's linked `Notes` / `Notes1` and `DOUBLE_ENTREY_VOUCHERS` / `DOUBLE_ENTREY_VOUCHERS1`.
- Row delete writes `LC.DeleteGridRow` audit.
- Grid-only rebuild action with confirmation pattern `REBUILD-LC-GRIDS-{TblLCID}`.
- Grid rebuild clears and recreates grid row vouchers only; it does not touch header/core LC vouchers.

Runtime delete validation:

- Deleted `TBLLCMargin2.ID=50858` for `TblLCID=198` from the UI.
- Verified the row was removed.
- Verified `Notes.NoteID=222101` was removed.
- Verified `DOUBLE_ENTREY_VOUCHERS.Notes_ID=222101` was removed.
- Verified `MainErp_AuditLog` has `LC.DeleteGridRow`.

Build:

- `MyERP.sln` Debug / Any CPU builds successfully.
- Build has existing legacy warnings only.

Safety:

- No `Areas\Pos` files were intentionally changed in this LC step.
- No `AllScripts.sql` changes.
- No schema changes beyond the already documented `MainErp_AuditLog` test setup in `Eng`.

## 2026-05-09 Open Screens Completion Pass

Created:

- `AI_Docs\MainErp\59_OpenScreens_CompletionPass.md`
- `Areas\MainErp\Views\LC\Report.cshtml`
- `Areas\MainErp\Views\ProjectExtracts\Report.cshtml`
- `Areas\MainErp\Views\WorkshopSales\Report.cshtml`

Modified:

- `Areas\MainErp\Controllers\LCController.cs`
- `Areas\MainErp\Controllers\ProjectExtractsController.cs`
- `Areas\MainErp\Controllers\WorkshopSalesController.cs`
- `Areas\MainErp\Views\LC\Index.cshtml`
- `Areas\MainErp\Views\ProjectExtracts\Details.cshtml`
- `Areas\MainErp\Views\WorkshopSales\Details.cshtml`
- `MyERP.csproj`

Implemented:

- Added granular LC permission constants for header posting, grid posting, rebuild, delete, and reports.
- Kept VB6 `FrmLC` permission fallback until final MainErp permission persistence is introduced.
- Added a read-only LC Web Report route and view.
- Added a read-only Project Extract Web Report route and view.
- Added a read-only Workshop Sales Web Report route and view.
- Registered existing Pump Sales MainErp views in the project file.
- Fixed `SalesInvoiceReadRepository` optional FrmSaleBill6 trace-field reading so missing columns such as `IsPosted` do not crash Workshop Sales report/details paths.
- Added a compile-safe `Areas\Reports` skeleton because `MyERP.csproj` already referenced those files but the files were missing from disk.

Build:

- `MyERP.sln` Debug / Any CPU builds successfully.

Runtime validation:

- `/MainErp/LC/Report/198` returns HTTP 200 without server/Razor/404 errors.
- `/MainErp/ProjectExtracts/Report/222097` returns HTTP 200 without server/Razor/404 errors.
- `/MainErp/WorkshopSales/Report/3832` returns HTTP 200 without server/Razor/404 errors.

Safety:

- No new database writes were introduced by these report routes.
- No `AllScripts.sql` changes.
- No intentional `Areas\Pos` changes in this pass.

## 2026-05-09 LC and Project Extracts UAT Delivery Test

Created:

- `AI_Docs\MainErp\60_UAT_LC_ProjectExtracts_20260509.md`

Modified:

- `Areas\MainErp\Repositories\LC\LcWriteRepository.cs`

Test database:

- `MainErp_ConnectionString -> Wael\Sql2019 / Eng`

LC UAT sample:

- Created `TblLCID=199`, `LCNO=UAT-LC-20260509164035`.
- Auto-created 4 linked accounts under the selected parent accounts.
- Edited the same LC and verified `Value`, `OpenValue`, `OpenBalance`, `ProjectName`, and `Remarks`.
- Created real LC header/opening voucher:
  - `Notes.NoteID=222101`
  - Debit `625.00`
  - Credit `625.00`
- Created real LC open-expense voucher:
  - `Notes.NoteID=222102`
  - Debit `333.33`
  - Credit `333.33`
- Created real opening-balance rows in `DOUBLE_ENTREY_VOUCHERS1`:
  - `opening_balance_voucher_id=6578`
  - Debit `1100.00`
  - Credit `1100.00`
- Verified journal links for `222101` and `222102`.
- Verified audit entries for header posting, expense posting, opening balance posting, and grid voucher action.

LC usability/performance fix:

- Reduced LC edit account lookup rendering from about `9.29 MB` to about `874 KB`.
- Account lookup now loads a bounded working set plus all selected account codes.
- Future recommended improvement: AJAX account search for very large charts of accounts.

Project Extracts UAT sample:

- Tested `project_billl.id=3499`, `NoteID=222097`.
- Verified list/search/details/report routes return HTTP 200.
- Verified one real `project_bill_details` row is loaded.
- Verified linked voucher rows are balanced:
  - Debit `1725.00`
  - Credit `1725.00`
- Verified no linked advance payments exist for this sample and the screen shows a real empty state.

Current known gap:

- Project Extracts are still read-only in code. Create/edit/save/post/delete/account creation are not implemented yet.

Build:

- `MyERP.sln` Debug / Any CPU builds successfully.

Safety:

- No `AllScripts.sql` changes.
- No intentional `Areas\Pos` changes in this UAT pass.

## 2026-05-09 Completion & Hardening Delivery Pass

Created:

- `AI_Docs\MainErp\61_CompletionHardening_FinalDeliveryReport_20260509.md`

Modified:

- `Areas\MainErp\Repositories\LC\LcReadRepository.cs`
- `Areas\MainErp\Repositories\LC\LcWriteRepository.cs`
- `Areas\MainErp\Repositories\ProjectExtracts\ProjectExtractReadRepository.cs`
- `Areas\MainErp\Repositories\SalesInvoices\SalesInvoiceReadRepository.cs`

Implemented hardening:

- Converted raw user-facing SQL schema errors into friendly Arabic compatibility warnings.
- Preserved full SQL details internally through `Trace.TraceWarning`.
- Re-tested MainErp route set on both `Eng` and `Cash`.
- Re-tested key report/print routes on both `Eng` and `Cash`.
- Restarted IIS Express after build so validation used the updated binaries.

Validation:

- `MyERP.sln` Debug / Any CPU builds successfully.
- MainErp tested routes on `Eng`: Dashboard, LC, ProjectExtracts, WorkshopSales, PumpSales, JournalEntries, AccountingReports, SalesReports, Payments, Cashing, DiscountNotifications.
- MainErp tested routes on `Cash`: same route set.
- Report/print routes tested:
  - LC report.
  - Project Extract report.
  - Workshop Sales report.
  - Payment details/print.
  - Cashing details/print.

Outcome:

- No server errors, stack traces, null reference errors, raw SQL schema errors, or unhandled exceptions were detected in the tested route responses.
- Remaining production gaps are documented clearly in report 61, especially Project Extract write lifecycle and full sales posting/cancel/reverse matrix.
