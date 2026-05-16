\# README\_FOR\_CODEX.md



\# IMPORTANT — READ BEFORE ANY TASK



This repository contains a large enterprise migration and stabilization project.



Before making ANY code, SQL, UI, routing, authentication, reporting, architecture, or deployment change, you MUST read ALL files under:



F:\\Source Code\\DynamicErp\\Docs\\AI\_CONTEXT\\



Especially:



\- AGENT\_START\_HERE.md

\- SYSTEM\_ARCHITECTURE.md

\- UI\_UX\_CONSTITUTION.md

\- DATABASE\_CONSTITUTION.md

\- CODING\_STANDARDS.md

\- MIGRATION\_PLAYBOOK.md

\- MIGRATED\_CORE\_STABILIZATION.md



These files are NOT optional documentation.



They are mandatory engineering laws for this repository.



\---



\# VERY IMPORTANT CONTEXT



This solution contains MULTIPLE different systems and migration tracks.



The solution includes:



1\. Original Web Application

2\. Kishny POS Web (`/Areas/Pos`)

3\. MainErp Migration (`/Areas/MainErp`)

4\. Shared reusable modules

5\. Legacy VB6 systems used as business references



DO NOT treat the entire solution as one random application.



Understand ownership before making changes.



\---



\# ORIGINAL WEB APPLICATION



The original web application is:



\- independent

\- existing

\- production-sensitive

\- NOT POS

\- NOT MainErp



It is the main host/solution shell.



Do NOT:



\- redirect `/` to POS

\- hijack original web routes

\- mix POS logic into root application behavior

\- break original web functionality while fixing POS/MainErp



\---



\# POS AREA RULES



Path:



F:\\Source Code\\DynamicErp\\Areas\\Pos



POS is specialized for:



\- POS sales

\- teller operations

\- KYC

\- cash in / cash out

\- card operations

\- POS reports

\- POS printing

\- operational cashier workflows



Source of truth:

Kishny VB6.



POS priorities:



\- stability

\- performance

\- save reliability

\- printing reliability

\- session reliability

\- fast operational UX



Avoid:



\- heavy startup loading

\- unnecessary dropdown loading

\- duplicate saves

\- blocking UI

\- random enterprise complexity inside POS screens



\---



\# MAINERP RULES



Path:



F:\\Source Code\\DynamicErp\\Areas\\MainErp



MainErp is the enterprise ERP migration path.



Source of truth:

Main Original VB6.



MainErp priorities:



\- accounting correctness

\- inventory correctness

\- reporting correctness

\- enterprise workflows

\- reusable architecture

\- shared reusable modules



\---



\# SHARED MODULE RULE



If an entity/module is shared across business areas, implementation should usually be SHARED.



Examples:



\- Items

\- Customers

\- Employees

\- Users

\- Permissions

\- Branches

\- Warehouses

\- Chart of Accounts

\- Settings

\- Banks

\- Cashboxes



DO NOT create duplicate POS/MainErp versions unless explicitly approved.



Default behavior:



\- reuse existing implementation

\- stabilize it

\- improve it

\- expose through correct menus/context



NOT rebuilding from zero.



\---



\# CURRENT PROJECT STAGE



The project is NOT at early migration stage anymore.



Many important/core screens already exist and are partially operational.



Current focus is:



\# Stabilization + Unification



NOT random migration.



\---



\# CURRENT MIGRATED CORE



The following modules already exist or are largely migrated and should usually be STABILIZED rather than recreated:



\- Branches

\- Treasuries / Cashboxes / Banks

\- Items

\- Employees

\- Chart of Accounts

\- Warehouses

\- Projects

\- System Manager

\- Permissions

\- Users

\- Settings

\- Purchases

\- Journal Vouchers

\- Opening Balance

\- Stock Transfer

\- Custody Replenishment

\- Kishny POS Sales



Before creating ANY new version of these modules:



1\. Search existing implementation.

