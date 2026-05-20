using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Infrastructure;
using MyERP.Models.PropertyMigration;
using MyERP.Services.PropertyMigration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Web.Mvc;

namespace MyERP.Controllers
{
    [AllowAnonymous]
    [SkipERPAuthorize]
    public class DevStartController : Controller
    {
        [HttpGet]
        public ActionResult Root()
        {
            if (IsEnabled())
            {
                PopulateViewBag();
                return View("Index");
            }

            return Redirect("~/Home/Index");
        }

        [HttpGet]
        public ActionResult Index()
        {
            if (!IsEnabled())
            {
                return HttpNotFound();
            }

            PopulateViewBag();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SelectMainErpDatabase(string connectionStringName, string databaseName, string customDatabaseName)
        {
            if (!IsEnabled())
            {
                return HttpNotFound();
            }

            if (!string.IsNullOrWhiteSpace(connectionStringName) && ConfigurationManager.ConnectionStrings[connectionStringName] != null)
            {
                MainErpDbConnectionFactory.SetSelectedDebugConnectionStringName(connectionStringName);
            }

            MainErpDebugDatabaseOverride.SetSelectedDatabaseName(string.IsNullOrWhiteSpace(customDatabaseName) ? databaseName : customDatabaseName);
            return Redirect("~/MainErp");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SelectOriginalWebDatabase(string databaseName, string customDatabaseName)
        {
            if (!IsEnabled())
            {
                return HttpNotFound();
            }

            DebugConnectionStringOverride.ApplyOriginalWebDatabase(string.IsNullOrWhiteSpace(customDatabaseName) ? databaseName : customDatabaseName);
            return Redirect("~/Home/Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ClearMainErpDatabase()
        {
            if (!IsEnabled())
            {
                return HttpNotFound();
            }

            MainErpDebugDatabaseOverride.SetSelectedDatabaseName(null);
            MainErpDbConnectionFactory.SetSelectedDebugConnectionStringName(null);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PropertyMigrationDryRun(PropertyMigrationRunnerRequest request)
        {
            if (!IsEnabled())
            {
                return HttpNotFound();
            }

            var result = new PropertyMigrationRunnerService().Run(request, true);
            StorePropertyMigrationResult(result);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PropertyMigrationRunStage(PropertyMigrationRunnerRequest request)
        {
            if (!IsEnabled())
            {
                return HttpNotFound();
            }

            var result = new PropertyMigrationRunnerService().Run(request, false);
            StorePropertyMigrationResult(result);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult PropertyMigrationLatestReport()
        {
            if (!IsEnabled())
            {
                return HttpNotFound();
            }

            var path = new PropertyMigrationRunnerService().GetLatestReportPath();
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return Content("No Property Migration Runner report was found.", "text/plain");
            }

            return Content(System.IO.File.ReadAllText(path), "text/plain");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OpenPropertyMigrationReportsFolder()
        {
            if (!IsEnabled())
            {
                return HttpNotFound();
            }

            OpenFolderSafe(new PropertyMigrationRunnerService().GetReportsDirectory());
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OpenPropertyMigrationDocsFolder()
        {
            if (!IsEnabled())
            {
                return HttpNotFound();
            }

            OpenFolderSafe(new PropertyMigrationRunnerService().GetToolkitDocsDirectory());
            return RedirectToAction("Index");
        }

        private void PopulateViewBag()
        {
            ViewBag.KishnyCashConnection = DescribeConnection("KishnyCashConnection");
            ViewBag.MyErpConnection = DescribeConnection("MyERP_ConnectionString");
            ViewBag.MainErpConnection = DescribeConnection(MainErpDbConnectionFactory.ResolveActiveConnectionStringName());
            ViewBag.MainErpSelectedConnection = MainErpDbConnectionFactory.ResolveActiveConnectionStringName();
            ViewBag.MainErpConnectionOptions = GetMainErpConnectionOptions();
            ViewBag.MainErpSelectedDatabase = MainErpDebugDatabaseOverride.GetSelectedDatabaseName();
            ViewBag.MainErpDisplayDatabase = MainErpDebugDatabaseOverride.GetDisplayDatabaseName();
            ViewBag.OriginalWebDebugDatabase = DebugConnectionStringOverride.GetOriginalWebDatabase();
            var propertyMigrationService = new PropertyMigrationRunnerService();
            ViewBag.PropertyMigrationSources = propertyMigrationService.GetDefaultSourceDatabases();
            ViewBag.PropertyMigrationTargets = propertyMigrationService.GetDefaultTargetDatabases();
            ViewBag.PropertyMigrationReportsDirectory = propertyMigrationService.GetReportsDirectory();
            ViewBag.PropertyMigrationDocsDirectory = propertyMigrationService.GetToolkitDocsDirectory();
        }

        private static IEnumerable<SelectListItem> GetMainErpConnectionOptions()
        {
            var selected = MainErpDbConnectionFactory.ResolveActiveConnectionStringName();
            foreach (ConnectionStringSettings setting in ConfigurationManager.ConnectionStrings)
            {
                if (setting == null || string.IsNullOrWhiteSpace(setting.Name) || !setting.Name.StartsWith("MainErp_", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return new SelectListItem
                {
                    Text = DescribeConnection(setting.Name),
                    Value = setting.Name,
                    Selected = string.Equals(setting.Name, selected, StringComparison.OrdinalIgnoreCase)
                };
            }
        }

        private static string DescribeConnection(string name)
        {
            var setting = ConfigurationManager.ConnectionStrings[name];
            if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
            {
                return name + ": not configured";
            }

            try
            {
                var builder = new SqlConnectionStringBuilder(setting.ConnectionString);
                return name + ": " + builder.DataSource + " / " + builder.InitialCatalog;
            }
            catch
            {
                return name + ": configured";
            }
        }

        private void StorePropertyMigrationResult(PropertyMigrationRunnerResult result)
        {
            TempData["PropertyMigrationStatus"] = result.Status;
            TempData["PropertyMigrationMessage"] = result.Message;
            TempData["PropertyMigrationReportPath"] = result.ReportPath;
            TempData["PropertyMigrationBatchId"] = result.BatchId;
            TempData["PropertyMigrationOutput"] = Truncate(result.StandardOutput, 3000);
            TempData["PropertyMigrationError"] = Truncate(result.StandardError, 3000);
        }

        private static void OpenFolderSafe(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo("explorer.exe", "\"" + path + "\"") { UseShellExecute = false });
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength) + "...";
        }

        private bool IsEnabled()
        {
#if DEBUG
            return Request != null && Request.IsLocal;
#else
            return false;
#endif
        }
    }
}



