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

namespace MyERP.Controllers.SystemSettings
{
    
    public class RolePrivilegeController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public ActionResult Index()
        {

            ViewBag.RoleId = new SelectList(db.ERPRoles.Where(e => e.IsDeleted == false && e.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.PageId = new SelectList(db.SystemPages.Where(s =>s.IsModule==true&&s.IsActive==true&& s.IsDeleted == false && s.Id != 2116 && s.ParentId != 2116 /*(s.IsReport==false ||s.IsReport==null )*/), "Id", "ArName");
            return View();
        }


        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Index(ICollection<RolePrivilege> rolePrivileges)
        {
            if (ModelState.IsValid)
            {
                //var rp= rolePrivileges.FirstOrDefault();
                //db.RolePrivileges.RemoveRange(db.RolePrivileges.Where(r => r.RoleId == rp.RoleId && r.PageId==rp.PageId));
                //db.RolePrivileges.AddRange(rolePrivileges);
                int?[] pages = rolePrivileges.Select(u => u.PageId).ToArray();
                int? roleId = rolePrivileges.FirstOrDefault().RoleId;
                db.RolePrivileges.RemoveRange(db.RolePrivileges.Where(r => r.RoleId == roleId && pages.Contains(r.PageId)));
                db.RolePrivileges.AddRange(rolePrivileges);
                db.SaveChanges();
                return Content("true");
            }
            return Content("false");
        }

        [SkipERPAuthorize]
        public ActionResult ActionsByPageId(int roleId, int PageId)
        {
            return PartialView(db.GetRolePrivileges(roleId, PageId));
        }

        public JsonResult AllPages(int id, int? pageId)
        {
            return Json(db.GetRolePriviledges_AllPages(pageId, id), JsonRequestBehavior.AllowGet);
        }

        public JsonResult PagesByModule(int id)
        {
            var x = db.SystemPage_ByModule(id);
            return Json(x, JsonRequestBehavior.AllowGet);
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
