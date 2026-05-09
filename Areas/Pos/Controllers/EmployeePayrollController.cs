using System;
using System.Configuration;
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

        public EmployeePayrollController()
        {
            _repository = new EmployeePayrollRepository(ResolveConnectionString("KishnyCashConnection"));
            _posRepository = new PosSqlRepository();
            _permissionService = new PosLegacyScreenPermissionService();
        }

        public ActionResult Employees()
        {
            var context = GetContext();
            if (context == null) return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            if (!CanOpen(context)) return new HttpStatusCodeResult(403, "ليست لديك صلاحية فتح شاشة الموظفين");
            ViewBag.ActiveScreen = "employee-payroll";
            return View();
        }

        public ActionResult SalaryRun()
        {
            var context = GetContext();
            if (context == null) return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            if (!CanOpen(context)) return new HttpStatusCodeResult(403, "ليست لديك صلاحية فتح مسير الرواتب");
            ViewBag.ActiveScreen = "salary-run";
            return View();
        }

        public ActionResult MedicalInsurance()
        {
            var context = GetContext();
            if (context == null) return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            if (!CanOpen(context)) return new HttpStatusCodeResult(403, "ليست لديك صلاحية فتح التأمين الطبي");
            ViewBag.ActiveScreen = "medical-insurance";
            return View();
        }

        public ActionResult MedicalInsuranceReports()
        {
            var context = GetContext();
            if (context == null) return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            if (!CanOpen(context)) return new HttpStatusCodeResult(403, "ليست لديك صلاحية فتح تقارير التأمين الطبي");
            ViewBag.ActiveScreen = "medical-insurance-reports";
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
            var context = GetContext();
            if (context == null) return Json(new { success = false, message = "يجب تسجيل الدخول" });
            if (!CanOpen(context)) return Json(new { success = false, message = "ليست لديك صلاحية" });
            try
            {
                var id = _repository.SaveMedicalInsuranceProvider(provider, context.UserId);
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
            var context = GetContext();
            if (context == null) return Json(new { success = false, message = "يجب تسجيل الدخول" });
            if (!CanOpen(context)) return Json(new { success = false, message = "ليست لديك صلاحية" });
            try
            {
                var id = _repository.SaveMedicalInsurancePlan(plan, context.UserId);
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
            var context = GetContext();
            if (context == null) return Json(new { success = false, message = "يجب تسجيل الدخول" });
            if (!CanOpen(context)) return Json(new { success = false, message = "ليست لديك صلاحية فتح شاشات الموظفين والرواتب" });
            try
            {
                var id = _repository.SaveEmployee(request, context.UserId);
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
            var context = GetContext();
            if (context == null) return Json(new { success = false, message = "يجب تسجيل الدخول" });
            if (!CanOpen(context)) return Json(new { success = false, message = "ليست لديك صلاحية فتح شاشات الموظفين والرواتب" });
            try
            {
                return Json(new { success = true, result = _repository.SaveSalaryRun(request, context.UserId) });
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

        private bool CanOpen(Models.PosUserContext context)
        {
            return context != null && (context.IsFullAccess || context.UserType.GetValueOrDefault(-1) == 0 || _permissionService.CanView(context, "FrmEmployee") || _permissionService.CanView(context, "FrmEmpSalary5"));
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
