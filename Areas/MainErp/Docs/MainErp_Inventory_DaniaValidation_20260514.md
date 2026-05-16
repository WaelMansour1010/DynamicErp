# MainErp Inventory Dania Validation - 2026-05-14

## Scope

- Source authority: Main Original VB6 only.
- Runtime validation database: `Dania`.
- Target area: `Areas/MainErp`.
- Screens validated: Items, Stores, Inventory Count `FrmNewGard` / `FrmNewGard1`, Assembly Voucher `FrmDefinCompItem`.
- Testing mode: read-heavy validation against real Dania inventory data. Destructive save/delete/regenerate actions were not executed because Dania is real operational data and no isolated safe test document was supplied.

## Source Forms Checked

| Screen | Main Original VB6 source path | MainErp route |
| --- | --- | --- |
| Items | `F:\Source Code\SatriahMain\Frm\FrmItems.frm` | `/MainErp/Items` |
| Stores | `F:\Source Code\SatriahMain\Frm\FrmStoreData.frm` | `/MainErp/StoreData` |
| Inventory Count | `F:\Source Code\SatriahMain\Frm\FrmNewGard.frm` | `/MainErp/Stocktaking` |
| Inventory Count / Settlement | `F:\Source Code\SatriahMain\Frm\FrmNewGard1.frm` | `/MainErp/Stocktaking?mode=FrmNewGard1` |
| Assembly Voucher | `F:\Source Code\SatriahMain\Frm\FrmDefinCompItem.frm` | `/MainErp/DefinCompItem` |

## Dania Runtime Route Smoke

MainErp was switched through the local debug database override to `Dania`; `Web.config` was not changed for this validation.

| Route | Result | Dania record/search check | Console/server status |
| --- | --- | --- | --- |
| `/MainErp/Items` | Pass | Opened item card/list against Dania. Existing item `9604` and search `C326` loaded. | No raw server error; no browser console errors observed. |
| `/MainErp/StoreData` | Pass | Store `1` loaded; page showed 24 Dania stores and linked counts. | No raw server error; no browser console errors observed. |
| `/MainErp/Stocktaking` | Pass | Existing stock count `143149`, serial `70`, loaded. Search `70` returned a real document. | No raw server error; no browser console errors observed. |
| `/MainErp/Stocktaking?mode=FrmNewGard1` | Pass | Settlement/count mode route opened. Existing Dania stock-count data available through the same screen. | No raw server error; no browser console errors observed. |
| `/MainErp/DefinCompItem` | Partial | Route opened, but Dania has zero `TblDefComItem` headers, so historical assembly load could not be validated. | No raw server error; no browser console errors observed. |

## Stock Tables Inspected

| Table | Dania rows |
| --- | ---: |
| `TblItems` | 8,447 |
| `TblItemsUnits` | 10,307 |
| `TblUnites` | 156 |
| `Groups` | 223 |
| `TblStore` | 24 |
| `TblBranchesData` | 7 |
| `Transactions` | 74,890 |
| `Transaction_Details` | 685,215 |
| `TblDefComItem` | 0 |
| `TblDefComItemDet` | 0 |
| `TblDefComItemData` | 0 |

## Movement Tables Inspected

- Main stock movement source is `Transactions` joined to `Transaction_Details` and `TransactionTypes`.
- Main Original VB6 stock count queries use `Transaction_Details.Quantity * TransactionTypes.StockEffect` as the balance basis.
- `Transaction_Details.ShowQty` is display/count quantity.
- `Transaction_Details.Quantity` is normalized quantity.
- `Transaction_Details.QtyBySmalltUnit` stores the unit factor used at save time.
- `FrmNewGard1.frm` explicitly documents that normalized stock must not apply `UnitFactor` twice; it calculates book stock from normalized quantity and divides by the row unit factor only for display.

High-volume Dania movement types observed:

| Transaction type | Header count | Observation |
| --- | ---: | --- |
| 42 | 16,766 | Heavy movement type; stock effect must be kept in the source sign map. |
| 19 | 8,707 | Heavy movement type, likely issue/out flow in legacy operation. |
| 6 | 6,450 | Heavy movement type. |
| 28 | 6,181 | Present in Dania, but not linked to `TblDefComItem`. |
| 21 | 5,295 | Sales-like movement present in Main Original assembly flow. |
| 30 | 55 | Stock count documents. |
| 27 | 1 | Present once, no detail rows and not linked to `TblDefComItem`. |

## Unit Conversion Findings

