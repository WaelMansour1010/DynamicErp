using System;
using System.Diagnostics;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.Cashing;
using MyERP.Areas.MainErp.Repositories.Payments;
using MyERP.Areas.MainErp.ViewModels.Payments;

namespace MyERP.Areas.MainErp.Controllers
{
    public class CashingController : MainErpControllerBase
    {
        private readonly CashingVoucherReadRepository _repository;
        private readonly PaymentVoucherWriteRepository _writeRepository;
        private readonly LegacyScreenPermissionService _permissionService;

        public CashingController()
            : this(new CashingVoucherReadRepository(new MainErpDbConnectionFactory()), new PaymentVoucherWriteRepository(new MainErpDbConnectionFactory()), new LegacyScreenPermissionService(new MainErpDbConnectionFactory()))
        {
        }

        public CashingController(CashingVoucherReadRepository repository, PaymentVoucherWriteRepository writeRepository, LegacyScreenPermissionService permissionService)
        {
            _repository = repository;
            _writeRepository = writeRepository;
            _permissionService = permissionService;
        }

        public ActionResult Index(DateTime? fromDate, DateTime? toDate, string serial, string party, int? branchId, string cashboxOrBank, decimal? amount, int page = 1, int pageSize = 50)
        {
            ViewBag.ActiveScreen = "cashing";
            if (!_permissionService.CanView(MainErpUserContext, "FrmCashing"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية استعراض سندات القبض");
            }

            return View(_repository.Search(fromDate, toDate, serial, party, branchId, cashboxOrBank, amount, page, pageSize));
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

        public ActionResult Create()
        {
            ViewBag.ActiveScreen = "cashing";
            if (!_permissionService.CanAdd(MainErpUserContext, "FrmCashing"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية إضافة سند قبض");
            }

            return View("~/Areas/MainErp/Views/Payments/Edit.cshtml", _writeRepository.CreateEditModel(4, "إضافة سند قبض", "FrmCashing", "Cashing", "Index"));
        }

        public ActionResult Edit(int id)
        {
            ViewBag.ActiveScreen = "cashing";
            if (!_permissionService.CanEdit(MainErpUserContext, "FrmCashing"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية تعديل سند قبض");
            }

            var details = _repository.GetDetails(id);
            if (details == null)
            {
                return HttpNotFound("Cashing voucher was not found.");
            }

            return View("~/Areas/MainErp/Views/Payments/Edit.cshtml", _writeRepository.CreateEditModelFromDetails(details, 4, "تعديل سند قبض", "FrmCashing", "Cashing", "Index"));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(PaymentVoucherEditViewModel model)
        {
            ViewBag.ActiveScreen = "cashing";
            var isEdit = model != null && model.NoteId.HasValue;
            if (!_permissionService.CanView(MainErpUserContext, "FrmCashing") || (isEdit ? !_permissionService.CanEdit(MainErpUserContext, "FrmCashing") : !_permissionService.CanAdd(MainErpUserContext, "FrmCashing")))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية حفظ سند قبض");
            }

            model = model ?? new PaymentVoucherEditViewModel();
            model.NoteType = 4;
            var validationMessage = PaymentsController.ValidateVoucher(model);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                return CashingEditView(model, isEdit, validationMessage);
            }

            try
            {
                var result = _writeRepository.Save(model, MainErpUserContext.UserId);
                TempData["VoucherMessage"] = result.Message;
                return RedirectToAction("Details", new { id = result.NoteId });
            }
            catch (Exception ex)
            {
                Trace.TraceError("MainErp cashing voucher save failed. NoteId={0}. {1}", model.NoteId, ex);
                return CashingEditView(model, isEdit, PaymentsController.FriendlyVoucherError(ex));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Post(int id)
        {
            if (!_permissionService.CanEdit(MainErpUserContext, "FrmCashing"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية ترحيل سند قبض");
            }

            try
            {
                _writeRepository.Post(4, id, MainErpUserContext.UserId);
                TempData["VoucherMessage"] = "تم ترحيل السند.";
            }
            catch (Exception ex)
            {
                Trace.TraceError("MainErp cashing voucher post failed. NoteId={0}. {1}", id, ex);
                TempData["VoucherError"] = PaymentsController.FriendlyVoucherError(ex);
            }

            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            if (!_permissionService.CanDelete(MainErpUserContext, "FrmCashing"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية حذف سند قبض");
            }

            try
            {
                _writeRepository.Delete(4, id, MainErpUserContext.UserId);
                TempData["VoucherMessage"] = "تم حذف السند.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Trace.TraceError("MainErp cashing voucher delete failed. NoteId={0}. {1}", id, ex);
                TempData["VoucherError"] = PaymentsController.FriendlyVoucherError(ex);
                return RedirectToAction("Details", new { id });
            }
        }

        public ActionResult Print(int id)
        {
            if (!_permissionService.CanPrint(MainErpUserContext, "FrmCashing"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية طباعة سند قبض");
            }

            var model = _repository.GetDetails(id);
            if (model == null)
            {
                return HttpNotFound("Cashing voucher was not found.");
            }

            return View("~/Areas/MainErp/Views/Payments/Print.cshtml", model);
        }

        public ActionResult LegacyCrystalPrint(int id)
        {
            if (!_permissionService.CanPrint(MainErpUserContext, "FrmCashing"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية طباعة سند قبض");
            }

            var profile = _repository.GetLegacyPrintProfile(id);
            if (profile == null)
            {
                return HttpNotFound("Cashing voucher was not found.");
            }

            Response.StatusCode = profile.CrystalParityReady ? 200 : 501;
            return Json(profile, JsonRequestBehavior.AllowGet);
        }

        private ActionResult CashingEditView(PaymentVoucherEditViewModel model, bool isEdit, string warning)
        {
            model.Title = isEdit ? "تعديل سند قبض" : "إضافة سند قبض";
            model.ScreenName = "FrmCashing";
            model.BackController = "Cashing";
            model.BackAction = "Index";
            model.Warning = warning;
            return View("~/Areas/MainErp/Views/Payments/Edit.cshtml", _writeRepository.WithLookups(model));
        }
    }
}
