using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.FinancialAdministration;
using MyERP.Areas.MainErp.Services.FinancialAdministration;
using MyERP.Areas.MainErp.ViewModels.FinancialAdministration;

namespace MyERP.Areas.MainErp.Controllers
{
    public class FinancialAdministrationController : MainErpControllerBase
    {
        private const string BanksScreenName = "FrmBanksData";
        private const string BoxesScreenName = "FrmBoxesData";
        private readonly FinancialAdministrationService _service;
        private readonly LegacyScreenPermissionService _permissionService;

        public FinancialAdministrationController()
            : this(new FinancialAdministrationService(new FinancialAdministrationRepository(new MainErpDbConnectionFactory())),
                  new LegacyScreenPermissionService(new MainErpDbConnectionFactory()))
        {
        }

        public FinancialAdministrationController(FinancialAdministrationService service, LegacyScreenPermissionService permissionService)
        {
            _service = service;
            _permissionService = permissionService;
        }

        public ActionResult Index(FinancialAdministrationSearchViewModel search)
        {
            var canBanks = _permissionService.CanView(MainErpUserContext, BanksScreenName);
            var canBoxes = _permissionService.CanView(MainErpUserContext, BoxesScreenName);
            if (!canBanks && !canBoxes)
            {
                return new HttpUnauthorizedResult("ليست لديك صلاحية عرض إدارة البنوك والصناديق.");
            }

            var model = _service.LoadIndex(search);
            model.Permissions = new FinancialAdministrationPermissionViewModel
            {
                CanViewBanks = canBanks,
                CanViewBoxes = canBoxes,
                CanAddBanks = _permissionService.CanAdd(MainErpUserContext, BanksScreenName),
                CanEditBanks = _permissionService.CanEdit(MainErpUserContext, BanksScreenName),
                CanAddBoxes = _permissionService.CanAdd(MainErpUserContext, BoxesScreenName),
                CanEditBoxes = _permissionService.CanEdit(MainErpUserContext, BoxesScreenName)
            };

            ViewBag.ActiveScreen = "financial-administration";
            return View(model);
        }

        [HttpGet]
        public JsonResult BankDetails(int id)
        {
            if (!_permissionService.CanView(MainErpUserContext, BanksScreenName))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية عرض البنوك." }, JsonRequestBehavior.AllowGet);
            }

            var bank = _service.GetBank(id);
            return Json(new { success = bank != null, data = bank, message = bank == null ? "لم يتم العثور على البنك." : "" }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult BoxDetails(int id)
        {
            if (!_permissionService.CanView(MainErpUserContext, BoxesScreenName))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية عرض الصناديق." }, JsonRequestBehavior.AllowGet);
            }

            var box = _service.GetBox(id);
            return Json(new { success = box != null, data = box, message = box == null ? "لم يتم العثور على الصندوق." : "" }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult AccountLookup(string term, int limit = 20)
        {
            if (!_permissionService.CanView(MainErpUserContext, BanksScreenName) && !_permissionService.CanView(MainErpUserContext, BoxesScreenName))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, rows = new object[0], message = "ليست لديك صلاحية البحث في الحسابات." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, rows = _service.SearchAccounts(term, limit) }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SaveBank(FinancialBankEditViewModel request)
        {
            var isEdit = request != null && request.BankId.GetValueOrDefault() > 0;
            if (isEdit && !_permissionService.CanEdit(MainErpUserContext, BanksScreenName))
            {
                Response.StatusCode = 403;
                return Json(new FinancialAdministrationSaveResult { Success = false, Message = "ليست لديك صلاحية تعديل البنوك." });
            }

            if (!isEdit && !_permissionService.CanAdd(MainErpUserContext, BanksScreenName))
            {
                Response.StatusCode = 403;
                return Json(new FinancialAdministrationSaveResult { Success = false, Message = "ليست لديك صلاحية إضافة بنك." });
            }

            var result = _service.SaveBank(request);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SaveBox(FinancialBoxEditViewModel request)
        {
            var isEdit = request != null && request.BoxId.GetValueOrDefault() > 0;
            if (isEdit && !_permissionService.CanEdit(MainErpUserContext, BoxesScreenName))
            {
                Response.StatusCode = 403;
                return Json(new FinancialAdministrationSaveResult { Success = false, Message = "ليست لديك صلاحية تعديل الصناديق." });
            }

            if (!isEdit && !_permissionService.CanAdd(MainErpUserContext, BoxesScreenName))
            {
                Response.StatusCode = 403;
                return Json(new FinancialAdministrationSaveResult { Success = false, Message = "ليست لديك صلاحية إضافة صندوق." });
            }

            var result = _service.SaveBox(request);
            if (!result.Success) { Response.StatusCode = 400; }
            return Json(result);
        }
    }
}
