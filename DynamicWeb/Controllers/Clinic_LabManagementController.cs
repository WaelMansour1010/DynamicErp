using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers
{
    public class Clinic_LabManagementController : Controller
    {
        // GET: Clinic_LabManagement
        public ActionResult Index()
        {
            return View();

        }

        public ActionResult AddEdit()
        {
            return View();
        }

    }
}