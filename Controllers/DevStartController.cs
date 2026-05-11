using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Infrastructure;
using System;
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
        public ActionResult SelectMainErpDatabase(string databaseName, string customDatabaseName)
        {
            if (!IsEnabled())
            {
                return HttpNotFound();
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
            return RedirectToAction("Index");
        }

        private void PopulateViewBag()
        {
            ViewBag.KishnyCashConnection = DescribeConnection("KishnyCashConnection");
            ViewBag.MyErpConnection = DescribeConnection("MyERP_ConnectionString");
            ViewBag.MainErpConnection = DescribeConnection("MainErp_ConnectionString");
            ViewBag.MainErpSelectedDatabase = MainErpDebugDatabaseOverride.GetSelectedDatabaseName();
            ViewBag.MainErpDisplayDatabase = MainErpDebugDatabaseOverride.GetDisplayDatabaseName();
            ViewBag.OriginalWebDebugDatabase = DebugConnectionStringOverride.GetOriginalWebDatabase();
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
