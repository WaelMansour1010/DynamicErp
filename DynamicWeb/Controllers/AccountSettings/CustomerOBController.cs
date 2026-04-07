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
using System.Threading;
using System.Globalization;

namespace MyERP.Controllers
{
    
    public class CustomerOBController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Customers
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمةالأرصدةالإفتتاحية للعملاء",
                EnAction = "Index",
                ControllerName = "CustomerOB",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("CustomerOB", "View", "Index", null, null, "الأرصدةالإفتتاحية للعملاء");
       
            SystemSetting sysObj = db.SystemSettings.FirstOrDefault();
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
             
            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName");
                
            }
   
         
            return View();
        }
        [SkipERPAuthorize]
        public ActionResult _GetAllCustomers(int id)
        {
            return PartialView(db.GetCustomerOB(id));
        }

        //save changes in OBDebit &OBcredit
        [HttpPost]
        public ActionResult Save(List<CustomerOpenningBalance> CustomerOBList)
        {
            if (ModelState.IsValid)
            {
                var cOb = CustomerOBList.FirstOrDefault();
                Department department = db.Departments.Find(cOb.DepartmentId);
                ChartOfAccount account = db.ChartOfAccounts.Find(department.CustomersAccountId);
                if (account != null)
                {
                    account.ObCredit = account.ObCredit != null ? account.ObCredit : 0;
                    account.ObDebit = account.ObDebit != null ? account.ObDebit : 0;
                    var obCredit = CustomerOBList.Sum(c => c.OBCredit);
                    var obDebit = CustomerOBList.Sum(c => c.OBDebit);
                    var obDebitOnSystem = db.CustomerOpenningBalances.Where(c => c.DepartmentId == department.Id).Sum(c => c.OBDebit);
                    var obCreditOnSystem = db.CustomerOpenningBalances.Where(c => c.DepartmentId == department.Id).Sum(c => c.OBCredit);
                    account.ObCredit = account.ObCredit - (obCreditOnSystem != null ? obCreditOnSystem : 0) + (obCredit != null ? obCredit : 0);
                    account.ObDebit = account.ObDebit - (obDebitOnSystem != null ? obDebitOnSystem : 0) + (obDebit != null ? obDebit : 0);
                    db.CustomerOpenningBalances.RemoveRange(db.CustomerOpenningBalances.Where(r => r.DepartmentId == cOb.DepartmentId));
                    db.CustomerOpenningBalances.AddRange(CustomerOBList);
                    db.Entry(account).State = EntityState.Modified;
                    db.SaveChanges();
                    return Content("true");
                }
                return Content("nullAccount");
            }
            return Content("false");
        }


        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(List<CustomerOpenningBalance> CustomerOBList)
        {
            if (ModelState.IsValid)
            {
                var cOb = CustomerOBList.FirstOrDefault();
                Department department = db.Departments.Find(cOb.DepartmentId);
                ChartOfAccount account = db.ChartOfAccounts.Find(department.CustomersAccountId);
                if (account != null)
                {
                    account.ObCredit = account.ObCredit != null ? account.ObCredit : 0;
                    account.ObDebit = account.ObDebit != null ? account.ObDebit : 0;
                    var obCredit = CustomerOBList.Sum(c => c.OBCredit);
                    var obDebit = CustomerOBList.Sum(c => c.OBDebit);
                    var obDebitOnSystem = db.CustomerOpenningBalances.Where(c => c.DepartmentId == department.Id).Sum(c => c.OBDebit);
                    var obCreditOnSystem = db.CustomerOpenningBalances.Where(c => c.DepartmentId == department.Id).Sum(c => c.OBCredit);
                    account.ObCredit = account.ObCredit - (obCreditOnSystem != null ? obCreditOnSystem : 0) + account.ObCredit - (obCredit != null ? obCredit : 0);
                    account.ObDebit = account.ObDebit - (obDebitOnSystem != null ? obDebitOnSystem : 0) + account.ObDebit - (obDebit != null ? obDebit : 0);
                    db.CustomerOpenningBalances.RemoveRange(db.CustomerOpenningBalances.Where(r => r.DepartmentId == cOb.DepartmentId));
                    db.CustomerOpenningBalances.AddRange(CustomerOBList);
                    db.Entry(account).State = EntityState.Modified;
                    db.SaveChanges();
                    return Content("true");
                }
                return Content("nullAccount");
            }
            return Content("false");
        }


    }
}
