using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.Stocktaking;
using MyERP.Areas.MainErp.Services.Stocktaking;
using MyERP.Areas.MainErp.ViewModels.Stocktaking;

namespace MyERP.Areas.MainErp.Controllers
{
    public class StocktakingController : MainErpControllerBase
    {
        private const string ScreenName = "FrmNewGard";
        private readonly StocktakingService _service;
        private readonly LegacyScreenPermissionService _permissionService;

        public StocktakingController()
            : this(new StocktakingService(new StocktakingRepository(new MainErpDbConnectionFactory())), new LegacyScreenPermissionService(new MainErpDbConnectionFactory()))
        {
        }

        public StocktakingController(StocktakingService service, LegacyScreenPermissionService permissionService)
        {
            _service = service;
            _permissionService = permissionService;
        }

        public ActionResult Index(string searchText, int? storeId, int? branchId, string mode)
        {
            var user = MainErpUserContext;
            if (!_permissionService.CanView(user, ScreenName) && !_permissionService.CanView(user, "FrmNewGard1"))
            {
                return new HttpUnauthorizedResult("الصلاحية غير كافية لعرض شاشة الجرد.");
            }

            var model = _service.LoadIndex(searchText, storeId, branchId, mode);
            model.Permissions.CanView = true;
            model.Permissions.CanAdd = _permissionService.CanAdd(user, ScreenName) || _permissionService.CanAdd(user, "FrmNewGard1");
            model.Permissions.CanEdit = _permissionService.CanEdit(user, ScreenName) || _permissionService.CanEdit(user, "FrmNewGard1");
            model.Permissions.CanDelete = _permissionService.CanDelete(user, ScreenName) || _permissionService.CanDelete(user, "FrmNewGard1");

            ViewBag.ActiveScreen = "stocktaking";
            return View(model);
        }

        [HttpGet]
        public JsonResult Details(int id)
        {
            if (!_permissionService.CanView(MainErpUserContext, ScreenName) && !_permissionService.CanView(MainErpUserContext, "FrmNewGard1"))
            {
                return Json(new { success = false, message = "الصلاحية غير كافية لعرض الجرد." }, JsonRequestBehavior.AllowGet);
            }

            var data = _service.GetDetails(id);
            return Json(new { success = data != null, message = data == null ? "لم يتم العثور على مستند الجرد." : "", data }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Save(StocktakingSaveRequest request)
        {
            var user = MainErpUserContext;
            if (request != null && request.Id.HasValue && request.Id.Value > 0)
            {
                if (!_permissionService.CanEdit(user, ScreenName) && !_permissionService.CanEdit(user, "FrmNewGard1"))
                {
                    return Json(new StocktakingSaveResult { Success = false, Message = "الصلاحية غير كافية لتعديل الجرد." });
                }
            }
            else if (!_permissionService.CanAdd(user, ScreenName) && !_permissionService.CanAdd(user, "FrmNewGard1"))
            {
                return Json(new StocktakingSaveResult { Success = false, Message = "الصلاحية غير كافية لإضافة جرد." });
            }

            return Json(_service.Save(request, user));
        }

        [HttpPost]
        public JsonResult Delete(int id)
        {
            if (!_permissionService.CanDelete(MainErpUserContext, ScreenName) && !_permissionService.CanDelete(MainErpUserContext, "FrmNewGard1"))
            {
                return Json(new StocktakingSaveResult { Success = false, Message = "الصلاحية غير كافية لحذف الجرد." });
            }

            return Json(_service.Delete(id));
        }
    }
}
