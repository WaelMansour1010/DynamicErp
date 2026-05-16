# MainErp Parallel Run Validation - 2026-05-14

Database: `Eng`  
Source authority: Main Original VB6  
Decision model: controlled parallel operation beside official VB6 posting.

## Executive Position

MainErp is ready to enter a protected parallel-run pilot, not full production enablement.

Official posting must remain in VB6 during this phase. MainErp should be used beside VB6 for selected branches/users, with daily comparison of balances, documents, payroll preview, inventory movement, project extracts, LC summaries, and operator feedback.

## Parallel Run Scope

| Area | MainErp usage during pilot | VB6 status | Evidence needed |
|---|---|---|---|
| HR employees | View/edit selected employee records with admin supervision | Official master remains VB6 until approved | Daily employee/branch/department comparison |
| Medical insurance | Use MainErp dashboard, renewal visibility, reports, accounting preview | Official finance/posting remains VB6 | Operator feedback and renewal-case comparison |
| Payroll preview | Run preview and replay diagnostics only | Official payroll/posting remains VB6 | Net, deduction, advance, account distribution comparison |
| Inventory count | Create/test selected count documents in controlled branch/store only | Official inventory remains VB6 | Counted qty, settlement, store/branch comparison |
| Assembly voucher | Controlled test-only transaction until business approves | Official assembly remains VB6 | Component/output cost and linked voucher comparison |
| Customers/vendors | Search/open/edit safe fields only after approval | Official customer balance remains VB6 | Balance/account/type comparison |
| Banks/boxes | Read/admin validation; avoid unapproved financial movement | Official treasury remains VB6 | Bank/box balance and account-link comparison |
| Project extracts | Use MainErp for readability/reports and approval walkthrough | Official billing/accounting remains VB6 | Extract totals, VAT, retention, net, voucher trace comparison |
| LC | Use MainErp workbench/details/report in protected mode | Official LC posting/rebuild remains VB6 | Financial summary, permission, rebuild understanding |

## Workflows Compared So Far

| Workflow | Current MainErp evidence | Parallel status |
|---|---|---|
| Customers/vendors | Route stable, real rows visible, account/opening fields present | Requires daily VB6 balance comparison |
| Items | Route stable, 40,012-item table handled with paging/search | Requires operator item lookup timing |
| Inventory count | Route stable, no empty/negative historical count docs, safeguards added | Requires controlled live count comparison |
| Assembly voucher | Route stable and structurally protected; `Eng` has zero historical assembly rows | Requires controlled create/rebuild/delete scenario |
| Banks/boxes | Routes stable, account linkage visible, write guards added | Requires finance balance sign-off |
| Project extracts | Details/report route stable, totals/deductions/VAT/net visible | Requires finance review against real extract file |
| LC | List/details/report stable, lifecycle and protected controls visible | Requires LC team workflow walkthrough |
| Payroll preview/replay | Preview compacted; replay protected/read-only | Requires payroll period parity review |
| Medical insurance | MainErp HR feature is visually and operationally ready for pilot | Requires operator feedback and policy approval |

## Operational Findings

- MainErp opens the core routes required for a controlled pilot.
- Search/list screens are usable and generally fast enough for operator trial.
- Salary preview payload was reduced from about 5.8 MB to about 106 KB.
- Stocktaking item lookup is now async and bounded, avoiding large per-row dropdown payloads.
- Payroll posting remains intentionally disabled/protected.
- Accounting and inventory safeguards reject several invalid states before write.

## Operator Feedback

Not yet collected from real operators in a live parallel run.

Required feedback log for each pilot day:

| Date | Branch | User | Workflow | VB6 time | MainErp time | Confusion point | Support needed | Accepted? |
|---|---|---|---|---:|---:|---|---|---|
| Pending | Pending | Pending | Pending | Pending | Pending | Pending | Pending | Pending |

Key questions:

- Did the user understand the branch/store/account context?
- Did terminology match their VB6 mental model?
- Did the screen reduce or increase support requests?
- Did medical insurance dashboard create confidence or confusion?
- Did Project Extracts and LC summaries explain the financial state clearly?

## Required Daily Comparison Pack

For each selected branch/day:

- Customer balance sample: 10 active customers and 10 vendors.
- Bank/box balance sample: all pilot branch boxes and banks.
- Inventory movement sample: 20 high-volume items and any counted items.
- Payroll sample: selected branch/department for the active salary period.
- Project extracts: all extracts touched that day.
- LC: all active LC records touched that day.
- Exceptions: every mismatch above agreed tolerance.

## Mismatch Handling

Each mismatch must be classified:

- `VB6 expected / MainErp bug`
- `MainErp expected / VB6 legacy issue`
- `Data-quality issue`
- `Unapproved business-rule difference`
- `Operator usage error`
- `Performance/usability issue`

No mismatch should be hidden or patched directly in production data without written business approval.

## Pilot Entry Criteria

- Admin routes open and build passes: met.
- MainErp menu and routes stable: met.
- No raw server errors in smoke: met.
- Payroll production posting disabled: met.
- Finance/inventory risky writes protected: met.
- Real operator parallel-run schedule: pending business coordination.
- Daily comparison owner assigned: pending.

## Pilot Exit Criteria

MainErp can be considered for staged production rollout only after:

- At least 5 consecutive business days of pilot comparison.
- No unresolved high-severity financial mismatches.
- Payroll preview/replay accepted by finance for at least one real pay period.
- Inventory count and assembly scenarios validated with real branch/store data.
- Medical insurance workflow approved by HR and finance.
- Project Extracts and LC owners approve the displayed lifecycle and totals.
- Permission matrix signed off for pilot roles.

## Recommended Next Phase

Run a protected pilot with one branch, one finance reviewer, one HR reviewer, one inventory operator, and one project/LC reviewer. Keep VB6 as the official system of record until the exit criteria above are met.
