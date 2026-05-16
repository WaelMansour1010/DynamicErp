# MainErp Production Readiness Decision - 2026-05-14

Database: `Eng`  
Decision scope: MainErp staged rollout readiness after UI, route, runtime, finance, inventory, and performance hardening.

## Decision

Status: still requires protected pilot mode.

MainErp is not approved for full production posting. It is ready for a controlled parallel-run pilot with VB6 remaining the official system of record.

## Why This Decision

MainErp has reached a strong client-trial state:

- Core routes open.
- MainErp menu is organized.
- Customers, items, banks, boxes, stores, inventory, assembly, project extracts, LC, users, receipts/payments, and payroll surfaces are usable.
- Build passes.
- Browser smoke has shown no JavaScript console errors in the focused checks.
- Heavy payload risk was reduced, especially payroll preview.
- Server-side write safeguards were added around financial and inventory operations.

But production readiness requires live business evidence that is not yet collected:

- No completed multi-day VB6 vs MainErp parallel comparison.
- No operator feedback log from real daily work.
- No finance sign-off on payroll replay, voucher grouping, Project Extracts, LC summaries, or assembly costing.
- Known master-data issues remain.
- Payroll production posting is intentionally not enabled.

## Recommended Rollout Scope

Allowed for protected pilot:

- Read-only finance visibility.
- Customer/vendor search and safe master-data review.
- Items search and item/unit review.
- Banks/boxes/stores review and controlled admin edits after finance approval.
- Project Extracts review/report walkthrough.
- LC list/details/report walkthrough.
- Payroll preview and replay diagnostics.
- Medical insurance dashboard, reports, and policy walkthrough.
- Stocktaking controlled test documents in a pilot branch/store.
- Assembly controlled test-only scenario after inventory/finance approval.

Not allowed yet:

- Production payroll posting.
- Production accounting posting from MainErp.
- Unapproved receipt/payment posting.
- LC rebuild/delete in production.
- Assembly posting as official inventory movement without controlled comparison.
- Automated historical voucher repair.
- Silent master-data merges.

## Production Blockers

| Blocker | Severity | Required action |
|---|---|---|
| No real parallel-run evidence | High | Run at least 5 business days of side-by-side VB6/MainErp comparison |
| Payroll posting not approved | High | Finance/HR sign-off on preview, replay, account distribution |
| Voucher grouping semantics unresolved | High | Validate against Main Original VB6 rules before hard accounting claims |
| BankID 18 missing account | Medium | Fix, block, or hide before treasury use |
| 2 customer/vendor rows missing valid linked account | Medium | Correct master data before operational use |
| Customer/vendor `Type=3` mapping unresolved | Medium | Business classification decision |
| Assembly lacks historical `Eng` samples | Medium | Create controlled test scenario and compare costs/movements |
| Payroll/replay server time still heavy | Medium | Add instrumentation and indexes before broad HR rollout |
| Operator feedback not collected | Medium | Run supervised user sessions and record friction |

## Readiness by Area

| Area | Decision | Notes |
|---|---|---|
| Customers/vendors | Pilot-ready | Needs balance/type cleanup before production |
| Items | Pilot-ready | Search/page performance acceptable |
| Banks/boxes/stores | Pilot-ready with finance supervision | Known bank account issue must be resolved |
| Receipts/payments | Protected pilot only | Write guards exist; posting comparison required |
| Inventory count | Protected pilot only | Needs real branch/store count comparison |
| Assembly voucher | Test-only pilot | No historical assembly rows in `Eng` |
| Project Extracts | Pilot-ready for review/reporting | Approval lifecycle needs business sign-off |
| LC | Pilot-ready for visibility | Rebuild/posting remains protected |
| Users/permissions | Pilot-ready | Role matrix still needs named business sign-off |
| Payroll | Preview/replay pilot only | No production posting |
| Medical insurance | Pilot-ready as selling feature | HR/finance workflow approval still required |
| Reports | Pilot-ready for summaries | Large report exports need continued hardening |

## Business Decision Log

| Decision | Current recommendation |
|---|---|
| Approval flows | Keep in pilot/manual approval until owners define final workflow |
| Posting rules | Do not activate production posting |
| Rebuild permissions | Restrict to admin/finance owners only |
| Insurance workflow approval | Pilot with HR and finance reviewers |
| Department routing policy | Validate during payroll replay comparison |
| Project allocation policy | Compare extracts and payroll distribution before production |
| LC protected workflow | Keep rebuild/delete protected until LC owner signs off |

## Recommended Next Phase

Phase N should be a real-world protected pilot:

- One pilot branch.
- One HR/payroll reviewer.
- One finance reviewer.
- One inventory/store operator.
- One project/LC owner.
- Daily mismatch log.
- Daily finance comparison pack.
- End-of-week go/no-go review.

## Final Decision Statement

MainErp is ready for controlled parallel operation and client-supervised pilot use. It is not yet ready to replace VB6 as the official production system of record. The correct next move is protected pilot mode with explicit daily finance and operations comparison.
