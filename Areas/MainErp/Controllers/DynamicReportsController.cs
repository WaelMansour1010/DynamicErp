using System.Web.Mvc;
using MyERP.Areas.Reports.Controllers;
using MyERP.Areas.Reports.Models;
using MyERP.Areas.Reports.Services;

namespace MyERP.Areas.MainErp.Controllers
{
    [AllowAnonymous]
    [SkipERPAuthorize]
    public class DynamicReportsController : ViewerController
    {
        public override ActionResult Index(string scope)
        {
            var context = DynamicReportSecurity.RestoreMainErpContext(HttpContext);
            if (context == null)
            {
                return RedirectToAction("Index", "Login", new { area = "MainErp", returnUrl = Request.RawUrl });
            }

            ViewBag.ActiveScreen = "dynamic-reports";
            ViewBag.Scope = DynamicReportScopes.MainErp;
            ViewBag.DynamicReportsApiBase = Url.Content("~/MainErp/DynamicReports");
            ViewBag.LayoutPath = "~/Areas/MainErp/Views/Shared/_MainErpLayout.cshtml";
            return View("~/Areas/Reports/Views/Viewer/Index.cshtml");
        }
    }
}
