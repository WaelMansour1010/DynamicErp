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
