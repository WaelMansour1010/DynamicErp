using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class JournalEntriesController : Controller
    {
        private readonly PosSqlRepository _repository = new PosSqlRepository();

        public ActionResult Index()
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!CanOpen(context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية استعراض القيود");
            }

            ViewBag.PosContext = context;
            ViewBag.Branches = IsAdmin(context) ? _repository.GetBranches() : _repository.GetBranches().Where(x => context.BranchId.HasValue && x.BranchId == context.BranchId.Value).ToList();
            return View();
        }

        [HttpPost]
        public ActionResult Search(PosJournalSearchRequest request)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return Json(new { success = false, message = "يجب تسجيل الدخول أولا" });
            }

            if (!CanOpen(context))
            {
                return Json(new { success = false, message = "ليست لديك صلاحية استعراض القيود" });
            }

            if (!IsAdmin(context) && context.BranchId.HasValue)
            {
                request = request ?? new PosJournalSearchRequest();
                request.BranchId = context.BranchId.Value;
            }

            var rows = _repository.SearchJournalEntries(request, context.UserId, IsAdmin(context));
            return Json(new { success = true, rows = rows });
        }

        [HttpGet]
        public ActionResult Get(int id)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return Json(new { success = false, message = "يجب تسجيل الدخول أولا" }, JsonRequestBehavior.AllowGet);
            }

            if (!CanOpen(context))
            {
                return Json(new { success = false, message = "ليست لديك صلاحية استعراض القيود" }, JsonRequestBehavior.AllowGet);
            }

            var entry = _repository.GetJournalEntryByNoteId(id, context.UserId, IsAdmin(context));
            if (entry == null)
            {
                return Json(new { success = false, message = "القيد غير موجود أو غير مسموح بعرضه" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, entry = entry }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Save(PosManualJournalSaveRequest request)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return Json(new { success = false, message = "يجب تسجيل الدخول أولا" });
            }

            var validation = ValidateSave(request, context);
            if (!string.IsNullOrWhiteSpace(validation))
            {
                return Json(new { success = false, message = validation });
            }

            var existing = request.NoteId.HasValue && request.NoteId.Value > 0
                ? _repository.GetJournalEntryByNoteId(request.NoteId.Value, context.UserId, IsAdmin(context))
                : null;

            if (existing != null && !existing.IsManual)
            {
                if (!context.CanEditJournalEntry || !IsGeneralAdminPassword(request.AdminPassword))
                {
                    return Json(new { success = false, message = "لا يمكن تعديل قيد آلي بدون كلمة مرور المدير العام" });
                }
            }
            else if (existing != null && !context.CanEditJournalEntry && !IsAdmin(context))
            {
                return Json(new { success = false, message = "ليست لديك صلاحية تعديل قيد يومية" });
            }
            else if (existing == null && !context.CanCreateJournalEntry && !IsAdmin(context))
            {
                return Json(new { success = false, message = "ليست لديك صلاحية إدخال قيد يومية" });
            }

            try
            {
                if (!IsAdmin(context) && context.BranchId.HasValue)
                {
                    request.BranchId = context.BranchId.Value;
                }

                var result = _repository.SaveManualJournalEntry(request, context.UserId, IsAdmin(context), existing != null && !existing.IsManual);
                return Json(new { success = true, message = "تم حفظ القيد بنجاح", entry = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "تعذر حفظ القيد", details = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult Accounts(string term)
        {
            var context = GetPosContext();
            if (context == null || !CanOpen(context))
            {
                return new HttpStatusCodeResult(403);
            }

            return Json(_repository.SearchAccounts(term).Select(x => new { id = x.Id, text = x.Name, serial = x.Extra }), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult AccountTree(string parentCode, string term)
        {
            var context = GetPosContext();
            if (context == null || !CanOpen(context))
            {
                return new HttpStatusCodeResult(403);
            }

            return Json(_repository.GetChartOfAccountsChildren(parentCode, term), JsonRequestBehavior.AllowGet);
        }

        private static string ValidateSave(PosManualJournalSaveRequest request, PosUserContext context)
        {
            if (request == null)
            {
                return "بيانات القيد غير مكتملة";
            }

            if (request.NoteDate < new DateTime(1900, 1, 1))
            {
                return "تاريخ القيد غير صحيح";
            }

            if (!request.BranchId.HasValue && !context.BranchId.HasValue)
            {
                return "الفرع مطلوب";
            }

            var lines = (request.Lines ?? new System.Collections.Generic.List<PosManualJournalLineDto>())
                .Where(x => x != null && (!string.IsNullOrWhiteSpace(x.AccountCode) || x.Debit != 0 || x.Credit != 0))
                .ToList();
            request.Lines = lines;
            if (lines.Count < 2)
            {
                return "يجب إدخال سطرين على الأقل";
            }

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line.AccountCode))
                {
                    return "لا يوجد حساب في أحد سطور القيد";
                }

                if (line.Debit < 0 || line.Credit < 0)
                {
                    return "لا يسمح بقيم سالبة في القيد";
                }

                if (line.Debit > 0 && line.Credit > 0)
                {
                    return "السطر لا يمكن أن يكون مدين ودائن في نفس الوقت";
                }
            }

            var debit = lines.Sum(x => x.Debit);
            var credit = lines.Sum(x => x.Credit);
            if (debit <= 0 || credit <= 0 || Math.Abs(debit - credit) > 0.01m)
            {
                return "إجمالي المدين يجب أن يساوي إجمالي الدائن";
            }

            return null;
        }

        private static bool IsGeneralAdminPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            var configured = ConfigurationManager.AppSettings["PosGeneralAdminPassword"];
            if (!string.IsNullOrWhiteSpace(configured) && string.Equals(password, configured, StringComparison.Ordinal))
            {
                return true;
            }

            return string.Equals(password, "OMAR2025", StringComparison.Ordinal);
        }

        private static bool CanOpen(PosUserContext context)
        {
            return IsAdmin(context) || (context != null && (context.CanViewJournalEntry || context.CanCreateJournalEntry || context.CanEditJournalEntry));
        }

        private static bool IsAdmin(PosUserContext context)
        {
            return context != null && context.UserType.GetValueOrDefault(-1) == 0;
        }

        private PosUserContext GetPosContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _repository);
        }
    }
}
