# Final Client Trial Readiness - 2026-05-14

## Readiness Summary

The HR/payroll demo surface is ready for a controlled client trial as a protected preview and test-posting experience.

It is not ready for production payroll posting.

## What The Client Can Test

- Employee search and profile visibility.
- Salary Run preview.
- Payroll component breakdown.
- Legacy snapshot vs calculated preview visibility.
- Accounting replay readability.
- Protected Test Posting dry-run.
- Protected Test Posting generation on `Dania`.
- Batch cleanup by `TestPostingBatchId`.
- Medical-insurance visibility/fallback behavior.
- Project Extracts and LC operational screens from the previous stabilization phases.
- POS read-only employee/payroll/insurance visibility.

## What Remains Protected

- Production payroll posting.
- Salary payment posting.
- Production `Notes` creation.
- Production `DOUBLE_ENTREY_VOUCHERS` creation.
- `SendTopost` replacement.
- Allocation rebuild.
- Any posting from calculated preview rows that has not passed parity approval.

## Recommended Demo Sequence

1. Open MainErp dashboard and explain the Main Original business ownership rule.
2. Open Employees and show centralized employee/insurance visibility.
3. Open Salary Run.
4. Run salary preview for `2026/03` on `Dania`.
5. Explain `LegacySnapshot` vs `Calculated preview`.
6. Open accounting replay and show account/branch distribution.
7. Run Protected Test Posting dry-run.
8. Generate Test Posting after password and `POST TO TEST`.
9. Show `TestPostingBatchId`.
10. Cleanup by batch id and show zero remaining marked rows.
11. Open POS visibility screens and emphasize read-only operational access.

## Pilot Rules

- Use `Dania` only for test posting.
- Change `PayrollTestPostingPassword` before any client-facing shared environment.
- Keep every demo-generated batch id in the meeting notes.
- Cleanup every generated test batch before closing the session.
- Do not demo production posting as enabled.

## Business Sign-Off Checklist

| Item | Status |
| --- | --- |
| HR ownership follows Main Original rule | PASS |
| Kishny limited to payroll/accounting runtime replay | PASS |
| Salary preview visible | PASS |
| Accounting replay visible | PASS |
| Test posting allowlist enforced | PASS |
| Password and phrase required | PASS |
| Batch audit created | PASS |
| Cleanup by batch tested | PASS |
| Production posting remains blocked | PASS |
| Payroll parity still requires finance approval | OPEN |

## Next Phase Recommendation

Move into a controlled finance review workshop:

- select 3 to 5 real salary periods;
- compare replay/test-posted rows against finance expectations;
- approve or reject remaining 0.33 balance differences and similar small historical deviations;
- define formal production posting acceptance criteria;
- only then design production posting activation.
