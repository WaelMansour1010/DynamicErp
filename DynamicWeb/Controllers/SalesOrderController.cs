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
using System.Data.Entity.Core.Objects;
using System.Threading.Tasks;
using MyERP.Repository;
using DevExpress.XtraPrinting;

namespace MyERP.Controllers
{
    public class SalesOrderController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();
            ViewBag.ShowItemWidthAndHeightAndAreaInTransactions = systemSetting.ShowItemWidthAndHeightAndAreaInTransactions;

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
            var depIds = db.Departments.Where(d => (userId == 1 || db.UserDepartments.Where(u => u.UserId == userId && u.Privilege == true).Select(u => u.DepartmentId).Contains(d.Id)) && d.IsDeleted == false && d.IsActive == true).Select(d => (int?)d.Id);
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("SalesOrder", "View", "Index", null, null, "سند حجز");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<SalesOrder> salesOrders;

            if (string.IsNullOrEmpty(searchWord))
            {
                salesOrders = db.SalesOrders.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId)).Include(s => s.Branch).Include(s => s.Currency).Include(s => s.Department).Include(s => s.Warehouse).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await db.SalesOrders.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId)).CountAsync();
            }
            else
            {
                salesOrders = db.SalesOrders.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && s.DocumentNumber.Contains(searchWord)).Include(s => s.Branch).Include(s => s.Currency).Include(s => s.Department).Include(s => s.Warehouse).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await db.SalesOrders.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && s.DocumentNumber.Contains(searchWord)).CountAsync();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة سند حجز",
                EnAction = "Index",
                ControllerName = "SalesOrder",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(await salesOrders.ToListAsync());
        }

        public async Task<ActionResult> _Index()
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
            var salesOrders = db.SalesOrders.Where(s => s.IsDeleted == false && s.UserId == userId && s.VoucherDate >= cTime && s.VoucherDate <= cTime2);
            return PartialView(await salesOrders.ToListAsync());
        }

        public async Task<ActionResult> Panel(int? id)
        {
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
            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();
            ViewBag.ApplyTaxes = systemSetting.ApplyTaxesOnSalesIvoiceAuto;
            CashboxReposistory cashboxReposistory = new CashboxReposistory(db);
            ViewBag.UseRefundableInsurancesForAccessories = systemSetting.UseRefundableInsurancesForAccessories;
            ViewBag.ServiceFeesPercentage = systemSetting.ServiceFeesPercentage.HasValue ? systemSetting.ServiceFeesPercentage : 0;
            ViewBag.CashBoxId = await db.UserCashBoxes.Where(x => x.UserId == userId && x.Privilege == true).Select(x => x.CashBoxId).FirstOrDefaultAsync();
            ViewBag.EnableAdditionalItemsOnPos = systemSetting.EnableAdditionalItemsOnPos;
            ViewBag.DefaultQuantityInPos = systemSetting.DefaultQuantityInPos;
            ViewBag.AllowToAddSameItemMultipleTimes = systemSetting.AllowToAddSameItemMultipleTimes;
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            WarehouseRepository warehouseRepository = new WarehouseRepository(db);
            ViewBag.ShowCashBoxBalance = systemSetting.ShowCashBoxBalance;

            if (id == null)
            {
                ViewBag.NewInvoice = true;
                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.WarehouseId = new SelectList(warehouseRepository.UserWarehouses(userId, systemSetting.DefaultDepartmentId), "Id", "ArName", systemSetting.DefaultWarehouseId);
                ViewBag.CustomerType = new SelectList(new List<dynamic>
                {
                    new { Id=1, ArName="تيك اواي"},
                    new { Id = 2, ArName = "كافيه" },
                new { Id=3, ArName="دليفري"},
                new { Id=4, ArName="فرع"}}, "Id", "ArName", 1);
                ViewBag.PaymentMethodId = new SelectList(new List<dynamic>
                {new { Id=1, ArName="كاش"}, new { Id=4, ArName="فيزا"}}, "Id", "ArName");
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
                //exclude prevoius reservation from sales order payment
                List<PaymentMethod> paymentMethods = db.PaymentMethods.Where(p => p.IsActive == true && p.IsDeleted == false && p.ForPos == true && p.Id != 3).ToList();
                ViewBag.PaymentMethods = paymentMethods;
                var cashBoxSelectList = new SelectList(await cashboxReposistory.UserCashboxes(userId, systemSetting.DefaultDepartmentId).ToListAsync(), "Id", "ArName", systemSetting.DefaultCashBoxId);
                ViewBag.CashBoxId = cashBoxSelectList;


                ViewBag.ReservationMethodId = new SelectList(new List<dynamic>
                {
                    new { Id=1, ArName="عن طريق التليفون"},
                    new { Id=2, ArName="عن طريق الايميل"},
                    new { Id=3, ArName="اونلاين"},
                    new { Id=4, ArName="الحضور للفرع"}
                }, "Id", "ArName");

                ViewBag.AdministrativeDepartmentId = new SelectList(db.AdministrativeDepartments.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");


                return View();
            }
            SalesOrder salesOrder = await db.SalesOrders.FindAsync(id);
            if (salesOrder == null)
            {
                return HttpNotFound();
            }
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", salesOrder.DepartmentId);
            ViewBag.WarehouseId = new SelectList(warehouseRepository.UserWarehouses(userId, salesOrder.DepartmentId), "Id", "ArName", salesOrder.WarehouseId);
            ViewBag.CustomerType = new SelectList(new List<dynamic>
                {
                    new { Id=1, ArName="تيك اواي"},
                    new { Id = 2, ArName = "كافيه" },
                new { Id=3, ArName="دليفري"},
                new { Id=4, ArName="فرع"}}, "Id", "ArName", salesOrder.CustomerType);
            ViewBag.PaymentMethodId = new SelectList(new List<dynamic>
                {new { Id=1, ArName="كاش"}, new { Id=4, ArName="فيزا"}}, "Id", "ArName", salesOrder.SalesOrderPaymentMethods.OrderByDescending(x => x.Amount).Select(x => x.PaymentMethodId).FirstOrDefault());
            ViewBag.VoucherDate = salesOrder.VoucherDate.ToString("yyyy-MM-ddTHH:mm");
            if (salesOrder.DeliveryDate.HasValue)
                ViewBag.DeliveryDate = salesOrder.DeliveryDate.Value.ToString("yyyy-MM-ddTHH:mm");
            var cashBoxes = await cashboxReposistory.UserCashboxes(userId, salesOrder.DepartmentId).ToListAsync();
            foreach (var method in salesOrder.SalesOrderPaymentMethods)
            {
                if (method.PaymentMethodId == 1)
                {
                    ViewBag.CashBoxId = new SelectList(cashBoxes, "Id", "ArName", method.CashBoxId);
                }

            }

            ViewBag.ReservationMethodId = new SelectList(new List<dynamic>
                {
                    new { Id=1, ArName="عن طريق التليفون"},
                    new { Id=2, ArName="عن طريق الايميل"},
                    new { Id=3, ArName="اونلاين"},
                    new { Id=4, ArName="الحضور للفرع"}
                }, "Id", "ArName", salesOrder.ReservationMethodId);

            ViewBag.AdministrativeDepartmentId = new SelectList(db.AdministrativeDepartments.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", salesOrder.AdministrativeDepartmentId);
            return View(salesOrder);
        }

        public async Task<ActionResult> AddEdit(int? id)
        {
            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();
            ViewBag.ShowItemWidthAndHeightAndAreaInTransactions = systemSetting.ShowItemWidthAndHeightAndAreaInTransactions;
            ViewBag.ShowSerialNumbers = systemSetting.ShowSerialNumbers == true;
            ViewBag.ServiceFeesPercentage = systemSetting.ServiceFeesPercentage.HasValue ? systemSetting.ServiceFeesPercentage : 0;
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var departments = await departmentRepository.UserDepartments(1).ToListAsync();//show all deprartments so that user can select factory department
            ViewBag.LinkWithDocModalDepartmentId = new SelectList(departments, "Id", "ArName", systemSetting.DefaultDepartmentId);

            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            var canChangeItemPrice = userId == 1 ? true : await db.UserPrivileges.Where(u => u.PageAction.EnName == "ChangeItemPrice" && u.PageAction.Action == "ChangeItemPrice" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefaultAsync();
            ViewBag.CanChangeItemPrice = canChangeItemPrice == true;

            var CanAccessInsallmentPlan = userId == 1 ? true : await db.UserPrivileges.Where(u => u.PageAction.EnName == "CanAccessInsallmentPlan" && u.PageAction.Action == "CanAccessInsallmentPlan" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefaultAsync();
            ViewBag.CanAccessInsallmentPlan = CanAccessInsallmentPlan == true;
            ViewBag.CanPrintInvoice = false;//allow only admin and branch manager to print from index
            var roleId = db.ERPUsers.Where(u => u.Id == userId).FirstOrDefault().RoleId;
            if (userId == 1 || roleId == 4)
                ViewBag.CanPrintInvoice = true;
            if (id == null)
            {
                if (userId == 1)
                {
                    ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                    {
                        b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", systemSetting.DefaultDepartmentId);
                    ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false && b.DepartmentId == systemSetting.DefaultDepartmentId).Select(b => new
                    {
                        b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", systemSetting.DefaultWarehouseId);
                }
                else
                {
                    ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId && d.Department.IsDeleted == false && d.Department.IsActive == true && d.Privilege == true).Select(b => new
                    {
                        Id = b.DepartmentId,
                        ArName = b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName", systemSetting.DefaultDepartmentId);
                    ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId && b.Warehouse.DepartmentId == systemSetting.DefaultDepartmentId).Select(b => new
                    {
                        Id = b.WareHouseId,
                        ArName = b.Warehouse.Code + " - " + b.Warehouse.ArName
                    }), "Id", "ArName", systemSetting.DefaultWarehouseId);
                }

                ViewBag.VendorOrCustomerId = new SelectList(db.Customers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.CustomerRepId = new SelectList(db.CustomerReps.Where(a => (a.IsActive == true)).OrderBy(a => a.IsDefault).Select(b => new
                {
                    b.Employee.Id,
                    ArName = b.Employee.Code + " - " + b.Employee.ArName
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
                return View();
            }
            SalesOrder salesOrder = await db.SalesOrders.FindAsync(id);
            if (salesOrder == null)
            {
                return HttpNotFound();
            }

            ViewBag.VendorOrCustomerId = new SelectList(db.Customers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", salesOrder.VendorOrCustomerId);

            ViewBag.CustomerRepId = new SelectList(db.CustomerReps.Where(a => (a.IsActive == true)).OrderBy(a => a.IsDefault).Select(b => new
            {
                b.Employee.Id,
                ArName = b.Employee.Code + " - " + b.Employee.ArName
            }), "Id", "ArName", salesOrder.CustomerRepId);

            ViewBag.SatusId = new SelectList(db.SalesOrderStatus.Where(a => (a.IsActive == true)).OrderBy(a => a.StatusNameAr).Select(b => new
            {
                b.Id,
                ArName = b.StatusNameAr
            }), "Id", "ArName", salesOrder.SatusId);

            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", salesOrder.DepartmentId);


                ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false && b.DepartmentId == salesOrder.DepartmentId).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", salesOrder.WarehouseId);

            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId && d.Privilege == true && d.Department.IsActive == true && d.Department.IsDeleted == false).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", salesOrder.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId && b.Privilege == true && b.Warehouse.IsActive == true && b.Warehouse.IsDeleted == false && b.Warehouse.DepartmentId == salesOrder.DepartmentId).Select(b => new
                {
                    Id = b.WareHouseId,
                    ArName = b.Warehouse.Code + " - " + b.Warehouse.ArName
                }), "Id", "ArName", salesOrder.WarehouseId);

            }

            ViewBag.VoucherDate = salesOrder.VoucherDate.ToString("yyyy-MM-ddTHH:mm");

            ViewBag.Next = QueryHelper.Next((int)id, "SalesOrder");
            ViewBag.Previous = QueryHelper.Previous((int)id, "SalesOrder");
            ViewBag.Last = QueryHelper.GetLast("SalesOrder");
            ViewBag.First = QueryHelper.GetFirst("SalesOrder");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل سند حجز",
                EnAction = "AddEdit",
                ControllerName = "SalesOrder",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = salesOrder.Id,
                CodeOrDocNo = salesOrder.DocumentNumber
            });
            return View(salesOrder);
        }

        [HttpPost]
        public async Task<JsonResult> AddEdit(SalesOrder salesOrder)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var ProjectName = System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"];
            salesOrder.UserId = userId;
            int? cashierUserId = null;
            int? posId = null, shiftId = null;
            var pos = db.Pos.Where(p => p.CurrentCashierUserId == userId && p.PosStatusId == 2).FirstOrDefault();
            if (pos != null)
            {
                posId = pos.Id;
                cashierUserId = userId;
                shiftId = pos.CurrentShiftId;
            }
            if (posId == null)
            {
                if (ProjectName == "Genoise")
                {
                    if (salesOrder.DepartmentId != 28)
                    {
                        return Json(new { success = "false", errors = "NotCashier" });
                    }
                }


            }
            if (ModelState.IsValid)
            {
                var id = salesOrder.Id;
                salesOrder.IsDeleted = false;
                //----------------- Edit--------------------->>
                if (salesOrder.Id > 0)
                {
                    var sO = await db.SalesOrders.Where(x => x.Id == salesOrder.Id).Select(x => new { x.IsPosted, x.IsCompleted }).FirstOrDefaultAsync();
                    if (sO.IsPosted == true)
                        return Json(new { success = "false", errors = "IsPosted" });
                    else if (sO.IsCompleted == true)
                        return Json(new { success = "false", errors = "IsCompleted" });

                    MyXML.xPathName = "Details";
                    var SalesOrderDetails = MyXML.GetXML(salesOrder.SalesOrderDetails);
                    MyXML.xPathName = "PaymentMethods";
                    var salesOrderPaymentMethods = MyXML.GetXML(salesOrder.SalesOrderPaymentMethods);

                    await Task.Run(() => db.SalesOrder_Update(salesOrder.Id,
                        salesOrder.DocumentNumber,
                        salesOrder.BranchId,
                        salesOrder.WarehouseId,
                        salesOrder.DepartmentId,
                        salesOrder.VoucherDate,
                        salesOrder.VendorOrCustomerId,
                        salesOrder.CurrencyId,
                        salesOrder.CurrencyEquivalent,
                        salesOrder.Total,
                        salesOrder.TotalItemsDiscount,
                        salesOrder.SalesTaxes,
                        salesOrder.TotalAfterTaxes,
                        salesOrder.VoucherDiscountValue,
                        salesOrder.VoucherDiscountPercentage,
                        salesOrder.NetTotal,
                        salesOrder.Paid,
                        salesOrder.DestinationWarehouseId,
                        salesOrder.SystemPageId,
                        salesOrder.SelectedId,
                        salesOrder.TotalCostPrice,
                        salesOrder.TotalItemDirectExpenses,
                        salesOrder.CommercialRevenueTaxAmount,
                        salesOrder.IsDelivered,
                        salesOrder.IsAccepted,
                        salesOrder.IsLinked,
                        salesOrder.IsCompleted,
                        salesOrder.IsPosted,
                        userId, salesOrder.IsActive,
                        salesOrder.IsDeleted,
                        salesOrder.AutoCreated,
                        salesOrder.Notes,
                        salesOrder.Image,
                        salesOrder.UpdatedId,
                        salesOrder.DeliveredTo,
                        SalesOrderDetails,
                        salesOrderPaymentMethods,
                        salesOrder.ServiceFees,
                        salesOrder.CustomerType,
                        salesOrder.DeliveryDate,
                        posId,
                        cashierUserId,
                        shiftId,
                        false,
                        false,
                        salesOrder.ReservationMethodId,
                        salesOrder.AdministrativeDepartmentId,
                        salesOrder.SatusId,
                        salesOrder.CartId,
                        salesOrder.CustomerAddressId,
                        salesOrder.CustomerRepId,
                        salesOrder.DeliveryCost, salesOrder.RefundableInsurance, salesOrder.PosCustomerId

                     ));

                    Notification.GetNotification("SalesOrder", "Edit", "AddEdit", id, null, "سند حجز");
                }
                //----------------------------- ADD--------------------->>
                else
                {
                    salesOrder.IsActive = true;
                    MyXML.xPathName = "Details";
                    var SalesOrderDetails = MyXML.GetXML(salesOrder.SalesOrderDetails);
                    MyXML.xPathName = "PaymentMethods";
                    var salesOrderPaymentMethods = MyXML.GetXML(salesOrder.SalesOrderPaymentMethods);

                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    await Task.Run(() => db.SalesOrder_Insert(idResult, salesOrder.BranchId, salesOrder.WarehouseId, salesOrder.DepartmentId, salesOrder.VoucherDate, salesOrder.VendorOrCustomerId, salesOrder.CurrencyId, salesOrder.CurrencyEquivalent, salesOrder.Total, salesOrder.TotalItemsDiscount, salesOrder.SalesTaxes, salesOrder.TotalAfterTaxes, salesOrder.VoucherDiscountValue, salesOrder.VoucherDiscountPercentage, salesOrder.NetTotal, salesOrder.Paid, salesOrder.DestinationWarehouseId, salesOrder.SystemPageId, salesOrder.SelectedId, salesOrder.TotalCostPrice, salesOrder.TotalItemDirectExpenses, salesOrder.CommercialRevenueTaxAmount, salesOrder.IsDelivered, salesOrder.IsAccepted, salesOrder.IsLinked, salesOrder.IsCompleted, false, userId, salesOrder.IsActive, salesOrder.IsDeleted, salesOrder.AutoCreated, salesOrder.Notes, salesOrder.Image, salesOrder.UpdatedId, salesOrder.DeliveredTo, SalesOrderDetails, salesOrderPaymentMethods, salesOrder.ServiceFees, salesOrder.CustomerType, salesOrder.DeliveryDate, posId, cashierUserId, shiftId, false, false,
                        salesOrder.ReservationMethodId,
                        salesOrder.AdministrativeDepartmentId,
                        salesOrder.SatusId,
                        salesOrder.CartId,
                        salesOrder.CustomerAddressId,
                        salesOrder.CustomerRepId,
                        salesOrder.DeliveryCost, salesOrder.RefundableInsurance, salesOrder.PosCustomerId
                        ));
                    id = (int)idResult.Value;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("SalesOrder", "Add", "AddEdit", id, null, "سند حجز");
                }

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = salesOrder.Id > 0 ? "تعديل سند حجز " : "اضافة سند حجز",
                    EnAction = "AddEdit",
                    ControllerName = "SalesOrder",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = salesOrder.Id > 0 ? salesOrder.Id : db.SalesOrders.Max(i => i.Id),
                    CodeOrDocNo = salesOrder.DocumentNumber
                });

                //try
                //{
                //    SalesOrder_Report rpt = new SalesOrder_Report();

                //    SalesOrder si = db.SalesOrders.Find(id);
                //    rpt.Parameters["Id"].Value = id;
                //    rpt.Parameters["DocNum"].Value = si.DocumentNumber;
                //    rpt.Parameters["DepId"].Value = si.DepartmentId;
                //    rpt.RequestParameters = false;
                //    rpt.CreateDocument();
                //    PrintToolBase tool = new PrintToolBase(rpt.PrintingSystem);
                //    tool.Print();
                //}
                //catch(Exception ex)
                //{
                //    return Json(new { success = "true", id, printingError = ex });

                //}

                return Json(new { success = "true", id });
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

            return Json(new { success = "false", errors });
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
            var lastObj = db.SalesOrders.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.SalesOrders.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Month == VoucherDate.Value.Month && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.SalesOrders.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "SalesOrder");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            SalesOrder salesOrder = db.SalesOrders.Find(id);
            if (salesOrder.IsPosted == true)
            {
                return Content("false");
            }
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            salesOrder.IsDeleted = true;
            Random random = new Random();
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
            salesOrder.DocumentNumber = Code;
            db.Entry(salesOrder).State = EntityState.Modified;
            db.SaveChanges();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = " حذف اوامر الشراء",
                EnAction = "AddEdit",
                ControllerName = "PurchaseOrder",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                CodeOrDocNo = salesOrder.DocumentNumber
            });

            ////-------------------- Notification-------------------------////
            Notification.GetNotification("SalesOrder", "Delete", "Delete", id, null, "سند حجز");

            return Content("true");
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