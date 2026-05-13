using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.LegacyOperations;
using MyERP.Areas.MainErp.Services.LegacyOperations;
using MyERP.Areas.MainErp.ViewModels.LegacyOperations;

namespace MyERP.Areas.MainErp.Controllers
{
    public class LegacyOperationsController : MainErpControllerBase
    {
        private readonly LegacyOperationsService _service;
        private readonly LegacyScreenPermissionService _permissionService;

        public LegacyOperationsController()
            : this(new LegacyOperationsService(new LegacyOperationsRepository(new MainErpDbConnectionFactory())), new LegacyScreenPermissionService(new MainErpDbConnectionFactory()))
        {
        }

        public LegacyOperationsController(LegacyOperationsService service, LegacyScreenPermissionService permissionService)
        {
            _service = service;
            _permissionService = permissionService;
        }

        public ActionResult Index()
        {
            if (!CanAnyView())
            {
                return new HttpUnauthorizedResult("الصلاحية غير كافية لعرض الشاشات المحولة.");
            }

            ViewBag.ActiveScreen = "legacy-operations";
            return View(_service.LoadIndex());
        }

        [HttpGet] public JsonResult Boxes(string search) { return Json(new { success = true, data = _service.SearchBoxes(search) }, JsonRequestBehavior.AllowGet); }
        [HttpGet] public JsonResult Box(int id) { var data = _service.GetBox(id); return Json(new { success = data != null, data }, JsonRequestBehavior.AllowGet); }
        [HttpGet] public JsonResult Cashing(string search) { return Json(new { success = true, data = _service.SearchCashing(search) }, JsonRequestBehavior.AllowGet); }
        [HttpGet] public JsonResult CashingDetails(int id) { var data = _service.GetCashing(id); return Json(new { success = data != null, data }, JsonRequestBehavior.AllowGet); }
        [HttpGet] public JsonResult CarMaintenance(string search) { return Json(new { success = true, data = _service.SearchCarMaintenance(search) }, JsonRequestBehavior.AllowGet); }
        [HttpGet] public JsonResult CarMaintenanceDetails(int id) { var data = _service.GetCarMaintenance(id); return Json(new { success = data != null, data }, JsonRequestBehavior.AllowGet); }
        [HttpGet] public JsonResult Cars(string search) { return Json(new { success = true, data = _service.SearchCars(search) }, JsonRequestBehavior.AllowGet); }
        [HttpGet] public JsonResult CarData(int id) { var data = _service.GetCarData(id); return Json(new { success = data != null, data }, JsonRequestBehavior.AllowGet); }
        [HttpGet] public JsonResult CarAuthorizations(string search) { return Json(new { success = true, data = _service.SearchCarAuthorizations(search) }, JsonRequestBehavior.AllowGet); }
        [HttpGet] public JsonResult CarAuthorizationDetails(int id) { var data = _service.GetCarAuthorization(id); return Json(new { success = data != null, data }, JsonRequestBehavior.AllowGet); }
        [HttpGet] public JsonResult Attachments(string screenName, int recordId) { return Json(new { success = true, data = _service.GetAttachments(screenName, recordId) }, JsonRequestBehavior.AllowGet); }

        [HttpPost] public JsonResult SaveBox(CashBoxDetails request) { return Json(CheckSave("FrmBoxesData", request != null && request.Id > 0) ?? _service.SaveBox(request)); }
        [HttpPost] public JsonResult SaveCashing(GeneralCashingDetails request) { return Json(CheckSave("FrmBankDeposite3", request != null && request.Id > 0) ?? _service.SaveCashing(request, MainErpUserContext)); }
        [HttpPost] public JsonResult SaveCarMaintenance(CarMaintenanceDetails request) { return Json(CheckSave("FrmBillCarMaintExtra", request != null && request.Id > 0) ?? _service.SaveCarMaintenance(request, MainErpUserContext)); }
        [HttpPost] public JsonResult SaveCarData(CarDataDetails request) { return Json(CheckSave("FrmCars", request != null && request.Id > 0) ?? _service.SaveCarData(request)); }
        [HttpPost] public JsonResult SaveCarAuthorization(CarAuthorizationDetails request) { return Json(CheckSave("FrmCarAuthontication", request != null && request.Id > 0) ?? _service.SaveCarAuthorization(request, MainErpUserContext)); }
        [HttpPost] public JsonResult DeleteBox(int id) { return Json(CheckDelete("FrmBoxesData") ?? _service.DeleteBox(id)); }
        [HttpPost] public JsonResult DeleteCashing(int id) { return Json(CheckDelete("FrmBankDeposite3") ?? _service.DeleteCashing(id)); }
        [HttpPost] public JsonResult DeleteCarMaintenance(int id) { return Json(CheckDelete("FrmBillCarMaintExtra") ?? _service.DeleteCarMaintenance(id)); }
        [HttpPost] public JsonResult DeleteCarData(int id) { return Json(CheckDelete("FrmCars") ?? _service.DeleteCarData(id)); }
        [HttpPost] public JsonResult DeleteCarAuthorization(int id) { return Json(CheckDelete("FrmCarAuthontication") ?? _service.DeleteCarAuthorization(id)); }
        [HttpPost] public JsonResult SetPrimaryAttachment(int id) { return Json(_service.SetPrimaryAttachment(id)); }

