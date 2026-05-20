# Phase 10 - User Testing Guide
Date: 2026-05-20
ReadyToTest DB: $db

## How To Start
1. Open DynamicErp locally.
2. Go to http://localhost:63735/DevStart.
3. Select Original Web database: $db.
4. Login using ErpAdmin.
5. Start testing from the property module screens below.

## Suggested Starting Screens
| Purpose | URL / Screen |
|---|---|
| Properties | /Property?searchWord=ADNAN-P- |
| Units | /PropertyUnit?searchWord=ADNAN |
| Contracts | /PropertyContract?searchWord=ADNAN-C-1096 |
| Open arrears sample | /PropertyContract/AddEdit/1227 |
| Open advance sample | /PropertyContract/AddEdit/1478 |
| Open high-value sample | /PropertyContract/AddEdit/1229 |
| Receipt screen | /CashReceiptVoucher/AddEdit?cid=1227 |

## Suggested Business Scenarios
| Scenario | Expected Result |
|---|---|
| Open contract | Contract loads with renter, unit, and batches |
| Review batches | Paid/remain values are visible and coherent |
| Cash receipt | Use CASH-PILOT; cashbox account should be used |
| Bank receipt | Use BANK-PILOT; bank account should be used |
| Termination | Test one contract, then reset if needed using draft cleanup/reset script |
| Direct expense payment | Allowed only if account setup remains valid |
| Property owner payment | Do not approve yet; Manual Review only |

## Important Testing Rule
If you create receipts, payments, or terminations during testing, they will remain in this ReadyToTest DB. Use the reset script only after deciding to clear test artifacts.

## What Not To Expect
- Full historical accounting from Adnan is not migrated.
- The 10 excluded shell contracts are not present as active contracts.
- Adnan users/passwords are not migrated.
