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
using MyERP.Reporting;
using System.Threading.Tasks;
using MyERP.Repository;
using System.Data.Entity.Core.Objects;

namespace MyERP.Controllers.AccountSettings
{
    public class ChequeReceiptController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: ChequeReceipt
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
                ArAction = "فتح قائمة استلام الشيكات",
                EnAction = "Index",
                ControllerName = "ChequeReceipt",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("ChequeReceipt", "View", "Index", null, null, "استلام الشيكات");

            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<ChequeReceipt> chequeReceipts;
            if (string.IsNullOrEmpty(searchWord))
            {
                chequeReceipts = db.ChequeReceipts.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ChequeReceipts.Where(s => s.IsDeleted == false && s.IsActive == true && depIds.Contains(s.DepartmentId)).Count();
            }
            else
            {
                chequeReceipts = db.ChequeReceipts.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Bank.ArName.Contains(searchWord) || s.BankBranch.Contains(searchWord) || s.Notes.Contains(searchWord) || s.BankAccount.AccountNumber.Contains(searchWord) || s.Customer.ArName.Contains(searchWord) || s.Vendor.ArName.Contains(searchWord) || s.ChequeSourceType.ArName.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.ChequeReceiptStatu.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.ChartOfAccount.ArName.Contains(searchWord) || s.ChequeNumber.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);

