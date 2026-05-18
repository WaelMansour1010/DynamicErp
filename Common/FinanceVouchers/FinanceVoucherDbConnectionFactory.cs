using System.Configuration;
using System.Data.SqlClient;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Interfaces;

namespace MyERP.Common.FinanceVouchers
{
    public class FinanceVoucherDbConnectionFactory : IMainErpDbConnectionFactory
    {
        private readonly string _connectionStringName;

        public FinanceVoucherDbConnectionFactory()
            : this(ResolveConnectionStringName())
        {
        }

        public FinanceVoucherDbConnectionFactory(string connectionStringName)
        {
            _connectionStringName = string.IsNullOrWhiteSpace(connectionStringName)
                ? MainErpDbConnectionFactory.DefaultConnectionStringName
                : connectionStringName.Trim();
        }

        public SqlConnection CreateOpenConnection()
        {
            var setting = ConfigurationManager.ConnectionStrings[_connectionStringName];
            if ((setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
                && _connectionStringName == MainErpDbConnectionFactory.DefaultConnectionStringName)
            {
                setting = ConfigurationManager.ConnectionStrings["MyERP_ConnectionString"];
            }

            if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
            {
                throw new ConfigurationErrorsException("Missing finance voucher connection string: " + _connectionStringName);
            }

            var connection = new SqlConnection(MainErpDebugDatabaseOverride.Apply(setting.ConnectionString));
            connection.Open();
            return connection;
        }

        private static string ResolveConnectionStringName()
        {
            var selected = MainErpDbConnectionFactory.GetSelectedDebugConnectionStringName();
            if (!string.IsNullOrWhiteSpace(selected))
            {
                return selected;
            }

            var configured = ConfigurationManager.AppSettings[MainErpDbConnectionFactory.ActiveConnectionStringNameKey];
            return string.IsNullOrWhiteSpace(configured)
                ? MainErpDbConnectionFactory.DefaultConnectionStringName
                : configured.Trim();
        }
    }
}
