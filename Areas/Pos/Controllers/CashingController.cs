using System;
using System.Web.Mvc;
using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Repositories.Cashing;
using MyERP.Areas.Pos.Services;

namespace MyERP.Areas.Pos.Controllers
{
    public class CashingController : Controller
    {
        private readonly PosSqlRepository _posRepository;
        private readonly CashingVoucherReadRepository _repository;
        private readonly PosLegacyScreenPermissionService _legacyPermissionService;

        public CashingController()
        {
            _posRepository = new PosSqlRepository();
            _repository = new CashingVoucherReadRepository();
            _legacyPermissionService = new PosLegacyScreenPermissionService();
        }

        public ActionResult Index(DateTime? fromDate, DateTime? toDate, string serial, string party, int? branchId, string cashboxOrBank, decimal? amount)
        {
            var context = PosLoginController.RestorePosContext(Request, Session, _posRepository);
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!_legacyPermissionService.CanView(context, "FrmCashing"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية استعراض سندات القبض");
            }

            ViewBag.ActiveScreen = "cashing";
            return View(_repository.Search(fromDate, toDate, serial, party, context.IsFullAccess ? branchId : context.BranchId, cashboxOrBank, amount));
        }

        public ActionResult Details(int id)
        {
            var context = PosLoginController.RestorePosContext(Request, Session, _posRepository);
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!_legacyPermissionService.CanView(context, "FrmCashing"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية استعراض سندات القبض");
            }

            ViewBag.ActiveScreen = "cashing";
            var model = _repository.GetDetails(id);
            if (model == null)
            {
                return HttpNotFound("Cashing voucher was not found.");
            }

            return View(model);
        }
    }
}
