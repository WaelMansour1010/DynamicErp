using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Security.Claims;

namespace MyERP.Controllers.SystemSettings
{
    
    public class DeletedTransactionController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: DeletedTransaction
        public ActionResult Index(string table=null)
        {
            ViewBag.SystemPages = new SelectList(db.SystemPages.Where(s=> s.IsMasterFile==false), "TableName", "ArName",table);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الحركات الملغاة",
                EnAction = "Index",
                ControllerName = "DeletedTransaction",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("DeletedTransaction", "View", "Index", null, null, " الحركات الملغاة");


            return View();
        }

        public ActionResult GetDeletedTransaction(string table, DateTime? from, DateTime? to)
        {
            ViewBag.Controller = db.SystemPages.Where(s => s.TableName == table).FirstOrDefault().ControllerName;
            var query = QueryHelper.DeletedTransactions(table, from, to);
            return PartialView(query);
        }
    }
}