using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers
{
    [ERPAuthorize]
    public class DeliveryInvoicesController : Controller
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();

        // GET: DeliveryInvoices
        public ActionResult Index()
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            bool? IsCashier = db.ERPUsers.Where(e => e.Id == userId).FirstOrDefault().IsCashier;
            ViewBag.IsCashier = IsCashier;

            ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            return View();
        }
    }
}