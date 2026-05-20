# Property Migration Toolkit - Architecture
Date: 2026-05-20
Project: `F:\Source Code\DynamicErp`
Scope: DynamicErp main property module, not POS.

## Goal
Turn the Adnan pilot migration into a reusable, controlled toolkit for migrating property-management data from legacy VB6 customer databases into DynamicErp clones.

## Recommended Current Implementation
Use SQL Scripts + Config + Runbook now. Add a console runner later after the SQL process is stable across at least two customers.

## Why Not UI/Runner Now
| Option | Decision | Reason |
|---|---|---|
| SQL scripts + config | Selected | Lowest risk, transparent, reviewable, works with SQL Server 2012 |
| Console tool | Later | Useful after templates mature; avoid hiding accounting decisions now |
| Admin UI | Later | Too risky before migration rules stabilize |

## Fixed Stages
| Stage | Name | Purpose |
|---:|---|---|
| 0 | Preflight | Verify source, clone, backup, schema, permissions, guards |
| 1 | Discovery | Discover property/accounting tables, note types, links, branches, cashboxes, banks |
| 2 | Mapping | Map properties, units, tenants, contracts, installments, accounts, operational setup |
| 3 | Diagnostics | Find broken links, missing accounts, unbalanced journals, unsafe vouchers |
| 4 | Migration | Migrate master data, contracts, installments, balances, advances, selected accounting |
| 5 | Reconciliation | Compare counts, totals, balances, journals, null-account risk |
| 6 | Web Validation | Login, property screens, contracts, receipts, termination, reports |
| 7 | ReadyToTest Delivery | Clean tests, deliver DB, guide user testing, decision memo |

## Toolkit Components
| Component | Path |
|---|---|
| Architecture | `PropertyMigrationToolkit_Architecture_20260520.md` |
| Runbook | `PropertyMigrationToolkit_Runbook_20260520.md` |
| Safety Rules | `PropertyMigrationToolkit_SafetyRules_20260520.md` |
| Accounting Strategy | `PropertyMigrationToolkit_AccountingStrategy_20260520.md` |
| Config Spec | `PropertyMigrationToolkit_ConfigSpec_20260520.md` |
| Customer configs | `PropertyMigrationToolkit_*Config*.sql` |
| SQL templates | `Sql\*.sql` |

## General vs Customer-Specific
| General | Customer-Specific Config |
|---|---|
| Stage structure | Source table names |
| Clone guard rules | Note type meanings |
| Cross reference model | Active contract rule |
| Reconciliation model | Property/unit/renter column names |
| Accounting safety gates | Payment method/cashbox/bank mapping |
| Rollback principles | Owner payment treatment |

## Lessons From Adnan
- Active contracts can be migrated safely if property/unit/renter/account links are complete.
- Opening balance must be recomputed, not blindly trusted from stored `Payed`/`Remains` fields.
- Payment methods must not rely on fixed IDs without resolver/validation.
- Cash issue vouchers need same debit/credit account prevention.
- Historical accounting can be migrated only when voucher-to-contract/installment linkage is proven.
- Owner payments and ambiguous cash issues must default to Manual Review.
