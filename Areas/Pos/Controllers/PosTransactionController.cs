using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using MyERP.Areas.Pos.Reports;
using MyERP.Areas.Pos.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    [AllowAnonymous]
    [SkipERPAuthorize]
    public class PosTransactionController : Controller
    {
        private const decimal DefaultMaxRechargeValue = 100000m;

        private readonly PosSqlRepository _repository;

        public PosTransactionController()
        {
            _repository = new PosSqlRepository();
        }

        public ActionResult Index()
        {
            var context = GetPosContext();
            if (context == null)
            {
                TempData["PosLoginMessage"] = PosLoginController.PosSessionExpiredMessage;
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (OpensInvoiceDirectly(context) && !HasRequiredSalesDefaults(context))
            {
                ViewBag.Message = "لا توجد إعدادات بيع افتراضية لهذا المستخدم، برجاء مراجعة الإدارة";
                return View("MissingSalesDefaults");
            }

            ViewBag.PosContext = context;
            return View();
        }

        private static bool HasRequiredSalesDefaults(PosUserContext context)
        {
            if (context != null && (context.IsFullAccess || context.UserType.GetValueOrDefault(-1) == 0))
            {
                return true;
            }

            return context != null
                && context.BranchId.GetValueOrDefault() > 0
                && context.EmpId.GetValueOrDefault() > 0
                && context.StoreId.GetValueOrDefault() > 0
                && context.BoxId.GetValueOrDefault() > 0
                && context.PaymentTypeId.GetValueOrDefault() > 0;
        }

        private static bool OpensInvoiceDirectly(PosUserContext context)
        {
            return context != null
                && (IsTellerCategory(context.UserCategory) || context.CanTeller || context.UserType.GetValueOrDefault(-1) == 2)
                && !context.IsFullAccess
                && context.UserType.GetValueOrDefault(-1) != 0;
        }

        private static bool IsTellerCategory(string category)
        {
            var value = (category ?? string.Empty).Trim();
            return string.Equals(value, "تلر", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Teller", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAdmin(PosUserContext context)
        {
            return context != null && (context.UserType.GetValueOrDefault(-1) == 0 || context.IsFullAccess);
        }

        [HttpGet]
        public JsonResult GetItems(string term)
        {
            return Json(_repository.GetItems(term), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetDefaultServiceItem(string serviceType, int? itemId)
        {
            var context = GetPosContext();
            return Json(_repository.GetDefaultServiceItem(serviceType, itemId, context != null ? context.BranchId : null), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetPrimaryServiceItems(string serviceType)
        {
            return Json(_repository.GetPrimaryServiceItems(serviceType), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult CommissionBootstrap()
        {
            var context = GetPosContext();
            if (context == null)
            {
                SetJsonErrorStatus(401);
                return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."), JsonRequestBehavior.AllowGet);
            }

            var serviceTypes = new[] { "cash-in", "cash-out", "card", "violations" };
            var stopwatch = Stopwatch.StartNew();
            var primaryServices = serviceTypes.ToDictionary(serviceType => serviceType, serviceType => _repository.GetPrimaryServiceItems(serviceType));
            stopwatch.Stop();
            PosPerformanceLogger.LogQuery("PosTransaction.CommissionBootstrap", "GetPrimaryServiceItems", stopwatch.ElapsedMilliseconds, primaryServices.Sum(x => x.Value != null ? x.Value.Count : 0), context);
            return Json(new
            {
                success = true,
                primaryServices = primaryServices,
                loadedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetSecondaryServiceItems(string serviceType, int itemId)
        {
            return Json(_repository.GetSecondaryServiceItems(serviceType, itemId), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult LookupCustomerByPhone(string phone)
        {
            var context = GetPosContext();
            if (context == null || !context.BranchId.HasValue)
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }

            return Json(_repository.LookupKeshniCardCustomer(phone, context.BranchId, context.CanChangeDefaults), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult SearchKeshniCardCustomers(string term)
        {
            var context = GetPosContext();
            if (context == null)
            {
                SetJsonErrorStatus(401);
                return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."), JsonRequestBehavior.AllowGet);
            }

            term = (term ?? string.Empty).Trim();
            if (term.Length < 2)
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }

            if (!context.BranchId.HasValue)
            {
                SetJsonErrorStatus(400);
                return Json(Fail("الفرع غير محدد", "POS branch context is missing."), JsonRequestBehavior.AllowGet);
            }

            var matches = _repository.SearchKeshniCardCustomers(term, context.BranchId, context.CanChangeDefaults);
            if (matches.Count > 0)
            {
                return Json(matches, JsonRequestBehavior.AllowGet);
            }

            var otherBranchHint = context.CanChangeDefaults ? null : BuildOtherBranchKycHint(term, context.BranchId);
            return Json(otherBranchHint ?? (object)matches, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult LookupUnusedKeshniCardCustomer(string term)
        {
            var context = GetPosContext();
            if (context == null)
            {
                SetJsonErrorStatus(401);
                return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."), JsonRequestBehavior.AllowGet);
            }

            term = (term ?? string.Empty).Trim();
            if (term.Length < 2)
            {
                return Json(new
                {
                    success = true,
                    found = false,
                    message = string.Empty
                }, JsonRequestBehavior.AllowGet);
            }

            if (!context.BranchId.HasValue)
            {
                SetJsonErrorStatus(400);
                return Json(Fail("الفرع غير محدد", "POS branch context is missing."), JsonRequestBehavior.AllowGet);
            }

            var matches = _repository.SearchUnusedKeshniCardCustomers(term, context.BranchId, context.CanChangeDefaults);
            if (matches.Count == 1)
            {
                return Json(new
                {
                    success = true,
                    found = true,
                    customer = matches[0],
                    message = "تم العثور على بيانات KYC محفوظة مسبقاً ولم يتم إصدار فاتورة لها، وتم تحميلها للتعديل/الاستخدام."
                }, JsonRequestBehavior.AllowGet);
            }

            if (matches.Count > 1)
            {
                return Json(new
                {
                    success = false,
                    found = false,
                    multiple = true,
                    customers = matches,
                    message = "يوجد أكثر من عميل مطابق. برجاء اختيار العميل من النتائج أو البحث ببيانات أدق."
                }, JsonRequestBehavior.AllowGet);
            }

            var otherBranchHint = context.CanChangeDefaults ? null : BuildOtherBranchKycHint(term, context.BranchId);
            if (otherBranchHint != null)
            {
                return Json(otherBranchHint, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                success = true,
                found = false,
                message = string.Empty
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CalculateCommission(PosCommissionRequest request)
        {
            request = request ?? new PosCommissionRequest();
            var context = GetPosContext();
            if (context == null)
            {
                SetJsonErrorStatus(401);
                return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."));
            }

            if (context != null && !request.BranchId.HasValue)
            {
                request.BranchId = context.BranchId;
            }

            var amountError = ValidateCommissionAmount(request);
            if (!string.IsNullOrWhiteSpace(amountError))
            {
                SetJsonErrorStatus(400);
                return Json(Fail(amountError, amountError));
            }

            try
            {
                return Json(_repository.CalculateCommission(request));
            }
            catch (SqlException ex)
            {
                LogCommissionFailure(context, "CalculateCommission.SqlException", request, ex, "SqlException");
                SetJsonErrorStatus(500);
                return Json(Fail(GetCommissionFailureMessage(request), ex.Message));
            }
            catch (Exception ex)
            {
                LogCommissionFailure(context, "CalculateCommission.Exception", request, ex, "Exception");
                SetJsonErrorStatus(500);
                return Json(Fail(GetCommissionFailureMessage(request), ex.Message));
            }
        }

        [HttpGet]
        public JsonResult GetPaymentTypes()
        {
            var context = GetPosContext();
            if (context != null && !context.CanChangeDefaults)
            {
                return Json(new[]
                {
                    new PosPaymentTypeDto
                    {
                        PaymentID = context.PaymentTypeId.GetValueOrDefault(),
                        PaymentName = context.PaymentName,
                        BankId = context.BankId,
                        BankName = context.BankName
                    }
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(_repository.GetPaymentTypes(), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetCashBoxes()
        {
            var context = GetPosContext();
            if (context != null && !context.CanChangeDefaults)
            {
                return Json(new[]
                {
                    new PosCashBoxDto
                    {
                        BoxID = context.BoxId.GetValueOrDefault(),
                        BoxName = context.BoxName,
                        BranchId = context.BranchId
                    }
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(_repository.GetCashBoxesByUserOrBranch(context != null ? (int?)context.UserId : null, context != null ? context.BranchId : null), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetBranches()
        {
            var context = GetPosContext();
            if (context != null && !context.CanChangeDefaults)
            {
                return Json(new[]
                {
                    new PosBranchDto
                    {
                        BranchId = context.BranchId.GetValueOrDefault(),
                        BranchName = context.BranchName
                    }
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(_repository.GetBranches(), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetStoresByBranch(int? branchId)
        {
            var context = GetPosContext();
            if (context != null && !context.CanChangeDefaults)
            {
                return Json(new[]
                {
                    new PosStoreDto
                    {
                        StoreID = context.StoreId.GetValueOrDefault(),
                        StoreName = context.StoreName,
                        BranchId = context.BranchId
                    }
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(_repository.GetStoresByBranch(branchId), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetContext()
        {
            var context = GetPosContext();
            if (context == null)
            {
                SetJsonErrorStatus(401);
                return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."), JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                UserId = context.UserId,
                UserName = context.UserName,
                UserType = context.UserType,
                UserCategory = context.UserCategory,
                EmpId = context.EmpId,
                EmpName = context.EmpName,
                BranchId = context.BranchId,
                BranchName = context.BranchName,
                StoreID = context.StoreId,
                StoreId = context.StoreId,
                StoreName = context.StoreName,
                BoxID = context.BoxId,
                BoxId = context.BoxId,
                BoxName = context.BoxName,
                PaymentNetid = context.PaymentNetId,
                PaymentNetId = context.PaymentNetId,
                PaymentTypeId = context.PaymentTypeId,
                PaymentName = context.PaymentName,
                BankId = context.BankId,
                BankName = context.BankName,
                CanAdd = context.CanSave,
                CanSave = context.CanSave,
                CanPrint = context.CanPrint,
                CanReturn = context.CanReturn,
                CanOpenCashCustomer = context.CanOpenCashCustomer,
                CanViewJournalEntry = context.CanViewJournalEntry,
                CanViewReports = context.CanViewReports,
                CanPrintKycAcknowledgment = context.CanPrintKycAcknowledgment,
                CanPrintKycCard = context.CanPrintKycCard,
                CanEditKyc = context.CanEditKyc,
                CanTeller = context.CanTeller,
                CanOpenPayments = context.CanOpenPayments,
                CanExecutePayments = context.CanExecutePayments,
                CanEditInvoice = context.CanEditInvoice,
                CanAdminDeleteInvoice = IsAdmin(context),
                CanCancelInvoice = context.CanCancelInvoice,
                IsFullAccess = context.IsFullAccess,
                CanChangeDefaults = context.CanChangeDefaults,
                CanManagePrintTemplates = context.CanManagePrintTemplates
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetEmployeeBalances()
        {
            var context = GetPosContext();
            if (context == null)
            {
                SetJsonErrorStatus(401);
                return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."), JsonRequestBehavior.AllowGet);
            }

            return Json(_repository.GetEmployeeBalances(context.UserId, context.BoxId), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetTodayInvoices(string term, string operationType, DateTime? fromDate, DateTime? toDate, int? branchId, bool excelOnly = false)
        {
            var context = GetPosContext();
            if (context == null)
            {
                SetJsonErrorStatus(401);
                return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."), JsonRequestBehavior.AllowGet);
            }

            try
            {
                return Json(_repository.GetTodayInvoices(context.UserId, context.BranchId, context.CanChangeDefaults, context.CanEditInvoice, term, operationType, fromDate, toDate, branchId, excelOnly), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                PosSystemErrorLogger.Log(_repository, Request, context, "PosTransaction", "GetTodayInvoices", operationType, null, ex.Message, ex, "termLength=" + ((term ?? string.Empty).Length.ToString(CultureInfo.InvariantCulture)), "Error", "Exception");
                SetJsonErrorStatus(500);
                return Json(Fail("تعذر تحميل فواتير اليوم", ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult CancelInvoice(PosCancelInvoiceRequest request)
        {
            var context = GetPosContext();
            if (context == null)
            {
                SetJsonErrorStatus(401);
                return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."));
            }

            if (request == null || request.TransactionId <= 0)
            {
                SetJsonErrorStatus(400);
                return Json(Fail("رقم الفاتورة غير صحيح", "TransactionId is required."));
            }

            if (!context.CanCancelInvoice || !_repository.HasPosPermission(context.UserId, "CanCancelInvoice"))
            {
                SetJsonErrorStatus(403);
                return Json(Fail("ليست لديك صلاحية إلغاء الفاتورة", "Missing CanCancelInvoice permission."));
            }

            if (string.IsNullOrWhiteSpace(request.Password) || !_repository.ValidatePosUserPassword(context.UserId, request.Password))
            {
                SetJsonErrorStatus(403);
                return Json(Fail("كلمة المرور غير صحيحة", "Password validation failed."));
            }

            try
            {
                _repository.CancelPosInvoice(request.TransactionId, context.UserId, request.CancelReason);
                return Json(new { success = true, message = "تم إلغاء الفاتورة بنجاح." });
            }
            catch (SqlException ex)
            {
                SetJsonErrorStatus(500);
                return Json(Fail("تعذر إلغاء الفاتورة", ex.Message));
            }
            catch (Exception ex)
            {
                SetJsonErrorStatus(500);
                return Json(Fail("تعذر إلغاء الفاتورة", ex.Message));
            }
        }

        [HttpPost]
        public JsonResult DeleteInvoice(PosDeleteInvoiceRequest request)
        {
            var context = GetPosContext();
            if (context == null)
            {
                SetJsonErrorStatus(401);
                return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."));
            }

            if (!IsAdmin(context))
            {
                SetJsonErrorStatus(403);
                return Json(Fail("حذف الفواتير متاح للمدير فقط", "Current POS user is not an admin."));
            }

            if (request == null || request.TransactionId <= 0)
            {
                SetJsonErrorStatus(400);
                return Json(Fail("رقم الفاتورة غير صحيح", "TransactionId is required."));
            }

            if (!_repository.ValidatePosUserPassword(context.UserId, request.AdminPassword))
            {
                SetJsonErrorStatus(403);
                return Json(Fail("كلمة مرور المدير غير صحيحة", "Admin password validation failed."));
            }

            try
            {
                var result = _repository.DeletePosSaleInvoice(request.TransactionId, context.UserId);
                return Json(new { success = true, message = string.Join(" ", result.Messages), result = result });
            }
            catch (Exception ex)
            {
                PosSystemErrorLogger.Log(_repository, Request, context, "PosTransaction", "DeleteInvoice", null, request.TransactionId, ex.Message, ex, "DeleteInvoice", "Error", "Exception");
                SetJsonErrorStatus(500);
                return Json(Fail("تعذر حذف الفاتورة", ex.Message));
            }
        }

        [HttpPost]
        public JsonResult DeleteExcelInvoices(PosDeleteExcelInvoicesRequest request)
        {
            var context = GetPosContext();
            if (context == null)
            {
                SetJsonErrorStatus(401);
                return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."));
            }

            if (!IsAdmin(context))
            {
                SetJsonErrorStatus(403);
                return Json(Fail("حذف فواتير Excel متاح للمدير فقط", "Current POS user is not an admin."));
            }

            if (request == null || !request.FromDate.HasValue || !request.ToDate.HasValue)
            {
                SetJsonErrorStatus(400);
                return Json(Fail("حدد فترة الحذف أولاً", "FromDate and ToDate are required."));
            }

            if (!_repository.ValidatePosUserPassword(context.UserId, request.AdminPassword))
            {
                SetJsonErrorStatus(403);
                return Json(Fail("كلمة مرور المدير غير صحيحة", "Admin password validation failed."));
            }

            try
            {
                var branchId = context.IsFullAccess ? request.BranchId : context.BranchId;
                var result = _repository.DeletePosExcelImportedInvoices(request.FromDate.Value, request.ToDate.Value, branchId, request.OperationType, context.UserId);
                return Json(new { success = true, message = string.Join(" ", result.Messages), result = result });
            }
            catch (Exception ex)
            {
                PosSystemErrorLogger.Log(_repository, Request, context, "PosTransaction", "DeleteExcelInvoices", null, null, ex.Message, ex, "DeleteExcelInvoices", "Error", "Exception");
                SetJsonErrorStatus(500);
                return Json(Fail("تعذر حذف فواتير Excel", ex.Message));
            }
        }

        [HttpGet]
        public JsonResult TodaySummary()
        {
            var context = GetPosContext();
            if (context == null)
            {
                SetJsonErrorStatus(401);
                return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."), JsonRequestBehavior.AllowGet);
            }

            try
            {
                return Json(_repository.GetTodaySummary(context.UserId, context.BranchId, context.CanChangeDefaults), JsonRequestBehavior.AllowGet);
            }
            catch (SqlException ex)
            {
                PosSystemErrorLogger.Log(_repository, Request, context, "PosTransaction", "TodaySummary.SqlException", null, null, ex.Message, ex, "TodaySummary", "Error", "SqlException");
                SetJsonErrorStatus(500);
                return Json(Fail("تعذر تحميل ملخص اليوم من قاعدة البيانات", ex.Message), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                PosSystemErrorLogger.Log(_repository, Request, context, "PosTransaction", "TodaySummary.Exception", null, null, ex.Message, ex, "TodaySummary", "Error", "Exception");
                SetJsonErrorStatus(500);
                return Json(Fail("تعذر تحميل ملخص اليوم", ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetInvoice(int transactionId)
        {
            var context = GetPosContext();
            if (context == null)
            {
                SetJsonErrorStatus(401);
                return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."), JsonRequestBehavior.AllowGet);
            }

            var invoice = _repository.GetInvoiceForReview(transactionId, context.UserId, context.CanChangeDefaults || context.CanEditInvoice);
            if (invoice == null)
            {
                Response.StatusCode = 404;
                return Json(Fail("لم يتم العثور على الفاتورة أو لا تملك صلاحية فتحها", "Invoice not found or not allowed."), JsonRequestBehavior.AllowGet);
            }

            return Json(invoice, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetJournalEntry(int transactionId)
        {
            var context = GetPosContext();
            if (context == null)
            {
                SetJsonErrorStatus(401);
                return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."), JsonRequestBehavior.AllowGet);
            }

            if (!context.CanViewJournalEntry)
            {
                Response.StatusCode = 403;
                return Json(Fail("ليست لديك صلاحية استعراض القيد المحاسبي", "CanViewJournalEntry is false."), JsonRequestBehavior.AllowGet);
            }

            try
            {
                var entries = _repository.GetJournalEntriesForTransaction(transactionId, context.UserId, context.CanChangeDefaults);
                return Json(new { success = true, entries = entries }, JsonRequestBehavior.AllowGet);
            }
            catch (SqlException ex)
            {
                Response.StatusCode = 500;
                return Json(Fail("تعذر تحميل القيد المحاسبي", ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult PrintPreview(int? transactionId)
        {
            var savedTransactionId = ResolvePrintTransactionId(transactionId);
            if (savedTransactionId <= 0)
            {
                return HttpNotFound("لا توجد فاتورة محفوظة للطباعة");
            }

            var context = GetPosContext();
            if (context == null)
            {
                return new HttpStatusCodeResult(401, "POS session context is missing.");
            }

            if (!context.CanPrint)
            {
                return new HttpStatusCodeResult(403, "POS user is not allowed to print.");
            }

            if (!_repository.TransactionExists(savedTransactionId))
            {
                return HttpNotFound("الفاتورة غير موجودة");
            }

            if (!_repository.CanPrintTransaction(savedTransactionId, context.UserId, context.CanChangeDefaults))
            {
                return new HttpStatusCodeResult(403, "الفاتورة موجودة ولكن غير مسموح لهذا المستخدم بطباعتها");
            }

            if (!_repository.TransactionHasDetails(savedTransactionId))
            {
                return HttpNotFound("تم العثور على الفاتورة ولكن لا توجد تفاصيل للطباعة");
            }

            var receipt = _repository.GetReceipt(savedTransactionId, context.UserId, context.CanChangeDefaults);
            if (receipt == null)
            {
                return HttpNotFound("تعذر تحميل بيانات الفاتورة للطباعة");
            }

            ViewBag.TransactionId = savedTransactionId;
            return View(new PosReceiptReport(receipt));
        }

        [HttpGet]
        public ActionResult Print(int? transactionId)
        {
            var savedTransactionId = ResolvePrintTransactionId(transactionId);
            if (savedTransactionId <= 0)
            {
                return HttpNotFound("لا توجد فاتورة محفوظة للطباعة");
            }

            var context = GetPosContext();
            if (context == null)
            {
                return new HttpStatusCodeResult(401, "POS session context is missing.");
            }

            if (!context.CanPrint)
            {
                return new HttpStatusCodeResult(403, "POS user is not allowed to print.");
            }

            if (!_repository.TransactionExists(savedTransactionId))
            {
                return HttpNotFound("الفاتورة غير موجودة");
            }

            if (!_repository.CanPrintTransaction(savedTransactionId, context.UserId, context.CanChangeDefaults))
            {
                return new HttpStatusCodeResult(403, "الفاتورة موجودة ولكن غير مسموح لهذا المستخدم بطباعتها");
            }

            if (!_repository.TransactionHasDetails(savedTransactionId))
            {
                return HttpNotFound("تم العثور على الفاتورة ولكن لا توجد تفاصيل للطباعة");
            }

            var receipt = _repository.GetReceipt(savedTransactionId, context.UserId, context.CanChangeDefaults);
            if (receipt == null)
            {
                return HttpNotFound("تعذر تحميل بيانات الفاتورة للطباعة");
            }

            using (var report = new PosReceiptReport(receipt))
            using (var stream = new MemoryStream())
            {
                report.ExportToPdf(stream);
                return File(stream.ToArray(), "application/pdf", "pos-receipt-" + savedTransactionId + ".pdf");
            }
        }

        private int ResolvePrintTransactionId(int? transactionId)
        {
            if (transactionId.HasValue)
            {
                return transactionId.Value;
            }

            var routeId = RouteData.Values["id"];
            if (routeId == null)
            {
                return 0;
            }

            int parsed;
            return int.TryParse(Convert.ToString(routeId), out parsed) ? parsed : 0;
        }

        [HttpPost]
        public JsonResult SaveCashCustomer(PosCashCustomerSaveRequest request)
        {
            try
            {
                request = request ?? new PosCashCustomerSaveRequest();
                if (!IsValidEgyptianMobile(request.PhoneNo2))
                {
                    var errors = new Dictionary<string, string>
                    {
                        { "PhoneNo2", "رقم التليفون يجب أن يكون 11 رقم ويبدأ بـ 010 أو 011 أو 012 أو 015" }
                    };
                    LogKycFailure("SaveCashCustomer.Validation", request, null, errors);
                    Response.StatusCode = 400;
                    return Json(Fail("راجع بيانات العميل", "Invalid Egyptian mobile format.", errors));
                }

                var dateErrors = ValidateSqlDateRange(request);
                if (dateErrors.Count > 0)
                {
                    LogKycFailure("SaveCashCustomer.DateValidation", request, null, dateErrors);
                    Response.StatusCode = 400;
                    return Json(Fail("راجع تواريخ العميل. يجب إدخال سنة كاملة وصحيحة مثل 2026.", "KYC date is outside SQL Server datetime range.", dateErrors));
                }

                var context = GetPosContext();
                if (context != null)
                {
                    request.UserId = context.UserId;
                    if (!context.CanChangeDefaults || !request.BranchId.HasValue)
                    {
                        request.BranchId = context.BranchId;
                    }
                }

                return Json(_repository.SaveCashCustomer(request));
            }
            catch (SqlException ex)
            {
                LogKycFailure("SaveCashCustomer.SqlException", request, ex, null);
                Response.StatusCode = 500;
                return Json(Fail(FriendlySqlKycMessage(ex), ex.ToString()));
            }
            catch (Exception ex)
            {
                LogKycFailure("SaveCashCustomer.Exception", request, ex, null);
                Response.StatusCode = 500;
                return Json(Fail("حدث خطأ أثناء حفظ بيانات العميل", ex.ToString()));
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult SaveKeshniCardCustomer(PosCashCustomerSaveRequest request)
        {
            try
            {
                request = request ?? new PosCashCustomerSaveRequest();
                var context = GetPosContext();
                if (context == null)
                {
                    LogKycFailure("SaveKeshniCardCustomer.NoSession", request, null, null);
                    SetJsonErrorStatus(401);
                    return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."));
                }

                if (!context.CanEditKyc)
                {
                    SetJsonErrorStatus(403);
                    return Json(Fail("ليست لديك صلاحية تعديل بيانات KYC", "CanEditKyc is false."));
                }

                request.UserId = context.UserId;
                request.EmpId = context.EmpId;
                request.EasyCashType = 0;
                request.OrderDate = request.OrderDate ?? DateTime.Today;
                if (!context.CanChangeDefaults || !request.BranchId.HasValue)
                {
                    request.BranchId = context.BranchId;
                }

                var validationErrors = ValidateKeshniCardCustomer(request);
                var duplicateInfo = AddKeshniCardDuplicateErrors(request, validationErrors, context);
                if (validationErrors.Count > 0)
                {
                    LogKycFailure("SaveKeshniCardCustomer.Validation", request, null, validationErrors);
                    SetJsonErrorStatus(400);
                    var message = duplicateInfo.HasDuplicate
                        ? "هذا العميل/الكارت مسجل من قبل. برجاء البحث عن العميل واختياره أولاً ثم تعديل بياناته."
                        : "راجع بيانات تفعيل الكارت";
                    return Json(Fail(message, "Keshni Card KYC validation failed.", validationErrors, duplicateInfo.ExistingCustomerId, duplicateInfo.HasDuplicate, duplicateInfo.ExistingCustomer));
                }

                var saved = _repository.SaveKeshniCardCustomer(request);
                var attachmentSubject = PosSqlRepository.BuildKeshniAttachmentSubject(
                    saved.BranchName ?? context.BranchName,
                    saved.ArabicName0,
                    saved.ArabicName1,
                    saved.Tet_NumPoket);
                IList<PosKycAttachmentDto> attachments;
                try
                {
                    attachments = SaveKeshniAttachments(attachmentSubject);
                }
                catch (UnauthorizedAccessException ex)
                {
                    LogKycFailure("SaveKeshniCardCustomer.AttachmentUnauthorized", request, ex, null);
                    SetJsonErrorStatus(500);
                    return Json(Fail("تم حفظ بيانات العميل لكن لا توجد صلاحية لحفظ المرفقات. راجع صلاحيات مسار المرفقات.", ex.ToString()));
                }
                catch (IOException ex)
                {
                    LogKycFailure("SaveKeshniCardCustomer.AttachmentIO", request, ex, null);
                    SetJsonErrorStatus(500);
                    return Json(Fail("تم حفظ بيانات العميل لكن حدث خطأ أثناء حفظ ملفات المرفقات.", ex.ToString()));
                }
                catch (SqlException ex)
                {
                    LogKycFailure("SaveKeshniCardCustomer.AttachmentSql", request, ex, null);
                    SetJsonErrorStatus(500);
                    return Json(Fail("تم حفظ بيانات العميل لكن تعذر تسجيل بيانات المرفقات في قاعدة البيانات.", ex.ToString()));
                }
                catch (Exception ex)
                {
                    LogKycFailure("SaveKeshniCardCustomer.AttachmentException", request, ex, null);
                    SetJsonErrorStatus(500);
                    return Json(Fail("تم حفظ بيانات العميل لكن تعذر حفظ المرفقات. راجع نوع الملف وصلاحيات مسار المرفقات.", ex.ToString()));
                }

                return Json(new
                {
                    success = true,
                    message = "تم حفظ بيانات العميل وتفعيل الكارت",
                    technicalMessage = string.Empty,
                    technicalDetails = string.Empty,
                    validationErrors = new Dictionary<string, string>(),
                    customer = saved,
                    customerId = saved.CustomerID,
                    activationStatus = true,
                    attachmentSubject = attachmentSubject,
                    attachmentFolder = DateTime.Today.ToString("yyyyMMdd"),
                    attachments = attachments
                });
            }
            catch (SqlException ex)
            {
                LogKycFailure("SaveKeshniCardCustomer.SqlException", request, ex, null);
                SetJsonErrorStatus(IsKeshniActivationValidationSqlError(ex) ? 400 : 500);
                return Json(Fail(FriendlySqlKycMessage(ex), ex.ToString()));
            }
            catch (Exception ex)
            {
                LogKycFailure("SaveKeshniCardCustomer.Exception", request, ex, null);
                SetJsonErrorStatus(500);
                return Json(Fail("حدث خطأ أثناء حفظ بيانات الكارت", ex.ToString()));
            }
        }

        [HttpPost]
        public JsonResult Save(PosSaveTransactionRequest request)
        {
            PosUserContext context = null;
            try
            {
                request = request ?? new PosSaveTransactionRequest();

                context = GetPosContext();
                if (context == null)
                {
                    SetJsonErrorStatus(401);
                    return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."));
                }

                var isExistingInvoiceSave = request.Transaction_ID.HasValue && request.Transaction_ID.Value > 0;
                if (!context.CanSave && !(isExistingInvoiceSave && context.CanEditInvoice))
                {
                    SetJsonErrorStatus(403);
                    return Json(Fail("ليس لديك صلاحية الحفظ", "ScreenJuncUser does not allow CanAdd/FullAccess for FrmSaleBill6, and this is not an allowed edit operation."));
                }

                ApplyCashOutSecondaryDefault(request);

                PosInvoiceReviewDto originalInvoice = null;
                if (isExistingInvoiceSave)
                {
                    originalInvoice = _repository.GetInvoiceForReview(request.Transaction_ID.Value, context.UserId, context.CanChangeDefaults || context.CanEditInvoice);
                    if (originalInvoice == null)
                    {
                        SetJsonErrorStatus(404);
                        return Json(Fail("لم يتم العثور على الفاتورة أو لا تملك صلاحية تعديلها", "Existing invoice not found or not allowed."));
                    }

                    if (!context.CanEditInvoice)
                    {
                        SetJsonErrorStatus(403);
                        return Json(Fail("ليست لديك صلاحية تعديل هذه الفاتورة", "CanEditInvoice is false."));
                    }

                    if (originalInvoice.CreatedUserId.HasValue && originalInvoice.CreatedUserId.Value != context.UserId
                        && !_repository.ValidatePosUserPassword(context.UserId, request.EditPassword))
                    {
                        SetJsonErrorStatus(403);
                        return Json(Fail("كلمة المرور غير صحيحة، لم يتم حفظ التعديل", "Current POS user password validation failed."));
                    }

                    request.BranchId = originalInvoice.BranchId;
                    request.StoreID = originalInvoice.StoreID;
                    request.BoxID = originalInvoice.BoxID;
                    request.UserID = originalInvoice.CreatedUserId;
                    request.Emp_ID = originalInvoice.Emp_ID;
                    request.NoID = originalInvoice.NoID;
                    request.TransactionDate = originalInvoice.TransactionDate.HasValue
                        ? originalInvoice.TransactionDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                        : request.TransactionDate;
                }

                var validationErrors = ValidateSaveRequest(request, context);
                AddServiceTypeValidationErrors(request, validationErrors);
                AddImportantIpnDuplicateErrors(request, validationErrors);
                if (validationErrors.Count > 0)
                {
                    LogPosSystemIssue(context, "Save.Validation", request, null, "POS save validation failed", "Warning", "Validation", BuildSaveDiagnostic("PosTransactionController.Save", request, validationErrors, null));
                    SetJsonErrorStatus(400);
                    return Json(Fail("راجع بيانات العملية قبل الحفظ", "Client request failed POS server validation.", validationErrors));
                }

                if (originalInvoice == null)
                {
                    request.UserID = context.UserId;
                    request.Emp_ID = context.EmpId;
                    request.NoID = PosSqlRepository.WebInvoiceSourceMarker;
                }

                if (originalInvoice != null)
                {
                    request.BranchId = originalInvoice.BranchId;
                    request.StoreID = originalInvoice.StoreID;
                    request.BoxID = originalInvoice.BoxID;
                }
                else if (context.CanChangeDefaults)
                {
                    request.BranchId = request.BranchId ?? context.BranchId;
                    request.StoreID = request.StoreID ?? context.StoreId;
                    request.BoxID = request.BoxID ?? context.BoxId;
                }
                else
                {
                    request.BranchId = context.BranchId;
                    request.StoreID = context.StoreId;
                    request.BoxID = context.BoxId;
                }

                request.PaymentNetid = context.PaymentNetId;
                if (!context.CanChangeDefaults && context.PaymentTypeId.HasValue)
                {
                    request.PaymentType = context.PaymentTypeId.Value;
                }
                else if (context.PaymentTypeId.HasValue && request.PaymentType <= 0)
                {
                    request.PaymentType = context.PaymentTypeId.Value;
                }

                if (IsKeshniCardTransaction(request.TransactionType))
                {
                    var cashCustomer = request.TblCusCshId.HasValue
                        ? _repository.GetKeshniCardCustomerById(request.TblCusCshId.Value, context.BranchId, context.CanChangeDefaults)
                        : _repository.LookupKeshniCardCustomer(request.CashCustomerPhone, context.BranchId, context.CanChangeDefaults);

                    if (cashCustomer != null)
                    {
                        request.TblCusCshId = cashCustomer.CustomerID;
                        request.CashCustomerName = string.IsNullOrWhiteSpace(request.CashCustomerName) ? cashCustomer.CustomerName : request.CashCustomerName;
                        request.CashCustomerPhone = string.IsNullOrWhiteSpace(request.CashCustomerPhone) ? cashCustomer.Phone : request.CashCustomerPhone;
                        request.Phone2 = string.IsNullOrWhiteSpace(request.Phone2) ? cashCustomer.Phone2 : request.Phone2;
                        request.VisaNumber = string.IsNullOrWhiteSpace(request.VisaNumber) ? cashCustomer.VisaNumber : request.VisaNumber;
                    }

                    var existingCardInvoiceId = _repository.FindKeshniCardInvoiceDuplicateId(request.VisaNumber, request.Transaction_ID);
                    if (existingCardInvoiceId.HasValue)
                    {
                        validationErrors["VisaNumberDuplicate"] = "هذا الكارت تم إصدار فاتورة تفعيل له من قبل. رقم الفاتورة السابقة: " + existingCardInvoiceId.Value.ToString(CultureInfo.InvariantCulture);
                        LogPosSystemIssue(context, "Save.KycCardInvoiceDuplicate", request, null, "Duplicate Keshni card activation invoice blocked", "Warning", "Validation", BuildSaveDiagnostic("PosTransactionController.Save", request, validationErrors, null));
                        SetJsonErrorStatus(400);
                        return Json(Fail("لا يمكن إصدار فاتورة أخرى لنفس كارت كيشني", "Keshni card activation invoice already exists.", validationErrors));
                    }
                }
                else
                {
                    request.TblCusCshId = null;
                    request.Phone2 = null;
                    request.VisaNumber = null;
                    request.CardSerial = null;
                }

                // Kishny POS uses fixed cash customer CusID=2 for Transactions.CusID; KYC customer remains in TblCusCsh fields.
                request.DefaultCustomerId = 2;
                request.CustomerID = 2;

                // Legacy POS mapping:
                // request.IPN is the UI "ID" field.
                // request.ManualNO is the UI "IPN" field.
                var result = _repository.SaveTransaction(request);
                if (result == null || result.Transaction_ID <= 0 || !_repository.TransactionExists(result.Transaction_ID))
                {
                    SetJsonErrorStatus(500);
                    return Json(Fail("تعذر تأكيد حفظ الفاتورة في قاعدة البيانات", "usp_POS_SaveTransaction returned success but the transaction row was not found after save."));
                }

                if (!_repository.TransactionHasDetails(result.Transaction_ID))
                {
                    SetJsonErrorStatus(500);
                    return Json(Fail("تم إنشاء رأس الفاتورة ولكن لا توجد تفاصيل محفوظة", "Transaction header exists, but Transaction_Details has no rows after save."));
                }

                return Json(new
                {
                    success = true,
                    transactionId = result.Transaction_ID,
                    noteSerial1 = result.NoteSerial1,
                    branchId = request.BranchId,
                    userId = request.UserID
                });
            }
            catch (SqlException ex)
            {
                var diagnostic = BuildSaveDiagnostic("PosTransactionController.Save", request, null, ex);
                LogPosSystemIssue(context, "Save.SqlException", request, ex, ex.Message, "Error", "SqlException", AppendSqlErrorSummary(diagnostic, ex));
                LogPosSaveFailure("Save.SqlException", request, ex);
                SetJsonErrorStatus(500);
                return Json(Fail(FriendlySqlSaveMessage(ex), ex.Message));
            }
            catch (Exception ex)
            {
                var diagnostic = BuildSaveDiagnostic("PosTransactionController.Save", request, null, ex);
                LogPosSystemIssue(context, "Save.Exception", request, ex, ex.Message, "Error", "Exception", diagnostic);
                LogPosSaveFailure("Save.Exception", request, ex);
                SetJsonErrorStatus(500);
                var visibleMessage = "تعذر حفظ الفاتورة. " + SafeExceptionMessage(ex);
                return Json(Fail(visibleMessage, diagnostic));
            }
        }

        [HttpGet]
        public JsonResult SearchAvailableKeshniCards(string term, int? storeId)
        {
            var context = GetPosContext();
            if (context == null)
            {
                SetJsonErrorStatus(401);
                return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."), JsonRequestBehavior.AllowGet);
            }

            var effectiveStoreId = context.CanChangeDefaults ? (storeId ?? context.StoreId) : context.StoreId;
            var cards = _repository.SearchAvailableKeshniCardTokens(term, effectiveStoreId, context.BranchId, context.CanChangeDefaults, 30);
            return Json(new
            {
                success = true,
                cards,
                storeId = effectiveStoreId,
                storeName = context.StoreName
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult ValidateKeshniCardAvailability(string cardNo, string nationalId, string mobile, int? customerId)
        {
            var context = GetPosContext();
            if (context == null)
            {
                SetJsonErrorStatus(401);
                return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."), JsonRequestBehavior.AllowGet);
            }

            if (!context.CanEditKyc)
            {
                SetJsonErrorStatus(403);
                return Json(Fail("ليست لديك صلاحية تعديل بيانات KYC", "CanEditKyc is false."), JsonRequestBehavior.AllowGet);
            }

            var result = _repository.ValidateKeshniCardAvailability(cardNo, nationalId, mobile, customerId);
            if (!result.Available)
            {
                SetJsonErrorStatus(200);
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        private static Dictionary<string, string> ValidateSaveRequest(PosSaveTransactionRequest request, PosUserContext context)
        {
            var errors = new Dictionary<string, string>();
            var isCard = IsKeshniCardTransaction(request.TransactionType);
            var isViolations = string.Equals(request.TransactionType, "violations", StringComparison.OrdinalIgnoreCase);
            var isCashOut = string.Equals(request.TransactionType, "cash-out", StringComparison.OrdinalIgnoreCase);
            var isCashIn = string.Equals(request.TransactionType, "cash-in", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(request.TransactionType))
            {
                errors["TransactionType"] = "نوع العملية مطلوب";
            }

            if (string.IsNullOrWhiteSpace(request.CashCustomerPhone))
            {
                errors["CashCustomerPhone"] = "رقم التليفون مطلوب";
            }

            if (!string.IsNullOrWhiteSpace(request.CashCustomerPhone) && !IsValidEgyptianMobile(request.CashCustomerPhone))
            {
                errors["CashCustomerPhone"] = "رقم التليفون يجب أن يكون 11 رقم ويبدأ بـ 010 أو 011 أو 012 أو 015";
            }

            if (string.IsNullOrWhiteSpace(request.CashCustomerName))
            {
                errors["CashCustomerName"] = "اسم العميل مطلوب";
            }

            if (!isCard && string.IsNullOrWhiteSpace(request.IPN))
            {
                errors["IPN"] = "ID مطلوب";
            }

            if (IsImportantIpnTransaction(request.TransactionType) && string.IsNullOrWhiteSpace(request.ManualNO))
            {
                errors["ManualNO"] = "IPN مطلوب";
            }

            if (isViolations)
            {
                if (request.ViolationsValue.GetValueOrDefault() <= 0)
                {
                    errors["ViolationsValue"] = "قيمة المخالفات مطلوبة";
                }

                if (!request.ViolationPayType.HasValue || (request.ViolationPayType.Value != 0 && request.ViolationPayType.Value != 1))
                {
                    errors["ViolationPayType"] = "طريقة دفع المخالفات مطلوبة";
                }

                if (string.IsNullOrWhiteSpace(request.Tet_NumPoket))
                {
                    errors["Tet_NumPoket"] = "رقم المحفظة مطلوب";
                }
            }
            else if (!isCard && request.RechargeValue.GetValueOrDefault() <= 0)
            {
                errors["RechargeValue"] = "مبلغ الشحن يجب أن يكون أكبر من صفر";
            }

            if (!isCard && !isViolations && request.RechargeValue.GetValueOrDefault() > GetMaxRechargeValue())
            {
                errors["RechargeValue"] = LooksLikePhoneNumber(request.RechargeValue.GetValueOrDefault())
                    ? "قيمة المبلغ غير صحيحة. يبدو أنك أدخلت رقم هاتف بدل مبلغ العملية."
                    : "مبلغ الشحن أكبر من الحد المسموح لهذه الخدمة";
            }

            if (isViolations && request.ViolationsValue.GetValueOrDefault() > GetMaxRechargeValue())
            {
                errors["ViolationsValue"] = LooksLikePhoneNumber(request.ViolationsValue.GetValueOrDefault())
                    ? "قيمة المبلغ غير صحيحة. يبدو أنك أدخلت رقم هاتف بدل مبلغ العملية."
                    : "قيمة المخالفات أكبر من الحد المسموح لهذه الخدمة";
            }

            if (isCashOut && string.IsNullOrWhiteSpace(request.Tet_NumPoket))
            {
                errors["Tet_NumPoket"] = "رقم المحفظة مطلوب";
            }

            if (isCard && (request.IsCashOut || request.IsWallet || request.ItemIDService2.HasValue))
            {
                errors["TransactionType"] = "بيانات العملية لا تطابق كارت كيشني. ابدأ عملية جديدة ثم اختر كارت كيشني مرة أخرى";
            }

            if ((isCashIn || isCashOut || isViolations)
                && (request.TblCusCshId.HasValue
                    || !string.IsNullOrWhiteSpace(request.VisaNumber)
                    || !string.IsNullOrWhiteSpace(request.CardSerial)))
            {
                errors["VisaNumber"] = "بيانات الكارت لا تطابق نوع العملية المحدد. ابدأ عملية جديدة ثم اختر النوع الصحيح";
            }

            if (!isCashOut && !isCard && !isViolations && request.IsWallet)
            {
                errors["IsWallet"] = "بيانات المحفظة لا تطابق نوع العملية المحدد";
            }

            if (request.Items == null || request.Items.Count == 0 || !request.Items.Exists(i => i != null && i.Item_ID.HasValue))
            {
                errors["Items"] = "لا توجد خدمة كيشني محملة";
            }

            if (request.Emp_ID.GetValueOrDefault(context.EmpId.GetValueOrDefault()) <= 0)
            {
                errors["Emp_ID"] = "لا يوجد موظف / مندوب مبيعات مضبوط لهذا المستخدم";
            }

            if (isCashOut && !request.ItemIDService2.HasValue)
            {
                errors["ItemIDService2"] = "المحفظة/البنك مطلوبة لهذا النوع";
            }

            if (request.PaymentType <= 0 && !context.PaymentTypeId.HasValue)
            {
                errors["PaymentType"] = "طريقة الدفع مطلوبة";
            }

            if (!request.BoxID.HasValue && !context.BoxId.HasValue)
            {
                errors["BoxID"] = "الخزنة غير محددة";
            }

            if (!request.BranchId.HasValue && !context.BranchId.HasValue)
            {
                errors["BranchId"] = "الفرع غير محدد";
            }

            if (!request.StoreID.HasValue && !context.StoreId.HasValue)
            {
                errors["StoreID"] = "المخزن غير محدد";
            }

            if (isCard && string.IsNullOrWhiteSpace(request.VisaNumber))
            {
                errors["VisaNumber"] = "الكارت مطلوب في حالة كارت كيشني";
            }

            if (isCard && !request.TblCusCshId.HasValue)
            {
                errors["TblCusCshId"] = "يجب تفعيل الكارت وحفظ بيانات KYC قبل حفظ الفاتورة";
            }

            return errors;
        }

        private static bool LooksLikePhoneNumber(decimal value)
        {
            if (value <= 0 || value != decimal.Truncate(value))
            {
                return false;
            }

            var digits = value.ToString("0", CultureInfo.InvariantCulture);
            return digits.Length >= 10 && digits.Length <= 14;
        }

        private void AddImportantIpnDuplicateErrors(PosSaveTransactionRequest request, IDictionary<string, string> errors)
        {
            if (request == null || errors == null || !IsImportantIpnTransaction(request.TransactionType) || string.IsNullOrWhiteSpace(request.ManualNO))
            {
                return;
            }

            if (_repository.ImportantIpnExistsForPosSale(request.ManualNO, request.Transaction_ID))
            {
                errors["ManualNO"] = "رقم IPN مكرر في كاش إن أو كارت كيشني";
            }
        }

        private static bool IsImportantIpnTransaction(string transactionType)
        {
            return string.Equals(transactionType, "cash-in", StringComparison.OrdinalIgnoreCase);
        }

        private static Dictionary<string, string> ValidateSqlDateRange(PosCashCustomerSaveRequest request)
        {
            var errors = new Dictionary<string, string>();
            if (request == null)
            {
                return errors;
            }

            AddSqlDateRangeError(errors, "BirthDate", request.BirthDate, "تاريخ الميلاد");
            AddSqlDateRangeError(errors, "CardDate", request.CardDate, "تاريخ الإصدار");
            AddSqlDateRangeError(errors, "CardEndDate", request.CardEndDate, "تاريخ الانتهاء");
            AddSqlDateRangeError(errors, "OrderDate", request.OrderDate, "تاريخ العملية");
            return errors;
        }

        private static void AddSqlDateRangeError(IDictionary<string, string> errors, string key, DateTime? value, string label)
        {
            if (!value.HasValue)
            {
                return;
            }

            if (value.Value < System.Data.SqlTypes.SqlDateTime.MinValue.Value
                || value.Value > System.Data.SqlTypes.SqlDateTime.MaxValue.Value)
            {
                errors[key] = label + " غير صحيح. برجاء إدخال تاريخ بسنة كاملة وصحيحة مثل 2026.";
            }
        }

        private void LogPosSystemIssue(
            PosUserContext context,
            string actionName,
            PosSaveTransactionRequest request,
            Exception exception,
            string message,
            string severity,
            string status,
            string requestSummary)
        {
            PosSystemErrorLogger.Log(
                _repository,
                Request,
                context,
                "PosTransaction",
                actionName,
                request == null ? null : request.TransactionType,
                request == null ? null : request.Transaction_ID,
                message,
                exception,
                requestSummary,
                severity,
                status);
        }

        private void LogCommissionFailure(PosUserContext context, string actionName, PosCommissionRequest request, Exception exception, string status)
        {
            PosSystemErrorLogger.Log(
                _repository,
                Request,
                context,
                "PosTransaction",
                actionName,
                request == null ? null : request.ServiceType,
                null,
                exception == null ? null : exception.Message,
                exception,
                BuildCommissionRequestSummary(request),
                "Error",
                status);
        }

        private static string BuildCommissionRequestSummary(PosCommissionRequest request)
        {
            if (request == null)
            {
                return "Request is null";
            }

            var parts = new List<string>
            {
                "ServiceType=" + (request.ServiceType ?? ""),
                "ItemID=" + (request.ItemID.HasValue ? request.ItemID.Value.ToString(CultureInfo.InvariantCulture) : ""),
                "BranchId=" + (request.BranchId.HasValue ? request.BranchId.Value.ToString(CultureInfo.InvariantCulture) : ""),
                "RechargeValue=" + request.RechargeValue.ToString(CultureInfo.InvariantCulture),
                "Vatyo=" + (request.Vatyo.HasValue ? request.Vatyo.Value.ToString(CultureInfo.InvariantCulture) : ""),
                "IsWallet=" + request.IsWallet,
                "HaveGuarantee=" + request.HaveGuarantee
            };

            return string.Join("; ", parts);
        }

        private static string ValidateCommissionAmount(PosCommissionRequest request)
        {
            if (request == null)
            {
                return null;
            }

            if (string.Equals(request.ServiceType, "card", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (request.RechargeValue < 0)
            {
                return "المبلغ يجب أن يكون أكبر من أو يساوي صفر";
            }

            return request.RechargeValue > GetMaxRechargeValue()
                ? (LooksLikePhoneNumber(request.RechargeValue)
                    ? "قيمة المبلغ غير صحيحة. يبدو أنك أدخلت رقم هاتف بدل مبلغ العملية."
                    : "المبلغ أكبر من الحد المسموح لهذه الخدمة")
                : null;
        }

        private static decimal GetMaxRechargeValue()
        {
            decimal configured;
            return decimal.TryParse(ConfigurationManager.AppSettings["PosMaxRechargeValue"], NumberStyles.Any, CultureInfo.InvariantCulture, out configured) && configured > 0
                ? configured
                : DefaultMaxRechargeValue;
        }

        private static string GetCommissionFailureMessage(PosCommissionRequest request)
        {
            if (request != null && string.Equals(request.ServiceType, "cash-out", StringComparison.OrdinalIgnoreCase))
            {
                return "تعذر حساب عمولة كاش أوت. راجع نوع الخدمة والمحفظة/البنك والمبلغ ثم حاول مرة أخرى.";
            }

            return "تعذر حساب عمولة العملية. راجع نوع الخدمة والمبلغ ثم حاول مرة أخرى.";
        }

        private static string BuildSaveRequestSummary(PosSaveTransactionRequest request, IDictionary<string, string> validationErrors)
        {
            if (request == null)
            {
                return "Request is null";
            }

            var parts = new List<string>
            {
                "Transaction_ID=" + (request.Transaction_ID.HasValue ? request.Transaction_ID.Value.ToString(CultureInfo.InvariantCulture) : ""),
                "TransactionType=" + (request.TransactionType ?? ""),
                "BranchId=" + (request.BranchId.HasValue ? request.BranchId.Value.ToString(CultureInfo.InvariantCulture) : ""),
                "StoreID=" + (request.StoreID.HasValue ? request.StoreID.Value.ToString(CultureInfo.InvariantCulture) : ""),
                "BoxID=" + (request.BoxID.HasValue ? request.BoxID.Value.ToString(CultureInfo.InvariantCulture) : ""),
                "PaymentType=" + request.PaymentType.ToString(CultureInfo.InvariantCulture),
                "IsCashOut=" + request.IsCashOut,
                "IsWallet=" + request.IsWallet,
                "ItemIDService=" + (request.ItemIDService.HasValue ? request.ItemIDService.Value.ToString(CultureInfo.InvariantCulture) : ""),
                "ItemIDService2=" + (request.ItemIDService2.HasValue ? request.ItemIDService2.Value.ToString(CultureInfo.InvariantCulture) : ""),
                "RechargeValue=" + request.RechargeValue.GetValueOrDefault().ToString(CultureInfo.InvariantCulture),
                "CommissionValue=" + Convert.ToString(request.CommissionValue, CultureInfo.InvariantCulture),
                "VatValue=" + Convert.ToString(request.VatValue, CultureInfo.InvariantCulture),
                "NetValue=" + Convert.ToString(request.NetValue, CultureInfo.InvariantCulture),
                "PayedValue=" + Convert.ToString(request.PayedValue, CultureInfo.InvariantCulture),
                "Items=" + (request.Items == null ? "0" : request.Items.Count.ToString(CultureInfo.InvariantCulture))
            };

            if (validationErrors != null && validationErrors.Count > 0)
            {
                parts.Add("Validation=" + string.Join(" | ", validationErrors.Select(v => v.Key + ":" + v.Value)));
            }

            return string.Join("; ", parts);
        }

        private static string BuildSaveDiagnostic(string endpointName, PosSaveTransactionRequest request, IDictionary<string, string> validationErrors, Exception exception)
        {
            var isCard = request != null && IsKeshniCardTransaction(request.TransactionType);
            var exceptionText = exception == null
                ? string.Empty
                : ("Exception=" + (exception.Message ?? string.Empty) + "; InnerException=" + (exception.InnerException != null ? exception.InnerException.Message : string.Empty));

            return string.Join("; ", new[]
            {
                "Endpoint=" + (endpointName ?? string.Empty),
                "ServiceType=" + (request != null ? (request.TransactionType ?? string.Empty) : string.Empty),
                "IsCard=" + isCard,
                BuildSaveRequestSummary(request, validationErrors),
                exceptionText
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private static string SafeExceptionMessage(Exception ex)
        {
            if (ex == null)
            {
                return "حدث خطأ غير متوقع.";
            }

            var message = string.IsNullOrWhiteSpace(ex.Message) ? null : ex.Message.Trim();
            var inner = ex.InnerException != null && !string.IsNullOrWhiteSpace(ex.InnerException.Message)
                ? ex.InnerException.Message.Trim()
                : null;

            if (!string.IsNullOrWhiteSpace(message) && !string.IsNullOrWhiteSpace(inner) && !string.Equals(message, inner, StringComparison.Ordinal))
            {
                return message + " | " + inner;
            }

            return !string.IsNullOrWhiteSpace(message) ? message : "حدث خطأ غير متوقع.";
        }

        private static string AppendSqlErrorSummary(string requestSummary, SqlException exception)
        {
            var sqlErrors = BuildSqlErrorSummary(exception);
            if (string.IsNullOrWhiteSpace(sqlErrors))
            {
                return requestSummary;
            }

            return (requestSummary ?? string.Empty) + "; SqlErrors=" + sqlErrors;
        }

        private static string BuildSqlErrorSummary(SqlException exception)
        {
            if (exception == null || exception.Errors == null)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            foreach (SqlError error in exception.Errors)
            {
                if (error != null)
                {
                    parts.Add(error.Number.ToString(CultureInfo.InvariantCulture));
                }
            }

            return string.Join(",", parts.Distinct());
        }

        private void AddServiceTypeValidationErrors(PosSaveTransactionRequest request, IDictionary<string, string> errors)
        {
            if (request == null)
            {
                return;
            }

            var selectedItems = request.Items == null
                ? new List<PosTransactionItemDto>()
                : request.Items.Where(i => i != null && i.Item_ID.HasValue && i.Item_ID.Value > 0).ToList();

            if (selectedItems.Count != 1)
            {
                errors["Items"] = "يجب تحميل خدمة كيشني واحدة فقط للفاتورة";
                return;
            }

            var detailItemId = selectedItems[0].Item_ID.Value;
            if (!_repository.IsServiceItemValidForTransactionType(request.TransactionType, detailItemId))
            {
                errors["Items"] = "الخدمة المحملة لا تطابق نوع العملية المحدد";
            }

            if (request.ItemIDService.HasValue
                && request.ItemIDService.Value > 0
                && !_repository.IsServiceItemValidForTransactionType(request.TransactionType, request.ItemIDService.Value))
            {
                errors["ItemIDService"] = "نوع الشحن لا يطابق نوع العملية المحدد";
            }
        }

        private static Dictionary<string, string> ValidateKeshniCardCustomer(PosCashCustomerSaveRequest request)
        {
            var errors = new Dictionary<string, string>();
            foreach (var dateError in ValidateSqlDateRange(request))
            {
                errors[dateError.Key] = dateError.Value;
            }

            if (string.IsNullOrWhiteSpace(request.PhoneNo2))
            {
                errors["PhoneNo2"] = "رقم المحمول مطلوب";
            }
            else if (!IsValidEgyptianMobile(request.PhoneNo2))
            {
                errors["PhoneNo2"] = "رقم المحمول يجب أن يكون 11 رقم ويبدأ بـ 010 أو 011 أو 012 أو 015";
            }

            if (string.IsNullOrWhiteSpace(request.Name) && string.IsNullOrWhiteSpace(request.ArabicName0))
            {
                errors["Name"] = "اسم العميل مطلوب";
            }

            if (string.IsNullOrWhiteSpace(request.Tet_NumPoket) || request.Tet_NumPoket.Trim().Length != 14)
            {
                errors["Tet_NumPoket"] = "الرقم القومي مطلوب ويجب أن يكون 14 رقم";
            }
            else if (!Regex.IsMatch(request.Tet_NumPoket.Trim(), @"^[0-9]{14}$"))
            {
                errors["Tet_NumPoket"] = "الرقم القومي يجب أن يحتوي على أرقام فقط";
            }

            if (string.IsNullOrWhiteSpace(request.CardNo))
            {
                errors["CardNo"] = "رقم الكارت مطلوب";
            }
            else
            {
                var cardNo = request.CardNo.Trim();
                if (cardNo.Length != 18 && cardNo.Length != 8)
                {
                    errors["CardNo"] = "رقم التوكن/الكارت يجب أن يكون 18 أو 8 رقم";
                }
            }

            if (!string.IsNullOrWhiteSpace(request.CardNo) && request.CardNo.Trim().Length != 8)
            {
                if (string.IsNullOrWhiteSpace(request.EnglishName0) || string.IsNullOrWhiteSpace(request.EnglishName1) || string.IsNullOrWhiteSpace(request.EnglishName2))
                {
                    errors["EnglishName"] = "الاسم الإنجليزي مطلوب";
                }

                if (string.IsNullOrWhiteSpace(request.EnglishName5))
                {
                    errors["EnglishAddress"] = "العنوان الإنجليزي مطلوب";
                }
            }

            return errors;
        }

        private KeshniDuplicateInfo AddKeshniCardDuplicateErrors(PosCashCustomerSaveRequest request, IDictionary<string, string> errors, PosUserContext context)
        {
            var duplicateIds = new List<int>();
            var cardLength = string.IsNullOrWhiteSpace(request.CardNo) ? 0 : request.CardNo.Trim().Length;

            // The same KYC customer can have one 8-character card and one 18-character card.
            // Block duplicate national/mobile only within the same card type length.
            var duplicateCustomerId = _repository.FindKeshniCardDuplicateId("Tet_NumPoket", request.Tet_NumPoket, request.CustomerID, cardLength > 0 ? (int?)cardLength : null);
            if (duplicateCustomerId.HasValue)
            {
                duplicateIds.Add(duplicateCustomerId.Value);
                errors["Tet_NumPoketDuplicate"] = "هذا العميل لديه كارت مفعل بالفعل من نفس النوع. مسموح فقط بكارت واحد من كل نوع.";
            }

            var phoneDuplicateId = string.IsNullOrWhiteSpace(request.Tet_NumPoket)
                ? _repository.FindKeshniCardDuplicateId("PhoneNo2", request.PhoneNo2, request.CustomerID, cardLength > 0 ? (int?)cardLength : null)
                : null;
            if (phoneDuplicateId.HasValue)
            {
                duplicateIds.Add(phoneDuplicateId.Value);
                errors["PhoneNo2Duplicate"] = "هذا العميل لديه كارت مفعل بالفعل من نفس النوع. مسموح فقط بكارت واحد من كل نوع.";
            }

            var cardDuplicateId = _repository.FindKeshniCardDuplicateId("CardNo", request.CardNo, request.CustomerID);
            if (cardDuplicateId.HasValue)
            {
                duplicateIds.Add(cardDuplicateId.Value);
                errors["CardNoDuplicate"] = "هذا الكارت/التوكن تم تفعيله من قبل ولا يمكن استخدامه مرة أخرى.";
            }

            if (!string.IsNullOrWhiteSpace(request.CardNo))
            {
                var availability = _repository.ValidateKeshniCardAvailability(request.CardNo, request.Tet_NumPoket, request.PhoneNo2, request.CustomerID);
                if (availability != null && !availability.Available)
                {
                    if (availability.ExistingCustomerId.HasValue)
                    {
                        duplicateIds.Add(availability.ExistingCustomerId.Value);
                    }

                    errors["CardNoAvailability"] = availability.Message;
                }
            }

            var distinctDuplicateIds = duplicateIds.Distinct().ToList();
            var duplicateInfo = new KeshniDuplicateInfo
            {
                HasDuplicate = distinctDuplicateIds.Count > 0
            };

            if (distinctDuplicateIds.Count == 1)
            {
                duplicateInfo.ExistingCustomerId = distinctDuplicateIds[0];
                duplicateInfo.ExistingCustomer = _repository.GetKeshniCardCustomerById(
                    distinctDuplicateIds[0],
                    context == null ? (int?)null : context.BranchId,
                    context != null && context.CanChangeDefaults);
            }

            return duplicateInfo;
        }

        private IList<PosKycAttachmentDto> SaveKeshniAttachments(string subjectNo)
        {
            if (Request.Files == null || Request.Files.Count == 0)
            {
                return _repository.GetKeshniCardAttachments(subjectNo);
            }

            var folderName = DateTime.Now.ToString("yyyyMMdd");
            var folderPath = Path.Combine(GetKycAttachmentRootPath(), folderName);
            Directory.CreateDirectory(folderPath);
            var originalNames = Request.Form.GetValues("attachmentOriginalNamesBase64") ?? new string[0];

            for (var i = 0; i < Request.Files.Count; i++)
            {
                var file = Request.Files[i];
                if (file == null || file.ContentLength <= 0)
                {
                    continue;
                }

                if (file.ContentLength > GetKycMaxAttachmentBytes())
                {
                    throw new InvalidOperationException("حجم المرفق أكبر من الحد المسموح به: " + Path.GetFileName(file.FileName));
                }

                var originalName = DecodeAttachmentOriginalName(originalNames, i);
                if (string.IsNullOrWhiteSpace(originalName))
                {
                    originalName = Path.GetFileName(file.FileName);
                }
                if (string.IsNullOrWhiteSpace(originalName))
                {
                    throw new InvalidOperationException("اسم ملف المرفق غير صالح.");
                }

                var safeOriginalName = MakeSafeFileName(originalName);
                var imageName = MakeSafeFileName(DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture)
                    + "_" + i.ToString(CultureInfo.InvariantCulture)
                    + "_" + safeOriginalName);
                var destinationName = MakeSafeFileName(subjectNo + imageName);
                var destinationPath = Path.Combine(folderPath, destinationName);
                var duplicateIndex = 1;
                while (System.IO.File.Exists(destinationPath))
                {
                    var extension = Path.GetExtension(imageName);
                    var baseName = Path.GetFileNameWithoutExtension(imageName);
                    imageName = MakeSafeFileName(baseName + "_" + duplicateIndex.ToString(CultureInfo.InvariantCulture) + extension);
                    destinationName = MakeSafeFileName(subjectNo + imageName);
                    destinationPath = Path.Combine(folderPath, destinationName);
                    duplicateIndex++;
                }

                file.SaveAs(destinationPath);
                _repository.SaveKeshniCardAttachment(subjectNo, imageName, originalName);
            }

            return _repository.GetKeshniCardAttachments(subjectNo);
        }

        private static int GetKycMaxAttachmentBytes()
        {
            int configured;
            return int.TryParse(ConfigurationManager.AppSettings["KycMaxAttachmentBytes"], out configured) && configured > 0
                ? configured
                : 100 * 1024 * 1024;
        }

        private static string DecodeAttachmentOriginalName(string[] encodedNames, int index)
        {
            if (encodedNames == null || index < 0 || index >= encodedNames.Length || string.IsNullOrWhiteSpace(encodedNames[index]))
            {
                return null;
            }

            try
            {
                var bytes = Convert.FromBase64String(encodedNames[index]);
                return Path.GetFileName(Encoding.UTF8.GetString(bytes));
            }
            catch
            {
                System.Diagnostics.Trace.TraceWarning("Unable to decode KYC attachment original name at index " + index.ToString(CultureInfo.InvariantCulture));
                return null;
            }
        }

        private static string GetKycAttachmentRootPath()
        {
            var configuredPath = ConfigurationManager.AppSettings["PosKycAttachmentRootPath"];
            return string.IsNullOrWhiteSpace(configuredPath)
                ? @"C:\Dynamic Byte\Doc"
                : configuredPath.Trim();
        }

        private static string MakeSafeFileName(string value)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return new string((value ?? "attachment").Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        }

        private void ApplyCashOutSecondaryDefault(PosSaveTransactionRequest request)
        {
            if (request == null
                || !string.Equals(request.TransactionType, "cash-out", StringComparison.OrdinalIgnoreCase)
                || request.ItemIDService2.HasValue
                || !request.ItemIDService.HasValue)
            {
                return;
            }

            // VB6 fills cmbItemName2 from TblSpecification for Cash Out after cmbItemName is selected.
            var secondaryItems = _repository.GetSecondaryServiceItems(request.TransactionType, request.ItemIDService.Value);
            if (secondaryItems != null && secondaryItems.Count > 0)
            {
                request.ItemIDService2 = secondaryItems[0].Id;
            }
        }

        private object BuildOtherBranchKycHint(string term, int? branchId)
        {
            var hint = _repository.FindKeshniCardCustomerOtherBranchHint(term, branchId);
            if (hint == null)
            {
                return null;
            }

            var branchName = string.IsNullOrWhiteSpace(hint.BranchName)
                ? "فرع آخر"
                : hint.BranchName.Trim();

            return new
            {
                success = true,
                found = false,
                otherBranch = true,
                branchName = branchName,
                message = "تم العثور على بيانات KYC في فرع آخر: " + branchName
            };
        }

        private static object Fail(string message, string technicalMessage, IDictionary<string, string> validationErrors = null, int? duplicateCustomerId = null, bool duplicate = false, PosCustomerLookupDto existingCustomer = null)
        {
            var errors = validationErrors ?? new Dictionary<string, string>();
            var details = IsKycDebugEnabled() ? (technicalMessage ?? string.Empty) : string.Empty;
            return new
            {
                success = false,
                message = message,
                details = details,
                technicalMessage = details,
                technicalDetails = details,
                validationErrors = errors,
                validationErrorsDetailed = errors.Select(e => new { field = e.Key, message = e.Value }).ToArray(),
                validationErrorsList = errors.Values.ToArray(),
                duplicate = duplicate,
                duplicateCustomerId = duplicateCustomerId,
                existingCustomerId = duplicateCustomerId,
                existingCustomer = existingCustomer
            };
        }

        private sealed class KeshniDuplicateInfo
        {
            public bool HasDuplicate { get; set; }
            public int? ExistingCustomerId { get; set; }
            public PosCustomerLookupDto ExistingCustomer { get; set; }
        }

        private void SetJsonErrorStatus(int statusCode)
        {
            // OWIN cookie auth converts 401 responses into the main ERP login HTML.
            // POS AJAX endpoints need to keep returning JSON, so use 440 for expired POS sessions.
            Response.StatusCode = statusCode == 401 ? 440 : statusCode;
            Response.TrySkipIisCustomErrors = true;
            Response.SuppressFormsAuthenticationRedirect = true;
        }

        private static bool IsKycDebugEnabled()
        {
            return string.Equals(ConfigurationManager.AppSettings["DebugKYC"], "true", StringComparison.OrdinalIgnoreCase);
        }

        private static void LogKycFailure(string action, PosCashCustomerSaveRequest request, Exception exception, IDictionary<string, string> validationErrors)
        {
            try
            {
                var logRoot = System.Web.HttpContext.Current == null
                    ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Logs")
                    : System.Web.HttpContext.Current.Server.MapPath("~/App_Data/Logs");
                Directory.CreateDirectory(logRoot);

                var path = Path.Combine(logRoot, "pos-kyc-" + DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".log");
                var lines = new List<string>
                {
                    "------------------------------------------------------------",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
                    "Action: " + action,
                    "Request: " + BuildKycSafeSnapshot(request)
                };

                if (validationErrors != null && validationErrors.Count > 0)
                {
                    lines.Add("Validation: " + string.Join(" | ", validationErrors.Select(e => e.Key + "=" + e.Value)));
                }

                if (exception != null)
                {
                    lines.Add("Exception: " + exception.Message);
                    lines.Add("StackTrace: " + exception);
                }

                System.IO.File.AppendAllLines(path, lines, Encoding.UTF8);
            }
            catch (Exception logEx)
            {
                System.Diagnostics.Trace.TraceError("Failed to write POS KYC log: " + logEx);
            }
        }

        private static string BuildKycSafeSnapshot(PosCashCustomerSaveRequest request)
        {
            if (request == null)
            {
                return "<null request>";
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "CustomerID={0}; BranchId={1}; UserId={2}; EmpId={3}; PhoneNo2={4}; NationalId={5}; CardNo={6}; EasyCashType={7}; Files={8}",
                request.CustomerID,
                request.BranchId,
                request.UserId,
                request.EmpId,
                MaskValue(request.PhoneNo2),
                MaskValue(request.Tet_NumPoket),
                MaskValue(request.CardNo),
                request.EasyCashType,
                System.Web.HttpContext.Current != null && System.Web.HttpContext.Current.Request != null && System.Web.HttpContext.Current.Request.Files != null
                    ? System.Web.HttpContext.Current.Request.Files.Count
                    : 0);
        }

        private static string MaskValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            value = value.Trim();
            if (value.Length <= 4)
            {
                return new string('*', value.Length);
            }

            return new string('*', value.Length - 4) + value.Substring(value.Length - 4);
        }

        private static string FriendlySqlKycMessage(SqlException ex)
        {
            var message = ex == null ? string.Empty : ex.Message;
            if (message.IndexOf("هذا الكارت/التوكن تم تفعيله من قبل", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "هذا الكارت/التوكن تم تفعيله من قبل ولا يمكن استخدامه مرة أخرى.";
            }

            if (message.IndexOf("هذا العميل لديه كارت مفعل بالفعل من نفس النوع", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "هذا العميل لديه كارت مفعل بالفعل من نفس النوع. مسموح فقط بكارت واحد من كل نوع.";
            }

            if (message.IndexOf("هذا الكارت غير متاح بالمخزون", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "هذا الكارت غير متاح بالمخزون أو تم صرفه/استخدامه من قبل.";
            }

            if (ex != null && (ex.Number == 2601 || ex.Number == 2627))
            {
                return "البيانات مكررة. راجع رقم الهاتف أو الرقم القومي أو رقم الكارت.";
            }

            if (ex != null && ex.Number == 547)
            {
                return "هناك بيانات مرتبطة غير صحيحة. راجع الفرع أو المستخدم أو بيانات الكارت.";
            }

            if (ex != null && ex.Number == 515)
            {
                return "هناك بيانات أساسية ناقصة ولا يمكن حفظ الكارت قبل استكمالها.";
            }

            if (ex != null && (ex.Number == 245 || ex.Number == 8114))
            {
                return "هناك قيمة غير صالحة في بيانات الكارت. راجع الأرقام والتواريخ.";
            }

            if (ex != null && (ex.Number == 8152 || ex.Number == 2628))
            {
                return "إحدى القيم أطول من المسموح به في قاعدة البيانات.";
            }

            if (message.IndexOf("account is locked out", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "فشل الاتصال بقاعدة البيانات: حساب SQL مغلق. راجع مستخدم الاتصال في KishnyCashConnection.";
            }

            if (ex != null && ex.Number == 18456)
            {
                return "فشل تسجيل الدخول إلى قاعدة البيانات أثناء حفظ بيانات الكارت. راجع اسم المستخدم/كلمة المرور أو صلاحيات KishnyCashConnection.";
            }

            if (message.IndexOf("permission", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("denied", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "فشل حفظ بيانات الكارت بسبب صلاحيات غير كافية على قاعدة البيانات.";
            }

            if (message.IndexOf("duplicate", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("UNIQUE", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("PRIMARY KEY", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "تعذر حفظ بيانات الكارت بسبب تكرار بيانات مسجلة من قبل.";
            }

            return "حدث خطأ من قاعدة البيانات أثناء حفظ بيانات الكارت";
        }

        private static bool IsKeshniActivationValidationSqlError(SqlException ex)
        {
            var message = ex == null ? string.Empty : ex.Message;
            return message.IndexOf("هذا الكارت/التوكن تم تفعيله من قبل", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("هذا العميل لديه كارت مفعل بالفعل من نفس النوع", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("هذا الكارت غير متاح بالمخزون", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FriendlySqlSaveMessage(SqlException ex)
        {
            var message = ex == null ? string.Empty : ex.Message;
            if (message.IndexOf("هذا الكارت غير متاح بالمخزون", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "هذا الكارت غير متاح بالمخزون أو تم صرفه/استخدامه من قبل.";
            }

            if (message.IndexOf("يجب تفعيل الكارت وحفظ بيانات KYC", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "يجب تفعيل الكارت وحفظ بيانات KYC قبل حفظ الفاتورة.";
            }

            if (HasSqlError(ex, 1205))
            {
                return "حدث تزاحم أثناء الحفظ. برجاء المحاولة مرة أخرى، وإذا تكرر البلاغ تواصل مع الدعم.";
            }

            if (message.IndexOf("Unable to allocate", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "تعذر تجهيز أرقام القيد المحاسبي أو الفاتورة أثناء الحفظ. برجاء المحاولة مرة أخرى، وتم تسجيل التفاصيل الفنية للدعم.";
            }

            if (message.IndexOf("account is locked out", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "فشل الاتصال بقاعدة البيانات: حساب SQL مغلق. راجع مستخدم الاتصال في KishnyCashConnection.";
            }

            if (ex != null && ex.Number == 18456)
            {
                return "فشل تسجيل الدخول إلى قاعدة البيانات. راجع اسم المستخدم/كلمة المرور أو صلاحيات KishnyCashConnection.";
            }

            if (message.IndexOf("permission", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("denied", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "فشل تنفيذ الحفظ بسبب صلاحيات غير كافية على قاعدة البيانات.";
            }

            return "حدث خطأ من قاعدة البيانات أثناء الحفظ";
        }

        private static bool HasSqlError(SqlException exception, int errorNumber)
        {
            if (exception == null || exception.Errors == null)
            {
                return false;
            }

            foreach (SqlError error in exception.Errors)
            {
                if (error != null && error.Number == errorNumber)
                {
                    return true;
                }
            }

            return false;
        }

        private static void LogPosSaveFailure(string action, PosSaveTransactionRequest request, Exception exception)
        {
            try
            {
                var logRoot = System.Web.HttpContext.Current == null
                    ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Logs")
                    : System.Web.HttpContext.Current.Server.MapPath("~/App_Data/Logs");
                Directory.CreateDirectory(logRoot);

                var path = Path.Combine(logRoot, "pos-save-" + DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".log");
                var lines = new List<string>
                {
                    "------------------------------------------------------------",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
                    "Action: " + action,
                    "Request: " + BuildSaveSafeSnapshot(request)
                };

                if (exception != null)
                {
                    var sqlException = exception as SqlException;
                    if (sqlException != null)
                    {
                        lines.Add("SqlErrors: " + BuildSqlErrorSummary(sqlException));
                    }

                    lines.Add("Exception: " + exception.Message);
                    lines.Add("StackTrace: " + exception);
                }

                System.IO.File.AppendAllLines(path, lines, Encoding.UTF8);
            }
            catch (Exception logEx)
            {
                System.Diagnostics.Trace.TraceError("Failed to write POS save log: " + logEx);
            }
        }

        private static string BuildSaveSafeSnapshot(PosSaveTransactionRequest request)
        {
            if (request == null)
            {
                return "<null request>";
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "Transaction_ID={0}; Type={1}; BranchId={2}; StoreID={3}; BoxID={4}; UserID={5}; Emp_ID={6}; Items={7}; Net={8}; Paid={9}; ManualNO={10}; IPN={11}",
                request.Transaction_ID,
                request.TransactionType,
                request.BranchId,
                request.StoreID,
                request.BoxID,
                request.UserID,
                request.Emp_ID,
                request.Items == null ? 0 : request.Items.Count,
                request.NetValue,
                request.PayedValue,
                MaskValue(request.ManualNO),
                MaskValue(request.IPN));
        }

        private PosUserContext GetPosContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _repository);
        }

        private static bool IsKeshniCardTransaction(string transactionType)
        {
            return string.Equals(transactionType, "card", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsValidEgyptianMobile(string mobile)
        {
            return Regex.IsMatch(mobile ?? string.Empty, @"^(010|011|012|015)[0-9]{8}$");
        }
    }
}

