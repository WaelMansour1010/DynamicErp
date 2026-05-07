using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using MyERP.Areas.MainErp.Interfaces;

namespace MyERP.Areas.MainErp.Infrastructure
{
    public class MainErpDbConnectionFactory : IMainErpDbConnectionFactory
    {
        private readonly string _connectionStringName;

        public MainErpDbConnectionFactory()
            : this("MainErp_ConnectionString")
        {
        }

        public MainErpDbConnectionFactory(string connectionStringName)
        {
            _connectionStringName = connectionStringName;
        }

        public SqlConnection CreateOpenConnection()
        {
            var setting = ConfigurationManager.ConnectionStrings[_connectionStringName];
            if ((setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString)) && _connectionStringName == "MainErp_ConnectionString")
            {
                Trace.TraceWarning("MainErp_ConnectionString not found; falling back to MyERP_ConnectionString.");
                setting = ConfigurationManager.ConnectionStrings["MyERP_ConnectionString"];
            }

            if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
            {
                throw new ConfigurationErrorsException("Missing MainErp connection string: " + _connectionStringName);
            }

            var connectionString = _connectionStringName == "MainErp_ConnectionString"
                ? MainErpDebugDatabaseOverride.Apply(setting.ConnectionString)
                : setting.ConnectionString;

            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }
    }
}
