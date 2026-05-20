using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.Reports;
using MyERP.Common.AccountingReports;
using MyERP.Models.Reports;
using System;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace MyERP.Areas.MainErp.Controllers
{
    public class AccountingReportsController : MainErpControllerBase
    {
        private readonly MainErpDbConnectionFactory _connectionFactory;
        private readonly SharedAccountingReportRepository _reportRepository;
        private readonly SharedAccountingReportService _service;
        private readonly AccountingReportRepository _legacyRepository;

        public AccountingReportsController()
        {
            _connectionFactory = new MainErpDbConnectionFactory();
            _reportRepository = new SharedAccountingReportRepository(CreateMainConnection);
            _service = new SharedAccountingReportService(_reportRepository);
            _legacyRepository = new AccountingReportRepository(_connectionFactory);
        }

        public ActionResult Index(HtmlReportFilterModel filter)
        {
            ViewBag.ActiveScreen = "accounting-reports";
            ViewBag.AccountingReportsArea = "MainErp";
            return View("~/Areas/Pos/Views/AccountingReports/Index.cshtml", _service.BuildPage(filter, null));
        }

        [HttpPost]
        public ActionResult Search(HtmlReportFilterModel filter)
        {
            ViewBag.ActiveScreen = "accounting-reports";
            ViewBag.AccountingReportsArea = "MainErp";
            var model = _service.BuildPage(filter, null);
            if (model.ActiveReport == null)
            {
                model.Message = "اختر التقرير أولا";
                return View("~/Areas/Pos/Views/AccountingReports/Index.cshtml", model);
            }

            model.Result = _service.Run(model.Filter, model.ActiveReport, GetUserId(), true);
            return View("~/Areas/Pos/Views/AccountingReports/Index.cshtml", model);
        }

        [HttpPost]
        public ActionResult Export(HtmlReportFilterModel filter)
        {
            var model = _service.BuildPage(filter, null);
            if (model.ActiveReport == null)
            {
                return new HttpStatusCodeResult(400, "اختر التقرير أولا");
            }

            model.Result = _service.Run(model.Filter, model.ActiveReport, GetUserId(), true);
            var from = model.Filter.FromDate.GetValueOrDefault(DateTime.Today).ToString("yyyyMMdd");
            var to = model.Filter.ToDate.GetValueOrDefault(DateTime.Today).ToString("yyyyMMdd");
            return File(SharedAccountingReportExcelExporter.Build(model), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", SharedAccountingReportExcelExporter.SafeFileName(model.ActiveReport.Title) + "_" + from + "_" + to + ".xlsx");
        }

        public ActionResult JournalEntries(DateTime? fromDate, DateTime? toDate, int? branchId, string accountCode, int? noteType)
        {
            ViewBag.ActiveScreen = "accounting-reports";
            return View(_legacyRepository.GetJournalEntries(fromDate, toDate, branchId, accountCode, noteType));
        }

        public ActionResult AccountMovement(DateTime? fromDate, DateTime? toDate, string accountCode, int? branchId)
        {
            ViewBag.ActiveScreen = "account-movement";
            return View(_legacyRepository.GetAccountMovement(fromDate, toDate, accountCode, branchId));
        }

        [HttpGet]
        public ActionResult AccountTree(string parentCode, string term)
        {
            return Json(_reportRepository.GetAccountTree(parentCode, term), JsonRequestBehavior.AllowGet);
        }

        private SqlConnection CreateMainConnection()
        {
            return _connectionFactory.CreateOpenConnection();
        }

        private int GetUserId()
        {
            return MainErpUserContext == null ? 0 : MainErpUserContext.UserId;
        }
    }
}
