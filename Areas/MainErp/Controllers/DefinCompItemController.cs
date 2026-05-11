using System;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.DefinCompItem;
using MyERP.Areas.MainErp.Services.DefinCompItem;
using MyERP.Areas.MainErp.ViewModels.DefinCompItem;

namespace MyERP.Areas.MainErp.Controllers
{
    public class DefinCompItemController : MainErpControllerBase
    {
        private const string ScreenName = "FrmDefinCompItem";
        private readonly DefinCompItemService _service;
        private readonly LegacyScreenPermissionService _permissionService;

        public DefinCompItemController()
            : this(new DefinCompItemService(new DefinCompItemRepository(new MainErpDbConnectionFactory())), new LegacyScreenPermissionService(new MainErpDbConnectionFactory()))
        {
        }

        public DefinCompItemController(DefinCompItemService service, LegacyScreenPermissionService permissionService)
        {
            _service = service;
            _permissionService = permissionService;
        }

        public ActionResult Index(string searchText, DateTime? fromDate, DateTime? toDate, int? branchId, int? storeId, int? id, int page = 1, int pageSize = 20)
        {
            var user = MainErpUserContext;
            if (!_permissionService.CanView(user, ScreenName))
            {
                return new HttpUnauthorizedResult("لا تملك صلاحية عرض شاشة سند التجميع.");
            }

            var model = _service.LoadIndex(searchText, fromDate, toDate, branchId, storeId, page, pageSize, user);
            model.Permissions.CanView = true;
            model.Permissions.CanAdd = _permissionService.CanAdd(user, ScreenName);
            model.Permissions.CanEdit = _permissionService.CanEdit(user, ScreenName);
            model.Permissions.CanDelete = _permissionService.CanDelete(user, ScreenName);
            model.Permissions.CanPrint = _permissionService.CanPrint(user, ScreenName);

            if (id.HasValue && id.Value > 0)
            {
                model.Selected = _service.GetDetails(id.Value);
            }

            ViewBag.ActiveScreen = "defin-comp-item";
            return View(model);
        }

        [HttpGet]
        public JsonResult Details(int id)
        {
            if (!_permissionService.CanView(MainErpUserContext, ScreenName))
            {
                return Json(new { success = false, message = "لا تملك صلاحية عرض البيانات." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = _service.GetDetails(id) }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult LookupItems(string term)
        {
            if (!_permissionService.CanView(MainErpUserContext, ScreenName))
            {
                return Json(new { success = false, message = "لا تملك صلاحية العرض." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, items = _service.SearchItems(term, 12) }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult LookupUnits(int itemId)
        {
            if (!_permissionService.CanView(MainErpUserContext, ScreenName))
            {
                return Json(new { success = false, message = "لا تملك صلاحية العرض." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, items = _service.SearchUnits(itemId) }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult LookupBranches()
        {
            return Json(new { success = true, items = _service.LoadBranches() }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult LookupStores(int? branchId)
        {
            return Json(new { success = true, items = _service.LoadStores(branchId) }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult LookupCustomers()
        {
            return Json(new { success = true, items = _service.LoadCustomers() }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Save(DefinCompItemSaveRequest request)
        {
            var user = MainErpUserContext;
            if (request != null && request.Id.HasValue && request.Id.Value > 0)
            {
                if (!_permissionService.CanEdit(user, ScreenName))
                {
                    return Json(new DefinCompItemSaveResult { Success = false, Message = "لا تملك صلاحية تعديل سند التجميع." });
                }
            }
            else if (!_permissionService.CanAdd(user, ScreenName))
            {
                return Json(new DefinCompItemSaveResult { Success = false, Message = "لا تملك صلاحية إضافة سند تجميع جديد." });
            }

            return Json(_service.Save(request, user));
        }

        [HttpPost]
        public JsonResult Delete(int id)
        {
            if (!_permissionService.CanDelete(MainErpUserContext, ScreenName))
            {
                return Json(new DefinCompItemSaveResult { Success = false, Message = "لا تملك صلاحية حذف أو إلغاء سند التجميع." });
            }

            return Json(_service.Delete(id, MainErpUserContext));
        }

        [HttpPost]
        public JsonResult Rebuild(DefinCompItemSaveRequest request)
        {
            if (!_permissionService.CanEdit(MainErpUserContext, ScreenName))
            {
                return Json(new DefinCompItemSaveResult { Success = false, Message = "لا تملك صلاحية إعادة توليد السند." });
            }

            if (request == null || !request.Id.HasValue)
            {
                return Json(new DefinCompItemSaveResult { Success = false, Message = "يجب تحديد السند المراد إعادة توليده." });
            }

            request.ForceRebuild = true;
            return Json(_service.Save(request, MainErpUserContext));
        }
    }
}
