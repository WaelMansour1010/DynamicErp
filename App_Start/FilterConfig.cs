using System.Text;
using System.Web.Mvc;
using MyERP.Areas.Pos.Services;

namespace MyERP
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new ERPAuthorizeAttribute());
            filters.Add(new PosAwareAuthorizeAttribute());
            filters.Add(new Utf8ResponseEncodingAttribute());
            filters.Add(new PosPerformanceLogAttribute());
            filters.Add(new HandleErrorAttribute());
        }
    }

    public class Utf8ResponseEncodingAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            var result = filterContext.Result;
            if (result is ViewResultBase || result is JsonResult || result is ContentResult)
            {
                filterContext.HttpContext.Response.ContentEncoding = Encoding.UTF8;
                filterContext.HttpContext.Response.Charset = "utf-8";
            }

            base.OnResultExecuting(filterContext);
        }
    }

    public class PosAwareAuthorizeAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            var area = filterContext.RouteData.DataTokens["area"] as string;
            if (string.Equals(area, "Pos", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(area, "Sync", System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            base.OnAuthorization(filterContext);
        }
    }
}
