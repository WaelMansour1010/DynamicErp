/*
Property Migration Diagnostics - SELECT ONLY
Date: 2026-05-20

Purpose:
Pre-migration data review for moving VB6 property data from RSMDB/Adnan to DynamicErp Web.

Rules:
- SELECT only.
- Do not add UPDATE, DELETE, INSERT, ALTER, DROP, CREATE, MERGE, TRUNCATE, EXEC, or data-changing procedure calls.
*/

/* ============================================================
   Legacy VB6 diagnostics
   Run this section in RSMDB, then run it again in Adnan.
   ============================================================ */

SELECT DB_NAME() AS DatabaseName, 'Properties' AS Metric, COUNT(*) AS Value FROM dbo.TblAqar
UNION ALL SELECT DB_NAME(), 'Units', COUNT(*) FROM dbo.TblAqarDetai
UNION ALL SELECT DB_NAME(), 'Renters/Customers', COUNT(*) FROM dbo.TblCustemers
UNION ALL SELECT DB_NAME(), 'Contracts', COUNT(*) FROM dbo.TblContract
UNION ALL SELECT DB_NAME(), 'ContractInstallments', COUNT(*) FROM dbo.TblContractInstallments
UNION ALL SELECT DB_NAME(), 'SettlementHeaders', COUNT(*) FROM dbo.TblFiterWaiver
UNION ALL SELECT DB_NAME(), 'Notes/Vouchers', COUNT(*) FROM dbo.Notes
UNION ALL SELECT DB_NAME(), 'Accounts', COUNT(*) FROM dbo.ACCOUNTS;

SELECT 'Contracts missing property' AS Issue, c.ContNo, c.Iqar
FROM dbo.TblContract c
LEFT JOIN dbo.TblAqar a ON a.Aqarid = c.Iqar
WHERE c.Iqar IS NOT NULL AND a.Aqarid IS NULL;

SELECT 'Contracts missing unit' AS Issue, c.ContNo, c.Iqar, c.UnitNo
FROM dbo.TblContract c
LEFT JOIN dbo.TblAqarDetai u ON u.Id = c.UnitNo
WHERE c.UnitNo IS NOT NULL AND u.Id IS NULL;

SELECT 'Contracts missing renter' AS Issue, c.ContNo, c.CusID
FROM dbo.TblContract c
LEFT JOIN dbo.TblCustemers cu ON cu.CusID = c.CusID
WHERE c.CusID IS NOT NULL AND cu.CusID IS NULL;

SELECT 'Installments missing contract' AS Issue, i.id, i.ContNo, i.InstallNo, i.installValue
FROM dbo.TblContractInstallments i
LEFT JOIN dbo.TblContract c ON c.ContNo = i.ContNo
WHERE i.ContNo IS NOT NULL AND c.ContNo IS NULL;

SELECT 'Installments with negative remain' AS Issue, id, ContNo, InstallNo, installValue, payed, Remains
FROM dbo.TblContractInstallments
WHERE ISNULL(Remains, 0) < 0;

SELECT 'Installments paid over value' AS Issue, id, ContNo, InstallNo, installValue, payed, Remains
FROM dbo.TblContractInstallments
WHERE ISNULL(payed, 0) > ISNULL(installValue, 0);

SELECT 'Contracts invalid dates' AS Issue, ContNo, StrDate, EndDate
FROM dbo.TblContract
WHERE StrDate IS NOT NULL
  AND EndDate IS NOT NULL
  AND EndDate < StrDate;

SELECT 'Duplicate contract numbers' AS Issue, ContNo, COUNT(*) AS DuplicateCount
FROM dbo.TblContract
GROUP BY ContNo
HAVING COUNT(*) > 1;

SELECT 'Duplicate unit per property/unitno' AS Issue, Aqarid, unitno, COUNT(*) AS DuplicateCount
FROM dbo.TblAqarDetai
GROUP BY Aqarid, unitno
HAVING COUNT(*) > 1;

SELECT 'Settlements missing contract' AS Issue, f.ID, f.ContNo, f.RenterID, f.BulidID
FROM dbo.TblFiterWaiver f
LEFT JOIN dbo.TblContract c ON c.ContNo = f.ContNo
WHERE f.ContNo IS NOT NULL AND c.ContNo IS NULL;

SELECT 'Settlements missing renter' AS Issue, f.ID, f.ContNo, f.RenterID
FROM dbo.TblFiterWaiver f
LEFT JOIN dbo.TblCustemers cu ON cu.CusID = f.RenterID
WHERE f.RenterID IS NOT NULL AND cu.CusID IS NULL;

SELECT 'Notes linked to missing contract' AS Issue, n.NoteID, n.NoteType, n.NoteSerial, n.ContNo
FROM dbo.Notes n
LEFT JOIN dbo.TblContract c ON c.ContNo = n.ContNo
WHERE n.ContNo IS NOT NULL AND c.ContNo IS NULL;

