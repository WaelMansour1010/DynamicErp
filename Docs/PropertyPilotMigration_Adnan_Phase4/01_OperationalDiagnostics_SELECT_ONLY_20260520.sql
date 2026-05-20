/*
01_OperationalDiagnostics_SELECT_ONLY_20260520.sql
Purpose: Operational master data diagnostics for Adnan vs DynamicErp Sandbox.
Mode: SELECT ONLY.
*/

SELECT 'Adnan_TblUsers' Metric, COUNT(*) Value FROM Adnan.dbo.TblUsers
UNION ALL SELECT 'Adnan_ActiveUsers', COUNT(*) FROM Adnan.dbo.TblUsers WHERE ISNULL(IsActive,1)=1
UNION ALL SELECT 'Adnan_TblEmployee', COUNT(*) FROM Adnan.dbo.TblEmployee
UNION ALL SELECT 'Adnan_Branches', COUNT(*) FROM Adnan.dbo.TblBranchesData
UNION ALL SELECT 'Adnan_Boxes', COUNT(*) FROM Adnan.dbo.TblBoxesData
UNION ALL SELECT 'Adnan_Banks', COUNT(*) FROM Adnan.dbo.BanksData
UNION ALL SELECT 'Adnan_UserBoxes', COUNT(*) FROM Adnan.dbo.TblUsersBoxes
UNION ALL SELECT 'Adnan_UserBranches', COUNT(*) FROM Adnan.dbo.TblUsersBranches
UNION ALL SELECT 'Adnan_PaymentTypes', COUNT(*) FROM Adnan.dbo.TblPaymentType
UNION ALL SELECT 'Sandbox_ERPUser', COUNT(*) FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.ERPUser
UNION ALL SELECT 'Sandbox_ActiveERPUser', COUNT(*) FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.ERPUser WHERE IsDeleted=0 AND IsActive=1
UNION ALL SELECT 'Sandbox_Employee', COUNT(*) FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.Employee
UNION ALL SELECT 'Sandbox_Branch', COUNT(*) FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.Branch
UNION ALL SELECT 'Sandbox_ActiveBranch', COUNT(*) FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.Branch WHERE IsDeleted=0 AND IsActive=1
UNION ALL SELECT 'Sandbox_Department', COUNT(*) FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.Department
UNION ALL SELECT 'Sandbox_CashBox', COUNT(*) FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.CashBox
UNION ALL SELECT 'Sandbox_ActiveCashBox', COUNT(*) FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.CashBox WHERE IsDeleted=0 AND IsActive=1
UNION ALL SELECT 'Sandbox_Bank', COUNT(*) FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.Bank
UNION ALL SELECT 'Sandbox_BankAccount', COUNT(*) FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.BankAccount
UNION ALL SELECT 'Sandbox_CashReceiptPaymentMethod', COUNT(*) FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.CashReceiptPaymentMethod
UNION ALL SELECT 'Sandbox_CashIssuePaymentMethod', COUNT(*) FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.CashIssuePaymentMethod
UNION ALL SELECT 'Sandbox_UserCashBox', COUNT(*) FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.UserCashBox
UNION ALL SELECT 'Sandbox_UserDepartment', COUNT(*) FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.UserDepartment;

SELECT 'ERPUser' Entity, Id, UserName, Name, EmployeeId, RoleId, CustodyBoxId, IsCashier, IsActive, IsDeleted
FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.ERPUser
ORDER BY Id;

SELECT cb.Id, cb.Code, cb.ArName, cb.AccountId, ca.Code AccountCode, ca.ArName AccountName, cb.DepartmentId, d.ArName DepartmentName, cb.EmpId
FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.CashBox cb
LEFT JOIN Alromaizan_PropertyPilot_Adnan_20260520.dbo.ChartOfAccount ca ON ca.Id=cb.AccountId
LEFT JOIN Alromaizan_PropertyPilot_Adnan_20260520.dbo.Department d ON d.Id=cb.DepartmentId;

SELECT ba.Id, ba.AccountNumber, ba.AccountId, ca.Code AccountCode, ca.ArName AccountName, ba.BankId, b.ArName BankName, ba.BankAccountReceiptId, ba.BankAccountPaymentId
FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.BankAccount ba
LEFT JOIN Alromaizan_PropertyPilot_Adnan_20260520.dbo.ChartOfAccount ca ON ca.Id=ba.AccountId
LEFT JOIN Alromaizan_PropertyPilot_Adnan_20260520.dbo.Bank b ON b.Id=ba.BankId;

SELECT d.Id, d.Code, d.ArName, d.RenterAndBuyerAccountId, d.DueRentId, d.RentRevenueId, d.WaterDueRevenueId, d.ElectricityDueRevenueId, d.PropertyRefundInsuranceId
FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.Department d;
