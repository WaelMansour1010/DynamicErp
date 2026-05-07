using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class FinancialIntelligenceController : Controller
    {
        private readonly PosSqlRepository _posRepository;
        private readonly PosFinancialIntelligenceRepository _fiRepository;

        public FinancialIntelligenceController()
        {
            _posRepository = new PosSqlRepository();
            _fiRepository = new PosFinancialIntelligenceRepository();
        }

        public ActionResult Index()
        {
            return Page("Index");
        }

        public ActionResult EmployeeReceivables()
        {
            return Page("EmployeeReceivables");
        }

        public ActionResult Custody()
        {
            return Page("Custody");
        }

        public ActionResult AbnormalJournals()
        {
            return Page("AbnormalJournals");
        }

        public ActionResult RootCause(string accountCode)
        {
            var model = BuildPageModel();
            if (model == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            model.InitialAccountCode = accountCode;
            return View(model);
        }

        [HttpPost]
        public JsonResult DashboardData(PosFinancialIntelligenceFilter filter)
        {
            return Run(filter, f => _fiRepository.GetAccountingHealthDashboard(f, LockedBranch(GetContext())));
        }

        [HttpPost]
        public JsonResult EmployeeReceivablesData(PosFinancialIntelligenceFilter filter)
        {
            return Run(filter, f => _fiRepository.GetEmployeeReceivableDiagnostics(f, LockedBranch(GetContext())));
        }

        [HttpPost]
        public JsonResult CustodyData(PosFinancialIntelligenceFilter filter)
        {
            return Run(filter, f => _fiRepository.GetCustodyDiagnostics(f, LockedBranch(GetContext())));
        }

        [HttpPost]
        public JsonResult AbnormalJournalsData(PosFinancialIntelligenceFilter filter)
        {
            return Run(filter, f => _fiRepository.GetAbnormalJournalDetection(f, LockedBranch(GetContext())));
        }

        [HttpPost]
        public JsonResult RootCauseData(PosFinancialIntelligenceFilter filter)
        {
            return Run(filter, f => _fiRepository.GetRootCauseAnalyzer(f, LockedBranch(GetContext())));
        }

        private ActionResult Page(string viewName)
        {
            var model = BuildPageModel();
            if (model == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!CanOpen(model.Context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية عرض الحسابات الذكية");
            }

            return View(viewName, model);
        }

        private JsonResult Run(PosFinancialIntelligenceFilter filter, Func<PosFinancialIntelligenceFilter, PosFinancialIntelligenceResult> action)
        {
            var context = GetContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "يجب تسجيل دخول نقطة البيع أولا" });
            }

            if (!CanOpen(context))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية عرض الحسابات الذكية" });
            }

            try
            {
                return Json(new { success = true, data = action(filter ?? new PosFinancialIntelligenceFilter()) });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "تعذر تحميل بيانات الحسابات الذكية", technicalMessage = ex.Message });
            }
        }

        private PosFinancialIntelligencePageModel BuildPageModel()
        {
            var context = GetContext();
            if (context == null)
            {
                return null;
            }

            var lockedBranchId = LockedBranch(context);
            return new PosFinancialIntelligencePageModel
            {
                Context = context,
                LockedBranchId = lockedBranchId,
                Branches = IsAdmin(context)
                    ? _posRepository.GetBranches().ToList()
                    : _posRepository.GetBranches().Where(x => x.BranchId == context.BranchId.GetValueOrDefault()).ToList()
            };
        }

        private PosUserContext GetContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _posRepository);
        }

        private static int? LockedBranch(PosUserContext context)
        {
            return !IsAdmin(context) && context != null && context.BranchId.HasValue ? context.BranchId : null;
        }

        private static bool CanOpen(PosUserContext context)
        {
            return IsAdmin(context) || (context != null && context.CanViewAccountingReports);
        }

        private static bool IsAdmin(PosUserContext context)
        {
            return context != null && (context.UserType.GetValueOrDefault(-1) == 0 || context.IsFullAccess);
        }
    }
}
