using System.Web.Mvc;
using MyERP.Areas.Reports.Controllers;
using MyERP.Areas.Reports.Models;
using MyERP.Areas.Reports.Services;

namespace MyERP.Areas.MainErp.Controllers
{
    [AllowAnonymous]
    [SkipERPAuthorize]
    public class DynamicReportsAdminController : AdminController
    {
        public override ActionResult Index(string scope)
        {
            var context = DynamicReportSecurity.RestoreMainErpContext(HttpContext);
            if (context == null)
            {
                return RedirectToAction("Index", "Login", new { area = "MainErp", returnUrl = Request.RawUrl });
            }

            if (!context.IsAdmin && context.UserType.GetValueOrDefault(-1) != 0)
            {
                return new HttpStatusCodeResult(403, "Dynamic reports administration is not allowed for this user.");
            }

            ViewBag.ActiveScreen = "dynamic-reports";
            ViewBag.Scope = DynamicReportScopes.MainErp;
            ViewBag.DynamicReportsApiBase = Url.Content("~/MainErp/DynamicReportsAdmin");
            ViewBag.LayoutPath = "~/Areas/MainErp/Views/Shared/_MainErpLayout.cshtml";
            return View("~/Areas/Reports/Views/Admin/Index.cshtml");
        }
    }
}
