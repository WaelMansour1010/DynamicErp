using System;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Common.EmployeePayroll;

namespace MyERP.Areas.MainErp.Controllers
{
    public class EmployeePayrollController : MainErpControllerBase
    {
        private readonly EmployeePayrollRepository _repository;
        private readonly LegacyScreenPermissionService _permissionService;

        public EmployeePayrollController()
        {
            _repository = new EmployeePayrollRepository(MainErpDbConnectionFactory.ResolveActiveConnectionString());
            _permissionService = new LegacyScreenPermissionService(new MainErpDbConnectionFactory());
        }

        public ActionResult Employees()
        {
            ViewBag.ActiveScreen = "employee-payroll";
            if (!CanOpen()) return new HttpStatusCodeResult(403, "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط© ظپطھط­ ط´ط§ط´ط© ط§ظ„ظ…ظˆط¸ظپظٹظ†");
            FillSharedHrContext(false);
            return View();
        }

        public ActionResult SalaryRun()
        {
            ViewBag.ActiveScreen = "salary-run";
            if (!CanOpen()) return new HttpStatusCodeResult(403, "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط© ظپطھط­ ظ…ط³ظٹط± ط§ظ„ط±ظˆط§طھط¨");
            FillSharedHrContext(false);
            return View();
        }

        public ActionResult MedicalInsurance()
        {
            ViewBag.ActiveScreen = "medical-insurance";
            if (!CanOpen()) return new HttpStatusCodeResult(403, "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط© ظپطھط­ ط§ظ„طھط£ظ…ظٹظ† ط§ظ„ط·ط¨ظٹ");
            FillSharedHrContext(false);
            return View();
        }

        public ActionResult MedicalInsuranceReports()
        {
            ViewBag.ActiveScreen = "medical-insurance-reports";
            if (!CanOpen()) return new HttpStatusCodeResult(403, "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط© ظپطھط­ طھظ‚ط§ط±ظٹط± ط§ظ„طھط£ظ…ظٹظ† ط§ظ„ط·ط¨ظٹ");
            FillSharedHrContext(false);
            return View();
        }

