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
                return Json(new { ok = false, message = "يجب تسجيل دخول نقطة البيع قبل الترحيل." });
            }

            if (!CanExecuteExcelImport(context))
            {
                return Json(new { ok = false, message = "ليست لديك صلاحية استيراد العمليات من Excel." });
            }

            if (!_repository.ValidatePosUserPassword(context.UserId, adminPassword))
            {
                return Json(new { ok = false, message = "كلمة المرور غير صحيحة. لا يمكن تنفيذ استيراد Excel." });
            }

            var preview = Session[PreviewSessionKey] as PosExcelImportPreviewResult;
            if (preview == null)
            {
                return Json(new { ok = false, message = "لا توجد معاينة محفوظة. ارفع ملف Excel واعمل معاينة قبل الترحيل." });
            }

            var jobId = Guid.NewGuid().ToString("N");
            var outputDirectory = GetImportWorkDirectory();
            var initialProgress = new PosExcelImportCommitProgress
            {
                JobId = jobId,
                Status = "Queued",
                TotalCount = preview.Rows.Count,
                CurrentMessage = "تم وضع الترحيل في قائمة التنفيذ"
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
                return Json(new { ok = false, message = "لم يتم العثور على عملية الترحيل." }, JsonRequestBehavior.AllowGet);
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
                return Json(new { ok = false, message = "يجب تسجيل دخول نقطة البيع قبل التراجع." });
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
                return Json(new { ok = false, message = "لم يتم العثور على batch صالح للتراجع." });
            }

            var preview = Session[PreviewSessionKey] as PosExcelImportPreviewResult;
            if (preview == null)
            {
                return Json(new { ok = false, message = "لا توجد معاينة محفوظة للتراجع عن أثرها على ملف Excel." });
            }

            var outputDirectory = GetImportWorkDirectory();
            CommitJobs[jobId] = new PosExcelImportCommitProgress
            {
                JobId = jobId,
                Status = "RollbackRunning",
                TotalCount = progress.ImportedCount,
                CurrentMessage = "بدأ التراجع عن فواتير Excel",
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
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية استيراد العمليات من Excel");
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
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية استيراد العمليات من Excel");
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
                    ErrorMessage = "تعذر قراءة ملف Excel: " + ex.Message
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
                    ErrorMessage = "ليست لديك صلاحية استيراد العمليات من Excel."
                });
            }

            if (!_repository.ValidatePosUserPassword(context.UserId, adminPassword))
            {
                return View("Preview", new PosExcelImportPreviewViewModel
                {
                    Preview = preview,
                    ErrorMessage = "كلمة المرور غير صحيحة. لا يمكن تنفيذ استيراد Excel."
                });
            }

            if (preview == null)
            {
                return View("Preview", new PosExcelImportPreviewViewModel
                {
                    ErrorMessage = "لا توجد معاينة محفوظة. ارفع ملف Excel واعمل معاينة قبل الترحيل."
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
                    ErrorMessage = "تعذر ترحيل فواتير Excel: " + ex.Message
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
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية تحميل ملف Excel الناتج");
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
                    CurrentMessage = "بدأ الترحيل"
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
                    CurrentMessage = "انتهى الترحيل",
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
                    CurrentMessage = rollbackResult.FailedCount > 0 ? "تم التراجع جزئيا. بعض الفواتير لم تحذف." : "تم التراجع وحذف فواتير Excel.",
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
