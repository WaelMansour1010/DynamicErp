# MainErp Finance Confidence Review - 2026-05-14

## Scope

Phase S focused on finance confidence, not production posting activation.

Databases used:

- `Dania`: payroll protected posting, accounting replay, voucher-row residue checks, branch/account impact validation.
- `Eng`: MainErp operational UI/runtime smoke for receipt/payment, project extract, and LC finance screens.

## Accounting Readability Improvements

Implemented finance-facing trace/readability improvements:

- Receipt details (`Cashing/Details`): added a voucher trace panel showing source document, NoteID/serial, party/account, branch/project, accounting line count, debit/credit/difference, linked notes, and allocation rows.
- Payment details (`Payments/Details`): added the same trace pattern for سند صرف.
- Project extract details: added a financial trace strip connecting extract header, customer/project, voucher line count, debit/credit totals, and balance status.
- LC edit: added a finance trace summary showing posted-voucher state, source LC, branch/project, bank/box, linked account, margin value, and expense value.
- Payroll salary run: added a finance review section explaining the dry-run -> review -> protected test write -> cleanup-by-batch lifecycle.

Removed/softened client-facing debug wording where touched:

- Replaced VB6-facing terminology in receipt/payment report labels with general report wording.
- Replaced LC migration/debug wording with production-safe finance wording.
- Replaced protected LC button alert text with a controlled-action explanation.

## Finance Walkthrough Findings

### Receipts and Payments

Eng runtime smoke:

- `/MainErp/Payments/Details/222100`: opened, title `تفاصيل سند صرف`, trace panel visible, no console errors.
- `/MainErp/Cashing/Details/221554`: opened, title `تفاصيل سند قبض`, trace panel visible, no console errors.

Finance readability result:

- Finance can now see the document source, accounting impact, linked allocations, related notes, and journal-entry drilldowns from one screen.
- Existing journal-entry links remain available through `JournalEntries/DetailsByVoucher`.

### Project Extracts

Eng runtime smoke:

- `/MainErp/ProjectExtracts/Details/3502`: opened, title `مستخلص مشروع`, finance trace visible, no console errors.

Finance readability result:

- Extract review now connects project/customer, extract totals, detail lines, deductions/VAT/retention, previous payments, and accounting-voucher balance status.

### Letters of Credit

Eng runtime smoke:

- `/MainErp/LC/Edit/197`: opened, title `تعديل اعتماد مستندي`, finance trace visible, no console errors.

Finance readability result:

- LC edit now shows source LC, posted-voucher state, branch/project, bank/box, and account impact before users enter protected actions.

### Payroll Replay and Test Posting

Dania runtime result:

- Salary run dry-run used `Dania`.
- Dry-run returned 4 Notes, 940 voucher lines, balance `0.00`.
- UI warning confirmed dry-run creates no `Notes` or `DOUBLE_ENTREY_VOUCHERS` rows.
- Protected generate and cleanup were validated separately in `MainErp_ProtectedPostingValidation_20260514.md`.

## Databases and Route Notes

- Dania receipt/payment detail routes are not used for UI validation because Dania is missing `dbo.usp_DynamicErpVoucher_Header`; this is a database capability gap, not a UI regression.
- Eng contains the required receipt/payment stored procedures and was used for the operational UI smoke pass.
- Dania remains the correct database for payroll/accounting replay and protected posting validation.

## Unresolved Accounting Concerns

- Dania legacy voucher rows use `Value` plus `Credit_Or_Debit` as the meaningful amount/direction columns; `depet_value` and `credit_value` are mostly empty on sampled modern rows. Any new financial dashboard SQL must respect this legacy shape.
- Several legacy voucher lines use `Credit_Or_Debit = 0` for credit-like rows. Review queries should classify direction according to the repository logic, not assume only `1/2`.
- Dania branch/project/department dimension coverage is visible, but project dimension was `0` for the tested payroll batch. This may be valid for salary runs, but finance should approve whether payroll should ever allocate by project.
- Production payroll posting remains disabled. Protected posting is acceptable only for controlled Dania review.

## Screenshots Checklist

- [x] Eng payment details trace panel verified.
- [x] Eng cashing details trace panel verified.
- [x] Eng project extract details finance trace verified.
- [x] Eng LC edit finance trace verified.
- [x] Dania payroll test-posting panel dry-run/generate/cleanup verified.

## Files Changed

- `Areas/MainErp/Views/Cashing/Details.cshtml`
- `Areas/MainErp/Views/Payments/Details.cshtml`
- `Areas/MainErp/Views/ProjectExtracts/Details.cshtml`
- `Areas/MainErp/Views/LC/Edit.cshtml`
- `Areas/MainErp/Views/EmployeePayroll/_EmployeePayrollSalaryRun.cshtml`

## Build and Runtime Status

- Build: passed with MSBuild Debug / Any CPU.
- Browser console: no console errors in the Eng finance smoke pass.
- Protected posting residue after cleanup: zero exact `[TEST_PAYROLL_POSTING]` Dania notes.

