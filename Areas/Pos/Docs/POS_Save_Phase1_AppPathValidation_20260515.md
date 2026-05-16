# POS Save Phase 1 App-Path Validation - 2026-05-15

## Scope

Validation was run against:

```text
SQL Server: Wael\Sql2019
Database: Cash
```

Cash was kept in the approved Phase 1 test state:

- `TblOptions.POSVoucherSerialScope = Branch`
- Phase 1 `dbo.usp_POS_SaveTransaction` active
- rollback capture available

No SQL changes were made.

Validation used the actual POS MVC controller/repository path in-process, with a real `PosUserContext` loaded from Cash and a fake MVC session/context. It did not use the direct SQL load-test harness for saving.

## Endpoints / Controller Actions Tested

| Route | Controller action | Result |
|---|---|---|
| `POST /Pos/PosTransaction/Save` | `PosTransactionController.Save` | Tested cash-in duplicate submit and configured violations save |
| `GET /Pos/PosTransaction/Print` | `PosTransactionController.Print` | Passed, returned PDF |
| `GET /Pos/PosTransaction/PrintPreview` | `PosTransactionController.PrintPreview` | Passed, returned `ViewResult` |
| `GET /Pos/PosTransaction/SearchAvailableKeshniCards` | `PosTransactionController.SearchAvailableKeshniCards` | Passed for available stock lookup, but no valid invoice fixture found |

## Test User / Context

```text
UserID: 49
UserName: EC023
EmpID: 241
BranchId: 23
StoreID: 26
BoxID: 54
PaymentTypeId: 1
PaymentNetId: 1
CanSave: true
CanPrint: true
CanChangeDefaults: false
```

## Duplicate-Submit Validation

Request ID:

```text
86c50bf9-15e9-4c50-9429-8453defe2ec6
```

First save result:

```text
HTTP status: 200
Transaction_ID: 1299513
NoteSerial1: 232605719
BranchId: 23
UserID: 49
Duration: 170 ms
```

Second submit used the same `ClientRequestId` and the same request values.

Second save result:

```text
HTTP status: 400
Success: false
Validation field: ManualNO
Validation message: duplicate IPN for cash-in/card path
Duration: 9 ms
```

Database verification:

```text
POS_SaveIdempotency rows for ClientRequestId: 1
Idempotency status: Completed
Idempotency Transaction_ID: 1299513
Transaction exists: 1
Notes rows: 1
DOUBLE_ENTREY_VOUCHERS rows: 7
Debit: 277.80
Credit: 277.80
Duplicate NoteSerial1 rows in Branch scope: none
```

Classification:

- **Data correctness passed:** no duplicate invoice/voucher/accounting rows were created.
- **Response behavior needs follow-up:** the exact second submit was blocked by IPN duplicate validation before returning the nicer idempotent `duplicateRequest=true` response.
- This does not justify Phase 1 rollback, but it should be cleaned up before declaring duplicate-submit UX fully validated.

## Receipt / Print Validation

Test transaction:

```text
Transaction_ID: 1299513
NoteSerial1: 232605719
```

`GET /Pos/PosTransaction/Print` result:

```text
HTTP status: 200
Result type: System.Web.Mvc.FileContentResult
Content type: application/pdf
File name: pos-receipt-1299513.pdf
PDF bytes: 72774
Duration: 1489 ms
```

`GET /Pos/PosTransaction/PrintPreview` result:

```text
HTTP status: 200
Result type: System.Web.Mvc.ViewResult
Duration: 147 ms
```

Classification: **passed**. The receipt/print controller path did not produce a server error and resolved the correct transaction id.

## Card Flow Validation

Card availability route:

```text
GET /Pos/PosTransaction/SearchAvailableKeshniCards
StoreID: 26
Available token count returned by repository check: 5
First available stock token: 22889523
First available item id: 19
```

Card save was **not attempted**.

Reason:

- Available stock tokens exist.
- The app save path requires a KYC/customer fixture (`TblCusCshId`) for card invoices.
- A targeted read-only Cash fixture check did not find a matching unused KYC customer with stock for StoreID `26`.
- The earlier broad card candidate discovery timeout remains a harness/data-fixture issue, not a script 100 save-path failure.

Classification: **missing valid card fixture**.

Next safe step:

1. Provide or identify one known valid unused KYC card customer with available stock.
2. Run one app-path card save only.
3. Do not use the broad candidate discovery query for this smoke.

## Violations Flow Validation

The previous item `2` failure should not be treated as a Phase 1 regression. For app-path validation, a configured violations item was used:

```text
ItemID: 20
SupplierAccount: a1a2a2a1a1513
ItemRevenueAccount: a4a1a3a1a3
PricePercent: 0.002
IsValidFixture: 1
```

Save result:

```text
HTTP status: 200
Transaction_ID: 1299515
NoteSerial1: 232605720
BranchId: 23
UserID: 49
Duration: 216 ms
```

Database verification:

```text
Transaction_Type: 21
DetailRows: 1
NoteRows: 1
DOUBLE_ENTREY_VOUCHERS rows: 4
Debit: 50.20
Credit: 50.20
Duplicate NoteSerial1 rows in Branch scope: none
```

Classification: **passed with valid configuration**.

This confirms the earlier `ItemID=2` violations failure was a test/configuration gap, not evidence that script 100 broke the violations save path.

## Errors / Gaps

1. Exact duplicate submit did not create duplicate rows, but returned a `400` duplicate-IPN validation response instead of the idempotent duplicate-success response.
2. Card invoice save remains untested because no valid KYC+stock fixture was found.
3. Receipt PDF generation passed in-process; a browser-level print dialog was not tested.

## Supporting Logs

```text
F:\Source Code\DynamicErp\Areas\Pos\Logs\Phase1CashTest_20260515\18_app_path_validation_output_final.json
F:\Source Code\DynamicErp\Areas\Pos\Logs\Phase1CashTest_20260515\19_app_path_final_sql_verification.txt
F:\Source Code\DynamicErp\Areas\Pos\Logs\Phase1CashTest_20260515\14_card_fixture_targeted.txt
```

## Final Recommendation

Recommendation: **Phase 1 remains promising, but do not start medium/peak load yet**.

Safe conclusions:

- App-path cash-in save works under Phase 1.
- Duplicate submit did not create duplicate invoice/voucher/accounting rows.
- Receipt/print controller path works for the saved transaction.
- Valid configured violations save works and accounting balances.
- Branch-scope voucher numbers remained unique in the tested scope.

Remaining blockers before load:

1. Decide whether duplicate-submit should return the existing saved invoice before IPN duplicate validation.
2. Provide a deterministic valid card fixture and run one card app-path save.
3. Optionally run one browser/UI receipt smoke if visual rendering is required beyond controller/PDF generation.