- Total detail rows inspected: 685,215.
- Aggregate `Quantity`: 261,721,129.747484.
- Aggregate `ShowQty`: 257,048,101.736750.
- Historical rows where unit link or simple conversion check does not match: 3,902.
- Missing item references in details: 0.
- Missing store references on transaction headers: 0.
- Negative `ShowQty` rows: 41.
- Negative `Quantity` rows: 41.

Important interpretation:

- A simple `ShowQty * TblItemsUnits.UnitFactor = Quantity` rule is not reliable across all Dania history.
- Some sampled rows have `QtyBySmalltUnit` values such as `16` while the current `TblItemsUnits.UnitFactor` join reports `1`, which means historical transaction rows may preserve the true factor at transaction time better than current unit master data.
- MainErp stocktaking save currently calculates `Quantity = ShowQty * current TblItemsUnits.UnitFactor`; that is safe only when current unit setup still matches the historical/operational unit definition.
- Assembly save currently writes `QtyBySmalltUnit = 1` for generated component/output transaction details. That should not be treated as fully stock-safe for non-default units until a controlled assembly test confirms intended unit behavior.

## Store / Branch Findings

- Dania stores inspected: 24.
- Active high-volume stores include:
  - Store `1`: production finished goods, 48,311 transaction headers.
  - Store `2`: raw materials, 12,338 transaction headers.
  - Store `6`: work-in-progress, 4,429 transaction headers.
- Store branch mismatch rows found historically: 342.
- MainErp stocktaking save has a safeguard that rejects a document branch when it does not match the selected store branch.
- Existing Dania historical mismatches remain a reporting/balance risk and should be reviewed before using branch-filtered inventory reports as final finance evidence.

## Inventory Count Findings

- `Transactions.Transaction_Type = 30` is the stock-count document type in VB6 and MainErp.
- Dania has 55 stock-count documents.
- Stock-count documents with no lines: 0.
- Protected stock-count documents by closed/posted/settlement/link flags: 50.
- Latest sampled document: `143149`, serial `70`, store `3`, branch `10`, 474 lines.
- Stock-count difference fields are heavily populated:
  - Lines with `Gardresult` difference: 6,937.
  - Positive lines `Gardresult1`: 2,356.
  - Negative lines `Gardresult2`: 4,581.
  - Difference sum: `-3,071,307,623.9642`.
  - Plus sum: `1,002,357,855.0954`.
  - Minus sum: `4,073,665,479.0596`.

Validation result:

- MainErp opens and loads real Dania stock counts.
- Counted quantity, book quantity, difference fields, store, branch, and line grids are readable through the route.
- Delete protection is important and appears aligned with real Dania data because most count documents are already protected.
- Settlement generation was not executed because it creates plus/minus movement and accounting effects.

## Assembly Voucher Findings

- Dania has no rows in `TblDefComItem`, `TblDefComItemDet`, or `TblDefComItemData`.
- MainErp assembly route opens successfully, but there is no Dania historical assembly voucher to search/load.
- Dania has `Transactions` type `28` count 6,181 and type `27` count 1, but these are not linked to `TblDefComItem` through `InvoiceOrderNo` or `IDDefCIT`.
- The single type `27` header has no detail rows.
- Current MainErp assembly save creates linked type `27` and `28` inventory transactions, deletes prior linked transactions on rebuild, and blocks changes when linked transactions are posted.
- Main Original `FrmDefinCompItem.frm` includes more workflow than the current basic issue/receipt pair: production order creation uses type `26`, sales/out operations use types such as `21` and `19`, and VB6 includes hidden/re-save/rebuild behaviors.

Validation result:

- Assembly UI is route-stable.
- Historical Dania parity cannot be proven because assembly master tables are empty.
- Current MainErp assembly posting should remain controlled until a safe test assembly is created in an isolated Dania copy or approved pilot record.

## Item Master Findings

- Item route loads real Dania item masters.
- Search by code/name/barcode route works against Dania.
- Existing item `9604` loaded through `/MainErp/Items?id=9604`.
- Item master fields validated in route/data inspection:
  - item code/name;
  - group/category;
  - barcode;
  - purchase/sales/cost price;
  - active/archive flag;
  - item type;
  - units through `TblItemsUnits`;
  - default-unit selection logic.
- Main Original `FrmItems.frm` has broader behavior than the current route, including item prices, composite item parts, duplicate item/code/barcode validation, warranty/guarantee logic, and multi-unit pricing. Those remain important for full item-master parity, but stock lookup and inventory-facing fields are available.

