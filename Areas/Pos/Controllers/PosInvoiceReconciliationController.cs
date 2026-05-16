using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using MyERP.Areas.Pos.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class PosInvoiceReconciliationController : Controller
    {
        private const string SessionKey = "POS_EXCEL_INVOICE_RECONCILIATION_RESULT";
        private const string SavedSessionKey = "POS_SAVED_EXCEL_INVOICE_RECONCILIATION_RESULT";
        private readonly PosSqlRepository _repository;
        private readonly PosExcelInvoiceReconciliationService _service;

        public PosInvoiceReconciliationController()
        {
            _repository = new PosSqlRepository();
            _service = new PosExcelInvoiceReconciliationService();
        }

        [HttpGet]
        public ActionResult Index()
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!CanOpen(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية مراجعة وتسوية فواتير Excel");
            }

            ViewBag.PosContext = context;
            ViewBag.ActiveScreen = "invoice-reconciliation";
            return View("~/Areas/Pos/Views/InvoiceReconciliation/Index.cshtml", BuildModel(context, null));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Analyze(IEnumerable<HttpPostedFileBase> excelFiles, DateTime? fromDate, DateTime? toDate, int? branchId, string serviceType)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!CanOpen(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية مراجعة وتسوية فواتير Excel");
            }

            ViewBag.PosContext = context;
            ViewBag.ActiveScreen = "invoice-reconciliation";

            var files = (excelFiles ?? new HttpPostedFileBase[0]).Where(x => x != null && x.ContentLength > 0).ToList();
            if (files.Count == 0)
            {
                var empty = BuildModel(context, null);
                empty.ErrorMessage = "اختر ملف Excel واحد على الأقل قبل التحليل.";
                return View("~/Areas/Pos/Views/InvoiceReconciliation/Index.cshtml", empty);
            }

            var resolvedFrom = (fromDate ?? DateTime.Today).Date;
            var resolvedTo = (toDate ?? resolvedFrom).Date;
            var resolvedBranchId = IsAdmin(context) ? branchId : context.BranchId;

            try
            {
                var combined = new PosInvoiceReconciliationResult
                {
                    SourceFileName = string.Join(", ", files.Select(x => Path.GetFileName(x.FileName))),
                    FromDate = resolvedFrom,
                    ToDate = resolvedTo,
                    BranchId = resolvedBranchId,
                    ServiceType = serviceType
                };

                foreach (var file in files)
                {
                    var request = new PosInvoiceReconciliationRequest
                    {
                        FromDate = resolvedFrom,
                        ToDate = resolvedTo,
                        BranchId = resolvedBranchId,
                        ServiceType = serviceType,
                        UserId = context.UserId,
                        CanSeeAllBranches = IsAdmin(context)
                    };

                    var result = _service.Analyze(file.InputStream, file.FileName, request);
                    foreach (var map in result.ColumnMappings.Where(x => !combined.ColumnMappings.Any(m => string.Equals(m.FieldKey, x.FieldKey, StringComparison.OrdinalIgnoreCase))))
                    {
                        combined.ColumnMappings.Add(map);
                    }

                    foreach (var row in result.InvalidRows)
                    {
                        combined.InvalidRows.Add(row);
                    }

                    foreach (var row in result.Rows)
                    {
                        combined.Rows.Add(row);
                    }
                }

                MarkCombinedExcelDuplicates(combined);
                RecalculateCombinedSummary(combined);
                Session[SessionKey] = combined;
                return View("~/Areas/Pos/Views/InvoiceReconciliation/Index.cshtml", BuildModel(context, combined));
            }
            catch (Exception ex)
            {
                Trace.TraceError("PosInvoiceReconciliation.Analyze failed: " + ex);
                var model = BuildModel(context, null);
                model.ErrorMessage = "تعذر تحليل ملف Excel: " + ex.Message;
                return View("~/Areas/Pos/Views/InvoiceReconciliation/Index.cshtml", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AnalyzeSaved(DateTime? fromDate, DateTime? toDate, string month, int? branchId, string serviceType, int? tellerUserId, string importSource, decimal? minAmount, decimal? maxAmount, string token, string phone, string nationalId, string customerName, string riskLevel, string searchTerm, bool? onlyBothSources, bool? suspiciousOnly)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!CanOpen(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية مراجعة وتسوية فواتير Excel");
            }

            ViewBag.PosContext = context;
            ViewBag.ActiveScreen = "invoice-reconciliation";

            DateTime monthDate = DateTime.MinValue;
            var hasMonth = !string.IsNullOrWhiteSpace(month)
                && DateTime.TryParseExact(month + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out monthDate);
            var resolvedFrom = hasMonth ? new DateTime(monthDate.Year, monthDate.Month, 1) : (fromDate ?? DateTime.Today).Date;
            var resolvedTo = hasMonth ? resolvedFrom.AddMonths(1).AddDays(-1) : (toDate ?? resolvedFrom).Date;
            var resolvedBranchId = IsAdmin(context) ? branchId : context.BranchId;

            try
            {
                var request = new PosInvoiceReconciliationRequest
                {
                    FromDate = resolvedFrom,
                    ToDate = resolvedTo,
                    BranchId = resolvedBranchId,
                    ServiceType = serviceType,
                    TellerUserId = tellerUserId,
                    ImportSource = string.IsNullOrWhiteSpace(importSource) ? "Both" : importSource,
                    Month = hasMonth ? month : string.Empty,
                    MinAmount = minAmount,
                    MaxAmount = maxAmount,
                    Token = token,
                    Phone = phone,
                    NationalId = nationalId,
                    CustomerName = customerName,
                    RiskLevel = riskLevel,
                    SearchTerm = searchTerm,
                    OnlyBothSources = onlyBothSources.GetValueOrDefault(),
                    SuspiciousOnly = suspiciousOnly.GetValueOrDefault(),
                    UserId = context.UserId,
                    CanSeeAllBranches = IsAdmin(context)
                };
                var result = _service.AnalyzeSavedInvoices(request);
                SavedSessionKeySet(result);
                return View("~/Areas/Pos/Views/InvoiceReconciliation/Index.cshtml", BuildModel(context, null, result));
            }
            catch (Exception ex)
            {
                Trace.TraceError("PosInvoiceReconciliation.AnalyzeSaved failed: " + ex);
                var model = BuildModel(context, null, null);
                model.ErrorMessage = "تعذر تحليل الفواتير المحفوظة داخل قاعدة البيانات: " + ex.Message;
                model.FromDate = resolvedFrom;
                model.ToDate = resolvedTo;
                model.BranchId = resolvedBranchId;
                model.ServiceType = serviceType;
                model.UserId = tellerUserId;
                model.ImportSource = string.IsNullOrWhiteSpace(importSource) ? "Both" : importSource;
                model.Month = hasMonth ? month : string.Empty;
                model.MinAmount = minAmount;
                model.MaxAmount = maxAmount;
                model.Token = token;
                model.Phone = phone;
                model.NationalId = nationalId;
                model.CustomerName = customerName;
                model.RiskLevel = riskLevel;
                model.SearchTerm = searchTerm;
                model.OnlyBothSources = onlyBothSources.GetValueOrDefault();
                model.SuspiciousOnly = suspiciousOnly.GetValueOrDefault();
                return View("~/Areas/Pos/Views/InvoiceReconciliation/Index.cshtml", model);
            }
        }

        [HttpGet]
        public JsonResult GetReconciliationDayDetails(DateTime date, int? branchId)
        {
            var guard = ApiGuard();
            if (guard != null) return guard;
            var result = Session[SavedSessionKey] as PosSavedInvoiceReconciliationResult;
            if (result == null) return Json(new { success = false, message = "لا توجد نتيجة تحليل محفوظة." }, JsonRequestBehavior.AllowGet);

            var day = date.Date;
            var excel = result.ExcelInvoices.Where(x => x.InvoiceDate.HasValue && x.InvoiceDate.Value.Date == day && (!branchId.HasValue || x.BranchId == branchId)).ToList();
            var system = result.SystemInvoices.Where(x => x.InvoiceDate.HasValue && x.InvoiceDate.Value.Date == day && (!branchId.HasValue || x.BranchId == branchId)).ToList();
            var pairs = result.DuplicatePairs.Where(x => x.ExcelInvoiceDate.HasValue && x.ExcelInvoiceDate.Value.Date == day && (!branchId.HasValue || excel.Any(e => e.TransactionId == x.ExcelTransactionId))).ToList();
            var summary = new
            {
                excelCount = excel.Count,
                excelTotal = excel.Sum(x => x.Amount),
                systemCount = system.Count,
                systemTotal = system.Sum(x => x.Amount),
                difference = excel.Sum(x => x.Amount) - system.Sum(x => x.Amount),
                suspiciousCount = pairs.Count,
                riskReason = pairs.Any() ? string.Join(" | ", pairs.SelectMany(x => x.Reasons).Distinct().Take(5)) : "لا توجد تشابهات قوية"
            };
            return Json(new { success = true, summary = summary, excel = excel, system = system, pairs = pairs }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetSuspiciousPairs()
        {
            var guard = ApiGuard();
            if (guard != null) return guard;
            var result = Session[SavedSessionKey] as PosSavedInvoiceReconciliationResult;
            return Json(new { success = result != null, pairs = result == null ? new List<PosSavedInvoiceDuplicatePair>() : result.DuplicatePairs }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetInvoiceReconciliationDetails(int transactionId)
        {
            var guard = ApiGuard();
            if (guard != null) return guard;
            var result = Session[SavedSessionKey] as PosSavedInvoiceReconciliationResult;
            if (result == null) return Json(new { success = false, message = "لا توجد نتيجة تحليل محفوظة." }, JsonRequestBehavior.AllowGet);
            var invoice = result.ExcelInvoices.Concat(result.SystemInvoices).FirstOrDefault(x => x.TransactionId == transactionId);
            if (invoice == null) return Json(new { success = false, message = "لم يتم العثور على الفاتورة داخل نتيجة التحليل." }, JsonRequestBehavior.AllowGet);
            var pairs = result.DuplicatePairs.Where(x => x.ExcelTransactionId == transactionId || x.NormalTransactionId == transactionId).ToList();
            return Json(new { success = true, invoice = invoice, similarPairs = pairs }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeleteOrCancelReconciliationInvoices(PosReconciliationDeleteRequest request)
        {
            var context = GetPosContext();
            if (context == null) return Json(new { success = false, message = "يجب تسجيل الدخول أولا." });
            if (!CanDeleteReconciliationInvoices(context)) return Json(new { success = false, message = "ليست لديك صلاحية حذف فواتير شاشة المطابقة." });
            if (request == null || request.TransactionIds == null || request.TransactionIds.Count == 0) return Json(new { success = false, message = "اختر فاتورة واحدة على الأقل." });
            if (string.IsNullOrWhiteSpace(request.Reason)) return Json(new { success = false, message = "سبب الحذف مطلوب." });
            if (string.IsNullOrWhiteSpace(request.Password) || !_repository.ValidatePosUserPassword(context.UserId, request.Password)) return Json(new { success = false, message = "كلمة المرور غير صحيحة." });

            var result = Session[SavedSessionKey] as PosSavedInvoiceReconciliationResult;
            var messages = new List<string>();
            foreach (var id in request.TransactionIds.Distinct())
            {
                try
                {
                    var deleteResult = _repository.DeletePosSaleInvoice(id, context.UserId);
                    messages.Add("فاتورة " + id.ToString(CultureInfo.InvariantCulture) + ": " + string.Join(" ", deleteResult.Messages));
                    if (result != null)
                    {
                        result.ActionLog.Add(new PosReconciliationActionLogItem
                        {
                            ActionDate = DateTime.Now,
                            UserId = context.UserId,
                            UserName = context.UserName,
                            ActionType = "حذف/إلغاء آمن",
                            TransactionId = id,
                            Source = FindInvoiceSource(result, id),
                            Reason = request.Reason,
                            PasswordVerified = true,
                            ResultMessage = string.Join(" ", deleteResult.Messages)
                        });
                    }
                    PosSystemErrorLogger.Log(_repository, Request, context, "PosInvoiceReconciliation", "DeleteOrCancel", null, id, "تم حذف/إلغاء فاتورة من شاشة مطابقة Excel. السبب: " + request.Reason, null, "DeleteOrCancelReconciliationInvoices", "Info", "Success");
                }
                catch (Exception ex)
                {
                    messages.Add("فاتورة " + id.ToString(CultureInfo.InvariantCulture) + ": " + ex.Message);
                    PosSystemErrorLogger.Log(_repository, Request, context, "PosInvoiceReconciliation", "DeleteOrCancel", null, id, ex.Message, ex, "DeleteOrCancelReconciliationInvoices", "Error", "Exception");
                }
            }

            if (result != null) SavedSessionKeySet(result);
            return Json(new { success = true, message = string.Join(" ", messages), actionLog = result == null ? new List<PosReconciliationActionLogItem>() : result.ActionLog });
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Export()
        {
            var context = GetPosContext();
            if (context == null)
            {
                return new HttpStatusCodeResult(401, "يجب تسجيل دخول نقطة البيع أولاً");
            }

            if (!CanOpen(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية مراجعة وتسوية فواتير Excel");
            }

            var result = Session[SessionKey] as PosInvoiceReconciliationResult;
            if (result == null)
            {
                return new HttpStatusCodeResult(400, "لا توجد نتيجة تحليل محفوظة للتصدير");
            }

            var bytes = _service.BuildExcelReport(result);
            var fileName = string.Format(
                CultureInfo.InvariantCulture,
                "Pos_Excel_Reconciliation_{0}_{1}.xlsx",
                result.FromDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
                result.ToDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult ExportSaved()
        {
            var context = GetPosContext();
            if (context == null)
            {
                return new HttpStatusCodeResult(401, "يجب تسجيل دخول نقطة البيع أولاً");
            }

            if (!CanOpen(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية مراجعة وتسوية فواتير Excel");
            }

            var result = Session[SavedSessionKey] as PosSavedInvoiceReconciliationResult;
            if (result == null)
            {
                return new HttpStatusCodeResult(400, "لا توجد نتيجة تحليل محفوظة للتصدير");
            }

            var bytes = _service.BuildSavedAnalysisExcelReport(result);
            var fileName = string.Format(
                CultureInfo.InvariantCulture,
                "Pos_Saved_Excel_Reconciliation_{0}_{1}.xlsx",
                result.FromDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
                result.ToDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private PosInvoiceReconciliationIndexViewModel BuildModel(PosUserContext context, PosInvoiceReconciliationResult result, PosSavedInvoiceReconciliationResult savedResult = null)
        {
            var model = new PosInvoiceReconciliationIndexViewModel
            {
                FromDate = savedResult != null ? savedResult.FromDate : (result != null ? result.FromDate : DateTime.Today),
                ToDate = savedResult != null ? savedResult.ToDate : (result != null ? result.ToDate : DateTime.Today),
                BranchId = savedResult != null ? savedResult.BranchId : (result != null ? result.BranchId : (IsAdmin(context) ? null : context.BranchId)),
                ServiceType = savedResult != null ? savedResult.ServiceType : (result != null ? result.ServiceType : string.Empty),
                UserId = savedResult != null ? savedResult.TellerUserId : null,
                ImportSource = savedResult != null ? savedResult.ImportSource : "Both",
                Month = savedResult != null ? savedResult.Month : string.Empty,
                MinAmount = savedResult != null ? savedResult.MinAmount : null,
                MaxAmount = savedResult != null ? savedResult.MaxAmount : null,
                Token = savedResult != null ? savedResult.Token : string.Empty,
                Phone = savedResult != null ? savedResult.Phone : string.Empty,
                NationalId = savedResult != null ? savedResult.NationalId : string.Empty,
                CustomerName = savedResult != null ? savedResult.CustomerName : string.Empty,
                RiskLevel = savedResult != null ? savedResult.RiskLevel : string.Empty,
                SearchTerm = savedResult != null ? savedResult.SearchTerm : string.Empty,
                OnlyBothSources = savedResult != null && savedResult.OnlyBothSources,
                SuspiciousOnly = savedResult != null && savedResult.SuspiciousOnly,
                CanDeleteInvoices = CanDeleteReconciliationInvoices(context),
                Result = result,
                SavedResult = savedResult
            };

            model.Branches = IsAdmin(context)
                ? _repository.GetBranches()
                : new List<PosBranchDto> { new PosBranchDto { BranchId = context.BranchId.GetValueOrDefault(), BranchName = context.BranchName } };
            model.Users = _repository.GetPosReportUsers();
            return model;
        }

        private void SavedSessionKeySet(PosSavedInvoiceReconciliationResult result)
        {
            Session[SavedSessionKey] = result;
        }

        private static void RecalculateCombinedSummary(PosInvoiceReconciliationResult result)
        {
            result.Summary.TotalExcelRows = result.Rows.Count + result.InvalidRows.Count;
            result.Summary.ValidRows = result.Rows.Count;
            result.Summary.InvalidRows = result.InvalidRows.Count;
            result.Summary.ExactMatches = Count(result, "Exact Match");
            result.Summary.ProbableDuplicates = Count(result, "Probable Duplicate");
            result.Summary.PossibleMatches = Count(result, "Possible Match");
            result.Summary.NotFound = Count(result, "Not Found");
            result.Summary.AmountMismatches = Count(result, "Amount Mismatch");
            result.Summary.CustomerMismatches = Count(result, "Customer Mismatch");
            result.Summary.DateMismatches = Count(result, "Date Mismatch");
            result.Summary.DatabaseDuplicates = Count(result, "Database Duplicate");
            result.Summary.ExcelDuplicates = Count(result, "Excel Duplicate");
        }

        private static int Count(PosInvoiceReconciliationResult result, string status)
        {
            return result.Rows.Count(x => string.Equals(x.Status, status, StringComparison.OrdinalIgnoreCase));
        }

        private static void MarkCombinedExcelDuplicates(PosInvoiceReconciliationResult result)
        {
            if (result == null || result.Rows.Count < 2)
            {
                return;
            }

            var duplicateKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in result.Rows.SelectMany(ExcelDuplicateKeys).GroupBy(x => x).Where(x => x.Count() > 1))
            {
                duplicateKeys.Add(group.Key);
            }

            foreach (var row in result.Rows)
            {
                if (!ExcelDuplicateKeys(row).Any(duplicateKeys.Contains))
                {
                    continue;
                }

                if (!row.Warnings.Any(x => x.IndexOf("Excel duplicate", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    row.Warnings.Add("Excel duplicate: repeated row across uploaded workbook(s)");
                }

                row.Status = "Excel Duplicate";
            }
        }

        private static IEnumerable<string> ExcelDuplicateKeys(PosInvoiceReconciliationRow row)
        {
            if (!string.IsNullOrWhiteSpace(row.Token))
            {
                yield return "T:" + row.Token;
            }

            if (!string.IsNullOrWhiteSpace(row.Phone) && row.ExcelAmount.HasValue && row.ExcelInvoiceDate.HasValue)
            {
                yield return "P:" + row.Phone + ":" + row.ExcelAmount.Value.ToString("0.00", CultureInfo.InvariantCulture) + ":" + row.ExcelInvoiceDate.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrWhiteSpace(row.ExcelInvoiceNumber))
            {
                yield return "I:" + row.ExcelInvoiceNumber.Trim().ToUpperInvariant();
            }
        }

        private static bool CanOpen(PosUserContext context)
        {
            return IsAdmin(context) || (context != null && context.CanImportExcel);
        }

        private JsonResult ApiGuard()
        {
            var context = GetPosContext();
            if (context == null)
            {
                return Json(new { success = false, message = "يجب تسجيل الدخول أولا." }, JsonRequestBehavior.AllowGet);
            }

            if (!CanOpen(context))
            {
                return Json(new { success = false, message = "ليست لديك صلاحية فتح شاشة المطابقة." }, JsonRequestBehavior.AllowGet);
            }

            return null;
        }

        private bool CanDeleteReconciliationInvoices(PosUserContext context)
        {
            return IsAdmin(context)
                && context.CanCancelInvoice
                && _repository.HasPosPermission(context.UserId, "CanDeleteExcelReconciliationInvoices");
        }

        private static string FindInvoiceSource(PosSavedInvoiceReconciliationResult result, int transactionId)
        {
            var invoice = result.ExcelInvoices.Concat(result.SystemInvoices).FirstOrDefault(x => x.TransactionId == transactionId);
            return invoice == null ? string.Empty : invoice.Source;
        }

        private static bool IsAdmin(PosUserContext context)
        {
            return context != null && (context.UserType.GetValueOrDefault(-1) == 0 || context.IsFullAccess);
        }

        private PosUserContext GetPosContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _repository);
        }
    }
}
