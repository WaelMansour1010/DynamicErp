# RSMDB VB6 Mapping Review - 2026-05-20

## Reviewed Path

`F:\Source Code\SatriahMain\Frm\New frm\RealEstateMnag`

## Confirmed Property Tables

| VB6 / SQL Name | Meaning | Evidence |
|---|---|---|
| `TblAqar` | Property master | Used by property forms/reports and includes `ownerid`. |
| `TblAqarDetai` | Property unit/details rows | Used as unit/property detail data. |
| `TblUnites` | Unit type / unit lookup candidate | Should not be treated as the actual unit master without final review. |
| `TblContract` | Contract master | Used by rent/contract forms. |
| `TblContractInstallments` | Contract installment schedule | Used as installment/bill source. |
| `TblCustemers` | Party master for owners and renters | Role comes from relationship: owner through `TblAqar.ownerid`, renter through contract customer. |

## Owner/Landlord Evidence

VB6 forms and reports reference owner behavior explicitly:

- `TblAqar.ownerid = TblCustemers.CusID` in owner/property queries.
- `TblAqrOwin` appears as owner payable/owner installment candidate table.
- `GetOwnerPayment(dbo.TblAqrOwin.ID)` is referenced in reporting logic.
- Owner payment posting references owner account resolution and voucher generation.

## Notes / Accounting Evidence

Candidate note types remain consistent with previous findings, but RSMDB needs customer-specific validation before migration:

| NoteType | Candidate Meaning | Status |
|---:|---|---|
| 4 | Receipt | Strong candidate; only safe when linked to contract/installment. |
| 5 | Payment / Issue | Manual Review; may include owner or other payments. |
| 60 | Contract journal | Candidate; needs journal grouping/direction validation. |
| 9088 | VAT / installment / special transaction candidate | Unconfirmed; Review Queue only. |
| -1 | Termination / settlement candidate | Candidate; Review Queue only until settlement logic is verified. |

## Differences From Adnan

- RSMDB has owner payable candidates in `TblAqrOwin`, while Adnan discovered count was zero.
- RSMDB has a larger and noisier `Notes`/journal footprint.
- RSMDB has unit/property link gaps requiring Hybrid/Tolerant staging behavior.
- Owner migration must be included before any RSMDB migration decision.

## Decision

VB6 review confirms that owners are real business entities in the old system. RSMDB staging must include owners and owner links, but owner payments/payables remain Manual Review until finance and source-code mapping are complete.
