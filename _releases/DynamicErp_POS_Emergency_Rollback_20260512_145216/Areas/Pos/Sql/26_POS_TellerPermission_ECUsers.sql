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

UPDATE p
SET IsAllowed = 1,
    UpdatedAt = GETDATE()
FROM dbo.POS_UserPermissions p
INNER JOIN dbo.TblUsers u ON u.UserID = p.UserID
WHERE p.PermissionKey = N'CanTeller'
  AND u.UserName LIKE N'EC%'
  AND ISNULL(p.IsAllowed, 0) = 0;
GO

INSERT INTO dbo.POS_UserPermissions(UserID, PermissionKey, IsAllowed, UpdatedAt)
SELECT u.UserID, N'CanTeller', 1, GETDATE()
FROM dbo.TblUsers u
WHERE u.UserName LIKE N'EC%'
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.POS_UserPermissions p
      WHERE p.UserID = u.UserID
        AND p.PermissionKey = N'CanTeller'
  );
GO
