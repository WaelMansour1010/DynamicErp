using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.Models.Security;
using MyERP.Areas.MainErp.Security;
using MyERP.Areas.MainErp.ViewModels.Security;
using MyERP.Areas.Pos.Controllers;
using MyERP.Areas.Pos.Models;

namespace MyERP.Areas.MainErp.Infrastructure
{
    public class WebScreenPermissionService
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public WebScreenPermissionService()
            : this(new MainErpDbConnectionFactory())
        {
        }

        public WebScreenPermissionService(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public bool Can(string screenKey, string actionKey)
        {
            var mainContext = HttpContext.Current == null || HttpContext.Current.Session == null
                ? null
                : HttpContext.Current.Session[MainErpSessionKeys.Context] as MainErpUserContext;
            if (mainContext != null)
            {
                return Can(mainContext, screenKey, actionKey);
            }

            var posContext = HttpContext.Current == null || HttpContext.Current.Session == null
                ? null
                : HttpContext.Current.Session[PosLoginController.PosContextSessionKey] as PosUserContext;
            return Can(posContext, screenKey, actionKey);
        }

        public bool Can(MainErpUserContext context, string screenKey, string actionKey)
        {
            if (context == null)
            {
                return false;
            }

            if (context.IsAdmin || context.UserType.GetValueOrDefault(-1) == 0)
            {
                return true;
            }

            return CanUser(context.UserId, screenKey, actionKey);
        }

        public bool Can(PosUserContext context, string screenKey, string actionKey)
        {
            if (context == null)
            {
                return false;
            }

            if (context.IsFullAccess || context.UserType.GetValueOrDefault(-1) == 0)
            {
                return true;
            }

            return CanUser(context.UserId, screenKey, actionKey);
        }

        public bool CanView(MainErpUserContext context, string screenKey)
        {
            return Can(context, screenKey, "View");
        }

        public bool CanView(PosUserContext context, string screenKey)
        {
            return Can(context, screenKey, "View");
        }

        public IList<WebPermissionLookupItem> GetModules(string areaScope, bool showAllAreas)
        {
            var modules = new List<WebPermissionLookupItem>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand(@"
SELECT WebModuleId, ModuleKey, ArabicCaption, AreaName
FROM dbo.WebModules
WHERE IsActive = 1
  AND AreaName = @AreaScope
ORDER BY DisplayOrder, ArabicCaption;", connection))
            {
                command.Parameters.Add("@AreaScope", SqlDbType.NVarChar, 50).Value = (object)areaScope ?? DBNull.Value;
                command.Parameters.Add("@ShowAll", SqlDbType.Bit).Value = showAllAreas;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        modules.Add(new WebPermissionLookupItem
                        {
                            Id = ReadInt(reader, "WebModuleId"),
                            Key = ReadString(reader, "ModuleKey"),
                            Name = ReadString(reader, "ArabicCaption"),
                            GroupName = ReadString(reader, "AreaName")
                        });
                    }
                }
            }

            return modules;
        }

