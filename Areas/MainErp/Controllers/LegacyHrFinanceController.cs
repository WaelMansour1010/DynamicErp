using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.LegacyHrFinance;
using MyERP.Areas.MainErp.Services.LegacyHrFinance;
using MyERP.Areas.MainErp.ViewModels.LegacyHrFinance;

namespace MyERP.Areas.MainErp.Controllers
{
    public class LegacyHrFinanceController : MainErpControllerBase
    {
        private readonly LegacyHrFinanceService _service;
        private readonly LegacyScreenPermissionService _permissionService;

        public LegacyHrFinanceController()
            : this(new LegacyHrFinanceService(new LegacyHrFinanceRepository(new MainErpDbConnectionFactory())), new LegacyScreenPermissionService(new MainErpDbConnectionFactory()))
        {
        }

        public LegacyHrFinanceController(LegacyHrFinanceService service, LegacyScreenPermissionService permissionService)
        {
            _service = service;
            _permissionService = permissionService;
        }

        public ActionResult Components(string searchText, int page = 1, int pageSize = 40)
        {
            return Page("components", "MOFRAD", "legacy-components", searchText, page, pageSize);
        }

        public ActionResult Advances(string searchText, int page = 1, int pageSize = 40)
        {
            return Page("advances", "FrmEmpsAdvanceRequest", "legacy-advances", searchText, page, pageSize);
        }

        public ActionResult LeaveEntitlements(string searchText, int page = 1, int pageSize = 40)
        {
            return Page("leave", "FrmVocationEntitlements", "legacy-leave-entitlements", searchText, page, pageSize);
        }

        public ActionResult SickLeaves(string searchText, int page = 1, int pageSize = 40)
        {
            return Page("sickleave", "FrmRegsterSickleave", "legacy-sick-leaves", searchText, page, pageSize);
        }

        public ActionResult CompensationAdjustments(string searchText, int page = 1, int pageSize = 40)
        {
            return Page("adjustments", "FrmChangedComponentData", "legacy-compensation-adjustments", searchText, page, pageSize);
        }

        public ActionResult EmployeeAllocations(string searchText, int page = 1, int pageSize = 40)
        {
            return Page("allocations", "FrmChangedComponentData1", "legacy-employee-allocations", searchText, page, pageSize);
        }

        [HttpGet]
        public JsonResult ComponentDetails(int id)
        {
            if (!_permissionService.CanView(MainErpUserContext, "MOFRAD"))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية عرض مكونات الرواتب." }, JsonRequestBehavior.AllowGet);
            }

            var data = _service.GetComponent(id);
            return Json(new { success = data != null, data, message = data == null ? "لم يتم العثور على المكون." : "" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SaveComponent(PayrollComponentEditViewModel request)
        {
            var isEdit = request != null && request.Id.GetValueOrDefault() > 0;
            if (isEdit && !_permissionService.CanEdit(MainErpUserContext, "MOFRAD"))
            {
                Response.StatusCode = 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية تعديل مكونات الرواتب." });
            }

            if (!isEdit && !_permissionService.CanAdd(MainErpUserContext, "MOFRAD"))
            {
                Response.StatusCode = 403;
                return Json(new LegacyHrFinanceSaveResult { Success = false, Message = "ليست لديك صلاحية إضافة مكون راتب." });
            }

            var result = _service.SaveComponent(request);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        private ActionResult Page(string module, string screenName, string activeScreen, string searchText, int page, int pageSize)
        {
            if (!_permissionService.CanView(MainErpUserContext, screenName))
            {
                return new HttpUnauthorizedResult("ليست لديك صلاحية عرض الشاشة.");
            }

            var model = _service.Load(module, searchText, page, pageSize);
            model.Permissions = new LegacyHrFinancePermissionsViewModel
            {
                CanView = true,
                CanAdd = _permissionService.CanAdd(MainErpUserContext, screenName),
                CanEdit = _permissionService.CanEdit(MainErpUserContext, screenName),
                CanDelete = _permissionService.CanDelete(MainErpUserContext, screenName)
            };
            ViewBag.ActiveScreen = activeScreen;
            return View("Index", model);
        }
    }
}
