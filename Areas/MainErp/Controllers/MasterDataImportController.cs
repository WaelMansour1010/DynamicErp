using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Services.MasterDataImport;
using MyERP.Areas.MainErp.ViewModels.MasterDataImport;

namespace MyERP.Areas.MainErp.Controllers
{
    public class MasterDataImportController : MainErpControllerBase
    {
        private const string PreviewSessionKey = "MainErp.MasterDataImport.Preview";
        private const string LastImportReviewSessionKey = "MainErp.MasterDataImport.LastReview";
        private readonly ExcelImportReader _reader = new ExcelImportReader();

        public ActionResult Index(string entityType)
        {
            ApplySafeMasterImportCulture(entityType);
            if (!CanUseImport())
            {
                return new HttpStatusCodeResult(403, "Only MainERP administrators can use master data import.");
            }

            ViewBag.ActiveScreen = "master-data-import";
            ViewBag.CurrentDatabase = MainErpDebugDatabaseOverride.GetDisplayDatabaseName();
            ViewBag.Title = "Master Data Import / استيراد الملفات الأساسية";
            var preview = Session[PreviewSessionKey] as MasterDataImportPreview;
            ApplySafeMasterImportCulture(preview == null ? entityType : preview.EntityType);
            var model = BuildIndexModel(preview, Session[LastImportReviewSessionKey] as JournalImportReviewSnapshotViewModel);
            if (preview == null && IsSupportedEntity(entityType))
            {
                model.EntityType = entityType;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Preview(string entityType, HttpPostedFileBase excelFile, bool stopOnAnyError = true, string importMode = MasterDataImportMode.Merge, bool autoBalanceOpening = false)
        {
            ApplySafeMasterImportCulture(entityType);
            if (!CanUseImport())
            {
                return new HttpStatusCodeResult(403, "Only MainERP administrators can use master data import.");
            }

            if (!IsSupportedEntity(entityType))
            {
                TempData["ImportError"] = "نوع الملف المحدد غير متاح ضمن الاستيراد المعتمد حاليا.";
                return RedirectToAction("Index");
            }

            var uploadedFiles = GetUploadedFiles(excelFile);
            if (uploadedFiles.Count == 0)
            {
                TempData["ImportError"] = "Please upload an Excel file.";
                return RedirectToAction("Index");
            }

            if (uploadedFiles.Any(f => !IsExcelFile(f.FileName)))
            {
                TempData["ImportError"] = "Only .xls and .xlsx files are supported.";
                return RedirectToAction("Index");
            }

            var tempFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var file in uploadedFiles)
                {
                    var extension = Path.GetExtension(file.FileName);
                    var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + extension);
                    file.SaveAs(tempPath);
                    tempFiles[Path.GetFileName(file.FileName)] = tempPath;
                }

                importMode = entityType == MasterDataImportEntityType.ChartOfAccounts ? MasterDataImportMode.Normalize(importMode) : MasterDataImportMode.Merge;

                if (entityType == MasterDataImportEntityType.JournalEntries || entityType == MasterDataImportEntityType.OpeningBalances)
                {
                    var fileHashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var journalRows = entityType == MasterDataImportEntityType.OpeningBalances
                        ? _reader.ReadOpeningBalances(tempFiles, fileHashes)
                        : _reader.ReadJournalEntries(tempFiles, fileHashes);

                    var journalPreview = new MasterDataImportPreview
                    {
                        EntityType = entityType,
                        FileName = BuildPreviewFileName(tempFiles.Keys),
                        ImportMode = importMode,
                        JournalRows = journalRows,
                        FileHashes = fileHashes,
                        WorksheetDiagnostics = _reader.LastWorksheetDiagnostics == null
                            ? new List<MasterDataImportWorksheetDiagnosticViewModel>()
                            : _reader.LastWorksheetDiagnostics.ToList(),
                        AutoBalanceOpening = autoBalanceOpening
                    };

                    journalPreview.JournalRows = CreateJournalEntryService().Validate(journalPreview);
                    Session[PreviewSessionKey] = journalPreview;
                    TempData["ImportMessage"] = "تمت المعاينة بنجاح. الملفات: " + tempFiles.Count + "، الصفوف: " + journalPreview.JournalRows.Count + ".";
                    var journalModel = BuildIndexModel(journalPreview, Session[LastImportReviewSessionKey] as JournalImportReviewSnapshotViewModel);
                    journalModel.StopOnAnyError = stopOnAnyError;
                    journalModel.AutoBalanceOpening = autoBalanceOpening;
                    ViewBag.ActiveScreen = "master-data-import";
                    ViewBag.CurrentDatabase = MainErpDebugDatabaseOverride.GetDisplayDatabaseName();
                    ViewBag.Title = "Master Data Import / ط§ط³طھظٹط±ط§ط¯ ط§ظ„ظ…ظ„ظپط§طھ ط§ظ„ط£ط³ط§ط³ظٹط©";
                    return View("Index", journalModel);
                }

                var rows = entityType == MasterDataImportEntityType.ChartOfAccounts
                    ? _reader.ReadChartOfAccounts(tempFiles.First().Value)
                    : _reader.ReadAccountBalanceMasterFile(tempFiles.First().Value, entityType);
                rows = ValidateRows(entityType, rows, importMode);

                var masterPreview = new MasterDataImportPreview
                {
                    EntityType = entityType,
                    FileName = tempFiles.First().Key,
                    ImportMode = importMode,
                    Rows = rows,
                    WorksheetDiagnostics = _reader.LastWorksheetDiagnostics == null
                        ? new List<MasterDataImportWorksheetDiagnosticViewModel>()
                        : _reader.LastWorksheetDiagnostics.ToList()
                };

                Session[PreviewSessionKey] = masterPreview;
                TempData["ImportMessage"] = "تمت المعاينة بنجاح. الملفات: " + tempFiles.Count + "، الصفوف: " + masterPreview.Rows.Count + ".";
                var masterModel = BuildIndexModel(masterPreview, Session[LastImportReviewSessionKey] as JournalImportReviewSnapshotViewModel);
                masterModel.StopOnAnyError = stopOnAnyError;
                masterModel.ImportMode = importMode;
                ViewBag.ActiveScreen = "master-data-import";
                ViewBag.CurrentDatabase = MainErpDebugDatabaseOverride.GetDisplayDatabaseName();
                ViewBag.Title = "Master Data Import / استيراد الملفات الأساسية";
                return View("Index", masterModel);
            }
            catch (Exception ex)
            {
                TempData["ImportError"] = "Preview failed: " + ex.Message;
                return RedirectToAction("Index", new { entityType = entityType });
            }
            finally
            {
                foreach (var tempPath in tempFiles.Values)
                {
                    if (System.IO.File.Exists(tempPath))
                    {
                        System.IO.File.Delete(tempPath);
                    }
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Import(bool stopOnAnyError = true, bool autoBalanceOpening = false)
        {
            ApplySafeMasterImportCulture(null);
            if (!CanUseImport())
            {
                return new HttpStatusCodeResult(403, "Only MainERP administrators can use master data import.");
            }

            var preview = Session[PreviewSessionKey] as MasterDataImportPreview;
            ApplySafeMasterImportCulture(preview == null ? null : preview.EntityType);
            if (preview == null)
            {
                TempData["ImportError"] = "Preview the Excel file before importing.";
                return RedirectToAction("Index");
            }

            try
            {
                var userName = MainErpUserContext == null ? string.Empty : MainErpUserContext.UserName;
                preview.AutoBalanceOpening = preview.EntityType == MasterDataImportEntityType.OpeningBalances && autoBalanceOpening;
                var result = preview.EntityType == MasterDataImportEntityType.JournalEntries || preview.EntityType == MasterDataImportEntityType.OpeningBalances
                    ? CreateJournalEntryService().Import(preview, userName, stopOnAnyError)
                    : preview.EntityType == MasterDataImportEntityType.ChartOfAccounts
                        ? CreateChartOfAccountsService().Import(preview, userName, stopOnAnyError, preview.ImportMode)
                        : CreateAccountLinkedMasterImportService().Import(preview, MainErpUserContext, stopOnAnyError);
                TempData["ImportMessage"] = result.Message + " Batch #" + result.BatchId + ". Success: " + result.SuccessRows + ", Failed: " + result.FailedRows + ".";
                if (result.ReviewSnapshot != null && result.ReviewSnapshot.Items.Count > 0)
                {
                    Session[LastImportReviewSessionKey] = result.ReviewSnapshot;
                }
                if (result.SuccessRows > 0)
                {
                    Session.Remove(PreviewSessionKey);
                }
            }
            catch (Exception ex)
            {
                TempData["ImportError"] = "Import failed and was rolled back. " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RollbackBatch(int batchId)
        {
            ApplySafeMasterImportCulture(null);
            if (!CanUseImport())
            {
                return new HttpStatusCodeResult(403, "Only MainERP administrators can rollback master data imports.");
            }

            try
            {
                var result = CreateChartOfAccountsService().RollbackBatch(batchId, MainErpUserContext == null ? string.Empty : MainErpUserContext.UserName);
                TempData["ImportMessage"] = result.Message;
                Session.Remove(PreviewSessionKey);
            }
            catch (Exception ex)
            {
                TempData["ImportError"] = "Rollback failed. " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReviewBatch(int batchId)
        {
            ApplySafeMasterImportCulture(null);
            if (!CanUseImport())
            {
                return new HttpStatusCodeResult(403, "Only MainERP administrators can review import batches.");
            }

            try
            {
                var review = CreateJournalEntryService().GetReviewSnapshot(batchId);
                if (review == null || review.Items == null || review.Items.Count == 0)
                {
                    TempData["ImportError"] = "لا توجد مراجعة محفوظة لهذه الدفعة. المراجعة الدائمة متاحة للدفعات الجديدة بعد هذا التحديث.";
                    Session.Remove(LastImportReviewSessionKey);
                }
                else
                {
                    Session[LastImportReviewSessionKey] = review;
                    TempData["ImportMessage"] = "تم تحميل مراجعة الدفعة #" + batchId + ".";
                }
            }
            catch (Exception ex)
            {
                TempData["ImportError"] = "تعذر تحميل مراجعة الدفعة. " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SyncOperationalMasters()
        {
            ApplySafeMasterImportCulture(null);
            if (!CanUseImport())
            {
                return new HttpStatusCodeResult(403, "Only MainERP administrators can synchronize operational master records.");
            }

            try
            {
                var result = CreateChartOfAccountsService().SyncOperationalMasterRecordsFromExistingChart(MainErpUserContext == null ? string.Empty : MainErpUserContext.UserName);
                TempData["ImportMessage"] = result.Message + " Batch #" + result.BatchId + ".";
                Session.Remove(PreviewSessionKey);
            }
            catch (Exception ex)
            {
                TempData["ImportError"] = "Operational master sync failed and was rolled back. " + ex.Message;
            }

            return RedirectToAction("Index", new { entityType = MasterDataImportEntityType.ChartOfAccounts });
        }

        public ActionResult DownloadTemplate(string entityType)
        {
            ApplySafeMasterImportCulture(entityType);
            if (!CanUseImport())
            {
                return new HttpStatusCodeResult(403, "Only MainERP administrators can use master data import.");
            }

            if (!IsSupportedEntity(entityType))
            {
                return new HttpStatusCodeResult(404, "قالب هذا النوع غير متاح ضمن الاستيراد المعتمد حاليا.");
            }

            var bytes = entityType == MasterDataImportEntityType.ChartOfAccounts
                ? _reader.BuildTemplate()
                : entityType == MasterDataImportEntityType.JournalEntries
                    ? _reader.BuildJournalTemplate()
                    : entityType == MasterDataImportEntityType.OpeningBalances
                        ? _reader.BuildOpeningBalanceTemplate()
                        : _reader.BuildAccountLinkedTemplate(entityType);
            var fileName = entityType + "_Template.xls";
            return File(bytes, "application/vnd.ms-excel", fileName);
        }

        public ActionResult DownloadErrors()
        {
            ApplySafeMasterImportCulture(null);
            if (!CanUseImport())
            {
                return new HttpStatusCodeResult(403, "Only MainERP administrators can use master data import.");
            }

            var preview = Session[PreviewSessionKey] as MasterDataImportPreview;
            if (preview == null)
            {
                return new HttpStatusCodeResult(404, "No preview rows are available.");
            }

            if (preview.EntityType == MasterDataImportEntityType.JournalEntries || preview.EntityType == MasterDataImportEntityType.OpeningBalances)
            {
                return File(_reader.BuildJournalErrorReport(preview.JournalRows), "application/vnd.ms-excel", preview.EntityType + "_Errors.xls");
            }

            return File(_reader.BuildErrorReport(preview.Rows), "application/vnd.ms-excel", "MasterDataImport_Errors.xls");
        }

        private bool CanUseImport()
        {
            return MainErpUserContext != null && MainErpUserContext.IsAdmin;
        }

        private static MasterDataImportIndexViewModel BuildIndexModel(MasterDataImportPreview preview, JournalImportReviewSnapshotViewModel lastImportReview)
        {
            var model = new MasterDataImportIndexViewModel();
            model.ImportBatches = CreateChartOfAccountsService().GetRecentBatches(8);
            model.LastImportReview = lastImportReview;
            if (preview != null)
            {
                model.EntityType = preview.EntityType;
                model.FileName = preview.FileName;
                model.ImportMode = MasterDataImportMode.Normalize(preview.ImportMode);
                model.AutoBalanceOpening = preview.AutoBalanceOpening;
                model.Rows = preview.Rows;
                model.JournalRows = preview.JournalRows;
                model.WorksheetDiagnostics = preview.WorksheetDiagnostics;
                model.CurrentReviewItems = BuildCurrentReviewItems(preview);
            }

            return model;
        }

        private static IList<JournalImportReviewItemViewModel> BuildCurrentReviewItems(MasterDataImportPreview preview)
        {
            if (preview == null || preview.JournalRows == null || preview.JournalRows.Count == 0)
            {
                return new List<JournalImportReviewItemViewModel>();
            }

            return preview.JournalRows
                .GroupBy(r => new
                {
                    FileName = r.FileName ?? string.Empty,
                    SheetName = r.SheetName ?? string.Empty,
                    GroupKey = r.GroupKey ?? string.Empty
                })
                .Select(group => new JournalImportReviewItemViewModel
                {
                    FileName = group.Key.FileName,
                    SheetName = group.Key.SheetName,
                    GroupKey = group.Key.GroupKey,
                    RowCount = group.Count(),
                    TotalDebit = group.Sum(x => x.Debit),
                    TotalCredit = group.Sum(x => x.Credit),
                    Difference = group.Sum(x => x.Debit) - group.Sum(x => x.Credit),
                    FirstAccountSerial = group.Select(x => x.AccountSerial).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty,
                    LastAccountSerial = group.Select(x => x.AccountSerial).LastOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty,
                    ReviewStatus = group.All(x => x.IsValid) ? "Ready" : "Has errors",
                    ReviewSource = "Preview"
                })
                .OrderBy(x => x.FileName)
                .ThenBy(x => x.SheetName)
                .ThenBy(x => x.GroupKey)
                .ToList();
        }

        private static ChartOfAccountsImportService CreateChartOfAccountsService()
        {
            return new ChartOfAccountsImportService(new MainErpDbConnectionFactory());
        }

        private static AccountLinkedMasterImportService CreateAccountLinkedMasterImportService()
        {
            return new AccountLinkedMasterImportService(new MainErpDbConnectionFactory());
        }

        private static JournalEntryImportService CreateJournalEntryService()
        {
            return new JournalEntryImportService();
        }

        private static bool IsSupportedEntity(string entityType)
        {
            return entityType == MasterDataImportEntityType.ChartOfAccounts
                || entityType == MasterDataImportEntityType.Customers
                || entityType == MasterDataImportEntityType.Suppliers
                || entityType == MasterDataImportEntityType.Employees
                || entityType == MasterDataImportEntityType.JournalEntries
                || entityType == MasterDataImportEntityType.OpeningBalances;
        }

        private static System.Collections.Generic.IList<MasterDataImportRowViewModel> ValidateRows(string entityType, System.Collections.Generic.IList<MasterDataImportRowViewModel> rows, string importMode)
        {
            if (entityType == MasterDataImportEntityType.JournalEntries || entityType == MasterDataImportEntityType.OpeningBalances)
            {
                return rows;
            }

            return entityType == MasterDataImportEntityType.ChartOfAccounts
                ? CreateChartOfAccountsService().Validate(rows, importMode)
                : CreateAccountLinkedMasterImportService().Validate(entityType, rows);
        }

        private IList<HttpPostedFileBase> GetUploadedFiles(HttpPostedFileBase excelFile)
        {
            var files = new List<HttpPostedFileBase>();
            for (var i = 0; i < Request.Files.Count; i++)
            {
                var file = Request.Files[i];
                if (file != null && file.ContentLength > 0)
                {
                    files.Add(file);
                }
            }

            if (files.Count == 0 && excelFile != null && excelFile.ContentLength > 0)
            {
                files.Add(excelFile);
            }

            return files;
        }

        private static string BuildPreviewFileName(IEnumerable<string> fileNames)
        {
            var names = (fileNames ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(Path.GetFileName)
                .ToList();

            if (names.Count == 0)
            {
                return string.Empty;
            }

            if (names.Count == 1)
            {
                return names[0];
            }

            var first = names[0];
            var second = names.Count > 1 ? names[1] : null;
            var remaining = names.Count - (second == null ? 1 : 2);
            var summary = remaining > 0
                ? string.Format("{0}, {1} (+{2} more)", first, second, remaining)
                : string.Format("{0}, {1}", first, second);

            return summary.Length <= 260 ? summary : summary.Substring(0, 260);
        }

        private static bool IsExcelFile(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            return string.Equals(extension, ".xls", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase);
        }

        private static void ApplySafeMasterImportCulture(string entityType)
        {
            var culture = (CultureInfo)new CultureInfo("ar-SA").Clone();
            culture.DateTimeFormat.Calendar = new GregorianCalendar();
            culture.DateTimeFormat.ShortDatePattern = "yyyy-MM-dd";
            culture.DateTimeFormat.LongDatePattern = "yyyy-MM-dd";
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }
    }
}
