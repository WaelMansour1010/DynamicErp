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
using System.Security.Claims;

namespace MyERP.Controllers.HR
{
    public class OvertimeIssueController : Controller
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();

        // GET: OvertimeIssue
        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة اصدار الوقت الاضافي",
                EnAction = "Index",
                ControllerName = "OvertimeIssue",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("OvertimeIssue", "View", "Index", null, null, "اصدار الوقت الاضافي");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            IQueryable<OvertimeIssue> overtimeIssues;
            if (string.IsNullOrEmpty(searchWord))
            {
                overtimeIssues = db.OvertimeIssues.Where(c => c.IsDeleted == false).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.OvertimeIssues.Where(c => c.IsDeleted == false).Count();
            }
            else
            {
                overtimeIssues = db.OvertimeIssues.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Employee.EnName.Contains(searchWord))).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.OvertimeIssues.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Employee.EnName.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(await overtimeIssues.ToListAsync());
        }

        // GET: OvertimeIssue/Edit/5
        public async Task<ActionResult> AddEdit(int? id)
        {
            ViewBag.overtimeTypeId = new SelectList(db.OvertimeTypes.Where(x => x.IsActive && !x.IsDeleted).Select(x => new { x.Id,
                ArName = x.ArName + (x.CalculationMethodId == 1 ? " / ساعة" : x.CalculationMethodId == 2 ? " / يوم" : x.CalculationMethodId == 3 ? " / قيمة" : " ")
            }), "Id", "ArName");
            if (id == null)
            {
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(x => x.IsActive && !x.IsDeleted).Select(x => new { x.Id, ArName=x.Code+ " - " + x.ArName }), "Id", "ArName");
                ViewBag.Year = new SelectList(new int[] { 2019, 2020, 2021, 2022, 2023, 2024, 2025, 2026, 2027 });
                ViewBag.Month = new SelectList(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });
                var DocumentNumber = await db.OvertimeIssues.OrderByDescending(x => x.Id).Select(x => x.DocumentNumber).FirstOrDefaultAsync();
                ViewBag.DocumentNumber = string.IsNullOrEmpty(DocumentNumber) ? 1 : int.Parse(DocumentNumber) + 1;
                
                return View();
            }
            OvertimeIssue overtimeIssue = await db.OvertimeIssues.FindAsync(id);
            if (overtimeIssue == null)
            {
                return HttpNotFound();
            }
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(x => x.IsActive && !x.IsDeleted).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName }), "Id", "ArName", overtimeIssue.EmployeeId);
            ViewBag.Year = new SelectList(new int[] { 2019, 2020, 2021, 2022, 2023, 2024, 2025, 2026, 2027 }, overtimeIssue.Year);
            ViewBag.Month = new SelectList(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, overtimeIssue.Month);
            ViewBag.DocumentNumber = overtimeIssue.DocumentNumber;
           
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل اصدار الوقت الاضافي",
                EnAction = "AddEdit",
                ControllerName = "OvertimeIssue",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = overtimeIssue.Id,
                CodeOrDocNo = overtimeIssue.DocumentNumber
            });
            return View(overtimeIssue);
        }

        [HttpPost]
        public async Task<ActionResult> AddEdit(OvertimeIssue overtimeIssue)
        {
            if (ModelState.IsValid)
            {
                var id = overtimeIssue.Id;
                if (overtimeIssue.Id > 0)
                {
                    db.OvertimeIssueDetials.RemoveRange(db.OvertimeIssueDetials.Where(x => x.MainDocId == overtimeIssue.Id));
                    var overtimeIssueDetials = overtimeIssue.OvertimeIssueDetials.ToList();
                    overtimeIssueDetials.ForEach((x) => x.MainDocId = overtimeIssue.Id);
                    overtimeIssue.OvertimeIssueDetials = null;
                    db.Entry(overtimeIssue).State = EntityState.Modified;
                    db.OvertimeIssueDetials.AddRange(overtimeIssueDetials);
                }
                else
                {
                    db.OvertimeIssues.Add(overtimeIssue);
                }
                await db.SaveChangesAsync();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل اصدار الوقت الاضافي" : "اضافة اصدار الوقت الاضافي",
                    EnAction = "AddEdit",
                    ControllerName = "OvertimeIssue",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = overtimeIssue.Id,
                    CodeOrDocNo = overtimeIssue.DocumentNumber
                });
                Notification.GetNotification("OvertimeIssue", id > 0 ? "Edit" : "Add", "AddEdit", overtimeIssue.Id, null, "اصدار الوقت الاضافي");
                return Json(new { success = true });
            }
            var errors = ModelState
                   .Where(x => x.Value.Errors.Count > 0)
                   .Select(x => new { x.Key, x.Value.Errors })
                   .ToArray();
            return Json(new { success = false, errors });
        }

        // POST: OvertimeIssue/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            OvertimeIssue overtimeIssue = new OvertimeIssue() { Id = id, DocumentNumber = "" };
            db.OvertimeIssues.Attach(overtimeIssue);
            overtimeIssue.IsDeleted = true;
            db.Entry(overtimeIssue).Property(x => x.IsDeleted).IsModified = true;

            await db.SaveChangesAsync();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف اصدار الوقت الاضافي",
                EnAction = "AddEdit",
                ControllerName = "OvertimeIssue",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                CodeOrDocNo = overtimeIssue.DocumentNumber
            });
            Notification.GetNotification("OvertimeIssue", "Delete", "Delete", id, null, "اصدار الوقت الاضافي");
            return Content("true");
        }

        [SkipERPAuthorize]
        public async Task<JsonResult> PreviousOvertimeMonths(int employeeId, int year)
        {
            return Json(await db.OvertimeIssues.Where(x => x.EmployeeId == employeeId && !x.IsDeleted && x.Year == year).Select(x => x.Month).ToArrayAsync(), JsonRequestBehavior.AllowGet);
        }


        [SkipERPAuthorize]
        public JsonResult EmployeeSalary(int employeeId)
        {
            var TotalMonthSalary = db.EmployeeContractSalaryItems.Where(a => a.EmployeeId == employeeId).Select(a => a.ItemValue).Sum();
            return Json(new { TotalMonthSalary, TotalHourSalary = TotalMonthSalary / 30 / 8 }, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetTypeSalary(int employeeId, int id)
        
        {
            var _OverTimeType = db.OvertimeTypes.Where(a => a.IsActive && !a.IsDeleted && a.Id==id).FirstOrDefault();
            var SalaryItemId = db.OvertimeTypeDetails.Where(a => a.IsDeleted == false && a.MainDocId == id).Select(a => a.SalaryItemId).ToList();
            List<decimal?> SalaryItemValues = new List<decimal?>();
            decimal? SalaryItemValue = 0;

            for (var i = 0; i < SalaryItemId.Count; i++)
            {
                var salaryItemId = SalaryItemId[i];
                SalaryItemValue = db.EmployeeContractSalaryItems.Where(a => a.EmployeeId == employeeId && a.SalaryItemId == salaryItemId).Select(a => a.ItemValue).FirstOrDefault();
                SalaryItemValues.Add(SalaryItemValue);
            }
            var monthlySalary = SalaryItemValues.Sum();
            decimal? hourlySalary = 0;
            if (_OverTimeType.CalculationMethodId == 1)
            {
                hourlySalary = monthlySalary / 30 / 8;
            }
            else if (_OverTimeType.CalculationMethodId == 2)
            {
                hourlySalary = monthlySalary / 30;
            }
            // بنجيب اول واحد فى الجدول "باشمهندس اللى قال"
            var Equivalent = _OverTimeType.OvertimeTypeDetails.Where(a => a.SalaryItemId == SalaryItemId[0]).FirstOrDefault() != null ? _OverTimeType.OvertimeTypeDetails.Where(a => a.SalaryItemId == SalaryItemId[0]).FirstOrDefault().Equivalent : 1;
            return Json(new { monthlySalary, hourlySalary = /*hourlySalary*/ hourlySalary * (decimal?)Equivalent, CalculationMethodId = _OverTimeType.CalculationMethodId}, JsonRequestBehavior.AllowGet);
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