        public IList<WebPermissionLookupItem> GetUsers()
        {
            var users = new List<WebPermissionLookupItem>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand(@"
IF COL_LENGTH(N'dbo.TblUsers', N'UserCategory') IS NULL
BEGIN
    SELECT TOP (500) UserID, UserName, CAST(NULL AS NVARCHAR(100)) AS UserCategory
    FROM dbo.TblUsers
    WHERE ISNULL(UserName, N'') <> N''
    ORDER BY UserName, UserID;
END
ELSE
BEGIN
    EXEC(N'SELECT TOP (500) UserID, UserName, CONVERT(NVARCHAR(100), UserCategory) AS UserCategory FROM dbo.TblUsers WHERE ISNULL(UserName, N'''') <> N'''' ORDER BY UserName, UserID;');
END", connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    users.Add(new WebPermissionLookupItem
                    {
                        Id = ReadInt(reader, "UserID"),
                        Name = ReadString(reader, "UserName"),
                            GroupName = ReadString(reader, "UserCategory")
                    });
                }
            }

            return users;
        }

        public IList<WebPermissionLookupItem> GetTemplates()
        {
            var templates = new List<WebPermissionLookupItem>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand(@"
IF OBJECT_ID(N'dbo.WebPermissionRoleTemplates', N'U') IS NOT NULL
BEGIN
    SELECT TemplateId, TemplateKey, ArabicCaption, AreaName
    FROM dbo.WebPermissionRoleTemplates
    WHERE IsActive = 1
    ORDER BY DisplayOrder, ArabicCaption;
END", connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    templates.Add(new WebPermissionLookupItem
                    {
                        Id = ReadInt(reader, "TemplateId"),
                        Key = ReadString(reader, "TemplateKey"),
                        Name = ReadString(reader, "ArabicCaption"),
                        GroupName = ReadString(reader, "AreaName")
                    });
                }
            }

            return templates;
        }

        public WebPermissionDashboardDto GetDashboard()
        {
            return GetDashboard(null);
        }

        public WebPermissionDashboardDto GetDashboard(string areaName)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand(@"
SELECT
    (SELECT COUNT(1) FROM dbo.TblUsers WHERE ISNULL(UserName, N'') <> N'') AS ActiveUsers,
    (SELECT COUNT(1)
       FROM dbo.WebScreens s
       INNER JOIN dbo.WebModules m ON m.WebModuleId = s.WebModuleId
      WHERE s.IsActive = 1
        AND (@AreaName = N'' OR m.AreaName = @AreaName)) AS WebScreens,
    (SELECT COUNT(1)
       FROM dbo.WebScreenPermissions p
       INNER JOIN dbo.WebScreens s ON s.WebScreenId = p.WebScreenId
       INNER JOIN dbo.WebModules m ON m.WebModuleId = s.WebModuleId
      WHERE (p.CanView = 1 OR p.CanAdd = 1 OR p.CanEdit = 1 OR p.CanDelete = 1 OR p.CanPrint = 1 OR p.CanExport = 1 OR p.CanApprove = 1)
        AND (@AreaName = N'' OR m.AreaName = @AreaName)) AS GrantedPermissions,
    (SELECT COUNT(1)
       FROM dbo.TblUsers u
      WHERE ISNULL(u.UserName, N'') <> N''
        AND NOT EXISTS (
            SELECT 1
              FROM dbo.WebScreenPermissions p
              INNER JOIN dbo.WebScreens s ON s.WebScreenId = p.WebScreenId
              INNER JOIN dbo.WebModules m ON m.WebModuleId = s.WebModuleId
             WHERE p.UserId = u.UserID
               AND (p.CanView = 1 OR p.CanAdd = 1 OR p.CanEdit = 1 OR p.CanDelete = 1 OR p.CanPrint = 1 OR p.CanExport = 1 OR p.CanApprove = 1)
               AND (@AreaName = N'' OR m.AreaName = @AreaName))) AS UsersWithoutPermissions;", connection))
            {
                command.Parameters.Add("@AreaName", SqlDbType.NVarChar, 50).Value = EmptyToString(areaName);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return new WebPermissionDashboardDto();
                    }

                    return new WebPermissionDashboardDto
                    {
                        ActiveUsers = ReadInt(reader, "ActiveUsers"),
                        WebScreens = ReadInt(reader, "WebScreens"),
                        GrantedPermissions = ReadInt(reader, "GrantedPermissions"),
                        UsersWithoutPermissions = ReadInt(reader, "UsersWithoutPermissions")
                    };
                }
            }
        }

        public WebPermissionMatrixResponse Search(WebPermissionMatrixRequest request)
        {
            request = request ?? new WebPermissionMatrixRequest();
            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 || request.PageSize > 200 ? 80 : request.PageSize;
            var response = new WebPermissionMatrixResponse { Success = true, Page = page, PageSize = pageSize, Dashboard = GetDashboard(request.AreaName) };

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand(@"
DECLARE @Offset INT = (@Page - 1) * @PageSize;

;WITH Filtered AS
(
    SELECT
        s.WebScreenId,
        s.WebModuleId,
        m.ModuleKey,
        m.AreaName,
        s.ScreenKey,
        s.ArabicCaption,
        s.EnglishCaption,
        s.RouteUrl,
        s.ControllerName,
        s.ActionName,
        s.IconCss,
        s.DisplayOrder,
        s.IsActive,
        s.IsMenuVisible,
        p.CanView,
        p.CanAdd,
        p.CanEdit,
        p.CanDelete,
        p.CanPrint,
        p.CanExport,
        p.CanApprove,
        ROW_NUMBER() OVER (ORDER BY m.DisplayOrder, s.DisplayOrder, s.ArabicCaption, s.ScreenKey) AS RowNo,
        COUNT(1) OVER() AS TotalRows
    FROM dbo.WebScreens s
    INNER JOIN dbo.WebModules m ON m.WebModuleId = s.WebModuleId
    LEFT JOIN dbo.WebScreenPermissions p ON p.WebScreenId = s.WebScreenId AND p.UserId = @UserId
    WHERE s.IsActive = 1
      AND m.AreaName = @AreaName
      AND (@ModuleKey = N'' OR m.ModuleKey = @ModuleKey)
      AND (@SearchText = N'' OR s.ArabicCaption LIKE N'%' + @SearchText + N'%' OR s.EnglishCaption LIKE N'%' + @SearchText + N'%' OR s.RouteUrl LIKE N'%' + @SearchText + N'%' OR s.ScreenKey LIKE N'%' + @SearchText + N'%')
      AND (
            @PermissionFilter = N'all'
         OR (@PermissionFilter = N'granted' AND (ISNULL(p.CanView, 0) = 1 OR ISNULL(p.CanAdd, 0) = 1 OR ISNULL(p.CanEdit, 0) = 1 OR ISNULL(p.CanDelete, 0) = 1 OR ISNULL(p.CanPrint, 0) = 1 OR ISNULL(p.CanExport, 0) = 1 OR ISNULL(p.CanApprove, 0) = 1))
         OR (@PermissionFilter = N'missing' AND (p.WebPermissionId IS NULL OR (ISNULL(p.CanView, 0) = 0 AND ISNULL(p.CanAdd, 0) = 0 AND ISNULL(p.CanEdit, 0) = 0 AND ISNULL(p.CanDelete, 0) = 0 AND ISNULL(p.CanPrint, 0) = 0 AND ISNULL(p.CanExport, 0) = 0 AND ISNULL(p.CanApprove, 0) = 0)))
      )
)
SELECT *
FROM Filtered
WHERE RowNo BETWEEN @Offset + 1 AND @Offset + @PageSize
ORDER BY RowNo;", connection))
            {
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = request.UserId.GetValueOrDefault(0);
                command.Parameters.Add("@AreaName", SqlDbType.NVarChar, 50).Value = EmptyToDb(request.AreaName);
                command.Parameters.Add("@ModuleKey", SqlDbType.NVarChar, 80).Value = EmptyToString(request.ModuleKey);
                command.Parameters.Add("@SearchText", SqlDbType.NVarChar, 120).Value = EmptyToString(request.SearchText);
                command.Parameters.Add("@PermissionFilter", SqlDbType.NVarChar, 20).Value = string.IsNullOrWhiteSpace(request.PermissionFilter) ? "all" : request.PermissionFilter.Trim();
                command.Parameters.Add("@ShowAllAreas", SqlDbType.Bit).Value = request.ShowAllAreas;
                command.Parameters.Add("@Page", SqlDbType.Int).Value = page;
                command.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (response.TotalRows == 0)
                        {
                            response.TotalRows = ReadInt(reader, "TotalRows");
                        }

                        response.Screens.Add(ReadScreen(reader));
                    }
                }
            }

            if (request.ScreenId.GetValueOrDefault() > 0)
            {
                response.Users = GetScreenUsers(request.ScreenId.Value);
            }

            return response;
        }

        public void Save(int userId, IList<WebPermissionSaveItem> items)
        {
            if (userId <= 0)
            {
                throw new InvalidOperationException("Select a user first.");
            }

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                foreach (var item in items ?? new List<WebPermissionSaveItem>())
                {
                    using (var command = new SqlCommand(@"
IF EXISTS (SELECT 1 FROM dbo.WebScreenPermissions WHERE UserId = @UserId AND WebScreenId = @WebScreenId)
BEGIN
    UPDATE dbo.WebScreenPermissions
       SET CanView = @CanView,
           CanAdd = @CanAdd,
           CanEdit = @CanEdit,
           CanDelete = @CanDelete,
           CanPrint = @CanPrint,
           CanExport = @CanExport,
           CanApprove = @CanApprove,
           SeedSource = N'Manual',
           UpdatedAt = GETDATE()
     WHERE UserId = @UserId AND WebScreenId = @WebScreenId;
END
ELSE
BEGIN
    INSERT INTO dbo.WebScreenPermissions
    (UserId, WebScreenId, CanView, CanAdd, CanEdit, CanDelete, CanPrint, CanExport, CanApprove, SeedSource, CreatedAt, UpdatedAt)
    VALUES
    (@UserId, @WebScreenId, @CanView, @CanAdd, @CanEdit, @CanDelete, @CanPrint, @CanExport, @CanApprove, N'Manual', GETDATE(), GETDATE());
END", connection, transaction))
                    {
                        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                        command.Parameters.Add("@WebScreenId", SqlDbType.Int).Value = item.WebScreenId;
                        AddFlags(command, item);
                        command.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
            }
        }

        public int CopyPermissions(int sourceUserId, int targetUserId, string areaName)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand(@"
MERGE dbo.WebScreenPermissions AS target
USING
(
    SELECT p.WebScreenId, p.CanView, p.CanAdd, p.CanEdit, p.CanDelete, p.CanPrint, p.CanExport, p.CanApprove
    FROM dbo.WebScreenPermissions p
    INNER JOIN dbo.WebScreens s ON s.WebScreenId = p.WebScreenId
    INNER JOIN dbo.WebModules m ON m.WebModuleId = s.WebModuleId
    WHERE p.UserId = @SourceUserId
      AND m.AreaName = @AreaName
) AS source
ON target.UserId = @TargetUserId AND target.WebScreenId = source.WebScreenId
WHEN MATCHED THEN UPDATE SET
    CanView = source.CanView,
    CanAdd = source.CanAdd,
    CanEdit = source.CanEdit,
    CanDelete = source.CanDelete,
    CanPrint = source.CanPrint,
    CanExport = source.CanExport,
    CanApprove = source.CanApprove,
    SeedSource = N'ManualCopy',
    UpdatedAt = GETDATE()
WHEN NOT MATCHED THEN INSERT
    (UserId, WebScreenId, CanView, CanAdd, CanEdit, CanDelete, CanPrint, CanExport, CanApprove, SeedSource, CreatedAt, UpdatedAt)
    VALUES
    (@TargetUserId, source.WebScreenId, source.CanView, source.CanAdd, source.CanEdit, source.CanDelete, source.CanPrint, source.CanExport, source.CanApprove, N'ManualCopy', GETDATE(), GETDATE());", connection))
            {
                command.Parameters.Add("@SourceUserId", SqlDbType.Int).Value = sourceUserId;
                command.Parameters.Add("@TargetUserId", SqlDbType.Int).Value = targetUserId;
                command.Parameters.Add("@AreaName", SqlDbType.NVarChar, 50).Value = EmptyToString(areaName);
                return command.ExecuteNonQuery();
            }
        }

        public int ApplyTemplate(int templateId, int userId)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand(@"
MERGE dbo.WebScreenPermissions AS target
USING
(
    SELECT WebScreenId, CanView, CanAdd, CanEdit, CanDelete, CanPrint, CanExport, CanApprove
    FROM dbo.WebPermissionRoleTemplateItems
    WHERE TemplateId = @TemplateId
) AS source
ON target.UserId = @UserId AND target.WebScreenId = source.WebScreenId
WHEN MATCHED THEN UPDATE SET
    CanView = source.CanView,
    CanAdd = source.CanAdd,
    CanEdit = source.CanEdit,
    CanDelete = source.CanDelete,
    CanPrint = source.CanPrint,
    CanExport = source.CanExport,
    CanApprove = source.CanApprove,
    SeedSource = N'RoleTemplate',
    UpdatedAt = GETDATE()
WHEN NOT MATCHED THEN INSERT
    (UserId, WebScreenId, CanView, CanAdd, CanEdit, CanDelete, CanPrint, CanExport, CanApprove, SeedSource, CreatedAt, UpdatedAt)
    VALUES
    (@UserId, source.WebScreenId, source.CanView, source.CanAdd, source.CanEdit, source.CanDelete, source.CanPrint, source.CanExport, source.CanApprove, N'RoleTemplate', GETDATE(), GETDATE());", connection))
            {
                command.Parameters.Add("@TemplateId", SqlDbType.Int).Value = templateId;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                return command.ExecuteNonQuery();
            }
        }

        public int ApplyBulk(WebPermissionBulkApplyRequest request)
        {
            if (request == null || request.UserIds == null || request.UserIds.Count == 0)
            {
                throw new InvalidOperationException("Select at least one user.");
            }

            var mode = (request.Mode ?? string.Empty).Trim().ToLowerInvariant();
            if (mode != "full" && mode != "view" && mode != "nodelete" && mode != "clear")
            {
                throw new InvalidOperationException("Invalid bulk permission mode.");
            }

            var flags = BulkFlags(mode);
            var affected = 0;
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                foreach (var userId in request.UserIds.Where(id => id > 0).Distinct())
                {
                    using (var command = new SqlCommand(@"
MERGE dbo.WebScreenPermissions AS target
USING
(
    SELECT s.WebScreenId
    FROM dbo.WebScreens s
    INNER JOIN dbo.WebModules m ON m.WebModuleId = s.WebModuleId
    WHERE s.IsActive = 1
      AND m.AreaName = @AreaName
      AND (@ModuleKey = N'' OR m.ModuleKey = @ModuleKey)
      AND (@WebScreenId IS NULL OR s.WebScreenId = @WebScreenId)
) AS source
ON target.UserId = @UserId AND target.WebScreenId = source.WebScreenId
WHEN MATCHED THEN UPDATE SET
    CanView = @CanView,
    CanAdd = @CanAdd,
    CanEdit = @CanEdit,
    CanDelete = @CanDelete,
    CanPrint = @CanPrint,
    CanExport = @CanExport,
    CanApprove = @CanApprove,
    SeedSource = N'Bulk',
    UpdatedAt = GETDATE()
WHEN NOT MATCHED THEN INSERT
    (UserId, WebScreenId, CanView, CanAdd, CanEdit, CanDelete, CanPrint, CanExport, CanApprove, SeedSource, CreatedAt, UpdatedAt)
    VALUES
    (@UserId, source.WebScreenId, @CanView, @CanAdd, @CanEdit, @CanDelete, @CanPrint, @CanExport, @CanApprove, N'Bulk', GETDATE(), GETDATE());", connection, transaction))
                    {
                        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                        command.Parameters.Add("@AreaName", SqlDbType.NVarChar, 50).Value = EmptyToString(request.AreaName);
                        command.Parameters.Add("@ModuleKey", SqlDbType.NVarChar, 80).Value = EmptyToString(request.ModuleKey);
                        command.Parameters.Add("@WebScreenId", SqlDbType.Int).Value = request.WebScreenId.HasValue ? (object)request.WebScreenId.Value : DBNull.Value;
                        AddFlags(command, flags);
                        affected += command.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
            }

            return affected;
        }

        public DataTable ExportMatrix(int userId, string areaName, bool showAllAreas)
        {
            var table = new DataTable("WebScreenPermissions");
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand(@"
SELECT
    m.AreaName AS Area,
    m.ArabicCaption AS ModuleArabic,
    s.ArabicCaption AS ScreenArabic,
    s.EnglishCaption AS ScreenEnglish,
    s.ScreenKey,
    s.RouteUrl,
    ISNULL(p.CanView, 0) AS CanView,
    ISNULL(p.CanAdd, 0) AS CanAdd,
    ISNULL(p.CanEdit, 0) AS CanEdit,
    ISNULL(p.CanDelete, 0) AS CanDelete,
    ISNULL(p.CanPrint, 0) AS CanPrint,
    ISNULL(p.CanExport, 0) AS CanExport,
    ISNULL(p.CanApprove, 0) AS CanApprove
FROM dbo.WebScreens s
INNER JOIN dbo.WebModules m ON m.WebModuleId = s.WebModuleId
LEFT JOIN dbo.WebScreenPermissions p ON p.WebScreenId = s.WebScreenId AND p.UserId = @UserId
WHERE s.IsActive = 1
  AND m.AreaName = @AreaName
ORDER BY m.DisplayOrder, s.DisplayOrder, s.ArabicCaption;", connection))
            {
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@AreaName", SqlDbType.NVarChar, 50).Value = EmptyToDb(areaName);
                command.Parameters.Add("@ShowAllAreas", SqlDbType.Bit).Value = showAllAreas;
                using (var adapter = new SqlDataAdapter(command))
                {
                    adapter.Fill(table);
                }
            }

            return table;
        }

        private bool CanUser(int userId, string screenKey, string actionKey)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(screenKey))
            {
                return false;
            }

            var column = ActionToColumn(actionKey);
            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection())
                using (var command = new SqlCommand(@"
SELECT TOP (1) p." + column + @"
FROM dbo.WebScreenPermissions p
INNER JOIN dbo.WebScreens s ON s.WebScreenId = p.WebScreenId
WHERE p.UserId = @UserId
  AND s.ScreenKey = @ScreenKey
  AND s.IsActive = 1;", connection))
                {
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@ScreenKey", SqlDbType.NVarChar, 160).Value = screenKey.Trim();
                try
                {
                    var result = command.ExecuteScalar();
                    return result != null && result != DBNull.Value && Convert.ToBoolean(result);
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 208)
                    {
                        return false;
                    }

                    throw;
                }
            }
        }
            catch (SqlException)
            {
                return false;
            }
        }

        private IList<WebPermissionUserAccessDto> GetScreenUsers(int screenId)
        {
            var users = new List<WebPermissionUserAccessDto>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand(@"
SELECT TOP (500)
    u.UserID,
    u.UserName,
    CAST(NULL AS NVARCHAR(100)) AS UserCategory,
    p.CanView,
    p.CanAdd,
    p.CanEdit,
    p.CanDelete,
    p.CanPrint,
    p.CanExport,
    p.CanApprove
FROM dbo.TblUsers u
INNER JOIN dbo.WebScreenPermissions p ON p.UserId = u.UserID
WHERE p.WebScreenId = @WebScreenId
  AND (p.CanView = 1 OR p.CanAdd = 1 OR p.CanEdit = 1 OR p.CanDelete = 1 OR p.CanPrint = 1 OR p.CanExport = 1 OR p.CanApprove = 1)
ORDER BY u.UserName, u.UserID;", connection))
            {
                command.Parameters.Add("@WebScreenId", SqlDbType.Int).Value = screenId;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new WebPermissionUserAccessDto
                        {
                            UserId = ReadInt(reader, "UserID"),
                            UserName = ReadString(reader, "UserName"),
                            UserCategory = ReadString(reader, "UserCategory"),
                            Permissions = ReadFlags(reader)
                        });
                    }
                }
            }

            return users;
        }

        private static WebPermissionScreenDto ReadScreen(IDataRecord reader)
        {
            return new WebPermissionScreenDto
            {
                WebScreenId = ReadInt(reader, "WebScreenId"),
                WebModuleId = ReadInt(reader, "WebModuleId"),
                ModuleKey = ReadString(reader, "ModuleKey"),
                AreaName = ReadString(reader, "AreaName"),
                ScreenKey = ReadString(reader, "ScreenKey"),
                ArabicCaption = ReadString(reader, "ArabicCaption"),
                EnglishCaption = ReadString(reader, "EnglishCaption"),
                RouteUrl = ReadString(reader, "RouteUrl"),
                ControllerName = ReadString(reader, "ControllerName"),
                ActionName = ReadString(reader, "ActionName"),
                IconCss = ReadString(reader, "IconCss"),
                DisplayOrder = ReadInt(reader, "DisplayOrder"),
                IsActive = ReadBool(reader, "IsActive"),
                IsMenuVisible = ReadBool(reader, "IsMenuVisible"),
                Permissions = ReadFlags(reader)
            };
        }

        private static WebPermissionFlags ReadFlags(IDataRecord reader)
        {
            return new WebPermissionFlags
            {
                CanView = ReadBool(reader, "CanView"),
                CanAdd = ReadBool(reader, "CanAdd"),
                CanEdit = ReadBool(reader, "CanEdit"),
                CanDelete = ReadBool(reader, "CanDelete"),
                CanPrint = ReadBool(reader, "CanPrint"),
                CanExport = ReadBool(reader, "CanExport"),
                CanApprove = ReadBool(reader, "CanApprove")
            };
        }

        private static void AddFlags(SqlCommand command, WebPermissionFlags item)
        {
            command.Parameters.Add("@CanView", SqlDbType.Bit).Value = item.CanView;
            command.Parameters.Add("@CanAdd", SqlDbType.Bit).Value = item.CanAdd;
            command.Parameters.Add("@CanEdit", SqlDbType.Bit).Value = item.CanEdit;
            command.Parameters.Add("@CanDelete", SqlDbType.Bit).Value = item.CanDelete;
            command.Parameters.Add("@CanPrint", SqlDbType.Bit).Value = item.CanPrint;
            command.Parameters.Add("@CanExport", SqlDbType.Bit).Value = item.CanExport;
            command.Parameters.Add("@CanApprove", SqlDbType.Bit).Value = item.CanApprove;
        }

        private static WebPermissionFlags BulkFlags(string mode)
        {
            if (mode == "clear")
            {
                return new WebPermissionFlags();
            }

            if (mode == "view")
            {
                return new WebPermissionFlags { CanView = true };
            }

            if (mode == "nodelete")
            {
                return new WebPermissionFlags
                {
                    CanView = true,
                    CanAdd = true,
                    CanEdit = true,
                    CanPrint = true,
                    CanExport = true,
                    CanApprove = true
                };
            }

            return new WebPermissionFlags
            {
                CanView = true,
                CanAdd = true,
                CanEdit = true,
                CanDelete = true,
                CanPrint = true,
                CanExport = true,
                CanApprove = true
            };
        }

        private static string ActionToColumn(string actionKey)
        {
            switch ((actionKey ?? "View").Trim().ToLowerInvariant())
            {
                case "add": return "CanAdd";
                case "edit": return "CanEdit";
                case "delete": return "CanDelete";
                case "print": return "CanPrint";
                case "export": return "CanExport";
                case "approve": return "CanApprove";
                default: return "CanView";
            }
        }

        private static object EmptyToDb(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim();
        }

        private static string EmptyToString(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string ReadString(IDataRecord record, string name)
        {
            var ordinal = record.GetOrdinal(name);
            return record.IsDBNull(ordinal) ? null : Convert.ToString(record.GetValue(ordinal));
        }

        private static int ReadInt(IDataRecord record, string name)
        {
            var ordinal = record.GetOrdinal(name);
            return record.IsDBNull(ordinal) ? 0 : Convert.ToInt32(record.GetValue(ordinal));
        }

        private static bool ReadBool(IDataRecord record, string name)
        {
            var ordinal = record.GetOrdinal(name);
            return !record.IsDBNull(ordinal) && Convert.ToBoolean(record.GetValue(ordinal));
        }

        private static bool HasColumn(IDataRecord record, string columnName)
        {
            for (var i = 0; i < record.FieldCount; i++)
            {
                if (string.Equals(record.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class WebScreenAuthorizeAttribute : ActionFilterAttribute
    {
        public string ScreenKey { get; private set; }
        public string ActionKey { get; private set; }

        public WebScreenAuthorizeAttribute(string screenKey, string actionKey)
        {
            ScreenKey = screenKey;
            ActionKey = string.IsNullOrWhiteSpace(actionKey) ? "View" : actionKey;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!new WebScreenPermissionService().Can(ScreenKey, ActionKey))
            {
                filterContext.Result = new HttpStatusCodeResult(403, "لا توجد صلاحية كافية لفتح هذه الشاشة أو تنفيذ هذا الإجراء.");
                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
