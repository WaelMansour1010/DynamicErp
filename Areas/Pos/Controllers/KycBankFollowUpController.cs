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
            return context != null && context.UserType.GetValueOrDefault(-1) == 0;
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
