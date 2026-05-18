SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRAN;

IF OBJECT_ID('tempdb..#RollbackAccounts') IS NOT NULL
    DROP TABLE #RollbackAccounts;

;WITH Seed AS
(
    SELECT DISTINCT RecordKey AS Account_Code
    FROM dbo.MasterDataImportBatchDetail
    WHERE BatchId = 1
      AND TableName = N'ACCOUNTS'
      AND ActionType = N'Created'
),
Tree AS
(
    SELECT a.Account_Code, a.Parent_Account_Code, 0 AS Depth
    FROM dbo.ACCOUNTS a
    INNER JOIN Seed s ON s.Account_Code = a.Account_Code

    UNION ALL

    SELECT a.Account_Code, a.Parent_Account_Code, t.Depth + 1
    FROM dbo.ACCOUNTS a
    INNER JOIN Tree t ON a.Parent_Account_Code = t.Account_Code
)
SELECT Account_Code, MAX(Depth) AS Depth
INTO #RollbackAccounts
FROM Tree
GROUP BY Account_Code;

DELETE c
FROM dbo.TblCustemers c
INNER JOIN #RollbackAccounts r ON r.Account_Code = c.Account_Code;

DELETE s
FROM dbo.TblStore s
INNER JOIN #RollbackAccounts r ON r.Account_Code = s.Account_Code;

DELETE b
FROM dbo.BanksData b
INNER JOIN #RollbackAccounts r ON r.Account_Code = b.Account_Code;

DELETE x
FROM dbo.TblBoxesData x
INNER JOIN #RollbackAccounts r ON r.Account_Code = x.Account_Code;

DELETE ex
FROM dbo.ExpensesType ex
INNER JOIN #RollbackAccounts r ON r.Account_Code = ex.Account_Code;

DELETE rv
FROM dbo.TblRevenuesTypes rv
INNER JOIN #RollbackAccounts r ON r.Account_Code = rv.Account_Code;

DELETE f
FROM dbo.FixedAssetsGroup f
INNER JOIN #RollbackAccounts r ON r.Account_Code = f.Account_Code;

DELETE e
FROM dbo.TblEmployee e
INNER JOIN #RollbackAccounts r ON r.Account_Code = e.Account_code;

DELETE p
FROM dbo.projects p
INNER JOIN #RollbackAccounts r ON r.Account_Code = p.Project_account;

DELETE a
FROM dbo.ACCOUNTS a
INNER JOIN #RollbackAccounts r ON r.Account_Code = a.Account_Code;

DELETE FROM dbo.MasterDataImportBatchDetail
WHERE BatchId = 1;

DELETE FROM dbo.MasterDataImportBatch
WHERE BatchId = 1;

COMMIT TRAN;
