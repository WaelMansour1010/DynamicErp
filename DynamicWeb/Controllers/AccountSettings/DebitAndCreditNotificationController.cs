using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using MyERP.Repository;
using System.Data.Entity.Core.Objects;
using Microsoft.IdentityModel.Tokens;
using ExcelDataReader;
using System.IO;
using System.Data;
using System.Data.Entity.Core.Objects;
using ExcelDataReader;
using System.Data;
using System.Data.SqlClient;
using System.Security.Claims;
using System.Data.SqlClient;
using System.Text;
using System.Globalization;

using System.Text.RegularExpressions;
namespace MyERP.Controllers.AccountSettings
{
    public class DebitAndCreditNotificationController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: DebitAndCreditNotification
        public async Task<ActionResult> Index(int? id, int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "", int departmentId = 0)
        {
            ViewBag.PageIndex = pageIndex;

            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var depIds = await departmentRepository.UserDepartmentsIds(userId);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الإشعارات المدينة و الدائنة",
                EnAction = "Index",
                ControllerName = "DebitAndCreditNotification",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("DebitAndCreditNotification", "View", "Index", null, null, "الإشعارات المدينة و الدائنة");

            //////////////-----------------------------------------------------------------------

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
            /////////////////////////// Search ////////////////////

            IQueryable<DebitAndCreditNotification> debitAndCreditNotifications;
            if (string.IsNullOrEmpty(searchWord))
            {
                debitAndCreditNotifications = db.DebitAndCreditNotifications.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (departmentId == 0 || s.DepartmentId == departmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await db.DebitAndCreditNotifications.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (departmentId == 0 || s.DepartmentId == departmentId)).CountAsync();
            }
            else
            {
                debitAndCreditNotifications = db.DebitAndCreditNotifications.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.DebitAndCreditNotificationSourceType.ArName.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.Notes.Contains(searchWord) || s.Vendor.ArName.Contains(searchWord) || s.ChartOfAccount.ArName.Contains(searchWord) || s.Customer.ArName.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Techanician.ArName.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await db.DebitAndCreditNotifications.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.DebitAndCreditNotificationSourceType.ArName.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.Notes.Contains(searchWord) || s.Vendor.ArName.Contains(searchWord) || s.ChartOfAccount.ArName.Contains(searchWord) || s.Customer.ArName.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Techanician.ArName.Contains(searchWord))).CountAsync();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(await debitAndCreditNotifications.ToListAsync());
        }

        // GET: DebitAndCreditNotification/Edit/5
        public ActionResult AddEdit(int? id)
        {
            var subAccounts = db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && (c.ClassificationId == 3)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }).ToList();

            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            ViewBag.UseCostCenter = systemSetting.UseCostCenter;
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            List<int> year = new List<int>();

            if (id == null)
            {
                ViewBag.DebitId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.CreditId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.RepId = new SelectList(db.Employees.Where(b => b.IsDeleted == false && b.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.DebitAndCreditNotificationTypeId = new SelectList(db.DebitAndCreditNotificationTypes.Where(b => b.IsDeleted == false && b.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.TechnicianId = new SelectList(db.Techanicians.Where(b => b.IsDeleted == false && b.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.SourceTypeId = new SelectList(db.DebitAndCreditNotificationSourceTypes.Where(c => c.IsDeleted == false && c.IsActive == true && c.Id != 5), "Id", "ArName");
                ViewBag.VendorId = new SelectList(db.Vendors.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
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
                ViewBag.DirectRevenueId = new SelectList(db.DirectRevenues.Where(d => d.IsDeleted == false && d.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId).ToList(), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.CostCenterId = new SelectList(db.CostCenters.Where(a => a.IsActive && !a.IsDeleted && a.TypeId == 2).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

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
                ViewBag.ChildrenId = new SelectList(db.Children.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.ElderId = new SelectList(db.Elders.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.RenterId = new SelectList(db.PropertyRenters.Where(c => c.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
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
                return View();
            }
            int sysPageId = QueryHelper.SourcePageId("DebitAndCreditNotification");
            ViewBag.JE = db.GetJournalEntryBySourceIdAndSourcePageId(id, sysPageId).FirstOrDefault();
            if (ViewBag.JE != null)
            {
                var JEDocNo = ViewBag.JE;
                ViewBag.JE.DocumentNumber = JEDocNo != null ? JEDocNo.DocumentNumber.Replace("-", "") : "";
                int JEId = ViewBag.JE.Id;
                int sourcePageId = ViewBag.JE.SourcePageId;
                ViewBag.JEDetails = db.JournalEntryDetails.Where(a => a.SourceId == id && a.JournalEntryId == JEId && a.SourcePageId == sourcePageId).OrderBy(a => a.Id).ToList();

            }
            ViewBag.Accounts = new SelectList(db.ChartOfAccounts.Where(a => a.ClassificationId == 3 && a.IsActive == true && a.IsDeleted == false), "Id", "ArName");

            DebitAndCreditNotification debitAndCreditNotification = db.DebitAndCreditNotifications.Find(id);
            if (debitAndCreditNotification == null)
            {
                return HttpNotFound();
            }

            ViewBag.DebitId = new SelectList(subAccounts, "Id", "ArName", debitAndCreditNotification.DebitId);
            ViewBag.CreditId = new SelectList(subAccounts, "Id", "ArName", debitAndCreditNotification.CreditId);
            ViewBag.RepId = new SelectList(db.Employees.Where(b => b.IsDeleted == false && b.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", debitAndCreditNotification.RepId);
            ViewBag.DebitAndCreditNotificationTypeId = new SelectList(db.DebitAndCreditNotificationTypes.Where(b => b.IsDeleted == false && b.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", debitAndCreditNotification.DebitAndCreditNotificationTypeId);

            ViewBag.TechnicianId = new SelectList(db.Techanicians.Where(b => b.IsDeleted == false && b.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", debitAndCreditNotification.TechnicianId);


            ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", debitAndCreditNotification.AccountId);
            ViewBag.CurrencyId = new SelectList(db.Currencies.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", debitAndCreditNotification.CurrencyId);
            ViewBag.SourceTypeId = new SelectList(db.DebitAndCreditNotificationSourceTypes.Where(c => c.IsDeleted == false && c.IsActive == true && c.Id != 5), "Id", "ArName", debitAndCreditNotification.SourceTypeId);
            ViewBag.VendorId = new SelectList(db.Vendors.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", debitAndCreditNotification.VendorId);
            ViewBag.ShareholderId = new SelectList(db.ShareHolders.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", debitAndCreditNotification.ShareholderId);
            ViewBag.CustomerId = new SelectList(db.Customers.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", debitAndCreditNotification.CustomerId);
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(e => e.IsDeleted == false && e.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", debitAndCreditNotification.EmployeeId);

            ViewBag.DirectRevenueId = new SelectList(db.DirectRevenues.Where(d => d.IsDeleted == false && d.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", debitAndCreditNotification.DirectRevenueId);
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId).ToList(), "Id", "ArName", debitAndCreditNotification.DepartmentId);
            ViewBag.CostCenterId = new SelectList(db.CostCenters.Where(a => a.IsActive && !a.IsDeleted && a.TypeId == 2).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", debitAndCreditNotification.CostCenterId);
            ViewBag.ChildrenId = new SelectList(db.Children.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", debitAndCreditNotification.ChildrenId);
            ViewBag.ElderId = new SelectList(db.Elders.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", debitAndCreditNotification.ElderId);
            ViewBag.RenterId = new SelectList(db.PropertyRenters.Where(c => c.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", debitAndCreditNotification.RenterId);

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
                new { id=12,name="ديسمبر"}}, "id", "name", debitAndCreditNotification.Month);

            //year
            for (var i = 2019; i <= 2030; i++)
            {
                year.Add(i);
                ViewBag.Year = new SelectList(year, debitAndCreditNotification.Year);
            }
            ViewBag.Next = QueryHelper.Next((int)id, "DebitAndCreditNotification");
            ViewBag.Previous = QueryHelper.Previous((int)id, "DebitAndCreditNotification");
            ViewBag.Last = QueryHelper.GetLast("DebitAndCreditNotification");
            ViewBag.First = QueryHelper.GetFirst("DebitAndCreditNotification");
            ViewBag.Date = debitAndCreditNotification.Date.ToString("yyyy-MM-ddTHH:mm");


            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل الإشعارات المدينة و الدائنة",
                EnAction = "AddEdit",
                ControllerName = "DebitAndCreditNotification",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = debitAndCreditNotification.Id > 0 ? debitAndCreditNotification.Id : db.DebitAndCreditNotifications.Max(i => i.Id),
                CodeOrDocNo = debitAndCreditNotification.DocumentNumber
            });
            return View(debitAndCreditNotification);
        }

        [HttpPost]
        public ActionResult AddEdit(DebitAndCreditNotification debitAndCreditNotification)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            /*--- Document Coding ---*/
            var DocumentCoding = "";
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            if (systemSetting.DocumentCoding == true)
            {
                DocumentCoding = debitAndCreditNotification.DocumentNumber;
            }
            DocumentCoding = DocumentCoding.Length > 0 ? DocumentCoding : null;
            /*-------**************** End Of Document Coding *****************--------*/

            if (ModelState.IsValid)
            {
                var id = debitAndCreditNotification.Id;
                debitAndCreditNotification.IsDeleted = false;

                if (debitAndCreditNotification.SourceTypeId == 1) //Customer
                {
                    debitAndCreditNotification.ShareholderId = null;
                    debitAndCreditNotification.VendorId = null;
                    debitAndCreditNotification.EmployeeId = null;
                    debitAndCreditNotification.DirectRevenueId = null;
                    debitAndCreditNotification.TechnicianId = null;
                    debitAndCreditNotification.AccountId = null;
                    debitAndCreditNotification.ChildrenId = null;
                    debitAndCreditNotification.ElderId = null;
                    debitAndCreditNotification.RenterId = null;
                    debitAndCreditNotification.Month = null;
                    debitAndCreditNotification.Year = null;
                }
                else if (debitAndCreditNotification.SourceTypeId == 2) //Vendor
                {
                    debitAndCreditNotification.ShareholderId = null;
                    debitAndCreditNotification.TechnicianId = null;
                    debitAndCreditNotification.CustomerId = null;
                    debitAndCreditNotification.DirectRevenueId = null;
                    debitAndCreditNotification.EmployeeId = null;
                    debitAndCreditNotification.AccountId = null;
                    debitAndCreditNotification.ChildrenId = null;
                    debitAndCreditNotification.ElderId = null;
                    debitAndCreditNotification.RenterId = null;
                    debitAndCreditNotification.Month = null;
                    debitAndCreditNotification.Year = null;
                }
                else if (debitAndCreditNotification.SourceTypeId == 3) //Employee
                {
                    debitAndCreditNotification.ShareholderId = null;
                    debitAndCreditNotification.TechnicianId = null;
                    debitAndCreditNotification.DirectRevenueId = null;
                    debitAndCreditNotification.VendorId = null;
                    debitAndCreditNotification.CustomerId = null;
                    debitAndCreditNotification.AccountId = null;
                    debitAndCreditNotification.ChildrenId = null;
                    debitAndCreditNotification.ElderId = null;
                    debitAndCreditNotification.RenterId = null;
                    debitAndCreditNotification.Month = null;
                    debitAndCreditNotification.Year = null;
                }
                else if (debitAndCreditNotification.SourceTypeId == 4) //Direct Revenue
                {
                    debitAndCreditNotification.ShareholderId = null;
                    debitAndCreditNotification.TechnicianId = null;
                    debitAndCreditNotification.VendorId = null;
                    debitAndCreditNotification.CustomerId = null;
                    debitAndCreditNotification.EmployeeId = null;
                    debitAndCreditNotification.AccountId = null;
                    debitAndCreditNotification.ChildrenId = null;
                    debitAndCreditNotification.ElderId = null;
                    debitAndCreditNotification.RenterId = null;
                    debitAndCreditNotification.Month = null;
                    debitAndCreditNotification.Year = null;
                }
                else if (debitAndCreditNotification.SourceTypeId == 5) //Techinician
                {
                    debitAndCreditNotification.ShareholderId = null;
                    debitAndCreditNotification.VendorId = null;
                    debitAndCreditNotification.CustomerId = null;
                    debitAndCreditNotification.EmployeeId = null;
                    debitAndCreditNotification.DirectRevenueId = null;
                    debitAndCreditNotification.AccountId = null;
                    debitAndCreditNotification.ChildrenId = null;
                    debitAndCreditNotification.ElderId = null;
                    debitAndCreditNotification.RenterId = null;
                    debitAndCreditNotification.Month = null;
                    debitAndCreditNotification.Year = null;
                }
                else if (debitAndCreditNotification.SourceTypeId == 6) // Other
                {
                    debitAndCreditNotification.ShareholderId = null;
                    debitAndCreditNotification.TechnicianId = null;
                    debitAndCreditNotification.VendorId = null;
                    debitAndCreditNotification.CustomerId = null;
                    debitAndCreditNotification.EmployeeId = null;
                    debitAndCreditNotification.DirectRevenueId = null;
                    debitAndCreditNotification.ChildrenId = null;
                    debitAndCreditNotification.ElderId = null;
                    debitAndCreditNotification.RenterId = null;
                    debitAndCreditNotification.Month = null;
                    debitAndCreditNotification.Year = null;
                }
                else if (debitAndCreditNotification.SourceTypeId == 7) //ShareHolder
                {
                    debitAndCreditNotification.TechnicianId = null;
                    debitAndCreditNotification.VendorId = null;
                    debitAndCreditNotification.CustomerId = null;
                    debitAndCreditNotification.EmployeeId = null;
                    debitAndCreditNotification.DirectRevenueId = null;
                    debitAndCreditNotification.AccountId = null;
                    debitAndCreditNotification.ChildrenId = null;
                    debitAndCreditNotification.ElderId = null;
                    debitAndCreditNotification.RenterId = null;
                    debitAndCreditNotification.Month = null;
                    debitAndCreditNotification.Year = null;
                }
                else if (debitAndCreditNotification.SourceTypeId == 8) //Elder
                {
                    debitAndCreditNotification.TechnicianId = null;
                    debitAndCreditNotification.VendorId = null;
                    debitAndCreditNotification.CustomerId = null;
                    debitAndCreditNotification.EmployeeId = null;
                    debitAndCreditNotification.DirectRevenueId = null;
                    debitAndCreditNotification.AccountId = null;
                    debitAndCreditNotification.ShareholderId = null;
                    debitAndCreditNotification.ChildrenId = null;
                    debitAndCreditNotification.RenterId = null;
                }
                else if (debitAndCreditNotification.SourceTypeId == 9) //Children
                {
                    debitAndCreditNotification.TechnicianId = null;
                    debitAndCreditNotification.VendorId = null;
                    debitAndCreditNotification.CustomerId = null;
                    debitAndCreditNotification.EmployeeId = null;
                    debitAndCreditNotification.DirectRevenueId = null;
                    debitAndCreditNotification.AccountId = null;
                    debitAndCreditNotification.ShareholderId = null;
                    debitAndCreditNotification.ElderId = null;
                    debitAndCreditNotification.RenterId = null;
                    debitAndCreditNotification.Month = null;
                    debitAndCreditNotification.Year = null;
                }
                else if (debitAndCreditNotification.SourceTypeId == 10) //Renter
                {
                    debitAndCreditNotification.TechnicianId = null;
                    debitAndCreditNotification.VendorId = null;
                    debitAndCreditNotification.CustomerId = null;
                    debitAndCreditNotification.EmployeeId = null;
                    debitAndCreditNotification.DirectRevenueId = null;
                    debitAndCreditNotification.AccountId = null;
                    debitAndCreditNotification.ShareholderId = null;
                    debitAndCreditNotification.ElderId = null;
                    debitAndCreditNotification.ChildrenId = null;
                    debitAndCreditNotification.Month = null;
                    debitAndCreditNotification.Year = null;
                }


                if (debitAndCreditNotification.Id > 0)
                {
                    if (db.DebitAndCreditNotifications.Find(debitAndCreditNotification.Id).IsPosted == true)
                    {
                        return Content("false");
                    }
                    debitAndCreditNotification.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.DebitAndCreditNotifi_Update(debitAndCreditNotification.Id, debitAndCreditNotification.DocumentNumber,
debitAndCreditNotification.Date,
debitAndCreditNotification.DepartmentId,
debitAndCreditNotification.DebitAndCreditNotificationTypeId,
debitAndCreditNotification.SourceTypeId,
debitAndCreditNotification.RepId,
debitAndCreditNotification.TotalMoneyAmount,
debitAndCreditNotification.MoneyAmount,
debitAndCreditNotification.VATValue,
debitAndCreditNotification.VATPercentage,
debitAndCreditNotification.DebitId,
debitAndCreditNotification.CreditId,
debitAndCreditNotification.CurrencyId,
debitAndCreditNotification.CurrencyEquivalent,
debitAndCreditNotification.IsLinked,
debitAndCreditNotification.IsPosted,
debitAndCreditNotification.IsActive,
debitAndCreditNotification.IsDeleted,
debitAndCreditNotification.UserId,
debitAndCreditNotification.Notes,
debitAndCreditNotification.Image,
debitAndCreditNotification.CustomerId,
debitAndCreditNotification.VendorId,
debitAndCreditNotification.EmployeeId,
debitAndCreditNotification.TechnicianId,
debitAndCreditNotification.DirectRevenueId,
debitAndCreditNotification.ShareholderId,
debitAndCreditNotification.AccountId,
debitAndCreditNotification.CostCenterId,
debitAndCreditNotification.Month,
debitAndCreditNotification.Year,
debitAndCreditNotification.ChildrenId,
debitAndCreditNotification.ElderId,
debitAndCreditNotification.InvoiceNo,
debitAndCreditNotification.RenterId
);
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("DebitAndCreditNotification", "Edit", "AddEdit", id, null, " الإشعارات المدينة و الدائنة");
                    ////////////////-----------------------------------------------------------------------

                }
                else
                {
                    debitAndCreditNotification.IsActive = true;
                    debitAndCreditNotification.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    var idResult = new ObjectParameter("Id", typeof(Int32));

                    db.DebitAndCreditNotifi_Insert(idResult,
debitAndCreditNotification.Date,
debitAndCreditNotification.DepartmentId,
debitAndCreditNotification.DebitAndCreditNotificationTypeId,
debitAndCreditNotification.SourceTypeId,
debitAndCreditNotification.RepId,
debitAndCreditNotification.TotalMoneyAmount,
debitAndCreditNotification.MoneyAmount,
debitAndCreditNotification.VATValue,
debitAndCreditNotification.VATPercentage,
debitAndCreditNotification.DebitId,
debitAndCreditNotification.CreditId,
debitAndCreditNotification.CurrencyId,
debitAndCreditNotification.CurrencyEquivalent,
debitAndCreditNotification.IsLinked,
debitAndCreditNotification.IsPosted,
debitAndCreditNotification.IsActive,
debitAndCreditNotification.IsDeleted,
debitAndCreditNotification.UserId,
debitAndCreditNotification.Notes,
debitAndCreditNotification.Image,
debitAndCreditNotification.CustomerId,
debitAndCreditNotification.VendorId,
debitAndCreditNotification.EmployeeId,
debitAndCreditNotification.TechnicianId,
debitAndCreditNotification.DirectRevenueId,
debitAndCreditNotification.ShareholderId,
debitAndCreditNotification.AccountId,
debitAndCreditNotification.CostCenterId,
debitAndCreditNotification.Month,
debitAndCreditNotification.Year,
debitAndCreditNotification.ChildrenId,
debitAndCreditNotification.ElderId,
debitAndCreditNotification.InvoiceNo,
debitAndCreditNotification.RenterId
);
                    id = (int)idResult.Value;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("DebitAndCreditNotification", "Add", "AddEdit", debitAndCreditNotification.Id, null, "الإشعارات المدينة و الدائنة");
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = debitAndCreditNotification.Id > 0 ? "تعديل سند قبض" : "اضافة سند قبض",
                    EnAction = "AddEdit",
                    ControllerName = "DebitAndCreditNotification",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = debitAndCreditNotification.DocumentNumber
                });

                return Json(new { success = "true", id });
            }
            else
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
                return Json(new { success = "false", errors });
            }

        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                DebitAndCreditNotification debitAndCreditNotification = db.DebitAndCreditNotifications.Find(id);
                if (debitAndCreditNotification.IsPosted == true)
                {
                    return Content("false");
                }
                debitAndCreditNotification.IsDeleted = true;
                debitAndCreditNotification.UserId = userId;
                db.Entry(debitAndCreditNotification).State = EntityState.Modified;
                db.Database.ExecuteSqlCommand($"update JournalEntry set IsDeleted = 1, UserId={userId} where SourcePageId = (select Id from SystemPage where TableName = 'DebitAndCreditNotification') and SourceId = {id}; update JournalEntryDetail set IsDeleted = 1 where JournalEntryId = (select Id from JournalEntry where SourcePageId = (select Id from SystemPage where TableName = 'DebitAndCreditNotification') and SourceId = {id})");
                db.SaveChanges();

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف سند قبض",
                    EnAction = "AddEdit",
                    ControllerName = "DebitAndCreditNotification",
                    UserName = User.Identity.Name,
                    UserId = userId,
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = debitAndCreditNotification.Id,
                    CodeOrDocNo = debitAndCreditNotification.DocumentNumber
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("DebitAndCreditNotification", "Delete", "Delete", id, null, "الإشعارات المدينة و الدائنة");

                ///////////////-----------------------------------------------------------------------

                return Content("true");
            }
            catch (Exception)
            {
                throw;
            }

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
            var lastObj = db.DebitAndCreditNotifications
                .Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id)
                .OrderByDescending(a => a.Id).FirstOrDefault();
            var lastDocNo = lastObj != null ? lastObj.DocumentNumber : "0";
            var DepartmentCode = db.Departments
                .Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == id)
                .FirstOrDefault().Code;
            DepartmentCode = double.Parse(DepartmentCode) < 10 ? "0" + DepartmentCode : DepartmentCode;

            var DepartmentDoc = db.DocumentsCodings
                .Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id)
                .FirstOrDefault();

            var MonthFormat = VoucherDate.Value.Month < 10
                ? "0" + VoucherDate.Value.Month.ToString()
                : VoucherDate.Value.Month.ToString();

            if (DepartmentDoc == null)
            {
                DepartmentDoc = db.DocumentsCodings
                    .Where(a => a.IsActive == true && a.IsDeleted == false && a.AllDepartments == true)
                    .FirstOrDefault();
                IsExistInDocumentsCoding = DepartmentDoc != null;
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
                YearFormat = YearFormat == 2
                    ? int.Parse(VoucherDate.Value.Year.ToString().Substring(2, 2))
                    : int.Parse(VoucherDate.Value.Year.ToString());

                if (CodingTypeId == 1) // آلي
                {
                    if (lastDocNo.Contains("-"))
                    {
                        var ar = lastDocNo.Split('-');
                        newDocNo = IsZerosFills == true
                            ? QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(ar[3]) + 1).ToString())
                            : (double.Parse(ar[3]) + 1).ToString();
                    }
                    else
                    {
                        newDocNo = IsZerosFills == true
                            ? QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(lastDocNo) + 1).ToString())
                            : (double.Parse(lastDocNo) + 1).ToString();
                    }
                    GeneratedDocNo = DepartmentCode + "-" + YearFormat + "-" + MonthFormat + "-" + newDocNo;
                }
                else if (CodingTypeId == 2) // متصل شهري
                {
                    lastObj = db.DebitAndCreditNotifications
                        .Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id
                                    && a.Date.Month == VoucherDate.Value.Month && a.Date.Year == VoucherDate.Value.Year)
                        .OrderByDescending(a => a.Id).FirstOrDefault();

                    if (lastObj != null)
                    {
                        if (lastObj.DocumentNumber.Contains("-"))
                        {
                            var ar = lastObj.DocumentNumber.Split('-');
                            if (double.Parse(ar[2]) == VoucherDate.Value.Month)
                            {
                                newDocNo = IsZerosFills == true
                                    ? QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(ar[3]) + 1).ToString())
                                    : (double.Parse(ar[3]) + 1).ToString();
                            }
                            else
                            {
                                newDocNo = IsZerosFills == true
                                    ? QueryHelper.FillsWithZeros(noOfDigits, "1").ToString()
                                    : "1";
                            }
                        }
                        else
                        {
                            newDocNo = IsZerosFills == true
                                ? QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(lastObj.DocumentNumber) + 1).ToString()).ToString()
                                : (double.Parse(lastObj.DocumentNumber) + 1).ToString();
                        }
                    }
                    else
                    {
                        newDocNo = IsZerosFills == true
                            ? QueryHelper.FillsWithZeros(noOfDigits, "1").ToString()
                            : "1";
                    }

                    GeneratedDocNo = DepartmentCode + "-" + YearFormat + "-" + MonthFormat + "-" + newDocNo;
                }
                else if (CodingTypeId == 3) // متصل سنوي
                {
                    lastObj = db.DebitAndCreditNotifications
                        .Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id
                                    && (a.Date.Year == VoucherDate.Value.Year))
                        .OrderByDescending(a => a.Id).FirstOrDefault();

                    if (lastObj != null)
                    {
                        if (lastObj.DocumentNumber.Contains("-"))
                        {
                            var ar = lastObj.DocumentNumber.Split('-');
                            var VoucherDateFormate = int.Parse(ar[1]).ToString().Length == 2
                                ? int.Parse((VoucherDate.Value.Year.ToString()).Substring(2, 2))
                                : VoucherDate.Value.Year;

                            if (double.Parse(ar[1]) == VoucherDateFormate)
                            {
                                newDocNo = IsZerosFills == true
                                    ? QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(ar[3]) + 1).ToString())
                                    : (double.Parse(ar[3]) + 1).ToString();
                            }
                            else
                            {
                                newDocNo = IsZerosFills == true
                                    ? QueryHelper.FillsWithZeros(noOfDigits, "1").ToString()
                                    : "1";
                            }
                        }
                        else
                        {
                            newDocNo = IsZerosFills == true
                                ? QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(lastObj.DocumentNumber) + 1).ToString()).ToString()
                                : (double.Parse(lastObj.DocumentNumber) + 1).ToString();
                        }
                    }
                    else
                    {
                        newDocNo = IsZerosFills == true
                            ? QueryHelper.FillsWithZeros(noOfDigits, "1").ToString()
                            : "1";
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
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // ← دي الصحيحة (بدون false)



        public ActionResult ImportExcel(HttpPostedFileBase file, int? departmentId, DateTime? dateOverride)
        {
            try
            {
                if (file == null || file.ContentLength == 0)
                    return Json(new { success = false, message = "يرجى اختيار ملف إكسل." });

                using (var stream = file.InputStream)
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var ds = reader.AsDataSet(new ExcelDataSetConfiguration
                    {
                        ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
                    });

                    if (ds.Tables.Count == 0)
                        return Json(new { success = false, message = "لا توجد أوراق عمل في الملف." });

                    var dt = ds.Tables[0];
                    Func<string, bool> has = col => dt.Columns.Contains(col);

                    // أعمدة أساسية مطلوبة
                    string[] required = { "InvoiceDate", "CustomerName", "MoneyAmount", "VATPercentage", "TotalMoneyAmount", "NoteTypeText", "PartyType" };
                    foreach (var c in required)
                        if (!has(c)) return Json(new { success = false, message = $"العمود '{c}' مفقود في الشيت." });

                    // أعمدة اختيارية: InvoiceNo, Notes
                    bool hasInvoiceNoCol = has("InvoiceNo");
                    bool hasNotesCol = has("Notes");

                    // المستخدم الحالي
                    var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    // أول حساب فعّال للاحتياط
                    int anyActiveAccountId = db.ChartOfAccounts
                        .Where(a => (a.IsActive ?? false) && !(a.IsDeleted ?? false))
                        .Select(a => a.Id).FirstOrDefault();

                    // العملة الافتراضية
                    int? defaultCurrencyId = db.Currencies
                        .Where(a => a.IsDefault == true && (a.IsDeleted == false || a.IsDeleted == null) && (a.IsActive == true || a.IsActive == null))
                        .Select(a => (int?)a.Id).FirstOrDefault();

                    // اتصال SQL خام للـ lookup
                    var cn = (SqlConnection)db.Database.Connection;
                    bool needClose = false;
                    if (cn.State != ConnectionState.Open) { cn.Open(); needClose = true; }

                    int inserted = 0, skipped = 0;
                    var errors = new List<RowError>();

                    for (int r = 0; r < dt.Rows.Count; r++)
                    {
                        // هنحتاج القيم دي في الـcatch
                        string invoiceNoFromSheet = null, partyName = null, noteTypeText = null;
                        decimal amount = 0m, total = 0m;
                        double vatPerc = 0d;

                        try
                        {
                            var row = dt.Rows[r];

                            // ===== 1) التاريخ =====
                            DateTime date = dateOverride ?? DateTime.Now;
                            if (row["InvoiceDate"] != DBNull.Value)
                            {
                                if (!DateTime.TryParse(row["InvoiceDate"] + "", out date))
                                    date = dateOverride ?? DateTime.Now;
                            }

                            // ===== 2) القسم =====
                            int depId = departmentId ?? 0;
                            if (depId == 0)
                                throw new Exception("يجب اختيار القسم من المودال (لا يوجد DepartmentId افتراضي).");

                            var depInfo = db.Departments.Where(d => d.Id == depId)
                                         .Select(d => new
                                         {
                                             d.CustomersAccountId,
                                             d.RenterAndBuyerAccountId
                                         }).FirstOrDefault();
                            if (depInfo == null) throw new Exception("القسم غير موجود.");

                            // ===== 3) نوع الإشعار =====
                            noteTypeText = (row["NoteTypeText"] + "").Trim();
                            int typeId; // 4 = مدين، 5 = دائن
                            if (noteTypeText.Contains("خصم"))
                                typeId = 5;   // Credit Note
                            else if (noteTypeText.Contains("ضاف") || noteTypeText.Contains("إضاف") || noteTypeText.Contains("اضاف"))
                                typeId = 4;   // Debit Note
                            else
                                throw new Exception("NoteTypeText يجب أن يكون 'خصم' أو 'إضافة'.");

                            // ===== 4) الجهة (Party) =====
                            int partyType = 0; int.TryParse(row["PartyType"] + "", out partyType);
                            partyName = (row["CustomerName"] + "").Trim();

                            int sourceTypeId = 0;
                            int? customerId = null, renterId = null;

                            if (partyType == 1)
                            {
                                sourceTypeId = 1; // عميل
                                customerId = LookupIdByNameSmart(cn, "Customer", partyName);
                                if (customerId == 0) throw new Exception($"لم يتم العثور على العميل بالاسم: {partyName}");
                            }
                            else if (partyType == 4)
                            {
                                sourceTypeId = 10; // مستأجر/مشتري
                                renterId = LookupIdByNameSmart(cn, "PropertyRenter", partyName);
                                //if (renterId == 0) throw new Exception($"لم يتم العثور على المستأجر بالاسم: {partyName}");
                                if (renterId == 0)
                                {
#if DEBUG
                                    System.Diagnostics.Debugger.Break(); // 🔴 يوقف التنفيذ عند هذا السطر فقط في وضع Debug
#endif
                                    throw new Exception($"لم يتم العثور على المستأجر بالاسم: {partyName}");
                                }

                            }
                            else
                            {
                                throw new Exception("PartyType غير مدعوم في هذا الاستيراد (المسموح: 1 عميل، 4 مستأجر).");
                            }

                            // ===== 5) المبالغ =====
                            decimal.TryParse(row["MoneyAmount"] + "", out amount);
                            decimal.TryParse(row["TotalMoneyAmount"] + "", out total);
                            double.TryParse(row["VATPercentage"] + "", out vatPerc);

                            decimal vatValue = 0m;

                            if (total == 0 && amount > 0 && vatPerc > 0)
                                total = amount + (amount * (decimal)vatPerc / 100m);

                            if (total >= amount && amount > 0)
                                vatValue = total - amount;

                            // ===== 6) العملة =====
                            int currencyId = defaultCurrencyId ?? db.Currencies.OrderByDescending(c => c.IsDefault).Select(c => c.Id).FirstOrDefault();
                            double currEq = 1;

                            // ===== 7) Fallback Accounts =====
                            int creditIdFallback =
                                depInfo.CustomersAccountId
                                ?? depInfo.RenterAndBuyerAccountId
                                ?? anyActiveAccountId;

                            int debitIdFallback =
                                depInfo.RenterAndBuyerAccountId
                                ?? depInfo.CustomersAccountId
                                ?? anyActiveAccountId;

                            if (creditIdFallback == 0 || debitIdFallback == 0)
                                throw new Exception("تعذّر تعيين حسابات fallback (تحقّق من إعدادات القسم والحسابات).");

                            // ===== 8) ملاحظات — ضمّ InvoiceNo (اختياري) =====
                            invoiceNoFromSheet = hasInvoiceNoCol ? (row["InvoiceNo"] + "").Trim() : null;
                            string sheetNotes = hasNotesCol ? (row["Notes"] + "").Trim() : null;

                            string notes = null;
                            if (!string.IsNullOrEmpty(invoiceNoFromSheet) || !string.IsNullOrEmpty(sheetNotes))
                            {
                                var parts = new List<string>();
                                if (!string.IsNullOrEmpty(invoiceNoFromSheet)) parts.Add($"InvoiceNo: {invoiceNoFromSheet}");
                                if (!string.IsNullOrEmpty(sheetNotes)) parts.Add(sheetNotes);
                                notes = string.Join(" | ", parts);
                            }

                            // ===== 9) استدعاء الستورد =====
                            var idOut = new ObjectParameter("Id", typeof(Int32));

                            db.DebitAndCreditNotifi_Insert(
                                idOut,
                                date,
                                depId,
                                typeId,
                                sourceTypeId,
                                repId: (int?)null,
                                total,
                                amount,
                                vatValue,
                                vatPerc,
                                debitIdFallback,
                                creditIdFallback,
                                currencyId,
                                currEq,
                                false,   // IsLinked
                                false,   // IsPosted
                                true,    // IsActive
                                false,   // IsDeleted
                                userId,
                                notes,
                                null,    // Image
                                customerId, null, null, null, null,
                                null, null, (int?)null, (int?)null, (int?)null,
                                null, null, (string)null, renterId
                            );

                            inserted++;
                        }
                        catch (Exception exRow)
                        {
                            skipped++;
                            errors.Add(new RowError
                            {
                                Row = r + 2,
                                InvoiceNo = invoiceNoFromSheet,
                                PartyName = partyName,
                                NoteTypeText = noteTypeText,
                                Amount = amount,
                                VATPerc = vatPerc,
                                Total = total,
                                Message = exRow.Message
                            });
                        }
                    }

                    // === نهاية ImportExcel بعد انتهاء الـ for وقبل الخروج من using(...) ===
                    if (needClose) cn.Close();

                    // 1) اكتب ملف اللوج لو فيه أخطاء
                    var logUrlRel = ImportLogWriter.WriteCsv(
                        errors,
                        title: $"Import result on {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Inserted: {inserted} | Skipped: {skipped}"
                    );

                    // 2) ابنِ رابط تحميل مباشر (مطلق) لو الملف موجود
                    string logDownloadUrl = null;
                    if (!string.IsNullOrEmpty(logUrlRel))
                    {
                        // WriteCsv بيرجع "/Uploads/ImportLogs/xxx.csv" ⇒ هنستخرج اسم الملف ونبني اكشن التحميل
                        var fileName = System.IO.Path.GetFileName(logUrlRel);
                        logDownloadUrl = Url.Action(
                            "DownloadImportLog",
                            "DebitAndCreditNotification",
                            new { file = fileName },
                            protocol: Request.Url.Scheme
                        );
                    }

                    // 3) رجّع النتيجة — مافيش return تاني بعدها
                    return Json(new
                    {
                        success = true,
                        inserted,
                        skipped,
                        errors = (object)null,   // مش هنرجّع التفاصيل هنا
                        logUrl = logDownloadUrl  // لو null يبقى مفيش ملف لوج
                    }, JsonRequestBehavior.AllowGet);

                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public ActionResult DownloadImportLog(string file)
        {
            if (string.IsNullOrWhiteSpace(file)) return HttpNotFound();

            // امنع التلاعب بالمسار: خذ الاسم فقط
            var safeName = Path.GetFileName(file);

            var root = Server.MapPath("~/Uploads/ImportLogs");
            var full = Path.Combine(root, safeName);

            if (!System.IO.File.Exists(full)) return HttpNotFound();

            return File(full, "text/csv", safeName); // attachment; filename=safeName
        }


        // ======================
        // Lookup ذكي للاسم (SQL)
        // ======================
        //        private static int LookupIdByNameSmart(SqlConnection cn, string table, string personName)
        //        {
        //            string sql = $@"
        //DECLARE @hasFn BIT = CASE WHEN OBJECT_ID('dbo.fn_NormalizeArabic','FN') IS NULL THEN 0 ELSE 1 END;
        //DECLARE @n NVARCHAR(400) = @name;

        //IF (@hasFn = 1)
        //BEGIN
        //    DECLARE @q NVARCHAR(400) = dbo.fn_NormalizeArabic(@n);

        //    ;WITH cand AS (
        //      SELECT TOP 10 Id, ArName,
        //             N = dbo.fn_NormalizeArabic(ArName),
        //             Score = CASE 
        //                       WHEN dbo.fn_NormalizeArabic(ArName) = @q THEN 100
        //                       WHEN dbo.fn_NormalizeArabic(ArName) LIKE (@q + N'%') THEN 80
        //                       WHEN dbo.fn_NormalizeArabic(ArName) LIKE (N'%' + @q + N'%') THEN 60
        //                       ELSE 0
        //                     END,
        //             LenDiff = ABS(LEN(dbo.fn_NormalizeArabic(ArName)) - LEN(@q))
        //      FROM dbo.{table} WITH (NOLOCK)
        //    )
        //    SELECT TOP 1 Id FROM cand WHERE Score > 0
        //    ORDER BY Score DESC, LenDiff ASC, LEN(ArName) ASC;
        //END
        //ELSE
        //BEGIN
        //    DECLARE @q NVARCHAR(400) = @n;
        //    SET @q = REPLACE(@q, N'أ', N'ا'); SET @q = REPLACE(@q, N'إ', N'ا'); SET @q = REPLACE(@q, N'آ', N'ا');
        //    SET @q = REPLACE(@q, N'ؤ', N'و'); SET @q = REPLACE(@q, N'ئ', N'ي'); SET @q = REPLACE(@q, N'ى', N'ي'); SET @q = REPLACE(@q, N'ة', N'ه');

        //    ;WITH cand AS (
        //      SELECT TOP 10 Id, ArName,
        //             N = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(ArName,N'أ',N'ا'),N'إ',N'ا'),N'آ',N'ا'),N'ؤ',N'و'),N'ئ',N'ي'),N'ى',N'ي'),N'ة',N'ه'),
        //             Score = CASE 
        //                       WHEN REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(ArName,N'أ',N'ا'),N'إ',N'ا'),N'آ',N'ا'),N'ؤ',N'و'),N'ئ',N'ي'),N'ى',N'ي'),N'ة',N'ه') = @q THEN 100
        //                       WHEN REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(ArName,N'أ',N'ا'),N'إ',N'ا'),N'آ',N'ا'),N'ؤ',N'و'),N'ئ',N'ي'),N'ى',N'ي'),N'ة',N'ه') LIKE (@q + N'%') THEN 80
        //                       WHEN REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(ArName,N'أ',N'ا'),N'إ',N'ا'),N'آ',N'ا'),N'ؤ',N'و'),N'ئ',N'ي'),N'ى',N'ي'),N'ة',N'ه') LIKE (N'%' + @q + N'%') THEN 60
        //                       ELSE 0
        //                     END,
        //             LenDiff = ABS(LEN(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(ArName,N'أ',N'ا'),N'إ',N'ا'),N'آ',N'ا'),N'ؤ',N'و'),N'ئ',N'ي'),N'ى',N'ي'),N'ة',N'ه')) - LEN(@q))
        //      FROM dbo.{table} WITH (NOLOCK)
        //    )
        //    SELECT TOP 1 Id FROM cand WHERE Score > 0
        //    ORDER BY Score DESC, LenDiff ASC, LEN(ArName) ASC;
        //END
        //";
        //            using (var cmd = new SqlCommand(sql, cn))
        //            {
        //                cmd.Parameters.AddWithValue("@name", (object)personName ?? DBNull.Value);
        //                var o = cmd.ExecuteScalar();
        //                return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
        //            }
        //        }

        //        static int LookupIdByNameSmart(SqlConnection cn, string table, string rawNameOrCode)
        //        {
        //            if (string.IsNullOrWhiteSpace(rawNameOrCode)) return 0;

        //            // 1) تنظيف/توحيد عربي: حذف مسافات زائدة + توحيد أشكال الحروف
        //            string s = NormalizeArabic(rawNameOrCode);

        //            // 2) لو كله أرقام → جرّب كـ Code مباشرة
        //            bool isNumeric = s.All(ch => char.IsDigit(ch));
        //            string sql = $@"
        //;WITH C AS
        //(
        //    SELECT TOP (1) Id
        //    FROM dbo.{table} WITH (NOLOCK)
        //    WHERE IsNull(IsDeleted,0)=0
        //      AND IsNull(IsActive,1)=1
        //      AND (
        //            {(isNumeric ? "Code = @S OR" : "")}
        //            ArName = @S OR EnName = @S
        //          )
        //    ORDER BY Id DESC
        //)
        //SELECT Id FROM C;

        //IF @@ROWCOUNT = 0
        //BEGIN
        //    SELECT TOP (1) Id
        //    FROM dbo.{table} WITH (NOLOCK)
        //    WHERE IsNull(IsDeleted,0)=0
        //      AND IsNull(IsActive,1)=1
        //      AND (
        //            {(isNumeric ? "Code = @S OR" : "")}
        //            ArName LIKE @SLike OR EnName LIKE @SLike
        //          )
        //    ORDER BY Id DESC;
        //END
        //";

        //            using (var cmd = new SqlCommand(sql, cn))
        //            {
        //                cmd.Parameters.AddWithValue("@S", s);
        //                cmd.Parameters.AddWithValue("@SLike", "%" + s + "%");
        //                object o = cmd.ExecuteScalar();
        //                return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
        //            }

        //            // ============== Local helpers ==============
        //            string NormalizeArabic(string x)
        //            {
        //                x = x.Trim();
        //                x = System.Text.RegularExpressions.Regex.Replace(x, @"\s+", " "); // collapse spaces

        //                // توحيد أشكال الحروف الشائعة
        //                x = x.Replace('أ', 'ا').Replace('إ', 'ا').Replace('آ', 'ا');
        //                x = x.Replace('ى', 'ي').Replace('ئ', 'ي').Replace('ي', 'ي'); // ياء موحّدة
        //                x = x.Replace('ة', 'ه'); // اختياري: لو بياناتك مخلوطة بين (ة/ه)
        //                x = x.Replace('ؤ', 'و');

        //                // شيل التشكيل/المد
        //                x = new string(x.Where(ch =>
        //                    !"ًٌٍَُِّْـ".Contains(ch) // حركات وتطويل
        //                ).ToArray());

        //                // شيل رموز خفية/غير مرئية محتمَلة من الإكسل
        //                x = new string(x.Where(ch => ch == ' ' || !char.IsControl(ch)).ToArray());

        //                return x;
        //            }
        //        }

        static int LookupIdByNameSmart(SqlConnection cn, string table, string rawNameOrCode)
        {
            if (string.IsNullOrWhiteSpace(rawNameOrCode)) return 0;

            // 1. C# Normalization (This is correct)
            string s = NormalizeArabic(rawNameOrCode);

            // 2. SQL Query (Updated Scoring Logic)
            string sql = $@"
DECLARE @q NVARCHAR(200) = @S;

;WITH base AS (
    SELECT Id, ArName
    FROM dbo.{table} WITH (NOLOCK)
    WHERE ISNULL(IsDeleted,0)=0 AND ISNULL(IsActive,1)=1
),
cand AS (
    SELECT TOP 10 Id,
           ArName,
           Score =
             CASE 
               WHEN dbo.fn_NormalizeArabic(ArName) = @q THEN 100                  -- 1. Exact Match
               WHEN dbo.fn_NormalizeArabic(ArName) LIKE (@q + N'%') THEN 80      -- 2. Starts With Match
               ELSE 60                                                          -- 3. Contains Match (guaranteed by WHERE)
             END
    FROM base
    WHERE dbo.fn_NormalizeArabic(ArName) LIKE (N'%' + @q + N'%') -- Filter only 'Contains' matches
)
SELECT TOP 1 Id FROM cand 
ORDER BY Score DESC, LEN(ArName);
";

            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@S", s);
                var o = cmd.ExecuteScalar();
                return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            }

            // This local C# function is correct and matches the updated SQL function
            string NormalizeArabic(string x)
            {
                x = x.Trim();
                x = System.Text.RegularExpressions.Regex.Replace(x, @"\s+", " "); // دمج المسافات
                x = x.Replace('أ', 'ا').Replace('إ', 'ا').Replace('آ', 'ا');
                x = x.Replace('ى', 'ي').Replace('ئ', 'ي').Replace('ؤ', 'و');
               // x = x.Replace('ة', 'ه');
                x = new string(x.Where(ch => !"\u064B\u064C\u064D\u064E\u064F\u0650\u0651\u0652\u0640".Contains(ch)).ToArray());

                return x;
            }
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


public static class ImportLogWriter
{
    public static string WriteCsv(IEnumerable<RowError> errors, string title = null)
    {
        if (errors == null || !errors.Any()) return null;

        var root = HttpContext.Current.Server.MapPath("~/Uploads/ImportLogs");
        if (!Directory.Exists(root)) Directory.CreateDirectory(root);

        var fileName = $"DebitCreditImport_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.csv";
        var fullPath = Path.Combine(root, fileName);

        using (var sw = new StreamWriter(fullPath, false, Encoding.UTF8))
        {
            if (!string.IsNullOrWhiteSpace(title))
                sw.WriteLine(title.Replace(Environment.NewLine, " ").Trim());

            sw.WriteLine("Row,InvoiceNo,PartyName,NoteTypeText,Amount,VATPerc,Total,Message");
            foreach (var e in errors)
            {
                string q(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
                sw.WriteLine(string.Join(",",
                    e.Row.ToString(),
                    q(e.InvoiceNo),
                    q(e.PartyName),
                    q(e.NoteTypeText),
                    e.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    e.VATPerc.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    e.Total.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    q(e.Message)
                ));
            }
        }

        return $"/Uploads/ImportLogs/{fileName}";
    }
}


public class RowError
{
    public int Row { get; set; }
    public string InvoiceNo { get; set; }
    public string PartyName { get; set; }
    public string NoteTypeText { get; set; }
    public decimal Amount { get; set; }
    public double VATPerc { get; set; }
    public decimal Total { get; set; }
    public string Message { get; set; }
}
