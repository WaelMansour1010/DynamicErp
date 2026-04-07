/* =========================================================
   Car vehicle reports (DevExpress) data objects + menu pages
   ========================================================= */

CREATE OR ALTER PROCEDURE dbo.CarCurrentStock_Get
    @FromDate DATETIME = NULL,
    @ToDate DATETIME = NULL,
    @WarehouseId INT = NULL,
    @CarTypeId INT = NULL,
    @CarModelId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        pid.ChassisNo,
        ISNULL(ct.ArName, '') AS CarType,
        ISNULL(cm.ArName, '') AS CarModel,
        ISNULL(cc.ArName, '') AS CarColor,
        pid.ManufacturingYear,
        pid.EngineNo,
        pid.PlateNo,
        v.ArName AS VendorName,
        pi.DocumentNumber AS PurchaseInvoiceNumber,
        pi.VoucherDate AS PurchaseDate,
        ISNULL(pid.CostPrice, pid.Price) AS PurchaseCost,
        w.ArName AS WarehouseName,
        pid.VehicleNotes
    FROM PurchaseInvoiceDetails pid
    INNER JOIN PurchaseInvoice pi ON pi.Id = pid.MainDocId AND pi.IsDeleted = 0
    LEFT JOIN SalesInvoiceDetails sid ON sid.ChassisNo = pid.ChassisNo AND sid.IsDeleted = 0
    LEFT JOIN CarType ct ON ct.Id = pid.CarTypeId
    LEFT JOIN CarModel cm ON cm.Id = pid.CarModelId
    LEFT JOIN CarColor cc ON cc.Id = pid.CarColorId
    LEFT JOIN Vendor v ON v.Id = pi.VendorOrCustomerId
    LEFT JOIN Warehouse w ON w.Id = pi.WarehouseId
    WHERE pid.IsDeleted = 0
      AND ISNULL(pid.ChassisNo, '') <> ''
      AND sid.Id IS NULL
      AND (@FromDate IS NULL OR pi.VoucherDate >= @FromDate)
      AND (@ToDate IS NULL OR pi.VoucherDate < DATEADD(DAY, 1, @ToDate))
      AND (@WarehouseId IS NULL OR pi.WarehouseId = @WarehouseId)
      AND (@CarTypeId IS NULL OR pid.CarTypeId = @CarTypeId)
      AND (@CarModelId IS NULL OR pid.CarModelId = @CarModelId)
    ORDER BY pi.VoucherDate DESC, pid.Id DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.CarSalesHistory_Get
    @FromDate DATETIME = NULL,
    @ToDate DATETIME = NULL,
    @CarTypeId INT = NULL,
    @CarModelId INT = NULL,
    @CustomerId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Purch AS (
        SELECT pid.*, pi.DocumentNumber AS PurchaseInvoiceNumber, pi.VoucherDate AS PurchaseDate,
               ISNULL(pid.CostPrice, pid.Price) AS PurchaseCost
        FROM PurchaseInvoiceDetails pid
        INNER JOIN PurchaseInvoice pi ON pi.Id = pid.MainDocId AND pi.IsDeleted = 0
        WHERE pid.IsDeleted = 0 AND ISNULL(pid.ChassisNo, '') <> ''
    )
    SELECT
        sid.ChassisNo,
        ISNULL(ct.ArName, '') AS CarType,
        ISNULL(cm.ArName, '') AS CarModel,
        ISNULL(cc.ArName, '') AS CarColor,
        sid.ManufacturingYear,
        p.PurchaseInvoiceNumber,
        p.PurchaseDate,
        p.PurchaseCost,
        si.DocumentNumber AS SalesInvoiceNumber,
        si.VoucherDate AS SalesDate,
        sid.Price AS SalePrice,
        (sid.Price - ISNULL(p.PurchaseCost, 0)) AS GrossMargin,
        c.ArName AS CustomerName,
        sid.VehicleNotes
    FROM SalesInvoiceDetails sid
    INNER JOIN SalesInvoice si ON si.Id = sid.MainDocId AND si.IsDeleted = 0
    LEFT JOIN Purch p ON p.ChassisNo = sid.ChassisNo
    LEFT JOIN CarType ct ON ct.Id = sid.CarTypeId
    LEFT JOIN CarModel cm ON cm.Id = sid.CarModelId
    LEFT JOIN CarColor cc ON cc.Id = sid.CarColorId
    LEFT JOIN Customer c ON c.Id = si.VendorOrCustomerId
    WHERE sid.IsDeleted = 0
      AND ISNULL(sid.ChassisNo, '') <> ''
      AND (@FromDate IS NULL OR si.VoucherDate >= @FromDate)
      AND (@ToDate IS NULL OR si.VoucherDate < DATEADD(DAY, 1, @ToDate))
      AND (@CarTypeId IS NULL OR sid.CarTypeId = @CarTypeId)
      AND (@CarModelId IS NULL OR sid.CarModelId = @CarModelId)
      AND (@CustomerId IS NULL OR si.VendorOrCustomerId = @CustomerId)
    ORDER BY si.VoucherDate DESC, sid.Id DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.CarQuotationReport_Get
    @FromDate DATETIME = NULL,
    @ToDate DATETIME = NULL,
    @CustomerId INT = NULL,
    @CarTypeId INT = NULL,
    @CarModelId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        sq.DocumentNumber AS QuotationNumber,
        sq.VoucherDate AS QuotationDate,
        c.ArName AS CustomerName,
        sqd.ChassisNo,
        ISNULL(ct.ArName, '') AS CarType,
        ISNULL(cm.ArName, '') AS CarModel,
        ISNULL(cc.ArName, '') AS CarColor,
        sqd.ManufacturingYear,
        sqd.EngineNo,
        sqd.PlateNo,
        sqd.Price,
        sqd.VehicleNotes
    FROM SalesQuotationDetails sqd
    INNER JOIN SalesQuotation sq ON sq.Id = sqd.MainDocId AND sq.IsDeleted = 0
    LEFT JOIN Customer c ON c.Id = sq.VendorOrCustomerId
    LEFT JOIN CarType ct ON ct.Id = sqd.CarTypeId
    LEFT JOIN CarModel cm ON cm.Id = sqd.CarModelId
    LEFT JOIN CarColor cc ON cc.Id = sqd.CarColorId
    WHERE sqd.IsDeleted = 0
      AND ISNULL(sqd.ChassisNo, '') <> ''
      AND (@FromDate IS NULL OR sq.VoucherDate >= @FromDate)
      AND (@ToDate IS NULL OR sq.VoucherDate < DATEADD(DAY, 1, @ToDate))
      AND (@CustomerId IS NULL OR sq.VendorOrCustomerId = @CustomerId)
      AND (@CarTypeId IS NULL OR sqd.CarTypeId = @CarTypeId)
      AND (@CarModelId IS NULL OR sqd.CarModelId = @CarModelId)
    ORDER BY sq.VoucherDate DESC, sqd.Id DESC;
