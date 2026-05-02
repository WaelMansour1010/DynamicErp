using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;

namespace MyERP.Controllers.SystemSettings
{
    public class UserPrivilegeController : Controller
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
            ViewBag.ModuleId = new SelectList(db.SystemPages.Where(a => a.IsModule == true && a.IsActive == true && a.IsDeleted == false && a.Id != 2116 && a.IsReport !=true), "Id", "ArName");//exclude report module
            return View();
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Index(ICollection<UserPrivilege> userPrivileges)
        {
            if (ModelState.IsValid)
            {
                int?[] pages = userPrivileges.Select(u => u.PageId).ToArray();
                int? userId = userPrivileges.FirstOrDefault().UserId;
                db.UserPrivileges.RemoveRange(db.UserPrivileges.Where(r => r.UserId == userId && pages.Contains(r.PageId)));
                db.UserPrivileges.AddRange(userPrivileges);
                db.SaveChanges();
                return Content("true");
            }
            return Content("false");
        }

        public JsonResult PagesByModule(int id)
        {
            var x = db.SystemPage_ByModule(id);
            return Json(x, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ActionsByPageId(int userId, int pageId)
        {
            return PartialView(db.GetUserPrivileges(userId, pageId));
        }

        public JsonResult AllPages(int id, int? moduleId)
        {
            return Json(db.GetUserPriviledges_AllPages(moduleId, id), JsonRequestBehavior.AllowGet);
        }
        //select all pages
        //public ActionResult Pages(int userId)
        //{
        //  var  ListOfPages=db.GetUserPriviledges_AllPages(userId).ToList();
        //    return PartialView(ListOfPages);
        //}
        //public ActionResult Pages(int userId)
        //{
        //    var systempage = db.SystemPages.Where(a => a.IsReport != true).ToList();
        //    List<AllSystemPageViewModel> lists = new List<AllSystemPageViewModel>();
        //    foreach (var item in systempage)
        //    {
        //        AllSystemPageViewModel pages=new AllSystemPageViewModel();
        //        db.GetUserPrivileges(userId)
        //    }


        //    return PartialView(parent);
        //}




        //public ActionResult GetPages()
        //{
        //    var systempage = db.SystemPages.Where(a => a.IsReport != true);
        //    return PartialView(systempage.ToList());
        //}

        //public JsonResult getActions(int userId, int pageId)
        //{
        //    var systempage = db.SystemPages.Where(a => a.IsReport != true);
        //    foreach (var page in systempage)
        //    {
        //        ViewBag.p = db.GetUserPrivileges(userId, page.Id);
        //    }
        //    return ()
        //}

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
