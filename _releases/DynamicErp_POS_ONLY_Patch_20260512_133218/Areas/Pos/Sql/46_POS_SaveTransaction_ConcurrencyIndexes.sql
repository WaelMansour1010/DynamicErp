/*
    POS Save Transaction Concurrency Indexes
    SQL Server 2012 compatible.

    Purpose:
    - Reduce lock duration in dbo.usp_POS_SaveTransaction save/edit paths.
    - Keep indexes narrow and tied directly to WHERE/JOIN predicates used during save.
*/

IF OBJECT_ID(N'dbo.DOUBLE_ENTREY_VOUCHERS', N'U') IS NOT NULL
AND NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.DOUBLE_ENTREY_VOUCHERS')
      AND name = N'IX_POS_DEV_Transaction_ID'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_DEV_Transaction_ID
    ON dbo.DOUBLE_ENTREY_VOUCHERS (Transaction_ID)
    INCLUDE (Notes_ID);
END
GO

IF OBJECT_ID(N'dbo.DOUBLE_ENTREY_VOUCHERS', N'U') IS NOT NULL
AND NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.DOUBLE_ENTREY_VOUCHERS')
      AND name = N'IX_POS_DEV_Notes_ID'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_DEV_Notes_ID
    ON dbo.DOUBLE_ENTREY_VOUCHERS (Notes_ID)
    INCLUDE (Transaction_ID);
END
GO

IF OBJECT_ID(N'dbo.DOUBLE_ENTREY_VOUCHERS', N'U') IS NOT NULL
AND NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.DOUBLE_ENTREY_VOUCHERS')
      AND name = N'IX_POS_DEV_RecordDate'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_DEV_RecordDate
    ON dbo.DOUBLE_ENTREY_VOUCHERS (RecordDate, Double_Entry_Vouchers_ID);
END
GO

IF OBJECT_ID(N'dbo.Notes', N'U') IS NOT NULL
AND NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Notes')
      AND name = N'IX_POS_Notes_Transaction_ID'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_Notes_Transaction_ID
    ON dbo.Notes (Transaction_ID)
    INCLUDE (NoteID);
END
GO

IF OBJECT_ID(N'dbo.Transaction_Details', N'U') IS NOT NULL
AND NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Transaction_Details')
      AND name = N'IX_POS_Transaction_Details_SaveIssue'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_Transaction_Details_SaveIssue
    ON dbo.Transaction_Details (Transaction_ID, SavedItemType, StoreID2);
END
GO

IF OBJECT_ID(N'dbo.TblSalesPayment', N'U') IS NOT NULL
AND NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.TblSalesPayment')
      AND name = N'IX_POS_TblSalesPayment_TransID'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_POS_TblSalesPayment_TransID
    ON dbo.TblSalesPayment (TransID);
END
GO
