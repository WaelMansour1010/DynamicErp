IF DB_NAME() IN (N'Adnan', N'Alromaizan') OR (DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%Sandbox%')
BEGIN
    RAISERROR('Blocked: Sandbox/PropertyPilot database required.',16,1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRAN;

    SET IDENTITY_INSERT dbo.CashReceiptPaymentMethod ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.CashReceiptPaymentMethod WHERE Id = 1)
        INSERT INTO dbo.CashReceiptPaymentMethod(Id, Code, ArName, EnName, IsActive, IsDeleted)
        VALUES(1, N'CASH-COMPAT', N'نقدي - متوافق بايلوت', N'Cash - Pilot Compat', 1, 0);
    IF NOT EXISTS (SELECT 1 FROM dbo.CashReceiptPaymentMethod WHERE Id = 2)
        INSERT INTO dbo.CashReceiptPaymentMethod(Id, Code, ArName, EnName, IsActive, IsDeleted)
        VALUES(2, N'BANK-COMPAT', N'بنك - متوافق بايلوت', N'Bank - Pilot Compat', 1, 0);
    SET IDENTITY_INSERT dbo.CashReceiptPaymentMethod OFF;

    SET IDENTITY_INSERT dbo.CashIssuePaymentMethod ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.CashIssuePaymentMethod WHERE Id = 1)
        INSERT INTO dbo.CashIssuePaymentMethod(Id, Code, ArName, EnName, IsActive, IsDeleted)
        VALUES(1, N'CASH-COMPAT', N'نقدي - متوافق بايلوت', N'Cash - Pilot Compat', 1, 0);
    IF NOT EXISTS (SELECT 1 FROM dbo.CashIssuePaymentMethod WHERE Id = 2)
        INSERT INTO dbo.CashIssuePaymentMethod(Id, Code, ArName, EnName, IsActive, IsDeleted)
        VALUES(2, N'BANK-COMPAT', N'بنك - متوافق بايلوت', N'Bank - Pilot Compat', 1, 0);
    SET IDENTITY_INSERT dbo.CashIssuePaymentMethod OFF;

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF OBJECTPROPERTY(OBJECT_ID('dbo.CashReceiptPaymentMethod'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.CashReceiptPaymentMethod OFF;
    IF OBJECTPROPERTY(OBJECT_ID('dbo.CashIssuePaymentMethod'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT dbo.CashIssuePaymentMethod OFF;
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    DECLARE @Msg NVARCHAR(4000); SET @Msg = ERROR_MESSAGE();
    RAISERROR(@Msg,16,1);
END CATCH;

SELECT 'ReceiptMethods' Metric, Id, Code, ArName FROM dbo.CashReceiptPaymentMethod WHERE Id IN (1,2,5,6) ORDER BY Id;
SELECT 'IssueMethods' Metric, Id, Code, ArName FROM dbo.CashIssuePaymentMethod WHERE Id IN (1,2,5,6) ORDER BY Id;

