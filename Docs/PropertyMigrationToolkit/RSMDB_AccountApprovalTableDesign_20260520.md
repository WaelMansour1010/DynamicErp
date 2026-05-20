# RSMDB Account Approval Table Design - 2026-05-20

## Table
PropertyMigrationAccountFinanceApproval

## Columns
- BatchId
- CustomerCode
- SourceAccountCode
- SourceAccountName
- SuggestedTargetAccountSerial
- ApprovedTargetAccountSerial
- Decision
- ApprovedBy
- ApprovedAt
- Notes
- Status

## Decision Values
- Approved
- Changed
- SuspenseApproved
- Blocked
- NeedsMoreInfo

## Draft SQL
See:
RSMDB_AccountApprovalTableDesign_DRAFT_SQL_20260520.sql

## Safety
The table is clone/staging only and does not apply any mapping by itself.
