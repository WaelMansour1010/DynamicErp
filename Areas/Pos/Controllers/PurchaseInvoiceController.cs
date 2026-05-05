using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class PurchaseInvoiceController : Controller
    {
        private readonly PosSqlRepository _repository;

        public PurchaseInvoiceController()
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
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية فتح فاتورة المشتريات");
            }

            ViewBag.Context = context;
            ViewBag.Branches = context.IsFullAccess ? _repository.GetBranches() : new[] { new PosBranchDto { BranchId = context.BranchId.GetValueOrDefault(), BranchName = context.BranchName } };
            ViewBag.Stores = _repository.GetStoresByBranch(context.IsFullAccess ? (int?)null : context.BranchId);
            ViewBag.PaymentTypes = _repository.GetPaymentTypes();
            ViewBag.Boxes = _repository.GetCashBoxesByUserOrBranch(context.UserId, context.IsFullAccess ? (int?)null : context.BranchId);
            ViewBag.Banks = _repository.GetPosPaymentBanks();
            return View();
        }

        [HttpGet]
        public JsonResult Lookup(string kind, string term)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "يجب تسجيل دخول نقطة البيع أولاً" }, JsonRequestBehavior.AllowGet);
            }

            if (!CanOpen(context))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية فتح فاتورة المشتريات" }, JsonRequestBehavior.AllowGet);
            }

            var normalizedKind = (kind ?? string.Empty).Trim().ToLowerInvariant();
            term = (term ?? string.Empty).Trim();
            if (term.Length < 1)
            {
                return Json(new { success = true, rows = new object[0] }, JsonRequestBehavior.AllowGet);
            }

            if (normalizedKind == "supplier")
            {
                return Json(new
                {
                    success = true,
                    rows = _repository.SearchPurchaseSuppliers(term).Select(x => new { id = x.SupplierId, text = x.SupplierName, extra = x.AccountCode })
                }, JsonRequestBehavior.AllowGet);
            }

            if (normalizedKind == "item")
            {
                return Json(new { success = true, rows = _repository.GetPurchaseItems(term) }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, rows = new object[0] }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult Stores(int? branchId)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "يجب تسجيل دخول نقطة البيع أولاً" }, JsonRequestBehavior.AllowGet);
            }

            if (!CanOpen(context))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية فتح فاتورة المشتريات" }, JsonRequestBehavior.AllowGet);
            }

            var resolvedBranchId = context.IsFullAccess ? branchId : context.BranchId;
            return Json(new { success = true, rows = _repository.GetStoresByBranch(resolvedBranchId) }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult Units(int itemId)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "يجب تسجيل دخول نقطة البيع أولاً" }, JsonRequestBehavior.AllowGet);
            }

            if (!CanOpen(context))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية فتح فاتورة المشتريات" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, rows = _repository.GetPurchaseItemUnits(itemId) }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ImportExcel(HttpPostedFileBase file)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "يجب تسجيل دخول نقطة البيع أولاً" });
            }

            if (!CanOpen(context))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية فتح فاتورة المشتريات" });
            }

            if (file == null || file.ContentLength == 0)
            {
                return Json(new { success = false, message = "اختر ملف Excel للاستيراد" });
            }

            try
            {
                var result = ParsePurchaseImport(file);
                return Json(new { success = true, result = result });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult Save(PosPurchaseInvoiceRequestDto request)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "يجب تسجيل دخول نقطة البيع أولاً" });
            }

            if (!CanOpen(context))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية حفظ فاتورة المشتريات" });
            }

            ForceContext(request, context);
            try
            {
                var result = _repository.SavePurchaseInvoice(request, context.UserId, context.EmpId);
                return Json(new { success = true, message = "تم حفظ فاتورة المشتريات", result = result });
            }
            catch (SqlException ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "حدث خطأ من قاعدة البيانات أثناء حفظ فاتورة المشتريات", technicalMessage = ex.Message });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        private static void ForceContext(PosPurchaseInvoiceRequestDto request, PosUserContext context)
        {
            if (request == null)
            {
                return;
            }

            if (!context.IsFullAccess)
            {
                request.BranchId = context.BranchId.GetValueOrDefault();
                if (!request.BoxId.HasValue)
                {
                    request.BoxId = context.BoxId;
                }
            }

            if (request.InvoiceDate == DateTime.MinValue)
            {
                request.InvoiceDate = DateTime.Today;
            }
        }

        private static bool CanOpen(PosUserContext context)
        {
            return context != null && (context.IsFullAccess || context.CanSave);
        }

        private PosUserContext GetPosContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _repository);
        }

        private PosPurchaseImportResultDto ParsePurchaseImport(HttpPostedFileBase file)
        {
            var result = new PosPurchaseImportResultDto();
            using (var reader = ExcelReaderFactory.CreateReader(file.InputStream))
            {
                var dataSet = reader.AsDataSet();
                if (dataSet == null || dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
                {
                    throw new InvalidOperationException("ملف Excel لا يحتوي على بيانات");
                }

                var table = dataSet.Tables[0];
                var headers = BuildHeaderMap(table);
                var hasHeaders = headers.Count > 0;
                var startRow = hasHeaders ? 1 : 0;
                var importedSerials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (var rowIndex = startRow; rowIndex < table.Rows.Count; rowIndex++)
                {
                    var row = table.Rows[rowIndex];
                    var itemText = ReadImportValue(row, headers, "item", 0);
                    var serial = ReadImportValue(row, headers, "serial", 1);
                    var rowNumber = rowIndex + 1;

                    if (string.IsNullOrWhiteSpace(itemText) && string.IsNullOrWhiteSpace(serial))
                    {
                        continue;
                    }

                    var item = _repository.FindPurchaseItemForImport(itemText);
                    if (item == null)
                    {
                        result.Rejected.Add(new PosPurchaseImportRejectedRowDto { RowNumber = rowNumber, ItemText = itemText, Serial = serial, Reason = "لم يتم العثور على الصنف" });
                        continue;
                    }

                    var quantity = ReadImportDecimal(row, headers, "quantity", 2, item.HaveSerial ? 1 : 1);
                    var price = ReadImportDecimal(row, headers, "price", 3, item.Price);
                    var discount = ReadImportDecimal(row, headers, "discount", 4, 0);
                    var vatPercent = ReadImportDecimal(row, headers, "vatpercent", 5, 0);
                    var vatValue = ReadImportDecimal(row, headers, "vatvalue", 6, 0);
                    if (vatValue <= 0 && vatPercent > 0)
                    {
                        vatValue = Math.Round(((quantity * price) - discount) * (vatPercent / 100), 4);
                    }

                    if (item.HaveSerial)
                    {
                        quantity = 1;
                        if (string.IsNullOrWhiteSpace(serial))
                        {
                            result.Rejected.Add(new PosPurchaseImportRejectedRowDto { RowNumber = rowNumber, ItemText = itemText, Serial = serial, Reason = "السيريال مطلوب للصنف المسلسل" });
                            continue;
                        }

                        if (!importedSerials.Add(serial.Trim()))
                        {
                            result.Rejected.Add(new PosPurchaseImportRejectedRowDto { RowNumber = rowNumber, ItemText = itemText, Serial = serial, Reason = "سيريال مكرر في ملف الاستيراد" });
                            continue;
                        }
                    }

                    if (quantity <= 0 || price < 0 || discount < 0 || vatValue < 0)
                    {
                        result.Rejected.Add(new PosPurchaseImportRejectedRowDto { RowNumber = rowNumber, ItemText = itemText, Serial = serial, Reason = "كمية أو قيمة مالية غير صحيحة" });
                        continue;
                    }

                    result.Accepted.Add(new PosPurchaseInvoiceLineDto
                    {
                        ItemId = item.Item_ID,
                        ItemCode = item.ItemCode,
                        ItemName = item.ItemName,
                        UnitId = item.UnitId,
                        UnitName = item.UnitName,
                        Quantity = quantity,
                        PurchasePrice = price,
                        DiscountValue = discount,
                        VatPercent = vatPercent,
                        VatValue = vatValue,
                        LineTotal = Math.Max(0, (quantity * price) - discount + vatValue),
                        HaveSerial = item.HaveSerial,
                        ItemSerial = string.IsNullOrWhiteSpace(serial) ? null : serial.Trim()
                    });
                }
            }

            return result;
        }

        private static Dictionary<string, int> BuildHeaderMap(DataTable table)
        {
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (table.Rows.Count == 0)
            {
                return headers;
            }

            for (var i = 0; i < table.Columns.Count; i++)
            {
                var header = NormalizeHeader(Convert.ToString(table.Rows[0][i], CultureInfo.InvariantCulture));
                if (!string.IsNullOrWhiteSpace(header) && !headers.ContainsKey(header))
                {
                    headers.Add(header, i);
                }
            }

            return headers;
        }

        private static string ReadImportValue(DataRow row, Dictionary<string, int> headers, string key, int fallbackIndex)
        {
            var index = ResolveImportColumn(headers, key, fallbackIndex);
            if (index < 0 || index >= row.ItemArray.Length)
            {
                return string.Empty;
            }

            return Convert.ToString(row[index], CultureInfo.InvariantCulture).Trim();
        }

        private static decimal ReadImportDecimal(DataRow row, Dictionary<string, int> headers, string key, int fallbackIndex, decimal fallback)
        {
            var value = ReadImportValue(row, headers, key, fallbackIndex);
            decimal parsed;
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed)
                || decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out parsed)
                ? parsed
                : fallback;
        }

        private static int ResolveImportColumn(Dictionary<string, int> headers, string key, int fallbackIndex)
        {
            if (headers == null || headers.Count == 0)
            {
                return fallbackIndex;
            }

            string[] aliases;
            switch (key)
            {
                case "item": aliases = new[] { "item", "itemcode", "code", "barcode", "الصنف", "كودالصنف", "كود", "اسمالصنف" }; break;
                case "serial": aliases = new[] { "serial", "serialnumber", "itemserial", "سيريال", "السيريال", "رقمالسيريال", "رقمسيريال" }; break;
                case "quantity": aliases = new[] { "quantity", "qty", "الكمية", "كمية" }; break;
                case "price": aliases = new[] { "price", "purchaseprice", "unitprice", "السعر", "سعرالشراء" }; break;
                case "discount": aliases = new[] { "discount", "discountvalue", "خصم", "الخصم" }; break;
                case "vatpercent": aliases = new[] { "vatpercent", "vat%", "vat", "ضريبة", "نسبةالضريبة", "ضريبة%" }; break;
                case "vatvalue": aliases = new[] { "vatvalue", "taxvalue", "قيمةالضريبة", "قيمةvat" }; break;
                default: aliases = new[] { key }; break;
            }

            foreach (var alias in aliases)
            {
                int index;
                if (headers.TryGetValue(NormalizeHeader(alias), out index))
                {
                    return index;
                }
            }

            return -1;
        }

        private static string NormalizeHeader(string value)
        {
            return (value ?? string.Empty).Trim().Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
        }
    }
}
