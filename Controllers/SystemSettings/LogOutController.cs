using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers.SystemSettings
{
    [SkipERPAuthorize]
    [AllowAnonymous]
    public class LogOutController : Controller
    {
        public ActionResult Index()
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "تسجيل الخروج",
                EnAction = "LogOut Index",
                ControllerName = "LogOut",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Request.GetOwinContext().Authentication.SignOut();
            Session["PosId"] = "";
            Session["IsCashier"] = "";
            return RedirectToAction("Index", "LogIn");
        }

    }
}