/*
Phase 9 - Test Artifacts Cleanup / Reset
Target: PilotClone/Sandbox only.
Purpose: remove web validation test vouchers/terminations created with Notes LIKE 'Phase9%'
while keeping migrated 283 contracts, cross references, opening balances, advance staging, and operational seed.
SQL Server 2012 compatible.
*/
SET NOCOUNT ON;

IF DB_NAME() NOT LIKE '%PropertyPilot%' AND DB_NAME() NOT LIKE '%PilotClone%' AND DB_NAME() NOT LIKE '%Sandbox%'
BEGIN
    RAISERROR('Unsafe database name. This cleanup is allowed only on PropertyPilot/PilotClone/Sandbox databases.', 16, 1);
    RETURN;
END;

IF DB_NAME() IN ('Alromaizan', 'Adnan', 'RSMDB')
BEGIN
    RAISERROR('Production/source database blocked. Cleanup cancelled.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @ReceiptPageId int = 22;
    DECLARE @IssuePageId int = 21;
    DECLARE @TerminationPageId int = 10459;

    DECLARE @ReceiptIds TABLE (Id int PRIMARY KEY);
    DECLARE @IssueIds TABLE (Id int PRIMARY KEY);
    DECLARE @TerminationIds TABLE (Id int PRIMARY KEY);

    INSERT INTO @ReceiptIds(Id)
    SELECT Id FROM dbo.CashReceiptVoucher WHERE Notes LIKE 'Phase9%';

    INSERT INTO @IssueIds(Id)
    SELECT Id FROM dbo.CashIssueVoucher WHERE Notes LIKE 'Phase9%';

    INSERT INTO @TerminationIds(Id)
    SELECT Id FROM dbo.PropertyContractTermination WHERE Notes LIKE 'Phase9%';

    /* Restore unit status for contracts affected by Phase9 termination tests before deleting the termination. */
    UPDATE pd
       SET pd.StatusId = 1
    FROM dbo.PropertyDetail pd
    INNER JOIN dbo.PropertyContract pc ON pc.PropertyUnitId = pd.Id
    INNER JOIN dbo.PropertyContractTermination pct ON pct.PropertyContractId = pc.Id
    INNER JOIN @TerminationIds t ON t.Id = pct.Id;

    UPDATE pd
       SET pd.StatusId = 1
    FROM dbo.PropertyDetail pd
    INNER JOIN dbo.PropertyContractMergedUnit mu ON mu.PropertyUnitId = pd.Id
    INNER JOIN dbo.PropertyContractTermination pct ON pct.PropertyContractId = mu.PropertyContractId
    INNER JOIN @TerminationIds t ON t.Id = pct.Id;

    DELETE jd
    FROM dbo.JournalEntryDetail jd
    INNER JOIN dbo.JournalEntry je ON je.Id = jd.JournalEntryId
    WHERE (je.SourcePageId = @ReceiptPageId AND je.SourceId IN (SELECT Id FROM @ReceiptIds))
       OR (je.SourcePageId = @IssuePageId AND je.SourceId IN (SELECT Id FROM @IssueIds))
       OR (je.SourcePageId = @TerminationPageId AND je.SourceId IN (SELECT Id FROM @TerminationIds));

    DELETE je
    FROM dbo.JournalEntry je
    WHERE (je.SourcePageId = @ReceiptPageId AND je.SourceId IN (SELECT Id FROM @ReceiptIds))
       OR (je.SourcePageId = @IssuePageId AND je.SourceId IN (SELECT Id FROM @IssueIds))
       OR (je.SourcePageId = @TerminationPageId AND je.SourceId IN (SELECT Id FROM @TerminationIds));

    DELETE FROM dbo.CashReceiptVoucherPropertyContractBatch
    WHERE CashReceiptVoucherId IN (SELECT Id FROM @ReceiptIds);

    DELETE FROM dbo.CashReceiptVoucher
    WHERE Id IN (SELECT Id FROM @ReceiptIds);

    DELETE iad
    FROM dbo.IssueAnalysisDetail iad
    INNER JOIN dbo.IssueAnalysis ia ON ia.Id = iad.IssueAnalysisId
    WHERE ia.CashIssueVoucherId IN (SELECT Id FROM @IssueIds);

    DELETE FROM dbo.IssueAnalysis
    WHERE CashIssueVoucherId IN (SELECT Id FROM @IssueIds);

    DELETE FROM dbo.CashIssueVoucher
    WHERE Id IN (SELECT Id FROM @IssueIds);

    DELETE FROM dbo.PropertyContractTerminationDamage
    WHERE PropertyContractTerminationId IN (SELECT Id FROM @TerminationIds);

    DELETE FROM dbo.PropertyContractTerminationDetail
    WHERE MainDocId IN (SELECT Id FROM @TerminationIds);

    DELETE FROM dbo.PropertyContractTermination
    WHERE Id IN (SELECT Id FROM @TerminationIds);

    SELECT 'CleanupResult' AS Section,
           (SELECT COUNT(*) FROM dbo.CashReceiptVoucher WHERE Notes LIKE 'Phase9%') AS RemainingPhase9Receipts,
           (SELECT COUNT(*) FROM dbo.CashIssueVoucher WHERE Notes LIKE 'Phase9%') AS RemainingPhase9Issues,
           (SELECT COUNT(*) FROM dbo.PropertyContractTermination WHERE Notes LIKE 'Phase9%') AS RemainingPhase9Terminations,
           (SELECT COUNT(*) FROM dbo.PropertyContract WHERE DocumentNumber LIKE 'ADNAN-C-%') AS RemainingMigratedContracts,
           (SELECT COUNT(*) FROM dbo.PropertyPilotAdvancePaymentStaging WHERE MigrationBatchId='F9EAD000-0000-4000-9000-202605200009') AS RemainingAdvanceStagingRows;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err nvarchar(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;
