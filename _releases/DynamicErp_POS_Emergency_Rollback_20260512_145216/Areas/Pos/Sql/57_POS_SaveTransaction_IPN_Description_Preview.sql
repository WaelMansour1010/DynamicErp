/*
    POS legacy IPN description preview
    SQL Server 2012 compatible.

    Preview only. Does not modify data.
    Uses Transaction_ID to link each journal line back to the original POS invoice.
*/

SELECT
    d.Double_Entry_Vouchers_ID,
    d.DEV_ID_Line_No,
    d.Transaction_ID,
    t.NoteSerial1,
    t.Transaction_Date,
    t.CashCustomerName,
    t.CashCustomerPhone,
    t.IPN AS LegacyID,
    t.ManualNO AS LegacyIPN,
    b.branch_name AS BranchName,
    d.Account_Code,
    d.Value,
    d.Credit_Or_Debit,
    d.Double_Entry_Vouchers_Description AS CurrentDescription,
    N'العميل : ' + ISNULL(t.CashCustomerName, N'')
        + N' IPN ' + ISNULL(t.ManualNO, N'')
        + N' فرع ' + ISNULL(b.branch_name, N'')
        + N' فاتورة رقم ' + CONVERT(NVARCHAR(50), t.NoteSerial1) AS ExpectedDescription
FROM dbo.DOUBLE_ENTREY_VOUCHERS AS d
INNER JOIN dbo.Transactions AS t
    ON t.Transaction_ID = d.Transaction_ID
LEFT JOIN dbo.TblBranchesData AS b
    ON b.branch_id = t.BranchId
WHERE t.Transaction_Type = 21
  AND ISNULL(t.IsCashOut, 0) = 0
  AND ISNULL(t.TrafficViolations, 0) = 0
  AND ISNULL(t.OtherItems, 0) = 0
  AND ISNULL(t.HaveGuarantee, 0) = 0
  AND NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NULL
  AND ISNULL(d.Double_Entry_Vouchers_Description, N'') LIKE N'% IPN %'
ORDER BY
    t.Transaction_Date DESC,
    t.Transaction_ID DESC,
    d.Double_Entry_Vouchers_ID,
    d.DEV_ID_Line_No;
GO
