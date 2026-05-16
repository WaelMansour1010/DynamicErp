# MainErp Protected Posting Validation - 2026-05-14

## Scope

Validated protected payroll test posting on `Dania`.

This validation did not enable production posting and did not bypass protected posting rules.

## Protected Posting Controls Observed

Repository/controller behavior:

- Dry-run endpoint: `EmployeePayroll/PayrollTestPostingDryRun`
- Generate endpoint: `EmployeePayroll/GeneratePayrollTestPosting`
- Cleanup endpoint: `EmployeePayroll/CleanupPayrollTestPosting`
- Allowlisted database setting: `PayrollTestPostingAllowedDatabases = Dania`
- Confirmation phrase: `POST TO TEST`
- Test marker format: `[TEST_PAYROLL_POSTING] Batch=<guid>`
- Writes are limited to marked test `Notes`, marked `DOUBLE_ENTREY_VOUCHERS`, and `MainErpPayrollTestPostingAudit`.
- Cleanup deletes only voucher rows joined to marked notes for the supplied batch, then deletes the matching marked notes and updates the audit row.

## Runtime Test

Database:

- `Dania`

Screen:

- `/MainErp/EmployeePayroll/SalaryRun`

Period:

- 2026 / 5

Dry-run result:

- Database badge: `Dania`
- Allowlist result: `Allowed test database`
- Notes: `4`
- Voucher lines: `940`
- Balance: `0.00`
- UI warning: dry-run only, no `Notes` or `DOUBLE_ENTREY_VOUCHERS` rows created.

Generated protected batch:

- Batch: `0d7b9e23-3b7f-4654-a2c0-00b6f8f8565c`
- Notes created: `4`
- Voucher lines created: `940`
- Debit total: `3,159,983.00`
- Credit total: `3,159,983.00`
- Balance: `0.00`
- Audit status after generate: `Active`
- Notes were visibly marked with `[TEST_PAYROLL_POSTING] Batch=0d7b9e23-3b7f-4654-a2c0-00b6f8f8565c - Protected salary replay test posting. Production posting remains disabled.`

Generated note sample:

- NoteIDs: `197821`, `197822`, `197823`, `197824`
- NoteType: `66`
- Branches: `10`, `12`, `11`, `16`
- PayrollYear: `2026`
- PayrollMonth: `5`

Dimension sample:

- Branch 12 / Department 5: 396 lines
- Branch 10 / Department 9: 337 lines
- Branch 12 / Department 4: 39 lines
- Branch 11 / Department 9: 31 lines
- ProjectId was `0` in sampled grouped dimensions.

Cleanup result:

- UI message: protected test posting batch was cleaned by batch id only.
- Batch notes after cleanup: `0`
- Batch voucher lines after cleanup: `0`
- Exact `[TEST_PAYROLL_POSTING]` residue after cleanup: `0`
- Audit status after cleanup: `Cleaned`
- CleanedAt: `2026-05-15 08:46:01`

## Database Integrity Checks

Pre/post residue:

- Exact marker query after cleanup returned zero notes.
- Exact batch query after cleanup returned zero notes and zero voucher lines.

Audit row:

- Batch `0d7b9e23-3b7f-4654-a2c0-00b6f8f8565c`
- DatabaseName: `Dania`
- CleanupStatus: `Cleaned`
- NotesCount: `4`
- VoucherLinesCount: `940`
- DebitTotal: `3,159,983.00`
- CreditTotal: `3,159,983.00`
- Balance: `0.00`

Important query note:

- SQL `LIKE '%[TEST_PAYROLL_POSTING]%'` is unsafe because square brackets are pattern syntax in SQL Server. Exact residue checks used `CHARINDEX(N'[TEST_PAYROLL_POSTING]', Remark) > 0`.

## Finance Readability Findings

Positive:

- The UI explains dry-run, protected generate, and cleanup in finance-friendly steps.
- The generated notes are visibly test-only.
- Generated batch totals are balanced.
- Batch cleanup leaves no exact test marker residue.
- Branch and department grouping is visible.

Needs finance sign-off:

- Payroll project dimension was `0` in the tested batch. Finance must confirm whether this is expected for Dania payroll.
- Dania legacy accounting rows store direction mainly through `Value` plus `Credit_Or_Debit`; report writers must avoid assuming `depet_value` and `credit_value` are populated.
- Production posting remains blocked until replay parity, account mapping, and dimension policy are approved.

## Screenshot Checklist

- [x] Dry-run panel showing Dania and allowlist pass.
- [x] Generated batch warning with `[TEST_PAYROLL_POSTING] Batch=...`.
- [x] Cleanup message.
- [x] SQL post-cleanup zero-residue result.
- [x] Audit row showing `Cleaned`.

## Files Changed

- `Areas/MainErp/Views/EmployeePayroll/_EmployeePayrollSalaryRun.cshtml`
- `Areas/MainErp/Views/Cashing/Details.cshtml`
- `Areas/MainErp/Views/Payments/Details.cshtml`
- `Areas/MainErp/Views/ProjectExtracts/Details.cshtml`
- `Areas/MainErp/Views/LC/Edit.cshtml`

