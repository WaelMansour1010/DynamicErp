# Migrated Core Stabilization Audit

Date: 2026-05-15

Scope: audit only. No application code, SQL, routing, or screen rebuild changes were made.

## Constitution Understanding

Project areas:

- Original Web: the older MVC implementation under root controllers, views, repositories, reporting, and database scripts.
- POS: operational Kishny/POS area under `Areas/Pos`, with its own layouts, routes, session context, permissions, and SQL repository.
- MainErp: migrated ERP area under `Areas/MainErp`, intended to replace or wrap legacy ERP screens without breaking working behavior.
- Shared: cross-area code under `Common` and selected MainErp service/repository models that should become reusable engines where business behavior is common.

Current phase: Stabilization + Unification.

Main rules followed in this audit:

- Preserve working screens, especially POS/Kishny operational screens.
- Classify existing implementations before editing.
- Prefer shared business engines with separate MainErp/POS wrappers.
- Keep route, layout, permission, and database-context boundaries separate per area.
- Keep business logic out of views.
- Treat accounting, stock, permissions, opening balances, and journal posting as high-risk financial flows.
- Maintain SQL Server 2012 compatibility for any future SQL work.

Must avoid:

- Rebuilding already-working screens without a controlled migration reason.
- Duplicating POS/MainErp logic into separate hardcoded implementations.
- Using Kishny/POS as the source for MainErp accounting rules when Main Original VB6 is the required authority.
- Cross-area route leakage that bypasses the intended wrapper, layout, context, or permissions.
- Silent unsafe posting for journals, stock, vouchers, or opening balances.
- Code or SQL changes during this audit-only phase.

Classification method used:

- Route ownership: area registration and concrete URLs.
- Controller ownership: `Areas/MainErp`, `Areas/Pos`, or root Original Web.
- View/layout ownership: area layout, sidebar/menu, scripts, and styles.
- Logic ownership: service, repository, shared `Common` code, or view script.
- Permission ownership: MainErp legacy screen permissions, POS permission keys/context, or Original Web role/user privileges.
- Data ownership: tables, stored procedures, note/voucher tables, and database context usage.
- Shared readiness: whether the current implementation is a true shared core or only duplicated/similar behavior.

## Executive Summary

The migrated core is not in one consistent state yet. Some modules are good candidates for shared stabilization, some are placeholders, and some have independent Original Web, POS, and MainErp implementations.

Most urgent finding: Journal Vouchers are not unified. POS has the full operational manual journal screen and save path. MainErp has only a read-only journal list/details screen. There is no shared journal-entry engine today. This directly explains why the Kishny/POS journal screen works while MainErp does not work as an entry screen.

Opening Balance Journal mode does not currently exist as a professional shared mode. There are opening-balance fragments in account master data, LC opening vouchers, bank/box opening balances, and planned import models, but no unified `قيد افتتاحي` screen or route built on the journal engine.

Shared code already exists for some domains, especially warehouses/store data, users, employee payroll, and discount notifications. The same pattern should be used for journal entries after the Main Original VB6 opening-balance rules are traced.

## High-Risk Findings

### Journal Vouchers

Located implementations:

- POS operational controller: `Areas/Pos/Controllers/JournalEntriesController.cs`
- POS operational view: `Areas/Pos/Views/JournalEntries/Index.cshtml`
- POS account selector script: `Areas/Pos/Scripts/account-selector.js`
- POS repository logic: `Areas/Pos/Data/PosSqlRepository.cs`
- MainErp read-only controller: `Areas/MainErp/Controllers/JournalEntriesController.cs`
- MainErp read-only repository: `Areas/MainErp/Repositories/JournalEntries/JournalEntryReadRepository.cs`
- MainErp read-only view: `Areas/MainErp/Views/JournalEntries/Index.cshtml`
- Original Web EF implementation: `Controllers/JournalEntriesController.cs`, `Views/JournalEntries/Index.cshtml`, `Views/JournalEntries/AddEdit.cshtml`
- Reports: `Reporting/Reports/JournalEntry_Report.*`, `Reporting/Reports/JournalEntryDetails_Report.*`, `Views/Report/JournalEntry.cshtml`, `Views/Report/JournalEntryDetails.cshtml`

