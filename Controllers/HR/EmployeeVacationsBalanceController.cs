using MyERP.Models;
using MyERP.Repository;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace MyERP.Controllers.HR
{
    public class EmployeeVacationsBalanceController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: EmployeeVacationsBalance
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(IEnumerable<EmployeeVacationsBalance> EmployeeVacationsBalances)
        {
            foreach (var item in EmployeeVacationsBalances)
            {
                var obj = db.Employees.Find(item.EmployeeId);
                obj.CurrentYearBalanceEG = item.CurrentYearBalanceEG;
                obj.OpeningBalanceEG = item.OpeningBalanceEG;
                obj.PreviousVacationsEG = item.PreviousVacationsEG;
                obj.RemainingBalanceEG = item.RemainingBalanceEG;
                obj.IsDeported = item.IsDeported;
                db.Entry(obj).State = EntityState.Modified;
            }
            try
            {
                db.SaveChanges();
                return Json(new { success = "true" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = "false" }, JsonRequestBehavior.AllowGet);
            }
        }

        [SkipERPAuthorize]
        public JsonResult GetEmployeeData()
        {
            var vacationsBalance = db.Employees.Where(s => s.IsDeleted == false && s.IsActive == true).Select(a => new
            {
                a.Id,
                a.Code,
                a.ArName,
                a.CurrentYearBalanceEG,
                a.OpeningBalanceEG,
                PreviousVacationsEG =a.IsDeported==true?a.PreviousVacationsEG: db.EmployeeVacationsRegistrations.Where(v => v.EmployeeId == a.Id).Sum(v => v.NoOfDays),
                a.RemainingBalanceEG
            }).ToList();
            return Json(vacationsBalance, JsonRequestBehavior.AllowGet);
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
    public class EmployeeVacationsBalance
    {
        public int? EmployeeId { get; set; }
        public int? OpeningBalanceEG { get; set; }
        public int? CurrentYearBalanceEG { get; set; }
        public int? PreviousVacationsEG { get; set; }
        public int? RemainingBalanceEG { get; set; }
        public bool? IsDeported { get; set; }
    }
}