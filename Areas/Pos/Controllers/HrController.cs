using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using MyERP.Areas.Pos.Services;
using MyERP.Common.EnterpriseHr;

namespace MyERP.Areas.Pos.Controllers
{
    public class HrController : Controller
    {
        private readonly EnterpriseHrService _service;
        private readonly PosSqlRepository _posRepository;
        private readonly PosLegacyScreenPermissionService _legacyPermissionService;
        private readonly WebScreenPermissionService _webPermissionService;

        public HrController()
            : this(
                new EnterpriseHrService(new EnterpriseHrRepository(new PosEnterpriseHrConnectionFactory())),
                new PosSqlRepository(),
                new PosLegacyScreenPermissionService(),
                new WebScreenPermissionService(new PosEnterpriseHrConnectionFactory()))
        {
        }

        public HrController(
            EnterpriseHrService service,
            PosSqlRepository posRepository,
            PosLegacyScreenPermissionService legacyPermissionService,
            WebScreenPermissionService webPermissionService)
        {
            _service = service;
            _posRepository = posRepository;
            _legacyPermissionService = legacyPermissionService;
            _webPermissionService = webPermissionService;
        }

        public ActionResult Advances(string searchText, string employeeStatus = "active", int? employeeId = null, DateTime? dateFrom = null, DateTime? dateTo = null, string advanceStatus = "all", int page = 1, int pageSize = 40)
        {
            return Page("advances", "HR.Advances", "hr-advances", searchText, employeeStatus, page, pageSize, employeeId, dateFrom, dateTo, advanceStatus);
        }

        public ActionResult PayrollItems(string searchText, string employeeStatus = "active", int page = 1, int pageSize = 40)
        {
            return Page("payroll-items", "HR.PayrollItems", "hr-payroll-items", searchText, employeeStatus, page, pageSize);
        }

        public ActionResult Absences(string searchText, string employeeStatus = "active", int page = 1, int pageSize = 40)
        {
            return Page("absences", "HR.Absences", "hr-absences", searchText, employeeStatus, page, pageSize);
        }

        public ActionResult Vacations(string searchText, string employeeStatus = "active", int page = 1, int pageSize = 40)
        {
            return Page("vacations", "HR.Vacations", "hr-vacations", searchText, employeeStatus, page, pageSize);
        }

        public ActionResult Allowances(string searchText, string employeeStatus = "active", int page = 1, int pageSize = 40)
        {
            return Page("allowances", "HR.Allowances", "hr-allowances", searchText, employeeStatus, page, pageSize);
        }

        public ActionResult EndOfService(string searchText, string employeeStatus = "all", int page = 1, int pageSize = 40)
        {
            return Page("end-service", "HR.EndOfService", "hr-end-service", searchText, employeeStatus, page, pageSize);
        }

        [HttpGet]
        public JsonResult ComponentDetails(int id)
        {
            var context = GetContext();
            if (!Can(context, "HR.PayrollItems", "View"))
            {
                Trace.TraceWarning("POS HR permission denied: ComponentDetails user={0}", context == null ? 0 : context.UserId);
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new { success = false, message = "ليست لديك صلاحية عرض مفردات الرواتب." }, JsonRequestBehavior.AllowGet);
            }

            var data = _service.GetComponent(id);
            return Json(new { success = data != null, data, message = data == null ? "لم يتم العثور على المفردة." : "" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SaveComponent(PayrollComponentEditViewModel request)
        {
            var context = GetContext();
            var isEdit = request != null && request.Id.GetValueOrDefault() > 0;
            if (!Can(context, "HR.PayrollItems", isEdit ? "Edit" : "Add"))
            {
                Trace.TraceWarning("POS HR permission denied: SaveComponent user={0} edit={1}", context == null ? 0 : context.UserId, isEdit);
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = isEdit ? "ليست لديك صلاحية تعديل مفردات الرواتب." : "ليست لديك صلاحية إضافة مفردة راتب." });
            }

            var result = _service.SaveComponent(request);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpGet]
        public JsonResult AdvanceDetails(int id)
        {
            var context = GetContext();
            if (!Can(context, "HR.Advances", "View"))
            {
                Trace.TraceWarning("POS HR permission denied: AdvanceDetails user={0}", context == null ? 0 : context.UserId);
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new { success = false, message = "ليست لديك صلاحية عرض السلف." }, JsonRequestBehavior.AllowGet);
            }

