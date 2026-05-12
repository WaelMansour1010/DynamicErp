/*
    Optional rollback for candidate POS reporting indexes.
    Use only if 34_POS_PerformanceStoredProcedures.sql was executed before the
    index section was removed after load testing.

    Test result on local Cash, 2026-05-04:
    - 120-worker mixed load, 10 minutes
    - Save latency became worse after adding candidate indexes
    - Recommendation: keep stored procedures, do not keep these indexes unless a
      read-heavy benchmark proves the gain is worth the write overhead.
*/

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_Transactions_TypeDateBranchUser' AND object_id = OBJECT_ID(N'dbo.Transactions'))
    DROP INDEX IX_POS_Transactions_TypeDateBranchUser ON dbo.Transactions;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_TransactionDetails_TransactionItem' AND object_id = OBJECT_ID(N'dbo.Transaction_Details'))
    DROP INDEX IX_POS_TransactionDetails_TransactionItem ON dbo.Transaction_Details;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_TransactionDetails_ItemTransaction' AND object_id = OBJECT_ID(N'dbo.Transaction_Details'))
    DROP INDEX IX_POS_TransactionDetails_ItemTransaction ON dbo.Transaction_Details;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_Transactions_TypeIPN' AND object_id = OBJECT_ID(N'dbo.Transactions'))
    DROP INDEX IX_POS_Transactions_TypeIPN ON dbo.Transactions;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_Transactions_TypeCustomerSearch' AND object_id = OBJECT_ID(N'dbo.Transactions'))
    DROP INDEX IX_POS_Transactions_TypeCustomerSearch ON dbo.Transactions;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_DEV_RecordBranchAccountNotes' AND object_id = OBJECT_ID(N'dbo.DOUBLE_ENTREY_VOUCHERS'))
    DROP INDEX IX_POS_DEV_RecordBranchAccountNotes ON dbo.DOUBLE_ENTREY_VOUCHERS;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_Notes_TypeDateBranchUser' AND object_id = OBJECT_ID(N'dbo.Notes'))
    DROP INDEX IX_POS_Notes_TypeDateBranchUser ON dbo.Notes;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_TblCusCsh_KycCardNo' AND object_id = OBJECT_ID(N'dbo.TblCusCsh'))
    DROP INDEX IX_POS_TblCusCsh_KycCardNo ON dbo.TblCusCsh;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_TblCusCsh_KycCardId' AND object_id = OBJECT_ID(N'dbo.TblCusCsh'))
    DROP INDEX IX_POS_TblCusCsh_KycCardId ON dbo.TblCusCsh;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_TblCusCsh_KycNationalId' AND object_id = OBJECT_ID(N'dbo.TblCusCsh'))
    DROP INDEX IX_POS_TblCusCsh_KycNationalId ON dbo.TblCusCsh;
GO
