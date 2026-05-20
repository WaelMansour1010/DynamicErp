using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using MyERP.Common.AccountingReports;
using MyERP.Models.Reports;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class AccountingReportsController : Controller
    {
        private readonly PosSqlRepository _posRepository = new PosSqlRepository();
        private readonly SharedAccountingReportService _service;
        private readonly SharedAccountingReportRepository _reportRepository;

        public AccountingReportsController()
        {
            _reportRepository = new SharedAccountingReportRepository(CreatePosConnection);
            _service = new SharedAccountingReportService(_reportRepository);
        }

        public ActionResult Index(HtmlReportFilterModel filter)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }
            if (!CanOpen(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية عرض تقارير الحسابات");
            }

            ViewBag.AccountingReportsArea = "Pos";
            return View(BuildPageModel(context, filter));
        }

        [HttpPost]
        public ActionResult Search([Bind(Prefix = "Filter")] HtmlReportFilterModel filter)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }
            if (!CanOpen(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية عرض تقارير الحسابات");
            }

            var model = BuildPageModel(context, filter);
            if (model.ActiveReport == null)
            {
                model.Message = "اختر التقرير أولا";
                ViewBag.AccountingReportsArea = "Pos";
                return View("Index", model);
            }

            model.Result = _service.Run(model.Filter, model.ActiveReport, context.UserId, IsAdmin(context));
            ViewBag.AccountingReportsArea = "Pos";
            return View("Index", model);
        }

        [HttpPost]
        public ActionResult Export([Bind(Prefix = "Filter")] HtmlReportFilterModel filter)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return new HttpStatusCodeResult(401, "يجب تسجيل الدخول أولا");
            }
            if (!CanOpen(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية تصدير هذا التقرير");
            }

            var model = BuildPageModel(context, filter);
            if (model.ActiveReport == null)
            {
                return new HttpStatusCodeResult(400, "اختر التقرير أولا");
            }
            model.Result = _service.Run(model.Filter, model.ActiveReport, context.UserId, IsAdmin(context));
            var from = model.Filter.FromDate.GetValueOrDefault(DateTime.Today).ToString("yyyyMMdd");
            var to = model.Filter.ToDate.GetValueOrDefault(DateTime.Today).ToString("yyyyMMdd");
            return File(SharedAccountingReportExcelExporter.Build(model), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", SharedAccountingReportExcelExporter.SafeFileName(model.ActiveReport.Title) + "_" + from + "_" + to + ".xlsx");
        }

        [HttpGet]
        public ActionResult AccountTree(string parentCode, string term)
        {
            var context = GetPosContext();
            if (context == null || !CanOpen(context))
            {
                return new HttpStatusCodeResult(403);
            }
            return Json(_reportRepository.GetAccountTree(parentCode, term), JsonRequestBehavior.AllowGet);
        }

        private HtmlReportPageViewModel BuildPageModel(PosUserContext context, HtmlReportFilterModel filter)
        {
            var forcedBranch = !IsAdmin(context) && context.BranchId.HasValue ? context.BranchId : null;
            return _service.BuildPage(filter, forcedBranch);
        }

        private static bool CanOpen(PosUserContext context)
        {
            return IsAdmin(context) || (context != null && context.CanViewAccountingReports);
        }

        private static bool IsAdmin(PosUserContext context)
        {
            return context != null && context.UserType.GetValueOrDefault(-1) == 0;
        }

        private PosUserContext GetPosContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _posRepository);
        }

        private static SqlConnection CreatePosConnection()
        {
            var setting = ConfigurationManager.ConnectionStrings["KishnyCashConnection"];
            if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
            {
                throw new ConfigurationErrorsException("Missing connection string: KishnyCashConnection");
            }
            var connection = new SqlConnection(setting.ConnectionString);
            connection.Open();
            return connection;
        }
    }
}
