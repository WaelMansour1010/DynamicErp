# Phase 10 - Web Smoke Test
Date: 2026-05-20
ReadyToTest DB: $db

## Login
| Step | Result |
|---|---|
| Select DB via /DevStart | Pass |
| Login as ErpAdmin | Pass |
| Auth loop / HTTP 500 | None observed |

Note: The login response redirects to /PointOfSale/PosLogin, but authenticated property pages opened successfully. This is a UX/navigation note, not a blocker for testing property pages.

## Read-Only Page Checks
| Screen | URL | Result |
|---|---|---|
| Properties | /Property?searchWord=ADNAN-P- | Pass, HTTP 200 |
| Units | /PropertyUnit?searchWord=ADNAN | Pass, HTTP 200 |
| Contracts list | /PropertyContract?searchWord=ADNAN-C-1096 | Pass, HTTP 200 |
| Contract ADNAN-C-1096 | /PropertyContract/AddEdit/1227 | Pass, HTTP 200 |
| Advance-payment sample ADNAN-C-2131 | /PropertyContract/AddEdit/1478 | Pass, HTTP 200 |
| High-value sample ADNAN-C-1748 | /PropertyContract/AddEdit/1229 | Pass, HTTP 200 |
| Receipt screen read-only | /CashReceiptVoucher/AddEdit?cid=1227 | Pass, HTTP 200 |
| Contract batches API | /CashReceiptVoucher/GetContractBatches?PropertyContractId=1227&vid=0 | Pass, 10 batches returned |

## Test Data Policy
No receipt, issue, or termination was saved during Phase10 smoke test. The test was read-only.

Raw output: Phase10_WebSmokeTest_raw.json.
