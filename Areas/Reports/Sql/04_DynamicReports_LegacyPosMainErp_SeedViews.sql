/*
Legacy Kishny POS / MainErp sample views.
Apply to databases that use legacy tables such as Transactions, TblUsers, and DOUBLE_ENTREY_VOUCHERS.
SQL Server 2012 compatible.
*/

IF OBJECT_ID(N'dbo.DynamicReport_vw_WebUsersSample', N'V') IS NOT NULL
    DROP VIEW dbo.DynamicReport_vw_WebUsersSample;
GO
CREATE VIEW dbo.DynamicReport_vw_WebUsersSample
AS
SELECT TOP (1000)
    UserID AS UserId,
    UserName,
    UserType,
    BranchId,
    StoreID,
    IsActive
FROM dbo.TblUsers;
GO

IF OBJECT_ID(N'dbo.DynamicReport_vw_PosSalesSample', N'V') IS NOT NULL
    DROP VIEW dbo.DynamicReport_vw_PosSalesSample;
GO
CREATE VIEW dbo.DynamicReport_vw_PosSalesSample
AS
SELECT TOP (1000)
    Transaction_ID AS SalesTransactionId,
    Transaction_Serial,
    Transaction_Date,
    Transaction_Type,
    BranchId,
    StoreID,
    UserID,
    NetValue,
    PayedValue,
    RemainValue
FROM dbo.Transactions;
GO

IF OBJECT_ID(N'dbo.DynamicReport_vw_MainErpJournalSample', N'V') IS NOT NULL
    DROP VIEW dbo.DynamicReport_vw_MainErpJournalSample;
GO
CREATE VIEW dbo.DynamicReport_vw_MainErpJournalSample
AS
SELECT TOP (1000)
    Double_Entry_Vouchers_ID AS JournalLineId,
    DEV_Serial,
    Account_Code,
    RecordDate,
    Credit_Or_Debit,
    credit_value,
    depet_value,
    branch_id,
    UserID,
    Posted
FROM dbo.DOUBLE_ENTREY_VOUCHERS;
GO
