# LC Premium UX Plan

Date: 2026-05-07

## Goal

Transform Letters of Credit from a raw data screen into a premium enterprise accounting cockpit while preserving the `FrmLC.frm` workflow and read-only safety.

## Implemented Workspace

The LC screen now uses:

- left search rail for fast power-user lookup,
- cockpit header with LC number, vendor, bank, currency, and status,
- sticky summary KPI band,
- smart tabs for operational areas,
- account cards with health indicators,
- expandable voucher timeline,
- read-only margin/history/opening grids,
- disabled protected actions for save, delete, posting, and reports.

## Tabs

1. LC Data
   - LC number, supplier, project, branch, dates, bank, currency, value, open value, and remarks.

2. Linked Accounts
   - LC account, margin account, acceptance account, expense account, project expense account, and parent accounts.
   - User display uses `Account_Serial + localized Account_Name`.
   - Raw `Account_Code` remains internal only.

3. Accounting Timeline
   - Groups related Notes and voucher rows.
   - Shows debit, credit, affected accounts, voucher count, and balance state.
   - Each voucher can open the read-only Journal Entry details screen.

4. Margin and Payments
   - Displays mapped VB6 grid sections as read-only tables.
   - Voucher links are available where `NoteID` exists.

5. Opening Balance
   - Shows opening balance amount, type, opening NoteID, serial, and voucher group id.

6. Operation History
   - Read-only activity feed summarizing the LC lifecycle now visible from current data.

7. Reports and Attachments
   - Report/document actions are visible but disabled until report migration is approved.

## Accounting Visualization

The screen shows:

- generated accounting rows,
- total debit,
- total credit,
- balance difference,
- balanced/unbalanced badge,
- affected account count,
- missing account count,
- account health indicators.

## Performance Strategy

The tab interface hides inactive sections so the user does not visually process the whole LC at once. The next step is to move heavy voucher/grid tabs to AJAX partial loading if Eng data volumes become large.

## Safety

Still disabled:

- save,
- delete,
- posting,
- account creation,
- voucher rebuild,
- actual report generation.

No database writes were added.
