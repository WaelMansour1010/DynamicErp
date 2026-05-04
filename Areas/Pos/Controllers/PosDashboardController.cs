using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class PosDashboardController : Controller
    {
        private readonly PosSqlRepository _repository;

        public PosDashboardController()
        {
            _repository = new PosSqlRepository();
        }

        public ActionResult Index()
        {
            return OpenShell(IsMobileRequest() ? "sales" : "dashboard");
        }

        public ActionResult Sales()
        {
            return OpenShell("sales");
        }

        public ActionResult Closing()
        {
            return OpenShell("closing");
        }

        public ActionResult Kyc()
        {
            return OpenShell("kyc");
        }

        public ActionResult KycBankFollowUp()
        {
            return OpenShell("kyc-bank-follow-up");
        }

        public ActionResult Reports()
        {
            return OpenShell("reports");
        }

        public ActionResult AccountingReports()
        {
            return OpenShell("accounting-reports");
        }

        public ActionResult JournalEntries()
        {
            return OpenShell("journal-entries");
        }

        public ActionResult Payments()
        {
            return OpenShell("payments");
        }

        public ActionResult SystemHealth()
        {
            return OpenShell("system-health");
        }

        [HttpGet]
        public JsonResult Summary(string periodType, DateTime? fromDate, DateTime? toDate, int? branchId, string operationType, bool? advanced)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "يجب تسجيل دخول نقطة البيع أولاً" }, JsonRequestBehavior.AllowGet);
            }

            if (!IsAdmin(context))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية عرض لوحة التحكم" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var resolvedBranchId = branchId.HasValue && branchId.Value > 0 ? branchId : null;
                var range = ResolveDashboardRange(periodType, fromDate, toDate);
                var summary = _repository.GetAdminDashboardSummary(range.FromDate, range.ToDate, range.PreviousFromDate, range.PreviousToDate, resolvedBranchId, operationType, range.PeriodType, advanced.GetValueOrDefault(false));
                return Json(new { success = true, period = range, data = summary }, JsonRequestBehavior.AllowGet);
            }
            catch (SqlException ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "تعذر تحميل لوحة التحكم", technicalMessage = ex.Message }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "تعذر تحميل لوحة التحكم", technicalMessage = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        private ActionResult OpenShell(string screen)
        {
            var context = GetPosContext();
            if (context == null)
            {
                TempData["PosLoginMessage"] = PosLoginController.PosSessionExpiredMessage;
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (screen == "dashboard" && !IsAdmin(context))
            {
                screen = HasSalesDefaults(context) ? "sales" : "home";
            }

            if (screen == "kyc-bank-follow-up" && !CanOpenKycBankFollowUp(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية متابعة KYC والبنك");
            }

            if (screen == "accounting-reports" && !CanOpenAccountingReports(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية عرض تقارير الحسابات");
            }

            if (screen == "journal-entries" && !CanOpenJournalEntries(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية إدخال أو استعراض القيود");
            }

            if (screen == "system-health" && !IsAdmin(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية مراقبة النظام");
            }

            ViewBag.PosContext = context;
            ViewBag.ActiveScreen = screen;
            ViewBag.InitialScreenUrl = ScreenUrl(screen);
            ViewBag.HasSalesDefaults = HasSalesDefaults(context);
            ViewBag.Branches = IsAdmin(context)
                ? _repository.GetBranches()
                : new[] { new PosBranchDto { BranchId = context.BranchId.GetValueOrDefault(), BranchName = context.BranchName } };
            return View("Index");
        }

        private static bool IsAdmin(PosUserContext context)
        {
            return context != null && (context.UserType.GetValueOrDefault(-1) == 0 || context.IsFullAccess);
        }

        private static bool CanOpenKycBankFollowUp(PosUserContext context)
        {
            return IsAdmin(context) || (context != null && context.IsFullAccessCustomerService);
        }

        private static bool CanOpenAccountingReports(PosUserContext context)
        {
            return IsAdmin(context) || (context != null && context.CanViewAccountingReports);
        }

        private static bool CanOpenJournalEntries(PosUserContext context)
        {
            return IsAdmin(context)
                || (context != null && (context.CanViewJournalEntry || context.CanCreateJournalEntry || context.CanEditJournalEntry || context.CanDeleteJournalEntry));
        }

        private static bool HasSalesDefaults(PosUserContext context)
        {
            if (context != null && (context.IsFullAccess || context.UserType.GetValueOrDefault(-1) == 0))
            {
                return true;
            }

            return context != null
                && context.BranchId.GetValueOrDefault() > 0
                && context.EmpId.GetValueOrDefault() > 0
                && context.StoreId.GetValueOrDefault() > 0
                && context.BoxId.GetValueOrDefault() > 0
                && context.PaymentTypeId.GetValueOrDefault() > 0;
        }

        private static DashboardRange ResolveDashboardRange(string periodType, DateTime? fromDate, DateTime? toDate)
        {
            var today = DateTime.Today;
            var period = (periodType ?? "daily").Trim().ToLowerInvariant();
            DateTime from;
            DateTime to;

            if (period == "weekly")
            {
                to = today;
                from = today.AddDays(-6);
            }
            else if (period == "monthly")
            {
                from = new DateTime(today.Year, today.Month, 1);
                to = from.AddMonths(1).AddDays(-1);
            }
            else if (period == "yearly")
            {
                from = new DateTime(today.Year, 1, 1);
                to = new DateTime(today.Year, 12, 31);
            }
            else if (period == "custom")
            {
                from = (fromDate ?? today).Date;
                to = (toDate ?? from).Date;
                if (to < from)
                {
                    var temp = from;
                    from = to;
                    to = temp;
                }
            }
            else
            {
                period = "daily";
                from = today;
                to = today;
            }

            var dayCount = Math.Max(1, (to - from).Days + 1);
            return new DashboardRange
            {
                PeriodType = period,
                FromDate = from,
                ToDate = to,
                PreviousFromDate = from.AddDays(-dayCount),
                PreviousToDate = from.AddDays(-1)
            };
        }

        private string ScreenUrl(string screen)
        {
            if (screen == "closing")
            {
                return Url.Content("~/Pos/PosClosing/Index");
            }

            if (screen == "kyc")
            {
                return Url.Content("~/Pos/PosTransaction/Index?openKyc=true");
            }

            if (screen == "kyc-bank-follow-up")
            {
                return Url.Content("~/Pos/KycBankFollowUp/Index");
            }

            if (screen == "reports")
            {
                return Url.Content("~/Pos/PosReports/Index");
            }

            if (screen == "accounting-reports")
            {
                return Url.Content("~/Pos/AccountingReports/Index");
            }

            if (screen == "journal-entries")
            {
                return Url.Content("~/Pos/JournalEntries/Index");
            }

            if (screen == "payments")
            {
                return Url.Content("~/Pos/Payments/Index");
            }

            if (screen == "system-health")
            {
                return Url.Content("~/Pos/PosSystemHealth/Index");
            }

            if (screen == "home")
            {
                return string.Empty;
            }

            return Url.Content("~/Pos/PosTransaction/Index");
        }

        private bool IsMobileRequest()
        {
            var browser = Request != null ? Request.Browser : null;
            if (browser != null && browser.IsMobileDevice)
            {
                return true;
            }

            var agent = Request != null ? (Request.UserAgent ?? string.Empty) : string.Empty;
            return agent.IndexOf("Mobi", StringComparison.OrdinalIgnoreCase) >= 0
                || agent.IndexOf("Android", StringComparison.OrdinalIgnoreCase) >= 0
                || agent.IndexOf("iPhone", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private PosUserContext GetPosContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _repository);
        }

        public class DashboardRange
        {
            public string PeriodType { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public DateTime PreviousFromDate { get; set; }
            public DateTime PreviousToDate { get; set; }
        }
    }
}
