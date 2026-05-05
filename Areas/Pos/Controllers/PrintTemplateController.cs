using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using MyERP.Areas.Pos.Reports;
using MyERP.Areas.Pos.Services;
using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    [SkipERPAuthorize]
    public class PrintTemplateController : Controller
    {
        private readonly PrintTemplateService _service;
        private readonly PosSqlRepository _repository;

        public PrintTemplateController()
        {
            _service = new PrintTemplateService();
            _repository = new PosSqlRepository();
        }

        // Permission gate: this whole controller is only for users with
        // "إدارة نماذج الطباعة" (CanManagePrintTemplates) - i.e. admins
        // / super-users or anyone explicitly granted access via the
        // FrmPosPrintTemplate row in dbo.ScreenJuncUser. Returns null
        // when the user is allowed to continue, or an ActionResult to
        // short-circuit the action otherwise.
        private ActionResult RequireTemplateAdmin()
        {
            var context = Session[PosLoginController.PosContextSessionKey] as PosUserContext;
            if (context == null)
            {
                if (Request.IsAjaxRequest() ||
                    string.Equals(Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
                {
                    Response.StatusCode = 401;
                    return Json(new { success = false, message = "يجب تسجيل دخول نقطة البيع أولا" });
                }
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!context.CanManagePrintTemplates)
            {
                if (Request.IsAjaxRequest() ||
                    string.Equals(Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
                {
                    Response.StatusCode = 403;
                    return Json(new { success = false, message = "لا تمتلك صلاحية إدارة نماذج الطباعة" });
                }
                return new HttpStatusCodeResult(403, "لا تمتلك صلاحية إدارة نماذج الطباعة");
            }

            return null;
        }

        // GET /Pos/PrintTemplate?name=KycCard
        public ActionResult Index(string name = "KycCard")
        {
            var gate = RequireTemplateAdmin();
            if (gate != null) { return gate; }

            ViewBag.TemplateName = PrintTemplateService.SafeName(name);
            ViewBag.Templates = _service.ListNames().ToList();
            return View();
        }

        [HttpGet]
        public ActionResult GetTemplate(string name)
        {
            var gate = RequireTemplateAdmin();
            if (gate != null) { return gate; }

            var template = _service.Load(name) ?? KycCardReport.BuildDefaultTemplate();
            return Json(template, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SaveTemplate()
        {
            var gate = RequireTemplateAdmin();
            if (gate != null) { return gate; }

            try
            {
                var name = Request.QueryString["name"];
                Request.InputStream.Position = 0;
                string json;
                using (var reader = new StreamReader(Request.InputStream))
                {
                    json = reader.ReadToEnd();
                }

                if (string.IsNullOrWhiteSpace(json))
                {
                    Response.StatusCode = 400;
                    return Json(new { success = false, message = "Empty template payload" });
                }

                var template = Newtonsoft.Json.JsonConvert.DeserializeObject<PrintTemplate>(json);
                if (template == null)
                {
                    Response.StatusCode = 400;
                    return Json(new { success = false, message = "Invalid template payload" });
                }

                _service.Save(name, template);
                return Json(new { success = true, message = "تم حفظ القالب" });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult UploadBackground(string name)
        {
            var gate = RequireTemplateAdmin();
            if (gate != null) { return gate; }

            try
            {
                if (Request.Files == null || Request.Files.Count == 0)
                {
                    Response.StatusCode = 400;
                    return Json(new { success = false, message = "لم يتم اختيار ملف" });
                }

                var file = Request.Files[0];
                if (file == null || file.ContentLength <= 0)
                {
                    Response.StatusCode = 400;
                    return Json(new { success = false, message = "الملف فارغ" });
                }

                byte[] bytes;
                using (var ms = new MemoryStream())
                {
                    file.InputStream.CopyTo(ms);
                    bytes = ms.ToArray();
                }

                var fileName = _service.SaveBackground(name, bytes, file.FileName);
                return Json(new { success = true, fileName = fileName });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET /Pos/PrintTemplate/Background?fileName=KycCard.png
        [HttpGet]
        public ActionResult Background(string fileName)
        {
            var gate = RequireTemplateAdmin();
            if (gate != null) { return gate; }

            var bytes = _service.LoadBackground(fileName);
            if (bytes == null)
            {
                return HttpNotFound("Background not found");
            }

            return File(bytes, _service.GetBackgroundContentType(fileName));
        }

        // GET /Pos/PrintTemplate/Preview?name=KycCard&customerId=123
        [HttpGet]
        public ActionResult Preview(string name, int? customerId)
        {
            var gate = RequireTemplateAdmin();
            if (gate != null) { return gate; }

            var template = _service.Load(name);
            PosCustomerLookupDto sample = null;

            if (customerId.HasValue && customerId.Value > 0)
            {
                sample = _repository.GetKeshniCardCustomerById(customerId.Value, null, true);
            }

            sample = sample ?? BuildSampleCustomer();

            using (var report = new KycCardReport(sample, DateTime.Now, template))
            using (var stream = new MemoryStream())
            {
                report.ExportToPdf(stream);
                Response.AddHeader("Content-Disposition", "inline; filename=template-preview.pdf");
                return File(stream.ToArray(), "application/pdf");
            }
        }

        private static PosCustomerLookupDto BuildSampleCustomer()
        {
            return new PosCustomerLookupDto
            {
                CustomerID = 0,
                CustomerName = "أحمد محمد علي عبدالله",
                Name = "أحمد محمد علي عبدالله",
                ArabicName0 = "أحمد",
                ArabicName1 = "محمد",
                ArabicName2 = "علي",
                ArabicName3 = "عبدالله",
                EnglishName0 = "Ahmed",
                EnglishName1 = "Mohamed",
                EnglishName2 = "Aly",
                EnglishName3 = "Abdallah",
                CardNo = "T373f4700000d3ABC",
                CardId = "T373f4700000d3ABC",
                Tet_NumPoket = "29012345678901",
                Phone = "01112345678",
                Phone2 = "01112345678",
                Tel = "01112345678",
                BirthDate = new DateTime(1990, 1, 1),
                CardDate = new DateTime(2024, 1, 1),
                CardEndDate = new DateTime(2031, 1, 1),
                Address = "10 شارع التحرير القاهرة",
                MailAdress = "10 شارع التحرير القاهرة",
                Nationality = 1,
                BranchId = 1,
                BranchName = "الفرع الرئيسي",
                CardSource = "الفرع الرئيسي"
            };
        }
    }
}
