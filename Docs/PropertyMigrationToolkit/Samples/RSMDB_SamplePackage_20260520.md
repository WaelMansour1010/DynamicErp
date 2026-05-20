# RSMDB Sample Package - 2026-05-20

## Purpose

RSMDB is the reference sample for difficult legacy VB6 clients with weak links, hidden allocation tables, large review queues, and finance-assisted account mapping.

## Proven Scope

- Staging clone setup.
- VB6/schema intelligence.
- Receipt allocation discovery.
- CashingType=8 candidate building.
- Finance approval pack.
- Score>=60 approval apply on clone.
- Mini accounting pilot: 32 receipts, 32 journals, 64 lines, value 966,568.2500.
- Batch rollback to zero pilot artifacts.

## Configs

- `Tools\DynamicErp.PropertyMigration.Runner\Config\rsmdb-discovery.dryrun.json`
- `Tools\DynamicErp.PropertyMigration.Runner\Config\rsmdb-stagingclone.dryrun.json`

## Use As Template For

- Clients where receipt-to-contract allocation is hidden.
- Clients requiring account mapping intelligence.
- Finance-approved staged accounting pilots.

## Permanent Exclusions Until Separate Review

- Owner payments.
- Issues.
- Terminations.
- NoteType 9088.
- Weak/blocked matches.
