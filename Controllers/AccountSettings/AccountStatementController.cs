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
    
    public class AccountStatementController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: AccountStatement
        public ActionResult Index()
        {


            ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(d => d.IsDeleted == false && d.IsActive == true && d.ClassificationId==3), "Id", "ArName");
            try
            {
                ViewBag.DateFrom = db.JournalEntries.OrderBy(j => j.Date).FirstOrDefault().Date.ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception)
            {
                ViewBag.DateFrom = DateTime.MinValue.AddYears(1970).ToString("yyyy-MM-ddTHH:mm");
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح كشف حساب",
                EnAction = "AccountStatement",
                ControllerName = "AccountStatement",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("AccountStatement", "View", "Index", null, null, "كشف حساب");

            return View();
        }
        
        public JsonResult GETAccountStatement( int accId)
        {
            return Json(db.GetAccountStatement(null, null, accId, null, null,null), JsonRequestBehavior.AllowGet);
        }

        public ActionResult GETAccountStatementDetails(int accId)
        {
            return PartialView(db.GetAccountStatementDetails(null, null, accId,null, null, null, null, null));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
    
}
