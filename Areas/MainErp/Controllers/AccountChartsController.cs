using System.Net;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Services.AccountCharts;
using MyERP.Areas.MainErp.ViewModels.AccountCharts;

namespace MyERP.Areas.MainErp.Controllers
{
    public class AccountChartsController : MainErpControllerBase
    {
        private const string ScreenName = "FrmAccountCharts";
        private readonly AccountChartsService _accountChartsService;
        private readonly LegacyScreenPermissionService _permissionService;

        public AccountChartsController()
            : this(
                new AccountChartsService(new MainErpDbConnectionFactory()),
                new LegacyScreenPermissionService(new MainErpDbConnectionFactory()))
        {
        }

        public AccountChartsController(AccountChartsService accountChartsService, LegacyScreenPermissionService permissionService)
        {
            _accountChartsService = accountChartsService;
            _permissionService = permissionService;
        }

        public ActionResult Index()
        {
            if (!_permissionService.CanView(MainErpUserContext, ScreenName))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "ليست لديك صلاحية فتح دليل الحسابات.");
            }

            ViewBag.ActiveScreen = "account-charts";
            var model = _accountChartsService.GetIndexModel(GetPermissions());
            return View(model);
        }

        [HttpGet]
        public JsonResult Tree()
        {
            if (!_permissionService.CanView(MainErpUserContext, ScreenName))
            {
                return Json(new { success = false, message = "ليست لديك صلاحية عرض دليل الحسابات." }, JsonRequestBehavior.AllowGet);
            }

            return new JsonResult
            {
                Data = new { success = true, items = _accountChartsService.GetTree() },
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                MaxJsonLength = int.MaxValue
            };
        }

        [HttpGet]
        public JsonResult Details(string accountCode)
        {
            if (!_permissionService.CanView(MainErpUserContext, ScreenName))
            {
                return Json(new { success = false, message = "ليست لديك صلاحية عرض الحساب." }, JsonRequestBehavior.AllowGet);
            }

            var account = _accountChartsService.GetAccount(accountCode);
            if (account == null)
            {
                return Json(new { success = false, message = "الحساب غير موجود." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, account }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Create(AccountSaveRequest request)
        {
            if (!_permissionService.CanAdd(MainErpUserContext, ScreenName))
            {
                return Json(new { success = false, message = "ليست لديك صلاحية إضافة حساب." });
            }

            var result = _accountChartsService.Save(request, true);
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Update(AccountSaveRequest request)
        {
            if (!_permissionService.CanEdit(MainErpUserContext, ScreenName))
            {
                return Json(new { success = false, message = "ليست لديك صلاحية تعديل الحساب." });
            }

            var result = _accountChartsService.Save(request, false);
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Delete(string accountCode)
        {
            if (!_permissionService.CanDelete(MainErpUserContext, ScreenName))
            {
                return Json(new { success = false, message = "ليست لديك صلاحية حذف الحساب." });
            }

            var result = _accountChartsService.Delete(accountCode);
            return Json(result);
        }

        private AccountChartsPermissionsViewModel GetPermissions()
        {
            return new AccountChartsPermissionsViewModel
            {
                CanView = _permissionService.CanView(MainErpUserContext, ScreenName),
                CanAdd = _permissionService.CanAdd(MainErpUserContext, ScreenName),
                CanEdit = _permissionService.CanEdit(MainErpUserContext, ScreenName),
                CanDelete = _permissionService.CanDelete(MainErpUserContext, ScreenName),
                CanPrint = _permissionService.CanPrint(MainErpUserContext, ScreenName)
            };
        }
    }
}
