/*
Phase 10 - ReadyToTest Reset/Rollback Draft
Target DB: Alromaizan_PropertyPilot_Adnan_ReadyToTest_20260520 or another ReadyToTest/PropertyPilot clone only.
Status: DRAFT. Do not run without explicit approval.
Purpose:
  1) Optional cleanup of Phase9/Phase10 test artifacts.
  2) Optional full rollback of migration batch F9EAD000-0000-4000-9000-202605200009.

Safety:
  - Blocks Alromaizan, Adnan, RSMDB.
  - Requires DB name to contain PropertyPilot, ReadyToTest, PilotClone, or Sandbox.
  - Full rollback requires @ExecuteFullMigrationRollback = 1 and @Confirm = 'YES_RESET_READY_TO_TEST'.
*/
SET NOCOUNT ON;

DECLARE @MigrationBatchId uniqueidentifier = 'F9EAD000-0000-4000-9000-202605200009';
DECLARE @ExecuteTestArtifactCleanup bit = 1;
DECLARE @ExecuteFullMigrationRollback bit = 0;
DECLARE @Confirm nvarchar(100) = N'NO';

IF DB_NAME() IN ('Alromaizan', 'Adnan', 'RSMDB')
BEGIN
    RAISERROR('Blocked: production/source database.', 16, 1);
    RETURN;
END;

IF DB_NAME() NOT LIKE '%PropertyPilot%'
   AND DB_NAME() NOT LIKE '%ReadyToTest%'
   AND DB_NAME() NOT LIKE '%PilotClone%'
   AND DB_NAME() NOT LIKE '%Sandbox%'
