using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Text;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class PosSystemErrorLogController : Controller
    {
        private readonly PosSqlRepository _repository = new PosSqlRepository();

        public ActionResult Index()
        {
            var context = GetPosContext();
            if (context == null)
            {
                TempData["PosLoginMessage"] = PosLoginController.PosSessionExpiredMessage;
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!IsAdmin(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية استعراض سجل أخطاء النظام");
            }

            Response.ContentEncoding = Encoding.UTF8;
            Response.Charset = "utf-8";
            ViewBag.PosContext = context;
            ViewBag.Branches = _repository.GetBranches();
            return View();
        }

        [HttpGet]
        public JsonResult Search(PosSystemErrorLogSearchRequest request)
        {
            Response.ContentEncoding = Encoding.UTF8;
            Response.Charset = "utf-8";

            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                Response.TrySkipIisCustomErrors = true;
                return Json(new { success = false, message = "انتهت الجلسة، برجاء تسجيل الدخول مرة أخرى" }, JsonRequestBehavior.AllowGet);
            }

            if (!IsAdmin(context))
            {
                Response.StatusCode = 403;
                Response.TrySkipIisCustomErrors = true;
                return Json(new { success = false, message = "ليست لديك صلاحية استعراض سجل أخطاء النظام" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var result = _repository.SearchPosSystemErrorLogs(request);
                return Json(new { success = true, data = result.Items, count = result.Count }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                Response.TrySkipIisCustomErrors = true;
                return Json(new { success = false, message = "تعذر تحميل سجل الأخطاء", details = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        private static bool IsAdmin(PosUserContext context)
        {
            return context != null && (context.UserType.GetValueOrDefault(-1) == 0 || context.IsFullAccess);
        }

        private PosUserContext GetPosContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _repository);
        }
    }
}
