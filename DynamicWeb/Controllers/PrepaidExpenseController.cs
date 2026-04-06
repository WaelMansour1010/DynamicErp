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
    public class PrepaidExpenseController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: PrepaidExpense
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة تعريف المصروفات المقدمة",
                EnAction = "Index",
                ControllerName = "PrepaidExpense",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("PrepaidExpense", "View", "Index", null, null, "تعريف المصروفات المقدمة");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<PrepaidExpense> prepaidExpenses;

            if (string.IsNullOrEmpty(searchWord))
            {
                prepaidExpenses = db.PrepaidExpenses.Where(s => s.IsDeleted == false).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PrepaidExpenses.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                prepaidExpenses = db.PrepaidExpenses.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PrepaidExpenses.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord))).Count();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة تعريف المصروفات المقدمة",
                EnAction = "Index",
                ControllerName = "PrepaidExpense",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(prepaidExpenses.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            var subAccounts = db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && (c.ClassificationId == 3)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }).ToList();
            ViewBag.ExpenseAccountId = new SelectList(subAccounts, "Id", "ArName");
            ViewBag.PrePaymentAccountId = new SelectList(subAccounts, "Id", "ArName");
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.ProjectId = new SelectList(db.Projects.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.CostCenterId = new SelectList(db.CostCenters.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.ExpenseTypeId = new SelectList(new List<dynamic> {
                new { Id=1,ArName="موظف"},
                new { Id=1,ArName="حساب"}}, "Id", "ArName");
            ViewBag.DistributionTypeId = new SelectList(new List<dynamic> {
                new { Id=1,ArName="آلي"},
                new { Id=1,ArName="يدوي"}}, "Id", "ArName");

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
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);
            ViewBag.FromDate = cTime.ToString("yyyy-MM-dd");
            ViewBag.ToDate = cTime.ToString("yyyy-MM-dd");
            ViewBag.ProofDate = cTime.ToString("yyyy-MM-dd");
            if (id == null)
            {
               // ViewBag.ExpenseAmortizationDistributions = null;
                ViewBag.VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            PrepaidExpense prepaidExpense = db.PrepaidExpenses.Find(id);

            if (prepaidExpense == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل تعريف المصروفات المقدمة ",
                EnAction = "AddEdit",
                ControllerName = "PrepaidExpense",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "PrepaidExpense");
            ViewBag.Previous = QueryHelper.Previous((int)id, "PrepaidExpense");
            ViewBag.Last = QueryHelper.GetLast("PrepaidExpense");
            ViewBag.First = QueryHelper.GetFirst("PrepaidExpense");

            ViewBag.VoucherDate = prepaidExpense.VoucherDate != null ? prepaidExpense.VoucherDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;
            //var Distributions = prepaidExpense.PrepaidExpenseDetails.Where(s=>s.ExpenseAmortizationDistributions.Select(a=>a.PrepaidExpenseDetailId).Any()==true).ToList();             
            //ViewBag.ExpenseAmortizationDistributions = Distributions;
            ////-------------------- journal Entry --------------------//
            //int sysPageId = QueryHelper.SourcePageId("PrepaidExpense");
            //JournalEntry journal = db.JournalEntries.FirstOrDefault(j => j.SourceId == id && j.SourcePageId == sysPageId);
            //ViewBag.Journal = journal;
            //ViewBag.JournalDocumentNumber = journal != null && journal.DocumentNumber != null ? journal.DocumentNumber.Replace("-", "") : "";
            ////----------------------------------------------------------------------------//

            return View(prepaidExpense);
        }

        [HttpPost]
        public ActionResult AddEdit(PrepaidExpense prepaidExpense)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (ModelState.IsValid)
            {
                var id = prepaidExpense.Id;
                prepaidExpense.IsDeleted = false;
                prepaidExpense.UserId = userId;
                if (prepaidExpense.Id > 0)
                {
                    var Olds = new List<PrepaidExpenseDetail>();
                    //var News = new List<PrepaidExpenseDetail>();
                    foreach (var item in prepaidExpense.PrepaidExpenseDetails)
                    {
                        var old = db.PrepaidExpenseDetails.Find(item.Id);
                        if(old!=null)
                        {
                            old.ArName = item.ArName;
                            old.CostCenterId = item.CostCenterId;
                            old.DepartmentId = item.DepartmentId;
                            old.DistributionTypeId = item.DistributionTypeId;
                            old.EmployeeId = item.EmployeeId;
                            old.EnName = item.EnName;
                            old.ExpenseAccountId = item.ExpenseAccountId;
                            old.ExpenseAmortizationId = item.ExpenseAmortizationId;
                            old.ExpenseProofId = item.ExpenseProofId;
                            old.IsDeleted = false;
                            old.IsActive = true;
                            old.UserId = prepaidExpense.UserId;
                            old.Image = item.Image;
                            old.ExpenseTypeId = item.ExpenseTypeId;
                            old.FromDate = item.FromDate;
                            old.IsSelectedAmortization = item.IsSelectedAmortization;
                            old.IsSelectedProof = item.IsSelectedProof;
                            old.Notes = item.Notes;
                            old.PrePaymentAccountId = item.PrePaymentAccountId;
                            old.ProjectId = item.ProjectId;
                            old.ProofDate = item.ProofDate;
                            old.ToDate = item.ToDate;
                            old.Value = item.Value;
                            old.ExpenseAmortizationDistributions = item.ExpenseAmortizationDistributions;
                            Olds.Add(old);
                        }
                        else
                        {
                            var _new = new PrepaidExpenseDetail();
                            _new.MainDocId = prepaidExpense.Id;
                            _new.ArName = item.ArName;
                            _new.CostCenterId = item.CostCenterId;
                            _new.DepartmentId = item.DepartmentId;
                            _new.DistributionTypeId = item.DistributionTypeId;
                            _new.EmployeeId = item.EmployeeId;
                            _new.EnName = item.EnName;
                            _new.ExpenseAccountId = item.ExpenseAccountId;
                            _new.ExpenseAmortizationId = item.ExpenseAmortizationId;
                            _new.ExpenseProofId = item.ExpenseProofId;
                            _new.IsDeleted = false;
                            _new.IsActive = true;
                            _new.UserId = prepaidExpense.UserId;
                            _new.Image = item.Image;
                            _new.ExpenseTypeId = item.ExpenseTypeId;
                            _new.FromDate = item.FromDate;
                            _new.IsSelectedAmortization = item.IsSelectedAmortization;
                            _new.IsSelectedProof = item.IsSelectedProof;
                            _new.Notes = item.Notes;
                            _new.PrePaymentAccountId = item.PrePaymentAccountId;
                            _new.ProjectId = item.ProjectId;
                            _new.ProofDate = item.ProofDate;
                            _new.ToDate = item.ToDate;
                            _new.Value = item.Value;
                            _new.ExpenseAmortizationDistributions = item.ExpenseAmortizationDistributions;
                            db.PrepaidExpenseDetails.Add(_new);
                        }
                       
                    }
                    prepaidExpense.PrepaidExpenseDetails = Olds;

                    //db.PrepaidExpenseDetails.RemoveRange(db.PrepaidExpenseDetails.Where(x => x.MainDocId == prepaidExpense.Id));
                    //var prepaidExpenseDetails = prepaidExpense.PrepaidExpenseDetails.ToList();
                    //prepaidExpenseDetails.ForEach((x) => x.MainDocId = prepaidExpense.Id);
                    //prepaidExpense.PrepaidExpenseDetails = null;
                    db.Entry(prepaidExpense).State = EntityState.Modified;
                    // db.PrepaidExpenseDetails.AddRange(prepaidExpenseDetails);
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("PrepaidExpense", "Edit", "AddEdit", prepaidExpense.Id, null, "تعريف المصروفات المقدمة");
                }
                else
                {
                    prepaidExpense.DocumentNumber = new JavaScriptSerializer().Serialize(SetDocNum().Data).ToString().Trim('"');

                    db.PrepaidExpenses.Add(prepaidExpense);
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("PrepaidExpense", "Add", "AddEdit", id, null, "تعريف المصروفات المقدمة");
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {

                    var dbErrors = ex.InnerException.InnerException.Message;
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = prepaidExpense.Id > 0 ? "تعديل تعريف المصروفات المقدمة" : "اضافة تعريف المصروفات المقدمة",
                    EnAction = "AddEdit",
                    ControllerName = "PrepaidExpense",
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
                PrepaidExpense prepaidExpense = db.PrepaidExpenses.Find(id);
                prepaidExpense.IsDeleted = true;
                prepaidExpense.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                foreach (var item in prepaidExpense.PrepaidExpenseDetails)
                {
                    item.IsDeleted = true;
                    item.IsSelectedAmortization = false;
                    item.IsSelectedProof = false;
                }
                db.Entry(prepaidExpense).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف تعريف المصروفات المقدمة",
                    EnAction = "AddEdit",
                    ControllerName = "PrepaidExpense",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                });
                Notification.GetNotification("PrepaidExpense", "Delete", "Delete", id, null, "تعريف المصروفات المقدمة");
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
            var code = db.Database.SqlQuery<string>($"select top(1)[DocumentNumber] from [PrepaidExpense] order by [Id] desc");
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