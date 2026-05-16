# SYSTEM_ARCHITECTURE.md

## DynamicErp Solution Architecture Constitution

> This document defines the official architecture boundaries between the original web application, MainErp, Kishny POS, shared modules, and legacy VB6 sources.
>
> Any agent or developer must read this file before changing project structure, routes, authentication, shared screens, database logic, or migration direction.

---

# 1. Core Principle

DynamicErp is not a single simple web app.

It is a main web solution that hosts multiple business areas and migration tracks.

The solution contains:

1. The original/main web application.
2. Kishny POS Web under the POS area.
3. MainErp migration under the MainErp area.
4. Shared modules used by more than one area.
5. Legacy VB6 projects used as business references.

These parts must not be mixed randomly.

---

# 2. The Original Web Application

## 2.1 Definition

The original web application is the main existing web project and the main solution host.

It is independent from POS and MainErp as a business application.

It may contain existing controllers, views, routes, configuration, authentication, layout, and business features that existed before the POS/MainErp migration work.

## 2.2 Very Important Rule

The original web application must not be treated as POS.

The original web application must not be treated as MainErp.

It is the main web host/solution shell, but it has its own independent business scope.

## 2.3 Allowed Responsibilities

The original web application may own:

- Existing website/home routes
- Original web modules
- Global solution configuration
- Shared infrastructure
- Application-level startup/configuration
- Common layout infrastructure when explicitly shared
- Authentication/session infrastructure when intentionally unified

## 2.4 Forbidden Actions

Do not redirect the original web root `/` to POS login unless explicitly requested.

Do not force original web users into POS flows.

Do not place POS-only screens in the original web root.

Do not place MainErp migration screens directly in the original web root unless explicitly designed as shared/global modules.

Do not break existing original web behavior while fixing POS or MainErp.

---

# 3. Kishny POS Web Area

## 3.1 Definition

Kishny POS Web is the web migration/replacement path for Kishny POS and teller workflows.

Expected location:

```text
F:\Source Code\DynamicErp\Areas\Pos
```

## 3.2 Source of Truth

Kishny POS Web uses the Kishny VB6 project as source of truth only for POS/card/teller behavior.

## 3.3 Owns

POS owns:

- POS sales
- Teller operations
- Cash in
- Cash out
- Card issuance
- KYC
- Violations
- POS closing reports
- POS receipt printing
- POS cashier defaults
- POS session hardening
- POS-specific permissions

## 3.4 Does Not Own

POS does not own general ERP modules such as:

- General accounting management
- General HR/payroll
- General inventory management
- General purchasing
- General customers/suppliers management unless implemented as shared
- Enterprise reports outside POS scope
- Project/property modules

If POS needs any of these, it must call shared modules or shared services, not duplicate them.

---

# 4. MainErp Area

## 4.1 Definition

MainErp is the enterprise ERP web migration path from the Main Original VB6 system.

Expected location:

```text
F:\Source Code\DynamicErp\Areas\MainErp
```

## 4.2 Source of Truth

MainErp uses the Main Original VB6 project as source of truth for general enterprise behavior.

## 4.3 Owns

MainErp owns enterprise modules such as:

- Accounting
- Journal vouchers
- Opening balances
- Chart of accounts
- Inventory
- Purchases
- Sales outside Kishny POS
- Customers and suppliers when not implemented as shared
- HR
- Payroll
- Attendance
- Loans
- Vacations
- Medical/social insurance
- Properties
- Contracts
- Projects
- Letters of credit
- Assembly/manufacturing vouchers
- Enterprise reports

## 4.4 Does Not Own

MainErp does not own POS-specific behavior such as:

- Kishny card token rules
- POS teller behavior
- POS cash in/cash out service flow
- KYC rules specific to card issuance
- POS receipt flow
- POS closing report behavior

If MainErp needs data from POS, it must use controlled shared reporting/services, not copy POS logic blindly.

---

# 5. Shared Modules

## 5.1 Definition

A shared module is any screen, service, data-access component, or UI component used by more than one business area.

Shared modules must be implemented once and reused.

## 5.2 Shared Candidates

The following are strong shared candidates:

- Items
- Customers
- Suppliers
- Employees
- Users
- Roles
- Permissions
- Branches
- Stores/warehouses
- Cashboxes
- Banks
- Chart of accounts
- System options/settings
- Attachments
- Print templates
- Import/export tools
- Lookup services
- Audit/logging services

## 5.3 Rule Against Duplication

Do not create duplicate versions of the same screen for POS and MainErp unless there is a documented and approved reason.

Wrong:

```text
/Areas/Pos/Items
/Areas/MainErp/Items
/Controllers/ItemsController
```

Correct:

```text
A single shared Items implementation
Context/permissions decide what each user can see or do
```

## 5.4 Context-Aware Shared Screens

A shared screen may behave differently based on context.

Examples:

- POS user may see limited fields.
- MainErp user may see full enterprise fields.
- Admin may see configuration fields.
- Teller may see only operational data.

But the implementation should remain shared whenever the underlying business entity is shared.

---

# 6. Legacy VB6 Systems

## 6.1 Main Original VB6

Main Original VB6 is the source of truth for general ERP business logic.

It must be inspected before migrating MainErp or shared enterprise modules.

## 6.2 Kishny VB6

Kishny VB6 is the source of truth for POS/card/teller logic only.

It must be inspected before changing POS sales, card, KYC, teller, and POS receipt behavior.

## 6.3 VB6 Is Not Just UI

VB6 forms usually contain hidden business logic in:

- Save buttons
- Validation routines
- Accounting posting routines
- Report calls
- Printing routines
- Grid events
- Combo loading
- Permission checks
- Form load/default logic

Do not migrate the visible screen only.

---

# 7. Routing Rules

## 7.1 Root Route

The root route `/` belongs to the original web application unless explicitly changed by a controlled task.

Do not redirect `/` to POS by default.

Do not redirect `/` to MainErp by default.

## 7.2 POS Routes

POS routes must remain under:

```text
/Pos/...
```

Examples:

```text
/Pos/Login
/Pos/PosTransaction/Index
/Pos/Kyc
/Pos/Reports
```

## 7.3 MainErp Routes

MainErp routes must remain under:

```text
/MainErp/...
```

Examples:

```text
/MainErp/Journal
/MainErp/Items
/MainErp/Customers
/MainErp/MasterDataImport
```

## 7.4 Shared Routes

Shared routes must be carefully named and permission-protected.

They may be exposed through POS or MainErp menus, but they should not become duplicated implementations.

---

# 8. Authentication and Session Boundaries

## 8.1 One User Experience

Users should not be unexpectedly asked to log in again when moving to a shared screen.

If this happens, investigate session/authentication/context configuration.

Do not solve it by creating another duplicate screen.

## 8.2 POS Session

POS may have special session hardening because of cashier usage, AppPool recycling, and high-frequency operations.

But this must not hijack the original web application's login or root route.

## 8.3 Shared Screens

Shared screens must receive the correct user context, branch context, and permissions from the current area.

They must not silently switch database, branch, or user identity.

---

# 9. Menu Architecture

## 9.1 Original Web Menu

The original web menu belongs to the original web application.

Do not pollute it with POS-only or MainErp-only modules unless explicitly required.

## 9.2 POS Menu

The POS menu should show POS and POS-relevant shared screens only.

Examples:

- POS sales
- KYC
- Closing reports
- Teller reports
- POS settings
- Shared customers/items only if permissioned and context-safe

## 9.3 MainErp Menu

The MainErp menu should show enterprise modules and shared master screens.

Examples:

- Accounting
- Inventory
- HR
- Payroll
- Customers/suppliers
- Reports
- Import tools

## 9.4 Shared Menu Entries

A shared screen can appear in multiple menus, but the implementation must remain one.

Menu entry duplication is allowed.

Code/screen duplication is not allowed.

---

# 10. Database Architecture Boundaries

## 10.1 No Blind Cross-Database Assumptions

Do not assume POS, MainErp, and original web always use the same database or same connection behavior.

Inspect configuration and active connection strings before making changes.

## 10.2 Shared Data

Shared entities must be accessed through controlled shared services/repositories.

Do not let each area write its own incompatible interpretation of the same tables.

## 10.3 POS Critical Data

