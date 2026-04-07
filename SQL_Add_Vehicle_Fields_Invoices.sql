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

IF COL_LENGTH('dbo.SalesQuotationDetails', 'CarTypeId') IS NULL
    ALTER TABLE dbo.SalesQuotationDetails ADD CarTypeId INT NULL;
IF COL_LENGTH('dbo.SalesQuotationDetails', 'CarModelId') IS NULL
    ALTER TABLE dbo.SalesQuotationDetails ADD CarModelId INT NULL;
IF COL_LENGTH('dbo.SalesQuotationDetails', 'CarColorId') IS NULL
    ALTER TABLE dbo.SalesQuotationDetails ADD CarColorId INT NULL;
IF COL_LENGTH('dbo.SalesQuotationDetails', 'ChassisNo') IS NULL
    ALTER TABLE dbo.SalesQuotationDetails ADD ChassisNo NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.SalesQuotationDetails', 'EngineNo') IS NULL
    ALTER TABLE dbo.SalesQuotationDetails ADD EngineNo NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.SalesQuotationDetails', 'ManufacturingYear') IS NULL
    ALTER TABLE dbo.SalesQuotationDetails ADD ManufacturingYear INT NULL;
IF COL_LENGTH('dbo.SalesQuotationDetails', 'PlateNo') IS NULL
    ALTER TABLE dbo.SalesQuotationDetails ADD PlateNo NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.SalesQuotationDetails', 'VehicleNotes') IS NULL
    ALTER TABLE dbo.SalesQuotationDetails ADD VehicleNotes NVARCHAR(500) NULL;

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

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SalesQuotationDetails_CarType')
    ALTER TABLE dbo.SalesQuotationDetails WITH NOCHECK ADD CONSTRAINT FK_SalesQuotationDetails_CarType FOREIGN KEY (CarTypeId) REFERENCES dbo.CarType(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SalesQuotationDetails_CarModel')
    ALTER TABLE dbo.SalesQuotationDetails WITH NOCHECK ADD CONSTRAINT FK_SalesQuotationDetails_CarModel FOREIGN KEY (CarModelId) REFERENCES dbo.CarModel(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SalesQuotationDetails_CarColor')
    ALTER TABLE dbo.SalesQuotationDetails WITH NOCHECK ADD CONSTRAINT FK_SalesQuotationDetails_CarColor FOREIGN KEY (CarColorId) REFERENCES dbo.CarColor(Id);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PurchaseInvoiceDetails_ChassisNo' AND object_id = OBJECT_ID('dbo.PurchaseInvoiceDetails'))
    CREATE NONCLUSTERED INDEX IX_PurchaseInvoiceDetails_ChassisNo ON dbo.PurchaseInvoiceDetails(ChassisNo) WHERE ChassisNo IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SalesInvoiceDetails_ChassisNo' AND object_id = OBJECT_ID('dbo.SalesInvoiceDetails'))
    CREATE NONCLUSTERED INDEX IX_SalesInvoiceDetails_ChassisNo ON dbo.SalesInvoiceDetails(ChassisNo) WHERE ChassisNo IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SalesQuotationDetails_ChassisNo' AND object_id = OBJECT_ID('dbo.SalesQuotationDetails'))
    CREATE NONCLUSTERED INDEX IX_SalesQuotationDetails_ChassisNo ON dbo.SalesQuotationDetails(ChassisNo) WHERE ChassisNo IS NOT NULL;

/* IMPORTANT:
   Extend XML + OPENXML parsing inside the existing invoice procedures without redesigning procedure logic.

   1) PurchaseInvoice_Insert / PurchaseInvoice_Update
      - In OPENXML(@Details, '/DocumentElement/Details', 2) WITH (...) mapping, include:
          CarTypeId INT,
          CarModelId INT,
          CarColorId INT,
          ChassisNo NVARCHAR(100),
          EngineNo NVARCHAR(100),
          ManufacturingYear INT,
          PlateNo NVARCHAR(50),
          VehicleNotes NVARCHAR(500)
      - Ensure INSERT/UPDATE into PurchaseInvoiceDetails writes these columns.

   2) SalesInvoice_Insert / SalesInvoice_Update
      - Extend OPENXML details mapping with same vehicle columns plus:
          VehicleStockId INT
      - Ensure INSERT/UPDATE into SalesInvoiceDetails writes VehicleStockId and vehicle columns.

   3) SalesQuotation_Insert / SalesQuotation_Update
      - Extend OPENXML details mapping with same vehicle columns plus:
          VehicleStockId INT
      - Ensure INSERT/UPDATE into SalesQuotationDetails writes VehicleStockId and vehicle columns.

   4) Data type safety
      - ManufacturingYear must stay INT in OPENXML WITH clause and target INSERT/UPDATE.
      - Do not cast to decimal/float.

   5) Backward compatibility
      - Keep all existing accounting/stock/journal logic unchanged.
      - Only extend detail XML parsing + detail column persistence.
*/


/* =========================================================
   PurchaseInvoice_Update (XML + OPENXML extension patch)
   Place the following two changes INSIDE existing proc body:
   A) In INSERT..SELECT FROM OPENXML(@DetailsOut...) normalize vehicle values
   B) After inserting PurchaseInvoiceDetails, sync VehicleStock rows to InStock
   =========================================================

   -- A) Replace vehicle projection in SELECT with normalized values:
   CarTypeId = NULLIF(CarTypeId, 0),
   CarModelId = NULLIF(CarModelId, 0),
   CarColorId = NULLIF(CarColorId, 0),
   ChassisNo = NULLIF(LTRIM(RTRIM(ChassisNo)), ''),
   EngineNo = NULLIF(LTRIM(RTRIM(EngineNo)), ''),
   ManufacturingYear = NULLIF(ManufacturingYear, 0),
   PlateNo = NULLIF(LTRIM(RTRIM(PlateNo)), ''),
   VehicleNotes = NULLIF(LTRIM(RTRIM(VehicleNotes)), '')

   -- B) Add this block after PurchaseInvoiceDetails insert and before journal/stock vouchers:
   IF OBJECT_ID('dbo.VehicleStock','U') IS NOT NULL
   BEGIN
       ;WITH src AS (
           SELECT d.Id AS PurchaseInvoiceDetailId,
                  d.ItemId,
                  d.ChassisNo,
                  d.EngineNo,
                  d.CarTypeId,
                  d.CarModelId,
                  d.CarColorId,
                  d.ManufacturingYear,
                  d.PlateNo,
                  d.VehicleNotes,
                  d.CostPrice
           FROM dbo.PurchaseInvoiceDetails d
           WHERE d.MainDocId = @Id
             AND d.IsDeleted = 0
             AND d.ChassisNo IS NOT NULL
             AND LTRIM(RTRIM(d.ChassisNo)) <> ''
       )
       MERGE dbo.VehicleStock AS tgt
       USING src
          ON tgt.IsDeleted = 0
         AND LOWER(LTRIM(RTRIM(tgt.ChassisNo))) = LOWER(LTRIM(RTRIM(src.ChassisNo)))
       WHEN MATCHED THEN
         UPDATE SET
             tgt.ItemId = src.ItemId,
             tgt.EngineNo = src.EngineNo,
             tgt.CarTypeId = src.CarTypeId,
             tgt.CarModelId = src.CarModelId,
             tgt.CarColorId = src.CarColorId,
             tgt.ManufacturingYear = src.ManufacturingYear,
             tgt.PlateNo = src.PlateNo,
             tgt.VehicleNotes = src.VehicleNotes,
             tgt.PurchaseInvoiceId = @Id,
             tgt.PurchaseInvoiceDetailId = src.PurchaseInvoiceDetailId,
             tgt.PurchaseDate = @VoucherDate,
             tgt.PurchaseCost = src.CostPrice,
             tgt.WarehouseId = @WarehouseId,
             tgt.BranchId = @BranchId,
             tgt.VehicleStatusId = 1,
             tgt.SalesInvoiceId = NULL,
             tgt.SalesInvoiceDetailId = NULL,
             tgt.SalesDate = NULL,
             tgt.SalePrice = NULL,
             tgt.UpdatedDate = GETDATE()
       WHEN NOT MATCHED BY TARGET THEN
         INSERT (ItemId, ChassisNo, EngineNo, CarTypeId, CarModelId, CarColorId, ManufacturingYear, PlateNo, VehicleNotes,
                 PurchaseInvoiceId, PurchaseInvoiceDetailId, PurchaseDate, PurchaseCost, WarehouseId, BranchId,
                 VehicleStatusId, IsDeleted, UserId, CreatedDate, UpdatedDate)
         VALUES (src.ItemId, src.ChassisNo, src.EngineNo, src.CarTypeId, src.CarModelId, src.CarColorId, src.ManufacturingYear,
                 src.PlateNo, src.VehicleNotes, @Id, src.PurchaseInvoiceDetailId, @VoucherDate, src.CostPrice, @WarehouseId,
                 @BranchId, 1, 0, @UserId, GETDATE(), GETDATE());
   END
*/


/* =========================================================
   SalesInvoice_Update / SalesInvoice_Insert patch (inside existing body)
   =========================================================
   1) Extend OPENXML(@Details...) WITH (...) by adding:
      CarTypeId INT, CarModelId INT, CarColorId INT,
      ChassisNo NVARCHAR(100), EngineNo NVARCHAR(100),
      ManufacturingYear INT, PlateNo NVARCHAR(50), VehicleNotes NVARCHAR(500),
      VehicleStockId INT

   2) Normalize values in SELECT projection:
      CarTypeId = NULLIF(CarTypeId,0), CarModelId = NULLIF(CarModelId,0), CarColorId = NULLIF(CarColorId,0),
      ChassisNo = NULLIF(LTRIM(RTRIM(ChassisNo)),''), EngineNo = NULLIF(LTRIM(RTRIM(EngineNo)),''),
      ManufacturingYear = NULLIF(ManufacturingYear,0), PlateNo = NULLIF(LTRIM(RTRIM(PlateNo)),''),
      VehicleNotes = NULLIF(LTRIM(RTRIM(VehicleNotes)),''), VehicleStockId = NULLIF(VehicleStockId,0)

   3) After SalesInvoiceDetails insert/update, sync VehicleStock to Sold:
      UPDATE vs
         SET vs.VehicleStatusId = 3,
             vs.SalesInvoiceId = @Id,
             vs.SalesInvoiceDetailId = sid.Id,
             vs.SalesDate = @VoucherDate,
             vs.SalePrice = sid.Price,
             vs.UpdatedDate = GETDATE()
      FROM dbo.VehicleStock vs
      INNER JOIN dbo.SalesInvoiceDetails sid ON sid.VehicleStockId = vs.Id
      WHERE sid.MainDocId = @Id AND sid.IsDeleted = 0 AND vs.IsDeleted = 0;
*/

/* =========================================================
   SalesQuotation_Insert (XML + OPENXML extension patch)
   Apply inside existing proc body exactly where @Details XML is parsed.
   =========================================================

   -- 1) Extend INSERT INTO dbo.SalesQuotationDetails column list by adding vehicle columns:
   ..., [Area],
      CarTypeId,
      CarModelId,
      CarColorId,
      ChassisNo,
      EngineNo,
      ManufacturingYear,
      PlateNo,
      VehicleNotes,
      VehicleStockId

   -- 2) Extend SELECT projection from OPENXML with normalized values:
   CarTypeId = NULLIF(CarTypeId, 0),
   CarModelId = NULLIF(CarModelId, 0),
   CarColorId = NULLIF(CarColorId, 0),
   ChassisNo = NULLIF(LTRIM(RTRIM(ChassisNo)), ''),
   EngineNo = NULLIF(LTRIM(RTRIM(EngineNo)), ''),
   ManufacturingYear = NULLIF(ManufacturingYear, 0),
   PlateNo = NULLIF(LTRIM(RTRIM(PlateNo)), ''),
   VehicleNotes = NULLIF(LTRIM(RTRIM(VehicleNotes)), ''),
   VehicleStockId = NULLIF(VehicleStockId, 0)

   -- 3) Extend OPENXML WITH (...) mapping by adding:
   CarTypeId INT,
   CarModelId INT,
   CarColorId INT,
   ChassisNo NVARCHAR(100),
   EngineNo NVARCHAR(100),
   ManufacturingYear INT,
   PlateNo NVARCHAR(50),
   VehicleNotes NVARCHAR(500),
   VehicleStockId INT
*/

/* =========================================================
   SalesQuotation_Update (XML + OPENXML extension patch)
   Apply inside existing proc body exactly where @Details XML is parsed.
   =========================================================

   -- 1) Extend INSERT INTO dbo.SalesQuotationDetails column list by adding vehicle columns:
   ..., [Area],
      CarTypeId,
      CarModelId,
      CarColorId,
      ChassisNo,
      EngineNo,
      ManufacturingYear,
      PlateNo,
      VehicleNotes,
      VehicleStockId

   -- 2) Extend SELECT projection from OPENXML with normalized values:
   CarTypeId = NULLIF(CarTypeId, 0),
   CarModelId = NULLIF(CarModelId, 0),
   CarColorId = NULLIF(CarColorId, 0),
   ChassisNo = NULLIF(LTRIM(RTRIM(ChassisNo)), ''),
   EngineNo = NULLIF(LTRIM(RTRIM(EngineNo)), ''),
   ManufacturingYear = NULLIF(ManufacturingYear, 0),
   PlateNo = NULLIF(LTRIM(RTRIM(PlateNo)), ''),
   VehicleNotes = NULLIF(LTRIM(RTRIM(VehicleNotes)), ''),
   VehicleStockId = NULLIF(VehicleStockId, 0)

   -- 3) Extend OPENXML WITH (...) mapping by adding:
   CarTypeId INT,
   CarModelId INT,
   CarColorId INT,
   ChassisNo NVARCHAR(100),
   EngineNo NVARCHAR(100),
   ManufacturingYear INT,
   PlateNo NVARCHAR(50),
   VehicleNotes NVARCHAR(500),
   VehicleStockId INT

   -- 4) Keep all existing posting/accounting/stock logic unchanged
*/


/* =========================================================
   PurchaseInvoice_Insert (XML + OPENXML extension patch)
   Place the following changes INSIDE existing proc body:
   =========================================================

   -- A) In INSERT..SELECT FROM OPENXML(@DetailsOut...) normalize vehicle values:
   CarTypeId = NULLIF(CarTypeId, 0),
   CarModelId = NULLIF(CarModelId, 0),
   CarColorId = NULLIF(CarColorId, 0),
   ChassisNo = NULLIF(LTRIM(RTRIM(ChassisNo)), ''),
   EngineNo = NULLIF(LTRIM(RTRIM(EngineNo)), ''),
   ManufacturingYear = NULLIF(ManufacturingYear, 0),
   PlateNo = NULLIF(LTRIM(RTRIM(PlateNo)), ''),
   VehicleNotes = NULLIF(LTRIM(RTRIM(VehicleNotes)), '')

   -- B) Add this block after PurchaseInvoiceDetails insert and before accounting/voucher logic:
   IF OBJECT_ID('dbo.VehicleStock','U') IS NOT NULL
   BEGIN
       ;WITH src AS (
           SELECT d.Id AS PurchaseInvoiceDetailId,
                  d.ItemId,
                  d.ChassisNo,
                  d.EngineNo,
                  d.CarTypeId,
                  d.CarModelId,
                  d.CarColorId,
                  d.ManufacturingYear,
                  d.PlateNo,
                  d.VehicleNotes,
                  d.CostPrice
           FROM dbo.PurchaseInvoiceDetails d
           WHERE d.MainDocId = @Id
             AND d.IsDeleted = 0
             AND d.ChassisNo IS NOT NULL
             AND LTRIM(RTRIM(d.ChassisNo)) <> ''
       )
       MERGE dbo.VehicleStock AS tgt
       USING src
          ON tgt.IsDeleted = 0
         AND LOWER(LTRIM(RTRIM(tgt.ChassisNo))) = LOWER(LTRIM(RTRIM(src.ChassisNo)))
       WHEN MATCHED THEN
         UPDATE SET
             tgt.ItemId = src.ItemId,
             tgt.EngineNo = src.EngineNo,
             tgt.CarTypeId = src.CarTypeId,
             tgt.CarModelId = src.CarModelId,
             tgt.CarColorId = src.CarColorId,
             tgt.ManufacturingYear = src.ManufacturingYear,
             tgt.PlateNo = src.PlateNo,
             tgt.VehicleNotes = src.VehicleNotes,
             tgt.PurchaseInvoiceId = @Id,
             tgt.PurchaseInvoiceDetailId = src.PurchaseInvoiceDetailId,
             tgt.PurchaseDate = @VoucherDate,
             tgt.PurchaseCost = src.CostPrice,
             tgt.WarehouseId = @WarehouseId,
             tgt.BranchId = @BranchId,
             tgt.VehicleStatusId = 1,
             tgt.SalesInvoiceId = NULL,
             tgt.SalesInvoiceDetailId = NULL,
             tgt.SalesDate = NULL,
             tgt.SalePrice = NULL,
             tgt.UpdatedDate = GETDATE()
       WHEN NOT MATCHED BY TARGET THEN
         INSERT (ItemId, ChassisNo, EngineNo, CarTypeId, CarModelId, CarColorId, ManufacturingYear, PlateNo, VehicleNotes,
                 PurchaseInvoiceId, PurchaseInvoiceDetailId, PurchaseDate, PurchaseCost, WarehouseId, BranchId,
                 VehicleStatusId, IsDeleted, UserId, CreatedDate, UpdatedDate)
         VALUES (src.ItemId, src.ChassisNo, src.EngineNo, src.CarTypeId, src.CarModelId, src.CarColorId, src.ManufacturingYear,
                 src.PlateNo, src.VehicleNotes, @Id, src.PurchaseInvoiceDetailId, @VoucherDate, src.CostPrice, @WarehouseId,
                 @BranchId, 1, 0, @UserId, GETDATE(), GETDATE());
   END
*/


/* =========================================================
   SalesInvoice_Update (XML + OPENXML extension patch)
   Apply inside existing proc body exactly where @Details XML is parsed.
   =========================================================

   -- 1) Extend INSERT INTO dbo.SalesInvoiceDetails column list by adding VehicleStockId:
   ..., VehicleNotes,
      VehicleStockId

   -- 2) Extend SELECT projection from OPENXML with normalized values:
   CarTypeId = NULLIF(CarTypeId, 0),
   CarModelId = NULLIF(CarModelId, 0),
   CarColorId = NULLIF(CarColorId, 0),
   ChassisNo = NULLIF(LTRIM(RTRIM(ChassisNo)), ''),
   EngineNo = NULLIF(LTRIM(RTRIM(EngineNo)), ''),
   ManufacturingYear = NULLIF(ManufacturingYear, 0),
   PlateNo = NULLIF(LTRIM(RTRIM(PlateNo)), ''),
   VehicleNotes = NULLIF(LTRIM(RTRIM(VehicleNotes)), ''),
   VehicleStockId = NULLIF(VehicleStockId, 0)

   -- 3) Extend OPENXML WITH (...) mapping with VehicleStockId:
   ..., VehicleNotes NVARCHAR(500),
      VehicleStockId INT

   -- 4) Add VehicleStock lifecycle sync immediately after SalesInvoiceDetails insert:
   IF OBJECT_ID('dbo.VehicleStock','U') IS NOT NULL
   BEGIN
       -- release any previously linked stock rows for this invoice back to InStock (for update scenario)
       UPDATE vs
          SET vs.VehicleStatusId = 1,
              vs.SalesInvoiceId = NULL,
              vs.SalesInvoiceDetailId = NULL,
              vs.SalesDate = NULL,
              vs.SalePrice = NULL,
              vs.UpdatedDate = GETDATE()
       FROM dbo.VehicleStock vs
       WHERE vs.IsDeleted = 0
         AND vs.SalesInvoiceId = @Id;

       -- apply selected rows as Sold
       UPDATE vs
          SET vs.VehicleStatusId = 3,
              vs.SalesInvoiceId = @Id,
              vs.SalesInvoiceDetailId = sid.Id,
              vs.SalesDate = @VoucherDate,
              vs.SalePrice = sid.Price,
              vs.ItemId = sid.ItemId,
              vs.EngineNo = sid.EngineNo,
              vs.CarTypeId = sid.CarTypeId,
              vs.CarModelId = sid.CarModelId,
              vs.CarColorId = sid.CarColorId,
              vs.ManufacturingYear = sid.ManufacturingYear,
              vs.PlateNo = sid.PlateNo,
              vs.VehicleNotes = sid.VehicleNotes,
              vs.UpdatedDate = GETDATE()
       FROM dbo.VehicleStock vs
       INNER JOIN dbo.SalesInvoiceDetails sid
               ON sid.VehicleStockId = vs.Id
              AND sid.MainDocId = @Id
              AND sid.IsDeleted = 0
       WHERE vs.IsDeleted = 0;
   END
*/


/* =========================================================
   SalesInvoice_Insert (XML + OPENXML extension patch)
   Apply inside existing proc body exactly where @Details XML is parsed.
   =========================================================

   -- 1) Extend INSERT INTO dbo.SalesInvoiceDetails column list by adding VehicleStockId:
   ..., VehicleNotes,
      VehicleStockId

   -- 2) Extend SELECT projection from OPENXML with normalized values:
   CarTypeId = NULLIF(CarTypeId, 0),
   CarModelId = NULLIF(CarModelId, 0),
   CarColorId = NULLIF(CarColorId, 0),
   ChassisNo = NULLIF(LTRIM(RTRIM(ChassisNo)), ''),
   EngineNo = NULLIF(LTRIM(RTRIM(EngineNo)), ''),
   ManufacturingYear = NULLIF(ManufacturingYear, 0),
   PlateNo = NULLIF(LTRIM(RTRIM(PlateNo)), ''),
   VehicleNotes = NULLIF(LTRIM(RTRIM(VehicleNotes)), ''),
   VehicleStockId = NULLIF(VehicleStockId, 0)

   -- 3) Extend OPENXML WITH (...) mapping with VehicleStockId:
   ..., VehicleNotes NVARCHAR(500),
      VehicleStockId INT

   -- 4) Add VehicleStock lifecycle sync immediately after SalesInvoiceDetails insert:
   IF OBJECT_ID('dbo.VehicleStock','U') IS NOT NULL
   BEGIN
       UPDATE vs
          SET vs.VehicleStatusId = 3,
              vs.SalesInvoiceId = @Id,
              vs.SalesInvoiceDetailId = sid.Id,
              vs.SalesDate = @VoucherDate,
              vs.SalePrice = sid.Price,
              vs.ItemId = sid.ItemId,
              vs.EngineNo = sid.EngineNo,
              vs.CarTypeId = sid.CarTypeId,
              vs.CarModelId = sid.CarModelId,
              vs.CarColorId = sid.CarColorId,
              vs.ManufacturingYear = sid.ManufacturingYear,
              vs.PlateNo = sid.PlateNo,
              vs.VehicleNotes = sid.VehicleNotes,
              vs.UpdatedDate = GETDATE()
       FROM dbo.VehicleStock vs
       INNER JOIN dbo.SalesInvoiceDetails sid
               ON sid.VehicleStockId = vs.Id
              AND sid.MainDocId = @Id
              AND sid.IsDeleted = 0
       WHERE vs.IsDeleted = 0;
   END
*/
