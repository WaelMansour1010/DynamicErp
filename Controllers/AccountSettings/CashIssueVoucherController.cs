using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Security.Claims;
using MyERP.Repository;
using System.Threading.Tasks;
using System.Data.Entity.Core.Objects;
using System.Data.SqlClient;
using DevExpress.DataProcessing.InMemoryDataProcessor.GraphGenerator;
using MyERP.Models.MyModels;
using DocumentFormat.OpenXml.Office2010.Excel;
using MyERP.Utils;

namespace MyERP.Controllers.AccountSettings
{
    public class CashIssueVoucherController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: CashIssueVoucher
        public async Task<ActionResult> Index(bool? report, int? id, int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "", int departmentId = 0)
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
                ArAction = "فتح قائمة سند الدفع",
                EnAction = "Index",
                ControllerName = "CashIssueVoucher",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            ////-------------------- Notification-------------------------////
            Notification.GetNotification("CashIssueVoucher", "View", "Index", null, null, "سند الدفع");

            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;


            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", departmentId);

            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", departmentId);

            }

            IQueryable<CashIssueVoucher> cashIssueVouchers;
            if (string.IsNullOrEmpty(searchWord))
            {
                cashIssueVouchers = db.CashIssueVouchers.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (departmentId == 0 || s.DepartmentId == departmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);

                ViewBag.Count = await db.CashIssueVouchers.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (departmentId == 0 || s.DepartmentId == departmentId)).CountAsync();
            }
            else
            {
                cashIssueVouchers = db.CashIssueVouchers.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.MoneyAmount.ToString().Contains(searchWord) || s.CashIssueSourceType.ArName.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.DirectExpens.ArName.Contains(searchWord) || s.Notes.Contains(searchWord) || s.OtherSourceName.Contains(searchWord) || s.Vendor.ArName.Contains(searchWord) || s.CashBox.ArName.Contains(searchWord) || s.ChartOfAccount.ArName.Contains(searchWord) || s.Customer.ArName.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Techanician.ArName.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);

                ViewBag.Count = await db.CashIssueVouchers.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.MoneyAmount.ToString().Contains(searchWord) || s.CashIssueSourceType.ArName.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.DirectExpens.ArName.Contains(searchWord) || s.Notes.Contains(searchWord) || s.OtherSourceName.Contains(searchWord) || s.Vendor.ArName.Contains(searchWord) || s.CashBox.ArName.Contains(searchWord) || s.ChartOfAccount.ArName.Contains(searchWord) || s.Customer.ArName.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Techanician.ArName.Contains(searchWord))).CountAsync();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(await cashIssueVouchers.ToListAsync());
        }
        [HttpGet]
        [SkipERPAuthorize]
        public JsonResult geOwnerBatchest(int id)
        {
            var data = db.PropertyBatches.Where(t => t.Property.Id == id).ToList().Select(t =>
                new   
                {
                    BatchDate = t.BatchDate.Value.ToString("yyyy-MM-dd"),
                    BatchNo = t.BatchNo,
                    BatchTaxPercentage = t.BatchTaxPercentage??0d,
                    BatchTaxValue = t.BatchTaxValueC,
                    BatchValueBeforeDiscountAddtionAndTax = t.BatchValueBeforeDiscountAddtionAndTax ?? 0m,
                    Discount = t.Discount??0m,
                    TotalBatchValue = t.TotalBatchValue,
                    FirstBatchDate = t.FirstBatchDate,
                    Id = t.Id,
                    Image = t.Image,
                    IsDeleted = t.IsDeleted,
                    MainDocId = t.MainDocId,
                    Notes = t.Notes,
                    NumberOfBatches = t.NumberOfBatches,
                    PeriodBetweenBatchesNum = t.PeriodBetweenBatchesNum,
                    PeriodBetweenBatchesTypeId = t.PeriodBetweenBatchesTypeId,
                    UserId = t.UserId,
                    IsDelivered = t.IsDelivered??false ,
                    TotalPaid = t.TotalPaid??0,
                    Remain = t.Remain,
                    AddValue = t.AddValue
                }
          ).ToList();
            return Json(data, JsonRequestBehavior.AllowGet);
        }
        // GET: CashIssueVoucher/Edit/5
        public async Task<ActionResult> AddEdit(int? id)
        {
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            ViewBag.UseCostCenter = systemSetting.UseCostCenter;

            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            CashboxReposistory cashboxReposistory = new CashboxReposistory(db);

            var subAccounts = db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && (c.ClassificationId == 3)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }).ToList();
            ViewBag.IssueAnalysisAccountId = new SelectList(subAccounts, "Id", "ArName");

            //ToDO: Add changes here too
            if (id == null)
            {
                ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.SourceTypeId = new SelectList(db.CashIssueSourceTypes.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName");
                ViewBag.VendorId = new SelectList(db.Vendors.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.TechnicianId = new SelectList(db.Techanicians.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.ShareholderId = new SelectList(db.ShareHolders.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.CustomerId = new SelectList(db.Customers.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(e => e.IsDeleted == false && e.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.DirectExpensesId = new SelectList(db.DirectExpenses.Where(d => d.IsDeleted == false && d.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.DepartmentId = new SelectList(await departmentRepository.UserDepartments(userId).ToListAsync(), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.CashBoxId = new SelectList(await cashboxReposistory.UserCashboxes(userId, systemSetting.DefaultDepartmentId).ToListAsync(), "Id", "ArName", systemSetting.DefaultCashBoxId);
                ViewBag.CostCenterId = new SelectList(db.CostCenters.Where(a => a.IsActive && !a.IsDeleted && a.TypeId == 2).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.ChartOfAccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.BankAccountId = new SelectList(db.BankAccounts.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.AccountNumber
                }), "Id", "ArName");

                ViewBag.CashIssuePaymentMethodId = new SelectList(db.CashIssuePaymentMethods.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.DoctorId = new SelectList(db.Doctors.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.PrepaidExpenseDetailId = new SelectList(db.PrepaidExpenseDetails.Where(c => c.IsDeleted == false && c.IsActive == true && c.IsSelectedAmortization == true
                 && (db.CashIssueVouchers.Where(a => a.PrepaidExpenseDetailId == c.Id).Any() == false)
                ).Select(b => new
                {
                    b.Id,
                    ArName = b.Id + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.PropertyOwnerId = new SelectList(db.PropertyOwners.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.PropertyId = new SelectList(db.Properties.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.Year = new SelectList(new int[] { 2019, 2020, 2021, 2022, 2023, 2024, 2025, 2026, 2027 });
                ViewBag.Month = new SelectList(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });

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
                return View();
            }
            CashIssueVoucher cashIssueVoucher = await db.CashIssueVouchers.FindAsync(id);
            if (cashIssueVoucher == null)
            {
                return HttpNotFound();
            }
            int sysPageId = QueryHelper.SourcePageId("CashIssueVoucher");
            ViewBag.JE = db.GetJournalEntryBySourceIdAndSourcePageId(id, sysPageId).FirstOrDefault();
            if (ViewBag.JE != null)
            {
                var JEDocNo = ViewBag.JE;
                ViewBag.JE.DocumentNumber = JEDocNo != null ? JEDocNo.DocumentNumber.Replace("-", "") : "";
                int JEId = ViewBag.JE.Id;
                int sourcePageId = ViewBag.JE.SourcePageId;
                ViewBag.JEDetails = db.JournalEntryDetails.Where(a => a.SourceId == id && a.JournalEntryId == JEId && a.SourcePageId == sourcePageId).OrderBy(a => a.Id).ToList();

            }

            ViewBag.PropertyOwnerId = new SelectList(db.PropertyOwners.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.PropertyOwnerId);

            // Initialize an empty CashIssueVoucher with a sample IssueAnalysis to start with
            //if(cashIssueVoucher.IssueAnalysis == null)
            //{
            //    cashIssueVoucher.IssueAnalysis = new List<IssueAnalysi>();
            //}            
            
            ViewBag.PropertyId = new SelectList(db.Properties.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.PropertyId);

            var PropertyList = await db.Properties.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }).ToListAsync();
            var PropertyUnitList = await db.PropertyDetails.Where(a => a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.PropertyUnitNo + " - unit "
            }).ToListAsync();

            ViewBag.Properties = PropertyList;
            ViewBag.PropertyUnits = PropertyUnitList;

            ViewBag.PropertyList = new SelectList(PropertyList, "Id", "ArName", null);
            ViewBag.PropertyUnitList = new SelectList(new List<KeyValuePair<int, string>>(), "Id", "ArName", null);

            ViewBag.Accounts = new SelectList(db.ChartOfAccounts.Where(a => a.ClassificationId == 3 && a.IsActive == true && a.IsDeleted == false), "Id", "ArName");
            ViewBag.BranchId = new SelectList(db.Branches.Where(b => b.IsDeleted == false && b.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.BranchId);
            ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.AccountId);
            ViewBag.ShareholderId = new SelectList(db.ShareHolders.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.ShareholderId);
            ViewBag.CurrencyId = new SelectList(db.Currencies.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.CurrencyId);
            ViewBag.SourceTypeId = new SelectList(db.CashIssueSourceTypes.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", cashIssueVoucher.SourceTypeId);
            ViewBag.VendorId = new SelectList(db.Vendors.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.VendorId);
            ViewBag.TechnicianId = new SelectList(db.Techanicians.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.TechnicianId);
            ViewBag.CustomerId = new SelectList(db.Customers.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.CustomerId);
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(e => e.IsDeleted == false && e.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.EmployeeId);
            ViewBag.DirectExpensesId = new SelectList(db.DirectExpenses.Where(d => d.IsDeleted == false && d.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.DirectExpensesId);
            ViewBag.DepartmentId = new SelectList(await departmentRepository.UserDepartments(userId).ToListAsync(), "Id", "ArName", cashIssueVoucher.DepartmentId);
            ViewBag.CashBoxId = new SelectList(await cashboxReposistory.UserCashboxes(userId, cashIssueVoucher.DepartmentId).ToListAsync(), "Id", "ArName", cashIssueVoucher.CashBoxId);
            ViewBag.CostCenterId = new SelectList(db.CostCenters.Where(a => a.IsActive && !a.IsDeleted && a.TypeId == 2).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.CostCenterId);

            ViewBag.ChartOfAccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.ChartOfAccountId);

            ViewBag.BankAccountId = new SelectList(db.BankAccounts.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.AccountNumber
            }), "Id", "ArName", cashIssueVoucher.BankAccountId);
            ViewBag.CashIssuePaymentMethodId = new SelectList(db.CashIssuePaymentMethods.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.CashIssuePaymentMethodId);
            ViewBag.DoctorId = new SelectList(db.Doctors.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName",cashIssueVoucher.DoctorId);
            ViewBag.PrepaidExpenseDetailId = new SelectList(db.PrepaidExpenseDetails.Where(c => c.IsDeleted == false && c.IsActive == true && c.IsSelectedAmortization == true
            ).Select(b => new
            {
                b.Id,
                ArName = b.Id + " - " + b.ArName
            }), "Id", "ArName",cashIssueVoucher.PrepaidExpenseDetailId);
            ViewBag.Year = new SelectList(new int[] { 2019, 2020, 2021, 2022, 2023, 2024, 2025, 2026, 2027 }, cashIssueVoucher.Year);
            ViewBag.Month = new SelectList(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, cashIssueVoucher.Month);
           
            ViewBag.Next = QueryHelper.Next((int)id, "CashIssueVoucher");
            ViewBag.Previous = QueryHelper.Previous((int)id, "CashIssueVoucher");
            ViewBag.Last = QueryHelper.GetLast("CashIssueVoucher");
            ViewBag.First = QueryHelper.GetFirst("CashIssueVoucher");
            try
            {
                ViewBag.Date = cashIssueVoucher.Date.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.TransactionDate = cashIssueVoucher.TransactionDate.Value.ToString("yyyy-MM-ddTHH:mm");

            }
            catch (Exception)
            {
            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل سند الدفع",
                EnAction = "AddEdit",
                ControllerName = "CashIssueVoucher",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = cashIssueVoucher.Id,
                CodeOrDocNo = cashIssueVoucher.DocumentNumber
            });
            return View(cashIssueVoucher);
        }

        // POST: CashIssueVoucher/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult> AddEdit(CashIssueVoucher cashIssueVoucher/*, string newBtn*/)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var BorrowRequestId = cashIssueVoucher.BorrowRequestId > 0 ? cashIssueVoucher.BorrowRequestId : null;
            int? cashierUserId = null;
            int? posId = null, shiftId = null;
            var pos = db.Pos.Where(p => p.CurrentCashierUserId == userId && p.PosStatusId == 2).FirstOrDefault();
            if (pos != null)
            {
                posId = pos.Id;
                cashierUserId = userId;
                shiftId = pos.CurrentShiftId;
            }

            var subAccounts = db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && (c.ClassificationId == 3)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }).ToList();
            ViewBag.IssueAnalysisAccountId = new SelectList(subAccounts, "Id", "ArName");

            if (ModelState.IsValid)
            {
                var id = cashIssueVoucher.Id;
                cashIssueVoucher.IsDeleted = false;

                if (cashIssueVoucher.SourceTypeId == 1)
                {
                    cashIssueVoucher.ShareholderId = null;
            
                    cashIssueVoucher.VendorId = null;
                    cashIssueVoucher.EmployeeId = null;
                    cashIssueVoucher.DirectExpensesId = null;
                    cashIssueVoucher.AccountId = null;
                    cashIssueVoucher.TechnicianId = null;
                    cashIssueVoucher.Year = null;
                    cashIssueVoucher.Month = null;
                    cashIssueVoucher.PrepaidExpenseDetailId = null;
                    cashIssueVoucher.DoctorId = null;
                    cashIssueVoucher.PropertyId = null;
                    cashIssueVoucher.PropertyOwnerId = null;
                }
                else if (cashIssueVoucher.SourceTypeId == 2)
                {
                    cashIssueVoucher.ShareholderId = null;
                    cashIssueVoucher.CustomerId = null;
           
                    cashIssueVoucher.EmployeeId = null;
                    cashIssueVoucher.DirectExpensesId = null;
                    cashIssueVoucher.AccountId = null;
                    cashIssueVoucher.TechnicianId = null;
                    cashIssueVoucher.PrepaidExpenseDetailId = null;
                    cashIssueVoucher.DoctorId = null;
                    cashIssueVoucher.PropertyId = null;
                    cashIssueVoucher.PropertyOwnerId = null;
                }
                else if (cashIssueVoucher.SourceTypeId == 3)
                {
                    cashIssueVoucher.ShareholderId = null;
              
                    cashIssueVoucher.VendorId = null;
                    cashIssueVoucher.CustomerId = null;
                    cashIssueVoucher.DirectExpensesId = null;
                    cashIssueVoucher.AccountId = null;
                    cashIssueVoucher.TechnicianId = null;
                    cashIssueVoucher.Year = null;
                    cashIssueVoucher.Month = null;
                    cashIssueVoucher.PrepaidExpenseDetailId = null;
                    cashIssueVoucher.DoctorId = null;
                    cashIssueVoucher.PropertyId = null;
                    cashIssueVoucher.PropertyOwnerId = null;
                }
                else if (cashIssueVoucher.SourceTypeId == 4)
                {
                    cashIssueVoucher.ShareholderId = null;
       
                    cashIssueVoucher.VendorId = null;
                    cashIssueVoucher.CustomerId = null;
                    cashIssueVoucher.EmployeeId = null;
                    cashIssueVoucher.AccountId = null;
                    cashIssueVoucher.TechnicianId = null;
                    cashIssueVoucher.Year = null;
                    cashIssueVoucher.Month = null;
                    cashIssueVoucher.PrepaidExpenseDetailId = null;
                    cashIssueVoucher.DoctorId = null;
                    cashIssueVoucher.PropertyId = null;
                    cashIssueVoucher.PropertyOwnerId = null;
                }
                else if (cashIssueVoucher.SourceTypeId == 5)
                {
                    cashIssueVoucher.ShareholderId = null;
         
                    cashIssueVoucher.VendorId = null;
                    cashIssueVoucher.CustomerId = null;
                    cashIssueVoucher.EmployeeId = null;
                    cashIssueVoucher.DirectExpensesId = null;
                    cashIssueVoucher.AccountId = null;
                    cashIssueVoucher.TechnicianId = null;
                    cashIssueVoucher.Year = null;
                    cashIssueVoucher.Month = null;
                    cashIssueVoucher.PrepaidExpenseDetailId = null;
                    cashIssueVoucher.DoctorId = null;
                    cashIssueVoucher.PropertyId = null;
                    cashIssueVoucher.PropertyOwnerId = null;
                }
                else if (cashIssueVoucher.SourceTypeId == 6)
                {
                    cashIssueVoucher.ShareholderId = null;
       
                    cashIssueVoucher.VendorId = null;
                    cashIssueVoucher.CustomerId = null;
                    cashIssueVoucher.EmployeeId = null;
                    cashIssueVoucher.DirectExpensesId = null;
                    cashIssueVoucher.TechnicianId = null;
                    cashIssueVoucher.Year = null;
                    cashIssueVoucher.Month = null;
                    cashIssueVoucher.PrepaidExpenseDetailId = null;
                    cashIssueVoucher.DoctorId = null;
                    cashIssueVoucher.PropertyId = null;
                    cashIssueVoucher.PropertyOwnerId = null;
                }
                else if (cashIssueVoucher.SourceTypeId == 7)
                {
                    cashIssueVoucher.VendorId = null;
                    cashIssueVoucher.CustomerId = null;
              
                    cashIssueVoucher.EmployeeId = null;
                    cashIssueVoucher.DirectExpensesId = null;
                    cashIssueVoucher.AccountId = null;
                    cashIssueVoucher.TechnicianId = null;
                    cashIssueVoucher.Year = null;
                    cashIssueVoucher.Month = null;
                    cashIssueVoucher.PrepaidExpenseDetailId = null;
                    cashIssueVoucher.DoctorId = null;
                    cashIssueVoucher.PropertyId = null;
                    cashIssueVoucher.PropertyOwnerId = null;
                }
                else if (cashIssueVoucher.SourceTypeId == 8)
                {
                    cashIssueVoucher.VendorId = null;
                    cashIssueVoucher.PropertyId = null;
                    cashIssueVoucher.PropertyOwnerId = null;
                    cashIssueVoucher.CustomerId = null;
                    cashIssueVoucher.EmployeeId = null;
                    cashIssueVoucher.DirectExpensesId = null;
                    cashIssueVoucher.AccountId = null;
                    cashIssueVoucher.ShareholderId = null;
                    cashIssueVoucher.Year = null;
                    cashIssueVoucher.Month = null;
                    cashIssueVoucher.PrepaidExpenseDetailId = null;
                    cashIssueVoucher.DoctorId = null;
                }
                else if (cashIssueVoucher.SourceTypeId == 9)
                {
                    cashIssueVoucher.VendorId = null;
        
                    cashIssueVoucher.CustomerId = null;
                    cashIssueVoucher.EmployeeId = null;
                    cashIssueVoucher.DirectExpensesId = null;
                    cashIssueVoucher.AccountId = null;
                    cashIssueVoucher.ShareholderId = null;
                    cashIssueVoucher.PrepaidExpenseDetailId = null;
                    cashIssueVoucher.DoctorId = null;
                    cashIssueVoucher.PropertyId = null;
                    cashIssueVoucher.PropertyOwnerId = null;
                }
                else if (cashIssueVoucher.SourceTypeId == 10)
                {
                    cashIssueVoucher.VendorId = null;

                    cashIssueVoucher.CustomerId = null;
                    cashIssueVoucher.EmployeeId = null;
                    cashIssueVoucher.DirectExpensesId = null;
                    cashIssueVoucher.AccountId = null;
                    cashIssueVoucher.ShareholderId = null;
                    cashIssueVoucher.Year = null;
                    cashIssueVoucher.Month = null;
                    cashIssueVoucher.PrepaidExpenseDetailId = null;
                    cashIssueVoucher.DoctorId = null;
                    cashIssueVoucher.PropertyId = null;
                    cashIssueVoucher.PropertyOwnerId = null;

                }
                else if (cashIssueVoucher.SourceTypeId == 11)
                {
                    cashIssueVoucher.VendorId = null;
                    cashIssueVoucher.PropertyId = null;
                    cashIssueVoucher.EmployeeId = null;
                    cashIssueVoucher.DirectExpensesId = null;
                    cashIssueVoucher.AccountId = null;
                    cashIssueVoucher.ShareholderId = null;
                    cashIssueVoucher.Year = null;
                    cashIssueVoucher.Month = null;
                    cashIssueVoucher.PrepaidExpenseDetailId = null;
                    cashIssueVoucher.PropertyId = null;
                    cashIssueVoucher.PropertyOwnerId = null;
                }
                else if (cashIssueVoucher.SourceTypeId == 12)
                {
                    cashIssueVoucher.VendorId = null;
               
                    cashIssueVoucher.CustomerId = null;
                    cashIssueVoucher.EmployeeId = null;
                    cashIssueVoucher.DirectExpensesId = null;
                    cashIssueVoucher.AccountId = null;
                    cashIssueVoucher.ShareholderId = null;
                    cashIssueVoucher.Year = null;
                    cashIssueVoucher.Month = null;
                    cashIssueVoucher.DoctorId = null;
                    cashIssueVoucher.PropertyId = null;
                    cashIssueVoucher.PropertyOwnerId = null;
                }
                else if (cashIssueVoucher.SourceTypeId == 13)
                {
                    cashIssueVoucher.VendorId = null;
           
                  
                    cashIssueVoucher.DirectExpensesId = null;
                    cashIssueVoucher.AccountId = null;
                    cashIssueVoucher.ShareholderId = null;
                    cashIssueVoucher.Year = null;
                    cashIssueVoucher.Month = null;
                    cashIssueVoucher.DoctorId = null;
                    cashIssueVoucher.CustomerId = null;
                    cashIssueVoucher.EmployeeId = null;
                    cashIssueVoucher.PrepaidExpenseDetailId = null;
                    if (cashIssueVoucher.PropertyBatches != null)
                    {
                        foreach (var batch in cashIssueVoucher.PropertyBatches)
                        {
                            var btch = db.PropertyBatches.FirstOrDefault(t => t.Id == batch.Id);
                            if (btch != null)
                            {
                                btch.AddValue = batch.AddValue;
                                btch.Discount = batch.Discount;
                                btch.TotalPaid = batch.TotalPaid;
                                btch.IsDelivered = batch.IsDelivered;
                                db.Entry(btch).State = EntityState.Modified;
                            }
                        }
                    }
                }

                cashIssueVoucher.PosId = posId;
                cashIssueVoucher.ShiftId = shiftId;
                cashIssueVoucher.CashierUserId = cashierUserId;
                cashIssueVoucher.IsClosed = false;
                cashIssueVoucher.IsCollected = false;

                if (cashIssueVoucher.Id > 0)
                {
                    if (db.CashIssueVouchers.Find(cashIssueVoucher.Id).IsPosted == true)
                    {
                        return Content("false");
                    }
                    cashIssueVoucher.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    MyXML.xPathName = "PurchaseInvoiceActualPayment";
                    var PurchaseInvoiceActualPaymentsXml = MyXML.GetXML(cashIssueVoucher.PurchaseInvoiceActualPayments);
                    MyXML.xPathName = "CashIssueVoucherEmployeePayrollIssue";
                    var CashIssueVoucherEmployeePayrollIssue = MyXML.GetXML(cashIssueVoucher.CashIssueVoucherEmployeePayrollIssues);
                    //MyXML.xPathName = "IssueAnalysis";
                    //var IssueAnalysisWithoutDetails = cashIssueVoucher.IssueAnalysis.Select(o => new
                    //{ o.Id, o.CashIssueVoucherId, o.Value, o.Reason, o.AccountId, o.Notes, o.IsDeleted, o.Total, o.Taxes, o.TaxesPrecentage, o.NetTotal, o.VendorId, o.VendorArName, o.TaxNumber, o.VATNumber, o.InvoiceNo }).ToList();
                    //var IssueAnalysis = MyXML.GetXML(IssueAnalysisWithoutDetails);
                    ////convert all records of IssueAnalysisDetails to xml
                    //MyXML.xPathName = "IssueAnalysisDetail";
                    //var IssueAnalysisDetailRecords = cashIssueVoucher.IssueAnalysis
                    //    .SelectMany(o => o.IssueAnalysisDetails.Select(d => new
                    //    {
                    //        d.Id,
                    //        d.IssueAnalysisId,
                    //        d.PropertyDetailId,
                    //        d.Price
                    //    }))
                    //    .ToList();

                    
                    //first send IssueAnalysisWithoutDetails and IssueAnalysisDetail for the updated instances
                    //to CashIssueVoucher_Update
                    //this will update them and delete other instances that are not in XML
                    List<IssueAnalysis> updatedIssueAnalysis = new List<IssueAnalysis>();
                    List<IssueAnalysis> newIssueAnalysis = new List<IssueAnalysis>();
                    if (cashIssueVoucher.IssueAnalysis != null)
                    {
                        foreach (var analysis in cashIssueVoucher.IssueAnalysis)
                        {
                            if (analysis.Id < 0)
                            {
                                newIssueAnalysis.Add(analysis);
                            }
                            else
                            {
                                updatedIssueAnalysis.Add(analysis);
                            }
                        }
                    }
                    MyXML.xPathName = "IssueAnalysis";
                    var IssueAnalysisWithoutDetails = updatedIssueAnalysis.Select(o => new
                    { o.Id, o.CashIssueVoucherId, o.Value, o.Reason, o.AccountId, o.Notes, o.IsDeleted, o.Total, o.Taxes, o.TaxesPrecentage, o.NetTotal, o.VendorId, o.VendorArName, o.TaxNumber, o.VATNumber, o.InvoiceNo }).ToList();
                    var IssueAnalysis = MyXML.GetXML(IssueAnalysisWithoutDetails);
                    //convert all records of IssueAnalysisDetails to xml
                    MyXML.xPathName = "IssueAnalysisDetails";
                    var IssueAnalysisDetailRecords = updatedIssueAnalysis
                        .SelectMany(o => o.IssueAnalysisDetails.Select(d => new
                        {
                            d.Id,
                            d.IssueAnalysisId,
                            d.PropertyDetailId,
                            d.Price
                        }))
                        .ToList();
                    var IssueAnalysisDetail = MyXML.GetXML(IssueAnalysisDetailRecords);
                    db.CashIssueVoucher_Update(cashIssueVoucher.Id, cashIssueVoucher.DocumentNumber, cashIssueVoucher.BranchId,
                        cashIssueVoucher.MoneyAmount, cashIssueVoucher.SourceTypeId, cashIssueVoucher.DirectExpensesId,
                        cashIssueVoucher.Date, cashIssueVoucher.CurrencyId, cashIssueVoucher.AccountId, cashIssueVoucher.IsLinked,
                        cashIssueVoucher.IsPosted, cashIssueVoucher.IsActive, cashIssueVoucher.IsDeleted, cashIssueVoucher.UserId,
                        cashIssueVoucher.Notes, cashIssueVoucher.Image, cashIssueVoucher.CustomerId, cashIssueVoucher.VendorId,
                        cashIssueVoucher.TechnicianId, cashIssueVoucher.EmployeeId, cashIssueVoucher.CurrencyEquivalent,
                        cashIssueVoucher.DepartmentId, cashIssueVoucher.CashBoxId, cashIssueVoucher.ShareholderId,
                        cashIssueVoucher.CostCenterId, cashIssueVoucher.PosId, cashIssueVoucher.CashierUserId, cashIssueVoucher.ShiftId,
                        cashIssueVoucher.IsCollected, cashIssueVoucher.IsClosed, cashIssueVoucher.IsInvoiceSelected,
                        PurchaseInvoiceActualPaymentsXml, cashIssueVoucher.BankAccountId, cashIssueVoucher.TransactionNo,
                        cashIssueVoucher.TransactionDate, cashIssueVoucher.ChartOfAccountId,
                        cashIssueVoucher.CashIssuePaymentMethodId, cashIssueVoucher.FeesAmount,
                        cashIssueVoucher.ValueAddedTaxesAmount, cashIssueVoucher.IsSynced, cashIssueVoucher.IsUpdateSynced,
                        cashIssueVoucher.BorrowReceipt, BorrowRequestId, cashIssueVoucher.Month, cashIssueVoucher.Year,
                        CashIssueVoucherEmployeePayrollIssue, IssueAnalysis, IssueAnalysisDetail,
                        cashIssueVoucher.DoctorId, cashIssueVoucher.PrepaidExpenseDetailId, cashIssueVoucher.PropertyOwnerId, cashIssueVoucher.PropertyId);

                    //then loop for IssueAnalysis that has id =-1 
                    //call IssueAnalysis_Update SP to insert those new records
                    foreach (var newAnalysis in newIssueAnalysis)
                    {
                        MyXML.xPathName = "IssueAnalysisDetails";
                        var newIssueAnalysisDetails = MyXML.GetXML(newAnalysis.IssueAnalysisDetails);

                        db.IssueAnalysis_Update(
                            newAnalysis.Id,
                            newAnalysis.CashIssueVoucherId,
                            newAnalysis.Value,
                            newAnalysis.Reason,
                            newAnalysis.AccountId,
                            newAnalysis.Notes,
                            false,
                            newAnalysis.Total,
                            newAnalysis.Taxes,
                            (float?)newAnalysis.TaxesPrecentage,
                            newAnalysis.NetTotal,
                            newAnalysis.VendorId,
                            newAnalysis.VendorArName,
                            newAnalysis.TaxNumber,
                            newAnalysis.VATNumber,
                            newAnalysis.InvoiceNo,
                            newIssueAnalysisDetails);

                    }
                    //////-------------------- Notification-------------------------////
                    Notification.GetNotification("CashIssueVoucher", "Edit", "AddEdit", id, null, "سند الدفع");
                }
                else
                {
                    cashIssueVoucher.IsActive = true;
                    //issuePaymentVoucher.DocumentNumber = (QueryHelper.DocLastNum(issuePaymentVoucher.DepartmentId, "CashIssueVoucher") + 1).ToString();
                    cashIssueVoucher.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    MyXML.xPathName = "PurchaseInvoiceActualPayment";
                    var PurchaseInvoiceActualPaymentsXml = MyXML.GetXML(cashIssueVoucher.PurchaseInvoiceActualPayments);
                    MyXML.xPathName = "CashIssueVoucherEmployeePayrollIssue";
                    var CashIssueVoucherEmployeePayrollIssue = MyXML.GetXML(cashIssueVoucher.CashIssueVoucherEmployeePayrollIssues);
                    MyXML.xPathName = "IssueAnalysis";
                    var IssueAnalysisWithoutDetails = cashIssueVoucher.IssueAnalysis.Select(o => new
                        { o.Id, o.CashIssueVoucherId, o.Value, o.Reason, o.AccountId, o.Notes, o.IsDeleted, o.Total, o.Taxes, o.TaxesPrecentage, o.NetTotal, o.VendorId, o.VendorArName, o.TaxNumber, o.VATNumber, o.InvoiceNo }).ToList();
                    var IssueAnalysis = MyXML.GetXML(IssueAnalysisWithoutDetails);
                    //var IssueAnalysis = MyXML.GetXML(cashIssueVoucher.IssueAnalysis);

                    db.CashIssueVoucher_Insert(idResult, cashIssueVoucher.BranchId, cashIssueVoucher.MoneyAmount,
                        cashIssueVoucher.SourceTypeId, cashIssueVoucher.DirectExpensesId, cashIssueVoucher.Date,
                        cashIssueVoucher.CurrencyId, cashIssueVoucher.AccountId, cashIssueVoucher.IsLinked, false, 
                        cashIssueVoucher.IsActive, cashIssueVoucher.IsDeleted, cashIssueVoucher.UserId, cashIssueVoucher.Notes,
                        cashIssueVoucher.Image, cashIssueVoucher.CustomerId, cashIssueVoucher.VendorId, cashIssueVoucher.TechnicianId,
                        cashIssueVoucher.EmployeeId, cashIssueVoucher.CurrencyEquivalent, cashIssueVoucher.DepartmentId,
                        cashIssueVoucher.CashBoxId, cashIssueVoucher.ShareholderId, cashIssueVoucher.CostCenterId, 
                        cashIssueVoucher.PosId, cashIssueVoucher.CashierUserId, cashIssueVoucher.ShiftId, 
                        cashIssueVoucher.IsCollected, cashIssueVoucher.IsClosed, cashIssueVoucher.IsInvoiceSelected,
                        PurchaseInvoiceActualPaymentsXml, cashIssueVoucher.BankAccountId, cashIssueVoucher.TransactionNo, 
                        cashIssueVoucher.TransactionDate, cashIssueVoucher.ChartOfAccountId, cashIssueVoucher.CashIssuePaymentMethodId,
                        cashIssueVoucher.FeesAmount, cashIssueVoucher.ValueAddedTaxesAmount, cashIssueVoucher.IsSynced, 
                        cashIssueVoucher.IsUpdateSynced, cashIssueVoucher.BorrowReceipt, BorrowRequestId, cashIssueVoucher.Month,
                        cashIssueVoucher.Year, CashIssueVoucherEmployeePayrollIssue,null,cashIssueVoucher.DoctorId,
                        cashIssueVoucher.PrepaidExpenseDetailId, cashIssueVoucher.PropertyOwnerId,cashIssueVoucher.PropertyId);

                    id = (int)idResult.Value;
                    //then loop for IssueAnalysis that has id =-1 
                    //call IssueAnalysis_Update SP to insert those new records
                    foreach (var newAnalysis in cashIssueVoucher.IssueAnalysis)
                    {
                        MyXML.xPathName = "IssueAnalysisDetails";
                        var newIssueAnalysisDetails = MyXML.GetXML(newAnalysis.IssueAnalysisDetails);

                        db.IssueAnalysis_Update(
                            newAnalysis.Id,
                            id,
                            newAnalysis.Value,
                            newAnalysis.Reason,
                            newAnalysis.AccountId,
                            newAnalysis.Notes,
                            false,
                            newAnalysis.Total,
                            newAnalysis.Taxes,
                            (float?)newAnalysis.TaxesPrecentage,
                            newAnalysis.NetTotal,
                            newAnalysis.VendorId,
                            newAnalysis.VendorArName,
                            newAnalysis.TaxNumber,
                            newAnalysis.VATNumber,
                            newAnalysis.InvoiceNo,
                            newIssueAnalysisDetails);

                    }
                    
                    if (BorrowRequestId > 0)
                    {
                        var borrowrequest = db.BorrowRequests.Find(BorrowRequestId);
                        if (borrowrequest != null)
                        {
                            borrowrequest.IsIssued = true;
                            db.Entry(borrowrequest).State = EntityState.Modified;
                        }
                    }
                    if (cashIssueVoucher.CashIssueVoucherEmployeePayrollIssues.Count() > 0)
                    {
                        foreach (var item in cashIssueVoucher.CashIssueVoucherEmployeePayrollIssues)
                        {
                            var payrollissue = db.EmployeePayrollIssueDetails.Where(a => a.EmployeePayrollIssue.Month == item.Month && a.EmployeePayrollIssue.Year == item.Year && a.IsActive == true && a.IsDeleted == false && a.EmployeeId == item.EmployeeId).FirstOrDefault();
                            if(payrollissue!=null)
                            {
                                payrollissue.IsIssued = true;
                                db.Entry(payrollissue).State = EntityState.Modified;
                            }

                        }
                    }
                    db.SaveChanges();
                    //-------------------------------//
                    //this is a temporary action to fix the issue of not updating the IssueAnalysis in the JournalEntryDetails table
                    
                    if (cashIssueVoucher.IssueAnalysis.Count() > 0)
                    {
                        //this will update them and delete other instances that are not in XML

                        cashIssueVoucher.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                        //first send IssueAnalysisWithoutDetails and IssueAnalysisDetail for the updated instances
                        //to CashIssueVoucher_Update
                        //this will update them and delete other instances that are not in XML
                        List<IssueAnalysis> updatedIssueAnalysis = db.IssueAnalysis.Where(a => a.CashIssueVoucherId == id && a.IsDeleted == false).ToList();

                        MyXML.xPathName = "IssueAnalysis";
                        var IssueAnalysisWithoutDetails2 = updatedIssueAnalysis.Select(o => new
                        { o.Id, o.CashIssueVoucherId, o.Value, o.Reason, o.AccountId, o.Notes, o.IsDeleted, o.Total, o.Taxes, o.TaxesPrecentage, o.NetTotal, o.VendorId, o.VendorArName, o.TaxNumber, o.VATNumber, o.InvoiceNo }).ToList();
                        var IssueAnalysis2 = MyXML.GetXML(IssueAnalysisWithoutDetails2);
                        //convert all records of IssueAnalysisDetails to xml
                        MyXML.xPathName = "IssueAnalysisDetails";
                        var IssueAnalysisDetailRecords = updatedIssueAnalysis
                            .SelectMany(o => o.IssueAnalysisDetails.Select(d => new
                            {
                                d.Id,
                                d.IssueAnalysisId,
                                d.PropertyDetailId,
                                d.Price
                            }))
                            .ToList();
                        var IssueAnalysisDetail = MyXML.GetXML(IssueAnalysisDetailRecords);
                        db.CashIssueVoucher_Update(id, cashIssueVoucher.DocumentNumber, cashIssueVoucher.BranchId,
                            cashIssueVoucher.MoneyAmount, cashIssueVoucher.SourceTypeId, cashIssueVoucher.DirectExpensesId,
                            cashIssueVoucher.Date, cashIssueVoucher.CurrencyId, cashIssueVoucher.AccountId, cashIssueVoucher.IsLinked,
                            cashIssueVoucher.IsPosted, cashIssueVoucher.IsActive, cashIssueVoucher.IsDeleted, cashIssueVoucher.UserId,
                            cashIssueVoucher.Notes, cashIssueVoucher.Image, cashIssueVoucher.CustomerId, cashIssueVoucher.VendorId,
                            cashIssueVoucher.TechnicianId, cashIssueVoucher.EmployeeId, cashIssueVoucher.CurrencyEquivalent,
                            cashIssueVoucher.DepartmentId, cashIssueVoucher.CashBoxId, cashIssueVoucher.ShareholderId,
                            cashIssueVoucher.CostCenterId, cashIssueVoucher.PosId, cashIssueVoucher.CashierUserId, cashIssueVoucher.ShiftId,
                            cashIssueVoucher.IsCollected, cashIssueVoucher.IsClosed, cashIssueVoucher.IsInvoiceSelected,
                            PurchaseInvoiceActualPaymentsXml, cashIssueVoucher.BankAccountId, cashIssueVoucher.TransactionNo,
                            cashIssueVoucher.TransactionDate, cashIssueVoucher.ChartOfAccountId,
                            cashIssueVoucher.CashIssuePaymentMethodId, cashIssueVoucher.FeesAmount,
                            cashIssueVoucher.ValueAddedTaxesAmount, cashIssueVoucher.IsSynced, cashIssueVoucher.IsUpdateSynced,
                            cashIssueVoucher.BorrowReceipt, BorrowRequestId, cashIssueVoucher.Month, cashIssueVoucher.Year,
                            CashIssueVoucherEmployeePayrollIssue, IssueAnalysis2, IssueAnalysisDetail,
                            cashIssueVoucher.DoctorId, cashIssueVoucher.PrepaidExpenseDetailId, cashIssueVoucher.PropertyOwnerId, cashIssueVoucher.PropertyId);

                        db.SaveChanges();
                    }
                    //-------------------------------//
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("CashIssueVoucher", "Add", "AddEdit", id, null, "سند الدفع");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل سند الدفع" : "اضافة سند الدفع",
                    EnAction = "AddEdit",
                    ControllerName = "CashIssueVoucher",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = cashIssueVoucher.DocumentNumber
                });
                return Json(new { success = "true", id });

            }

            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();


            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            CashboxReposistory cashboxReposistory = new CashboxReposistory(db);
            ViewBag.Accounts = new SelectList(db.ChartOfAccounts.Where(a => a.ClassificationId == 3 && a.IsActive == true && a.IsDeleted == false), "Id", "ArName");
            ViewBag.BranchId = new SelectList(db.Branches.Where(b => b.IsDeleted == false && b.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.BranchId);
            ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.AccountId);
            ViewBag.ShareholderId = new SelectList(db.ShareHolders.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.ShareholderId);
            ViewBag.CurrencyId = new SelectList(db.Currencies.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.CurrencyId);
            ViewBag.SourceTypeId = new SelectList(db.CashIssueSourceTypes.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", cashIssueVoucher.SourceTypeId);
            ViewBag.VendorId = new SelectList(db.Vendors.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.VendorId);
            ViewBag.TechnicianId = new SelectList(db.Techanicians.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.TechnicianId);
            ViewBag.CustomerId = new SelectList(db.Customers.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.CustomerId);
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(e => e.IsDeleted == false && e.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.EmployeeId);
            ViewBag.DirectExpensesId = new SelectList(db.DirectExpenses.Where(d => d.IsDeleted == false && d.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.DirectExpensesId);
            ViewBag.DepartmentId = new SelectList(await departmentRepository.UserDepartments(userId).ToListAsync(), "Id", "ArName", cashIssueVoucher.DepartmentId);
            ViewBag.CashBoxId = new SelectList(await cashboxReposistory.UserCashboxes(userId, cashIssueVoucher.DepartmentId).ToListAsync(), "Id", "ArName", cashIssueVoucher.CashBoxId);
            ViewBag.ChartOfAccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.ChartOfAccountId);

            ViewBag.BankAccountId = new SelectList(db.BankAccounts.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.AccountNumber
            }), "Id", "ArName", cashIssueVoucher.BankAccountId);

            ViewBag.CashIssuePaymentMethodId = new SelectList(db.CashIssuePaymentMethods.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashIssueVoucher.CashIssuePaymentMethodId);

            try
            {
                ViewBag.Date = cashIssueVoucher.Date.Value.ToString("yyyy-MM-ddTHH:mm");
                if (cashIssueVoucher.TransactionDate != null)
                {
                    ViewBag.TransactionDate = cashIssueVoucher.TransactionDate.Value.ToString("yyyy-MM-ddTHH:mm");
                }

            }
            catch (Exception)
            {
            }
            return View(cashIssueVoucher);
        }

        // POST: CashIssueVoucher/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                CashIssueVoucher cashIssueVoucher = db.CashIssueVouchers.Find(id);
                if (cashIssueVoucher.IsPosted == true)
                {
                    return Content("false");
                }
                db.CashIssueVoucher_Delete(id, userId);
                var BorrowRequestId = cashIssueVoucher.BorrowRequestId;
                if (BorrowRequestId > 0)
                {
                    var borrowrequest = db.BorrowRequests.Find(BorrowRequestId);
                    if (borrowrequest != null)
                    {
                        borrowrequest.IsIssued = false;
                        db.Entry(borrowrequest).State = EntityState.Modified;
                    }
                }
                if (cashIssueVoucher.CashIssueVoucherEmployeePayrollIssues.Count() > 0)
                {
                    foreach (var item in cashIssueVoucher.CashIssueVoucherEmployeePayrollIssues)
                    {
                        var payrollissue = db.EmployeePayrollIssueDetails.Where(a => a.EmployeePayrollIssue.Month == item.Month && a.EmployeePayrollIssue.Year == item.Year && a.IsActive == true && a.IsDeleted == false && a.EmployeeId == item.EmployeeId).FirstOrDefault();
                        if (payrollissue != null)
                        {
                            payrollissue.IsIssued = false;
                            db.Entry(payrollissue).State = EntityState.Modified;
                        }

                    }
                }
                db.SaveChanges();

                //issuePaymentVoucher.IsDeleted = true;
                //issuePaymentVoucher.UserId = userId;
                //db.Entry(issuePaymentVoucher).State = EntityState.Modified;
                //db.Database.ExecuteSqlCommand($"update JournalEntry set IsDeleted = 1, UserId={userId} where SourcePageId = (select Id from SystemPage where TableName = 'CashIssueVoucher') and SourceId = {id}; update JournalEntryDetail set IsDeleted = 1 where JournalEntryId = (select Id from JournalEntry where SourcePageId = (select Id from SystemPage where TableName = 'CashIssueVoucher') and SourceId = {id})");
                //db.SaveChanges();

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف سند الدفع",
                    EnAction = "AddEdit",
                    ControllerName = "CashIssueVoucher",
                    UserName = User.Identity.Name,
                    UserId = userId,
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = cashIssueVoucher.Id,
                    CodeOrDocNo = cashIssueVoucher.DocumentNumber
                });
                ////-------------------- Notification-------------------------////

                Notification.GetNotification("CashIssueVoucher", "Delete", "Delete", id, null, "سند الدفع");

                ///////////////-----------------------------------------------------------------------

                return Content("true");
            }
            catch (Exception)
            {

                throw;
            }
        }

        public ActionResult Synchronization(/*int pageIndex = 1, int wantedRowsNo = 10*/)
        {
            //ViewBag.PageIndex = pageIndex;                      
            //int skipRowsNo = 0;

            //if (pageIndex > 1)
            //    skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            //ViewBag.wantedRowsNo = wantedRowsNo;

            var NotSyncedVouchers = db.CashIssueVouchers.Where(a => a.IsSynced != true)/*.OrderByDescending(a=>a.Id).Skip(skipRowsNo).Take(wantedRowsNo)*/.ToList();
            // ViewBag.Count = db.CashIssueVouchers.Where(a => a.IsSynced != true).Count();
            return View(NotSyncedVouchers);
        }
        [HttpPost]
        public ActionResult SynchronizationData(List<int> ids)
        {
            List<CashIssueVoucher> cashIssueVouchers = new List<CashIssueVoucher>();
            string databaseName = "MyERPNew", datasource = ".", userId = "", password = "";
            string connectionString = "data source=" + datasource + ";initial catalog=" + databaseName + ";Integrated Security=True;multipleactiveresultsets=True;";//user id="+userId+";password="+password+";
            MySoftERPEntity onlineDb = new MySoftERPEntity();
            onlineDb.Database.Connection.ConnectionString = connectionString;
            onlineDb.Database.Connection.Open();

            if (ids.Count() > 0)
            {
                foreach (var id in ids)
                {
                    if (id > 0)
                    {
                        var cashIssueVoucher = db.CashIssueVouchers.Find(id);
                        if (cashIssueVoucher != null)
                        {
                            // cashIssueVouchers.Add(cashIssueVoucher);
                            List<PurchaseInvoiceActualPayment> Actualpayments = new List<PurchaseInvoiceActualPayment>();
                            foreach (var item in cashIssueVoucher.PurchaseInvoiceActualPayments)
                            {
                                var actualPayment = new PurchaseInvoiceActualPayment();
                                actualPayment.Amount = item.Amount;
                                actualPayment.ChequeIssueId = item.ChequeIssueId;
                                actualPayment.NetAmountRemain = item.NetAmountRemain;
                                actualPayment.PaymentDate = item.PaymentDate;
                                actualPayment.PurchaseInvoiceDate = item.PurchaseInvoiceDate;
                                actualPayment.PurchaseInvoiceDocNumber = item.PurchaseInvoiceDocNumber;
                                actualPayment.PurchaseInvoiceId = item.PurchaseInvoiceId;
                                actualPayment.UserId = item.UserId;
                                actualPayment.Notes = item.Notes;
                                actualPayment.IsActive = item.IsActive;
                                actualPayment.IsDeleted = item.IsDeleted;
                                actualPayment.CashIssueVoucherId = 0;
                                Actualpayments.Add(actualPayment);
                            }
                            // var idResult = new ObjectParameter("Id", typeof(Int32));
                            // MyXML.xPathName = "PurchaseInvoiceActualPayment";
                            // var PurchaseInvoiceActualPaymentsXml = MyXML.GetXML(Actualpayments);
                            //onlineDb.CashIssueVoucher_Insert(idResult, cashIssueVoucher.BranchId, cashIssueVoucher.MoneyAmount, cashIssueVoucher.SourceTypeId, cashIssueVoucher.DirectExpensesId, cashIssueVoucher.Date, cashIssueVoucher.CurrencyId, cashIssueVoucher.AccountId, cashIssueVoucher.IsLinked, false, cashIssueVoucher.IsActive, cashIssueVoucher.IsDeleted, cashIssueVoucher.UserId, cashIssueVoucher.Notes, cashIssueVoucher.Image, cashIssueVoucher.CustomerId, cashIssueVoucher.VendorId, cashIssueVoucher.TechnicianId, cashIssueVoucher.EmployeeId, cashIssueVoucher.CurrencyEquivalent, cashIssueVoucher.DepartmentId, cashIssueVoucher.CashBoxId, cashIssueVoucher.ShareholderId, cashIssueVoucher.CostCenterId, cashIssueVoucher.PosId, cashIssueVoucher.CashierUserId, cashIssueVoucher.ShiftId, cashIssueVoucher.IsCollected, cashIssueVoucher.IsClosed, cashIssueVoucher.IsInvoiceSelected, PurchaseInvoiceActualPaymentsXml, cashIssueVoucher.BankAccountId, cashIssueVoucher.TransactionNo, cashIssueVoucher.TransactionDate, cashIssueVoucher.ChartOfAccountId, cashIssueVoucher.CashIssuePaymentMethodId, cashIssueVoucher.FeesAmount, cashIssueVoucher.ValueAddedTaxesAmount, cashIssueVoucher.IsSynced, cashIssueVoucher.IsUpdateSynced,cashIssueVoucher.BorrowReceipt, cashIssueVoucher.BorrowRequestId, cashIssueVoucher.Month, cashIssueVoucher.Year);
                            // cashIssueVoucher.IsSynced = true;
                            // cashIssueVoucher.IsUpdateSynced = true;
                            // db.Entry(cashIssueVoucher).State = EntityState.Modified;
                            // db.SaveChanges();
                        }
                    }
                }


                onlineDb.Database.Connection.Close();
                onlineDb.Database.Connection.Dispose();

            }
            return Json(new { success = "true", ids });
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
            var lastObj = db.CashIssueVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.CashIssueVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Month == VoucherDate.Value.Month && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.CashIssueVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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

            //var docNo = QueryHelper.DocLastNum(id, "CashIssueVoucher");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetEmployeeBorrowRequest(int? EmployeeId)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var EmployeeBorrow = db.BorrowRequests.Where(a => a.IsDeleted == false && a.EmployeeId == EmployeeId && a.IsIssued != true && a.BorrowStatusId == 2)
                .Select(a => new
                {
                    BorrowRequestId = a.Id,
                    a.DocumentNumber,
                    a.BorrowValue
                }).ToList();
            return Json(EmployeeBorrow, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetIssueAnalysisAccountDetails(int? IssueAnalysisAccountId)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var IssueAnalysisAccountDetails = db.ChartOfAccounts.Where(a => a.IsDeleted == false && a.Id == IssueAnalysisAccountId).Select(a=>new {a.Id,a.ArName,a.IncludeValueAddedTax }).FirstOrDefault();
            return Json(IssueAnalysisAccountDetails, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetVendorData(int? VendorId)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var vendor = db.Vendors.Where(a => a.IsDeleted == false &&a.IsActive==true && a.Id == VendorId).Select(a => new { a.Id, a.ArName, a.TaxNumber,a.VATNumber }).FirstOrDefault();
            return Json(vendor, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult ApproveNotApprove(int id)
        {
            try
            {
                CashIssueVoucher cashIssueVoucher = db.CashIssueVouchers.Find(id);
                if (cashIssueVoucher.IsActive == true)
                {
                    cashIssueVoucher.IsActive = false;
                }
                else
                {
                    cashIssueVoucher.IsActive = true;
                }
                cashIssueVoucher.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(cashIssueVoucher).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)cashIssueVoucher.IsActive ? "إعتماد سند الدفع" : "إلغاء إعتماد سند الدفع",
                    EnAction = "AddEdit",
                    ControllerName = "CashIssueVoucher",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = cashIssueVoucher.Id,
                    CodeOrDocNo = cashIssueVoucher.DocumentNumber
                });
                if (cashIssueVoucher.IsActive == true)
                {
                    Notification.GetNotification("CashIssueVoucher", "Approve", "Approve", id, true, "سند الدفع");
                }
                else
                {

                    Notification.GetNotification("CashIssueVoucher", "Approve", "Approve", id, false, " سند الدفع");
                }

                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }
        [SkipERPAuthorize]
        public JsonResult GetPrepaidExpenseDetailValue(int? PrepaidExpenseDetailId)
        {
            var valuue = db.PrepaidExpenseDetails.Where(a=>a.Id== PrepaidExpenseDetailId).FirstOrDefault()?.Value!=null? db.PrepaidExpenseDetails.Where(a => a.Id == PrepaidExpenseDetailId).FirstOrDefault().Value:0;
            return Json(valuue,JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public ActionResult GetProperties()
        {
            if (HttpContext.Request.IsAjaxRequest())
            {
                return Json(new SelectList(db.Properties.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName"), JsonRequestBehavior.AllowGet
                );
            }
            return View();
        }
       
        [SkipERPAuthorize]
        public ActionResult GetPropertyUnits(int PropertyId)
        {
            db.Configuration.ProxyCreationEnabled = false;
            List<PropertyDetail> propertyDetail = db.PropertyDetails.Where(oo => oo.MainDocId == PropertyId && oo.IsDeleted == false).ToList();

            if (HttpContext.Request.IsAjaxRequest())
            {
                return Json(new SelectList(
                    propertyDetail,
                    "Id",
                    "PropertyUnitNo"), JsonRequestBehavior.AllowGet
                    );
            }
            return View();
        }

        [SkipERPAuthorize]
        public ActionResult LoadEmptyIssueAnalysisPartial()
        {
            var subAccounts = db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && (c.ClassificationId == 3)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }).ToList();
            
            ViewBag.IssueAnalysisAccountId = new SelectList(subAccounts, "Id", "ArName");
            IssueAnalysis model = new IssueAnalysis()
            {
                Id = -1  // this is an indication for a new IssueAnalysis
            };
            return PartialView("_IssueAnalysis", model);
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
