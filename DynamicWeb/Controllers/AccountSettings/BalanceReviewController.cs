using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Security.Claims;
using MyERP.Repository;
using System.Threading.Tasks;
using System.Data.Entity;

namespace MyERP.Controllers.AccountSettings
{
    
    public class BalanceReviewController : Controller
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();
        // GET: BalanceReview
        public async Task<ActionResult> Index()
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            ViewBag.DepartmentId = new SelectList(await departmentRepository.UserDepartments(userId).ToListAsync(), "Id", "ArName");
            
            try
            {
                ViewBag.DateFrom = db.JournalEntries.Min(j => j.Date).ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception)
            {
                ViewBag.DateFrom = DateTime.MinValue.AddYears(1970).ToString("yyyy-MM-ddTHH:mm");
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح ميزان المراجعة",
                EnAction = "Index",
                ControllerName = "BalanceReview",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("BalanceReview", "View", "Index", null, null, "ميزان المراجعة");

            /////////////-----------------------------------------------------------------------

            return View();
        }

        public JsonResult GETBalanceReview(DateTime from, DateTime to, int? depId, int? activityId = null, int? companyId = null, int? accountId = null, string reportType = "General")
        {
            var result = db.GetBalanceReview(from, to, depId, activityId, companyId, accountId, reportType).ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
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