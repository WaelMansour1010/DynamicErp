using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using MyERP.Common.StoreData;

namespace MyERP.Areas.Pos.Controllers
{
    public class StoresController : Controller
    {
        private readonly StoreDataRepository _repository;

        public StoresController()
            : this(new StoreDataRepository(CreateOpenConnection))
        {
        }

        public StoresController(StoreDataRepository repository)
        {
            _repository = repository;
        }

        public ActionResult Index(string searchText, int? branchId)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            var resolvedBranchId = IsAdmin(context) ? branchId : context.BranchId;
            ViewBag.PosContext = context;
            ViewBag.ActiveScreen = "stores";
            ViewBag.SearchText = searchText;
            ViewBag.BranchId = resolvedBranchId;
            ViewBag.PosOperationalDatabase = GetDatabaseName();
            return View(_repository.GetOperationalStores(resolvedBranchId, searchText));
        }

        [HttpGet]
        public JsonResult Lookup(int? branchId, string term)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "يجب تسجيل دخول نقطة البيع أولا" }, JsonRequestBehavior.AllowGet);
            }

            var resolvedBranchId = IsAdmin(context) ? branchId : context.BranchId;
            return Json(new { success = true, rows = _repository.GetOperationalStores(resolvedBranchId, term) }, JsonRequestBehavior.AllowGet);
        }

        private PosUserContext GetPosContext()
        {
            var repository = new PosSqlRepository();
            return PosLoginController.RestorePosContext(Request, Session, repository);
        }

        private static bool IsAdmin(PosUserContext context)
        {
            return context != null && (context.IsFullAccess || context.UserType.GetValueOrDefault(-1) == 0 || context.CanChangeDefaults);
        }

        private static SqlConnection CreateOpenConnection()
        {
            var setting = ConfigurationManager.ConnectionStrings["KishnyCashConnection"];
            if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
            {
                throw new ConfigurationErrorsException("Missing connection string: KishnyCashConnection");
            }

            var connection = new SqlConnection(setting.ConnectionString);
            connection.Open();
            return connection;
        }

        private static string GetDatabaseName()
        {
            var setting = ConfigurationManager.ConnectionStrings["KishnyCashConnection"];
            if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
            {
                return string.Empty;
            }

            return new SqlConnectionStringBuilder(setting.ConnectionString).InitialCatalog;
        }
    }
}
