using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using MyERP.Areas.Pos.Services;
using System;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class ExcelImportController : Controller
    {
        private readonly PosSqlRepository _repository;
        private readonly PosExcelImportParser _parser;

        public ExcelImportController()
        {
            _repository = new PosSqlRepository();
            _parser = new PosExcelImportParser();
        }

        [HttpGet]
        public ActionResult Index()
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            ViewBag.PosContext = context;
            ViewBag.ActiveScreen = "excel-import";

            return View(new PosExcelImportIndexViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Preview(HttpPostedFileBase excelFile, string tokenMatchingStrategy, int? branchId, int? storeId, int? paymentTypeId, int? cashInItemId)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            ViewBag.PosContext = context;
            ViewBag.ActiveScreen = "excel-import";

            if (excelFile == null || excelFile.ContentLength == 0)
            {
                return View("Index", new PosExcelImportIndexViewModel());
            }

            var mapping = new PosExcelImportMappingDraft
            {
                BranchId = branchId,
                StoreId = storeId,
                PaymentTypeId = paymentTypeId,
                ImportUserId = context.UserId
            };
            mapping.ServiceItemMap["كاش ان"] = cashInItemId;

            try
            {
                var preview = _parser.Parse(excelFile.InputStream, excelFile.FileName, mapping);
                preview.TokenMatchingStrategy = string.IsNullOrWhiteSpace(tokenMatchingStrategy) ? "Sequential" : tokenMatchingStrategy.Trim();
                return View("Preview", new PosExcelImportPreviewViewModel { Preview = preview });
            }
            catch (Exception ex)
            {
                return View("Preview", new PosExcelImportPreviewViewModel
                {
                    ErrorMessage = "تعذر قراءة ملف Excel: " + ex.Message
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Commit()
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            ViewBag.PosContext = context;
            ViewBag.ActiveScreen = "excel-import";

            return View("Preview", new PosExcelImportPreviewViewModel
            {
                ErrorMessage = "مرحلة الحفظ الفعلي لم يتم تفعيلها بعد. يجب إكمال mapping والتحقق من التوكنات أولاً."
            });
        }

        private PosUserContext GetPosContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _repository);
        }
    }
}
