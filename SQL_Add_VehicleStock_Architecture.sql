SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('dbo.VehicleStatus','U') IS NULL
BEGIN
    CREATE TABLE dbo.VehicleStatus(
        Id INT NOT NULL PRIMARY KEY,
        EnName NVARCHAR(50) NOT NULL,
        ArName NVARCHAR(50) NULL
    );
END
GO

MERGE dbo.VehicleStatus AS t
USING (VALUES
    (1,N'InStock',N'متاح'),
    (2,N'Reserved',N'محجوز'),
    (3,N'Sold',N'مباع'),
    (4,N'Returned',N'مرتجع'),
    (5,N'Cancelled',N'ملغي')
) s(Id,EnName,ArName)
ON t.Id = s.Id
WHEN MATCHED THEN UPDATE SET EnName=s.EnName, ArName=s.ArName
WHEN NOT MATCHED THEN INSERT(Id,EnName,ArName) VALUES(s.Id,s.EnName,s.ArName);
GO

IF OBJECT_ID('dbo.VehicleStock','U') IS NULL
BEGIN
    CREATE TABLE dbo.VehicleStock(
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_VehicleStock PRIMARY KEY,
        ItemId INT NOT NULL,
        ChassisNo NVARCHAR(100) NOT NULL,
        EngineNo NVARCHAR(100) NULL,
        CarTypeId INT NULL,
        CarModelId INT NULL,
        CarColorId INT NULL,
        ManufacturingYear INT NULL,
        PlateNo NVARCHAR(50) NULL,
        VehicleNotes NVARCHAR(500) NULL,
        PurchaseInvoiceId INT NULL,
        PurchaseInvoiceDetailId INT NULL,
        PurchaseDate DATETIME NULL,
        PurchaseCost MONEY NULL,
        SalesInvoiceId INT NULL,
        SalesInvoiceDetailId INT NULL,
        SalesDate DATETIME NULL,
        SalePrice MONEY NULL,
        WarehouseId INT NULL,
        BranchId INT NULL,
        VehicleStatusId INT NOT NULL CONSTRAINT DF_VehicleStock_VehicleStatus DEFAULT(1),
        IsDeleted BIT NOT NULL CONSTRAINT DF_VehicleStock_IsDeleted DEFAULT(0),
        UserId INT NULL,
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_VehicleStock_CreatedDate DEFAULT(GETDATE()),
        UpdatedDate DATETIME NULL
    );
END
GO

IF COL_LENGTH('dbo.SalesInvoiceDetails', 'VehicleStockId') IS NULL
    ALTER TABLE dbo.SalesInvoiceDetails ADD VehicleStockId INT NULL;
