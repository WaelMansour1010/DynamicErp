using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers.AccountSettings
{
    public class CostCenterOBController : Controller
    {
        // GET: CostCenter
        public ActionResult Index()
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الارصدة الافتتاحية لمراكز التكلفة",
                EnAction = "Index",
                ControllerName = "CostCenterOB",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("CostCenterOB", "View", "Index", null, null, "الارصدة الافتتاحية للحسابات");
            return View();

        }

        public JsonResult GetCostCenterOB()
        {
            using (MySoftERPEntity db = new MySoftERPEntity())
            {
                return Json(db.CostCenterOB_Get().ToList(), JsonRequestBehavior.AllowGet);
            }
        }
    }
}