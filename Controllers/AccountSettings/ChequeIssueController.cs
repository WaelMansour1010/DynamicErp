using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using Newtonsoft.Json;
using System.Security.Claims;
using MyERP.Repository;
using System.Threading.Tasks;
using System.Data.Entity.Core.Objects;

namespace MyERP.Controllers.AccountSettings
{
    public class ChequeIssueController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: ChequeIssue
        public async Task<ActionResult> Index(bool? report, int? id, int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            ViewBag.PageIndex = pageIndex;
            ViewBag.OpenReport = report == true;
            if (report == true)
            {
                ViewBag.Id = id;
                ViewBag.Count = 0;
                return View();
            }
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var depIds = await departmentRepository.UserDepartmentsIds(userId);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة اصدار أوراق الدفع",
                EnAction = "Index",
                ControllerName = "ChequeIssue",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            ////-------------------- Notification-------------------------////
            Notification.GetNotification("ChequeIssue", "View", "Index", null, null, "اصدار أوراق الدفع");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<ChequeIssue> chequeIssues;
            if (string.IsNullOrEmpty(searchWord))
            {
                chequeIssues = db.ChequeIssues.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ChequeIssues.Where(s => s.IsDeleted == false && s.IsActive == true && depIds.Contains(s.DepartmentId)).Count();

            }
            else
            {
                chequeIssues = db.ChequeIssues.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.BankBranch.Contains(searchWord) || s.Notes.Contains(searchWord) || s.BankAccount.AccountNumber.Contains(searchWord) || s.Customer.ArName.Contains(searchWord) || s.Vendor.ArName.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.ChequeStatu.ArName.Contains(searchWord) || s.ChequeSourceType.ArName.Contains(searchWord) || s.ChequeNumber.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);

                ViewBag.Count = db.ChequeIssues.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.BankBranch.Contains(searchWord) || s.Notes.Contains(searchWord) || s.BankAccount.AccountNumber.Contains(searchWord) || s.Customer.ArName.Contains(searchWord) || s.Vendor.ArName.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.ChequeStatu.ArName.Contains(searchWord) || s.ChequeSourceType.ArName.Contains(searchWord) || s.ChequeNumber.Contains(searchWord))).Count();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(await chequeIssues.ToListAsync());
        }

        // GET: ChequeIssue/Edit/5
        public async Task<ActionResult> AddEdit(int? id)
        {
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            ViewBag.UseCostCenter = systemSetting.UseCostCenter;
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            if (id == null)
            {
                ViewBag.BankId = new SelectList(db.Banks.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", systemSetting.DefaultBankId);
                ViewBag.BankAccountId = new SelectList(db.BankAccounts.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "AccountNumber");
                ViewBag.BranchId = new SelectList(db.Branches.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.IssueTypeId = new SelectList(db.ChequeSourceTypes.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName");
                ViewBag.ChequeStatusId = new SelectList(db.ChequeStatus.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName");
                ViewBag.CurrencyId = new SelectList(db.Currencies.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.CustomerId = new SelectList(db.Customers.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.VendorId = new SelectList(db.Vendors.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.DirectExpensesId = new SelectList(db.DirectExpenses.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.ShareholderId = new SelectList(db.ShareHolders.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.DepartmentId = new SelectList(await departmentRepository.UserDepartments(userId).ToListAsync(), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.CostCenterId = new SelectList(db.CostCenters.Where(a => a.IsActive && !a.IsDeleted && a.TypeId == 2).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
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

                ViewBag.Date = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.DueDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            ChequeIssue chequeIssue =await db.ChequeIssues.FindAsync(id);
            if (chequeIssue == null)
            {
                return HttpNotFound();
            }
            int sysPageId = QueryHelper.SourcePageId("ChequeIssue");

            ViewBag.JE = db.GetJournalEntryBySourceIdAndSourcePageId(id, sysPageId).FirstOrDefault();
            if (ViewBag.JE != null)
            {
                var JEDocNo = ViewBag.JE;
                ViewBag.JE.DocumentNumber = JEDocNo != null ? JEDocNo.DocumentNumber.Replace("-", "") : "";
                int JEId = ViewBag.JE.Id;
                int sourcePageId = ViewBag.JE.SourcePageId;
                ViewBag.JEDetails = db.JournalEntryDetails.Where(a => a.SourceId == id && a.JournalEntryId == JEId && a.SourcePageId == sourcePageId).OrderBy(a => a.Id).ToList();

            }
            ViewBag.Accounts = new SelectList(db.ChartOfAccounts.Where(a => a.ClassificationId == 3 && a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.BankAccountId = new SelectList(db.BankAccounts.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "AccountNumber", chequeIssue.BankAccountId);
            ViewBag.BranchId = new SelectList(db.Branches.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeIssue.BranchId);
            ViewBag.IssueTypeId = new SelectList(db.ChequeSourceTypes.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", chequeIssue.IssueTypeId);
            ViewBag.ChequeStatusId = new SelectList(db.ChequeStatus.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", chequeIssue.ChequeStatusId);
            ViewBag.CurrencyId = new SelectList(db.Currencies.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeIssue.CurrencyId);
            ViewBag.CustomerId = new SelectList(db.Customers.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeIssue.CustomerId);
            ViewBag.ShareholderId = new SelectList(db.ShareHolders.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeIssue.ShareholderId);
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeIssue.EmployeeId);
            ViewBag.VendorId = new SelectList(db.Vendors.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeIssue.VendorId);
            ViewBag.DirectExpensesId = new SelectList(db.DirectExpenses.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeIssue.DirectExpensesId);
            ViewBag.DepartmentId = new SelectList(await departmentRepository.UserDepartments(userId).ToListAsync(), "Id", "ArName", chequeIssue.DepartmentId);
            ViewBag.BankId = new SelectList(db.Banks.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeIssue.BankId);
            ViewBag.CostCenterId = new SelectList(db.CostCenters.Where(a => a.IsActive && !a.IsDeleted && a.TypeId == 2).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName",chequeIssue.CostCenterId);
            ViewBag.Next = QueryHelper.Next((int)id, "ChequeIssue");
            ViewBag.Previous = QueryHelper.Previous((int)id, "ChequeIssue");
            ViewBag.Last = QueryHelper.GetLast("ChequeIssue");
            ViewBag.First = QueryHelper.GetFirst("ChequeIssue");
            try
            {
                ViewBag.Date = chequeIssue.Date.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.DueDate = chequeIssue.DueDate.Value.ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception)
            {
            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل اصدار أوراق الدفع",
                EnAction = "AddEdit",
                ControllerName = "ChequeIssue",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = chequeIssue.Id,
                CodeOrDocNo = chequeIssue.DocumentNumber
            });
            return View(chequeIssue);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddEdit(ChequeIssue chequeIssue, string newBtn)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (ModelState.IsValid)
            {
                chequeIssue.IsDeleted = false;
                chequeIssue.UserId = userId;

                var id = chequeIssue.Id;
                if (chequeIssue.IssueTypeId == 6)
                {
                    chequeIssue.VendorId = null;
                    chequeIssue.CustomerId = null;
                    chequeIssue.EmployeeId = null;
                    chequeIssue.ShareholderId = null;
                }
                if (chequeIssue.IssueTypeId == 5)
                {
                    chequeIssue.VendorId = null;
                    chequeIssue.CustomerId = null;
                    chequeIssue.EmployeeId = null;
                    chequeIssue.DirectExpensesId = null;
                }
                if (chequeIssue.IssueTypeId == 3)
                {
                    chequeIssue.VendorId = null;
                    chequeIssue.CustomerId = null;
                    chequeIssue.ShareholderId = null;
                    chequeIssue.DirectExpensesId = null;

                }
                else if (chequeIssue.IssueTypeId == 2)
                {
                    chequeIssue.DirectExpensesId = null;

                    chequeIssue.CustomerId = null;
                    chequeIssue.EmployeeId = null;
                    chequeIssue.ShareholderId = null;
                }
                else if (chequeIssue.IssueTypeId == 1)
                {
                    chequeIssue.DirectExpensesId = null;

                    chequeIssue.VendorId = null;
                    chequeIssue.ShareholderId = null;
                    chequeIssue.EmployeeId = null;
                }

                if (chequeIssue.Id > 0)
                {
                    if (db.ChequeIssues.Find(chequeIssue.Id).IsPosted == true)
                    {
                        return Content("false");
                    }
                    
                    db.ChequeIssue_Update(chequeIssue.Id, chequeIssue.DocumentNumber, chequeIssue.BranchId, chequeIssue.CurrencyId, chequeIssue.CurrencyEquivalent, chequeIssue.BankId, chequeIssue.BankBranch, chequeIssue.Date, chequeIssue.IssueTypeId, chequeIssue.ChequeNumber, chequeIssue.ChequeValue, chequeIssue.DueDate, chequeIssue.ChequeStatusId, chequeIssue.IsActive, chequeIssue.IsDeleted, chequeIssue.IsLinked, chequeIssue.IsPosted, chequeIssue.UserId, chequeIssue.Notes, chequeIssue.Image, chequeIssue.CustomerId, chequeIssue.VendorId, chequeIssue.DirectExpensesId, chequeIssue.EmployeeId, chequeIssue.BankAccountId, chequeIssue.DepartmentId, chequeIssue.ShareholderId, chequeIssue.CostCenterId);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("ChequeIssue", "Edit", "AddEdit", id, null, "اصدار أوراق الدفع");
                }
                else
                {
                    chequeIssue.IsActive = true;
                    var idResult = new ObjectParameter("Id", typeof(Int32));

                    db.ChequeIssue_Insert(idResult,chequeIssue.BranchId, chequeIssue.CurrencyId, chequeIssue.CurrencyEquivalent, chequeIssue.BankId, chequeIssue.BankBranch, chequeIssue.Date, chequeIssue.IssueTypeId, chequeIssue.ChequeNumber, chequeIssue.ChequeValue, chequeIssue.DueDate, chequeIssue.ChequeStatusId, chequeIssue.IsActive, chequeIssue.IsDeleted, chequeIssue.IsLinked, false, chequeIssue.UserId, chequeIssue.Notes, chequeIssue.Image, chequeIssue.CustomerId, chequeIssue.VendorId, chequeIssue.DirectExpensesId, chequeIssue.EmployeeId, chequeIssue.BankAccountId, chequeIssue.DepartmentId, chequeIssue.ShareholderId, chequeIssue.CostCenterId);
                    id = (int)idResult.Value;
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("ChequeIssue", "Add", "AddEdit", id, null, "اصدار أوراق الدفع");

                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = chequeIssue.Id > 0 ? "تعديل اصدار أوراق الدفع" : "اضافة اصدار أوراق الدفع",
                    EnAction = "AddEdit",
                    ControllerName = "ChequeIssue",
                    UserName = User.Identity.Name,
                    UserId = userId,
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = chequeIssue.Id,
                    CodeOrDocNo = chequeIssue.DocumentNumber
                });

                if (newBtn == "saveAndNew")
                    return RedirectToAction("AddEdit");
                else if (newBtn == "Report")
                    return RedirectToAction("Index", new { report = true, id });
                else
                    return RedirectToAction("Index");
            }
           
            ViewBag.BankId = new SelectList(db.Banks.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeIssue.BankId);
            ViewBag.BankAccountId = new SelectList(db.BankAccounts.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "AccountNumber", chequeIssue.BankAccountId);
            ViewBag.BranchId = new SelectList(db.Branches.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeIssue.BranchId);
            ViewBag.ShareholderId = new SelectList(db.ShareHolders.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeIssue.ShareholderId);
            ViewBag.IssueTypeId = new SelectList(db.ChequeSourceTypes.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", chequeIssue.IssueTypeId);
            ViewBag.ChequeStatusId = new SelectList(db.ChequeStatus.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", chequeIssue.ChequeStatusId);
            ViewBag.CurrencyId = new SelectList(db.Currencies.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeIssue.CurrencyId);
            ViewBag.CustomerId = new SelectList(db.Customers.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeIssue.CustomerId);
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeIssue.EmployeeId);
            ViewBag.VendorId = new SelectList(db.Vendors.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeIssue.VendorId);
            ViewBag.DirectExpensesId = new SelectList(db.DirectExpenses.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeIssue.DirectExpensesId);
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", chequeIssue.DepartmentId);
            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", chequeIssue.DepartmentId);
            }

            ViewBag.Next = QueryHelper.Next(chequeIssue.Id, "ChequeIssue");
            ViewBag.Previous = QueryHelper.Previous(chequeIssue.Id, "ChequeIssue");
            ViewBag.Last = QueryHelper.GetLast("ChequeIssue");
            ViewBag.First = QueryHelper.GetFirst("ChequeIssue");
            return View(chequeIssue);
        }

        // POST: ChequeIssue/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                ChequeIssue chequeIssue = db.ChequeIssues.Find(id);
                if (chequeIssue.IsPosted == true)
                {
                    return Content("false");
                }
                chequeIssue.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                chequeIssue.IsDeleted = true;
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                chequeIssue.DocumentNumber = Code;
                var JournalEntryDoc = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                db.Entry(chequeIssue).State = EntityState.Modified;
                db.Database.ExecuteSqlCommand($"update JournalEntry set IsDeleted = 1, UserId={userId},DocumentNumber=N'{JournalEntryDoc}' where SourcePageId = (select Id from SystemPage where TableName = 'ChequeIssue') and SourceId = {id}; update JournalEntryDetail set IsDeleted = 1 where JournalEntryId = (select Id from JournalEntry where SourcePageId = (select Id from SystemPage where TableName = 'ChequeIssue') and SourceId = {id})");
                db.SaveChanges();

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف اصدار أوراق الدفع",
                    EnAction = "AddEdit",
                    ControllerName = "ChequeIssue",
                    UserName = User.Identity.Name,
                    UserId = userId,
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = chequeIssue.Id,
                    CodeOrDocNo = chequeIssue.DocumentNumber
                });

                ////-------------------- Notification-------------------------////
                Notification.GetNotification("ChequeIssue", "Delete", "Delete", id, null, "اصدار أوراق الدفع");

                ////////////-----------------------------------------------------------------------
                return Content("true");

            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpGet]
        public JsonResult BankAccountsByBankId(int id)
        {
            var accounts = db.BankAccounts.Where(c => c.BankId == id && c.IsDeleted == false && c.IsActive == true).Select(a => new { a.Id, a.AccountNumber });

            return Json(accounts, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        [SkipERPAuthorize]
        public JsonResult SetDocNum(int id, DateTime? VoucherDate)
        {
            bool IsExistInDocumentsCoding = false;
            var noOfDigits = (int?)0;
            var YearFormat = (int?)0;
            var CodingTypeId = (int?)0;
            var IsZerosFills = (bool?)null;
            var newDocNo = "";
            var GeneratedDocNo = "";
            var lastObj = db.ChequeIssues.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
            var lastDocNo = lastObj != null ? lastObj.DocumentNumber : "0";
            var DepartmentCode = db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == id).FirstOrDefault().Code;
            DepartmentCode = double.Parse(DepartmentCode) < 10 ? "0" + DepartmentCode : DepartmentCode;
            var DepartmentDoc = db.DocumentsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).FirstOrDefault();
            var MonthFormat = VoucherDate.Value.Month < 10 ? "0" + VoucherDate.Value.Month.ToString() : VoucherDate.Value.Month.ToString();
            if (DepartmentDoc == null)
            {
                DepartmentDoc = db.DocumentsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.AllDepartments == true).FirstOrDefault();
                if (DepartmentDoc == null)
                {
                    IsExistInDocumentsCoding = false;
                }
                else
                {
                    IsExistInDocumentsCoding = true;
                }
            }
            else
            {
                IsExistInDocumentsCoding = true;
            }
            if (IsExistInDocumentsCoding == true)
            {
                noOfDigits = DepartmentDoc.DigitsNo;
                YearFormat = DepartmentDoc.YearFormat;
                CodingTypeId = DepartmentDoc.CodingTypeId;
                IsZerosFills = DepartmentDoc.IsZerosFills;
                YearFormat = YearFormat == 2 ? int.Parse(VoucherDate.Value.Year.ToString().Substring(2, 2)) : int.Parse(VoucherDate.Value.Year.ToString());

                if (CodingTypeId == 1)//آلي
                {
                    if (lastDocNo.Contains("-"))
                    {
                        var ar = lastDocNo.Split('-');
                        if (IsZerosFills == true)
                        {
                            newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(ar[3]) + 1).ToString());
                        }
                        else
                        {
                            newDocNo = (double.Parse(ar[3]) + 1).ToString();
                        }
                    }
                    else
                    {
                        if (IsZerosFills == true)
                        {
                            newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(lastDocNo) + 1).ToString());
                        }
                        else
                        {
                            newDocNo = (double.Parse(lastDocNo) + 1).ToString();
                        }
                    }
                    GeneratedDocNo = DepartmentCode + "-" + YearFormat + "-" + MonthFormat + "-" + newDocNo;
                }
                else if (CodingTypeId == 2)//متصل شهري
                {
                    lastObj = db.ChequeIssues.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Month == VoucherDate.Value.Month && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
                    if (lastObj != null)
                    {
                        if (lastObj.DocumentNumber.Contains("-"))
                        {
                            var ar = lastObj.DocumentNumber.Split('-');
                            if (double.Parse(ar[2]) == VoucherDate.Value.Month)
                            {
                                if (IsZerosFills == true)
                                {
                                    newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(ar[3]) + 1).ToString());
                                }
                                else
                                {
                                    newDocNo = (double.Parse(ar[3]) + 1).ToString();
                                }
                            }
                            else
                            {
                                if (IsZerosFills == true)
                                {
                                    newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                                }
                                else
                                {
                                    newDocNo = "1";
                                }
                            }
                        }
                        else
                        {
                            if (IsZerosFills == true)
                            {
                                newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(lastObj.DocumentNumber) + 1).ToString()).ToString();
                            }
                            else
                            {
                                newDocNo = (double.Parse(lastObj.DocumentNumber) + 1).ToString();
                            }
                        }
                    }
                    else
                    {
                        if (IsZerosFills == true)
                        {
                            newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                        }
                        else
                        {
                            newDocNo = "1";
                        }
                    }
                    GeneratedDocNo = DepartmentCode + "-" + YearFormat + "-" + MonthFormat + "-" + newDocNo;
                }
                else if (CodingTypeId == 3)//متصل سنوي
                {
                    lastObj = db.ChequeIssues.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

                    if (lastObj != null)
                    {
                        if (lastObj.DocumentNumber.Contains("-"))
                        {
                            var ar = lastObj.DocumentNumber.Split('-');
                            var VoucherDateFormate = int.Parse(ar[1]).ToString().Length == 2 ? int.Parse((VoucherDate.Value.Year.ToString()).Substring(2, 2)) : VoucherDate.Value.Year;
                            if (double.Parse(ar[1]) == VoucherDateFormate)
                            {
                                if (IsZerosFills == true)
                                {
                                    newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(ar[3]) + 1).ToString());
                                }
                                else
                                {
                                    newDocNo = (double.Parse(ar[3]) + 1).ToString();
                                }
                            }
                            else
                            {
                                if (IsZerosFills == true)
                                {
                                    newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                                }
                                else
                                {
                                    newDocNo = "1";
                                }
                            }
                        }
                        else
                        {
                            if (IsZerosFills == true)
                            {
                                newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(lastObj.DocumentNumber) + 1).ToString()).ToString();
                            }
                            else
                            {
                                newDocNo = (double.Parse(lastObj.DocumentNumber) + 1).ToString();
                            }
                        }

                    }
                    else
                    {
                        if (IsZerosFills == true)
                        {
                            newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                        }
                        else
                        {
                            newDocNo = "1";
                        }
                    }
                    GeneratedDocNo = DepartmentCode + "-" + YearFormat + "-" + MonthFormat + "-" + newDocNo;
                }
            }
            else
            {
                if (lastDocNo.Contains("-"))
                {
                    var ar = lastDocNo.Split('-');
                    newDocNo = (double.Parse(ar[3]) + 1).ToString();
                }
                else
                {
                    newDocNo = (double.Parse(lastDocNo) + 1).ToString();
                }
                GeneratedDocNo = newDocNo;
            }
            return Json(GeneratedDocNo, JsonRequestBehavior.AllowGet);
            //var docNo = QueryHelper.DocLastNum(id, "ChequeIssue");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }
    }
}
