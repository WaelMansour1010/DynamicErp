using MyERP.Areas.MainErp.Security;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web;

namespace MyERP.Areas.MainErp.Infrastructure
{
    public static class MainErpDebugDatabaseOverride
    {
        public static bool IsEnabled()
        {
#if DEBUG
            return HttpContext.Current != null
                && HttpContext.Current.Request != null
                && HttpContext.Current.Request.IsLocal;
#else
            return false;
#endif
        }

        public static string GetSelectedDatabaseName()
        {
            if (!IsEnabled() || HttpContext.Current == null || HttpContext.Current.Session == null)
            {
                return null;
            }

            return HttpContext.Current.Session[MainErpSessionKeys.DebugDatabaseName] as string;
        }

        public static void SetSelectedDatabaseName(string databaseName)
        {
            if (!IsEnabled() || HttpContext.Current == null || HttpContext.Current.Session == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                HttpContext.Current.Session.Remove(MainErpSessionKeys.DebugDatabaseName);
                return;
            }

            HttpContext.Current.Session[MainErpSessionKeys.DebugDatabaseName] = databaseName.Trim();
        }

        public static string Apply(string connectionString)
        {
            var databaseName = GetSelectedDatabaseName();
            if (string.IsNullOrWhiteSpace(databaseName) || string.IsNullOrWhiteSpace(connectionString))
            {
                return connectionString;
            }

            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = databaseName
            };
            return builder.ConnectionString;
        }

        public static string GetDisplayDatabaseName()
        {
            var selected = GetSelectedDatabaseName();
            if (!string.IsNullOrWhiteSpace(selected))
            {
                return selected + " (debug override)";
            }

            var setting = ConfigurationManager.ConnectionStrings["MainErp_ConnectionString"];
            if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
            {
                return "MainErp_ConnectionString not configured";
            }

            try
            {
                return new SqlConnectionStringBuilder(setting.ConnectionString).InitialCatalog;
            }
            catch
            {
                return "MainErp_ConnectionString";
            }
        }
    }
}
