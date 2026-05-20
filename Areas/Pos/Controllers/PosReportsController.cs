using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using MyERP.Areas.Pos.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class PosReportsController : Controller
    {
        private readonly PosSqlRepository _repository;

        public PosReportsController()
        {
            _repository = new PosSqlRepository();
        }

        public ActionResult Index()
        {
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.Charset = "utf-8";

            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!CanOpenReports(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية عرض التقارير");
            }

            ViewBag.PosContext = context;
            ViewBag.ReportDefinitions = BuildReports(context);
            ViewBag.Branches = GetInitialBranches(context);
            ViewBag.Stores = new PosStoreDto[0];
            ViewBag.Users = new PosPermissionUserDto[0];
            return View();
        }

        [HttpGet]
        public JsonResult Lookups(string type, int? branchId)
        {
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.Charset = "utf-8";
            Response.TrySkipIisCustomErrors = true;

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var context = GetPosContext();
                if (context == null)
                {
                    Response.StatusCode = 401;
                    return Json(Fail("يجب تسجيل دخول نقطة البيع أولًا", "POS session context is missing."), JsonRequestBehavior.AllowGet);
                }

                if (!CanOpenReports(context))
                {
                    Response.StatusCode = 403;
                    return Json(Fail("ليست لديك صلاحية عرض التقارير", "Report lookup permission denied."), JsonRequestBehavior.AllowGet);
                }

                var lookupType = (type ?? string.Empty).Trim().ToLowerInvariant();
                object rows;
                if (lookupType == "branches")
                {
                    rows = GetAllowedBranches(context).Select(x => new { id = x.BranchId, name = x.BranchName });
                }
                else if (lookupType == "stores")
                {
                    var resolvedBranchId = IsAdmin(context) ? branchId : context.BranchId;
                    rows = _repository.GetStoresByBranch(resolvedBranchId).Select(x => new { id = x.StoreID, name = x.StoreName, branchId = x.BranchId });
                }
                else if (lookupType == "users")
                {
                    rows = _repository.GetPosReportUsers().Select(x => new { id = x.UserId, name = string.IsNullOrWhiteSpace(x.EmpName) ? x.UserName : x.UserName + " - " + x.EmpName });
                }
                else
                {
                    Response.StatusCode = 400;
                    return Json(Fail("نوع القائمة غير صحيح", "Invalid lookup type."), JsonRequestBehavior.AllowGet);
                }

                stopwatch.Stop();
                PosPerformanceLogger.LogQuery("PosReports.Lookups", lookupType, stopwatch.ElapsedMilliseconds, null, context);
                return Json(new { success = true, rows = rows }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Trace.TraceError("PosReports.Lookups failed: " + ex);
                Response.StatusCode = 500;
                return Json(Fail("تعذر تحميل بيانات الفلاتر", ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult Run(PosReportRunRequest request)
        {
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.Charset = "utf-8";
            Response.TrySkipIisCustomErrors = true;

            try
            {
                var context = GetPosContext();
                var validation = ValidateReportRequest(context, request);
                if (validation != null && validation.StatusCode != 0)
                {
                    Response.StatusCode = validation.StatusCode;
                    return LargeJson(Fail(validation.Message, validation.TechnicalMessage));
                }

                var report = validation.Report;
                if (!report.Enabled)
                {
                    return LargeJson(new { success = false, message = "لم يتم تحديد مصدر التقرير بعد" });
                }

                var stopwatch = Stopwatch.StartNew();
                var table = LoadReportTable(report, request, context, validation.BranchId);
                stopwatch.Stop();
                PosPerformanceLogger.LogQuery("PosReports.Run", report.Key, stopwatch.ElapsedMilliseconds, table != null ? table.Rows.Count : 0, context);
                LogImportantReportIfSlow("Run", report.Key, request, context, validation.BranchId, stopwatch.ElapsedMilliseconds, null);
                return LargeJson(new
                {
                    success = true,
                    reportKey = report.Key,
                    reportName = report.Title,
                    columns = ToColumns(table),
                    rows = ToRows(table)
                });
            }
            catch (Exception ex)
            {
                var context = GetPosContext();
                var safeRequest = request ?? new PosReportRunRequest();
                LogImportantReportIfSlow("Run", safeRequest.ReportKey, safeRequest, context, ResolveBranchIdSafe(context, safeRequest.BranchId), 0, ex);
                Trace.TraceError("PosReports.Run failed: " + ex);
                Response.StatusCode = 500;
                return LargeJson(Fail("تعذر تشغيل التقرير. برجاء المحاولة مرة أخرى أو التواصل مع الدعم", ex.Message));
            }
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Export(PosReportRunRequest request)
        {
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.Charset = "utf-8";
            request = request ?? new PosReportRunRequest();

            try
            {
                var context = GetPosContext();
                var validation = ValidateReportRequest(context, request);
                if (validation != null && validation.StatusCode != 0)
                {
                    Response.StatusCode = validation.StatusCode;
                    return Json(Fail(validation.Message, validation.TechnicalMessage), JsonRequestBehavior.AllowGet);
                }

                var report = validation.Report;
                if (!report.Enabled)
                {
                    return Json(Fail("لم يتم تحديد مصدر التقرير بعد", "Report source is not enabled."), JsonRequestBehavior.AllowGet);
                }

                var from = request.FromDate.GetValueOrDefault(DateTime.Today);
                var to = request.ToDate.GetValueOrDefault(DateTime.Today);
                var stopwatch = Stopwatch.StartNew();
                var table = LoadReportTable(report, request, context, validation.BranchId);
                stopwatch.Stop();
                PosPerformanceLogger.LogQuery("PosReports.Export", report.Key, stopwatch.ElapsedMilliseconds, table != null ? table.Rows.Count : 0, context);
                LogImportantReportIfSlow("Export", report.Key, request, context, validation.BranchId, stopwatch.ElapsedMilliseconds, null);
                var bytes = BuildExcel(report.Title, from, to, validation.BranchId, table);
                var fileName = string.Format("{0}_{1}_{2}.xlsx", SafeFileName(report.Title), from.ToString("yyyyMMdd"), to.ToString("yyyyMMdd"));
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                var context = GetPosContext();
                LogImportantReportIfSlow("Export", request.ReportKey, request, context, ResolveBranchIdSafe(context, request.BranchId), 0, ex);
                Trace.TraceError("PosReports.Export failed: " + ex);
                Response.StatusCode = 500;
                return Json(Fail("تعذر تصدير Excel. برجاء المحاولة مرة أخرى أو التواصل مع الدعم", ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        internal static List<PosReportDefinition> BuildReports(PosUserContext context)
        {
            var admin = IsAdmin(context);
            return new List<PosReportDefinition>
            {
                new PosReportDefinition("daily-trans", "تقرير يومي بالحركات", "sales", admin || context.CanReportDailyTransactions, true, "تقرير تشغيلي"),
                new PosReportDefinition("daily-trans-2", "تقرير يومي بالحركات 2", "sales", admin || context.CanReportDailyTransactions2, true, "تقرير تشغيلي تفصيلي"),
                new PosReportDefinition("sales-complete", "تقرير المبيعات الشامل 1", "sales", admin || context.CanReportSalesComplete, true, "تقرير مبيعات شامل"),
                new PosReportDefinition("sales-complete-2", "تقرير المبيعات الشامل 2", "sales", admin || context.CanReportSalesComplete2, true, "تقرير مبيعات شامل"),
                new PosReportDefinition("sales-governorates", "تقرير المبيعات الشامل بالمحافظات", "sales", admin || context.CanReportSalesCompleteGovernorates, true, "تقرير مبيعات حسب المحافظات"),
                new PosReportDefinition("sales-departments", "تقرير المبيعات الشامل بالإدارات", "sales", admin || context.CanReportSalesCompleteDepartments, true, "تقرير مبيعات حسب الإدارات"),
                new PosReportDefinition("sales-sectors", "تقرير المبيعات الشامل بالقطاعات", "sales", admin || context.CanReportDailyTransactionsSectors, true, "تقرير مبيعات حسب القطاعات"),
                new PosReportDefinition("sales-analytical", "تقرير المبيعات تحليلي", "sales", admin || context.CanReportSalesCompleteAnalytical, true, "تقرير تحليلي"),
                new PosReportDefinition("items-sales-details", "تقرير مبيعات الأصناف تفصيلي", "sales", admin || context.CanReportSalesComplete, true, "تقرير تفصيلي حسب الأصناف"),
                new PosReportDefinition("general-sales", "تقرير المبيعات العام", "closings", admin || context.CanReportAllSales, true, "Command8 / CloseReprotTotal.rpt"),
                new PosReportDefinition("revenues", "تقرير الإيرادات", "closings", admin || context.CanViewReports, true, "Command10 / CloseReprotTotal2FastMini2.rpt"),
                new PosReportDefinition("web-invoices", "تقرير فواتير الويب", "sales", admin || context.CanViewReports, true, "عدد فواتير الويب حسب المستخدم"),
                new PosReportDefinition("non-web-login-users", "مستخدمين دخلوا من غير الويب", "sales", admin || context.CanViewReports, true, "Non-Web Login Users"),
                new PosReportDefinition("salesmen", "تقرير المناديب", "closings", admin || context.CanReportSalesmen, false, "لم يتم تحديد مصدر التقرير بعد"),
                new PosReportDefinition("finance-closing", "تقرير الإغلاق المالي", "closings", admin || context.CanReportClosings, true, "Command9 / CloseReprotTotal2Fast.rpt"),
                new PosReportDefinition("finance-closing-discounts", "تقرير الإغلاق المالي الشامل على مستوى الفروع", "closings", admin || context.CanReportFinanceClosing, true, "الإغلاقات والخصومات مع متابعة الفروع المفتوحة live"),
                new PosReportDefinition("discounts", "تقرير الخصومات", "closings", admin || context.CanReportDiscounts, false, "لم يتم تحديد مصدر التقرير بعد"),
                new PosReportDefinition("indicators", "تقرير المؤشرات العامة", "closings", admin || context.CanReportIndicators, false, "لم يتم تحديد مصدر التقرير بعد"),
                new PosReportDefinition("store-serials", "تقرير سيريالات المخزن", "serials", admin || context.CanReportStoreSerials, true, "تقرير المخزون والسيريالات"),
                new PosReportDefinition("export-excel", "تصدير ملف الإكسيل", "excel", admin || context.CanViewReports, false, "استخدم زر تصدير Excel الخاص بكل تقرير"),
                new PosReportDefinition("import-excel", "استيراد ملف الإكسيل", "excel", admin || context.CanViewReports, false, "لم يتم تحديد صيغة الاستيراد")
            };
        }

        private ReportValidationResult ValidateReportRequest(PosUserContext context, PosReportRunRequest request)
        {
            if (context == null)
            {
                return new ReportValidationResult { StatusCode = 401, Message = "يجب تسجيل دخول نقطة البيع أولًا", TechnicalMessage = "POS session context is missing." };
            }

            if (!CanOpenReports(context))
            {
                return new ReportValidationResult { StatusCode = 403, Message = "ليست لديك صلاحية عرض التقارير", TechnicalMessage = "Report screen permission denied." };
            }

            request = request ?? new PosReportRunRequest();
            var report = FindReport(request.ReportKey, context);
            if (report == null || !report.Allowed)
            {
                return new ReportValidationResult { StatusCode = 403, Message = "ليست لديك صلاحية تشغيل هذا التقرير", TechnicalMessage = "Report permission denied." };
            }

            if (!request.FromDate.HasValue || !request.ToDate.HasValue)
            {
                return new ReportValidationResult { StatusCode = 400, Message = "من تاريخ وإلى تاريخ مطلوبان قبل تشغيل التقرير", TechnicalMessage = "Required date filters are missing." };
            }

            if (request.ToDate.Value.Date < request.FromDate.Value.Date)
            {
                return new ReportValidationResult { StatusCode = 400, Message = "إلى تاريخ يجب أن يكون أكبر من أو يساوي من تاريخ", TechnicalMessage = "Invalid date range." };
            }

            if (string.Equals(report.Key, "store-serials", StringComparison.OrdinalIgnoreCase)
                && !request.StoreId.HasValue
                && (string.IsNullOrWhiteSpace(request.SerialSearch) || request.SerialSearch.Trim().Length < 3))
            {
                return new ReportValidationResult { StatusCode = 400, Message = "اختر المخزن أو اكتب 3 أحرف على الأقل قبل تشغيل تقرير السيريالات", TechnicalMessage = "Store serials report requires StoreId or at least 3 SerialSearch characters." };
            }

            var branchId = ResolveBranchId(context, request.BranchId);
            return new ReportValidationResult { Report = report, BranchId = branchId };
        }

        private DataTable LoadReportTable(PosReportDefinition report, PosReportRunRequest request, PosUserContext context, int branchId)
        {
            var from = (request.FromDate ?? DateTime.Today).Date;
            var to = (request.ToDate ?? DateTime.Today).Date;
            DataTable table;
            if (report.Key == "store-serials")
            {
                table = _repository.RunPosStoreSerialsReport(request.StoreId, request.SerialSearch, branchId, context.UserId, IsAdmin(context) || context.CanViewReports);
                return SortReportTable(table, request.SortBy);
            }

            if (report.Key == "web-invoices")
            {
                table = _repository.RunPosWebInvoiceAuditReport(from, to, branchId, context.UserId, IsAdmin(context) || context.CanViewReports);
                return SortReportTable(table, request.SortBy);
            }

            if (IsProjectStatusClosingParityReport(report.Key))
            {
                table = _repository.RunPosProjectStatusClosingReport(
                    report.Key,
                    from,
                    to,
                    branchId,
                    context.UserId,
                    IsAdmin(context) || context.CanViewReports,
                    request.BranchFromId,
                    request.BranchToId,
                    request.ShowEmptyBranches,
                    request.ServiceSearch,
                    request.ServiceType,
                    request.StoreId,
                    request.UserId);
                return SortReportTable(table, request.SortBy);
            }

            if (report.Key == "non-web-login-users")
            {
                table = _repository.RunPosNonWebLoginUsersReport(from, to, branchId, request.UserId, request.LoginSource, context.UserId, IsAdmin(context) || context.CanViewReports);
                return SortReportTable(table, request.SortBy);
            }

            if (IsOperationalSalesReport(report.Key) && !request.IncludeCardIssueCheck)
            {
                table = _repository.RunPosOperationalSalesReport(
                    report.Key,
                    from,
                    to,
                    branchId,
                    context.UserId,
                    IsAdmin(context) || context.CanViewReports,
                    request.ServiceType,
                    request.StoreId,
                    request.UserId);
                return SortReportTable(table, request.SortBy);
            }

            if (IsClosingReport(report.Key))
            {
                table = _repository.RunPosClosingReport(
                    report.Key,
                    from,
                    to,
                    branchId,
                    context.UserId,
                    IsAdmin(context) || context.CanViewReports,
                    request.BranchFromId,
                    request.BranchToId,
                    request.ShowEmptyBranches,
                    request.ServiceSearch,
                    request.UserId);
                RemoveEmptyDuplicateClosingRows(table);
                return SortReportTable(table, request.SortBy);
            }

            table = _repository.RunPosReport(
                report.Key,
                from,
                to,
                branchId,
                context.UserId,
                IsAdmin(context) || context.CanViewReports,
                request.BranchFromId,
                request.BranchToId,
                request.ShowEmptyBranches,
                request.ServiceSearch,
                request.ServiceType,
                request.StoreId,
                request.UserId,
                request.IncludeCardIssueCheck,
                request.CardIssueMode);
            return SortReportTable(table, request.SortBy);
        }

        private static DataTable SortReportTable(DataTable table, string sortBy)
        {
            if (table == null || table.Rows.Count == 0)
            {
                return table;
            }

            var sort = (sortBy ?? string.Empty).Trim().ToLowerInvariant();
            string expression = null;
            switch (sort)
            {
                case "branch-code":
                    expression = table.Columns.Contains("BranchID") ? "BranchID ASC" : null;
                    break;
                case "branch-code-desc":
                    expression = table.Columns.Contains("BranchID") ? "BranchID DESC" : null;
                    break;
                case "sales-high":
                    expression = table.Columns.Contains("TotalSupply") ? "TotalSupply DESC" : null;
                    break;
                case "sales-low":
                    expression = table.Columns.Contains("TotalSupply") ? "TotalSupply ASC" : null;
                    break;
                case "card-high":
                    expression = table.Columns.Contains("CardValue") ? "CardValue DESC" : null;
                    break;
                case "card-low":
                    expression = table.Columns.Contains("CardValue") ? "CardValue ASC" : null;
                    break;
            }

            if (string.IsNullOrWhiteSpace(expression))
            {
                return table;
            }

            table.DefaultView.Sort = expression;
            var sorted = table.DefaultView.ToTable();
            ResetRowNumbers(sorted);
            return sorted;
        }

        private static void RemoveEmptyDuplicateClosingRows(DataTable table)
        {
            if (table == null || table.Rows.Count == 0 || !table.Columns.Contains("BranchName"))
            {
                return;
            }

            var keysWithValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow row in table.Rows)
            {
                var key = GetClosingBranchKey(row);
                if (!string.IsNullOrWhiteSpace(key) && !IsEmptyClosingRow(row))
                {
                    keysWithValues.Add(key);
                }
            }

            if (keysWithValues.Count == 0)
            {
                return;
            }

            for (var i = table.Rows.Count - 1; i >= 0; i--)
            {
                var row = table.Rows[i];
                var key = GetClosingBranchKey(row);
                if (!string.IsNullOrWhiteSpace(key) && keysWithValues.Contains(key) && IsEmptyClosingRow(row))
                {
                    table.Rows.RemoveAt(i);
                }
            }

            ResetRowNumbers(table);
        }

        private static string GetClosingBranchKey(DataRow row)
        {
            var branchName = row["BranchName"] == DBNull.Value ? string.Empty : Convert.ToString(row["BranchName"], CultureInfo.InvariantCulture);
            var match = Regex.Match(branchName ?? string.Empty, @"\bEC\d+\b", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return "code:" + match.Value.ToUpperInvariant();
            }

            if (row.Table.Columns.Contains("BranchID") && row["BranchID"] != DBNull.Value)
            {
                var branchId = Convert.ToInt32(row["BranchID"], CultureInfo.InvariantCulture);
                if (branchId > 0)
                {
                    return "id:" + branchId.ToString(CultureInfo.InvariantCulture);
                }
            }

            return "name:" + (branchName ?? string.Empty).Trim();
        }

        private static bool IsEmptyClosingRow(DataRow row)
        {
            var columns = new[]
            {
                "TotalSupply", "CountCards", "TotalSaleDay2Vat", "CardValue", "CountTransaction",
                "WalletBalance", "WalletSupply", "BankBalanceCharge", "TotalRechargeValue",
                "TotalRev2", "TotalRevWithVat", "ReturnsCount", "TotalReturns", "NetCashOut",
                "BoxValue", "OpenBalance", "LastBalance", "TotalRev", "TotalVat", "CashOutTotal",
                "BoxBalance", "NoteValue"
            };

            foreach (var column in columns)
            {
                if (!row.Table.Columns.Contains(column) || row[column] == DBNull.Value)
                {
                    continue;
                }

                decimal value;
                if (decimal.TryParse(Convert.ToString(row[column], CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out value)
                    && value != 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static void ResetRowNumbers(DataTable table)
        {
            if (table == null || !table.Columns.Contains("RowNo"))
            {
                return;
            }

            for (var i = 0; i < table.Rows.Count; i++)
            {
                table.Rows[i]["RowNo"] = i + 1;
            }
        }

        private IEnumerable<PosBranchDto> GetAllowedBranches(PosUserContext context)
        {
            if (IsAdmin(context))
            {
                return _repository.GetBranches();
            }

            return context.BranchId.HasValue
                ? new[] { new PosBranchDto { BranchId = context.BranchId.Value, BranchName = context.BranchName } }
                : new PosBranchDto[0];
        }

        private IEnumerable<PosBranchDto> GetInitialBranches(PosUserContext context)
        {
            return context != null && !IsAdmin(context) && context.BranchId.HasValue
                ? new[] { new PosBranchDto { BranchId = context.BranchId.Value, BranchName = context.BranchName } }
                : new PosBranchDto[0];
        }

        private IEnumerable<PosStoreDto> GetAllowedStores(PosUserContext context)
        {
            return _repository.GetStoresByBranch(IsAdmin(context) ? (int?)null : context.BranchId);
        }

        private int ResolveBranchId(PosUserContext context, int? requestedBranchId)
        {
            if (IsAdmin(context))
            {
                return requestedBranchId.GetValueOrDefault(0);
            }

            return context.BranchId.GetValueOrDefault(0);
        }

        private int ResolveBranchIdSafe(PosUserContext context, int? requestedBranchId)
        {
            return context == null ? requestedBranchId.GetValueOrDefault(0) : ResolveBranchId(context, requestedBranchId);
        }

        private static bool IsClosingReport(string reportKey)
        {
            return string.Equals(reportKey, "finance-closing", StringComparison.OrdinalIgnoreCase)
                || string.Equals(reportKey, "finance-closing-discounts", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsProjectStatusClosingParityReport(string reportKey)
        {
            return string.Equals(reportKey, "general-sales", StringComparison.OrdinalIgnoreCase)
                || string.Equals(reportKey, "finance-closing", StringComparison.OrdinalIgnoreCase)
                || string.Equals(reportKey, "revenues", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsOperationalSalesReport(string reportKey)
        {
            return string.Equals(reportKey, "daily-trans", StringComparison.OrdinalIgnoreCase)
                || string.Equals(reportKey, "daily-trans-2", StringComparison.OrdinalIgnoreCase)
                || string.Equals(reportKey, "sales-complete", StringComparison.OrdinalIgnoreCase)
                || string.Equals(reportKey, "sales-complete-2", StringComparison.OrdinalIgnoreCase)
                || string.Equals(reportKey, "sales-governorates", StringComparison.OrdinalIgnoreCase)
                || string.Equals(reportKey, "sales-departments", StringComparison.OrdinalIgnoreCase)
                || string.Equals(reportKey, "sales-sectors", StringComparison.OrdinalIgnoreCase)
                || string.Equals(reportKey, "sales-analytical", StringComparison.OrdinalIgnoreCase)
                || string.Equals(reportKey, "items-sales-details", StringComparison.OrdinalIgnoreCase)
                || string.Equals(reportKey, "general-sales", StringComparison.OrdinalIgnoreCase)
                || string.Equals(reportKey, "revenues", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsImportantReport(string reportKey)
        {
            return IsClosingReport(reportKey)
                || IsOperationalSalesReport(reportKey)
                || string.Equals(reportKey, "web-invoices", StringComparison.OrdinalIgnoreCase)
                || string.Equals(reportKey, "non-web-login-users", StringComparison.OrdinalIgnoreCase)
                || string.Equals(reportKey, "store-serials", StringComparison.OrdinalIgnoreCase);
        }

        private static void LogImportantReportIfSlow(string action, string reportKey, PosReportRunRequest request, PosUserContext context, int branchId, long elapsedMilliseconds, Exception exception)
        {
            if (!IsImportantReport(reportKey) && exception == null)
            {
                return;
            }

            if (exception == null && elapsedMilliseconds < 300)
            {
                return;
            }

            request = request ?? new PosReportRunRequest();
            var from = (request.FromDate ?? DateTime.Today).Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var to = (request.ToDate ?? DateTime.Today).Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var message = string.Format(
                CultureInfo.InvariantCulture,
                "POS report {0}: Report={1}; UserId={2}; BranchId={3}; From={4}; To={5}; DurationMs={6}; Error={7}",
                action,
                reportKey,
                context != null ? context.UserId : 0,
                branchId,
                from,
                to,
                elapsedMilliseconds,
                exception != null ? exception.Message : string.Empty);

            if (exception == null)
            {
                Trace.TraceWarning(message);
            }
            else
            {
                Trace.TraceError(message);
            }
        }

        private static bool CanOpenReports(PosUserContext context)
        {
            return IsAdmin(context) || (context != null && context.CanViewReports);
        }

        private static bool IsAdmin(PosUserContext context)
        {
            return context != null && context.UserType.GetValueOrDefault(-1) == 0;
        }

        private static PosReportDefinition FindReport(string reportKey, PosUserContext context)
        {
            foreach (var report in BuildReports(context))
            {
                if (string.Equals(report.Key, reportKey, StringComparison.OrdinalIgnoreCase))
                {
                    return report;
                }
            }

            return null;
        }

        private static IEnumerable<PosReportColumnDto> ToColumns(DataTable table)
        {
            foreach (DataColumn column in table.Columns)
            {
                yield return new PosReportColumnDto { Key = column.ColumnName, Title = MapColumnTitle(column.ColumnName) };
            }
        }

         private static IEnumerable<Dictionary<string, object>> ToRows(DataTable table)
         {
             foreach (DataRow row in table.Rows)
             {
                 var item = new Dictionary<string, object>();
                 foreach (DataColumn column in table.Columns)
                 {
                     item[column.ColumnName] = FormatReportValue(row[column]);
                 }
                 yield return item;
             }
         }
 
         private static object FormatReportValue(object value)
         {
             if (value == null || value == DBNull.Value)
             {
                 return null;
             }
 
             var date = value as DateTime?;
             if (date.HasValue)
             {
                 return date.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
             }
 
             var text = value as string;
             if (!string.IsNullOrWhiteSpace(text))
             {
                 var match = Regex.Match(text.Trim(), @"^/Date\((-?\d+)\)/$");
                 if (match.Success)
                 {
                     long milliseconds;
                     if (long.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out milliseconds))
                     {
                        return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(milliseconds).ToLocalTime().ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                     }
                 }
             }
 
             return value;
         }
 
         private static string MapColumnTitle(string name)
         {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "BranchName", "بيان الفروع" },
                { "BranchCode", "كود الفرع" },
                 { "Transaction_ID", "رقم الحركة" },
                 { "NoteSerial1", "رقم الفاتورة" },
                 { "InvoiceNumber", "رقم الفاتورة" },
                 { "Transaction_Date", "التاريخ" },
                 { "InvoiceDate", "تاريخ الفاتورة" },
                 { "ReportType", "نوع العملية" },
                 { "ServiceType", "نوع الخدمة" },
                 { "CashCustomerName", "اسم العميل" },
                 { "CustomerName", "اسم العميل" },
                 { "CashCustomerPhone", "هاتف العميل" },
                 { "CustomerPhone", "هاتف العميل" },
                 { "CardNumber", "رقم الكارت / التوكن" },
                 { "CardIssueStatus", "حالة الكارت" },
                 { "CardIssueStatusAr", "حالة الكارت" },
                 { "CardStockBefore", "رصيد الكارت قبل العملية" },
                 { "CardStockAfter", "رصيد الكارت بعد العملية" },
                 { "ProblemReason", "سبب المشكلة" },
                 { "Branch", "الفرع" },
                 { "Store", "المخزن" },
                 { "Cashier", "المستخدم / الكاشير" },
                 { "TotalCards", "إجمالي الكروت" },
                 { "ProblematicCards", "كروت بها مشكلة" },
                 { "PotentialNegativeStockCards", "صرف/رصيد سالب محتمل" },
                 { "MissingIssueVoucherCards", "بدون إذن صرف" },
                 { "MissingKycCards", "بدون KYC" },
                 { "DuplicateCardInvoices", "كروت مكررة" },
                 { "NormalCards", "كروت طبيعية" },
                 { "ProblemRatio", "نسبة المشاكل" },
                 { "TransactionCount", "عدد الحركات" },
                 { "RechargeValue", "قيمة الشحن" },
                 { "RechargeAmount", "مبلغ الشحن" },
                { "RechargeTotal", "إجمالي الشحن" },
                { "NetValue", "الرسوم" },
                { "NetValueTotal", "إجمالي الرسوم" },
                { "Vat", "الضريبة" },
                { "VatTotal", "إجمالي الضريبة" },
                { "TotalValue", "الإجمالي" },
                { "StoreName", "المخزن" },
                { "ItemCode", "كود الصنف" },
                { "ItemName", "اسم الصنف" },
                 { "ItemSerial", "السيريال" },
                 { "StockBalance", "رصيد السيريال" },
                 { "LastTransactionId", "آخر حركة" },
                 { "LastTransactionDate", "تاريخ الدخول/آخر حركة" },
                 { "SerialStatus", "حالة السيريال" },
                { "ClosingDate", "تاريخ الإغلاق" },
                 { "RowNo", "م" },
                 { "TotalSupply", "إجمالي التوريد" },
                 { "CountCards", "كارت كيشني - عدد" },
                 { "CardValue", "كارت كيشني - قيمة" },
                 { "WalletBalance", "رصيد Wallet" },
                 { "WalletSupply", "توريد Wallet" },
                 { "BankBalanceCharge", "تكلفة رسوم" },
                 { "TotalRev2", "الخصم" },
                 { "TotalSaleDay2Vat", "ضريبة كارت كيشني" },
                 { "TotalRevvat", "ضريبة رسوم الشحن" },
                 { "TotalRevWithVat", "رسوم الشحن شامل الضريبة" },
                 { "ReturnsCount", "المرتجعات - عدد" },
                 { "TotalReturns", "المرتجعات - قيمة" },
                 { "NetCashOut", "صافي كاش أوت" },
                 { "BoxValue", "العهدة" },
                 { "ClosingStatus", "حالة الإغلاق" },
                 { "NoteDate", "تاريخ القيد" },
                 { "NoteID", "رقم القيد" },
                 { "NoteSerial", "مسلسل القيد" },
                 { "VoucherType", "نوع القيد" },
                 { "VoucherNo", "رقم الفاتورة" },
                 { "OpenBalance", "رصيد افتتاحي" },
                 { "LastBalance", "رصيد ختامي" },
                 { "TotalRechargeValue", "أصل مبلغ الشحن" },
                 { "CountTransaction", "عدد عمليات الشحن" },
                 { "TotalRev", "إجمالي الإيرادات" },
                 { "TotalVat", "إجمالي الضريبة" },
                 { "UserId", "رقم المستخدم" },
                 { "UserName", "اسم المستخدم" },
                 { "EmployeeName", "اسم الموظف" },
                 { "BranchId", "رقم الفرع" },
                 { "LoginCount", "عدد مرات الدخول" },
                 { "LastLoginDate", "آخر دخول" },
                 { "LoginSource", "مصدر الدخول" },
                 { "LoginType", "نوع الدخول" },
                 { "IpAddress", "IP Address" },
                 { "MachineName", "اسم الجهاز" },
                 { "AppName", "التطبيق" },
                 { "ClientType", "نوع العميل" }
             };

            string title;
            return map.TryGetValue(name, out title) ? title : name;
        }

        private static byte[] BuildExcel(string reportTitle, DateTime from, DateTime to, int branchId, DataTable table)
        {
            using (var stream = new MemoryStream())
            {
                using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    var workbookPart = document.AddWorkbookPart();
                    workbookPart.Workbook = new Workbook();
                    var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                    var sheetData = new SheetData();
                    worksheetPart.Worksheet = new Worksheet();
                    worksheetPart.Worksheet.Append(new SheetViews(new SheetView(new Pane { VerticalSplit = 4D, TopLeftCell = "A5", ActivePane = PaneValues.BottomLeft, State = PaneStateValues.Frozen }) { WorkbookViewId = 0, RightToLeft = true }));
                    worksheetPart.Worksheet.Append(new SheetFormatProperties { DefaultColumnWidth = 18D });
                    worksheetPart.Worksheet.Append(new Columns(new Column { Min = 1, Max = (uint)Math.Max(1, table.Columns.Count), Width = 22D, CustomWidth = true }));
                    worksheetPart.Worksheet.Append(sheetData);

                    sheetData.Append(RowFromValues(reportTitle));
                    sheetData.Append(RowFromValues("من تاريخ", from.ToString("yyyy-MM-dd"), "إلى تاريخ", to.ToString("yyyy-MM-dd"), "الفرع", branchId <= 0 ? "كل الفروع" : branchId.ToString()));
                    sheetData.Append(new Row());
                    sheetData.Append(RowFromValues(table.Columns.Cast<DataColumn>().Select(c => MapColumnTitle(c.ColumnName)).ToArray()));

                     foreach (DataRow row in table.Rows)
                     {
                     sheetData.Append(RowFromValues(table.Columns.Cast<DataColumn>().Select(c =>
                     {
                         var formatted = FormatReportValue(row[c]);
                         return formatted == null ? string.Empty : Convert.ToString(formatted, CultureInfo.InvariantCulture);
                     }).ToArray()));
                     }

                    var totals = BuildTotals(table);
                    if (totals.Count > 0)
                    {
                        sheetData.Append(RowFromValues(table.Columns.Cast<DataColumn>().Select((c, index) =>
                        {
                            decimal total;
                            if (totals.TryGetValue(c.ColumnName, out total))
                            {
                                return total.ToString("0.##", CultureInfo.InvariantCulture);
                            }

                            return index == 0 ? "الإجمالي العام" : string.Empty;
                        }).ToArray()));
                    }

                    AppendCardProblemsSummary(sheetData, table);

                    worksheetPart.Worksheet.Append(new AutoFilter { Reference = "A4:" + GetExcelColumnName(Math.Max(1, table.Columns.Count)) + Math.Max(4, table.Rows.Count + 4) });

                    var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                    sheets.Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Report" });
                    workbookPart.Workbook.Save();
                }

                return stream.ToArray();
            }
        }

        private JsonResult LargeJson(object data)
        {
            return new JsonResult
            {
                Data = data,
                JsonRequestBehavior = JsonRequestBehavior.DenyGet,
                MaxJsonLength = int.MaxValue,
                RecursionLimit = 100
            };
        }

        private static Row RowFromValues(params string[] values)
        {
            var row = new Row();
            foreach (var value in values)
            {
                row.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(value ?? string.Empty) });
            }

            return row;
        }

        private static void AppendCardProblemsSummary(SheetData sheetData, DataTable table)
        {
            if (sheetData == null || table == null || !HasColumn(table, "ServiceType") || !HasColumn(table, "CardIssueStatus"))
            {
                return;
            }

            var groups = new SortedDictionary<string, CardProblemsSummaryRow>(StringComparer.CurrentCultureIgnoreCase);
            foreach (DataRow row in table.Rows)
            {
                if (!string.Equals(ReadCell(row, "ServiceType"), "Card", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var groupName = ReadCell(row, "Store");
                if (string.IsNullOrWhiteSpace(groupName))
                {
                    groupName = ReadCell(row, "Branch");
                }

                if (string.IsNullOrWhiteSpace(groupName))
                {
                    groupName = "غير محدد";
                }

                CardProblemsSummaryRow summary;
                if (!groups.TryGetValue(groupName, out summary))
                {
                    summary = new CardProblemsSummaryRow();
                    groups[groupName] = summary;
                }

                summary.TotalCards++;
                if (IsProblemCardStatus(ReadCell(row, "CardIssueStatus")))
                {
                    summary.NegativeCards++;
                }
            }

            if (groups.Count == 0)
            {
                return;
            }

            sheetData.Append(new Row());
            sheetData.Append(RowFromValues("ملخص مشاكل الكروت حسب المخزن"));
            sheetData.Append(RowFromValues("المخزن / الفرع", "Total Cards", "Negative Cards", "Normal Cards", "Negative Ratio"));

            foreach (var group in groups)
            {
                var summary = group.Value;
                var normalCards = summary.TotalCards - summary.NegativeCards;
                var ratio = summary.TotalCards == 0 ? 0m : (summary.NegativeCards * 100m / summary.TotalCards);
                sheetData.Append(RowFromValues(
                    group.Key,
                    summary.TotalCards.ToString(CultureInfo.InvariantCulture),
                    summary.NegativeCards.ToString(CultureInfo.InvariantCulture),
                    normalCards.ToString(CultureInfo.InvariantCulture),
                    ratio.ToString("0.0", CultureInfo.InvariantCulture) + "%"));
            }
        }

        private static bool HasColumn(DataTable table, string columnName)
        {
            return table != null && table.Columns.Contains(columnName);
        }

        private static string ReadCell(DataRow row, string columnName)
        {
            if (row == null || row.Table == null || !row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
            {
                return string.Empty;
            }

            return Convert.ToString(row[columnName], CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static bool IsProblemCardStatus(string status)
        {
            return string.Equals(status, "Negative", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Insufficient Balance", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Problematic Card", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetExcelColumnName(int columnNumber)
        {
            var dividend = columnNumber;
            var columnName = string.Empty;
            while (dividend > 0)
            {
                var modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }

            return columnName;
        }

        private static string SafeFileName(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return value.Replace(' ', '_');
        }

        private static object Fail(string message, string technicalMessage)
        {
            return new { success = false, message = message };
        }

        private PosUserContext GetPosContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _repository);
        }

        private static Dictionary<string, decimal> BuildTotals(DataTable table)
        {
            var totals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            if (table == null)
            {
                return totals;
            }

            foreach (DataColumn column in table.Columns)
            {
                if (!ShouldTotalColumn(column))
                {
                    continue;
                }

                decimal total = 0m;
                foreach (DataRow row in table.Rows)
                {
                    if (row[column] == null || row[column] == DBNull.Value)
                    {
                        continue;
                    }

                    decimal value;
                    if (decimal.TryParse(Convert.ToString(row[column], CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                    {
                        total += value;
                    }
                }

                totals[column.ColumnName] = total;
            }

            return totals;
        }

        private static bool ShouldTotalColumn(DataColumn column)
        {
            if (column == null)
            {
                return false;
            }

            var name = column.ColumnName ?? string.Empty;
            if (name.Equals("RowNo", StringComparison.OrdinalIgnoreCase)
                || name.Equals("BranchID", StringComparison.OrdinalIgnoreCase)
                || name.Equals("CardStockBefore", StringComparison.OrdinalIgnoreCase)
                || name.Equals("CardStockAfter", StringComparison.OrdinalIgnoreCase)
                || name.IndexOf("Id", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Status", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            var type = column.DataType;
            return type == typeof(byte)
                || type == typeof(short)
                || type == typeof(int)
                || type == typeof(long)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(decimal);
        }

        private sealed class CardProblemsSummaryRow
        {
            public int TotalCards { get; set; }
            public int NegativeCards { get; set; }
        }
    }

    public class PosReportRunRequest
    {
        public string ReportKey { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? BranchId { get; set; }
        public int? BranchFromId { get; set; }
        public int? BranchToId { get; set; }
        public int? StoreId { get; set; }
        public int? UserId { get; set; }
        public string LoginSource { get; set; }
        public string SerialSearch { get; set; }
        public bool ShowEmptyBranches { get; set; }
        public string ServiceSearch { get; set; }
        public string ServiceType { get; set; }
        public bool IncludeCardIssueCheck { get; set; }
        public string CardIssueMode { get; set; }
        public string SortBy { get; set; }
    }

    internal class ReportValidationResult
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public string TechnicalMessage { get; set; }
        public PosReportDefinition Report { get; set; }
        public int BranchId { get; set; }
    }

    public class PosReportDefinition
    {
        public PosReportDefinition(string key, string title, string group, bool allowed, bool enabled, string source)
        {
            Key = key;
            Title = title;
            Group = group;
            Allowed = allowed;
            Enabled = enabled;
            Source = source;
        }

        public string Key { get; private set; }
        public string Title { get; private set; }
        public string Group { get; private set; }
        public bool Allowed { get; private set; }
        public bool Enabled { get; private set; }
        public string Source { get; private set; }
    }

    public class PosReportColumnDto
    {
        public string Key { get; set; }
        public string Title { get; set; }
    }
}
