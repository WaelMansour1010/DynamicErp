using System;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.Cashing;

namespace MyERP.Areas.MainErp.Controllers
{
    public class CashingController : MainErpControllerBase
    {
        private readonly CashingVoucherReadRepository _repository;
        private readonly LegacyScreenPermissionService _permissionService;

        public CashingController()
            : this(new CashingVoucherReadRepository(new MainErpDbConnectionFactory()), new LegacyScreenPermissionService(new MainErpDbConnectionFactory()))
        {
        }

        public CashingController(CashingVoucherReadRepository repository, LegacyScreenPermissionService permissionService)
        {
            _repository = repository;
            _permissionService = permissionService;
        }

        public ActionResult Index(DateTime? fromDate, DateTime? toDate, string serial, string party, int? branchId, string cashboxOrBank, decimal? amount)
        {
            ViewBag.ActiveScreen = "cashing";
            if (!_permissionService.CanView(MainErpUserContext, "FrmCashing"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية استعراض سندات القبض");
            }

            return View(_repository.Search(fromDate, toDate, serial, party, branchId, cashboxOrBank, amount));
        }

        public ActionResult Details(int id)
        {
            ViewBag.ActiveScreen = "cashing";
            if (!_permissionService.CanView(MainErpUserContext, "FrmCashing"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية استعراض سندات القبض");
            }

            var model = _repository.GetDetails(id);
            if (model == null)
            {
                return HttpNotFound("Cashing voucher was not found.");
            }

            return View(model);
        }
    }
}
