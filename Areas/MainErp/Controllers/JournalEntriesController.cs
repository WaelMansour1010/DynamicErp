using System.Linq;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.JournalEntries;
using MyERP.Areas.MainErp.Services.JournalEntries;
using MyERP.Common.JournalEntries;

namespace MyERP.Areas.MainErp.Controllers
{
    public class JournalEntriesController : MainErpControllerBase
    {
        private const string NormalScreenName = "FrmAccEditJournal";
        private const string OpeningBalanceScreenName = "FrmAccEditJournal1";

        private readonly MainErpJournalEntryService _journalEntries;
        private readonly SharedJournalService _journalService;
        private readonly LegacyScreenPermissionService _permissionService;

        public JournalEntriesController()
            : this(
                  new MainErpJournalEntryService(new JournalEntryReadRepository(new MainErpDbConnectionFactory())),
                  new SharedJournalService(new SharedJournalSqlRepository(() => new MainErpDbConnectionFactory().CreateOpenConnection())),
                  new LegacyScreenPermissionService(new MainErpDbConnectionFactory()))
        {
        }

        public JournalEntriesController(MainErpJournalEntryService journalEntries, SharedJournalService journalService, LegacyScreenPermissionService permissionService)
        {
            _journalEntries = journalEntries;
            _journalService = journalService;
            _permissionService = permissionService;
        }

        public ActionResult Index()
        {
            ViewBag.ActiveScreen = "journal-entries";
            return JournalWorkspace(SharedJournalEntryMode.Normal);
        }

        public ActionResult OpeningBalance()
        {
            ViewBag.ActiveScreen = "journal-opening-balance";
            return JournalWorkspace(SharedJournalEntryMode.OpeningBalance);
        }

        [HttpPost]
        public JsonResult Search(SharedJournalSearchRequest request)
        {
            return SearchByMode(SharedJournalEntryMode.Normal, request);
        }

        [HttpPost]
        public JsonResult SearchOpeningBalance(SharedJournalSearchRequest request)
        {
            return SearchByMode(SharedJournalEntryMode.OpeningBalance, request);
        }

        public JsonResult Get(int id)
        {
            return GetByMode(SharedJournalEntryMode.Normal, id);
        }

        public JsonResult GetOpeningBalance(int id)
        {
            return GetByMode(SharedJournalEntryMode.OpeningBalance, id);
        }

        [HttpPost]
        public JsonResult Save(SharedManualJournalSaveRequest request)
        {
            return SaveByMode(SharedJournalEntryMode.Normal, request);
        }

        [HttpPost]
        public JsonResult SaveOpeningBalance(SharedManualJournalSaveRequest request)
        {
            return SaveByMode(SharedJournalEntryMode.OpeningBalance, request);
        }

