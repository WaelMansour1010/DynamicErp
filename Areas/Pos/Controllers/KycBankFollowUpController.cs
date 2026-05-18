using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class KycBankFollowUpController : Controller
    {
        private readonly PosSqlRepository _repository;

        public KycBankFollowUpController()
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

            if (!CanOpen(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية متابعة KYC والبنك");
            }

            ViewBag.PosContext = context;
            ViewBag.Branches = IsAdmin(context)
                ? _repository.GetBranches()
                : new[] { new PosBranchDto { BranchId = context.BranchId.GetValueOrDefault(), BranchName = context.BranchName } };
            return View();
        }

        [HttpPost]
        public JsonResult Preview(KycBankFollowUpRequest request)
        {
            try
            {
                var context = GetPosContext();
                if (context == null)
                {
                    Response.StatusCode = 401;
                    return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً"));
                }

                if (!CanOpen(context))
                {
                    Response.StatusCode = 403;
                    return Json(Fail("ليست لديك صلاحية متابعة KYC والبنك"));
                }

                var table = LoadExportTable(request, context);
                return Json(new
                {
                    success = true,
                    columns = ToColumns(table),
                    rows = ToRows(table).Take(100).ToList(),
                    totalRows = table.Rows.Count
                });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(Fail("تعذر تحميل بيانات متابعة KYC والبنك", ex.Message));
            }
        }

        [HttpPost]
        public JsonResult Report(PosKycReportRequest request)
        {
            try
            {
                var context = GetPosContext();
                if (context == null)
                {
                    Response.StatusCode = 401;
                    return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً"));
                }

                if (!CanOpen(context))
                {
                    Response.StatusCode = 403;
                    return Json(Fail("ليست لديك صلاحية متابعة KYC"));
                }

                return Json(new { success = true, data = _repository.GetKycReport(request, context) });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(Fail("تعذر تحميل تقرير KYC", ex.Message));
            }
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult ExportReport(PosKycReportRequest request)
        {
            try
            {
                var context = GetPosContext();
                if (context == null)
                {
                    Response.StatusCode = 401;
                    return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً"), JsonRequestBehavior.AllowGet);
                }

                if (!CanOpen(context))
                {
                    Response.StatusCode = 403;
                    return Json(Fail("ليست لديك صلاحية متابعة KYC"), JsonRequestBehavior.AllowGet);
                }

                request = request ?? new PosKycReportRequest();
                request.Page = 1;
                request.PageSize = 100000;
                var report = _repository.GetKycReport(request, context);
                var bytes = BuildExcel(BuildKycReportTable(report.Rows));
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "KYC_Report_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx");
            }
            catch (Exception ex)
            {
                return Json(Fail("تعذر تصدير تقرير KYC: " + ex.Message, ex.ToString()), JsonRequestBehavior.AllowGet);
            }
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult PrintReport(PosKycReportRequest request)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!CanOpen(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية متابعة KYC");
            }

            request = request ?? new PosKycReportRequest();
            request.Page = 1;
            request.PageSize = 100000;
            var report = _repository.GetKycReport(request, context);
            return Content(BuildKycPrintHtml(report), "text/html", System.Text.Encoding.UTF8);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Export(KycBankFollowUpRequest request)
        {
            try
            {
                var context = GetPosContext();
                if (context == null)
                {
                    Response.StatusCode = 401;
                    return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً"), JsonRequestBehavior.AllowGet);
                }

                if (!CanOpen(context))
                {
                    Response.StatusCode = 403;
                    return Json(Fail("ليست لديك صلاحية متابعة KYC والبنك"), JsonRequestBehavior.AllowGet);
                }

                var table = LoadExportTable(request, context);
                var bytes = BuildExcel(table);
                var fileName = string.Format("Bulk Maintenance{0}.xlsx", DateTime.Now.ToString("yyyyMMddHHmmss"));
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return Json(Fail("تعذر تصدير Excel: " + ex.Message, ex.ToString()), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult KycFolder(KycBankFollowUpRequest request)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(Fail("يجب تسجيل دخول نقطة البيع أولاً"));
            }

            if (!CanOpen(context))
            {
                Response.StatusCode = 403;
                return Json(Fail("ليست لديك صلاحية متابعة KYC والبنك"));
            }

            var date = request != null && request.FromDate.HasValue ? request.FromDate.Value.Date : DateTime.Today;
            var root = GetKycAttachmentRootPath();
            var folderName = date.ToString("yyyyMMdd");
            var folderPath = Path.Combine(root, folderName);
            return Json(new
            {
                success = true,
                folderName = folderName,
                path = folderPath,
                exists = Directory.Exists(folderPath),
                message = Directory.Exists(folderPath)
                    ? "تم تحديد فولدر KYC حسب تاريخ من"
                    : "فولدر KYC غير موجود لهذا التاريخ"
            });
        }

        private DataTable LoadExportTable(KycBankFollowUpRequest request, PosUserContext context)
        {
            request = request ?? new KycBankFollowUpRequest();
            var from = request.FromDate.GetValueOrDefault(DateTime.Today).Date;
            var to = request.ToDate.GetValueOrDefault(from).Date;
            if (to < from)
            {
                var temp = from;
                from = to;
                to = temp;
            }

            var branchId = IsAdmin(context)
                ? (request.BranchId.HasValue && request.BranchId.Value > 0 ? request.BranchId : null)
                : context.BranchId;
            return _repository.RunKycBankExport(ResolveCardLength(request), from, to, branchId, IsAdmin(context));
        }

        private static DataTable BuildKycReportTable(IEnumerable<PosKycReportRowDto> rows)
        {
            var table = new DataTable("KYC Reports");
            table.Columns.Add("CustomerNameArabic");
            table.Columns.Add("CustomerNameEnglish");
            table.Columns.Add("NationalId");
            table.Columns.Add("Phone");
            table.Columns.Add("TokenCardNumber");
            table.Columns.Add("BranchName");
            table.Columns.Add("CreatedDate");
            table.Columns.Add("LastUpdateDate");
            table.Columns.Add("HasInvoice");
            table.Columns.Add("InvoiceCount");
            table.Columns.Add("LastInvoiceDate");
            table.Columns.Add("KycStatus");
            table.Columns.Add("HasAttachments");

            foreach (var row in rows ?? Enumerable.Empty<PosKycReportRowDto>())
            {
                table.Rows.Add(
                    row.ArabicName ?? string.Empty,
                    row.EnglishName ?? string.Empty,
                    row.NationalId ?? string.Empty,
                    row.Phone ?? string.Empty,
                    row.TokenCardNumber ?? string.Empty,
                    row.BranchName ?? string.Empty,
                    FormatDate(row.CreatedDate),
                    FormatDate(row.LastUpdateDate),
                    row.HasInvoice ? "نعم" : "لا",
                    row.InvoiceCount.ToString(),
                    FormatDate(row.LastInvoiceDate),
                    row.KycStatus ?? string.Empty,
                    row.HasAttachments ? "نعم" : "لا");
            }

            return table;
        }

        private static string BuildKycPrintHtml(PosKycReportResult result)
        {
            result = result ?? new PosKycReportResult { Summary = new PosKycReportSummaryDto(), Rows = new List<PosKycReportRowDto>() };
            var rows = result.Rows ?? new List<PosKycReportRowDto>();
            var html = new System.Text.StringBuilder();
            html.Append("<!doctype html><html lang=\"ar\" dir=\"rtl\"><head><meta charset=\"utf-8\"><title>تقارير KYC</title>");
            html.Append("<style>body{font-family:Tahoma,Arial,sans-serif;margin:20px;color:#172033}h1{font-size:22px}.cards{display:grid;grid-template-columns:repeat(4,1fr);gap:8px;margin:12px 0}.card{border:1px solid #dbe5f2;border-radius:8px;padding:10px;background:#f8fbff}.card small{display:block;color:#64748b}.card strong{font-size:20px}table{width:100%;border-collapse:collapse;font-size:12px}th,td{border:1px solid #d8e1ee;padding:7px;text-align:right}th{background:#eef3fb}@media print{button{display:none}.card{box-shadow:none}}</style>");
            html.Append("</head><body><button onclick=\"window.print()\">طباعة</button><h1>تقارير KYC</h1><div class=\"cards\">");
            html.Append(Card("إجمالي العملاء", result.Summary.TotalCustomers));
            html.Append(Card("لديهم فواتير", result.Summary.WithInvoices));
            html.Append(Card("بدون فواتير", result.Summary.WithoutInvoices));
            html.Append(Card("بيانات ناقصة", result.Summary.MissingRequiredData));
            html.Append("</div><table><thead><tr><th>اسم العميل عربي</th><th>English</th><th>الرقم القومي</th><th>الهاتف</th><th>التوكن/الكارت</th><th>الفرع</th><th>تاريخ الإنشاء</th><th>آخر تحديث</th><th>له فاتورة</th><th>عدد الفواتير</th><th>آخر فاتورة</th><th>الحالة</th></tr></thead><tbody>");
            foreach (var row in rows)
            {
                html.Append("<tr>");
                html.Append(Td(row.ArabicName));
                html.Append(Td(row.EnglishName));
                html.Append(Td(row.NationalId));
                html.Append(Td(row.Phone));
                html.Append(Td(row.TokenCardNumber));
                html.Append(Td(row.BranchName));
                html.Append(Td(FormatDate(row.CreatedDate)));
                html.Append(Td(FormatDate(row.LastUpdateDate)));
                html.Append(Td(row.HasInvoice ? "نعم" : "لا"));
                html.Append(Td(row.InvoiceCount.ToString()));
                html.Append(Td(FormatDate(row.LastInvoiceDate)));
                html.Append(Td(row.KycStatus));
                html.Append("</tr>");
            }
            html.Append("</tbody></table><script>window.onload=function(){setTimeout(function(){window.print();},300);};</script></body></html>");
            return html.ToString();
        }

        private static string Card(string title, int value)
        {
            return "<div class=\"card\"><small>" + HttpUtility.HtmlEncode(title) + "</small><strong>" + value.ToString() + "</strong></div>";
        }

        private static string Td(string value)
        {
            return "<td>" + HttpUtility.HtmlEncode(value ?? string.Empty) + "</td>";
        }

        private static string FormatDate(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("yyyy-MM-dd HH:mm") : string.Empty;
        }

        private static int ResolveCardLength(KycBankFollowUpRequest request)
        {
            var value = request != null ? request.CardLength.GetValueOrDefault(18) : 18;
            return value == 8 ? 8 : 18;
        }

        private static bool CanOpen(PosUserContext context)
        {
            return IsAdmin(context) || (context != null && context.IsFullAccessCustomerService);
        }

        private static bool IsAdmin(PosUserContext context)
        {
            return context != null && (context.UserType.GetValueOrDefault(-1) == 0 || context.IsFullAccess);
        }

        private PosUserContext GetPosContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _repository);
        }

        private static string GetKycAttachmentRootPath()
        {
            var configuredPath = ConfigurationManager.AppSettings["PosKycAttachmentRootPath"];
            return string.IsNullOrWhiteSpace(configuredPath) ? @"C:\Dynamic Byte\Doc" : configuredPath.Trim();
        }

        private static object Fail(string message, string technicalMessage = "")
        {
            return new { success = false, message = message, technicalMessage = technicalMessage };
        }

        private static IEnumerable<object> ToColumns(DataTable table)
        {
            return table.Columns.Cast<DataColumn>().Select(c => new { Key = c.ColumnName, Title = MapColumnTitle(c.ColumnName) });
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
                { "Token", "Token" },
                { "EmbossingName", "embossing Name(25)" },
                { "ExtensionName", "extension Name(25)" },
                { "MagstripeName", "magstripe Name(25)" },
                { "NationalId", "national id(14)" },
                { "Address1", "address1 (35)" },
                { "Address2", "address2 (35)" },
                { "Address3", "address3 (35)" },
                { "SmsFlag", "sms Flag(1)" },
                { "MobileNumber", "Mobile Number(10)" },
                { "BirthDate", "birth date (10)" },
                { "FullEnglishName", "Full English Name" },
                { "FullArabicName", "Full Arabic Name" },
                { "OperationBranchCode", "كود فرع العملية" },
                { "OperationBranchName", "اسم فرع العملية" },
                { "BranchName", "الفرع" },
                { "BranchCode", "كود الفرع" },
                { "UserName", "المستخدم" },
                { "EmpCode", "كود الموظف" },
                { "EmpName", "الموظف" },
                { "OrderDate", "تاريخ الطلب" },
                { "CardNo", "رقم الكارت" }
            };
            var reportMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "CustomerNameArabic", "اسم العميل عربي" },
                { "CustomerNameEnglish", "اسم العميل English" },
                { "Phone", "الهاتف" },
                { "TokenCardNumber", "رقم التوكن/الكارت" },
                { "CreatedDate", "تاريخ الإنشاء" },
                { "LastUpdateDate", "آخر تحديث" },
                { "HasInvoice", "له فاتورة" },
                { "InvoiceCount", "عدد الفواتير" },
                { "LastInvoiceDate", "آخر فاتورة" },
                { "KycStatus", "حالة KYC" },
                { "HasAttachments", "له مرفقات" }
            };
            if (reportMap.ContainsKey(name))
            {
                return reportMap[name];
            }

            return map.ContainsKey(name) ? map[name] : name;
        }

        private static byte[] BuildExcel(DataTable table)
        {
            using (var stream = new MemoryStream())
            {
                using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    var workbookPart = document.AddWorkbookPart();
                    workbookPart.Workbook = new Workbook();
                    AddWorkbookStyles(workbookPart);
                    var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                    var sheetData = new SheetData();
                    worksheetPart.Worksheet = new Worksheet(
                        new SheetViews(new SheetView { WorkbookViewId = 0U, RightToLeft = true }),
                        BuildColumns(table.Columns.Count),
                        sheetData);

                    var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                    sheets.Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Sheet1" });

                    // Matches VB6 FrmCustCash.btnExport: bank template data starts at row 3.
                    AppendRow(sheetData, table.Columns.Cast<DataColumn>().Select(c => MapColumnTitle(c.ColumnName)), 1U);
                    AppendRow(sheetData, Enumerable.Repeat(string.Empty, table.Columns.Count), 2U);
                    foreach (DataRow row in table.Rows)
                    {
                        AppendRow(sheetData, table.Columns.Cast<DataColumn>().Select(c => row[c] == DBNull.Value ? string.Empty : Convert.ToString(row[c])), 3U);
                    }

                    workbookPart.Workbook.Save();
                }

                return stream.ToArray();
            }
        }

        private static Columns BuildColumns(int count)
        {
            var columns = new Columns();
            for (uint i = 1; i <= count; i++)
            {
                columns.Append(new Column { Min = i, Max = i, Width = i == 12 || i == 13 ? 34D : 18D, CustomWidth = true });
            }
            return columns;
        }

        private static void AddWorkbookStyles(WorkbookPart workbookPart)
        {
            var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
            stylesPart.Stylesheet = new Stylesheet(
                new Fonts(
                    new Font(),
                    new Font(new Bold())),
                new Fills(
                    new Fill(new PatternFill { PatternType = PatternValues.None }),
                    new Fill(new PatternFill { PatternType = PatternValues.Gray125 })),
                new Borders(new Border()),
                new CellFormats(
                    new CellFormat(),
                    new CellFormat { FontId = 1U, ApplyFont = true, Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, ReadingOrder = 2U } },
                    new CellFormat { ApplyAlignment = true, Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Left, ReadingOrder = 2U } }));
            stylesPart.Stylesheet.Save();
        }

        private static void AppendRow(SheetData sheetData, IEnumerable<string> values, uint styleIndex)
        {
            var row = new Row();
            foreach (var value in values)
            {
                row.Append(new Cell
                {
                    StyleIndex = styleIndex,
                    DataType = CellValues.InlineString,
                    InlineString = new InlineString(new Text(value ?? string.Empty))
                });
            }
            sheetData.Append(row);
        }
    }

    public class KycBankFollowUpRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? BranchId { get; set; }
        public int? CardLength { get; set; }
    }
}
