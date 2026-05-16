using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly ExcelImportReader _reader = new ExcelImportReader();

        public ActionResult Index(string entityType)
        {
            if (!CanUseImport())
            {
                return new HttpStatusCodeResult(403, "Only MainERP administrators can use master data import.");
            }

            ViewBag.ActiveScreen = "master-data-import";
            ViewBag.CurrentDatabase = MainErpDebugDatabaseOverride.GetDisplayDatabaseName();
            ViewBag.Title = "Master Data Import / استيراد الملفات الأساسية";
            var preview = Session[PreviewSessionKey] as MasterDataImportPreview;
            var model = BuildIndexModel(preview);
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
                        FileName = string.Join(", ", tempFiles.Keys),
                        ImportMode = importMode,
                        JournalRows = journalRows,
                        FileHashes = fileHashes,
                        AutoBalanceOpening = autoBalanceOpening
                    };

                    journalPreview.JournalRows = CreateJournalEntryService().Validate(journalPreview);
                    Session[PreviewSessionKey] = journalPreview;
                    var journalModel = BuildIndexModel(journalPreview);
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
                    Rows = rows
                };

                Session[PreviewSessionKey] = masterPreview;
                var masterModel = BuildIndexModel(masterPreview);
                masterModel.StopOnAnyError = stopOnAnyError;
                masterModel.ImportMode = importMode;
                ViewBag.ActiveScreen = "master-data-import";
                ViewBag.CurrentDatabase = MainErpDebugDatabaseOverride.GetDisplayDatabaseName();
                ViewBag.Title = "Master Data Import / استيراد الملفات الأساسية";
                return View("Index", masterModel);
            }
            catch (Exception ex)
            {
                TempData["ImportError"] = ex.Message;
                return RedirectToAction("Index");
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
            if (!CanUseImport())
            {
                return new HttpStatusCodeResult(403, "Only MainERP administrators can use master data import.");
            }

            var preview = Session[PreviewSessionKey] as MasterDataImportPreview;
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

        public ActionResult DownloadTemplate(string entityType)
        {
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

        private static MasterDataImportIndexViewModel BuildIndexModel(MasterDataImportPreview preview)
        {
            var model = new MasterDataImportIndexViewModel();
            model.ImportBatches = CreateChartOfAccountsService().GetRecentBatches(8);
            if (preview != null)
            {
                model.EntityType = preview.EntityType;
                model.FileName = preview.FileName;
                model.ImportMode = MasterDataImportMode.Normalize(preview.ImportMode);
                model.AutoBalanceOpening = preview.AutoBalanceOpening;
                model.Rows = preview.Rows;
                model.JournalRows = preview.JournalRows;
            }

            return model;
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

        private static bool IsExcelFile(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            return string.Equals(extension, ".xls", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase);
        }
    }
}