        [HttpGet]
        public JsonResult Lookups()
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط© ظپطھط­ ط´ط§ط´ط§طھ ط§ظ„ظ…ظˆط¸ظپظٹظ† ظˆط§ظ„ط±ظˆط§طھط¨" }, JsonRequestBehavior.AllowGet);
            return Json(new { success = true, data = _repository.GetLookups() }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult Search(EmployeeSearchFilter filter)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط© ظپطھط­ ط´ط§ط´ط§طھ ط§ظ„ظ…ظˆط¸ظپظٹظ† ظˆط§ظ„ط±ظˆط§طھط¨" }, JsonRequestBehavior.AllowGet);
            return Json(new { success = true, rows = _repository.SearchEmployees(filter) }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult Get(int id)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط© ظپطھط­ ط´ط§ط´ط§طھ ط§ظ„ظ…ظˆط¸ظپظٹظ† ظˆط§ظ„ط±ظˆط§طھط¨" }, JsonRequestBehavior.AllowGet);
            return Json(new { success = true, employee = _repository.GetEmployee(id) }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult Providers(bool activeOnly = false)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط©" }, JsonRequestBehavior.AllowGet);
            return Json(new { success = true, rows = _repository.GetMedicalInsuranceProviders(activeOnly) }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveProvider(MedicalInsuranceProvider provider)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط©" });
            try
            {
                var userId = MainErpUserContext != null ? MainErpUserContext.UserId : 0;
                var id = _repository.SaveMedicalInsuranceProvider(provider, userId);
                return Json(new { success = true, providerId = id, message = "طھظ… ط­ظپط¸ ط´ط±ظƒط© ط§ظ„طھط£ظ…ظٹظ†" });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult Plans(bool activeOnly = false)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط©" }, JsonRequestBehavior.AllowGet);
            return Json(new { success = true, rows = _repository.GetMedicalInsurancePlans(activeOnly) }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult PlanDefaults(int id)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط©" }, JsonRequestBehavior.AllowGet);
            return Json(new { success = true, plan = _repository.GetMedicalInsurancePlan(id) }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SavePlan(MedicalInsurancePlan plan)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط©" });
            try
            {
                var userId = MainErpUserContext != null ? MainErpUserContext.UserId : 0;
                var id = _repository.SaveMedicalInsurancePlan(plan, userId);
                return Json(new { success = true, planId = id, message = "طھظ… ط­ظپط¸ ط®ط·ط© ط§ظ„طھط£ظ…ظٹظ†" });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult Save(EmployeeSaveRequest request)
        {
            if (!CanSaveEmployee(request)) return Json(new { success = false, message = "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط© ط­ظپط¸ ط¨ظٹط§ظ†ط§طھ ط§ظ„ظ…ظˆط¸ظپظٹظ†" });
            try
            {
                var userId = MainErpUserContext != null ? MainErpUserContext.UserId : 0;
                var id = _repository.SaveEmployee(request, userId);
                return Json(new { success = true, employeeId = id, message = "طھظ… ط­ظپط¸ ط¨ظٹط§ظ†ط§طھ ط§ظ„ظ…ظˆط¸ظپ" });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SetActive(int id, bool active)
        {
            if (!CanEditEmployee()) return Json(new { success = false, message = "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط© طھط¹ط¯ظٹظ„ ط­ط§ظ„ط© ط§ظ„ظ…ظˆط¸ظپ" });
            _repository.SetEmployeeActive(id, active);
            return Json(new { success = true });
        }

        [HttpGet]
        public JsonResult PreviewSalaryRun(SalaryRunRequest request)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط© ظپطھط­ ط´ط§ط´ط§طھ ط§ظ„ظ…ظˆط¸ظپظٹظ† ظˆط§ظ„ط±ظˆط§طھط¨" }, JsonRequestBehavior.AllowGet);
            try
            {
                request = request ?? new SalaryRunRequest();
                if (request.RowLimit <= 0) request.RowLimit = 150;
                if (request.JournalPreviewLimit <= 0) request.JournalPreviewLimit = 200;
                return LargeJson(new { success = true, preview = CompactSalaryPreview(_repository.PreviewSalaryRun(request)) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult PayrollCompatibilityParity(SalaryRunRequest request)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط© ظپطھط­ طھظ‚ط±ظٹط± طھظˆط§ظپظ‚ ظ…ط³ظٹط± ط§ظ„ط±ظˆط§طھط¨" }, JsonRequestBehavior.AllowGet);
            try
            {
                return LargeJson(new { success = true, report = _repository.BuildCompatibilityParityReport(request) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult PayrollCompatibilityExplain(PayrollCompatibilityExplainRequest request)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "Permission denied for payroll compatibility explainability." }, JsonRequestBehavior.AllowGet);
            try
            {
                return Json(new { success = true, explain = _repository.ExplainCompatibilityComponent(request) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult PayrollAccountingParityTrace(PayrollAccountingParityTraceRequest request)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "Permission denied for payroll accounting parity trace." }, JsonRequestBehavior.AllowGet);
            try
            {
                return LargeJson(new { success = true, trace = _repository.BuildPayrollAccountingParityTrace(request) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult PayrollAccountingReplay(PayrollAccountingReplayRequest request)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "Permission denied for payroll accounting replay." }, JsonRequestBehavior.AllowGet);
            try
            {
                return LargeJson(new { success = true, replay = _repository.BuildPayrollAccountingReplayReport(request) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult PayrollTestPostingDryRun(PayrollTestPostingRequest request)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "Permission denied for protected payroll test posting preview." });
            try
            {
                return LargeJson(new { success = true, result = _repository.BuildPayrollTestPostingDryRun(request) }, JsonRequestBehavior.DenyGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GeneratePayrollTestPosting(PayrollTestPostingRequest request)
        {
            if (!CanSaveSalaryRun()) return Json(new { success = false, message = "Permission denied for protected payroll test posting." });
            try
            {
                var userId = MainErpUserContext != null ? MainErpUserContext.UserId : 0;
                var userName = MainErpUserContext != null ? MainErpUserContext.UserName : "MainErp";
                return LargeJson(new { success = true, result = _repository.GeneratePayrollTestPosting(request, userId, userName) }, JsonRequestBehavior.DenyGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult CleanupPayrollTestPosting(PayrollTestPostingCleanupRequest request)
        {
            if (!CanSaveSalaryRun()) return Json(new { success = false, message = "Permission denied for protected payroll test posting cleanup." });
            try
            {
                var userId = MainErpUserContext != null ? MainErpUserContext.UserId : 0;
                return LargeJson(new { success = true, result = _repository.CleanupPayrollTestPosting(request, userId) }, JsonRequestBehavior.DenyGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SaveSalaryRun(SalaryRunRequest request)
        {
            if (!CanSaveSalaryRun()) return Json(new { success = false, message = "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط© ط­ظپط¸ ظ…ط³ظٹط± ط§ظ„ط±ظˆط§طھط¨" });
            try
            {
                var userId = MainErpUserContext != null ? MainErpUserContext.UserId : 0;
                return Json(new { success = true, result = _repository.SaveSalaryRun(request, userId) });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult PayrollRuns(SalaryRunRequest request)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ليست لديك صلاحية عرض مسيرات الرواتب" }, JsonRequestBehavior.AllowGet);
            try
            {
                return LargeJson(new { success = true, runs = _repository.GetPayrollRuns(request) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult ComparePayrollRuns(PayrollRunCompareRequest request)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ليست لديك صلاحية مقارنة مسيرات الرواتب" });
            try
            {
                return LargeJson(new { success = true, result = _repository.ComparePayrollRuns(request) }, JsonRequestBehavior.DenyGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult InsuranceSubscriptionReport(MedicalInsuranceReportFilter filter)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط©" }, JsonRequestBehavior.AllowGet);
            return Json(new { success = true, rows = _repository.GetMedicalInsuranceSubscriptions(filter) }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult InsuranceDeductionReport(MedicalInsuranceReportFilter filter)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط©" }, JsonRequestBehavior.AllowGet);
            return Json(new { success = true, rows = _repository.GetMedicalInsuranceDeductions(filter) }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult MedicalInsuranceOperationalDashboard(MedicalInsuranceOperationalFilter filter)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ظ„ظٹط³طھ ظ„ط¯ظٹظƒ طµظ„ط§ط­ظٹط©" }, JsonRequestBehavior.AllowGet);
            return LargeJson(new
            {
                success = true,
                dashboard = _repository.GetMedicalInsuranceOperationalDashboard(filter),
                operationalContext = BuildSharedHrOperationalContext()
            }, JsonRequestBehavior.AllowGet);
        }

        private void FillSharedHrContext(bool readOnly)
        {
            ViewBag.SharedHrArea = "MainErp";
            ViewBag.SharedHrReadOnly = readOnly;
            ViewBag.SharedHrDatabaseName = MainErpUserContext == null ? string.Empty : MainErpUserContext.DatabaseName;
            ViewBag.SharedHrDatabaseOverrideActive = false;
            ViewBag.SharedHrEnvironmentLabel = "MainErp enterprise HR context";
            ViewBag.SharedHrBranchName = MainErpUserContext == null ? string.Empty : MainErpUserContext.BranchName;
            ViewBag.SharedHrStoreName = MainErpUserContext == null ? string.Empty : MainErpUserContext.StoreName;
            ViewBag.SharedHrUserName = MainErpUserContext == null ? string.Empty : MainErpUserContext.UserName;
        }

        private object BuildSharedHrOperationalContext()
        {
            return new
            {
                DatabaseName = MainErpUserContext == null ? string.Empty : MainErpUserContext.DatabaseName,
                EnvironmentLabel = "MainErp enterprise HR context",
                BranchName = MainErpUserContext == null ? string.Empty : MainErpUserContext.BranchName,
                StoreName = MainErpUserContext == null ? string.Empty : MainErpUserContext.StoreName,
                UserName = MainErpUserContext == null ? string.Empty : MainErpUserContext.UserName
            };
        }

        private bool CanOpen()
        {
            return MainErpUserContext != null && (MainErpUserContext.IsAdmin || _permissionService.CanView(MainErpUserContext, "FrmEmployee") || _permissionService.CanView(MainErpUserContext, "FrmEmpSalary5"));
        }

        private JsonResult LargeJson(object data, JsonRequestBehavior behavior)
        {
            return new JsonResult
            {
                Data = data,
                JsonRequestBehavior = behavior,
                MaxJsonLength = int.MaxValue
            };
        }

        private bool CanUseJson()
        {
            if (!CanOpen())
            {
                Response.StatusCode = MainErpUserContext == null ? 401 : 403;
                return false;
            }

            return true;
        }

        private bool CanSaveEmployee(EmployeeSaveRequest request)
        {
            if (MainErpUserContext == null)
            {
                Response.StatusCode = 401;
                return false;
            }

            if (MainErpUserContext.IsAdmin)
            {
                return true;
            }

            var isEdit = request != null && request.EmployeeId.GetValueOrDefault() > 0;
            var allowed = isEdit
                ? _permissionService.CanEdit(MainErpUserContext, "FrmEmployee")
                : _permissionService.CanAdd(MainErpUserContext, "FrmEmployee");
            if (!allowed)
            {
                Response.StatusCode = 403;
            }

            return allowed;
        }

        private bool CanEditEmployee()
        {
            if (MainErpUserContext == null)
            {
                Response.StatusCode = 401;
                return false;
            }

            var allowed = MainErpUserContext.IsAdmin || _permissionService.CanEdit(MainErpUserContext, "FrmEmployee");
            if (!allowed)
            {
                Response.StatusCode = 403;
            }

            return allowed;
        }

        private bool CanSaveSalaryRun()
        {
            if (MainErpUserContext == null)
            {
                Response.StatusCode = 401;
                return false;
            }

            var allowed = MainErpUserContext.IsAdmin
                || _permissionService.CanAdd(MainErpUserContext, "FrmEmpSalary5")
                || _permissionService.CanEdit(MainErpUserContext, "FrmEmpSalary5");
            if (!allowed)
            {
                Response.StatusCode = 403;
            }

            return allowed;
        }

        private static object CompactSalaryPreview(SalaryRunPreview preview)
        {
            preview = preview ?? new SalaryRunPreview();
            var rows = preview.Rows ?? Enumerable.Empty<SalaryRunEmployeeRow>();
            var journal = preview.JournalPreview ?? Enumerable.Empty<SalaryRunJournalLine>();
            return new
            {
                preview.Request,
                preview.TotalBasic,
                preview.TotalAdditions,
                preview.TotalDeductions,
                preview.TotalMedicalInsurance,
                preview.TotalMedicalInsuranceCompanyCost,
                preview.TotalAdvance,
                preview.TotalNet,
                preview.TotalRows,
                preview.TotalJournalPreviewRows,
                preview.PayloadIsTruncated,
                preview.HasExistingApprovedRows,
                preview.Message,
                LegacyRows = rows.Count(x => x.IsLegacySnapshot || string.Equals(x.CompatibilityStatus, "LegacySnapshot", StringComparison.OrdinalIgnoreCase)),
                ReconstructedRows = Math.Max(0, (preview.TotalRows > 0 ? preview.TotalRows : rows.Count()) - rows.Count(x => x.IsLegacySnapshot || string.Equals(x.CompatibilityStatus, "LegacySnapshot", StringComparison.OrdinalIgnoreCase))),
                Rows = rows.Select(x => new
                {
                    x.EmployeeId,
                    x.EmployeeCode,
                    x.EmployeeName,
                    x.BranchName,
                    x.DepartmentName,
                    x.BasicSalary,
                    x.SalaryAllowances,
                    x.VariableAdditions,
                    x.AdvanceDeduction,
                    x.ExistingDiscounts,
                    x.MedicalInsuranceDeduction,
                    x.TotalInsuranceLegacy,
                    x.TotalDeductions,
                    x.NetSalary,
                    x.IsLegacySnapshot,
                    x.CompatibilityStatus
                }).ToList(),
                JournalPreview = journal.Select(x => new
                {
                    x.AccountCode,
                    x.Debit,
                    x.Credit,
                    x.Description,
                    x.EmployeeId
                }).ToList()
            };
        }

        private static string ResolveConnectionString(string name)
        {
            var setting = ConfigurationManager.ConnectionStrings[name];
            if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
            {
                throw new ConfigurationErrorsException("Missing connection string: " + name);
            }

            return string.Equals(name, "MainErp_ConnectionString", StringComparison.OrdinalIgnoreCase)
                ? MainErpDebugDatabaseOverride.Apply(setting.ConnectionString)
                : setting.ConnectionString;
        }
    }
}


