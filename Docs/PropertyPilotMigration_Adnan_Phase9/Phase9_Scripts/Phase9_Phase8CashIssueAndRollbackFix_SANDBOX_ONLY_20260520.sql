/* Phase8_SQL_DRAFT_OR_SANDBOX_ONLY_20260520.sql
   Scope: CashIssueVoucher direct-expense safety and rollback cleanup.
   Production: DRAFT ONLY. Execute write sections only on Sandbox/PropertyPilot after review.
*/

/* SELECT diagnostics */
SELECT d.Id DepartmentId, d.Code, d.ArName, d.DirectExpensesAccountId,
       ca.Code DirectExpensesAccountCode, ca.ArName DirectExpensesAccountName
FROM dbo.Department d
LEFT JOIN dbo.ChartOfAccount ca ON ca.Id=d.DirectExpensesAccountId
WHERE ISNULL(d.IsDeleted,0)=0 AND ISNULL(d.IsActive,1)=1;

SELECT de.Id DirectExpensesId, de.Code, de.ArName, de.AccountId,
       ca.Code AccountCode, ca.ArName AccountName
FROM dbo.DirectExpenses de
LEFT JOIN dbo.ChartOfAccount ca ON ca.Id=de.AccountId
WHERE ISNULL(de.IsDeleted,0)=0 AND ISNULL(de.IsActive,1)=1;

SELECT 'DepartmentDirectExpenseEqualsCashBox' CheckName, d.Id DepartmentId, d.DirectExpensesAccountId, cb.Id CashBoxId, cb.AccountId CashBoxAccountId
FROM dbo.Department d
JOIN dbo.CashBox cb ON cb.AccountId=d.DirectExpensesAccountId AND ISNULL(cb.IsDeleted,0)=0 AND ISNULL(cb.IsActive,1)=1
WHERE ISNULL(d.IsDeleted,0)=0 AND ISNULL(d.IsActive,1)=1;

/* SANDBOX ONLY: configure Pilot department direct-expense account to the selected direct expense account used in tests. */
IF DB_NAME() IN (N'Adnan', N'Alromaizan') OR (DB_NAME() NOT LIKE N'%PropertyPilot%' AND DB_NAME() NOT LIKE N'%Sandbox%')
BEGIN
    RAISERROR('Blocked: Phase8 write section is Sandbox/PropertyPilot only.', 16, 1);
    RETURN;
END;

DECLARE @PilotDepartmentId INT = 44;
DECLARE @PilotDirectExpensesId INT = 4;
DECLARE @SafeDirectExpenseAccountId INT;
SELECT @SafeDirectExpenseAccountId = AccountId FROM dbo.DirectExpenses WHERE Id=@PilotDirectExpensesId AND ISNULL(IsDeleted,0)=0 AND ISNULL(IsActive,1)=1;

IF @SafeDirectExpenseAccountId IS NULL
BEGIN
    RAISERROR('Blocked: selected DirectExpenses row has no AccountId.', 16, 1);
    RETURN;
END;

IF EXISTS (SELECT 1 FROM dbo.CashBox WHERE AccountId=@SafeDirectExpenseAccountId AND ISNULL(IsDeleted,0)=0 AND ISNULL(IsActive,1)=1)
BEGIN
    RAISERROR('Blocked: selected direct expense account is a cashbox account.', 16, 1);
    RETURN;
END;

UPDATE dbo.Department
SET DirectExpensesAccountId=@SafeDirectExpenseAccountId,
    Notes=ISNULL(Notes,N'') + N' Phase8 sandbox: DirectExpensesAccountId aligned to DirectExpensesId=4 account.'
WHERE Id=@PilotDepartmentId;

SELECT d.Id DepartmentId, d.DirectExpensesAccountId, ca.Code, ca.ArName
FROM dbo.Department d
LEFT JOIN dbo.ChartOfAccount ca ON ca.Id=d.DirectExpensesAccountId
WHERE d.Id=@PilotDepartmentId;

/* SANDBOX/DRAFT Rollback procedure fix: add this delete inside usp_PropertyPilot_RollbackBatch_Adnan before deleting account mapping/xref. */
-- DELETE FROM dbo.PropertyPilotAdvancePaymentStaging WHERE MigrationBatchId=@MigrationBatchId;
