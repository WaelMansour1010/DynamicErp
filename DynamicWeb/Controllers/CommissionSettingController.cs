using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;

namespace MyERP.Controllers
{
    public class CommissionSettingController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: CommissionSetting
        public ActionResult Index()
        {
            var Settings = db.CommissionSettings.ToList();
            if(Settings.Count()>0)
            {
                return View(Settings);
            }
            else
            return View();
        }

        [HttpPost]
        public ActionResult Index(ICollection<CommissionSetting> commissionSettings)
        {
            if (ModelState.IsValid)
            {
                var old = db.CommissionSettings.ToList();
                db.CommissionSettings.RemoveRange(old);
                db.CommissionSettings.AddRange(commissionSettings);
                db.SaveChanges();
                return Json(new { success = "true" });
            }
            return View();

        }
    }
}