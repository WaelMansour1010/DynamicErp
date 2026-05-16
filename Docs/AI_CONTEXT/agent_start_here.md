# AGENT_START_HERE.md

## DynamicErp / MainErp / Kishny POS — Engineering Constitution

> This file is mandatory reading before making any code, database, UI, or architecture change in this repository.
>
> The goal is to stop random migration, duplicated screens, broken business logic, inconsistent UI, and unsafe database changes.

---

# 1. Project Identity

This repository is part of a long-term migration from legacy VB6 ERP/POS systems into a modern ASP.NET MVC web platform.

The system is not a greenfield project.

It contains years of business logic, accounting behavior, voucher numbering, reporting rules, permissions, and customer-specific workflows.

Therefore, any change must preserve existing business behavior unless the task explicitly says otherwise.

---

# 2. Main Systems

## 2.1 Main Original VB6

The legacy main VB6 project is the primary source of truth for general ERP logic.

It is the reference for:

- Accounting
- Inventory
- Purchases
- Sales logic outside Kishny POS
- Customers and suppliers
- HR and payroll
- Projects
- Properties and contracts
- Reports
- General ERP business behavior

## 2.2 Kishny VB6

The Kishny VB6 project is a specialized POS/card/teller reference only.

It is the reference for:

- Kishny POS sales
- Card operations
- KYC
- Teller workflows
- Cash in / cash out
- Violations
- Card/token behavior
- POS receipt behavior

Do not use Kishny as a reference for general ERP, HR, inventory, accounting, or enterprise modules unless the task explicitly says so.

## 2.3 DynamicErp Web

DynamicErp Web is the new web platform.

It may contain:

- `/Areas/Pos` for Kishny POS Web
- `/Areas/MainErp` for Main ERP migration
- Shared modules used by both
- General web modules already existing in the original web application

The target is one coherent enterprise platform, not isolated duplicated systems.

---

# 3. Most Important Rule

## Do not migrate randomly.

Migration must be domain-based, not screen-based.

Wrong approach:

- Move one random screen today
- Move another unrelated report tomorrow
- Create duplicate versions for POS and MainErp
- Redesign logic without understanding VB6 behavior

Correct approach:

- Identify the business domain
- Identify the source of truth
- Decide whether it is Shared, POS-only, MainErp-only, or Legacy-only
- Extract business rules before coding
- Implement in the proper module
- Test permissions, saving, reports, printing, and accounting effects

---

# 4. Module Ownership Rules

## 4.1 POS Area

`/Areas/Pos` is for Kishny POS-related workflows only.

Allowed examples:

- POS sales
- KYC
- Card issuance
- Cash in / cash out
- Violations
- Teller screens
- POS closing reports
- POS receipt printing

Do not put general ERP screens inside POS.

## 4.2 MainErp Area

`/Areas/MainErp` is for enterprise ERP migration from the Main Original VB6 system.

Allowed examples:

- Accounting
- Inventory
- HR
- Payroll
- Purchasing
- Customers
- Suppliers
- Properties
- Projects
- Enterprise reports

## 4.3 Shared Modules

If a screen or service is used by both POS and MainErp, it must be implemented once as a shared module.

Examples of shared candidates:

- Items
- Customers
- Suppliers
- Branches
- Warehouses
- Users
- Permissions
- Employees
- Chart of accounts
- System settings
- Attachments
- Print templates

Do not create duplicate screens such as:

- POS Items screen
- MainErp Items screen
- Another general Items screen

There should be one shared implementation with permissions and context controlling behavior.

---

# 5. Source of Truth Rules

Before changing or migrating a feature, identify the correct reference.

## Use Main Original VB6 for:

- General ERP logic
- Accounting journals
- Inventory costing
- Purchasing
- HR/payroll
- Customers/suppliers
- Properties/projects
- General reports

## Use Kishny VB6 for:

- POS sales behavior
- KYC behavior
- Card/token behavior
- Cash in/out
- Teller logic
- POS receipt logic

If the task does not specify the source, inspect both only when needed, but do not assume Kishny is the general reference.

---

# 6. Database Constitution Summary

Detailed rules must live in `DATABASE_CONSTITUTION.md`, but these rules are mandatory immediately.

## 6.1 SQL Server Version

All database changes must be compatible with SQL Server 2012 unless explicitly stated otherwise.

## 6.2 Stored Procedures

Stored procedure changes must be written as:

```sql
IF OBJECT_ID('dbo.ProcedureName', 'P') IS NOT NULL
    DROP PROCEDURE dbo.ProcedureName;
GO

CREATE PROCEDURE dbo.ProcedureName
AS
BEGIN
    SET NOCOUNT ON;
    -- logic
END
GO
```

Do not use `ALTER PROCEDURE` for committed migration scripts unless the task explicitly asks for it.

## 6.3 Schema Changes

New tables, columns, or indexes must follow the approved project migration/update mechanism.

For legacy/Main Original-related schema work, use the established `UpdateDatabase` / `Update30Follow` style where applicable.

For POS Web SQL work, keep SQL scripts under the POS SQL folder unless the task explicitly says otherwise.

## 6.4 No Guessing

Never guess table names, column names, stored procedure names, or relationships.

Inspect the schema first.

## 6.5 Accounting and Voucher Logic

Voucher serials, journal entries, transaction IDs, costing, and stock effects are critical.

Do not rewrite them casually.

Preserve historical behavior unless the task explicitly requires a controlled correction.

---

# 7. Coding Rules Summary

Detailed rules must live in `CODING_STANDARDS.md`, but these rules are mandatory immediately.

## 7.1 No Business Logic in Views

Views must not contain business rules.

Allowed in views:

