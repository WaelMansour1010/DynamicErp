using System;
using System.Diagnostics;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.Payments;
using MyERP.Areas.MainErp.ViewModels.Payments;

namespace MyERP.Areas.MainErp.Controllers
{
    public class PaymentsController : MainErpControllerBase
    {
        private readonly PaymentVoucherReadRepository _repository;
        private readonly PaymentVoucherWriteRepository _writeRepository;
        private readonly LegacyScreenPermissionService _permissionService;

        public PaymentsController()
            : this(new PaymentVoucherReadRepository(new MainErpDbConnectionFactory()), new PaymentVoucherWriteRepository(new MainErpDbConnectionFactory()), new LegacyScreenPermissionService(new MainErpDbConnectionFactory()))
        {
        }

        public PaymentsController(PaymentVoucherReadRepository repository, PaymentVoucherWriteRepository writeRepository, LegacyScreenPermissionService permissionService)
        {
            _repository = repository;
            _writeRepository = writeRepository;
            _permissionService = permissionService;
        }

        public ActionResult Index(DateTime? fromDate, DateTime? toDate, string serial, string party, int? branchId, string cashboxOrBank, decimal? amount, int page = 1, int pageSize = 50)
        {
            ViewBag.ActiveScreen = "payments";
            if (!_permissionService.CanView(MainErpUserContext, "FrmPayments"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية استعراض سندات الصرف");
            }

            return View(_repository.Search(fromDate, toDate, serial, party, branchId, cashboxOrBank, amount, page, pageSize));
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

        public ActionResult Create()
        {
            ViewBag.ActiveScreen = "payments";
            if (!_permissionService.CanAdd(MainErpUserContext, "FrmPayments"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية إضافة سند صرف");
            }

            return View("Edit", _writeRepository.CreateEditModel(5, "إضافة سند صرف", "FrmPayments", "Payments", "Index"));
        }

        public ActionResult Edit(int id)
        {
            ViewBag.ActiveScreen = "payments";
            if (!_permissionService.CanEdit(MainErpUserContext, "FrmPayments"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية تعديل سند صرف");
            }

            var details = _repository.GetDetails(id);
            if (details == null)
            {
                return HttpNotFound("Payment voucher was not found.");
            }

            return View("Edit", _writeRepository.CreateEditModelFromDetails(details, 5, "تعديل سند صرف", "FrmPayments", "Payments", "Index"));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(PaymentVoucherEditViewModel model)
        {
            ViewBag.ActiveScreen = "payments";
            var isEdit = model != null && model.NoteId.HasValue;
            if (!_permissionService.CanView(MainErpUserContext, "FrmPayments") || (isEdit ? !_permissionService.CanEdit(MainErpUserContext, "FrmPayments") : !_permissionService.CanAdd(MainErpUserContext, "FrmPayments")))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية حفظ سند صرف");
            }

            model = model ?? new PaymentVoucherEditViewModel();
            model.NoteType = 5;
            var validationMessage = ValidateVoucher(model);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                return PaymentEditView(model, isEdit, validationMessage);
            }

            try
            {
                var result = _writeRepository.Save(model, MainErpUserContext.UserId);
                TempData["VoucherMessage"] = result.Message;
                return RedirectToAction("Details", new { id = result.NoteId });
            }
            catch (Exception ex)
            {
                Trace.TraceError("MainErp payment voucher save failed. NoteId={0}. {1}", model.NoteId, ex);
                return PaymentEditView(model, isEdit, FriendlyVoucherError(ex));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Post(int id)
        {
            if (!_permissionService.CanEdit(MainErpUserContext, "FrmPayments"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية ترحيل سند صرف");
            }

            try
            {
                _writeRepository.Post(5, id, MainErpUserContext.UserId);
                TempData["VoucherMessage"] = "تم ترحيل السند.";
            }
            catch (Exception ex)
            {
                Trace.TraceError("MainErp payment voucher post failed. NoteId={0}. {1}", id, ex);
                TempData["VoucherError"] = FriendlyVoucherError(ex);
            }

            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            if (!_permissionService.CanDelete(MainErpUserContext, "FrmPayments"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية حذف سند صرف");
            }

            try
            {
                _writeRepository.Delete(5, id, MainErpUserContext.UserId);
                TempData["VoucherMessage"] = "تم حذف السند.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Trace.TraceError("MainErp payment voucher delete failed. NoteId={0}. {1}", id, ex);
                TempData["VoucherError"] = FriendlyVoucherError(ex);
                return RedirectToAction("Details", new { id });
            }
        }

        public ActionResult Print(int id)
        {
            if (!_permissionService.CanPrint(MainErpUserContext, "FrmPayments"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية طباعة سند صرف");
            }

            var model = _repository.GetDetails(id);
            if (model == null)
            {
                return HttpNotFound("Payment voucher was not found.");
            }

            return View("Print", model);
        }

        public ActionResult LegacyCrystalPrint(int id)
        {
            if (!_permissionService.CanPrint(MainErpUserContext, "FrmPayments"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية طباعة سند صرف");
            }

            var profile = _repository.GetLegacyPrintProfile(id);
            if (profile == null)
            {
                return HttpNotFound("Payment voucher was not found.");
            }

            Response.StatusCode = profile.CrystalParityReady ? 200 : 501;
            return Json(profile, JsonRequestBehavior.AllowGet);
        }

        private ActionResult PaymentEditView(PaymentVoucherEditViewModel model, bool isEdit, string warning)
        {
            model.Title = isEdit ? "تعديل سند صرف" : "إضافة سند صرف";
            model.ScreenName = "FrmPayments";
            model.BackController = "Payments";
            model.BackAction = "Index";
            model.Warning = warning;
            return View("Edit", _writeRepository.WithLookups(model));
        }

        internal static string ValidateVoucher(PaymentVoucherEditViewModel model)
        {
            if (model == null)
            {
                return "بيانات السند غير مكتملة. أعد فتح الشاشة وحاول مرة أخرى.";
            }

            if (model.NoteDate == default(DateTime))
            {
                return "يجب تحديد تاريخ السند.";
            }

            if (string.IsNullOrWhiteSpace(model.PartyAccountCode))
            {
                return "يجب اختيار حساب الطرف.";
            }

            if (model.Amount <= 0)
            {
                return "يجب إدخال مبلغ أكبر من صفر.";
            }

            if (model.BoxId.HasValue == model.BankId.HasValue)
            {
                return "اختر خزنة أو بنك واحد فقط للسند.";
            }

            return null;
        }

        internal static string FriendlyVoucherError(Exception ex)
        {
            var message = (ex == null ? string.Empty : ex.Message) ?? string.Empty;
            if (ContainsArabic(message))
            {
                return message;
            }
            if (message.IndexOf("Posted voucher", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "لا يمكن تعديل أو حذف سند مرحل.";
            }

            if (message.IndexOf("Allocated", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "لا يمكن تعديل أو حذف سند مرتبط بتخصيصات أو فواتير. افتح المصدر المرتبط لإجراء التعديل الصحيح.";
            }

            if (message.IndexOf("Party account", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "حساب الطرف غير صحيح أو غير موجود.";
            }

            if (message.IndexOf("Cashbox", StringComparison.OrdinalIgnoreCase) >= 0 || message.IndexOf("bank", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "بيانات الخزنة أو البنك غير صحيحة.";
            }

            if (message.IndexOf("amount", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "المبلغ غير صحيح.";
            }

            return "تعذر تنفيذ العملية الآن. راجع البيانات وحاول مرة أخرى، وإذا استمرت المشكلة راجع مسؤول النظام.";
        }

        private static bool ContainsArabic(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            foreach (var c in value)
            {
                if (c >= '\u0600' && c <= '\u06FF')
                {
                    return true;
                }
            }

            return false;
        }
    }
}
