using System.Web.Mvc;
using System;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.Reports;

namespace MyERP.Areas.MainErp.Controllers
{
    public class AccountingReportsController : MainErpControllerBase
    {
        private readonly AccountingReportRepository _repository;

        public AccountingReportsController()
            : this(new AccountingReportRepository(new MainErpDbConnectionFactory()))
        {
        }

        public AccountingReportsController(AccountingReportRepository repository)
        {
            _repository = repository;
        }

        public ActionResult Index()
        {
            ViewBag.ActiveScreen = "accounting-reports";
            return View();
        }

        public ActionResult JournalEntries(DateTime? fromDate, DateTime? toDate, int? branchId, string accountCode, int? noteType)
        {
            ViewBag.ActiveScreen = "accounting-reports";
            return View(_repository.GetJournalEntries(fromDate, toDate, branchId, accountCode, noteType));
        }

        public ActionResult AccountMovement(DateTime? fromDate, DateTime? toDate, string accountCode, int? branchId)
        {
            ViewBag.ActiveScreen = "account-movement";
            return View(_repository.GetAccountMovement(fromDate, toDate, accountCode, branchId));
        }
    }
}
