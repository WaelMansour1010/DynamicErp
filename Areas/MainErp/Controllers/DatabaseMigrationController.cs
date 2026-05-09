using MyERP.Areas.MainErp.Security;
using MyERP.Areas.MainErp.Services.DatabaseMigration;
using MyERP.Areas.MainErp.ViewModels.DatabaseMigration;
using System;
using System.Diagnostics;
using System.Net;
using System.Web.Mvc;

namespace MyERP.Areas.MainErp.Controllers
{
    public class DatabaseMigrationController : MainErpControllerBase
    {
        private readonly DatabaseMigrationService _service = new DatabaseMigrationService();

        public ActionResult Index()
        {
            var denied = RequireAdmin(DatabaseMigrationPermissions.View);
            if (denied != null) return denied;
            ViewBag.ActiveScreen = "database-migration";
            ViewBag.Title = "Database Update Manager";
            return View(_service.BuildDashboard(TempData["DatabaseMigration.LastRun"] as MigrationRunResult));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DryRun()
        {
            var denied = RequireAdmin(DatabaseMigrationPermissions.Manage);
            if (denied != null) return denied;
            TempData["DatabaseMigration.LastRun"] = _service.DryRun(MainErpUserContext);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApplySelected(MigrationRunRequest request)
        {
            var denied = RequireAdmin(DatabaseMigrationPermissions.Apply);
            if (denied != null) return denied;
            try { TempData["DatabaseMigration.LastRun"] = _service.Apply(request, MainErpUserContext, false); }
            catch (Exception ex) { Trace.TraceWarning("Database migration apply selected blocked: " + ex); TempData["DatabaseMigration.Error"] = SafeMessage(ex); }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApplyAll(MigrationRunRequest request)
        {
            var denied = RequireAdmin(DatabaseMigrationPermissions.Apply);
            if (denied != null) return denied;
            try { TempData["DatabaseMigration.LastRun"] = _service.Apply(request, MainErpUserContext, true); }
            catch (Exception ex) { Trace.TraceWarning("Database migration apply all blocked: " + ex); TempData["DatabaseMigration.Error"] = SafeMessage(ex); }
            return RedirectToAction("Index");
        }

        public ActionResult PreviewScript(string id)
        {
            var denied = RequireAdmin(DatabaseMigrationPermissions.View);
            if (denied != null) return denied;
            ViewBag.ActiveScreen = "database-migration";
            ViewBag.Title = "Preview Migration Script";
            try { return View(_service.PreviewScript(id)); }
            catch { return new HttpStatusCodeResult(HttpStatusCode.NotFound, "Script not found"); }
        }

        public ActionResult History()
        {
            return RedirectToAction("Index");
        }

        public ActionResult ExportReport()
        {
            var denied = RequireAdmin(DatabaseMigrationPermissions.View);
            if (denied != null) return denied;
            return File(_service.ExportReport(), "text/csv", "DatabaseMigrationReport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
        }

        private ActionResult RequireAdmin(string permission)
        {
            if (MainErpUserContext == null) return RedirectToAction("Index", "Login", new { area = "MainErp", returnUrl = Request.RawUrl });
            if (!MainErpUserContext.IsAdmin) { Response.StatusCode = 403; return View("~/Views/Shared/Error.cshtml"); }
            return null;
        }

        private static string SafeMessage(Exception ex)
        {
            return ex is InvalidOperationException ? ex.Message : "The operation could not be completed. Details were written to the system log.";
        }
    }
}
