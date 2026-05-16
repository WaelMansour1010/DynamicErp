using System;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.Options;
using MyERP.Areas.MainErp.Services.Options;
using MyERP.Areas.MainErp.ViewModels.Options;

namespace MyERP.Areas.MainErp.Controllers
{
    public class OptionsController : MainErpControllerBase
    {
        private const string ScreenName = "FrmOptions";
        private readonly OptionsService _service;
        private readonly LegacyScreenPermissionService _permissionService;

        public OptionsController()
            : this(new OptionsService(new OptionsRepository(new MainErpDbConnectionFactory())), new LegacyScreenPermissionService(new MainErpDbConnectionFactory()))
        {
        }

        public OptionsController(OptionsService service, LegacyScreenPermissionService permissionService)
        {
            _service = service;
            _permissionService = permissionService;
        }

        public ActionResult Index()
        {
            if (!_permissionService.CanView(MainErpUserContext, ScreenName))
            {
                return new HttpUnauthorizedResult("الصلاحية غير كافية لعرض شاشة إعدادات النظام.");
            }

            var model = _service.Load();
            model.Permissions = BuildPermissions();
            ViewBag.ActiveScreen = "options";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Save(OptionSaveRequest request)
        {
            if (!_permissionService.CanEdit(MainErpUserContext, ScreenName))
            {
                return Json(new OptionsSaveResult { Success = false, Message = "الصلاحية غير كافية لتعديل إعدادات النظام." });
            }

            try
            {
                return Json(_service.Save(request));
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new OptionsSaveResult { Success = false, Message = ex.Message });
            }
        }

        private OptionsPermissionsViewModel BuildPermissions()
        {
            return new OptionsPermissionsViewModel
            {
                CanView = _permissionService.CanView(MainErpUserContext, ScreenName),
                CanEdit = _permissionService.CanEdit(MainErpUserContext, ScreenName)
            };
        }
    }
}
