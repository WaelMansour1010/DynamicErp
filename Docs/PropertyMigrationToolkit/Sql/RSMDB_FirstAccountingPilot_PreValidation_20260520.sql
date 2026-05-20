/* RSMDB First Accounting Pilot PreValidation - clone only */
DECLARE @BatchId uniqueidentifier='$(MigrationBatchId)';
IF DB_NAME() NOT LIKE '%Clone%' AND DB_NAME() NOT LIKE '%Sandbox%' AND DB_NAME() NOT LIKE '%PropertyPilot%' AND DB_NAME() NOT LIKE '%ReadyToTest%' AND DB_NAME() NOT LIKE '%PilotClone%' AND DB_NAME() NOT LIKE '%Migration%'
BEGIN RAISERROR('Clone database required.',16,1); RETURN; END;
IF DB_NAME() IN ('RSMDB','Adnan','Alromaizan','MyErp') BEGIN RAISERROR('Blocked database.',16,1); RETURN; END;

;WITH Ready AS (
    SELECT CONVERT(int, SourceId) NoteID
    FROM dbo.PropertyMigrationResolutionResult
    WHERE MigrationBatchId=@BatchId AND EntityType=N'Journal' AND ResolutionStatus=N'ReadyForMigration_FinanceApproved'
), Type4 AS (
    SELECT r.NoteID
    FROM Ready r JOIN RSMDB.dbo.Notes n ON n.NoteID=r.NoteID
    WHERE n.NoteType=4
), ScopeRows AS (
    SELECT t.NoteID, sr.SourceContractId, sr.SourceInstallmentId, sr.SourceRenterId, sr.MoneyAmount
    FROM Type4 t
    LEFT JOIN dbo.PropertyMigrationSourceReceipt sr ON sr.MigrationBatchId=@BatchId AND sr.SourceId=CONVERT(nvarchar(400),t.NoteID)
)
SELECT ReadyType4=COUNT(*), WithContract=SUM(CASE WHEN SourceContractId IS NOT NULL THEN 1 ELSE 0 END), WithInstallment=SUM(CASE WHEN SourceInstallmentId IS NOT NULL THEN 1 ELSE 0 END), WithRenter=SUM(CASE WHEN SourceRenterId IS NOT NULL THEN 1 ELSE 0 END), ReceiptTotal=SUM(MoneyAmount)
FROM ScopeRows;

IF EXISTS (
    SELECT 1 FROM ScopeRows
    WHERE SourceContractId IS NULL OR SourceInstallmentId IS NULL OR SourceRenterId IS NULL
)
BEGIN
    RAISERROR('PreValidation failed: pilot receipts are not safely linked to contract/installment/renter. Accounting pilot execute is blocked.',16,1);
    RETURN;
END;
