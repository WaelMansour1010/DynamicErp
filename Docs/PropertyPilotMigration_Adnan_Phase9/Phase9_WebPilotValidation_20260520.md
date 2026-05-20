# Phase 9 - Web Pilot Validation
Date: 2026-05-20
Pilot Clone: $clone
User: ErpAdmin with local dev master password, no Adnan password copied.

## Login
| Step | Result | Notes |
|---|---|---|
| Select clone DB through /DevStart | Pass | Local debug-only selection |
| Login /LogIn | Pass | Returned normal app redirect to /PointOfSale/PosLogin, authenticated session was valid |
| Property screens after login | Pass | Returned HTTP 200, not blocked by 302 auth loop |

## Screen Checks
| Screen | URL | Result |
|---|---|---|
| Properties | /Property?searchWord=ADNAN-P- | Pass, HTTP 200 |
| Units | /PropertyUnit?searchWord=ADNAN | Pass, HTTP 200 |
| Contracts search | /PropertyContract?searchWord=ADNAN-C-1096 | Pass, HTTP 200 |
| Contract edit - arrears sample | /PropertyContract/AddEdit/1227 | Pass, HTTP 200 |
| Contract edit - advance sample | /PropertyContract/AddEdit/1478 | Pass, HTTP 200 |
| Contract edit - high value sample | /PropertyContract/AddEdit/1229 | Pass, HTTP 200 |
| Receipt screen | /CashReceiptVoucher/AddEdit?cid=1227 | Pass, HTTP 200 |
| Issue screen | /CashIssueVoucher/AddEdit | Pass, HTTP 200 |
| Termination screen | /PropertyContractTermination/AddEdit | Pass, HTTP 200 |

## Operational Scenarios
| Scenario | Result | Evidence |
|---|---|---|
| Cash receipt partial on migrated contract | Pass | Voucher 40, amount 125.0000 |
| Bank receipt full on migrated contract batch | Pass | Voucher 41, amount 41,400.0000 |
| Cash direct expense issue | Pass | Voucher 24, amount 77.0000 |
| Bank direct expense issue | Pass | Voucher 25, amount 88.0000 |
| Contract termination | Pass | Termination 4, total unpaid 311,928.9800 after the test receipts |

## Deferred Web Scenarios
- Property Owner / SourceTypeId=13 payment is still Manual Review only.
- Full report-by-report UAT was not completed in this phase.
- The login landing redirect to POS is not a blocker for property pages, but should be reviewed for user experience before Go Live.