Current route status:

- MainErp route exists through area default: `/MainErp/JournalEntries`
- MainErp detail routes exist: `/MainErp/JournalEntries/DetailsByNote/{noteId}`, `/MainErp/JournalEntries/DetailsByVoucher/{voucherId}`
- POS route exists through area default and menu: `/Pos/JournalEntries/Index`
- Original Web route exists: `/JournalEntries`

Current ownership:

- POS is the source of the currently working manual journal UX/save flow.
- MainErp is only a read-only wrapper over legacy voucher tables.
- Original Web is a separate EF-era implementation over different journal tables/models.
- There is no shared journal core yet.

Important behavior found in POS:

- Manual journal `NoteType = 57`.
- Saves to `dbo.Notes` and `dbo.DOUBLE_ENTREY_VOUCHERS`.
- Validates branch, date, account existence by lookup, at least two lines, non-negative values, one side per line, and balanced totals.
- Protects updates to automatic journals with a general admin password.
- Uses POS permissions such as `CanViewJournalEntry`, `CanCreateJournalEntry`, `CanEditJournalEntry`, `CanDeleteJournalEntry`.

MainErp gap:

- `Index` can search/list journals.
- Details can open by note or voucher.
- New/save/edit/delete workflow is not present.
- Toolbar actions in the MainErp journal view are disabled/read-only.

Required stabilization direction:

- Extract a shared journal core from the proven POS behavior only after verifying table semantics against MainErp rules.
- Keep separate wrappers:
  - `/MainErp/JournalEntries`
  - `/Pos/JournalEntries`
- Use separate layouts and permissions.
- Keep shared validation/posting in a shared service/repository layer.
- Do not make MainErp call POS routes.

### Opening Balance

Located implementations/fragments:

- Account opening balance maintenance: `Controllers/AccountSettings/ChartOfAccountOBController.cs`, `Views/ChartOfAccountOB/Index.cshtml`
- Account opening balance report: `Reporting/Reports/AccountsOpeningBalance_Report.*`, `Views/Report/AccountsOpeningBalance.cshtml`
- MainErp accounting model flag: `Areas/MainErp/Models/Accounting/VoucherBatch.cs`
- MainErp posting service: `Areas/MainErp/Services/Accounting/VoucherPostingService.cs`
- MainErp voucher repository: `Areas/MainErp/Repositories/Accounting/VoucherRepository.cs`
- LC opening balance voucher logic: `Areas/MainErp/Repositories/LC/LcWriteRepository.cs`, `Areas/MainErp/Repositories/LC/LcReadRepository.cs`
- Master data import planning fields: `Areas/MainErp/ViewModels/MasterDataImport/MasterDataImportViewModels.cs`
- Bank/box opening balances: `Areas/MainErp/Repositories/FinancialAdministration/FinancialAdministrationRepository.cs`

Current route/menu status:

- No confirmed MainErp route for `/MainErp/JournalEntries/OpeningBalance`.
- No MainErp sidebar item for `قيد افتتاحي`.
- No POS opening balance journal route.

Current logic status:

- Account opening balances exist as account master data fields (`ObDebit`, `ObCredit`) in Original Web.
- LC opening balance voucher logic posts to `Notes1` and `DOUBLE_ENTREY_VOUCHERS1`, with opening note type `101`.
- General manual POS journal posts to `Notes` and `DOUBLE_ENTREY_VOUCHERS`, with manual note type `57`.
- `VoucherBatch.OpeningBalanceMode` exists and posts to `DOUBLE_ENTREY_VOUCHERS1`, but no full screen is wired around it.

VB6 source trace status:

