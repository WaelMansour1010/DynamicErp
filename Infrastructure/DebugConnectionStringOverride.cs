using System;
using System.Configuration;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using System.Reflection;
using System.Web;

namespace MyERP.Infrastructure
{
    public static class DebugConnectionStringOverride
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

        public static void ApplyOriginalWebDatabase(string databaseName)
        {
            if (!IsEnabled() || string.IsNullOrWhiteSpace(databaseName))
            {
                return;
            }

            var trimmed = databaseName.Trim();
            ApplySqlCatalog("MyERP_ConnectionString", trimmed);
            ApplySqlCatalog("MyErpConnectionString", trimmed);
            ApplySqlCatalog("localhost_MySoftERP_Connection", trimmed);
            ApplyEntityCatalog("MySoftERPEntity", trimmed);
            ApplyEntityCatalog("MyErpEntities", trimmed);
            HttpContext.Current.Application["DebugOriginalWebDatabase"] = trimmed;
        }

        public static string GetOriginalWebDatabase()
        {
            if (!IsEnabled() || HttpContext.Current == null)
            {
                return null;
            }

            return HttpContext.Current.Application["DebugOriginalWebDatabase"] as string;
        }

        private static void ApplySqlCatalog(string name, string databaseName)
        {
            var setting = ConfigurationManager.ConnectionStrings[name];
            if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
            {
                return;
            }

            if (setting.ConnectionString.IndexOf("XpoProvider=", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                MakeWritable(setting);
                setting.ConnectionString = ReplaceKeyValue(setting.ConnectionString, "initial catalog", databaseName);
                return;
            }

            var builder = new SqlConnectionStringBuilder(setting.ConnectionString)
            {
                InitialCatalog = databaseName
            };
            MakeWritable(setting);
            setting.ConnectionString = builder.ConnectionString;
        }

        private static void ApplyEntityCatalog(string name, string databaseName)
        {
            var setting = ConfigurationManager.ConnectionStrings[name];
            if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
            {
                return;
            }

            var entityBuilder = new EntityConnectionStringBuilder(setting.ConnectionString);
            var sqlBuilder = new SqlConnectionStringBuilder(entityBuilder.ProviderConnectionString)
            {
                InitialCatalog = databaseName
            };
            entityBuilder.ProviderConnectionString = sqlBuilder.ConnectionString;
            MakeWritable(setting);
            setting.ConnectionString = entityBuilder.ConnectionString;
        }

        private static void MakeWritable(ConfigurationElement element)
        {
            if (element == null)
            {
                return;
            }

            var currentType = typeof(ConfigurationElement);
            while (currentType != null)
            {
                var field = currentType.GetField("_bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? currentType.GetField("bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(element, false);
                    return;
                }

                currentType = currentType.BaseType;
            }
        }

        private static string ReplaceKeyValue(string connectionString, string key, string value)
        {
            var parts = connectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i].Trim();
                var equalsIndex = part.IndexOf('=');
                if (equalsIndex <= 0)
                {
                    continue;
                }

                if (string.Equals(part.Substring(0, equalsIndex).Trim(), key, StringComparison.OrdinalIgnoreCase))
                {
                    parts[i] = part.Substring(0, equalsIndex + 1) + value;
                    return string.Join(";", parts);
                }
            }

            return connectionString + ";" + key + "=" + value;
        }
    }
}
