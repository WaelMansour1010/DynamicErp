using System;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using System.Web.Configuration;

namespace MyERP.Areas.Sync.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class SyncAuthorizeAttribute : AuthorizeAttribute
    {
        public SyncAuthorizeAttribute(string permission)
        {
            Permission = permission;
        }

        public string Permission { get; private set; }

        protected override bool AuthorizeCore(System.Web.HttpContextBase httpContext)
        {
            if (httpContext == null || httpContext.User == null || httpContext.User.Identity == null)
            {
                return false;
            }

            if (IsLocalDebugReadOnly(httpContext))
            {
                return true;
            }

            if (!httpContext.User.Identity.IsAuthenticated)
            {
                return false;
            }

            var roles = GetAllowedRoles();
            if (roles.Length == 0)
            {
                roles = GetConfiguredRoles("Sync.AdminRoles", "SyncAdmin,Administrators,Admin");
            }

            return roles.Any(httpContext.User.IsInRole);
        }

        private string[] GetAllowedRoles()
        {
            var specific = GetConfiguredRoles("Sync.Permission." + Permission, null);
            return specific.Length > 0 ? specific : GetConfiguredRoles("Sync.AdminRoles", "SyncAdmin,Administrators,Admin");
        }

        private static string[] GetConfiguredRoles(string key, string defaultValue)
        {
            var raw = ConfigurationManager.AppSettings[key];
            if (String.IsNullOrWhiteSpace(raw))
            {
                raw = defaultValue;
            }

            return String.IsNullOrWhiteSpace(raw)
                ? new string[0]
                : raw.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private bool IsLocalDebugReadOnly(System.Web.HttpContextBase httpContext)
        {
            if (!IsReadOnlyPermission())
            {
                return false;
            }

            var compilation = WebConfigurationManager.GetSection("system.web/compilation") as CompilationSection;
            return httpContext.Request != null && IsLocalRequest(httpContext.Request) && compilation != null && compilation.Debug;
        }

        private bool IsReadOnlyPermission()
        {
            return String.Equals(Permission, SyncPermissions.View, StringComparison.OrdinalIgnoreCase)
                || String.Equals(Permission, SyncPermissions.Diagnostics, StringComparison.OrdinalIgnoreCase)
                || String.Equals(Permission, SyncPermissions.Audit, StringComparison.OrdinalIgnoreCase)
                || String.Equals(Permission, SyncPermissions.Notifications, StringComparison.OrdinalIgnoreCase)
                || String.Equals(Permission, SyncPermissions.Export, StringComparison.OrdinalIgnoreCase)
                || String.Equals(Permission, SyncPermissions.RoleManagement, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLocalRequest(System.Web.HttpRequestBase request)
        {
            if (request.IsLocal)
            {
                return true;
            }

            var hostAddress = request.UserHostAddress;
            return String.Equals(hostAddress, "::1", StringComparison.OrdinalIgnoreCase)
                || String.Equals(hostAddress, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || String.Equals(hostAddress, "localhost", StringComparison.OrdinalIgnoreCase);
        }
    }
}
