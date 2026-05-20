/*
Property Pilot Migration Diagnostics - Adnan to DynamicErp
Date: 2026-05-20
Mode: SELECT ONLY / no data changes
Target strategy: Hybrid Migration (Active Contracts + Opening Balances + Historical Archive)

Run context recommendation:
- Execute against SQL Server instance containing Adnan and Alromaizan.
- This script intentionally does not create, insert, update, delete, alter, merge, truncate, or execute stored procedures.
*/

/* 01. Active contract classification */
;WITH Settled AS (
    SELECT DISTINCT CAST(ContNo AS int) AS ContNo
    FROM Adnan.dbo.TblFiterWaiver
    WHERE ContNo IS NOT NULL
), ActiveOperational AS (
    SELECT c.*
    FROM Adnan.dbo.TblContract c
    LEFT JOIN Settled s ON s.ContNo = CAST(c.ContNo AS int)
    WHERE ISNULL(c.EndContract, 0) = 0
      AND s.ContNo IS NULL
), ActiveStrict AS (
    SELECT *
    FROM ActiveOperational
    WHERE EndDate IS NULL OR EndDate >= '20260520'
)
SELECT 'TotalContracts' AS Metric, COUNT(*) AS Value FROM Adnan.dbo.TblContract
UNION ALL SELECT 'EndContractNullOrZero', COUNT(*) FROM Adnan.dbo.TblContract WHERE ISNULL(EndContract,0)=0
UNION ALL SELECT 'HasSettlement', COUNT(DISTINCT CAST(ContNo AS int)) FROM Adnan.dbo.TblFiterWaiver WHERE ContNo IS NOT NULL
UNION ALL SELECT 'OperationalActive_NoEndFlag_NoSettlement', COUNT(*) FROM ActiveOperational
UNION ALL SELECT 'StrictActive_AsOf_2026_05_20', COUNT(*) FROM ActiveStrict
UNION ALL SELECT 'OperationalActive_ButEndDatePast', COUNT(*) FROM ActiveOperational WHERE EndDate < '20260520'
UNION ALL SELECT 'StrictActive_Properties', COUNT(DISTINCT Iqar) FROM ActiveStrict
UNION ALL SELECT 'StrictActive_Units', COUNT(DISTINCT UnitNo) FROM ActiveStrict
UNION ALL SELECT 'StrictActive_Renters', COUNT(DISTINCT CusID) FROM ActiveStrict;

/* 02. True paid/remain calculation from ContracttBillInstallmentsDone + Notes.NoteType=4 */
;WITH Settled AS (
    SELECT DISTINCT CAST(ContNo AS int) AS ContNo
    FROM Adnan.dbo.TblFiterWaiver
    WHERE ContNo IS NOT NULL
), ActiveContracts AS (
    SELECT c.*
    FROM Adnan.dbo.TblContract c
    LEFT JOIN Settled s ON s.ContNo = CAST(c.ContNo AS int)
    WHERE ISNULL(c.EndContract, 0) = 0
      AND s.ContNo IS NULL
      AND (c.EndDate IS NULL OR c.EndDate >= '20260520')
), Paid AS (
    SELECT d.istallid,
           SUM(ISNULL(d.RentValuePayed,0)+ISNULL(d.CommissionsPayed,0)+ISNULL(d.InsurancePayed,0)+ISNULL(d.WaterPayed,0)+ISNULL(d.ElectricPayed,0)+ISNULL(d.TelandNetPayed,0)+ISNULL(d.OldValuePayed,0)+ISNULL(d.VATPayed,0)) AS TruePaid
    FROM Adnan.dbo.ContracttBillInstallmentsDone d
    LEFT JOIN Adnan.dbo.Notes n ON n.NoteID = d.NoteID
    WHERE n.NoteType = 4 OR n.NoteType IS NULL
    GROUP BY d.istallid
), Inst AS (
    SELECT i.*,
           ISNULL(p.TruePaid,0) AS TruePaid,
           ISNULL(i.installValue,0)-ISNULL(p.TruePaid,0) AS TrueRemain
    FROM Adnan.dbo.TblContractInstallments i
    INNER JOIN ActiveContracts c ON CAST(c.ContNo AS int) = CAST(i.ContNo AS int)
    LEFT JOIN Paid p ON p.istallid = i.id
)
SELECT CASE WHEN Installdate <= '20260520' THEN 'DueOrPast_OpeningBalanceCandidate' ELSE 'Future_ContractBatchCandidate' END AS Bucket,
       COUNT(*) AS RowsCount,
       COUNT(DISTINCT ContNo) AS ContractsCount,
       SUM(ISNULL(installValue,0)) AS InstallmentTotal,
       SUM(TruePaid) AS TruePaid,
       SUM(TrueRemain) AS TrueRemain,
       SUM(CASE WHEN TrueRemain < -0.01 THEN 1 ELSE 0 END) AS NegativeRemainRows,
       SUM(CASE WHEN TruePaid > ISNULL(installValue,0)+0.01 THEN 1 ELSE 0 END) AS OverPaidRows
