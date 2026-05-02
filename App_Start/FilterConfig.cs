using System.Web;
using System.Web.Mvc;

namespace MyERP
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new ERPAuthorizeAttribute());
            filters.Add(new PosAwareAuthorizeAttribute());
            filters.Add(new HandleErrorAttribute());
        }
    }

    public class PosAwareAuthorizeAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            var area = filterContext.RouteData.DataTokens["area"] as string;
            if (string.Equals(area, "Pos", System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            base.OnAuthorization(filterContext);
        }
    }
}
