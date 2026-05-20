# Property Owners / Landlords Assessment - 2026-05-20

## Executive Finding

Yes, Property Owners / Landlords were under-documented in the earlier toolkit design. They are a first-class property entity in VB6 and DynamicErp.

## DynamicErp Target Model

Reference databases `Alromaizan` and `MyErp` contain:

- `PropertyOwner`
- `Property.PropertyOwnerId`

`PropertyOwner` columns include:

- `Id`
- `Code`
- `ArName`
- `EnName`
- `DepartmentId`
- `Agent`
- `AgencyNo`
- `ContactPerson`
- `AccountId`
- `VATNo`
- `Phone`
- `Mobile`
- `Fax`
- `Email`
- `Address`

`Property.PropertyOwnerId` links property to its primary owner.

## Adnan Findings

Old owner source:

- Owner master data is not a separate owner-only table.
- Owners are rows in `TblCustemers`.
- Property-owner link is `TblAqar.ownerid -> TblCustemers.CusID`.
- `TblCustemers.Owner` exists as a flag.
- Owner account candidates: `TblCustemers.Account_Code`, `Account_Code_As_Supplier`, `Account_Code2`, and property-specific `AccountAccountAqar`.

Counts:

- `TblAqar`: `28`
- Properties with ownerid: `28`
- Distinct owners from properties: `3`
- `TblAqrOwin`: `0`
- `TblOwnerPayment`: `0`
- `TblNotesOwnerPayment`: `0`

## RSMDB Findings

Old owner source follows the same model:

- Owner master data: `TblCustemers`
- Property-owner link: `TblAqar.ownerid -> TblCustemers.CusID`
- Owner payable/schedule candidate: `TblAqrOwin`
- Owner payment tables exist but are empty in current discovery: `TblOwnerPayment`, `TblNotesOwnerPayment`

Counts:

- `TblAqar`: `16`
- Properties with ownerid: `16`
- Distinct owners from properties: `4`
- `TblAqrOwin`: `4`
- `TblOwnerPayment`: `0`
- `TblNotesOwnerPayment`: `0`

## VB6 Evidence

VB6 forms confirm owner usage:

- `RSOwner.frm`
- `FrmOwnerAqarReport.frm`
- `FrmAqarListOfOwner.frm`
- `FrmAqarInstallAlert.frm`
- `RSTradingCenter.frm`

Important code patterns found:

- `TblAqar.ownerid` joins to `TblCustemers.CusID`.
- Reports show owner name via `TblCustemers` alias.
- `GetOwnerPayment(dbo.TblAqrOwin.ID)` is used for owner payable/payment review.
- `payGlPaymentOwner` creates owner-related GL lines in VB6.

## Owner Payment / SourceTypeId=13

In `MyErp`, `CashIssueVoucher` rows with `SourceTypeId=13` exist and are likely linked to property owner payments or property source payments. This was not present in the Adnan ReadyToTest clone count, but it confirms the scenario exists in DynamicErp and must not be ignored.

## Answers To Key Questions

| Question | Finding |
|---|---|
| Old owner table in Adnan | `TblCustemers`, linked by `TblAqar.ownerid` |
| Old owner table in RSMDB | `TblCustemers`, linked by `TblAqar.ownerid` |
| DynamicErp table | `PropertyOwner` |
| Link level | Property-level via `Property.PropertyOwnerId` |
| More than one owner | Not proven in current RSMDB/Adnan property schema; toolkit now supports review for percentage/multi-owner |
| Ownership percentage | Not found in property link; default `100%` with manual review if other evidence appears |
| Owner account | Old account candidates in `TblCustemers`; DynamicErp uses `PropertyOwner.AccountId` |
| Owner payables | `TblAqrOwin` candidate, especially in RSMDB |
| Owner payments | `TblOwnerPayment` / `TblNotesOwnerPayment`; empty in Adnan/RSMDB current counts |
| SourceTypeId=13 | Requires separate owner-payment validation; not approved by default |
| Termination impact | Not proven; must remain review-only until settlement logic is confirmed |
| Reports | Yes, property reports use owner data |
