using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class PosDashboardController : Controller
    {
        private readonly PosSqlRepository _repository;

        public PosDashboardController()
        {
            _repository = new PosSqlRepository();
        }

        public ActionResult Index()
        {
            return OpenShell(IsMobileRequest() ? "sales" : "dashboard");
        }

        public ActionResult Sales()
        {
            return OpenShell("sales");
        }

        public ActionResult Closing()
        {
            return OpenShell("closing");
        }

        public ActionResult Kyc()
        {
            return OpenShell("kyc");
        }

        public ActionResult Reports()
        {
            return OpenShell("reports");
        }

        public ActionResult Payments()
        {
            return OpenShell("payments");
        }

        [HttpGet]
        public JsonResult Summary(DateTime? fromDate, DateTime? toDate, int? branchId, string operationType)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "يجب تسجيل دخول نقطة البيع أولاً" }, JsonRequestBehavior.AllowGet);
            }

            if (!context.IsFullAccess)
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية عرض Dashboard الإدارة" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var resolvedBranchId = branchId.HasValue && branchId.Value > 0 ? branchId : null;
                var summary = _repository.GetAdminDashboardSummary((fromDate ?? DateTime.Today).Date, (toDate ?? DateTime.Today).Date, resolvedBranchId, operationType);
                return Json(new { success = true, data = summary }, JsonRequestBehavior.AllowGet);
            }
            catch (SqlException ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "تعذر تحميل Dashboard الإدارة", technicalMessage = ex.Message }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "تعذر تحميل Dashboard الإدارة", technicalMessage = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        private ActionResult OpenShell(string screen)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (screen == "dashboard" && !context.IsFullAccess)
            {
                screen = "sales";
            }

            ViewBag.PosContext = context;
            ViewBag.ActiveScreen = screen;
            ViewBag.InitialScreenUrl = ScreenUrl(screen);
            ViewBag.Branches = context.IsFullAccess
                ? _repository.GetBranches()
                : new[] { new PosBranchDto { BranchId = context.BranchId.GetValueOrDefault(), BranchName = context.BranchName } };
            return View("Index");
        }

        private string ScreenUrl(string screen)
        {
            if (screen == "closing")
            {
                return Url.Content("~/Pos/PosClosing/Index");
            }

            if (screen == "kyc")
            {
                return Url.Content("~/Pos/PosTransaction/Index?openKyc=true");
            }

            if (screen == "reports")
            {
                return Url.Content("~/Pos/PosReports/Index");
            }

            if (screen == "payments")
            {
                return Url.Content("~/Pos/Payments/Index");
            }

            return Url.Content("~/Pos/PosTransaction/Index");
        }

        private bool IsMobileRequest()
        {
            var browser = Request != null ? Request.Browser : null;
            if (browser != null && browser.IsMobileDevice)
            {
                return true;
            }

            var agent = Request != null ? (Request.UserAgent ?? string.Empty) : string.Empty;
            return agent.IndexOf("Mobi", StringComparison.OrdinalIgnoreCase) >= 0
                || agent.IndexOf("Android", StringComparison.OrdinalIgnoreCase) >= 0
                || agent.IndexOf("iPhone", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private PosUserContext GetPosContext()
        {
            return Session[PosLoginController.PosContextSessionKey] as PosUserContext;
        }
    }
}
