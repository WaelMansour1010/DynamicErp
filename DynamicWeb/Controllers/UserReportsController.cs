using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;

namespace MyERP.Controllers
{
    
    public class UserReportsController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: UserPrivilege/Edit/5
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
        public ActionResult Index(ERPUser user)
        {
            if (ModelState.IsValid)
            {

                MyXML.xPathName = "ERPUserReports";
                var userReports = MyXML.GetXML(user.UserReports.Where(a => a.Privileged == true));
                db.UserReport_Insert(user.Id, userReports);

                return RedirectToAction("Index", "Home");
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
           
            return Content("false");
        }
        [SkipERPAuthorize]
        public ActionResult UserReports(int userId)
        {
            return PartialView(db.GetUserReports(userId));
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