END
GO

/* Menu integration (SystemPage entries under same parent as CarEntrance report) */
DECLARE @ReportsParentId INT = (SELECT TOP 1 ParentId FROM SystemPage WHERE EnName = 'CarEntrance');
IF @ReportsParentId IS NULL SET @ReportsParentId = 3185;

IF NOT EXISTS(SELECT 1 FROM SystemPage WHERE EnName = 'CarCurrentStock')
INSERT INTO SystemPage (Code, ArName, EnName, IsMasterFile, TableName, ControllerName, IsTransaction, IsUpdated, IsReport, ParentId, IsActive, IsDeleted, IsModule, PageCode, ShowInReportPage)
VALUES ('CarCurrentStock', N'مخزون السيارات الحالي', 'CarCurrentStock', 0, 'PurchaseInvoiceDetails', 'Report', 0, 0, 1, @ReportsParentId, 1, 0, 0, 'RPT_CarCurrentStock', 1);

IF NOT EXISTS(SELECT 1 FROM SystemPage WHERE EnName = 'CarSalesHistory')
INSERT INTO SystemPage (Code, ArName, EnName, IsMasterFile, TableName, ControllerName, IsTransaction, IsUpdated, IsReport, ParentId, IsActive, IsDeleted, IsModule, PageCode, ShowInReportPage)
VALUES ('CarSalesHistory', N'تاريخ مبيعات السيارات', 'CarSalesHistory', 0, 'SalesInvoiceDetails', 'Report', 0, 0, 1, @ReportsParentId, 1, 0, 0, 'RPT_CarSalesHistory', 1);

IF NOT EXISTS(SELECT 1 FROM SystemPage WHERE EnName = 'CarQuotationReport')
INSERT INTO SystemPage (Code, ArName, EnName, IsMasterFile, TableName, ControllerName, IsTransaction, IsUpdated, IsReport, ParentId, IsActive, IsDeleted, IsModule, PageCode, ShowInReportPage)
VALUES ('CarQuotationReport', N'تقرير عروض أسعار السيارات', 'CarQuotationReport', 0, 'SalesQuotationDetails', 'Report', 0, 0, 1, @ReportsParentId, 1, 0, 0, 'RPT_CarQuotationReport', 1);

IF NOT EXISTS(SELECT 1 FROM SystemPage WHERE EnName = 'VehicleSalesDashboard')
INSERT INTO SystemPage (Code, ArName, EnName, IsMasterFile, TableName, ControllerName, IsTransaction, IsUpdated, IsReport, ParentId, IsActive, IsDeleted, IsModule, PageCode, ShowInReportPage)
VALUES ('VehicleSalesDashboard', N'لوحة تحكم مبيعات السيارات', 'VehicleSalesDashboard', 0, 'VehicleStock', 'Report', 0, 0, 1, @ReportsParentId, 1, 0, 0, 'RPT_VehicleSalesDashboard', 1);
