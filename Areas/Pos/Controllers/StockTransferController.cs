using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Data.SqlClient;
using System.Linq;
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
                return Json(new { success = false, message = "حدث خطأ من قاعدة البيانات أثناء حفظ سند التحويل", technicalMessage = ex.Message });
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

        private static bool CanOpen(PosUserContext context)
        {
            return context != null && (context.IsFullAccess || context.CanSave);
        }

        private PosUserContext GetPosContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _repository);
        }
    }
}
