using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Linq;
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
            ViewBag.UserCategories = PosUserCategories;
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
                return Json(new { success = true, message = "تم حفظ صلاحيات POS. تظهر بعد إعادة تسجيل الدخول أو تحديث السياق." });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "تعذر حفظ الصلاحيات", technicalMessage = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SaveUserCategory(PosUserCategorySaveRequest request)
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

            if (!IsValidCategory(request.UserCategory))
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = "فئة المستخدم غير صحيحة" });
            }

            try
            {
                _repository.SavePosUserCategory(request.UserId, request.UserCategory);
                return Json(new { success = true, message = "تم حفظ فئة المستخدم" });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "تعذر حفظ فئة المستخدم", technicalMessage = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult BulkApply(PosBulkPermissionRequest request)
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

            if (request == null || !IsValidCategory(request.UserCategory) || string.IsNullOrWhiteSpace(request.UserCategory))
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = "اختر فئة مستخدم صحيحة" });
            }

            if (!IsValidPermissionKey(request.PermissionKey))
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = "اختر صلاحية صحيحة" });
            }

            try
            {
                var result = _repository.ApplyPosPermissionToCategory(request.UserCategory, request.PermissionKey, request.IsAllowed);
                return Json(new
                {
                    success = true,
                    message = "تم تطبيق الصلاحية على الفئة المحددة",
                    affectedUsers = result.Item1,
                    updatedRows = result.Item2
                });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "تعذر تطبيق الصلاحية على الفئة", technicalMessage = ex.Message });
            }
        }

        private PosUserContext GetPosContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _repository);
        }

        private static readonly string[] PosUserCategories = { "حسابات", "ادارة", "تلر", "شئون موظفين", "KYC" };

        private static bool IsValidCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return true;
            }

            return PosUserCategories.Any(c => string.Equals(c, category.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsValidPermissionKey(string permissionKey)
        {
            return PosSqlRepository.BuildPosPermissionItems(null)
                .Any(p => string.Equals(p.Key, permissionKey, StringComparison.OrdinalIgnoreCase));
        }
    }
}
