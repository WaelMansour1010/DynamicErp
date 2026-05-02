using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;

namespace MyERP.Controllers
{
    public class ShareHolderEquityController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: ShareHolderEquity
        public ActionResult Index()
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            ViewBag.DepartmentId = new SelectList(db.Department_ReportUserDepartments(userId), "Id", "ArName");
            return View();
        }

        public ActionResult _ShareHolderEquity(int depId)
        {
            return PartialView(db.ShareholderEquity_GetByDeparment(depId));
        }

        [HttpPost]
        public ActionResult PostShareholderEquity(ShareHolderEquity shareHolderEquity)
        {
            db.ShareHolderEquities.RemoveRange(db.ShareHolderEquities.Where(x => x.ShareHolderId == shareHolderEquity.ShareHolderId && x.DepartmentId==shareHolderEquity.DepartmentId));
            db.ShareHolderEquities.Add(shareHolderEquity);
            db.SaveChanges();
            return Json("true");
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