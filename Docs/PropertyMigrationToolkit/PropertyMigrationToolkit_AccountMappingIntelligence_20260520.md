# Property Migration Toolkit Account Mapping Intelligence - 2026-05-20

## What Was Added
SQL template:
- Sql/RSMDB_AccountMappingIntelligence_20260520.sql

Tables:
- PropertyMigrationAccountDiscovery
- PropertyMigrationAccountMatchCandidate
- PropertyMigrationAccountConfidence
- PropertyMigrationAccountReviewQueue
- PropertyMigrationAccountResolution
- PropertyMigrationAccountFamily
- PropertyMigrationSuspenseUsage

## Runner Integration
DynamicErp.PropertyMigration.Runner now supports:
- IncludeAccountIntelligence
- Stage: AccountDiscovery

For RSMDB the stage runs:
- RSMDB_AccountMappingIntelligence_20260520.sql

## Safety
- Clone-only guard.
- Source DB read-only.
- No journal creation.
- No AccountId=NULL posting.
- No hardcoded target account IDs.
