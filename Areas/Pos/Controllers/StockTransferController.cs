using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class StockTransferController : Controller
    {
        private readonly PosSqlRepository _repository;

        public StockTransferController()
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
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية فتح سند تحويل المخزون");
            }

            ViewBag.Context = context;
            ViewBag.Branches = context.IsFullAccess
                ? _repository.GetBranches()
                : new[] { new PosBranchDto { BranchId = context.BranchId.GetValueOrDefault(), BranchName = context.BranchName } };
            ViewBag.Stores = _repository.GetStoresByBranch(context.IsFullAccess ? (int?)null : context.BranchId);
            return View();
        }

        [HttpGet]
        public JsonResult Lookup(string term)
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
                return Json(new { success = false, message = "ليست لديك صلاحية فتح سند تحويل المخزون" }, JsonRequestBehavior.AllowGet);
            }

            term = (term ?? string.Empty).Trim();
            if (term.Length < 1)
            {
                return Json(new { success = true, rows = new object[0] }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, rows = _repository.GetStockTransferItems(term) }, JsonRequestBehavior.AllowGet);
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
                return Json(new { success = false, message = "ليست لديك صلاحية فتح سند تحويل المخزون" }, JsonRequestBehavior.AllowGet);
            }

            var resolvedBranchId = context.IsFullAccess ? branchId : context.BranchId;
            return Json(new { success = true, rows = _repository.GetStoresByBranch(resolvedBranchId) }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Search(PosStockTransferSearchRequestDto request)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "انتهت الجلسة، برجاء تسجيل الدخول مرة أخرى" });
            }

            if (!CanOpen(context))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية عرض سندات تحويل المخزون" });
            }

            var validationMessage = ValidateStockSearch(request);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = validationMessage });
            }

            var rows = _repository.SearchStockTransfers(request, context);
            return Json(new
            {
                success = true,
                rows = rows.Select(x => new
                {
                    x.SourceTransactionId,
                    x.DestinationTransactionId,
                    x.VoucherNumber,
                    TransferDate = x.TransferDate.HasValue ? x.TransferDate.Value.ToString("dd/MM/yyyy") : "",
                    x.SourceStoreName,
                    x.DestinationStoreName,
                    x.ItemCount,
                    x.TotalQuantity
                })
            });
        }

        [HttpGet]
        public JsonResult Get(int sourceTransactionId)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "انتهت الجلسة، برجاء تسجيل الدخول مرة أخرى" }, JsonRequestBehavior.AllowGet);
            }

            if (!CanOpen(context))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية عرض سندات تحويل المخزون" }, JsonRequestBehavior.AllowGet);
            }

            var detail = _repository.GetStockTransferDetail(sourceTransactionId, context);
            if (detail == null)
            {
                Response.StatusCode = 404;
                return Json(new { success = false, message = "لم يتم العثور على السند أو لا تملك صلاحية عرضه" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                success = true,
                transfer = new
                {
                    detail.SourceTransactionId,
                    detail.DestinationTransactionId,
                    detail.VoucherNumber,
                    TransferDate = detail.TransferDate.ToString("yyyy-MM-dd"),
                    detail.BranchId,
                    detail.SourceStoreId,
                    detail.DestinationStoreId,
                    detail.Remarks,
                    detail.Items
                }
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ImportSerials(PosStockTransferImportRequestDto request)
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
                return Json(new { success = false, message = "ليست لديك صلاحية استيراد السيريالات" });
            }

            ForceContext(request, context);

            try
            {
                var result = _repository.ImportStockTransferSerials(request);
                return Json(new { success = true, result = result });
            }
            catch (SqlException ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "حدث خطأ أثناء فحص السيريالات", technicalMessage = ex.Message });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult AvailableSerials(PosStockTransferSerialSearchRequestDto request)
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
                return Json(new { success = false, message = "ليست لديك صلاحية عرض سيريالات المخزن" });
            }

            ForceContext(request, context);

            try
            {
                var rows = _repository.SearchStockTransferAvailableSerials(request);
                var totalRows = rows.Count == 0 ? 0 : rows[0].TotalRows;
                var pageSize = request.PageSize <= 0 ? 50 : request.PageSize;
                if (pageSize > 500) { pageSize = 500; }
                return Json(new
                {
                    success = true,
                    rows = rows,
                    totalRows = totalRows,
                    page = request.Page <= 0 ? 1 : request.Page,
                    pageSize = pageSize
                });
            }
            catch (SqlException ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "حدث خطأ أثناء تحميل السيريالات المتاحة", technicalMessage = FixArabicMojibakeForDisplay(ex.Message) });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ImportExcel(HttpPostedFileBase file, int branchId, int sourceStoreId, DateTime? transferDate)
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
                return Json(new { success = false, message = "ليست لديك صلاحية استيراد السيريالات" });
            }

            if (file == null || file.ContentLength == 0)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = "اختر ملف Excel أولاً" });
            }

            try
            {
                var request = new PosStockTransferImportRequestDto
                {
                    BranchId = branchId,
                    SourceStoreId = sourceStoreId,
                    TransferDate = transferDate.GetValueOrDefault(DateTime.Today),
                    Serials = _repository.ReadSerialsFromExcel(file.InputStream)
                };
                ForceContext(request, context);

                var result = _repository.ImportStockTransferSerials(request);
                return Json(new { success = true, result = result });
            }
            catch (SqlException ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "حدث خطأ أثناء فحص ملف Excel", technicalMessage = ex.Message });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult Save(PosStockTransferRequestDto request)
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
                return Json(new { success = false, message = "ليست لديك صلاحية حفظ سند تحويل المخزون" });
            }

            ForceContext(request, context);

            try
            {
                var result = _repository.SaveStockTransfer(request, context.UserId, context.EmpId);
                return Json(new { success = true, message = "تم حفظ سند التحويل", result = result });
            }
            catch (SqlException ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "حدث خطأ من قاعدة البيانات أثناء حفظ سند التحويل", technicalMessage = FixArabicMojibakeForDisplay(ex.Message) });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        private static void ForceContext(PosStockTransferRequestDto request, PosUserContext context)
        {
            if (request == null)
            {
                return;
            }

            if (!context.IsFullAccess)
            {
                request.BranchId = context.BranchId.GetValueOrDefault();
            }

            if (request.TransferDate == DateTime.MinValue)
            {
                request.TransferDate = DateTime.Today;
            }
        }

        private static void ForceContext(PosStockTransferImportRequestDto request, PosUserContext context)
        {
            if (request == null)
            {
                return;
            }

            if (!context.IsFullAccess)
            {
                request.BranchId = context.BranchId.GetValueOrDefault();
            }

            if (request.TransferDate == DateTime.MinValue)
            {
                request.TransferDate = DateTime.Today;
            }

            request.Serials = request.Serials == null
                ? Enumerable.Empty<string>().ToList()
                : request.Serials.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();
        }

        private static void ForceContext(PosStockTransferSerialSearchRequestDto request, PosUserContext context)
        {
            if (request == null)
            {
                return;
            }

            if (!context.IsFullAccess)
            {
                request.BranchId = context.BranchId.GetValueOrDefault();
            }

            if (request.TransferDate == DateTime.MinValue)
            {
                request.TransferDate = DateTime.Today;
            }
        }

        private static string FixArabicMojibakeForDisplay(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var fixedValue = value;
            for (var i = 0; i < 2 && LooksLikeArabicMojibake(fixedValue); i++)
            {
                try
                {
                    fixedValue = Encoding.UTF8.GetString(Encoding.GetEncoding(1256).GetBytes(fixedValue));
                }
                catch
                {
                    return value;
                }
            }

            return string.IsNullOrWhiteSpace(fixedValue) ? value : fixedValue;
        }

        private static bool LooksLikeArabicMojibake(string value)
        {
            return !string.IsNullOrEmpty(value)
                && (value.IndexOf('ط') >= 0 || value.IndexOf('ظ') >= 0 || value.IndexOf('€') >= 0);
        }

        private static bool CanOpen(PosUserContext context)
        {
            return context != null && (context.IsFullAccess || (!IsTellerOnly(context) && context.CanSave));
        }

        private static bool IsTellerOnly(PosUserContext context)
        {
            return context != null && context.CanTeller;
        }

        private PosUserContext GetPosContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _repository);
        }

        private static string ValidateStockSearch(PosStockTransferSearchRequestDto request)
        {
            request = request ?? new PosStockTransferSearchRequestDto();
            var voucher = (request.VoucherNumber ?? string.Empty).Trim();
            var itemTerm = (request.ItemOrSerialTerm ?? string.Empty).Trim();
            var hasDateRange = request.FromDate.HasValue && request.ToDate.HasValue;
            if (voucher.Length > 0 && voucher.Length < 3 && !voucher.All(char.IsDigit))
            {
                return "اكتب 3 أحرف على الأقل في رقم السند";
            }

            if (itemTerm.Length > 0 && itemTerm.Length < 3 && !itemTerm.All(char.IsDigit))
            {
                return "اكتب 3 أحرف على الأقل في بحث الصنف أو السيريال";
            }

            if (!hasDateRange && voucher.Length < 3 && itemTerm.Length < 3)
            {
                return "حدد فترة البحث أو اكتب بحثاً محدداً من 3 أحرف على الأقل";
            }

            return string.Empty;
        }
    }
}
