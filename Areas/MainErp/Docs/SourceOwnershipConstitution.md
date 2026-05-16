# Source Ownership Constitution - MainErp

This document is a controlling architecture rule for MainErp migration and modernization work.

## Core Rule

`MAIN_ORIGINAL_SOURCE = F:\Source Code\SatriahMain`

Main Original VB6 is the general source of truth and the primary business reference for MainErp.

Kishny is not a general business reference for MainErp.

## Main Original Ownership

Use Main Original VB6 as the source of truth for:

- employees
- departments
- jobs
- branches
- HR hierarchy
- payroll administration workflow
- medical insurance workflow
- advances
- vacations
- sick leave
- employee allocations
- changed components as HR/admin workflow
- approvals
- HR statuses
- attendance-related HR rules
- insurance-related employee rules
- employee operational structure
- accounting/ERP workflows unless explicitly marked as Kishny/POS-specific

## Kishny Limitation

`KISHNY_SOURCE = F:\Source Code\SatriahMain\Cayshny`

Kishny may be used only for:

- cards
- POS workflows
- Kishny-specific operational cases
- payroll snapshot structure, only where required for replay compatibility
- `emp_salary`
- `Comp1..Comp40`
- historical salary replay tracing
- accounting replay tracing
- `Notes` / `DOUBLE_ENTREY_VOUCHERS` linkage
- `AddNewDev` posting mechanics

Do not use Kishny as a Human Resources business reference unless explicitly instructed.

## Medical Insurance Rule

Medical insurance is an HR domain.

Main Original owns:

- provider/category/status behavior
- enrollment/exclusion
- HR approval
- employee insurance profile
- insurance administration

Payroll may consume only the approved HR insurance result when calculating salary impact or replaying historical payroll snapshots.

## Conflict Rule

If Main Original and Kishny differ:

- Main Original wins for MainErp business logic.
- Kishny wins only for POS/cards/Kishny-specific cases, or payroll replay mechanics explicitly required for compatibility.
- Any exception must be documented as technical runtime compatibility, not business ownership.

## Final Target

MainErp must be:

```text
Modernized Main Original ERP/HR architecture
with payroll replay compatibility layered underneath where needed
```

It must not become:

```text
Kishny HR or Kishny business workflows migrated into MainErp
```
