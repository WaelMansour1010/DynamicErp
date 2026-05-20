/*
03_UsersEmployeesDiagnostics_SELECT_ONLY_20260520.sql
Purpose: Diagnose users, employees, roles, privileges, branch/cashbox permissions.
Mode: SELECT ONLY.
*/

SELECT UserID, UserName, UserType, FullPremis, IsActive, BranchId, StoreID, BoxID, BankID, Empid
FROM Adnan.dbo.TblUsers
ORDER BY UserID;

SELECT u.UserID, u.UserName, b.BranchID, bd.branch_name
FROM Adnan.dbo.TblUsers u
LEFT JOIN Adnan.dbo.TblUsersBranches b ON b.userid=u.UserID
LEFT JOIN Adnan.dbo.TblBranchesData bd ON bd.branch_id=b.BranchID
ORDER BY u.UserID, b.BranchID;

SELECT u.UserID, u.UserName, ub.BoxId, bx.BoxName
FROM Adnan.dbo.TblUsers u
LEFT JOIN Adnan.dbo.TblUsersBoxes ub ON ub.userid=u.UserID
LEFT JOIN Adnan.dbo.TblBoxesData bx ON bx.BoxID=ub.BoxId
ORDER BY u.UserID, ub.BoxId;

SELECT Id, UserName, Name, EmployeeId, RoleId, SystemAdmin, IsCashier, CustodyBoxId, IsActive, IsDeleted
FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.ERPUser
ORDER BY Id;

SELECT r.Id, r.Code, r.ArName, r.EnName, r.IsActive, r.IsDeleted, COUNT(rp.Id) RolePrivilegeCount
FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.ERPRole r
LEFT JOIN Alromaizan_PropertyPilot_Adnan_20260520.dbo.RolePrivilege rp ON rp.RoleId=r.Id
GROUP BY r.Id, r.Code, r.ArName, r.EnName, r.IsActive, r.IsDeleted
ORDER BY r.Id;

SELECT ucb.Id, ucb.UserId, eu.UserName, ucb.CashBoxId, cb.ArName CashBoxName, ucb.Privilege
FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.UserCashBox ucb
LEFT JOIN Alromaizan_PropertyPilot_Adnan_20260520.dbo.ERPUser eu ON eu.Id=ucb.UserId
LEFT JOIN Alromaizan_PropertyPilot_Adnan_20260520.dbo.CashBox cb ON cb.Id=ucb.CashBoxId
ORDER BY ucb.UserId, ucb.CashBoxId;

SELECT ud.Id, ud.UserId, eu.UserName, ud.DepartmentId, d.ArName DepartmentName, ud.Privilege
FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.UserDepartment ud
LEFT JOIN Alromaizan_PropertyPilot_Adnan_20260520.dbo.ERPUser eu ON eu.Id=ud.UserId
LEFT JOIN Alromaizan_PropertyPilot_Adnan_20260520.dbo.Department d ON d.Id=ud.DepartmentId
ORDER BY ud.UserId, ud.DepartmentId;
