using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyERP.Models.Reports;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace MyERP.Common.AccountingReports
{
    public class SharedAccountingReportService
    {
        private readonly SharedAccountingReportRepository _repository;

        public SharedAccountingReportService(SharedAccountingReportRepository repository)
        {
            _repository = repository;
        }

        public HtmlReportPageViewModel BuildPage(HtmlReportFilterModel filter, int? forcedBranchId, bool loadBranches)
        {
            filter = filter ?? new HtmlReportFilterModel();
            if (!filter.FromDate.HasValue) { filter.FromDate = DateTime.Today; }
            if (!filter.ToDate.HasValue) { filter.ToDate = DateTime.Today; }
            if (forcedBranchId.HasValue) { filter.BranchId = forcedBranchId.Value; }

            var reports = SharedAccountingReportCatalog.GetAll();
            var active = string.IsNullOrWhiteSpace(filter.ReportKey) ? null : reports.FirstOrDefault(x => string.Equals(x.Key, filter.ReportKey, StringComparison.OrdinalIgnoreCase));
            if (active == null && !string.IsNullOrWhiteSpace(filter.ReportKey))
            {
                active = reports.First();
                filter.ReportKey = active.Key;
            }

            return new HtmlReportPageViewModel
            {
                PageTitle = "تقارير الحسابات",
                Filter = filter,
                Reports = reports,
                ActiveReport = active,
                Branches = loadBranches ? GetBranches(forcedBranchId) : new List<HtmlReportLookupItem>()
            };
        }

        public IList<HtmlReportLookupItem> GetBranches(int? forcedBranchId)
        {
            var branches = _repository.GetBranches();
            return forcedBranchId.HasValue
                ? branches.Where(x => x.Id == forcedBranchId.Value).ToList()
                : branches;
        }

        public HtmlReportResultModel Run(HtmlReportFilterModel filter, HtmlReportDefinition report, int userId, bool canChangeDefaults)
        {
            var table = _repository.RunReport(filter, userId, canChangeDefaults);
            return ToResult(report == null ? "تقرير حسابات" : report.Title, table);
        }

        private static HtmlReportResultModel ToResult(string title, DataTable table)
        {
            var result = new HtmlReportResultModel { Title = title };
            foreach (DataColumn column in table.Columns)
            {
                if (string.Equals(column.ColumnName, "AccountCode", StringComparison.OrdinalIgnoreCase) && table.Columns.Contains("AccountSerial"))
                {
                    continue;
                }

                var numeric = IsNumeric(column);
                result.Columns.Add(new HtmlReportColumnModel
                {
                    Key = column.ColumnName,
                    Title = MapColumn(column.ColumnName),
                    IsNumeric = numeric,
                    ShowTotal = numeric && ShouldTotal(column.ColumnName)
                });
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
                && name.IndexOf("Level", StringComparison.OrdinalIgnoreCase) < 0
                && (name.IndexOf("Debit", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Credit", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Balance", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Amount", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Value", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Net", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static string MapColumn(string name)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "SectionName", "البند" },
                { "ReportSource", "مصدر التقرير" },
                { "AccountSerial", "رقم الحساب" },
                { "AccountCode", "كود الحساب" },
                { "AccountName", "اسم الحساب" },
                { "AccountNameEng", "اسم الحساب إنجليزي" },
                { "ParentAccountCode", "الحساب الأب" },
                { "AccountLevel", "المستوى" },
                { "OpeningBalance", "رصيد افتتاحي" },
                { "Debit", "مدين" },
                { "Credit", "دائن" },
                { "DebitBalance", "رصيد مدين" },
                { "CreditBalance", "رصيد دائن" },
                { "ClosingBalance", "رصيد ختامي" },
                { "Balance", "الرصيد" },
                { "NetAmount", "الصافي" },
                { "RecordDate", "التاريخ" },
                { "NoteDate", "تاريخ القيد" },
                { "NoteSerial", "رقم القيد" },
                { "NoteSerial1", "رقم المستند" },
                { "NoteType", "نوع القيد" },
                { "BranchName", "الفرع" },
                { "Description", "البيان" },
                { "RunningBalance", "الرصيد المتحرك" },
                { "CostCenterId", "مركز التكلفة" },
                { "CostCenterName", "اسم مركز التكلفة" },
                { "ProjectId", "المشروع" },
                { "ProjectName", "اسم المشروع" },
                { "OperationId", "البند/العملية" },
                { "OperationName", "اسم البند/العملية" },
                { "UserName", "المستخدم" }
            };
            string mapped;
            return map.TryGetValue(name, out mapped) ? mapped : name;
        }
    }

    public static class SharedAccountingReportExcelExporter
    {
        public static byte[] Build(HtmlReportPageViewModel model)
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
                    sheetData.Append(RowFromValues("نوع التقرير", model.ActiveReport.Description ?? string.Empty));
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

        public static string SafeFileName(string value)
        {
            foreach (var ch in Path.GetInvalidFileNameChars())
            {
                value = (value ?? "Report").Replace(ch, '_');
            }
            return string.IsNullOrWhiteSpace(value) ? "Report" : value;
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

        private static string FormatCell(object value)
        {
            if (value == null) { return string.Empty; }
            if (value is DateTime) { return ((DateTime)value).ToString("dd/MM/yyyy"); }
            if (value is decimal) { return ((decimal)value).ToString("0.##"); }
            if (value is double) { return ((double)value).ToString("0.##"); }
            if (value is float) { return ((float)value).ToString("0.##"); }
            return Convert.ToString(value);
        }
    }
}
