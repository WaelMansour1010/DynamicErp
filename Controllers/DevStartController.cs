using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Infrastructure;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
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
