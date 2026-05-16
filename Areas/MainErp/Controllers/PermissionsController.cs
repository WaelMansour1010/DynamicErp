using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.ViewModels.Security;

namespace MyERP.Areas.MainErp.Controllers
{
    public class PermissionsController : MainErpControllerBase
    {
        private readonly MainErpDbConnectionFactory _connectionFactory;

        public PermissionsController()
            : this(new MainErpDbConnectionFactory())
        {
        }

        public PermissionsController(MainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public ActionResult Index(string searchText = "")
        {
            ViewBag.ActiveScreen = "permissions";
            var model = new MainErpPermissionsIndexViewModel
            {
                SearchText = (searchText ?? string.Empty).Trim(),
                IsAdminView = MainErpUserContext != null && MainErpUserContext.IsAdmin
            };

            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                ReadTotals(connection, model);
                ReadUserSummary(connection, model);
                ReadScreenMatrix(connection, model);
            }

            return View(model);
        }

        private void ReadTotals(SqlConnection connection, MainErpPermissionsIndexViewModel model)
        {
            using (var command = new SqlCommand(@"
SELECT
    COUNT(1) AS PermissionRows,
    COUNT(DISTINCT User_ID) AS UsersCount,
    COUNT(DISTINCT ScreenName) AS ScreensCount
FROM dbo.ScreenJuncUser
WHERE (@IsAdmin = 1 OR User_ID = @UserId);", connection))
            {
                AddScope(command, model);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        model.TotalPermissionRows = ReadInt(reader, "PermissionRows");
                        model.TotalUsers = ReadInt(reader, "UsersCount");
                        model.TotalScreens = ReadInt(reader, "ScreensCount");
                    }
                }
            }
        }

        private void ReadUserSummary(SqlConnection connection, MainErpPermissionsIndexViewModel model)
        {
            using (var command = new SqlCommand(@"
SELECT TOP (40)
    u.UserID,
    u.UserName,
    COUNT(DISTINCT j.ScreenName) AS ScreenCount,
    SUM(CASE WHEN ISNULL(j.FullAccess, 0) = 1 THEN 1 ELSE 0 END) AS FullAccessCount,
    SUM(CASE WHEN ISNULL(j.CanShow, 0) = 0 THEN 1 ELSE 0 END) AS HiddenCount
FROM dbo.ScreenJuncUser j
LEFT JOIN dbo.TblUsers u ON u.UserID = j.User_ID
WHERE (@IsAdmin = 1 OR j.User_ID = @UserId)
  AND (@SearchText = N'' OR u.UserName LIKE N'%' + @SearchText + N'%' OR j.ScreenName LIKE N'%' + @SearchText + N'%')
GROUP BY u.UserID, u.UserName
ORDER BY u.UserID;", connection))
            {
                AddScope(command, model);
                AddSearch(command, model.SearchText);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.Users.Add(new MainErpPermissionUserSummary
                        {
                            UserId = ReadInt(reader, "UserID"),
                            UserName = ReadString(reader, "UserName"),
                            ScreenCount = ReadInt(reader, "ScreenCount"),
                            FullAccessCount = ReadInt(reader, "FullAccessCount"),
                            HiddenCount = ReadInt(reader, "HiddenCount")
                        });
                    }
                }
            }
        }

        private void ReadScreenMatrix(SqlConnection connection, MainErpPermissionsIndexViewModel model)
        {
            using (var command = new SqlCommand(@"
SELECT TOP (120)
    j.ScreenName,
    MAX(s.Name) AS DisplayName,
    MAX(s.Mdiol) AS ModuleName,
    COUNT(DISTINCT j.User_ID) AS UsersCount,
    MAX(CASE WHEN ISNULL(j.CanShow, 0) = 1 THEN 1 ELSE 0 END) AS CanShow,
    MAX(CASE WHEN ISNULL(j.CanAdd, 0) = 1 THEN 1 ELSE 0 END) AS CanAdd,
    MAX(CASE WHEN ISNULL(j.CanEdit, 0) = 1 THEN 1 ELSE 0 END) AS CanEdit,
    MAX(CASE WHEN ISNULL(j.CanDelete, 0) = 1 THEN 1 ELSE 0 END) AS CanDelete,
    MAX(CASE WHEN ISNULL(j.CanPrint, 0) = 1 THEN 1 ELSE 0 END) AS CanPrint,
    MAX(CASE WHEN ISNULL(j.CanSearch, 0) = 1 THEN 1 ELSE 0 END) AS CanSearch,
    MAX(CASE WHEN ISNULL(j.FullAccess, 0) = 1 THEN 1 ELSE 0 END) AS FullAccess
FROM dbo.ScreenJuncUser j
LEFT JOIN dbo.TblUserScreen s ON s.Name = j.ScreenName
WHERE (@IsAdmin = 1 OR j.User_ID = @UserId)
  AND (@SearchText = N'' OR j.ScreenName LIKE N'%' + @SearchText + N'%' OR s.Name LIKE N'%' + @SearchText + N'%' OR s.Mdiol LIKE N'%' + @SearchText + N'%')
GROUP BY j.ScreenName
ORDER BY COALESCE(MAX(s.Mdiol), N''), COALESCE(MAX(s.Name), j.ScreenName), j.ScreenName;", connection))
            {
                AddScope(command, model);
                AddSearch(command, model.SearchText);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.Screens.Add(new MainErpPermissionScreenRow
                        {
                            ScreenName = ReadString(reader, "ScreenName"),
                            DisplayName = ReadString(reader, "DisplayName"),
                            ModuleName = ReadString(reader, "ModuleName"),
                            UsersCount = ReadInt(reader, "UsersCount"),
                            CanShow = ReadBool(reader, "CanShow"),
                            CanAdd = ReadBool(reader, "CanAdd"),
                            CanEdit = ReadBool(reader, "CanEdit"),
                            CanDelete = ReadBool(reader, "CanDelete"),
                            CanPrint = ReadBool(reader, "CanPrint"),
                            CanSearch = ReadBool(reader, "CanSearch"),
                            FullAccess = ReadBool(reader, "FullAccess")
                        });
                    }
                }
            }
        }

        private void AddScope(SqlCommand command, MainErpPermissionsIndexViewModel model)
        {
            command.Parameters.Add("@IsAdmin", SqlDbType.Bit).Value = model.IsAdminView;
            command.Parameters.Add("@UserId", SqlDbType.Int).Value = MainErpUserContext == null ? 0 : MainErpUserContext.UserId;
        }

        private static void AddSearch(SqlCommand command, string searchText)
        {
            command.Parameters.Add("@SearchText", SqlDbType.NVarChar, 100).Value = (searchText ?? string.Empty).Trim();
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
            return !record.IsDBNull(ordinal) && Convert.ToInt32(record.GetValue(ordinal)) != 0;
        }
    }
}
