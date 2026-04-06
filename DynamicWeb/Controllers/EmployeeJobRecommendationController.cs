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
    public class EmployeeJobRecommendationController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: EmployeeJobRecommendation
        public ActionResult Index(DateTime? dateFrom, DateTime? dateTo, int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة متابعة الموظفين",
                EnAction = "Index",
                ControllerName = "EmployeeJobRecommendation",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            Notification.GetNotification("EmployeeJobRecommendation", "View", "Index", null, null, "متابعة الموظفين");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            var employeeJobRecommendations = db.GetEmployeeJobRecommendation().ToList();
            ViewBag.Count = employeeJobRecommendations.Count();

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(employeeJobRecommendations.ToList());
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