/*
04_MissingContractLinks_SELECT_ONLY_20260520.sql
Purpose: Read-only diagnostics for the 10 strict-active Adnan contracts with missing critical links.
Mode: SELECT ONLY.
*/

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
), Paid AS (
    SELECT d.istallid,
           SUM(ISNULL(d.RentValuePayed,0)+ISNULL(d.CommissionsPayed,0)+ISNULL(d.InsurancePayed,0)+ISNULL(d.WaterPayed,0)+ISNULL(d.ElectricPayed,0)+ISNULL(d.TelandNetPayed,0)+ISNULL(d.OldValuePayed,0)+ISNULL(d.VATPayed,0)) AS TruePaid
    FROM Adnan.dbo.ContracttBillInstallmentsDone d
    LEFT JOIN Adnan.dbo.Notes n ON n.NoteID=d.NoteID
    WHERE n.NoteType=4 OR n.NoteType IS NULL
    GROUP BY d.istallid
), Bal AS (
    SELECT i.ContNo,
           COUNT(*) AS InstallmentRows,
           SUM(ISNULL(i.installValue,0)) AS InstallmentTotal,
           SUM(ISNULL(p.TruePaid,0)) AS TruePaid,
           SUM(ISNULL(i.installValue,0)-ISNULL(p.TruePaid,0)) AS TrueRemain
    FROM Adnan.dbo.TblContractInstallments i
    LEFT JOIN Paid p ON p.istallid=i.id
    GROUP BY i.ContNo
)
SELECT c.ContNo AS OldContractNo,
       c.NoteSerial1 AS OldDisplayContractNo,
       c.ContDate,
       c.StrDate,
       c.EndDate,
       c.Iqar AS OldPropertyId,
       p.aqarNo AS OldPropertyNo,
       c.UnitNo AS OldUnitId,
       u.unitno AS OldUnitNo,
       c.CusID AS OldRenterId,
       r.CusName AS RenterName,
       r.Account_Code,
       a.Account_ID AS OldAccountId,
       ca.Id AS TargetAccountId,
       CASE WHEN p.Aqarid IS NULL THEN 1 ELSE 0 END AS MissingProperty,
       CASE WHEN u.Id IS NULL THEN 1 ELSE 0 END AS MissingUnit,
       CASE WHEN r.CusID IS NULL THEN 1 ELSE 0 END AS MissingRenter,
       CASE WHEN ISNULL(r.Account_Code,'')='' THEN 1 WHEN a.Account_ID IS NULL THEN 1 WHEN ca.Id IS NULL THEN 1 ELSE 0 END AS MissingOrUnmappedAccount,
       b.InstallmentRows,
       b.InstallmentTotal,
       b.TruePaid,
       b.TrueRemain,
       CASE
           WHEN c.Iqar IS NULL AND c.UnitNo IS NULL AND c.CusID IS NULL THEN 'Contract shell row: core fields are blank. Exclude From Pilot / Archive Only.'
           WHEN r.CusID IS NULL THEN 'Manual Mapping or Archive Only - renter missing.'
           WHEN p.Aqarid IS NULL OR u.Id IS NULL THEN 'Manual Mapping or Exclude - property/unit missing.'
           WHEN ca.Id IS NULL THEN 'Blocked until Account Mapping/Seeding.'
           ELSE 'Can Migrate'
       END AS SuggestedDecision
FROM ActiveContracts c
LEFT JOIN Adnan.dbo.TblAqar p ON p.Aqarid = c.Iqar
LEFT JOIN Adnan.dbo.TblAqarDetai u ON u.Id = c.UnitNo
LEFT JOIN Adnan.dbo.TblCustemers r ON r.CusID = c.CusID
LEFT JOIN Adnan.dbo.ACCOUNTS a ON a.Account_Code = r.Account_Code
LEFT JOIN Alromaizan.dbo.ChartOfAccount ca ON ca.Code = r.Account_Code COLLATE Arabic_CI_AS
LEFT JOIN Bal b ON CAST(b.ContNo AS int) = CAST(c.ContNo AS int)
WHERE p.Aqarid IS NULL OR u.Id IS NULL OR r.CusID IS NULL
ORDER BY c.ContNo;

/* Supporting check: same display contract numbers */
SELECT NoteSerial1, COUNT(*) AS ContractRows
FROM Adnan.dbo.TblContract
WHERE NoteSerial1 IN (N'219030003', N'120030012', N'219050021', N'121080003', N'119020012', N'121070003', N'119060033', N'119080012')
GROUP BY NoteSerial1
ORDER BY NoteSerial1;
