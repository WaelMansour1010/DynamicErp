using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class PosClosingController : Controller
    {
        private readonly PosClosingSqlRepository _repository;

        public PosClosingController()
        {
            _repository = new PosClosingSqlRepository();
        }

        public ActionResult Index()
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!CanOpenClosing(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية فتح شاشة الإغلاق");
            }

            ViewBag.PosContext = context;
            return View();
        }

        [HttpPost]
        public JsonResult LoadValues(PosClosingValuesRequest request)
        {
            try
            {
                var context = GetPosContext();
                if (context == null)
                {
                    Response.StatusCode = 401;
                    return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."));
                }

                if (!CanOpenClosing(context))
                {
                    Response.StatusCode = 403;
                    return Json(Fail("ليست لديك صلاحية فتح شاشة الإغلاق", "CanOpenClosing is false."));
                }

                var closingDate = (request != null && request.ClosingDate.HasValue ? request.ClosingDate.Value : DateTime.Today).Date;
                var branchId = ResolveBranchId(request != null ? request.BranchId : null, context);
                var values = _repository.GetClosingValues(closingDate, branchId, context.UserId, context.CanChangeDefaults);
                return Json(new { success = true, values = values });
            }
            catch (SqlException ex)
            {
                Response.StatusCode = 500;
                return Json(Fail("حدث خطأ من قاعدة البيانات أثناء تحديث بيانات الإغلاق", ex.Message));
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(Fail("تعذر تحديث بيانات الإغلاق", ex.Message));
            }
        }

        [HttpPost]
        public JsonResult ExecuteClosing(PosClosingExecuteRequest request)
        {
            try
            {
                var context = GetPosContext();
                if (context == null)
                {
                    Response.StatusCode = 401;
                    return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."));
                }

                if (!CanExecuteClosing(context))
                {
                    Response.StatusCode = 403;
                    return Json(Fail("ليست لديك صلاحية تنفيذ الإغلاق", "CanExecuteClosing is false."));
                }

                var closingDate = (request != null && request.ClosingDate.HasValue ? request.ClosingDate.Value : DateTime.Today).Date;
                var branchId = ResolveBranchId(request != null ? request.BranchId : null, context);
                if (request == null || string.IsNullOrWhiteSpace(request.Password))
                {
                    Response.StatusCode = 400;
                    return Json(Fail("كلمة المرور مطلوبة لتنفيذ الإغلاق", "Closing password is required."));
                }

                var result = _repository.ExecuteClosing(closingDate, branchId, context.UserId, request.Password, request.ActualValue);
                return Json(new { success = true, result = result });
            }
            catch (SqlException ex)
            {
                Response.StatusCode = 500;
                return Json(Fail("حدث خطأ من قاعدة البيانات أثناء تنفيذ الإغلاق", ex.Message));
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(Fail("تعذر تنفيذ الإغلاق", ex.Message));
            }
        }

        private int ResolveBranchId(int? postedBranchId, PosUserContext context)
        {
            if (context.CanChangeDefaults && postedBranchId.HasValue && postedBranchId.Value > 0)
            {
                return postedBranchId.Value;
            }

            if (!context.BranchId.HasValue || context.BranchId.Value <= 0)
            {
                throw new InvalidOperationException("لا يوجد فرع افتراضي مضبوط للمستخدم");
            }

            return context.BranchId.Value;
        }

        private PosUserContext GetPosContext()
        {
            return Session[PosLoginController.PosContextSessionKey] as PosUserContext;
        }

        private static bool CanOpenClosing(PosUserContext context)
        {
            return context != null && (context.IsFullAccess || context.CanTeller || context.CanOpenClosing);
        }

        private static bool CanExecuteClosing(PosUserContext context)
        {
            return context != null && (context.IsFullAccess || context.CanTeller || context.CanExecuteClosing);
        }

        private static object Fail(string message, string technicalMessage)
        {
            return new
            {
                success = false,
                message = message,
                technicalMessage = technicalMessage
            };
        }
    }
}