GO
IF COL_LENGTH('dbo.SalesQuotationDetails', 'VehicleStockId') IS NULL
    ALTER TABLE dbo.SalesQuotationDetails ADD VehicleStockId INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_VehicleStock_ChassisNo_Active' AND object_id = OBJECT_ID('dbo.VehicleStock'))
    CREATE UNIQUE INDEX UX_VehicleStock_ChassisNo_Active ON dbo.VehicleStock(ChassisNo) WHERE IsDeleted = 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_VehicleStock_Item_Status' AND object_id = OBJECT_ID('dbo.VehicleStock'))
    CREATE INDEX IX_VehicleStock_Item_Status ON dbo.VehicleStock(ItemId, VehicleStatusId, IsDeleted);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_VehicleStock_Items')
    ALTER TABLE dbo.VehicleStock WITH NOCHECK ADD CONSTRAINT FK_VehicleStock_Items FOREIGN KEY(ItemId) REFERENCES dbo.Item(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_VehicleStock_CarType')
    ALTER TABLE dbo.VehicleStock WITH NOCHECK ADD CONSTRAINT FK_VehicleStock_CarType FOREIGN KEY(CarTypeId) REFERENCES dbo.CarType(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_VehicleStock_CarModel')
    ALTER TABLE dbo.VehicleStock WITH NOCHECK ADD CONSTRAINT FK_VehicleStock_CarModel FOREIGN KEY(CarModelId) REFERENCES dbo.CarModel(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_VehicleStock_CarColor')
    ALTER TABLE dbo.VehicleStock WITH NOCHECK ADD CONSTRAINT FK_VehicleStock_CarColor FOREIGN KEY(CarColorId) REFERENCES dbo.CarColor(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_VehicleStock_VehicleStatus')
    ALTER TABLE dbo.VehicleStock WITH NOCHECK ADD CONSTRAINT FK_VehicleStock_VehicleStatus FOREIGN KEY(VehicleStatusId) REFERENCES dbo.VehicleStatus(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_VehicleStock_PurchaseInvoice')
    ALTER TABLE dbo.VehicleStock WITH NOCHECK ADD CONSTRAINT FK_VehicleStock_PurchaseInvoice FOREIGN KEY(PurchaseInvoiceId) REFERENCES dbo.PurchaseInvoice(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_VehicleStock_SalesInvoice')
    ALTER TABLE dbo.VehicleStock WITH NOCHECK ADD CONSTRAINT FK_VehicleStock_SalesInvoice FOREIGN KEY(SalesInvoiceId) REFERENCES dbo.SalesInvoice(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_VehicleStock_PurchaseInvoiceDetails')
    ALTER TABLE dbo.VehicleStock WITH NOCHECK ADD CONSTRAINT FK_VehicleStock_PurchaseInvoiceDetails FOREIGN KEY(PurchaseInvoiceDetailId) REFERENCES dbo.PurchaseInvoiceDetails(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_VehicleStock_SalesInvoiceDetails')
    ALTER TABLE dbo.VehicleStock WITH NOCHECK ADD CONSTRAINT FK_VehicleStock_SalesInvoiceDetails FOREIGN KEY(SalesInvoiceDetailId) REFERENCES dbo.SalesInvoiceDetails(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_VehicleStock_Warehouse')
    ALTER TABLE dbo.VehicleStock WITH NOCHECK ADD CONSTRAINT FK_VehicleStock_Warehouse FOREIGN KEY(WarehouseId) REFERENCES dbo.Warehouse(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_VehicleStock_Branch')
    ALTER TABLE dbo.VehicleStock WITH NOCHECK ADD CONSTRAINT FK_VehicleStock_Branch FOREIGN KEY(BranchId) REFERENCES dbo.Branch(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_SalesInvoiceDetails_VehicleStock')
    ALTER TABLE dbo.SalesInvoiceDetails WITH NOCHECK ADD CONSTRAINT FK_SalesInvoiceDetails_VehicleStock FOREIGN KEY(VehicleStockId) REFERENCES dbo.VehicleStock(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_SalesQuotationDetails_VehicleStock')
    ALTER TABLE dbo.SalesQuotationDetails WITH NOCHECK ADD CONSTRAINT FK_SalesQuotationDetails_VehicleStock FOREIGN KEY(VehicleStockId) REFERENCES dbo.VehicleStock(Id);
GO

CREATE OR ALTER VIEW dbo.vw_AvailableVehicleStock
AS
SELECT vs.Id, vs.ItemId, vs.ChassisNo, vs.EngineNo, vs.CarTypeId, vs.CarModelId, vs.CarColorId, vs.ManufacturingYear, vs.PlateNo, vs.VehicleNotes,
       vs.PurchaseInvoiceId, vs.PurchaseInvoiceDetailId, vs.PurchaseDate, vs.PurchaseCost, vs.WarehouseId, vs.BranchId,
       ct.ArName AS CarTypeName, cm.ArName AS CarModelName, cc.ArName AS CarColorName, w.ArName AS WarehouseName
FROM dbo.VehicleStock vs
LEFT JOIN dbo.CarType ct ON ct.Id = vs.CarTypeId
LEFT JOIN dbo.CarModel cm ON cm.Id = vs.CarModelId
LEFT JOIN dbo.CarColor cc ON cc.Id = vs.CarColorId
LEFT JOIN dbo.Warehouse w ON w.Id = vs.WarehouseId
WHERE vs.IsDeleted = 0 AND vs.VehicleStatusId = 1;
GO