## Store Master Findings

- Store route loads real Dania stores and branch/store linkage.
- Store `1` loaded with transaction count and operational flags.
- Store deletion protection exists in the shared store repository: a store with transactions or linked accounting voucher use is blocked from deletion.
- Because Dania has heavy store movement, destructive store delete tests were not attempted.

## Save/Edit/Delete Tests

| Area | Result |
| --- | --- |
| Items | Existing record load/search verified. Save/edit was not performed on Dania real item master without a safe test item. |
| Stores | Existing store load/search verified. Delete protection was not destructively executed; repository blocks stores with transactions/accounting use. |
| Inventory Count | Existing document load/search verified. Save path inspected; branch/store/unit validations are present. Settlement/delete/regenerate not executed against Dania real count documents. |
| Assembly Voucher | Route opens and save path inspected. No Dania historical assembly rows exist; no new assembly was generated because that would create inventory/accounting movement. |

## Safeguards Observed

- Stocktaking save validates document date, branch, store, at least one line, non-negative count/price/book quantity, expiry after production date, branch/store existence, item existence, and selected unit belonging to the item.
- Stocktaking delete blocks closed, posted, settled, or linked documents.
- Store delete blocks stores that have transaction history or linked accounting voucher usage.
- Assembly save blocks rebuild when linked type `27`/`28` transactions are posted.
- Assembly rebuild deletes linked `Transactions`, `Transaction_Details`, `Notes`, and `DOUBLE_ENTREY_VOUCHERS` for the linked assembly transaction IDs before re-creating them.

## Remaining Risks

1. **Assembly parity risk:** Dania has no assembly master rows, and current MainErp assembly movement types do not prove parity with Main Original `FrmDefinCompItem` production/sales workflow.
2. **Unit conversion risk:** 3,902 historical detail rows do not match a simple current-master `ShowQty * UnitFactor` comparison. Use transaction-time `QtyBySmalltUnit` carefully and avoid double conversion.
3. **Branch/store historical risk:** 342 transaction headers have store branch mismatch. MainErp should block new mismatches, but old balances may still look wrong under branch filters.
4. **Stock-count settlement risk:** Dania stock count difference totals are very large; plus/minus settlement should not be executed from MainErp until finance signs off on the sign map and costing behavior.
5. **Item-master completeness risk:** The MainErp item screen supports inventory-facing fields, but full VB6 item behavior includes more price/composite/warranty behavior than was validated here.

## Recommendations

1. Create an isolated Dania copy for destructive inventory QA, then run controlled save/edit/delete tests for:
   - one new item with two units;
   - one test store if allowed;
   - one small stock-count document;
   - one assembly voucher with one final product and two components.
2. Add a read-only stock balance diagnostic endpoint/page in MainErp that shows:
   - source `TransactionTypes.StockEffect`;
   - `Quantity`, `ShowQty`, `QtyBySmalltUnit`;
   - current `TblItemsUnits.UnitFactor`;
   - balance by store, item, unit, color, size, class, lot/expiry where present.
3. For assembly, reconcile MainErp type `27`/`28` behavior with Main Original `FrmDefinCompItem.frm` type `26`, `21`, and `19` flows before enabling client-side rebuild in production.
4. Review the 342 historical branch/store mismatches and decide whether reports should show a data-quality warning when branch-filtered inventory includes mismatched historical rows.
5. Treat Dania stock-count settlement as protected pilot functionality until plus/minus movement and costing are finance-approved.

## Screenshots Checklist

| Screen | Screenshot status |
| --- | --- |
| Items | Browser route opened against Dania; existing item/search confirmed. Capture full-page visual if final client packet requires images. |
| Stores | Browser route opened against Dania; store counts and store `1` confirmed. Capture full-page visual if final client packet requires images. |
| Stocktaking | Browser route opened against Dania; document `143149` and search `70` confirmed. Capture line-grid screenshot before finance review. |
| Stocktaking settlement mode | Browser route opened against Dania. Capture workflow controls before settlement pilot. |
| Assembly Voucher | Browser route opened; no Dania assembly record exists. Capture empty-state/workflow screen only, not historical parity. |

## Delivery Status

MainErp inventory screens are route-stable against Dania and can load real item, store, and stock-count data. Stock movement confidence is **conditional**, not final: item/store/count read workflows are usable, but assembly voucher parity and unit-conversion integrity require a controlled Dania copy or approved pilot document before production movement generation should be trusted.
