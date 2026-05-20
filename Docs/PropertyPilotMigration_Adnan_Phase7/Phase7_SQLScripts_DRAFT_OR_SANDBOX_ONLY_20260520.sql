/*
Phase7_SQLScripts_DRAFT_OR_SANDBOX_ONLY_20260520.sql
Purpose: Payment method diagnostics and guarded sandbox compatibility seed.
Safety: Do not run write sections on Adnan, Alromaizan, or any production database.
*/

/* SECTION 1 - SELECT ONLY diagnostics: safe to run for review */
SELECT 'CashReceiptPaymentMethod' AS TableName, Id, Code, ArName, EnName, IsActive, IsDeleted
FROM dbo.CashReceiptPaymentMethod
ORDER BY Id;

SELECT 'CashIssuePaymentMethod' AS TableName, Id, Code, ArName, EnName, IsActive, IsDeleted
FROM dbo.CashIssuePaymentMethod
ORDER BY Id;

SELECT 'CashBoxWithoutAccount' AS CheckName, Id, Code, ArName, AccountId
FROM dbo.CashBox
WHERE ISNULL(IsDeleted,0)=0 AND ISNULL(IsActive,1)=1 AND AccountId IS NULL;

SELECT 'BankAccountWithoutPostingAccounts' AS CheckName, Id, AccountNumber, AccountId, BankAccountReceiptId, BankAccountPaymentId
FROM dbo.BankAccount
WHERE ISNULL(IsDeleted,0)=0 AND ISNULL(IsActive,1)=1
  AND AccountId IS NULL AND BankAccountReceiptId IS NULL AND BankAccountPaymentId IS NULL;

SELECT 'CashReceiptJournalNullAccount' AS CheckName, je.Id JournalEntryId, je.SourceId CashReceiptVoucherId, jed.Id DetailId
FROM dbo.JournalEntry je
JOIN dbo.JournalEntryDetail jed ON jed.JournalEntryId = je.Id
WHERE je.SourcePageId = (SELECT TOP 1 Id FROM dbo.SystemPage WHERE TableName='CashReceiptVoucher' OR ControllerName='CashReceiptVoucher')
  AND ISNULL(je.IsDeleted,0)=0 AND ISNULL(jed.IsDeleted,0)=0 AND jed.AccountId IS NULL;

SELECT 'CashIssueJournalNullAccount' AS CheckName, je.Id JournalEntryId, je.SourceId CashIssueVoucherId, jed.Id DetailId
FROM dbo.JournalEntry je
JOIN dbo.JournalEntryDetail jed ON jed.JournalEntryId = je.Id
WHERE je.SourcePageId = (SELECT TOP 1 Id FROM dbo.SystemPage WHERE TableName='CashIssueVoucher' OR ControllerName='CashIssueVoucher')
  AND ISNULL(je.IsDeleted,0)=0 AND ISNULL(jed.IsDeleted,0)=0 AND jed.AccountId IS NULL;

/* SECTION 2 - SANDBOX ONLY compatibility seed for legacy SP behavior */
IF DB_NAME() IN (N'Adnan', N'Alromaizan') OR (DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%Sandbox%')
BEGIN
    RAISERROR('Blocked: compatibility seed is Sandbox/PropertyPilot only.', 16, 1);
    RETURN;
END;

BEGIN TRAN;
SET IDENTITY_INSERT dbo.CashReceiptPaymentMethod ON;
IF NOT EXISTS (SELECT 1 FROM dbo.CashReceiptPaymentMethod WHERE Id=1)
    INSERT INTO dbo.CashReceiptPaymentMethod(Id, Code, ArName, EnName, IsActive, IsDeleted)
    VALUES(1, N'CASH-COMPAT', N'نقدي - متوافق بايلوت', N'Cash - Pilot Compat', 1, 0);
IF NOT EXISTS (SELECT 1 FROM dbo.CashReceiptPaymentMethod WHERE Id=2)
    INSERT INTO dbo.CashReceiptPaymentMethod(Id, Code, ArName, EnName, IsActive, IsDeleted)
    VALUES(2, N'BANK-COMPAT', N'بنك - متوافق بايلوت', N'Bank - Pilot Compat', 1, 0);
SET IDENTITY_INSERT dbo.CashReceiptPaymentMethod OFF;

SET IDENTITY_INSERT dbo.CashIssuePaymentMethod ON;
IF NOT EXISTS (SELECT 1 FROM dbo.CashIssuePaymentMethod WHERE Id=1)
    INSERT INTO dbo.CashIssuePaymentMethod(Id, Code, ArName, EnName, IsActive, IsDeleted)
    VALUES(1, N'CASH-COMPAT', N'نقدي - متوافق بايلوت', N'Cash - Pilot Compat', 1, 0);
IF NOT EXISTS (SELECT 1 FROM dbo.CashIssuePaymentMethod WHERE Id=2)
    INSERT INTO dbo.CashIssuePaymentMethod(Id, Code, ArName, EnName, IsActive, IsDeleted)
    VALUES(2, N'BANK-COMPAT', N'بنك - متوافق بايلوت', N'Bank - Pilot Compat', 1, 0);
SET IDENTITY_INSERT dbo.CashIssuePaymentMethod OFF;
COMMIT;

SELECT 'ReceiptMethods' AS Metric, Id, Code, ArName FROM dbo.CashReceiptPaymentMethod ORDER BY Id;
SELECT 'IssueMethods' AS Metric, Id, Code, ArName FROM dbo.CashIssuePaymentMethod ORDER BY Id;

/* SECTION 3 - PRODUCTION DRAFT ONLY
Recommendation before production: keep Id 1/2 validation as compatibility baseline, but do not execute any seed
on a customer database until the current IDs are inspected and approved. If Id 1/2 are occupied by different meanings,
create an explicit data-fix plan instead of forcing identity values.
*/
