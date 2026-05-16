# Medical Insurance Workflow - 2026-05-14

## Operational Model

Medical Insurance is split between MainErp administration and POS operational visibility.

## MainErp Workflow

1. Create insurance provider.
2. Create insurance plan.
3. Define monthly cost.
4. Define employee contribution.
5. Define company contribution.
6. Configure start/end dates, payroll visibility, family eligibility, and renewal policy.
7. Enroll employee and family members.
8. Review payroll impact.
9. Review accounting preview.
10. Approve or suspend coverage.

## POS Workflow

1. Search employee by code/name/provider/plan.
2. View insurance state: active, renewal due, suspended, expired.
3. Confirm provider and plan.
4. Review monthly employee deduction.
5. Review company contribution.
6. See overdue installments.
7. Escalate changes to MainErp administration.

POS does not administer insurance plans and does not post accounting.

## Status Model

- Active: current active policy.
- Renewal Due: active policy ending within the configured renewal window.
- Suspended: inactive subscription before/without expiry.
- Expired: policy end date passed.
- Payroll-linked: insurance deduction exists in payroll deduction tables.
- Overdue: unpaid installment due date has passed.

## Demo Data

Demo data script:

`Areas/Pos/Sql/84_POS_MedicalInsurance_ProductDemo_Dania.sql`

Prerequisite script:

`Areas/MainErp/Sql/06_EmployeePayroll_MedicalInsurance.sql`

The demo script creates realistic providers, plans, dependents, installments, and subscriptions in Dania without accounting posting.

## Remaining Gaps

- Production posting remains intentionally blocked.
- Provider network directory is a product roadmap item.
- Attachment file upload/viewer is scaffolded as a data concept, not fully wired to document storage yet.
- MainErp approval workflow can be expanded after client workflow sign-off.
