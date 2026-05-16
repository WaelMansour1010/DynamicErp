/*
Legacy HR/Finance Dania QA seed
Date: 2026-05-14

Purpose:
Creates a small, clearly marked QA data set for runtime verification of the
MainErp legacy HR/Finance migration screens on database Dania.

Safety:
- SQL Server 2012 compatible.
- Idempotent by QA code/name.
- Does not create notes or DOUBLE_ENTREY_VOUCHERS.
- Uses existing branch/account references from Dania.

Rollback:
Run the cleanup block at the end after the QA cycle if persistent test rows are
not desired.
*/

BEGIN TRANSACTION;

DECLARE @BranchId int;
DECLARE @AccountCode nvarchar(50);
DECLARE @CurrencyId int;
DECLARE @EmployeeId int;
DECLARE @ComponentId int;
DECLARE @BankId int;
DECLARE @BoxId int;

SELECT TOP (1) @BranchId = branch_id
FROM dbo.TblBranchesData WITH (NOLOCK)
ORDER BY branch_id;

SELECT TOP (1) @AccountCode = Account_Code
FROM dbo.ACCOUNTS WITH (NOLOCK)
WHERE ISNULL(last_account, 0) = 1
ORDER BY Account_Code;

SELECT TOP (1) @CurrencyId = id
FROM dbo.currency WITH (NOLOCK)
ORDER BY id;

IF @BranchId IS NULL OR @AccountCode IS NULL
BEGIN
    RAISERROR('Dania QA seed requires at least one branch and one last account.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.TblEmployee WHERE Emp_Code = N'QA-MIG-HR-001')
BEGIN
    SELECT @EmployeeId = ISNULL(MAX(Emp_ID), 0) + 1 FROM dbo.TblEmployee WITH (UPDLOCK, HOLDLOCK);

    INSERT INTO dbo.TblEmployee
    (Emp_ID, Emp_Code, Emp_Name, BranchId, DepartmentID, JobTypeID, BignDateWork, chkStop, Emp_Salary, Account_code, Account_code1, Emp_Phone, Emp_mobile, Emp_Mail, EmpNotes)
    VALUES
    (@EmployeeId, N'QA-MIG-HR-001', N'موظف اختبار ترحيل HR Finance', @BranchId, 1, 1, GETDATE(), 0, 3500, @AccountCode, @AccountCode, N'0111111111', N'0500000000', N'qa@example.local', N'QA migration runtime seed');
END
ELSE
BEGIN
    SELECT @EmployeeId = Emp_ID FROM dbo.TblEmployee WHERE Emp_Code = N'QA-MIG-HR-001';
END;

IF NOT EXISTS (SELECT 1 FROM dbo.BanksData WHERE BankName = N'QA_MIG_BANK_DANIA')
BEGIN
    SELECT @BankId = ISNULL(MAX(BankID), 0) + 1 FROM dbo.BanksData WITH (UPDLOCK, HOLDLOCK);

    INSERT INTO dbo.BanksData
    (BankID, BankName, BankNamee, Remarks, Account_Code, BranchId, Commision, OpenBalanceDate, OpenBalanceType, OpenBalance, account_no, IBan, Tel, Address, Email, Currency_ID, chkapprov, chkLoan)
    VALUES
    (@BankId, N'QA_MIG_BANK_DANIA', N'QA Migration Bank Dania', N'QA migration runtime seed', @AccountCode, @BranchId, 0, GETDATE(), 1, 1250, N'QA-ACC-001', N'SA00QA0001', N'0111111111', N'QA address', N'qa-bank@example.local', @CurrencyId, 0, 0);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.tblBoxesData WHERE BoxName = N'QA_MIG_BOX_DANIA')
BEGIN
    SELECT @BoxId = ISNULL(MAX(BoxID), 0) + 1 FROM dbo.tblBoxesData WITH (UPDLOCK, HOLDLOCK);

    INSERT INTO dbo.tblBoxesData
    (BoxID, BoxName, BoxNameE, Comments, Account_Code, Type, empid, BranchId, ChequeBox, OpenBalanceDate, OpenBalanceType, OpenBalance, boxValue, Priod, PriodDMY)
    VALUES
    (@BoxId, N'QA_MIG_BOX_DANIA', N'QA Migration Box Dania', N'QA migration runtime seed', @AccountCode, 0, @EmployeeId, @BranchId, 0, GETDATE(), 1, 500, 2500, 1, 2);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.mofrad WHERE name = N'QA_MIG_COMPONENT_DANIA')
BEGIN
    SELECT @ComponentId = ISNULL(MAX(id), 0) + 1 FROM dbo.mofrad WITH (UPDLOCK, HOLDLOCK);

    INSERT INTO dbo.mofrad
    (id, name, nameE, AddOrDiscount, FixedOrChanged, Unit, Account_Code, Account_code1, ViewComp, Salary, Absence, Late, OverTime, Insurances, Reward, AllowIntrod)
    VALUES
    (@ComponentId, N'QA_MIG_COMPONENT_DANIA', N'QA Migration Component Dania', 1, 1, 1, @AccountCode, @AccountCode, 1, 1, 0, 0, 0, 0, 0, 0);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.emp_salary WHERE emp_id = @EmployeeId AND sgn = '209911')
BEGIN
    INSERT INTO dbo.emp_salary
    (emp_id, Emp_Code, Emp_Name, Emp_Salary, total1, TotalAdvance, TotalDiscount, total2, EmpTotalNet, sgn, m_year, m_month, payed, DepartmentID, BranchId, RecordDate)
    VALUES
    (@EmployeeId, N'QA-MIG-HR-001', N'موظف اختبار ترحيل HR Finance', 3500, 3500, 0, 0, 0, 3500, '209911', '2099', 'November', 0, 1, @BranchId, GETDATE());
END;

COMMIT TRANSACTION;

/*
-- Cleanup block
BEGIN TRANSACTION;
DELETE FROM dbo.emp_salary WHERE emp_id IN (SELECT Emp_ID FROM dbo.TblEmployee WHERE Emp_Code = N'QA-MIG-HR-001');
DELETE FROM dbo.TblEmployee WHERE Emp_Code = N'QA-MIG-HR-001' AND EmpNotes = N'QA migration runtime seed';
DELETE FROM dbo.BanksData WHERE BankName = N'QA_MIG_BANK_DANIA';
DELETE FROM dbo.tblBoxesData WHERE BoxName = N'QA_MIG_BOX_DANIA';
DELETE FROM dbo.mofrad WHERE name = N'QA_MIG_COMPONENT_DANIA';
COMMIT TRANSACTION;
*/
