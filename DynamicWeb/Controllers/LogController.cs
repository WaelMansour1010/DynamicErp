using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers
{
    public class LogController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: Log
        public ActionResult Index(DateTime? fromDate, DateTime? toDate, int pageIndex = 1, int wantedRowsNo = 10, int userId = 0, int systemPageId = 0, string actionName = "", string docNo = "")
        {
            var count= 0;
            ViewBag.Count = count;
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            ViewBag.Users = new SelectList(db.ERPUsers.Where(a => (a.IsActive == true)).Select(u => new
            {
                u.Id,
                Name = u.Name
            }), "Id", "Name", userId);

            ViewBag.SystemPageId = new SelectList(db.SystemPages.Where(a => (a.IsActive == true)).Select(s => new
            {
                s.Id,
                ArName = s.ArName
            }), "Id", "ArName", systemPageId);

            //ViewBag.ActionName = new SelectList(db.PageActions.Where(a => (a.IsActive == true && a.PageId == systemPageId)).Select(pa => new
            //{
            //    pa.ArName,
            //    Name = pa.ArName
            //}), "ArName", "ArName", actionName);

            ViewBag.DocNo = docNo;
            ViewBag.ActionName = actionName;

            if (fromDate != null && toDate != null)
            {
                ViewBag.fromDate = fromDate.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.toDate = toDate.Value.ToString("yyyy-MM-ddTHH:mm");
            }

            if (fromDate == null && toDate == null)
            {
                List<MyLog_GetLogList_Result> EmpList = new List<MyLog_GetLogList_Result>();
                return View(EmpList);
            }

            var logList = db.MyLog_GetLogList(fromDate, toDate, userId, systemPageId, actionName, docNo).ToList();

            count = logList.Count();
            ViewBag.Count = count;
            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
          
                var logListPaging = logList.OrderBy(r => r.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                return View(logListPaging.ToList());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}