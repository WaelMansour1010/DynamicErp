using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;

namespace MyERP.Controllers
{
    
    public class FinancialStatementController : Controller
    {
        // GET: FinancialStatement
        private MySoftERPEntity db = new MySoftERPEntity();
       
        public ActionResult Index()
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName");
            }

            try
            {
                ViewBag.DateFrom = db.JournalEntries.Min(j => j.Date).ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception)
            {
                ViewBag.DateFrom = DateTime.MinValue.AddYears(1970).ToString("yyyy-MM-ddTHH:mm");
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة المركز المالى",
                EnAction = "Index",
                ControllerName = "FinancialStatement",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("FinancialStatement", "View", "Index", null, null, "ميزان المراجعة");
            //////////-----------------------------------------------------------------------

            return View();
        }

        public JsonResult GetFinancialStatement(int? depId)
        {
            return Json(db.GetFinancialStatement(depId,null, null,null, null), JsonRequestBehavior.AllowGet);
        }
    }
}