using System;
using System.IO;
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

        public ActionResult Index()
        {
            if (!CanUseImport())
            {
                return new HttpStatusCodeResult(403, "Only MainERP administrators can use master data import.");
            }

            ViewBag.ActiveScreen = "master-data-import";
            ViewBag.Title = "Master Data Import / استيراد الملفات الأساسية";
            var preview = Session[PreviewSessionKey] as MasterDataImportPreview;
            return View(BuildIndexModel(preview));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Preview(string entityType, HttpPostedFileBase excelFile, bool stopOnAnyError = true)
        {
            if (!CanUseImport())
            {
                return new HttpStatusCodeResult(403, "Only MainERP administrators can use master data import.");
            }

            if (!IsSupportedEntity(entityType))
            {
                TempData["ImportError"] = "This entity type is not implemented yet.";
                return RedirectToAction("Index");
            }

            if (excelFile == null || excelFile.ContentLength == 0)
            {
                TempData["ImportError"] = "Please upload an Excel file.";
                return RedirectToAction("Index");
            }

            var extension = Path.GetExtension(excelFile.FileName);
            if (!string.Equals(extension, ".xls", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ImportError"] = "Only .xls and .xlsx files are supported.";
                return RedirectToAction("Index");
            }

            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + extension);
            try
            {
                excelFile.SaveAs(tempPath);
                var rows = entityType == MasterDataImportEntityType.ChartOfAccounts
                    ? _reader.ReadChartOfAccounts(tempPath)
                    : _reader.ReadAccountBalanceMasterFile(tempPath, entityType);
                rows = ValidateRows(entityType, rows);

                var preview = new MasterDataImportPreview
                {
                    EntityType = entityType,
                    FileName = Path.GetFileName(excelFile.FileName),
                    Rows = rows
                };

                Session[PreviewSessionKey] = preview;
                var model = BuildIndexModel(preview);
                model.StopOnAnyError = stopOnAnyError;
                ViewBag.ActiveScreen = "master-data-import";
                ViewBag.Title = "Master Data Import / استيراد الملفات الأساسية";
                return View("Index", model);
            }
            catch (Exception ex)
            {
                TempData["ImportError"] = ex.Message;
                return RedirectToAction("Index");
            }
            finally
            {
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Import(bool stopOnAnyError = true)
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
                var result = preview.EntityType == MasterDataImportEntityType.ChartOfAccounts
                    ? CreateChartOfAccountsService().Import(preview, userName, stopOnAnyError)
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

        public ActionResult DownloadTemplate(string entityType)
        {
            if (!CanUseImport())
            {
                return new HttpStatusCodeResult(403, "Only MainERP administrators can use master data import.");
            }

            if (!IsSupportedEntity(entityType))
            {
                return new HttpStatusCodeResult(404, "Template is not implemented yet for this entity type.");
            }

            var bytes = entityType == MasterDataImportEntityType.ChartOfAccounts
                ? _reader.BuildTemplate()
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

            return File(_reader.BuildErrorReport(preview.Rows), "application/vnd.ms-excel", "MasterDataImport_Errors.xls");
        }

        private bool CanUseImport()
        {
            return MainErpUserContext != null && MainErpUserContext.IsAdmin;
        }

        private static MasterDataImportIndexViewModel BuildIndexModel(MasterDataImportPreview preview)
        {
            var model = new MasterDataImportIndexViewModel();
            if (preview != null)
            {
                model.EntityType = preview.EntityType;
                model.FileName = preview.FileName;
                model.Rows = preview.Rows;
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

        private static bool IsSupportedEntity(string entityType)
        {
            return entityType == MasterDataImportEntityType.ChartOfAccounts
                || entityType == MasterDataImportEntityType.Customers
                || entityType == MasterDataImportEntityType.Suppliers
                || entityType == MasterDataImportEntityType.Employees;
        }

        private static System.Collections.Generic.IList<MasterDataImportRowViewModel> ValidateRows(string entityType, System.Collections.Generic.IList<MasterDataImportRowViewModel> rows)
        {
            return entityType == MasterDataImportEntityType.ChartOfAccounts
                ? CreateChartOfAccountsService().Validate(rows)
                : CreateAccountLinkedMasterImportService().Validate(entityType, rows);
        }
    }
}
