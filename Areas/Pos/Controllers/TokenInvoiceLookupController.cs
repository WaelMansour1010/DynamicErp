using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using MyERP.Areas.Pos.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class TokenInvoiceLookupController : Controller
    {
        private const string SessionKey = "PosTokenInvoiceLookup.LastResult";
        private const string UploadSessionKey = "PosTokenInvoiceLookup.Upload";
        private const string LoadedCountSessionKey = "PosTokenInvoiceLookup.LoadedCount";
        private const int DefaultBatchSize = 500;
        private readonly PosSqlRepository _repository;
        private readonly PosTokenInvoiceLookupExcelParser _parser;

        public TokenInvoiceLookupController()
        {
            _repository = new PosSqlRepository();
            _parser = new PosTokenInvoiceLookupExcelParser();
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
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية بحث مبيعات التوكينات");
            }

            ViewBag.PosContext = context;
            ViewBag.Branches = IsAdmin(context)
                ? _repository.GetBranches()
                : new[] { new PosBranchDto { BranchId = context.BranchId.GetValueOrDefault(), BranchName = context.BranchName } };
            return View();
        }

        [HttpPost]
        public JsonResult Analyze(HttpPostedFileBase file)
        {
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.Charset = "utf-8";
            Response.TrySkipIisCustomErrors = true;

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
                    return Json(Fail("ليست لديك صلاحية بحث مبيعات التوكينات"));
                }

                if (file == null || file.ContentLength <= 0)
                {
                    Response.StatusCode = 400;
                    return Json(Fail("اختر ملف Excel يحتوي على التوكينات"));
                }

                var extension = Path.GetExtension(file.FileName ?? string.Empty);
                if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(extension, ".xls", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase))
                {
                    Response.StatusCode = 400;
                    return Json(Fail("صيغة الملف يجب أن تكون xlsx أو xls أو csv أو txt"));
                }

                PosTokenInvoiceLookupUploadResult upload;
                try
                {
                    upload = string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase)
                        ? _parser.ParsePlainText(file.InputStream)
                        : _parser.Parse(file.InputStream);
                }
                catch (Exception ex)
                {
                    Response.StatusCode = 400;
                    return Json(Fail("تعذر قراءة ملف التوكنات. تأكد أن الملف غير مفتوح أو تالف ثم حاول مرة أخرى.", ex.Message));
                }

                if (upload.Tokens.Count == 0)
                {
                    Response.StatusCode = 400;
                    return Json(Fail("لم يتم العثور على توكنات داخل الملف. يجب أن يحتوي الملف على عمود توكنات واحد على الأقل."));
                }

                var result = new PosTokenInvoiceLookupResult
                {
                    Summary = BuildSummary(upload, new List<PosTokenInvoiceLookupRow>())
                };

                Session[UploadSessionKey] = upload;
                Session[SessionKey] = result;
                Session[LoadedCountSessionKey] = 0;

                return LargeJson(new
                {
                    success = true,
                    summary = result.Summary,
                    rows = new List<object>(),
                    paging = BuildPaging(upload, 0, DefaultBatchSize)
                });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(Fail("تعذر إكمال البحث في التوكنات.", ex.Message));
            }
        }

        [HttpPost]
        public JsonResult AnalyzeBatch(int? skip, int? take)
        {
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.Charset = "utf-8";
            Response.TrySkipIisCustomErrors = true;

            try
            {
                var context = GetPosContext();
                if (context == null)
                {
                    Response.StatusCode = 401;
                    return Json(Fail("يجب تسجيل دخول نقطة البيع أولا"));
                }

                if (!CanOpen(context))
                {
                    Response.StatusCode = 403;
                    return Json(Fail("ليست لديك صلاحية بحث مبيعات التوكنات"));
                }

                var upload = Session[UploadSessionKey] as PosTokenInvoiceLookupUploadResult;
                if (upload == null || upload.Tokens == null || upload.Tokens.Count == 0)
                {
                    Response.StatusCode = 400;
                    return Json(Fail("لا توجد دفعة محفوظة للبحث. ارفع الملف مرة أخرى."));
                }

                var start = Math.Max(0, skip.GetValueOrDefault());
                var pageSize = Math.Max(1, Math.Min(500, take.GetValueOrDefault(DefaultBatchSize)));
                var rows = LookupTokenBatch(upload, start, pageSize);
                var batchTokens = new HashSet<string>(
                    upload.Tokens.Skip(start).Take(pageSize).Select(x => x.Token ?? string.Empty),
                    StringComparer.OrdinalIgnoreCase);
                var result = Session[SessionKey] as PosTokenInvoiceLookupResult;
                if (result == null)
                {
                    result = new PosTokenInvoiceLookupResult();
                }

                result.Rows = result.Rows
                    .Where(x => !batchTokens.Contains(x.Token ?? string.Empty))
                    .ToList();

                foreach (var row in rows)
                {
                    result.Rows.Add(row);
                }

                var previouslyLoaded = Session[LoadedCountSessionKey] is int ? (int)Session[LoadedCountSessionKey] : 0;
                var loadedTokens = Math.Max(previouslyLoaded, Math.Min(upload.Tokens.Count, start + batchTokens.Count));
                result.Summary = BuildSummary(upload, result.Rows);
                Session[SessionKey] = result;
                Session[LoadedCountSessionKey] = loadedTokens;

                return LargeJson(new
                {
                    success = true,
                    summary = result.Summary,
                    rows = rows.Select(ToClientRow).ToList(),
                    paging = BuildPaging(upload, loadedTokens, pageSize)
                });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(Fail("تعذر تحميل الدفعة التالية من التوكنات.", ex.Message));
            }
        }

        [HttpPost]
        public JsonResult TokenStory(string token)
        {
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.Charset = "utf-8";
            Response.TrySkipIisCustomErrors = true;

            try
            {
                var context = GetPosContext();
                if (context == null)
                {
                    Response.StatusCode = 401;
                    return Json(Fail("يجب تسجيل دخول نقطة البيع أولا"));
                }

                if (!CanOpen(context))
                {
                    Response.StatusCode = 403;
                    return Json(Fail("ليست لديك صلاحية بحث مبيعات التوكينات"));
                }

                token = NormalizeToken(token);
                if (string.IsNullOrWhiteSpace(token))
                {
                    Response.StatusCode = 400;
                    return Json(Fail("اكتب رقم التوكن أولا"));
                }

                var canViewAll = IsAdmin(context) || context.CanChangeDefaults;
                var result = _repository.GetTokenLifeStory(token, context.BranchId, canViewAll);
                return LargeJson(new
                {
                    success = true,
                    story = ToClientStory(result)
                });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(Fail("تعذر تحميل حركة التوكن", ex.Message));
            }
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Export()
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
                return Json(Fail("ليست لديك صلاحية بحث مبيعات التوكينات"), JsonRequestBehavior.AllowGet);
            }

            var result = Session[SessionKey] as PosTokenInvoiceLookupResult;
            if (result == null || result.Rows == null || result.Rows.Count == 0)
            {
                Response.StatusCode = 400;
                return Json(Fail("لا توجد نتيجة جاهزة للتصدير. ارفع الملف ثم اضغط تحليل أولاً."), JsonRequestBehavior.AllowGet);
            }

            var bytes = BuildExcel(result);
            var fileName = "TokenInvoiceLookup_" + DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + ".xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private static PosTokenInvoiceLookupSummary BuildSummary(PosTokenInvoiceLookupUploadResult upload, IList<PosTokenInvoiceLookupRow> rows)
        {
            rows = rows ?? new List<PosTokenInvoiceLookupRow>();
            var grouped = rows.GroupBy(x => x.Token ?? string.Empty, StringComparer.Ordinal).ToList();
            return new PosTokenInvoiceLookupSummary
            {
                UploadedTokensCount = upload.Summary.UploadedTokensCount,
                UniqueTokensCount = upload.Summary.UniqueTokensCount,
                DuplicatedInUploadedFileCount = upload.Summary.DuplicatedInUploadedFileCount,
                FoundCount = grouped.Count(g => g.Any(x => x.Transaction_ID.HasValue)),
                NotFoundCount = grouped.Count(g => !g.Any(x => x.Transaction_ID.HasValue)),
                DuplicatedInDatabaseCount = grouped.Count(g => g.Any(x => x.DatabaseMatchCount > 1))
            };
        }

        private IList<PosTokenInvoiceLookupRow> LookupTokenBatch(PosTokenInvoiceLookupUploadResult upload, int skip, int take)
        {
            var batch = new PosTokenInvoiceLookupUploadResult();
            foreach (var item in upload.Tokens.Skip(skip).Take(take))
            {
                batch.Tokens.Add(item);
            }

            batch.Summary.UploadedTokensCount = batch.Tokens.Sum(x => x.UploadedCount);
            batch.Summary.UniqueTokensCount = batch.Tokens.Count;
            batch.Summary.DuplicatedInUploadedFileCount = batch.Tokens.Count(x => x.UploadedCount > 1);
            return _repository.LookupTokenInvoices(batch.Tokens);
        }

        private static object BuildPaging(PosTokenInvoiceLookupUploadResult upload, int loadedTokens, int batchSize)
        {
            var total = upload == null || upload.Tokens == null ? 0 : upload.Tokens.Count;
            return new
            {
                totalTokens = total,
                loadedTokens = Math.Min(total, loadedTokens),
                batchSize = batchSize,
                nextSkip = Math.Min(total, loadedTokens),
                hasMore = loadedTokens < total
            };
        }

        private static object ToClientRow(PosTokenInvoiceLookupRow row)
        {
            return new
            {
                token = row.Token,
                searchStatus = row.SearchStatus,
                uploadedDuplicateCount = row.UploadedDuplicateCount,
                databaseMatchCount = row.DatabaseMatchCount,
                transactionId = row.Transaction_ID,
                invoiceDate = row.InvoiceDate.HasValue ? row.InvoiceDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty,
                invoiceNumber = row.InvoiceNumber,
                customerName = row.CustomerName,
                customerPhone = row.CustomerPhone,
                idIpn = row.IdIpn,
                manualNo = row.ManualNo,
                nationalId = row.NationalId,
                branch = row.Branch,
                store = row.Store,
                cashier = row.Cashier,
                serviceType = row.ServiceType,
                notes = row.Notes,
                transactionType = row.TransactionType
            };
        }

        private static object ToClientStory(PosTokenLifeStoryResult result)
        {
            result = result ?? new PosTokenLifeStoryResult();
            return new
            {
                token = result.Token,
                currentStatus = result.CurrentStatus,
                summaryText = result.SummaryText,
                isRestrictedByBranch = result.IsRestrictedByBranch,
                customers = result.Customers.Select(x => new
                {
                    customerId = x.CustomerId,
                    cardNo = x.CardNo,
                    cardId = x.CardId,
                    customerName = x.CustomerName,
                    customerPhone = x.CustomerPhone,
                    nationalId = x.NationalId,
                    branchName = x.BranchName,
                    orderDate = DateTimeText(x.OrderDate),
                    saveDate = DateTimeText(x.SaveDate),
                    easyCashType = x.EasyCashType
                }).ToList(),
                currentStock = result.CurrentStock.Select(x => new
                {
                    storeName = x.StoreName,
                    itemName = x.ItemName,
                    currentQty = x.CurrentQty,
                    lastTransactionId = x.LastTransactionId,
                    lastTransactionDate = DateTimeText(x.LastTransactionDate)
                }).ToList(),
                movements = result.Movements.Select(ToClientMovement).ToList(),
                salesReferences = result.SalesReferences.Select(ToClientMovement).ToList()
            };
        }

        private static object ToClientMovement(PosTokenLifeMovementRow row)
        {
            return new
            {
                transactionId = row.TransactionId,
                transactionDate = DateTimeText(row.TransactionDate),
                transactionTypeId = row.TransactionTypeId,
                transactionTypeName = row.TransactionTypeName,
                movementKind = row.MovementKind,
                stockEffect = row.StockEffect,
                quantity = row.Quantity,
                signedQuantity = row.SignedQuantity,
                invoiceNumber = row.InvoiceNumber,
                branchName = row.BranchName,
                storeName = row.StoreName,
                itemName = row.ItemName,
                customerName = row.CustomerName,
                customerPhone = row.CustomerPhone,
                userName = row.UserName,
                linkedTransactionId = row.LinkedTransactionId,
                isCancelled = row.IsCancelled,
                notes = row.Notes
            };
        }

        private static string DateTimeText(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : string.Empty;
        }

        private static string NormalizeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(value.Trim().Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        }

        private static byte[] BuildExcel(PosTokenInvoiceLookupResult result)
        {
            using (var stream = new MemoryStream())
            {
                using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    var workbookPart = document.AddWorkbookPart();
                    workbookPart.Workbook = new Workbook();
                    var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                    var sheetData = new SheetData();
                    worksheetPart.Worksheet = new Worksheet(
                        new SheetViews(new SheetView { WorkbookViewId = 0U, RightToLeft = true }),
                        BuildColumns(17),
                        sheetData);

                    var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                    sheets.Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Result" });

                    AppendRow(sheetData, new[]
                    {
                        "Token", "حالة البحث", "تكرار في الملف", "تكرار في قاعدة البيانات", "Transaction_ID",
                        "تاريخ الفاتورة", "رقم الفاتورة", "اسم العميل", "هاتف العميل", "ID / IPN",
                        "ManualNO", "الرقم القومي", "الفرع", "المخزن", "المستخدم / الكاشير", "Service Type", "ملاحظات"
                    });

                    foreach (var row in result.Rows)
                    {
                        AppendRow(sheetData, new[]
                        {
                            row.Token,
                            row.SearchStatus,
                            row.UploadedDuplicateCount.ToString(CultureInfo.InvariantCulture),
                            row.DatabaseMatchCount.ToString(CultureInfo.InvariantCulture),
                            row.Transaction_ID.HasValue ? row.Transaction_ID.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                            row.InvoiceDate.HasValue ? row.InvoiceDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty,
                            row.InvoiceNumber,
                            row.CustomerName,
                            row.CustomerPhone,
                            row.IdIpn,
                            row.ManualNo,
                            row.NationalId,
                            row.Branch,
                            row.Store,
                            row.Cashier,
                            row.ServiceType,
                            row.Notes
                        });
                    }

                    worksheetPart.Worksheet.Append(new AutoFilter { Reference = "A1:Q" + Math.Max(1, result.Rows.Count + 1).ToString(CultureInfo.InvariantCulture) });
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
                columns.Append(new Column { Min = i, Max = i, Width = i == 1 ? 26D : 20D, CustomWidth = true });
            }
            return columns;
        }

        private static void AppendRow(SheetData sheetData, IEnumerable<string> values)
        {
            var row = new Row();
            foreach (var value in values)
            {
                row.Append(new Cell
                {
                    DataType = CellValues.InlineString,
                    InlineString = new InlineString(new Text(value ?? string.Empty))
                });
            }

            sheetData.Append(row);
        }

        private static bool CanOpen(PosUserContext context)
        {
            return IsAdmin(context) || (context != null && context.CanViewReports);
        }

        private static bool IsAdmin(PosUserContext context)
        {
            return context != null && (context.UserType.GetValueOrDefault(-1) == 0 || context.IsFullAccess);
        }

        private PosUserContext GetPosContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _repository);
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

        private static object Fail(string message, string technicalMessage = "")
        {
            return new { success = false, message = message, technicalMessage = technicalMessage };
        }
    }
}
