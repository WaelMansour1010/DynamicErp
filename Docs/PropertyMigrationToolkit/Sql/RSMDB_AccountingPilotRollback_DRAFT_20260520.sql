/* RSMDB Accounting Pilot Rollback - DRAFT clone only */
DECLARE @BatchId uniqueidentifier='$(MigrationBatchId)';
IF DB_NAME() NOT LIKE '%Clone%' AND DB_NAME() NOT LIKE '%Sandbox%' AND DB_NAME() NOT LIKE '%PropertyPilot%' AND DB_NAME() NOT LIKE '%ReadyToTest%' AND DB_NAME() NOT LIKE '%PilotClone%' AND DB_NAME() NOT LIKE '%Migration%'
BEGIN RAISERROR('Clone database required.',16,1); RETURN; END;
IF DB_NAME() IN ('RSMDB','Adnan','Alromaizan','MyErp') BEGIN RAISERROR('Blocked database.',16,1); RETURN; END;
-- No pilot execution rows were created in this phase, so rollback is currently a no-op.
SELECT RollbackStatus=N'NoOp', Reason=N'Pilot execute was blocked by prevalidation; no receipt/journal rows were inserted.';
