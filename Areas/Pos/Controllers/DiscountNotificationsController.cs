using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using MyERP.Common.DiscountNotifications;

namespace MyERP.Areas.Pos.Controllers
{
    public class DiscountNotificationsController : Controller
    {
        private readonly PosSqlRepository _posRepository = new PosSqlRepository();
        private readonly DiscountNotificationReadRepository _repository;

        public DiscountNotificationsController()
        {
            _repository = new DiscountNotificationReadRepository(CreateOpenConnection);
        }

        public ActionResult Index(string searchText, int? branchId, DateTime? fromDate, DateTime? toDate, int? selectedId)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            ViewBag.PosContext = context;
            ViewBag.ActiveScreen = "discount-notifications";
            if (!IsAdmin(context) && context.BranchId.HasValue)
            {
                branchId = context.BranchId.Value;
            }

            return View(_repository.Search(searchText, branchId, fromDate, toDate, selectedId, true));
        }

        public ActionResult Open(int id)
        {
            return RedirectToAction("Index", new { selectedId = id });
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

        private static bool IsAdmin(PosUserContext context)
        {
            return context != null && (context.UserType.GetValueOrDefault(-1) == 0 || context.IsFullAccess);
        }

        private PosUserContext GetPosContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _posRepository);
        }
    }
}
