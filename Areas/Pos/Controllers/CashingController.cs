using System;
using System.Diagnostics;
using System.Web.Mvc;
using MyERP.Areas.MainErp.ViewModels.Payments;
using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Repositories.Cashing;
using MyERP.Areas.Pos.Repositories.Payments;
using MyERP.Areas.Pos.Services;

namespace MyERP.Areas.Pos.Controllers
{
    public class CashingController : Controller
    {
        private readonly PosSqlRepository _posRepository;
        private readonly CashingVoucherReadRepository _repository;
        private readonly PaymentVoucherWriteRepository _writeRepository;
        private readonly PosLegacyScreenPermissionService _legacyPermissionService;

        public CashingController()
        {
            _posRepository = new PosSqlRepository();
            _repository = new CashingVoucherReadRepository();
            _writeRepository = new PaymentVoucherWriteRepository();
            _legacyPermissionService = new PosLegacyScreenPermissionService();
        }

        public ActionResult Index(DateTime? fromDate, DateTime? toDate, string serial, string party, int? branchId, string cashboxOrBank, decimal? amount, int page = 1, int pageSize = 50)
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
            return View(_repository.Search(fromDate, toDate, serial, party, context.IsFullAccess ? branchId : context.BranchId, cashboxOrBank, amount, page, pageSize));
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

        public ActionResult Create()
        {
            var context = PosLoginController.RestorePosContext(Request, Session, _posRepository);
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!_legacyPermissionService.CanAdd(context, "FrmCashing"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية إضافة سند قبض");
            }

            ViewBag.ActiveScreen = "cashing";
            var model = _writeRepository.CreateEditModel(4, "إضافة سند قبض", "FrmCashing", "Cashing", "Index");
            if (!context.IsFullAccess)
            {
                model.BranchId = context.BranchId;
            }

            return View("~/Areas/Pos/Views/Payments/Edit.cshtml", model);
        }

        public ActionResult Edit(int id)
        {
            var context = PosLoginController.RestorePosContext(Request, Session, _posRepository);
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!_legacyPermissionService.CanEdit(context, "FrmCashing"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية تعديل سند قبض");
            }

            var details = _repository.GetDetails(id);
            if (details == null)
            {
                return HttpNotFound("Cashing voucher was not found.");
            }

            ViewBag.ActiveScreen = "cashing";
            return View("~/Areas/Pos/Views/Payments/Edit.cshtml", _writeRepository.CreateEditModelFromDetails(details, 4, "تعديل سند قبض", "FrmCashing", "Cashing", "Index"));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(PaymentVoucherEditViewModel model)
        {
            var context = PosLoginController.RestorePosContext(Request, Session, _posRepository);
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            var isEdit = model != null && model.NoteId.HasValue;
            if (isEdit ? !_legacyPermissionService.CanEdit(context, "FrmCashing") : !_legacyPermissionService.CanAdd(context, "FrmCashing"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية حفظ سند قبض");
            }

            model = model ?? new PaymentVoucherEditViewModel();
            model.NoteType = 4;
            if (!context.IsFullAccess)
            {
                model.BranchId = context.BranchId;
            }

            var validationMessage = PaymentsController.ValidateVoucher(model);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                return CashingEditView(model, isEdit, validationMessage);
            }

            try
            {
                var result = _writeRepository.Save(model, context.UserId);
                TempData["VoucherMessage"] = result.Message;
                return RedirectToAction("Details", new { id = result.NoteId });
            }
            catch (Exception ex)
            {
                Trace.TraceError("POS cashing voucher save failed. NoteId={0}. {1}", model.NoteId, ex);
                return CashingEditView(model, isEdit, PaymentsController.FriendlyVoucherError(ex));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Post(int id)
        {
            var context = PosLoginController.RestorePosContext(Request, Session, _posRepository);
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!_legacyPermissionService.CanEdit(context, "FrmCashing"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية ترحيل سند قبض");
            }

            try
            {
                _writeRepository.Post(4, id, context.UserId);
                TempData["VoucherMessage"] = "تم ترحيل السند.";
            }
            catch (Exception ex)
            {
                Trace.TraceError("POS cashing voucher post failed. NoteId={0}. {1}", id, ex);
                TempData["VoucherError"] = PaymentsController.FriendlyVoucherError(ex);
            }

            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var context = PosLoginController.RestorePosContext(Request, Session, _posRepository);
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!_legacyPermissionService.CanDelete(context, "FrmCashing"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية حذف سند قبض");
            }

            try
            {
                _writeRepository.Delete(4, id, context.UserId);
                TempData["VoucherMessage"] = "تم حذف السند.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Trace.TraceError("POS cashing voucher delete failed. NoteId={0}. {1}", id, ex);
                TempData["VoucherError"] = PaymentsController.FriendlyVoucherError(ex);
                return RedirectToAction("Details", new { id });
            }
        }

        public ActionResult Print(int id)
        {
            var context = PosLoginController.RestorePosContext(Request, Session, _posRepository);
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!_legacyPermissionService.CanPrint(context, "FrmCashing"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية طباعة سند قبض");
            }

            var model = _repository.GetDetails(id);
            if (model == null)
            {
                return HttpNotFound("Cashing voucher was not found.");
            }

            return View("~/Areas/Pos/Views/Payments/Print.cshtml", model);
        }

        public ActionResult LegacyCrystalPrintVoucher(int id)
        {
            var context = PosLoginController.RestorePosContext(Request, Session, _posRepository);
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!_legacyPermissionService.CanPrint(context, "FrmCashing"))
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
            return View("~/Areas/Pos/Views/Payments/Edit.cshtml", _writeRepository.WithLookups(model));
        }
    }
}
