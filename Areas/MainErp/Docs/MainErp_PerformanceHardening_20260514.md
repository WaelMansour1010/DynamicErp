# MainErp Performance Hardening - 2026-05-14

Database: `Eng`  
Scope: MainErp route load, large search surfaces, payroll preview/replay payloads, inventory item lookup, report stability.

## Build and Runtime Status

- Build: pass after performance hardening changes.
- Browser smoke: pass for `/MainErp/Stocktaking` and `/MainErp/EmployeePayroll/SalaryRun`.
- Browser console: 0 errors in focused smoke.
- Authentication: measured as admin against `Eng`.

## Measured Route and API Timings

| Route / API | Status | Time | Payload |
|---|---:|---:|---:|
| `/MainErp/Customers` | 200 | 527 ms | 41.2 KB |
| `/MainErp/Items` | 200 | 1,382 ms | 79.5 KB |
| `/MainErp/Stocktaking` | 200 | 534 ms | 40.1 KB |
| `/MainErp/EmployeePayroll/SalaryRun` | 200 | 493 ms | 27.0 KB |
| `/MainErp/ProjectExtracts` | 200 | 520 ms | 32.5 KB |
| `/MainErp/LC` | 200 | 749 ms | 138.6 KB |
| `/MainErp/AccountingReports` | 200 | 377 ms | 13.1 KB |
| `/MainErp/Stocktaking/LookupItems?term=oil` | 200 | 134 ms | 6.2 KB |
| `/MainErp/EmployeePayroll/PreviewSalaryRun?Year=2026&Month=5&IncludeSavedDrafts=true` | 200 | 6,348 ms | 105.8 KB |
| `/MainErp/EmployeePayroll/PayrollAccountingReplay?Year=2026&Month=5&IncludeSavedDrafts=true&IncludeLineDetails=false` | 200 | 6,780 ms | 459.1 KB |

Before compacting salary preview, the same preview endpoint returned about 5,774.8 KB and took about 9,814 ms. Payload is now about 105.8 KB, a reduction of roughly 98%.

## Optimizations Applied

- Stocktaking item selection no longer embeds a large item `<select>` into every grid row.
- Added `/MainErp/Stocktaking/LookupItems` with server-side `TOP` limiting and term filtering by code, name, and barcode.
- Stocktaking initial item seed reduced to a small starter list; operator search is async and throttled client-side.
- Payroll preview now returns a compact DTO for browser rendering and keeps detailed component explainability on the existing drill-down endpoint.
- Payroll preview caps displayed rows and journal rows while preserving totals and full row counts.
- Payroll replay trims comparison and line collections when `IncludeLineDetails=false`.
- Payroll table rendering now builds HTML in batches instead of repeated `insertAdjacentHTML` per row.

## Remaining Bottlenecks

- Salary preview still spends about 6.3 seconds server-side because it executes the full Main Original-compatible payroll reconstruction and legacy functions for the period.
- Payroll replay still spends about 6.8 seconds because it rebuilds accounting replay and legacy trace comparisons.
- LC page payload is the largest HTML route at about 138.6 KB due to its detailed operational form and lookup payloads.
- Items route is acceptable but still heavier than Customers because it loads group/tree and item edit metadata.

## Recommended Future Indexing

Validate existing indexes before applying. Recommended candidates:

- `TblItems(ItemCode) INCLUDE (ItemID, ItemName, barCodeNO, IsArchive, ItemType)`
- `TblItems(ItemName) INCLUDE (ItemID, ItemCode, barCodeNO, IsArchive, ItemType)`
- `TblItems(barCodeNO) INCLUDE (ItemID, ItemCode, ItemName)`
- `TblCustemers(Type, BranchId, CusName) INCLUDE (CusID, Account_Code, Fullcode)`
- `Transactions(Transaction_Type, StoreID, BranchId, Transaction_ID DESC) INCLUDE (Transaction_Serial, Transaction_Date, Nots, Nots2)`
- `Transaction_Details(Transaction_ID) INCLUDE (Item_ID, UnitID, ShowQty, Quantity, showPrice, Price)`
- `emp_salary(sgn, emp_id) INCLUDE (payed, EmpTotalNet, total1, total2, TotalAdvance, TotalDiscount, ToalInsurance, project_id)`
- `DOUBLE_ENTREY_VOUCHERS(Notes_ID) INCLUDE (Account_Code, depet_value, credit_value, branch_id)`
- `notes(NoteType, salary, branch_no) INCLUDE (NoteID, NoteDate, Note_Value)`
- `TblLC(TblLCID DESC) INCLUDE (LCNO, Name, BankId, VendorId, BranchID, Value)`

## Files Changed

- `Areas/MainErp/Controllers/EmployeePayrollController.cs`
- `Areas/MainErp/Controllers/StocktakingController.cs`
- `Areas/MainErp/Repositories/Stocktaking/StocktakingRepository.cs`
- `Areas/MainErp/Services/Stocktaking/StocktakingService.cs`
- `Areas/MainErp/Scripts/employee-payroll.js`
- `Areas/MainErp/Scripts/stocktaking.js`
- `Areas/MainErp/Views/Stocktaking/Index.cshtml`
- `Common/EmployeePayroll/EmployeePayrollModels.cs`
- `Common/EmployeePayroll/EmployeePayrollRepository.cs`

## Readiness

MainErp is noticeably safer for heavy browser usage. The largest immediate browser payload problem, salary preview, is compacted. Remaining work is mostly SQL/runtime calculation optimization, not front-end payload pressure.
