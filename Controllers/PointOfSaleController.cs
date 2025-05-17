using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using MyERP.Repository;
using Newtonsoft.Json;
using System.Data.Entity.Core.Objects;
using Microsoft.Owin.Security;
using Microsoft.AspNet.Identity;
using System.Globalization;

namespace MyERP.Controllers
{
    public class PointOfSaleController : ViewToStringController
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();
        // GET: PointOfSale

        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "", int departmentId = 0)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var roleId = db.ERPUsers.Where(u => u.Id == userId).FirstOrDefault().RoleId;
            bool? IsCashier = db.ERPUsers.Where(e => e.Id == userId).FirstOrDefault().IsCashier;
            if (IsCashier == true)
            {
                Session["IsCashier"] = true;
                var pos = db.Pos.Where(p => p.CurrentCashierUserId == userId && p.PosStatusId == 2).FirstOrDefault();
                if (pos == null)
                    return RedirectToAction("PosLogin", "PointOfSale");
                else
                {
                    Session["PosId"] = pos.Id;
                }
            }
            ViewBag.IsCashier = IsCashier;

            ViewBag.CanPrintInvoice = false;//allow only admin and branch manager to print from index
            if (userId == 1 || roleId == 4)
                ViewBag.CanPrintInvoice = true;
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var depIds = await departmentRepository.UserDepartmentsIds(userId);
            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();
            ViewBag.InabilityToEditSalesAndPurchaseInvoicesAndReturns = systemSetting.InabilityToEditSalesAndPurchaseInvoicesAndReturns;
            ViewBag.ShowSerialNumbers = systemSetting.ShowSerialNumbers == true;
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("SalesInvoice", "View", "Index", null, null, "فواتير البيع");
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName", departmentId);
            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = session.ToString() == "en" && b.Department.EnName != null ? b.Department.Code + " - " + b.Department.EnName : b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", departmentId);

            }

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            Repository<SalesInvoice> repository = new Repository<SalesInvoice>(db);
            IQueryable<SalesInvoice> salesInvoices;
            var projectName = System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"];
            if (string.IsNullOrEmpty(searchWord))
            {
                salesInvoices = repository.GetAll().Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.PosId != null || (projectName == "Genoise" && s.DepartmentId == 28))).Include(s => s.Department).Include(s => s.Warehouse).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await repository.GetAll().Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.PosId != null || (projectName == "Genoise" && s.DepartmentId == 28))).CountAsync();
            }
            else
            {
                salesInvoices = repository.GetAll().Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Customer.ArName.Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord) || s.VoucherDate.ToString().Contains(searchWord)) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.PosId != null || (projectName == "Genoise" && s.DepartmentId == 28))).Include(s => s.Branch).Include(s => s.Currency).Include(s => s.Department).Include(s => s.Warehouse).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await repository.GetAll().Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Customer.ArName.Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord) || s.VoucherDate.ToString().Contains(searchWord)) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.PosId != null || (projectName == "Genoise" && s.DepartmentId == 28))).CountAsync();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة فواتير البيع",
                EnAction = "Index",
                ControllerName = "SalesInvoice",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(await salesInvoices.ToListAsync());
        }

        [SkipERPAuthorize]
        public ActionResult PosLogin()
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var user = db.ERPUsers.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == userId).FirstOrDefault();
            var roleId = db.ERPUsers.Where(u => u.Id == userId).FirstOrDefault().RoleId;
            ViewBag.IsCashier = db.ERPUsers.Where(e => e.Id == userId).FirstOrDefault().IsCashier;
            var IsWaiter = user.IsWaiter;
            ViewBag.IsWaiter = IsWaiter;
            if (IsWaiter == true) //  لو ويتر لازم يسجل دخول من نقطة بيع مفتوحة قبل كدا عشان فى اغلاق النقطة اللى بيتم بواسطة الكاشير يتحذف من جدول ال PosWaiter 
            {
                if (userId == 1)
                    ViewBag.PosId = new SelectList(db.Pos.Where(c => c.IsDeleted == false && c.IsActive == true && c.PosStatusId == 2).Select(b => new
                    {
                        Id = b.Id,
                        ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                    }), "Id", "ArName");
                else
                {
                    var posIds = db.Pos.Where(d => (db.UserPos.Where(u => u.UserId == userId && u.Privilege == true).Select(u => u.PosId).Contains(d.Id)) && d.IsDeleted == false && d.IsActive == true).Select(d => (int?)d.Id);
                    ViewBag.PosId = new SelectList(db.Pos.Where(c => c.IsDeleted == false && c.IsActive == true && (c.PosStatusId == 2 || IsWaiter == true) && posIds.Contains(c.Id)).Select(b => new
                    {
                        Id = b.Id,
                        ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                    }), "Id", "ArName");
                }
            }
            else
            {
                if (userId == 1)
                    ViewBag.PosId = new SelectList(db.Pos.Where(c => c.IsDeleted == false && c.IsActive == true && c.PosStatusId == 1).Select(b => new
                    {
                        Id = b.Id,
                        ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                    }), "Id", "ArName");
                else
                {
                    var posIds = db.Pos.Where(d => (db.UserPos.Where(u => u.UserId == userId && u.Privilege == true).Select(u => u.PosId).Contains(d.Id)) && d.IsDeleted == false && d.IsActive == true).Select(d => (int?)d.Id);
                    ViewBag.PosId = new SelectList(db.Pos.Where(c => c.IsDeleted == false && c.IsActive == true && (c.PosStatusId == 1 || IsWaiter == true) && posIds.Contains(c.Id)).Select(b => new
                    {
                        Id = b.Id,
                        ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                    }), "Id", "ArName");
                }
            }
            ViewBag.PosManagerId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.ShiftId = new SelectList(db.Shifts.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }), "Id", "ArName");
            return View();
        }
        [SkipERPAuthorize]
        [HttpPost]
        public ActionResult PosLogin(int posId, int posManagerId, string posManagerPassword, int ShiftId, decimal custodyAmount)
        {
            try
            {
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

                var timeNow = cTime.TimeOfDay;
                var shiftTime = db.ShiftDetails.Where(s => s.ShiftId == ShiftId && s.EnDay == cTime.DayOfWeek.ToString()).FirstOrDefault();
                if (shiftTime == null)
                    return Content("notExactTime");

                var StartTime = (TimeSpan)shiftTime.StartTime;
                var EndTime = (TimeSpan)shiftTime.EndTime;
                if (timeNow > EndTime || timeNow < StartTime || shiftTime.IsVacation == true)
                {
                    return Content("notExactTime");
                }


                int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
               // samy 
                var PosId = 0; 
                if (Session["PosId"] != null)
                {
                    int.TryParse(Session["PosId"].ToString(), out PosId);
                    
                }
                

              //  var PosId = Session["PosId"] != null ? int.Parse(Session["PosId"].ToString()) : 0;
                var user = db.ERPUsers.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == userId).FirstOrDefault();
                var IsWaiter = user.IsWaiter;
                var manager = db.ERPUsers.Where(u => u.EmployeeId == posManagerId).FirstOrDefault();
                if (manager == null)
                    return Content("managerUserIsNull");
                var managerUserName = manager.UserName;
                var custodyBoxId = db.ERPUsers.Where(u => u.Id == userId).FirstOrDefault().CustodyBoxId;
                var posManagerBoxId = db.ERPUsers.Where(e => e.EmployeeId == posManagerId).FirstOrDefault().CustodyBoxId;
                if (custodyAmount > 0 && custodyBoxId == null)
                    return Content("noCustodyBox");
                else if (custodyAmount > 0 && posManagerBoxId == null)
                    return Content("noManagerCustodyBox");
                ObjectParameter HashPW = new ObjectParameter("HashPW", typeof(string));
                db.ERPUser_GetHashPw(managerUserName, HashPW);
                string strHashPw = HashPW.Value.ToString();
                bool authenticated = PasswordEncrypt.VerifyHashPwd(posManagerPassword, strHashPw);
                if (!authenticated)
                {
                    return Content("managerPasswordIncorrect");
                }
                else
                {
                    var pos = db.Pos.Find(posId);
                    if (IsWaiter==true)
                    {
                        var posWaiter = new PosWaiter();
                        posWaiter.PosId = posId;
                        posWaiter.WaiterId = userId;
                        db.PosWaiters.Add(posWaiter);
                    }
                    else {
                        pos.CurrentCashierUserId = userId;
                        pos.CurrentShiftId = ShiftId;
                        pos.PosStatusId = 2;//open
                        db.Entry(pos).State = EntityState.Modified;
                    }
                    
                    db.SaveChanges();

                    if (custodyAmount > 0)
                    {
                        var idResult = new ObjectParameter("Id", typeof(Int32));
                        db.CashTransfer_Insert(idResult, null, null, null, null, null, null, null, posManagerBoxId, custodyBoxId, null, null, custodyAmount, cTime, userId, true, false, true, false, null, null, 3, pos.DepartmentId, null, null, pos.DepartmentId);
                    }
                    Session["PosId"] = posId;
                    Session["IsCashier"] = true;
                    return Content(Session["PosId"].ToString());
                }
            }
            catch (Exception ex)
            {
                return Content("false");
            }
        }

        //[SkipERPAuthorize]
        public async Task<ActionResult> AddEdit(int? id, int? selectedTableId, string selectedTableName, int? depId, int? selectedCustomerType, bool? showPaymentMethods, string WaitersName)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            int? posId = null;
            if (Session["posId"] != null && int.TryParse(Session["posId"].ToString(), out int parsedPosId))
            {
                posId = parsedPosId;
            }

            if (posId.HasValue)
            {
                ViewBag.Printer = db.Pos.FirstOrDefault(a => a.Id == posId)?.Printer;
            }
            else
            {
                ViewBag.Printer = null;
            }

            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();
            ViewBag.ServiceFeesPercentage = systemSetting.ServiceFeesPercentage.HasValue ? systemSetting.ServiceFeesPercentage : 0;
            ViewBag.ApplyTaxes = systemSetting.ApplyTaxesOnSalesIvoiceAuto;
            ViewBag.EnableAdditionalItemsOnPos = systemSetting.EnableAdditionalItemsOnPos;
            ViewBag.DefaultQuantityInPos = systemSetting.DefaultQuantityInPos;
            ViewBag.AllowToAddSameItemMultipleTimes = systemSetting.AllowToAddSameItemMultipleTimes;
            ViewBag.ShowCashBoxBalance = systemSetting.ShowCashBoxBalance;
            ViewBag.AllowSalesOrderInPos = systemSetting.AllowSalesOrderInPos;
            ViewBag.UseRefundableInsurancesForAccessories = systemSetting.UseRefundableInsurancesForAccessories;
            ViewBag.WorkWithCommercialPOS = systemSetting.WorkWithCommercialPOS;
            ViewBag.ShowTaxPercentageAndValue = systemSetting.ShowTaxPercentageAndValue;
            ViewBag.UsePricePolicyIncludedInItemsValueAddedTax = systemSetting.UsePricePolicyIncludedInItemsValueAddedTax;
            ViewBag.Logo = db.SystemSettings.FirstOrDefault().Logo;
            ViewBag.ApplySmokingTax = systemSetting.ApplySmokingTax;
            ViewBag.SmokingTaxPercentage = systemSetting.SmokingTaxPercentage;
            ViewBag.CashierMaximumAllowedDiscountPercentage = systemSetting.CashierMaximumAllowedDiscountPercentage;
            ViewBag.UseThermalPrinterToPrintInvoices = systemSetting.UseThermalPrinterToPrintInvoices;
            ViewBag.UsePrintingAssistantProgram = systemSetting.UsePrintingAssistantProgram;
            ViewBag.IsDeliveryPossibility = systemSetting.IsDeliveryPossibility;
            ViewBag.IsTableSellingPossibility = systemSetting.IsTableSellingPossibility;
            ViewBag.IsTakeAwaySellingPossibility = systemSetting.IsTakeAwaySellingPossibility;
            //if (!string.IsNullOrEmpty(WaitersName))
            //{
            //    var invoice = db.SalesInvoices.Find(id);
            //    if (invoice != null)
            //    {
            //        var table = db.Tables.Find(invoice.TableId);
            //        if (table != null)
            //        {
            //            if (showPaymentMethods == true)
            //            {
            //                table.WaitersName = null;
            //            }
            //            else
            //            {
            //                table.WaitersName = WaitersName;
            //            }
            //            db.SaveChanges();
            //        }
            //    }
            //}
            ViewBag.WaitersName = WaitersName;

            // Accessory Group Id
            ViewBag.AccessoryGroupId = db.Items.Where(e => e.IsAccessory == true).Include(e => e.ItemGroup).Select(e => e.ItemGroupId).FirstOrDefault();
            var roleId = db.ERPUsers.Where(u => u.Id == userId).FirstOrDefault().RoleId;
            var _User = db.ERPUsers.Where(e => e.Id == userId).FirstOrDefault();
            bool? IsCashier = _User.IsCashier;
            bool? IsWaiter = _User.IsWaiter;
            ViewBag.ProjectName = System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"];
            if (IsCashier == true)
            {
                Session["IsCashier"] = true;
                var pos = db.Pos.Where(p => p.CurrentCashierUserId == userId && p.PosStatusId == 2).FirstOrDefault();
                if (pos == null)
                    return RedirectToAction("PosLogin", "PointOfSale");
                else
                {
                    Session["PosId"] = pos.Id;
                }
            }
            ViewBag.IsCashier = IsCashier;
            ViewBag.IsWaiter = IsWaiter;
            ViewBag.CanPrintInvoice = false;//allow only admin and branch manager to print from index
            if (userId == 1 || roleId == 4)
                ViewBag.CanPrintInvoice = true;
            UserRepository userRepository = new UserRepository(db);
            var DisplayFactoryPrice = userId == 1 ? true : db.UserPrivileges.Where(u => u.PageAction.PageId == 58 && u.PageAction.EnName == "DisplayFactoryPrice" && u.PageAction.Action == "DisplayFactoryPrice" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefault();
            DisplayFactoryPrice = DisplayFactoryPrice ?? db.RolePrivileges.Where(u => u.PageAction.PageId == 58 && u.PageAction.EnName == "DisplayFactoryPrice" && u.PageAction.Action == "DisplayFactoryPrice" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefault();
            ViewBag.DisplayFactoryPrice = DisplayFactoryPrice;
            // ViewBag.DisplayFactoryPrice = await userRepository.HasActionPrivilege(userId, "DisplayFactoryPrice", "DisplayFactoryPrice");
            ViewBag.CashBoxId = await db.UserCashBoxes.Where(x => x.UserId == userId && x.Privilege == true).Select(x => x.CashBoxId).FirstOrDefaultAsync();
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            WarehouseRepository warehouseRepository = new WarehouseRepository(db);
            CashboxReposistory cashboxReposistory = new CashboxReposistory(db);
            var banks = await db.Banks.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                b.Id,
                // ArName = b.Code + " - " + b.ArName
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }).ToListAsync();
            if (id == null)
            {
                ViewBag.NewInvoice = true;
                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.WarehouseId = new SelectList(warehouseRepository.UserWarehouses(userId, systemSetting.DefaultDepartmentId), "Id", "ArName", systemSetting.DefaultWarehouseId);

                ViewBag.CustomerType = new SelectList(new List<dynamic>
                {
                    new { Id=1, ArName= session.ToString() == "en"?"Take Away":"تيك اواي"},
                    new { Id = 2, ArName =session.ToString() == "en"?"Cafe": "كافيه" },
                    new { Id=3, ArName=session.ToString() == "en"?"Delivery":"دليفري"},
                    new { Id=4, ArName=session.ToString() == "en"?"Department":"فرع"}}, "Id", "ArName", 1);

                ViewBag.VendorOrCustomerId = new SelectList(await db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }).ToListAsync(), "Id", "ArName");

                ViewBag.PosCustomerId = new SelectList(await db.PosCustomers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }).ToListAsync(), "Id", "ArName");
                ViewBag.PosLinkedCustomerId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                var cashBoxSelectList = new SelectList(await cashboxReposistory.UserCashboxes(userId, systemSetting.DefaultDepartmentId).ToListAsync(), "Id", "ArName", systemSetting.DefaultCashBoxId);
                ViewBag.CashBoxId = cashBoxSelectList;
                List<PaymentMethod> paymentMethods = new List<PaymentMethod>();
                if (systemSetting.AllowSalesOrderInPos == true)
                {
                    paymentMethods = db.PaymentMethods.Where(p => p.IsActive == true && p.IsDeleted == false && p.ForPos == true).OrderBy(p => p.Code).ToList();
                }
                else
                {
                    paymentMethods = db.PaymentMethods.Where(p => p.IsActive == true && p.IsDeleted == false && p.ForPos == true && p.Id != 3).OrderBy(p => p.Code).ToList();
                }

                ViewBag.PaymentMethods = paymentMethods;
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

                ViewBag.VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.DeliveryDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.SelectedTableId = selectedTableId;
                ViewBag.SelectedTableName = selectedTableName;
                ViewBag.DepId = depId;
                ViewBag.SelectedCustomerType = selectedCustomerType;
                //ViewBag.DriverId = new SelectList(db.Employees.Where(e => e.IsActive == true && e.IsDeleted == false && e.IsDriver == true&&e.DepartmentId == depId), "Id", "ArName");
                //ViewBag.GovernorateId = new SelectList(db.Countries.Where(e => e.IsActive == true && e.IsDeleted == false), "Id", "ArName");


                return View();
            }

            SalesInvoice salesInvoice = await db.SalesInvoices.FindAsync(id);
            if (salesInvoice == null)
            {
                return HttpNotFound();
            }
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", salesInvoice.DepartmentId);
            ViewBag.WarehouseId = new SelectList(warehouseRepository.UserWarehouses(userId, salesInvoice.DepartmentId), "Id", "ArName", salesInvoice.WarehouseId);
            ViewBag.CustomerType = new SelectList(new List<dynamic>
                {
                    new { Id=1, ArName= session.ToString() == "en"?"Take Away":"تيك اواي"},
                    new { Id = 2, ArName =session.ToString() == "en"?"Cafe": "كافيه" },
                new { Id=3, ArName=session.ToString() == "en"?"Delivery":"دليفري"},
                new { Id=4, ArName=session.ToString() == "en"?"Department":"فرع"}}, "Id", "ArName", salesInvoice.CustomerType);
            ViewBag.PaymentMethodId = new SelectList(await db.PaymentMethods.Where(a => a.IsActive == true && a.IsDeleted == false && a.ForPos == true).Select(b => new
            {
                b.Id,
                //ArName = b.ArName
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }).ToListAsync(), "Id", "ArName", salesInvoice.SalesInvoicePaymentMethods.OrderByDescending(x => x.Amount).Select(x => x.PaymentMethodId).FirstOrDefault());
            var cashBoxes = await cashboxReposistory.UserCashboxes(userId, salesInvoice.DepartmentId).ToListAsync();
            foreach (var method in salesInvoice.SalesInvoicePaymentMethods)
            {
                if (method.PaymentMethodId == 1)
                {
                    ViewBag.CashBoxId = new SelectList(cashBoxes, "Id", "ArName", method.CashBoxId);
                }
                else if (method.PaymentMethodId == 3)
                {

                    ViewBag.BankIdForCheque = new SelectList(banks, "Id", "ArName", method.BankId);
                    ViewBag.BankAccountIdForCheque = new SelectList(db.BankAccounts.Where(a => a.IsActive == true && a.IsDeleted == false && a.BankId == method.BankId), "Id", "AccountNumber", method.BankAccountId);
                }
                else
                {
                    ViewBag.BankIdForVisa = new SelectList(banks, "Id", "ArName", method.BankId);
                    ViewBag.BankAccountIdForVisa = new SelectList(db.BankAccounts.Where(a => a.IsActive == true && a.IsDeleted == false && a.BankId == method.BankId), "Id", "AccountNumber", method.BankAccountId);
                }
            }

            ViewBag.VendorOrCustomerId = new SelectList(await db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false && a.IncludeFees == true).Select(b => new
            {
                b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }).ToListAsync(), "Id", "ArName", salesInvoice.VendorOrCustomerId);

            ViewBag.VoucherDate = salesInvoice.VoucherDate.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.DeliveryDate = salesInvoice.DeliveryDate != null ? salesInvoice.DeliveryDate.Value.ToString("yyyy-MM-ddTHH:mm") : "";
            ViewBag.DeliveryStartDate = salesInvoice.DeliveryStartDate != null ? salesInvoice.DeliveryStartDate.Value.ToString("yyyy-MM-ddTHH:mm") : "";
            ViewBag.DeliveryEndDate = salesInvoice.DeliveryEndDate != null ? salesInvoice.DeliveryEndDate.Value.ToString("yyyy-MM-ddTHH:mm") : "";


            ViewBag.PosCustomerId = new SelectList(await db.PosCustomers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }).ToListAsync(), "Id", "ArName", salesInvoice.PosCustomerId);
            var CustomerId = db.PosCustomers.Where(a => a.IsDeleted == false && a.IsActive == true && a.Id == salesInvoice.PosCustomerId).FirstOrDefault().CustomerId;
            ViewBag.PosLinkedCustomerId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", CustomerId);

            ViewBag.showPaymentMethods = showPaymentMethods;

            //ViewBag.DriverId = new SelectList(db.Employees.Where(e => e.IsActive == true && e.IsDeleted == false && e.IsDriver == true && e.DepartmentId == salesInvoice.DepartmentId), "Id", "ArName",salesInvoice.DriverId);
            ViewBag.DriverId = salesInvoice.DriverId;

            ViewBag.CarId = salesInvoice.CarId;
            ViewBag.SelectedCustomerType = selectedCustomerType;
            

            return View(salesInvoice);
        }

        // GET
        // [SkipERPAuthorize]
        public ActionResult AddEditSerialNumber(int? id, int itemId)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";

            SystemSetting sysObj = db.SystemSettings.FirstOrDefault();
            ViewData["ShowSerialNumbers"] = true;


            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            SalesInvoice salesInvoice = db.SalesInvoices.Find(id);
            if (salesInvoice == null)
            {
                return HttpNotFound();
            }
            if (itemId == 0)
            {
                var x = db.SalesInvoiceDetails.Where(p => p.MainDocId == id).FirstOrDefault().ItemId;
                itemId = int.Parse(x.ToString());
            }
            ViewBag.GeneralItemId = itemId;
            int sysPageId = QueryHelper.SourcePageId("SalesInvoice");
            ViewBag.VendorId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName + " - " + b.EnName
            }), "Id", "ArName", salesInvoice.VendorOrCustomerId);
            ViewBag.VendorOrCustomerId = new SelectList(db.Customers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName + " - " + b.EnName
            }), "Id", "ArName", salesInvoice.VendorOrCustomerId);

            ViewBag.PosCustomerId = new SelectList(db.PosCustomers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }).ToList(), "Id", "ArName", salesInvoice.PosCustomerId);

            ViewBag.BranchId = new SelectList(db.Branches.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }), "Id", "ArName", salesInvoice.BranchId);
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName", salesInvoice.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName", salesInvoice.WarehouseId);

            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = session.ToString() == "en" && b.Department.EnName != null ? b.Department.Code + " - " + b.Department.EnName : b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", salesInvoice.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId).Select(b => new
                {
                    Id = b.WareHouseId,
                    ArName = session.ToString() == "en" && b.Warehouse.EnName != null ? b.Warehouse.Code + " - " + b.Warehouse.EnName : b.Warehouse.Code + " - " + b.Warehouse.ArName
                }), "Id", "ArName", salesInvoice.WarehouseId);

            }
            ViewBag.SerialNumbers = JsonConvert.SerializeObject(db.PurchaseSaleSerialNumbers.Where(s => s.SelectedId == id && s.ItemId == itemId && s.PageSourceId == db.SystemPages.FirstOrDefault(d => d.TableName == "SalesInvoice").Id).Select(d => new { d.ItemId, d.SerialNumber }));



            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح سيريال فاتورة البيع",
                EnAction = "AddEdit",
                ControllerName = "AddEditSerialNumber",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = salesInvoice.Id,
                CodeOrDocNo = salesInvoice.DocumentNumber
            });

            return View(salesInvoice);
        }
        [SkipERPAuthorize]
        public async Task<ActionResult> SalesOrders(int depId, int warehouseId, int customerType)
        {
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
            DateTime time1DayAfter = cTime.Date.AddDays(1);
            return PartialView(await db.SalesOrders.Where(x => x.IsDeleted == false && x.IsCompleted == false && x.DepartmentId == depId && x.WarehouseId == warehouseId && x.CustomerType == customerType).OrderByDescending(x => x.Id).ToListAsync());
        }

        [SkipERPAuthorize]
        [HttpPost]
        public async Task<ActionResult> SalesOrdersDetails(List<int?> salesOrderIds)
        {
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

            var view = RenderRazorViewToString("SalesOrdersDetails", await db.SalesOrderDetails.Where(x => salesOrderIds.Contains(x.MainDocId)).ToListAsync());
            return Json(new { view, Paid = await db.SalesOrders.Where(x => salesOrderIds.Contains(x.Id)).SumAsync(x => x.Paid), VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm") }, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        [HttpPost]
        public ActionResult GetSalesIvoiceDetails(string SalesInvoiceNo, int departmentId, bool? accessoryReturn = false)
        {
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

            var salesInvoice = db.SalesInvoices.Where(s => s.DocumentNumber == SalesInvoiceNo && s.DepartmentId == departmentId).FirstOrDefault();
            if (salesInvoice != null)
            {
                //var salesInvoiceDetails = await db.SalesInvoiceDetails.Where(x => x.MainDocId == salesInvoice.Id&&(accessoryReturn==false ||x.Item.IsAccessory==true)).ToListAsync();
                var salesInvoiceDetails = db.GetSalesInvoiceDetails(salesInvoice.Id, accessoryReturn).ToList();
                var view = RenderRazorViewToString("SalesInvoiceDetails", salesInvoiceDetails);
                var CheckDebit = db.SalesInvoicePaymentMethods.Where(a => a.SalesInvoiceId == salesInvoice.Id && a.PaymentMethodId == 2).FirstOrDefault();
                if (CheckDebit != null && CheckDebit.Amount > 0)
                {
                    return Json(new { success = "CantChange" });
                }
                else
                {
                    return Json(new { view, Paid = salesInvoice.Paid, VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm"), customerId = salesInvoice.VendorOrCustomerId, SalesInvoiceId = salesInvoice.Id, CustomerType = salesInvoice.CustomerType }, JsonRequestBehavior.AllowGet);
                }
            }
            return Json(new { success = "false" });
        }

        [SkipERPAuthorize]
        public async Task<ActionResult> PurchaseRqeusts()
        {
            return PartialView(await db.PurchaseRequests.Where(x => x.IsDeleted == false && x.IsApproved == true && x.SalesInvoicePurchaseRequests.Count == 0).OrderByDescending(x => x.Id).ToListAsync());
        }

        [SkipERPAuthorize]
        [HttpPost]
        public async Task<ActionResult> PurchaseRqeustsDetails(List<int?> purchaseReqeustIds)
        {
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

            var view = RenderRazorViewToString("PurchaseRqeustsDetails", await db.PurchaseRequestDetails.Where(x => purchaseReqeustIds.Contains(x.MainDocId)).ToListAsync());
            return Json(new { view, VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm") }, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public async Task<ActionResult> SalesInvoiceAvailability(int customerId, int depId)
        {
            var salesInvoice = await db.SalesInvoices.Where(x => x.VendorOrCustomerId == customerId && x.DepartmentId == depId && x.IsDeleted == false && x.IsCompleted == false && x.IsLinked == false).OrderByDescending(x => x.Id).Select(x => new { x.Id, x.Total, x.TotalAfterTaxes, x.Paid, x.NetTotal, x.VoucherDiscountPercentage, x.VoucherDiscountValue, x.WarehouseId, x.DepartmentId, x.ServiceFees, x.VoucherDate, x.DocumentNumber, x.IsLinked, x.SelectedId, x.SystemPageId, SalesOrdersSalesInvoices = x.SalesOrdersSalesInvoices.Select(s => s.SalesOrderId) }).FirstOrDefaultAsync();
            if (salesInvoice != null)
            {
                var view = RenderRazorViewToString("SalesInvoiceAvailability", await db.SalesInvoiceDetails.Where(x => x.MainDocId == salesInvoice.Id).ToListAsync());
                return Json(new { view, salesInvoice, VoucherDate = salesInvoice.VoucherDate.ToString("yyyy-MM-ddTHH:mm") }, JsonRequestBehavior.AllowGet);
            }
            return PartialView();
        }

        [SkipERPAuthorize]
        public async Task<JsonResult> SalesInvoiceDetails(int id)
        {
            var salesInvoice = await db.SalesInvoices.Where(x => x.Id == id).OrderByDescending(x => x.Id).Select(x => new { x.Id, x.Total, x.TotalAfterTaxes, x.Paid, x.NetTotal, x.VoucherDiscountPercentage, x.VoucherDiscountValue, x.WarehouseId, x.DepartmentId, x.ServiceFees, x.VoucherDate, x.DocumentNumber, x.IsLinked, x.SelectedId, x.SystemPageId, SalesOrdersSalesInvoices = x.SalesOrdersSalesInvoices.Select(s => s.SalesOrderId) }).FirstOrDefaultAsync();

            var view = RenderRazorViewToString("SalesInvoiceAvailability", await db.SalesInvoiceDetails.Where(x => x.MainDocId == id).ToListAsync());
            return Json(new { view, salesInvoice, VoucherDate = salesInvoice.VoucherDate.ToString("yyyy-MM-ddTHH:mm") }, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public async Task<ActionResult> SalesInvoices()
        {
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

            DateTime cTime2 = cTime.Date.AddDays(1);
            var salesInvoices = db.SalesInvoices.Where(s => s.IsDeleted == false && s.UserId == userId && s.VoucherDate >= cTime && s.VoucherDate <= cTime2);

            return PartialView(await salesInvoices.ToListAsync());
        }

        [SkipERPAuthorize]
        public JsonResult GetShiftTime(int id)
        {
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

            var timeNow = cTime.TimeOfDay;
            var shiftTime = db.ShiftDetails.Where(s => s.ShiftId == id && s.EnDay == cTime.DayOfWeek.ToString()).FirstOrDefault();
            var time = new { StartTime = shiftTime.StartTime.ToString(), EndTime = shiftTime.EndTime.ToString() };
            return Json(time, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public async Task<ActionResult> UnpaidSalesInvoicesByDepartment(int id)
        {
            var invoices = await db.SalesInvoices.Where(x => x.IsCollectedByCashier == false && x.CustomerType == 3 && x.IsActive == true && x.IsDeleted == false && x.DepartmentId == id && x.TableId == null).OrderByDescending(x => x.Id).ToListAsync();

            foreach (var item in invoices)
            {
                var sum = item.SalesInvoicePaymentMethods.Where(a => a.PaymentMethodId != 2).Sum(a => a.Amount);
                item.Paid = sum;
                var Remain = item.SalesInvoicePaymentMethods.Where(a => a.PaymentMethodId == 2 && a.SalesInvoiceId == item.Id).FirstOrDefault().Amount;//Select(a => a.Amount).ToList();
                ViewBag.Remain = Remain;
            }
            return PartialView(invoices);
        }

        [SkipERPAuthorize]
        public ActionResult DriversWithOrdersByDepartment(int depId)
        {
            //var drivers = await (from e in db.Employees
            //               join s in db.SalesInvoices on e.Id equals s.DriverId
            //               where s.IsCollectedByCashier == false && e.IsActive == true && e.IsDeleted == false && 
            //               e.IsDriver == true
            //               select e).GroupBy(e => e.ArName).ToListAsync();
            //var drivers = await db.Employees.Where(x => x.IsActive == true && x.IsDeleted == false && x.IsDriver == true).ToListAsync();
            //var drivers =  db.GetDriversWithOrdersByDepartment(depId).ToList();
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            var drivers = db.GetDriversWithInvoicesByDepId(depId, userId).ToList();

            return PartialView(drivers);
        }

        [SkipERPAuthorize]
        public ActionResult Test(int? SalesInvoiceId = 31314)
        {
            var salesinvoice = db.SalesInvoices.Find(SalesInvoiceId);
            //var SalesInvoiceDetails = db.SalesInvoiceDetails.Where(a => a.IsDeleted == false && a.MainDocId == SalesInvoiceId).ToList();
            //var items = new List<Item>();
            //foreach (var detail in SalesInvoiceDetails)
            //{
            //    var item = db.Items.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == detail.ItemId).FirstOrDefault();
            //    items.Add(item);
            //}
            //var Sort = new List<Item>();
            //foreach (var item in items)
            //{
            //    var i = 0;
            //    if (items.Any(a => a.ItemGroupId == item.ItemGroupId) == true)
            //    {
            //        Sort.Add( item);
            //    }
            //}
            //var x = Sort;

            return View(salesinvoice);
        }

        [SkipERPAuthorize]
        public JsonResult GetItemPrice(int PosCustomerId, int itemId, int departmentId)
        {
            var posCustomer = db.PosCustomers.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == PosCustomerId).FirstOrDefault();
            var customerId = posCustomer != null ? posCustomer.CustomerId : null;
            var ItemPrice = db.GetItemPriceByCustomerIdAndDepartmentId(customerId, itemId, departmentId).FirstOrDefault();
            if (ItemPrice == null)
            {
                return Json(0, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(ItemPrice, JsonRequestBehavior.AllowGet);
            }
        }

        [SkipERPAuthorize]
        public JsonResult CheckManagerPassWord(string posManagerPassword)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var PosId = Session["PosId"] != null ? int.Parse(Session["PosId"].ToString()) : 0;

            var PosManagerId = db.Pos.Where(a => a.Id == PosId).FirstOrDefault().PosManagerId;
            var managerUserName = db.ERPUsers.Where(u => u.EmployeeId == PosManagerId).FirstOrDefault().UserName;
            ObjectParameter HashPW = new ObjectParameter("HashPW", typeof(string));
            db.ERPUser_GetHashPw(managerUserName, HashPW);
            string strHashPw = HashPW.Value.ToString();
            bool authenticated = PasswordEncrypt.VerifyHashPwd(posManagerPassword, strHashPw);
            if (!authenticated)
            {
                return Json(new { success = "managerPasswordIncorrect" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = "success" }, JsonRequestBehavior.AllowGet);

        }
        [SkipERPAuthorize]
        public JsonResult CancelTableReservation(int? id)
        {
            try
            {
                var table = db.Tables.Find(id);
                table.IsReserved = false;
                db.Entry(table).State = EntityState.Modified;
                db.SaveChanges();
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            catch(Exception ex )
            {
                return Json(false, JsonRequestBehavior.AllowGet);
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
