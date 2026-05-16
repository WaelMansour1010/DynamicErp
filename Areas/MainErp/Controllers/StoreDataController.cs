using System.Linq;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Common.StoreData;

namespace MyERP.Areas.MainErp.Controllers
{
    public class StoreDataController : MainErpControllerBase
    {
        private const string ScreenName = "FrmStoreData";
        private readonly StoreDataRepository _repository;
        private readonly LegacyScreenPermissionService _permissionService;

        public StoreDataController()
            : this(
                new StoreDataRepository(() => new MainErpDbConnectionFactory().CreateOpenConnection()),
                new LegacyScreenPermissionService(new MainErpDbConnectionFactory()))
        {
        }

        public StoreDataController(StoreDataRepository repository, LegacyScreenPermissionService permissionService)
        {
            _repository = repository;
            _permissionService = permissionService;
        }

        public ActionResult Index(string searchText, int? branchId, string mode, int? id, string message)
        {
            var context = MainErpUserContext;
            if (!_permissionService.CanView(context, ScreenName))
            {
                return new HttpUnauthorizedResult("الصلاحية غير كافية لعرض بيانات المخازن.");
            }

            var model = _repository.LoadIndex(new StoreDataSearchRequest
            {
                SearchText = searchText,
                BranchId = branchId,
                Mode = mode
            }, id);
            model.Permissions.CanView = true;
            model.Permissions.CanAdd = _permissionService.CanAdd(context, ScreenName);
            model.Permissions.CanEdit = _permissionService.CanEdit(context, ScreenName);
            model.Permissions.CanDelete = _permissionService.CanDelete(context, ScreenName);
            model.SuccessMessage = message;

            ViewBag.ActiveScreen = "store-data";
            return View(model);
        }

        public ActionResult New(int? branchId)
        {
            return RedirectToAction("Index", new { branchId, id = 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(StoreDataEditViewModel model)
        {
            var context = MainErpUserContext;
            var isEdit = model != null && model.StoreId > 0;
            if (isEdit && !_permissionService.CanEdit(context, ScreenName))
            {
                TempData["StoreDataMessage"] = "الصلاحية غير كافية لتعديل المخزن.";
                return RedirectToAction("Index", new { id = model.StoreId });
            }

            if (!isEdit && !_permissionService.CanAdd(context, ScreenName))
            {
                TempData["StoreDataMessage"] = "الصلاحية غير كافية لإضافة مخزن.";
                return RedirectToAction("Index");
            }

            if (model != null)
            {
                model.UserIds = Request.Form.GetValues("UserIds") == null
                    ? new System.Collections.Generic.List<int>()
                    : Request.Form.GetValues("UserIds").Select(x =>
                    {
                        int value;
                        return int.TryParse(x, out value) ? value : 0;
                    }).Where(x => x > 0).Distinct().ToList();
            }

            var result = _repository.Save(model);
            TempData["StoreDataMessage"] = result.Message;
            return RedirectToAction("Index", new { id = result.StoreId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int storeId)
        {
            if (!_permissionService.CanDelete(MainErpUserContext, ScreenName))
            {
                TempData["StoreDataMessage"] = "الصلاحية غير كافية لحذف المخزن.";
                return RedirectToAction("Index", new { id = storeId });
            }

            var result = _repository.Delete(storeId);
            TempData["StoreDataMessage"] = result.Message;
            return RedirectToAction("Index");
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            if (TempData.ContainsKey("StoreDataMessage"))
            {
                ViewBag.StoreDataMessage = TempData["StoreDataMessage"];
            }
        }
    }
}
