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
    public class RewardIssueController : Controller
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();

        // GET: RewardIssue
        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة اصدار المكافئات",
                EnAction = "Index",
                ControllerName = "RewardIssue",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("RewardIssue", "View", "Index", null, null, "اصدار المكافئات");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            IQueryable<RewardIssue> rewardIssues;
            if (string.IsNullOrEmpty(searchWord))
            {
                rewardIssues = db.RewardIssues.Where(c => c.IsDeleted == false).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.RewardIssues.Where(c => c.IsDeleted == false).Count();
            }
            else
            {
                rewardIssues = db.RewardIssues.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Employee.EnName.Contains(searchWord))).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.RewardIssues.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Employee.EnName.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(await rewardIssues.ToListAsync());
        }

        // GET: RewardIssue/Edit/5
        public async Task<ActionResult> AddEdit(int? id)
        {
            ViewBag.rewardTypeId = new SelectList(db.RewardTypes.Where(x => x.IsActive && !x.IsDeleted).Select(x => new {
                x.Id,
                ArName = x.ArName + (x.CalculationMethodId == 1 ? " / ساعة" : x.CalculationMethodId == 2 ? " / يوم" : x.CalculationMethodId == 3 ? " / قيمة" : " ")
            }), "Id", "ArName");
            if (id == null)
            {
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(x => x.IsActive && !x.IsDeleted).Select(x => new { x.Id, x.ArName }), "Id", "ArName");
                ViewBag.Year = new SelectList(new int[] { 2019, 2020, 2021, 2022, 2023, 2024, 2025, 2026, 2027 });
                ViewBag.Month = new SelectList(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });
                var DocumentNumber = await db.RewardIssues.OrderByDescending(x => x.Id).Select(x => x.DocumentNumber).FirstOrDefaultAsync();
                ViewBag.DocumentNumber = string.IsNullOrEmpty(DocumentNumber) ? 1 : int.Parse(DocumentNumber) + 1;
                
                return View();
            }
            RewardIssue rewardIssue = await db.RewardIssues.FindAsync(id);
            if (rewardIssue == null)
            {
                return HttpNotFound();
            }
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(x => x.IsActive && !x.IsDeleted).Select(x => new { x.Id, x.ArName }), "Id", "ArName", rewardIssue.EmployeeId);
            ViewBag.Year = new SelectList(new int[] { 2019, 2020, 2021, 2022, 2023, 2024, 2025, 2026, 2027 }, rewardIssue.Year);
            ViewBag.Month = new SelectList(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, rewardIssue.Month);
            ViewBag.DocumentNumber = rewardIssue.DocumentNumber;
           
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل اصدار المكافئات",
                EnAction = "AddEdit",
                ControllerName = "RewardIssue",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = rewardIssue.Id,
                CodeOrDocNo = rewardIssue.DocumentNumber
            });
            return View(rewardIssue);
        }

        [HttpPost]
        public async Task<ActionResult> AddEdit([Bind(Include = "Id,DocumentNumber,Month,Year,EmployeeId,IsDeleted,RewardIssueDetials")] RewardIssue rewardIssue)
        {
            if (ModelState.IsValid)
            {
                var id = rewardIssue.Id;
                rewardIssue.IsDeleted = false;
                if (rewardIssue.Id > 0)
                {
                    db.RewardIssueDetials.RemoveRange(db.RewardIssueDetials.Where(x => x.MainDocId == rewardIssue.Id));
                    var rewardIssueDetials = rewardIssue.RewardIssueDetials.ToList();
                    rewardIssueDetials.ForEach((x) => x.MainDocId = rewardIssue.Id);
                    rewardIssue.RewardIssueDetials = null;
                    db.Entry(rewardIssue).State = EntityState.Modified;
                    db.RewardIssueDetials.AddRange(rewardIssueDetials);
                }
                else
                {
                    db.RewardIssues.Add(rewardIssue);
                }
                await db.SaveChangesAsync();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل اصدار المكافئات" : "اضافة اصدار المكافئات",
                    EnAction = "AddEdit",
                    ControllerName = "RewardIssue",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = rewardIssue.Id,
                    CodeOrDocNo = rewardIssue.DocumentNumber
                });
                Notification.GetNotification("RewardIssue", id > 0 ? "Edit" : "Add", "AddEdit", rewardIssue.Id, null, "اصدار المكافئات");
                return Json(new { success = true });
            }
            var errors = ModelState
                   .Where(x => x.Value.Errors.Count > 0)
                   .Select(x => new { x.Key, x.Value.Errors })
                   .ToArray();
            return Json(new { success = false, errors });
        }

        // POST: RewardIssue/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            RewardIssue rewardIssue = new RewardIssue() { Id = id, DocumentNumber = "" };
            db.RewardIssues.Attach(rewardIssue);
            rewardIssue.IsDeleted = true;
            db.Entry(rewardIssue).Property(x => x.IsDeleted).IsModified = true;

            await db.SaveChangesAsync();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف اصدار المكافئات",
                EnAction = "AddEdit",
                ControllerName = "RewardIssue",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                CodeOrDocNo = rewardIssue.DocumentNumber
            });
            Notification.GetNotification("RewardIssue", "Delete", "Delete", id, null, "اصدار المكافئات");
            return Content("true");
        }

        [SkipERPAuthorize]
        public async Task<JsonResult> PreviousRewardMonths(int employeeId, int year)
        {
            return Json(await db.RewardIssues.Where(x => x.EmployeeId == employeeId && !x.IsDeleted && x.Year == year).Select(x => x.Month).ToArrayAsync(), JsonRequestBehavior.AllowGet);
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
            var _RewardType = db.RewardTypes.Where(a => a.IsActive && !a.IsDeleted && a.Id == id).FirstOrDefault();
            var SalaryItemId = db.RewardTypeDetails.Where(a => a.IsDeleted == false && a.MainDocId == id).Select(a => a.SalaryItemId).ToList();
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
            if (_RewardType.CalculationMethodId == 1)
            {
                hourlySalary = monthlySalary / 30 / 8;
            }
            else if (_RewardType.CalculationMethodId == 2)
            {
                hourlySalary = monthlySalary / 30;
            }
            // بنجيب اول واحد فى الجدول "باشمهندس اللى قال"
            var Equivalent = _RewardType.RewardTypeDetails.Where(a => a.SalaryItemId == SalaryItemId[0]).FirstOrDefault() != null ? _RewardType.RewardTypeDetails.Where(a => a.SalaryItemId == SalaryItemId[0]).FirstOrDefault().Equivalent : 1;

            return Json(new { monthlySalary, hourlySalary = /*hourlySalary*/ hourlySalary * (decimal?)Equivalent, CalculationMethodId=_RewardType.CalculationMethodId }, JsonRequestBehavior.AllowGet);
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
