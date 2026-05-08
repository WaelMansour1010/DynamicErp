using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using MyERP.Areas.Pos.Services;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class ExcelImportController : Controller
    {
        private const string PreviewSessionKey = "POS_EXCEL_IMPORT_PREVIEW";
        private static readonly ConcurrentDictionary<string, PosExcelImportCommitProgress> CommitJobs = new ConcurrentDictionary<string, PosExcelImportCommitProgress>(StringComparer.OrdinalIgnoreCase);
        private readonly PosSqlRepository _repository;
        private readonly PosExcelImportParser _parser;
        private readonly PosExcelImportPreflightService _preflightService;
        private readonly PosExcelImportCommitService _commitService;
        private readonly PosExcelImportWorkbookMarker _workbookMarker;

        public ExcelImportController()
        {
            _repository = new PosSqlRepository();
            _parser = new PosExcelImportParser();
            _preflightService = new PosExcelImportPreflightService(_repository);
            _commitService = new PosExcelImportCommitService(_repository);
            _workbookMarker = new PosExcelImportWorkbookMarker();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult StartCommit(string adminPassword)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return Json(new { ok = false, message = "ظٹط¬ط¨ طھط³ط¬ظٹظ„ ط¯ط®ظˆظ„ ظ†ظ‚ط·ط© ط§ظ„ط¨ظٹط¹ ظ‚ط¨ظ„ ط§ظ„طھط±ط­ظٹظ„." });
            }

            if (!CanExecuteExcelImport(context))
            {
                return Json(new { ok = false, message = "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط© ط§ط³طھظٹط±ط§ط¯ ط§ظ„ط¹ظ…ظ„ظٹط§طھ ظ…ظ† Excel." });
            }

            if (!_repository.ValidatePosUserPassword(context.UserId, adminPassword))
            {
                return Json(new { ok = false, message = "ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط± ط؛ظٹط± طµط­ظٹط­ط©. ظ„ط§ ظٹظ…ظƒظ† طھظ†ظپظٹط° ط§ط³طھظٹط±ط§ط¯ Excel." });
            }

            var preview = Session[PreviewSessionKey] as PosExcelImportPreviewResult;
            if (preview == null)
            {
                return Json(new { ok = false, message = "ظ„ط§ طھظˆط¬ط¯ ظ…ط¹ط§ظٹظ†ط© ظ…ط­ظپظˆط¸ط©. ط§ط±ظپط¹ ظ…ظ„ظپ Excel ظˆط§ط¹ظ…ظ„ ظ…ط¹ط§ظٹظ†ط© ظ‚ط¨ظ„ ط§ظ„طھط±ط­ظٹظ„." });
            }

            var jobId = Guid.NewGuid().ToString("N");
            var outputDirectory = GetImportWorkDirectory();
            var initialProgress = new PosExcelImportCommitProgress
            {
                JobId = jobId,
                Status = "Queued",
                TotalCount = preview.Rows.Count,
                CurrentMessage = "طھظ… ظˆط¶ط¹ ط§ظ„طھط±ط­ظٹظ„ ظپظٹ ظ‚ط§ط¦ظ…ط© ط§ظ„طھظ†ظپظٹط°"
            };
            CommitJobs[jobId] = initialProgress;

            Task.Run(() => RunCommitJob(jobId, preview, context, outputDirectory));
            return Json(new { ok = true, jobId = jobId });
        }

        [HttpGet]
        public ActionResult CommitProgress(string jobId)
        {
            PosExcelImportCommitProgress progress;
            if (string.IsNullOrWhiteSpace(jobId) || !CommitJobs.TryGetValue(jobId, out progress))
            {
                return Json(new { ok = false, message = "ظ„ظ… ظٹطھظ… ط§ظ„ط¹ط«ظˆط± ط¹ظ„ظ‰ ط¹ظ…ظ„ظٹط© ط§ظ„طھط±ط­ظٹظ„." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                ok = true,
                jobId = progress.JobId,
                status = progress.Status,
                totalCount = progress.TotalCount,
                processedCount = progress.ProcessedCount,
                importedCount = progress.ImportedCount,
                failedCount = progress.FailedCount,
                skippedCount = progress.SkippedCount,
                percent = progress.Percent,
                currentSheet = progress.CurrentSheet,
                currentRowNumber = progress.CurrentRowNumber,
                currentServiceType = progress.CurrentServiceType,
                currentMessage = progress.CurrentMessage,
                markedWorkbookFileName = progress.MarkedWorkbookFileName,
                canRollback = progress.Result != null && progress.Result.BatchId > 0 && progress.Result.ImportedCount > 0,
                isDone = string.Equals(progress.Status, "Imported", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(progress.Status, "ImportedWithErrors", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(progress.Status, "RolledBack", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(progress.Status, "RollbackPartial", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(progress.Status, "Failed", StringComparison.OrdinalIgnoreCase)
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult StartRollback(string jobId, string adminPassword)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return Json(new { ok = false, message = "ظٹط¬ط¨ طھط³ط¬ظٹظ„ ط¯ط®ظˆظ„ ظ†ظ‚ط·ط© ط§ظ„ط¨ظٹط¹ ظ‚ط¨ظ„ ط§ظ„طھط±ط§ط¬ط¹." });
            }

            if (!CanExecuteExcelImport(context))
            {
                return Json(new { ok = false, message = "ليست لديك صلاحية التراجع عن استيراد Excel." });
            }

            if (!_repository.ValidatePosUserPassword(context.UserId, adminPassword))
            {
                return Json(new { ok = false, message = "كلمة المرور غير صحيحة. لا يمكن تنفيذ التراجع." });
            }

            PosExcelImportCommitProgress progress;
            if (string.IsNullOrWhiteSpace(jobId) || !CommitJobs.TryGetValue(jobId, out progress) || progress.Result == null || progress.Result.BatchId <= 0)
            {
                return Json(new { ok = false, message = "ظ„ظ… ظٹطھظ… ط§ظ„ط¹ط«ظˆط± ط¹ظ„ظ‰ batch طµط§ظ„ط­ ظ„ظ„طھط±ط§ط¬ط¹." });
            }

            var preview = Session[PreviewSessionKey] as PosExcelImportPreviewResult;
            if (preview == null)
            {
                return Json(new { ok = false, message = "ظ„ط§ طھظˆط¬ط¯ ظ…ط¹ط§ظٹظ†ط© ظ…ط­ظپظˆط¸ط© ظ„ظ„طھط±ط§ط¬ط¹ ط¹ظ† ط£ط«ط±ظ‡ط§ ط¹ظ„ظ‰ ظ…ظ„ظپ Excel." });
            }

            var outputDirectory = GetImportWorkDirectory();
            CommitJobs[jobId] = new PosExcelImportCommitProgress
            {
                JobId = jobId,
                Status = "RollbackRunning",
                TotalCount = progress.ImportedCount,
                CurrentMessage = "ط¨ط¯ط£ ط§ظ„طھط±ط§ط¬ط¹ ط¹ظ† ظپظˆط§طھظٹط± Excel",
                Result = progress.Result
            };

            Task.Run(() => RunRollbackJob(jobId, preview, context.UserId, progress.Result.BatchId, outputDirectory));
            return Json(new { ok = true, jobId = jobId });
        }

        [HttpGet]
        public ActionResult Index()
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!CanExecuteExcelImport(context))
            {
                return new HttpStatusCodeResult(403, "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط© ط§ط³طھظٹط±ط§ط¯ ط§ظ„ط¹ظ…ظ„ظٹط§طھ ظ…ظ† Excel");
            }

            ViewBag.PosContext = context;
            ViewBag.ActiveScreen = "excel-import";
            ViewBag.CanImportExcel = CanExecuteExcelImport(context);

            return View(new PosExcelImportIndexViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Preview(HttpPostedFileBase excelFile, string tokenMatchingStrategy)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!CanExecuteExcelImport(context))
            {
                return new HttpStatusCodeResult(403, "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط© ط§ط³طھظٹط±ط§ط¯ ط§ظ„ط¹ظ…ظ„ظٹط§طھ ظ…ظ† Excel");
            }

            ViewBag.PosContext = context;
            ViewBag.ActiveScreen = "excel-import";
            ViewBag.CanImportExcel = CanExecuteExcelImport(context);

            if (excelFile == null || excelFile.ContentLength == 0)
            {
                return View("Index", new PosExcelImportIndexViewModel());
            }

            try
            {
                var storedPath = SaveUploadedWorkbook(excelFile);
                PosExcelImportPreviewResult preview;
                using (var stream = System.IO.File.OpenRead(storedPath))
                {
                    preview = _parser.Parse(stream, excelFile.FileName, new PosExcelImportMappingDraft { ImportUserId = context.UserId });
                }

                preview.StoredWorkbookPath = storedPath;
                preview.TokenMatchingStrategy = string.IsNullOrWhiteSpace(tokenMatchingStrategy) ? "Sequential" : tokenMatchingStrategy.Trim();
                _preflightService.Apply(preview);
                Session[PreviewSessionKey] = preview;
                return View("Preview", new PosExcelImportPreviewViewModel { Preview = preview });
            }
            catch (Exception ex)
            {
                return View("Preview", new PosExcelImportPreviewViewModel
                {
                    ErrorMessage = "طھط¹ط°ط± ظ‚ط±ط§ط،ط© ظ…ظ„ظپ Excel: " + ex.Message
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Commit(string adminPassword)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            ViewBag.PosContext = context;
            ViewBag.ActiveScreen = "excel-import";
            ViewBag.CanImportExcel = CanExecuteExcelImport(context);

            var preview = Session[PreviewSessionKey] as PosExcelImportPreviewResult;
            if (!CanExecuteExcelImport(context))
            {
                return View("Preview", new PosExcelImportPreviewViewModel
                {
                    Preview = preview,
                    ErrorMessage = "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط© ط§ط³طھظٹط±ط§ط¯ ط§ظ„ط¹ظ…ظ„ظٹط§طھ ظ…ظ† Excel."
                });
            }

            if (!_repository.ValidatePosUserPassword(context.UserId, adminPassword))
            {
                return View("Preview", new PosExcelImportPreviewViewModel
                {
                    Preview = preview,
                    ErrorMessage = "ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط± ط؛ظٹط± طµط­ظٹط­ط©. ظ„ط§ ظٹظ…ظƒظ† طھظ†ظپظٹط° ط§ط³طھظٹط±ط§ط¯ Excel."
                });
            }

            if (preview == null)
            {
                return View("Preview", new PosExcelImportPreviewViewModel
                {
                    ErrorMessage = "ظ„ط§ طھظˆط¬ط¯ ظ…ط¹ط§ظٹظ†ط© ظ…ط­ظپظˆط¸ط©. ط§ط±ظپط¹ ظ…ظ„ظپ Excel ظˆط§ط¹ظ…ظ„ ظ…ط¹ط§ظٹظ†ط© ظ‚ط¨ظ„ ط§ظ„طھط±ط­ظٹظ„."
                });
            }

            try
            {
                var commitResult = _commitService.Commit(preview, context);
                commitResult.MarkedWorkbookFileName = _workbookMarker.MarkWorkbook(preview, commitResult, GetImportWorkDirectory());
                return View("Preview", new PosExcelImportPreviewViewModel
                {
                    Preview = preview,
                    CommitResult = commitResult
                });
            }
            catch (Exception ex)
            {
                return View("Preview", new PosExcelImportPreviewViewModel
                {
                    Preview = preview,
                    ErrorMessage = "طھط¹ط°ط± طھط±ط­ظٹظ„ ظپظˆط§طھظٹط± Excel: " + ex.Message
                });
            }
        }

        [HttpGet]
        public ActionResult DownloadMarked(string file)
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!CanExecuteExcelImport(context))
            {
                return new HttpStatusCodeResult(403, "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط© طھط­ظ…ظٹظ„ ظ…ظ„ظپ Excel ط§ظ„ظ†ط§طھط¬");
            }

            var safeName = Path.GetFileName(file ?? string.Empty);
            if (string.IsNullOrWhiteSpace(safeName))
            {
                return HttpNotFound();
            }

            var fullPath = Path.Combine(GetImportWorkDirectory(), safeName);
            if (!System.IO.File.Exists(fullPath))
            {
                return HttpNotFound();
            }

            return File(fullPath, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", safeName);
        }

        private PosUserContext GetPosContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _repository);
        }

        private static bool CanExecuteExcelImport(PosUserContext context)
        {
            return context != null
                && (context.UserType.GetValueOrDefault(-1) == 0 || context.IsFullAccess || context.CanImportExcel);
        }

        private static void RunCommitJob(string jobId, PosExcelImportPreviewResult preview, PosUserContext context, string outputDirectory)
        {
            try
            {
                var repository = new PosSqlRepository();
                var commitService = new PosExcelImportCommitService(repository);
                var marker = new PosExcelImportWorkbookMarker();

                CommitJobs[jobId] = new PosExcelImportCommitProgress
                {
                    JobId = jobId,
                    Status = "Running",
                    TotalCount = preview.Rows.Count,
                    CurrentMessage = "ط¨ط¯ط£ ط§ظ„طھط±ط­ظٹظ„"
                };

                var result = commitService.Commit(preview, context, progress =>
                {
                    progress.JobId = jobId;
                    progress.Status = "Running";
                    CommitJobs[jobId] = progress;
                });

                result.MarkedWorkbookFileName = marker.MarkWorkbook(preview, result, outputDirectory);

                CommitJobs[jobId] = new PosExcelImportCommitProgress
                {
                    JobId = jobId,
                    Status = result.Status,
                    TotalCount = preview.Rows.Count,
                    ProcessedCount = preview.Rows.Count,
                    ImportedCount = result.ImportedCount,
                    FailedCount = result.FailedCount,
                    SkippedCount = result.SkippedCount,
                    CurrentMessage = "ط§ظ†طھظ‡ظ‰ ط§ظ„طھط±ط­ظٹظ„",
                    MarkedWorkbookFileName = result.MarkedWorkbookFileName,
                    Result = result
                };
            }
            catch (Exception ex)
            {
                CommitJobs[jobId] = new PosExcelImportCommitProgress
                {
                    JobId = jobId,
                    Status = "Failed",
                    TotalCount = preview == null || preview.Rows == null ? 0 : preview.Rows.Count,
                    CurrentMessage = ex.Message
                };
            }
        }

        private static void RunRollbackJob(string jobId, PosExcelImportPreviewResult preview, int userId, long batchId, string outputDirectory)
        {
            try
            {
                var repository = new PosSqlRepository();
                var marker = new PosExcelImportWorkbookMarker();
                var rollbackResult = repository.RollbackPosExcelImportBatch(batchId, userId);
                rollbackResult.ClearedWorkbookFileName = marker.ClearImportedMarkers(preview, rollbackResult, outputDirectory);

                CommitJobs[jobId] = new PosExcelImportCommitProgress
                {
                    JobId = jobId,
                    Status = rollbackResult.Status,
                    TotalCount = rollbackResult.Rows.Count,
                    ProcessedCount = rollbackResult.Rows.Count,
                    ImportedCount = 0,
                    FailedCount = rollbackResult.FailedCount,
                    SkippedCount = rollbackResult.RolledBackCount,
                    CurrentMessage = rollbackResult.FailedCount > 0 ? "طھظ… ط§ظ„طھط±ط§ط¬ط¹ ط¬ط²ط¦ظٹط§. ط¨ط¹ط¶ ط§ظ„ظپظˆط§طھظٹط± ظ„ظ… طھط­ط°ظپ." : "طھظ… ط§ظ„طھط±ط§ط¬ط¹ ظˆط­ط°ظپ ظپظˆط§طھظٹط± Excel.",
                    MarkedWorkbookFileName = rollbackResult.ClearedWorkbookFileName
                };
            }
            catch (Exception ex)
            {
                CommitJobs[jobId] = new PosExcelImportCommitProgress
                {
                    JobId = jobId,
                    Status = "Failed",
                    CurrentMessage = ex.Message
                };
            }
        }

        private string SaveUploadedWorkbook(HttpPostedFileBase excelFile)
        {
            var directory = GetImportWorkDirectory();
            Directory.CreateDirectory(directory);

            var extension = Path.GetExtension(excelFile.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".xlsx";
            }

            var safeName = Path.GetFileNameWithoutExtension(excelFile.FileName ?? "ExcelImport");
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                safeName = safeName.Replace(invalid, '_');
            }

            var fileName = DateTime.Now.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture)
                + "_"
                + Guid.NewGuid().ToString("N")
                + "_"
                + safeName
                + extension;

            var fullPath = Path.Combine(directory, fileName);
            excelFile.SaveAs(fullPath);
            return fullPath;
        }

        private string GetImportWorkDirectory()
        {
            return Server.MapPath("~/App_Data/PosExcelImports");
        }
    }
}
