using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using MyERP.Areas.Pos.Services;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class PosDashboardController : Controller
    {
        private readonly PosSqlRepository _repository;
        private readonly PosLegacyScreenPermissionService _legacyPermissionService;

        public PosDashboardController()
        {
            _repository = new PosSqlRepository();
            _legacyPermissionService = new PosLegacyScreenPermissionService();
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

        public ActionResult TokenInvoiceLookup()
        {
            return OpenShell("token-invoice-lookup");
        }

        public ActionResult AccountingReports()
        {
            return OpenShell("accounting-reports");
        }

        public ActionResult FinancialIntelligence()
        {
            return OpenShell("financial-intelligence");
        }

        public ActionResult JournalEntries()
        {
            return OpenShell("journal-entries");
        }

        public ActionResult Payments()
        {
            return OpenShell("payments");
        }

        public ActionResult Cashing()
        {
            return OpenShell("cashing");
        }

        public ActionResult PurchaseInvoice()
        {
            return OpenShell("purchase-invoice");
        }

        public ActionResult StockTransfer()
        {
            return OpenShell("stock-transfer");
        }

        public ActionResult Stores()
        {
            return OpenShell("stores");
        }

        public ActionResult ExcelImport()
        {
            return OpenShell("excel-import");
        }

        public ActionResult InvoiceReconciliation()
        {
            return OpenShell("invoice-reconciliation");
        }

        public ActionResult EmployeePayroll()
        {
            return OpenShell("employee-payroll");
        }

        public ActionResult SalaryRun()
        {
            return OpenShell("salary-run");
        }

        public ActionResult MedicalInsurance()
        {
            return OpenShell("medical-insurance");
        }

        public ActionResult MedicalInsuranceReports()
        {
            return OpenShell("medical-insurance-reports");
        }

        public ActionResult SystemHealth()
        {
            return OpenShell("system-health");
        }

        public ActionResult SqlUpdates()
        {
            return OpenShell("sql-updates");
        }

        public ActionResult PrintTemplates()
        {
            return OpenShell("print-templates");
        }

        [HttpGet]
        public JsonResult Summary(string periodType, DateTime? fromDate, DateTime? toDate, int? branchId, string operationType, bool? advanced, bool? refreshSnapshot)
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
                 var stopwatch = Stopwatch.StartNew();
                 PosDashboardSummaryDto summary;
                 var useDailySnapshot = string.Equals(range.PeriodType, "daily", StringComparison.OrdinalIgnoreCase)
                     && range.FromDate.Date == range.ToDate.Date
                     && range.ToDate.Date < DateTime.Today;

                 if (useDailySnapshot)
                 {
                     if (refreshSnapshot.GetValueOrDefault(false))
                     {
                         _repository.GenerateAdminDashboardDailySnapshot(range.FromDate, resolvedBranchId, operationType, context.UserId);
                     }

                     summary = _repository.GetAdminDashboardDailySnapshot(range.FromDate, resolvedBranchId, operationType, range.PeriodType, advanced.GetValueOrDefault(false));
                     if (!string.Equals(summary.SnapshotStatus, "Completed", StringComparison.OrdinalIgnoreCase))
                     {
                         summary.SnapshotMessage = string.IsNullOrWhiteSpace(summary.SnapshotMessage)
                             ? "لم يتم تجهيز مؤشرات هذه الفترة بعد"
                             : summary.SnapshotMessage;
                     }
                 }
                 else
                 {
                     summary = _repository.GetAdminDashboardSummary(range.FromDate, range.ToDate, range.PreviousFromDate, range.PreviousToDate, resolvedBranchId, operationType, range.PeriodType, advanced.GetValueOrDefault(false));
                 }

                 stopwatch.Stop();
                 PosPerformanceLogger.LogQuery("PosDashboard.Summary", useDailySnapshot ? "GetAdminDashboardDailySnapshot" : "GetAdminDashboardSummary", stopwatch.ElapsedMilliseconds, null, context);
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

            if (IsTellerOnly(context)
                && !string.Equals(screen, "sales", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(screen, "kyc", StringComparison.OrdinalIgnoreCase))
            {
                screen = "sales";
            }

            if (screen == "kyc-bank-follow-up" && !CanOpenKycBankFollowUp(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية متابعة KYC والبنك");
            }

            if (screen == "accounting-reports" && !CanOpenAccountingReports(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية عرض تقارير الحسابات");
            }

            if (screen == "token-invoice-lookup" && !CanOpenReports(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية بحث مبيعات التوكينات");
            }

            if (screen == "financial-intelligence" && !CanOpenAccountingReports(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية عرض الحسابات الذكية");
            }

            if (screen == "journal-entries" && !CanOpenJournalEntries(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية إدخال أو استعراض القيود");
            }

            if (screen == "cashing" && !_legacyPermissionService.CanView(context, "FrmCashing"))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية استعراض سندات القبض");
            }

            if (screen == "system-health" && !IsAdmin(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية مراقبة النظام");
            }

            if (screen == "sql-updates" && !IsAdmin(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية إدارة تحديثات قاعدة البيانات");
            }

            if (screen == "print-templates" && !CanOpenPrintTemplates(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية إدارة نماذج الطباعة");
            }

            if (screen == "purchase-invoice" && !CanOpenPurchaseInvoice(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية فتح فاتورة المشتريات");
            }

            if (screen == "stock-transfer" && !CanOpenPurchaseInvoice(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية فتح سند تحويل المخزون");
            }

            if ((screen == "excel-import" || screen == "invoice-reconciliation") && !CanOpenExcelImport(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية استيراد العمليات من Excel");
            }

            if ((screen == "employee-payroll" || screen == "salary-run" || screen == "medical-insurance" || screen == "medical-insurance-reports"
                || screen == "hr-advances" || screen == "hr-payroll-items" || screen == "hr-absences" || screen == "hr-vacations" || screen == "hr-allowances" || screen == "hr-end-service")
                && !CanOpenEmployeePayroll(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية فتح شاشات الموظفين والرواتب");
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

        private static bool IsTellerOnly(PosUserContext context)
        {
            return context != null && context.CanTeller;
        }

        private static bool CanOpenKycBankFollowUp(PosUserContext context)
        {
            return IsAdmin(context) || (context != null && context.IsFullAccessCustomerService);
        }

        private static bool CanOpenAccountingReports(PosUserContext context)
        {
            return IsAdmin(context) || (context != null && context.CanViewAccountingReports);
        }

        private static bool CanOpenReports(PosUserContext context)
        {
            return IsAdmin(context) || (context != null && context.CanViewReports);
        }

        private static bool CanOpenJournalEntries(PosUserContext context)
        {
            return IsAdmin(context)
                || (context != null && (context.CanViewJournalEntry || context.CanCreateJournalEntry || context.CanEditJournalEntry || context.CanDeleteJournalEntry));
        }

        private static bool CanOpenPrintTemplates(PosUserContext context)
        {
            return IsAdmin(context) || (context != null && context.CanManagePrintTemplates);
        }

        private static bool CanOpenPurchaseInvoice(PosUserContext context)
        {
            return IsAdmin(context) || (context != null && !IsTellerOnly(context) && context.CanSave);
        }

        private static bool CanOpenExcelImport(PosUserContext context)
        {
            return IsAdmin(context) || (context != null && context.CanImportExcel);
        }

        private bool CanOpenEmployeePayroll(PosUserContext context)
        {
            return IsAdmin(context)
                || (context != null && (_legacyPermissionService.CanView(context, "FrmEmployee") || _legacyPermissionService.CanView(context, "FrmEmpSalary5")));
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
                return Url.Content("~/Pos/PosTransaction/Kyc");
            }

            if (screen == "kyc-bank-follow-up")
            {
                return Url.Content("~/Pos/KycBankFollowUp/Index");
            }

            if (screen == "reports")
            {
                return Url.Content("~/Pos/PosReports/Index");
            }

            if (screen == "token-invoice-lookup")
            {
                return Url.Content("~/Pos/TokenInvoiceLookup/Index");
            }

            if (screen == "accounting-reports")
            {
                return Url.Content("~/Pos/AccountingReports/Index");
            }

            if (screen == "financial-intelligence")
            {
                return Url.Content("~/Pos/FinancialIntelligence/Index");
            }

            if (screen == "journal-entries")
            {
                return Url.Content("~/Pos/JournalEntries/Index");
            }

            if (screen == "payments")
            {
                return Url.Content("~/Pos/Payments/Index");
            }

            if (screen == "cashing")
            {
                return Url.Content("~/Pos/Cashing/Index");
            }

            if (screen == "purchase-invoice")
            {
                return Url.Content("~/Pos/PurchaseInvoice/Index");
            }

            if (screen == "stock-transfer")
            {
                return Url.Content("~/Pos/StockTransfer/Index");
            }

            if (screen == "stores")
            {
                return Url.Content("~/Pos/Stores/Index");
            }

            if (screen == "excel-import")
            {
                return Url.Content("~/Pos/ExcelImport/Index");
            }

            if (screen == "invoice-reconciliation")
            {
                return Url.Content("~/Pos/PosInvoiceReconciliation/Index");
            }

            if (screen == "employee-payroll")
            {
                return Url.Content("~/Pos/EmployeePayroll/Employees");
            }

            if (screen == "salary-run")
            {
                return Url.Content("~/Pos/EmployeePayroll/SalaryRun");
            }

            if (screen == "medical-insurance")
            {
                return Url.Content("~/Pos/EmployeePayroll/MedicalInsurance");
            }

            if (screen == "medical-insurance-reports")
            {
                return Url.Content("~/Pos/EmployeePayroll/MedicalInsuranceReports");
            }

            if (screen == "hr-advances")
            {
                return Url.Content("~/Pos/Hr/Advances");
            }

            if (screen == "hr-payroll-items")
            {
                return Url.Content("~/Pos/Hr/PayrollItems");
            }

            if (screen == "hr-absences")
            {
                return Url.Content("~/Pos/Hr/Absences");
            }

            if (screen == "hr-vacations")
            {
                return Url.Content("~/Pos/Hr/Vacations");
            }

            if (screen == "hr-allowances")
            {
                return Url.Content("~/Pos/Hr/Allowances");
            }

            if (screen == "hr-end-service")
            {
                return Url.Content("~/Pos/Hr/EndOfService");
            }

            if (screen == "system-health")
            {
                return Url.Content("~/Pos/PosSystemHealth/Index");
            }

            if (screen == "sql-updates")
            {
                return Url.Content("~/Pos/PosSqlUpdates/Index");
            }

            if (screen == "print-templates")
            {
                return Url.Content("~/Pos/PrintTemplate/Index?name=KycCard");
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
