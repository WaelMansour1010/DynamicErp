using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Data.SqlClient;
using System.Linq;
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
    }
}
