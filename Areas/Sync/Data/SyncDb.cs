using System;
using System.Configuration;
using System.Data.SqlClient;

namespace MyERP.Areas.Sync.Data
{
    public static class SyncDb
    {
        public static SqlConnection Open()
        {
            var connectionString = GetConnectionString();
            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        public static string TargetName
        {
            get
            {
                var builder = new SqlConnectionStringBuilder(GetConnectionString());
                return builder.DataSource + " / " + builder.InitialCatalog;
            }
        }

        private static string GetConnectionString()
        {
            var name = ConfigurationManager.AppSettings["Sync.ConnectionStringName"];
            if (String.IsNullOrWhiteSpace(name))
            {
                name = "SyncAdminConnection";
            }

            var entry = ConfigurationManager.ConnectionStrings[name]
                ?? ConfigurationManager.ConnectionStrings["MyERP_ConnectionString"]
                ?? ConfigurationManager.ConnectionStrings["MyErpConnectionString"];

            if (entry == null || String.IsNullOrWhiteSpace(entry.ConnectionString))
            {
                throw new ConfigurationErrorsException("Sync Admin requires SyncAdminConnection or Sync.ConnectionStringName.");
            }

            return entry.ConnectionString;
        }
    }
}
