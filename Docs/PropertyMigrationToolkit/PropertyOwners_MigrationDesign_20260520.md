# Property Owners Migration Design - 2026-05-20

## Design Decision

Property owners are now first-class entities in the staging contract and migration templates.

## New Staging Tables

Added to `01_SourceStagingTables_Generic.sql`:

- `PropertyMigrationSourceOwner`
- `PropertyMigrationSourcePropertyOwner`
- `PropertyMigrationSourceOwnerBalance`
- `PropertyMigrationSourceOwnerPayment`

## Generic Templates Added

- `Migration_Owners_Generic.sql`
- `Migration_PropertyOwnerLinks_Generic.sql`
- `Migration_OwnerPayments_Generic.sql`

## Mapping Model

### Owner Master

Old VB6:

- `TblCustemers`
- property owners identified by `TblAqar.ownerid`

DynamicErp:

- `PropertyOwner`

### Property Owner Link

Old VB6:

- `TblAqar.ownerid`

DynamicErp:

- `Property.PropertyOwnerId`

### Owner Accounts

Old VB6 candidates:

- `TblCustemers.Account_Code`
- `TblCustemers.Account_Code_As_Supplier`
- `TblCustemers.Account_Code2`
- `TblCustemers.AccountAccountAqar`

DynamicErp:

- `PropertyOwner.AccountId`

### Owner Balances / Payables

Old VB6 candidate:

- `TblAqrOwin`

DynamicErp strategy:

- Stage as `PropertyMigrationSourceOwnerBalance`.
- Do not post accounting automatically.
- Review before migration or GoLive.

### Owner Payments

Old VB6 candidates:

- `TblOwnerPayment`
- `TblNotesOwnerPayment`
- Notes payment rows if proven linked to owner.

DynamicErp strategy:

- Stage as `PropertyMigrationSourceOwnerPayment`.
- Route to Review Queue by default.
- Do not insert payment vouchers until owner/property/account linkage and accounting direction are approved.

## Safety Rules

- Owner is never assumed to be the renter.
- Owner payment is never assumed from generic cash issue without proof.
- Owner payment journal cannot be created with `AccountId=NULL`.
- Suspense account for owner is allowed only with explicit config and review queue.
- Multi-owner / percentage cases must be manual review until DynamicErp support is confirmed.
