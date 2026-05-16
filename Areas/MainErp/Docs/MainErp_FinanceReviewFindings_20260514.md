# MainErp Finance Review Findings - 2026-05-14

Database: `Eng`  
Scope: finance visibility, accounting linkage, balances, payroll replay, Project Extracts, LC, and controlled parallel-run risks.

## Finance Review Summary

MainErp is finance-visible and safer than a simple migrated UI, but finance sign-off is not complete. The system should remain in protected pilot mode until finance compares daily MainErp outputs against official VB6 results.

## Balance and Linkage Evidence

| Area | Evidence | Finding |
|---|---|---|
| Customers/vendors | 2,688 customer/vendor rows observed | 2 operational customer/vendor rows missing/invalid linked accounts |
| Customer/vendor classifications | 1,128 rows outside strict customer/supplier `Type IN (1,2)` | Business mapping required before forcing classification |
| Banks | 23 rows observed | 1 bank missing linked account: `BankID 18`, `بنك التجربة` |
| Boxes | 83 rows observed | No missing account links in the probe |
| Notes | 75,973 rows observed | Historical volume significant; compare by original grouping rules |
| Voucher lines | 567,289 rows observed | Simple grouping by line/note can show false imbalance due to legacy structure |
| Transactions | 17,421 headers and 53,136 details | One sampled type 38 header/detail total mismatch requires review |

## Finance Findings by Workflow

### Customers and Vendors

- Account/opening balance fields are visible in MainErp.
- Linked-account validation is now safer for new operations where implemented.
- Existing duplicate risk remains: 10 duplicate customer-name groups.
- `Type = 3` and other legacy classifications require business mapping before UI or reporting hard assumptions.

### Banks and Boxes

- Banks/boxes are visible and route-stable.
- Server-side reference checks were added for account, branch, currency, and employee/cashier references.
- `BankID 18` must be fixed, hidden, or blocked from operation before rollout.
- Existing duplicate box names: 2 groups. Finance should decide merge/rename policy.

### Receipts and Payments

- Receipt/payment create routes open.
- New server checks block zero/negative value, negative VAT, missing party account, and bank/box ambiguity.
- Finance still needs to compare a controlled receipt/payment entry against VB6 expected voucher lines before enabling normal production entry.

### Project Extracts

- MainErp now shows project/customer context, totals, deductions, VAT, retention/net visibility, voucher trace, and report access.
- Current state is readable enough for finance walkthrough.
- Finance must compare at least 3 real extracts:
  - current value,
  - previous extracts,
  - VAT,
  - retention,
  - deductions,
  - net payable,
  - accounting trace.

### Letters of Credit

- LC list/details/report routes open with real rows.
- Lifecycle, financial summary, local value, opening expenses, shipping/documents tab, and protected rebuild/delete controls are visible.
- Rebuild/posting permissions are gated and should remain protected until LC owner approval.
- Finance should compare 5 active LC records against VB6 summary and voucher movement.

### Payroll Replay

- Payroll preview and replay remain read-only.
- Preview payload was reduced to about 105.8 KB while keeping totals.
- Replay normal mode measured about 459.1 KB.
- Payroll preview still takes about 6.3 seconds because it uses Main Original-compatible reconstruction and legacy logic.
- Finance must compare at least one real pay period:
  - net salary,
  - deductions,
  - advances,
  - insurance,
  - branch/department distribution,
  - account distribution.

## Broken or Risky Legacy Behaviors Discovered

- Historical voucher storage is line-centric/split; simple web grouping is not enough to declare historical vouchers balanced or broken.
- One bank lacks account linkage.
- Two customer/vendor rows lack valid account linkage.
- One transaction type 38 has header/detail total mismatch.
- One transaction detail row uses a unit not registered for its item.
- Assembly table has zero rows in `Eng`, so historical assembly finance behavior could not be sampled.

## Business Decisions Required

| Decision | Owner | Status |
|---|---|---|
| Customer/vendor `Type=3` meaning and UI label | Finance / operations | Pending |
| BankID 18 cleanup/blocking | Finance | Pending |
| Duplicate customer and box cleanup policy | Finance / master data | Pending |
| Receipt/payment posting approval path | Finance | Pending |
| Payroll account distribution approval | Finance / HR | Pending |
| Payroll production posting activation | Executive finance | Not approved |
| Project Extract approval lifecycle | Projects / finance | Pending |
| LC rebuild permission policy | Finance / LC owner | Pending |
| Assembly cost variance policy | Inventory / finance | Pending |

## Production Blockers

- No completed daily VB6 vs MainErp parallel comparison yet.
- Payroll posting remains intentionally disabled.
- Assembly voucher lacks historical `Eng` samples.
- Finance has not signed off voucher grouping semantics.
- Master-data cleanup is still needed for known missing account and duplicate risks.

## Finance Recommendation

Proceed with protected pilot mode only. Do not enable production posting or make MainErp the financial system of record until daily finance comparison packs are reviewed and signed off.
