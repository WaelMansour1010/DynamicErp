using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Services;
using MyERP.Common.EmployeePayroll;

namespace MyERP.Areas.Pos.Controllers
{
    public class EmployeePayrollController : Controller
    {
        private readonly EmployeePayrollRepository _repository;
        private readonly PosSqlRepository _posRepository;
        private readonly PosLegacyScreenPermissionService _permissionService;
        private readonly string _employeePayrollConnectionString;
        private readonly bool _databaseOverrideActive;

        public EmployeePayrollController()
        {
            _employeePayrollConnectionString = ResolveConnectionString("KishnyCashConnection", out _databaseOverrideActive);
            _repository = new EmployeePayrollRepository(_employeePayrollConnectionString);
            _posRepository = new PosSqlRepository();
            _permissionService = new PosLegacyScreenPermissionService();
        }

        public ActionResult Employees()
        {
            var context = GetContext();
            if (context == null) return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            if (!CanOpen(context)) return new HttpStatusCodeResult(403, "ليست لديك صلاحية فتح شاشة الموظفين");
            ViewBag.ActiveScreen = "employee-payroll";
            FillOperationalContext(context);
            ViewBag.SharedHrReadOnly = !CanSaveMedicalInsurance();
            return View();
        }

        public ActionResult SalaryRun()
        {
            var context = GetContext();
            if (context == null) return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            if (!CanOpen(context)) return new HttpStatusCodeResult(403, "ليست لديك صلاحية فتح مسير الرواتب");
            ViewBag.ActiveScreen = "salary-run";
            FillOperationalContext(context);
            ViewBag.SharedHrReadOnly = false;
            ViewBag.SharedHrSalaryWriteEnabled = CanSaveSalaryRun();
            return View();
        }

        public ActionResult MedicalInsurance()
        {
            var context = GetContext();
            if (context == null) return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            if (!CanOpen(context)) return new HttpStatusCodeResult(403, "ليست لديك صلاحية فتح التأمين الطبي");
            ViewBag.ActiveScreen = "medical-insurance";
            FillOperationalContext(context);
            ViewBag.SharedHrReadOnly = !CanSaveMedicalInsurance();
            return View();
        }

        public ActionResult MedicalInsuranceReports()
        {
            var context = GetContext();
            if (context == null) return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            if (!CanOpen(context)) return new HttpStatusCodeResult(403, "ليست لديك صلاحية فتح تقارير التأمين الطبي");
            ViewBag.ActiveScreen = "medical-insurance-reports";
            FillOperationalContext(context);
            return View();
        }

