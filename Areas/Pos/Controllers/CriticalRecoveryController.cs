using System;
using System.Web.Mvc;
using MyERP.Areas.Shared.CriticalRecovery;

namespace MyERP.Areas.Pos.Controllers
{
    [CriticalRecoveryAuthorize]
    public class CriticalRecoveryController : Controller
    {
        private readonly CriticalRecoveryService _service = new CriticalRecoveryService("Pos");

        public ActionResult Index()
        {
            return View("~/Areas/Shared/CriticalRecovery/Views/Index.cshtml", _service.BuildIndex("Pos"));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Analyze(CriticalRecoveryIndexViewModel model)
        {
            model = model ?? new CriticalRecoveryIndexViewModel();
            model.AreaName = "Pos";
            model.Impact = _service.Analyze(model.Filter);
            var fresh = _service.BuildIndex("Pos");
            model.SnapshotBatches = fresh.SnapshotBatches;
            model.AuditItems = fresh.AuditItems;
            TempData["CriticalRecoveryMessage"] = "Preview only. No data was changed.";
            return View("~/Areas/Shared/CriticalRecovery/Views/Index.cshtml", model);
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
                var result = _service.Initiate(model.Filter, model.Request, CurrentUserName(), Request);
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
            return PartialView("~/Areas/Shared/CriticalRecovery/Views/_CriticalRecoveryMenuItem.cshtml", "Pos");
        }

        private string CurrentUserName()
        {
            return User == null || User.Identity == null ? string.Empty : User.Identity.Name;
        }
    }
}