POS save, serial, transaction, payment, KYC, card, and closing logic are high-risk.

Changes must preserve production stability.

## 10.4 MainErp Critical Data

Accounting, inventory costing, HR/payroll, contracts, and reports are high-risk.

Changes must preserve legacy business behavior unless explicitly redesigned.

---

# 11. UI Architecture Boundaries

## 11.1 Original Web UI

The original web application may have its own UI identity.

Do not break it while improving POS or MainErp.

## 11.2 POS UI

POS UI must be cashier-first:

- Fast
- Simple
- Minimal clicks
- Clear large controls
- No heavy startup loading
- Locked defaults where appropriate

## 11.3 MainErp UI

MainErp UI must be enterprise/power-user friendly:

- Advanced grids
- Filters
- Search
- Export
- Totals
- Tree filters where useful
- Permission-aware actions

## 11.4 Shared UI

Shared UI must use the common design system and adapt by context.

---

# 12. Migration Priority Model

Migration priority must be based on business value and risk, not developer convenience.

Recommended priority order:

1. Stabilize POS production.
2. Build shared architecture and shared authentication/session behavior.
3. Migrate shared master data screens once.
4. Migrate accounting foundation.
5. Migrate inventory/purchasing foundation.
6. Migrate HR/payroll.
7. Migrate advanced enterprise modules.
8. Migrate secondary reports and utilities.

---

# 13. Decision Rules for Any New Task

Before starting any task, classify it:

## 13.1 Original Web Task

Use this when the task affects the existing independent web application, home routes, existing web screens, or global host behavior.

Rules:

- Do not assume POS/MainErp behavior.
- Preserve original web behavior.
- Avoid redirecting into POS/MainErp unless explicitly requested.

## 13.2 POS Task

Use this when the task is cashier/card/teller/KYC/POS report related.

Rules:

- Source of truth is Kishny VB6.
- Keep under `/Areas/Pos`.
- Protect POS session and performance.

## 13.3 MainErp Task

Use this when the task is enterprise ERP migration.

Rules:

- Source of truth is Main Original VB6.
- Keep under `/Areas/MainErp` unless shared.
- Preserve enterprise business logic.

## 13.4 Shared Task

Use this when the feature/entity is needed by more than one area.

Rules:

- Implement once.
- Context controls behavior.
- Permissions must be enforced server-side.

---

# 14. Examples

## 14.1 Items Screen

Items are shared master data.

Do not create separate POS and MainErp item screens.

Create or improve one shared implementation.

POS may open it with restricted permissions or limited fields.

MainErp may open it with full permissions.

## 14.2 KYC Screen

KYC for Kishny card issuance is POS-owned.

It belongs under POS unless later generalized intentionally.

Source of truth is Kishny VB6.

## 14.3 Opening Balance Screen

Opening balances are accounting/MainErp-owned, but may reuse a shared journal voucher implementation.

The professional approach is usually:

- One journal voucher engine.
- Opening balance opened through a menu entry with fixed/default filter/context.
- Do not duplicate the full journal screen unless business behavior is truly different.

## 14.4 POS Closing Report

POS closing report is POS-owned.

It belongs under POS.

Performance is critical.

## 14.5 Property Screen

Property/contracts are original web or MainErp enterprise scope depending on the current implementation.

Do not move them into POS.

Before changing, identify whether the active screen belongs to the original web app or MainErp migration.

---

# 15. Current Strategic Correction

The project previously evolved through urgent crisis handling:

- TSPlus/RDP instability
- High user count
- POS pressure
- Web POS proof of concept
- Gradual expansion into broader migration

That success created architectural drift.

The correction now is:

- Stop random migration.
- Define ownership.
- Use shared modules where appropriate.
- Keep original web independent.
- Keep POS specialized.
- Keep MainErp enterprise-focused.
- Preserve VB6 business logic.
- Build a coherent platform instead of disconnected rewrites.

---

# 16. Final Architecture Law

The original web application is the host and an independent business application.

POS is a specialized operational area.

MainErp is the enterprise migration area.

Shared modules are implemented once and reused.

Legacy VB6 systems remain the source of truth for business behavior until each domain is formally migrated and verified.

Any change that violates these boundaries must be rejected or redesigned.

