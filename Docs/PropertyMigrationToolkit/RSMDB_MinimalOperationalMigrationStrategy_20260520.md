# RSMDB Minimal Operational Migration Strategy - 2026-05-20

## Goal
Create target operational entities and EntityMap only, without receipts, journals, issues, owner payments, terminations, or posting.

## Included
- Properties
- Units
- Renters
- Active contracts
- Installments for active contracts

## Excluded
- Receipts
- Journals
- Issues
- Owner payments
- Terminations
- NoteType 9088
- Suspense posting

## Safety Adjustments
VB6 lookup ids that do not exist in the clone are stored as NULL instead of breaking FK constraints. This affected department/unit type style lookups.
