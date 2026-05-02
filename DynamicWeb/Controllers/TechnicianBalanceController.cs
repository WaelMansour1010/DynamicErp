using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers
{
    public class TechnicianBalanceController : Controller
    {
        private MyERP.Models.MySoftERPEntity db = new Models.MySoftERPEntity();
        // GET: TechnicianBalance
        public ActionResult Index()
        {
            return View(db.Technicians_Balance(null));
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