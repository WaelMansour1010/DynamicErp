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
    public class PenaltyIssueController : Controller
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();

        // GET: PenaltyIssue
        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة اصدار الجزاءات",
                EnAction = "Index",
                ControllerName = "PenaltyIssue",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("PenaltyIssue", "View", "Index", null, null, "اصدار الجزاءات");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            IQueryable<PenaltyIssue> penaltyIssues;
            if (string.IsNullOrEmpty(searchWord))
            {
                penaltyIssues = db.PenaltyIssues.Where(c => c.IsDeleted == false).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PenaltyIssues.Where(c => c.IsDeleted == false).Count();
            }
            else
            {
                penaltyIssues = db.PenaltyIssues.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Employee.EnName.Contains(searchWord))).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PenaltyIssues.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Employee.EnName.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(await penaltyIssues.ToListAsync());
        }

        // GET: PenaltyIssue/Edit/5
        public async Task<ActionResult> AddEdit(int? id)
        {
            ViewBag.penaltyTypeId = new SelectList(db.PenaltyTypes.Where(x => x.IsActive && !x.IsDeleted).Select(x => new { Id=x.Id,
                ArName = x.ArName + (x.CalculationMethodId == 1 ? " / ساعة" : x.CalculationMethodId == 2 ? " / يوم" : x.CalculationMethodId == 3 ? " / قيمة" : " ")
            }), "Id", "ArName");
            if (id == null)
            {
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(x => x.IsActive && !x.IsDeleted).Select(x => new { x.Id, x.ArName }), "Id", "ArName");
                ViewBag.Year = new SelectList(new int[] { 2019, 2020, 2021, 2022, 2023, 2024, 2025, 2026, 2027 });
                ViewBag.Month = new SelectList(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });
                var DocumentNumber = await db.PenaltyIssues.OrderByDescending(x => x.Id).Select(x => x.DocumentNumber).FirstOrDefaultAsync();
                ViewBag.DocumentNumber = string.IsNullOrEmpty(DocumentNumber) ? 1 : int.Parse(DocumentNumber) + 1;
               
                return View();
            }
            PenaltyIssue penaltyIssue = await db.PenaltyIssues.FindAsync(id);
            if (penaltyIssue == null)
            {
                return HttpNotFound();
            }
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(x => x.IsActive && !x.IsDeleted).Select(x => new { x.Id, x.ArName }), "Id", "ArName", penaltyIssue.EmployeeId);
            ViewBag.Year = new SelectList(new int[] { 2019, 2020, 2021, 2022, 2023, 2024, 2025, 2026, 2027 }, penaltyIssue.Year);
            ViewBag.Month = new SelectList(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, penaltyIssue.Month);
            ViewBag.DocumentNumber = penaltyIssue.DocumentNumber;
            
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل اصدار الجزاءات",
                EnAction = "AddEdit",
                ControllerName = "PenaltyIssue",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = penaltyIssue.Id,
                CodeOrDocNo = penaltyIssue.DocumentNumber
            });
            return View(penaltyIssue);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult> AddEdit([Bind(Include = "Id,DocumentNumber,Month,Year,EmployeeId,IsDeleted,PenaltyIssueDetails")] PenaltyIssue penaltyIssue)
        {
            if (ModelState.IsValid)
            {
                var id = penaltyIssue.Id;
                penaltyIssue.IsDeleted = false;
                if (penaltyIssue.Id > 0)
                {
                    db.PenaltyIssueDetails.RemoveRange(db.PenaltyIssueDetails.Where(x => x.MainDocId == penaltyIssue.Id));
                    var penaltyIssueDetails = penaltyIssue.PenaltyIssueDetails.ToList();
                    penaltyIssueDetails.ForEach((x) => x.MainDocId = penaltyIssue.Id);
                    penaltyIssue.PenaltyIssueDetails = null;
                    db.Entry(penaltyIssue).State = EntityState.Modified;
                    db.PenaltyIssueDetails.AddRange(penaltyIssueDetails);
                }
                else
                {
                    db.PenaltyIssues.Add(penaltyIssue);
                }
                await db.SaveChangesAsync();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل اصدار الجزاءات" : "اضافة اصدار الجزاءات",
                    EnAction = "AddEdit",
                    ControllerName = "PenaltyIssue",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = penaltyIssue.Id,
                    CodeOrDocNo = penaltyIssue.DocumentNumber
                });
                Notification.GetNotification("PenaltyIssue", id > 0 ? "Edit" : "Add", "AddEdit", penaltyIssue.Id, null, "اصدار الجزاءات");
                return Json(new { success = true });
            }
            var errors = ModelState
                   .Where(x => x.Value.Errors.Count > 0)
                   .Select(x => new { x.Key, x.Value.Errors })
                   .ToArray();
            return Json(new { success = false, errors });
        }


        // POST: PenaltyIssue/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            PenaltyIssue penaltyIssue = new PenaltyIssue() { Id = id, DocumentNumber = ""};
            db.PenaltyIssues.Attach(penaltyIssue);
            penaltyIssue.IsDeleted = true;
            db.Entry(penaltyIssue).Property(x => x.IsDeleted).IsModified = true;

            await db.SaveChangesAsync();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف اصدار الجزاءات",
                EnAction = "AddEdit",
                ControllerName = "PenaltyIssue",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                CodeOrDocNo = penaltyIssue.DocumentNumber
            });
            Notification.GetNotification("PenaltyIssue", "Delete", "Delete", id, null, "اصدار الجزاءات");
            return Content("true");
        }

        [SkipERPAuthorize]
        public async Task<JsonResult> PreviousPenaltyMonths(int employeeId, int year)
        {
            return Json(await db.PenaltyIssues.Where(x => x.EmployeeId == employeeId && !x.IsDeleted && x.Year == year).Select(x => x.Month).ToArrayAsync(), JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult EmployeeSalary(int employeeId)
        {            
            var TotalMonthSalary = db.EmployeeContractSalaryItems.Where(a => a.EmployeeId == employeeId).Select(a => a.ItemValue).Sum();
            return Json(new {TotalMonthSalary , TotalHourSalary=TotalMonthSalary / 30/8}, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetTypeSalary(int employeeId,int id)
        {
            var _PenaltyType = db.PenaltyTypes.Where(a => a.IsActive && !a.IsDeleted&&a.Id==id).FirstOrDefault();
            var SalaryItemId = db.PenaltyTypeDetails.Where(a => a.IsDeleted == false && a.MainDocId == id).Select(a => a.SalaryItemId).ToList();
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
            if (_PenaltyType.CalculationMethodId == 1)
            {
                hourlySalary = monthlySalary / 30 / 8;
            }
            else if (_PenaltyType.CalculationMethodId == 2)
            {
                hourlySalary = monthlySalary / 30;
            }
            // بنجيب اول واحد فى الجدول "باشمهندس اللى قال"
            var Equivalent = _PenaltyType.PenaltyTypeDetails.Where(a => a.SalaryItemId == SalaryItemId[0]).FirstOrDefault() != null ? _PenaltyType.PenaltyTypeDetails.Where(a => a.SalaryItemId == SalaryItemId[0]).FirstOrDefault().Equivalent : 1;
            return Json(new { monthlySalary, hourlySalary = /*hourlySalary*/ hourlySalary * (decimal?)Equivalent, CalculationMethodId = _PenaltyType.CalculationMethodId }, JsonRequestBehavior.AllowGet);
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
