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
END

IF OBJECT_ID(N'dbo.POS_SalesInvoiceEditLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_SalesInvoiceEditLog
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_SalesInvoiceEditLog PRIMARY KEY,
        Transaction_ID INT NOT NULL,
        OldValues NVARCHAR(MAX) NULL,
        NewValues NVARCHAR(MAX) NULL,
        UserId INT NOT NULL,
        EditDateTime DATETIME NOT NULL CONSTRAINT DF_POS_SalesInvoiceEditLog_EditDateTime DEFAULT(GETDATE()),
        EditReason NVARCHAR(500) NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_SalesInvoiceEditLog_Transaction_ID' AND object_id = OBJECT_ID(N'dbo.POS_SalesInvoiceEditLog'))
BEGIN
    CREATE INDEX IX_POS_SalesInvoiceEditLog_Transaction_ID
        ON dbo.POS_SalesInvoiceEditLog(Transaction_ID, EditDateTime);
END

INSERT INTO dbo.POS_UserPermissions(UserID, PermissionKey, IsAllowed, UpdatedAt)
SELECT u.UserID, p.PermissionKey, 0, GETDATE()
FROM dbo.TblUsers u
CROSS JOIN
(
    SELECT N'CanEditSalesInvoice' AS PermissionKey
    UNION ALL
    SELECT N'CanEditSalesInvoicePos'
) p
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.POS_UserPermissions existing
    WHERE existing.UserID = u.UserID
      AND existing.PermissionKey = p.PermissionKey
);
