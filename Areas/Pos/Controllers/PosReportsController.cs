using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
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
            ViewBag.Branches = GetAllowedBranches(context);
            ViewBag.Stores = GetAllowedStores(context);
            return View();
        }

        [HttpPost]
        public JsonResult Run(PosReportRunRequest request)
        {
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.Charset = "utf-8";

            try
            {
                var context = GetPosContext();
                var validation = ValidateReportRequest(context, request);
                if (validation != null && validation.StatusCode != 0)
                {
                    Response.StatusCode = validation.StatusCode;
                    return Json(Fail(validation.Message, validation.TechnicalMessage));
                }

                var report = validation.Report;
                if (!report.Enabled)
                {
                    return Json(new { success = false, message = "لم يتم تحديد مصدر التقرير بعد" });
                }

                var table = LoadReportTable(report, request, context, validation.BranchId);
                return Json(new
                {
                    success = true,
                    reportName = report.Title,
                    columns = ToColumns(table),
                    rows = ToRows(table)
                });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(Fail("تعذر تشغيل التقرير", ex.Message));
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
                var table = LoadReportTable(report, request, context, validation.BranchId);
                var bytes = BuildExcel(report.Title, from, to, validation.BranchId, table);
                var fileName = string.Format("{0}_{1}_{2}.xlsx", SafeFileName(report.Title), from.ToString("yyyyMMdd"), to.ToString("yyyyMMdd"));
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return Json(Fail("تعذر تصدير Excel: " + ex.Message, ex.ToString()), JsonRequestBehavior.AllowGet);
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
                new PosReportDefinition("general-sales", "تقرير المبيعات العام", "sales", admin || context.CanReportAllSales, true, "تقرير مبيعات عام"),
                new PosReportDefinition("web-invoices", "تقرير فواتير الويب", "sales", admin || context.CanViewReports, true, "عدد فواتير الويب حسب المستخدم"),
                new PosReportDefinition("salesmen", "تقرير المناديب", "closings", admin || context.CanReportSalesmen, false, "لم يتم تحديد مصدر التقرير بعد"),
                new PosReportDefinition("finance-closing", "تقرير الإغلاقات", "closings", admin || context.CanReportClosings, true, "بيانات الإغلاقات من TBLClosePos و Notes"),
                new PosReportDefinition("finance-closing-discounts", "تقرير الإغلاق المالي والخصومات", "closings", admin || context.CanReportFinanceClosing, true, "بيانات الإغلاقات والخصومات من TBLClosePos و Notes"),
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

            var branchId = ResolveBranchId(context, request.BranchId);
            return new ReportValidationResult { Report = report, BranchId = branchId };
        }

        private DataTable LoadReportTable(PosReportDefinition report, PosReportRunRequest request, PosUserContext context, int branchId)
        {
            var from = (request.FromDate ?? DateTime.Today).Date;
            var to = (request.ToDate ?? DateTime.Today).Date;
            if (report.Key == "store-serials")
            {
                return _repository.RunPosStoreSerialsReport(request.StoreId, request.SerialSearch, branchId, context.UserId, IsAdmin(context) || context.CanViewReports);
            }

            if (report.Key == "web-invoices")
            {
                return _repository.RunPosWebInvoiceAuditReport(from, to, branchId, context.UserId, IsAdmin(context) || context.CanViewReports);
            }

            return _repository.RunPosReport(report.Key, from, to, branchId, context.UserId, IsAdmin(context) || context.CanViewReports);
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
                    item[column.ColumnName] = row[column] == DBNull.Value ? null : row[column];
                }
                yield return item;
            }
        }

        private static string MapColumnTitle(string name)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "BranchName", "الفرع" },
                { "BranchCode", "كود الفرع" },
                { "Transaction_ID", "رقم الحركة" },
                { "NoteSerial1", "رقم الفاتورة" },
                { "Transaction_Date", "التاريخ" },
                { "ReportType", "نوع العملية" },
                { "CashCustomerName", "اسم العميل" },
                { "CashCustomerPhone", "هاتف العميل" },
                { "TransactionCount", "عدد الحركات" },
                { "RechargeValue", "قيمة الشحن" },
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
                { "SerialStatus", "حالة السيريال" }
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
                        sheetData.Append(RowFromValues(table.Columns.Cast<DataColumn>().Select(c => row[c] == DBNull.Value ? string.Empty : Convert.ToString(row[c])).ToArray()));
                    }

                    worksheetPart.Worksheet.Append(new AutoFilter { Reference = "A4:" + GetExcelColumnName(Math.Max(1, table.Columns.Count)) + Math.Max(4, table.Rows.Count + 4) });

                    var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                    sheets.Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Report" });
                    workbookPart.Workbook.Save();
                }

                return stream.ToArray();
            }
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
            return new { success = false, message = message, technicalMessage = technicalMessage };
        }

        private PosUserContext GetPosContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _repository);
        }
    }

    public class PosReportRunRequest
    {
        public string ReportKey { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? BranchId { get; set; }
        public int? StoreId { get; set; }
        public string SerialSearch { get; set; }
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
