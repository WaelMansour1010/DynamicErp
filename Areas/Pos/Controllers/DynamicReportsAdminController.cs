using System.Web.Mvc;
using MyERP.Areas.Pos.Models;
using MyERP.Areas.Reports.Controllers;
using MyERP.Areas.Reports.Models;
using MyERP.Areas.Reports.Services;

namespace MyERP.Areas.Pos.Controllers
{
    [AllowAnonymous]
    [SkipERPAuthorize]
    public class DynamicReportsAdminController : AdminController
    {
        public override ActionResult Index(string scope)
        {
            var posContext = DynamicReportSecurity.RestorePosContext(HttpContext);
            if (posContext == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos", returnUrl = Request.RawUrl });
            }

            if (!IsAdmin(posContext))
            {
                return new HttpStatusCodeResult(403, "Dynamic reports administration is not allowed for this user.");
            }

            ViewBag.PosContext = posContext;
            ViewBag.Scope = DynamicReportScopes.Pos;
            ViewBag.DynamicReportsApiBase = Url.Content("~/Pos/DynamicReportsAdmin");
            ViewBag.LayoutPath = null;
            return View("~/Areas/Reports/Views/Admin/Index.cshtml");
        }

        private static bool IsAdmin(PosUserContext context)
        {
            return context != null && (context.IsFullAccess || context.UserType.GetValueOrDefault(-1) == 0);
        }
    }
}
