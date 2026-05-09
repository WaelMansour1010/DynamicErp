using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using MyERP.Areas.Pos.Models;

namespace MyERP.Areas.Pos.Services
{
    public class PosLegacyScreenPermissionService
    {
        private readonly string _connectionString;

        public PosLegacyScreenPermissionService()
        {
            var setting = ConfigurationManager.ConnectionStrings["KishnyCashConnection"];
            if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
            {
                throw new ConfigurationErrorsException("Missing connection string: KishnyCashConnection");
            }

            _connectionString = setting.ConnectionString;
        }

        public bool CanView(PosUserContext context, string screenName)
        {
            if (context == null)
            {
                return false;
            }

            if (context.IsFullAccess || context.UserType.GetValueOrDefault(-1) == 0)
            {
                return true;
            }

            const string sql = @"
SELECT TOP (1) 1
FROM dbo.ScreenJuncUser
WHERE User_ID = @userId
  AND ScreenName = @screenName
  AND (
        ISNULL(FullAccess, 0) = 1
     OR ISNULL(CanShow, 0) = 1
     OR ISNULL(CanSearch, 0) = 1
  );";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@userId", SqlDbType.Int).Value = context.UserId;
                command.Parameters.Add("@screenName", SqlDbType.NVarChar, 255).Value = screenName ?? string.Empty;
                connection.Open();
                return command.ExecuteScalar() != null;
            }
        }

        public bool CanAdd(PosUserContext context, string screenName)
        {
            return CanUseFlag(context, screenName, "CanAdd");
        }

        public bool CanEdit(PosUserContext context, string screenName)
        {
            return CanUseFlag(context, screenName, "CanEdit");
        }

        public bool CanDelete(PosUserContext context, string screenName)
        {
            return CanUseFlag(context, screenName, "CanDelete");
        }

        public bool CanPrint(PosUserContext context, string screenName)
        {
            return CanUseFlag(context, screenName, "CanPrint");
        }

        private bool CanUseFlag(PosUserContext context, string screenName, string flagColumn)
        {
            if (context == null)
            {
                return false;
            }

            if (context.IsFullAccess || context.UserType.GetValueOrDefault(-1) == 0)
            {
                return true;
            }

            var sql = @"
SELECT TOP (1) 1
FROM dbo.ScreenJuncUser
WHERE User_ID = @userId
  AND ScreenName = @screenName
  AND (ISNULL(FullAccess, 0) = 1 OR ISNULL(" + flagColumn + @", 0) = 1);";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@userId", SqlDbType.Int).Value = context.UserId;
                command.Parameters.Add("@screenName", SqlDbType.NVarChar, 255).Value = screenName ?? string.Empty;
                connection.Open();
                return command.ExecuteScalar() != null;
            }
        }
    }
}
