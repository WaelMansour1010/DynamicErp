/*
Legacy HR/Finance Dania permission QA users
Date: 2026-05-14

Purpose:
Creates stable non-admin QA users for MainErp permission hardening tests.
The script is SQL Server 2012 compatible and idempotent.

Users:
- QA_HRFIN_VIEW: view/search only.
- QA_HRFIN_ADD: view/search/add only.
- QA_HRFIN_EDIT: view/search/edit only.
- QA_HRFIN_DENIED: explicit denied access.

Source mapping:
- Kishny source screens: FrmBanksData, FrmBoxesData, FrmEmployee, FrmEmpSalary5.
- Main Original screens: MOFRAD, FrmEmpsAdvanceRequest,
  FrmVocationEntitlements, FrmRegsterSickleave,
  FrmChangedComponentData, FrmChangedComponentData1.
*/

BEGIN TRANSACTION;

DECLARE @BranchId int;
DECLARE @EmpId int;
DECLARE @NextUserId int;

SELECT TOP (1) @BranchId = branch_id
FROM dbo.TblBranchesData WITH (NOLOCK)
ORDER BY branch_id;

SELECT TOP (1) @EmpId = Emp_ID
FROM dbo.TblEmployee WITH (NOLOCK)
WHERE Emp_Code = N'QA-MIG-HR-001'
ORDER BY Emp_ID;

IF @BranchId IS NULL
BEGIN
    RAISERROR('Permission QA users require at least one branch in TblBranchesData.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

DECLARE @Users TABLE
(
    UserName nvarchar(50) NOT NULL,
    CanShow bit NOT NULL,
    CanAdd bit NOT NULL,
    CanEdit bit NOT NULL,
    CanDelete bit NOT NULL,
    FullAccess bit NOT NULL
);

INSERT INTO @Users (UserName, CanShow, CanAdd, CanEdit, CanDelete, FullAccess)
VALUES
(N'QA_HRFIN_VIEW', 1, 0, 0, 0, 0),
(N'QA_HRFIN_ADD', 1, 1, 0, 0, 0),
(N'QA_HRFIN_EDIT', 1, 0, 1, 0, 0),
(N'QA_HRFIN_DENIED', 0, 0, 0, 0, 0);

DECLARE @UserName nvarchar(50);

DECLARE user_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT UserName FROM @Users;

OPEN user_cursor;
FETCH NEXT FROM user_cursor INTO @UserName;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.TblUsers WHERE UserName = @UserName)
    BEGIN
        SELECT @NextUserId = ISNULL(MAX(UserID), 0) + 1
        FROM dbo.TblUsers WITH (UPDLOCK, HOLDLOCK);

        INSERT INTO dbo.TblUsers
        (UserID, UserName, [PassWord], UserType, FullPremis, IsActive, BranchId, Empid, isDeactivated)
        VALUES
        (@NextUserId, @UserName, N'Alex2025', 2, 2, 1, @BranchId, @EmpId, 0);
    END
    ELSE
    BEGIN
        UPDATE dbo.TblUsers
        SET [PassWord] = N'Alex2025',
            UserType = 2,
            FullPremis = 2,
            IsActive = 1,
            BranchId = @BranchId,
            Empid = ISNULL(@EmpId, Empid),
            isDeactivated = 0
        WHERE UserName = @UserName;
    END;

    FETCH NEXT FROM user_cursor INTO @UserName;
END;

CLOSE user_cursor;
DEALLOCATE user_cursor;

DECLARE @Screens TABLE (ScreenName nvarchar(50) NOT NULL);
INSERT INTO @Screens (ScreenName)
VALUES
(N'FrmBanksData'),
(N'FrmBoxesData'),
(N'FrmEmployee'),
(N'FrmEmpSalary5'),
(N'MOFRAD'),
(N'FrmEmpsAdvanceRequest'),
(N'FrmVocationEntitlements'),
(N'FrmRegsterSickleave'),
(N'FrmChangedComponentData'),
(N'FrmChangedComponentData1');

DELETE sju
FROM dbo.ScreenJuncUser sju
INNER JOIN dbo.TblUsers u ON u.UserID = sju.User_ID
INNER JOIN @Users qa ON qa.UserName = u.UserName
INNER JOIN @Screens screens ON screens.ScreenName = sju.ScreenName;

INSERT INTO dbo.ScreenJuncUser
(ScreenName, User_ID, CanAdd, CanEdit, CanDelete, CanPrint, CanSearch, FullAccess, CanShow, Attachments)
SELECT
    screens.ScreenName,
    u.UserID,
    qa.CanAdd,
    qa.CanEdit,
    qa.CanDelete,
    qa.CanShow,
    qa.CanShow,
    qa.FullAccess,
    qa.CanShow,
    qa.CanEdit
FROM dbo.TblUsers u
INNER JOIN @Users qa ON qa.UserName = u.UserName
CROSS JOIN @Screens screens;

COMMIT TRANSACTION;

SELECT u.UserID, u.UserName, p.ScreenName, p.CanShow, p.CanAdd, p.CanEdit, p.CanDelete, p.FullAccess
FROM dbo.TblUsers u
INNER JOIN dbo.ScreenJuncUser p ON p.User_ID = u.UserID
WHERE u.UserName IN (N'QA_HRFIN_VIEW', N'QA_HRFIN_ADD', N'QA_HRFIN_EDIT', N'QA_HRFIN_DENIED')
  AND p.ScreenName IN
  (
      N'FrmBanksData', N'FrmBoxesData', N'FrmEmployee', N'FrmEmpSalary5',
      N'MOFRAD', N'FrmEmpsAdvanceRequest', N'FrmVocationEntitlements',
      N'FrmRegsterSickleave', N'FrmChangedComponentData', N'FrmChangedComponentData1'
  )
ORDER BY u.UserName, p.ScreenName;
