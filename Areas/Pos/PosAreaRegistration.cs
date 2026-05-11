using System.Web.Mvc;

namespace MyERP.Areas.Pos
{
    public class PosAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get { return "Pos"; }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            // Keep explicit POS URLs available independently from any root/startup redirect flag.
            // If POS must be blocked completely, use a dedicated access policy instead of reusing
            // EnableKishnyPos, which should not make /Pos/Login fall through to the main ERP routes.
            MapPosRoute(
                context,
                "Pos_root",
                "Pos",
                new { controller = "PosLogin", action = "Root" }
            );

            MapPosRoute(
                context,
                "Pos_login",
                "Pos/Login",
                new { controller = "PosLogin", action = "Index" }
            );

            MapPosRoute(
                context,
                "Pos_dashboard_summary",
                "Pos/Dashboard/Summary",
                new { controller = "PosDashboard", action = "Summary" }
            );

            MapPosRoute(
                context,
                "Pos_dashboard",
                "Pos/Dashboard",
                new { controller = "PosDashboard", action = "Index" }
            );

            MapPosRoute(
                context,
                "Pos_sales",
                "Pos/Sales",
                new { controller = "PosDashboard", action = "Sales" }
            );

            MapPosRoute(
                context,
                "Pos_closing_shell",
                "Pos/Closing",
                new { controller = "PosDashboard", action = "Closing" }
            );

            MapPosRoute(
                context,
                "Pos_kyc_shell",
                "Pos/Kyc",
                new { controller = "PosDashboard", action = "Kyc" }
            );

            MapPosRoute(
                context,
                "Pos_reports_shell",
                "Pos/Reports",
                new { controller = "PosDashboard", action = "Reports" }
            );

            MapPosRoute(
                context,
                "Pos_payments_shell",
                "Pos/Payments",
                new { controller = "PosDashboard", action = "Payments" }
            );

            MapPosRoute(
                context,
                "Pos_cashing_shell",
                "Pos/Cashing",
                new { controller = "PosDashboard", action = "Cashing" }
            );

            MapPosRoute(
                context,
                "Pos_permissions_shell",
                "Pos/Permissions",
                new { controller = "PosPermissions", action = "Index" }
            );

            MapPosRoute(
                context,
                "Pos_default",
                "Pos/{controller}/{action}/{id}",
                new { controller = "PosTransaction", action = "Index", id = UrlParameter.Optional }
            );
        }

        private static void MapPosRoute(AreaRegistrationContext context, string name, string url, object defaults)
        {
            var route = context.MapRoute(
                name,
                url,
                defaults,
                new[] { "MyERP.Areas.Pos.Controllers" }
            );
            route.DataTokens["UseNamespaceFallback"] = false;
        }
    }
}
