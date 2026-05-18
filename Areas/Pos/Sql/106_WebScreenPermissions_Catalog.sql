/*
    Web screen permission catalog for POS and MainERP.
    SQL Server 2012 compatible.
    POS and MainERP are intentionally separated; there is no Shared area in the customer matrix.
    Legacy ScreenJuncUser and POS_UserPermissions are read only as seed sources.
*/

IF OBJECT_ID(N'dbo.WebModules', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WebModules
    (
        WebModuleId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WebModules PRIMARY KEY,
        ModuleKey NVARCHAR(80) NOT NULL,
        AreaName NVARCHAR(50) NOT NULL,
        ArabicCaption NVARCHAR(200) NOT NULL,
        EnglishCaption NVARCHAR(200) NULL,
        IconCss NVARCHAR(120) NULL,
        DisplayOrder INT NOT NULL CONSTRAINT DF_WebModules_DisplayOrder DEFAULT(0),
        IsActive BIT NOT NULL CONSTRAINT DF_WebModules_IsActive DEFAULT(1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_WebModules_CreatedAt DEFAULT(GETDATE()),
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_WebModules_UpdatedAt DEFAULT(GETDATE())
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.WebModules', N'AreaName') IS NULL
        ALTER TABLE dbo.WebModules ADD AreaName NVARCHAR(50) NULL;
    IF COL_LENGTH(N'dbo.WebModules', N'IconCss') IS NULL
        ALTER TABLE dbo.WebModules ADD IconCss NVARCHAR(120) NULL;
    IF COL_LENGTH(N'dbo.WebModules', N'DisplayOrder') IS NULL
        ALTER TABLE dbo.WebModules ADD DisplayOrder INT NOT NULL CONSTRAINT DF_WebModules_DisplayOrder_Legacy DEFAULT(0);
    IF COL_LENGTH(N'dbo.WebModules', N'IsActive') IS NULL
        ALTER TABLE dbo.WebModules ADD IsActive BIT NOT NULL CONSTRAINT DF_WebModules_IsActive_Legacy DEFAULT(1);
    IF COL_LENGTH(N'dbo.WebModules', N'CreatedAt') IS NULL
        ALTER TABLE dbo.WebModules ADD CreatedAt DATETIME NOT NULL CONSTRAINT DF_WebModules_CreatedAt_Legacy DEFAULT(GETDATE());
    IF COL_LENGTH(N'dbo.WebModules', N'UpdatedAt') IS NULL
        ALTER TABLE dbo.WebModules ADD UpdatedAt DATETIME NOT NULL CONSTRAINT DF_WebModules_UpdatedAt_Legacy DEFAULT(GETDATE());

    EXEC(N'
    UPDATE dbo.WebModules
       SET AreaName = CASE
            WHEN ModuleKey LIKE N''POS.%'' THEN N''POS''
            WHEN ModuleKey LIKE N''MainERP.%'' THEN N''MainERP''
            ELSE ISNULL(AreaName, N''MainERP'')
        END
     WHERE AreaName IS NULL;');
END

IF OBJECT_ID(N'dbo.WebScreens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WebScreens
    (
        WebScreenId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WebScreens PRIMARY KEY,
        WebModuleId INT NOT NULL,
        ScreenKey NVARCHAR(160) NOT NULL,
        ArabicCaption NVARCHAR(220) NOT NULL,
        EnglishCaption NVARCHAR(220) NULL,
        AreaName NVARCHAR(80) NULL,
        ControllerName NVARCHAR(120) NULL,
        ActionName NVARCHAR(120) NULL,
        RouteUrl NVARCHAR(300) NULL,
        IconCss NVARCHAR(120) NULL,
        DisplayOrder INT NOT NULL CONSTRAINT DF_WebScreens_DisplayOrder DEFAULT(0),
        IsActive BIT NOT NULL CONSTRAINT DF_WebScreens_IsActive DEFAULT(1),
        IsMenuVisible BIT NOT NULL CONSTRAINT DF_WebScreens_IsMenuVisible DEFAULT(1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_WebScreens_CreatedAt DEFAULT(GETDATE()),
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_WebScreens_UpdatedAt DEFAULT(GETDATE())
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.WebScreens', N'AreaName') IS NULL
        ALTER TABLE dbo.WebScreens ADD AreaName NVARCHAR(80) NULL;
    IF COL_LENGTH(N'dbo.WebScreens', N'ControllerName') IS NULL
        ALTER TABLE dbo.WebScreens ADD ControllerName NVARCHAR(120) NULL;
    IF COL_LENGTH(N'dbo.WebScreens', N'ActionName') IS NULL
        ALTER TABLE dbo.WebScreens ADD ActionName NVARCHAR(120) NULL;
    IF COL_LENGTH(N'dbo.WebScreens', N'RouteUrl') IS NULL
        ALTER TABLE dbo.WebScreens ADD RouteUrl NVARCHAR(300) NULL;
    IF COL_LENGTH(N'dbo.WebScreens', N'IconCss') IS NULL
        ALTER TABLE dbo.WebScreens ADD IconCss NVARCHAR(120) NULL;
    IF COL_LENGTH(N'dbo.WebScreens', N'DisplayOrder') IS NULL
        ALTER TABLE dbo.WebScreens ADD DisplayOrder INT NOT NULL CONSTRAINT DF_WebScreens_DisplayOrder_Legacy DEFAULT(0);
    IF COL_LENGTH(N'dbo.WebScreens', N'IsActive') IS NULL
        ALTER TABLE dbo.WebScreens ADD IsActive BIT NOT NULL CONSTRAINT DF_WebScreens_IsActive_Legacy DEFAULT(1);
    IF COL_LENGTH(N'dbo.WebScreens', N'IsMenuVisible') IS NULL
        ALTER TABLE dbo.WebScreens ADD IsMenuVisible BIT NOT NULL CONSTRAINT DF_WebScreens_IsMenuVisible_Legacy DEFAULT(1);
    IF COL_LENGTH(N'dbo.WebScreens', N'CreatedAt') IS NULL
        ALTER TABLE dbo.WebScreens ADD CreatedAt DATETIME NOT NULL CONSTRAINT DF_WebScreens_CreatedAt_Legacy DEFAULT(GETDATE());
    IF COL_LENGTH(N'dbo.WebScreens', N'UpdatedAt') IS NULL
        ALTER TABLE dbo.WebScreens ADD UpdatedAt DATETIME NOT NULL CONSTRAINT DF_WebScreens_UpdatedAt_Legacy DEFAULT(GETDATE());

    EXEC(N'
    UPDATE s
       SET AreaName = CASE
            WHEN s.ScreenKey LIKE N''POS.%'' THEN N''POS''
            WHEN s.ScreenKey LIKE N''MainERP.%'' THEN N''MainERP''
            WHEN m.AreaName IS NOT NULL THEN m.AreaName
            ELSE ISNULL(s.AreaName, N''MainERP'')
        END
      FROM dbo.WebScreens s
      LEFT JOIN dbo.WebModules m ON m.WebModuleId = s.WebModuleId
     WHERE s.AreaName IS NULL;');
END

IF OBJECT_ID(N'dbo.WebScreenPermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WebScreenPermissions
    (
        WebPermissionId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WebScreenPermissions PRIMARY KEY,
        UserId INT NOT NULL,
        WebScreenId INT NOT NULL,
        CanView BIT NOT NULL CONSTRAINT DF_WebScreenPermissions_CanView DEFAULT(0),
        CanAdd BIT NOT NULL CONSTRAINT DF_WebScreenPermissions_CanAdd DEFAULT(0),
        CanEdit BIT NOT NULL CONSTRAINT DF_WebScreenPermissions_CanEdit DEFAULT(0),
        CanDelete BIT NOT NULL CONSTRAINT DF_WebScreenPermissions_CanDelete DEFAULT(0),
        CanPrint BIT NOT NULL CONSTRAINT DF_WebScreenPermissions_CanPrint DEFAULT(0),
        CanExport BIT NOT NULL CONSTRAINT DF_WebScreenPermissions_CanExport DEFAULT(0),
        CanApprove BIT NOT NULL CONSTRAINT DF_WebScreenPermissions_CanApprove DEFAULT(0),
        SeedSource NVARCHAR(60) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_WebScreenPermissions_CreatedAt DEFAULT(GETDATE()),
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_WebScreenPermissions_UpdatedAt DEFAULT(GETDATE())
    );
END
ELSE IF COL_LENGTH(N'dbo.WebScreenPermissions', N'SeedSource') IS NULL
BEGIN
    ALTER TABLE dbo.WebScreenPermissions ADD SeedSource NVARCHAR(60) NULL;
END

IF OBJECT_ID(N'dbo.WebPermissionRoleTemplates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WebPermissionRoleTemplates
    (
        TemplateId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WebPermissionRoleTemplates PRIMARY KEY,
        TemplateKey NVARCHAR(120) NOT NULL,
        AreaName NVARCHAR(50) NOT NULL,
        ArabicCaption NVARCHAR(200) NOT NULL,
        EnglishCaption NVARCHAR(200) NULL,
        DisplayOrder INT NOT NULL CONSTRAINT DF_WebPermissionRoleTemplates_DisplayOrder DEFAULT(0),
        IsActive BIT NOT NULL CONSTRAINT DF_WebPermissionRoleTemplates_IsActive DEFAULT(1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_WebPermissionRoleTemplates_CreatedAt DEFAULT(GETDATE()),
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_WebPermissionRoleTemplates_UpdatedAt DEFAULT(GETDATE())
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.WebPermissionRoleTemplates', N'AreaName') IS NULL
        ALTER TABLE dbo.WebPermissionRoleTemplates ADD AreaName NVARCHAR(50) NULL;
    IF COL_LENGTH(N'dbo.WebPermissionRoleTemplates', N'DisplayOrder') IS NULL
        ALTER TABLE dbo.WebPermissionRoleTemplates ADD DisplayOrder INT NOT NULL CONSTRAINT DF_WebPermissionRoleTemplates_DisplayOrder_Legacy DEFAULT(0);
    IF COL_LENGTH(N'dbo.WebPermissionRoleTemplates', N'IsActive') IS NULL
        ALTER TABLE dbo.WebPermissionRoleTemplates ADD IsActive BIT NOT NULL CONSTRAINT DF_WebPermissionRoleTemplates_IsActive_Legacy DEFAULT(1);
    IF COL_LENGTH(N'dbo.WebPermissionRoleTemplates', N'CreatedAt') IS NULL
        ALTER TABLE dbo.WebPermissionRoleTemplates ADD CreatedAt DATETIME NOT NULL CONSTRAINT DF_WebPermissionRoleTemplates_CreatedAt_Legacy DEFAULT(GETDATE());
    IF COL_LENGTH(N'dbo.WebPermissionRoleTemplates', N'UpdatedAt') IS NULL
        ALTER TABLE dbo.WebPermissionRoleTemplates ADD UpdatedAt DATETIME NOT NULL CONSTRAINT DF_WebPermissionRoleTemplates_UpdatedAt_Legacy DEFAULT(GETDATE());
END

IF OBJECT_ID(N'dbo.WebPermissionRoleTemplateItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WebPermissionRoleTemplateItems
    (
        TemplateItemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WebPermissionRoleTemplateItems PRIMARY KEY,
        TemplateId INT NOT NULL,
        WebScreenId INT NOT NULL,
        CanView BIT NOT NULL CONSTRAINT DF_WebPermissionRoleTemplateItems_CanView DEFAULT(0),
        CanAdd BIT NOT NULL CONSTRAINT DF_WebPermissionRoleTemplateItems_CanAdd DEFAULT(0),
        CanEdit BIT NOT NULL CONSTRAINT DF_WebPermissionRoleTemplateItems_CanEdit DEFAULT(0),
        CanDelete BIT NOT NULL CONSTRAINT DF_WebPermissionRoleTemplateItems_CanDelete DEFAULT(0),
        CanPrint BIT NOT NULL CONSTRAINT DF_WebPermissionRoleTemplateItems_CanPrint DEFAULT(0),
        CanExport BIT NOT NULL CONSTRAINT DF_WebPermissionRoleTemplateItems_CanExport DEFAULT(0),
        CanApprove BIT NOT NULL CONSTRAINT DF_WebPermissionRoleTemplateItems_CanApprove DEFAULT(0)
    );
END

IF OBJECT_ID(N'dbo.WebScreenLegacyMap', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WebScreenLegacyMap
    (
        LegacyMapId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WebScreenLegacyMap PRIMARY KEY,
        LegacyScreenName NVARCHAR(255) NOT NULL,
        ScreenKey NVARCHAR(160) NOT NULL,
        Notes NVARCHAR(400) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_WebScreenLegacyMap_IsActive DEFAULT(1)
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_WebModules_ModuleKey' AND object_id = OBJECT_ID(N'dbo.WebModules'))
    CREATE UNIQUE INDEX UX_WebModules_ModuleKey ON dbo.WebModules(ModuleKey);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_WebModules_Area' AND object_id = OBJECT_ID(N'dbo.WebModules'))
    CREATE INDEX IX_WebModules_Area ON dbo.WebModules(AreaName, IsActive, DisplayOrder);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_WebScreens_ScreenKey' AND object_id = OBJECT_ID(N'dbo.WebScreens'))
    CREATE UNIQUE INDEX UX_WebScreens_ScreenKey ON dbo.WebScreens(ScreenKey);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_WebScreens_Module' AND object_id = OBJECT_ID(N'dbo.WebScreens'))
    CREATE INDEX IX_WebScreens_Module ON dbo.WebScreens(WebModuleId, IsActive, IsMenuVisible, DisplayOrder);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_WebScreens_Route' AND object_id = OBJECT_ID(N'dbo.WebScreens'))
    CREATE INDEX IX_WebScreens_Route ON dbo.WebScreens(AreaName, ControllerName, ActionName);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_WebScreenPermissions_UserScreen' AND object_id = OBJECT_ID(N'dbo.WebScreenPermissions'))
    CREATE UNIQUE INDEX UX_WebScreenPermissions_UserScreen ON dbo.WebScreenPermissions(UserId, WebScreenId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_WebScreenPermissions_Screen' AND object_id = OBJECT_ID(N'dbo.WebScreenPermissions'))
    CREATE INDEX IX_WebScreenPermissions_Screen ON dbo.WebScreenPermissions(WebScreenId, UserId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_WebPermissionRoleTemplates_Key' AND object_id = OBJECT_ID(N'dbo.WebPermissionRoleTemplates'))
    CREATE UNIQUE INDEX UX_WebPermissionRoleTemplates_Key ON dbo.WebPermissionRoleTemplates(TemplateKey);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_WebPermissionRoleTemplateItems_TemplateScreen' AND object_id = OBJECT_ID(N'dbo.WebPermissionRoleTemplateItems'))
    CREATE UNIQUE INDEX UX_WebPermissionRoleTemplateItems_TemplateScreen ON dbo.WebPermissionRoleTemplateItems(TemplateId, WebScreenId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_WebScreenLegacyMap_Legacy' AND object_id = OBJECT_ID(N'dbo.WebScreenLegacyMap'))
    CREATE UNIQUE INDEX UX_WebScreenLegacyMap_Legacy ON dbo.WebScreenLegacyMap(LegacyScreenName, ScreenKey);
GO

IF OBJECT_ID(N'dbo.usp_WebScreenPermissions_SeedCatalog', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_WebScreenPermissions_SeedCatalog;
GO

CREATE PROCEDURE dbo.usp_WebScreenPermissions_SeedCatalog
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Modules TABLE
    (
        ModuleKey NVARCHAR(80),
        AreaName NVARCHAR(50),
        ArabicCaption NVARCHAR(200),
        EnglishCaption NVARCHAR(200),
        IconCss NVARCHAR(120),
        DisplayOrder INT
    );

    INSERT INTO @Modules VALUES
    (N'POS.Sales', N'POS', N'المبيعات', N'POS Sales', N'fas fa-store', 10),
    (N'POS.Reports', N'POS', N'التقارير', N'POS Reports', N'fas fa-chart-bar', 20),
    (N'POS.Inventory', N'POS', N'المخزون', N'POS Inventory', N'fas fa-boxes', 30),
    (N'POS.Accounting', N'POS', N'الحسابات والعهد', N'POS Accounting', N'fas fa-wallet', 40),
    (N'POS.Purchases', N'POS', N'المشتريات', N'POS Purchases', N'fas fa-receipt', 50),
    (N'POS.Excel', N'POS', N'Excel والاستيراد', N'POS Excel', N'fas fa-file-excel', 60),
    (N'POS.HR', N'POS', N'الموارد البشرية POS', N'POS HR', N'fas fa-users', 70),
    (N'POS.Settings', N'POS', N'الإعدادات', N'POS Settings', N'fas fa-cog', 80),
    (N'POS.Admin', N'POS', N'النظام والمراقبة', N'POS Admin', N'fas fa-shield-alt', 90),
    (N'MainERP.Customers', N'MainERP', N'العملاء والموردون', N'Customers and Suppliers', N'fas fa-address-book', 10),
    (N'MainERP.Finance', N'MainERP', N'المالية', N'Finance', N'fas fa-book', 20),
    (N'MainERP.Inventory', N'MainERP', N'المخازن', N'Inventory', N'fas fa-warehouse', 30),
    (N'MainERP.Projects', N'MainERP', N'المشاريع', N'Projects', N'fas fa-project-diagram', 40),
    (N'MainERP.HR', N'MainERP', N'الموارد البشرية', N'Human Resources', N'fas fa-user-tie', 50),
    (N'MainERP.Excel', N'MainERP', N'Excel والاستيراد', N'MainERP Excel', N'fas fa-file-excel', 60),
    (N'MainERP.Admin', N'MainERP', N'النظام', N'MainERP Admin', N'fas fa-shield-alt', 70);

    UPDATE target
       SET AreaName = source.AreaName,
           ArabicCaption = source.ArabicCaption,
           EnglishCaption = source.EnglishCaption,
           IconCss = source.IconCss,
           DisplayOrder = source.DisplayOrder,
           IsActive = 1,
           UpdatedAt = GETDATE()
      FROM dbo.WebModules target
      INNER JOIN @Modules source ON target.ModuleKey = source.ModuleKey;

    INSERT INTO dbo.WebModules
        (ModuleKey, AreaName, ArabicCaption, EnglishCaption, IconCss, DisplayOrder, IsActive, CreatedAt, UpdatedAt)
    SELECT source.ModuleKey, source.AreaName, source.ArabicCaption, source.EnglishCaption, source.IconCss, source.DisplayOrder, 1, GETDATE(), GETDATE()
      FROM @Modules source
     WHERE NOT EXISTS (SELECT 1 FROM dbo.WebModules target WHERE target.ModuleKey = source.ModuleKey);

    UPDATE dbo.WebModules
       SET IsActive = 0, UpdatedAt = GETDATE()
     WHERE AreaName = N'Shared' OR ModuleKey LIKE N'Shared.%';

    DECLARE @Screens TABLE
    (
        ModuleKey NVARCHAR(80),
        ScreenKey NVARCHAR(160),
        ArabicCaption NVARCHAR(220),
        EnglishCaption NVARCHAR(220),
        AreaName NVARCHAR(80),
        ControllerName NVARCHAR(120),
        ActionName NVARCHAR(120),
        RouteUrl NVARCHAR(300),
        IconCss NVARCHAR(120),
        DisplayOrder INT,
        IsMenuVisible BIT
    );

    INSERT INTO @Screens VALUES
    (N'POS.Sales', N'POS.Sales.Index', N'شاشة البيع', N'POS Sales Invoice', N'Pos', N'PosTransaction', N'Index', N'/Pos/PosTransaction/Index', N'fas fa-cash-register', 10, 1),
    (N'POS.Sales', N'POS.KYC.Index', N'KYC العملاء والكروت', N'KYC Customers', N'Pos', N'PosTransaction', N'Kyc', N'/Pos/PosTransaction/Kyc', N'fas fa-id-card', 20, 1),
    (N'POS.Sales', N'POS.KYC.BankFollowUp', N'متابعة KYC والبنك', N'KYC Bank Follow Up', N'Pos', N'KycBankFollowUp', N'Index', N'/Pos/KycBankFollowUp/Index', N'fas fa-landmark', 30, 1),
    (N'POS.Sales', N'POS.Closing.Index', N'إغلاق اليومية', N'Daily Closing', N'Pos', N'PosClosing', N'Index', N'/Pos/PosClosing/Index', N'fas fa-lock', 40, 1),
    (N'POS.Sales', N'POS.SalesPerformance.Index', N'أداء المناديب والمبيعات', N'Sales Representatives Performance', N'Pos', N'SalesRepresentativesPerformance', N'Index', N'/Pos/SalesRepresentativesPerformance/Index', N'fas fa-user-tie', 50, 1),
    (N'POS.Sales', N'POS.SalesTargets.Index', N'تارجت المناديب', N'Sales Targets', N'Pos', N'SalesTargets', N'Index', N'/Pos/SalesTargets/Index', N'fas fa-bullseye', 60, 1),
    (N'POS.Sales', N'POS.SalesCollections.Index', N'تحليل المبيعات والتحصيل', N'Sales Collections', N'Pos', N'FinancialIntelligence', N'SalesCollections', N'/Pos/FinancialIntelligence/SalesCollections', N'fas fa-chart-line', 70, 1),
    (N'POS.Reports', N'POS.Reports.Index', N'تقارير كيشني', N'Kishny Reports', N'Pos', N'PosReports', N'Index', N'/Pos/PosReports/Index', N'fas fa-chart-bar', 10, 1),
    (N'POS.Reports', N'POS.Reports.FinancialIntelligence', N'التقارير المالية', N'Financial Reports', N'Pos', N'FinancialIntelligence', N'Index', N'/Pos/FinancialIntelligence/Index', N'fas fa-gauge', 20, 1),
    (N'POS.Reports', N'POS.Reports.Accounting', N'تقارير الحسابات', N'Accounting Reports', N'Pos', N'AccountingReports', N'Index', N'/Pos/AccountingReports/Index', N'fas fa-landmark', 30, 1),
    (N'POS.Reports', N'POS.Reports.DynamicAdmin', N'إدارة التقارير الديناميكية', N'Dynamic Reports Admin', N'Pos', N'DynamicReportsAdmin', N'Index', N'/Pos/DynamicReportsAdmin/Index', N'fas fa-cog', 40, 1),
    (N'POS.Reports', N'POS.Reports.DynamicDesigner', N'مصمم التقارير الديناميكي', N'Dynamic Reports Designer', N'Pos', N'DynamicReports', N'Index', N'/Pos/DynamicReports/Index', N'fas fa-sliders', 50, 1),
    (N'POS.Inventory', N'POS.Items.Index', N'الأصناف', N'Items', N'Pos', N'Items', N'Index', N'/Pos/Items/Index', N'fas fa-boxes', 10, 1),
    (N'POS.Inventory', N'POS.Stores.Index', N'المخازن', N'Stores', N'Pos', N'Stores', N'Index', N'/Pos/Stores/Index', N'fas fa-warehouse', 20, 1),
    (N'POS.Inventory', N'POS.Stocktaking.Index', N'سند الجرد', N'Stocktaking', N'Pos', N'Stocktaking', N'Index', N'/Pos/Stocktaking/Index', N'fas fa-clipboard-check', 30, 1),
    (N'POS.Inventory', N'POS.StockTransfer.Index', N'التحويل المخزني', N'Stock Transfer', N'Pos', N'StockTransfer', N'Index', N'/Pos/StockTransfer/Index', N'fas fa-exchange-alt', 40, 1),
    (N'POS.Inventory', N'POS.InventoryProfitability.Index', N'مؤشرات المخزون والربحية', N'Inventory Profitability', N'Pos', N'FinancialIntelligence', N'InventoryProfitability', N'/Pos/FinancialIntelligence/InventoryProfitability', N'fas fa-chart-pie', 50, 1),
    (N'POS.Accounting', N'POS.AccountCharts.Index', N'دليل الحسابات', N'Chart of Accounts', N'Pos', N'AccountCharts', N'Index', N'/Pos/AccountCharts/Index', N'fas fa-book-open', 10, 1),
    (N'POS.Accounting', N'POS.Cashing.Index', N'الخزن / سندات القبض', N'Cashing', N'Pos', N'Cashing', N'Index', N'/Pos/Cashing/Index', N'fas fa-hand-holding-usd', 20, 1),
    (N'POS.Accounting', N'POS.Payments.Index', N'العهد / كيشني كارت', N'Custody Funding', N'Pos', N'Payments', N'Index', N'/Pos/Payments/Index', N'fas fa-wallet', 30, 1),
    (N'POS.Accounting', N'POS.PaymentVouchers.Index', N'سندات الصرف', N'Payment Vouchers', N'Pos', N'Payments', N'Vouchers', N'/Pos/Payments/Vouchers', N'fas fa-money-bill-wave', 40, 1),
    (N'POS.Accounting', N'POS.JournalEntries.Index', N'القيود اليومية', N'Journal Entries', N'Pos', N'JournalEntries', N'Index', N'/Pos/JournalEntries/Index', N'fas fa-book-open', 50, 1),
    (N'POS.Accounting', N'POS.DiscountNotifications.Index', N'إشعارات الخصم', N'Discount Notifications', N'Pos', N'DiscountNotifications', N'Index', N'/Pos/DiscountNotifications/Index', N'fas fa-percent', 60, 1),
    (N'POS.Accounting', N'POS.Custody.Index', N'العهد', N'Custody', N'Pos', N'FinancialIntelligence', N'Custody', N'/Pos/FinancialIntelligence/Custody', N'fas fa-wallet', 70, 1),
    (N'POS.Accounting', N'POS.CashFlow.Index', N'تحليل التدفقات النقدية', N'Cash Flow', N'Pos', N'FinancialIntelligence', N'CashFlow', N'/Pos/FinancialIntelligence/CashFlow', N'fas fa-coins', 80, 1),
    (N'POS.Accounting', N'POS.Expenses.Index', N'تحليل المصروفات', N'Expenses', N'Pos', N'FinancialIntelligence', N'Expenses', N'/Pos/FinancialIntelligence/Expenses', N'fas fa-chart-pie', 90, 1),
    (N'POS.Purchases', N'POS.PurchaseInvoice.Index', N'فاتورة مشتريات', N'Purchase Invoice', N'Pos', N'PurchaseInvoice', N'Index', N'/Pos/PurchaseInvoice/Index', N'fas fa-receipt', 10, 1),
    (N'POS.Excel', N'POS.ExcelImport.Index', N'استيراد فواتير Excel', N'Excel Import', N'Pos', N'ExcelImport', N'Index', N'/Pos/ExcelImport/Index', N'fas fa-file-excel', 10, 1),
    (N'POS.Excel', N'POS.TokenInvoiceLookup.Index', N'بحث التوكنات من Excel', N'Token Invoice Lookup', N'Pos', N'TokenInvoiceLookup', N'Index', N'/Pos/TokenInvoiceLookup/Index', N'fas fa-search', 20, 1),
    (N'POS.Excel', N'POS.InvoiceReconciliation.Index', N'مطابقة وتسوية فواتير Excel', N'Invoice Reconciliation', N'Pos', N'PosInvoiceReconciliation', N'Index', N'/Pos/PosInvoiceReconciliation/Index', N'fas fa-table', 30, 1),
    (N'POS.Excel', N'POS.ExcelErrorReports.Index', N'تقارير أخطاء Excel', N'Excel Error Reports', N'Pos', N'PosSystemErrorLog', N'Index', N'/Pos/PosSystemErrorLog/Index?source=excel', N'fas fa-bug', 40, 1),
    (N'POS.HR', N'POS.EmployeePayroll.Employees', N'الموظفون', N'Employees', N'Pos', N'EmployeePayroll', N'Employees', N'/Pos/EmployeePayroll/Employees', N'fas fa-users', 10, 1),
    (N'POS.HR', N'POS.EmployeePayroll.SalaryRun', N'مسير الرواتب', N'Salary Run', N'Pos', N'EmployeePayroll', N'SalaryRun', N'/Pos/EmployeePayroll/SalaryRun', N'fas fa-receipt', 20, 1),
    (N'POS.HR', N'POS.EmployeePayroll.MedicalInsurance', N'التأمين الطبي', N'Medical Insurance', N'Pos', N'EmployeePayroll', N'MedicalInsurance', N'/Pos/EmployeePayroll/MedicalInsurance', N'fas fa-heartbeat', 30, 1),
    (N'POS.HR', N'POS.EmployeePayroll.MedicalInsuranceReports', N'تقارير التأمين', N'Medical Insurance Reports', N'Pos', N'EmployeePayroll', N'MedicalInsuranceReports', N'/Pos/EmployeePayroll/MedicalInsuranceReports', N'fas fa-chart-bar', 40, 1),
    (N'POS.Settings', N'POS.Branches.Index', N'الفروع', N'Branches', N'Pos', N'Branches', N'Index', N'/Pos/Branches/Index', N'fas fa-building', 10, 1),
    (N'POS.Settings', N'POS.Options.Index', N'إعدادات النظام', N'Options', N'Pos', N'Options', N'Index', N'/Pos/Options/Index', N'fas fa-cog', 20, 1),
    (N'POS.Settings', N'POS.BranchLinking.Index', N'إعدادات الربط', N'Branch Linking', N'Pos', N'PosLegacyAdmin', N'BranchesData', N'/Pos/PosLegacyAdmin/BranchesData', N'fas fa-link', 30, 1),
    (N'POS.Settings', N'POS.PrintTemplates.KycCard', N'تصميم كارت KYC', N'KYC Print Templates', N'Pos', N'PrintTemplate', N'Index', N'/Pos/PrintTemplate/Index?name=KycCard', N'fas fa-id-card', 40, 1),
    (N'POS.Admin', N'POS.Dashboard.Index', N'لوحة التحكم', N'POS Dashboard', N'Pos', N'PosDashboard', N'Index', N'/Pos/PosDashboard/Index', N'fas fa-tachometer-alt', 10, 1),
    (N'POS.Admin', N'POS.Users.Index', N'المستخدمون', N'POS Users', N'Pos', N'PosLegacyAdmin', N'Users', N'/Pos/PosLegacyAdmin/Users', N'fas fa-users', 20, 1),
    (N'POS.Admin', N'POS.Permissions.LegacyFlags', N'صلاحيات POS الخاصة', N'POS Special Permissions', N'Pos', N'PosPermissions', N'Index', N'/Pos/PosPermissions/Index', N'fas fa-shield-alt', 30, 1),
    (N'POS.Admin', N'POS.Admin.WebPermissions', N'الصلاحيات على الشاشات', N'POS Web Screen Permissions', N'Pos', N'WebPermissions', N'Index', N'/Pos/WebPermissions/Index', N'fas fa-user-shield', 40, 1),
    (N'POS.Admin', N'POS.SystemHealth.Index', N'مراقبة النظام', N'System Health', N'Pos', N'PosSystemHealth', N'Index', N'/Pos/PosSystemHealth/Index', N'fas fa-heartbeat', 50, 1),
    (N'POS.Admin', N'POS.SystemErrorLog.Index', N'سجل أخطاء النظام', N'System Error Log', N'Pos', N'PosSystemErrorLog', N'Index', N'/Pos/PosSystemErrorLog/Index', N'fas fa-bug', 60, 1),
    (N'POS.Admin', N'POS.SqlUpdates.Index', N'تحديثات قاعدة البيانات', N'SQL Updates', N'Pos', N'PosSqlUpdates', N'Index', N'/Pos/PosSqlUpdates/Index', N'fas fa-database', 70, 1),
    (N'MainERP.Customers', N'MainERP.Customers.Index', N'العملاء والموردون', N'Customers and Suppliers', N'MainErp', N'Customers', N'Index', N'/MainErp/Customers', N'fas fa-users', 10, 1),
    (N'MainERP.Finance', N'MainERP.Finance.Banks', N'البنوك', N'Banks', N'MainErp', N'FinancialAdministration', N'Index', N'/MainErp/FinancialAdministration?scope=banks', N'fas fa-university', 10, 1),
    (N'MainERP.Finance', N'MainERP.Finance.Boxes', N'الخزن', N'Boxes', N'MainErp', N'FinancialAdministration', N'Index', N'/MainErp/FinancialAdministration?scope=boxes', N'fas fa-cash-register', 20, 1),
    (N'MainERP.Finance', N'MainERP.Cashing.Index', N'سند قبض', N'Cashing Voucher', N'MainErp', N'Cashing', N'Index', N'/MainErp/Cashing', N'fas fa-hand-holding-usd', 30, 1),
    (N'MainERP.Finance', N'MainERP.Payments.Index', N'سند صرف', N'Payment Voucher', N'MainErp', N'Payments', N'Index', N'/MainErp/Payments', N'fas fa-money-bill-wave', 40, 1),
    (N'MainERP.Finance', N'MainERP.LC.Index', N'الاعتمادات المستندية', N'Letters of Credit', N'MainErp', N'LC', N'Index', N'/MainErp/LC', N'fas fa-landmark', 50, 1),
    (N'MainERP.Finance', N'MainERP.AccountCharts.Index', N'دليل الحسابات', N'Chart of Accounts', N'MainErp', N'AccountCharts', N'Index', N'/MainErp/AccountCharts', N'fas fa-sitemap', 60, 1),
    (N'MainERP.Finance', N'MainERP.JournalEntries.Index', N'اليومية العامة', N'Journal Entries', N'MainErp', N'JournalEntries', N'Index', N'/MainErp/JournalEntries', N'fas fa-book-open', 70, 1),
    (N'MainERP.Finance', N'MainERP.AccountingReports.Index', N'تقارير الحسابات', N'Accounting Reports', N'MainErp', N'AccountingReports', N'Index', N'/MainErp/AccountingReports', N'fas fa-chart-line', 80, 1),
    (N'MainERP.Inventory', N'MainERP.Items.Index', N'الأصناف', N'Items', N'MainErp', N'Items', N'Index', N'/MainErp/Items', N'fas fa-box', 10, 1),
    (N'MainERP.Inventory', N'MainERP.StoreData.Index', N'المخازن', N'Stores', N'MainErp', N'StoreData', N'Index', N'/MainErp/StoreData', N'fas fa-warehouse', 20, 1),
    (N'MainERP.Inventory', N'MainERP.Stocktaking.Index', N'الجرد', N'Stocktaking', N'MainErp', N'Stocktaking', N'Index', N'/MainErp/Stocktaking', N'fas fa-clipboard-check', 30, 1),
    (N'MainERP.Inventory', N'MainERP.DefinCompItem.Index', N'سند التجميع', N'Assembly Voucher', N'MainErp', N'DefinCompItem', N'Index', N'/MainErp/DefinCompItem', N'fas fa-layer-group', 40, 1),
    (N'MainERP.Projects', N'MainERP.Projects.Index', N'المشاريع', N'Projects', N'MainErp', N'Projects', N'Index', N'/MainErp/Projects', N'fas fa-folder-open', 10, 1),
    (N'MainERP.Projects', N'MainERP.ProjectExtracts.Index', N'مستخلصات المشاريع', N'Project Extracts', N'MainErp', N'ProjectExtracts', N'Index', N'/MainErp/ProjectExtracts', N'fas fa-tasks', 20, 1),
    (N'MainERP.HR', N'MainERP.EmployeePayroll.Employees', N'الموظفون', N'Employees', N'MainErp', N'EmployeePayroll', N'Employees', N'/MainErp/EmployeePayroll/Employees', N'fas fa-users', 10, 1),
    (N'MainERP.HR', N'MainERP.EmployeePayroll.SalaryRun', N'المسير', N'Salary Run', N'MainErp', N'EmployeePayroll', N'SalaryRun', N'/MainErp/EmployeePayroll/SalaryRun', N'fas fa-file-invoice-dollar', 20, 1),
    (N'MainERP.HR', N'MainERP.EmployeePayroll.MedicalInsurance', N'التأمين الطبي', N'Medical Insurance', N'MainErp', N'EmployeePayroll', N'MedicalInsurance', N'/MainErp/EmployeePayroll/MedicalInsurance', N'fas fa-briefcase-medical', 30, 1),
    (N'MainERP.Excel', N'MainERP.MasterDataImport.Index', N'استيراد بيانات ERP من Excel', N'Master Data Import', N'MainErp', N'MasterDataImport', N'Index', N'/MainErp/MasterDataImport', N'fas fa-file-import', 10, 1),
    (N'MainERP.Admin', N'MainERP.Users.Index', N'المستخدمون', N'Users', N'MainErp', N'Users', N'Index', N'/MainErp/Users', N'fas fa-user-cog', 10, 1),
    (N'MainERP.Admin', N'MainERP.Admin.WebPermissions', N'الصلاحيات على الشاشات', N'MainERP Web Screen Permissions', N'MainErp', N'Permissions', N'Index', N'/MainErp/Permissions', N'fas fa-user-shield', 20, 1),
    (N'MainERP.Admin', N'MainERP.Branches.Index', N'الفروع', N'Branches', N'MainErp', N'Branches', N'Index', N'/MainErp/Branches', N'fas fa-building', 30, 1),
    (N'MainERP.Admin', N'MainERP.Options.Index', N'الإعدادات', N'Options', N'MainErp', N'Options', N'Index', N'/MainErp/Options', N'fas fa-cog', 40, 1),
    (N'MainERP.Admin', N'MainERP.DatabaseMigration.Index', N'تحديثات قاعدة البيانات', N'Database Updates', N'MainErp', N'DatabaseMigration', N'Index', N'/MainErp/DatabaseMigration', N'fas fa-database', 50, 1),
    (N'MainERP.Admin', N'MainERP.Dashboard.Index', N'لوحة التحكم', N'MainERP Dashboard', N'MainErp', N'Dashboard', N'Index', N'/MainErp/Dashboard', N'fas fa-tachometer-alt', 60, 1);

    UPDATE target
       SET WebModuleId = source.WebModuleId,
           ArabicCaption = source.ArabicCaption,
           EnglishCaption = source.EnglishCaption,
           AreaName = source.AreaName,
           ControllerName = source.ControllerName,
           ActionName = source.ActionName,
           RouteUrl = source.RouteUrl,
           IconCss = source.IconCss,
           DisplayOrder = source.DisplayOrder,
           IsActive = 1,
           IsMenuVisible = source.IsMenuVisible,
           UpdatedAt = GETDATE()
      FROM dbo.WebScreens target
      INNER JOIN (SELECT m.WebModuleId, s.* FROM @Screens s INNER JOIN dbo.WebModules m ON m.ModuleKey = s.ModuleKey) source
              ON target.ScreenKey = source.ScreenKey;

    INSERT INTO dbo.WebScreens
        (WebModuleId, ScreenKey, ArabicCaption, EnglishCaption, AreaName, ControllerName, ActionName, RouteUrl, IconCss, DisplayOrder, IsActive, IsMenuVisible, CreatedAt, UpdatedAt)
    SELECT source.WebModuleId, source.ScreenKey, source.ArabicCaption, source.EnglishCaption, source.AreaName, source.ControllerName, source.ActionName, source.RouteUrl, source.IconCss, source.DisplayOrder, 1, source.IsMenuVisible, GETDATE(), GETDATE()
      FROM (SELECT m.WebModuleId, s.* FROM @Screens s INNER JOIN dbo.WebModules m ON m.ModuleKey = s.ModuleKey) source
     WHERE NOT EXISTS (SELECT 1 FROM dbo.WebScreens target WHERE target.ScreenKey = source.ScreenKey);

    UPDATE dbo.WebScreens
       SET IsActive = 0, IsMenuVisible = 0, UpdatedAt = GETDATE()
     WHERE ScreenKey LIKE N'Shared.%';

    DECLARE @LegacyMap TABLE (LegacyScreenName NVARCHAR(255), ScreenKey NVARCHAR(160), Notes NVARCHAR(400));

    INSERT INTO @LegacyMap
    SELECT *
    FROM
    (
        SELECT N'FrmSaleBill6' LegacyScreenName, N'POS.Sales.Index' ScreenKey, N'فاتورة POS القديمة' Notes
        UNION ALL SELECT N'FrmCashing', N'POS.Cashing.Index', N'سندات قبض POS'
        UNION ALL SELECT N'FrmPayments', N'POS.PaymentVouchers.Index', N'سندات صرف POS'
        UNION ALL SELECT N'FrmStoreData', N'POS.Stores.Index', N'المخازن'
        UNION ALL SELECT N'FrmItem', N'POS.Items.Index', N'الأصناف'
        UNION ALL SELECT N'FrmStocktaking', N'POS.Stocktaking.Index', N'الجرد'
        UNION ALL SELECT N'FrmEmployee', N'MainERP.EmployeePayroll.Employees', N'الموظفون MainERP'
        UNION ALL SELECT N'FrmEmpSalary5', N'MainERP.EmployeePayroll.SalaryRun', N'مسير الرواتب MainERP'
        UNION ALL SELECT N'FrmCustemers', N'MainERP.Customers.Index', N'العملاء MainERP'
        UNION ALL SELECT N'FrmBanksData', N'MainERP.Finance.Banks', N'البنوك MainERP'
        UNION ALL SELECT N'FrmBoxesData', N'MainERP.Finance.Boxes', N'الخزن MainERP'
    ) source;

    UPDATE target
       SET Notes = source.Notes,
           IsActive = 1
      FROM dbo.WebScreenLegacyMap target
      INNER JOIN @LegacyMap source ON target.LegacyScreenName = source.LegacyScreenName AND target.ScreenKey = source.ScreenKey;

    INSERT INTO dbo.WebScreenLegacyMap (LegacyScreenName, ScreenKey, Notes, IsActive)
    SELECT source.LegacyScreenName, source.ScreenKey, source.Notes, 1
      FROM @LegacyMap source
     WHERE NOT EXISTS
     (
         SELECT 1
           FROM dbo.WebScreenLegacyMap target
          WHERE target.LegacyScreenName = source.LegacyScreenName
            AND target.ScreenKey = source.ScreenKey
     );

    IF OBJECT_ID(N'dbo.ScreenJuncUser', N'U') IS NOT NULL
    BEGIN
        DECLARE @LegacyPermissions TABLE
        (
            UserId INT,
            WebScreenId INT,
            CanView INT,
            CanAdd INT,
            CanEdit INT,
            CanDelete INT,
            CanPrint INT
        );

        INSERT INTO @LegacyPermissions
            SELECT
                j.User_ID AS UserId,
                s.WebScreenId,
                MAX(CASE WHEN ISNULL(j.FullAccess, 0) = 1 OR ISNULL(j.CanShow, 0) = 1 OR ISNULL(j.CanSearch, 0) = 1 THEN 1 ELSE 0 END) AS CanView,
                MAX(CASE WHEN ISNULL(j.FullAccess, 0) = 1 OR ISNULL(j.CanAdd, 0) = 1 THEN 1 ELSE 0 END) AS CanAdd,
                MAX(CASE WHEN ISNULL(j.FullAccess, 0) = 1 OR ISNULL(j.CanEdit, 0) = 1 THEN 1 ELSE 0 END) AS CanEdit,
                MAX(CASE WHEN ISNULL(j.FullAccess, 0) = 1 OR ISNULL(j.CanDelete, 0) = 1 THEN 1 ELSE 0 END) AS CanDelete,
                MAX(CASE WHEN ISNULL(j.FullAccess, 0) = 1 OR ISNULL(j.CanPrint, 0) = 1 THEN 1 ELSE 0 END) AS CanPrint
            FROM dbo.ScreenJuncUser j
            INNER JOIN dbo.WebScreenLegacyMap map ON map.LegacyScreenName = j.ScreenName AND map.IsActive = 1
            INNER JOIN dbo.WebScreens s ON s.ScreenKey = map.ScreenKey AND s.IsActive = 1
            GROUP BY j.User_ID, s.WebScreenId;

        UPDATE target
           SET CanView = CASE WHEN target.SeedSource IS NULL OR target.SeedSource IN (N'LegacyScreenJuncUser', N'POS_UserPermissions') THEN source.CanView ELSE target.CanView END,
            CanAdd = CASE WHEN target.SeedSource IS NULL OR target.SeedSource IN (N'LegacyScreenJuncUser', N'POS_UserPermissions') THEN source.CanAdd ELSE target.CanAdd END,
            CanEdit = CASE WHEN target.SeedSource IS NULL OR target.SeedSource IN (N'LegacyScreenJuncUser', N'POS_UserPermissions') THEN source.CanEdit ELSE target.CanEdit END,
            CanDelete = CASE WHEN target.SeedSource IS NULL OR target.SeedSource IN (N'LegacyScreenJuncUser', N'POS_UserPermissions') THEN source.CanDelete ELSE target.CanDelete END,
            CanPrint = CASE WHEN target.SeedSource IS NULL OR target.SeedSource IN (N'LegacyScreenJuncUser', N'POS_UserPermissions') THEN source.CanPrint ELSE target.CanPrint END,
            SeedSource = ISNULL(target.SeedSource, N'LegacyScreenJuncUser'),
            UpdatedAt = GETDATE()
          FROM dbo.WebScreenPermissions target
          INNER JOIN @LegacyPermissions source ON target.UserId = source.UserId AND target.WebScreenId = source.WebScreenId;

        INSERT INTO dbo.WebScreenPermissions
            (UserId, WebScreenId, CanView, CanAdd, CanEdit, CanDelete, CanPrint, CanExport, CanApprove, SeedSource, CreatedAt, UpdatedAt)
        SELECT source.UserId, source.WebScreenId, source.CanView, source.CanAdd, source.CanEdit, source.CanDelete, source.CanPrint, 0, 0, N'LegacyScreenJuncUser', GETDATE(), GETDATE()
          FROM @LegacyPermissions source
         WHERE NOT EXISTS
         (
             SELECT 1
               FROM dbo.WebScreenPermissions target
              WHERE target.UserId = source.UserId
                AND target.WebScreenId = source.WebScreenId
         );
    END

    IF OBJECT_ID(N'dbo.POS_UserPermissions', N'U') IS NOT NULL
    BEGIN
        DECLARE @PosPermissionMap TABLE
        (
            PermissionKey NVARCHAR(100),
            ScreenKey NVARCHAR(160),
            CanView BIT,
            CanAdd BIT,
            CanEdit BIT,
            CanDelete BIT,
            CanPrint BIT,
            CanExport BIT,
            CanApprove BIT
        );

        INSERT INTO @PosPermissionMap VALUES
        (N'CanTeller', N'POS.Sales.Index', 1, 1, 0, 0, 1, 0, 0),
        (N'CanOpenSales', N'POS.Sales.Index', 1, 0, 0, 0, 0, 0, 0),
        (N'CanSaveInvoice', N'POS.Sales.Index', 1, 1, 0, 0, 0, 0, 0),
        (N'CanEditSalesInvoice', N'POS.Sales.Index', 1, 0, 1, 0, 0, 0, 0),
        (N'CanEditSalesInvoicePos', N'POS.Sales.Index', 1, 0, 1, 0, 0, 0, 0),
        (N'CanCancelInvoice', N'POS.Sales.Index', 1, 0, 0, 1, 0, 0, 0),
        (N'CanCancelOrReturn', N'POS.Sales.Index', 1, 0, 0, 1, 0, 0, 0),
        (N'CustomerService', N'POS.KYC.Index', 1, 1, 0, 0, 0, 0, 0),
        (N'CanEditKyc', N'POS.KYC.Index', 1, 1, 1, 0, 0, 0, 0),
        (N'CanPrintKycAcknowledgment', N'POS.KYC.Index', 1, 0, 0, 0, 1, 0, 0),
        (N'CanPrintKycCard', N'POS.KYC.Index', 1, 0, 0, 0, 1, 0, 0),
        (N'IsFullAccsesCustomerService', N'POS.KYC.BankFollowUp', 1, 0, 1, 0, 1, 1, 0),
        (N'CanOpenClosing', N'POS.Closing.Index', 1, 0, 0, 0, 0, 0, 0),
        (N'CanExecuteClosing', N'POS.Closing.Index', 1, 1, 0, 0, 1, 1, 0),
        (N'CanViewReports', N'POS.Reports.Index', 1, 0, 0, 0, 1, 1, 0),
        (N'CanReportAllSales', N'POS.Reports.Index', 1, 0, 0, 0, 1, 1, 0),
        (N'CanReportDailyTransactions', N'POS.Reports.Index', 1, 0, 0, 0, 1, 1, 0),
        (N'CanReportStoreSerials', N'POS.InventoryProfitability.Index', 1, 0, 0, 0, 1, 1, 0),
        (N'CanViewAccountingReports', N'POS.Reports.Accounting', 1, 0, 0, 0, 1, 1, 0),
        (N'CanViewTrialBalance', N'POS.Reports.Accounting', 1, 0, 0, 0, 1, 1, 0),
        (N'CanViewIncomeStatement', N'POS.Reports.Accounting', 1, 0, 0, 0, 1, 1, 0),
        (N'CanViewAccountStatement', N'POS.Reports.Accounting', 1, 0, 0, 0, 1, 1, 0),
        (N'CanViewJournalEntry', N'POS.JournalEntries.Index', 1, 0, 0, 0, 1, 1, 0),
        (N'CanCreateJournalEntry', N'POS.JournalEntries.Index', 1, 1, 0, 0, 0, 0, 0),
        (N'CanEditJournalEntry', N'POS.JournalEntries.Index', 1, 0, 1, 0, 0, 0, 0),
        (N'CanDeleteJournalEntry', N'POS.JournalEntries.Index', 1, 0, 0, 1, 0, 0, 0),
        (N'CanOpenPayments', N'POS.Payments.Index', 1, 0, 0, 0, 0, 0, 0),
        (N'CanOpenPayments', N'POS.Custody.Index', 1, 0, 0, 0, 0, 0, 0),
        (N'CanExecutePayments', N'POS.Payments.Index', 1, 1, 0, 0, 1, 1, 0),
        (N'CanEditPayments', N'POS.Payments.Index', 1, 0, 1, 0, 0, 0, 0),
        (N'CanImportExcel', N'POS.ExcelImport.Index', 1, 1, 0, 0, 0, 1, 0),
        (N'CanDeleteExcelReconciliationInvoices', N'POS.InvoiceReconciliation.Index', 1, 0, 0, 1, 0, 0, 0);

        DECLARE @PosPermissions TABLE
        (
            UserId INT,
            WebScreenId INT,
            CanView INT,
            CanAdd INT,
            CanEdit INT,
            CanDelete INT,
            CanPrint INT,
            CanExport INT,
            CanApprove INT
        );

        INSERT INTO @PosPermissions
            SELECT
                p.UserID AS UserId,
                s.WebScreenId,
                MAX(CONVERT(INT, m.CanView)) AS CanView,
                MAX(CONVERT(INT, m.CanAdd)) AS CanAdd,
                MAX(CONVERT(INT, m.CanEdit)) AS CanEdit,
                MAX(CONVERT(INT, m.CanDelete)) AS CanDelete,
                MAX(CONVERT(INT, m.CanPrint)) AS CanPrint,
                MAX(CONVERT(INT, m.CanExport)) AS CanExport,
                MAX(CONVERT(INT, m.CanApprove)) AS CanApprove
            FROM dbo.POS_UserPermissions p
            INNER JOIN @PosPermissionMap m ON m.PermissionKey = p.PermissionKey
            INNER JOIN dbo.WebScreens s ON s.ScreenKey = m.ScreenKey AND s.IsActive = 1
            WHERE p.IsAllowed = 1
            GROUP BY p.UserID, s.WebScreenId;

        UPDATE target
           SET CanView = CASE WHEN target.SeedSource IS NULL OR target.SeedSource IN (N'LegacyScreenJuncUser', N'POS_UserPermissions') THEN CONVERT(BIT, source.CanView) ELSE target.CanView END,
            CanAdd = CASE WHEN target.SeedSource IS NULL OR target.SeedSource IN (N'LegacyScreenJuncUser', N'POS_UserPermissions') THEN CONVERT(BIT, source.CanAdd) ELSE target.CanAdd END,
            CanEdit = CASE WHEN target.SeedSource IS NULL OR target.SeedSource IN (N'LegacyScreenJuncUser', N'POS_UserPermissions') THEN CONVERT(BIT, source.CanEdit) ELSE target.CanEdit END,
            CanDelete = CASE WHEN target.SeedSource IS NULL OR target.SeedSource IN (N'LegacyScreenJuncUser', N'POS_UserPermissions') THEN CONVERT(BIT, source.CanDelete) ELSE target.CanDelete END,
            CanPrint = CASE WHEN target.SeedSource IS NULL OR target.SeedSource IN (N'LegacyScreenJuncUser', N'POS_UserPermissions') THEN CONVERT(BIT, source.CanPrint) ELSE target.CanPrint END,
            CanExport = CASE WHEN target.SeedSource IS NULL OR target.SeedSource IN (N'LegacyScreenJuncUser', N'POS_UserPermissions') THEN CONVERT(BIT, source.CanExport) ELSE target.CanExport END,
            CanApprove = CASE WHEN target.SeedSource IS NULL OR target.SeedSource IN (N'LegacyScreenJuncUser', N'POS_UserPermissions') THEN CONVERT(BIT, source.CanApprove) ELSE target.CanApprove END,
            SeedSource = ISNULL(target.SeedSource, N'POS_UserPermissions'),
            UpdatedAt = GETDATE()
          FROM dbo.WebScreenPermissions target
          INNER JOIN @PosPermissions source ON target.UserId = source.UserId AND target.WebScreenId = source.WebScreenId;

        INSERT INTO dbo.WebScreenPermissions
            (UserId, WebScreenId, CanView, CanAdd, CanEdit, CanDelete, CanPrint, CanExport, CanApprove, SeedSource, CreatedAt, UpdatedAt)
        SELECT source.UserId, source.WebScreenId, CONVERT(BIT, source.CanView), CONVERT(BIT, source.CanAdd), CONVERT(BIT, source.CanEdit), CONVERT(BIT, source.CanDelete), CONVERT(BIT, source.CanPrint), CONVERT(BIT, source.CanExport), CONVERT(BIT, source.CanApprove), N'POS_UserPermissions', GETDATE(), GETDATE()
          FROM @PosPermissions source
         WHERE NOT EXISTS
         (
             SELECT 1
               FROM dbo.WebScreenPermissions target
              WHERE target.UserId = source.UserId
                AND target.WebScreenId = source.WebScreenId
         );
    END

    IF COL_LENGTH(N'dbo.TblUsers', N'UserCategory') IS NOT NULL
    BEGIN
        DECLARE @TellerPermissions TABLE (UserId INT, WebScreenId INT);

        INSERT INTO @TellerPermissions
            SELECT u.UserID AS UserId, s.WebScreenId
            FROM dbo.TblUsers u
            CROSS JOIN dbo.WebScreens s
            WHERE s.ScreenKey IN (N'POS.Sales.Index')
              AND LTRIM(RTRIM(ISNULL(u.UserCategory, N''))) IN (N'تلر', N'Teller', N'teller');

        UPDATE target
           SET CanView = CASE WHEN target.SeedSource IS NULL OR target.SeedSource IN (N'LegacyScreenJuncUser', N'POS_UserPermissions', N'POS_Teller_Default') THEN 1 ELSE target.CanView END,
            CanAdd = CASE WHEN target.SeedSource IS NULL OR target.SeedSource IN (N'LegacyScreenJuncUser', N'POS_UserPermissions', N'POS_Teller_Default') THEN 1 ELSE target.CanAdd END,
            CanEdit = CASE WHEN target.SeedSource IS NULL OR target.SeedSource IN (N'LegacyScreenJuncUser', N'POS_UserPermissions', N'POS_Teller_Default') THEN 1 ELSE target.CanEdit END,
            CanPrint = CASE WHEN target.SeedSource IS NULL OR target.SeedSource IN (N'LegacyScreenJuncUser', N'POS_UserPermissions', N'POS_Teller_Default') THEN 1 ELSE target.CanPrint END,
            SeedSource = ISNULL(target.SeedSource, N'POS_Teller_Default'),
            UpdatedAt = GETDATE()
          FROM dbo.WebScreenPermissions target
          INNER JOIN @TellerPermissions source ON target.UserId = source.UserId AND target.WebScreenId = source.WebScreenId;

        INSERT INTO dbo.WebScreenPermissions
            (UserId, WebScreenId, CanView, CanAdd, CanEdit, CanDelete, CanPrint, CanExport, CanApprove, SeedSource, CreatedAt, UpdatedAt)
        SELECT source.UserId, source.WebScreenId, 1, 1, 1, 0, 1, 0, 0, N'POS_Teller_Default', GETDATE(), GETDATE()
          FROM @TellerPermissions source
         WHERE NOT EXISTS
         (
             SELECT 1
               FROM dbo.WebScreenPermissions target
              WHERE target.UserId = source.UserId
                AND target.WebScreenId = source.WebScreenId
         );
    END

    DECLARE @RoleTemplates TABLE
    (
        TemplateKey NVARCHAR(120),
        AreaName NVARCHAR(50),
        ArabicCaption NVARCHAR(200),
        EnglishCaption NVARCHAR(200),
        DisplayOrder INT
    );

    INSERT INTO @RoleTemplates
    SELECT *
    FROM
    (
        SELECT N'POS.Teller' TemplateKey, N'POS' AreaName, N'تلر POS' ArabicCaption, N'POS Teller' EnglishCaption, 10 DisplayOrder
        UNION ALL SELECT N'POS.KYC', N'POS', N'موظف KYC', N'KYC Officer', 20
        UNION ALL SELECT N'POS.Accountant', N'POS', N'حسابات POS', N'POS Accountant', 30
        UNION ALL SELECT N'MainERP.Accountant', N'MainERP', N'محاسب MainERP', N'MainERP Accountant', 40
    ) source;

    UPDATE target
       SET AreaName = source.AreaName,
           ArabicCaption = source.ArabicCaption,
           EnglishCaption = source.EnglishCaption,
           DisplayOrder = source.DisplayOrder,
           IsActive = 1,
           UpdatedAt = GETDATE()
      FROM dbo.WebPermissionRoleTemplates target
      INNER JOIN @RoleTemplates source ON target.TemplateKey = source.TemplateKey;

    INSERT INTO dbo.WebPermissionRoleTemplates
        (TemplateKey, AreaName, ArabicCaption, EnglishCaption, DisplayOrder, IsActive, CreatedAt, UpdatedAt)
    SELECT source.TemplateKey, source.AreaName, source.ArabicCaption, source.EnglishCaption, source.DisplayOrder, 1, GETDATE(), GETDATE()
      FROM @RoleTemplates source
     WHERE NOT EXISTS
     (
         SELECT 1
           FROM dbo.WebPermissionRoleTemplates target
          WHERE target.TemplateKey = source.TemplateKey
     );

    UPDATE dbo.WebPermissionRoleTemplates
       SET IsActive = 0, UpdatedAt = GETDATE()
     WHERE AreaName = N'Shared' OR TemplateKey LIKE N'Shared.%';

    DELETE i
    FROM dbo.WebPermissionRoleTemplateItems i
    INNER JOIN dbo.WebPermissionRoleTemplates t ON t.TemplateId = i.TemplateId
    WHERE t.TemplateKey IN (N'POS.Teller', N'POS.KYC', N'POS.Accountant', N'MainERP.Accountant');

    INSERT INTO dbo.WebPermissionRoleTemplateItems
    (TemplateId, WebScreenId, CanView, CanAdd, CanEdit, CanDelete, CanPrint, CanExport, CanApprove)
    SELECT t.TemplateId, s.WebScreenId,
           1,
           CASE WHEN s.ScreenKey = N'POS.Sales.Index' THEN 1 ELSE 0 END,
           CASE WHEN s.ScreenKey = N'POS.Sales.Index' THEN 1 ELSE 0 END,
           0,
           CASE WHEN s.ScreenKey = N'POS.Sales.Index' THEN 1 ELSE 0 END,
           0,
           0
    FROM dbo.WebPermissionRoleTemplates t
    INNER JOIN dbo.WebScreens s ON s.ScreenKey IN (N'POS.Sales.Index')
    WHERE t.TemplateKey = N'POS.Teller';

    INSERT INTO dbo.WebPermissionRoleTemplateItems
    (TemplateId, WebScreenId, CanView, CanAdd, CanEdit, CanDelete, CanPrint, CanExport, CanApprove)
    SELECT t.TemplateId, s.WebScreenId,
           1,
           CASE WHEN s.ScreenKey = N'POS.KYC.Index' THEN 1 ELSE 0 END,
           CASE WHEN s.ScreenKey IN (N'POS.KYC.Index', N'POS.KYC.BankFollowUp') THEN 1 ELSE 0 END,
           0,
           CASE WHEN s.ScreenKey IN (N'POS.KYC.Index', N'POS.KYC.BankFollowUp') THEN 1 ELSE 0 END,
           1,
           0
    FROM dbo.WebPermissionRoleTemplates t
    INNER JOIN dbo.WebScreens s ON s.ScreenKey IN (N'POS.KYC.Index', N'POS.KYC.BankFollowUp', N'POS.ExcelImport.Index')
    WHERE t.TemplateKey = N'POS.KYC';

    INSERT INTO dbo.WebPermissionRoleTemplateItems
    (TemplateId, WebScreenId, CanView, CanAdd, CanEdit, CanDelete, CanPrint, CanExport, CanApprove)
    SELECT t.TemplateId, s.WebScreenId, 1, 0, 0, 0, 1, 1, 0
    FROM dbo.WebPermissionRoleTemplates t
    INNER JOIN dbo.WebScreens s ON s.ScreenKey IN (N'POS.Reports.Index', N'POS.Reports.Accounting', N'POS.Cashing.Index', N'POS.Payments.Index', N'POS.Custody.Index', N'POS.JournalEntries.Index')
    WHERE t.TemplateKey = N'POS.Accountant';

    INSERT INTO dbo.WebPermissionRoleTemplateItems
    (TemplateId, WebScreenId, CanView, CanAdd, CanEdit, CanDelete, CanPrint, CanExport, CanApprove)
    SELECT t.TemplateId, s.WebScreenId, 1, 1, 1, 0, 1, 1, 0
    FROM dbo.WebPermissionRoleTemplates t
    INNER JOIN dbo.WebScreens s ON s.ScreenKey IN (N'MainERP.Finance.Banks', N'MainERP.Finance.Boxes', N'MainERP.Cashing.Index', N'MainERP.Payments.Index', N'MainERP.AccountCharts.Index', N'MainERP.JournalEntries.Index', N'MainERP.AccountingReports.Index')
    WHERE t.TemplateKey = N'MainERP.Accountant';
END
GO

EXEC dbo.usp_WebScreenPermissions_SeedCatalog;
GO
