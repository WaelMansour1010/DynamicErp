IF OBJECT_ID('dbo.SalesInvoice', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.SalesInvoice', 'LastModifiedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.SalesInvoice ADD LastModifiedByUserId int NULL;
END
GO

IF OBJECT_ID('dbo.SalesInvoice', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.SalesInvoice', 'LastModifiedDate') IS NULL
BEGIN
    ALTER TABLE dbo.SalesInvoice ADD LastModifiedDate datetime NULL;
END
GO

IF OBJECT_ID('dbo.SalesInvoice', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.SalesInvoice', 'LastModifiedByUserId') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE name = 'IX_SalesInvoice_LastModifiedByUserId'
         AND object_id = OBJECT_ID('dbo.SalesInvoice')
   )
BEGIN
    CREATE INDEX IX_SalesInvoice_LastModifiedByUserId
    ON dbo.SalesInvoice(LastModifiedByUserId);
END
GO

IF OBJECT_ID('dbo.SalesInvoice', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.ERPUsers', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.SalesInvoice', 'LastModifiedByUserId') IS NOT NULL
   AND OBJECT_ID('dbo.FK_SalesInvoice_LastModifiedByUser', 'F') IS NULL
BEGIN
    ALTER TABLE dbo.SalesInvoice WITH NOCHECK
    ADD CONSTRAINT FK_SalesInvoice_LastModifiedByUser
    FOREIGN KEY (LastModifiedByUserId) REFERENCES dbo.ERPUsers(Id);
END
GO
