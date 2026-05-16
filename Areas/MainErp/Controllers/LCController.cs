using System;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.LC;
using MyERP.Areas.MainErp.ViewModels.LC;

namespace MyERP.Areas.MainErp.Controllers
{
    public class LCController : MainErpControllerBase
    {
        private readonly LcReadRepository _repository;
        private readonly LcWriteRepository _writeRepository;
        private readonly LegacyScreenPermissionService _permissionService;
        private const string LegacyScreenName = "FrmLC";
        private const string PermissionPostHeader = "MainErp.LC.PostHeader";
        private const string PermissionPostGrids = "MainErp.LC.PostGrids";
        private const string PermissionRebuild = "MainErp.LC.Rebuild";
        private const string PermissionDelete = "MainErp.LC.Delete";
        private const string PermissionReports = "MainErp.LC.Reports";

        public LCController()
            : this(new LcReadRepository(new MainErpDbConnectionFactory()),
                  new LcWriteRepository(new MainErpDbConnectionFactory()),
                  new LegacyScreenPermissionService(new MainErpDbConnectionFactory()))
        {
        }

        public LCController(LcReadRepository repository, LcWriteRepository writeRepository, LegacyScreenPermissionService permissionService)
        {
            _repository = repository;
            _writeRepository = writeRepository;
            _permissionService = permissionService;
        }

        public ActionResult Index(string searchText, int? bankId, int? vendorId, int? branchId, int? selectedId, int page = 1)
        {
            ViewBag.ActiveScreen = "lc";
            SetPermissions();
            if (!CanViewLc())
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية عرض الاعتمادات المستندية.");
            }

            const int pageSize = 20;
            var data = _repository.Search(searchText, bankId, vendorId, branchId, page, pageSize);
            var model = new LCIndexViewModel
            {
                Title = "Letters of Credit",
                ArabicTitle = "الاعتمادات المستندية",
                AnalysisStatus = "Real FrmLC migration: read, create, edit, account creation, and controlled 22001 opening voucher.",
                SearchText = searchText,
                BankId = bankId,
                VendorId = vendorId,
                BranchId = branchId,
                SelectedId = selectedId,
                Page = page,
                PageSize = pageSize,
                TotalCount = data.TotalCount,
                Warning = data.Warning
            };

            foreach (var item in data.Items)
            {
                model.Items.Add(item);
            }

            _repository.LoadSearchLookups(model);

            var detailId = selectedId ?? (model.Items.Count > 0 ? (int?)model.Items[0].TblLCID : null);
            if (detailId.HasValue)
            {
                model.SelectedId = detailId;
                model.SelectedDetails = _repository.GetDetails(detailId.Value);
                if (string.IsNullOrWhiteSpace(model.Warning) && !string.IsNullOrWhiteSpace(model.SelectedDetails.Warning))
                {
                    model.Warning = model.SelectedDetails.Warning;
                }
            }

            return View(model);
        }

        public ActionResult Open(int id)
        {
            return RedirectToAction("Index", new { selectedId = id });
        }

        public ActionResult New()
        {
            ViewBag.ActiveScreen = "lc";
            SetPermissions();
            if (!CanEditLc())
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية إضافة اعتماد مستندي.");
            }

