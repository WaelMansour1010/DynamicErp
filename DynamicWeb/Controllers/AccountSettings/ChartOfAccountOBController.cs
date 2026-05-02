using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Data.Entity;
using System.Security.Claims;

namespace MyERP.Controllers.AccountSettings
{

    public class ChartOfAccountOBController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: ChartOfAccountOB
        public ActionResult Index()
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الارصدة الافتتاحية للحسابات",
                EnAction = "Index",
                ControllerName = "ChartOfAccountOB",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("ChartOfAccountOB", "View", "Index", null, null, "الارصدة الافتتاحية للحسابات");
            ViewBag.Debit = db.ChartOfAccounts.Where(c => c.IsActive == true && c.IsDeleted == false && (c.CategoryId == 1 || c.CategoryId == 2)).Select(c => c.ObDebit).Sum();
            ViewBag.Credit = db.ChartOfAccounts.Where(c => c.IsActive == true && c.IsDeleted == false && (c.CategoryId == 1 || c.CategoryId == 2)).Select(c => c.ObCredit).Sum();
            //int pageid = db.Get_PageId("ChartOfAccountOB").SingleOrDefault().Value;
            //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "Index" && c.EnName == "View" && c.PageId == pageid).Id;
            //int userid = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            //var UserName = User.Identity.Name;
            //db.Sp_OccuredNotification(actionId, $"بفتح شاشة الارصدة الافتتاحية للحسابات  {UserName}قام المستخدم  ");
            //////////////////-----------------------------------------------------------------------

            return View();
        }

        public JsonResult GetChartOfAccountOB()
        {
            using (MySoftERPEntity db = new MySoftERPEntity())
            {
                return Json(db.GetChartOfAccountOB().ToList(), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult Save(ChartOfAccount chartOfAccount)
        {
            if (ModelState.IsValid)
            {
                ChartOfAccount Old = db.ChartOfAccounts.Find(chartOfAccount.Id);
                if (Old != null)
                {
                    Old.ObCredit = (chartOfAccount.ObCredit != null ? chartOfAccount.ObCredit : 0);
                    Old.ObDebit = (chartOfAccount.ObDebit != null ? chartOfAccount.ObDebit : 0);
                    db.Entry(Old).State = EntityState.Modified;
                    db.SaveChanges();
                    return Json("true");
                }
                return Json("nullAccount");

            }
            return Json("false");
        }
    }
}