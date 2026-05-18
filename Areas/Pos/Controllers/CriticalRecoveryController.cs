using System;
using System.Web.Mvc;
using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using MyERP.Areas.Shared.CriticalRecovery;

namespace MyERP.Areas.Pos.Controllers
{
    public class CriticalRecoveryController : Controller
    {
        private readonly PosSqlRepository _repository = new PosSqlRepository();
        private readonly CriticalRecoveryService _service = new CriticalRecoveryService("Pos");

        public ActionResult Index()
        {
            var context = RequireAdminContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            ViewBag.PosContext = context;
            ViewBag.ActiveScreen = "critical-recovery";
            return View("Index", _service.BuildIndex("Pos"));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Analyze(CriticalRecoveryIndexViewModel model)
        {
            var context = RequireAdminContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            model = model ?? new CriticalRecoveryIndexViewModel();
            model.AreaName = "Pos";
            model.Impact = _service.Analyze(model.Filter);
            var fresh = _service.BuildIndex("Pos");
            model.SnapshotBatches = fresh.SnapshotBatches;
            model.AuditItems = fresh.AuditItems;
            model.BranchOptions = fresh.BranchOptions;
            ViewBag.PosContext = context;
            ViewBag.ActiveScreen = "critical-recovery";
            TempData["CriticalRecoveryMessage"] = "تمت المعاينة فقط. لم يتم تعديل أي بيانات.";
            return View("Index", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Export(CriticalRecoveryIndexViewModel model)
        {
            if (RequireAdminContext() == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            model = model ?? new CriticalRecoveryIndexViewModel();
            var impact = _service.Analyze(model.Filter);
            return File(_service.BuildExcelExport(impact), "application/vnd.ms-excel", "CriticalRecovery_AffectedRows.xls");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Initiate(CriticalRecoveryIndexViewModel model)
        {
            var context = RequireAdminContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            try
            {
                model = model ?? new CriticalRecoveryIndexViewModel();
                var result = _service.Initiate(model.Filter, model.Request, context.UserName, Request);
                TempData[result.Success ? "CriticalRecoveryMessage" : "CriticalRecoveryError"] = result.Message;
            }
            catch (Exception ex)
            {
                TempData["CriticalRecoveryError"] = ex.Message;
            }

            return RecoveryView(context, model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApproveAndExecute(CriticalRecoveryIndexViewModel model)
        {
            var context = RequireAdminContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            try
            {
                model = model ?? new CriticalRecoveryIndexViewModel();
                if (model.Request == null)
                {
                    model.Request = new CriticalRecoveryRequestViewModel();
                }

                model.Request.Mode = CriticalRecoveryMode.FullRollback;
                model.Request.AllowPhysicalDelete = true;
                model.Request.DryRun = false;
                model.Request.DeleteOrphanKycRecords = false;

                if (!model.Request.RequestId.HasValue || model.Request.RequestId.Value <= 0)
                {
                    var initiateResult = _service.Initiate(model.Filter, model.Request, context.UserName, Request);
                    if (!initiateResult.Success || !initiateResult.RequestId.HasValue)
                    {
                        TempData["CriticalRecoveryError"] = initiateResult.Message;
                        return RecoveryView(context, model);
                    }

                    model.Request.RequestId = initiateResult.RequestId;
                }

                var result = _service.ApproveAndExecute(model.Request, context.UserName, Request);
                TempData[result.Success ? "CriticalRecoveryMessage" : "CriticalRecoveryError"] = result.Message;
            }
            catch (Exception ex)
            {
                TempData["CriticalRecoveryError"] = ex.Message;
            }

            return RecoveryView(context, model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Restore(CriticalRecoveryIndexViewModel model)
        {
            var context = RequireAdminContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            try
            {
                model = model ?? new CriticalRecoveryIndexViewModel();
                var result = _service.Restore(model.Restore, context.UserName, Request);
                TempData[result.Success ? "CriticalRecoveryMessage" : "CriticalRecoveryError"] = result.Message;
            }
            catch (Exception ex)
            {
                TempData["CriticalRecoveryError"] = ex.Message;
            }

            return RecoveryView(context, model);
        }

        public ActionResult MenuItem()
        {
            return PartialView("_CriticalRecoveryMenuItem", "Pos");
        }

        private PosUserContext RequireAdminContext()
        {
            var context = PosLoginController.RestorePosContext(Request, Session, _repository);
            if (context == null)
            {
                TempData["PosLoginMessage"] = PosLoginController.PosSessionExpiredMessage;
                return null;
            }

            if (context.UserType.GetValueOrDefault(-1) != 0 && !context.IsFullAccess)
            {
                throw new UnauthorizedAccessException("Critical Recovery Center requires POS admin access.");
            }

            return context;
        }

        private ActionResult RecoveryView(PosUserContext context, CriticalRecoveryIndexViewModel model)
        {
            model = model ?? new CriticalRecoveryIndexViewModel();
            var fresh = _service.BuildIndex("Pos");
            model.AreaName = "Pos";
            model.BranchOptions = fresh.BranchOptions;
            model.SnapshotBatches = fresh.SnapshotBatches;
            model.AuditItems = fresh.AuditItems;
            if (model.Impact == null)
            {
                model.Impact = new CriticalRecoveryImpactViewModel();
            }

            ViewBag.PosContext = context;
            ViewBag.ActiveScreen = "critical-recovery";
            return View("Index", model);
        }
    }
}
