\# ENGINEERING\_GUARDRAILS.md



\## DynamicErp Engineering Guardrails



\---



\# Git \& Workflow Rules



\- Inspect existing implementation before rewriting.

\- Avoid overwriting unrelated work.

\- Prefer stabilization over unnecessary rewrites.

\- Recommended before major work:



```bash

git pull --ff-only

````



\---



\# VB6 Migration Rules



\* VB6 forms contain hidden business logic.

\* Never migrate UI only.

\* Preserve:



&#x20; \* save behavior

&#x20; \* accounting logic

&#x20; \* reports

&#x20; \* printing

&#x20; \* permissions

&#x20; \* default loading behavior



\## Encoding Rules



VB6 `.frm` files may contain Arabic encodings and legacy charset behavior.



Be careful with:



\* encoding

\* CRLF

\* DEFAULT\_CHARSET

\* Arabic labels

\* designer sections



Do not corrupt Arabic forms accidentally.



\---



\# SQL \& Database Rules



\## SQL Server Version



All SQL must remain compatible with:



SQL Server 2012



unless explicitly approved otherwise.



\## Stored Procedures



Use:



```sql

DROP + CREATE

```



NOT random ALTER procedures.



\## SQL Script Locations



\### POS SQL



```text

F:\\Source Code\\DynamicErp\\Areas\\Pos\\Sql

```



\### MainErp / General



Use approved migration/update locations only.



\## No Schema Guessing



Never guess:



\* table names

\* column names

\* relationships

\* business flags

\* procedures



Inspect schema first.



\---



\# Accounting Protection Rules



These are HIGH-RISK areas:



\* voucher serials

\* accounting posting

\* stock effects

\* costing

\* transaction numbering

\* opening balances



Do NOT rewrite casually.



Preserve historical behavior unless explicitly redesigning.



\---



\# Reporting \& Printing Rules



Reports and printing are BUSINESS-CRITICAL.



Do not casually redesign:



\* reports

\* receipts

\* vouchers

\* operational print layouts



Preserve:



\* Arabic direction

\* spacing

\* totals behavior

\* operational readability

\* print flow



\---



\# Shared Authentication Rules



Shared screens must reuse authenticated context.



Do NOT create duplicate login flows for shared modules.



If shared screens request login again:



\* inspect cookies

\* inspect auth/session scope

\* inspect routing/context



DO NOT solve by duplicating screens.



\---



\# Shared Module Rules



If a module/entity is shared:



\* reuse implementation

\* stabilize implementation

\* improve implementation



NOT duplicate implementation.



Examples:



\* Items

\* Customers

\* Employees

\* Users

\* Permissions

\* Warehouses

\* ChartOfAccounts



Menu duplication is allowed.



Code duplication is NOT allowed.



\---



\# Reuse Before Rebuild



Before creating ANY new screen/service:



1\. Search existing implementation.

2\. Inspect routes/controllers/views/services/sql.

3\. Inspect permissions.

4\. Inspect save behavior.

5\. Inspect reports/printing.

6\. Reuse if possible.



Default action:



STABILIZE existing implementation.



NOT rebuild from zero.



\---



\# UI/UX Rules



DynamicErp must feel like ONE enterprise system.



Forbidden:



\* random styles

\* random themes

\* inconsistent buttons

\* inconsistent grids

\* inconsistent save workflows

\* decorative redesigns without operational value



System Persona:



Enterprise Arabic RTL Power System



Meaning:



\* professional

\* stable

\* operational

\* business-first

\* productivity-focused



\---



\# Keyboard Workflow Rules



The system is keyboard-heavy.



Protect:



\* tab order

\* enter navigation

\* focus clarity

\* operational speed



\---



\# Performance Rules



Performance is production-critical.



Especially for:



\* POS

\* reports

\* accounting

\* large grids

\* save operations



Avoid:



\* huge dropdown loading

\* repeated heavy queries

\* blocking UI

\* unnecessary startup loading



\---



\# Deployment Rules



Every major task should clearly specify:



\* changed files

\* SQL scripts

\* DB changes

\* deployment notes

\* testing performed

\* pending risks



Do NOT introduce hidden breaking changes.



\---



\# Current Strategic Direction



Current phase is:



\# Stabilization + Unification



NOT early migration.



Current priorities:



1\. stabilize production

2\. unify UI/UX

3\. unify architecture

4\. improve shared modules

5\. fix shared auth/session behavior

6\. complete partially migrated workflows



\---



\# Final Law



This is an enterprise transformation project.



Goals:



\* stable production

\* preserved business behavior

\* unified architecture

\* reusable shared modules

\* coherent enterprise UX

\* controlled modernization



Every change must move the project toward these goals.