2\. Inspect routes/controllers/views/services/sql.

3\. Identify ownership.

4\. Review permissions.

5\. Review save behavior.

6\. Review UI consistency.

7\. Review reports/printing behavior.

8\. Reuse before rebuilding.



\---



\# PHASE 2 MODULES



The following modules may already exist partially but are NOT yet considered fully stabilized:



\- Payment Voucher

\- Receipt Voucher

\- Payroll

\- Advanced Projects

\- Project Extracts

\- Letters of Credit



These should be completed AFTER core stabilization/unification work.



\---



\# UI/UX RULES



DynamicErp must feel like ONE enterprise system.



Do NOT create:



\- random styles

\- random themes

\- different button philosophies

\- different grid behaviors

\- different save workflows

\- inconsistent spacing/layouts

\- unrelated screen personalities



Follow:

UI\_UX\_CONSTITUTION.md



System persona:



\# Enterprise Arabic RTL Power System



Meaning:



\- professional

\- stable

\- readable

\- business-first

\- productivity-first

\- operationally efficient



NOT:



\- flashy startup dashboard

\- random bootstrap experiments

\- colorful toy UI

\- unrelated screen redesigns



\---



\# RTL RULES



Arabic RTL is mandatory.



The system is NOT an English application translated later.



Respect RTL in:



\- forms

\- grids

\- dialogs

\- reports

\- printing

\- spacing

\- alignment

\- keyboard flow



\---



\# GRID RULES



Enterprise grids are critical.



Expected features where appropriate:



\- filtering

\- searching

\- export

\- totals

\- sorting

\- resizing

\- virtualization/paging if needed



Grid behavior should feel unified across modules.



\---



\# DATABASE RULES



SQL Server 2012 compatibility is required unless explicitly approved otherwise.



Stored procedures must use:



DROP + CREATE



NOT random ALTER procedures.



Do NOT:



\- guess schema names

\- casually rewrite accounting logic

\- casually rewrite voucher serial logic

\- ignore stock/costing behavior

\- ignore transaction integrity



Voucher/accounting behavior is CRITICAL.



Preserve legacy business behavior unless explicitly redesigning it.



\---



\# AUTHENTICATION AND SESSION RULES



Shared screens must NOT randomly request login again.



If session/context breaks:



\- investigate authentication/session architecture

\- investigate cookies/routes/context

\- DO NOT solve by duplicating screens



\---



\# BEFORE STARTING ANY TASK



You MUST identify:



1\. Is this:

&#x20;  - Original Web

&#x20;  - POS

&#x20;  - MainErp

&#x20;  - Shared



2\. What is the source of truth?



3\. Does implementation already exist?



4\. Is this duplicated already somewhere else?



5\. Does this affect:

&#x20;  - accounting

&#x20;  - stock

&#x20;  - serials

&#x20;  - reports

&#x20;  - printing

&#x20;  - permissions

&#x20;  - performance

&#x20;  - session behavior



6\. Does UI follow system constitution?



\---



\# REQUIRED TASK OUTPUT



Every completed task should clearly state:



\- changed files

\- changed SQL scripts

\- routes affected

\- database changes

\- testing performed

\- deployment notes

\- pending risks/issues



\---



\# FORBIDDEN BEHAVIORS



Unless explicitly approved, DO NOT:



\- create duplicate screens

\- randomly redesign core workflows

\- rewrite accounting logic casually

\- rewrite voucher serial logic casually

\- move modules into wrong areas

\- add random UI styles

\- ignore permissions

\- ignore printing/reporting behavior

\- guess database schema

\- rebuild existing migrated modules blindly



\---



\# FINAL PRINCIPLE



This project is an enterprise transformation project.



The goal is:



\- stable production

\- unified architecture

\- reusable shared modules

\- consistent enterprise UX

\- preserved business behavior

\- reduced dependency on TSPlus/RDP over time



Every change should move the project toward these goals.

