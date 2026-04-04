/* Vehicle tracking fields for PurchaseInvoiceDetails and SalesInvoiceDetails */

IF COL_LENGTH('dbo.PurchaseInvoiceDetails', 'CarTypeId') IS NULL
    ALTER TABLE dbo.PurchaseInvoiceDetails ADD CarTypeId INT NULL;
IF COL_LENGTH('dbo.PurchaseInvoiceDetails', 'CarModelId') IS NULL
    ALTER TABLE dbo.PurchaseInvoiceDetails ADD CarModelId INT NULL;
IF COL_LENGTH('dbo.PurchaseInvoiceDetails', 'CarColorId') IS NULL
    ALTER TABLE dbo.PurchaseInvoiceDetails ADD CarColorId INT NULL;
IF COL_LENGTH('dbo.PurchaseInvoiceDetails', 'ChassisNo') IS NULL
    ALTER TABLE dbo.PurchaseInvoiceDetails ADD ChassisNo NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.PurchaseInvoiceDetails', 'EngineNo') IS NULL
    ALTER TABLE dbo.PurchaseInvoiceDetails ADD EngineNo NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.PurchaseInvoiceDetails', 'ManufacturingYear') IS NULL
    ALTER TABLE dbo.PurchaseInvoiceDetails ADD ManufacturingYear INT NULL;
IF COL_LENGTH('dbo.PurchaseInvoiceDetails', 'PlateNo') IS NULL
    ALTER TABLE dbo.PurchaseInvoiceDetails ADD PlateNo NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.PurchaseInvoiceDetails', 'VehicleNotes') IS NULL
    ALTER TABLE dbo.PurchaseInvoiceDetails ADD VehicleNotes NVARCHAR(500) NULL;

IF COL_LENGTH('dbo.SalesInvoiceDetails', 'CarTypeId') IS NULL
    ALTER TABLE dbo.SalesInvoiceDetails ADD CarTypeId INT NULL;
IF COL_LENGTH('dbo.SalesInvoiceDetails', 'CarModelId') IS NULL
    ALTER TABLE dbo.SalesInvoiceDetails ADD CarModelId INT NULL;
IF COL_LENGTH('dbo.SalesInvoiceDetails', 'CarColorId') IS NULL
    ALTER TABLE dbo.SalesInvoiceDetails ADD CarColorId INT NULL;
IF COL_LENGTH('dbo.SalesInvoiceDetails', 'ChassisNo') IS NULL
    ALTER TABLE dbo.SalesInvoiceDetails ADD ChassisNo NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.SalesInvoiceDetails', 'EngineNo') IS NULL
    ALTER TABLE dbo.SalesInvoiceDetails ADD EngineNo NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.SalesInvoiceDetails', 'ManufacturingYear') IS NULL
    ALTER TABLE dbo.SalesInvoiceDetails ADD ManufacturingYear INT NULL;
IF COL_LENGTH('dbo.SalesInvoiceDetails', 'PlateNo') IS NULL
    ALTER TABLE dbo.SalesInvoiceDetails ADD PlateNo NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.SalesInvoiceDetails', 'VehicleNotes') IS NULL
    ALTER TABLE dbo.SalesInvoiceDetails ADD VehicleNotes NVARCHAR(500) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PurchaseInvoiceDetails_CarType')
    ALTER TABLE dbo.PurchaseInvoiceDetails WITH NOCHECK ADD CONSTRAINT FK_PurchaseInvoiceDetails_CarType FOREIGN KEY (CarTypeId) REFERENCES dbo.CarType(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PurchaseInvoiceDetails_CarModel')
    ALTER TABLE dbo.PurchaseInvoiceDetails WITH NOCHECK ADD CONSTRAINT FK_PurchaseInvoiceDetails_CarModel FOREIGN KEY (CarModelId) REFERENCES dbo.CarModel(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PurchaseInvoiceDetails_CarColor')
    ALTER TABLE dbo.PurchaseInvoiceDetails WITH NOCHECK ADD CONSTRAINT FK_PurchaseInvoiceDetails_CarColor FOREIGN KEY (CarColorId) REFERENCES dbo.CarColor(Id);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SalesInvoiceDetails_CarType')
    ALTER TABLE dbo.SalesInvoiceDetails WITH NOCHECK ADD CONSTRAINT FK_SalesInvoiceDetails_CarType FOREIGN KEY (CarTypeId) REFERENCES dbo.CarType(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SalesInvoiceDetails_CarModel')
    ALTER TABLE dbo.SalesInvoiceDetails WITH NOCHECK ADD CONSTRAINT FK_SalesInvoiceDetails_CarModel FOREIGN KEY (CarModelId) REFERENCES dbo.CarModel(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SalesInvoiceDetails_CarColor')
    ALTER TABLE dbo.SalesInvoiceDetails WITH NOCHECK ADD CONSTRAINT FK_SalesInvoiceDetails_CarColor FOREIGN KEY (CarColorId) REFERENCES dbo.CarColor(Id);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PurchaseInvoiceDetails_ChassisNo' AND object_id = OBJECT_ID('dbo.PurchaseInvoiceDetails'))
    CREATE NONCLUSTERED INDEX IX_PurchaseInvoiceDetails_ChassisNo ON dbo.PurchaseInvoiceDetails(ChassisNo) WHERE ChassisNo IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SalesInvoiceDetails_ChassisNo' AND object_id = OBJECT_ID('dbo.SalesInvoiceDetails'))
    CREATE NONCLUSTERED INDEX IX_SalesInvoiceDetails_ChassisNo ON dbo.SalesInvoiceDetails(ChassisNo) WHERE ChassisNo IS NOT NULL;

/* IMPORTANT:
   If PurchaseInvoice_Insert/Update and SalesInvoice_Insert/Update SPs parse detail XML with explicit field list,
   add the new XML nodes there as well (CarTypeId, CarModelId, CarColorId, ChassisNo, EngineNo, ManufacturingYear, PlateNo, VehicleNotes).
*/
