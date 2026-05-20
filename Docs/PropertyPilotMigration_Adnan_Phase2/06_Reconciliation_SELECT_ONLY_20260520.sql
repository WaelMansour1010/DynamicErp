/*
06_Reconciliation_SELECT_ONLY_20260520.sql
Purpose: Compare Adnan source against the current sandbox DB after pilot migration.
Mode: SELECT ONLY.
Run this from the sandbox database after migration draft execution.
*/

DECLARE @MigrationBatchId UNIQUEIDENTIFIER;
DECLARE @CutoverDate DATETIME;

/* Set these before running in sandbox */
SET @MigrationBatchId = NULL;
SET @CutoverDate = '20260520';

/* 00. Safety visibility */
SELECT DB_NAME() AS CurrentDatabase,
       @MigrationBatchId AS MigrationBatchId,
       @CutoverDate AS CutoverDate;

/* 01. Source candidate counts: strict active, structurally valid only */
;WITH Settled AS (
    SELECT DISTINCT CAST(ContNo AS int) AS ContNo FROM Adnan.dbo.TblFiterWaiver WHERE ContNo IS NOT NULL
), SourceContracts AS (
    SELECT c.* FROM Adnan.dbo.TblContract c
    LEFT JOIN Settled s ON s.ContNo = CAST(c.ContNo AS int)
    WHERE ISNULL(c.EndContract,0)=0
      AND s.ContNo IS NULL
      AND (c.EndDate IS NULL OR c.EndDate >= @CutoverDate)
      AND c.Iqar IS NOT NULL AND c.UnitNo IS NOT NULL AND c.CusID IS NOT NULL
)
SELECT 'SourceProperties' AS Metric, COUNT(DISTINCT Iqar) AS SourceValue FROM SourceContracts
UNION ALL SELECT 'SourceUnits', COUNT(DISTINCT UnitNo) FROM SourceContracts
UNION ALL SELECT 'SourceTenants', COUNT(DISTINCT CusID) FROM SourceContracts
UNION ALL SELECT 'SourceContracts', COUNT(*) FROM SourceContracts;

/* 02. Sandbox migrated counts through cross-reference */
SELECT EntityType AS Metric, COUNT(*) AS SandboxValue
FROM dbo.PropertyPilotCrossReference
WHERE (@MigrationBatchId IS NULL OR MigrationBatchId = @MigrationBatchId)
  AND EntityType IN (N'Property', N'Unit', N'Tenant', N'Contract', N'ContractBatch', N'Account')
GROUP BY EntityType
ORDER BY EntityType;

/* 03. Count reconciliation */
;WITH Settled AS (
    SELECT DISTINCT CAST(ContNo AS int) AS ContNo FROM Adnan.dbo.TblFiterWaiver WHERE ContNo IS NOT NULL
), SourceContracts AS (
    SELECT c.* FROM Adnan.dbo.TblContract c
    LEFT JOIN Settled s ON s.ContNo = CAST(c.ContNo AS int)
    WHERE ISNULL(c.EndContract,0)=0
      AND s.ContNo IS NULL
      AND (c.EndDate IS NULL OR c.EndDate >= @CutoverDate)
      AND c.Iqar IS NOT NULL AND c.UnitNo IS NOT NULL AND c.CusID IS NOT NULL
), SourceCounts AS (
    SELECT N'Property' EntityType, COUNT(DISTINCT Iqar) Cnt FROM SourceContracts
    UNION ALL SELECT N'Unit', COUNT(DISTINCT UnitNo) FROM SourceContracts
    UNION ALL SELECT N'Tenant', COUNT(DISTINCT CusID) FROM SourceContracts
    UNION ALL SELECT N'Contract', COUNT(*) FROM SourceContracts
), TargetCounts AS (
    SELECT EntityType, COUNT(*) Cnt
    FROM dbo.PropertyPilotCrossReference
    WHERE (@MigrationBatchId IS NULL OR MigrationBatchId=@MigrationBatchId)
      AND EntityType IN (N'Property',N'Unit',N'Tenant',N'Contract')
    GROUP BY EntityType
)
SELECT s.EntityType, s.Cnt AS SourceCount, ISNULL(t.Cnt,0) AS SandboxCount, ISNULL(t.Cnt,0)-s.Cnt AS Difference
FROM SourceCounts s
LEFT JOIN TargetCounts t ON t.EntityType=s.EntityType;

