using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using MyERP.Areas.Reports.Models;

namespace MyERP.Areas.Reports.Services
{
    public class ReportPermissionService
    {
        private readonly DynamicReportConnectionFactory _connectionFactory;

        public ReportPermissionService()
            : this(new DynamicReportConnectionFactory())
        {
        }

        public ReportPermissionService(DynamicReportConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public bool CanDesign(DynamicReportUserContext user)
        {
            return user != null && user.IsAdmin;
        }

        public bool CanView(DynamicReportUserContext user, DynamicReportDefinition definition)
        {
            return Resolve(definition, user).CanView;
        }

        public IList<DynamicReportPermission> ListPermissions(int reportId, DynamicReportUserContext user)
        {
            var list = new List<DynamicReportPermission>();
            const string sql = @"
SELECT PermissionId, ReportId, ProjectScope, UserId, RoleId, CanView, CanDesign, CanExport, CreatedAt
FROM dbo.DynamicReportPermissions
WHERE ReportId = @ReportId
  AND (ProjectScope = @ProjectScope OR ProjectScope = N'Shared')
ORDER BY ProjectScope, RoleId, UserId, PermissionId;";
            using (var connection = _connectionFactory.CreateOpenConnection(user.ProjectScope))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@ReportId", SqlDbType.Int).Value = reportId;
                command.Parameters.Add("@ProjectScope", SqlDbType.NVarChar, 20).Value = user.ProjectScope;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(ReadPermission(reader));
                    }
                }
            }

            foreach (var permission in list)
            {
                permission.DisplayName = ResolvePermissionDisplayName(user.ProjectScope, permission);
            }