FROM Inst
GROUP BY CASE WHEN Installdate <= '20260520' THEN 'DueOrPast_OpeningBalanceCandidate' ELSE 'Future_ContractBatchCandidate' END;

/* 03. Contract-level opening balance candidate */
;WITH Settled AS (
    SELECT DISTINCT CAST(ContNo AS int) AS ContNo
    FROM Adnan.dbo.TblFiterWaiver
    WHERE ContNo IS NOT NULL
), ActiveContracts AS (
    SELECT c.*
    FROM Adnan.dbo.TblContract c
    LEFT JOIN Settled s ON s.ContNo = CAST(c.ContNo AS int)
    WHERE ISNULL(c.EndContract, 0) = 0
      AND s.ContNo IS NULL
      AND (c.EndDate IS NULL OR c.EndDate >= '20260520')
), Paid AS (
    SELECT d.istallid,
           SUM(ISNULL(d.RentValuePayed,0)+ISNULL(d.CommissionsPayed,0)+ISNULL(d.InsurancePayed,0)+ISNULL(d.WaterPayed,0)+ISNULL(d.ElectricPayed,0)+ISNULL(d.TelandNetPayed,0)+ISNULL(d.OldValuePayed,0)+ISNULL(d.VATPayed,0)) AS TruePaid
    FROM Adnan.dbo.ContracttBillInstallmentsDone d
    LEFT JOIN Adnan.dbo.Notes n ON n.NoteID = d.NoteID
    WHERE n.NoteType = 4 OR n.NoteType IS NULL
    GROUP BY d.istallid
), Inst AS (
    SELECT i.ContNo,
           COUNT(*) AS InstallmentRows,
           SUM(ISNULL(i.installValue,0)) AS InstallmentTotal,
           SUM(CASE WHEN i.Installdate <= '20260520' THEN ISNULL(i.installValue,0) ELSE 0 END) AS DueInstallmentTotal,
           SUM(CASE WHEN i.Installdate > '20260520' THEN ISNULL(i.installValue,0) ELSE 0 END) AS FutureInstallmentTotal,
           SUM(ISNULL(p.TruePaid,0)) AS TruePaid,
           SUM(CASE WHEN i.Installdate <= '20260520' THEN ISNULL(i.installValue,0)-ISNULL(p.TruePaid,0) ELSE 0 END) AS DueOpeningRemain,
           SUM(CASE WHEN i.Installdate > '20260520' THEN ISNULL(i.installValue,0)-ISNULL(p.TruePaid,0) ELSE 0 END) AS FutureRemain
    FROM Adnan.dbo.TblContractInstallments i
    INNER JOIN ActiveContracts c ON CAST(c.ContNo AS int) = CAST(i.ContNo AS int)
    LEFT JOIN Paid p ON p.istallid = i.id
    GROUP BY i.ContNo
)
SELECT c.ContNo,
       c.NoteSerial1,
       c.ContDate,
       c.StrDate,
       c.EndDate,
       c.Iqar,
       c.UnitNo,
       c.CusID,
       r.CusName,
       r.Account_Code,
       i.InstallmentRows,
       i.InstallmentTotal,
       i.DueInstallmentTotal,
       i.FutureInstallmentTotal,
       i.TruePaid,
       i.DueOpeningRemain,
       i.FutureRemain,
       ISNULL(i.DueOpeningRemain,0)+ISNULL(i.FutureRemain,0) AS TrueRemainTotal
FROM ActiveContracts c
LEFT JOIN Inst i ON CAST(i.ContNo AS int) = CAST(c.ContNo AS int)
LEFT JOIN Adnan.dbo.TblCustemers r ON r.CusID = c.CusID
ORDER BY c.ContNo;

/* 04. Blocking missing links */
;WITH Settled AS (
    SELECT DISTINCT CAST(ContNo AS int) AS ContNo
    FROM Adnan.dbo.TblFiterWaiver
    WHERE ContNo IS NOT NULL
), ActiveContracts AS (
    SELECT c.*
    FROM Adnan.dbo.TblContract c
    LEFT JOIN Settled s ON s.ContNo = CAST(c.ContNo AS int)
    WHERE ISNULL(c.EndContract, 0) = 0
      AND s.ContNo IS NULL
      AND (c.EndDate IS NULL OR c.EndDate >= '20260520')
)
SELECT c.ContNo,
       c.NoteSerial1,
       c.ContDate,
       c.StrDate,
       c.EndDate,
       c.Iqar,
       c.UnitNo,
       c.CusID,
       CASE WHEN p.Aqarid IS NULL THEN 1 ELSE 0 END AS MissingProperty,
       CASE WHEN u.Id IS NULL THEN 1 ELSE 0 END AS MissingUnit,
       CASE WHEN r.CusID IS NULL THEN 1 ELSE 0 END AS MissingRenter
