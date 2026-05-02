using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    [SkipERPAuthorize]
    public class KycAttachmentController : Controller
    {
        private readonly PosSqlRepository _repository;

        public KycAttachmentController()
        {
            _repository = new PosSqlRepository();
        }

        [HttpGet]
        public JsonResult List(int? customerId)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "يجب تسجيل دخول نقطة البيع أولاً" }, JsonRequestBehavior.AllowGet);
            }

            if (!customerId.HasValue || customerId.Value <= 0)
            {
                return Json(new { success = true, attachments = new PosKycAttachmentDto[0] }, JsonRequestBehavior.AllowGet);
            }

            var customer = _repository.GetKeshniCardCustomerById(customerId.Value, context.BranchId, context.CanChangeDefaults);
            if (customer == null)
            {
                Response.StatusCode = 404;
                return Json(new { success = false, message = "لا توجد بيانات كارت مسموح بها لهذا المستخدم" }, JsonRequestBehavior.AllowGet);
            }

            var subjectNo = PosSqlRepository.BuildKeshniAttachmentSubject(
                customer.BranchName ?? context.BranchName,
                customer.ArabicName0,
                customer.ArabicName1,
                customer.Tet_NumPoket);
            var attachments = _repository.GetKeshniCardAttachments(subjectNo);

            return Json(new { success = true, attachments = attachments }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Open(int? id)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return new HttpStatusCodeResult(401, "يجب تسجيل دخول نقطة البيع أولاً");
            }

            if (!id.HasValue || id.Value <= 0)
            {
                return HttpNotFound("لم يتم تحديد المرفق");
            }

            var attachment = _repository.GetKeshniCardAttachmentById(id.Value, context.BranchId, context.CanChangeDefaults);
            if (attachment == null)
            {
                return HttpNotFound("المرفق غير موجود أو غير مسموح بعرضه");
            }

            string physicalPath;
            if (!TryResolveAttachmentPath(attachment, out physicalPath))
            {
                return HttpNotFound("ملف المرفق غير موجود على السيرفر");
            }

            var downloadName = string.IsNullOrWhiteSpace(attachment.FileName)
                ? "attachment"
                : Path.GetFileName(attachment.FileName);
            var contentType = MimeMapping.GetMimeMapping(downloadName);
            return File(physicalPath, contentType, downloadName);
        }

        private bool TryResolveAttachmentPath(PosKycAttachmentDto attachment, out string physicalPath)
        {
            var root = Server.MapPath("~/Doc");
            var dateFolder = attachment.ImageDate.HasValue
                ? attachment.ImageDate.Value.ToString("yyyyMMdd")
                : DateTime.Today.ToString("yyyyMMdd");
            var legacyFileName = MakeSafeFileName((attachment.SubjectNo ?? string.Empty) + (attachment.FileName ?? string.Empty));
            var plainFileName = MakeSafeFileName(attachment.FileName ?? string.Empty);
            var candidates = new List<string>
            {
                Path.Combine(root, dateFolder, legacyFileName),
                Path.Combine(root, dateFolder, (attachment.SubjectNo ?? string.Empty) + (attachment.FileName ?? string.Empty)),
                Path.Combine(root, legacyFileName),
                Path.Combine(root, plainFileName)
            };

            physicalPath = candidates.FirstOrDefault(System.IO.File.Exists);
            return physicalPath != null;
        }

        private PosUserContext GetPosContext()
        {
            return Session[PosLoginController.PosContextSessionKey] as PosUserContext;
        }

        private static string MakeSafeFileName(string value)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return new string((value ?? "attachment").Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        }
    }
}
