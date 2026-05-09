using MyERP.Areas.MainErp.Models.Security;
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
        private readonly DatabaseMigrationService _service;

        public DatabaseMigrationController()
            : this(new DatabaseMigrationService())
        {
        }

        public DatabaseMigrationController(DatabaseMigrationService service)
        {
            _service = service;
        }

        public ActionResult Index()
        {
            var denied = RequirePermission(DatabaseMigrationPermissions.View);
            if (denied != null) return denied;

            ViewBag.ActiveScreen = "database-migration";
            ViewBag.Title = "إدارة تحديثات قاعدة البيانات";
            return View(_service.BuildDashboard(TempData["DatabaseMigration.LastRun"] as MigrationRunResult));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DryRun()
        {
            var denied = RequirePermission(DatabaseMigrationPermissions.Manage);
            if (denied != null) return denied;

            TempData["DatabaseMigration.LastRun"] = _service.DryRun(MainErpUserContext);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApplySelected(MigrationRunRequest request)
        {
            var denied = RequirePermission(DatabaseMigrationPermissions.Apply);
            if (denied != null) return denied;

            try
            {
                TempData["DatabaseMigration.LastRun"] = _service.Apply(request, MainErpUserContext, false);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Database migration apply selected blocked: " + ex);
                TempData["DatabaseMigration.Error"] = SafeUiMessage(ex);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApplyAll(MigrationRunRequest request)
        {
            var denied = RequirePermission(DatabaseMigrationPermissions.Apply);
            if (denied != null) return denied;

            try
            {
                TempData["DatabaseMigration.LastRun"] = _service.Apply(request, MainErpUserContext, true);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Database migration apply all blocked: " + ex);
                TempData["DatabaseMigration.Error"] = SafeUiMessage(ex);
            }

            return RedirectToAction("Index");
        }

        public ActionResult PreviewScript(string id)
        {
            var denied = RequirePermission(DatabaseMigrationPermissions.View);
            if (denied != null) return denied;

            try
            {
                ViewBag.ActiveScreen = "database-migration";
                ViewBag.Title = "معاينة سكريبت تحديث قاعدة البيانات";
                return View(_service.PreviewScript(id));
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Database migration preview failed: " + ex);
                return new HttpStatusCodeResult(HttpStatusCode.NotFound, "Script not found");
            }
        }

        public ActionResult History()
        {
            return RedirectToAction("Index");
        }

        public ActionResult ExportReport()
        {
            var denied = RequirePermission(DatabaseMigrationPermissions.View);
            if (denied != null) return denied;

            var report = _service.ExportReport();
            var fileName = "DatabaseMigrationReport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
            return File(report, "text/csv", fileName);
        }

        private ActionResult RequirePermission(string permission)
        {
            if (MainErpUserContext == null)
            {
                return RedirectToAction("Index", "Login", new { area = "MainErp", returnUrl = Request.RawUrl });
            }

            if (!HasPermission(MainErpUserContext, permission))
            {
                Response.StatusCode = 403;
                return View("~/Views/Shared/Error.cshtml");
            }

            return null;
        }

        private static bool HasPermission(MainErpUserContext context, string permission)
        {
            if (context == null || !context.IsAdmin)
            {
                return false;
            }

            return string.Equals(permission, DatabaseMigrationPermissions.View, StringComparison.OrdinalIgnoreCase)
                || string.Equals(permission, DatabaseMigrationPermissions.Manage, StringComparison.OrdinalIgnoreCase)
                || string.Equals(permission, DatabaseMigrationPermissions.Apply, StringComparison.OrdinalIgnoreCase);
        }

        private static string SafeUiMessage(Exception ex)
        {
            if (ex == null)
            {
                return "تعذر تنفيذ العملية.";
            }

            if (ex is InvalidOperationException)
            {
                return ex.Message;
            }

            return "تعذر تنفيذ العملية. تمت كتابة التفاصيل في سجل النظام.";
        }
    }
}

