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
            context.MapRoute(
                "Pos_root",
                "Pos",
                new { controller = "PosDashboard", action = "Index" },
                new[] { "MyERP.Areas.Pos.Controllers" }
            );

            context.MapRoute(
                "Pos_dashboard_summary",
                "Pos/Dashboard/Summary",
                new { controller = "PosDashboard", action = "Summary" },
                new[] { "MyERP.Areas.Pos.Controllers" }
            );

            context.MapRoute(
                "Pos_dashboard",
                "Pos/Dashboard",
                new { controller = "PosDashboard", action = "Index" },
                new[] { "MyERP.Areas.Pos.Controllers" }
            );

            context.MapRoute(
                "Pos_sales",
                "Pos/Sales",
                new { controller = "PosDashboard", action = "Sales" },
                new[] { "MyERP.Areas.Pos.Controllers" }
            );

            context.MapRoute(
                "Pos_closing_shell",
                "Pos/Closing",
                new { controller = "PosDashboard", action = "Closing" },
                new[] { "MyERP.Areas.Pos.Controllers" }
            );

            context.MapRoute(
                "Pos_kyc_shell",
                "Pos/Kyc",
                new { controller = "PosDashboard", action = "Kyc" },
                new[] { "MyERP.Areas.Pos.Controllers" }
            );

            context.MapRoute(
                "Pos_reports_shell",
                "Pos/Reports",
                new { controller = "PosDashboard", action = "Reports" },
                new[] { "MyERP.Areas.Pos.Controllers" }
            );

            context.MapRoute(
                "Pos_payments_shell",
                "Pos/Payments",
                new { controller = "PosDashboard", action = "Payments" },
                new[] { "MyERP.Areas.Pos.Controllers" }
            );

            context.MapRoute(
                "Pos_cashing_shell",
                "Pos/Cashing",
                new { controller = "PosDashboard", action = "Cashing" },
                new[] { "MyERP.Areas.Pos.Controllers" }
            );

            context.MapRoute(
                "Pos_permissions_shell",
                "Pos/Permissions",
                new { controller = "PosPermissions", action = "Index" },
                new[] { "MyERP.Areas.Pos.Controllers" }
            );

            context.MapRoute(
                "Pos_login",
                "Pos/Login",
                new { controller = "PosLogin", action = "Index" },
                new[] { "MyERP.Areas.Pos.Controllers" }
            );

            context.MapRoute(
                "Pos_default",
                "Pos/{controller}/{action}/{id}",
                new { controller = "PosTransaction", action = "Index", id = UrlParameter.Optional },
                new[] { "MyERP.Areas.Pos.Controllers" }
            );
        }
    }
}
