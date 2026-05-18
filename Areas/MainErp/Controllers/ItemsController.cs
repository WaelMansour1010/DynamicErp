using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.Items;
using MyERP.Areas.MainErp.Services.Items;
using MyERP.Areas.MainErp.ViewModels.Items;

namespace MyERP.Areas.MainErp.Controllers
{
    public class ItemsController : MainErpControllerBase
    {
        private const string ScreenName = "FrmItems";
        private readonly ItemsService _service;
        private readonly LegacyScreenPermissionService _permissionService;

        public ItemsController()
            : this(new ItemsService(new ItemsRepository(new MainErpDbConnectionFactory())), new LegacyScreenPermissionService(new MainErpDbConnectionFactory()))
        {
        }

        public ItemsController(ItemsService service, LegacyScreenPermissionService permissionService)
        {
            _service = service;
            _permissionService = permissionService;
        }

        public ActionResult Index(string searchText, int? groupId, int? id, int page = 1, int pageSize = 20)
        {
            var user = MainErpUserContext;
            var isPosHosted = MainErpHostContext.IsPosHosted(Request, Session);
            if (!_permissionService.CanView(user, ScreenName))
            {
                return new HttpUnauthorizedResult("الصلاحية غير كافية لعرض شاشة الأصناف.");
            }

            var model = _service.LoadIndex(searchText, groupId, page, pageSize, user);
            model.IsPosHosted = isPosHosted;
            model.Permissions.CanView = true;
            model.Permissions.CanAdd = _permissionService.CanAdd(user, ScreenName);
            model.Permissions.CanEdit = _permissionService.CanEdit(user, ScreenName);
            model.Permissions.CanDelete = !isPosHosted && _permissionService.CanDelete(user, ScreenName);

            if (id.HasValue && id.Value > 0)
            {
                model.Selected = _service.GetDetails(id.Value);
            }

            ViewBag.ActiveScreen = "items";
            return View(model);
        }

        [HttpGet]
        public JsonResult Details(int id)
        {
            if (!_permissionService.CanView(MainErpUserContext, ScreenName))
            {
                return Json(new { success = false, message = "الصلاحية غير كافية لعرض الصنف." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = _service.GetDetails(id) }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult LookupGroups()
        {
            return Json(new { success = true, items = _service.LoadGroups() }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GroupDetails(int id)
        {
            if (!_permissionService.CanView(MainErpUserContext, ScreenName))
            {
                return Json(new { success = false, message = "الصلاحية غير كافية لعرض المجموعة." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = _service.GetGroupDetails(id) }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveGroup(GroupSaveRequest request)
        {
            var user = MainErpUserContext;
            if (request != null && request.Id.HasValue && request.Id.Value > 0)
            {
                if (!_permissionService.CanEdit(user, ScreenName))
                {
                    return Json(new GroupSaveResult { Success = false, Message = "الصلاحية غير كافية لتعديل المجموعة." });
                }
            }
            else if (!_permissionService.CanAdd(user, ScreenName))
            {
                return Json(new GroupSaveResult { Success = false, Message = "الصلاحية غير كافية لإضافة مجموعة." });
            }

            return Json(_service.SaveGroup(request, user));
        }

        [HttpPost]
        public JsonResult DeleteGroup(int id)
        {
            if (!_permissionService.CanDelete(MainErpUserContext, ScreenName))
            {
                return Json(new GroupSaveResult { Success = false, Message = "الصلاحية غير كافية لحذف المجموعة." });
            }

            return Json(_service.DeleteGroup(id));
        }

        [HttpGet]
        public JsonResult LookupUnits()
        {
            return Json(new { success = true, items = _service.LoadUnits() }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Save(ItemSaveRequest request)
        {
            var user = MainErpUserContext;
            if (request != null && request.Id.HasValue && request.Id.Value > 0)
            {
                if (!_permissionService.CanEdit(user, ScreenName))
                {
                    return Json(new ItemSaveResult { Success = false, Message = "الصلاحية غير كافية لتعديل الصنف." });
                }
            }
            else if (!_permissionService.CanAdd(user, ScreenName))
            {
                return Json(new ItemSaveResult { Success = false, Message = "الصلاحية غير كافية لإضافة صنف جديد." });
            }

            return Json(_service.Save(request, user));
        }

        [HttpPost]
        public JsonResult Delete(int id)
        {
            if (!_permissionService.CanDelete(MainErpUserContext, ScreenName))
            {
                return Json(new ItemSaveResult { Success = false, Message = "الصلاحية غير كافية لحذف الصنف." });
            }

            return Json(_service.Delete(id, MainErpUserContext));
        }
    }
}
