using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using MyERP.Models.Reports;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class HtmlReportsController : Controller
    {
        private readonly PosSqlRepository _repository = new PosSqlRepository();

        public ActionResult Index(HtmlReportFilterModel filter)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!CanOpenReports(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية عرض التقارير");
            }

            return View("Index", BuildPageModel(context, filter));
        }

        [HttpPost]
        public ActionResult Search(HtmlReportFilterModel filter)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!CanOpenReports(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية عرض التقارير");
            }

            var model = BuildPageModel(context, filter);
            if (!string.IsNullOrWhiteSpace(model.Filter.ReportKey))
            {
                model.Result = RunReport(context, model.Filter, model.ActiveReport);
            }

            return View("Index", model);
        }

        [HttpPost]
        public ActionResult Export(HtmlReportFilterModel filter)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return new HttpStatusCodeResult(401, "يجب تسجيل دخول نقطة البيع أولاً");
            }

            if (!CanOpenReports(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية عرض التقارير");
            }

            var model = BuildPageModel(context, filter);
            if (model.ActiveReport == null || string.IsNullOrWhiteSpace(model.Filter.ReportKey))
            {
                return new HttpStatusCodeResult(400, "اختر التقرير أولاً");
            }
            model.Result = RunReport(context, model.Filter, model.ActiveReport);
            var bytes = BuildExcel(model);
            var from = model.Filter.FromDate.GetValueOrDefault(DateTime.Today).ToString("yyyyMMdd");
            var to = model.Filter.ToDate.GetValueOrDefault(DateTime.Today).ToString("yyyyMMdd");
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", SafeFileName(model.ActiveReport.Title) + "_" + from + "_" + to + ".xlsx");
        }

        private HtmlReportPageViewModel BuildPageModel(PosUserContext context, HtmlReportFilterModel filter)
        {
            filter = filter ?? new HtmlReportFilterModel();
            if (filter.FromDate == null) { filter.FromDate = DateTime.Today; }
            if (filter.ToDate == null) { filter.ToDate = DateTime.Today; }

            var reports = BuildReportDefinitions();
            var activeReport = !string.IsNullOrWhiteSpace(filter.ReportKey)
                ? reports.FirstOrDefault(x => string.Equals(x.Key, filter.ReportKey, StringComparison.OrdinalIgnoreCase))
                : null;
            if (activeReport == null && !string.IsNullOrWhiteSpace(filter.ReportKey)) { activeReport = reports[0]; }

            if (!IsAdmin(context) && context.BranchId.HasValue)
            {
                filter.BranchId = context.BranchId.Value;
            }

            return new HtmlReportPageViewModel
            {
                PageTitle = "التقارير الذكية",
                Filter = filter,
                Reports = reports,
                ActiveReport = activeReport,
                Branches = GetAllowedBranches(context).ToList(),
                Stores = GetAllowedStores(context).ToList()
            };
        }

        private HtmlReportResultModel RunReport(PosUserContext context, HtmlReportFilterModel filter, HtmlReportDefinition report)
        {
            var from = filter.FromDate.GetValueOrDefault(DateTime.Today).Date;
            var to = filter.ToDate.GetValueOrDefault(DateTime.Today).Date;
            var branchId = IsAdmin(context) ? filter.BranchId.GetValueOrDefault(0) : context.BranchId.GetValueOrDefault(0);
            var canSeeBranchReports = IsAdmin(context) || context.CanViewReports;
            var table = report.SupportsStoreFilter
                ? _repository.RunPosStoreSerialsReport(filter.StoreId, filter.SerialSearch, branchId, context.UserId, canSeeBranchReports)
                : _repository.RunPosReport(report.Key, from, to, branchId, context.UserId, canSeeBranchReports);
            return ToResult(report.Title, table);
        }

        private static IList<HtmlReportDefinition> BuildReportDefinitions()
        {
            return new List<HtmlReportDefinition>
            {
                Report("daily-trans", "تقرير يومي بالحركات", "حركات نقطة البيع اليومية في شاشة تقارير كيشني.", "مصدر بيانات تقارير نقطة البيع"),
                Report("daily-trans-2", "تقرير يومي بالحركات 2", "نسخة تفصيلية ثانية لحركات نقطة البيع اليومية.", "مصدر بيانات تقارير نقطة البيع"),
                Report("sales-complete", "تقرير المبيعات الشامل 1", "ملخص مبيعات حسب الفرع ونوع العملية.", "مصدر بيانات تقارير نقطة البيع"),
                Report("sales-complete-2", "تقرير المبيعات الشامل 2", "ملخص مبيعات شامل قابل للطباعة والتصدير.", "مصدر بيانات تقارير نقطة البيع"),
                Report("general-sales", "تقرير المبيعات العام", "تقرير المبيعات العام من بيانات كيشني.", "مصدر بيانات تقارير نقطة البيع"),
                Report("finance-closing", "تقرير الإغلاق المالي", "حركات إغلاق نقطة البيع.", "مصدر بيانات تقارير نقطة البيع"),
                Report("finance-closing-discounts", "تقرير الإغلاق المالي والخصومات", "قيود الإغلاق والخصومات المرتبطة بها.", "مصدر بيانات تقارير نقطة البيع"),
                Report("revenues", "تقرير الإيرادات", "إيرادات الرسوم والضريبة وصافي التحصيل حسب الفرع ونوع العملية.", "مصدر بيانات تقارير نقطة البيع"),
                Report("store-serials", "تقرير سيريالات المخزن", "السيريالات المتاحة داخل المخزن مع بحث اختياري.", "مصدر بيانات السيريالات", true)
            };
        }

        private static HtmlReportDefinition Report(string key, string title, string description, string source, bool store = false)
        {
            return new HtmlReportDefinition { Key = key, Title = title, Description = description, SourceName = source, SupportsStoreFilter = store };
        }

        private static HtmlReportResultModel ToResult(string title, DataTable table)
        {
            var result = new HtmlReportResultModel { Title = title };
            foreach (DataColumn column in table.Columns)
            {
                var numeric = IsNumericColumn(column);
                result.Columns.Add(new HtmlReportColumnModel
                {
                    Key = column.ColumnName,
                    Title = MapColumnTitle(column.ColumnName),
                    IsNumeric = numeric,
                    ShowTotal = numeric && ShouldTotal(column.ColumnName)
                });
            }

            foreach (DataRow row in table.Rows)
            {
                var item = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (DataColumn column in table.Columns)
                {
                    var value = row[column] == DBNull.Value ? null : NormalizeReportValue(row[column]);
                    item[column.ColumnName] = value;
                    decimal number;
                    if (value != null && ShouldTotal(column.ColumnName) && decimal.TryParse(Convert.ToString(value), out number))
                    {
                        result.Totals[column.ColumnName] = result.Totals.ContainsKey(column.ColumnName) ? result.Totals[column.ColumnName] + number : number;
                    }
                }

                result.Rows.Add(item);
            }

            return result;
        }

        private static object NormalizeReportValue(object value)
        {
            if (value == null)
            {
                return null;
            }

            var text = Convert.ToString(value);
            if (!string.IsNullOrWhiteSpace(text))
            {
                text = text.Trim();
                var match = System.Text.RegularExpressions.Regex.Match(text, @"\/Date\((-?\d+)(?:[+-]\d+)?\)\/");
                long milliseconds;
                if (match.Success && long.TryParse(match.Groups[1].Value, out milliseconds))
                {
                    return new DateTime(1970, 1, 1).AddMilliseconds(milliseconds).ToLocalTime();
                }
            }

            return value;
        }

        private IEnumerable<HtmlReportLookupItem> GetAllowedBranches(PosUserContext context)
        {
            if (IsAdmin(context))
            {
                return _repository.GetBranches().Select(x => new HtmlReportLookupItem { Id = x.BranchId, Name = x.BranchName });
            }

            return context.BranchId.HasValue
                ? new[] { new HtmlReportLookupItem { Id = context.BranchId.Value, Name = context.BranchName } }
                : new HtmlReportLookupItem[0];
        }

        private IEnumerable<HtmlReportLookupItem> GetAllowedStores(PosUserContext context)
        {
            return _repository.GetStoresByBranch(IsAdmin(context) ? (int?)null : context.BranchId).Select(x => new HtmlReportLookupItem { Id = x.StoreID, Name = x.StoreName });
        }

        private static bool CanOpenReports(PosUserContext context)
        {
            return IsAdmin(context) || (context != null && context.CanViewReports);
        }

        private static bool IsAdmin(PosUserContext context)
        {
            return context != null && context.UserType.GetValueOrDefault(-1) == 0;
        }

        private PosUserContext GetPosContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _repository);
        }

        private static bool IsNumericColumn(DataColumn column)
        {
            var type = column.DataType;
            return type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double) || type == typeof(decimal);
        }

        private static bool ShouldTotal(string columnName)
        {
            var name = columnName ?? string.Empty;
            return name.IndexOf("Id", StringComparison.OrdinalIgnoreCase) < 0
                && name.IndexOf("Count", StringComparison.OrdinalIgnoreCase) < 0
                && (name.IndexOf("Value", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Total", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Vat", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Balance", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Price", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Collection", StringComparison.OrdinalIgnoreCase) >= 0);
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
                { "FeesTotal", "إجمالي الرسوم" },
                { "NetCollection", "صافي التحصيل" },
                { "StoreName", "المخزن" },
                { "ItemCode", "كود الصنف" },
                { "ItemName", "اسم الصنف" },
                { "ItemSerial", "السيريال" },
                { "StockBalance", "رصيد السيريال" },
                { "LastTransactionId", "آخر حركة" },
                { "LastTransactionDate", "تاريخ الدخول/آخر حركة" },
                { "SerialStatus", "حالة السيريال" },
                { "NoteID", "رقم القيد" },
                { "NoteSerial", "سيريال القيد" },
                { "NoteDate", "تاريخ القيد" },
                { "VoucherType", "نوع القيد" },
                { "NoteValue", "قيمة القيد" },
                { "UserName", "المستخدم" },
                { "ClosingDate", "تاريخ الإغلاق" },
                { "OpenBalance", "رصيد أول اليوم" },
                { "LastBalance", "رصيد نهاية اليوم" },
                { "TotalRechargeValue", "مبلغ الشحن" },
                { "TotalRev", "مبلغ الرسوم" },
                { "TotalVat", "الضريبة" },
                { "TotalSupply", "إجمالي التوريد" },
                { "CashOut", "Cash Out" },
                { "CashOutTotal", "إجمالي Cash Out" },
                { "CashOutDisc", "خصم Cash Out" },
                { "BoxBalance", "رصيد العهدة أول اليوم" }
            };

            var cleanMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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
                { "FeesTotal", "إجمالي الرسوم" },
                { "NetCollection", "صافي التحصيل" },
                { "StoreName", "المخزن" },
                { "ItemCode", "كود الصنف" },
                { "ItemName", "اسم الصنف" },
                { "ItemSerial", "السيريال" },
                { "StockBalance", "رصيد السيريال" },
                { "LastTransactionId", "آخر حركة" },
                { "LastTransactionDate", "تاريخ الدخول/آخر حركة" },
                { "SerialStatus", "حالة السيريال" },
                { "NoteID", "رقم القيد" },
                { "NoteSerial", "سيريال القيد" },
                { "NoteDate", "تاريخ القيد" },
                { "VoucherType", "نوع القيد" },
                { "NoteValue", "قيمة القيد" },
                { "UserName", "المستخدم" },
                { "ClosingDate", "تاريخ الإغلاق" },
                { "OpenBalance", "رصيد أول اليوم" },
                { "LastBalance", "رصيد نهاية اليوم" },
                { "TotalRechargeValue", "مبلغ الشحن" },
                { "TotalRev", "مبلغ الرسوم" },
                { "TotalVat", "الضريبة" },
                { "TotalSupply", "إجمالي التوريد" },
                { "CashOut", "Cash Out" },
                { "CashOutTotal", "إجمالي Cash Out" },
                { "CashOutDisc", "خصم Cash Out" },
                { "BoxBalance", "رصيد العهدة أول اليوم" }
            };

            string title;
            if (cleanMap.TryGetValue(name, out title)) { return title; }
            return map.TryGetValue(name, out title) ? title : name;
        }

        private static byte[] BuildExcel(HtmlReportPageViewModel model)
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
                    worksheetPart.Worksheet.Append(new SheetViews(new SheetView(new Pane { VerticalSplit = 5D, TopLeftCell = "A6", ActivePane = PaneValues.BottomLeft, State = PaneStateValues.Frozen }) { WorkbookViewId = 0, RightToLeft = true }));
                    worksheetPart.Worksheet.Append(new SheetFormatProperties { DefaultColumnWidth = 18D });
                    worksheetPart.Worksheet.Append(new Columns(new Column { Min = 1, Max = (uint)Math.Max(1, model.Result.Columns.Count), Width = 22D, CustomWidth = true }));
                    worksheetPart.Worksheet.Append(sheetData);

                    sheetData.Append(RowFromValues(model.ActiveReport.Title));
                    sheetData.Append(RowFromValues("من تاريخ", DateText(model.Filter.FromDate), "إلى تاريخ", DateText(model.Filter.ToDate)));
                    sheetData.Append(RowFromValues("نوع التقرير", "تقرير تشغيلي"));
                    sheetData.Append(new Row());
                    sheetData.Append(RowFromValues(model.Result.Columns.Select(c => c.Title).ToArray()));

                    foreach (var row in model.Result.Rows)
                    {
                        sheetData.Append(RowFromValues(model.Result.Columns.Select(c => row.ContainsKey(c.Key) && row[c.Key] != null ? FormatExportCell(row[c.Key]) : string.Empty).ToArray()));
                    }

                    if (model.Result.Totals.Count > 0)
                    {
                        sheetData.Append(RowFromValues(model.Result.Columns.Select(c => c.ShowTotal && model.Result.Totals.ContainsKey(c.Key) ? model.Result.Totals[c.Key].ToString("0.##") : (c == model.Result.Columns.First() ? "الإجمالي" : string.Empty)).ToArray()));
                    }

                    worksheetPart.Worksheet.Append(new AutoFilter { Reference = "A5:" + GetExcelColumnName(Math.Max(1, model.Result.Columns.Count)) + Math.Max(5, model.Result.Rows.Count + 5) });
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

        private static string DateText(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("dd/MM/yyyy") : string.Empty;
        }

        private static string FormatExportCell(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is DateTime)
            {
                return ((DateTime)value).ToString("dd/MM/yyyy");
            }

            var text = Convert.ToString(value);
            if (!string.IsNullOrWhiteSpace(text))
            {
                text = text.Trim();
                var match = System.Text.RegularExpressions.Regex.Match(text, @"\/Date\((-?\d+)(?:[+-]\d+)?\)\/");
                long milliseconds;
                if (match.Success && long.TryParse(match.Groups[1].Value, out milliseconds))
                {
                    return new DateTime(1970, 1, 1).AddMilliseconds(milliseconds).ToLocalTime().ToString("dd/MM/yyyy");
                }
            }

            return text;
        }

        private static string SafeFileName(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return value.Replace(' ', '_');
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
    }
}
