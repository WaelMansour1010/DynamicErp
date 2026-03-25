using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using MyERP.Models.CustomModels;

namespace MyERP.Controllers.HR
{
    public class EmployeeSalaryItemController : ViewToStringController
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();

        // GET: EmployeeSalaryItem
        public ActionResult Index()
        {
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(x => x.IsActive && !x.IsDeleted).Select(x => new { x.Id, x.ArName }), "Id", "ArName");
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> AddEdit(ICollection<EmployeeSalaryItem> employeeSalaryItems)
        {
            if (ModelState.IsValid)
            {
                var employeeId = employeeSalaryItems.Select(x => x.EmployeeId).FirstOrDefault();
                db.EmployeeSalaryItems.RemoveRange(db.EmployeeSalaryItems.Where(x => x.EmployeeId == employeeId));
                db.EmployeeSalaryItems.AddRange(employeeSalaryItems);
                await db.SaveChangesAsync();
                return Json("true");
            }
            return Json("false");
        }

        [SkipERPAuthorize]
        public async Task<JsonResult> GetSalaryItems(int employeeId)
        {
            var salaryItems=(await db.EmployeeSalaryItems.Where(x => x.SalaryItem.IsActive && !x.SalaryItem.IsDeleted && x.EmployeeId == employeeId).Select(x => new { x.SalaryItemId, x.SalaryItem.Code, x.SalaryItem.ArName, x.Amount,x.SalaryItem.Type })
                    .Union(db.SalaryItems.Where(x => x.IsActive && !x.IsDeleted && !db.EmployeeSalaryItems.Where(e => e.EmployeeId == employeeId).Select(e => e.SalaryItemId).Contains(x.Id)).Select(x => new { SalaryItemId = x.Id, x.Code, x.ArName, Amount = (decimal)0 ,x.Type})).ToListAsync()).Select(x => new SalaryItemDto { SalaryItemId = x.SalaryItemId, Code = x.Code, ArName = x.ArName, Amount = x.Amount ,Type=x.Type});
            string due = RenderRazorViewToString("GetSalaryItems", salaryItems.Where(x => x.Type == 0));
            string deduction = RenderRazorViewToString("GetSalaryItems", salaryItems.Where(x => x.Type == 1));
            return Json(new { due, deduction }, JsonRequestBehavior.AllowGet);
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
