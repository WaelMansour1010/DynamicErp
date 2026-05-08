using System;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.Payments;

namespace MyERP.Areas.MainErp.Controllers
{
    public class PaymentsController : MainErpControllerBase
    {
        private readonly PaymentVoucherReadRepository _repository;
        private readonly LegacyScreenPermissionService _permissionService;

        public PaymentsController()
            : this(new PaymentVoucherReadRepository(new MainErpDbConnectionFactory()), new LegacyScreenPermissionService(new MainErpDbConnectionFactory()))
        {
        }

        public PaymentsController(PaymentVoucherReadRepository repository, LegacyScreenPermissionService permissionService)
        {
            _repository = repository;
            _permissionService = permissionService;
        }

        public ActionResult Index(DateTime? fromDate, DateTime? toDate, string serial, string party, int? branchId, string cashboxOrBank, decimal? amount)
        {
            ViewBag.ActiveScreen = "payments";
            if (!_permissionService.CanView(MainErpUserContext, "FrmPayments"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية استعراض سندات الصرف");
            }

            return View(_repository.Search(fromDate, toDate, serial, party, branchId, cashboxOrBank, amount));
        }

        public ActionResult Details(int id)
        {
            ViewBag.ActiveScreen = "payments";
            if (!_permissionService.CanView(MainErpUserContext, "FrmPayments"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية استعراض سندات الصرف");
            }

            var model = _repository.GetDetails(id);
            if (model == null)
            {
                return HttpNotFound("Payment voucher was not found.");
            }

            return View(model);
        }
    }
}
