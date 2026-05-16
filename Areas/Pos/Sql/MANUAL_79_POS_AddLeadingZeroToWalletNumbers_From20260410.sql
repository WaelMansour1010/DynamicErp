/*
    POS manual data fix:
    Add a leading zero to wallet numbers saved without it from 10 April 2026 onward.

    Target column:
      dbo.Transactions.AccountTypeName1

    Scope:
      POS sales transactions only, cash-out and traffic-violation wallet transactions.

    Safety:
      - Updates only numeric wallet values that start with 1-9.
      - Does not touch values already starting with 0.
      - Does not touch non-numeric values.
*/

SET XACT_ABORT ON;
GO

DECLARE @FromDate DATETIME = '20260410';

IF COL_LENGTH(N'dbo.Transactions', N'AccountTypeName1') IS NULL
BEGIN
    RAISERROR(N'Column dbo.Transactions.AccountTypeName1 does not exist.', 16, 1);
    RETURN;
END;

PRINT N'Preview rows that will be updated:';

SELECT TOP (200)
    Transaction_ID,
    NoteSerial1,
    Transaction_Date,
    CASE
        WHEN ISNULL(IsCashOut, 0) = 1 THEN N'Cash Out'
        WHEN ISNULL(TrafficViolations, 0) = 1 THEN N'Violations'
        ELSE N'Other'
    END AS WalletTransactionType,
    AccountTypeName1 AS OldWalletNumber,
    N'0' + LTRIM(RTRIM(AccountTypeName1)) AS NewWalletNumber
FROM dbo.Transactions
WHERE Transaction_Type = 21
  AND Transaction_Date >= @FromDate
  AND (ISNULL(IsCashOut, 0) = 1 OR ISNULL(TrafficViolations, 0) = 1)
  AND NULLIF(LTRIM(RTRIM(ISNULL(AccountTypeName1, N''))), N'') IS NOT NULL
  AND LTRIM(RTRIM(AccountTypeName1)) LIKE N'[1-9]%'
  AND LTRIM(RTRIM(AccountTypeName1)) NOT LIKE N'0%'
  AND LTRIM(RTRIM(AccountTypeName1)) NOT LIKE N'%[^0-9]%'
ORDER BY Transaction_Date, Transaction_ID;

BEGIN TRANSACTION;

UPDATE dbo.Transactions
SET AccountTypeName1 = N'0' + LTRIM(RTRIM(AccountTypeName1))
WHERE Transaction_Type = 21
  AND Transaction_Date >= @FromDate
  AND (ISNULL(IsCashOut, 0) = 1 OR ISNULL(TrafficViolations, 0) = 1)
  AND NULLIF(LTRIM(RTRIM(ISNULL(AccountTypeName1, N''))), N'') IS NOT NULL
  AND LTRIM(RTRIM(AccountTypeName1)) LIKE N'[1-9]%'
  AND LTRIM(RTRIM(AccountTypeName1)) NOT LIKE N'0%'
  AND LTRIM(RTRIM(AccountTypeName1)) NOT LIKE N'%[^0-9]%';

DECLARE @UpdatedRows INT = @@ROWCOUNT;

COMMIT TRANSACTION;

SELECT
    CONVERT(VARCHAR(10), @FromDate, 120) AS FromDate,
    @UpdatedRows AS UpdatedRows;
GO
