using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers
{
    [ERPAuthorize]
    public class DriversWithInvoicesController : Controller
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();

        // GET: DriversWithInvoices
        public ActionResult Index()
        {
            ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            return View();
        }
    }
}