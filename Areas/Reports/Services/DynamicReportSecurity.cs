using System;
using System.Linq;
using System.Security.Claims;
using System.Web;
using MyERP.Areas.MainErp.Models.Security;
using MyERP.Areas.MainErp.Security;
using MyERP.Areas.Pos.Controllers;
using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using MyERP.Areas.Reports.Models;

namespace MyERP.Areas.Reports.Services
{
    public static class DynamicReportSecurity
    {
        public static DynamicReportUserContext Build(HttpContextBase httpContext, string scope)
        {
            var normalized = DynamicReportScopes.Normalize(scope);
            if (normalized == DynamicReportScopes.Pos) return BuildPos(httpContext);
            if (normalized == DynamicReportScopes.MainErp) return BuildMainErp(httpContext);
            return BuildWeb(httpContext);
        }

        public static PosUserContext RestorePosContext(HttpContextBase httpContext)
        {
            try
            {
                return PosLoginController.RestorePosContext(httpContext.Request, httpContext.Session, new PosSqlRepository());
            }
            catch
            {
                return httpContext.Session != null ? httpContext.Session[PosLoginController.PosContextSessionKey] as PosUserContext : null;
            }
        }

        public static MainErpUserContext RestoreMainErpContext(HttpContextBase httpContext)
        {
            return httpContext.Session != null ? httpContext.Session[MainErpSessionKeys.Context] as MainErpUserContext : null;
        }

        private static DynamicReportUserContext BuildWeb(HttpContextBase httpContext)
        {
            var identity = httpContext.User != null ? httpContext.User.Identity as ClaimsIdentity : null;
            var userId = ReadClaimInt(identity, "Id") ?? 0;
            return new DynamicReportUserContext
            {
                UserId = userId,
                RoleId = ReadClaimInt(identity, "RoleId"),
                UserName = httpContext.User != null ? httpContext.User.Identity.Name : string.Empty,
                ProjectScope = DynamicReportScopes.Web,
                IsAdmin = userId == 1
            };
        }

        private static DynamicReportUserContext BuildPos(HttpContextBase httpContext)
        {
            var context = RestorePosContext(httpContext);
            return new DynamicReportUserContext
            {
                UserId = context != null ? context.UserId : 0,
                UserName = context != null ? context.UserName : string.Empty,
                ProjectScope = DynamicReportScopes.Pos,
                IsAdmin = context != null && (context.IsFullAccess || context.UserType.GetValueOrDefault(-1) == 0)
            };
        }

        private static DynamicReportUserContext BuildMainErp(HttpContextBase httpContext)
        {
            var context = RestoreMainErpContext(httpContext);
            return new DynamicReportUserContext
            {
                UserId = context != null ? context.UserId : 0,
                UserName = context != null ? context.UserName : string.Empty,
                ProjectScope = DynamicReportScopes.MainErp,
                IsAdmin = context != null && (context.IsAdmin || context.UserType.GetValueOrDefault(-1) == 0)
            };
        }

        private static int? ReadClaimInt(ClaimsIdentity identity, string type)
        {
            if (identity == null) return null;
            var claim = identity.Claims.FirstOrDefault(c => string.Equals(c.Type, type, StringComparison.OrdinalIgnoreCase));
            int value;
            return claim != null && int.TryParse(claim.Value, out value) ? (int?)value : null;
        }
    }
}
