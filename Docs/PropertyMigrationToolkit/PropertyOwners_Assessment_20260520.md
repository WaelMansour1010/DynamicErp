# Property Owners / Landlords Assessment - 2026-05-20

## Executive Summary

Property owners were under-documented in the earlier PropertyMigrationToolkit design. The migration model previously treated properties, units, renters, contracts, installments, receipts, and journals as first-class entities, but owner/landlord data was not explicitly modeled. This is now corrected in the staging contract and generic templates.

## Findings By Database

| Database | Owner Master Source | Property Link | Owner Payables / Payments | Notes |
|---|---|---|---|---|
| Adnan | `TblCustemers` via `TblAqar.ownerid = TblCustemers.CusID` | `TblAqar.ownerid` | `TblAqrOwin`, `TblOwnerPayment`, `TblNotesOwnerPayment` exist but current row counts are zero | 28 properties, all have owner id, 3 distinct owners |
| RSMDB | `TblCustemers` via `TblAqar.ownerid = TblCustemers.CusID` | `TblAqar.ownerid` | `TblAqrOwin` has 4 owner payable candidate rows; `TblOwnerPayment` and `TblNotesOwnerPayment` are zero | 16 properties, all have owner id, 4 distinct owners |
| Alromaizan | `PropertyOwner` | `Property.PropertyOwnerId` | No observed `CashIssueVoucher` owner rows in current sample | Existing production/reference clone has minimal owner data |
| MyErp | `PropertyOwner` | `Property.PropertyOwnerId` | `CashIssueVoucher.SourceTypeId = 13` has 260 rows | Strong reference signal that SourceTypeId=13 is owner-related, but not enough alone to migrate owner payments |

## VB6 Evidence

Reviewed VB6 property module under `F:\Source Code\SatriahMain\Frm\New frm\RealEstateMnag`.

Important evidence:

- `TblAqar.ownerid` is used as owner reference.
- Owner is joined to `TblCustemers.CusID` in owner/property reports.
- `TblAqrOwin` appears in owner payable/owner installment reports.
- VB6 code references `GetOwnerPayment(dbo.TblAqrOwin.ID)`.
- VB6 payment code references owner account resolution through `GetMyAccountCode("TblCustemers", "CusID", ...)`.
- Owner payment posting references `payGlPaymentOwner`, `OwnerAccount`, and `CREATE_VOUCHER_GE`.

## Business Meaning

- The old VB6 model appears to store owners in the same customer master table (`TblCustemers`) used for other parties, while the role is determined by links such as `TblAqar.ownerid`.
- DynamicErp has a separate `PropertyOwner` entity, so owner migration must not reuse renter migration blindly.
- Property ownership is currently a direct property-level relationship, not a unit-level or contract-level relationship in the discovered RSMDB/Adnan samples.

## Key Answers

| Question | Answer |
|---|---|
| Is there an owner table in the old system? | Yes, owner master data is represented by `TblCustemers` when referenced from `TblAqar.ownerid`. |
| Real owner source in Adnan | `TblCustemers` via `TblAqar.ownerid`. |
| Real owner source in RSMDB | `TblCustemers` via `TblAqar.ownerid`. |
| DynamicErp equivalent | `PropertyOwner`, linked from `Property.PropertyOwnerId`. |
| Is owner linked to property/unit/contract? | Discovered link is property-level: `TblAqar.ownerid`. |
| Multiple owners/ownership percentage? | Not proven in current samples. The new toolkit design supports review/staging if discovered. |
| Owner account? | VB6 resolves owner accounts from `TblCustemers`; account mapping must be validated per owner. |
| Are deferred payment vouchers owner-related? | `SourceTypeId=13` in MyErp and VB6 owner payment code strongly suggest owner-payment semantics, but owner payments remain Manual Review until source/customer mapping is proven. |
| Does termination affect owner? | Not proven safely. Any owner impact from terminations must be reviewed before posting. |
| Do reports need owner data? | Yes. VB6 has owner/property reports and DynamicErp property screens/reports may require `PropertyOwnerId`. |

## Risk Assessment

| Risk | Level | Mitigation |
|---|---:|---|
| Migrating properties without owners | High | Add owner staging and property-owner link templates. |
| Treating owner as renter | High | Separate `PropertyMigrationSourceOwner` from `PropertyMigrationSourceRenter`. |
| Posting owner payments incorrectly | Critical | Owner payments are Manual Review by default. |
| SourceTypeId=13 assumptions | High | Use only as candidate until code/data prove meaning. |
| Missing owner accounts | High | Stage owner account mapping; no journal with null account. |
