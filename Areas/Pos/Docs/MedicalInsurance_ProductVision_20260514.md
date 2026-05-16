# Medical Insurance Product Vision - 2026-05-14

## Product Positioning

Medical Insurance is a new sellable module, not a migrated VB6 form.

The module is designed as an Egyptian enterprise medical-insurance operating platform for HR, payroll, accounting, branch operators, and management.

## Source Ownership

- HR/business ownership: Main Original ERP architecture.
- POS/Kishny role: operational visibility, quick follow-up, and branch-friendly access.
- Payroll role: consumes approved insurance result only.
- Accounting role: preview/simulation now; production posting remains protected.

## Client Value

- Centralizes employee and family insurance status.
- Shows employee deduction and company contribution clearly.
- Makes unpaid installments and renewals visible before payroll.
- Gives branch/POS operators a safe read-only operating view.
- Gives MainErp administration the full setup/reporting surface.
- Explains accounting impact without enabling unsafe production posting.

## Product Experience

POS now presents a focused operational workspace:

- Executive hero section for medical insurance operations.
- KPI cards for insured employees, deductions, company contribution, renewals, overdue installments, and inactive policies.
- Employee insurance profile cards with provider, plan, branch, department, family count, payroll-linked state, and overdue state.
- Accounting flow preview: employee deduction, company contribution, and provider payment.
- Clear protected-state messaging: POS is visibility and follow-up only.

MainErp remains the place for:

- Provider and plan administration.
- HR approvals.
- Advanced reporting.
- Payroll preview integration.
- Accounting review.

## UI Decisions

- Avoided dense table-only UI.
- Used product-style cards and status badges.
- Kept POS lightweight and operational.
- Used financially understandable labels.
- Kept protected workflows intentional, not “disabled-looking”.

## Future Roadmap

- Full provider logos and medical network directory.
- Attachment viewer for insurance cards and policy documents.
- Renewal workflow with approvals.
- Installment payment collection workflow.
- MainErp approval matrix.
- Production posting only after business sign-off and accounting safety validation.
