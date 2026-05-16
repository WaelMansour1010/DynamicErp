# MainErp Inventory Workflow Validation - 2026-05-14

Database: `Eng`  
Source authority: Main Original VB6  
Scope: inventory count, assembly voucher, item movement, unit/store/branch integrity, costing sanity, and operational safeguards.

## Workflows tested

| Workflow | Route | Result |
|---|---|---|
| Inventory count | `/MainErp/Stocktaking` | Opens, searches real transaction type 30 rows, details load |
| Assembly voucher | `/MainErp/DefinCompItem` | Opens; no existing `TblDefComItem` rows in `Eng` to validate historical assemblies |
| Items | `/MainErp/Items` | Opens and exposes item/unit/group structure |
| Stores | `/MainErp/StoreData` | Opens and exposes branch/account linkage |
| Movement visibility | Transaction and detail probes | Real movement rows exist and link to item/store/branch fields |

## Inventory sanity checks

Read-only probes:

| Check | Result |
|---|---:|
| Stocktaking documents with no lines | 0 |
| Stocktaking lines with negative counted quantity | 0 |
| Transaction detail rows without header | 0 |
| Transaction/store branch mismatches | 0 |
| Assembly rows in `TblDefComItem` | 0 |
| Detail unit not linked to item | 1 |
| Duplicate item-code groups | 0 |

## Inventory count validation

Validated current behavior:

- Stocktaking reads `Transactions` where `Transaction_Type = 30`.
- Stocktaking details read from `Transaction_Details`.
- Branch and store are visible and filterable.
- Counted quantity, system quantity fields, lot number, production/expiry dates, serial, dimensions, and auto-detect flags are represented.
- Existing data probe found no empty stocktaking documents and no negative stocktaking quantities.

Safeguards added in `Areas/MainErp/Repositories/Stocktaking/StocktakingRepository.cs`:

- Reject save when document is missing.
- Require date, branch, and store.
- Reject invalid date ranges.
- Require at least one valid item line.
- Reject negative counted quantity, negative price, and negative book quantity.
- Reject expiry date earlier than production date.
- Verify branch exists.
- Verify store exists.
- Reject store/branch mismatch.
- Verify every item exists.
- Verify selected unit belongs to the item through `TblItemsUnits`.
- Block delete if the stocktaking transaction is closed, posted, settlement-started, or linked through `Nots`/`Nots2`.

## Assembly voucher validation

Validated current behavior:

- Assembly save creates linked issue/receipt inventory transactions for transaction types 27 and 28.
- Component and final-product totals are calculated from request lines.
- Header stores linked transaction IDs and serials.
- Accounting note creation is attempted with fallback account handling.
- Existing `Eng` data currently has zero `TblDefComItem` rows, so historical cost/quantity parity could not be sampled.

Existing safeguards already present:

- Requires date, branch, store, components, and outputs.
- Requires component item, unit, positive quantity, and non-negative cost.
- Requires output item, unit, positive quantity, and non-negative cost.
- Requires each output to have linked components.
- Blocks normal edit/delete when posted/approved linked transactions exist unless explicitly forced.

Remaining assembly risk:

- Because `Eng` has no existing assembly rows, the next controlled QA pass should create one test-only assembly in a backup/test branch or sandbox data set and verify:
  - component cost total equals expected consumption,
  - output cost total and difference are acceptable,
  - linked issue/receipt vouchers are visible,
  - branch/store/account postings match Main Original business rules.

## Item movement and costing observations

- One historical `Transaction_Details` row uses a unit not linked to the item in `TblItemsUnits`.
- No duplicate item-code groups were detected.
- One sampled transaction total mismatch exists for transaction type 38 where header `NetValue = 0` while detail total is 25.00. This is classified as historical data behavior until its transaction type is mapped to the original form workflow.
- Assembly costing currently calculates component/output totals from line cost and quantity. No automatic correction was applied.

## Branch/store correctness

- No transaction/store branch mismatch was found in the probe.
- New stocktaking saves now reject mismatched store/branch combinations before writing.
- Assembly already loads stores by branch in the UI; no historical rows existed to sample mismatch.

## Serial/lot handling

- Stocktaking preserves serial, lot, production date, expiry date, part number, detailed code, item case, color, size, class, dimensions, and auto-detect fields.
- New validation prevents impossible production/expiry ordering.
- No destructive serial/lot mutation was performed in `Eng`.

## Broken legacy behaviors discovered

- Historical unit mismatch: one detail row has a unit that is not registered for the item.
- Historical transaction type 38 total mismatch requires source-workflow classification before repair.
- Assembly voucher cannot be historically validated in `Eng` because the assembly header table is empty.

## Safeguards added

- `StocktakingRepository`: branch/store/unit/date/quantity/delete protections.
- `FinancialAdministrationRepository`: linked account/reference protections for bank/box records that affect inventory/account linkage.
- `PaymentVoucherWriteRepository`: bank/box/account/branch protections for financial movement connected to inventory/customer/vendor documents.

## Remaining inventory/business risks

- Assembly voucher needs a controlled create/edit/delete test in a non-production-safe data slice.
- Historical transaction type 38 mismatch needs Main Original classification.
- Unit mismatch row should be reviewed and corrected by data maintenance, not silently fixed by the web layer.

## Verification

- Build passed after safeguards.
- Browser smoke passed with no console errors for stocktaking, assembly voucher, banks, boxes, receipt create, and payment create.
- Runtime database was `Eng`.
- No destructive inventory or accounting data correction was executed; all data-quality findings were classified and documented.

## Screenshots checklist

| Screen | Runtime checked | Screenshot evidence |
|---|---|---|
| Inventory count | Pass | Covered by runtime smoke and final polish inventory screenshots |
| Assembly voucher | Pass | Runtime smoke passed; historical-data screenshot not available because `TblDefComItem` has 0 rows |
| Items and units | Pass | Covered by `runtime-final-enterprise-polish-items.png` and mobile variant |
| Stores/branch linkage | Pass | Covered by route/menu QA and runtime smoke |

## Files changed in this phase

- `Areas/MainErp/Repositories/Stocktaking/StocktakingRepository.cs`
- `Areas/MainErp/Repositories/Payments/PaymentVoucherWriteRepository.cs`
- `Areas/MainErp/Repositories/FinancialAdministration/FinancialAdministrationRepository.cs`
- `Areas/MainErp/Docs/MainErp_FinancialIntegrityValidation_20260514.md`
- `Areas/MainErp/Docs/MainErp_InventoryWorkflowValidation_20260514.md`

## Client-readiness status

Inventory workflows are safer and financially more believable for controlled trial use. Stocktaking now has meaningful server-side integrity gates. Assembly is structurally protected and route-stable, but still needs a controlled real create scenario because `Eng` does not contain historical assembly rows to compare.
