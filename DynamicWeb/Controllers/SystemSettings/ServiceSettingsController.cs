using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers.SystemSettings
{
    public class ServiceSettingsController : Controller
    {
        // GET: ServiceSettings
        public ActionResult Index()
        {
            if (Session["MSERPtoken"] == null)
                return Content("<script>window.location = '/login/';</script>");


            return PartialView("~/Views/ServiceSettings/Index.cshtml");
        }

       
    }
}