BEGIN
    RAISERROR('Blocked: database name is not a safe clone/sandbox name.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    IF @ExecuteTestArtifactCleanup = 1
    BEGIN
        DECLARE @ReceiptPageId int = 22;
        DECLARE @IssuePageId int = 21;
        DECLARE @TerminationPageId int = 10459;

        DECLARE @ReceiptIds TABLE (Id int PRIMARY KEY);
        DECLARE @IssueIds TABLE (Id int PRIMARY KEY);
        DECLARE @TerminationIds TABLE (Id int PRIMARY KEY);

        INSERT INTO @ReceiptIds(Id)
        SELECT Id FROM dbo.CashReceiptVoucher
        WHERE Notes LIKE 'Phase9%' OR Notes LIKE 'Phase10%' OR Notes LIKE '%test%' OR Notes LIKE N'%اختبار%';

        INSERT INTO @IssueIds(Id)
        SELECT Id FROM dbo.CashIssueVoucher
        WHERE Notes LIKE 'Phase9%' OR Notes LIKE 'Phase10%' OR Notes LIKE '%test%' OR Notes LIKE N'%اختبار%';

        INSERT INTO @TerminationIds(Id)
        SELECT Id FROM dbo.PropertyContractTermination
        WHERE Notes LIKE 'Phase9%' OR Notes LIKE 'Phase10%' OR Notes LIKE '%test%' OR Notes LIKE N'%اختبار%';

        UPDATE pd SET pd.StatusId = 1
        FROM dbo.PropertyDetail pd
        INNER JOIN dbo.PropertyContract pc ON pc.PropertyUnitId = pd.Id
        INNER JOIN dbo.PropertyContractTermination pct ON pct.PropertyContractId = pc.Id
        INNER JOIN @TerminationIds t ON t.Id = pct.Id;

        DELETE jd
        FROM dbo.JournalEntryDetail jd
        INNER JOIN dbo.JournalEntry je ON je.Id = jd.JournalEntryId
        WHERE (je.SourcePageId = @ReceiptPageId AND je.SourceId IN (SELECT Id FROM @ReceiptIds))
           OR (je.SourcePageId = @IssuePageId AND je.SourceId IN (SELECT Id FROM @IssueIds))
           OR (je.SourcePageId = @TerminationPageId AND je.SourceId IN (SELECT Id FROM @TerminationIds))
           OR je.Notes LIKE 'Phase9%' OR je.Notes LIKE 'Phase10%' OR je.Notes LIKE '%test%' OR je.Notes LIKE N'%اختبار%';

        DELETE je
        FROM dbo.JournalEntry je
        WHERE (je.SourcePageId = @ReceiptPageId AND je.SourceId IN (SELECT Id FROM @ReceiptIds))
           OR (je.SourcePageId = @IssuePageId AND je.SourceId IN (SELECT Id FROM @IssueIds))
           OR (je.SourcePageId = @TerminationPageId AND je.SourceId IN (SELECT Id FROM @TerminationIds))
           OR je.Notes LIKE 'Phase9%' OR je.Notes LIKE 'Phase10%' OR je.Notes LIKE '%test%' OR je.Notes LIKE N'%اختبار%';

        DELETE FROM dbo.CashReceiptVoucherPropertyContractBatch WHERE CashReceiptVoucherId IN (SELECT Id FROM @ReceiptIds);
        DELETE FROM dbo.CashReceiptVoucher WHERE Id IN (SELECT Id FROM @ReceiptIds);

        DELETE iad FROM dbo.IssueAnalysisDetail iad
        INNER JOIN dbo.IssueAnalysis ia ON ia.Id = iad.IssueAnalysisId
        WHERE ia.CashIssueVoucherId IN (SELECT Id FROM @IssueIds);
        DELETE FROM dbo.IssueAnalysis WHERE CashIssueVoucherId IN (SELECT Id FROM @IssueIds);
        DELETE FROM dbo.CashIssueVoucher WHERE Id IN (SELECT Id FROM @IssueIds);

        DELETE FROM dbo.PropertyContractTerminationDamage WHERE PropertyContractTerminationId IN (SELECT Id FROM @TerminationIds);
        DELETE FROM dbo.PropertyContractTerminationDetail WHERE MainDocId IN (SELECT Id FROM @TerminationIds);
        DELETE FROM dbo.PropertyContractTermination WHERE Id IN (SELECT Id FROM @TerminationIds);
    END;

    IF @ExecuteFullMigrationRollback = 1
    BEGIN
        IF @Confirm <> N'YES_RESET_READY_TO_TEST'
        BEGIN
            RAISERROR('Full migration rollback requires explicit @Confirm value.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        DELETE FROM dbo.PropertyPilotAdvancePaymentStaging WHERE MigrationBatchId = @MigrationBatchId;
        DELETE FROM dbo.PropertyPilotOpeningBalanceStaging WHERE MigrationBatchId = @MigrationBatchId;

        DELETE b
        FROM dbo.PropertyContractBatch b
        INNER JOIN dbo.PropertyPilotCrossReference x ON x.NewId = b.Id
        WHERE x.MigrationBatchId = @MigrationBatchId AND x.EntityType = 'ContractBatch';

        DELETE c
        FROM dbo.PropertyContract c
        INNER JOIN dbo.PropertyPilotCrossReference x ON x.NewId = c.Id
        WHERE x.MigrationBatchId = @MigrationBatchId AND x.EntityType = 'Contract';

        DELETE u
        FROM dbo.PropertyDetail u
        INNER JOIN dbo.PropertyPilotCrossReference x ON x.NewId = u.Id
        WHERE x.MigrationBatchId = @MigrationBatchId AND x.EntityType = 'Unit';

        DELETE p
        FROM dbo.Property p
        INNER JOIN dbo.PropertyPilotCrossReference x ON x.NewId = p.Id
        WHERE x.MigrationBatchId = @MigrationBatchId AND x.EntityType = 'Property';

        DELETE r
        FROM dbo.PropertyRenter r
        INNER JOIN dbo.PropertyPilotCrossReference x ON x.NewId = r.Id
        WHERE x.MigrationBatchId = @MigrationBatchId AND x.EntityType = 'Tenant';

        DELETE ca
        FROM dbo.ChartOfAccount ca
        INNER JOIN dbo.PropertyPilotCrossReference x ON x.NewId = ca.Id
        WHERE x.MigrationBatchId = @MigrationBatchId AND x.EntityType = 'Account';

        DELETE FROM dbo.PropertyPilotCrossReference WHERE MigrationBatchId = @MigrationBatchId;
        UPDATE dbo.PropertyPilotMigrationBatch SET Status='RolledBack', Notes=ISNULL(Notes,'') + ' | Rolled back by Phase10 draft script.' WHERE MigrationBatchId = @MigrationBatchId;
    END;

    SELECT 'DraftResult' AS Section,
           DB_NAME() AS DatabaseName,
           (SELECT COUNT(*) FROM dbo.CashReceiptVoucher WHERE Notes LIKE 'Phase9%' OR Notes LIKE 'Phase10%' OR Notes LIKE '%test%' OR Notes LIKE N'%اختبار%') AS TestReceipts,
           (SELECT COUNT(*) FROM dbo.CashIssueVoucher WHERE Notes LIKE 'Phase9%' OR Notes LIKE 'Phase10%' OR Notes LIKE '%test%' OR Notes LIKE N'%اختبار%') AS TestIssues,
           (SELECT COUNT(*) FROM dbo.PropertyContractTermination WHERE Notes LIKE 'Phase9%' OR Notes LIKE 'Phase10%' OR Notes LIKE '%test%' OR Notes LIKE N'%اختبار%') AS TestTerminations,
           (SELECT COUNT(*) FROM dbo.PropertyContract WHERE DocumentNumber LIKE 'ADNAN-C-%') AS MigratedContracts;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err nvarchar(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;