- Rendering
- Basic UI binding
- Client-side interaction

Not allowed in views:

- Accounting logic
- Save logic
- Permission decisions
- SQL logic
- Voucher numbering
- Costing decisions

## 7.2 Use Services and Repositories

Business logic should be placed in services.

Database access should be placed in repositories or approved data-access classes.

Controllers should coordinate the request, not carry the whole business process.

## 7.3 Do Not Duplicate Logic

If logic is shared, extract it.

Do not copy-paste business logic between POS and MainErp.

## 7.4 Preserve Existing Behavior

When migrating from VB6, the first target is functional equivalence.

UI may improve, but business behavior must not change silently.

---

# 8. UI/UX Constitution Summary

Detailed rules must live in `UI_UX_CONSTITUTION.md`, but these rules are mandatory immediately.

## 8.1 One Visual System

The application must look like one system.

Do not create isolated screens with unrelated colors, spacing, buttons, tables, or layouts.

## 8.2 RTL First

Arabic RTL support is mandatory.

Do not break Arabic labels, alignment, printing direction, or input flow.

## 8.3 Theme Consistency

If the system is light, all major layout parts should be light.

If the system is dark, all major layout parts should be dark.

Do not mix a dark sidebar with unrelated light pages unless this is part of an approved theme system.

## 8.4 Enterprise Screens

ERP screens must be designed for power users.

Expected features where relevant:

- Search
- Filters
- Date ranges
- Export
- Totals
- Clear save/cancel actions
- Validation messages
- Permission-aware buttons

## 8.5 POS Screens

POS screens must be fast and simple.

Expected behavior:

- Minimal clicks
- Clear defaults
- Large readable fields
- Locked branch/store/cashbox where appropriate
- No unnecessary combos
- No heavy loading on open

---

# 9. Permissions and Authentication Rules

Permissions are not cosmetic.

Any action button must be protected both:

- In the UI
- On the server

Do not rely on hiding buttons only.

Shared screens must use the same authenticated session/context where possible.

A shared screen must not redirect the user to a different login or database unexpectedly.

If a shared screen asks for login again, investigate authentication/session/context configuration before adding workarounds.

---

# 10. Migration Playbook Summary

Detailed rules must live in `MIGRATION_PLAYBOOK.md`, but these steps are mandatory for every migrated screen.

Before coding:

1. Identify the business domain.
2. Identify the correct VB6 source of truth.
3. Identify whether the target is POS, MainErp, Shared, or Legacy-only.
4. Inspect the original form, save logic, validation, permissions, reports, and printing.
5. Inspect related database objects.
6. Write down the business rules.
7. Implement incrementally.
8. Test read, save, edit, delete, permissions, reports, printing, and accounting impact.

Never migrate only the visible UI and ignore the hidden save/report/accounting logic.

---

# 11. Reporting and Printing Rules

Reports and print layouts must preserve the original meaning and customer expectations.

Do not invent a new report layout when the task says to match an existing Crystal Report or VB6 print behavior.

For POS/KYC/receipts, printing direction, logos, spacing, and Arabic formatting are part of the requirement.

---

# 12. Performance Rules

The system serves real users and large databases.

Always consider:

- Large transaction tables
- Many concurrent users
- Slow reports
- Connection pool pressure
- Index usage
- Avoiding full-table scans
- Avoiding heavy dropdown loading
- Avoiding repeated initialization calls

For high-use screens, test performance before considering the task complete.

---

# 13. Release Rules

Do not treat code changes and database changes as separate unrelated work.

Every completed task should clearly state:

- Changed files
- Changed SQL scripts
- Required database updates
- Testing performed
- Deployment notes
- Risks or pending items

Production should receive controlled, reviewed, testable changes only.

---

# 14. Forbidden Behaviors

The following are not allowed unless the task explicitly authorizes them:

- Randomly creating duplicate screens
- Changing accounting behavior without analysis
- Changing voucher serial logic casually
- Using Kishny as a general ERP reference
- Putting general ERP screens under POS
- Adding SQL that is not SQL Server 2012-compatible
- Adding business logic inside views
- Ignoring permissions on server-side actions
- Changing UI theme inconsistently
- Replacing old business behavior just because a new design seems cleaner
- Guessing schema names
- Ignoring printing/reporting behavior during migration

---

# 15. Mandatory Agent Checklist

Before making changes, the agent must answer internally:

1. What business domain is this task in?
2. Is this POS, MainErp, Shared, or Legacy-only?
3. What is the source of truth?
4. Is there an existing screen/service that should be reused?
5. Will this duplicate an existing screen?
6. Are there database changes?
7. Are stored procedures SQL Server 2012-compatible and DROP+CREATE?
8. Are permissions enforced server-side?
9. Does this affect accounting, stock, serials, or costing?
10. Does this affect printing or reports?
11. Does the UI follow the shared design system?
12. What tests prove this is safe?

If the agent cannot answer these questions, it must inspect the codebase and database before editing.

---

# 16. Immediate Current Strategic Direction

The current strategic direction is:

1. Stabilize Kishny POS Web.
2. Build and enforce shared architecture.
3. Move shared master screens once, not multiple times.
4. Fix authentication/session behavior for shared screens.
5. Continue migration by business domain priority.
6. Avoid random screen-by-screen migration.
7. Preserve VB6 business behavior unless a task explicitly defines a controlled redesign.

---

# 17. Final Principle

This project is an enterprise migration, not a UI rewrite.

Success means:

- Stable production
- Preserved business logic
- Unified architecture
- Shared screens where appropriate
- Consistent UI
- Safe database evolution
- Clear releases
- Less dependency on TSPlus/RDP over time

Any change that moves the project away from these goals should be rejected or redesigned.

