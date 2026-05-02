using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers
{
    public class CashBoxBalanceController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: CashBoxBalance
        public ActionResult Index(int? depId)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

          
            List<int> cashesIds =db.CashBoxes.Where(d => (userId == 1 || db.UserCashBoxes.Where(u => u.UserId == userId && u.Privilege == true).Select(u => u.CashBoxId).Contains(d.Id)) && d.IsDeleted == false && d.IsActive == true).Select(d =>d.Id).ToList();

            List<dynamic> CashBoxesBalanceList =new List<dynamic>() ;
            var CashBoxesIds = db.CashBoxes.Where(a => a.IsDeleted == false && a.IsActive == true).Select(a => a.Id).ToList();
            foreach(var id in CashBoxesIds)
            {
                CashBoxesBalanceList.Add(db.CashBox_Balances(id).FirstOrDefault());
            }
            if (depId != null)
            {
                if (userId == 1)
                {
                    ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                    {
                        b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName",depId);

                }
                else
                {
                    ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                    {
                        Id = b.DepartmentId,
                        ArName = b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName", depId);
                }
                ViewBag.CashBoxesList = CashBoxesBalanceList.Where(a=>a.DepartmentId== depId && cashesIds.Contains(a.Id)).ToList();
            }
            else
            {
                ViewBag.CashBoxesList = CashBoxesBalanceList.Where(a=>(cashesIds.Contains(a.Id))).ToList();
                if (userId == 1)
                {
                    ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                    {
                        b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName");

                }
                else
                {
                    ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                    {
                        Id = b.DepartmentId,
                        ArName = b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName");
                }
            }
            return View();
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