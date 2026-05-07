# Project Extracts Premium UX Plan

Date: 2026-05-07

## Goal

Transform Project Extracts into a project financial control center instead of a flat invoice-like form, while keeping the phase read-only and aligned with `projectsbill.frm`.

## Implemented Workspace

The Project Extracts screen now uses:

- left search rail for extract/project/branch filtering,
- selected extract executive header,
- sticky KPI band,
- project completion progress bars,
- smart tabs,
- read-only accounting and financial visualization,
- protected disabled actions for save, voucher posting, and reports.

## Tabs

1. Extract Data
   - Extract id, note serial, date, project, customer, branch, manual number, totals, VAT, and net payable.

2. Execution Items
   - Visual execution cards for previous, current, and remaining quantities.
   - Current implementation uses available summary values until full `project_bill_details` mapping is wired.

3. Deductions and Retentions
   - Visual deduction center showing retention, advance deduction, VAT, and net payable.
   - Calculated placeholders are clearly read-only and will be replaced by mapped VB6 fields in the next phase.

4. Accounting Timeline
   - Shows the linked project extract note/voucher entry point where `note_id` exists.
   - Opens the read-only Journal Entry details screen.

5. Cash Flow
   - Shows list-level totals for billed value, VAT, and net payable.

6. Smart Insights
   - Holds future intelligent warnings such as abnormal deductions, delayed collection, execution slowdown, and profitability movement.

## Visual Strategy

The screen uses:

- KPI cards,
- progress bars,
- status badges,
- tabbed cockpit navigation,
- financial cards,
- read-only voucher panel,
- responsive card/grid stacking.

## Performance Strategy

The screen renders the first page of extracts and focuses the newest returned extract as the current cockpit item. Future work should lazy-load:

- `project_bill_details`,
- advance payment links,
- VAT detail rows,
- journal lines,
- cash-flow history.

## Safety

Still disabled:

- save,
- edit,
- delete,
- approval,
- posting,
- real voucher generation,
- database writes.

No database schema or SQL script changes were made.
