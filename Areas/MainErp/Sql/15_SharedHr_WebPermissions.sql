/*
Shared HR web permissions catalog.
SQL Server 2012 compatible.
*/

IF OBJECT_ID(N'dbo.WebModules', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WebModules
    (
        WebModuleId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WebModules PRIMARY KEY,
        ModuleKey NVARCHAR(80) NOT NULL,
        ArabicCaption NVARCHAR(200) NOT NULL,
        EnglishCaption NVARCHAR(200) NULL,
        AreaName NVARCHAR(50) NOT NULL,
        DisplayOrder INT NOT NULL CONSTRAINT DF_WebModules_DisplayOrder DEFAULT(0),
        IsActive BIT NOT NULL CONSTRAINT DF_WebModules_IsActive DEFAULT(1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_WebModules_CreatedAt DEFAULT(GETDATE()),
        UpdatedAt DATETIME NULL
    );
END;

IF OBJECT_ID(N'dbo.WebScreens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WebScreens
    (
        WebScreenId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WebScreens PRIMARY KEY,
        WebModuleId INT NOT NULL,
        ScreenKey NVARCHAR(160) NOT NULL,
        ArabicCaption NVARCHAR(200) NOT NULL,
        EnglishCaption NVARCHAR(200) NULL,
        RouteUrl NVARCHAR(500) NULL,
        ControllerName NVARCHAR(120) NULL,
        ActionName NVARCHAR(120) NULL,
        IconCss NVARCHAR(120) NULL,
        DisplayOrder INT NOT NULL CONSTRAINT DF_WebScreens_DisplayOrder DEFAULT(0),
        IsActive BIT NOT NULL CONSTRAINT DF_WebScreens_IsActive DEFAULT(1),
        IsMenuVisible BIT NOT NULL CONSTRAINT DF_WebScreens_IsMenuVisible DEFAULT(1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_WebScreens_CreatedAt DEFAULT(GETDATE()),
        UpdatedAt DATETIME NULL,
        CONSTRAINT FK_WebScreens_WebModules FOREIGN KEY (WebModuleId) REFERENCES dbo.WebModules(WebModuleId)
    );
END;

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
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_WebScreenPermissions_CreatedAt DEFAULT(GETDATE()),
        UpdatedAt DATETIME NULL,
        CONSTRAINT FK_WebScreenPermissions_WebScreens FOREIGN KEY (WebScreenId) REFERENCES dbo.WebScreens(WebScreenId)
    );
END;

IF OBJECT_ID(N'dbo.WebPermissionRoleTemplates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WebPermissionRoleTemplates
    (
        TemplateId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WebPermissionRoleTemplates PRIMARY KEY,
        TemplateKey NVARCHAR(120) NOT NULL,
        ArabicCaption NVARCHAR(200) NOT NULL,
        EnglishCaption NVARCHAR(200) NULL,
        AreaName NVARCHAR(50) NOT NULL,
        DisplayOrder INT NOT NULL CONSTRAINT DF_WebPermissionRoleTemplates_DisplayOrder DEFAULT(0),
        IsActive BIT NOT NULL CONSTRAINT DF_WebPermissionRoleTemplates_IsActive DEFAULT(1)
    );
END;

IF OBJECT_ID(N'dbo.WebPermissionTemplateItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WebPermissionTemplateItems
    (
        TemplateItemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WebPermissionTemplateItems PRIMARY KEY,
        TemplateId INT NOT NULL,
        WebScreenId INT NOT NULL,
        CanView BIT NOT NULL CONSTRAINT DF_WebPermissionTemplateItems_CanView DEFAULT(0),
        CanAdd BIT NOT NULL CONSTRAINT DF_WebPermissionTemplateItems_CanAdd DEFAULT(0),
        CanEdit BIT NOT NULL CONSTRAINT DF_WebPermissionTemplateItems_CanEdit DEFAULT(0),
        CanDelete BIT NOT NULL CONSTRAINT DF_WebPermissionTemplateItems_CanDelete DEFAULT(0),
        CanPrint BIT NOT NULL CONSTRAINT DF_WebPermissionTemplateItems_CanPrint DEFAULT(0),
        CanExport BIT NOT NULL CONSTRAINT DF_WebPermissionTemplateItems_CanExport DEFAULT(0),
        CanApprove BIT NOT NULL CONSTRAINT DF_WebPermissionTemplateItems_CanApprove DEFAULT(0),
        CONSTRAINT FK_WebPermissionTemplateItems_Template FOREIGN KEY (TemplateId) REFERENCES dbo.WebPermissionRoleTemplates(TemplateId),
        CONSTRAINT FK_WebPermissionTemplateItems_Screen FOREIGN KEY (WebScreenId) REFERENCES dbo.WebScreens(WebScreenId)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_WebModules_ModuleKey' AND object_id = OBJECT_ID(N'dbo.WebModules', N'U'))
    CREATE UNIQUE INDEX UX_WebModules_ModuleKey ON dbo.WebModules(ModuleKey);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_WebScreens_ScreenKey' AND object_id = OBJECT_ID(N'dbo.WebScreens', N'U'))
    CREATE UNIQUE INDEX UX_WebScreens_ScreenKey ON dbo.WebScreens(ScreenKey);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_WebScreenPermissions_User_Screen' AND object_id = OBJECT_ID(N'dbo.WebScreenPermissions', N'U'))
    CREATE UNIQUE INDEX UX_WebScreenPermissions_User_Screen ON dbo.WebScreenPermissions(UserId, WebScreenId);

DECLARE @ModuleId INT;

IF EXISTS (SELECT 1 FROM dbo.WebModules WHERE ModuleKey = N'Shared.HR')
BEGIN
    UPDATE dbo.WebModules
       SET ArabicCaption = N'شؤون الموظفين',
           EnglishCaption = N'Human Resources',
           AreaName = N'Shared',
           DisplayOrder = 30,
           IsActive = 1,
           UpdatedAt = GETDATE()
     WHERE ModuleKey = N'Shared.HR';
END
ELSE
BEGIN
    INSERT INTO dbo.WebModules(ModuleKey, ArabicCaption, EnglishCaption, AreaName, DisplayOrder, IsActive)
    VALUES (N'Shared.HR', N'شؤون الموظفين', N'Human Resources', N'Shared', 30, 1);
END;

SELECT @ModuleId = WebModuleId FROM dbo.WebModules WHERE ModuleKey = N'Shared.HR';

DECLARE @Screens TABLE
(
    ScreenKey NVARCHAR(160) NOT NULL,
    ArabicCaption NVARCHAR(200) NOT NULL,
    EnglishCaption NVARCHAR(200) NOT NULL,
    RouteUrl NVARCHAR(500) NOT NULL,
    ControllerName NVARCHAR(120) NOT NULL,
    ActionName NVARCHAR(120) NOT NULL,
    IconCss NVARCHAR(120) NOT NULL,
    DisplayOrder INT NOT NULL,
    CanAdd BIT NOT NULL,
    CanEdit BIT NOT NULL,
    CanDelete BIT NOT NULL,
    CanPrint BIT NOT NULL,
    CanExport BIT NOT NULL
);

INSERT INTO @Screens VALUES
(N'HR.Employees', N'الموظفون', N'Employees', N'/MainErp/EmployeePayroll/Employees', N'EmployeePayroll', N'Employees', N'fas fa-users', 10, 1, 1, 0, 1, 1),
(N'HR.Advances', N'السلف', N'Employee advances', N'/MainErp/Hr/Advances', N'Hr', N'Advances', N'fas fa-hand-holding-usd', 20, 0, 0, 0, 1, 1),
(N'HR.PayrollItems', N'المفردات', N'Payroll items', N'/MainErp/Hr/PayrollItems', N'Hr', N'PayrollItems', N'fas fa-list-alt', 30, 1, 1, 0, 1, 1),
(N'HR.Absences', N'الغياب', N'Employee absence', N'/MainErp/Hr/Absences', N'Hr', N'Absences', N'fas fa-user-clock', 40, 0, 0, 0, 1, 1),
(N'HR.Vacations', N'الإجازات', N'Vacations', N'/MainErp/Hr/Vacations', N'Hr', N'Vacations', N'fas fa-calendar-check', 50, 0, 0, 0, 1, 1),
(N'HR.Allowances', N'البدلات', N'Allowances', N'/MainErp/Hr/Allowances', N'Hr', N'Allowances', N'fas fa-plus-circle', 60, 0, 0, 0, 1, 1),
(N'HR.EndOfService', N'نهاية الخدمة', N'End of service', N'/MainErp/Hr/EndOfService', N'Hr', N'EndOfService', N'fas fa-user-minus', 70, 0, 0, 0, 1, 1),
(N'HR.SalaryRun', N'مسير الرواتب', N'Salary run', N'/MainErp/EmployeePayroll/SalaryRun', N'EmployeePayroll', N'SalaryRun', N'fas fa-file-invoice-dollar', 80, 1, 1, 0, 1, 1),
(N'HR.MedicalInsurance', N'التأمين الطبي', N'Medical insurance', N'/MainErp/EmployeePayroll/MedicalInsurance', N'EmployeePayroll', N'MedicalInsurance', N'fas fa-briefcase-medical', 90, 1, 1, 0, 1, 1),
(N'Shared.WebPermissions', N'صلاحيات شاشات الويب', N'Web screen permissions', N'/MainErp/Permissions', N'Permissions', N'Index', N'fas fa-user-shield', 900, 1, 1, 1, 1, 1);

INSERT INTO @Screens VALUES
(N'HR.ChangedComponentData', N'تسجيل المفردات المتغيرة', N'Variable salary components', N'/MainErp/Hr/ChangedComponentData', N'Hr', N'ChangedComponentData', N'fas fa-exchange-alt', 35, 1, 1, 1, 1, 1),
(N'FrmChangedComponentData', N'تسجيل المفردات المتغيرة - صلاحية VB6', N'VB6 variable salary components fallback', N'/MainErp/Hr/ChangedComponentData', N'Hr', N'ChangedComponentData', N'fas fa-exchange-alt', 36, 1, 1, 1, 1, 1);

UPDATE s
   SET s.WebModuleId = @ModuleId,
       s.ArabicCaption = src.ArabicCaption,
       s.EnglishCaption = src.EnglishCaption,
       s.RouteUrl = src.RouteUrl,
       s.ControllerName = src.ControllerName,
       s.ActionName = src.ActionName,
       s.IconCss = src.IconCss,
       s.DisplayOrder = src.DisplayOrder,
       s.IsActive = 1,
       s.IsMenuVisible = 1,
       s.UpdatedAt = GETDATE()
FROM dbo.WebScreens s
INNER JOIN @Screens src ON src.ScreenKey = s.ScreenKey;

INSERT INTO dbo.WebScreens(WebModuleId, ScreenKey, ArabicCaption, EnglishCaption, RouteUrl, ControllerName, ActionName, IconCss, DisplayOrder, IsActive, IsMenuVisible)
SELECT @ModuleId, src.ScreenKey, src.ArabicCaption, src.EnglishCaption, src.RouteUrl, src.ControllerName, src.ActionName, src.IconCss, src.DisplayOrder, 1, 1
FROM @Screens src
WHERE NOT EXISTS (SELECT 1 FROM dbo.WebScreens s WHERE s.ScreenKey = src.ScreenKey);

INSERT INTO dbo.WebScreenPermissions(UserId, WebScreenId, CanView, CanAdd, CanEdit, CanDelete, CanPrint, CanExport, CanApprove, CreatedAt, UpdatedAt)
SELECT u.UserID, ws.WebScreenId, 1, src.CanAdd, src.CanEdit, src.CanDelete, src.CanPrint, src.CanExport, 0, GETDATE(), GETDATE()
FROM dbo.TblUsers u
INNER JOIN dbo.WebScreens ws ON ws.ScreenKey IN (SELECT ScreenKey FROM @Screens)
INNER JOIN @Screens src ON src.ScreenKey = ws.ScreenKey
WHERE (ISNULL(u.UserType, -1) = 0 OR ISNULL(u.FullPremis, 0) = 1)
  AND NOT EXISTS (SELECT 1 FROM dbo.WebScreenPermissions p WHERE p.UserId = u.UserID AND p.WebScreenId = ws.WebScreenId);
