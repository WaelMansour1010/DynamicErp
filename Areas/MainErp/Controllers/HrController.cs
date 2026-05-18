using System;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Common.EnterpriseHr;

namespace MyERP.Areas.MainErp.Controllers
{
    public class HrController : MainErpControllerBase
    {
        private readonly EnterpriseHrService _service;
        private readonly WebScreenPermissionService _permissionService;

        public HrController()
            : this(new EnterpriseHrService(new EnterpriseHrRepository(new MainErpDbConnectionFactory())), new WebScreenPermissionService(new MainErpDbConnectionFactory()))
        {
        }

        public HrController(EnterpriseHrService service, WebScreenPermissionService permissionService)
        {
            _service = service;
            _permissionService = permissionService;
        }

        public ActionResult Employees()
        {
            return RedirectToAction("Employees", "EmployeePayroll", new { area = "MainErp", host = Request["host"] });
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
            if (!Can("HR.PayrollItems", "View"))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية عرض مفردات الرواتب." }, JsonRequestBehavior.AllowGet);
            }

            var data = _service.GetComponent(id);
            return Json(new { success = data != null, data, message = data == null ? "لم يتم العثور على المفردة." : "" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SaveComponent(PayrollComponentEditViewModel request)
        {
            var isEdit = request != null && request.Id.GetValueOrDefault() > 0;
            if (!Can("HR.PayrollItems", isEdit ? "Edit" : "Add"))
            {
                Response.StatusCode = 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = isEdit ? "ليست لديك صلاحية تعديل مفردات الرواتب." : "ليست لديك صلاحية إضافة مفردة راتب." });
            }

            var result = _service.SaveComponent(request);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpGet]
        public JsonResult AdvanceDetails(int id)
        {
            if (!Can("HR.Advances", "View"))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية عرض السلف." }, JsonRequestBehavior.AllowGet);
            }

            var data = _service.GetAdvance(id);
            return Json(new { success = data != null, data, message = data == null ? "لم يتم العثور على طلب السلفة." : "" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SaveAdvance(EmployeeAdvanceViewModel request)
        {
            var isEdit = request != null && request.Id.GetValueOrDefault() > 0;
            if (!Can("HR.Advances", isEdit ? "Edit" : "Add"))
            {
                Response.StatusCode = 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = isEdit ? "ليست لديك صلاحية تعديل السلف." : "ليست لديك صلاحية إضافة سلفة." });
            }

            var result = _service.SaveAdvance(request, MainErpUserContext == null ? (int?)null : MainErpUserContext.UserId);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteAdvance(int id)
        {
            if (!Can("HR.Advances", "Delete"))
            {
                Response.StatusCode = 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية حذف السلف." });
            }

            var result = _service.DeleteAdvance(id);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpGet]
        public JsonResult EmployeesLookup(string term, string employeeStatus = "active")
        {
            if (!Can("HR.Advances", "View"))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية عرض الموظفين." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = _service.SearchEmployees(term, NormalizeEmployeeStatus(employeeStatus), 30) }, JsonRequestBehavior.AllowGet);
        }

        private ActionResult Page(string module, string screenKey, string activeScreen, string searchText, string employeeStatus, int page, int pageSize, int? employeeId = null, DateTime? dateFrom = null, DateTime? dateTo = null, string advanceStatus = null)
        {
            if (!Can(screenKey, "View"))
            {
                Response.StatusCode = 403;
                return View("~/Views/Shared/Error.cshtml");
            }

            var model = _service.Load(module, searchText, page, pageSize, NormalizeEmployeeStatus(employeeStatus), employeeId, dateFrom, dateTo, advanceStatus);
            model.ScreenKey = screenKey;
            model.HostContext = "MainErp";
            model.ComponentDetailsUrl = Url.Action("ComponentDetails", "Hr", new { area = "MainErp" });
            model.SaveComponentUrl = Url.Action("SaveComponent", "Hr", new { area = "MainErp" });
            model.AdvanceDetailsUrl = Url.Action("AdvanceDetails", "Hr", new { area = "MainErp" });
            model.SaveAdvanceUrl = Url.Action("SaveAdvance", "Hr", new { area = "MainErp" });
            model.DeleteAdvanceUrl = Url.Action("DeleteAdvance", "Hr", new { area = "MainErp" });
            model.EmployeeLookupUrl = Url.Action("EmployeesLookup", "Hr", new { area = "MainErp" });
            model.Permissions = new LegacyHrFinancePermissionsViewModel
            {
                CanView = true,
                CanAdd = Can(screenKey, "Add"),
                CanEdit = Can(screenKey, "Edit"),
                CanDelete = Can(screenKey, "Delete"),
                CanPrint = Can(screenKey, "Print"),
                CanExport = Can(screenKey, "Export")
            };
            ViewBag.ActiveScreen = activeScreen;
            return View("~/Areas/MainErp/Views/LegacyHrFinance/Index.cshtml", model);
        }

        private bool Can(string screenKey, string actionKey)
        {
            return _permissionService.Can(MainErpUserContext, screenKey, actionKey);
        }

        private static string NormalizeEmployeeStatus(string status)
        {
            status = (status ?? "active").Trim().ToLowerInvariant();
            return status == "stopped" || status == "all" ? status : "active";
        }
    }
}