        public JsonResult Accounts(string term)
        {
            if (!CanViewAnyJournal())
            {
                return Json(Enumerable.Empty<object>(), JsonRequestBehavior.AllowGet);
            }

            return Json(_journalService.SearchAccounts(term).Select(x => new { id = x.Id, text = x.Name, serial = x.Extra }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult AccountTree(string parentCode, string term)
        {
            if (!CanViewAnyJournal())
            {
                return Json(Enumerable.Empty<SharedAccountTreeDto>(), JsonRequestBehavior.AllowGet);
            }

            return Json(_journalService.GetChartOfAccountsChildren(parentCode, term), JsonRequestBehavior.AllowGet);
        }

        public ActionResult DetailsByNote(int noteId)
        {
            ViewBag.ActiveScreen = "journal-entries";
            return View("Details", _journalEntries.GetDetailsByNote(noteId));
        }

        public ActionResult DetailsByVoucher(int voucherId)
        {
            ViewBag.ActiveScreen = "journal-entries";
            return View("Details", _journalEntries.GetDetailsByVoucher(voucherId));
        }

        private ActionResult JournalWorkspace(SharedJournalEntryMode mode)
        {
            if (!CanView(mode))
            {
                return new HttpStatusCodeResult(MainErpUserContext == null ? 401 : 403, "الصلاحية غير كافية لفتح شاشة القيود.");
            }

            var isOpening = mode == SharedJournalEntryMode.OpeningBalance;
            var model = new SharedJournalWorkspaceViewModel
            {
                Title = isOpening ? "قيد افتتاحي" : "إدخال واستعراض القيود",
                Intro = isOpening
                    ? "إدخال واستعراض القيود الافتتاحية بنفس محرك القيود مع تخزين Notes1 و DOUBLE_ENTREY_VOUCHERS1."
                    : "إدخال القيود اليدوية واستعراض القيود السابقة مع حماية تعديل القيود الآلية.",
                CommandTitle = isOpening ? "إدارة القيد الافتتاحي" : "إدارة القيود",
                CommandIntro = isOpening
                    ? "أنشئ قيدا افتتاحيا أو افتح شاشة البحث لاختيار قيد افتتاحي سابق."
                    : "أنشئ قيدا جديدا أو افتح شاشة البحث لاختيار قيد سابق للعرض.",
                SearchUrl = Url.Action(isOpening ? "SearchOpeningBalance" : "Search", "JournalEntries", new { area = "MainErp" }),
                GetUrl = Url.Action(isOpening ? "GetOpeningBalance" : "Get", "JournalEntries", new { area = "MainErp" }),
                SaveUrl = Url.Action(isOpening ? "SaveOpeningBalance" : "Save", "JournalEntries", new { area = "MainErp" }),
                AccountLookupUrl = Url.Action("Accounts", "JournalEntries", new { area = "MainErp" }),
                AccountTreeUrl = Url.Action("AccountTree", "JournalEntries", new { area = "MainErp" }),
                CanCreate = CanAdd(mode),
                CanEdit = CanEdit(mode),
                IsAdmin = MainErpUserContext != null && MainErpUserContext.IsAdmin,
                Branches = _journalService.GetBranches()
            };

            return View("Index", model);
        }

        private JsonResult SearchByMode(SharedJournalEntryMode mode, SharedJournalSearchRequest request)
        {
            if (!CanView(mode))
            {
                return Json(new { success = false, message = "الصلاحية غير كافية للبحث في القيود." });
            }

            var rows = _journalService.Search(mode, request, CurrentUserId(), CanChangeDefaults());
            return Json(new { success = true, rows });
        }

        private JsonResult GetByMode(SharedJournalEntryMode mode, int id)
        {
            if (!CanView(mode))
            {
                return Json(new { success = false, message = "الصلاحية غير كافية لفتح القيد." }, JsonRequestBehavior.AllowGet);
            }

            var entry = _journalService.Get(mode, id, CurrentUserId(), CanChangeDefaults());
            if (entry == null)
            {
                return Json(new { success = false, message = "لم يتم العثور على القيد." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, entry }, JsonRequestBehavior.AllowGet);
        }

        private JsonResult SaveByMode(SharedJournalEntryMode mode, SharedManualJournalSaveRequest request)
        {
            var isEdit = request != null && request.NoteId.GetValueOrDefault() > 0;
            if (isEdit ? !CanEdit(mode) : !CanAdd(mode))
            {
                return Json(new { success = false, message = "الصلاحية غير كافية لحفظ القيد." });
            }

            var result = _journalService.Save(
                mode,
                request,
                CurrentUserId(),
                CanChangeDefaults(),
                allowAutomaticOverride: false,
                successMessage: mode == SharedJournalEntryMode.OpeningBalance ? "تم حفظ القيد الافتتاحي بنجاح" : "تم حفظ القيد بنجاح",
                automaticEditDeniedMessage: "لا يمكن تعديل قيد آلي بدون صلاحية المدير.",
                validateAccountsExist: true,
                validateBranchExists: true,
                rejectZeroValueLines: true);

            return Json(new { success = result.Success, message = result.Message, details = result.Details, entry = result.Entry });
        }

        private bool CanViewAnyJournal()
        {
            return CanView(SharedJournalEntryMode.Normal) || CanView(SharedJournalEntryMode.OpeningBalance);
        }

        private bool CanView(SharedJournalEntryMode mode)
        {
            return MainErpUserContext != null && (MainErpUserContext.IsAdmin || _permissionService.CanView(MainErpUserContext, ScreenNameFor(mode)));
        }

        private bool CanAdd(SharedJournalEntryMode mode)
        {
            return MainErpUserContext != null && (MainErpUserContext.IsAdmin || _permissionService.CanAdd(MainErpUserContext, ScreenNameFor(mode)));
        }

        private bool CanEdit(SharedJournalEntryMode mode)
        {
            return MainErpUserContext != null && (MainErpUserContext.IsAdmin || _permissionService.CanEdit(MainErpUserContext, ScreenNameFor(mode)));
        }

        private bool CanChangeDefaults()
        {
            return MainErpUserContext != null && MainErpUserContext.IsAdmin;
        }

        private int CurrentUserId()
        {
            return MainErpUserContext == null ? 0 : MainErpUserContext.UserId;
        }

        private static string ScreenNameFor(SharedJournalEntryMode mode)
        {
            return mode == SharedJournalEntryMode.OpeningBalance ? OpeningBalanceScreenName : NormalScreenName;
        }
    }
}