            return list;
        }

        public DynamicReportPermission Grant(DynamicReportPermissionInput input, DynamicReportUserContext user)
        {
            ValidatePermissionInput(input, user);
            using (var connection = _connectionFactory.CreateOpenConnection(user.ProjectScope))
            using (var transaction = connection.BeginTransaction())
            {
                EnsureReportExists(connection, transaction, input.ReportId, input.ProjectScope);
                var existingId = FindExistingPermission(connection, transaction, input);
                if (existingId > 0)
                {
                    using (var update = new SqlCommand(@"
UPDATE dbo.DynamicReportPermissions
SET CanView = @CanView, CanDesign = @CanDesign, CanExport = @CanExport
WHERE PermissionId = @PermissionId;", connection, transaction))
                    {
                        AddFlagParameters(update, input);
                        update.Parameters.Add("@PermissionId", SqlDbType.Int).Value = existingId;
                        update.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (var insert = new SqlCommand(@"
INSERT INTO dbo.DynamicReportPermissions
(ReportId, ProjectScope, UserId, RoleId, CanView, CanDesign, CanExport, CreatedAt)
VALUES (@ReportId, @ProjectScope, @UserId, @RoleId, @CanView, @CanDesign, @CanExport, GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);", connection, transaction))
                    {
                        AddIdentityParameters(insert, input);
                        AddFlagParameters(insert, input);
                        existingId = Convert.ToInt32(insert.ExecuteScalar());
                    }
                }

                transaction.Commit();
                return GetPermissionById(existingId, user);
            }
        }

        public void Revoke(int permissionId, DynamicReportUserContext user)
        {
            using (var connection = _connectionFactory.CreateOpenConnection(user.ProjectScope))
            using (var transaction = connection.BeginTransaction())
            using (var command = new SqlCommand(@"
DELETE FROM dbo.DynamicReportPermissions
WHERE PermissionId = @PermissionId
  AND (ProjectScope = @ProjectScope OR ProjectScope = N'Shared');", connection, transaction))
            {
                command.Parameters.Add("@PermissionId", SqlDbType.Int).Value = permissionId;
                command.Parameters.Add("@ProjectScope", SqlDbType.NVarChar, 20).Value = user.ProjectScope;
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public EffectivePermission Resolve(DynamicReportDefinition definition, DynamicReportUserContext user)
        {
            var result = new EffectivePermission { Source = "None" };
            if (user == null || definition == null || !ScopeMatches(user.ProjectScope, definition.ProjectScope))
            {
                return result;
            }

            if (user.IsAdmin)
            {
                result.CanView = true;
                result.CanDesign = true;
                result.CanExport = true;
                result.Source = "Admin";
                return result;
            }

            if (!definition.IsActive)
            {
                return result;
            }

            const string sql = @"
SELECT UserId, RoleId, CanView, CanDesign, CanExport
FROM dbo.DynamicReportPermissions
WHERE ReportId = @ReportId
  AND (ProjectScope = @ProjectScope OR ProjectScope = N'Shared')
  AND (UserId = @UserId OR (@RoleId IS NOT NULL AND RoleId = @RoleId))
ORDER BY CASE WHEN UserId = @UserId THEN 0 ELSE 1 END;";
            using (var connection = _connectionFactory.CreateOpenConnection(user.ProjectScope))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@ReportId", SqlDbType.Int).Value = definition.ReportId;
                command.Parameters.Add("@ProjectScope", SqlDbType.NVarChar, 20).Value = user.ProjectScope;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = user.UserId;
                command.Parameters.Add("@RoleId", SqlDbType.Int).Value = (object)user.RoleId ?? DBNull.Value;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.CanView = result.CanView || Convert.ToBoolean(reader["CanView"]);
                        result.CanDesign = result.CanDesign || Convert.ToBoolean(reader["CanDesign"]);
                        result.CanExport = result.CanExport || Convert.ToBoolean(reader["CanExport"]);
                        if (reader["UserId"] != DBNull.Value)
                        {
                            result.Source = "Direct";
                        }
                        else if (result.Source == "None")
                        {
                            result.Source = "Role:" + Convert.ToString(reader["RoleId"]);
                        }
                    }

                    return result;
                }
            }
        }

        public IList<KeyValuePair<int, string>> ListRoles(DynamicReportUserContext user)
        {
            if (user.ProjectScope == DynamicReportScopes.Web) return ListWebRoles(user.ProjectScope);
            return ListLegacyUserTypes(user.ProjectScope);
        }

        public IList<KeyValuePair<int, string>> ListUsersLite(DynamicReportUserContext user, string q)
        {
            return user.ProjectScope == DynamicReportScopes.Web
                ? ListWebUsers(user.ProjectScope, q)
                : ListLegacyUsers(user.ProjectScope, q);
        }

        private DynamicReportPermission GetPermissionById(int permissionId, DynamicReportUserContext user)
        {
            const string sql = @"
SELECT TOP (1) PermissionId, ReportId, ProjectScope, UserId, RoleId, CanView, CanDesign, CanExport, CreatedAt
FROM dbo.DynamicReportPermissions
WHERE PermissionId = @PermissionId;";
            using (var connection = _connectionFactory.CreateOpenConnection(user.ProjectScope))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@PermissionId", SqlDbType.Int).Value = permissionId;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    var permission = ReadPermission(reader);
                    permission.DisplayName = ResolvePermissionDisplayName(user.ProjectScope, permission);
                    return permission;
                }
            }
        }

        private static DynamicReportPermission ReadPermission(IDataRecord reader)
        {
            return new DynamicReportPermission
            {
                PermissionId = Convert.ToInt32(reader["PermissionId"]),
                ReportId = Convert.ToInt32(reader["ReportId"]),
                ProjectScope = Convert.ToString(reader["ProjectScope"]),
                UserId = reader["UserId"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["UserId"]),
                RoleId = reader["RoleId"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["RoleId"]),
                CanView = Convert.ToBoolean(reader["CanView"]),
                CanDesign = Convert.ToBoolean(reader["CanDesign"]),
                CanExport = Convert.ToBoolean(reader["CanExport"]),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
            };
        }

        private string ResolvePermissionDisplayName(string scope, DynamicReportPermission permission)
        {
            if (permission.UserId.HasValue) return "User: " + ResolveUserName(scope, permission.UserId.Value);
            if (permission.RoleId.HasValue) return "Role: " + ResolveRoleName(scope, permission.RoleId.Value);
            return "Unknown";
        }

        private static void ValidatePermissionInput(DynamicReportPermissionInput input, DynamicReportUserContext user)
        {
            if (input == null) throw new InvalidOperationException("بيانات الصلاحية غير صحيحة.");
            input.ProjectScope = DynamicReportScopes.Normalize(input.ProjectScope);
            if (input.ReportId <= 0) throw new InvalidOperationException("اختر تقريرًا صحيحًا.");
            if (!ScopeMatches(user.ProjectScope, input.ProjectScope)) throw new InvalidOperationException("نطاق الصلاحية غير متاح لهذا المستخدم.");
            if (input.UserId.HasValue == input.RoleId.HasValue) throw new InvalidOperationException("اختر مستخدمًا أو دورًا واحدًا فقط.");
            if (input.UserId.GetValueOrDefault() <= 0 || input.RoleId.GetValueOrDefault() <= 0) throw new InvalidOperationException("المعرف غير صحيح.");
            if (!input.CanView && !input.CanDesign && !input.CanExport) throw new InvalidOperationException("اختر صلاحية واحدة على الأقل.");
        }

        private static void EnsureReportExists(SqlConnection connection, SqlTransaction transaction, int reportId, string scope)
        {
            using (var command = new SqlCommand(@"
SELECT TOP (1) 1
FROM dbo.DynamicReportDefinitions
WHERE ReportId = @ReportId AND (ProjectScope = @ProjectScope OR ProjectScope = N'Shared');", connection, transaction))
            {
                command.Parameters.Add("@ReportId", SqlDbType.Int).Value = reportId;
                command.Parameters.Add("@ProjectScope", SqlDbType.NVarChar, 20).Value = scope;
                if (command.ExecuteScalar() == null) throw new InvalidOperationException("التقرير غير موجود في هذا النطاق.");
            }
        }

        private static int FindExistingPermission(SqlConnection connection, SqlTransaction transaction, DynamicReportPermissionInput input)
        {
            using (var command = new SqlCommand(@"
SELECT TOP (1) PermissionId
FROM dbo.DynamicReportPermissions
WHERE ReportId = @ReportId
  AND ProjectScope = @ProjectScope
  AND ((@UserId IS NOT NULL AND UserId = @UserId AND RoleId IS NULL)
       OR (@RoleId IS NOT NULL AND RoleId = @RoleId AND UserId IS NULL));", connection, transaction))
            {
                AddIdentityParameters(command, input);
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
            }
        }

        private static void AddIdentityParameters(SqlCommand command, DynamicReportPermissionInput input)
        {
            command.Parameters.Add("@ReportId", SqlDbType.Int).Value = input.ReportId;
            command.Parameters.Add("@ProjectScope", SqlDbType.NVarChar, 20).Value = input.ProjectScope;
            command.Parameters.Add("@UserId", SqlDbType.Int).Value = (object)input.UserId ?? DBNull.Value;
            command.Parameters.Add("@RoleId", SqlDbType.Int).Value = (object)input.RoleId ?? DBNull.Value;
        }

        private static void AddFlagParameters(SqlCommand command, DynamicReportPermissionInput input)
        {
            command.Parameters.Add("@CanView", SqlDbType.Bit).Value = input.CanView;
            command.Parameters.Add("@CanDesign", SqlDbType.Bit).Value = input.CanDesign;
            command.Parameters.Add("@CanExport", SqlDbType.Bit).Value = input.CanExport;
        }

        private IList<KeyValuePair<int, string>> ListWebRoles(string scope)
        {
            var list = new List<KeyValuePair<int, string>>();
            const string sql = @"SELECT TOP (100) Id, COALESCE(NULLIF(ArName, N''), NULLIF(EnName, N''), Code) AS Name FROM dbo.ERPRole WITH (NOLOCK) WHERE ISNULL(IsDeleted, 0) = 0 AND ISNULL(IsActive, 1) = 1 ORDER BY ArName, EnName, Id;";
            using (var connection = _connectionFactory.CreateOpenConnection(scope))
            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read()) list.Add(new KeyValuePair<int, string>(Convert.ToInt32(reader["Id"]), Convert.ToString(reader["Name"])));
            }
            return list;
        }

        private IList<KeyValuePair<int, string>> ListLegacyUserTypes(string scope)
        {
            var list = new List<KeyValuePair<int, string>>();
            const string sql = @"SELECT DISTINCT TOP (30) UserType FROM dbo.TblUsers WITH (NOLOCK) WHERE UserType IS NOT NULL AND UserType > 0 ORDER BY UserType;";
            using (var connection = _connectionFactory.CreateOpenConnection(scope))
            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = Convert.ToInt32(reader["UserType"]);
                    list.Add(new KeyValuePair<int, string>(id, "UserType " + id));
                }
            }
            return list;
        }

        private IList<KeyValuePair<int, string>> ListWebUsers(string scope, string q)
        {
            var list = new List<KeyValuePair<int, string>>();
            const string sql = @"SELECT TOP (30) Id, COALESCE(NULLIF(Name, N''), UserName) AS Name FROM dbo.ERPUser WITH (NOLOCK) WHERE (@q IS NULL OR UserName LIKE @like OR Name LIKE @like OR CONVERT(NVARCHAR(20), Id) LIKE @like) ORDER BY Name, UserName;";
            using (var connection = _connectionFactory.CreateOpenConnection(scope))
            using (var command = new SqlCommand(sql, connection))
            {
                AddSearchParameters(command, q);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read()) list.Add(new KeyValuePair<int, string>(Convert.ToInt32(reader["Id"]), Convert.ToString(reader["Name"])));
                }
            }
            return list;
        }

        private IList<KeyValuePair<int, string>> ListLegacyUsers(string scope, string q)
        {
            var list = new List<KeyValuePair<int, string>>();
            const string sql = @"SELECT TOP (30) UserID, UserName FROM dbo.TblUsers WITH (NOLOCK) WHERE (@q IS NULL OR UserName LIKE @like OR CONVERT(NVARCHAR(20), UserID) LIKE @like) ORDER BY UserName, UserID;";
            using (var connection = _connectionFactory.CreateOpenConnection(scope))
            using (var command = new SqlCommand(sql, connection))
            {
                AddSearchParameters(command, q);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read()) list.Add(new KeyValuePair<int, string>(Convert.ToInt32(reader["UserID"]), Convert.ToString(reader["UserName"])));
                }
            }
            return list;
        }

        private static void AddSearchParameters(SqlCommand command, string q)
        {
            var term = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
            command.Parameters.Add("@q", SqlDbType.NVarChar, 100).Value = (object)term ?? DBNull.Value;
            command.Parameters.Add("@like", SqlDbType.NVarChar, 120).Value = term == null ? (object)DBNull.Value : term + "%";
        }

        private string ResolveUserName(string scope, int userId)
        {
            try
            {
                var sql = scope == DynamicReportScopes.Web
                    ? @"SELECT TOP (1) COALESCE(NULLIF(Name, N''), UserName, CONVERT(NVARCHAR(20), Id)) FROM dbo.ERPUser WITH (NOLOCK) WHERE Id = @Id;"
                    : @"SELECT TOP (1) COALESCE(NULLIF(UserName, N''), CONVERT(NVARCHAR(20), UserID)) FROM dbo.TblUsers WITH (NOLOCK) WHERE UserID = @Id;";
                return ResolveName(scope, sql, userId, userId.ToString());
            }
            catch
            {
                return userId.ToString();
            }
        }

        private string ResolveRoleName(string scope, int roleId)
        {
            try
            {
                if (scope == DynamicReportScopes.Web)
                {
                    const string sql = @"SELECT TOP (1) COALESCE(NULLIF(ArName, N''), NULLIF(EnName, N''), Code, CONVERT(NVARCHAR(20), Id)) FROM dbo.ERPRole WITH (NOLOCK) WHERE Id = @Id;";
                    return ResolveName(scope, sql, roleId, roleId.ToString());
                }

                return "UserType " + roleId;
            }
            catch
            {
                return roleId.ToString();
            }
        }

        private string ResolveName(string scope, string sql, int id, string fallback)
        {
            using (var connection = _connectionFactory.CreateOpenConnection(scope))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value || string.IsNullOrWhiteSpace(Convert.ToString(value))
                    ? fallback
                    : Convert.ToString(value);
            }
        }

        private static bool ScopeMatches(string userScope, string reportScope)
        {
            return string.Equals(reportScope, DynamicReportScopes.Shared, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(userScope, reportScope, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(userScope, DynamicReportScopes.Shared, StringComparison.OrdinalIgnoreCase);
        }
    }
}
