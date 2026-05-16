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
            PrepareJsonResponse();

            try
            {
                var context = GetPosContext();
                if (context == null)
                {
                    return JsonFailure(401, "انتهت الجلسة، برجاء تسجيل الدخول مرة أخرى");
                }

                if (!IsAdmin(context))
                {
                    return JsonFailure(403, "ليست لديك صلاحية استعراض سجل أخطاء النظام");
                }

                var result = _repository.SearchPosSystemErrorLogs(request);
                return Json(new { success = true, data = result.Items, count = result.Count }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return JsonFailure(500, "تعذر تحميل سجل الأخطاء", ex.Message);
            }
        }

        [HttpGet]
        public JsonResult SearchSaveAttempts(PosSaveAttemptSearchRequest request)
        {
            PrepareJsonResponse();

            try
            {
                var context = GetPosContext();
                if (context == null)
                {
                    return JsonFailure(401, "انتهت الجلسة، برجاء تسجيل الدخول مرة أخرى");
                }

                if (!IsAdmin(context))
                {
                    return JsonFailure(403, "ليست لديك صلاحية استعراض سجل أخطاء النظام");
                }

                var result = _repository.SearchPosSaveAttempts(request);
                return Json(new { success = true, data = result.Items, summary = result.Summary, count = result.Count }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return JsonFailure(500, "تعذر تحميل محاولات حفظ POS", ex.Message);
            }
        }

        private void PrepareJsonResponse()
        {
            Response.ContentEncoding = Encoding.UTF8;
            Response.Charset = "utf-8";
            Response.ContentType = "application/json";
            Response.TrySkipIisCustomErrors = true;
        }

        private JsonResult JsonFailure(int statusCode, string message, string details = null)
        {
            PrepareJsonResponse();
            Response.StatusCode = statusCode;
            return Json(new { success = false, message = message, details = details }, JsonRequestBehavior.AllowGet);
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
