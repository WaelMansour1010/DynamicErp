# Medical Insurance Alerts Center - 2026-05-14

## Purpose

The alerts center converts medical insurance from passive data into an operating workflow.

## Current Alerts

- Renewal due.
- Expired coverage.
- Overdue installment.
- Missing payroll linkage.

## Alert Visuals

- Danger alerts for expired coverage and overdue installments.
- Warning alerts for renewal and payroll linkage review.
- Employee and branch context on each alert.
- Due date where available.

## Operational Flow

1. Branch/POS user sees the alert.
2. User confirms employee/provider/plan context.
3. User escalates changes to MainErp administration.
4. HR/finance handles approval, renewal, or payment action.

## Safety

POS remains read-only. Alerts do not create accounting entries, do not change payroll, and do not update employee insurance status.

## Remaining Roadmap

- Alert acknowledgement.
- Assignment to HR/finance users.
- SLA aging.
- Missing document detection after attachment workflow is fully wired.
- Renewal approval workflow in MainErp.
