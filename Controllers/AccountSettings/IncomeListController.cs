using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;

using System.IO;
using System.Security.Claims;

namespace MyERP.Controllers.AccountSettings
{
    
    public class IncomeListController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: IncomeList
        public ActionResult Index()
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

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
                ViewBag.DateFrom = db.JournalEntries.Min(j=>j.Date).ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception)
            {
                ViewBag.DateFrom = DateTime.MinValue.AddYears(1970).ToString("yyyy-MM-ddTHH:mm");
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الدخل",
                EnAction = "Index",
                ControllerName = "IncomeList",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("IncomeList", "View", "Index", null, null, "الدخل");

            return View();
        }

        //public ActionResult Reports(string id,int depId)
        //{
        //    LocalReport lr = new LocalReport();
        //    string path = Path.Combine(Server.MapPath("~/Reports"), ("IncomeReport.rdlc"));
        //    if (System.IO.File.Exists(path))
        //    {
        //        lr.ReportPath = path;
        //    }
        //    else
        //    {
        //        return View("Index");
        //    }
        //    List<GetIncomeList_Result> cm = new List<GetIncomeList_Result>();
        //    cm = db.GetIncomeList(depId).ToList();
        //    ReportDataSource rd = new ReportDataSource("DataSet1", cm);
        //    lr.DataSources.Add(rd);
        //    string reportType = id;
        //    string mimeType;
        //    string encoding;
        //    string fileNameextension;
        //    string deviceInfo =
        //        "<DeviceInfo>" +
        //        "<OutputFormat>" + id + "</OutputFormat>" +
        //         "</DeviceInfo>";
        //    Warning[] warning;
        //    string[] streams;
        //    byte[] renderedbytes;
        //    renderedbytes = lr.Render(
        //        reportType,
        //        deviceInfo,
        //        out mimeType,
        //        out encoding,
        //        out fileNameextension,
        //        out streams,
        //        out warning

        //        );
        //    return File(renderedbytes, mimeType);

        //}
        public JsonResult GETIncomeList(DateTime from, DateTime to,int? id)
        {
            return Json(db.GetIncomeList( id, from,to,null,null), JsonRequestBehavior.AllowGet);
        }
    }
}