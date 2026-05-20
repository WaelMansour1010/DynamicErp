/*
02_BranchCashBankMapping_SELECT_ONLY_20260520.sql
Purpose: Diagnose branches, cash boxes, banks, payment methods, and active-contract receipt usage.
Mode: SELECT ONLY.
*/

;WITH Settled AS (
    SELECT DISTINCT CAST(ContNo AS int) ContNo FROM Adnan.dbo.TblFiterWaiver WHERE ContNo IS NOT NULL
), ActiveContracts AS (
    SELECT c.* FROM Adnan.dbo.TblContract c
    LEFT JOIN Settled s ON s.ContNo=CAST(c.ContNo AS int)
    WHERE ISNULL(c.EndContract,0)=0 AND s.ContNo IS NULL
      AND (c.EndDate IS NULL OR c.EndDate>='20260520')
      AND c.Iqar IS NOT NULL AND c.UnitNo IS NOT NULL AND c.CusID IS NOT NULL
)
SELECT c.Branch_NO, b.branch_name, COUNT(*) ContractCount
FROM ActiveContracts c
LEFT JOIN Adnan.dbo.TblBranchesData b ON b.branch_id=c.Branch_NO
GROUP BY c.Branch_NO,b.branch_name
ORDER BY ContractCount DESC;

;WITH Settled AS (
    SELECT DISTINCT CAST(ContNo AS int) ContNo FROM Adnan.dbo.TblFiterWaiver WHERE ContNo IS NOT NULL
), ActiveContracts AS (
    SELECT c.* FROM Adnan.dbo.TblContract c
    LEFT JOIN Settled s ON s.ContNo=CAST(c.ContNo AS int)
    WHERE ISNULL(c.EndContract,0)=0 AND s.ContNo IS NULL
      AND (c.EndDate IS NULL OR c.EndDate>='20260520')
      AND c.Iqar IS NOT NULL AND c.UnitNo IS NOT NULL AND c.CusID IS NOT NULL
)
SELECT n.NoteCashingType, n.CashingType, n.BoxID, bx.BoxName, n.BankID, bk.BankName, COUNT(*) ReceiptCount, SUM(ISNULL(n.Note_Value,0)) ReceiptTotal
FROM Adnan.dbo.Notes n
INNER JOIN ActiveContracts c ON c.ContNo=n.ContNo
LEFT JOIN Adnan.dbo.TblBoxesData bx ON bx.BoxID=n.BoxID
LEFT JOIN Adnan.dbo.BanksData bk ON bk.BankID=n.BankID
WHERE n.NoteType=4
GROUP BY n.NoteCashingType,n.CashingType,n.BoxID,bx.BoxName,n.BankID,bk.BankName
ORDER BY ReceiptCount DESC;

SELECT BoxID, BoxName, Account_Code, BranchId, Type
FROM Adnan.dbo.TblBoxesData
ORDER BY BoxID;

SELECT BankID, BankName, Account_Code, BranchId, account_no, IBan
FROM Adnan.dbo.BanksData
ORDER BY BankID;

SELECT Id, Code, ArName, EnName, IsActive, IsDeleted, AccountId, CommissionAccountId
FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.PaymentMethod
ORDER BY Id;

SELECT Id, ArName, EnName, IsActive, IsDeleted
FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.CashReceiptSourceType
ORDER BY Id;

SELECT Id, Code, ArName, EnName, IsActive, IsDeleted
FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.CashReceiptPaymentMethod
ORDER BY Id;

SELECT Id, Code, ArName, EnName, IsActive, IsDeleted
FROM Alromaizan_PropertyPilot_Adnan_20260520.dbo.CashIssuePaymentMethod
ORDER BY Id;
