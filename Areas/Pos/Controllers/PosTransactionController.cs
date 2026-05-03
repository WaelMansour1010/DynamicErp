using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using MyERP.Areas.Pos.Reports;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class PosTransactionController : Controller
    {
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
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            ViewBag.PosContext = context;
            return View();
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
        public JsonResult GetSecondaryServiceItems(string serviceType, int itemId)
        {
            return Json(_repository.GetSecondaryServiceItems(serviceType, itemId), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult LookupCustomerByPhone(string phone)
        {
            var context = GetPosContext();
            return Json(_repository.LookupKeshniCardCustomer(phone, context != null ? context.BranchId : null, context != null && context.CanChangeDefaults), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult SearchKeshniCardCustomers(string term)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."), JsonRequestBehavior.AllowGet);
            }

            return Json(_repository.SearchKeshniCardCustomers(term, context.BranchId, context.CanChangeDefaults), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CalculateCommission(PosCommissionRequest request)
        {
            request = request ?? new PosCommissionRequest();
            var context = GetPosContext();
            if (context != null && !request.BranchId.HasValue)
            {
                request.BranchId = context.BranchId;
            }

            return Json(_repository.CalculateCommission(request));
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
                Response.StatusCode = 401;
                return Json(Fail("ظٹط¬ط¨ طھط³ط¬ظٹظ„ ط¯ط®ظˆظ„ ظ†ظ‚ط·ط© ط§ظ„ط¨ظٹط¹ ط£ظˆظ„ط§ظ‹", "POS session context is missing."), JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                UserId = context.UserId,
                UserName = context.UserName,
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
                IsFullAccess = context.IsFullAccess,
                CanChangeDefaults = context.CanChangeDefaults
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetEmployeeBalances()
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(Fail("ظٹط¬ط¨ طھط³ط¬ظٹظ„ ط¯ط®ظˆظ„ ظ†ظ‚ط·ط© ط§ظ„ط¨ظٹط¹ ط£ظˆظ„ط§ظ‹", "POS session context is missing."), JsonRequestBehavior.AllowGet);
            }

            return Json(_repository.GetEmployeeBalances(context.UserId, context.BoxId), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetTodayInvoices(string term)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."), JsonRequestBehavior.AllowGet);
            }

            return Json(_repository.GetTodayInvoices(context.UserId, context.CanChangeDefaults, term), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult TodaySummary()
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."), JsonRequestBehavior.AllowGet);
            }

            try
            {
                return Json(_repository.GetTodaySummary(context.UserId, context.BranchId, context.CanChangeDefaults), JsonRequestBehavior.AllowGet);
            }
            catch (SqlException ex)
            {
                Response.StatusCode = 500;
                return Json(Fail("تعذر تحميل ملخص اليوم من قاعدة البيانات", ex.Message), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(Fail("تعذر تحميل ملخص اليوم", ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetInvoice(int transactionId)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."), JsonRequestBehavior.AllowGet);
            }

            var invoice = _repository.GetInvoiceForReview(transactionId, context.UserId, context.CanChangeDefaults);
            if (invoice == null)
            {
                Response.StatusCode = 404;
                return Json(Fail("لم يتم العثور على الفاتورة أو لا تملك صلاحية فتحها", "Invoice not found or not allowed."), JsonRequestBehavior.AllowGet);
            }

            return Json(invoice, JsonRequestBehavior.AllowGet);
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
                    Response.StatusCode = 401;
                    return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."));
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
                var duplicateCustomerId = AddKeshniCardDuplicateErrors(request, validationErrors);
                if (validationErrors.Count > 0)
                {
                    LogKycFailure("SaveKeshniCardCustomer.Validation", request, null, validationErrors);
                    Response.StatusCode = 400;
                    return Json(Fail("راجع بيانات تفعيل الكارت", "Keshni Card KYC validation failed.", validationErrors, duplicateCustomerId));
                }

                var saved = _repository.SaveCashCustomer(request);
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
                    Response.StatusCode = 500;
                    return Json(Fail("تم حفظ بيانات العميل لكن لا توجد صلاحية لحفظ المرفقات. راجع صلاحيات مسار المرفقات.", ex.ToString()));
                }
                catch (IOException ex)
                {
                    LogKycFailure("SaveKeshniCardCustomer.AttachmentIO", request, ex, null);
                    Response.StatusCode = 500;
                    return Json(Fail("تم حفظ بيانات العميل لكن حدث خطأ أثناء حفظ ملفات المرفقات.", ex.ToString()));
                }
                catch (SqlException ex)
                {
                    LogKycFailure("SaveKeshniCardCustomer.AttachmentSql", request, ex, null);
                    Response.StatusCode = 500;
                    return Json(Fail("تم حفظ بيانات العميل لكن تعذر تسجيل بيانات المرفقات في قاعدة البيانات.", ex.ToString()));
                }
                catch (Exception ex)
                {
                    LogKycFailure("SaveKeshniCardCustomer.AttachmentException", request, ex, null);
                    Response.StatusCode = 500;
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
                Response.StatusCode = 500;
                return Json(Fail(FriendlySqlKycMessage(ex), ex.ToString()));
            }
            catch (Exception ex)
            {
                LogKycFailure("SaveKeshniCardCustomer.Exception", request, ex, null);
                Response.StatusCode = 500;
                return Json(Fail("حدث خطأ أثناء حفظ بيانات الكارت", ex.ToString()));
            }
        }

        [HttpPost]
        public JsonResult Save(PosSaveTransactionRequest request)
        {
            try
            {
                request = request ?? new PosSaveTransactionRequest();

                var context = GetPosContext();
                if (context == null)
                {
                    Response.StatusCode = 401;
                    return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً", "POS session context is missing."));
                }

                if (!context.CanSave)
                {
                    Response.StatusCode = 403;
                    return Json(Fail("ليس لديك صلاحية الحفظ", "ScreenJuncUser does not allow CanAdd/FullAccess for FrmSaleBill6."));
                }

                ApplyCashOutSecondaryDefault(request);

                var validationErrors = ValidateSaveRequest(request, context);
                AddServiceTypeValidationErrors(request, validationErrors);
                if (validationErrors.Count > 0)
                {
                    Response.StatusCode = 400;
                    return Json(Fail("راجع بيانات العملية قبل الحفظ", "Client request failed POS server validation.", validationErrors));
                }

                request.UserID = context.UserId;
                request.Emp_ID = context.EmpId;

                if (context.CanChangeDefaults)
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

                var result = _repository.SaveTransaction(request);
                return Json(new
                {
                    success = true,
                    transactionId = result.Transaction_ID,
                    noteSerial1 = result.NoteSerial1
                });
            }
            catch (SqlException ex)
            {
                Response.StatusCode = 500;
                return Json(Fail(FriendlySqlSaveMessage(ex), ex.Message));
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(Fail("حدث خطأ أثناء الحفظ التجريبي", ex.Message));
            }
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

            if (string.IsNullOrWhiteSpace(request.IPN))
            {
                errors["IPN"] = "ID مطلوب";
            }

            if (string.IsNullOrWhiteSpace(request.ManualNO))
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

            if (context.EmpId.GetValueOrDefault() <= 0)
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

            if (!context.BoxId.HasValue)
            {
                errors["BoxID"] = "الخزنة غير محددة";
            }

            if (!context.BranchId.HasValue)
            {
                errors["BranchId"] = "الفرع غير محدد";
            }

            if (!context.StoreId.HasValue)
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
                else if (!Regex.IsMatch(cardNo, @"^[0-9]+$"))
                {
                    errors["CardNo"] = "رقم التوكن/الكارت يجب أن يحتوي على أرقام فقط";
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

        private int? AddKeshniCardDuplicateErrors(PosCashCustomerSaveRequest request, IDictionary<string, string> errors)
        {
            int? duplicateCustomerId = null;
            var cardLength = string.IsNullOrWhiteSpace(request.CardNo) ? 0 : request.CardNo.Trim().Length;

            // VB6 FrmCustCash blocks duplicate Tet_NumPoket for optEasyCash(0)
            // only when the duplicate belongs to another Id and the card/token is not the 8-character type.
            if (cardLength != 8)
            {
                duplicateCustomerId = _repository.FindKeshniCardDuplicateId("Tet_NumPoket", request.Tet_NumPoket, request.CustomerID);
                if (duplicateCustomerId.HasValue)
                {
                    errors["Tet_NumPoketDuplicate"] = "الرقم القومي مسجل من قبل. ابحث عن العميل المسجل واستخدمه بدلاً من إنشاء سجل جديد";
                }
            }

            if (_repository.KeshniCardDuplicateExists("PhoneNo2", request.PhoneNo2, request.CustomerID))
            {
                errors["PhoneNo2Duplicate"] = "رقم التليفون مسجل من قبل";
            }

            if (_repository.KeshniCardDuplicateExists("CardNo", request.CardNo, request.CustomerID))
            {
                errors["CardNoDuplicate"] = "رقم الكارت مسجل من قبل";
            }

            if (!string.IsNullOrWhiteSpace(request.CardNo) && !_repository.KeshniCardSerialExistsInIssuedCards(request.CardNo))
            {
                errors["CardNoNotIssued"] = "رقم الكارت غير موجود أو تم إدخاله بشكل خاطئ";
            }

            return duplicateCustomerId;
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

        private static object Fail(string message, string technicalMessage, IDictionary<string, string> validationErrors = null, int? duplicateCustomerId = null)
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
                validationErrorsList = errors.Values.ToArray(),
                duplicateCustomerId = duplicateCustomerId
            };
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

        private static string FriendlySqlSaveMessage(SqlException ex)
        {
            var message = ex == null ? string.Empty : ex.Message;
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

            return "حدث خطأ من قاعدة البيانات أثناء الحفظ التجريبي";
        }

        private PosUserContext GetPosContext()
        {
            return Session[PosLoginController.PosContextSessionKey] as PosUserContext;
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
