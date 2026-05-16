# Medical Insurance Accounting Flow - 2026-05-14

## Purpose

This document describes the client-facing accounting model for medical insurance.

The current phase shows accounting simulation/preview only. It does not enable production posting.

## Monthly Employee Deduction

Example:

- Employee insurance share: 450.00

Suggested entry:

- Debit: Employee Receivable / Salary Deduction
- Credit: Insurance Payable

Meaning:

The employee share is deducted through payroll and becomes payable to the insurance provider.

## Company Contribution

Example:

- Company contribution: 1,000.00

Suggested entry:

- Debit: Medical Insurance Expense
- Credit: Insurance Payable

Meaning:

The company recognizes its medical-insurance cost and the provider payable increases.

## Payment To Provider

Example:

- Provider payment: 1,450.00

Suggested entry:

- Debit: Insurance Payable
- Credit: Cash / Bank

Meaning:

The company settles the provider payable from cash or bank.

## Safety Rules

Still blocked in production:

- Notes creation.
- DOUBLE_ENTREY_VOUCHERS creation.
- Payroll posting.
- Salary payment posting.
- SendTopost replacement.
- Allocation rebuild.

Allowed for this phase:

- Operational preview.
- Accounting simulation.
- Protected test posting from the payroll module only when test-mode conditions are met.

## How To Explain To Client

Use this wording:

“The system already understands the financial effect of medical insurance. We are showing the accounting flow safely before enabling production posting, so finance can approve the routing and controls before any real ledger impact.”
