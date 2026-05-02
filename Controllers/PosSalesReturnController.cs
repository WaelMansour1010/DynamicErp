using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using MyERP.Repository;
using Newtonsoft.Json;

namespace MyERP.Controllers
{
    public class PosSalesReturnController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: SalesReturn
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            ViewBag.ShowSerialNumbers = systemSetting.ShowSerialNumbers == true;
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("SalesReturn", "View", "Index", null, null, "مرتجع بيع");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<SalesReturn> salesReturns;
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
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

            var projectName = System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"];
            var depIds = db.Departments.Where(d => (userId == 1 || db.UserDepartments.Where(u => u.UserId == userId && u.Privilege == true).Select(u => u.DepartmentId).Contains(d.Id)) && d.IsDeleted == false && d.IsActive == true).Select(d => (int?)d.Id);
            if (string.IsNullOrEmpty(searchWord))
            {
                salesReturns = db.SalesReturns.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.PosId != null || (projectName == "Genoise" && s.DepartmentId == 28))).Include(s => s.Branch).Include(s => s.Currency).Include(s => s.Department).Include(s => s.Warehouse).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.SalesReturns.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.PosId != null || (projectName == "Genoise" && s.DepartmentId == 28))).Count();
            }
            else
            {
                salesReturns = db.SalesReturns.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord) || s.Notes.Contains(searchWord) || s.Customer.ArName.Contains(searchWord)) && (s.PosId != null || (projectName == "Genoise" && s.DepartmentId == 28))).Include(s => s.Department).Include(s => s.Warehouse).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.SalesReturns.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord) || s.Notes.Contains(searchWord) || s.Customer.ArName.Contains(searchWord)) && (s.PosId != null || (projectName == "Genoise" && s.DepartmentId == 28))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة مرتجع البيع",
                EnAction = "Index",
                ControllerName = "SalesReturn",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(salesReturns.ToList());
        }

        public async Task<ActionResult> AddEdit(int? id,bool accessoryReturn=false)
        {
            UserRepository userRepository = new UserRepository(db);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            WarehouseRepository warehouseRepository = new WarehouseRepository(db);
            CashboxReposistory cashboxReposistory = new CashboxReposistory(db);
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            bool? IsCashier = db.ERPUsers.Where(e => e.Id == userId).FirstOrDefault().IsCashier;
            // Accessory Group Id
            ViewBag.AccessoryGroupId = db.Items.Where(e => e.IsAccessory == true).Include(e => e.ItemGroup).Select(e => e.ItemGroupId).FirstOrDefault();

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

            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();
            ViewBag.ApplyTaxes = systemSetting.ApplyTaxesOnSalesIvoiceAuto;
            //ViewBag.UseRefundableInsurancesForAccessories = systemSetting.UseRefundableInsurancesForAccessories;
            ViewBag.AllowToAddSameItemMultipleTimes = systemSetting.AllowToAddSameItemMultipleTimes;

            int roleId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("RoleId").Value);
            var CanChangeItemPrice = userId == 1 ? true : await db.UserPrivileges.Where(u => u.PageAction.PageId == 58 && u.PageAction.EnName == "ChangeItemPrice" && u.PageAction.Action == "ChangeItemPrice" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefaultAsync();
            CanChangeItemPrice = CanChangeItemPrice ?? await db.RolePrivileges.Where(u => u.PageAction.PageId == 58 && u.PageAction.EnName == "ChangeItemPrice" && u.PageAction.Action == "ChangeItemPrice" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefaultAsync();

            //var canChangeItemPrice = await userRepository.HasActionPrivilege(userId, "ChangeItemPrice", "ChangeItemPrice");

            var DisplayFactoryPrice = userId == 1 ? true : db.UserPrivileges.Where(u => u.PageAction.PageId == 58 && u.PageAction.EnName == "DisplayFactoryPrice" && u.PageAction.Action == "DisplayFactoryPrice" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefault();
            DisplayFactoryPrice = DisplayFactoryPrice ?? db.RolePrivileges.Where(u => u.PageAction.PageId == 58 && u.PageAction.EnName == "DisplayFactoryPrice" && u.PageAction.Action == "DisplayFactoryPrice" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefault();
            ViewBag.DisplayFactoryPrice = DisplayFactoryPrice;
            //ViewBag.DisplayFactoryPrice = await userRepository.HasActionPrivilege(userId, "DisplayFactoryPrice", "DisplayFactoryPrice");
            ViewBag.CanChangeItemPrice = CanChangeItemPrice == true;
            ViewBag.ShowSerialNumbers = systemSetting.ShowSerialNumbers == true;
            ViewBag.PayViaCarryOver = systemSetting.PayViaCarryOver == true;
            ViewBag.PayViaCash = systemSetting.PayViaCash == true;
            ViewBag.PayViaCheque = systemSetting.PayViaCheque == true;
            ViewBag.PayViaVisa = systemSetting.PayViaVisa == true;
            ViewBag.UseCostCenter = systemSetting.UseCostCenter;
            ViewBag.ShowCashBoxBalance = systemSetting.ShowCashBoxBalance;
            ViewBag.accessoryReturn = accessoryReturn;
            ViewBag.EnableAdditionalItemsOnPos = systemSetting.EnableAdditionalItemsOnPos;
            ViewBag.DefaultQuantityInPos = systemSetting.DefaultQuantityInPos;

            var banks = db.Banks.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }).ToList();

            if (id == null)
            {
                ViewBag.NewInvoice = true;
                var bankAccounts = db.BankAccounts.Where(a => a.IsActive == true && a.IsDeleted == false && a.BankId == systemSetting.DefaultBankId).Select(x => new { x.Id, x.AccountNumber }).ToList();
                ViewBag.BankIdForVisa = new SelectList(banks, "Id", "ArName", systemSetting.DefaultBankId);
                ViewBag.BankAccountIdForVisa = new SelectList(bankAccounts, "Id", "AccountNumber");
                ViewBag.BankIdForCheque = new SelectList(banks, "Id", "ArName", systemSetting.DefaultBankId);
                ViewBag.BankAccountIdForCheque = new SelectList(bankAccounts, "Id", "AccountNumber");
                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);

                ViewBag.WarehouseId = new SelectList(warehouseRepository.UserWarehouses(userId, systemSetting.DefaultDepartmentId), "Id", "ArName", systemSetting.DefaultWarehouseId);
                ViewBag.CashBoxId = new SelectList(cashboxReposistory.UserCashboxes(userId, systemSetting.DefaultDepartmentId), "Id", "ArName", systemSetting.DefaultCashBoxId);
                List<PaymentMethod> paymentMethods = db.PaymentMethods.Where(p => p.IsActive == true && p.IsDeleted == false&&p.ForPos==true).ToList();
                ViewBag.PaymentMethods = paymentMethods;
                ViewBag.VendorOrCustomerId = new SelectList(db.Customers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
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

                ViewBag.VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                
                //ViewBag.CustomerType = new SelectList(new List<dynamic>
                //{
                //    new { Id=1, ArName="تيك اواي"},
                //    new { Id = 2, ArName = "كافيه" },
                //new { Id=3, ArName="دليفري"},
                //new { Id=4, ArName="فرع"}}, "Id", "ArName", 1);

                return View();
            }
            SalesReturn salesReturn = db.SalesReturns.Find(id);
            if (salesReturn == null)
            {
                return HttpNotFound();
            }
            var cashBoxes = await cashboxReposistory.UserCashboxes(userId, salesReturn.DepartmentId).ToListAsync();

            ViewBag.VendorOrCustomerId = new SelectList(db.Customers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", salesReturn.VendorOrCustomerId);
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", salesReturn.DepartmentId);
            ViewBag.WarehouseId = new SelectList(warehouseRepository.UserWarehouses(userId, salesReturn.DepartmentId), "Id", "ArName", salesReturn.WarehouseId);
            ViewBag.CostCenterId = new SelectList(db.CostCenters.Where(a => a.IsActive && !a.IsDeleted && a.TypeId == 2).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", salesReturn.CostCenterId);

            //ViewBag.CustomerType = new SelectList(new List<dynamic>
            //    {
            //        new { Id=1, ArName="تيك اواي"},
            //        new { Id = 2, ArName = "كافيه" },
            //    new { Id=3, ArName="دليفري"},
            //    new { Id=4, ArName="فرع"}}, "Id", "ArName", salesReturn.CustomerType);
            var sysPageId = QueryHelper.SourcePageId("SalesReturns");
            JournalEntry journal = db.JournalEntries.FirstOrDefault(j => j.SourceId == id && j.SourcePageId == sysPageId);

            foreach (var method in salesReturn.SalesReturnPaymentMethods)
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
            ViewBag.Journal = journal;
            if(journal!=null)
            {
                ViewBag.Journal.DocumentNumber = journal != null ? journal.DocumentNumber.Replace("-", "") : "";
            }
            ViewBag.VoucherDate = salesReturn.VoucherDate.ToString("yyyy-MM-ddTHH:mm"); ;

            return View(salesReturn);
        }
    }
}