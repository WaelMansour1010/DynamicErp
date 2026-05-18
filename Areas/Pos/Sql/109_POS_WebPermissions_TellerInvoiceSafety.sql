SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.WebScreens', N'U') IS NULL
   OR OBJECT_ID(N'dbo.WebScreenPermissions', N'U') IS NULL
   OR OBJECT_ID(N'dbo.TblUsers', N'U') IS NULL
BEGIN
    RAISERROR(N'Web permission catalog is not ready. Run 106_WebScreenPermissions_Catalog.sql before this script.', 16, 1);
    RETURN;
END;

DECLARE @SalesScreenId INT;
SELECT @SalesScreenId = WebScreenId
FROM dbo.WebScreens
WHERE ScreenKey COLLATE DATABASE_DEFAULT = N'POS.Sales.Index' COLLATE DATABASE_DEFAULT
  AND IsActive = 1;

IF @SalesScreenId IS NULL
BEGIN
    RAISERROR(N'POS.Sales.Index is missing from dbo.WebScreens.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'tempdb..#TellerPermissionRaw') IS NOT NULL
    DROP TABLE #TellerPermissionRaw;

CREATE TABLE #TellerPermissionRaw
(
    UserId INT NOT NULL,
    SourceNotes NVARCHAR(500) COLLATE DATABASE_DEFAULT NULL,
    CanView BIT NOT NULL DEFAULT(0),
    CanAdd BIT NOT NULL DEFAULT(0),
    CanEdit BIT NOT NULL DEFAULT(0),
    CanDelete BIT NOT NULL DEFAULT(0),
    CanPrint BIT NOT NULL DEFAULT(0)
);

IF OBJECT_ID(N'tempdb..#TellerInvoiceUsers') IS NOT NULL
    DROP TABLE #TellerInvoiceUsers;

CREATE TABLE #TellerInvoiceUsers
(
    UserId INT NOT NULL PRIMARY KEY,
    SourceNotes NVARCHAR(500) COLLATE DATABASE_DEFAULT NULL,
    CanView BIT NOT NULL DEFAULT(0),
    CanAdd BIT NOT NULL DEFAULT(0),
    CanEdit BIT NOT NULL DEFAULT(0),
    CanDelete BIT NOT NULL DEFAULT(0),
    CanPrint BIT NOT NULL DEFAULT(0)
);

IF OBJECT_ID(N'tempdb..#TellerCategories') IS NOT NULL
    DROP TABLE #TellerCategories;

CREATE TABLE #TellerCategories(CategoryName NVARCHAR(100) COLLATE DATABASE_DEFAULT NOT NULL PRIMARY KEY);

INSERT INTO #TellerCategories(CategoryName)
VALUES
    (N'Teller'),
    (N'Cashier'),
    (N'EC'),
    (NCHAR(1578) + NCHAR(1604) + NCHAR(1585)), -- Arabic: teller
    (NCHAR(1603) + NCHAR(1575) + NCHAR(1588) + NCHAR(1610) + NCHAR(1585)); -- Arabic: cashier

IF OBJECT_ID(N'dbo.POS_UserPermissions', N'U') IS NOT NULL
BEGIN
    ;WITH LegacyInvoicePermissions AS
    (
        SELECT
            p.UserID AS UserId,
            MAX(CASE WHEN p.PermissionKey COLLATE DATABASE_DEFAULT IN (N'CanTeller') THEN 1 ELSE 0 END) AS HasTeller,
            MAX(CASE WHEN p.PermissionKey COLLATE DATABASE_DEFAULT IN (N'CanOpenSales') THEN 1 ELSE 0 END) AS HasOpen,
            MAX(CASE WHEN p.PermissionKey COLLATE DATABASE_DEFAULT IN (N'CanSaveInvoice') THEN 1 ELSE 0 END) AS HasSave,
            MAX(CASE WHEN p.PermissionKey COLLATE DATABASE_DEFAULT IN (N'CanEditSalesInvoice', N'CanEditSalesInvoicePos', N'CanEditInvoice') THEN 1 ELSE 0 END) AS HasEdit,
            MAX(CASE WHEN p.PermissionKey COLLATE DATABASE_DEFAULT IN (N'CanCancelInvoice', N'CanCancelOrReturn') THEN 1 ELSE 0 END) AS HasCancel
        FROM dbo.POS_UserPermissions p
        WHERE ISNULL(p.IsAllowed, 0) = 1
          AND p.PermissionKey COLLATE DATABASE_DEFAULT IN
          (
              N'CanTeller',
              N'CanOpenSales',
              N'CanSaveInvoice',
              N'CanEditInvoice',
              N'CanEditSalesInvoice',
              N'CanEditSalesInvoicePos',
              N'CanCancelInvoice',
              N'CanCancelOrReturn'
          )
        GROUP BY p.UserID
    )
    INSERT INTO #TellerPermissionRaw(UserId, SourceNotes, CanView, CanAdd, CanEdit, CanDelete, CanPrint)
    SELECT
        UserId,
        N'POS_UserPermissions',
        CONVERT(BIT, 1),
        CONVERT(BIT, CASE WHEN HasTeller = 1 OR HasSave = 1 THEN 1 ELSE 0 END),
        CONVERT(BIT, CASE WHEN HasTeller = 1 OR HasEdit = 1 THEN 1 ELSE 0 END),
        CONVERT(BIT, CASE WHEN HasCancel = 1 THEN 1 ELSE 0 END),
        CONVERT(BIT, CASE WHEN HasTeller = 1 OR HasSave = 1 OR HasEdit = 1 THEN 1 ELSE 0 END)
    FROM LegacyInvoicePermissions;
END;

IF COL_LENGTH(N'dbo.TblUsers', N'UserCategory') IS NOT NULL
BEGIN
    INSERT INTO #TellerPermissionRaw(UserId, SourceNotes, CanView, CanAdd, CanEdit, CanDelete, CanPrint)
    SELECT
        u.UserID,
        N'TblUsers.UserCategory',
        CONVERT(BIT, 1),
        CONVERT(BIT, 1),
        CONVERT(BIT, 1),
        CONVERT(BIT, 0),
        CONVERT(BIT, 1)
    FROM dbo.TblUsers u
    INNER JOIN #TellerCategories c
        ON LTRIM(RTRIM(ISNULL(u.UserCategory, N''))) COLLATE DATABASE_DEFAULT = c.CategoryName COLLATE DATABASE_DEFAULT
    WHERE u.UserID IS NOT NULL;
END;

IF COL_LENGTH(N'dbo.TblUsers', N'CanEditSalesInvoice') IS NOT NULL
BEGIN
    EXEC(N'
    INSERT INTO #TellerPermissionRaw(UserId, SourceNotes, CanView, CanAdd, CanEdit, CanDelete, CanPrint)
    SELECT
        UserID,
        N''TblUsers.CanEditSalesInvoice'',
        CONVERT(BIT, 1),
        CONVERT(BIT, 0),
        CONVERT(BIT, 1),
        CONVERT(BIT, 0),
        CONVERT(BIT, 1)
    FROM dbo.TblUsers
    WHERE ISNULL(CanEditSalesInvoice, 0) = 1
      AND UserID IS NOT NULL;');
END;

IF COL_LENGTH(N'dbo.TblUsers', N'CanEditSalesInvoicePos') IS NOT NULL
BEGIN
    EXEC(N'
    INSERT INTO #TellerPermissionRaw(UserId, SourceNotes, CanView, CanAdd, CanEdit, CanDelete, CanPrint)
    SELECT
        UserID,
        N''TblUsers.CanEditSalesInvoicePos'',
        CONVERT(BIT, 1),
        CONVERT(BIT, 0),
        CONVERT(BIT, 1),
        CONVERT(BIT, 0),
        CONVERT(BIT, 1)
    FROM dbo.TblUsers
    WHERE ISNULL(CanEditSalesInvoicePos, 0) = 1
      AND UserID IS NOT NULL;');
END;

IF OBJECT_ID(N'dbo.ScreenJuncUser', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.WebScreenLegacyMap', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ScreenJuncUser', N'User_ID') IS NOT NULL
BEGIN
    INSERT INTO #TellerPermissionRaw(UserId, SourceNotes, CanView, CanAdd, CanEdit, CanDelete, CanPrint)
    SELECT
        sj.User_ID,
        N'ScreenJuncUser',
        CONVERT(BIT, 1),
        CONVERT(BIT, MAX(CASE WHEN ISNULL(sj.CanAdd, 0) = 1 OR ISNULL(sj.FullAccess, 0) = 1 THEN 1 ELSE 0 END)),
        CONVERT(BIT, MAX(CASE WHEN ISNULL(sj.CanEdit, 0) = 1 OR ISNULL(sj.FullAccess, 0) = 1 THEN 1 ELSE 0 END)),
        CONVERT(BIT, MAX(CASE WHEN ISNULL(sj.CanDelete, 0) = 1 OR ISNULL(sj.FullAccess, 0) = 1 THEN 1 ELSE 0 END)),
        CONVERT(BIT, MAX(CASE WHEN ISNULL(sj.CanPrint, 0) = 1 OR ISNULL(sj.FullAccess, 0) = 1 THEN 1 ELSE 0 END))
    FROM dbo.ScreenJuncUser sj
    INNER JOIN dbo.WebScreenLegacyMap lm
        ON lm.LegacyScreenName COLLATE DATABASE_DEFAULT = sj.ScreenName COLLATE DATABASE_DEFAULT
       AND lm.ScreenKey COLLATE DATABASE_DEFAULT = N'POS.Sales.Index' COLLATE DATABASE_DEFAULT
    WHERE sj.User_ID IS NOT NULL
    GROUP BY sj.User_ID;
END;

INSERT INTO #TellerInvoiceUsers(UserId, SourceNotes, CanView, CanAdd, CanEdit, CanDelete, CanPrint)
SELECT
    raw.UserId,
    STUFF
    (
        (
            SELECT DISTINCT N'; ' + raw2.SourceNotes
            FROM #TellerPermissionRaw raw2
            WHERE raw2.UserId = raw.UserId
            FOR XML PATH(''), TYPE
        ).value('.', 'nvarchar(max)'),
        1,
        2,
        N''
    ),
    CONVERT(BIT, MAX(CONVERT(INT, raw.CanView))),
    CONVERT(BIT, MAX(CONVERT(INT, raw.CanAdd))),
    CONVERT(BIT, MAX(CONVERT(INT, raw.CanEdit))),
    CONVERT(BIT, MAX(CONVERT(INT, raw.CanDelete))),
    CONVERT(BIT, MAX(CONVERT(INT, raw.CanPrint)))
FROM #TellerPermissionRaw raw
GROUP BY raw.UserId;

IF OBJECT_ID(N'tempdb..#AppliedWebPermissionSafety') IS NOT NULL
    DROP TABLE #AppliedWebPermissionSafety;

CREATE TABLE #AppliedWebPermissionSafety
(
    ActionName NVARCHAR(10) COLLATE DATABASE_DEFAULT NOT NULL,
    UserId INT NOT NULL,
    WebScreenId INT NOT NULL
);

INSERT INTO #AppliedWebPermissionSafety(ActionName, UserId, WebScreenId)
SELECT N'UPDATE', target.UserId, target.WebScreenId
FROM dbo.WebScreenPermissions target
INNER JOIN #TellerInvoiceUsers source
    ON target.UserId = source.UserId
   AND target.WebScreenId = @SalesScreenId
WHERE
    (target.CanView = 0 AND source.CanView = 1)
    OR (target.CanAdd = 0 AND source.CanAdd = 1)
    OR (target.CanEdit = 0 AND source.CanEdit = 1)
    OR (target.CanDelete = 0 AND source.CanDelete = 1)
    OR (target.CanPrint = 0 AND source.CanPrint = 1)
    OR target.SeedSource IS NULL
    OR target.SeedSource COLLATE DATABASE_DEFAULT IN (N'LegacyScreenJuncUser', N'POS_UserPermissions', N'POS_Teller_Default');

UPDATE target
   SET CanView = CONVERT(BIT, CASE WHEN target.CanView = 1 OR source.CanView = 1 THEN 1 ELSE 0 END),
       CanAdd = CONVERT(BIT, CASE WHEN target.CanAdd = 1 OR source.CanAdd = 1 THEN 1 ELSE 0 END),
       CanEdit = CONVERT(BIT, CASE WHEN target.CanEdit = 1 OR source.CanEdit = 1 THEN 1 ELSE 0 END),
       CanDelete = CONVERT(BIT, CASE WHEN target.CanDelete = 1 OR source.CanDelete = 1 THEN 1 ELSE 0 END),
       CanPrint = CONVERT(BIT, CASE WHEN target.CanPrint = 1 OR source.CanPrint = 1 THEN 1 ELSE 0 END),
       SeedSource = CASE
                        WHEN target.SeedSource COLLATE DATABASE_DEFAULT IN (N'Manual', N'Bulk', N'RoleTemplate', N'ManualCopy') THEN target.SeedSource
                        ELSE N'POS_TellerInvoiceSafety'
                    END,
       UpdatedAt = GETDATE()
FROM dbo.WebScreenPermissions target
INNER JOIN #TellerInvoiceUsers source
    ON target.UserId = source.UserId
   AND target.WebScreenId = @SalesScreenId
WHERE
    (target.CanView = 0 AND source.CanView = 1)
    OR (target.CanAdd = 0 AND source.CanAdd = 1)
    OR (target.CanEdit = 0 AND source.CanEdit = 1)
    OR (target.CanDelete = 0 AND source.CanDelete = 1)
    OR (target.CanPrint = 0 AND source.CanPrint = 1)
    OR target.SeedSource IS NULL
    OR target.SeedSource COLLATE DATABASE_DEFAULT IN (N'LegacyScreenJuncUser', N'POS_UserPermissions', N'POS_Teller_Default');

INSERT INTO dbo.WebScreenPermissions
    (UserId, WebScreenId, CanView, CanAdd, CanEdit, CanDelete, CanPrint, CanExport, CanApprove, SeedSource, CreatedAt, UpdatedAt)
SELECT
    source.UserId,
    @SalesScreenId,
    source.CanView,
    source.CanAdd,
    source.CanEdit,
    source.CanDelete,
    source.CanPrint,
    0,
    0,
    N'POS_TellerInvoiceSafety',
    GETDATE(),
    GETDATE()
FROM #TellerInvoiceUsers source
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.WebScreenPermissions target
    WHERE target.UserId = source.UserId
      AND target.WebScreenId = @SalesScreenId
);

INSERT INTO #AppliedWebPermissionSafety(ActionName, UserId, WebScreenId)
SELECT N'INSERT', source.UserId, @SalesScreenId
FROM #TellerInvoiceUsers source
WHERE EXISTS
(
    SELECT 1
    FROM dbo.WebScreenPermissions target
    WHERE target.UserId = source.UserId
      AND target.WebScreenId = @SalesScreenId
      AND target.SeedSource COLLATE DATABASE_DEFAULT = N'POS_TellerInvoiceSafety' COLLATE DATABASE_DEFAULT
)
AND NOT EXISTS
(
    SELECT 1
    FROM #AppliedWebPermissionSafety a
    WHERE a.UserId = source.UserId
      AND a.WebScreenId = @SalesScreenId
);

SELECT
    COUNT(1) AS LegacyTellerOrInvoiceUsers,
    SUM(CASE WHEN p.WebPermissionId IS NOT NULL AND p.CanView = 1 THEN 1 ELSE 0 END) AS UsersWithWebSalesView,
    SUM(CASE WHEN p.WebPermissionId IS NOT NULL AND p.CanAdd = 1 THEN 1 ELSE 0 END) AS UsersWithWebSalesAdd,
    SUM(CASE WHEN p.WebPermissionId IS NOT NULL AND p.CanPrint = 1 THEN 1 ELSE 0 END) AS UsersWithWebSalesPrint
FROM #TellerInvoiceUsers u
LEFT JOIN dbo.WebScreenPermissions p
    ON p.UserId = u.UserId
   AND p.WebScreenId = @SalesScreenId;

SELECT
    ActionName,
    COUNT(1) AS RowsAffected
FROM #AppliedWebPermissionSafety
GROUP BY ActionName
ORDER BY ActionName;

SELECT TOP 100
    u.UserId,
    tu.UserName,
    u.SourceNotes,
    p.CanView,
    p.CanAdd,
    p.CanEdit,
    p.CanDelete,
    p.CanPrint
FROM #TellerInvoiceUsers u
LEFT JOIN dbo.TblUsers tu ON tu.UserID = u.UserId
LEFT JOIN dbo.WebScreenPermissions p
    ON p.UserId = u.UserId
   AND p.WebScreenId = @SalesScreenId
WHERE ISNULL(p.CanView, 0) = 0
   OR (u.CanAdd = 1 AND ISNULL(p.CanAdd, 0) = 0)
   OR (u.CanPrint = 1 AND ISNULL(p.CanPrint, 0) = 0)
ORDER BY u.UserId;

DROP TABLE #AppliedWebPermissionSafety;
DROP TABLE #TellerCategories;
DROP TABLE #TellerInvoiceUsers;
DROP TABLE #TellerPermissionRaw;
