IF COL_LENGTH(N'dbo.Transactions', N'AccountTypeName1') IS NULL
    ALTER TABLE dbo.Transactions ADD AccountTypeName1 NVARCHAR(255) NULL;
GO

SET XACT_ABORT ON;
GO

DECLARE @MovedWalletNumbers INT;
DECLARE @FilledWalletNumbersFromPhone INT;
DECLARE @ClearedWrongNationalIds INT;
DECLARE @MigrationStartDate DATETIME;

SET @MigrationStartDate = '20260501';

BEGIN TRANSACTION;

UPDATE dbo.Transactions
SET AccountTypeName1 = CONVERT(NVARCHAR(100), CONVERT(DECIMAL(38, 0), Tet_NumPoket))
WHERE Transaction_Type = 21
  AND ISNULL(IsCashOut, 0) = 1
  AND Transaction_Date >= @MigrationStartDate
  AND Tet_NumPoket IS NOT NULL
  AND NULLIF(LTRIM(RTRIM(ISNULL(AccountTypeName1, N''))), N'') IS NULL;

SET @MovedWalletNumbers = @@ROWCOUNT;

UPDATE dbo.Transactions
SET AccountTypeName1 = LTRIM(RTRIM(CashCustomerPhone))
WHERE Transaction_Type = 21
  AND ISNULL(IsCashOut, 0) = 1
  AND Transaction_Date >= @MigrationStartDate
  AND NULLIF(LTRIM(RTRIM(ISNULL(AccountTypeName1, N''))), N'') IS NULL
  AND NULLIF(LTRIM(RTRIM(ISNULL(CashCustomerPhone, N''))), N'') IS NOT NULL;

SET @FilledWalletNumbersFromPhone = @@ROWCOUNT;

UPDATE dbo.Transactions
SET Tet_NumPoket = NULL
WHERE Transaction_Type = 21
  AND ISNULL(IsCashOut, 0) = 1
  AND Transaction_Date >= @MigrationStartDate
  AND Tet_NumPoket IS NOT NULL
  AND NULLIF(LTRIM(RTRIM(ISNULL(AccountTypeName1, N''))), N'') IS NOT NULL;

SET @ClearedWrongNationalIds = @@ROWCOUNT;

COMMIT TRANSACTION;

SELECT
    CONVERT(VARCHAR(10), @MigrationStartDate, 120) AS MigrationStartDate,
    @MovedWalletNumbers AS MovedWalletNumbers,
    @FilledWalletNumbersFromPhone AS FilledWalletNumbersFromPhone,
    @ClearedWrongNationalIds AS ClearedWrongNationalIds;
GO