            return View("Edit", _writeRepository.CreateNew(
                MainErpUserContext == null ? null : MainErpUserContext.BranchId,
                MainErpUserContext == null ? null : (int?)MainErpUserContext.UserId));
        }

        public ActionResult Edit(int id)
        {
            ViewBag.ActiveScreen = "lc";
            SetPermissions();
            if (!CanEditLc())
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية تعديل الاعتمادات المستندية.");
            }

            return View(_writeRepository.GetForEdit(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(LCEditViewModel model)
        {
            ViewBag.ActiveScreen = "lc";
            SetPermissions();
            try
            {
                if (!CanEditLc())
                {
                    return new HttpStatusCodeResult(403, "ليست لديك صلاحية حفظ الاعتمادات المستندية.");
                }

                var id = _writeRepository.Save(model, MainErpUserContext == null ? null : (int?)MainErpUserContext.UserId);
                TempData["MainErpSuccess"] = "تم حفظ الاعتماد.";
                return RedirectToAction("Index", new { selectedId = id });
            }
            catch (Exception ex)
            {
                model.Warning = ex.Message;
                _writeRepository.PrepareEditModel(model);
                return View("Edit", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateVoucher(int id)
        {
            ViewBag.ActiveScreen = "lc";
            SetPermissions();
            try
            {
                if (!CanPostHeaderLc())
                {
                    return new HttpStatusCodeResult(403, "ليست لديك صلاحية إنشاء قيود الاعتمادات المستندية.");
                }

                var result = _writeRepository.CreateNormalOpeningVoucher(id, MainErpUserContext == null ? null : (int?)MainErpUserContext.UserId);
                TempData[result.Success ? "MainErpSuccess" : "MainErpWarning"] = result.Message;
            }
            catch (Exception ex)
            {
                TempData["MainErpWarning"] = ex.Message;
            }

            return RedirectToAction("Index", new { selectedId = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateOpenExpenseVoucher(int id)
        {
            ViewBag.ActiveScreen = "lc";
            SetPermissions();
            try
            {
                if (!CanPostHeaderLc())
                {
                    return new HttpStatusCodeResult(403, "ليست لديك صلاحية إنشاء قيود الاعتمادات المستندية.");
                }

                var result = _writeRepository.CreateOpenExpenseVoucher(id, MainErpUserContext == null ? null : (int?)MainErpUserContext.UserId);
                TempData[result.Success ? "MainErpSuccess" : "MainErpWarning"] = result.Message;
            }
            catch (Exception ex)
            {
                TempData["MainErpWarning"] = ex.Message;
            }

            return RedirectToAction("Index", new { selectedId = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CloseLc(int id)
        {
            ViewBag.ActiveScreen = "lc";
            SetPermissions();
            try
            {
                if (!CanPostHeaderLc())
                {
                    return new HttpStatusCodeResult(403, "ليست لديك صلاحية إغلاق الاعتمادات المستندية.");
                }

                var result = _writeRepository.CreateCloseVoucher(id, MainErpUserContext == null ? null : (int?)MainErpUserContext.UserId);
                TempData[result.Success ? "MainErpSuccess" : "MainErpWarning"] = result.Message;
            }
            catch (Exception ex)
            {
                TempData["MainErpWarning"] = ex.Message;
            }

            return RedirectToAction("Index", new { selectedId = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateOpeningBalanceVoucher(int id)
        {
            ViewBag.ActiveScreen = "lc";
            SetPermissions();
            try
            {
                if (!CanPostHeaderLc())
                {
                    return new HttpStatusCodeResult(403, "ليست لديك صلاحية ترحيل الرصيد الافتتاحي للاعتمادات.");
                }

                var result = _writeRepository.CreateOpeningBalanceVoucher(id, MainErpUserContext == null ? null : (int?)MainErpUserContext.UserId);
                TempData[result.Success ? "MainErpSuccess" : "MainErpWarning"] = result.Message;
            }
            catch (Exception ex)
            {
                TempData["MainErpWarning"] = ex.Message;
            }

            return RedirectToAction("Index", new { selectedId = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateGridVouchers(int id)
        {
            ViewBag.ActiveScreen = "lc";
            SetPermissions();
            try
            {
                if (!CanPostGridsLc())
                {
                    return new HttpStatusCodeResult(403, "ليست لديك صلاحية إنشاء قيود جريدات الاعتمادات.");
                }

                var result = _writeRepository.CreateGridVouchers(id, MainErpUserContext == null ? null : (int?)MainErpUserContext.UserId);
                TempData[result.Success ? "MainErpSuccess" : "MainErpWarning"] = result.Message;
            }
            catch (Exception ex)
            {
                TempData["MainErpWarning"] = ex.Message;
            }

            return RedirectToAction("Index", new { selectedId = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RebuildVouchers(int id, string confirmationText)
        {
            ViewBag.ActiveScreen = "lc";
            SetPermissions();
            try
            {
                if (!CanRebuildLc())
                {
                    return new HttpStatusCodeResult(403, "إعادة بناء قيود الاعتمادات متاحة لمسؤول النظام فقط.");
                }

                var result = _writeRepository.RebuildVouchers(id, confirmationText, MainErpUserContext == null ? null : (int?)MainErpUserContext.UserId);
                TempData[result.Success ? "MainErpSuccess" : "MainErpWarning"] = result.Message;
            }
            catch (Exception ex)
            {
                TempData["MainErpWarning"] = ex.Message;
            }

            return RedirectToAction("Index", new { selectedId = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RebuildGridVouchers(int id, string confirmationText)
        {
            ViewBag.ActiveScreen = "lc";
            SetPermissions();
            try
            {
                if (!CanRebuildLc())
                {
                    return new HttpStatusCodeResult(403, "إعادة بناء قيود جريدات الاعتماد متاحة لمسؤول النظام فقط.");
                }

                var result = _writeRepository.RebuildGridVouchers(id, confirmationText, MainErpUserContext == null ? null : (int?)MainErpUserContext.UserId);
                TempData[result.Success ? "MainErpSuccess" : "MainErpWarning"] = result.Message;
            }
            catch (Exception ex)
            {
                TempData["MainErpWarning"] = ex.Message;
            }

            return RedirectToAction("Index", new { selectedId = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, string confirmationText)
        {
            ViewBag.ActiveScreen = "lc";
            SetPermissions();
            try
            {
                if (!CanDeleteLc())
                {
                    return new HttpStatusCodeResult(403, "ليست لديك صلاحية حذف الاعتمادات المستندية.");
                }

                _writeRepository.DeleteLc(id, confirmationText, MainErpUserContext == null ? null : (int?)MainErpUserContext.UserId);
                TempData["MainErpSuccess"] = "تم حذف الاعتماد.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["MainErpWarning"] = ex.Message;
                return RedirectToAction("Index", new { selectedId = id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteGridRow(int id, string sourceTable, int rowId, string confirmationText)
        {
            ViewBag.ActiveScreen = "lc";
            SetPermissions();
            try
            {
                if (!CanDeleteLc())
                {
                    return new HttpStatusCodeResult(403, "ليست لديك صلاحية حذف صفوف جريدات الاعتماد.");
                }

                var result = _writeRepository.DeleteGridRow(id, sourceTable, rowId, confirmationText, MainErpUserContext == null ? null : (int?)MainErpUserContext.UserId);
                TempData[result.Success ? "MainErpSuccess" : "MainErpWarning"] = result.Message;
            }
            catch (Exception ex)
            {
                TempData["MainErpWarning"] = ex.Message;
            }

            return RedirectToAction("Index", new { selectedId = id });
        }

        public ActionResult Details(int id)
        {
            ViewBag.ActiveScreen = "lc";
            SetPermissions();
            if (!CanViewLc())
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية عرض الاعتمادات المستندية.");
            }

            return View(_repository.GetDetails(id));
        }

        public ActionResult Report(int id)
        {
            ViewBag.ActiveScreen = "lc";
            SetPermissions();
            if (!CanReportLc())
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية تقارير الاعتمادات المستندية.");
            }

            var model = _repository.GetDetails(id);
            return View(model);
        }

        private void SetPermissions()
        {
            ViewBag.CanEditLc = CanEditLc();
            ViewBag.CanPostLc = CanPostHeaderLc() || CanPostGridsLc();
            ViewBag.CanPostHeaderLc = CanPostHeaderLc();
            ViewBag.CanPostGridsLc = CanPostGridsLc();
            ViewBag.CanRebuildLc = CanRebuildLc();
            ViewBag.CanDeleteLc = CanDeleteLc();
            ViewBag.CanReportLc = CanReportLc();
        }

        private bool CanViewLc()
        {
            return _permissionService.CanView(MainErpUserContext, LegacyScreenName);
        }

        private bool CanEditLc()
        {
            return _permissionService.CanEdit(MainErpUserContext, LegacyScreenName)
                || _permissionService.CanAdd(MainErpUserContext, LegacyScreenName);
        }

        private bool CanDeleteLc()
        {
            return CanNamedPermission(PermissionDelete, _permissionService.CanDelete(MainErpUserContext, LegacyScreenName));
        }

        private bool CanPostHeaderLc()
        {
            return CanNamedPermission(PermissionPostHeader, CanEditLc());
        }

        private bool CanPostGridsLc()
        {
            return CanNamedPermission(PermissionPostGrids, CanEditLc());
        }

        private bool CanRebuildLc()
        {
            return CanNamedPermission(PermissionRebuild, CanEditLc());
        }

        private bool CanReportLc()
        {
            return CanNamedPermission(PermissionReports, _permissionService.CanPrint(MainErpUserContext, LegacyScreenName));
        }

        private bool CanNamedPermission(string permissionName, bool legacyFallback)
        {
            if (MainErpUserContext == null)
            {
                return false;
            }

            if (MainErpUserContext.IsAdmin || MainErpUserContext.UserType.GetValueOrDefault(-1) == 0)
            {
                return true;
            }

            // Dedicated MainErp permission storage is not finalized yet.
            // Until then, keep the VB6 screen permission fallback to avoid blocking migrated users.
            return legacyFallback;
        }
    }
}
