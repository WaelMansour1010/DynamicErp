using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.Customers;
using MyERP.Areas.MainErp.Services.Customers;
using MyERP.Areas.MainErp.ViewModels.Customers;

namespace MyERP.Areas.MainErp.Controllers
{
    public class CustomersController : MainErpControllerBase
    {
        private const string ScreenName = "FrmCustemers";
        private readonly CustomerService _service;
        private readonly LegacyScreenPermissionService _permissionService;

        public CustomersController()
            : this(new CustomerService(new CustomerRepository(new MainErpDbConnectionFactory())), new LegacyScreenPermissionService(new MainErpDbConnectionFactory()))
        {
        }

        public CustomersController(CustomerService service, LegacyScreenPermissionService permissionService)
        {
            _service = service;
            _permissionService = permissionService;
        }

        public ActionResult Index(string searchText, int? customerType, int? branchId, int? id, int page = 1, int pageSize = 20)
        {
            var user = MainErpUserContext;
            if (!_permissionService.CanView(user, ScreenName))
            {
                return new HttpUnauthorizedResult("الصلاحية غير كافية لعرض شاشة العملاء والموردين.");
            }

            var model = _service.LoadIndex(searchText, customerType, branchId, page, pageSize);
            model.Permissions = BuildPermissions();
            if (id.HasValue && id.Value > 0)
            {
                model.Selected = _service.Get(id.Value) ?? model.Selected;
            }

            ViewBag.ActiveScreen = "customers";
            return View(model);
        }

        [HttpGet]
        public JsonResult New()
        {
            if (!_permissionService.CanAdd(MainErpUserContext, ScreenName))
            {
                return Json(new { success = false, message = "الصلاحية غير كافية لإضافة عميل جديد." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = _service.New() }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult Details(int id)
        {
            if (!_permissionService.CanView(MainErpUserContext, ScreenName))
            {
                return Json(new { success = false, message = "الصلاحية غير كافية لعرض بيانات العميل." }, JsonRequestBehavior.AllowGet);
            }

            var data = _service.Get(id);
            if (data == null)
            {
                return Json(new { success = false, message = "لم يتم العثور على العميل المطلوب." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Save(CustomerEditViewModel request)
        {
            var user = MainErpUserContext;
            if (request != null && request.CusId.HasValue && request.CusId.Value > 0)
            {
                if (!_permissionService.CanEdit(user, ScreenName))
                {
                    return Json(new CustomerSaveResult { Success = false, Message = "الصلاحية غير كافية لتعديل بيانات العميل." });
                }
            }
            else if (!_permissionService.CanAdd(user, ScreenName))
            {
                return Json(new CustomerSaveResult { Success = false, Message = "الصلاحية غير كافية لإضافة عميل جديد." });
            }

            return Json(_service.Save(request, user));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Delete(int id)
        {
            if (!_permissionService.CanDelete(MainErpUserContext, ScreenName))
            {
                return Json(new CustomerSaveResult { Success = false, Message = "الصلاحية غير كافية لحذف بيانات العميل." });
            }

            return Json(_service.Delete(id));
        }

        private CustomerPermissionsViewModel BuildPermissions()
        {
            var user = MainErpUserContext;
            return new CustomerPermissionsViewModel
            {
                CanView = _permissionService.CanView(user, ScreenName),
                CanAdd = _permissionService.CanAdd(user, ScreenName),
                CanEdit = _permissionService.CanEdit(user, ScreenName),
                CanDelete = _permissionService.CanDelete(user, ScreenName),
                CanPrint = _permissionService.CanPrint(user, ScreenName)
            };
        }
    }
}
