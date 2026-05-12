/*
Optional sample views for Dynamic Report Designer.
Apply only after confirming referenced base tables exist in the target database.
*/

IF OBJECT_ID(N'dbo.DynamicReport_vw_WebUsersSample', N'V') IS NOT NULL
    DROP VIEW dbo.DynamicReport_vw_WebUsersSample;
GO
CREATE VIEW dbo.DynamicReport_vw_WebUsersSample
AS
SELECT TOP (1000)
    Id AS UserId,
    UserName,
    Name,
    IsActive,
    IsDeleted
FROM dbo.ERPUser;
GO

IF OBJECT_ID(N'dbo.DynamicReport_vw_PosSalesSample', N'V') IS NOT NULL
    DROP VIEW dbo.DynamicReport_vw_PosSalesSample;
GO
CREATE VIEW dbo.DynamicReport_vw_PosSalesSample
AS
SELECT TOP (1000)
    Id AS SalesInvoiceId,
    DocumentNumber,
    VoucherDate,
    DepartmentId,
    VendorOrCustomerId,
    NetTotal AS NetAmount,
    IsActive,
    IsDeleted
FROM dbo.SalesInvoice;
GO

IF OBJECT_ID(N'dbo.DynamicReport_vw_MainErpJournalSample', N'V') IS NOT NULL
    DROP VIEW dbo.DynamicReport_vw_MainErpJournalSample;
GO
CREATE VIEW dbo.DynamicReport_vw_MainErpJournalSample
AS
SELECT TOP (1000)
    Id AS JournalEntryId,
    DocumentNumber,
    [Date] AS JournalEntryDate,
    DepartmentId,
    SourcePageId,
    SourceId,
    IsActive,
    IsDeleted
FROM dbo.JournalEntry;
GO
