# RSMDB Final Decision Before Migration - 2026-05-20

## Decision

RSMDB is not approved for migration execution yet. It is approved only for clone-based staging population and diagnostics.

## What Is Ready

- RSMDB source tables have been identified for property staging.
- Owner/landlord staging has been added to the toolkit.
- RSMDB staging mapping draft has been created.
- Runner DryRun config has been created and validated at preflight level.
- Diagnostics show the main relationship and accounting risks.

## What Is Not Ready

- Final active contract rule.
- Final NoteType interpretation for `60`, `9088`, and `-1`.
- Safe issue/payment migration.
- Owner payable/payment posting.
- Journal direction and grouping validation.
- Account mapping validation against an RSMDB target clone.

## Go / No-Go

| Area | Decision |
|---|---|
| Master data staging | Go on clone only |
| Contract staging | Go on clone only |
| Installment staging | Go on clone only |
| Receipt staging | Go on clone only, linked receipts only |
| Issues/payments | No-Go for migration; Review Queue only |
| Journals | No-Go until balancing semantics are confirmed |
| Owners | Go for owner master/link staging; owner payments review only |
| Full migration | No-Go |

## Required Next Step

Create a dedicated RSMDB target clone, run toolkit core/staging setup, execute the RSMDB staging mapping script on the clone, then review diagnostics and Review Queue before any migration template is allowed.
