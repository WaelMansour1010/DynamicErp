using DevExpress.Data.WcfLinq.Helpers;
using MyERP.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web.Mvc;

namespace MyERP.Controllers
{
    public class JobFollowUpController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: JobFollowUp
        public ActionResult Index( DateTime? dateFrom, DateTime? dateTo,int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة متابعة الوظائف",
                EnAction = "Index",
                ControllerName = "JobFollowUp",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            Notification.GetNotification("JobFollowUp", "View", "Index", null, null, "متابعة الوظائف");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            var jobFollowUps = db.GetJobFollowUp(dateFrom,dateTo).ToList();
            ViewBag.Count=jobFollowUps.Count();
            
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(jobFollowUps.ToList());
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