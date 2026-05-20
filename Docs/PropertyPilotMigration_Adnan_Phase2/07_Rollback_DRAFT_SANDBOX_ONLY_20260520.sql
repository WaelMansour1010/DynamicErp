/*
07_Rollback_DRAFT_SANDBOX_ONLY_20260520.sql
Purpose: Batch rollback draft for Sandbox-only Pilot Migration.
Status: DRAFT SANDBOX ONLY. Do not execute on production.

Rollback principle:
- Delete only rows linked to @MigrationBatchId in dbo.PropertyPilotCrossReference.
- Delete child rows before parent rows.
- Never run on Alromaizan.
- Never run unless DB_NAME contains PropertyPilot or Sandbox.
*/

DECLARE @MigrationBatchId UNIQUEIDENTIFIER;
DECLARE @ConfirmSandboxRollback NVARCHAR(50);

/* Set manually before approved sandbox rollback */
SET @MigrationBatchId = NULL;
SET @ConfirmSandboxRollback = NULL; -- must be: ROLLBACK SANDBOX BATCH

IF DB_NAME() = N'Alromaizan' OR (DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%Sandbox%')
BEGIN
    RAISERROR('Blocked: rollback can run only inside PropertyPilot/Sandbox database, never Alromaizan.', 16, 1);
    RETURN;
END;

IF @MigrationBatchId IS NULL
BEGIN
    RAISERROR('Blocked: @MigrationBatchId is required.', 16, 1);
    RETURN;
END;

IF @ConfirmSandboxRollback <> N'ROLLBACK SANDBOX BATCH'
BEGIN
    RAISERROR('Blocked: confirmation phrase is required.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRAN;

    /* Preview inside transaction */
    SELECT EntityType, NewTableName, COUNT(*) AS RowsToRollback
    FROM dbo.PropertyPilotCrossReference
    WHERE MigrationBatchId = @MigrationBatchId
    GROUP BY EntityType, NewTableName
    ORDER BY EntityType, NewTableName;

    DELETE crvpcb
    FROM dbo.CashReceiptVoucherPropertyContractBatch crvpcb
    INNER JOIN dbo.PropertyPilotCrossReference x ON x.NewTableName=N'CashReceiptVoucher' AND x.NewId=crvpcb.CashReceiptVoucherId
    WHERE x.MigrationBatchId=@MigrationBatchId;

    DELETE crv
    FROM dbo.CashReceiptVoucher crv
    INNER JOIN dbo.PropertyPilotCrossReference x ON x.NewTableName=N'CashReceiptVoucher' AND x.NewId=crv.Id
    WHERE x.MigrationBatchId=@MigrationBatchId;

    DELETE pcb
    FROM dbo.PropertyContractBatch pcb
    INNER JOIN dbo.PropertyPilotCrossReference x ON x.NewTableName=N'PropertyContractBatch' AND x.NewId=pcb.Id
    WHERE x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'ContractBatch';

    DELETE pc
    FROM dbo.PropertyContract pc
    INNER JOIN dbo.PropertyPilotCrossReference x ON x.NewTableName=N'PropertyContract' AND x.NewId=pc.Id
    WHERE x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'Contract';

    DELETE pd
    FROM dbo.PropertyDetail pd
    INNER JOIN dbo.PropertyPilotCrossReference x ON x.NewTableName=N'PropertyDetail' AND x.NewId=pd.Id
    WHERE x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'Unit';

    DELETE pr
    FROM dbo.PropertyRenter pr
    INNER JOIN dbo.PropertyPilotCrossReference x ON x.NewTableName=N'PropertyRenter' AND x.NewId=pr.Id
    WHERE x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'Tenant';

    DELETE p
    FROM dbo.Property p
    INNER JOIN dbo.PropertyPilotCrossReference x ON x.NewTableName=N'Property' AND x.NewId=p.Id
    WHERE x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'Property';

    DELETE ca
    FROM dbo.ChartOfAccount ca
    INNER JOIN dbo.PropertyPilotCrossReference x ON x.NewTableName=N'ChartOfAccount' AND x.NewId=ca.Id
    WHERE x.MigrationBatchId=@MigrationBatchId AND x.EntityType=N'Account';

    DELETE FROM dbo.PropertyPilotOpeningBalanceStaging WHERE MigrationBatchId=@MigrationBatchId;
    DELETE FROM dbo.PropertyPilotAccountMapping WHERE MigrationBatchId=@MigrationBatchId;
    DELETE FROM dbo.PropertyPilotValidationIssue WHERE MigrationBatchId=@MigrationBatchId;
    DELETE FROM dbo.PropertyPilotCrossReference WHERE MigrationBatchId=@MigrationBatchId;

    UPDATE dbo.PropertyPilotMigrationBatch
    SET Status=N'RolledBack', Notes=ISNULL(Notes,N'') + N' Rolled back at ' + CONVERT(NVARCHAR(30), GETDATE(), 120)
    WHERE MigrationBatchId=@MigrationBatchId;

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    DECLARE @Msg NVARCHAR(4000); SET @Msg = ERROR_MESSAGE();
    RAISERROR(@Msg, 16, 1);
END CATCH;
