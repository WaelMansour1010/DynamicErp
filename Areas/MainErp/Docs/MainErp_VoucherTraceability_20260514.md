# MainErp Voucher Traceability - 2026-05-14

## Objective

Make finance users understand where accounting entries come from, which document generated them, and which accounts/branches/projects/departments are affected.

## Traceability Added

### Receipts - سند قبض

Route tested on Eng:

- `/MainErp/Cashing/Details/221554`

Trace panel now shows:

- source document type: سند قبض
- NoteID and serial
- party and linked account display
- branch and project display
- accounting line count
- debit total, credit total, and difference
- related note count
- allocation row count

Navigation retained:

- document details
- allocation details
- accounting entries
- report/print action
- related notes log
- `JournalEntries/DetailsByVoucher` for each accounting voucher line

### Payments - سند صرف

Route tested on Eng:

- `/MainErp/Payments/Details/222100`

Trace panel now shows:

- source document type: سند صرف
- NoteID and serial
- party and linked account display
- branch and project display
- accounting line count
- debit total, credit total, and difference
- related note count
- allocation row count

Navigation retained:

- document details
- allocation details
- accounting entries
- report/print action
- related notes log
- `JournalEntries/DetailsByVoucher` for each accounting voucher line

### Project Extracts

Route tested on Eng:

- `/MainErp/ProjectExtracts/Details/3502`

Trace panel now shows:

- source document: project extract id
- NoteID and serial
- customer and project
- voucher line count
- debit and credit totals
- review status and balance difference

The screen already exposes:

- extract totals
- previous payments
- deductions
- VAT
- retention
- net payable
- detail lines
- accounting voucher lines
- `JournalEntries/DetailsByNote` links

### LC

Route tested on Eng:

- `/MainErp/LC/Edit/197`

Finance trace now shows:

- source LC id and number
- posted voucher status
- branch and project
- bank and box
- LC account code
- expected margin impact
- expected expense impact

Protected actions remain controlled; delete/rebuild actions are not opened from the UI.

### Payroll Replay

Route tested on Dania:

- `/MainErp/EmployeePayroll/SalaryRun`

Trace/readability now emphasizes:

- dry-run means no writes
- finance reviews totals and grouping first
- protected generate writes marked test-only rows
- cleanup removes by exact batch marker

## Cross-Document Visibility

Current visible paths:

- receipt/payment -> accounting lines -> journal voucher details
- project extract -> voucher lines -> journal entry by note
- LC edit -> expected account impact and protected posted-voucher state
- payroll run -> replay summary -> protected test posting accounts/dimensions -> cleanup by batch

Remaining recommended navigation work:

- Add explicit links from payroll protected posting audit rows to generated NoteIDs before cleanup.
- Add LC read-only voucher drilldown once the LC posted-voucher IDs are consistently exposed in the edit model.
- Add receipt/payment links from allocation rows to their source document when the source type can be reliably mapped.

## Runtime Findings

- Eng finance routes opened without raw server errors.
- Browser console showed zero errors during the tested Eng routes.
- Dania receipt/payment details cannot be used for UI smoke because Dania lacks `dbo.usp_DynamicErpVoucher_Header`.
- Dania payroll protected posting path is functional and documented in the protected-posting validation report.

## Screenshot Checklist

- [x] Receipt trace panel.
- [x] Payment trace panel.
- [x] Project extract trace panel.
- [x] LC finance trace.
- [x] Payroll protected posting review panel.

## Remaining Traceability Risks

- Legacy voucher direction columns in Dania are inconsistent across eras; finance dashboards must use repository-approved direction logic.
- Some payroll voucher dimensions are department-heavy and project-light. Finance should approve expected project allocation policy.
- Production posting must remain blocked until finance signs off on replay parity and account mapping.

