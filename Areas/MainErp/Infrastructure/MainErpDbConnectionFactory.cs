using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Web;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.Security;

namespace MyERP.Areas.MainErp.Infrastructure
{
    public class MainErpDbConnectionFactory : IMainErpDbConnectionFactory
    {
        public const string DefaultConnectionStringName = "MainErp_ConnectionString";
        public const string ActiveConnectionStringNameKey = "MainErp.ActiveConnectionStringName";
        private const string DebugConnectionCookieName = "MainErp.Debug.ConnectionStringName";

        private readonly string _connectionStringName;

        public MainErpDbConnectionFactory()
            : this(ResolveActiveConnectionStringName())
        {
        }

        public MainErpDbConnectionFactory(string connectionStringName)
        {
            _connectionStringName = connectionStringName;
        }

        public SqlConnection CreateOpenConnection()
        {
            var setting = ConfigurationManager.ConnectionStrings[_connectionStringName];
            if ((setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString)) && _connectionStringName == DefaultConnectionStringName)
            {
                Trace.TraceWarning("MainErp_ConnectionString not found; falling back to MyERP_ConnectionString.");
                setting = ConfigurationManager.ConnectionStrings["MyERP_ConnectionString"];
            }

            if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
            {
                throw new ConfigurationErrorsException("Missing MainErp connection string: " + _connectionStringName);
            }

            var connectionString = MainErpDebugDatabaseOverride.Apply(setting.ConnectionString);

            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        public static string ResolveActiveConnectionStringName()
        {
            if (MainErpHostContext.IsPosHosted(HttpContext.Current))
            {
                return "KishnyCashConnection";
            }

            var selected = GetSelectedDebugConnectionStringName();
            if (!string.IsNullOrWhiteSpace(selected))
            {
                return selected;
            }

            var configured = ConfigurationManager.AppSettings[ActiveConnectionStringNameKey];
            return string.IsNullOrWhiteSpace(configured) ? DefaultConnectionStringName : configured.Trim();
        }

        public static string ResolveActiveConnectionString()
        {
            var name = ResolveActiveConnectionStringName();
            var setting = ConfigurationManager.ConnectionStrings[name];
            if ((setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString)) && string.Equals(name, DefaultConnectionStringName, System.StringComparison.OrdinalIgnoreCase))
            {
                setting = ConfigurationManager.ConnectionStrings["MyERP_ConnectionString"];
            }

            if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
            {
                throw new ConfigurationErrorsException("Missing MainErp connection string: " + name);
            }

            return MainErpDebugDatabaseOverride.Apply(setting.ConnectionString);
        }

        public static string GetSelectedDebugConnectionStringName()
        {
            if (!MainErpDebugDatabaseOverride.IsEnabled() || HttpContext.Current == null)
            {
                return null;
            }

            var sessionValue = HttpContext.Current.Session == null
                ? null
                : HttpContext.Current.Session[MainErpSessionKeys.DebugConnectionStringName] as string;
            if (IsConfiguredConnectionString(sessionValue))
            {
                return sessionValue.Trim();
            }

            var cookie = HttpContext.Current.Request == null ? null : HttpContext.Current.Request.Cookies[DebugConnectionCookieName];
            var cookieValue = cookie == null ? null : cookie.Value;
            return IsConfiguredConnectionString(cookieValue) ? cookieValue.Trim() : null;
        }

        public static void SetSelectedDebugConnectionStringName(string connectionStringName)
        {
            if (!MainErpDebugDatabaseOverride.IsEnabled() || HttpContext.Current == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(connectionStringName))
            {
                if (HttpContext.Current.Session != null)
                {
                    HttpContext.Current.Session.Remove(MainErpSessionKeys.DebugConnectionStringName);
                }

                WriteDebugConnectionCookie(string.Empty, true);
                return;
            }

            if (!IsConfiguredConnectionString(connectionStringName))
            {
                return;
            }

            var trimmed = connectionStringName.Trim();
            if (HttpContext.Current.Session != null)
            {
                HttpContext.Current.Session[MainErpSessionKeys.DebugConnectionStringName] = trimmed;
            }

            WriteDebugConnectionCookie(trimmed, false);
        }

        private static bool IsConfiguredConnectionString(string connectionStringName)
        {
            return !string.IsNullOrWhiteSpace(connectionStringName)
                && ConfigurationManager.ConnectionStrings[connectionStringName.Trim()] != null;
        }

        private static void WriteDebugConnectionCookie(string value, bool expire)
        {
            if (HttpContext.Current == null || HttpContext.Current.Response == null)
            {
                return;
            }

            var cookie = new HttpCookie(DebugConnectionCookieName, value ?? string.Empty)
            {
                HttpOnly = true,
                Expires = expire ? System.DateTime.UtcNow.AddDays(-1) : System.DateTime.UtcNow.AddDays(7)
            };
            HttpContext.Current.Response.Cookies.Set(cookie);
        }
    }
}