SELECT 'Notes linked to missing renter' AS Issue, n.NoteID, n.NoteType, n.NoteSerial, n.CusID
FROM dbo.Notes n
LEFT JOIN dbo.TblCustemers cu ON cu.CusID = n.CusID
WHERE n.CusID IS NOT NULL AND cu.CusID IS NULL;

SELECT 'Customers without account code' AS Issue, CusID, CusName, Account_Code
FROM dbo.TblCustemers
WHERE NULLIF(LTRIM(RTRIM(Account_Code)), '') IS NULL;

SELECT 'Customers account code missing in ACCOUNTS' AS Issue, c.CusID, c.CusName, c.Account_Code
FROM dbo.TblCustemers c
LEFT JOIN dbo.ACCOUNTS a ON a.Account_Code = c.Account_Code
WHERE NULLIF(LTRIM(RTRIM(c.Account_Code)), '') IS NOT NULL
  AND a.Account_Code IS NULL;

SELECT TOP (100)
       DB_NAME() AS DatabaseName,
       NoteType,
       COUNT(*) AS CountRows,
       SUM(ISNULL(Note_Value, 0)) AS TotalValue,
       SUM(CASE WHEN ContNo IS NOT NULL THEN 1 ELSE 0 END) AS LinkedContractCount,
       SUM(CASE WHEN FiterWaiver IS NOT NULL OR FilterID IS NOT NULL THEN 1 ELSE 0 END) AS LinkedSettlementCount,
       SUM(CASE WHEN CusID IS NOT NULL THEN 1 ELSE 0 END) AS LinkedCustomerCount
FROM dbo.Notes
GROUP BY NoteType
ORDER BY CountRows DESC;

SELECT TOP (100)
       i.ContNo,
       COUNT(*) AS InstallmentCount,
       SUM(ISNULL(i.installValue, 0)) AS InstallmentTotal,
       SUM(ISNULL(i.payed, 0)) AS PaidTotal,
       SUM(ISNULL(i.Remains, 0)) AS RemainTotal,
       SUM(ISNULL(i.RentValue, 0)) AS RentTotal,
       SUM(ISNULL(i.Commissions, 0)) AS CommissionTotal,
       SUM(ISNULL(i.Insurance, 0)) AS InsuranceTotal,
       SUM(ISNULL(i.Water, 0)) AS WaterTotal,
       SUM(ISNULL(i.Electric, 0)) AS ElectricTotal,
       SUM(ISNULL(i.VATValue, 0)) AS VatTotal
FROM dbo.TblContractInstallments i
GROUP BY i.ContNo
ORDER BY ABS(SUM(ISNULL(i.Remains, 0))) DESC;

SELECT TOP (100)
       f.ID,
       f.ContNo,
       f.ContractNo,
       f.RenterID,
       f.BulidID,
       f.ApartmentID,
       f.RecordDate,
       f.FilterDate,
       f.EndDate,
       f.Insurance,
       f.OFRenter,
       f.ForRenter,
       f.RemainRent,
       f.RemainWater,
       f.RemainService,
       f.net,
       f.NoteID,
       f.NoteSerial
FROM dbo.TblFiterWaiver f
ORDER BY f.ID DESC;

/* ============================================================
   DynamicErp target diagnostics
   Run this section in Alromaizan.
   ============================================================ */

SELECT DB_NAME() AS DatabaseName, 'Property' AS Metric, COUNT(*) AS Value FROM dbo.Property
UNION ALL SELECT DB_NAME(), 'PropertyDetail units', COUNT(*) FROM dbo.PropertyDetail
UNION ALL SELECT DB_NAME(), 'PropertyRenter', COUNT(*) FROM dbo.PropertyRenter
UNION ALL SELECT DB_NAME(), 'PropertyOwner', COUNT(*) FROM dbo.PropertyOwner
UNION ALL SELECT DB_NAME(), 'PropertyContract', COUNT(*) FROM dbo.PropertyContract
UNION ALL SELECT DB_NAME(), 'PropertyContractBatch', COUNT(*) FROM dbo.PropertyContractBatch
UNION ALL SELECT DB_NAME(), 'CashReceiptVoucher', COUNT(*) FROM dbo.CashReceiptVoucher
UNION ALL SELECT DB_NAME(), 'CashReceiptVoucherPropertyContractBatch', COUNT(*) FROM dbo.CashReceiptVoucherPropertyContractBatch
UNION ALL SELECT DB_NAME(), 'PropertyContractTermination', COUNT(*) FROM dbo.PropertyContractTermination
UNION ALL SELECT DB_NAME(), 'PropertyDueBatch', COUNT(*) FROM dbo.PropertyDueBatch
UNION ALL SELECT DB_NAME(), 'PropertyRevenueProof', COUNT(*) FROM dbo.PropertyRevenueProof
UNION ALL SELECT DB_NAME(), 'JournalEntry', COUNT(*) FROM dbo.JournalEntry
UNION ALL SELECT DB_NAME(), 'JournalEntryDetail', COUNT(*) FROM dbo.JournalEntryDetail
UNION ALL SELECT DB_NAME(), 'ChartOfAccount', COUNT(*) FROM dbo.ChartOfAccount;