/* 04. Financial reconciliation by contract */
;WITH Paid AS (
    SELECT d.istallid,
           SUM(ISNULL(d.RentValuePayed,0)+ISNULL(d.CommissionsPayed,0)+ISNULL(d.InsurancePayed,0)+ISNULL(d.WaterPayed,0)+ISNULL(d.ElectricPayed,0)+ISNULL(d.TelandNetPayed,0)+ISNULL(d.OldValuePayed,0)+ISNULL(d.VATPayed,0)) AS TruePaid
    FROM Adnan.dbo.ContracttBillInstallmentsDone d
    LEFT JOIN Adnan.dbo.Notes n ON n.NoteID=d.NoteID
    WHERE n.NoteType=4 OR n.NoteType IS NULL
    GROUP BY d.istallid
), SourceFinancial AS (
    SELECT CAST(i.ContNo AS int) AS OldContractNo,
           SUM(ISNULL(i.installValue,0)) AS SourceInstallmentTotal,
           SUM(CASE WHEN i.Installdate <= @CutoverDate THEN ISNULL(i.installValue,0)-ISNULL(p.TruePaid,0) ELSE 0 END) AS SourceOpeningBalance,
           SUM(CASE WHEN i.Installdate > @CutoverDate THEN ISNULL(i.installValue,0)-ISNULL(p.TruePaid,0) ELSE 0 END) AS SourceFutureRemain
    FROM Adnan.dbo.TblContractInstallments i
    INNER JOIN dbo.PropertyPilotCrossReference cx ON cx.OldDatabaseName=N'Adnan' AND cx.OldTableName=N'TblContract' AND cx.OldId=CAST(CAST(i.ContNo AS int) AS NVARCHAR(100)) AND cx.EntityType=N'Contract'
    LEFT JOIN Paid p ON p.istallid=i.id
    WHERE (@MigrationBatchId IS NULL OR cx.MigrationBatchId=@MigrationBatchId)
    GROUP BY CAST(i.ContNo AS int)
), TargetFinancial AS (
    SELECT CAST(cx.OldId AS int) AS OldContractNo,
           SUM(ISNULL(pcb.BatchTotal,0)) AS SandboxBatchTotal
    FROM dbo.PropertyPilotCrossReference cx
    INNER JOIN dbo.PropertyContractBatch pcb ON pcb.MainDocId = cx.NewId
    WHERE (@MigrationBatchId IS NULL OR cx.MigrationBatchId=@MigrationBatchId)
      AND cx.EntityType=N'Contract'
    GROUP BY CAST(cx.OldId AS int)
), TargetOB AS (
    SELECT OldContractNo, SUM(OpeningBalanceAmount) AS SandboxOpeningBalance
    FROM dbo.PropertyPilotOpeningBalanceStaging
    WHERE (@MigrationBatchId IS NULL OR MigrationBatchId=@MigrationBatchId)
    GROUP BY OldContractNo
)
SELECT s.OldContractNo,
       s.SourceInstallmentTotal,
       ISNULL(t.SandboxBatchTotal,0) AS SandboxBatchTotal,
       ISNULL(t.SandboxBatchTotal,0)-s.SourceInstallmentTotal AS BatchTotalDifference,
       s.SourceOpeningBalance,
       ISNULL(ob.SandboxOpeningBalance,0) AS SandboxOpeningBalance,
       ISNULL(ob.SandboxOpeningBalance,0)-s.SourceOpeningBalance AS OpeningBalanceDifference,
       s.SourceFutureRemain
FROM SourceFinancial s
LEFT JOIN TargetFinancial t ON t.OldContractNo=s.OldContractNo
LEFT JOIN TargetOB ob ON ob.OldContractNo=s.OldContractNo
WHERE ABS(ISNULL(t.SandboxBatchTotal,0)-s.SourceInstallmentTotal) > 0.01
   OR ABS(ISNULL(ob.SandboxOpeningBalance,0)-s.SourceOpeningBalance) > 0.01
ORDER BY s.OldContractNo;

