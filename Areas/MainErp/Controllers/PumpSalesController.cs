using System;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.SalesInvoices;
using MyERP.Areas.MainErp.ViewModels.SalesInvoices;

namespace MyERP.Areas.MainErp.Controllers
{
    public class PumpSalesController : MainErpControllerBase
    {
        private const string LegacyScreenName = "FrmSaleBill6";
        private readonly SalesInvoiceReadRepository _repository;
        private readonly LegacyScreenPermissionService _permissionService;

        public PumpSalesController()
            : this(new SalesInvoiceReadRepository(new MainErpDbConnectionFactory()), new LegacyScreenPermissionService(new MainErpDbConnectionFactory()))
        {
        }

        public PumpSalesController(SalesInvoiceReadRepository repository, LegacyScreenPermissionService permissionService)
        {
            _repository = repository;
            _permissionService = permissionService;
        }

        public ActionResult Index(string searchText, DateTime? fromDate, DateTime? toDate, int? branchId, int page = 1)
        {
            ViewBag.ActiveScreen = "pump-sales";
            const int pageSize = 20;
            var data = _repository.Search(MainErpSalesInvoiceKind.Pump, searchText, fromDate, toDate, branchId, page, pageSize);
            var model = new SalesInvoiceIndexViewModel
            {
                Kind = MainErpSalesInvoiceKind.Pump,
                ArabicTitle = "فاتورة مبيعات المضخات",
                SearchText = searchText,
                FromDate = fromDate,
                ToDate = toDate,
                BranchId = branchId,
                Page = page,
                PageSize = pageSize,
                TotalCount = data.TotalCount,
                Warning = data.Warning,
                Diagnostics = data.Diagnostics as SalesInvoiceDiagnosticsViewModel
            };

            foreach (var item in data.Items)
            {
                model.Items.Add(item);
            }

            return View("~/Areas/MainErp/Views/WorkshopSales/Index.cshtml", model);
        }

        public ActionResult Details(int id)
        {
            ViewBag.ActiveScreen = "pump-sales";
            ApplyPermissions();
            return View("~/Areas/MainErp/Views/WorkshopSales/Details.cshtml", _repository.GetDetails(MainErpSalesInvoiceKind.Pump, id));
        }