        [HttpGet]
        public JsonResult Lookups()
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ليست لديك صلاحية فتح شاشات الموظفين والرواتب" }, JsonRequestBehavior.AllowGet);
            return Json(new { success = true, data = _repository.GetLookups() }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult Search(EmployeeSearchFilter filter)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ليست لديك صلاحية فتح شاشات الموظفين والرواتب" }, JsonRequestBehavior.AllowGet);
            var rows = _repository.SearchEmployees(filter);
            foreach (var row in rows)
            {
                HideInternalEmployeeAccounts(row);
            }

            return Json(new { success = true, rows = rows }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult Get(int id)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ليست لديك صلاحية فتح شاشات الموظفين والرواتب" }, JsonRequestBehavior.AllowGet);
            var employee = _repository.GetEmployee(id);
            HideInternalEmployeeAccounts(employee);
            return Json(new { success = true, employee = employee }, JsonRequestBehavior.AllowGet);
        }

        private static void HideInternalEmployeeAccounts(EmployeeSummary employee)
        {
            if (employee == null) return;
            employee.AccountCode = null;
            employee.AccruedSalaryAccountCode = null;
            employee.VacationProvisionAccountCode = null;
            employee.AdvancePaymentAccountCode = null;
            employee.EndOfServiceAccountCode = null;
            employee.TicketProvisionAccountCode = null;
        }

        [HttpGet]
        public JsonResult Providers(bool activeOnly = false)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ليست لديك صلاحية" }, JsonRequestBehavior.AllowGet);
            return Json(new { success = true, rows = _repository.GetMedicalInsuranceProviders(activeOnly) }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveProvider(MedicalInsuranceProvider provider)
        {
            if (!CanSaveMedicalInsurance()) return Json(new { success = false, message = "ليست لديك صلاحية حفظ شركات التأمين الطبي" });
            try
            {
                var context = GetContext();
                var id = _repository.SaveMedicalInsuranceProvider(provider, context == null ? 0 : context.UserId);
                return Json(new { success = true, id = id, message = "تم حفظ شركة التأمين الطبي بنجاح" });
            }
            catch (Exception ex)
            {
                Response.TrySkipIisCustomErrors = true;
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpGet]
        public JsonResult Plans(bool activeOnly = false)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ليست لديك صلاحية" }, JsonRequestBehavior.AllowGet);
            return Json(new { success = true, rows = _repository.GetMedicalInsurancePlans(activeOnly) }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult PlanDefaults(int id)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ليست لديك صلاحية" }, JsonRequestBehavior.AllowGet);
            return Json(new { success = true, plan = _repository.GetMedicalInsurancePlan(id) }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SavePlan(MedicalInsurancePlan plan)
        {
            if (!CanSaveMedicalInsurance()) return Json(new { success = false, message = "ليست لديك صلاحية حفظ خطط التأمين الطبي" });
            try
            {
                var context = GetContext();
                var id = _repository.SaveMedicalInsurancePlan(plan, context == null ? 0 : context.UserId);
                return Json(new { success = true, id = id, message = "تم حفظ خطة التأمين الطبي بنجاح" });
            }
            catch (Exception ex)
            {
                Response.TrySkipIisCustomErrors = true;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult Save(EmployeeSaveRequest request)
        {
            if (!CanSaveMedicalInsurance()) return Json(new { success = false, message = "ليست لديك صلاحية حفظ بيانات الموظف أو التأمين الطبي" });
            try
            {
                var context = GetContext();
                var id = _repository.SaveEmployee(request, context == null ? 0 : context.UserId);
                return Json(new { success = true, id = id, message = "تم حفظ بيانات الموظف والتأمين الطبي بنجاح" });
            }
            catch (Exception ex)
            {
                Response.TrySkipIisCustomErrors = true;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SetActive(int id, bool active)
        {
            if (!CanSaveMedicalInsurance()) return Json(new { success = false, message = "ليست لديك صلاحية تعديل حالة الموظف" });
            try
            {
                var context = GetContext();
                _repository.SetEmployeeActive(id, active);
                return Json(new { success = true, message = active ? "تم تفعيل الموظف" : "تم تعطيل الموظف" });
            }
            catch (Exception ex)
            {
                Response.TrySkipIisCustomErrors = true;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult PreviewSalaryRun(SalaryRunRequest request)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ليست لديك صلاحية فتح شاشات الموظفين والرواتب" }, JsonRequestBehavior.AllowGet);
            try
            {
                return LargeJson(new { success = true, preview = _repository.PreviewSalaryRun(request) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult SaveSalaryRun(SalaryRunRequest request)
        {
            if (!CanSaveSalaryRun()) return Json(new { success = false, message = "ليست لديك صلاحية حفظ مسير الرواتب" });
            try
            {
                var context = GetContext();
                var userId = context == null ? 0 : context.UserId;
                return Json(new { success = true, result = _repository.SaveSalaryRun(request, userId) });
            }
            catch (Exception ex)
            {
                Response.TrySkipIisCustomErrors = true;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult PayrollAccountingReplay(PayrollAccountingReplayRequest request)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ليست لديك صلاحية معاينة قيد الرواتب" }, JsonRequestBehavior.AllowGet);
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

        [HttpGet]
        public JsonResult PayrollSalarySheet(SalaryRunRequest request)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ليست لديك صلاحية طباعة مسير الرواتب" }, JsonRequestBehavior.AllowGet);
            try
            {
                return LargeJson(new { success = true, report = _repository.BuildPayrollSalarySheetReport(request) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
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
                Response.TrySkipIisCustomErrors = true;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult PayrollPostingDryRun(PayrollPostingRequest request)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ليست لديك صلاحية معاينة ترحيل الرواتب" });
            try
            {
                return LargeJson(new { success = true, result = _repository.BuildPayrollPostingDryRun(request) }, JsonRequestBehavior.DenyGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult PostPayrollJournal(PayrollPostingRequest request)
        {
            if (!CanSaveSalaryRun()) return Json(new { success = false, message = "ليست لديك صلاحية ترحيل قيد الرواتب" });
            try
            {
                var context = GetContext();
                var userId = context == null ? 0 : context.UserId;
                var userName = context == null ? "POS Web" : context.UserName;
                return LargeJson(new { success = true, result = _repository.PostPayrollJournal(request, userId, userName) }, JsonRequestBehavior.DenyGet);
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
            if (!CanUseJson()) return Json(new { success = false, message = "ليست لديك صلاحية" }, JsonRequestBehavior.AllowGet);
            return Json(new { success = true, rows = _repository.GetMedicalInsuranceSubscriptions(filter) }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult InsuranceDeductionReport(MedicalInsuranceReportFilter filter)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ليست لديك صلاحية" }, JsonRequestBehavior.AllowGet);
            return Json(new { success = true, rows = _repository.GetMedicalInsuranceDeductions(filter) }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult MedicalInsuranceReportBundle(MedicalInsuranceReportFilter filter)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ليست لديك صلاحية عرض تقارير التأمين الطبي" }, JsonRequestBehavior.AllowGet);
            try
            {
                return LargeJson(new { success = true, report = _repository.GetMedicalInsuranceReportBundle(filter) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult MedicalInsuranceOperationalDashboard(MedicalInsuranceOperationalFilter filter)
        {
            var context = GetContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "ليست لديك صلاحية" }, JsonRequestBehavior.AllowGet);
            }

            if (!CanOpen(context))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "ليست لديك صلاحية" }, JsonRequestBehavior.AllowGet);
            }

            return LargeJson(new
            {
                success = true,
                dashboard = _repository.GetMedicalInsuranceOperationalDashboard(filter),
                operationalContext = BuildOperationalContext(context)
            }, JsonRequestBehavior.AllowGet);
        }

        private bool CanOpen(Models.PosUserContext context)
        {
            return context != null && (context.IsFullAccess || context.UserType.GetValueOrDefault(-1) == 0 || _permissionService.CanView(context, "FrmEmployee") || _permissionService.CanView(context, "FrmEmpSalary5"));
        }

        private bool CanSaveSalaryRun()
        {
            var context = GetContext();
            return context != null && (context.IsFullAccess || context.UserType.GetValueOrDefault(-1) == 0 || _permissionService.CanAdd(context, "FrmEmpSalary5") || _permissionService.CanEdit(context, "FrmEmpSalary5"));
        }

        private bool CanSaveMedicalInsurance()
        {
            var context = GetContext();
            return context != null && (context.IsFullAccess
                || context.UserType.GetValueOrDefault(-1) == 0
                || _permissionService.CanAdd(context, "FrmEmployee")
                || _permissionService.CanEdit(context, "FrmEmployee")
                || _permissionService.CanAdd(context, "FrmInsurances")
                || _permissionService.CanEdit(context, "FrmInsurances"));
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

        private JsonResult PosOperationalOnly(string workflow)
        {
            Response.StatusCode = 403;
            return Json(new
            {
                success = false,
                message = workflow + " is managed from MainErp. POS exposes operational read-only access only."
            });
        }

        private Models.PosUserContext GetContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _posRepository);
        }

        private bool CanUseJson()
        {
            var context = GetContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return false;
            }

            if (!CanOpen(context))
            {
                Response.StatusCode = 403;
                return false;
            }

            return true;
        }

        private void FillOperationalContext(Models.PosUserContext context)
        {
            var operationalContext = BuildOperationalContext(context);
            ViewBag.SharedHrArea = "Pos";
            ViewBag.SharedHrReadOnly = true;
            ViewBag.SharedHrDatabaseName = operationalContext.DatabaseName;
            ViewBag.SharedHrDatabaseOverrideActive = operationalContext.DemoOverrideActive;
            ViewBag.SharedHrEnvironmentLabel = operationalContext.EnvironmentLabel;
            ViewBag.SharedHrBranchName = operationalContext.BranchName;
            ViewBag.SharedHrStoreName = operationalContext.StoreName;
            ViewBag.SharedHrUserName = operationalContext.UserName;
            ViewBag.PosOperationalDatabase = operationalContext.DatabaseName;
            ViewBag.PosDatabaseOverrideActive = operationalContext.DemoOverrideActive;
            ViewBag.PosEnvironmentLabel = operationalContext.EnvironmentLabel;
            ViewBag.PosBranchName = operationalContext.BranchName;
            ViewBag.PosStoreName = operationalContext.StoreName;
            ViewBag.PosUserName = operationalContext.UserName;
        }

        private PosOperationalDatabaseContext BuildOperationalContext(Models.PosUserContext context)
        {
            return new PosOperationalDatabaseContext
            {
                DatabaseName = GetDatabaseName(_employeePayrollConnectionString),
                DemoOverrideActive = _databaseOverrideActive,
                EnvironmentLabel = _databaseOverrideActive ? "Demo database" : "POS operational context",
                BranchName = context == null ? string.Empty : context.BranchName,
                StoreName = context == null ? string.Empty : context.StoreName,
                UserName = context == null ? string.Empty : context.UserName
            };
        }

        private static string ResolveConnectionString(string name, out bool databaseOverrideActive)
        {
            databaseOverrideActive = false;
            var setting = ConfigurationManager.ConnectionStrings[name];
            if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
            {
                throw new ConfigurationErrorsException("Missing connection string: " + name);
            }

            var connectionString = setting.ConnectionString;
            var overrideEnabled = IsTruthy(ConfigurationManager.AppSettings["PosEmployeePayrollDemoOverrideEnabled"]);
            var databaseOverride = ConfigurationManager.AppSettings["PosEmployeePayrollDatabaseOverride"];
            if (overrideEnabled && !string.IsNullOrWhiteSpace(databaseOverride))
            {
                var builder = new SqlConnectionStringBuilder(connectionString)
                {
                    InitialCatalog = databaseOverride.Trim()
                };
                connectionString = builder.ConnectionString;
                databaseOverrideActive = true;
            }

            return connectionString;
        }

        private static string GetDatabaseName(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) return string.Empty;
            return new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        }

        private static bool IsTruthy(string value)
        {
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
        }

        private class PosOperationalDatabaseContext
        {
            public string DatabaseName { get; set; }
            public bool DemoOverrideActive { get; set; }
            public string EnvironmentLabel { get; set; }
            public string BranchName { get; set; }
            public string StoreName { get; set; }
            public string UserName { get; set; }
        }
    }
}
