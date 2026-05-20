# Property Migration Toolkit Intelligence Layer - 2026-05-20

## What Was Added
- SQL template: Sql/RSMDB_IntelligenceLayer_20260520.sql.
- Intelligence tables:
  - PropertyMigrationMatchCandidate
  - PropertyMigrationConfidenceScore
  - PropertyMigrationResolutionResult
  - PropertyMigrationClassificationResult
  - PropertyMigrationSuggestedMapping
  - PropertyMigrationManualReview

## Capabilities
- Journal line resolver.
- Receipt matching engine.
- Owner/payment classification.
- NoteType intelligence.
- Confidence scoring.
- ReviewQueue status enrichment.

## Safety
- Runs only on Clone/Sandbox/PropertyPilot/ReadyToTest/PilotClone/Migration database names.
- Refuses RSMDB, Adnan, Alromaizan, and MyErp as target.
- Does not create DynamicErp accounting entries.
- Does not update the source database.
- Does not delete ReviewQueue records.
