# POS General Performance Audit - 2026-05-14

Scope: `Areas\Pos` only.

This audit intentionally stops the closing-summary work and looks for general POS bottlenecks across page open, AJAX endpoints, searches, lookups, dashboards, and invoice loading. No business logic or invoice/accounting save logic was changed.

## Baseline Observations

- Local live schema checked on `Cash` through `KishnyCashConnection`.
- Largest row-volume tables observed:
  - `DOUBLE_ENTREY_VOUCHERS`: 7,227,939 rows
  - `Transaction_Details`: 2,014,078 rows
  - `Notes`: 1,336,415 rows
  - `Transactions`: 1,248,549 rows
  - `TblCusCsh`: 148,294 rows
  - `TBLClosePos`: 53,631 rows
  - `TblSalesPayment`: 14,537 rows
  - Lookup tables are small in this database: `TblUsers` 139, `TblStore` 115, `TblItems` 15, `TblCustemers` 31.
- Existing POS performance work already exists:
  - `dbo.usp_POS_SalesInvoices_Search` in `Areas\Pos\Sql\77_POS_SalesAndKycSearch_Performance.sql`
  - `dbo.usp_POS_KycCustomers_Search` in `Areas\Pos\Sql\77_POS_SalesAndKycSearch_Performance.sql`
  - `dbo.usp_POS_Payments_Search` in `Areas\Pos\Sql\34_POS_PerformanceStoredProcedures.sql`
  - dashboard daily snapshot scripts in `Areas\Pos\Sql\40_POS_Dashboard_DailySnapshots.sql`
  - several report/search indexes already exist on `Transactions`, `Transaction_Details`, `TblCusCsh`, `TBLClosePos`, `Notes`, and `DOUBLE_ENTREY_VOUCHERS`.

## Audit Table

