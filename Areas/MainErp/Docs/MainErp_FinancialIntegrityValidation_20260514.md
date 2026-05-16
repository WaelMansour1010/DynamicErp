# MainErp Financial Integrity Validation - 2026-05-14

Database: `Eng`  
Source authority: Main Original VB6  
Scope: financial linkage, balances, branch safety, voucher visibility, and write protections. Payroll posting was not activated.

## Workflows tested

| Workflow | Route / area | Result |
|---|---|---|
| Banks | `/MainErp/FinancialAdministration?scope=banks` | Opens, lists real rows, branch/account linkage visible |
| Boxes | `/MainErp/FinancialAdministration?scope=boxes` | Opens, lists real rows, branch/account linkage visible |
| Customers / vendors | `/MainErp/Customers` | Opens, account/opening-balance fields visible |
| Receipt voucher | `/MainErp/Cashing`, `/MainErp/Cashing/Create` | Opens, party account and bank/box fields visible |
| Payment voucher | `/MainErp/Payments`, `/MainErp/Payments/Create` | Opens, party account and bank/box fields visible |
| Journal/accounting visibility | `/MainErp/JournalEntries`, accounting traces in project/extract/sales screens | Opens; remains protected/read-only where posting is not approved |
| Project extracts | `/MainErp/ProjectExtracts` | Totals, deductions, VAT, retention, net, voucher/accounting visibility remain readable |

## Balances checked

Read-only probes against `Eng`:

| Check | Result |
|---|---:|
| Customers | 2,688 rows |
| Items | 40,012 rows |
| Stores | 12 rows |
| Banks | 23 rows |
| Boxes | 83 rows |
| Transactions | 17,421 rows |
| Transaction details | 53,136 rows |
| Notes | 75,973 rows |
| Voucher lines | 567,289 rows |
| Assembly headers | 0 rows |

Opening-balance summary from probed fields:

| Balance bucket | Rows | Amount |
|---|---:|---:|
| Banks opening balance | 23 | 0.00 |
| Boxes opening balance | 83 | 0.00 |
| Customer opening balance | 23 | 0.00 |
| Vendor opening balance | 1,537 | 0.00 |
| Other customer/vendor type opening balance | 1,128 | 0.00 |

## Financial sanity observations

- Bank/account linkage mostly exists, but one bank row is missing a linked account: `BankID 18`, `بنك التجربة`.
- Boxes have no missing account links in the probe.
- Customer/vendor rows with `Type IN (1,2)` have 2 missing/invalid linked accounts.
- There are 1,128 rows with customer/vendor `Type` outside the current screen's strict customer/supplier model. These appear to be legacy/project/subcontractor classifications and should not be forcibly converted.
- Duplicate-risk probes found:
  - 10 duplicate groups by customer name + branch + type.
  - 2 duplicate box-name groups.
  - 0 duplicate item-code groups.
- Simple voucher-line grouping by `Double_Entry_Vouchers_ID` or `Notes_ID` shows historical imbalance patterns because old data stores many voucher lines as split/single-line groups. This is a legacy behavior classification, not a new MainErp posting activation approval.

## Safeguards added

### Banks / boxes

File: `Areas/MainErp/Repositories/FinancialAdministration/FinancialAdministrationRepository.cs`

Added server-side checks before save:

- Linked account must exist in `ACCOUNTS`.
- Branch must exist when supplied.
- Bank currency must exist when supplied.
- Box employee/cashier must exist when supplied.
- Existing duplicate-name checks remain active.

### Receipt / payment vouchers

File: `Areas/MainErp/Repositories/Payments/PaymentVoucherWriteRepository.cs`

Added pre-stored-procedure validations:

- Voucher value must be greater than zero.
- VAT cannot be negative.
- Party account is required and must exist.
- Voucher must use either a box or a bank, not both.
- Selected box/bank must exist.
- Selected box/bank branch must match voucher branch when both are defined.

These checks keep unsafe requests out of `usp_DynamicErpVoucher_Save` before any accounting write.

## Duplicate-risk observations

- Existing duplicate customer and box names are historical data risks. The web save paths now preserve duplicate prevention where implemented, but existing legacy duplicates remain documented and should be resolved by controlled master-data cleanup, not automatic merge.
- Customer/vendor `Type = 3` and other non-1/2 types should be treated as legacy classifications until the original business mapping is explicitly approved.

## Broken legacy behaviors discovered

- Historical voucher storage is not consistently group-balanced by the simple web grouping model. Do not use that grouping alone to declare the ledger wrong; validate against the original note/voucher grouping rules before any corrective SQL.
- One bank row lacks a linked account.
- Two customer/vendor operational rows lack valid linked accounts.

## Remaining accounting/business risks

- No production payroll posting was enabled.
- Accounting report and journal creation remain protected until posting rules are signed off.
- Historical voucher grouping needs a dedicated Main Original parity pass before automated balance repair.
- Customer/vendor type mapping needs business approval for `Type` values outside customer/supplier.

## Verification

- Build passed after safeguards.
- Focused browser smoke passed with no console errors for:
  - `/MainErp/Stocktaking`
  - `/MainErp/DefinCompItem`
  - `/MainErp/FinancialAdministration?scope=banks`
  - `/MainErp/FinancialAdministration?scope=boxes`
  - `/MainErp/Cashing/Create`
  - `/MainErp/Payments/Create`
- Anonymous/admin behavior for these routes remains covered by the MainErp runtime/menu QA documents; this phase focused on authenticated financial integrity checks against `Eng`.

## Screenshots checklist

| Screen | Runtime checked | Screenshot evidence |
|---|---|---|
| Banks / boxes | Pass | Covered by `runtime-final-enterprise-polish-finance.png` |
| Receipts / payments create | Pass | Browser smoke completed with console clean |
| Stocktaking / assembly links | Pass | Covered by inventory workflow validation |
| Project extracts accounting visibility | Pass | Covered by previous project extracts and runtime QA docs |

## Files changed in this phase

- `Areas/MainErp/Repositories/Stocktaking/StocktakingRepository.cs`
- `Areas/MainErp/Repositories/Payments/PaymentVoucherWriteRepository.cs`
- `Areas/MainErp/Repositories/FinancialAdministration/FinancialAdministrationRepository.cs`
- `Areas/MainErp/Docs/MainErp_FinancialIntegrityValidation_20260514.md`
- `Areas/MainErp/Docs/MainErp_InventoryWorkflowValidation_20260514.md`

## Client-readiness status

Financial surface is safer for controlled real-world use. The screens expose the relevant accounting links and now reject several dangerous invalid states before writing. Remaining risks are historical-data and parity-approval issues, not route or UI blockers.