- Not completed in this audit-only pass.
- Before implementation, Main Original VB6 must be traced from `F:\Source Code\SatriahMain`, especially `FrmOpeningBalance.frm`, related modules, note type selection, balancing account behavior, branch/date/serial handling, and whether unbalanced opening balances are allowed.
- Kishny must not be used as the authority for MainErp opening-balance business rules.

Required stabilization direction:

- Add Opening Balance as a mode of the shared journal engine, not as a duplicate screen.
- Preferred UX:
  - `/MainErp/JournalEntries` for normal journals.
  - `/MainErp/JournalEntries/OpeningBalance` for `قيد افتتاحي`.
  - Shared UI engine with mode-specific title, badge, filters, defaults, and validation.
- POS should be read-only or hidden for opening balances unless explicitly approved.

## Module Audit Matrix

| Module | Located Implementation | Ownership Classification | Duplication / Source Candidate | UI/UX Consistency | Permissions | Save/Edit/Delete & DB Risk | Reports / Printing | Priority |
|---|---|---|---|---|---|---|---|---|
| Branches | `Areas/MainErp/Controllers/BranchesController.cs`, service/repository/viewmodel/view/script/css; POS menu links to MainErp Branches; Original Web has older branch/system settings screens | MainErp wrapper with legacy DB tables; POS consumes via shared link | MainErp should be source wrapper; consider shared read model if POS needs embedded branch UX | MainErp migrated UI appears structured; POS currently leaves its area for this screen | MainErp legacy screen permission service, likely `FrmBranchesData`; POS visibility via dashboard link | Branch master data is sensitive; keep validation/service layer, avoid view logic | Branch reports not confirmed in this pass | Medium |
| Treasuries / Cashboxes / Banks | `Areas/MainErp/Controllers/FinancialAdministrationController.cs`, `FinancialAdministrationService`, `FinancialAdministrationRepository`, view/script/css; Original Web `Bank`, `BankAccount`, `CashBox`, `CashBoxBalance` | MainErp migrated module plus Original Web legacy | MainErp likely target source, but compare with Original Web before deleting/retiring anything | Migrated MainErp UI present; opening balance fields included for banks/boxes | Screen names include `FrmBanksData`, `FrmBoxesData` | Writes bank/box master and opening balance fields; financial risk high | Bank/cashbox reports likely in reporting area, needs report map verification | High |
| Items | `Areas/MainErp/Controllers/ItemsController.cs`, `ItemsService`, `ItemsRepository`, view/script/css; POS links to MainErp Items; Original Web `ItemController`, item groups/units/categories | MainErp target wrapper; Original Web source legacy; POS consumer | Duplicated old/new implementations. MainErp should become source after parity audit | MainErp migrated UI present; must verify toolbar/grid/dialog parity | MainErp uses `FrmItems`; POS visibility via menu/context | Item master affects stock and POS sales; save/delete must protect referenced items | Item reports exist in reporting but need mapping | High |
| Employees | MainErp `EmployeePayrollController`; POS `EmployeePayrollController`; shared `Common/EmployeePayroll`; Original Web HR employee screens | Shared core with separate MainErp/POS wrappers | Good shared candidate already present | Need compare layouts and toolbar consistency across wrappers | Permissions differ by area; needs server-side parity check | Payroll/employee writes should remain in repository/service, not views | Payroll reports likely separate | Medium |
| Chart of Accounts | `Areas/MainErp/Controllers/AccountChartsController.cs`, service/repository/view/script/css; Original Web `ChartOfAccountsController`; opening balance controller | MainErp migrated target plus Original Web legacy | MainErp should be target, but opening balance logic still split | Tree/search UX must be consistent with journal account selector | MainErp screen permission expected; server enforcement requires review | High accounting risk: account branch/user links, currency, cost center, ObDebit/ObCredit | Accounts opening balance report exists | Critical |
| Warehouses | MainErp `StoreDataController`; POS `StoresController`; shared `Common/StoreData`; Original Web `WarehouseController`, `WarehouseOBController` | Shared store data core with wrappers | Good shared pattern. Original Web still separate | Needs visual consistency between MainErp and POS wrappers | Permissions are area-specific and need mapping | Warehouse master affects inventory; delete/update safeguards required | Warehouse/stock reports likely exist | High |
| Projects | MainErp `ProjectsController`, `ProjectRepository`, `ProjectExtracts`; Original Web project management controllers/views | MainErp migrated target plus Original Web legacy | Duplication exists; MainErp should be compared with Original Web source | MainErp views present; extract workflow needs UX parity | Needs screen permission mapping verification | Project billing/extract tables are financially relevant | Project reports not fully mapped | Medium |
| System Manager | MainErp Dashboard, DatabaseMigration, Options, Users, Permissions; POS dashboard/system health/error log/sql updates/legacy admin | Area-specific system management | Do not merge wholesale; share only safe diagnostics/models | POS dashboard and MainErp admin screens have different purposes | Admin-only paths must stay server-protected | SQL update/admin screens are dangerous; require strong guards | Operational reports/admin logs | Critical |
| Permissions | MainErp `PermissionsController`; POS `PosPermissionsController`; Original Web user/role privilege controllers | Separate permission systems | Needs unification map, not immediate merge | UI matrix consistency needed | MainErp appears read-only matrix; POS writes temp permissions | Missing or mismatched server enforcement is a critical risk | Permission reports not applicable | Critical |
| Users | MainErp `UsersController` with `Common/Users`; POS `PosLegacyAdminController.Users`; Original Web ERP/user controllers | Shared user read/write partly exists; POS admin separate | Need define whether POS users are same identity source or operational subset | MainErp and POS user admin UX differ | Admin checks exist; permission names need mapping | User writes affect access control; high risk | Not applicable | High |
| Settings | MainErp `OptionsController`, service/repository/view/script/css; POS links to MainErp Options; Original Web system setting screens | MainErp target wrapper, POS consumer | MainErp likely source after Original Web parity check | Migrated settings UI present | `FRMOptions`/legacy permission expected | Global settings can alter posting/stock behavior; require audit trail and validation | Not applicable | High |
| Purchases | MainErp `PurchasesController` and view appear review/placeholder; POS `PurchaseInvoiceController`, view/script/sql; Original Web warehouse purchase controllers/views | POS operational for POS purchase invoice; MainErp not complete | Do not classify MainErp as migrated-complete. Original Web/Main Original remain source for ERP purchasing | MainErp has disabled workflow; POS operational UX | POS permissions separate; MainErp permission needs definition | Purchases affect stock/accounting; high risk before enabling save | Purchase reports exist in legacy/reporting | Critical |
| Journal Vouchers | POS full implementation; MainErp read-only implementation; Original Web EF implementation; reports | Not shared yet | POS is working source for current manual journal mechanics, but MainErp business source must be verified | POS UX is complete; MainErp is browse/detail only | POS permission keys exist; MainErp needs journal create/edit permissions | Posts to `Notes`/`DOUBLE_ENTREY_VOUCHERS`; high accounting risk | Journal reports exist | Critical |
| Opening Balance | Original Web account OB; MainErp voucher opening flags; LC opening vouchers; bank/box opening fields; report | Fragmented | Must trace Main Original VB6 before implementation | No unified opening-balance journal UI | Permissions not defined for opening-balance journal | Very high accounting risk; note type/table behavior unresolved | Accounts opening balance report exists | Critical |
| Stock Transfer | MainErp `StockTransfersController` placeholder/review; POS `StockTransferController`, view/script/sql; Original Web stock transfer voucher controller/views | POS operational; MainErp not complete | Do not duplicate POS blindly into MainErp; source rules must be verified | POS operational; MainErp disabled/review state | POS permissions separate; MainErp needs definition | Stock movement and serial logic high risk | Stock transfer reports exist in legacy/reporting | Critical |
| Custody Replenishment | POS `PaymentsController`, `PosSqlRepository`, custody funding/refund SQL; MainErp payment voucher services/repos; POS financial intelligence/custody reports | Mostly POS operational plus MainErp payment infrastructure | Need clarify shared payment voucher core boundary | POS payment UX operational | POS permission keys; MainErp payment permissions need mapping | Cash/custody posting high risk; transaction safety required | POS custody/payment reports exist | High |
| Kishny POS Sales | POS `PosTransactionController`, view/script/models/reports/sql; KYC/closing/cancel diagnostics | POS source implementation | Preserve as operational source. Do not rebuild for MainErp | Rich POS operational UI; keep layout separate | POS session and operational permissions | Affects sales, stock, cash, tax/card/receipt; highest operational risk | Receipt, closing, sales reports exist | Critical |

