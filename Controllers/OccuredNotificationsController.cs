using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;

namespace MyERP.Controllers
{
    public class OccuredNotificationsController : ViewToStringController
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: OccuredNotifications
        public async Task<ActionResult> Index()
        {
            int UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var occuredNotifications = db.OccuredNotifications.Where(c => c.UserId == UserId).OrderByDescending(x => x.Id).Take(50);
            ViewBag.Count = db.OccuredNotifications.Where(c => c.UserId == UserId).Count();
            return View(await occuredNotifications.ToListAsync());
        }

        public async Task<ActionResult> _index(int pageIndex = 0)
        {
            int UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            return PartialView(await db.OccuredNotifications.Where(x => x.UserId == UserId).OrderByDescending(x => x.Id).Skip(50 * pageIndex > 0 ? pageIndex - 1 : 0).Take(50).ToListAsync());
        }

        [SkipERPAuthorize]
        public async Task<JsonResult> Notifications()
        {
            int UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            int empId = db.ERPUsers.Where(u => u.Id == UserId).FirstOrDefault().EmployeeId;
            int? UserDepartmentId = db.Employees.Where(a => a.Id==empId).FirstOrDefault().DepartmentId;
            var UnAcceptedTransferCount = db.GetCountOfUnAcceptedStockTransferVoucherForDepartment(UserDepartmentId);
            var occuredNotifications = await db.OccuredNotifications.Where(p => p.UserId == UserId).OrderByDescending(x => x.Id).Take(10).ToListAsync();
            string view = RenderRazorViewToString("Notifications", occuredNotifications);
            return Json(new { notifications = view, count = db.OccuredNotifications.Where(p => p.IsRead == false && p.UserId == UserId).Count(), UnAcceptedTransferCount= UnAcceptedTransferCount }, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        [HttpPost]
        public async Task<ActionResult> NotificationRead(int id)
        {
            await db.Database.ExecuteSqlCommandAsync($"update OccuredNotifications set IsRead=1 where Id={id}");
            return Content("true");
        }

        [SkipERPAuthorize]
        [HttpPost]
        public async Task<ActionResult> AllNotificationRead()
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            await db.Database.ExecuteSqlCommandAsync($"update OccuredNotifications set IsRead=1 where UserId={userId}");
            return Content("true");
        }
        [SkipERPAuthorize]
        public ActionResult ListView()
        {
            var userid = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var list = db.UserHomePages.Where(c => c.UserId == userid && c.Appear == true).ToList();
            ViewBag.count = list.Count();
            return PartialView("ListView", list);
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
