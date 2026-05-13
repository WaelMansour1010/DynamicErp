/*
    Kishny POS - Today invoices read indexes
    SQL Server 2012 compatible.

    Purpose:
    - Keep the side "today invoices" list away from save locks/scans.
    - The list is read-only UI data; code also uses NOLOCK and OPTION(RECOMPILE).
*/

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.Transactions', N'U') IS NOT NULL
AND NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Transactions')
      AND name = N'IX_POS_TodayInvoices_Read'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_TodayInvoices_Read
    ON dbo.Transactions
    (
        Transaction_Type,
        Transaction_Date,
        BranchId,
        UserID,
        Transaction_ID
    )
    INCLUDE
    (
        NoteSerial1,
        CashCustomerName,
        CashCustomerPhone,
        Phone2,
        VisaNumber,
        IPN,
        ManualNO,
        PayedValue,
        RechargeValue,
        VAT,
        NetValue,
        TrafficViolations,
        IsCashOut,
        IsCancelled
    );
END;
GO

IF OBJECT_ID(N'dbo.POS_ImportBatchRow', N'U') IS NOT NULL
AND NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.POS_ImportBatchRow')
      AND name = N'IX_POS_ImportBatchRow_Transaction_Status'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_ImportBatchRow_Transaction_Status
    ON dbo.POS_ImportBatchRow(TransactionId, Status, RowId)
    INCLUDE (BatchId, Message);
END;
GO

IF OBJECT_ID(N'dbo.Transaction_Details', N'U') IS NOT NULL
AND NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Transaction_Details')
      AND name = N'IX_POS_TransactionDetails_Transaction_ItemSerial'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_TransactionDetails_Transaction_ItemSerial
    ON dbo.Transaction_Details(Transaction_ID, ItemSerial);
END;
GO