        public ActionResult DailyReport(int id)
        {
            ViewBag.ActiveScreen = "pump-sales";
            return View(_repository.GetDetails(MainErpSalesInvoiceKind.Pump, id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Post(int id, string command)
        {
            ViewBag.ActiveScreen = "pump-sales";
            var dryRun = string.Equals(command, "preview", StringComparison.OrdinalIgnoreCase)
                || string.Equals(command, "preview-cost", StringComparison.OrdinalIgnoreCase);
            var forceRebuild = string.Equals(command, "rebuild", StringComparison.OrdinalIgnoreCase);
            var includeInventoryCost = string.Equals(command, "preview-cost", StringComparison.OrdinalIgnoreCase)
                || string.Equals(command, "post-cost", StringComparison.OrdinalIgnoreCase);

            try
            {
                if (!CanPost())
                {
                    return new HttpStatusCodeResult(403, "ليست لديك صلاحية ترحيل فواتير المضخات.");
                }

                if (forceRebuild && !CanRebuild())
                {
                    return new HttpStatusCodeResult(403, "ليست لديك صلاحية إعادة بناء قيود فواتير المضخات.");
                }

                if (includeInventoryCost && !dryRun && !CanPostWithCost())
                {
                    return new HttpStatusCodeResult(403, "ليست لديك صلاحية ترحيل فاتورة المضخات مع قيد تكلفة المخزون.");
                }

                var result = _repository.PostPumpInvoice(id, CurrentUserId(), forceRebuild, dryRun, includeInventoryCost);
                TempData[result.Success ? "MainErpSuccess" : "MainErpWarning"] = result.Message;
            }
            catch (Exception ex)
            {
                TempData["MainErpWarning"] = ex.Message;
            }

            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteDraft(int id, string command)
        {
            ViewBag.ActiveScreen = "pump-sales";
            var dryRun = string.Equals(command, "preview", StringComparison.OrdinalIgnoreCase);

            try
            {
                if (!CanDeleteDraft())
                {
                    return new HttpStatusCodeResult(403, "ليست لديك صلاحية حذف مسودة فاتورة المضخات.");
                }

                var result = _repository.DeletePumpInvoiceDraft(id, CurrentUserId(), dryRun);
                TempData[result.Success ? "MainErpSuccess" : "MainErpWarning"] = result.Message;

                if (result.Success && !dryRun)
                {
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["MainErpWarning"] = ex.Message;
            }

            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelPreview(int id)
        {
            return CancelPosted(id, "preview");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelPosted(int id, string command)
        {
            ViewBag.ActiveScreen = "pump-sales";
            var dryRun = !string.Equals(command, "cancel", StringComparison.OrdinalIgnoreCase);

            try
            {
                if (!CanCancelPosted())
                {
                    return new HttpStatusCodeResult(403, "ليست لديك صلاحية معاينة إلغاء فواتير المضخات المرحلة.");
                }

                var result = _repository.CancelPumpInvoice(id, CurrentUserId(), dryRun);
                TempData[result.Success ? "MainErpSuccess" : "MainErpWarning"] = result.Message;
            }
            catch (Exception ex)
            {
                TempData["MainErpWarning"] = ex.Message;
            }

            return RedirectToAction("Details", new { id });
        }

        public ActionResult New()
        {
            ViewBag.ActiveScreen = "pump-sales";
            if (!CanEdit())
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية إنشاء فاتورة مضخات.");
            }

            return View("Edit", _repository.GetPumpEdit(null));
        }

        public ActionResult Edit(int id)
        {
            ViewBag.ActiveScreen = "pump-sales";
            if (!CanEdit())
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية تعديل فاتورة المضخات.");
            }

            return View(_repository.GetPumpEdit(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(PumpSalesEditViewModel model, string command)
        {
            ViewBag.ActiveScreen = "pump-sales";

            if (model == null)
            {
                return RedirectToAction("Index");
            }

            var dryRun = string.Equals(command, "preview", StringComparison.OrdinalIgnoreCase);
            try
            {
                if (!CanEdit())
                {
                    return new HttpStatusCodeResult(403, "ليست لديك صلاحية حفظ فاتورة المضخات.");
                }

                var result = _repository.SavePumpInvoiceDraft(model, dryRun);
                TempData[result.Success ? "MainErpSuccess" : "MainErpWarning"] = result.Message;

                if (result.Success && !dryRun && result.TransactionId.HasValue)
                {
                    return RedirectToAction("Details", new { id = result.TransactionId.Value });
                }

                var refreshed = model.TransactionId.HasValue && model.TransactionId.Value > 0
                    ? _repository.GetPumpEdit(model.TransactionId.Value)
                    : _repository.GetPumpEdit(null);
                refreshed.Message = result.Message;
                return View(refreshed);
            }
            catch (Exception ex)
            {
                var refreshed = model.TransactionId.HasValue && model.TransactionId.Value > 0
                    ? _repository.GetPumpEdit(model.TransactionId.Value)
                    : _repository.GetPumpEdit(null);
                refreshed.Message = ex.Message;
                return View(refreshed);
            }
        }

        public ActionResult DeferredDistribution(int transactionId, int lineId)
        {
            ViewBag.ActiveScreen = "pump-sales";
            if (!CanEdit())
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية تعديل توزيع الآجل.");
            }

            return View(_repository.GetPumpDeferredDistribution(transactionId, lineId));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeferredDistribution(PumpDeferredDistributionEditViewModel model, string command)
        {
            ViewBag.ActiveScreen = "pump-sales";

            if (model == null)
            {
                return RedirectToAction("Index");
            }

            var dryRun = string.Equals(command, "preview", StringComparison.OrdinalIgnoreCase);
            try
            {
                if (!CanEdit())
                {
                    return new HttpStatusCodeResult(403, "ليست لديك صلاحية حفظ توزيع الآجل.");
                }

                var result = _repository.SavePumpDeferredDistribution(model, dryRun);
                TempData[result.Success ? "MainErpSuccess" : "MainErpWarning"] = result.Message;

                if (result.Success && !dryRun)
                {
                    return RedirectToAction("Details", new { id = model.TransactionId });
                }

                var refreshed = _repository.GetPumpDeferredDistribution(model.TransactionId, model.LineId);
                refreshed.Message = result.Message;
                return View(refreshed);
            }
            catch (Exception ex)
            {
                var refreshed = _repository.GetPumpDeferredDistribution(model.TransactionId, model.LineId);
                refreshed.Message = ex.Message;
                return View(refreshed);
            }
        }

        private void ApplyPermissions()
        {
            ViewBag.CanEditPumpSales = CanEdit();
            ViewBag.CanPostPumpSales = CanPost();
            ViewBag.CanRebuildPumpSales = CanRebuild();
            ViewBag.CanDeletePumpSalesDraft = CanDeleteDraft();
            ViewBag.CanPostPumpSalesWithCost = CanPostWithCost();
            ViewBag.CanCancelPumpSalesPosted = CanCancelPosted();
        }

        private bool CanEdit()
        {
            return _permissionService.CanEdit(MainErpUserContext, LegacyScreenName)
                || _permissionService.CanAdd(MainErpUserContext, LegacyScreenName);
        }

        private bool CanDeleteDraft()
        {
            return _permissionService.CanDelete(MainErpUserContext, LegacyScreenName);
        }

        private bool CanPost()
        {
            return MainErpUserContext != null
                && (MainErpUserContext.IsAdmin || MainErpUserContext.CanPostPumpInvoice);
        }

        private bool CanRebuild()
        {
            return MainErpUserContext != null
                && MainErpUserContext.IsAdmin
                && MainErpUserContext.CanPostPumpInvoice;
        }

        private bool CanPostWithCost()
        {
            return MainErpUserContext != null
                && MainErpUserContext.IsAdmin
                && MainErpUserContext.CanPostPumpInvoice;
        }

        private bool CanCancelPosted()
        {
            return MainErpUserContext != null
                && MainErpUserContext.IsAdmin
                && MainErpUserContext.CanPostPumpInvoice;
        }

        private int? CurrentUserId()
        {
            return MainErpUserContext == null ? null : (int?)MainErpUserContext.UserId;
        }
    }
}
