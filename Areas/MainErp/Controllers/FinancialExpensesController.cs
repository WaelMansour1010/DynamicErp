using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.FinancialExpenses;
using MyERP.Areas.MainErp.Services.FinancialExpenses;
using MyERP.Areas.MainErp.ViewModels.FinancialExpenses;

namespace MyERP.Areas.MainErp.Controllers
{
    public class FinancialExpensesController : MainErpControllerBase
    {
        private readonly FinancialExpensesService _service;
        private readonly LegacyScreenPermissionService _permissionService;

        public FinancialExpensesController()
            : this(new FinancialExpensesService(new FinancialExpensesRepository(new MainErpDbConnectionFactory())), new LegacyScreenPermissionService(new MainErpDbConnectionFactory()))
        {
        }

        public FinancialExpensesController(FinancialExpensesService service, LegacyScreenPermissionService permissionService)
        {
            _service = service;
            _permissionService = permissionService;
        }

        public ActionResult Index(string mode, string searchText)
        {
            var user = MainErpUserContext;
            if (!_permissionService.CanView(user, "FrmExpenses3") && !_permissionService.CanView(user, "FrmExpenses30"))
            {
                return new HttpUnauthorizedResult("الصلاحية غير كافية لعرض الفواتير المالية.");
            }

            var model = _service.LoadIndex(mode, searchText);
            model.Permissions.CanView = true;
            model.Permissions.CanAdd = _permissionService.CanAdd(user, "FrmExpenses3") || _permissionService.CanAdd(user, "FrmExpenses30");
            model.Permissions.CanEdit = _permissionService.CanEdit(user, "FrmExpenses3") || _permissionService.CanEdit(user, "FrmExpenses30");
            model.Permissions.CanDelete = _permissionService.CanDelete(user, "FrmExpenses3") || _permissionService.CanDelete(user, "FrmExpenses30");

            ViewBag.ActiveScreen = "financial-expenses";
            return View(model);
        }

        [HttpGet]
        public JsonResult Details(int id)
        {
            var data = _service.GetDetails(id);
            return Json(new { success = data != null, message = data == null ? "لم يتم العثور على المستند." : "", data }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Save(FinancialExpenseSaveRequest request)
        {
            var user = MainErpUserContext;
            if (request != null && request.Id.HasValue && request.Id.Value > 0)
            {
                if (!_permissionService.CanEdit(user, "FrmExpenses3") && !_permissionService.CanEdit(user, "FrmExpenses30"))
                {
                    return Json(new FinancialExpenseSaveResult { Success = false, Message = "الصلاحية غير كافية للتعديل." });
                }
            }
            else if (!_permissionService.CanAdd(user, "FrmExpenses3") && !_permissionService.CanAdd(user, "FrmExpenses30"))
            {
                return Json(new FinancialExpenseSaveResult { Success = false, Message = "الصلاحية غير كافية للإضافة." });
            }

            return Json(_service.Save(request, user));
        }

        [HttpPost]
        public JsonResult Delete(int id)
        {
            if (!_permissionService.CanDelete(MainErpUserContext, "FrmExpenses3") && !_permissionService.CanDelete(MainErpUserContext, "FrmExpenses30"))
            {
                return Json(new FinancialExpenseSaveResult { Success = false, Message = "الصلاحية غير كافية للحذف." });
            }

            return Json(_service.Delete(id));
        }
    }
}