| Screen/Route | Controller/Action | JS/View | Repository/SQL | Loads on open? | Estimated row-volume risk | Current limit/paging/filter | Problem found | Recommended fix | Risk level | Quick win or deeper change |
|---|---|---|---|---|---|---|---|---|---|---|
| POS transaction shell `/Pos/PosDashboard/Sales` -> iframe `/Pos/PosTransaction/Index` | `PosDashboardController.Sales`, `PosTransactionController.Index` | `Areas\Pos\Views\PosDashboard\Index.cshtml`, `Areas\Pos\Views\PosTransaction\Index.cshtml`, `Areas\Pos\Scripts\pos-transaction.js` | page context from session | Yes | Medium | no heavy SQL in `Index`; JS starts extra calls | Screen open still triggers employee balance and may initialize invoice entry depending mode | Keep HTML open light; keep expensive data behind explicit actions | Medium | Quick win: timing added for read endpoints |
| Employee/box balances | `PosTransactionController.GetEmployeeBalances` | `pos-transaction.js:loadEmployeeBalances` | `PosSqlRepository.GetEmployeeBalances` | Yes | Medium to high if account voucher history is large | no paging; returns one balance payload | Runs automatically on every POS transaction open | Keep, but measure production duration; consider caching per user/box for 30-60 seconds | Medium | Quick win applied: duration logging |
| Today invoices side panel | `PosTransactionController.GetTodayInvoices` | `pos-transaction.js:loadTodayInvoices`, `todayInvoiceSearch` | `PosSqlRepository.GetTodayInvoicesFast` -> `dbo.usp_POS_SalesInvoices_Search` | No initial auto-load currently; triggered by search/type/date controls and after save/cancel/delete | High because `Transactions` and optional `Transaction_Details.ItemSerial` search are large | `TOP 50`, today default unless date range enabled, term min 2 in JS | Search supports broad `%name%` and serial fallback; admin all-branch today can still scan many rows | Raise user-facing search minimum to 3 for free-text, keep exact invoice/token paths; add date range hard cap for all-branch admin searches | High | Deeper change |
| Sales invoice index | same endpoint as today invoices | `loadSalesIndexInvoices` | `dbo.usp_POS_SalesInvoices_Search` | Yes when `data-sales-index-first=true`; otherwise user-triggered | High | Today default dates; `TOP 50`; branch filter optional for admin | Search allows all-branch today and user can choose wider ranges; repeated loads on filters | Require branch or short date range for admin; disable search button while request is active | High | Quick win candidate |
| KYC modal search | `PosTransactionController.SearchKeshniCardCustomers` | `kycSearchTerm`, KYC search button | `dbo.usp_POS_KycCustomers_Search` over `TblCusCsh` | No, user-triggered | High in production if `TblCusCsh` grows; local 148k | `TOP 50`; controller allows min 2; stored procedure broad name search only at min 3 | Two-character numeric/prefix search can still touch many phone/card/national fields; non-admin fallback checks other branch with older inline LIKE query | Raise controller/JS min to 3 for manual KYC search; convert other-branch hint to stored procedure style or exact/prefix only | High | Deeper change; logging added now |
| Unused KYC lookup while typing | `PosTransactionController.LookupUnusedKeshniCardCustomer` | `scheduleUnusedKycLookup` | `dbo.usp_POS_KycCustomers_Search` with `@unusedOnly=1` | While typing card/customer fields | High | `TOP 20`; min 2 | Repeated debounced calls can overlap user typing and include NOT EXISTS against `Transactions` | Require exact phone/card/national length for auto lookup, or min 3 plus cancellation; keep manual search available | High | Deeper change |
| KYC attachments | KYC customer apply -> attachment endpoint | `loadKycAttachments` | `GetKeshniCardAttachments` | On selecting saved customer | Low/Medium | customer id required | No full scan found; risk is file IO/large blobs if many attachments per customer | Keep by customer id; add row limit if production has many attachments | Low | Quick win candidate |
| Available Keshni card/token picker | `PosTransactionController.SearchAvailableKeshniCards` | `scheduleAvailableKycCardsSearch` | `PosSqlRepository.SearchAvailableKeshniCardTokens` -> `dbo.usp_POS_SearchAvailableKeshniCards` | While typing card token | High because availability derives from `Transaction_Details` and `Transactions` | `take` capped to 50; controller uses 30; store required | Empty/short term may still ask the DB for first available cards in a store | Keep store required; require 3 characters before broad token search unless explicitly listing first available cards is required | High | Deeper change; logging added now |
| Commission bootstrap | `PosTransactionController.CommissionBootstrap` | `preloadCommissionSettings` during invoice entry initialization | `GetPrimaryServiceItems`, `GetCommissionBootstrapData` | Yes when entry screen opens | Low in local DB, can be medium if item/range tables grow | logs duration already | Multiple service queries are made on entry init | Cache commission bootstrap per app/session for short TTL | Medium | Deeper change |
| Item autocomplete in sale | `PosTransactionController.GetItems` | `.item-name` input search | `PosSqlRepository.GetItems` | User typing | Low locally (`TblItems` 15), potentially high in real item catalog | needs exact method review before changing | Endpoint has no controller guard; risk if repo allows empty term | Confirm repo `TOP`/term guard; add min 2/3 and `TOP 20` if missing | Medium | Quick win candidate |
| Dashboard summary | `PosDashboardController.Summary` | `loadAdminDashboard(false/true)` | `dbo.usp_POS_Dashboard_Summary`, snapshot procedures | Basic dashboard can load after shell, advanced via button | High | Uses snapshot only for completed past daily snapshots; live raw query otherwise | Current/live ranges still aggregate raw `Transactions`, plus KYC activation EXISTS against `TblCusCsh` and `Transactions` | Use snapshots by default for past days; restrict advanced live dashboard to short ranges; schedule snapshot generation | High | Deeper change |
| Payments screen | `PaymentsController.Index`, `Lookups`, `Search` | `Areas\Pos\Views\Payments\Index.cshtml` | `GetPosPaymentRelationshipLookups`, `dbo.usp_POS_Payments_Search` over `Notes` | Branch list on open; lookups/balances after field changes | High because `Notes` has 1.3M rows | search `TOP 100`; dates optional in stored procedure | Payment search can run without date filters; account/balance calls repeat on field changes | Default dates to today/last 7 days and require date or specific search text; debounce balance calls | High | Quick win candidate |
| Purchase invoice index/search | `PurchaseInvoiceController.Index/Search/Lookup` | `purchase-invoice.js` | `SearchPurchaseInvoices`, `SearchPurchaseSuppliers`, `GetPurchaseItems` | Lookups on open; search by button | Medium/High because purchase rows live in `Transactions` | `TOP 100` for index, `TOP 20` lookups; dates optional | Search can run with no date range; supplier/item lookup accepts empty term and returns first 20 | Keep defaults today/last 7 days; require date or invoice/supplier term for search | Medium | Quick win candidate |
| Stock transfer index/search/serial picker | `StockTransferController.Index/Search/AvailableSerials` | `stock-transfer.js` | `SearchStockTransfers`, `SearchStockTransferAvailableSerials` | Lookups on open; serial picker by user | High for serial availability due `Transaction_Details` aggregation | Search `TOP 100`; serial picker paged, page size max 500 | Serial availability scans historical stock movement up to transfer date for one item/store | Require item+store already done; add from-date/materialized stock balance later | High | Deeper change |
| POS permissions | `PosPermissionsController.Index` | `Areas\Pos\Views\PosPermissions\Index.cshtml` | `GetPosPermissionUsers`, permission item builder | Yes | Low locally (`TblUsers` 139), medium if many users | loads all POS users | Full user list on open is acceptable locally but can grow | Lazy user search if users become large | Low | Deeper change not urgent |
| Report lookup shell | `PosReportsController.Lookup`, HTML reports lookup | `PosReports/Index.cshtml`, `HtmlReports/Index.cshtml` | branches/stores/users/service types | Lazy after Phase 2 | Medium | dropdowns lazy; serial report requires store or 3 chars | Current protection looks good | Keep validation; reuse same pattern elsewhere | Low | Already improved |

