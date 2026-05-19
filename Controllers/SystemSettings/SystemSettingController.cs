using MyERP.Models;
using MyERP.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers.SystemSettings
{
    public class SystemSettingController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public ActionResult Index()
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            ViewBag.CanEditZatcaLink = userId == 1;

            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();

            if (userId == 1)
            {
                ViewBag.DefaultDepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.DefaultCashBoxId = new SelectList(db.CashBoxes.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName", systemSetting.DefaultCashBoxId);
                ViewBag.DefaultWarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false && b.DepartmentId == systemSetting.DefaultDepartmentId).Select(b => new
                {
                    Id = b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName", systemSetting.DefaultWarehouseId);
            }
            else
            {
                ViewBag.DefaultDepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId && d.Department.IsDeleted == false && d.Department.IsActive == true && d.Privilege == true).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = session.ToString() == "en" && b.Department.EnName != null ? b.Department.Code + " - " + b.Department.EnName : b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.DefaultCashBoxId = new SelectList(db.UserCashBoxes.Where(d => d.UserId == userId && d.CashBox.IsDeleted == false && d.CashBox.IsActive == true && d.Privilege == true).Select(b => new
                {
                    Id = b.CashBoxId,
                    ArName = session.ToString() == "en" && b.CashBox.EnName != null ? b.CashBox.Code + " - " + b.CashBox.EnName : b.CashBox.Code + " - " + b.CashBox.ArName
                }), "Id", "ArName", systemSetting.DefaultCashBoxId);
                ViewBag.DefaultWarehouseId = new SelectList(db.UserWareHouses.Where(d => d.UserId == userId && d.Warehouse.IsDeleted == false && d.Warehouse.IsActive == true && d.Privilege == true && d.Warehouse.DepartmentId == systemSetting.DefaultDepartmentId).Select(b => new
                {
                    Id = b.WareHouseId,
                    ArName = session.ToString() == "en" && b.Warehouse.EnName != null ? b.Warehouse.Code + " - " + b.Warehouse.EnName : b.Warehouse.Code + " - " + b.Warehouse.ArName
                }), "Id", "ArName", systemSetting.DefaultWarehouseId);
            }
            ViewBag.DefaultBankId = new SelectList(db.Banks.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }), "Id", "ArName", systemSetting.DefaultBankId);
            systemSetting.ServiceFeesPercentage = systemSetting.ServiceFeesPercentage.HasValue ? systemSetting.ServiceFeesPercentage * 100 : 0;

            ViewBag.CountryId = new SelectList(db.Countries.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }), "Id", "ArName", systemSetting.CountryId);
            ViewBag.CityId = new SelectList(db.Cities.Where(a => (a.IsActive == true && a.IsDeleted == false && a.CountryId == systemSetting.CountryId)).Select(b => new
            {
                Id = b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }), "Id", "ArName", systemSetting.CityId);
            ViewBag.DocumentTypeId = new SelectList(db.SystemPages.Where(a => (a.IsActive == true && a.IsDeleted == false && a.IsTransaction == true)).Select(b => new
            {
                Id = b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }), "Id", "ArName", systemSetting.DocumentTypeId);
            return View(systemSetting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(SystemSetting systemSetting, HttpPostedFileBase upload)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            var count = db.SystemSettings.Count();
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var currentSystemSetting = db.SystemSettings.AsNoTracking().FirstOrDefault();
            if (userId != 1 && currentSystemSetting != null)
            {
                systemSetting.IsZatcaLinked = currentSystemSetting.IsZatcaLinked;
            }

            if (systemSetting.ShowCashBoxBalance == null)
                systemSetting.ShowCashBoxBalance = false;
            systemSetting.PayViaCash = true;
            if (systemSetting.ServiceFeesPercentage.HasValue)
                systemSetting.ServiceFeesPercentage /= 100;
            if (count > 0)
            {
                if (upload != null)
                {
                    //string path = Path.Combine(Server.MapPath("~/UploadedFiles"), Path.GetFileName(upload.FileName));
                    //upload.SaveAs(path);
                    upload.SaveAs(Server.MapPath("/assets/images/logo/") + upload.FileName);
                    systemSetting.Logo = domainName + ("/assets/images/logo/") + upload.FileName;

                }
                else
                {
                    if (systemSetting.Logo == null)
                    {
                        systemSetting.Logo = domainName + "/assets/images/logo-light.png";
                    }
                    else
                    {
                        systemSetting.Logo = systemSetting.Logo;
                    }
                }
                db.Entry(systemSetting).State = EntityState.Modified;

                // Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "SystemSetting",
                    SelectedId = systemSetting.Id,
                    IsMasterChange = true,
                    IsNew = false,
                    IsTransaction = false
                });
            }

            else
            {
                if (upload != null)
                {
                    upload.SaveAs(Server.MapPath("/assets/images/logo/") + upload.FileName);

                    systemSetting.Logo = domainName + ("/assets/images/logo/") + upload.FileName;
                }
                else
                {
                    systemSetting.Logo = domainName + "/assets/images/logo-light.png";
                }
                db.SystemSettings.Add(systemSetting);
                db.SaveChanges();
                // Add DB Change
                var SelectedId = db.SystemSettings.OrderByDescending(r => r.Id).FirstOrDefault().Id;
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "SystemSetting",
                    SelectedId = SelectedId,
                    IsMasterChange = true,
                    IsNew = true,
                    IsTransaction = false
                });

            }
            var reservation = db.PaymentMethods.Find(3);//deactivate reservation payment method so that not appear in invoices
            if (systemSetting.AllowSalesOrderInPos == true)
                reservation.IsActive = true;
            else
                reservation.IsActive = false;
            db.Entry(reservation).State = EntityState.Modified;

            db.SaveChanges();

            // Add DB Change
            QueryHelper.AddDBChange(new DBChange()
            {
                TableName = "PaymentMethod",
                SelectedId = reservation.Id,
                IsMasterChange = true,
                IsNew = false,
                IsTransaction = false
            });

            // change CashIssue && CashReceipt Vouchers Activation Status
            if (systemSetting.NotAutomaticallyApprovingCashIssueAndCashReceiptVouchers == true)
            {
                db.Database.ExecuteSqlCommand($"update CashIssueVoucher set IsActive=0 where IsActive=1 and IsDeleted=0");
                db.Database.ExecuteSqlCommand($"update CashReceiptVoucher set IsActive=0 where IsActive=1 and IsDeleted=0");
            }
            //else
            //{
            //    db.Database.ExecuteSqlCommand($"update CashIssueVoucher set IsActive=1 where IsActive=0 and IsDeleted=0");
            //    db.Database.ExecuteSqlCommand($"update CashReceiptVoucher set IsActive=1 where IsActive=0 and IsDeleted=0");
            //}

            return RedirectToAction("Index");
        }

        public JsonResult InitializeDataBase(int type)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (type == 1)
            {
                db.DataBase_FullInitialization();
            }
            else
            {
                if (type == 2)
                {
                    db.DataBase_PartialInitialization();
                }
                else if (type == 3)
                {
                    db.DataBase_TransactsInitialization();
                }
            }

            db.SaveChanges();
            ////-------------------- Notification-------------------------////
            //Notification.GetNotification("DatabaseInitialization", "Delete", "Delete", userId, null, "تهيئة قاعدة البيانات");

            //QueryHelper.AddLog(new MyLog()
            //{
            //    ArAction = "تهيئة قاعدة البيانات",
            //    EnAction = "Delete",
            //    ControllerName = "DatabaseInitialization",
            //    UserName = User.Identity.Name,
            //    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
            //    LogDate = DateTime.Now,
            //    RequestMethod = "POST",
            //});
            return Json(new { success = "true" });
        }

        public ActionResult DBSetting()
        {
            //QueryHelper.AddLog(new MyLog()
            //{
            //    ArAction = "فتح شاشة تهيئة قاعدة البيانات",
            //    EnAction = "Index",
            //    ControllerName = "DatabaseInitialization",
            //    UserName = User.Identity.Name,
            //    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
            //    LogDate = DateTime.Now,
            //    RequestMethod = "GET"
            //});
            //////-------------------- Notification-------------------------////
            //Notification.GetNotification("DatabaseInitialization", "View", "Index", null, null, "تهيئة قاعدة البيانات");
            ////////////-----------------------------------------------------------------------
            return View();
        }
        public ActionResult AllowedModule()
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            ViewBag.UserId = userId;
            var AllowedModule = db.AllowedModule_Get().ToList();
            return View(AllowedModule);
        }
        [HttpPost]
        public ActionResult AllowedModule(ICollection<AllowedModule> allowedModules)
        {
            if (ModelState.IsValid)
            {
                var selectedModules = (allowedModules ?? new List<AllowedModule>())
                    .Where(a => a.SystemPageId.HasValue && a.IsSelected != false)
                    .GroupBy(a => a.SystemPageId.Value)
                    .Select(g => new AllowedModule
                    {
                        SystemPageId = g.Key,
                        IsSelected = true,
                        UserId = null
                    })
                    .ToList();

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var prevRecord = db.AllowedModules.ToList();
                        if (prevRecord.Count > 0)
                        {
                            db.AllowedModules.RemoveRange(prevRecord);
                        }

                        db.AllowedModules.AddRange(selectedModules);
                        db.SaveChanges();
                        transaction.Commit();
                        return Content("true");
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            return Content("false");
        }

        public ActionResult AllowedScreens()
        {
            ViewBag.SchemaMissing = !CompanyAllowedPagesTableExists();
            return View(GetAllowedScreensViewModel());
        }

        [HttpPost]
        public ActionResult AllowedScreens(ICollection<AllowedScreensSaveItem> allowedScreens)
        {
            if (!CompanyAllowedPagesTableExists())
            {
                return Content("schema-missing");
            }

            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var criticalPageIds = GetCriticalPageIds();
            var selectedPageIds = (allowedScreens ?? new List<AllowedScreensSaveItem>())
                .Where(a => a.IsSelected)
                .Select(a => a.SystemPageId)
                .Concat(criticalPageIds)
                .Distinct()
                .ToList();

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    db.Database.ExecuteSqlCommand("DELETE FROM dbo.CompanyAllowedPages");

                    foreach (var pageId in selectedPageIds)
                    {
                        db.Database.ExecuteSqlCommand(
                            @"INSERT INTO dbo.CompanyAllowedPages
                              (SystemPageId, IsSelected, CreatedDate, CreatedBy, UpdatedDate, UpdatedBy)
                              VALUES (@SystemPageId, 1, GETDATE(), @UserId, GETDATE(), @UserId)",
                            new SqlParameter("@SystemPageId", pageId),
                            new SqlParameter("@UserId", userId));
                    }

                    transaction.Commit();
                    return Content("true");
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private List<AllowedScreensViewModel> GetAllowedScreensViewModel()
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            var tableExists = CompanyAllowedPagesTableExists();
            var configured = tableExists && db.Database.SqlQuery<int>("SELECT COUNT(1) FROM dbo.CompanyAllowedPages").FirstOrDefault() > 0;
            var selectedPageIds = configured
                ? db.Database.SqlQuery<int>("SELECT SystemPageId FROM dbo.CompanyAllowedPages WHERE IsSelected = 1").ToList()
                : new List<int>();
            var criticalPageIds = GetCriticalPageIds();
            var modules = db.SystemPages
                .Where(p => p.IsDeleted == false && p.IsModule == true)
                .Select(p => new { p.Id, p.ArName, p.EnName })
                .ToList()
                .ToDictionary(p => p.Id);

            return db.SystemPages
                .Where(p => p.IsActive == true
                            && p.IsDeleted == false
                            && p.IsModule != true
                            && p.ControllerName != null
                            && p.ControllerName != "")
                .Select(p => new
                {
                    p.Id,
                    p.ArName,
                    p.EnName,
                    p.ControllerName,
                    p.ParentId
                })
                .ToList()
                .Select(p => new AllowedScreensViewModel
                {
                    SystemPageId = p.Id,
                    SystemPageName = session == "en" && !string.IsNullOrEmpty(p.EnName) ? p.EnName : p.ArName,
                    ControllerName = p.ControllerName,
                    ModuleId = p.ParentId,
                    ModuleName = p.ParentId.HasValue && modules.ContainsKey(p.ParentId.Value)
                        ? (session == "en" && !string.IsNullOrEmpty(modules[p.ParentId.Value].EnName)
                            ? modules[p.ParentId.Value].EnName
                            : modules[p.ParentId.Value].ArName)
                        : "",
                    IsCritical = criticalPageIds.Contains(p.Id) || IsCriticalController(p.ControllerName),
                    IsSelected = !configured || selectedPageIds.Contains(p.Id) || criticalPageIds.Contains(p.Id) || IsCriticalController(p.ControllerName)
                })
                .OrderBy(p => p.ModuleName)
                .ThenBy(p => p.SystemPageName)
                .ToList();
        }

        private bool CompanyAllowedPagesTableExists()
        {
            return db.Database.SqlQuery<int>(
                "SELECT CASE WHEN OBJECT_ID(N'dbo.CompanyAllowedPages', N'U') IS NULL THEN 0 ELSE 1 END").FirstOrDefault() == 1;
        }

        private List<int> GetCriticalPageIds()
        {
            var criticalControllers = CriticalCompanyAvailabilityControllers();
            return db.SystemPages
                .Where(p => p.IsDeleted == false && criticalControllers.Contains(p.ControllerName))
                .Select(p => p.Id)
                .ToList();
        }

        private static bool IsCriticalController(string controllerName)
        {
            return CriticalCompanyAvailabilityControllers().Contains(controllerName);
        }

        private static string[] CriticalCompanyAvailabilityControllers()
        {
            return new[]
            {
                "SystemSetting",
                "ERPUsers",
                "ERPRoles",
                "RolePrivilege",
                "UserPrivilege"
            };
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
