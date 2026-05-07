# Project Extracts Premium Workspace

Date: 2026-05-07

## Scope

Upgraded `/MainErp/ProjectExtracts/Details/{id}` into a more operational project financial workspace while keeping it read-only.

## Workspace Structure

The screen includes:

- executive extract header
- KPI cards
- extract summary
- real detail lines from `project_bill_details`
- deductions and VAT area
- linked advance payment section
- linked voucher/accounting section
- report/attachments placeholder
- read-only save-preview indicators

## Real Data Loaded

The detail page continues to load:

- `project_billl`
- `project_bill_details`
- `TblPayPrePayed`
- `TblProjePayPrePayed`
- `DOUBLE_ENTREY_VOUCHERS`
- `Notes`
- `projects`
- `TblCustemers`
- `TblBranchesData`
- `ACCOUNTS`

## Accounting Trace

The accounting section shows:

- voucher line count
- total debit
- total credit
- balance difference
- balanced/unbalanced status
- open journal links by note id

## Save Preview Only

The screen includes a read-only save-preview summary.

It confirms:

- header reference remains `project_billl`
- line effect comes from `project_bill_details`
- advance/prepaid rows are displayed only
- voucher lines are displayed only
- no write/post/rebuild behavior is active

## Safety

- No save/edit/post/delete behavior.
- No voucher rebuild.
- No `Notes` writes.
- No `DOUBLE_ENTREY_VOUCHERS` writes.
- No database schema change.
- No `AllScripts.sql` change.
- No `Areas\Pos` change.
