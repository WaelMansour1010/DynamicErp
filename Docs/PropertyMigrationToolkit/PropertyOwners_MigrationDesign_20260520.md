# Property Owners Migration Design - 2026-05-20

## Design Decision

Property owners are now first-class migration entities in the PropertyMigrationToolkit. They are not renters, suppliers, or generic accounts.

## New Staging Tables

| Staging Table | Purpose |
|---|---|
| `PropertyMigrationSourceOwner` | Owner master data from old VB6 source. |
| `PropertyMigrationSourcePropertyOwner` | Property-to-owner relationship and optional ownership percentage. |
| `PropertyMigrationSourceOwnerBalance` | Owner payable/receivable balances for review or approved migration. |
| `PropertyMigrationSourceOwnerPayment` | Owner payment vouchers, manual-review by default. |

## Generic Templates Added

| Template | Default Behavior |
|---|---|
| `Migration_Owners_Generic.sql` | Migrates owner master data into `PropertyOwner` and logs missing account warnings. |
| `Migration_PropertyOwnerLinks_Generic.sql` | Links migrated properties to primary owner using `Property.PropertyOwnerId`; multi-owner/percentage cases are sent to Review Queue. |
| `Migration_OwnerPayments_Generic.sql` | Does not post owner payments by default; creates Review Queue items unless explicitly approved in config. |

## Migration Flow

1. Customer-specific staging script reads old source data.
2. Owners are loaded into `PropertyMigrationSourceOwner`.
3. Property owner links are loaded into `PropertyMigrationSourcePropertyOwner`.
4. Owner balances/payments are staged separately.
5. Generic owner templates migrate safe owner master/link data.
6. Owner payments remain review-only unless finance approves a customer-specific mapping.

## Required Validation

- Owner exists.
- Property exists.
- Owner account code maps to target `ChartOfAccount` when accounting is involved.
- Owner payment source type is proven.
- Owner payment journal is balanced.
- No owner payment journal line has `AccountId=NULL`.
- No owner payment uses same debit and credit account unless explicitly approved.

## RSMDB Current Design

RSMDB staging draft maps:

- `TblAqar.ownerid -> TblCustemers.CusID` into owners.
- `TblAqar.AqarID -> ownerid` into property-owner links.
- `TblAqrOwin` into owner balance staging/review.
- `Notes Type 5` and owner payment candidates into review-only staging until payment source is proven.

## Deferred Items

- Multi-owner percentage migration if a customer has an ownership split table.
- Owner payment posting.
- Owner payable opening balances.
- SourceTypeId=13 production approval.