## Routes And Menus Observed

MainErp:

- `Areas/MainErp/MainErpAreaRegistration.cs`
- Default route: `/MainErp/{controller}/{action}/{id}`
- Journal list route: `/MainErp/JournalEntries`
- Journal detail routes:
  - `/MainErp/JournalEntries/DetailsByNote/{noteId}`
  - `/MainErp/JournalEntries/DetailsByVoucher/{voucherId}`
- Sidebar includes `اليومية العامة`.
- Sidebar does not currently include `قيد افتتاحي`.

POS:

- `Areas/Pos/PosAreaRegistration.cs`
- Default route: `/Pos/{controller}/{action}/{id}`
- Journal route from POS sidebar: `/Pos/JournalEntries/Index`
- POS sidebar also links some screens to MainErp with `?fromPos=1&host=pos`.
- POS journal visibility is controlled by POS user context/permissions.

Original Web:

- Root default route remains available for legacy controllers.
- Old journal route: `/JournalEntries`
- Old account opening balance route: `/ChartOfAccountOB`

Route boundary concern:

- POS menu links directly into MainErp for some shared screens. This may be acceptable as a temporary shared-screen bridge, but the target professional pattern should be an explicit wrapper per area when POS needs a distinct layout, session, permission, or context.

## UI/UX Review Themes