FROM ActiveContracts c
LEFT JOIN Adnan.dbo.TblAqar p ON p.Aqarid = c.Iqar
LEFT JOIN Adnan.dbo.TblAqarDetai u ON u.Id = c.UnitNo
LEFT JOIN Adnan.dbo.TblCustemers r ON r.CusID = c.CusID
WHERE p.Aqarid IS NULL OR u.Id IS NULL OR r.CusID IS NULL
ORDER BY c.ContNo;

/* 05. Active renter account mapping against target chart */
;WITH Settled AS (
    SELECT DISTINCT CAST(ContNo AS int) AS ContNo
    FROM Adnan.dbo.TblFiterWaiver
    WHERE ContNo IS NOT NULL
), ActiveContracts AS (
    SELECT c.*
    FROM Adnan.dbo.TblContract c
    LEFT JOIN Settled s ON s.ContNo = CAST(c.ContNo AS int)
    WHERE ISNULL(c.EndContract, 0) = 0
      AND s.ContNo IS NULL
      AND (c.EndDate IS NULL OR c.EndDate >= '20260520')
), ActiveRenters AS (
    SELECT DISTINCT r.CusID, r.CusName, r.Account_Code
    FROM ActiveContracts c
    INNER JOIN Adnan.dbo.TblCustemers r ON r.CusID = c.CusID
)
SELECT r.CusID,
       r.CusName,
       r.Account_Code,
       a.Account_ID AS AdnanAccountId,
       a.Account_Name AS AdnanAccountName,
       ca.Id AS AlromaizanChartOfAccountId,
       ca.ArName AS AlromaizanAccountName,
       CASE
           WHEN ISNULL(r.Account_Code,'') = '' THEN 'MissingOldAccountCode'
           WHEN a.Account_ID IS NULL THEN 'MissingInAdnanAccounts'
           WHEN ca.Id IS NULL THEN 'MissingInTargetChartOfAccount'
           ELSE 'Matched'
       END AS MappingStatus
FROM ActiveRenters r
LEFT JOIN Adnan.dbo.ACCOUNTS a ON a.Account_Code = r.Account_Code
LEFT JOIN Alromaizan.dbo.ChartOfAccount ca ON ca.Code = r.Account_Code COLLATE Arabic_CI_AS
ORDER BY MappingStatus, r.CusID;

/* 06. Voucher classification for strict active contracts */
;WITH Settled AS (
    SELECT DISTINCT CAST(ContNo AS int) AS ContNo
    FROM Adnan.dbo.TblFiterWaiver
    WHERE ContNo IS NOT NULL
), ActiveContracts AS (
    SELECT c.*
    FROM Adnan.dbo.TblContract c
    LEFT JOIN Settled s ON s.ContNo = CAST(c.ContNo AS int)
    WHERE ISNULL(c.EndContract, 0) = 0
      AND s.ContNo IS NULL
      AND (c.EndDate IS NULL OR c.EndDate >= '20260520')
)
SELECT n.NoteType,
       n.CashingType,
       n.NoteCashingType,
       COUNT(*) AS VoucherRows,
       COUNT(DISTINCT n.ContNo) AS ContractsCount,
       SUM(ISNULL(n.Note_Value,0)) AS TotalNoteValue
FROM Adnan.dbo.Notes n
INNER JOIN ActiveContracts c ON c.ContNo = n.ContNo
GROUP BY n.NoteType, n.CashingType, n.NoteCashingType
ORDER BY n.NoteType, n.CashingType, n.NoteCashingType;

/* 07. Accounting lines linked to active-contract receipt notes */
;WITH Settled AS (
    SELECT DISTINCT CAST(ContNo AS int) AS ContNo
    FROM Adnan.dbo.TblFiterWaiver
    WHERE ContNo IS NOT NULL
), ActiveContracts AS (
    SELECT c.*
    FROM Adnan.dbo.TblContract c
    LEFT JOIN Settled s ON s.ContNo = CAST(c.ContNo AS int)
    WHERE ISNULL(c.EndContract, 0) = 0
      AND s.ContNo IS NULL
      AND (c.EndDate IS NULL OR c.EndDate >= '20260520')
), ActiveReceiptNotes AS (
    SELECT n.NoteID, n.ContNo, n.Note_Value
    FROM Adnan.dbo.Notes n
    INNER JOIN ActiveContracts c ON c.ContNo = n.ContNo
    WHERE n.NoteType = 4
)
SELECT COUNT(*) AS AccountingRows,
       COUNT(DISTINCT dev.Notes_ID) AS NotesWithAccountingRows,
       SUM(CASE WHEN dev.Credit_Or_Debit = 0 THEN ISNULL(dev.Value,0) ELSE 0 END) AS DebitValue,
       SUM(CASE WHEN dev.Credit_Or_Debit = 1 THEN ISNULL(dev.Value,0) ELSE 0 END) AS CreditValue
FROM Adnan.dbo.DOUBLE_ENTREY_VOUCHERS dev
INNER JOIN ActiveReceiptNotes n ON n.NoteID = dev.Notes_ID;
