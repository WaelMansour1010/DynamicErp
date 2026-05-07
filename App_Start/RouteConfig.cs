using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace MyERP
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.MapMvcAttributeRoutes();
            var runModeRoute = routes.MapRoute(
                name: "RunMode",
                url: "RunMode",
                defaults: new { controller = "DevStart", action = "Index" },
                namespaces: new[] { "MyERP.Controllers" }
            );
            runModeRoute.DataTokens["UseNamespaceFallback"] = false;

            var rootRoute = routes.MapRoute(
                name: "PosRoot",
                url: "",
                defaults: new { controller = "DevStart", action = "Root" },
                namespaces: new[] { "MyERP.Controllers" }
            );
            rootRoute.DataTokens["UseNamespaceFallback"] = false;

            var defaultRoute = routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "MyERP.Controllers", "MyERP.Controllers.*" }
            );
            defaultRoute.DataTokens["UseNamespaceFallback"] = false;
            
        }
    }
}
