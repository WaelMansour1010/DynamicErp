# MainErp Eng Runtime QA - 2026-05-14

Database used: `Eng`  
Build: PASS (`MyERP -> F:\Source Code\DynamicErp\bin\MyERP.dll`)  
Browser QA: Playwright against IIS Express `http://localhost:51234`  
Console status: PASS, final route pass had `0` JavaScript console errors.

## Runtime Routes Tested

| Route | HTTP | Console | Result |
|---|---:|---:|---|
| `/MainErp/Customers` | 200 | 0 | PASS |
| `/MainErp/FinancialAdministration?scope=banks` | 200 | 0 | PASS |
| `/MainErp/FinancialAdministration?scope=boxes` | 200 | 0 | PASS |
| `/MainErp/StoreData` | 200 | 0 | PASS |
| `/MainErp/Items` | 200 | 0 | PASS |
| `/MainErp/Stocktaking` | 200 | 0 | PASS |
| `/MainErp/DefinCompItem` | 200 | 0 | PASS |
| `/MainErp/Cashing` | 200 | 0 | PASS |
| `/MainErp/Payments` | 200 | 0 | PASS |
| `/MainErp/Users` | 200 | 0 | PASS |
| `/MainErp/Permissions` | 200 | 0 | PASS |
| `/MainErp/ProjectExtracts` | 200 | 0 | PASS |
| `/MainErp/EmployeePayroll/Employees` | 200 | 0 | PASS |
| `/MainErp/EmployeePayroll/SalaryRun` | 200 | 0 | PASS |
| `/MainErp/EmployeePayroll/MedicalInsurance` | 200 | 0 | PASS |
| `/MainErp/EmployeePayroll/MedicalInsuranceReports` | 200 | 0 | PASS |
| `/MainErp/LegacyHrFinance/Advances` | 200 | 0 | PASS |
| `/MainErp/LegacyHrFinance/LeaveEntitlements` | 200 | 0 | PASS |
| `/MainErp/LegacyHrFinance/CompensationAdjustments` | 200 | 0 | PASS |
| `/MainErp/LC` | 200 | 0 | PASS |
| `/MainErp/Options` | 200 | 0 | PASS |

## Data Validation

| Object | Count in `Eng` |
|---|---:|
| `TblCustemers` | 2688 |
| `TblItems` | 40012 |
| `TblStore` | 12 |
| `BanksData` | 23 |
| `tblBoxesData` | 83 |
| `TblUsers` | 34 |
| `ScreenJuncUser` | 16038 |
| `MedicalInsuranceProviders` | 0 |
| `MedicalInsurancePlans` | 0 |

## Save/Search Status

- Customers, banks, boxes, stores, items, users, permissions, receipts/payments, project extracts, and payroll routes opened and rendered searchable/list views.
- Receipt/payment route failures were fixed by applying `04_MainErp_PaymentCashing_ReadProcedures.sql`.
- Medical-insurance provider/plan JSON failures were fixed by applying `06_EmployeePayroll_MedicalInsurance.sql`.
- No destructive delete was run.
- No production payroll or accounting posting was enabled.

## Screenshots

- `Areas/MainErp/Docs/runtime-main-original-vb6-mainerp-qa.png`

## Known Blockers / Next Steps

- Medical-insurance setup tables now exist in `Eng` but contain no provider/plan rows yet.
- Live edit/save mutation checks should be done with named client-approved sample records before client trial data is changed.
- Older non-menu views `Purchases` and `StockTransfers` still carry historical Kishny/POS reuse notes and should be separated in a later pass if they become part of MainErp delivery.
