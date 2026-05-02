using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace MyERP.Controllers.SystemSettings.GroupCoding
{
    public class JobTitleController : Controller
    {
        // GET: Company
        public ActionResult Index()
        {
            if (Session["MSERPtoken"] == null)
                return Content("<script>window.location = '/login/';</script>");
            //List<MySoft_DAL.ERP_DB_Model.Company> obj = new CallingAPI<MySoft_DAL.ERP_DB_Model.Company>().GETAsync();
            return PartialView("~/Views/SystemSettings/GroupCoding/JobTitle/Index.cshtml");
        }

        public ActionResult AddEdit(int id = 0)
        {
            if (Session["MSERPtoken"] == null)
                return Content("<script>window.location = '/login/';</script>");
            ViewBag.ID = id;
            return PartialView("~/Views/SystemSettings/GroupCoding/JobTitle/AddEdit.cshtml");
        }
        
    }
}