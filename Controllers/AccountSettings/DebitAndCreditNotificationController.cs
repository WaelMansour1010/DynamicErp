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
            var lastObj = db.DebitAndCreditNotifications.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.DebitAndCreditNotifications.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Month == VoucherDate.Value.Month && a.Date.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.DebitAndCreditNotifications.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && (a.Date.Year == VoucherDate.Value.Year)).OrderByDescending(a => a.Id).FirstOrDefault();

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