/* 05. Contracts not migrated */
;WITH Settled AS (
    SELECT DISTINCT CAST(ContNo AS int) AS ContNo FROM Adnan.dbo.TblFiterWaiver WHERE ContNo IS NOT NULL
), SourceContracts AS (
    SELECT c.* FROM Adnan.dbo.TblContract c
    LEFT JOIN Settled s ON s.ContNo = CAST(c.ContNo AS int)
    WHERE ISNULL(c.EndContract,0)=0
      AND s.ContNo IS NULL
      AND (c.EndDate IS NULL OR c.EndDate >= @CutoverDate)
)
SELECT c.ContNo, c.NoteSerial1, c.Iqar, c.UnitNo, c.CusID,
       CASE WHEN c.Iqar IS NULL OR c.UnitNo IS NULL OR c.CusID IS NULL THEN 'Excluded_MissingCriticalLinks' ELSE 'NotMigrated_Investigate' END AS Reason
FROM SourceContracts c
LEFT JOIN dbo.PropertyPilotCrossReference x
       ON x.OldDatabaseName=N'Adnan'
      AND x.OldTableName=N'TblContract'
      AND x.OldId=CAST(CAST(c.ContNo AS int) AS NVARCHAR(100))
      AND x.EntityType=N'Contract'
      AND (@MigrationBatchId IS NULL OR x.MigrationBatchId=@MigrationBatchId)
WHERE x.Id IS NULL
ORDER BY c.ContNo;

/* 06. Unmapped accounts */
SELECT m.OldAccountCode, m.OldAccountName, m.MappingMode, m.IsApproved, m.NewChartOfAccountId
FROM dbo.PropertyPilotAccountMapping m
WHERE (@MigrationBatchId IS NULL OR m.MigrationBatchId=@MigrationBatchId)
  AND (m.IsApproved=0 OR m.NewChartOfAccountId IS NULL)
ORDER BY m.OldAccountCode;

/* 07. Contract date/unit/tenant differences */
SELECT CAST(cx.OldId AS int) AS OldContractNo,
       c.StrDate AS SourceStartDate,
       pc.ContractStartDate AS SandboxStartDate,
       c.EndDate AS SourceEndDate,
       pc.ContractEndDate AS SandboxEndDate,
       c.UnitNo AS SourceUnitId,
       ux.NewId AS SandboxUnitIdExpected,
       pc.PropertyUnitId AS SandboxUnitIdActual,
       c.CusID AS SourceTenantId,
       tx.NewId AS SandboxTenantIdExpected,
       pc.PropertyRenterId AS SandboxTenantIdActual
FROM dbo.PropertyPilotCrossReference cx
INNER JOIN Adnan.dbo.TblContract c ON CAST(c.ContNo AS int)=CAST(cx.OldId AS int)
INNER JOIN dbo.PropertyContract pc ON pc.Id=cx.NewId
LEFT JOIN dbo.PropertyPilotCrossReference ux ON ux.MigrationBatchId=cx.MigrationBatchId AND ux.OldDatabaseName=N'Adnan' AND ux.OldTableName=N'TblAqarDetai' AND ux.OldId=CAST(c.UnitNo AS NVARCHAR(100)) AND ux.EntityType=N'Unit'
LEFT JOIN dbo.PropertyPilotCrossReference tx ON tx.MigrationBatchId=cx.MigrationBatchId AND tx.OldDatabaseName=N'Adnan' AND tx.OldTableName=N'TblCustemers' AND tx.OldId=CAST(c.CusID AS NVARCHAR(100)) AND tx.EntityType=N'Tenant'
WHERE (@MigrationBatchId IS NULL OR cx.MigrationBatchId=@MigrationBatchId)
  AND cx.EntityType=N'Contract'
  AND (
       ISNULL(CONVERT(VARCHAR(10), c.StrDate, 120),'') <> ISNULL(CONVERT(VARCHAR(10), pc.ContractStartDate, 120),'')
    OR ISNULL(CONVERT(VARCHAR(10), c.EndDate, 120),'') <> ISNULL(CONVERT(VARCHAR(10), pc.ContractEndDate, 120),'')
    OR ISNULL(ux.NewId,-1) <> ISNULL(pc.PropertyUnitId,-1)
    OR ISNULL(tx.NewId,-1) <> ISNULL(pc.PropertyRenterId,-1)
  )
ORDER BY OldContractNo;
