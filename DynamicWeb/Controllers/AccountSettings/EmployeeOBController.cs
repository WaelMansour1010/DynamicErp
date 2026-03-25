using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Security.Claims;


namespace MyERP.Controllers.AccountSettings
{
    public class EmployeeOBController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: EmployeeOB
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمةالأرصدةالإفتتاحية للعملاء",
                EnAction = "Index",
                ControllerName = "EmployeeOB",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("EmployeeOB", "View", "Index", null, null, "الأرصدةالإفتتاحية للعملاء");

            SystemSetting sysObj = db.SystemSettings.FirstOrDefault();
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName");

            }


            return View();
        }
        [SkipERPAuthorize]
        public ActionResult _GetAllEmployees(int id)
        {
            return PartialView(db.GetEmployeeOB(id));
        }

        //save changes in OBDebit &OBcredit
        [HttpPost]
        public ActionResult Save(List<EmployeeOpenningBalance> EmployeeOBList)
        {
            if (ModelState.IsValid)
            {
                var eOb = EmployeeOBList.FirstOrDefault();
                Department department = db.Departments.Find(eOb.DepartmentId);

                ChartOfAccount PaidAllowancesAccount = db.ChartOfAccounts.Find(department.PaidAllowancesAccumulatedAccountId);
                ChartOfAccount EmpVacationAccumulatedAccount = db.ChartOfAccounts.Find(department.EmpVacationAccumulatedAccountId);
                ChartOfAccount TravelingTicketsAccumulatedAccount = db.ChartOfAccounts.Find(department.TravelingTicketsAccumulatedAccountId);
                ChartOfAccount EndOfServiceAccumulatedAccount = db.ChartOfAccounts.Find(department.EndOfServiceAccumulatedAccountId);
                ChartOfAccount EmployeeReceivableValueAccount = db.ChartOfAccounts.Find(department.EmployeeReceivableAccountId);
                ChartOfAccount DueSalariesValueAccount = db.ChartOfAccounts.Find(department.DueSalariesAccountId);

                var PaidAllowancesAccountSum = EmployeeOBList.Sum(e => e.PaidAllowancesAccumulated);
                var EmpVacationAccumulatedSum = EmployeeOBList.Sum(e => e.EmpVacationAccumulated);
                var TravelingTicketsAccumulatedSum = EmployeeOBList.Sum(e => e.TravelingTicketsAccumulated);
                var EndOfServiceAccumulatedSum = EmployeeOBList.Sum(e => e.EndOfServiceAccumulated);
                var EmployeeReceivableValueSum = EmployeeOBList.Sum(e => e.EmployeeReceivableValue);
                var DueSalariesValueSum = EmployeeOBList.Sum(e => e.DueSalariesValue);

                if (PaidAllowancesAccount != null)
                {
                    var PaidAllowancesAccountOnSystem = db.EmployeeOpenningBalances.Where(c => c.DepartmentId == department.Id).Sum(c => c.PaidAllowancesAccumulated);

                    PaidAllowancesAccount.ObCredit = PaidAllowancesAccount.ObCredit - (PaidAllowancesAccountOnSystem != null ? PaidAllowancesAccountOnSystem : 0) + (PaidAllowancesAccountSum != null ? PaidAllowancesAccountSum : 0);

                    db.Entry(PaidAllowancesAccount).State = EntityState.Modified;
                }
                else
                {
                    return Content("nullAccount-مخصص البدلات المدفوعة مقدما");
                }

                if (EmpVacationAccumulatedAccount != null)
                {
                    var EmpVacationAccumulatedAccountOnSystem = db.EmployeeOpenningBalances.Where(c => c.DepartmentId == department.Id).Sum(c => c.EmpVacationAccumulated);

                    EmpVacationAccumulatedAccount.ObCredit = EmpVacationAccumulatedAccount.ObCredit - (EmpVacationAccumulatedAccountOnSystem != null ? EmpVacationAccumulatedAccountOnSystem : 0) + (EmpVacationAccumulatedSum != null ? EmpVacationAccumulatedSum : 0);

                    db.Entry(EmpVacationAccumulatedAccount).State = EntityState.Modified;
                }
                else
                {
                    return Content("nullAccount-مخصص أجازات العاملين");
                }

                if (TravelingTicketsAccumulatedAccount != null)
                {
                    var TravelingTicketsOnSystem = db.EmployeeOpenningBalances.Where(c => c.DepartmentId == department.Id).Sum(c => c.TravelingTicketsAccumulated);

                    TravelingTicketsAccumulatedAccount.ObCredit = TravelingTicketsAccumulatedAccount.ObCredit - (TravelingTicketsOnSystem != null ? TravelingTicketsOnSystem : 0) + (TravelingTicketsAccumulatedSum != null ? TravelingTicketsAccumulatedSum : 0);

                    db.Entry(TravelingTicketsAccumulatedAccount).State = EntityState.Modified;
                }
                else
                {
                    return Content("nullAccount-مخصص تذاكر سفر العاملين");
                }

                if (EndOfServiceAccumulatedAccount != null)
                {
                    var EndOfServiceOnSystem = db.EmployeeOpenningBalances.Where(c => c.DepartmentId == department.Id).Sum(c => c.EndOfServiceAccumulated);

                    EndOfServiceAccumulatedAccount.ObCredit = EndOfServiceAccumulatedAccount.ObCredit - (EndOfServiceOnSystem != null ? EndOfServiceOnSystem : 0) + (EndOfServiceAccumulatedSum != null ? EndOfServiceAccumulatedSum : 0);

                    db.Entry(EndOfServiceAccumulatedAccount).State = EntityState.Modified;
                }
                else
                {
                    return Content("nullAccount-مخصص نهاية الخدمة");
                }

                if (EmployeeReceivableValueAccount != null)
                {
                    var EmployeeReceivableOnSystem = db.EmployeeOpenningBalances.Where(c => c.DepartmentId == department.Id).Sum(c => c.EmployeeReceivableValue);

                    EmployeeReceivableValueAccount.ObDebit = EmployeeReceivableValueAccount.ObDebit - (EmployeeReceivableOnSystem != null ? EmployeeReceivableOnSystem : 0) + (EmployeeReceivableValueSum != null ? EmployeeReceivableValueSum : 0);

                    db.Entry(EmployeeReceivableValueAccount).State = EntityState.Modified;
                }
                else
                {
                    return Content("nullAccount-ذمم العاملين");
                }

                if (DueSalariesValueAccount != null)
                {
                    var DueSalariesValueOnSystem = db.EmployeeOpenningBalances.Where(c => c.DepartmentId == department.Id).Sum(c => c.DueSalariesValue);

                    DueSalariesValueAccount.ObCredit = DueSalariesValueAccount.ObCredit - (DueSalariesValueOnSystem != null ? DueSalariesValueOnSystem : 0) + (DueSalariesValueSum != null ? DueSalariesValueSum : 0);

                    db.Entry(DueSalariesValueAccount).State = EntityState.Modified;
                }
                else
                {
                    return Content("nullAccount-الأجور المستحقة");
                }

                db.EmployeeOpenningBalances.RemoveRange(db.EmployeeOpenningBalances.Where(r => r.DepartmentId == eOb.DepartmentId));
                db.EmployeeOpenningBalances.AddRange(EmployeeOBList);
                db.SaveChanges();
                return Content("true");


            }
            return Content("false");

        }
    }
}