Consistent patterns already visible:

- MainErp migrated screens generally use dedicated views plus scripts/styles.
- POS operational screens use POS layout and POS-specific scripts.
- Some Common modules are wrapped by both areas.

Inconsistent or risky patterns:

- POS journal is a full operational app screen; MainErp journal is read-only.
- Some MainErp modules look like placeholders with disabled actions, especially Purchases and Stock Transfers.
- POS links into MainErp for shared screens, which can confuse user expectations when layouts and permissions differ.
- Account selector/tree behavior should be shared for journals, opening balances, and chart-of-account dependent workflows.
- Arabic/RTL labels should be reviewed in browser after any UI unification because terminal output can show mojibake even when files are valid.

## Permission Review Themes

Observed systems:

- MainErp uses legacy screen permissions through services such as `LegacyScreenPermissionService`.
- POS uses `PosUserContext` and POS-specific keys such as `CanViewJournalEntry`, `CanCreateJournalEntry`, `CanEditJournalEntry`, and `CanDeleteJournalEntry`.
- Original Web has older user/role privilege controllers.

Required direction:

- Build a permission map per shared module.
- Enforce visibility in menus and server-side controller actions.
- Never rely on hidden buttons only.
- Opening balance journal should have separate permission names from normal journal save/edit/delete.
- POS should not expose opening balance creation/editing unless explicitly approved.

## Database And SQL Server 2012 Review Themes

High-risk tables/procedures observed:

- `Notes`
- `Notes1`
- `DOUBLE_ENTREY_VOUCHERS`
- `DOUBLE_ENTREY_VOUCHERS1`
- `ACCOUNTS`
- `TblAccountBranch`
- `TblAccountUser`
- `BanksData`
- `tblBoxesData`
- Warehouse/store/stock transfer tables and POS save procedures
- POS sales transaction SQL scripts under `Areas/Pos/Sql`

Required direction:

