using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers.SystemSettings
{
    public class AdvertisingCompaignsController : Controller
    {
        // GET: Company
        public ActionResult Index()
        {
            if (Session["MSERPtoken"] == null)
                return Content("<script>window.location = '/login/';</script>");

            
            return PartialView("~/Views/AdvertisingCompaigns/Index.cshtml");
        }

        public ActionResult AddEdit(int id = 0)
        {
            if (Session["MSERPtoken"] == null)
                return Content("<script>window.location = '/login/';</script>");

            ViewBag.ID = id;
            return PartialView("~/Views/AdvertisingCompaigns/AddEdit.cshtml");
        }



    }
}