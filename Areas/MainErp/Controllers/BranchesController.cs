using System;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.Branches;
using MyERP.Areas.MainErp.Services.Branches;
using MyERP.Areas.MainErp.ViewModels.Branches;

namespace MyERP.Areas.MainErp.Controllers
{
    public class BranchesController : MainErpControllerBase
    {
        private const string ScreenName = "baranches";
        private readonly BranchesService _service;
        private readonly LegacyScreenPermissionService _permissionService;

        public BranchesController()
            : this(new BranchesService(new BranchesRepository(new MainErpDbConnectionFactory())), new LegacyScreenPermissionService(new MainErpDbConnectionFactory()))
        {
        }

        public BranchesController(BranchesService service, LegacyScreenPermissionService permissionService)
        {
            _service = service;
            _permissionService = permissionService;
        }

        public ActionResult Index(string searchText, int? id)
        {
            if (!_permissionService.CanView(MainErpUserContext, ScreenName))
            {
                return new HttpUnauthorizedResult("الصلاحية غير كافية لعرض شاشة الفروع وربط الحسابات.");
            }

            var model = _service.LoadIndex(searchText, id);
            model.Permissions = BuildPermissions();
            ViewBag.ActiveScreen = "branches";
            return View(model);
        }

        [HttpGet]
        public JsonResult New()
        {
            if (!_permissionService.CanAdd(MainErpUserContext, ScreenName))
            {
                return Json(new { success = false, message = "الصلاحية غير كافية لإضافة فرع جديد." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = _service.New() }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult Details(int id)
        {
            if (!_permissionService.CanView(MainErpUserContext, ScreenName))
            {
                return Json(new { success = false, message = "الصلاحية غير كافية لعرض بيانات الفرع." }, JsonRequestBehavior.AllowGet);
            }

            var data = _service.Get(id);
            return Json(new { success = data != null, data, message = data == null ? "لم يتم العثور على الفرع." : "" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Save(BranchEditViewModel request)
        {
            var isEdit = request != null && request.BranchId.HasValue && request.BranchId.Value > 0 && _service.Get(request.BranchId.Value) != null;
            if (isEdit && !_permissionService.CanEdit(MainErpUserContext, ScreenName))
            {
                return Json(new BranchSaveResult { Success = false, Message = "الصلاحية غير كافية لتعديل بيانات الفرع." });
            }

            if (!isEdit && !_permissionService.CanAdd(MainErpUserContext, ScreenName))
            {
                return Json(new BranchSaveResult { Success = false, Message = "الصلاحية غير كافية لإضافة فرع جديد." });
            }

            try
            {
                return Json(_service.Save(request));
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new BranchSaveResult { Success = false, Message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Delete(int id)
        {
            if (!_permissionService.CanDelete(MainErpUserContext, ScreenName))
            {
                return Json(new BranchSaveResult { Success = false, Message = "الصلاحية غير كافية لحذف الفرع." });
            }

            try
            {
                return Json(_service.Delete(id));
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new BranchSaveResult { Success = false, Message = "تعذر حذف الفرع لأنه مرتبط بحركات أو مستخدم في النظام. " + ex.Message });
            }
        }

        private BranchPermissionsViewModel BuildPermissions()
        {
            return new BranchPermissionsViewModel
            {
                CanView = _permissionService.CanView(MainErpUserContext, ScreenName),
                CanAdd = _permissionService.CanAdd(MainErpUserContext, ScreenName),
                CanEdit = _permissionService.CanEdit(MainErpUserContext, ScreenName),
                CanDelete = _permissionService.CanDelete(MainErpUserContext, ScreenName)
            };
        }
    }
}