                ViewBag.Count = db.ChequeReceipts.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Bank.ArName.Contains(searchWord) || s.BankBranch.Contains(searchWord) || s.Notes.Contains(searchWord) || s.BankAccount.AccountNumber.Contains(searchWord) || s.Customer.ArName.Contains(searchWord) || s.Vendor.ArName.Contains(searchWord) || s.ChequeSourceType.ArName.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.ChequeReceiptStatu.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.ChartOfAccount.ArName.Contains(searchWord) || s.ChequeNumber.Contains(searchWord))).Count();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(await chequeReceipts.ToListAsync());
        }

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
                ViewBag.BranchId = new SelectList(db.Branches.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.SourceTypeId = new SelectList(db.ChequeSourceTypes.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName");
                ViewBag.ChequeStatusId = new SelectList(db.ChequeReceiptStatus.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName");
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
                ViewBag.DirectRevenueId = new SelectList(db.DirectRevenues.Where(d => d.IsDeleted == false && d.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.DepartmentId = new SelectList(await departmentRepository.UserDepartments(userId).ToListAsync(), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.DebitBankId = new SelectList(db.Banks.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.DebitBankAccountId = new SelectList(db.BankAccounts.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "AccountNumber");
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
            ChequeReceipt chequeReceipt = await db.ChequeReceipts.FindAsync(id);
            if (chequeReceipt == null)
            {
                return HttpNotFound();
            }
            int sysPageId = QueryHelper.SourcePageId("ChequeReceipt");

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
            ViewBag.BankId = new SelectList(db.Banks.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeReceipt.BankId);
            ViewBag.BranchId = new SelectList(db.Branches.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeReceipt.BranchId);
            ViewBag.SourceTypeId = new SelectList(db.ChequeSourceTypes.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", chequeReceipt.SourceTypeId);
            ViewBag.ChequeStatusId = new SelectList(db.ChequeReceiptStatus.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", chequeReceipt.ChequeStatusId);
            ViewBag.CurrencyId = new SelectList(db.Currencies.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeReceipt.CurrencyId);
            ViewBag.CustomerId = new SelectList(db.Customers.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeReceipt.CustomerId);
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeReceipt.EmployeeId);
            ViewBag.DirectExpensesId = new SelectList(db.DirectExpenses.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeReceipt.DirectExpensesId);
            ViewBag.VendorId = new SelectList(db.Vendors.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", chequeReceipt.VendorId);
            ViewBag.DirectRevenueId = new SelectList(db.DirectRevenues.Where(d => d.IsDeleted == false && d.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeReceipt.DirectRevenueId);
            ViewBag.DepartmentId = new SelectList(await departmentRepository.UserDepartments(userId).ToListAsync(), "Id", "ArName", chequeReceipt.DepartmentId);
            ViewBag.ShareholderId = new SelectList(db.ShareHolders.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeReceipt.ShareholderId);
            ViewBag.ReceiptAccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeReceipt.ReceiptAccountId);
            ViewBag.DebitBankId = new SelectList(db.Banks.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeReceipt.DebitBankId);
            ViewBag.DebitBankAccountId = new SelectList(db.BankAccounts.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "AccountNumber", chequeReceipt.DebitBankAccountId);
            ViewBag.CostCenterId = new SelectList(db.CostCenters.Where(a => a.IsActive && !a.IsDeleted && a.TypeId == 2).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName",chequeReceipt.CostCenterId);
            ViewBag.Next = QueryHelper.Next((int)id, "ChequeReceipt");
            ViewBag.Previous = QueryHelper.Previous((int)id, "ChequeReceipt");
            ViewBag.Last = QueryHelper.GetLast("ChequeReceipt");
            ViewBag.First = QueryHelper.GetFirst("ChequeReceipt");
            try
            {
                ViewBag.Date = chequeReceipt.Date.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.DueDate = chequeReceipt.DueDate.Value.ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception)
            {
            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل استلام الشيكات",
                EnAction = "AddEdit",
                ControllerName = "ChequeReceipt",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = chequeReceipt.Id,
                CodeOrDocNo = chequeReceipt.DocumentNumber
            });
            return View(chequeReceipt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddEdit(ChequeReceipt chequeReceipt, string newBtn)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            chequeReceipt.UserId = userId;

            if (ModelState.IsValid)
            {
                chequeReceipt.IsDeleted = false;
                var id = chequeReceipt.Id;
                if (chequeReceipt.SourceTypeId == 6)
                {
                    chequeReceipt.VendorId = null;
                    chequeReceipt.CustomerId = null;
                    chequeReceipt.DirectRevenueId = null;
                    chequeReceipt.EmployeeId = null;
                    chequeReceipt.ShareholderId = null;

                }
                if (chequeReceipt.SourceTypeId == 5)
                {
                    chequeReceipt.VendorId = null;
                    chequeReceipt.CustomerId = null;
                    chequeReceipt.DirectRevenueId = null;
                    chequeReceipt.EmployeeId = null;
                    chequeReceipt.DirectExpensesId = null;

                }
                else if (chequeReceipt.SourceTypeId == 3)
                {
                    chequeReceipt.VendorId = null;
                    chequeReceipt.CustomerId = null;
                    chequeReceipt.DirectRevenueId = null;
                    chequeReceipt.ShareholderId = null;
                    chequeReceipt.DirectExpensesId = null;
                }
                else if (chequeReceipt.SourceTypeId == 2)
                {

                    chequeReceipt.CustomerId = null;
                    chequeReceipt.EmployeeId = null;
                    chequeReceipt.DirectRevenueId = null;
                    chequeReceipt.ShareholderId = null;
                    chequeReceipt.DirectExpensesId = null;
                }
                else if (chequeReceipt.SourceTypeId == 1)
                {
                    chequeReceipt.VendorId = null;
                    chequeReceipt.DirectRevenueId = null;
                    chequeReceipt.EmployeeId = null;
                    chequeReceipt.ShareholderId = null;
                    chequeReceipt.DirectExpensesId = null;
                }

                if (chequeReceipt.Id > 0)
                {
                    if (chequeReceipt.IsPosted == true)
                    {
                        return Content("false");
                    }

                    db.ChequeReceipt_Update(chequeReceipt.Id, chequeReceipt.DocumentNumber, chequeReceipt.BranchId, chequeReceipt.CurrencyId, chequeReceipt.CurrencyEquivalent, chequeReceipt.BankId, chequeReceipt.BankBranch, chequeReceipt.Date, chequeReceipt.SourceTypeId, chequeReceipt.ChequeNumber, chequeReceipt.ChequeValue, chequeReceipt.DueDate, chequeReceipt.ChequeStatusId, chequeReceipt.IsActive, chequeReceipt.IsDeleted, chequeReceipt.IsLinked, chequeReceipt.IsPosted, chequeReceipt.UserId, chequeReceipt.Notes, chequeReceipt.Image, chequeReceipt.CustomerId, chequeReceipt.VendorId, chequeReceipt.DirectExpensesId, chequeReceipt.EmployeeId, chequeReceipt.DepartmentId, chequeReceipt.ReceiptAccountId, chequeReceipt.DebitBankId, chequeReceipt.DebitBankAccountId, chequeReceipt.DirectRevenueId, chequeReceipt.ShareholderId, chequeReceipt.CostCenterId);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("ChequeReceipt", "Edit", "AddEdit", id, null, "استلام الشيكات");
                }
                else
                {
                    chequeReceipt.IsActive = true;
                    var idResult = new ObjectParameter("Id", typeof(Int32));

                    db.ChequeReceipt_Insert(idResult,chequeReceipt.BranchId, chequeReceipt.CurrencyId, chequeReceipt.CurrencyEquivalent, chequeReceipt.BankId, chequeReceipt.BankBranch, chequeReceipt.Date, chequeReceipt.SourceTypeId, chequeReceipt.ChequeNumber, chequeReceipt.ChequeValue, chequeReceipt.DueDate, chequeReceipt.ChequeStatusId, chequeReceipt.IsActive, chequeReceipt.IsDeleted, chequeReceipt.IsLinked, false, chequeReceipt.UserId, chequeReceipt.Notes, chequeReceipt.Image, chequeReceipt.CustomerId, chequeReceipt.VendorId, chequeReceipt.DirectExpensesId, chequeReceipt.EmployeeId, chequeReceipt.DepartmentId, chequeReceipt.ReceiptAccountId, chequeReceipt.DebitBankId, chequeReceipt.DebitBankAccountId, chequeReceipt.DirectRevenueId, chequeReceipt.ShareholderId, chequeReceipt.CostCenterId);
                    id = (int)idResult.Value;
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("ChequeReceipt", "Add", "AddEdit", chequeReceipt.Id, null, "استلام الشيكات");

                }

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = chequeReceipt.Id > 0 ? "تعديل استلام الشيكات" : "اضافة استلام الشيكات",
                    EnAction = "AddEdit",
                    ControllerName = "ChequeReceipt",
                    UserName = User.Identity.Name,
                    UserId = userId,
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = chequeReceipt.Id,
                    CodeOrDocNo = chequeReceipt.DocumentNumber
                });
                
                if (newBtn == "saveAndNew")
                    return RedirectToAction("AddEdit");
                else if (newBtn == "Report")
                    return RedirectToAction("Index", new { report = true, id });
                else
                    return RedirectToAction("Index");
            }

            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

            ViewBag.BankId = new SelectList(db.Banks.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", chequeReceipt.BankId);
            ViewBag.BranchId = new SelectList(db.Branches.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", chequeReceipt.BranchId);
            ViewBag.SourceTypeId = new SelectList(db.ChequeSourceTypes.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", chequeReceipt.SourceTypeId);
            ViewBag.ChequeStatusId = new SelectList(db.ChequeReceiptStatus.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", chequeReceipt.ChequeStatusId);
            ViewBag.CurrencyId = new SelectList(db.Currencies.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", chequeReceipt.CurrencyId);
            ViewBag.CustomerId = new SelectList(db.Customers.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", chequeReceipt.CustomerId);
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", chequeReceipt.EmployeeId);
            ViewBag.VendorId = new SelectList(db.Vendors.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", chequeReceipt.VendorId);
            ViewBag.DirectRevenueId = new SelectList(db.DirectRevenues.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", chequeReceipt.DirectRevenueId);
            ViewBag.ShareholderId = new SelectList(db.ShareHolders.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeReceipt.ShareholderId);
            ViewBag.DirectExpensesId = new SelectList(db.DirectExpenses.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeReceipt.DirectExpensesId);
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", chequeReceipt.DepartmentId);
            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", chequeReceipt.DepartmentId);
            }
            ViewBag.ShareholderId = new SelectList(db.ShareHolders.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", chequeReceipt.ShareholderId);
            ViewBag.ReceiptAccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3), "Id", "ArName", chequeReceipt.ReceiptAccountId);
            ViewBag.DebitBankId = new SelectList(db.Banks.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", chequeReceipt.DebitBankId);
            ViewBag.DebitBankAccountId = new SelectList(db.BankAccounts.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "AccountNumber", chequeReceipt.DebitBankAccountId);

            ViewBag.Next = QueryHelper.Next(chequeReceipt.Id, "ChequeReceipt");
            ViewBag.Previous = QueryHelper.Previous(chequeReceipt.Id, "ChequeReceipt");
            ViewBag.Last = QueryHelper.GetLast("ChequeReceipt");
            ViewBag.First = QueryHelper.GetFirst("ChequeReceipt");
            return View(chequeReceipt);
        }

        // POST: ChequeReceipt/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                ChequeReceipt chequeReceipt = db.ChequeReceipts.Find(id);
                if (chequeReceipt.IsPosted == true)
                {
                    return Content("false");
                }
                chequeReceipt.IsDeleted = true;
                chequeReceipt.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                chequeReceipt.DocumentNumber = Code;
                var JournalEntryDoc = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                db.Entry(chequeReceipt).State = EntityState.Modified;
                db.Database.ExecuteSqlCommand($"update JournalEntry set IsDeleted = 1, UserId={userId},DocumentNumber=N'{JournalEntryDoc}' where SourcePageId = (select Id from SystemPage where TableName = 'ChequeReceipt') and SourceId = {id}; update JournalEntryDetail set IsDeleted = 1 where JournalEntryId = (select Id from JournalEntry where SourcePageId = (select Id from SystemPage where TableName = 'ChequeReceipt') and SourceId = {id})");
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف استلام الشيكات",
                    EnAction = "AddEdit",
                    ControllerName = "ChequeReceipt",
                    UserName = User.Identity.Name,
                    UserId = userId,
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = chequeReceipt.Id,
                    CodeOrDocNo = chequeReceipt.DocumentNumber
                });

                ////-------------------- Notification-------------------------////
                Notification.GetNotification("ChequeReceipt", "Delete", "Delete", id, null, "استلام الشيكات");

                //////////-----------------------------------------------------------------------

                return Content("true");
            }
            catch (Exception)
            {

                throw;
            }
        }

        public ActionResult OpenReport(string save)
        {
            ViewBag.SaveType = save;
            var lastId = QueryHelper.GetLast("ChequeReceipt");
            ViewBag.Id = lastId;
            ViewBag.ControllerName = "ChequeReceipt";
            ViewBag.ReportName = "ChequeReceiptDetails";
            SystemSetting sysObj = db.SystemSettings.FirstOrDefault();
            ViewData["AfterAdd"] = sysObj.PrintTransactionsAfterAdd;

            return View("OpenReport");
        }
        [HttpGet]
        public JsonResult BankAccountsByBankId(int id)
        {
            var accounts = db.BankAccounts.Where(a => a.BankId == id && a.IsDeleted == false && a.IsActive == true).Select(a => new { a.Id, a.AccountNumber });
            var json = JsonConvert.SerializeObject(accounts);
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
            var lastObj = db.ChequeReceipts.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.ChequeReceipts.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Month == VoucherDate.Value.Month && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.ChequeReceipts.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "ChequeReceipt");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }
    }
}