SELECT 'Contracts without property' AS Issue, pc.Id, pc.DocumentNumber, pc.PropertyId
FROM dbo.PropertyContract pc
LEFT JOIN dbo.Property p ON p.Id = pc.PropertyId
WHERE pc.IsDeleted = 0 AND pc.PropertyId IS NOT NULL AND p.Id IS NULL;

SELECT 'Contracts without renter' AS Issue, pc.Id, pc.DocumentNumber, pc.PropertyRenterId
FROM dbo.PropertyContract pc
LEFT JOIN dbo.PropertyRenter r ON r.Id = pc.PropertyRenterId
WHERE pc.IsDeleted = 0 AND pc.PropertyRenterId IS NOT NULL AND r.Id IS NULL;

SELECT 'Contracts without unit' AS Issue, pc.Id, pc.DocumentNumber, pc.PropertyUnitId
FROM dbo.PropertyContract pc
LEFT JOIN dbo.PropertyDetail u ON u.Id = pc.PropertyUnitId
WHERE pc.IsDeleted = 0 AND pc.PropertyUnitId IS NOT NULL AND u.Id IS NULL;

SELECT 'Batches without contract' AS Issue, b.Id, b.MainDocId, b.BatchNo, b.BatchTotal
FROM dbo.PropertyContractBatch b
LEFT JOIN dbo.PropertyContract pc ON pc.Id = b.MainDocId
WHERE b.IsDeleted = 0 AND pc.Id IS NULL;

SELECT 'Receipt batch links without receipt' AS Issue, l.Id, l.CashReceiptVoucherId, l.PropertyContractBatchId
FROM dbo.CashReceiptVoucherPropertyContractBatch l
LEFT JOIN dbo.CashReceiptVoucher r ON r.Id = l.CashReceiptVoucherId
WHERE r.Id IS NULL;

SELECT 'Receipt batch links without batch' AS Issue, l.Id, l.CashReceiptVoucherId, l.PropertyContractBatchId
FROM dbo.CashReceiptVoucherPropertyContractBatch l
LEFT JOIN dbo.PropertyContractBatch b ON b.Id = l.PropertyContractBatchId
WHERE l.PropertyContractBatchId IS NOT NULL AND b.Id IS NULL;

SELECT 'Renters without account' AS Issue, Id, Code, ArName, AccountId
FROM dbo.PropertyRenter
WHERE IsDeleted = 0 AND AccountId IS NULL;

SELECT 'Owners without account' AS Issue, Id, Code, ArName, AccountId
FROM dbo.PropertyOwner
WHERE IsDeleted = 0 AND AccountId IS NULL;

SELECT 'Department property account mapping review' AS Issue,
       Id,
       ArName,
       DueRentId,
       RentRevenueId,
       RenterAndBuyerAccountId,
       OwnerAccountId,
       RefundableInsuranceAccountForRentersId,
       PropertyExpensesAccountId,
       CommissionAccountId,
       CreditValueAddedTaxesAccountId,
       DebitValueAddedTaxesAccountId
FROM dbo.Department;

SELECT 'Duplicate unit per property/unitno' AS Issue, MainDocId, PropertyUnitNo, COUNT(*) AS DuplicateCount
FROM dbo.PropertyDetail
WHERE IsDeleted = 0
GROUP BY MainDocId, PropertyUnitNo
HAVING COUNT(*) > 1;

SELECT 'Contract batch payment summary' AS Issue,
       b.Id AS PropertyContractBatchId,
       b.MainDocId AS PropertyContractId,
       b.BatchNo,
       b.BatchTotal,
       SUM(ISNULL(l.Paid, 0)) AS PaidTotal,
       b.BatchTotal - SUM(ISNULL(l.Paid, 0)) AS CalculatedRemain,
       MAX(CASE WHEN l.IsDelivered = 1 THEN 1 ELSE 0 END) AS HasDeliveredReceipt
FROM dbo.PropertyContractBatch b
LEFT JOIN dbo.CashReceiptVoucherPropertyContractBatch l ON l.PropertyContractBatchId = b.Id
WHERE b.IsDeleted = 0
GROUP BY b.Id, b.MainDocId, b.BatchNo, b.BatchTotal;
