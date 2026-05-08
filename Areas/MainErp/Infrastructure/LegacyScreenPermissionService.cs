using System.Data;
using System.Data.SqlClient;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.Models.Security;

namespace MyERP.Areas.MainErp.Infrastructure
{
    public class LegacyScreenPermissionService
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public LegacyScreenPermissionService(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public bool CanView(MainErpUserContext context, string screenName)
        {
            if (context == null)
            {
                return false;
            }

            if (context.IsAdmin || context.UserType.GetValueOrDefault(-1) == 0)
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

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@userId", SqlDbType.Int).Value = context.UserId;
                command.Parameters.Add("@screenName", SqlDbType.NVarChar, 255).Value = screenName ?? string.Empty;
                return command.ExecuteScalar() != null;
            }
        }

        public bool CanAdd(MainErpUserContext context, string screenName)
        {
            return CanUseFlag(context, screenName, "CanAdd");
        }

        public bool CanEdit(MainErpUserContext context, string screenName)
        {
            return CanUseFlag(context, screenName, "CanEdit");
        }

        public bool CanDelete(MainErpUserContext context, string screenName)
        {
            return CanUseFlag(context, screenName, "CanDelete");
        }

        public bool CanPrint(MainErpUserContext context, string screenName)
        {
            return CanUseFlag(context, screenName, "CanPrint");
        }

        private bool CanUseFlag(MainErpUserContext context, string screenName, string flagColumn)
        {
            if (context == null)
            {
                return false;
            }

            if (context.IsAdmin || context.UserType.GetValueOrDefault(-1) == 0)
            {
                return true;
            }

            var sql = @"
SELECT TOP (1) 1
FROM dbo.ScreenJuncUser
WHERE User_ID = @userId
  AND ScreenName = @screenName
  AND (ISNULL(FullAccess, 0) = 1 OR ISNULL(" + flagColumn + @", 0) = 1);";

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@userId", SqlDbType.Int).Value = context.UserId;
                command.Parameters.Add("@screenName", SqlDbType.NVarChar, 255).Value = screenName ?? string.Empty;
                return command.ExecuteScalar() != null;
            }
        }
    }
}
