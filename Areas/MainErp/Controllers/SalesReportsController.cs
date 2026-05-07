using System.Web.Mvc;
using System;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.Reports;

namespace MyERP.Areas.MainErp.Controllers
{
    public class SalesReportsController : MainErpControllerBase
    {
        private readonly SalesReportRepository _repository;

        public SalesReportsController()
            : this(new SalesReportRepository(new MainErpDbConnectionFactory()))
        {
        }

        public SalesReportsController(SalesReportRepository repository)
        {
            _repository = repository;
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult SalesSummary(DateTime? fromDate, DateTime? toDate, int? branchId, int? userId, int? customerId)
        {
            return View(_repository.GetSalesSummary(fromDate, toDate, branchId, userId, customerId));
        }
    }
}
