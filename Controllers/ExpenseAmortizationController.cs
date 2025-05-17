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
    public class ExpenseAmortizationController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: ExpenseAmortization
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة إطفاء المصروفات",
                EnAction = "Index",
                ControllerName = "ExpenseAmortization",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("ExpenseAmortization", "View", "Index", null, null, "إطفاء المصروفات");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<ExpenseAmortization> expenseAmortizations;

            if (string.IsNullOrEmpty(searchWord))
            {
                expenseAmortizations = db.ExpenseAmortizations.Where(s => s.IsDeleted == false).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ExpenseAmortizations.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                expenseAmortizations = db.ExpenseAmortizations.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ExpenseAmortizations.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord))).Count();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة إطفاء المصروفات",
                EnAction = "Index",
                ControllerName = "ExpenseAmortization",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(expenseAmortizations.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            List<int> year = new List<int>();
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
                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.StartDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.EndDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                //Month
                ViewBag.Month = new SelectList(new List<dynamic> {
                new { id=1,name="يناير"},
                new { id=2,name="فبراير"},
                new { id=3,name="مارس"},
                new { id=4,name="إبريل"},
                new { id=5,name="مايو"},
                new { id=6,name="يونيه"},
                new { id=7,name="يوليو"},
                new { id=8,name="اغسطس"},
                new { id=9,name="سبتمبر"},
                new { id=10,name="اكتوبر"},
                new { id=11,name="نوفمبر"},
                new { id=12,name="ديسمبر"}}, "id", "name");

                //year
                for (var i = 2019; i <= 2030; i++)
                {
                    year.Add(i);
                    ViewBag.Year = new SelectList(year);
                }
                return View();
            }
            ExpenseAmortization expenseAmortization = db.ExpenseAmortizations.Find(id);

            if (expenseAmortization == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل إطفاء المصروفات ",
                EnAction = "AddEdit",
                ControllerName = "ExpenseAmortization",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "ExpenseAmortization");
            ViewBag.Previous = QueryHelper.Previous((int)id, "ExpenseAmortization");
            ViewBag.Last = QueryHelper.GetLast("ExpenseAmortization");
            ViewBag.First = QueryHelper.GetFirst("ExpenseAmortization");

            ViewBag.VoucherDate = expenseAmortization.VoucherDate != null ? expenseAmortization.VoucherDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;
            ViewBag.StartDate = expenseAmortization.StartDate != null ? expenseAmortization.StartDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;
            ViewBag.EndDate = expenseAmortization.EndDate != null ? expenseAmortization.EndDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId).ToList(), "Id", "ArName", expenseAmortization.DepartmentId);

            //Month
            ViewBag.Month = new SelectList(new List<dynamic> {
                 new { id=1,name="يناير"},
                new { id=2,name="فبراير"},
                new { id=3,name="مارس"},
                new { id=4,name="إبريل"},
                new { id=5,name="مايو"},
                new { id=6,name="يونيه"},
                new { id=7,name="يوليو"},
                new { id=8,name="اغسطس"},
                new { id=9,name="سبتمبر"},
                new { id=10,name="اكتوبر"},
                new { id=11,name="نوفمبر"},
                new { id=12,name="ديسمبر"}}, "id", "name", expenseAmortization.Month);

            //year
            for (var i = 2019; i <= 2030; i++)
            {
                year.Add(i);
                ViewBag.Year = new SelectList(year, expenseAmortization.Year);
            }

            //-------------------- journal Entry --------------------//
            int sysPageId = QueryHelper.SourcePageId("ExpenseAmortization");
            JournalEntry journal = db.JournalEntries.FirstOrDefault(j => j.SourceId == id && j.SourcePageId == sysPageId && j.IsDeleted == false);
            ViewBag.Journal = journal;
            ViewBag.JournalDocumentNumber = journal != null && journal.DocumentNumber != null ? journal.DocumentNumber.Replace("-", "") : "";
            //----------------------------------------------------------------------------//

            return View(expenseAmortization);
        }

        [HttpPost]
        public ActionResult AddEdit(ExpenseAmortization expenseAmortization)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var Details = expenseAmortization.PrepaidExpenseDetails;
            var distributions = expenseAmortization.ExpenseAmortizationDistributions;

            if (ModelState.IsValid)
            {
                var id = expenseAmortization.Id;
                expenseAmortization.IsDeleted = false;
                expenseAmortization.UserId = userId;
                expenseAmortization.ExpenseAmortizationDistributions = null;

                if (expenseAmortization.Id > 0)
                {
                    expenseAmortization.PrepaidExpenseDetails = null;
                    db.Entry(expenseAmortization).State = EntityState.Modified;
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("ExpenseAmortization", "Edit", "AddEdit", expenseAmortization.Id, null, "إطفاء المصروفات");
                }
                else
                {
                    expenseAmortization.DocumentNumber = new JavaScriptSerializer().Serialize(SetDocNum().Data).ToString().Trim('"');
                    expenseAmortization.PrepaidExpenseDetails = null;

                    db.ExpenseAmortizations.Add(expenseAmortization);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("ExpenseAmortization", "Add", "AddEdit", id, null, "إطفاء المصروفات");
                }
                try
                {
                    db.SaveChanges();
                    if (Details.Count == 0)
                    {
                        db.Database.ExecuteSqlCommand($"update PrepaidExpenseDetail set ExpenseAmortizationId=NULL,IsSelectedAmortization=NULL where ExpenseAmortizationId= {expenseAmortization.Id}");
                    }
                    if (distributions != null)
                    {
                        var distributionList = new List<ExpenseAmortizationDistribution>();
                        var PrepaidExpenseDetailId = distributions.FirstOrDefault().PrepaidExpenseDetailId;
                        var expenseAmortizationDistributions = db.ExpenseAmortizationDistributions.Where(a => a.PrepaidExpenseDetailId == PrepaidExpenseDetailId).ToList();
                        if (expenseAmortizationDistributions != null)
                        {
                            foreach (var item in distributions)
                            {
                                var oldDistribution = db.ExpenseAmortizationDistributions.Where(a => a.PrepaidExpenseDetailId == item.PrepaidExpenseDetailId && a.Month == item.Month).FirstOrDefault();
                                if (oldDistribution != null)
                                {
                                    if (item.IsSelectedAmortization == true)
                                    {
                                        db.Database.ExecuteSqlCommand($"update ExpenseAmortizationDistribution set ExpenseAmortizationId={expenseAmortization.Id},IsSelectedAmortization=1 where Id= {oldDistribution.Id}");
                                    }
                                    else
                                    {
                                        db.Database.ExecuteSqlCommand($"update ExpenseAmortizationDistribution set ExpenseAmortizationId=NULL where Id= {oldDistribution.Id}");
                                    }
                                }
                                else
                                {
                                    var Dist = new ExpenseAmortizationDistribution();
                                    Dist.ExpenseAmortizationId = expenseAmortization.Id;
                                    Dist.IsDeleted = false;
                                    Dist.IsSelectedAmortization = item.IsSelectedAmortization;
                                    Dist.Month = item.Month;
                                    Dist.PrepaidExpenseDetailId = item.PrepaidExpenseDetailId;
                                    Dist.Value = item.Value;
                                    distributionList.Add(Dist);
                                }

                            }
                            db.ExpenseAmortizationDistributions.AddRange(distributionList);
                            db.SaveChanges();
                        }
                    }
                    foreach (var item in Details)
                    {
                        var IsSelectedAmortization = item.IsSelectedAmortization == true ? 1 : 0;
                        db.Database.ExecuteSqlCommand($"update PrepaidExpenseDetail set ExpenseAmortizationId={expenseAmortization.Id},IsSelectedAmortization={IsSelectedAmortization} where Id= {item.Id}");
                        if (id > 0)
                        {
                            db.ExpenseAmortizationJE_Update(id, item.Id);
                        }
                        else
                        {
                            db.ExpenseAmortizationJE_Insert(expenseAmortization.Id, item.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var dbErrors = ex.InnerException.InnerException.Message;
                    return Json(new { success = false });
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = expenseAmortization.Id > 0 ? "تعديل إطفاء المصروفات" : "اضافة إطفاء المصروفات",
                    EnAction = "AddEdit",
                    ControllerName = "ExpenseAmortization",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = id,
                });
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

                ExpenseAmortization expenseAmortization = db.ExpenseAmortizations.Find(id);
                expenseAmortization.IsDeleted = true;
                expenseAmortization.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                foreach (var item in expenseAmortization.PrepaidExpenseDetails)
                {
                    item.IsSelectedAmortization = false;
                    item.ExpenseAmortizationId = null;
                }
                foreach (var item in expenseAmortization.ExpenseAmortizationDistributions)
                {
                    item.IsDeleted = true;
                    item.IsSelectedAmortization = false;
                    item.ExpenseAmortizationId = null;
                }
                var systempageid = db.SystemPages.Where(a => a.Code == "ExpenseAmortization" && a.IsActive == true && a.IsDeleted == false).FirstOrDefault().Id;
                JournalEntry journalEntry = db.JournalEntries.Where(a => a.IsActive == true && a.IsDeleted == false && a.SourcePageId == systempageid && a.SourceId == id).FirstOrDefault();
                journalEntry.IsDeleted = true;
                foreach (var detail in journalEntry.JournalEntryDetails)
                {
                    detail.IsDeleted = true;
                }
                db.Entry(journalEntry).State = EntityState.Modified;
                db.Entry(expenseAmortization).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف إطفاء المصروفات",
                    EnAction = "AddEdit",
                    ControllerName = "ExpenseAmortization",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                });
                Notification.GetNotification("ExpenseAmortization", "Delete", "Delete", id, null, "إطفاء المصروفات");
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
            var code = db.Database.SqlQuery<string>($"select top(1)[DocumentNumber] from [ExpenseAmortization] order by [Id] desc");
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
        public JsonResult GetUnSelectedExpenseAmortization(int? Month, int? Year, int? DepartmentId)
        {
            var expensesProof = db.GetUnSelectedExpenseAmortization(Month, Year, DepartmentId).ToList();
            return Json(expensesProof, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetDistributionDetails(int? PrepaidExpenseDetailId)
        {
            var Distribution = db.ExpenseAmortizationDistributions.Where(a => a.PrepaidExpenseDetailId == PrepaidExpenseDetailId && a.IsDeleted == false && a.IsSelectedAmortization != true).Select(a => new
            {
                a.ExpenseAmortizationId,
                a.IsSelectedAmortization,
                a.Month,
                a.PrepaidExpenseDetailId,
                a.Value
            }).ToList();
            return Json(Distribution, JsonRequestBehavior.AllowGet);
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