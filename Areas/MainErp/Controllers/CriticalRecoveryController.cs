using System;
using System.Web.Mvc;
using MyERP.Areas.Shared.CriticalRecovery;

namespace MyERP.Areas.MainErp.Controllers
{
    [CriticalRecoveryAuthorize]
    public class CriticalRecoveryController : Controller
    {
        private readonly CriticalRecoveryService _service = new CriticalRecoveryService("MainErp");

        public ActionResult Index()
        {
            return View("~/Areas/Shared/CriticalRecovery/Views/Index.cshtml", _service.BuildIndex("MainErp"));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Analyze(CriticalRecoveryIndexViewModel model)
        {
            return RenderWithImpact(model, "Preview only. No data was changed.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Export(CriticalRecoveryIndexViewModel model)
        {
            model = model ?? new CriticalRecoveryIndexViewModel();
            var impact = _service.Analyze(model.Filter);
            return File(_service.BuildExcelExport(impact), "application/vnd.ms-excel", "CriticalRecovery_AffectedRows.xls");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Initiate(CriticalRecoveryIndexViewModel model)
        {
            try
            {
                model = model ?? new CriticalRecoveryIndexViewModel();
                model.Impact = _service.Analyze(model.Filter);
                var result = _service.Initiate(model.Filter, model.Request, CurrentUserName(), Request);
                TempData[result.Success ? "CriticalRecoveryMessage" : "CriticalRecoveryError"] = result.Message;
                if (result.RequestId.HasValue)
                {
                    TempData["CriticalRecoveryRequestId"] = result.RequestId.Value;
                }
            }
            catch (Exception ex)
            {
                TempData["CriticalRecoveryError"] = ex.Message;
            }

            return View("~/Areas/Shared/CriticalRecovery/Views/Index.cshtml", Hydrate(model));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApproveAndExecute(CriticalRecoveryIndexViewModel model)
        {
            try
            {
                model = model ?? new CriticalRecoveryIndexViewModel();
                var result = _service.ApproveAndExecute(model.Request, CurrentUserName(), Request);
                TempData[result.Success ? "CriticalRecoveryMessage" : "CriticalRecoveryError"] = result.Message;
            }
            catch (Exception ex)
            {
                TempData["CriticalRecoveryError"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Restore(CriticalRecoveryIndexViewModel model)
        {
            try
            {
                model = model ?? new CriticalRecoveryIndexViewModel();
                var result = _service.Restore(model.Restore, CurrentUserName(), Request);
                TempData[result.Success ? "CriticalRecoveryMessage" : "CriticalRecoveryError"] = result.Message;
            }
            catch (Exception ex)
            {
                TempData["CriticalRecoveryError"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        public ActionResult MenuItem()
        {
            return PartialView("~/Areas/Shared/CriticalRecovery/Views/_CriticalRecoveryMenuItem.cshtml", "MainErp");
        }

        private ActionResult RenderWithImpact(CriticalRecoveryIndexViewModel model, string message)
        {
            model = model ?? new CriticalRecoveryIndexViewModel();
            model.AreaName = "MainErp";
            model.Impact = _service.Analyze(model.Filter);
            TempData["CriticalRecoveryMessage"] = message;
            return View("~/Areas/Shared/CriticalRecovery/Views/Index.cshtml", Hydrate(model));
        }

        private CriticalRecoveryIndexViewModel Hydrate(CriticalRecoveryIndexViewModel model)
        {
            var fresh = _service.BuildIndex("MainErp");
            model.AreaName = "MainErp";
            model.SnapshotBatches = fresh.SnapshotBatches;
            model.AuditItems = fresh.AuditItems;
            return model;
        }

        private string CurrentUserName()
        {
            return User == null || User.Identity == null ? string.Empty : User.Identity.Name;
        }
    }
}
