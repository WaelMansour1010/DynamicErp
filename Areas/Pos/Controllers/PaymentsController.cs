using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly PosSqlRepository _repository;

        public PaymentsController()
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

            if (!context.IsFullAccess && !context.CanOpenPayments)
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية فتح شاشة التمويل والاستعاضة");
            }

            var branchId = context.IsFullAccess ? (int?)null : context.BranchId;
            ViewBag.Context = context;
            ViewBag.Branches = context.IsFullAccess ? _repository.GetBranches() : new[] { new PosBranchDto { BranchId = context.BranchId.GetValueOrDefault(), BranchName = context.BranchName } };
            ViewBag.CustodyAccounts = _repository.GetPosPaymentBoxes(branchId, 1);
            ViewBag.BoxAccounts = _repository.GetPosPaymentBoxes(branchId, 0);
            ViewBag.PaymentBoxes = _repository.GetPosPaymentBoxes(branchId, null);
            ViewBag.Banks = _repository.GetPosPaymentBanks();
            ViewBag.Employees = _repository.GetPosPaymentEmployees();
            return View();
        }

        [HttpPost]
        public JsonResult Preview(PosPaymentRequestDto request)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "يجب تسجيل دخول نقطة البيع أولاً" });
            }

            if (!context.IsFullAccess && !context.CanOpenPayments)
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية فتح شاشة التمويل والاستعاضة" });
            }

            ForceContext(request, context);
            try
            {
                return Json(new { success = true, lines = _repository.PreviewPosPayment(request) });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult Create(PosPaymentRequestDto request)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "يجب تسجيل دخول نقطة البيع أولاً" });
            }

            if (!context.IsFullAccess && !context.CanExecutePayments)
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية تنفيذ التمويل والاستعاضة" });
            }

            ForceContext(request, context);
            try
            {
                var result = _repository.SavePosPayment(request, context.UserId, context.IsFullAccess || context.CanExecutePayments);
                return Json(new { success = true, message = "تم حفظ عملية التمويل والاستعاضة", result = result });
            }
            catch (SqlException ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "حدث خطأ من قاعدة البيانات أثناء الحفظ", technicalMessage = ex.Message });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        private static void ForceContext(PosPaymentRequestDto request, PosUserContext context)
        {
            if (request == null)
            {
                return;
            }

            if (!context.IsFullAccess)
            {
                request.BranchId = context.BranchId.GetValueOrDefault();
            }

            if (request.PaymentDate == DateTime.MinValue)
            {
                request.PaymentDate = DateTime.Today;
            }
        }

        private PosUserContext GetPosContext()
        {
            return Session[PosLoginController.PosContextSessionKey] as PosUserContext;
        }
    }
}
