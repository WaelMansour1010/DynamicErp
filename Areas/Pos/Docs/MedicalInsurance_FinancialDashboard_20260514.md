# Medical Insurance Financial Dashboard - 2026-05-14

## Purpose

The financial dashboard makes insurance cost understandable for owners, HR directors, branch managers, and finance users.

## Financial Widgets

- Employee monthly contribution.
- Company monthly contribution.
- Total provider payable.
- Unpaid installments.
- Branch cost comparison.
- Department cost comparison.
- Top expensive branches.
- Top expensive departments.
- Simplified accounting flow preview.

## Accounting Preview Model

The screen explains the expected accounting flow without enabling production posting:

- Employee deduction: Debit employee receivable or salary deduction, Credit insurance payable.
- Company contribution: Debit medical insurance expense, Credit insurance payable.
- Provider payment: Debit insurance payable, Credit cash/bank.

## Safety Position

- POS shows read-only operational visibility.
- MainErp owns setup, approval, and reporting.
- Production posting remains protected.
- Test/demo posting, if used later, must remain explicit, audited, and test-database-only.

## Client-Selling Rationale

The client sees that insurance is not just an HR note. It has payroll impact, branch cost impact, and clear accounting consequences.

## Roadmap

- Provider payable aging.
- Monthly cost trend.
- Branch budget comparison.
- Exportable executive summary.

