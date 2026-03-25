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

    public class VendorOBController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمةالأرصدةالإفتتاحية للموردين",
                EnAction = "Index",
                ControllerName = "VendorOB",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("VendorOB", "View", "Index", null, null, "الأرصدةالإفتتاحية للموردين");

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
        public ActionResult _GetAllVendors(int id)
        {
            return PartialView(db.GetVendorOB(id));
        }
        //save changes in OBDebit &OBcredit
        [SkipERPAuthorize]
        [HttpPost]
        public ActionResult Save(List<VendorOpenningBalance> VendorOBList)
        {
            if (ModelState.IsValid)
            {
                var vOb = VendorOBList.FirstOrDefault();
                Department department = db.Departments.Find(vOb.DepartmentId);
                ChartOfAccount account = db.ChartOfAccounts.Find(department.VendorsAccountId);
                if (account != null)
                {
                    account.ObCredit = account.ObCredit != null ? account.ObCredit : 0;
                    account.ObDebit = account.ObDebit != null ? account.ObDebit : 0;
                    var obCredit = VendorOBList.Sum(c => c.OBCredit);
                    var obDebit = VendorOBList.Sum(c => c.OBDebit);
                    var obDebitOnSystem = db.VendorOpenningBalances.Where(c => c.DepartmentId == department.Id).Sum(c => c.OBDebit);
                    var obCreditOnSystem = db.VendorOpenningBalances.Where(c => c.DepartmentId == department.Id).Sum(c => c.OBCredit);
                    account.ObCredit = account.ObCredit - (obCreditOnSystem != null ? obCreditOnSystem : 0) + (obCredit != null ? obCredit : 0);
                    account.ObDebit = account.ObDebit - (obDebitOnSystem != null ? obDebitOnSystem : 0) + (obDebit != null ? obDebit : 0);
                    db.VendorOpenningBalances.RemoveRange(db.VendorOpenningBalances.Where(r => r.DepartmentId == vOb.DepartmentId));
                    db.VendorOpenningBalances.AddRange(VendorOBList);
                    db.Entry(account).State = EntityState.Modified;
                    db.SaveChanges();
                    return Content("true");
                }
                return Content("nullAccount");
            }
            return Content("false");
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(List<VendorOpenningBalance> VendorOBList)
        {
            if (ModelState.IsValid)
            {
                var vOb = VendorOBList.FirstOrDefault();
                Department department = db.Departments.Find(vOb.DepartmentId);
                ChartOfAccount account = db.ChartOfAccounts.Find(department.VendorsAccountId);
                if (account != null)
                {
                    account.ObCredit = account.ObCredit != null ? account.ObCredit : 0;
                    account.ObDebit = account.ObDebit != null ? account.ObDebit : 0;
                    var obCredit = VendorOBList.Sum(c => c.OBCredit);
                    var obDebit = VendorOBList.Sum(c => c.OBDebit);
                    var obDebitOnSystem = db.VendorOpenningBalances.Where(c => c.DepartmentId == department.Id).Sum(c => c.OBDebit);
                    var obCreditOnSystem = db.VendorOpenningBalances.Where(c => c.DepartmentId == department.Id).Sum(c => c.OBCredit);
                    account.ObCredit = account.ObCredit - (obCreditOnSystem != null ? obCreditOnSystem : 0) + account.ObCredit - (obCredit != null ? obCredit : 0);
                    account.ObDebit = account.ObDebit - (obDebitOnSystem != null ? obDebitOnSystem : 0) + account.ObDebit - (obDebit != null ? obDebit : 0);
                    db.VendorOpenningBalances.RemoveRange(db.VendorOpenningBalances.Where(r => r.DepartmentId == vOb.DepartmentId));
                    db.VendorOpenningBalances.AddRange(VendorOBList);
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
