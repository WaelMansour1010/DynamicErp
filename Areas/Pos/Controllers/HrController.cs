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

        public ActionResult ChangedComponentData(string searchText, string employeeStatus = "active", int? employeeId = null, int? componentId = null, int? branchId = null, int? departmentId = null, DateTime? dateFrom = null, DateTime? dateTo = null, int? year = null, int? month = null, string componentType = "all", string status = "all", int page = 1, int pageSize = 40)
        {
            return Page("changed-components", "HR.ChangedComponentData", "hr-changed-components", searchText, employeeStatus, page, pageSize, employeeId, dateFrom, dateTo, status, null, null, componentId, branchId, departmentId, year, month, componentType);
        }

        public ActionResult Absences(string searchText, string employeeStatus = "active", int page = 1, int pageSize = 40)
        {
            return Page("absences", "HR.Absences", "hr-absences", searchText, employeeStatus, page, pageSize);
        }

        public ActionResult Vacations(string searchText, string employeeStatus = "active", int? employeeId = null, DateTime? dateFrom = null, DateTime? dateTo = null, string status = "all", string vacationType = null, int page = 1, int pageSize = 40)
        {
            return Page("vacations", "HR.Vacations", "hr-vacations", searchText, employeeStatus, page, pageSize, employeeId, dateFrom, dateTo, null, status, vacationType);
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
        public JsonResult ChangedComponentDetails(int id)
        {
            var context = GetContext();
            if (!Can(context, "HR.ChangedComponentData", "View"))
            {
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new { success = false, message = "ليست لديك صلاحية عرض المفردات المتغيرة." }, JsonRequestBehavior.AllowGet);
            }

            var data = _service.GetChangedComponent(id);
            return Json(new { success = data != null, data, message = data == null ? "لم يتم العثور على سجل المفردة المتغيرة." : "" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SaveChangedComponent(ChangedComponentEntryViewModel request)
        {
            var context = GetContext();
            var isEdit = request != null && request.Id.GetValueOrDefault() > 0;
            if (!Can(context, "HR.ChangedComponentData", isEdit ? "Edit" : "Add"))
            {
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = isEdit ? "ليست لديك صلاحية تعديل المفردات المتغيرة." : "ليست لديك صلاحية إضافة مفردة متغيرة." });
            }

            var result = _service.SaveChangedComponent(request, context == null ? (int?)null : context.UserId);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult PreviewChangedComponentBulk(ChangedComponentBulkRequestViewModel request)
        {
            var context = GetContext();
            if (!Can(context, "HR.ChangedComponentData", "Add"))
            {
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new { success = false, message = "ليست لديك صلاحية إضافة المفردات المتغيرة." });
            }

            var result = _service.PreviewChangedComponentBulk(request);
            return Json(new { success = result.Success, message = result.Message, data = result });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SaveChangedComponentBulk(ChangedComponentBulkRequestViewModel request)
        {
            var context = GetContext();
            if (!Can(context, "HR.ChangedComponentData", "Add"))
            {
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية إضافة المفردات المتغيرة." });
            }

            var result = _service.SaveChangedComponentBulk(request, context == null ? (int?)null : context.UserId);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteChangedComponent(int id)
        {
            var context = GetContext();
            if (!Can(context, "HR.ChangedComponentData", "Delete"))
            {
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية حذف المفردات المتغيرة." });
            }

            var result = _service.DeleteChangedComponent(id);
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

        [HttpGet]
        public JsonResult AdvanceAccountingBoundary(int id)
        {
            var context = GetContext();
            if (!Can(context, "HR.Advances", "View"))
            {
                Trace.TraceWarning("POS HR permission denied: AdvanceAccountingBoundary user={0}", context == null ? 0 : context.UserId);
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new { success = false, message = "ليست لديك صلاحية عرض الحدود المحاسبية للسلف." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = _service.GetAdvanceAccountingBoundary(id) }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult VacationBalance(int employeeId, DateTime? asOfDate = null, DateTime? vacationStartDate = null, DateTime? vacationEndDate = null, decimal? requestedDays = null, int? excludeVacationId = null, int? excludeEntitlementId = null)
        {
            var context = GetContext();
            if (!Can(context, "HR.Vacations", "View"))
            {
                Trace.TraceWarning("POS HR permission denied: VacationBalance user={0}", context == null ? 0 : context.UserId);
                Response.StatusCode = context == null ? 401 : 403;
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
            var context = GetContext();
            if (!Can(context, "HR.Vacations", "View"))
            {
                Trace.TraceWarning("POS HR permission denied: VacationDetails user={0}", context == null ? 0 : context.UserId);
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new { success = false, message = "ليست لديك صلاحية عرض طلبات الإجازات." }, JsonRequestBehavior.AllowGet);
            }

            var data = _service.GetVacation(id);
            return Json(new { success = data != null, data, message = data == null ? "لم يتم العثور على طلب الإجازة." : "" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SaveVacation(EmployeeVacationRequestViewModel request)
        {
            var context = GetContext();
            var isEdit = request != null && request.Id.GetValueOrDefault() > 0;
            if (!Can(context, "HR.Vacations", isEdit ? "Edit" : "Add"))
            {
                Trace.TraceWarning("POS HR permission denied: SaveVacation user={0} edit={1}", context == null ? 0 : context.UserId, isEdit);
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = isEdit ? "ليست لديك صلاحية تعديل طلبات الإجازات." : "ليست لديك صلاحية إضافة طلب إجازة." });
            }

            var result = _service.SaveVacation(request, context == null ? (int?)null : context.UserId);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ManagerApproveVacation(int id, string remarks)
        {
            var context = GetContext();
            if (!Can(context, "HR.Vacations", "Edit"))
            {
                Trace.TraceWarning("POS HR permission denied: ManagerApproveVacation user={0}", context == null ? 0 : context.UserId);
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية اعتماد طلبات الإجازات." });
            }

            var result = _service.ManagerApproveVacation(id, context == null ? (int?)null : context.UserId, context == null ? null : context.UserName, remarks);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult HrApproveVacation(int id, string remarks)
        {
            var context = GetContext();
            if (!Can(context, "HR.Vacations", "Edit"))
            {
                Trace.TraceWarning("POS HR permission denied: HrApproveVacation user={0}", context == null ? 0 : context.UserId);
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية اعتماد الموارد البشرية للإجازات." });
            }

            var result = _service.HrApproveVacation(id, context == null ? (int?)null : context.UserId, context == null ? null : context.UserName, remarks);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult RejectVacation(int id, string remarks)
        {
            var context = GetContext();
            if (!Can(context, "HR.Vacations", "Edit"))
            {
                Trace.TraceWarning("POS HR permission denied: RejectVacation user={0}", context == null ? 0 : context.UserId);
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية رفض طلبات الإجازات." });
            }

            var result = _service.RejectVacation(id, context == null ? (int?)null : context.UserId, context == null ? null : context.UserName, remarks);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CancelVacation(int id, string remarks)
        {
            var context = GetContext();
            if (!Can(context, "HR.Vacations", "Delete"))
            {
                Trace.TraceWarning("POS HR permission denied: CancelVacation user={0}", context == null ? 0 : context.UserId);
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية إلغاء طلبات الإجازات." });
            }

            var result = _service.CancelVacation(id, context == null ? (int?)null : context.UserId, context == null ? null : context.UserName, remarks);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CreateVacationEntitlement(int id)
        {
            var context = GetContext();
            if (!Can(context, "HR.Vacations", "Add"))
            {
                Trace.TraceWarning("POS HR permission denied: CreateVacationEntitlement user={0}", context == null ? 0 : context.UserId);
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية إنشاء مستحقات الإجازات." });
            }

            var result = _service.CreateVacationEntitlementFromRequest(id, context == null ? (int?)null : context.UserId);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteVacationEntitlement(int id)
        {
            var context = GetContext();
            if (!Can(context, "HR.Vacations", "Delete"))
            {
                Trace.TraceWarning("POS HR permission denied: DeleteVacationEntitlement user={0}", context == null ? 0 : context.UserId);
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية حذف مستحقات الإجازات." });
            }

            var result = _service.DeleteVacationEntitlement(id, context == null ? (int?)null : context.UserId);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DisburseAdvance(int id)
        {
            var context = GetContext();
            if (!Can(context, "HR.Advances", "Edit"))
            {
                Trace.TraceWarning("POS HR permission denied: DisburseAdvance user={0}", context == null ? 0 : context.UserId);
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية صرف السلف." });
            }

            var result = _service.DisburseAdvanceRequest(id, context == null ? (int?)null : context.UserId);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ApproveAdvance(int id, string remarks)
        {
            var context = GetContext();
            if (!Can(context, "HR.Advances", "Edit"))
            {
                Trace.TraceWarning("POS HR permission denied: ApproveAdvance user={0}", context == null ? 0 : context.UserId);
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية اعتماد السلف." });
            }

            var result = _service.ApproveAdvanceRequest(id, context == null ? (int?)null : context.UserId, context == null ? null : context.UserName, remarks);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CancelAdvance(int id, string remarks)
        {
            var context = GetContext();
            if (!Can(context, "HR.Advances", "Delete"))
            {
                Trace.TraceWarning("POS HR permission denied: CancelAdvance user={0}", context == null ? 0 : context.UserId);
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية إلغاء السلف." });
            }

            var result = _service.CancelAdvanceRequest(id, context == null ? (int?)null : context.UserId, context == null ? null : context.UserName, remarks);
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

        private ActionResult Page(string module, string screenKey, string activeScreen, string searchText, string employeeStatus, int page, int pageSize, int? employeeId = null, DateTime? dateFrom = null, DateTime? dateTo = null, string advanceStatus = null, string vacationStatus = null, string vacationType = null, int? componentId = null, int? branchId = null, int? departmentId = null, int? yearFilter = null, int? monthFilter = null, string componentType = null)
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

            var model = _service.Load(module, searchText, page, pageSize, NormalizeEmployeeStatus(employeeStatus), employeeId, dateFrom, dateTo, advanceStatus, vacationStatus, vacationType, componentId, branchId, departmentId, yearFilter, monthFilter, componentType);
            model.ScreenKey = screenKey;
            model.HostContext = "POS";
            model.ComponentDetailsUrl = Url.Action("ComponentDetails", "Hr", new { area = "Pos" });
            model.SaveComponentUrl = Url.Action("SaveComponent", "Hr", new { area = "Pos" });
            model.ChangedComponentDetailsUrl = Url.Action("ChangedComponentDetails", "Hr", new { area = "Pos" });
            model.SaveChangedComponentUrl = Url.Action("SaveChangedComponent", "Hr", new { area = "Pos" });
            model.DeleteChangedComponentUrl = Url.Action("DeleteChangedComponent", "Hr", new { area = "Pos" });
            model.PreviewChangedComponentBulkUrl = Url.Action("PreviewChangedComponentBulk", "Hr", new { area = "Pos" });
            model.SaveChangedComponentBulkUrl = Url.Action("SaveChangedComponentBulk", "Hr", new { area = "Pos" });
            model.AdvanceDetailsUrl = Url.Action("AdvanceDetails", "Hr", new { area = "Pos" });
            model.SaveAdvanceUrl = Url.Action("SaveAdvance", "Hr", new { area = "Pos" });
            model.DeleteAdvanceUrl = Url.Action("DeleteAdvance", "Hr", new { area = "Pos" });
            model.DisburseAdvanceUrl = Url.Action("DisburseAdvance", "Hr", new { area = "Pos" });
            model.ApproveAdvanceUrl = Url.Action("ApproveAdvance", "Hr", new { area = "Pos" });
            model.CancelAdvanceUrl = Url.Action("CancelAdvance", "Hr", new { area = "Pos" });
            model.AdvanceAccountingBoundaryUrl = Url.Action("AdvanceAccountingBoundary", "Hr", new { area = "Pos" });
            model.VacationBalanceUrl = Url.Action("VacationBalance", "Hr", new { area = "Pos" });
            model.VacationDetailsUrl = Url.Action("VacationDetails", "Hr", new { area = "Pos" });
            model.SaveVacationUrl = Url.Action("SaveVacation", "Hr", new { area = "Pos" });
            model.ManagerApproveVacationUrl = Url.Action("ManagerApproveVacation", "Hr", new { area = "Pos" });
            model.HrApproveVacationUrl = Url.Action("HrApproveVacation", "Hr", new { area = "Pos" });
            model.RejectVacationUrl = Url.Action("RejectVacation", "Hr", new { area = "Pos" });
            model.CancelVacationUrl = Url.Action("CancelVacation", "Hr", new { area = "Pos" });
            model.CreateVacationEntitlementUrl = Url.Action("CreateVacationEntitlement", "Hr", new { area = "Pos" });
            model.DeleteVacationEntitlementUrl = Url.Action("DeleteVacationEntitlement", "Hr", new { area = "Pos" });
            model.EmployeeLookupUrl = Url.Action("EmployeesLookup", "Hr", new { area = "Pos" });
            model.PayrollRunUrl = Url.Action("SalaryRun", "EmployeePayroll", new { area = "Pos" });
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
                case "HR.ChangedComponentData": return new[] { "FrmChangedComponentData", "FrmEmpSalary5", "FrmEmployee" };
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
