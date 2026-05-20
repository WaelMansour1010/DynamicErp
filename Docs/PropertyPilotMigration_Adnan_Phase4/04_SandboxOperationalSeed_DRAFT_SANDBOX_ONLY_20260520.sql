/*
04_SandboxOperationalSeed_DRAFT_SANDBOX_ONLY_20260520.sql
Purpose: Minimum operational setup for Property Pilot web validation in Sandbox only.
Status: DRAFT ONLY. Review before execution.
*/

IF DB_NAME() IN (N'Adnan', N'Alromaizan') OR (DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%Sandbox%')
BEGIN
    RAISERROR('Blocked: this script can run only inside a PropertyPilot/Sandbox database, never Adnan or Alromaizan.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRAN;

    DECLARE @CompanyId INT, @DepartmentId INT, @AdminUserId INT, @CashBoxId INT, @CashAccountId INT, @BankId INT, @BankAccountId INT;

    SELECT TOP 1 @CompanyId = Id FROM dbo.Company ORDER BY Id;
    SELECT TOP 1 @DepartmentId = Id FROM dbo.Department WHERE IsDeleted=0 AND IsActive=1 ORDER BY Id;
    SELECT TOP 1 @AdminUserId = Id FROM dbo.ERPUser WHERE IsDeleted=0 AND IsActive=1 ORDER BY SystemAdmin DESC, Id;
    SELECT TOP 1 @CashBoxId = Id, @CashAccountId = AccountId FROM dbo.CashBox WHERE IsDeleted=0 AND IsActive=1 ORDER BY Id;
    SELECT TOP 1 @BankId = Id FROM dbo.Bank WHERE IsDeleted=0 AND IsActive=1 ORDER BY Id;
    SELECT TOP 1 @BankAccountId = Id FROM dbo.BankAccount WHERE IsDeleted=0 AND IsActive=1 ORDER BY Id;

    IF @DepartmentId IS NULL RAISERROR('No active Department found in sandbox.',16,1);
    IF @AdminUserId IS NULL RAISERROR('No active ERPUser found in sandbox.',16,1);
    IF @CashBoxId IS NULL RAISERROR('No active CashBox found in sandbox.',16,1);

    IF NOT EXISTS (SELECT 1 FROM dbo.Branch WHERE Code=N'ADNAN-PILOT' AND Notes LIKE N'%PropertyPilot Adnan%')
    BEGIN
        INSERT INTO dbo.Branch(Code, CompanyId, ArName, EnName, CreationDate, BranchManager, Notes, UserId, IsDeleted, IsActive)
        VALUES(N'ADNAN-PILOT', @CompanyId, N'فرع بايلوت أملاك عدنان', N'Adnan Property Pilot Branch', GETDATE(), N'Pilot', N'PropertyPilot Adnan sandbox branch', @AdminUserId, 0, 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.UserDepartment WHERE UserId=@AdminUserId AND DepartmentId=@DepartmentId)
    BEGIN
        INSERT INTO dbo.UserDepartment(UserId, DepartmentId, Privilege)
        VALUES(@AdminUserId, @DepartmentId, 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.UserCashBox WHERE UserId=@AdminUserId AND CashBoxId=@CashBoxId)
    BEGIN
        INSERT INTO dbo.UserCashBox(UserId, CashBoxId, Privilege)
        VALUES(@AdminUserId, @CashBoxId, 1);
    END;

    UPDATE dbo.ERPUser
    SET IsCashier=1, CustodyBoxId=@CashBoxId
    WHERE Id=@AdminUserId AND (ISNULL(IsCashier,0)=0 OR CustodyBoxId IS NULL);

    IF NOT EXISTS (SELECT 1 FROM dbo.CashReceiptPaymentMethod WHERE Code=N'CASH-PILOT')
        INSERT INTO dbo.CashReceiptPaymentMethod(Code, ArName, EnName, IsActive, IsDeleted) VALUES(N'CASH-PILOT', N'نقدي - بايلوت', N'Cash Pilot', 1, 0);

    IF NOT EXISTS (SELECT 1 FROM dbo.CashReceiptPaymentMethod WHERE Code=N'BANK-PILOT')
        INSERT INTO dbo.CashReceiptPaymentMethod(Code, ArName, EnName, IsActive, IsDeleted) VALUES(N'BANK-PILOT', N'بنك - بايلوت', N'Bank Pilot', 1, 0);

    IF NOT EXISTS (SELECT 1 FROM dbo.CashIssuePaymentMethod WHERE Code=N'CASH-PILOT')
        INSERT INTO dbo.CashIssuePaymentMethod(Code, ArName, EnName, IsActive, IsDeleted) VALUES(N'CASH-PILOT', N'نقدي - بايلوت', N'Cash Pilot', 1, 0);

    IF NOT EXISTS (SELECT 1 FROM dbo.CashIssuePaymentMethod WHERE Code=N'BANK-PILOT')
        INSERT INTO dbo.CashIssuePaymentMethod(Code, ArName, EnName, IsActive, IsDeleted) VALUES(N'BANK-PILOT', N'بنك - بايلوت', N'Bank Pilot', 1, 0);

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    DECLARE @Msg NVARCHAR(4000); SET @Msg = ERROR_MESSAGE();
    RAISERROR(@Msg, 16, 1);
END CATCH;

SELECT 'Branch' Entity, COUNT(*) Cnt FROM dbo.Branch WHERE Notes LIKE N'%PropertyPilot Adnan%'
UNION ALL SELECT 'CashReceiptPaymentMethod', COUNT(*) FROM dbo.CashReceiptPaymentMethod WHERE Code LIKE N'%-PILOT'
UNION ALL SELECT 'CashIssuePaymentMethod', COUNT(*) FROM dbo.CashIssuePaymentMethod WHERE Code LIKE N'%-PILOT';
