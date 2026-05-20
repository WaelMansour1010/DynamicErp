# Property Schema Compare - MyErp vs Alromaizan
Date: 2026-05-20
Project: `F:\Source Code\DynamicErp`
Reference Database: `MyErp`
Target Database: `Alromaizan`
Scope: DynamicErp main property module only, not POS.

## Executive Summary
Compared property-module tables only. The comparison found one missing column in `Alromaizan` compared with `MyErp`.

A general idempotent SQL script was created:

`PropertySchema_MyErp_to_Alromaizan_MissingColumns_20260520.sql`

The script was not executed on `Alromaizan`, `MyErp`, or any production database.

## Scope Rule
Included tables were selected by schema metadata using this scope:

- Tables in `MyErp` and `Alromaizan` whose names start with `Property`.
- Direct property/payment bridge table: `CashReceiptVoucherPropertyContractBatch`.

Excluded from scope:

- General accounting tables.
- Users/security tables.
- POS tables.
- Non-property modules.

## Tables Compared
Common property tables compared for columns: 29.

| # | Table |
|---:|---|
| 1 | CashReceiptVoucherPropertyContractBatch |
| 2 | Property |
| 3 | PropertyBatch |
| 4 | PropertyBillRegisteration |
| 5 | PropertyComponent |
| 6 | PropertyComponentDetail |
| 7 | PropertyContract |
| 8 | PropertyContractBatch |
| 9 | PropertyContractImage |
| 10 | PropertyContractMergedUnit |
| 11 | PropertyContractRep |
| 12 | PropertyContractTermination |
| 13 | PropertyContractTerminationDamage |
| 14 | PropertyContractTerminationDetail |
| 15 | PropertyDetail |
| 16 | PropertyDueBatch |
| 17 | PropertyDueBatchDetail |
| 18 | PropertyNotification |
| 19 | PropertyNotificationType |
| 20 | PropertyOwner |
| 21 | PropertyPaymentHistory |
| 22 | PropertyRenter |
| 23 | PropertyRenterImage |
| 24 | PropertyRevenueProof |
| 25 | PropertyRevenueProofDetail |
| 26 | PropertyStatus |
| 27 | PropertyType |
| 28 | PropertyUnit |
| 29 | PropertyUnitType |

## Missing Columns In Alromaizan
| TableName | ColumnName | DataType | Length | Precision | Scale | Nullable | Default Constraint | Identity | Computed |
|---|---|---|---:|---:|---:|---|---|---|---|
| PropertyBatch | Discount_Backup | money | 8 | 19 | 4 | Yes | None | No | No |

## Columns Included In Script
| TableName | ColumnName | Script Decision |
|---|---|---|
| PropertyBatch | Discount_Backup | Included as `money NULL` guarded by `IF OBJECT_ID` and `COL_LENGTH` |

## Columns Excluded From Script
None. The only missing column is nullable, not identity, and not computed.

## Complete Tables In MyErp Missing From Alromaizan
| TableName | Decision | Reason |
|---|---|---|
| PropertyContractFixLog | Not created | Whole-table creation is outside current scope. It is not a small required lookup table; it appears to be a log/fix tracking table. |

## NOT NULL Handling
No missing `NOT NULL` column was found.

If future comparisons find `NOT NULL` columns, the safe rule should be:

- Do not add them as `NOT NULL` directly to customer databases with existing rows.
- Prefer nullable addition first, or a reviewed default if the business meaning is clear.
- Avoid breaking existing customer data during schema alignment.

## Script Safety Review
| Safety Check | Result |
|---|---|
| SQL Server 2012 compatible | Yes |
| Idempotent | Yes, uses `COL_LENGTH(...) IS NULL` |
| Uses `USE MyErp` or `USE Alromaizan` | No |
| Contains UPDATE / DELETE / INSERT | No |
| Adds FK or Index | No |
| Changes existing column types | No |
| Creates missing full tables | No |

## Risks Before Running
- Run only after selecting the intended target database connection.
- The script does not hard-code a database name by design, so operator selection matters.
- `PropertyContractFixLog` remains missing if code later depends on it; it should be reviewed separately before any table-create decision.

## Recommendation
The generated script is low-risk and suitable for review before applying to other customer databases. After your review, it can be run on a backup/clone first, then on other targets as needed.

Raw evidence files:

- `PropertyTables_scope_raw.txt`
- `MissingColumns_raw.txt`
