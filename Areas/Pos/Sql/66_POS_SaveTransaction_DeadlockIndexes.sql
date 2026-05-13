/*
    POS Save Transaction Deadlock Mitigation Indexes
    SQL Server 2012 compatible.

    Purpose:
    - Reduce broad scans and lock duration inside dbo.usp_POS_SaveTransaction.
    - Target the two observed deadlock paths:
      1) Cash-in duplicate IPN/ManualNO check.
      2) Keshni card token availability checks against ItemSerial/VisaNumber/CardNo.

    Deployment note:
    - Run off-hours on production if Transactions/Transaction_Details are large.
    - This script is additive only: computed columns + nonclustered indexes.
*/

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.Transactions', N'U') IS NOT NULL
AND COL_LENGTH(N'dbo.Transactions', N'POS_ManualNO_Normalized') IS NULL
BEGIN
    ALTER TABLE dbo.Transactions
    ADD POS_ManualNO_Normalized AS NULLIF(LTRIM(RTRIM(ISNULL(ManualNO, N''))), N'') PERSISTED;
END
GO

IF OBJECT_ID(N'dbo.Transactions', N'U') IS NOT NULL
AND COL_LENGTH(N'dbo.Transactions', N'POS_VisaNumber_Normalized') IS NULL
BEGIN
    ALTER TABLE dbo.Transactions
    ADD POS_VisaNumber_Normalized AS NULLIF(LTRIM(RTRIM(ISNULL(VisaNumber, N''))), N'') PERSISTED;
END
GO

IF OBJECT_ID(N'dbo.Transaction_Details', N'U') IS NOT NULL
AND COL_LENGTH(N'dbo.Transaction_Details', N'POS_ItemSerial_Normalized') IS NULL
BEGIN
    ALTER TABLE dbo.Transaction_Details
    ADD POS_ItemSerial_Normalized AS NULLIF(LTRIM(RTRIM(ISNULL(ItemSerial, N''))), N'') PERSISTED;
END
GO

IF OBJECT_ID(N'dbo.TblCusCsh', N'U') IS NOT NULL
AND COL_LENGTH(N'dbo.TblCusCsh', N'POS_CardNo_Normalized') IS NULL
BEGIN
    ALTER TABLE dbo.TblCusCsh
    ADD POS_CardNo_Normalized AS NULLIF(LTRIM(RTRIM(ISNULL(CardNo, N''))), N'') PERSISTED;
END
GO

IF OBJECT_ID(N'dbo.Transactions', N'U') IS NOT NULL
AND NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Transactions')
      AND name = N'IX_POS_Transactions_CashIn_ManualNO'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_Transactions_CashIn_ManualNO
    ON dbo.Transactions
    (
        Transaction_Type,
        POS_ManualNO_Normalized,
        IsCashOut,
        TrafficViolations,
        isRecharg,
        Transaction_ID
    )
    INCLUDE (RechargeValue);
END
GO

IF OBJECT_ID(N'dbo.Transactions', N'U') IS NOT NULL
AND NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Transactions')
      AND name = N'IX_POS_Transactions_Card_VisaNumber'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_Transactions_Card_VisaNumber
    ON dbo.Transactions
    (
        Transaction_Type,
        POS_VisaNumber_Normalized,
        IsCancelled,
        Transaction_ID
    );
END
GO

IF OBJECT_ID(N'dbo.Transaction_Details', N'U') IS NOT NULL
AND NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Transaction_Details')
      AND name = N'IX_POS_TransactionDetails_ItemSerial_Transaction'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_TransactionDetails_ItemSerial_Transaction
    ON dbo.Transaction_Details
    (
        POS_ItemSerial_Normalized,
        Transaction_ID
    );
END
GO

IF OBJECT_ID(N'dbo.TblCusCsh', N'U') IS NOT NULL
AND NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.TblCusCsh')
      AND name = N'IX_POS_TblCusCsh_CardNo_EasyCashType'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_TblCusCsh_CardNo_EasyCashType
    ON dbo.TblCusCsh
    (
        POS_CardNo_Normalized,
        EasyCashType
    );
END
GO
