using System;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.SalesInvoices;
using MyERP.Areas.MainErp.ViewModels.SalesInvoices;

namespace MyERP.Areas.MainErp.Controllers
{
    public class PumpSalesController : MainErpControllerBase
    {
        private readonly SalesInvoiceReadRepository _repository;

        public PumpSalesController()
            : this(new SalesInvoiceReadRepository(new MainErpDbConnectionFactory()))
        {
        }

        public PumpSalesController(SalesInvoiceReadRepository repository)
        {
            _repository = repository;
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
            return View("~/Areas/MainErp/Views/WorkshopSales/Details.cshtml", _repository.GetDetails(MainErpSalesInvoiceKind.Pump, id));
        }

        public ActionResult DeferredDistribution(int transactionId, int lineId)
        {
            ViewBag.ActiveScreen = "pump-sales";
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
    }
}
