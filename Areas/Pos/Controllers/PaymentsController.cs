using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using MyERP.Areas.Pos.Repositories.Payments;
using MyERP.Areas.Pos.Services;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly PosSqlRepository _repository;
        private readonly PaymentVoucherReadRepository _voucherReadRepository;
        private readonly PaymentVoucherWriteRepository _voucherWriteRepository;
        private readonly PosLegacyScreenPermissionService _legacyPermissionService;

        public PaymentsController()
        {
            _repository = new PosSqlRepository();
            _voucherReadRepository = new PaymentVoucherReadRepository();
            _voucherWriteRepository = new PaymentVoucherWriteRepository();
            _legacyPermissionService = new PosLegacyScreenPermissionService();
        }

        public ActionResult Vouchers(DateTime? fromDate, DateTime? toDate, string serial, string party, int? branchId, string cashboxOrBank, decimal? amount, int page = 1, int pageSize = 50)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!_legacyPermissionService.CanView(context, "FrmPayments"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية استعراض سندات الصرف");
            }

            ViewBag.ActiveScreen = "payment-vouchers";
            return View("Vouchers", _voucherReadRepository.Search(fromDate, toDate, serial, party, context.IsFullAccess ? branchId : context.BranchId, cashboxOrBank, amount, page, pageSize));
        }

        public ActionResult Details(int id)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!_legacyPermissionService.CanView(context, "FrmPayments"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية استعراض سندات الصرف");
            }

            ViewBag.ActiveScreen = "payment-vouchers";
            var model = _voucherReadRepository.GetDetails(id);
            if (model == null)
            {
                return HttpNotFound("Payment voucher was not found.");
            }

            return View("Details", model);
        }

        public ActionResult CreateVoucher()
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!_legacyPermissionService.CanAdd(context, "FrmPayments"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية إضافة سند صرف");
            }

            ViewBag.ActiveScreen = "payment-vouchers";
            var model = _voucherWriteRepository.CreateEditModel(5, "إضافة سند صرف", "FrmPayments", "Payments", "Vouchers");
            if (!context.IsFullAccess)
            {
                model.BranchId = context.BranchId;
            }

            return View("Edit", model);
        }

        public ActionResult EditVoucher(int id)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!_legacyPermissionService.CanEdit(context, "FrmPayments"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية تعديل سند صرف");
            }

            var details = _voucherReadRepository.GetDetails(id);
            if (details == null)
            {
                return HttpNotFound("Payment voucher was not found.");
            }

            ViewBag.ActiveScreen = "payment-vouchers";
            return View("Edit", _voucherWriteRepository.CreateEditModelFromDetails(details, 5, "تعديل سند صرف", "FrmPayments", "Payments", "Vouchers"));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveVoucher(MyERP.Areas.MainErp.ViewModels.Payments.PaymentVoucherEditViewModel model)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            var isEdit = model != null && model.NoteId.HasValue;
            if (isEdit ? !_legacyPermissionService.CanEdit(context, "FrmPayments") : !_legacyPermissionService.CanAdd(context, "FrmPayments"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية حفظ سند صرف");
            }

            model = model ?? new MyERP.Areas.MainErp.ViewModels.Payments.PaymentVoucherEditViewModel();
            model.NoteType = 5;
            if (!context.IsFullAccess)
            {
                model.BranchId = context.BranchId;
            }

            var validationMessage = ValidateVoucher(model);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                return PaymentVoucherEditView(model, isEdit, validationMessage);
            }

            try
            {
                var result = _voucherWriteRepository.Save(model, context.UserId);
                TempData["VoucherMessage"] = result.Message;
                return RedirectToAction("Details", new { id = result.NoteId });
            }
            catch (Exception ex)
            {
                Trace.TraceError("POS payment voucher save failed. NoteId={0}. {1}", model.NoteId, ex);
                return PaymentVoucherEditView(model, isEdit, FriendlyVoucherError(ex));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PostVoucher(int id)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!_legacyPermissionService.CanEdit(context, "FrmPayments"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية ترحيل سند صرف");
            }

            try
            {
                _voucherWriteRepository.Post(5, id, context.UserId);
                TempData["VoucherMessage"] = "تم ترحيل السند.";
            }
            catch (Exception ex)
            {
                Trace.TraceError("POS payment voucher post failed. NoteId={0}. {1}", id, ex);
                TempData["VoucherError"] = FriendlyVoucherError(ex);
            }

            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteVoucher(int id)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!_legacyPermissionService.CanDelete(context, "FrmPayments"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية حذف سند صرف");
            }

            try
            {
                _voucherWriteRepository.Delete(5, id, context.UserId);
                TempData["VoucherMessage"] = "تم حذف السند.";
                return RedirectToAction("Vouchers");
            }
            catch (Exception ex)
            {
                Trace.TraceError("POS payment voucher delete failed. NoteId={0}. {1}", id, ex);
                TempData["VoucherError"] = FriendlyVoucherError(ex);
                return RedirectToAction("Details", new { id });
            }
        }

        public ActionResult PrintVoucher(int id)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!_legacyPermissionService.CanPrint(context, "FrmPayments"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية طباعة سند صرف");
            }

            var model = _voucherReadRepository.GetDetails(id);
            if (model == null)
            {
                return HttpNotFound("Payment voucher was not found.");
            }

            return View("Print", model);
        }

        public ActionResult Index()
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!context.IsFullAccess && !context.CanOpenPayments)
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية فتح شاشة التمويل والاستعاضة");
            }

            ViewBag.Context = context;
            ViewBag.Branches = context.IsFullAccess ? _repository.GetBranches() : new[] { new PosBranchDto { BranchId = context.BranchId.GetValueOrDefault(), BranchName = context.BranchName } };
            return View();
        }

        [HttpGet]
        public JsonResult Lookups(int? branchId, int? cashingType, string selectedMainAccountCode, int? selectedBoxId, int? selectedBankId, int? selectedEmployeeId)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "يجب تسجيل دخول نقطة البيع أولاً" }, JsonRequestBehavior.AllowGet);
            }

            if (!context.IsFullAccess && !context.CanOpenPayments)
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية فتح شاشة التمويل والاستعاضة" }, JsonRequestBehavior.AllowGet);
            }

            var effectiveBranchId = EffectiveBranchId(context, branchId);
            var lookups = _repository.GetPosPaymentRelationshipLookups(effectiveBranchId, cashingType.GetValueOrDefault(5), selectedMainAccountCode, selectedBoxId, selectedBankId, selectedEmployeeId);
            return Json(new
            {
                success = true,
                custodyAccounts = lookups.MainAccounts,
                boxAccounts = lookups.MainAccounts,
                paymentBoxes = lookups.PaymentBoxes,
                banks = lookups.Banks,
                employees = lookups.Employees,
                validity = new
                {
                    mainAccount = lookups.IsMainAccountValid,
                    box = lookups.IsBoxValid,
                    bank = lookups.IsBankValid,
                    employee = lookups.IsEmployeeValid
                }
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult Lookup(string kind, string term, int? cashingType, int? branchId)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "يجب تسجيل دخول نقطة البيع أولاً" }, JsonRequestBehavior.AllowGet);
            }

            if (!context.IsFullAccess && !context.CanOpenPayments)
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية فتح شاشة التمويل والاستعاضة" }, JsonRequestBehavior.AllowGet);
            }

            term = (term ?? string.Empty).Trim();
            if (term.Length < 2)
            {
                return Json(new { success = true, rows = new PosLookupDto[0] }, JsonRequestBehavior.AllowGet);
            }

            var effectiveBranchId = EffectiveBranchId(context, branchId);
            var normalizedKind = (kind ?? string.Empty).Trim().ToLowerInvariant();
            var rows = new System.Collections.Generic.List<PosLookupDto>();
            switch (normalizedKind)
            {
                case "name-account":
                    rows.AddRange(_repository.SearchPosPaymentNameAccounts(effectiveBranchId, cashingType.GetValueOrDefault(5) == 6 ? 0 : 1, term));
                    break;
                case "box":
                    rows.AddRange(_repository.SearchPosPaymentBoxes(effectiveBranchId, null, term));
                    break;
                case "bank":
                    rows.AddRange(_repository.SearchPosPaymentBanks(effectiveBranchId, term));
                    break;
                case "employee":
                    rows.AddRange(_repository.SearchPosPaymentEmployees(effectiveBranchId, term));
                    break;
            }

            return Json(new
            {
                success = true,
                rows = rows.Select(x => new { id = x.Id, text = x.Name, extra = x.Extra })
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult AccountBalance(int branchId, string accountCode, DateTime? asOfDate)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "يجب تسجيل دخول نقطة البيع أولاً" }, JsonRequestBehavior.AllowGet);
            }

            if (!context.IsFullAccess && !context.CanOpenPayments)
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية فتح شاشة التمويل والاستعاضة" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var balance = _repository.GetPosPaymentAccountBalance(EffectiveBranchId(context, branchId), accountCode, asOfDate);
                return Json(new { success = true, balance = balance }, JsonRequestBehavior.AllowGet);
            }
            catch (SqlException ex)
            {
                Trace.TraceError("POS payment account balance lookup failed. AccountCode={0}; BranchId={1}. {2}", accountCode, branchId, ex);
                Response.StatusCode = 500;
                return Json(new { success = false, message = "تعذر تحميل رصيد الحساب من قاعدة البيانات", technicalMessage = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult EmployeeCustody(int branchId, int employeeId)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "يجب تسجيل دخول نقطة البيع أولاً" }, JsonRequestBehavior.AllowGet);
            }

            if (!context.IsFullAccess && !context.CanOpenPayments)
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية فتح شاشة التمويل والاستعاضة" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var account = _repository.GetPosPaymentEmployeeCustodyAccount(EffectiveBranchId(context, branchId), employeeId);
                return Json(new { success = true, account = account }, JsonRequestBehavior.AllowGet);
            }
            catch (SqlException ex)
            {
                Trace.TraceError("POS payment employee custody account lookup failed. EmployeeId={0}; BranchId={1}. {2}", employeeId, branchId, ex);
                Response.StatusCode = 500;
                return Json(new { success = false, message = "تعذر تحميل حساب عهدة الموظف من قاعدة البيانات", technicalMessage = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult Preview(PosPaymentRequestDto request)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "يجب تسجيل دخول نقطة البيع أولاً" });
            }

            if (!context.IsFullAccess && !context.CanOpenPayments)
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية فتح شاشة التمويل والاستعاضة" });
            }

            ForceContext(request, context);
            try
            {
                return Json(new { success = true, canSave = true, lines = _repository.PreviewPosPayment(request) });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult Search(PosPaymentSearchRequestDto request)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "يجب تسجيل دخول نقطة البيع أولاً" });
            }

            if (!context.IsFullAccess && !context.CanOpenPayments)
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية فتح شاشة التمويل والاستعاضة" });
            }

            request = request ?? new PosPaymentSearchRequestDto();
            if (!context.IsFullAccess)
            {
                request.BranchId = context.BranchId;
            }

            var validationMessage = ValidatePaymentSearch(request);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = validationMessage });
            }

            try
            {
                return Json(new { success = true, rows = _repository.SearchPosPayments(request, context.UserId, context.IsFullAccess) });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = "تعذر البحث في الحركات السابقة", technicalMessage = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult Get(int id)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "يجب تسجيل دخول نقطة البيع أولاً" }, JsonRequestBehavior.AllowGet);
            }

            if (!context.IsFullAccess && !context.CanOpenPayments)
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية فتح شاشة التمويل والاستعاضة" }, JsonRequestBehavior.AllowGet);
            }

            var movement = _repository.GetPosPayment(id, context.UserId, context.IsFullAccess, context.BranchId);
            if (movement == null)
            {
                Response.StatusCode = 404;
                return Json(new { success = false, message = "لم يتم العثور على الحركة" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                success = true,
                movement = movement,
                canEdit = context.IsFullAccess || context.CanEditPayments,
                editMessage = (context.IsFullAccess || context.CanEditPayments) ? "" : "ليس لديك صلاحية تعديل هذه الحركة"
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Create(PosPaymentRequestDto request)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "يجب تسجيل دخول نقطة البيع أولاً" });
            }

            var isUpdate = request != null && request.NoteId.HasValue && request.NoteId.Value > 0;
            if (!context.IsFullAccess && (!context.CanExecutePayments || (isUpdate && !context.CanEditPayments)))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = isUpdate ? "ليس لديك صلاحية تعديل هذه الحركة" : "ليست لديك صلاحية تنفيذ التمويل والاستعاضة" });
            }

            ForceContext(request, context);
            try
            {
                var result = _repository.SavePosPayment(request, context.UserId, context.IsFullAccess || context.CanExecutePayments);
                return Json(new { success = true, message = isUpdate ? "تم تعديل حركة التمويل والاستعاضة" : "تم حفظ عملية التمويل والاستعاضة", result = result });
            }
            catch (SqlException ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "حدث خطأ من قاعدة البيانات أثناء الحفظ", technicalMessage = ex.Message });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        private ActionResult PaymentVoucherEditView(MyERP.Areas.MainErp.ViewModels.Payments.PaymentVoucherEditViewModel model, bool isEdit, string warning)
        {
            model.Title = isEdit ? "تعديل سند صرف" : "إضافة سند صرف";
            model.ScreenName = "FrmPayments";
            model.BackController = "Payments";
            model.BackAction = "Vouchers";
            model.Warning = warning;
            return View("Edit", _voucherWriteRepository.WithLookups(model));
        }

        internal static string ValidateVoucher(MyERP.Areas.MainErp.ViewModels.Payments.PaymentVoucherEditViewModel model)
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

        private static void ForceContext(PosPaymentRequestDto request, PosUserContext context)
        {
            if (request == null)
            {
                return;
            }

            if (!context.IsFullAccess)
            {
                request.BranchId = context.BranchId.GetValueOrDefault();
            }

            if (request.PaymentDate == DateTime.MinValue)
            {
                request.PaymentDate = DateTime.Today;
            }
        }

        private static int EffectiveBranchId(PosUserContext context, int? requestedBranchId)
        {
            if (context == null)
            {
                return requestedBranchId.GetValueOrDefault();
            }

            if (!context.IsFullAccess)
            {
                return context.BranchId.GetValueOrDefault();
            }

            return requestedBranchId.GetValueOrDefault(context.BranchId.GetValueOrDefault());
        }

        private static string ValidatePaymentSearch(PosPaymentSearchRequestDto request)
        {
            request = request ?? new PosPaymentSearchRequestDto();
            var term = (request.SearchText ?? string.Empty).Trim();
            var hasDateRange = request.FromDate.HasValue && request.ToDate.HasValue;
            if (term.Length > 0 && term.Length < 3 && !term.All(char.IsDigit))
            {
                return "اكتب 3 أحرف على الأقل للبحث العام";
            }

            if (!hasDateRange && term.Length < 3)
            {
                return "حدد فترة البحث أو اكتب بحثاً محدداً من 3 أحرف على الأقل";
            }

            return string.Empty;
        }

        private PosUserContext GetPosContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _repository);
        }
    }
}
