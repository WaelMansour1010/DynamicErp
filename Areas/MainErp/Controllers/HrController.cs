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

        public ActionResult Advances(string searchText, string employeeStatus = "active", int? employeeId = null, int? branchId = null, int? departmentId = null, DateTime? dateFrom = null, DateTime? dateTo = null, string advanceStatus = "all", int page = 1, int pageSize = 40)
        {
            return Page("advances", "HR.Advances", "hr-advances", searchText, employeeStatus, page, pageSize, employeeId, dateFrom, dateTo, advanceStatus, null, null, null, branchId, departmentId);
        }

        public ActionResult PayrollItems(string searchText, string employeeStatus = "active", int page = 1, int pageSize = 40)
        {
            return Page("payroll-items", "HR.PayrollItems", "hr-payroll-items", searchText, employeeStatus, page, pageSize);
        }

        public ActionResult ChangedComponentData(string searchText, string employeeStatus = "active", int? employeeId = null, int? componentId = null, int? branchId = null, int? departmentId = null, DateTime? dateFrom = null, DateTime? dateTo = null, int? year = null, int? month = null, string componentType = "all", string status = "all", int page = 1, int pageSize = 40)
        {
            return Page("changed-components", "HR.ChangedComponentData", "hr-changed-components", searchText, employeeStatus, page, pageSize, employeeId, dateFrom, dateTo, status, null, null, componentId, branchId, departmentId, year, month, componentType);
        }

        public ActionResult Absences(string searchText, string employeeStatus = "active", int page = 1, int pageSize = 40)
        {
            return Page("absences", "HR.Absences", "hr-absences", searchText, employeeStatus, page, pageSize);
        }

        public ActionResult Vacations(string searchText, string employeeStatus = "active", int? employeeId = null, int? branchId = null, int? departmentId = null, DateTime? dateFrom = null, DateTime? dateTo = null, string status = "all", string vacationType = null, int page = 1, int pageSize = 40)
        {
            return Page("vacations", "HR.Vacations", "hr-vacations", searchText, employeeStatus, page, pageSize, employeeId, dateFrom, dateTo, null, status, vacationType, null, branchId, departmentId);
        }

        public ActionResult VacationEntitlements(string searchText, string employeeStatus = "active", int page = 1, int pageSize = 40)
        {
            return Page("leave", "HR.Vacations", "hr-vacations", searchText, employeeStatus, page, pageSize);
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
        public JsonResult ChangedComponentDetails(int id)
        {
            if (!CanChangedComponents("View"))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية عرض المفردات المتغيرة." }, JsonRequestBehavior.AllowGet);
            }

            var data = _service.GetChangedComponent(id);
            return Json(new { success = data != null, data, message = data == null ? "لم يتم العثور على سجل المفردة المتغيرة." : "" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SaveChangedComponent(ChangedComponentEntryViewModel request)
        {
            var isEdit = request != null && request.Id.GetValueOrDefault() > 0;
            if (!CanChangedComponents(isEdit ? "Edit" : "Add"))
            {
                Response.StatusCode = 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = isEdit ? "ليست لديك صلاحية تعديل المفردات المتغيرة." : "ليست لديك صلاحية إضافة مفردة متغيرة." });
            }

            var result = _service.SaveChangedComponent(request, MainErpUserContext == null ? (int?)null : MainErpUserContext.UserId);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult PreviewChangedComponentBulk(ChangedComponentBulkRequestViewModel request)
        {
            if (!CanChangedComponents("Add"))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية إضافة المفردات المتغيرة." });
            }

            var result = _service.PreviewChangedComponentBulk(request);
            return Json(new { success = result.Success, message = result.Message, data = result });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SaveChangedComponentBulk(ChangedComponentBulkRequestViewModel request)
        {
            if (!CanChangedComponents("Add"))
            {
                Response.StatusCode = 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية إضافة المفردات المتغيرة." });
            }

            var result = _service.SaveChangedComponentBulk(request, MainErpUserContext == null ? (int?)null : MainErpUserContext.UserId);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteChangedComponent(int id)
        {
            if (!CanChangedComponents("Delete"))
            {
                Response.StatusCode = 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية حذف المفردات المتغيرة." });
            }

            var result = _service.DeleteChangedComponent(id);
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

        [HttpGet]
        public JsonResult AdvanceAccountingBoundary(int id)
        {
            if (!Can("HR.Advances", "View"))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية عرض الحدود المحاسبية للسلف." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = _service.GetAdvanceAccountingBoundary(id) }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult VacationBalance(int employeeId, DateTime? asOfDate = null, DateTime? vacationStartDate = null, DateTime? vacationEndDate = null, decimal? requestedDays = null, int? excludeVacationId = null, int? excludeEntitlementId = null)
        {
            if (!Can("HR.Vacations", "View"))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية عرض رصيد الإجازات." }, JsonRequestBehavior.AllowGet);
            }

            var data = _service.CalculateVacationBalance(new VacationBalanceRequestViewModel
            {
                EmployeeId = employeeId,
                AsOfDate = asOfDate.HasValue ? asOfDate.Value.ToString("yyyy-MM-dd") : null,
                VacationStartDate = vacationStartDate.HasValue ? vacationStartDate.Value.ToString("yyyy-MM-dd") : null,
                VacationEndDate = vacationEndDate.HasValue ? vacationEndDate.Value.ToString("yyyy-MM-dd") : null,
                RequestedDays = requestedDays,
                ExcludeVacationId = excludeVacationId,
                ExcludeEntitlementId = excludeEntitlementId
            });
            return Json(new { success = data.Errors.Count == 0, data, message = data.Errors.Count > 0 ? string.Join(" ", data.Errors) : "" }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult VacationDetails(int id)
        {
            if (!Can("HR.Vacations", "View"))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية عرض طلبات الإجازات." }, JsonRequestBehavior.AllowGet);
            }

            var data = _service.GetVacation(id);
            return Json(new { success = data != null, data, message = data == null ? "لم يتم العثور على طلب الإجازة." : "" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SaveVacation(EmployeeVacationRequestViewModel request)
        {
            var isEdit = request != null && request.Id.GetValueOrDefault() > 0;
            if (!Can("HR.Vacations", isEdit ? "Edit" : "Add"))
            {
                Response.StatusCode = 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = isEdit ? "ليست لديك صلاحية تعديل طلبات الإجازات." : "ليست لديك صلاحية إضافة طلب إجازة." });
            }

            var result = _service.SaveVacation(request, MainErpUserContext == null ? (int?)null : MainErpUserContext.UserId);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ManagerApproveVacation(int id, string remarks)
        {
            if (!Can("HR.Vacations", "Edit"))
            {
                Response.StatusCode = 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية اعتماد طلبات الإجازات." });
            }

            var result = _service.ManagerApproveVacation(id, MainErpUserContext == null ? (int?)null : MainErpUserContext.UserId, MainErpUserContext == null ? null : MainErpUserContext.UserName, remarks);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult HrApproveVacation(int id, string remarks)
        {
            if (!Can("HR.Vacations", "Edit"))
            {
                Response.StatusCode = 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية اعتماد الموارد البشرية للإجازات." });
            }

            var result = _service.HrApproveVacation(id, MainErpUserContext == null ? (int?)null : MainErpUserContext.UserId, MainErpUserContext == null ? null : MainErpUserContext.UserName, remarks);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult RejectVacation(int id, string remarks)
        {
            if (!Can("HR.Vacations", "Edit"))
            {
                Response.StatusCode = 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية رفض طلبات الإجازات." });
            }

            var result = _service.RejectVacation(id, MainErpUserContext == null ? (int?)null : MainErpUserContext.UserId, MainErpUserContext == null ? null : MainErpUserContext.UserName, remarks);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CancelVacation(int id, string remarks)
        {
            if (!Can("HR.Vacations", "Delete"))
            {
                Response.StatusCode = 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية إلغاء طلبات الإجازات." });
            }

            var result = _service.CancelVacation(id, MainErpUserContext == null ? (int?)null : MainErpUserContext.UserId, MainErpUserContext == null ? null : MainErpUserContext.UserName, remarks);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteVacation(int id)
        {
            if (!Can("HR.Vacations", "Delete"))
            {
                Response.StatusCode = 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية حذف طلبات الإجازات." });
            }

            var result = _service.DeleteVacation(id);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CreateVacationEntitlement(int id)
        {
            if (!Can("HR.Vacations", "Add"))
            {
                Response.StatusCode = 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية إنشاء مستحقات الإجازات." });
            }

            var result = _service.CreateVacationEntitlementFromRequest(id, MainErpUserContext == null ? (int?)null : MainErpUserContext.UserId);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteVacationEntitlement(int id)
        {
            if (!Can("HR.Vacations", "Delete"))
            {
                Response.StatusCode = 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية حذف مستحقات الإجازات." });
            }

            var result = _service.DeleteVacationEntitlement(id, MainErpUserContext == null ? (int?)null : MainErpUserContext.UserId);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SaveVacationReturnToWork(VacationReturnToWorkViewModel request)
        {
            if (!Can("HR.Vacations", "Edit"))
            {
                Response.StatusCode = 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية تسجيل مباشرة العمل للإجازات." });
            }

            var result = _service.SaveVacationReturnToWork(request, MainErpUserContext == null ? (int?)null : MainErpUserContext.UserId);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteVacationReturnToWork(int id)
        {
            if (!Can("HR.Vacations", "Delete"))
            {
                Response.StatusCode = 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية حذف مباشرة العمل للإجازات." });
            }

            var result = _service.DeleteVacationReturnToWork(id);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DisburseAdvance(int id)
        {
            if (!Can("HR.Advances", "Edit"))
            {
                Response.StatusCode = 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية صرف السلف." });
            }

            var result = _service.DisburseAdvanceRequest(id, MainErpUserContext == null ? (int?)null : MainErpUserContext.UserId);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SendAdvanceForApproval(int id, string remarks)
        {
            if (!Can("HR.Advances", "Edit"))
            {
                Response.StatusCode = 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية إرسال طلبات السلف للاعتماد." });
            }

            var result = _service.SendAdvanceForApproval(id, MainErpUserContext == null ? (int?)null : MainErpUserContext.UserId, MainErpUserContext == null ? null : MainErpUserContext.UserName, remarks);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ApproveAdvance(int id, string remarks)
        {
            if (!Can("HR.Advances", "Edit"))
            {
                Response.StatusCode = 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية اعتماد السلف." });
            }

            var result = _service.ApproveAdvanceRequest(id, MainErpUserContext == null ? (int?)null : MainErpUserContext.UserId, MainErpUserContext == null ? null : MainErpUserContext.UserName, remarks);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CancelAdvance(int id, string remarks)
        {
            if (!Can("HR.Advances", "Delete"))
            {
                Response.StatusCode = 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية إلغاء السلف." });
            }

            var result = _service.CancelAdvanceRequest(id, MainErpUserContext == null ? (int?)null : MainErpUserContext.UserId, MainErpUserContext == null ? null : MainErpUserContext.UserName, remarks);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpGet]
        public JsonResult EmployeesLookup(string term, string employeeStatus = "active")
        {
            if (!Can("HR.Advances", "View") && !CanChangedComponents("View"))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية عرض الموظفين." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = _service.SearchEmployees(term, NormalizeEmployeeStatus(employeeStatus), 30) }, JsonRequestBehavior.AllowGet);
        }

        private ActionResult Page(string module, string screenKey, string activeScreen, string searchText, string employeeStatus, int page, int pageSize, int? employeeId = null, DateTime? dateFrom = null, DateTime? dateTo = null, string advanceStatus = null, string vacationStatus = null, string vacationType = null, int? componentId = null, int? branchId = null, int? departmentId = null, int? yearFilter = null, int? monthFilter = null, string componentType = null)
        {
            if (!(Can(screenKey, "View") || (screenKey == "HR.ChangedComponentData" && Can("FrmChangedComponentData", "View"))))
            {
                Response.StatusCode = 403;
                return View("~/Views/Shared/Error.cshtml");
            }

            var model = _service.Load(module, searchText, page, pageSize, NormalizeEmployeeStatus(employeeStatus), employeeId, dateFrom, dateTo, advanceStatus, vacationStatus, vacationType, componentId, branchId, departmentId, yearFilter, monthFilter, componentType);
            model.ScreenKey = screenKey;
            model.HostContext = "MainErp";
            model.ComponentDetailsUrl = Url.Action("ComponentDetails", "Hr", new { area = "MainErp" });
            model.SaveComponentUrl = Url.Action("SaveComponent", "Hr", new { area = "MainErp" });
            model.ChangedComponentDetailsUrl = Url.Action("ChangedComponentDetails", "Hr", new { area = "MainErp" });
            model.SaveChangedComponentUrl = Url.Action("SaveChangedComponent", "Hr", new { area = "MainErp" });
            model.DeleteChangedComponentUrl = Url.Action("DeleteChangedComponent", "Hr", new { area = "MainErp" });
            model.PreviewChangedComponentBulkUrl = Url.Action("PreviewChangedComponentBulk", "Hr", new { area = "MainErp" });
            model.SaveChangedComponentBulkUrl = Url.Action("SaveChangedComponentBulk", "Hr", new { area = "MainErp" });
            model.AdvanceDetailsUrl = Url.Action("AdvanceDetails", "Hr", new { area = "MainErp" });
            model.SaveAdvanceUrl = Url.Action("SaveAdvance", "Hr", new { area = "MainErp" });
            model.DeleteAdvanceUrl = Url.Action("DeleteAdvance", "Hr", new { area = "MainErp" });
            model.DisburseAdvanceUrl = Url.Action("DisburseAdvance", "Hr", new { area = "MainErp" });
            model.SendAdvanceForApprovalUrl = Url.Action("SendAdvanceForApproval", "Hr", new { area = "MainErp" });
            model.ApproveAdvanceUrl = Url.Action("ApproveAdvance", "Hr", new { area = "MainErp" });
            model.CancelAdvanceUrl = Url.Action("CancelAdvance", "Hr", new { area = "MainErp" });
            model.AdvanceAccountingBoundaryUrl = Url.Action("AdvanceAccountingBoundary", "Hr", new { area = "MainErp" });
            model.VacationBalanceUrl = Url.Action("VacationBalance", "Hr", new { area = "MainErp" });
            model.VacationDetailsUrl = Url.Action("VacationDetails", "Hr", new { area = "MainErp" });
            model.SaveVacationUrl = Url.Action("SaveVacation", "Hr", new { area = "MainErp" });
            model.ManagerApproveVacationUrl = Url.Action("ManagerApproveVacation", "Hr", new { area = "MainErp" });
            model.HrApproveVacationUrl = Url.Action("HrApproveVacation", "Hr", new { area = "MainErp" });
            model.RejectVacationUrl = Url.Action("RejectVacation", "Hr", new { area = "MainErp" });
            model.CancelVacationUrl = Url.Action("CancelVacation", "Hr", new { area = "MainErp" });
            model.DeleteVacationUrl = Url.Action("DeleteVacation", "Hr", new { area = "MainErp" });
            model.CreateVacationEntitlementUrl = Url.Action("CreateVacationEntitlement", "Hr", new { area = "MainErp" });
            model.DeleteVacationEntitlementUrl = Url.Action("DeleteVacationEntitlement", "Hr", new { area = "MainErp" });
            model.SaveVacationReturnToWorkUrl = Url.Action("SaveVacationReturnToWork", "Hr", new { area = "MainErp" });
            model.DeleteVacationReturnToWorkUrl = Url.Action("DeleteVacationReturnToWork", "Hr", new { area = "MainErp" });
            model.EmployeeLookupUrl = Url.Action("EmployeesLookup", "Hr", new { area = "MainErp" });
            model.PayrollRunUrl = Url.Action("SalaryRun", "EmployeePayroll", new { area = "MainErp" });
            model.Permissions = new LegacyHrFinancePermissionsViewModel
            {
                CanView = true,
                CanAdd = Can(screenKey, "Add") || (screenKey == "HR.ChangedComponentData" && Can("FrmChangedComponentData", "Add")),
                CanEdit = Can(screenKey, "Edit") || (screenKey == "HR.ChangedComponentData" && Can("FrmChangedComponentData", "Edit")),
                CanDelete = Can(screenKey, "Delete") || (screenKey == "HR.ChangedComponentData" && Can("FrmChangedComponentData", "Delete")),
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

        private bool CanChangedComponents(string actionKey)
        {
            return Can("HR.ChangedComponentData", actionKey) || Can("FrmChangedComponentData", actionKey);
        }

        private static string NormalizeEmployeeStatus(string status)
        {
            status = (status ?? "active").Trim().ToLowerInvariant();
            return status == "stopped" || status == "all" ? status : "active";
        }
    }
}
