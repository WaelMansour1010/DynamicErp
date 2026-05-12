/*
    POS legacy IPN description update
    SQL Server 2012 compatible.

    Updates only the journal lines that are definitely linked back to POS
    transactions through Transaction_ID and still carry the legacy wrong text.
    Review the preview script first before running this update.
*/

SET XACT_ABORT ON;

BEGIN TRANSACTION;

;WITH AffectedLines AS
(
    SELECT
        d.Double_Entry_Vouchers_ID,
        NewDescription =
            N'العميل : ' + ISNULL(t.CashCustomerName, N'')
                + N' IPN ' + ISNULL(t.ManualNO, N'')
                + N' فرع ' + ISNULL(b.branch_name, N'')
                + N' فاتورة رقم ' + CONVERT(NVARCHAR(50), t.NoteSerial1)
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
)
UPDATE d
SET d.Double_Entry_Vouchers_Description = a.NewDescription
OUTPUT
    deleted.Double_Entry_Vouchers_ID,
    deleted.Transaction_ID,
    deleted.DEV_ID_Line_No,
    deleted.Double_Entry_Vouchers_Description AS OldDescription,
    inserted.Double_Entry_Vouchers_Description AS NewDescription
FROM dbo.DOUBLE_ENTREY_VOUCHERS AS d
INNER JOIN AffectedLines AS a
    ON a.Double_Entry_Vouchers_ID = d.Double_Entry_Vouchers_ID
WHERE ISNULL(d.Double_Entry_Vouchers_Description, N'') <> a.NewDescription;

COMMIT TRANSACTION;
GO
