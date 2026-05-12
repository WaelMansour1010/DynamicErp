using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using MyERP.Areas.Pos.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class ExcelImportController : Controller
    {
        private const string PreviewSessionKey = "POS_EXCEL_IMPORT_PREVIEW";
        private const string KycPreviewSessionKey = "POS_KYC_EXCEL_IMPORT_PREVIEW";
        private static readonly ConcurrentDictionary<string, PosExcelImportCommitProgress> CommitJobs = new ConcurrentDictionary<string, PosExcelImportCommitProgress>(StringComparer.OrdinalIgnoreCase);
        private readonly PosSqlRepository _repository;
        private readonly PosExcelImportParser _parser;
        private readonly PosExcelImportPreflightService _preflightService;
        private readonly PosExcelImportCommitService _commitService;
        private readonly PosExcelImportWorkbookMarker _workbookMarker;
        private readonly PosKycExcelParser _kycExcelParser;

        public ExcelImportController()
        {
            _repository = new PosSqlRepository();
            _parser = new PosExcelImportParser();
            _preflightService = new PosExcelImportPreflightService(_repository);
            _commitService = new PosExcelImportCommitService(_repository);
            _workbookMarker = new PosExcelImportWorkbookMarker();
            _kycExcelParser = new PosKycExcelParser();
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
        public ActionResult KycPreview(HttpPostedFileBase kycExcelFile)
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

            if (kycExcelFile == null || kycExcelFile.ContentLength == 0)
            {
                ViewBag.KycErrorMessage = "اختر ملف Excel يحتوي على بيانات KYC.";
                return View("Index", new PosExcelImportIndexViewModel());
            }

            var extension = Path.GetExtension(kycExcelFile.FileName ?? string.Empty);
            if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(extension, ".xls", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.KycErrorMessage = "صيغة الملف غير مدعومة. اختر ملف Excel فقط.";
                return View("Index", new PosExcelImportIndexViewModel());
            }

            try
            {
                var rows = _kycExcelParser.Parse(kycExcelFile.InputStream, _repository.GetBranches(), context);
                Session[KycPreviewSessionKey] = rows;
                ViewBag.KycRows = rows;
                ViewBag.KycFileName = Path.GetFileName(kycExcelFile.FileName ?? string.Empty);
                if (rows.Count == 0)
                {
                    ViewBag.KycErrorMessage = "لم يتم العثور على صفوف KYC صالحة داخل الملف.";
                }

                return View("Index", new PosExcelImportIndexViewModel());
            }
            catch (Exception ex)
            {
                ViewBag.KycErrorMessage = "تعذر قراءة ملف KYC Excel: " + ex.Message;
                return View("Index", new PosExcelImportIndexViewModel());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult KycCommit(string adminPassword)
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

            var rows = Session[KycPreviewSessionKey] as IList<PosKycExcelPreviewRow>;
            ViewBag.KycRows = rows;
            if (rows == null || rows.Count == 0)
            {
                ViewBag.KycErrorMessage = "لا توجد معاينة KYC محفوظة. ارفع ملف KYC أولاً.";
                return View("Index", new PosExcelImportIndexViewModel());
            }

            if (!_repository.ValidatePosUserPassword(context.UserId, adminPassword))
            {
                ViewBag.KycErrorMessage = "كلمة المرور غير صحيحة. لا يمكن حفظ بيانات KYC.";
                return View("Index", new PosExcelImportIndexViewModel());
            }

            var result = CommitKycRows(rows, context);
            ViewBag.KycCommitResult = result;
            return View("Index", new PosExcelImportIndexViewModel());
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

        private PosKycExcelCommitResult CommitKycRows(IList<PosKycExcelPreviewRow> rows, PosUserContext context)
        {
            var result = new PosKycExcelCommitResult { Status = "Completed" };
            var duplicateTokens = rows
                .Where(x => x != null && x.Customer != null && !string.IsNullOrWhiteSpace(x.Customer.CardNo))
                .GroupBy(x => NormalizeToken(x.Customer.CardNo))
                .Where(x => x.Key.Length > 0 && x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

            foreach (var row in rows.Where(x => x != null && x.Customer != null))
            {
                var customer = row.Customer;
                var rowResult = new PosKycExcelCommitRowResult
                {
                    RowNumber = row.RowNumber,
                    Token = customer.CardNo,
                    CustomerName = customer.CustomerName,
                    Status = "Failed"
                };

                try
                {
                    var validation = ValidateKycExcelRow(row, context, duplicateTokens);
                    if (!string.IsNullOrWhiteSpace(validation))
                    {
                        rowResult.Message = validation;
                        result.FailedCount++;
                        result.Rows.Add(rowResult);
                        continue;
                    }

                    var request = BuildKycSaveRequest(row, context);
                    var saved = _repository.SaveKeshniCardCustomer(request);
                    rowResult.Status = "Imported";
                    rowResult.CustomerId = saved.CustomerID;
                    rowResult.Message = "تم حفظ بيانات KYC وتفعيل الكارت";
                    result.ImportedCount++;
                }
                catch (Exception ex)
                {
                    rowResult.Message = FriendlyKycImportMessage(ex);
                    result.FailedCount++;
                }

                result.Rows.Add(rowResult);
            }

            if (result.FailedCount > 0 && result.ImportedCount > 0)
            {
                result.Status = "ImportedWithErrors";
            }
            else if (result.FailedCount > 0)
            {
                result.Status = "Failed";
            }
            else
            {
                result.Status = "Imported";
            }

            return result;
        }

        private static PosCashCustomerSaveRequest BuildKycSaveRequest(PosKycExcelPreviewRow row, PosUserContext context)
        {
            var customer = row.Customer;
            return new PosCashCustomerSaveRequest
            {
                CustomerID = null,
                Name = customer.Name,
                NameE = customer.NameE,
                ArabicName0 = customer.ArabicName0,
                ArabicName1 = customer.ArabicName1,
                ArabicName2 = customer.ArabicName2,
                ArabicName3 = customer.ArabicName3,
                EnglishName0 = customer.EnglishName0,
                EnglishName1 = customer.EnglishName1,
                EnglishName2 = customer.EnglishName2,
                EnglishName3 = customer.EnglishName3,
                EnglishName5 = customer.EnglishName5,
                PhoneNo2 = customer.Phone2,
                PhoneNo = customer.Phone,
                CardNo = customer.CardNo,
                CardId = customer.CardId,
                Tet_NumPoket = customer.Tet_NumPoket,
                Address = customer.Address,
                BirthDate = customer.BirthDate,
                CardDate = customer.CardDate,
                CardEndDate = customer.CardEndDate,
                OrderDate = customer.OrderDate ?? DateTime.Today,
                EasyCashType = 0,
                EmpId = context.EmpId,
                BranchId = context.CanChangeDefaults && customer.BranchId.HasValue ? customer.BranchId : context.BranchId,
                StoreId = context.StoreId,
                UserId = context.UserId
            };
        }

        private static string ValidateKycExcelRow(PosKycExcelPreviewRow row, PosUserContext context, IList<string> duplicateTokens)
        {
            var customer = row.Customer;
            var token = NormalizeToken(customer.CardNo);
            if (string.IsNullOrWhiteSpace(token))
            {
                return "رقم التوكن/الكارت مطلوب.";
            }

            if (duplicateTokens.Contains(token))
            {
                return "رقم التوكن مكرر داخل نفس ملف KYC.";
            }

            if (token.Length != 8 && token.Length != 18)
            {
                return "رقم التوكن/الكارت يجب أن يكون 8 أو 18 رقم.";
            }

            if (string.IsNullOrWhiteSpace(customer.Phone2) || !System.Text.RegularExpressions.Regex.IsMatch(customer.Phone2, @"^(010|011|012|015)[0-9]{8}$"))
            {
                return "رقم المحمول مطلوب ويجب أن يكون 11 رقم ويبدأ بـ 010 أو 011 أو 012 أو 015.";
            }

            if (string.IsNullOrWhiteSpace(customer.CustomerName) && string.IsNullOrWhiteSpace(customer.ArabicName0))
            {
                return "اسم العميل مطلوب.";
            }

            if (string.IsNullOrWhiteSpace(customer.Tet_NumPoket) || !System.Text.RegularExpressions.Regex.IsMatch(customer.Tet_NumPoket, @"^[0-9]{14}$"))
            {
                return "الرقم القومي مطلوب ويجب أن يكون 14 رقم.";
            }

            if (token.Length == 18)
            {
                if (string.IsNullOrWhiteSpace(customer.EnglishName0) || string.IsNullOrWhiteSpace(customer.EnglishName1) || string.IsNullOrWhiteSpace(customer.EnglishName2))
                {
                    return "الاسم الإنجليزي مطلوب للكارت 18 رقم.";
                }

                if (string.IsNullOrWhiteSpace(customer.EnglishName5))
                {
                    return "العنوان الإنجليزي مطلوب للكارت 18 رقم.";
                }
            }

            if (context == null || !context.StoreId.HasValue || !context.BranchId.HasValue || !context.EmpId.HasValue)
            {
                return "defaults غير مكتملة: الفرع/المخزن/المندوب مطلوبين لحفظ KYC.";
            }

            return string.Empty;
        }

        private static string NormalizeToken(string value)
        {
            return System.Text.RegularExpressions.Regex.Replace(value ?? string.Empty, @"\D+", string.Empty);
        }

        private static string FriendlyKycImportMessage(Exception ex)
        {
            var message = ex == null ? string.Empty : ex.Message;
            if (message.IndexOf("هذا الكارت/التوكن تم تفعيله من قبل", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "هذا الكارت/التوكن تم تفعيله من قبل ولا يمكن استخدامه مرة أخرى.";
            }

            if (message.IndexOf("هذا العميل لديه كارت مفعل بالفعل", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "هذا العميل لديه كارت مفعل بالفعل من نفس النوع.";
            }

            if (message.IndexOf("هذا الكارت غير متاح بالمخزون", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "هذا الكارت غير متاح بالمخزون أو تم صرفه/استخدامه من قبل.";
            }

            return string.IsNullOrWhiteSpace(message) ? "تعذر حفظ بيانات KYC." : message;
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
