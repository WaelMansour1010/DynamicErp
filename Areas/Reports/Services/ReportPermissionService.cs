using System;
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
            if (user == null || definition == null) return false;
            if (!ScopeMatches(user.ProjectScope, definition.ProjectScope)) return false;
            if (user.IsAdmin) return true;
            if (!definition.IsActive) return false;

            const string sql = @"
SELECT TOP (1) 1
FROM dbo.DynamicReportPermissions
WHERE ReportId = @ReportId
  AND (ProjectScope = @ProjectScope OR ProjectScope = N'Shared')
  AND CanView = 1
  AND (UserId = @UserId OR RoleId = @RoleId OR (UserId IS NULL AND RoleId IS NULL));";
            using (var connection = _connectionFactory.CreateOpenConnection(user.ProjectScope))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@ReportId", SqlDbType.Int).Value = definition.ReportId;
                command.Parameters.Add("@ProjectScope", SqlDbType.NVarChar, 20).Value = user.ProjectScope;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = user.UserId;
                command.Parameters.Add("@RoleId", SqlDbType.Int).Value = (object)user.RoleId ?? DBNull.Value;
                return command.ExecuteScalar() != null;
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
