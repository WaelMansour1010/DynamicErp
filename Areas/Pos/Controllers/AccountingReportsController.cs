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
    public class AccountingReportsController : Controller
    {
        private readonly PosSqlRepository _repository = new PosSqlRepository();

        public ActionResult Index(HtmlReportFilterModel filter)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!CanOpen(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية عرض تقارير الحسابات");
            }

            return View(BuildPageModel(context, filter));
        }

        [HttpPost]
        public ActionResult Search(HtmlReportFilterModel filter)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!CanOpen(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية عرض تقارير الحسابات");
            }

            var model = BuildPageModel(context, filter);
            if (!CanRun(context, model.Filter.ReportKey))
            {
                model.Message = "ليست لديك صلاحية عرض هذا التقرير";
                return View("Index", model);
            }

            model.Result = RunReport(context, model.Filter, model.ActiveReport);
            return View("Index", model);
        }

        [HttpPost]
        public ActionResult Export(HtmlReportFilterModel filter)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return new HttpStatusCodeResult(401, "يجب تسجيل الدخول أولا");
            }

            if (!CanOpen(context) || !CanRun(context, filter != null ? filter.ReportKey : null))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية تصدير هذا التقرير");
            }

            var model = BuildPageModel(context, filter);
            model.Result = RunReport(context, model.Filter, model.ActiveReport);
            var from = model.Filter.FromDate.GetValueOrDefault(DateTime.Today).ToString("yyyyMMdd");
            var to = model.Filter.ToDate.GetValueOrDefault(DateTime.Today).ToString("yyyyMMdd");
            return File(BuildExcel(model), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", SafeFileName(model.ActiveReport.Title) + "_" + from + "_" + to + ".xlsx");
        }

        [HttpGet]
        public ActionResult Accounts(string term)
        {
            var context = GetPosContext();
            if (context == null || !CanOpen(context))
            {
                return new HttpStatusCodeResult(403);
            }

            return Json(_repository.SearchAccounts(term).Select(x => new { id = x.Id, text = x.Name, serial = x.Extra }), JsonRequestBehavior.AllowGet);
        }

        private HtmlReportPageViewModel BuildPageModel(PosUserContext context, HtmlReportFilterModel filter)
        {
            filter = filter ?? new HtmlReportFilterModel();
            if (filter.FromDate == null) { filter.FromDate = DateTime.Today; }
            if (filter.ToDate == null) { filter.ToDate = DateTime.Today; }

            var reports = BuildDefinitions();
            var active = reports.FirstOrDefault(x => string.Equals(x.Key, filter.ReportKey, StringComparison.OrdinalIgnoreCase))
                ?? reports.FirstOrDefault(x => CanRun(context, x.Key))
                ?? reports.First();
            filter.ReportKey = active.Key;
            if (!IsAdmin(context) && context.BranchId.HasValue)
            {
                filter.BranchId = context.BranchId.Value;
            }

            return new HtmlReportPageViewModel
            {
                PageTitle = "تقارير الحسابات",
                Filter = filter,
                Reports = reports,
                ActiveReport = active,
                Branches = GetAllowedBranches(context).ToList()
            };
        }

        private HtmlReportResultModel RunReport(PosUserContext context, HtmlReportFilterModel filter, HtmlReportDefinition report)
        {
            var from = filter.FromDate.GetValueOrDefault(DateTime.Today).Date;
            var to = filter.ToDate.GetValueOrDefault(DateTime.Today).Date;
            var branchId = IsAdmin(context) ? filter.BranchId.GetValueOrDefault(0) : context.BranchId.GetValueOrDefault(0);
            var table = _repository.RunAccountingReport(report.Key, from, to, branchId, filter.AccountFrom, filter.AccountTo, filter.CostCenterId, context.UserId, IsAdmin(context));
            return ToResult(report.Title, table);
        }

        private static IList<HtmlReportDefinition> BuildDefinitions()
        {
            return new List<HtmlReportDefinition>
            {
                new HtmlReportDefinition { Key = "trial-balance", Title = "ميزان مراجعة", Description = "الأرصدة الافتتاحية والحركة والرصيد الختامي حسب الحساب." },
                new HtmlReportDefinition { Key = "income-statement", Title = "قائمة الدخل", Description = "الإيرادات والمصروفات وصافي الربح أو الخسارة." },
                new HtmlReportDefinition { Key = "account-statement", Title = "كشف حساب", Description = "حركات الحساب مع رصيد متحرك حسب الفترة والفروع." },
                new HtmlReportDefinition { Key = "general-ledger-assistant", Title = "أستاذ عام مساعد", Description = "حركات تفصيلية مجمعة حسب الحساب." }
            };
        }

        private static bool CanRun(PosUserContext context, string key)
        {
            if (IsAdmin(context)) { return true; }
            if (context == null || !context.CanViewAccountingReports) { return false; }
            switch ((key ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "trial-balance": return context.CanViewTrialBalance;
                case "income-statement": return context.CanViewIncomeStatement;
                case "account-statement": return context.CanViewAccountStatement;
                case "general-ledger-assistant": return context.CanViewGeneralLedgerAssistant;
                default: return false;
            }
        }

        private static bool CanOpen(PosUserContext context)
        {
            return IsAdmin(context) || (context != null && context.CanViewAccountingReports);
        }

        private IEnumerable<HtmlReportLookupItem> GetAllowedBranches(PosUserContext context)
        {
            if (IsAdmin(context))
            {
                return _repository.GetBranches().Select(x => new HtmlReportLookupItem { Id = x.BranchId, Name = x.BranchName });
            }

            return context != null && context.BranchId.HasValue
                ? new[] { new HtmlReportLookupItem { Id = context.BranchId.Value, Name = context.BranchName } }
                : new HtmlReportLookupItem[0];
        }

        private static HtmlReportResultModel ToResult(string title, DataTable table)
        {
            var result = new HtmlReportResultModel { Title = title };
            foreach (DataColumn column in table.Columns)
            {
                var numeric = IsNumeric(column);
                result.Columns.Add(new HtmlReportColumnModel { Key = column.ColumnName, Title = MapColumn(column.ColumnName), IsNumeric = numeric, ShowTotal = numeric && ShouldTotal(column.ColumnName) });
            }

            foreach (DataRow row in table.Rows)
            {
                var item = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (DataColumn column in table.Columns)
                {
                    var value = row[column] == DBNull.Value ? null : row[column];
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

        private static bool IsNumeric(DataColumn column)
        {
            var type = column.DataType;
            return type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double) || type == typeof(decimal);
        }

        private static bool ShouldTotal(string name)
        {
            name = name ?? string.Empty;
            return name.IndexOf("Serial", StringComparison.OrdinalIgnoreCase) < 0
                && name.IndexOf("Id", StringComparison.OrdinalIgnoreCase) < 0
                && (name.IndexOf("Debit", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Credit", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Balance", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Amount", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string MapColumn(string name)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "SectionName", "البند" },
                { "AccountSerial", "رقم الحساب" },
                { "AccountCode", "كود الحساب" },
                { "AccountName", "اسم الحساب" },
                { "OpeningBalance", "رصيد افتتاحي" },
                { "Debit", "مدين" },
                { "Credit", "دائن" },
                { "ClosingBalance", "رصيد ختامي" },
                { "NetAmount", "الصافي" },
                { "RecordDate", "التاريخ" },
                { "NoteSerial", "رقم القيد" },
                { "NoteSerial1", "رقم المستند" },
                { "BranchName", "الفرع" },
                { "Description", "البيان" },
                { "RunningBalance", "الرصيد المتحرك" }
            };
            string mapped;
            return map.TryGetValue(name, out mapped) ? mapped : name;
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
                    worksheetPart.Worksheet.Append(sheetData);
                    sheetData.Append(RowFromValues(model.ActiveReport.Title));
                    sheetData.Append(RowFromValues("من تاريخ", DateText(model.Filter.FromDate), "إلى تاريخ", DateText(model.Filter.ToDate)));
                    sheetData.Append(RowFromValues("نوع التقرير", "تقرير حسابات"));
                    sheetData.Append(new Row());
                    sheetData.Append(RowFromValues(model.Result.Columns.Select(x => x.Title).ToArray()));
                    foreach (var row in model.Result.Rows)
                    {
                        sheetData.Append(RowFromValues(model.Result.Columns.Select(c => row.ContainsKey(c.Key) && row[c.Key] != null ? FormatCell(row[c.Key]) : string.Empty).ToArray()));
                    }
                    if (model.Result.Totals.Count > 0)
                    {
                        sheetData.Append(RowFromValues(model.Result.Columns.Select(c => c.ShowTotal && model.Result.Totals.ContainsKey(c.Key) ? model.Result.Totals[c.Key].ToString("0.##") : (c == model.Result.Columns.First() ? "الإجمالي" : string.Empty)).ToArray()));
                    }
                    workbookPart.Workbook.AppendChild(new Sheets()).Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Report" });
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

        private static string FormatCell(object value)
        {
            if (value == null) { return string.Empty; }
            if (value is DateTime) { return ((DateTime)value).ToString("dd/MM/yyyy"); }
            if (value is decimal) { return ((decimal)value).ToString("0.##"); }
            if (value is double) { return ((double)value).ToString("0.##"); }
            if (value is float) { return ((float)value).ToString("0.##"); }
            return Convert.ToString(value);
        }

        private static string DateText(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("dd/MM/yyyy") : string.Empty;
        }

        private static string SafeFileName(string value)
        {
            foreach (var ch in Path.GetInvalidFileNameChars())
            {
                value = (value ?? "Report").Replace(ch, '_');
            }
            return string.IsNullOrWhiteSpace(value) ? "Report" : value;
        }

        private static bool IsAdmin(PosUserContext context)
        {
            return context != null && context.UserType.GetValueOrDefault(-1) == 0;
        }

        private PosUserContext GetPosContext()
        {
            return Session[PosLoginController.PosContextSessionKey] as PosUserContext;
        }
    }
}
