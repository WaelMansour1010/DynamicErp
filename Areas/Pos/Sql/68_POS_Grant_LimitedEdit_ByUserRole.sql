/*
    Kishny POS - Grant limited sales invoice edit permissions by user role
    SQL Server 2012 compatible.

    Rules:
    - Teller users get CanEditSalesInvoicePos only.
      This permission is enforced in code as same-day invoices only.
    - Admin users get CanEditSalesInvoice.
      This permission is still limited-field editing, not unrestricted invoice editing.
    - Non-admin tellers must not keep CanEditSalesInvoice from this script, to preserve today-only editing.
*/

IF OBJECT_ID(N'dbo.POS_UserPermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_UserPermissions
    (
        UserID INT NOT NULL,
        PermissionKey NVARCHAR(100) NOT NULL,
        IsAllowed BIT NOT NULL CONSTRAINT DF_POS_UserPermissions_IsAllowed DEFAULT(0),
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_POS_UserPermissions_UpdatedAt DEFAULT(GETDATE()),
        CONSTRAINT PK_POS_UserPermissions PRIMARY KEY(UserID, PermissionKey)
    );
END;
GO

IF OBJECT_ID(N'tempdb..#PosRolePermissionUsers') IS NOT NULL
    DROP TABLE #PosRolePermissionUsers;
GO

CREATE TABLE #PosRolePermissionUsers
(
    UserID INT NOT NULL PRIMARY KEY,
    IsAdmin BIT NOT NULL,
    IsTeller BIT NOT NULL
);
GO

INSERT INTO #PosRolePermissionUsers(UserID, IsAdmin, IsTeller)
SELECT
    u.UserID,
    CASE WHEN ISNULL(u.UserType, -1) = 0 THEN 1 ELSE 0 END AS IsAdmin,
    CASE
        WHEN ISNULL(u.UserType, -1) = 0 THEN 0
        WHEN LTRIM(RTRIM(ISNULL(u.UserCategory, N''))) IN (N'تلر', N'Teller', N'teller') THEN 1
        WHEN EXISTS
        (
            SELECT 1
            FROM dbo.POS_UserPermissions p
            WHERE p.UserID = u.UserID
              AND p.PermissionKey = N'CanTeller'
              AND ISNULL(p.IsAllowed, 0) = 1
        ) THEN 1
        ELSE 0
    END AS IsTeller
FROM dbo.TblUsers u
WHERE u.UserID IS NOT NULL
  AND
  (
      ISNULL(u.UserType, -1) = 0
      OR LTRIM(RTRIM(ISNULL(u.UserCategory, N''))) IN (N'تلر', N'Teller', N'teller')
      OR EXISTS
      (
          SELECT 1
          FROM dbo.POS_UserPermissions p
          WHERE p.UserID = u.UserID
            AND p.PermissionKey = N'CanTeller'
            AND ISNULL(p.IsAllowed, 0) = 1
      )
  );
GO

/* Admins: limited general sales invoice edit. */
UPDATE p
SET IsAllowed = 1,
    UpdatedAt = GETDATE()
FROM dbo.POS_UserPermissions p
INNER JOIN #PosRolePermissionUsers u ON u.UserID = p.UserID
WHERE u.IsAdmin = 1
  AND p.PermissionKey = N'CanEditSalesInvoice'
  AND ISNULL(p.IsAllowed, 0) <> 1;
GO

INSERT INTO dbo.POS_UserPermissions(UserID, PermissionKey, IsAllowed, UpdatedAt)
SELECT u.UserID, N'CanEditSalesInvoice', 1, GETDATE()
FROM #PosRolePermissionUsers u
WHERE u.IsAdmin = 1
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.POS_UserPermissions p
      WHERE p.UserID = u.UserID
        AND p.PermissionKey = N'CanEditSalesInvoice'
  );
GO

/* Tellers: POS same-day limited edit only. */
UPDATE p
SET IsAllowed = 1,
    UpdatedAt = GETDATE()
FROM dbo.POS_UserPermissions p
INNER JOIN #PosRolePermissionUsers u ON u.UserID = p.UserID
WHERE u.IsTeller = 1
  AND p.PermissionKey = N'CanEditSalesInvoicePos'
  AND ISNULL(p.IsAllowed, 0) <> 1;
GO

INSERT INTO dbo.POS_UserPermissions(UserID, PermissionKey, IsAllowed, UpdatedAt)
SELECT u.UserID, N'CanEditSalesInvoicePos', 1, GETDATE()
FROM #PosRolePermissionUsers u
WHERE u.IsTeller = 1
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.POS_UserPermissions p
      WHERE p.UserID = u.UserID
        AND p.PermissionKey = N'CanEditSalesInvoicePos'
  );
GO

/*
    Preserve today-only rule for non-admin tellers.
    If a teller was previously given the broader permission, disable it unless they are admin.
*/
UPDATE p
SET IsAllowed = 0,
    UpdatedAt = GETDATE()
FROM dbo.POS_UserPermissions p
INNER JOIN #PosRolePermissionUsers u ON u.UserID = p.UserID
WHERE u.IsTeller = 1
  AND u.IsAdmin = 0
  AND p.PermissionKey = N'CanEditSalesInvoice'
  AND ISNULL(p.IsAllowed, 0) <> 0;
GO

SELECT
    SUM(CASE WHEN IsAdmin = 1 THEN 1 ELSE 0 END) AS AdminUsersGrantedCanEditSalesInvoice,
    SUM(CASE WHEN IsTeller = 1 THEN 1 ELSE 0 END) AS TellerUsersGrantedTodayOnlyCanEditSalesInvoicePos
FROM #PosRolePermissionUsers;
GO

DROP TABLE #PosRolePermissionUsers;
GO
