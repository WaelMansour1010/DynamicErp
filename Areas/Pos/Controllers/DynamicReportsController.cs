using System.Web.Mvc;
using MyERP.Areas.Reports.Controllers;
using MyERP.Areas.Reports.Models;
using MyERP.Areas.Reports.Services;

namespace MyERP.Areas.Pos.Controllers
{
    [AllowAnonymous]
    [SkipERPAuthorize]
    public class DynamicReportsController : ViewerController
    {
        public override ActionResult Index(string scope)
        {
            var posContext = DynamicReportSecurity.RestorePosContext(HttpContext);
            if (posContext == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos", returnUrl = Request.RawUrl });
            }

            ViewBag.PosContext = posContext;
            ViewBag.Scope = DynamicReportScopes.Pos;
            ViewBag.DynamicReportsApiBase = Url.Content("~/Pos/DynamicReports");
            ViewBag.LayoutPath = null;
            return View("~/Areas/Reports/Views/Viewer/Index.cshtml");
        }
    }
}
