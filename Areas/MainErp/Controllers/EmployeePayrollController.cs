using System;
using System.Configuration;
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
            _repository = new EmployeePayrollRepository(ResolveConnectionString("MainErp_ConnectionString"));
            _permissionService = new LegacyScreenPermissionService(new MainErpDbConnectionFactory());
        }

        public ActionResult Employees()
        {
            ViewBag.ActiveScreen = "employee-payroll";
            if (!CanOpen()) return new HttpStatusCodeResult(403, "ليست لديك صلاحية فتح شاشة الموظفين");
            return View();
        }

        public ActionResult SalaryRun()
        {
            ViewBag.ActiveScreen = "salary-run";
            if (!CanOpen()) return new HttpStatusCodeResult(403, "ليست لديك صلاحية فتح مسير الرواتب");
            return View();
        }

        public ActionResult MedicalInsurance()
        {
            ViewBag.ActiveScreen = "medical-insurance";
            if (!CanOpen()) return new HttpStatusCodeResult(403, "ليست لديك صلاحية فتح التأمين الطبي");
            return View();
        }

        public ActionResult MedicalInsuranceReports()
        {
            ViewBag.ActiveScreen = "medical-insurance-reports";
            if (!CanOpen()) return new HttpStatusCodeResult(403, "ليست لديك صلاحية فتح تقارير التأمين الطبي");
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
            return Json(new { success = true, rows = _repository.SearchEmployees(filter) }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult Get(int id)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ليست لديك صلاحية فتح شاشات الموظفين والرواتب" }, JsonRequestBehavior.AllowGet);
            return Json(new { success = true, employee = _repository.GetEmployee(id) }, JsonRequestBehavior.AllowGet);
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
            if (!CanUseJson()) return Json(new { success = false, message = "ليست لديك صلاحية" });
            try
            {
                var userId = MainErpUserContext != null ? MainErpUserContext.UserId : 0;
                var id = _repository.SaveMedicalInsuranceProvider(provider, userId);
                return Json(new { success = true, providerId = id, message = "تم حفظ شركة التأمين" });
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
            if (!CanUseJson()) return Json(new { success = false, message = "ليست لديك صلاحية" });
            try
            {
                var userId = MainErpUserContext != null ? MainErpUserContext.UserId : 0;
                var id = _repository.SaveMedicalInsurancePlan(plan, userId);
                return Json(new { success = true, planId = id, message = "تم حفظ خطة التأمين" });
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
            if (!CanUseJson()) return Json(new { success = false, message = "ليست لديك صلاحية فتح شاشات الموظفين والرواتب" });
            try
            {
                var userId = MainErpUserContext != null ? MainErpUserContext.UserId : 0;
                var id = _repository.SaveEmployee(request, userId);
                return Json(new { success = true, employeeId = id, message = "تم حفظ بيانات الموظف" });
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
            if (!CanUseJson()) return Json(new { success = false, message = "ليست لديك صلاحية فتح شاشات الموظفين والرواتب" });
            _repository.SetEmployeeActive(id, active);
            return Json(new { success = true });
        }

        [HttpGet]
        public JsonResult PreviewSalaryRun(SalaryRunRequest request)
        {
            if (!CanUseJson()) return Json(new { success = false, message = "ليست لديك صلاحية فتح شاشات الموظفين والرواتب" }, JsonRequestBehavior.AllowGet);
            try
            {
                return Json(new { success = true, preview = _repository.PreviewSalaryRun(request) }, JsonRequestBehavior.AllowGet);
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
            if (!CanUseJson()) return Json(new { success = false, message = "ليست لديك صلاحية فتح شاشات الموظفين والرواتب" });
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

        private bool CanOpen()
        {
            return MainErpUserContext != null && (MainErpUserContext.IsAdmin || _permissionService.CanView(MainErpUserContext, "FrmEmployee") || _permissionService.CanView(MainErpUserContext, "FrmEmpSalary5"));
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

        private static string ResolveConnectionString(string name)
        {
            var setting = ConfigurationManager.ConnectionStrings[name];
            if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
            {
                throw new ConfigurationErrorsException("Missing connection string: " + name);
            }

            return setting.ConnectionString;
        }
    }
}
