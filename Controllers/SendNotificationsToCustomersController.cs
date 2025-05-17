using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers.SystemSettings
    {
        public class SendNotificationsToCustomersController : Controller
        {
            Models.MySoftERPEntity db = new Models.MySoftERPEntity();
            // GET: SendNotification
            public ActionResult Index()
            {
                ViewBag.CustomerId = new SelectList(db.Customers.Where(t => t.IsActive == true && t.IsDeleted == false).Select(t => new { t.Id, ArName = t.Code + " - " + t.ArName }), "Id", "ArName");
                return View();
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