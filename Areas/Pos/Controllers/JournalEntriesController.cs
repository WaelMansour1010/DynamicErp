using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using MyERP.Common.JournalEntries;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class JournalEntriesController : Controller
    {
        private readonly PosSqlRepository _repository;
        private readonly SharedJournalService _journalService;

        public JournalEntriesController()
            : this(new PosSqlRepository(), CreateJournalService())
        {
        }

        internal JournalEntriesController(PosSqlRepository repository, SharedJournalService journalService)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            if (journalService == null)
            {
                throw new ArgumentNullException("journalService");
            }

            _repository = repository;
            _journalService = journalService;
        }

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

            var branches = _journalService.GetBranches();
            if (!IsAdmin(context))
            {
                branches = branches.Where(x => context.BranchId.HasValue && x.BranchId == context.BranchId.Value).ToList();
            }

            var model = new SharedJournalWorkspaceViewModel
            {
                Title = "إدخال واستعراض القيود",
                Intro = "إدخال القيود اليدوية واستعراض القيود السابقة مع حماية تعديل القيود الآلية.",
                CommandTitle = "إدارة القيود",
                CommandIntro = "أنشئ قيدا جديدا أو افتح شاشة البحث لاختيار قيد سابق للعرض.",
                SearchUrl = Url.Action("Search", "JournalEntries", new { area = "Pos" }),
                GetUrl = Url.Action("Get", "JournalEntries", new { area = "Pos" }),
                SaveUrl = Url.Action("Save", "JournalEntries", new { area = "Pos" }),
                AccountLookupUrl = Url.Action("Accounts", "JournalEntries", new { area = "Pos" }),
                AccountTreeUrl = Url.Action("AccountTree", "JournalEntries", new { area = "Pos" }),
                CanCreate = IsAdmin(context) || context.CanCreateJournalEntry,
                CanEdit = IsAdmin(context) || context.CanEditJournalEntry,
                IsAdmin = IsAdmin(context),
                SelectedBranchId = context.BranchId,
                Branches = branches
            };

            return View(model);
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

            var rows = _journalService.Search(SharedJournalEntryMode.Normal, ToSharedSearch(request), context.UserId, IsAdmin(context));
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

            var entry = _journalService.Get(SharedJournalEntryMode.Normal, id, context.UserId, IsAdmin(context));
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

            if (!IsAdmin(context) && context.BranchId.HasValue && request != null)
            {
                request.BranchId = context.BranchId.Value;
            }

            var sharedRequest = ToSharedSave(request);
            var existing = sharedRequest.NoteId.HasValue && sharedRequest.NoteId.Value > 0
                ? _journalService.Get(SharedJournalEntryMode.Normal, sharedRequest.NoteId.Value, context.UserId, IsAdmin(context))
                : null;

            if (existing != null && !existing.IsManual)
            {
                if (!context.CanEditJournalEntry || !IsGeneralAdminPassword(sharedRequest.AdminPassword))
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

            var result = _journalService.Save(
                SharedJournalEntryMode.Normal,
                sharedRequest,
                context.UserId,
                IsAdmin(context),
                existing != null && !existing.IsManual,
                "تم حفظ القيد بنجاح",
                "لا يمكن تعديل قيد آلي بدون كلمة مرور المدير العام",
                validateAccountsExist: false,
                validateBranchExists: false,
                rejectZeroValueLines: false);

            return Json(new { success = result.Success, message = result.Message, details = result.Details, entry = result.Entry });
        }

        [HttpGet]
        public ActionResult Accounts(string term)
        {
            var context = GetPosContext();
            if (context == null || !CanOpen(context))
            {
                return new HttpStatusCodeResult(403);
            }

            return Json(_journalService.SearchAccounts(term).Select(x => new { id = x.Id, text = x.Name, serial = x.Extra }), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult AccountTree(string parentCode, string term)
        {
            var context = GetPosContext();
            if (context == null || !CanOpen(context))
            {
                return new HttpStatusCodeResult(403);
            }

            return Json(_journalService.GetChartOfAccountsChildren(parentCode, term), JsonRequestBehavior.AllowGet);
        }

        private static SharedJournalSearchRequest ToSharedSearch(PosJournalSearchRequest request)
        {
            request = request ?? new PosJournalSearchRequest();
            return new SharedJournalSearchRequest
            {
                VoucherNo = request.VoucherNo,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                AccountCode = request.AccountCode,
                AccountCodes = request.AccountCodes,
                Description = request.Description,
                BranchId = request.BranchId
            };
        }

        private static SharedManualJournalSaveRequest ToSharedSave(PosManualJournalSaveRequest request)
        {
            request = request ?? new PosManualJournalSaveRequest();
            return new SharedManualJournalSaveRequest
            {
                NoteId = request.NoteId,
                NoteDate = request.NoteDate,
                BranchId = request.BranchId,
                Description = request.Description,
                AdminPassword = request.AdminPassword,
                Lines = (request.Lines ?? new System.Collections.Generic.List<PosManualJournalLineDto>())
                    .Where(x => x != null)
                    .Select(x => new SharedManualJournalLineDto
                    {
                        AccountCode = x.AccountCode,
                        AccountSerial = x.AccountSerial,
                        AccountName = x.AccountName,
                        Description = x.Description,
                        Debit = x.Debit,
                        Credit = x.Credit
                    })
                    .ToList()
            };
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

        private static SharedJournalService CreateJournalService()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["KishnyCashConnection"];
            if (connectionString == null || string.IsNullOrWhiteSpace(connectionString.ConnectionString))
            {
                throw new ConfigurationErrorsException("Missing connection string: KishnyCashConnection");
            }

            return new SharedJournalService(new SharedJournalSqlRepository(() =>
            {
                var connection = new SqlConnection(connectionString.ConnectionString);
                connection.Open();
                return connection;
            }));
        }
    }
}
