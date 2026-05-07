using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class SalesTargetsController : Controller
    {
        private readonly PosSqlRepository _posRepository;
        private readonly PosSalesPerformanceRepository _targetsRepository;

        public SalesTargetsController()
        {
            _posRepository = new PosSqlRepository();
            _targetsRepository = new PosSalesPerformanceRepository();
        }

        public ActionResult Index()
        {
            var model = BuildModel();
            if (model == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!CanManage(model.Context))
            {
                return new HttpStatusCodeResult(403, "ليس لديك صلاحية إدارة تارجت المناديب");
            }

            ViewBag.PosContext = model.Context;
            ViewBag.ActiveScreen = "sales-targets";
            return View(model);
        }

        [HttpPost]
        public ActionResult Save(PosSalesTargetSaveRequest request)
        {
            var context = PosLoginController.RestorePosContext(Request, Session, _posRepository);
            if (context == null)
            {
                return Json(new { ok = false, message = "انتهت الجلسة. سجل الدخول مرة أخرى." });
            }

            if (!CanManage(context))
            {
                return Json(new { ok = false, message = "ليس لديك صلاحية إدارة تارجت المناديب." });
            }

            var validationMessage = ValidateSalesTargetRequest(request);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                return Json(new { ok = false, message = validationMessage });
            }

            _targetsRepository.SaveSalesTargets(request, LockedBranch(context), context.UserId);
            return Json(new { ok = true, message = "تم حفظ التارجت بنجاح." });
        }

        [HttpPost]
        public ActionResult Deactivate(int targetId)
        {
            var context = PosLoginController.RestorePosContext(Request, Session, _posRepository);
            if (context == null)
            {
                return Json(new { ok = false, message = "انتهت الجلسة. سجل الدخول مرة أخرى." });
            }

            if (!CanManage(context))
            {
                return Json(new { ok = false, message = "ليس لديك صلاحية إدارة تارجت المناديب." });
            }

            if (targetId <= 0)
            {
                return Json(new { ok = false, message = "اختر تارجت صحيح." });
            }

            _targetsRepository.DeactivateSalesTarget(targetId, context.UserId);
            return Json(new { ok = true, message = "تم إيقاف التارجت." });
        }

        private PosSalesTargetsPageModel BuildModel()
        {
            var context = PosLoginController.RestorePosContext(Request, Session, _posRepository);
            if (context == null)
            {
                return null;
            }

            var model = new PosSalesTargetsPageModel
            {
                Context = context,
                LockedBranchId = LockedBranch(context)
            };

            model.Branches = IsAdmin(context)
                ? _posRepository.GetBranches().ToList()
                : _posRepository.GetBranches().Where(x => x.BranchId == context.BranchId.GetValueOrDefault()).ToList();
            model.SalesRepresentatives = _targetsRepository.GetSalesRepresentatives(model.LockedBranchId);
            model.Targets = _targetsRepository.GetSalesTargets(model.FromDate, model.ToDate, model.LockedBranchId, null);
            return model;
        }

        private static string ValidateSalesTargetRequest(PosSalesTargetSaveRequest request)
        {
            if (request == null)
            {
                return "بيانات التارجت غير مكتملة.";
            }

            var fromDate = request.FromDate.GetValueOrDefault(DateTime.MinValue).Date;
            var toDate = request.ToDate.GetValueOrDefault(DateTime.MinValue).Date;
            if (fromDate == DateTime.MinValue || toDate == DateTime.MinValue)
            {
                return "حدد الفترة من وإلى.";
            }

            if (toDate < fromDate)
            {
                return "تاريخ النهاية يجب أن يكون بعد تاريخ البداية.";
            }

            if (request.MonthlyRechargeTarget < 0 || request.MonthlyCardTarget < 0)
            {
                return "قيم التارجت لا تقبل أرقام سالبة.";
            }

            if (request.MonthlyRechargeTarget <= 0 && request.MonthlyCardTarget <= 0)
            {
                return "أدخل تارجت للشحنات أو للكروت على الأقل.";
            }

            if (request.WorkingDaysInMonth <= 0 || request.WorkingDaysInMonth > 31)
            {
                return "أيام العمل يجب أن تكون من 1 إلى 31.";
            }

            var applyMode = (request.ApplyMode ?? string.Empty).Trim().ToLowerInvariant();
            if (applyMode != "all" && applyMode != "selected")
            {
                return "اختر طريقة تطبيق التارجت.";
            }

            if (applyMode == "selected" && (request.UserIds == null || !request.UserIds.Any(x => x > 0)))
            {
                return "اختر مندوب واحد على الأقل.";
            }

            return null;
        }

        private static int? LockedBranch(PosUserContext context)
        {
            return !IsAdmin(context) && context != null && context.BranchId.HasValue ? context.BranchId : null;
        }

        private static bool CanManage(PosUserContext context)
        {
            return IsAdmin(context) || (context != null && context.IsFullAccess);
        }

        private static bool IsAdmin(PosUserContext context)
        {
            return context != null && (context.UserType.GetValueOrDefault(-1) == 0 || context.IsFullAccess);
        }
    }
}