## Top 10 Likely Causes Of General Slowness

1. POS transaction screen read endpoints run around screen open and common actions: employee balances, commission bootstrap, invoice refreshes, and context lookup hydration.
2. Sales invoice search still allows admin/all-branch today or wider date ranges against `Transactions` and sometimes `Transaction_Details`.
3. KYC search and auto-lookup over `TblCusCsh` can be frequent while typing and has a second other-branch fallback query.
4. Available Keshni card/token search calculates current stock availability from raw transaction details.
5. Dashboard live summary aggregates raw sales data and KYC activation checks when snapshots are missing or advanced mode is requested.
6. Payments search over `Notes` can execute without required date filters and with broad text predicates.
7. Stock transfer available serial picker aggregates historical movement by serial.
8. Purchase and stock index searches have optional dates; `TOP` limits output but the optimizer may still scan/filter large transaction ranges.
9. Repeated AJAX after save/cancel/delete reloads today invoices and sometimes sales index, causing duplicate read load after a write.
10. The database already has many nonclustered indexes on write-heavy POS tables, so write latency may be affected by index maintenance as much as by read queries.

## Safe Logging Added

Lightweight slow-query timing was added in `PosTransactionController` using the existing `PosPerformanceLogger` threshold of 300 ms. It logs only action/query name, elapsed ms, row count, user id, and branch id.

Logged endpoints:

- `PosTransaction.GetEmployeeBalances`
- `PosTransaction.GetTodayInvoices`
- `PosTransaction.SearchKeshniCardCustomers`
- `PosTransaction.LookupUnusedKeshniCardCustomer`
- `PosTransaction.SearchAvailableKeshniCards`

No KYC values, token values, national IDs, phone numbers, passwords, or search terms are logged.

## Phase 2 Recommendation

Start with read-side protections before adding indexes:

1. Add required-filter rules to high-risk searches: admin all-branch must choose a branch or a short date range.
2. Raise manual search minimum from 2 to 3 characters for broad invoice/KYC/search boxes, while preserving exact invoice/token/phone flows.
3. Disable duplicate search clicks on sales index, payments, purchase, and stock transfer screens.
4. Measure production logs for one business day, then choose only the top 3 slow endpoints for SQL tuning.
5. Review actual execution plans before any new index. Given the existing index count, every new index must prove read benefit greater than write overhead.
