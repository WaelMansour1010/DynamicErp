using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using MyERP.Areas.Pos.Reports;
using System;
using System.Collections.Generic;
using System.Configuration;
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
            var root = GetKycAttachmentRootPath();
            var webRoot = Server.MapPath("~/Doc");
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
                Path.Combine(root, plainFileName),
                Path.Combine(webRoot, dateFolder, legacyFileName),
                Path.Combine(webRoot, dateFolder, (attachment.SubjectNo ?? string.Empty) + (attachment.FileName ?? string.Empty)),
                Path.Combine(webRoot, legacyFileName),
                Path.Combine(webRoot, plainFileName)
            };

            physicalPath = candidates.FirstOrDefault(System.IO.File.Exists);
            return physicalPath != null;
        }

        // Mirrors VB6 FrmCustCash.cmdPrint2_Click (Cayshny\Frm\New frm\FrmCustCash.frm
        // line 4646) which prints repCashCustomer4.rpt for the saved Keshni Card
        // KYC customer. The customer record is loaded strictly by TblCusCsh.Id —
        // never by token / ScreenID / IPN / ManualNo.
        [HttpGet]
        public ActionResult PrintAcknowledgment(int? id)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return new HttpStatusCodeResult(401, "POS session context is missing.");
            }

            if (!context.CanPrintKycAcknowledgment)
            {
                return new HttpStatusCodeResult(403, "POS user is not allowed to print KYC acknowledgment.");
            }

            if (!id.HasValue || id.Value <= 0)
            {
                return HttpNotFound("لم يتم تحديد بيانات الكارت");
            }

            var customer = _repository.GetKeshniCardCustomerById(id.Value, context.BranchId, context.CanChangeDefaults);
            if (customer == null)
            {
                return HttpNotFound("بيانات الكارت غير موجودة أو غير مسموح بطباعتها");
            }

            using (var report = new KycCardAcknowledgmentReport(customer, DateTime.Now))
            using (var stream = new MemoryStream())
            {
                report.ExportToPdf(stream);
                Response.AddHeader("Content-Disposition", "inline; filename=kyc-acknowledgment-" + id.Value + ".pdf");
                return File(stream.ToArray(), "application/pdf");
            }
        }

        // Mirrors VB6 FrmCustCash.BtnPrint_Click -> print_report2 which loads
        // repCashCustomer.rpt (the per-character pre-printed card body).
        // Customer is loaded strictly by TblCusCsh.Id.
        [HttpGet]
        public ActionResult PrintCard(int? id)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return new HttpStatusCodeResult(401, "POS session context is missing.");
            }

            if (!context.CanPrintKycCard)
            {
                return new HttpStatusCodeResult(403, "POS user is not allowed to print KYC card.");
            }

            if (!id.HasValue || id.Value <= 0)
            {
                return HttpNotFound("لم يتم تحديد بيانات الكارت");
            }

            var customer = _repository.GetKeshniCardCustomerById(id.Value, context.BranchId, context.CanChangeDefaults);
            if (customer == null)
            {
                return HttpNotFound("بيانات الكارت غير موجودة أو غير مسموح بطباعتها");
            }

            using (var report = new KycCardReport(customer, DateTime.Now))
            using (var stream = new MemoryStream())
            {
                report.ExportToPdf(stream);
                Response.AddHeader("Content-Disposition", "inline; filename=kyc-card-" + id.Value + ".pdf");
                return File(stream.ToArray(), "application/pdf");
            }
        }

        private static string GetKycAttachmentRootPath()
        {
            var configuredPath = ConfigurationManager.AppSettings["PosKycAttachmentRootPath"];
            return string.IsNullOrWhiteSpace(configuredPath)
                ? @"C:\Dynamic Byte\Doc"
                : configuredPath.Trim();
        }

        private PosUserContext GetPosContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _repository);
        }

        private static string MakeSafeFileName(string value)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return new string((value ?? "attachment").Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        }
    }
}
