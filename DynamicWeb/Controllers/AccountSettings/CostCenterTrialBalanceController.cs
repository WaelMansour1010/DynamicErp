using MyERP.Models;
using MyERP.Repository;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers.AccountSettings
{
    public class CostCenterTrialBalanceController : Controller
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();
        // GET: CostCenterTrialBalance
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
            return View();
        }


        public JsonResult GetTrialBalance(DateTime from, DateTime to, int? depId)
        {
            return Json(db.CostCenter_TrialBalance(from, to, depId,null,null), JsonRequestBehavior.AllowGet);
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