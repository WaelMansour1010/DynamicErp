/*
    Kishny POS - Web permissions teller/invoice safety seed
    SQL Server 2012 compatible.

    Purpose:
    - Protect teller and invoice users during rollout of the new web permission catalog.
    - Read legacy POS permissions and ensure matching WebScreenPermissions for POS.Sales.Index.
    - Never revoke or downgrade existing web permissions.
*/

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
WHERE ScreenKey = N'POS.Sales.Index'
  AND IsActive = 1;

IF @SalesScreenId IS NULL
BEGIN
    RAISERROR(N'POS.Sales.Index is missing from dbo.WebScreens.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'tempdb..#TellerInvoiceUsers') IS NOT NULL
    DROP TABLE #TellerInvoiceUsers;

CREATE TABLE #TellerInvoiceUsers
(
    UserId INT NOT NULL PRIMARY KEY,
    SourceNotes NVARCHAR(500) NULL,
    CanView BIT NOT NULL DEFAULT(0),
    CanAdd BIT NOT NULL DEFAULT(0),
    CanEdit BIT NOT NULL DEFAULT(0),
    CanDelete BIT NOT NULL DEFAULT(0),
    CanPrint BIT NOT NULL DEFAULT(0)
);

IF OBJECT_ID(N'tempdb..#TellerCategories') IS NOT NULL
    DROP TABLE #TellerCategories;

CREATE TABLE #TellerCategories(CategoryName NVARCHAR(100) NOT NULL PRIMARY KEY);

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
            MAX(CASE WHEN p.PermissionKey IN (N'CanTeller') THEN 1 ELSE 0 END) AS HasTeller,
            MAX(CASE WHEN p.PermissionKey IN (N'CanOpenSales') THEN 1 ELSE 0 END) AS HasOpen,
            MAX(CASE WHEN p.PermissionKey IN (N'CanSaveInvoice') THEN 1 ELSE 0 END) AS HasSave,
            MAX(CASE WHEN p.PermissionKey IN (N'CanEditSalesInvoice', N'CanEditSalesInvoicePos', N'CanEditInvoice') THEN 1 ELSE 0 END) AS HasEdit,
            MAX(CASE WHEN p.PermissionKey IN (N'CanCancelInvoice', N'CanCancelOrReturn') THEN 1 ELSE 0 END) AS HasCancel
        FROM dbo.POS_UserPermissions p
        WHERE ISNULL(p.IsAllowed, 0) = 1
          AND p.PermissionKey IN
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
    MERGE #TellerInvoiceUsers AS target
    USING
    (
        SELECT
            UserId,
            N'POS_UserPermissions' AS SourceNotes,
            CONVERT(BIT, 1) AS CanView,
            CONVERT(BIT, CASE WHEN HasTeller = 1 OR HasSave = 1 THEN 1 ELSE 0 END) AS CanAdd,
            CONVERT(BIT, CASE WHEN HasTeller = 1 OR HasEdit = 1 THEN 1 ELSE 0 END) AS CanEdit,
            CONVERT(BIT, CASE WHEN HasCancel = 1 THEN 1 ELSE 0 END) AS CanDelete,
            CONVERT(BIT, CASE WHEN HasTeller = 1 OR HasSave = 1 OR HasEdit = 1 THEN 1 ELSE 0 END) AS CanPrint
        FROM LegacyInvoicePermissions
    ) AS source
    ON target.UserId = source.UserId
    WHEN MATCHED THEN UPDATE SET
        SourceNotes = ISNULL(target.SourceNotes + N'; ', N'') + source.SourceNotes,
        CanView = CONVERT(BIT, CASE WHEN target.CanView = 1 OR source.CanView = 1 THEN 1 ELSE 0 END),
        CanAdd = CONVERT(BIT, CASE WHEN target.CanAdd = 1 OR source.CanAdd = 1 THEN 1 ELSE 0 END),
        CanEdit = CONVERT(BIT, CASE WHEN target.CanEdit = 1 OR source.CanEdit = 1 THEN 1 ELSE 0 END),
        CanDelete = CONVERT(BIT, CASE WHEN target.CanDelete = 1 OR source.CanDelete = 1 THEN 1 ELSE 0 END),
        CanPrint = CONVERT(BIT, CASE WHEN target.CanPrint = 1 OR source.CanPrint = 1 THEN 1 ELSE 0 END)
    WHEN NOT MATCHED THEN INSERT (UserId, SourceNotes, CanView, CanAdd, CanEdit, CanDelete, CanPrint)
        VALUES (source.UserId, source.SourceNotes, source.CanView, source.CanAdd, source.CanEdit, source.CanDelete, source.CanPrint);
END;

IF COL_LENGTH(N'dbo.TblUsers', N'UserCategory') IS NOT NULL
BEGIN
    MERGE #TellerInvoiceUsers AS target
    USING
    (
        SELECT
            u.UserID AS UserId,
            N'TblUsers.UserCategory' AS SourceNotes,
            CONVERT(BIT, 1) AS CanView,
            CONVERT(BIT, 1) AS CanAdd,
            CONVERT(BIT, 1) AS CanEdit,
            CONVERT(BIT, 0) AS CanDelete,
            CONVERT(BIT, 1) AS CanPrint
        FROM dbo.TblUsers u
        INNER JOIN #TellerCategories c
            ON LTRIM(RTRIM(ISNULL(u.UserCategory, N''))) = c.CategoryName
        WHERE u.UserID IS NOT NULL
    ) AS source
    ON target.UserId = source.UserId
    WHEN MATCHED THEN UPDATE SET
        SourceNotes = ISNULL(target.SourceNotes + N'; ', N'') + source.SourceNotes,
        CanView = CONVERT(BIT, CASE WHEN target.CanView = 1 OR source.CanView = 1 THEN 1 ELSE 0 END),
        CanAdd = CONVERT(BIT, CASE WHEN target.CanAdd = 1 OR source.CanAdd = 1 THEN 1 ELSE 0 END),
        CanEdit = CONVERT(BIT, CASE WHEN target.CanEdit = 1 OR source.CanEdit = 1 THEN 1 ELSE 0 END),
        CanDelete = CONVERT(BIT, CASE WHEN target.CanDelete = 1 OR source.CanDelete = 1 THEN 1 ELSE 0 END),
        CanPrint = CONVERT(BIT, CASE WHEN target.CanPrint = 1 OR source.CanPrint = 1 THEN 1 ELSE 0 END)
    WHEN NOT MATCHED THEN INSERT (UserId, SourceNotes, CanView, CanAdd, CanEdit, CanDelete, CanPrint)
        VALUES (source.UserId, source.SourceNotes, source.CanView, source.CanAdd, source.CanEdit, source.CanDelete, source.CanPrint);
END;

IF COL_LENGTH(N'dbo.TblUsers', N'CanEditSalesInvoice') IS NOT NULL
BEGIN
    EXEC(N'
    MERGE #TellerInvoiceUsers AS target
    USING
    (
        SELECT UserID AS UserId,
               N''TblUsers.CanEditSalesInvoice'' AS SourceNotes,
               CONVERT(BIT, 1) AS CanView,
               CONVERT(BIT, 0) AS CanAdd,
               CONVERT(BIT, 1) AS CanEdit,
               CONVERT(BIT, 0) AS CanDelete,
               CONVERT(BIT, 1) AS CanPrint
        FROM dbo.TblUsers
        WHERE ISNULL(CanEditSalesInvoice, 0) = 1
    ) AS source
    ON target.UserId = source.UserId
    WHEN MATCHED THEN UPDATE SET
        SourceNotes = ISNULL(target.SourceNotes + N''; '', N'''') + source.SourceNotes,
        CanView = CONVERT(BIT, CASE WHEN target.CanView = 1 OR source.CanView = 1 THEN 1 ELSE 0 END),
        CanAdd = CONVERT(BIT, CASE WHEN target.CanAdd = 1 OR source.CanAdd = 1 THEN 1 ELSE 0 END),
        CanEdit = CONVERT(BIT, CASE WHEN target.CanEdit = 1 OR source.CanEdit = 1 THEN 1 ELSE 0 END),
        CanDelete = CONVERT(BIT, CASE WHEN target.CanDelete = 1 OR source.CanDelete = 1 THEN 1 ELSE 0 END),
        CanPrint = CONVERT(BIT, CASE WHEN target.CanPrint = 1 OR source.CanPrint = 1 THEN 1 ELSE 0 END)
    WHEN NOT MATCHED THEN INSERT (UserId, SourceNotes, CanView, CanAdd, CanEdit, CanDelete, CanPrint)
        VALUES (source.UserId, source.SourceNotes, source.CanView, source.CanAdd, source.CanEdit, source.CanDelete, source.CanPrint);');
END;

IF COL_LENGTH(N'dbo.TblUsers', N'CanEditSalesInvoicePos') IS NOT NULL
BEGIN
    EXEC(N'
    MERGE #TellerInvoiceUsers AS target
    USING
    (
        SELECT UserID AS UserId,
               N''TblUsers.CanEditSalesInvoicePos'' AS SourceNotes,
               CONVERT(BIT, 1) AS CanView,
               CONVERT(BIT, 0) AS CanAdd,
               CONVERT(BIT, 1) AS CanEdit,
               CONVERT(BIT, 0) AS CanDelete,
               CONVERT(BIT, 1) AS CanPrint
        FROM dbo.TblUsers
        WHERE ISNULL(CanEditSalesInvoicePos, 0) = 1
    ) AS source
    ON target.UserId = source.UserId
    WHEN MATCHED THEN UPDATE SET
        SourceNotes = ISNULL(target.SourceNotes + N''; '', N'''') + source.SourceNotes,
        CanView = CONVERT(BIT, CASE WHEN target.CanView = 1 OR source.CanView = 1 THEN 1 ELSE 0 END),
        CanAdd = CONVERT(BIT, CASE WHEN target.CanAdd = 1 OR source.CanAdd = 1 THEN 1 ELSE 0 END),
        CanEdit = CONVERT(BIT, CASE WHEN target.CanEdit = 1 OR source.CanEdit = 1 THEN 1 ELSE 0 END),
        CanDelete = CONVERT(BIT, CASE WHEN target.CanDelete = 1 OR source.CanDelete = 1 THEN 1 ELSE 0 END),
        CanPrint = CONVERT(BIT, CASE WHEN target.CanPrint = 1 OR source.CanPrint = 1 THEN 1 ELSE 0 END)
    WHEN NOT MATCHED THEN INSERT (UserId, SourceNotes, CanView, CanAdd, CanEdit, CanDelete, CanPrint)
        VALUES (source.UserId, source.SourceNotes, source.CanView, source.CanAdd, source.CanEdit, source.CanDelete, source.CanPrint);');
END;

IF OBJECT_ID(N'dbo.ScreenJuncUser', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.WebScreenLegacyMap', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ScreenJuncUser', N'User_ID') IS NOT NULL
BEGIN
    MERGE #TellerInvoiceUsers AS target
    USING
    (
        SELECT
            sj.User_ID AS UserId,
            N'ScreenJuncUser' AS SourceNotes,
            CONVERT(BIT, 1) AS CanView,
            CONVERT(BIT, MAX(CASE WHEN ISNULL(sj.CanAdd, 0) = 1 OR ISNULL(sj.FullAccess, 0) = 1 THEN 1 ELSE 0 END)) AS CanAdd,
            CONVERT(BIT, MAX(CASE WHEN ISNULL(sj.CanEdit, 0) = 1 OR ISNULL(sj.FullAccess, 0) = 1 THEN 1 ELSE 0 END)) AS CanEdit,
            CONVERT(BIT, MAX(CASE WHEN ISNULL(sj.CanDelete, 0) = 1 OR ISNULL(sj.FullAccess, 0) = 1 THEN 1 ELSE 0 END)) AS CanDelete,
            CONVERT(BIT, MAX(CASE WHEN ISNULL(sj.CanPrint, 0) = 1 OR ISNULL(sj.FullAccess, 0) = 1 THEN 1 ELSE 0 END)) AS CanPrint
        FROM dbo.ScreenJuncUser sj
        INNER JOIN dbo.WebScreenLegacyMap lm
            ON lm.LegacyScreenName = sj.ScreenName
           AND lm.ScreenKey = N'POS.Sales.Index'
        WHERE sj.User_ID IS NOT NULL
        GROUP BY sj.User_ID
    ) AS source
    ON target.UserId = source.UserId
    WHEN MATCHED THEN UPDATE SET
        SourceNotes = ISNULL(target.SourceNotes + N'; ', N'') + source.SourceNotes,
        CanView = CONVERT(BIT, CASE WHEN target.CanView = 1 OR source.CanView = 1 THEN 1 ELSE 0 END),
        CanAdd = CONVERT(BIT, CASE WHEN target.CanAdd = 1 OR source.CanAdd = 1 THEN 1 ELSE 0 END),
        CanEdit = CONVERT(BIT, CASE WHEN target.CanEdit = 1 OR source.CanEdit = 1 THEN 1 ELSE 0 END),
        CanDelete = CONVERT(BIT, CASE WHEN target.CanDelete = 1 OR source.CanDelete = 1 THEN 1 ELSE 0 END),
        CanPrint = CONVERT(BIT, CASE WHEN target.CanPrint = 1 OR source.CanPrint = 1 THEN 1 ELSE 0 END)
    WHEN NOT MATCHED THEN INSERT (UserId, SourceNotes, CanView, CanAdd, CanEdit, CanDelete, CanPrint)
        VALUES (source.UserId, source.SourceNotes, source.CanView, source.CanAdd, source.CanEdit, source.CanDelete, source.CanPrint);
END;

IF OBJECT_ID(N'tempdb..#AppliedWebPermissionSafety') IS NOT NULL
    DROP TABLE #AppliedWebPermissionSafety;

CREATE TABLE #AppliedWebPermissionSafety
(
    MergeAction NVARCHAR(10) NOT NULL,
    UserId INT NOT NULL,
    WebScreenId INT NOT NULL
);

MERGE dbo.WebScreenPermissions AS target
USING
(
    SELECT
        UserId,
        @SalesScreenId AS WebScreenId,
        MAX(CONVERT(INT, CanView)) AS CanView,
        MAX(CONVERT(INT, CanAdd)) AS CanAdd,
        MAX(CONVERT(INT, CanEdit)) AS CanEdit,
        MAX(CONVERT(INT, CanDelete)) AS CanDelete,
        MAX(CONVERT(INT, CanPrint)) AS CanPrint
    FROM #TellerInvoiceUsers
    GROUP BY UserId
) AS source
ON target.UserId = source.UserId
AND target.WebScreenId = source.WebScreenId
WHEN MATCHED AND
(
    (target.CanView = 0 AND source.CanView = 1)
    OR (target.CanAdd = 0 AND source.CanAdd = 1)
    OR (target.CanEdit = 0 AND source.CanEdit = 1)
    OR (target.CanDelete = 0 AND source.CanDelete = 1)
    OR (target.CanPrint = 0 AND source.CanPrint = 1)
    OR target.SeedSource IS NULL
    OR target.SeedSource IN (N'LegacyScreenJuncUser', N'POS_UserPermissions', N'POS_Teller_Default')
) THEN UPDATE SET
    CanView = CONVERT(BIT, CASE WHEN target.CanView = 1 OR source.CanView = 1 THEN 1 ELSE 0 END),
    CanAdd = CONVERT(BIT, CASE WHEN target.CanAdd = 1 OR source.CanAdd = 1 THEN 1 ELSE 0 END),
    CanEdit = CONVERT(BIT, CASE WHEN target.CanEdit = 1 OR source.CanEdit = 1 THEN 1 ELSE 0 END),
    CanDelete = CONVERT(BIT, CASE WHEN target.CanDelete = 1 OR source.CanDelete = 1 THEN 1 ELSE 0 END),
    CanPrint = CONVERT(BIT, CASE WHEN target.CanPrint = 1 OR source.CanPrint = 1 THEN 1 ELSE 0 END),
    SeedSource = CASE
                    WHEN target.SeedSource IN (N'Manual', N'Bulk', N'RoleTemplate', N'ManualCopy') THEN target.SeedSource
                    ELSE N'POS_TellerInvoiceSafety'
                 END,
    UpdatedAt = GETDATE()
WHEN NOT MATCHED THEN INSERT
    (UserId, WebScreenId, CanView, CanAdd, CanEdit, CanDelete, CanPrint, CanExport, CanApprove, SeedSource, CreatedAt, UpdatedAt)
    VALUES
    (source.UserId, source.WebScreenId, CONVERT(BIT, source.CanView), CONVERT(BIT, source.CanAdd), CONVERT(BIT, source.CanEdit), CONVERT(BIT, source.CanDelete), CONVERT(BIT, source.CanPrint), 0, 0, N'POS_TellerInvoiceSafety', GETDATE(), GETDATE())
OUTPUT $action, inserted.UserId, inserted.WebScreenId INTO #AppliedWebPermissionSafety(MergeAction, UserId, WebScreenId);

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
    MergeAction,
    COUNT(1) AS RowsAffected
FROM #AppliedWebPermissionSafety
GROUP BY MergeAction
ORDER BY MergeAction;

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
