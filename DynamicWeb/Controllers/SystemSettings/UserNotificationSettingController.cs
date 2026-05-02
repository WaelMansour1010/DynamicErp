using System;
using System.Data;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;

namespace MyERP.Controllers.SystemSettings
{
    public class UserNotificationSettingController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public int actionid;
        // GET: UserNotificationSetting
        public ActionResult Index()
        {
            ViewBag.UserId = new SelectList(db.ERPUsers.Where(u => u.IsDeleted == false && u.IsActive == true).Select(b => new
            {
                Id = b.Id,
                Name = b.UserName + " - " + b.Name
            }), "Id", "Name");
            ViewBag.PageId = new SelectList(db.SystemPages.Where(s=>s.IsDeleted==false), "Id", "ArName");
            return View();
        }
        [HttpPost]
        public ActionResult Index(ICollection<UserNotificationSetting> userNotifications, int pageId ,int userId)
        {
            if (ModelState.IsValid)
            {
             //   var userId = userNotifications.FirstOrDefault().UserId;
                db.UserNotificationSettings.RemoveRange(db.UserNotificationSettings.Where(r => r.UserId == userId && r.PageAction.PageId==pageId));
               if(userNotifications!=null)
                {
                    db.UserNotificationSettings.AddRange(userNotifications);
                }
                db.SaveChanges();
                return Content("true");
            }
            return Content("false");
        }
        [SkipERPAuthorize]
        public ActionResult ActionsByPageId(int userId, int pageId)
        {
            return PartialView(db.UserNotificationSetting_Get(userId, pageId));
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