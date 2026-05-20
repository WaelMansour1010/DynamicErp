/*
03_AccountMappingDiagnostics_SELECT_ONLY_20260520.sql
Purpose: Read-only account mapping diagnostics for Adnan strict-active renters.
Mode: SELECT ONLY.
*/

/* 01. Active renter account mapping summary */
;WITH Settled AS (
    SELECT DISTINCT CAST(ContNo AS int) AS ContNo
    FROM Adnan.dbo.TblFiterWaiver
    WHERE ContNo IS NOT NULL
), ActiveContracts AS (
    SELECT c.*
    FROM Adnan.dbo.TblContract c
    LEFT JOIN Settled s ON s.ContNo = CAST(c.ContNo AS int)
    WHERE ISNULL(c.EndContract,0)=0
      AND s.ContNo IS NULL
      AND (c.EndDate IS NULL OR c.EndDate >= '20260520')
), ActiveRenters AS (
    SELECT DISTINCT r.CusID, r.CusName, r.Account_Code
    FROM ActiveContracts c
    INNER JOIN Adnan.dbo.TblCustemers r ON r.CusID = c.CusID
)
SELECT 'ActiveRenters' AS Metric, COUNT(*) AS Value FROM ActiveRenters
UNION ALL SELECT 'WithOldAccountCode', COUNT(*) FROM ActiveRenters WHERE ISNULL(Account_Code,'') <> ''
UNION ALL SELECT 'ExistsInAdnanAccounts', COUNT(*) FROM ActiveRenters r INNER JOIN Adnan.dbo.ACCOUNTS a ON a.Account_Code = r.Account_Code
UNION ALL SELECT 'ExistsInTargetChartOfAccount', COUNT(*) FROM ActiveRenters r INNER JOIN Alromaizan.dbo.ChartOfAccount ca ON ca.Code = r.Account_Code COLLATE Arabic_CI_AS
UNION ALL SELECT 'MissingInTargetChartOfAccount', COUNT(*) FROM ActiveRenters r LEFT JOIN Alromaizan.dbo.ChartOfAccount ca ON ca.Code = r.Account_Code COLLATE Arabic_CI_AS WHERE ca.Id IS NULL;

/* 02. Required renter accounts for Option A seeding */
;WITH Settled AS (
    SELECT DISTINCT CAST(ContNo AS int) AS ContNo
    FROM Adnan.dbo.TblFiterWaiver
    WHERE ContNo IS NOT NULL
), ActiveContracts AS (
    SELECT c.*
    FROM Adnan.dbo.TblContract c
    LEFT JOIN Settled s ON s.ContNo = CAST(c.ContNo AS int)
    WHERE ISNULL(c.EndContract,0)=0
      AND s.ContNo IS NULL
      AND (c.EndDate IS NULL OR c.EndDate >= '20260520')
), ActiveRenters AS (
    SELECT DISTINCT r.CusID, r.CusName, r.Account_Code
    FROM ActiveContracts c
    INNER JOIN Adnan.dbo.TblCustemers r ON r.CusID = c.CusID
)
SELECT r.CusID,
       r.CusName,
       r.Account_Code AS OldAccountCode,
       a.Account_ID AS OldAccountId,
       a.Account_Name AS OldAccountName,
       a.Parent_Account_Code AS OldParentAccountCode,
       a.opening_balance AS OldAccountOpeningBalance,
       a.opening_balance_type AS OldOpeningBalanceType,
       ca.Id AS ExistingTargetAccountId,
       ca.Code AS ExistingTargetAccountCode,
       CASE
           WHEN ISNULL(r.Account_Code,'') = '' THEN 'MissingOldAccountCode'
           WHEN a.Account_ID IS NULL THEN 'MissingInAdnanAccounts'
           WHEN ca.Id IS NULL THEN 'NeedsSandboxSeedOrManualMap'
           ELSE 'Matched'
       END AS MappingStatus
FROM ActiveRenters r
LEFT JOIN Adnan.dbo.ACCOUNTS a ON a.Account_Code = r.Account_Code
LEFT JOIN Alromaizan.dbo.ChartOfAccount ca ON ca.Code = r.Account_Code COLLATE Arabic_CI_AS
ORDER BY MappingStatus, r.CusID;

/* 03. Suggested manual review: duplicated account codes among active renters */
;WITH Settled AS (
    SELECT DISTINCT CAST(ContNo AS int) AS ContNo
    FROM Adnan.dbo.TblFiterWaiver
    WHERE ContNo IS NOT NULL
), ActiveContracts AS (
    SELECT c.*
    FROM Adnan.dbo.TblContract c
    LEFT JOIN Settled s ON s.ContNo = CAST(c.ContNo AS int)
    WHERE ISNULL(c.EndContract,0)=0
      AND s.ContNo IS NULL
      AND (c.EndDate IS NULL OR c.EndDate >= '20260520')
), ActiveRenters AS (
    SELECT DISTINCT r.CusID, r.CusName, r.Account_Code
    FROM ActiveContracts c
    INNER JOIN Adnan.dbo.TblCustemers r ON r.CusID = c.CusID
)
SELECT Account_Code, COUNT(*) AS RenterCount
FROM ActiveRenters
GROUP BY Account_Code
HAVING COUNT(*) > 1
ORDER BY RenterCount DESC, Account_Code;

/* 04. Target chart candidates for approved parent account decision */
SELECT Id, Code, ArName, EnName, ParentId, TypeId, ClassificationId, CategoryId, IsActive, IsDeleted
FROM Alromaizan.dbo.ChartOfAccount
WHERE ISNULL(IsDeleted,0)=0
ORDER BY Code;
