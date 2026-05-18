using System;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Areas.Shared.CriticalRecovery
{
    public class CriticalRecoveryAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext == null || httpContext.User == null || httpContext.User.Identity == null || !httpContext.User.Identity.IsAuthenticated)
            {
                return false;
            }

            var user = httpContext.User;
            return user.IsInRole(CriticalRecoveryPermissions.SuperAdminRole)
                || user.IsInRole(CriticalRecoveryPermissions.AdministratorRole)
                || user.IsInRole(CriticalRecoveryPermissions.CanAccessCriticalRecoveryCenter);
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            throw new UnauthorizedAccessException("Critical Recovery Center requires super admin access and the CanAccessCriticalRecoveryCenter permission.");
        }
    }
}
