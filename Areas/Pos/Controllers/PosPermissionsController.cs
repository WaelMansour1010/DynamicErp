using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class PosPermissionsController : Controller
    {
        private readonly PosSqlRepository _repository;

        public PosPermissionsController()
        {
            _repository = new PosSqlRepository();
        }

        public ActionResult Index()
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!context.IsFullAccess)
            {
                return new HttpStatusCodeResult(403, "هذه الشاشة للمدير فقط");
            }

            ViewBag.Users = _repository.GetPosPermissionUsers();
            ViewBag.Permissions = PosSqlRepository.BuildPosPermissionItems(null);
            return View();
        }

        [HttpGet]
        public JsonResult GetUserPermissions(int userId)
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
                return Json(new { success = false, message = "هذه الشاشة للمدير فقط" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, permissions = _repository.GetPosUserTemporaryPermissionItems(userId) }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Save(PosPermissionSaveRequest request)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "يجب تسجيل دخول نقطة البيع أولاً" });
            }

            if (!context.IsFullAccess)
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "هذه الشاشة للمدير فقط" });
            }

            if (request == null || request.UserId <= 0)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = "اختر المستخدم أولاً" });
            }

            try
            {
                _repository.SavePosUserTemporaryPermissions(request.UserId, request.Permissions);
                return Json(new { success = true, message = "تم حفظ صلاحيات POS المؤقتة. تظهر بعد إعادة تسجيل الدخول أو تحديث السياق." });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "تعذر حفظ الصلاحيات", technicalMessage = ex.Message });
            }
        }

        private PosUserContext GetPosContext()
        {
            return Session[PosLoginController.PosContextSessionKey] as PosUserContext;
        }
    }
}