            var data = _service.GetAdvance(id);
            return Json(new { success = data != null, data, message = data == null ? "لم يتم العثور على طلب السلفة." : "" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SaveAdvance(EmployeeAdvanceViewModel request)
        {
            var context = GetContext();
            var isEdit = request != null && request.Id.GetValueOrDefault() > 0;
            if (!Can(context, "HR.Advances", isEdit ? "Edit" : "Add"))
            {
                Trace.TraceWarning("POS HR permission denied: SaveAdvance user={0} edit={1}", context == null ? 0 : context.UserId, isEdit);
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = isEdit ? "ليست لديك صلاحية تعديل السلف." : "ليست لديك صلاحية إضافة سلفة." });
            }

            var result = _service.SaveAdvance(request, context == null ? (int?)null : context.UserId);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteAdvance(int id)
        {
            var context = GetContext();
            if (!Can(context, "HR.Advances", "Delete"))
            {
                Trace.TraceWarning("POS HR permission denied: DeleteAdvance user={0}", context == null ? 0 : context.UserId);
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية حذف السلف." });
            }

            var result = _service.DeleteAdvance(id);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpGet]
        public JsonResult EmployeesLookup(string term, string employeeStatus = "active")
        {
            var context = GetContext();
            if (!Can(context, "HR.Advances", "View"))
            {
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new { success = false, message = "ليست لديك صلاحية عرض الموظفين." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = _service.SearchEmployees(term, NormalizeEmployeeStatus(employeeStatus), 30) }, JsonRequestBehavior.AllowGet);
        }

        private ActionResult Page(string module, string screenKey, string activeScreen, string searchText, string employeeStatus, int page, int pageSize, int? employeeId = null, DateTime? dateFrom = null, DateTime? dateTo = null, string advanceStatus = null)
        {
            var context = GetContext();
            if (context == null)
            {
                Trace.TraceWarning("POS HR session missing for screen {0}.", screenKey);
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!Can(context, screenKey, "View"))
            {
                Trace.TraceWarning("POS HR permission denied: screen={0} user={1}", screenKey, context.UserId);
                Response.StatusCode = 403;
                return Content("ليست لديك صلاحية فتح هذه الشاشة من نقاط البيع.", "text/plain");
            }

            var model = _service.Load(module, searchText, page, pageSize, NormalizeEmployeeStatus(employeeStatus), employeeId, dateFrom, dateTo, advanceStatus);
            model.ScreenKey = screenKey;
            model.HostContext = "POS";
            model.ComponentDetailsUrl = Url.Action("ComponentDetails", "Hr", new { area = "Pos" });
            model.SaveComponentUrl = Url.Action("SaveComponent", "Hr", new { area = "Pos" });
            model.AdvanceDetailsUrl = Url.Action("AdvanceDetails", "Hr", new { area = "Pos" });
            model.SaveAdvanceUrl = Url.Action("SaveAdvance", "Hr", new { area = "Pos" });
            model.DeleteAdvanceUrl = Url.Action("DeleteAdvance", "Hr", new { area = "Pos" });
            model.EmployeeLookupUrl = Url.Action("EmployeesLookup", "Hr", new { area = "Pos" });
            model.Permissions = new LegacyHrFinancePermissionsViewModel
            {
                CanView = true,
                CanAdd = Can(context, screenKey, "Add"),
                CanEdit = Can(context, screenKey, "Edit"),
                CanDelete = Can(context, screenKey, "Delete"),
                CanPrint = Can(context, screenKey, "Print"),
                CanExport = Can(context, screenKey, "Export")
            };

            ViewBag.ActiveScreen = activeScreen;
            return View("~/Areas/Pos/Views/Hr/Index.cshtml", model);
        }

        private bool Can(PosUserContext context, string screenKey, string actionKey)
        {
            if (context == null) return false;
            if (context.IsFullAccess || context.UserType.GetValueOrDefault(-1) == 0) return true;
            if (_webPermissionService.Can(context, screenKey, actionKey)) return true;
            return CanLegacyFallback(context, screenKey, actionKey);
        }

        private bool CanLegacyFallback(PosUserContext context, string screenKey, string actionKey)
        {
            var forms = LegacyForms(screenKey);
            foreach (var form in forms)
            {
                if (actionKey == "View" && _legacyPermissionService.CanView(context, form)) return true;
                if (actionKey == "Add" && _legacyPermissionService.CanAdd(context, form)) return true;
                if (actionKey == "Edit" && _legacyPermissionService.CanEdit(context, form)) return true;
                if (actionKey == "Delete" && _legacyPermissionService.CanDelete(context, form)) return true;
                if (actionKey == "Print" && _legacyPermissionService.CanPrint(context, form)) return true;
            }

            return false;
        }

        private static string[] LegacyForms(string screenKey)
        {
            switch ((screenKey ?? string.Empty).Trim())
            {
                case "HR.Advances": return new[] { "FrmEmpsAdvanceRequest", "FrmEmpsAdvance", "FrmEmployee", "FrmEmpSalary5" };
                case "HR.PayrollItems": return new[] { "MOFRAD", "FrmEmpSalary5", "FrmEmployee" };
                case "HR.Absences": return new[] { "FrmAbsent", "FrmEmpSalary5", "FrmEmployee" };
                case "HR.Vacations": return new[] { "FrmEmpVacations", "FrmVocationEntitlements", "FrmEmployee" };
                case "HR.Allowances": return new[] { "MOFRAD", "FrmEmpSalary5", "FrmEmployee" };
                case "HR.EndOfService": return new[] { "End_oF_service", "FrmENdServiceSearsh", "FrmEmployee" };
                default: return new[] { "FrmEmployee", "FrmEmpSalary5" };
            }
        }

        private PosUserContext GetContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _posRepository);
        }

        private static string NormalizeEmployeeStatus(string status)
        {
            status = (status ?? "active").Trim().ToLowerInvariant();
            return status == "stopped" || status == "all" ? status : "active";
        }

        private sealed class PosEnterpriseHrConnectionFactory : IEnterpriseHrDbConnectionFactory, IMainErpDbConnectionFactory
        {
            public SqlConnection CreateOpenConnection()
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
}