        [HttpPost]
        public JsonResult UploadAttachment(string screenName, int recordId, string caption)
        {
            var permission = CheckSave(screenName, true);
            if (permission != null) return Json(permission);
            if (!IsAttachmentScreen(screenName)) return Json(new LegacySaveResult { Success = false, Message = "نوع الشاشة غير مسموح للمرفقات." });
            if (recordId <= 0) return Json(new LegacySaveResult { Success = false, Message = "احفظ السجل أولا قبل رفع الصور." });
            var file = Request.Files.Count > 0 ? Request.Files[0] : null;
            if (file == null || file.ContentLength <= 0) return Json(new LegacySaveResult { Success = false, Message = "لم يتم اختيار ملف." });
            if (!IsAllowedAttachment(file)) return Json(new LegacySaveResult { Success = false, Message = "نوع الملف غير مسموح. استخدم صور JPG/PNG/WebP أو PDF بحد أقصى 10MB." });

            var extension = Path.GetExtension(file.FileName);
            var safeName = Guid.NewGuid().ToString("N") + extension;
            var virtualDir = "/Uploads/MainErp/LegacyOperations/" + screenName + "/" + recordId + "/";
            var physicalDir = Server.MapPath("~" + virtualDir);
            Directory.CreateDirectory(physicalDir);
            file.SaveAs(Path.Combine(physicalDir, safeName));

            var item = _service.AddAttachment(screenName, recordId, Path.GetFileName(file.FileName), virtualDir + safeName, file.ContentType, file.ContentLength, caption, MainErpUserContext == null ? null : (int?)MainErpUserContext.UserId);
            return Json(new { Success = true, success = true, Message = "تم رفع المرفق.", data = item });
        }

        [HttpPost]
        public JsonResult DeleteAttachment(int id)
        {
            var item = _service.GetAttachment(id);
            if (item == null) return Json(new LegacySaveResult { Success = false, Message = "المرفق غير موجود." });
            var permission = CheckDelete(item.ScreenName);
            if (permission != null) return Json(permission);
            var result = _service.DeleteAttachment(id);
            if (result.Success)
            {
                var physical = Server.MapPath("~" + item.FilePath);
                if (System.IO.File.Exists(physical)) System.IO.File.Delete(physical);
            }
            return Json(result);
        }

        private bool CanAnyView()
        {
            var user = MainErpUserContext;
            return _permissionService.CanView(user, "FrmBoxesData") || _permissionService.CanView(user, "FrmBankDeposite3") || _permissionService.CanView(user, "FrmBillCarMaintExtra") || _permissionService.CanView(user, "FrmCars") || _permissionService.CanView(user, "FrmCarAuthontication");
        }

        private LegacySaveResult CheckSave(string screenName, bool edit)
        {
            var allowed = edit ? _permissionService.CanEdit(MainErpUserContext, screenName) : _permissionService.CanAdd(MainErpUserContext, screenName);
            return allowed ? null : new LegacySaveResult { Success = false, Message = edit ? "الصلاحية غير كافية للتعديل." : "الصلاحية غير كافية للإضافة." };
        }

        private LegacySaveResult CheckDelete(string screenName)
        {
            return _permissionService.CanDelete(MainErpUserContext, screenName) ? null : new LegacySaveResult { Success = false, Message = "الصلاحية غير كافية للحذف." };
        }
        private static bool IsAttachmentScreen(string screenName)
        {
            return screenName == "FrmCars" || screenName == "FrmCarAuthontication";
        }

        private static bool IsAllowedAttachment(HttpPostedFileBase file)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
            var extension = (Path.GetExtension(file.FileName) ?? string.Empty).ToLowerInvariant();
            return allowed.Contains(extension) && file.ContentLength <= 10 * 1024 * 1024;
        }
    }
}