- Keep SQL Server 2012-compatible SQL.
- Avoid changing stored procedures during the stabilization audit.
- Before journal/opening balance implementation, document exact note type and target table rules from Main Original VB6.
- Confirm transaction boundaries for every save that posts accounting or stock effects.

## Prioritized Action Plan

### Critical Fixes

1. Journal unification:
   - Create a shared journal core service/repository/model.
   - Keep POS and MainErp wrappers separate.
   - Move common validation/posting/search behavior into shared code.
   - Preserve the working POS journal UX while enabling MainErp operational entry.

2. Opening balance journal:
   - Trace Main Original VB6 first.
   - Implement as `OpeningBalance` mode in the shared journal engine.
   - Add MainErp route and menu item only after rules are confirmed.
   - Restrict POS to hidden or read-only opening balance visibility unless approved.

3. Mark incomplete modules clearly:
   - MainErp Purchases and Stock Transfers are not fully operational migrations yet.
   - Do not expose disabled placeholder screens as complete workflows.

4. Permission hardening:
   - Build permission matrix across Original Web, MainErp, POS, and shared engines.
   - Add server-side enforcement before enabling any write workflow.

### UI Unification Fixes

- Standardize migrated screen toolbar actions: New, Save, Delete, Print, Search, Clear, Close.
- Standardize RTL grid spacing, filters, empty states, validation messages, and modal/dialog behavior.
- Use one account tree/search component for chart-of-account selection flows.
- Keep area layouts separate even when shared engines are used.

### Shared-Module Refactoring

- Use `Common` as the preferred destination for truly shared services/models.
- Good existing examples: `Common/StoreData`, `Common/Users`, `Common/EmployeePayroll`, `Common/DiscountNotifications`.
- Candidate next shared modules:
  - Journal entries
  - Account lookup/tree selector
  - Voucher validation/posting primitives
  - Shared permission descriptors, not necessarily one permission store

### Permission Fixes

- Define permission names for:
  - Journal view/create/edit/delete/print.
  - Opening balance view/create/edit/delete/approve/print.
  - Stock transfer create/edit/post/delete.
  - Purchase invoice create/edit/post/delete.
- Add deny-by-default behavior for high-risk write actions.
- Document POS-specific restrictions separately.

### Performance Fixes

- Review journal search limits and indexes on `Notes`, `DOUBLE_ENTREY_VOUCHERS`, and account tables.
- Keep default list pages bounded.
- Avoid loading full account trees repeatedly without caching or filtering.
- Review POS sales and stock transfer SQL scripts for expensive scans after functional stabilization.

### Phase 2 Dependencies

- Main Original VB6 trace for opening balances and ERP accounting flows.
- Original Web parity audit for Purchases, Stock Transfer, Chart of Accounts, and Settings.
- Browser smoke testing after route changes are implemented in a later phase.
- Database-backed QA on `Eng` after write paths are implemented.

## QA Status For This Audit

Performed:

- Static repository inspection.
- Route registration inspection.
- Menu/sidebar inspection.
- Controller/view/repository/script discovery.
- High-risk accounting and stock flow classification.

Not performed by design:

- Build.
- Browser smoke.
- Database write tests.
- SQL changes.
- Route changes.
- Application code changes.

Reason: the requested phase is audit only and explicitly forbids rebuilding screens, modifying SQL, changing routing, or changing application code.

## Remaining Risks

- Main Original VB6 opening-balance logic is still the blocker for correct `قيد افتتاحي` implementation.
- POS journal behavior is working, but it should not automatically become MainErp accounting authority without VB6/MainErp validation.
- MainErp journal currently creates user confusion because the menu item exists but the screen is read-only.
- POS direct links into MainErp may be acceptable temporarily, but they must be reviewed against separate layout/context/permission requirements.
- Placeholder MainErp modules can be mistaken for complete migrations.

## Documentation Files Created In This Audit

- `Docs/AI_CONTEXT/MIGRATED_CORE_STABILIZATION_AUDIT.md`
- `Docs/AI_CONTEXT/CURRENT_PRIORITY.md`
