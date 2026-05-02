using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;

namespace MyERP.Controllers.SystemSettings
{
    public class UserHomePageController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public ActionResult Index()
        {
            ViewBag.UserId = new SelectList(db.ERPUsers.Where(u => u.IsDeleted == false && u.IsActive == true).Select(b => new
            {
                Id = b.Id,
                Name = b.UserName + " - " + b.Name
            }), "Id", "Name");
           
            return View();
        }
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Index(ICollection<UserHomePage> userHomePage)
        {
            if (ModelState.IsValid)
            {
               
                var userId = userHomePage.FirstOrDefault().UserId; 
                db.UserHomePages.RemoveRange(db.UserHomePages.Where(r => r.UserId == userId));
                db.UserHomePages.AddRange(userHomePage);
                db.SaveChanges();
                return Content("true");
            }
            return Content("false");
        }
        public ActionResult ActionsByPageId(int userId)
        {
           
   return PartialView(db.GetUserHomePage(userId));
            
            
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
