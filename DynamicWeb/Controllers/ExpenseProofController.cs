using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using MyERP.Models;
using MyERP.Repository;

namespace MyERP.Controllers
{
    public class ExpenseProofController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: ExpenseProof
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة  إثبات المصروفات",
                EnAction = "Index",
                ControllerName = "ExpenseProof",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("ExpenseProof", "View", "Index", null, null, " إثبات المصروفات");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<ExpenseProof> expenseProofs;

            if (string.IsNullOrEmpty(searchWord))
            {
                expenseProofs = db.ExpenseProofs.Where(s => s.IsDeleted == false).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ExpenseProofs.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                expenseProofs = db.ExpenseProofs.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ExpenseProofs.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord))).Count();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة  إثبات المصروفات",
                EnAction = "Index",
                ControllerName = "ExpenseProof",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(expenseProofs.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            //------ Time Zone Depends On Currency --------//
            var Currency = db.Currencies.Where(a => a.IsActive == true && a.IsDeleted == false && a.IsDefault == true).FirstOrDefault();
            var CurrencyCode = Currency != null ? Currency.Code : "";
            TimeZoneInfo info;
            if (CurrencyCode == "SAR")
            {
                //info = TimeZoneInfo.FindSystemTimeZoneById("Arabian Standard Time");//+2H from Egypt Standard Time
                info = TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time");// +1H from Egypt Standard Time
            }
            else
            {
                info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            }
            DateTime utcNow = DateTime.UtcNow;
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
            //----------------- End of Time Zone Depends On Currency --------------------//
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
           
            if (id == null)
            {
                ViewBag.VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);
                return View();
            }
            ExpenseProof expenseProof = db.ExpenseProofs.Find(id);

            if (expenseProof == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل  إثبات المصروفات ",
                EnAction = "AddEdit",
                ControllerName = "ExpenseProof",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "ExpenseProof");
            ViewBag.Previous = QueryHelper.Previous((int)id, "ExpenseProof");
            ViewBag.Last = QueryHelper.GetLast("ExpenseProof");
            ViewBag.First = QueryHelper.GetFirst("ExpenseProof");

            ViewBag.VoucherDate = expenseProof.VoucherDate != null ? expenseProof.VoucherDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId).ToList(), "Id", "ArName", expenseProof.DepartmentId);


            ////-------------------- journal Entry --------------------//
            //int sysPageId = QueryHelper.SourcePageId("ExpenseProof");
            //JournalEntry journal = db.JournalEntries.FirstOrDefault(j => j.SourceId == id && j.SourcePageId == sysPageId);
            //ViewBag.Journal = journal;
            //ViewBag.JournalDocumentNumber = journal != null && journal.DocumentNumber != null ? journal.DocumentNumber.Replace("-", "") : "";
            ////----------------------------------------------------------------------------//

            return View(expenseProof);
        }

        [HttpPost]
        public ActionResult AddEdit(ExpenseProof expenseProof)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var Details = expenseProof.PrepaidExpenseDetails;
            if (ModelState.IsValid)
            {
                var id = expenseProof.Id;
                expenseProof.IsDeleted = false;
                expenseProof.UserId = userId;
                if (expenseProof.Id > 0)
                {
                    expenseProof.PrepaidExpenseDetails = null;
                    db.Entry(expenseProof).State = EntityState.Modified;
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("ExpenseProof", "Edit", "AddEdit", expenseProof.Id, null, " إثبات المصروفات");
                }
                else
                {
                    expenseProof.DocumentNumber = new JavaScriptSerializer().Serialize(SetDocNum().Data).ToString().Trim('"');
                    expenseProof.PrepaidExpenseDetails = null;

                    db.ExpenseProofs.Add(expenseProof);
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("ExpenseProof", "Add", "AddEdit", id, null, " إثبات المصروفات");
                }
                try
                {
                    db.SaveChanges();
                    foreach (var item in Details)
                    {
                        var IsSelectedProof = item.IsSelectedProof == true ? 1 : 0;
                        db.Database.ExecuteSqlCommand($"update PrepaidExpenseDetail set ExpenseProofId={expenseProof.Id},IsSelectedProof={IsSelectedProof} where Id= {item.Id}");
                    }
                }
                catch (Exception ex)
                {
                    var dbErrors = ex.InnerException.InnerException.Message;
                    return Json(new { success = false });
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = expenseProof.Id > 0 ? "تعديل  إثبات المصروفات" : "اضافة  إثبات المصروفات",
                    EnAction = "AddEdit",
                    ControllerName = "ExpenseProof",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = id,
                });
                //try
                //{
                //    foreach (var item in Details)
                //    {
                //        var IsSelectedProof = item.IsSelectedProof == true ? 1 : 0;
                //        db.Database.ExecuteSqlCommand($"update PrepaidExpenseDetail set ExpenseProofId={expenseProof.Id},IsSelectedProof={IsSelectedProof} where Id= {item.Id}");
                //    }
                //}
                //catch (Exception ex)
                //{
                //    var dbErrors = ex.StackTrace;
                //    return Json(new { success = false });
                //}
                return Json(new { success = true });
            }
            var errors = ModelState
                   .Where(x => x.Value.Errors.Count > 0)
                   .Select(x => new { x.Key, x.Value.Errors })
                   .ToArray();
            return Json(new { success = false });
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                ExpenseProof expenseProof = db.ExpenseProofs.Find(id);
                expenseProof.IsDeleted = true;
                expenseProof.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(expenseProof).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف  إثبات المصروفات",
                    EnAction = "AddEdit",
                    ControllerName = "ExpenseProof",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                });
                Notification.GetNotification("ExpenseProof", "Delete", "Delete", id, null, " إثبات المصروفات");
                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

        }

        [SkipERPAuthorize]
        public JsonResult SetDocNum()
        {
            double DocNo = 0;
            var code = db.Database.SqlQuery<string>($"select top(1)[DocumentNumber] from [ExpenseProof] order by [Id] desc");
            if (code.FirstOrDefault() == null)
            {
                DocNo = 0;
            }
            else
            {
                DocNo = double.Parse(code.FirstOrDefault().ToString());
            }
            return Json(DocNo + 1, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetUnSelectedExpenseProof(int? DepartmentId)
        {
            var expensesProof = db.GetUnSelectedExpenseProof(DepartmentId).ToList();
            return Json(expensesProof, JsonRequestBehavior.AllowGet);
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