using System.Configuration;
using System.Data.SqlClient;
using MyERP.Areas.Reports.Models;

namespace MyERP.Areas.Reports.Services
{
    public class DynamicReportConnectionFactory
    {
        public SqlConnection CreateOpenConnection(string projectScope)
        {
            var connection = new SqlConnection(ResolveConnectionString(projectScope));
            connection.Open();
            return connection;
        }

        private static string ResolveConnectionString(string projectScope)
        {
            var scope = DynamicReportScopes.Normalize(projectScope);
            if (scope == DynamicReportScopes.Pos)
            {
                return GetConnectionString("KishnyCashConnection");
            }

            if (scope == DynamicReportScopes.MainErp)
            {
                return GetConnectionString("MainErp_ConnectionString");
            }

            var direct = ConfigurationManager.ConnectionStrings["MyERP_ConnectionString"] ??
                         ConfigurationManager.ConnectionStrings["MyErpConnectionString"];
            if (direct != null && !string.IsNullOrWhiteSpace(direct.ConnectionString))
            {
                return direct.ConnectionString;
            }

            throw new ConfigurationErrorsException("Missing Dynamic Reports connection string.");
        }

        private static string GetConnectionString(string name)
        {
            var setting = ConfigurationManager.ConnectionStrings[name];
            if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
            {
                throw new ConfigurationErrorsException("Missing connection string: " + name);
            }

            return setting.ConnectionString;
        }
    }
}
