# Journal Migration Status - Adnan and RSMDB - 2026-05-20

## Adnan Status

In the ReadyToTest clone used for the pilot, the current validated counts are:

- Contracts: `283`
- Cash receipts: `753`
- Cash issues: `0`
- Journal entries: `754`
- Journal lines: `2363`
- `AccountId=NULL`: `0`
- Unbalanced journals: `0`

## Was This Full Accounting History?

No. The previous Adnan accounting work must be classified as partial accounting migration/validation, not full accounting history.

## What Appears To Be Included

- Receipt-related accounting sufficient for cash receipt testing and ReadyToTest validation.
- Opening Balance staging: `1,156,544.6600`.
- Advance payment staging: `55,592.8900`.
- Voucher-linked journal safety validation.

## What Was Not Proven As Fully Migrated

- Full historical GL.
- All `Notes` types.
- All contract creation journals `NoteType=60`.
- VAT/installment `NoteType=9088` as a complete class.
- Termination `NoteType=-1` journals as a complete class.
- Owner payment journals.
- Cash issue/owner payment journals.

## RSMDB Journal Discovery

RSMDB accounting candidates:

- `Notes`: `39257` rows.
- `DOUBLE_ENTREY_VOUCHERS`: `139769` journal lines.
- `NoteType=4`: `10365` receipt candidates.
- `NoteType=5`: `7632` issue/payment candidates.
- `NoteType=60`: `2426` contract journal candidates.
- `NoteType=9088`: `64` unclassified VAT/installment candidates.
- `NoteType=-1`: `754` termination/settlement candidates.

## RSMDB Accounting Risks

- `DOUBLE_ENTREY_VOUCHERS.Account_Code` exists for all discovered lines, but account mapping to DynamicErp is not approved yet.
- Potential unbalanced groups by `Double_Entry_Vouchers_ID`: `51968` using the initial direction assumption. This strongly indicates that journal grouping/direction semantics need deeper mapping before migration.
- Owner payment GL logic exists in VB6 via `payGlPaymentOwner`, but RSMDB owner payment tables are empty in current discovery.

## Decision

- Adnan journals are partial/scope-limited, not full accounting history.
- RSMDB journals must not be migrated yet.
- RSMDB journals can only be staged when linked to approved vouchers/contracts and when every account maps to a target `ChartOfAccount` row.
