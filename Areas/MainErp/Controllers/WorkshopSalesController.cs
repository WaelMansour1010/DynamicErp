using System;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.SalesInvoices;
using MyERP.Areas.MainErp.ViewModels.SalesInvoices;

namespace MyERP.Areas.MainErp.Controllers
{
    public class WorkshopSalesController : MainErpControllerBase
    {
        private readonly SalesInvoiceReadRepository _repository;

        public WorkshopSalesController()
            : this(new SalesInvoiceReadRepository(new MainErpDbConnectionFactory()))
        {
        }

        public WorkshopSalesController(SalesInvoiceReadRepository repository)
        {
            _repository = repository;
        }

        public ActionResult Index(string searchText, DateTime? fromDate, DateTime? toDate, int? branchId, int page = 1)
        {
            ViewBag.ActiveScreen = "workshop-sales";
            const int pageSize = 20;
            var data = _repository.Search(MainErpSalesInvoiceKind.Workshop, searchText, fromDate, toDate, branchId, page, pageSize);
            var model = new SalesInvoiceIndexViewModel
            {
                Kind = MainErpSalesInvoiceKind.Workshop,
                ArabicTitle = "فاتورة مبيعات الورشة",
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

            return View(model);
        }

        public ActionResult Details(int id)
        {
            ViewBag.ActiveScreen = "workshop-sales";
            return View(_repository.GetDetails(MainErpSalesInvoiceKind.Workshop, id));
        }

        public ActionResult Report(int id)
        {
            ViewBag.ActiveScreen = "workshop-sales";
            return View(_repository.GetDetails(MainErpSalesInvoiceKind.Workshop, id));
        }
    }
}
