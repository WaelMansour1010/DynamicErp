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
    public class ERPRoleReportsController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: UserPrivilege/Edit/5
        public ActionResult Index()
        {
            ViewBag.UserRoleId = new SelectList(db.ERPRoles.Where(u => u.IsDeleted == false && u.IsActive == true).Select(b => new
            {
                Id = b.Id,
                Name = b.ArName + " - " + b.Code
            }), "Id", "Name");


            return View();
        }


        [HttpPost]
        public ActionResult Index(ERPRole role)
        {
            if (ModelState.IsValid)
            {
            
                    MyXML.xPathName = "ERPRoleReports";
                    var userReports = MyXML.GetXML(role.ERPRoleReports.Where(a => a.Privileged == true));
                    db.ERPRoleReport_Insert(role.Id, userReports);
                
              
                return RedirectToAction("Index", "Home");
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
            ViewBag.UserRoleId = new SelectList(db.ERPRoles.Where(u => u.IsDeleted == false && u.IsActive == true).Select(b => new
            {
                Id = b.Id,
                Name = b.ArName + " - " + b.Code
            }), "Id", "Name");
            return Content("false");
        }
        [SkipERPAuthorize]
        public ActionResult GetERPRoleReports(int roleId)
        {
            return PartialView(db.GetUserGroupReports(roleId));
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
