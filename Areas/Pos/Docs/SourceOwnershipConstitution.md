# Source Ownership Constitution - POS

This document is a controlling architecture rule for POS migration, hardening, and MainErp/POS integration.

## Core Rule

`MAIN_ORIGINAL_SOURCE = F:\Source Code\SatriahMain`

Main Original VB6 is the general ERP and HR source of truth.

`KISHNY_SOURCE = F:\Source Code\SatriahMain\Cayshny`

Kishny is a source of truth only for cards, POS workflows, and Kishny-specific operational cases.

## Kishny Ownership In POS

Use Kishny only for:

- POS transaction behavior
- Kishny card workflows
- POS closing and cash/card operational reports
- POS-specific boxes/terminals/wallet behavior
- POS invoice behavior
- POS stock-transfer behavior when it is specifically a Kishny POS workflow
- POS SQL/reporting behavior historically implemented in Kishny

## Main Original Ownership

Use Main Original for:

- general ERP behavior
- HR business logic
- employees
- departments
- jobs
- branches as business master data
- medical insurance workflow
- advances
- vacations
- sick leave
- employee allocations
- approvals
- attendance-related HR rules
- insurance-related employee rules

## Payroll Runtime Exception

Kishny may be used as a technical/runtime reference only where payroll replay compatibility explicitly requires:

- `emp_salary`
- `Comp1..Comp40`
- historical salary replay tracing
- accounting replay tracing
- `Notes` / `DOUBLE_ENTREY_VOUCHERS` linkage
- `AddNewDev` posting mechanics

This exception does not make Kishny an HR business reference.

## Conflict Rule

If Main Original and Kishny differ:

- Main Original wins for general ERP/HR/business behavior.
- Kishny wins only for POS/cards/Kishny-specific operational behavior.
- Any use of Kishny outside POS/cards/special Kishny cases requires explicit instruction and documentation.

## Final Target

POS must remain a Kishny-compatible POS module where required, but it must not redefine MainErp or HR business ownership.
