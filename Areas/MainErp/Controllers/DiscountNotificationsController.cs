using System;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Common.DiscountNotifications;

namespace MyERP.Areas.MainErp.Controllers
{
    public class DiscountNotificationsController : MainErpControllerBase
    {
        private readonly DiscountNotificationReadRepository _repository;

        public DiscountNotificationsController()
        {
            var factory = new MainErpDbConnectionFactory();
            _repository = new DiscountNotificationReadRepository(factory.CreateOpenConnection);
        }

        public ActionResult Index(string searchText, int? branchId, DateTime? fromDate, DateTime? toDate, int? selectedId)
        {
            ViewBag.ActiveScreen = "discount-notifications";
            return View(_repository.Search(searchText, branchId, fromDate, toDate, selectedId, false));
        }

        public ActionResult Open(int id)
        {
            return RedirectToAction("Index", new { selectedId = id });
        }
    }
}
