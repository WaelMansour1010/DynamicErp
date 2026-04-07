using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using MyERP.Repository;
using System.Security.Claims;
using System.Data.Entity.Core.Objects;
using System.Threading.Tasks;
using Newtonsoft.Json;
using DevExpress.XtraPrinting;
using System.Text.RegularExpressions;
using System.Data.SqlClient;


namespace MyERP.Controllers
{
    public class SalesInvoiceController : ViewToStringController
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: SalesInvoice
        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "", int departmentId = 0)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var depIds = await departmentRepository.UserDepartmentsIds(userId);
            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();
            ViewBag.InabilityToEditSalesAndPurchaseInvoicesAndReturns= systemSetting.InabilityToEditSalesAndPurchaseInvoicesAndReturns;
            ViewBag.ShowSerialNumbers = systemSetting.ShowSerialNumbers == true;
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("SalesInvoice", "View", "Index", null, null, "فواتير البيع");
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            ViewBag.domainName = domainName;
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
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            Repository<SalesInvoice> repository = new Repository<SalesInvoice>(db);
            IQueryable<SalesInvoice> salesInvoices;

            if (string.IsNullOrEmpty(searchWord))
            {
                salesInvoices = repository.GetAll().Where(s => s.IsDeleted == false && s.SystemPageId != 10366 && depIds.Contains(s.DepartmentId) && (departmentId == 0 || s.DepartmentId == departmentId) && s.PosId == null).Include(s => s.Department).Include(s => s.Warehouse).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await repository.GetAll().Where(s => s.IsDeleted == false && s.SystemPageId != 10366 && depIds.Contains(s.DepartmentId) && (departmentId == 0 || s.DepartmentId == departmentId) && s.PosId == null).CountAsync();
            }
            else
            {
                salesInvoices = repository.GetAll().Where(s => s.IsDeleted == false && s.SystemPageId != 10366 && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Customer.ArName.Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord) || s.VoucherDate.ToString().Contains(searchWord)) && (departmentId == 0 || s.DepartmentId == departmentId) && s.PosId == null).Include(s => s.Branch).Include(s => s.Currency).Include(s => s.Department).Include(s => s.Warehouse).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await repository.GetAll().Where(s => s.IsDeleted == false && s.SystemPageId != 10366 && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Customer.ArName.Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord) || s.VoucherDate.ToString().Contains(searchWord)) && (departmentId == 0 || s.DepartmentId == departmentId) && s.PosId == null).CountAsync();
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
            ViewBag.PrintReceiptInsteadOfSalesInvoice = db.SystemSettings.Select(a => a.PrintReceiptInsteadOfSalesInvoice).FirstOrDefault();

            return View(await salesInvoices.ToListAsync());
        }

        // GET: SalesInvoice/Edit/5
        public async Task<ActionResult> AddEdit(int? id, int? salesOrderId, int? OrderId)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            FillVehicleLookupData(session);
            ViewBag.OrderId = OrderId;
            //-- Check if this Sales Invoice Exist In Cash Receipt Voucher
            var check = db.CheckSalesInvoiceExistInCashReceiptVoucher(id).FirstOrDefault();
            ViewBag.check = check;
            //ViewBag.PrintReceiptInsteadOfSalesInvoice = db.SystemSettings.Select(a => a.PrintReceiptInsteadOfSalesInvoice).FirstOrDefault();
            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();
            ViewBag.ShowSerialNumbers = systemSetting.ShowSerialNumbers == true;
            ViewBag.PayViaCarryOver = systemSetting.PayViaCarryOver == true;
            ViewBag.PayViaCash = systemSetting.PayViaCash == true;
            ViewBag.PayViaCheque = systemSetting.PayViaCheque == true;
            ViewBag.PayViaVisa = systemSetting.PayViaVisa == true;
            ViewBag.ServiceFeesPercentage = systemSetting.ServiceFeesPercentage.HasValue ? systemSetting.ServiceFeesPercentage : 0;
            ViewBag.UseCostCenter = systemSetting.UseCostCenter;
            ViewBag.ApplyTaxes = systemSetting.ApplyTaxesOnSalesIvoiceAuto;
            ViewBag.DefaultQuantityInPos = systemSetting.DefaultQuantityInPos;
            ViewBag.AllowDiscountPerItemInSalesInvoice = systemSetting.AllowDiscountPerItemInSalesInvoice;
            ViewBag.UseExpiryDateForItems = systemSetting.UseExpiryDateForItems;
            ViewBag.UseAvgCostPrice = systemSetting.UseAvgCostPrice;
            ViewBag.BarcodeUsingCameraInSalesInvoice = systemSetting.BarcodeUsingCameraInSalesInvoice;
            ViewBag.ShowSalesInvoiceProfit = systemSetting.ShowSalesInvoiceProfit;
            ViewBag.UseMultiWarehousesInSalesInvoice = systemSetting.UseMultiWarehousesInSalesInvoice;
            ViewBag.ShowItemWidthAndHeightAndAreaInTransactions = systemSetting.ShowItemWidthAndHeightAndAreaInTransactions;
            ViewBag.UseOverDraftPolicy = systemSetting.UseOverDraftPolicy;
            ViewBag.LinkSalesRepresentativesWithCustomers = systemSetting.LinkSalesRepresentativesWithCustomers;

            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var departments = await departmentRepository.UserDepartments(1).ToListAsync();//show all deprartments so that user can select factory department
            ViewBag.LinkWithDocModalDepartmentId = new SelectList(departments, "Id", "ArName", systemSetting.DefaultDepartmentId);
            ViewBag.ProjectName = System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"];
            UserRepository userRepository = new UserRepository(db);
            WarehouseRepository warehouseRepository = new WarehouseRepository(db);
            CashboxReposistory cashboxReposistory = new CashboxReposistory(db);

            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            ViewBag.IsPos = false;

            if (Session["distribute"] != null)
            {
                if (Session["distribute"].ToString() == "True")
                {
                    ViewBag.UseMultiWarehousesInSalesInvoice = true;
                }
                else
                {
                    ViewBag.UseMultiWarehousesInSalesInvoice = false;
                }
                //Session.Clear();
            }
            else
            {
                ViewBag.UseMultiWarehousesInSalesInvoice = systemSetting.UseMultiWarehousesInSalesInvoice;
            }

            ViewBag.PrintReceiptInsteadOfSalesInvoice = systemSetting.PrintReceiptInsteadOfSalesInvoice;

            int roleId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("RoleId").Value);
            var ShowCost = userId == 1 ? true : db.UserPrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "ShowAvgCostPrice" && u.PageAction.Action == "ShowAvgCostPrice" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefault();
            ShowCost = ShowCost ?? db.RolePrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "ShowAvgCostPrice" && u.PageAction.Action == "ShowAvgCostPrice" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefault();
            if (ShowCost != true)
                ShowCost = false;
            ViewBag.ShowCost = ShowCost;

            var CanChangeItemPrice = userId == 1 ? true : db.UserPrivileges.Where(u => u.PageAction.PageId == 58 && u.PageAction.EnName == "ChangeItemPrice" && u.PageAction.Action == "ChangeItemPrice" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefault();
            CanChangeItemPrice = CanChangeItemPrice ?? db.RolePrivileges.Where(u => u.PageAction.PageId == 58 && u.PageAction.EnName == "ChangeItemPrice" && u.PageAction.Action == "ChangeItemPrice" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefault();
            ViewBag.CanChangeItemPrice = CanChangeItemPrice;
            //ViewBag.CanChangeItemPrice = await userRepository.HasActionPrivilege(userId, "ChangeItemPrice", "ChangeItemPrice");

            var CanAccessInsallmentPlan = userId == 1 ? true : db.UserPrivileges.Where(u => u.PageAction.PageId == 58 && u.PageAction.EnName == "CanAccessInsallmentPlan" && u.PageAction.Action == "CanAccessInsallmentPlan" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefault();
            CanAccessInsallmentPlan = CanAccessInsallmentPlan ?? db.RolePrivileges.Where(u => u.PageAction.PageId == 58 && u.PageAction.EnName == "CanAccessInsallmentPlan" && u.PageAction.Action == "CanAccessInsallmentPlan" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefault();
            ViewBag.CanAccessInsallmentPlan = CanAccessInsallmentPlan;
            //ViewBag.CanAccessInsallmentPlan = await userRepository.HasActionPrivilege(userId, "CanAccessInsallmentPlan", "CanAccessInsallmentPlan");

            var banks = await db.Banks.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }).ToListAsync();

            if (id == null && salesOrderId == null)
            {
                //---------- Create Order Sales Invoice -------------------//
                if (OrderId != null)
                {
                    Order order = db.Orders.Find(OrderId);
                    SalesInvoice OrdersalesInvoice = new SalesInvoice();
                    List<SalesInvoiceDetail> salesInvoiceDetails = new List<SalesInvoiceDetail>();
                    OrdersalesInvoice.Id = 0;
                    foreach (var item in order.OrderItems)
                    {
                        SalesInvoiceDetail salesInvoiceDetail = new SalesInvoiceDetail();
                        salesInvoiceDetail.ItemId = (int)item.ItemId;
                        salesInvoiceDetail.Price = (decimal)item.Price;
                        salesInvoiceDetail.Qty = 1;
                        salesInvoiceDetail.UnitEquivalent = db.ItemPrices.Where(a => a.IsActive == true && a.IsDeleted == false && a.ItemId == item.ItemId).FirstOrDefault().ItemUnit.Equivalent;
                        salesInvoiceDetail.Item = db.Items.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == item.ItemId).FirstOrDefault();
                        salesInvoiceDetail.ItemPriceId = db.ItemPrices.Where(a => a.IsActive == true && a.IsDeleted == false && a.ItemId == item.ItemId).FirstOrDefault().Id;
                        salesInvoiceDetail.ItemPrice = db.ItemPrices.Where(a => a.IsActive == true && a.IsDeleted == false && a.ItemId == item.ItemId).FirstOrDefault();
                        salesInvoiceDetail.ItemUnitId = db.ItemPrices.Where(a => a.IsActive == true && a.IsDeleted == false && a.ItemId == item.ItemId).FirstOrDefault().ItemUnitId;
                        salesInvoiceDetail.ItemUnit = db.ItemPrices.Where(a => a.IsActive == true && a.IsDeleted == false && a.ItemId == item.ItemId).FirstOrDefault().ItemUnit;
                        salesInvoiceDetails.Add(salesInvoiceDetail);
                    }

                    OrdersalesInvoice.SalesInvoiceDetails = salesInvoiceDetails;
                    ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", order.DepartmentId);
                    ViewBag.WarehouseId = new SelectList(warehouseRepository.UserWarehouses(userId, systemSetting.DefaultDepartmentId), "Id", "ArName", systemSetting.DefaultWarehouseId);
                    var cashBoxSelectList = new SelectList(await cashboxReposistory.UserCashboxes(userId, systemSetting.DefaultDepartmentId).ToListAsync(), "Id", "ArName", systemSetting.DefaultCashBoxId);
                    ViewBag.CashBoxId = cashBoxSelectList;
                    ViewBag.InstallmentActualPaymentCashBoxId = cashBoxSelectList;

                    ViewBag.VendorOrCustomerId = new SelectList(db.Customers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                    {
                        b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", order.CustomerId);

                    ViewBag.CustomerRepId = new SelectList(db.CustomerReps.Where(a => (a.IsActive == true)).OrderBy(a => a.IsDefault).Select(b => new
                    {
                        b.Employee.Id,
                        ArName = b.Employee.Code + " - " + b.Employee.ArName
                    }), "Id", "ArName");

                    ViewBag.CostCenterId = new SelectList(db.CostCenters.Where(a => a.IsActive && !a.IsDeleted && a.TypeId == 2).Select(b => new
                    {
                        b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName");

                    ViewBag.PaymentType = new SelectList(new List<dynamic>
                {
                    new { Id=1, ArName="نقدى"},
                    new { Id = 2, ArName = "آجل" },
                new { Id=3, ArName="متعدد"}}, "Id", "ArName");
                    ViewBag.IssueMethodId = new SelectList(new List<dynamic>
                {
                    new { Id=1, ArName="صرف مباشر من المخزن"},
                    new { Id = 2, ArName = "سحب على المكشوف" }
            }, "Id", "ArName");
                    List<PaymentMethod> paymentMethods = db.PaymentMethods.Where(p => p.IsActive == true && p.IsDeleted == false).ToList();
                    ViewBag.PaymentMethods = paymentMethods;
                    ViewBag.VoucherDate = order.Date.Value.ToString("yyyy-MM-ddTHH:mm");
                    return View(OrdersalesInvoice);
                }
                //---------- End Of Order Sales Invoice -------------------//
                else
                {
                    var bankAccounts = await db.BankAccounts.Where(a => a.IsActive == true && a.IsDeleted == false && a.BankId == systemSetting.DefaultBankId).Select(x => new { x.Id, x.AccountNumber }).ToListAsync();
                    ViewBag.BankIdForVisa = new SelectList(banks, "Id", "ArName", systemSetting.DefaultBankId);
                    ViewBag.BankAccountIdForVisa = new SelectList(bankAccounts, "Id", "AccountNumber");
                    ViewBag.BankIdForCheque = new SelectList(banks, "Id", "ArName", systemSetting.DefaultBankId);
                    ViewBag.BankAccountIdForCheque = new SelectList(bankAccounts, "Id", "AccountNumber");
                    List<PaymentMethod> paymentMethods = db.PaymentMethods.Where(p => p.IsActive == true && p.IsDeleted == false).ToList();
                    ViewBag.PaymentMethods = paymentMethods;
                    ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);
                    ViewBag.WarehouseId = new SelectList(warehouseRepository.UserWarehouses(userId, systemSetting.DefaultDepartmentId), "Id", "ArName", systemSetting.DefaultWarehouseId);
                    var cashBoxSelectList = new SelectList(await cashboxReposistory.UserCashboxes(userId, systemSetting.DefaultDepartmentId).ToListAsync(), "Id", "ArName", systemSetting.DefaultCashBoxId);
                    ViewBag.CashBoxId = cashBoxSelectList;
                    ViewBag.InstallmentActualPaymentCashBoxId = cashBoxSelectList;
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
                    ViewBag.CostCenterId = new SelectList(db.CostCenters.Where(a => a.IsActive && !a.IsDeleted && a.TypeId == 2).Select(b => new
                    {
                        b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName");
                    ViewBag.PaymentType = new SelectList(new List<dynamic>
                {
                    new { Id=1, ArName="نقدى"},
                    new { Id = 2, ArName = "آجل" },
                new { Id=3, ArName="متعدد"}}, "Id", "ArName");
                    ViewBag.IssueMethodId = new SelectList(new List<dynamic>
                {
                    new { Id=1, ArName="صرف مباشر من المخزن"},
                    new { Id = 2, ArName = "سحب على المكشوف" }
            }, "Id", "ArName");
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
            }
            SalesInvoice salesInvoice = null;
            if (salesOrderId != null && id == null)
            {
                SalesOrder salesOrder = await new Repository<SalesOrder>(db).GetByIdAsync((int)salesOrderId);
                if (salesOrder == null)
                    return HttpNotFound();

                salesInvoice = new SalesInvoice() { AutoCreated = true, BranchId = salesOrder.BranchId, DepartmentId = salesOrder.DepartmentId, IsActive = true, IsDeleted = false, IsLinked = true, NetTotal = salesOrder.NetTotal, Notes = salesOrder.Notes, SalesTaxes = salesOrder.SalesTaxes, SelectedId = salesOrder.Id, Total = salesOrder.Total, VoucherDate = salesOrder.VoucherDate, SystemPageId = 5194, ServiceFees = salesOrder.ServiceFees, TotalAfterTaxes = salesOrder.TotalAfterTaxes, TotalCostPrice = salesOrder.TotalCostPrice, UserId = salesOrder.UserId, WarehouseId = salesOrder.WarehouseId, VoucherDiscountValue = salesOrder.VoucherDiscountValue, VoucherDiscountPercentage = salesOrder.VoucherDiscountPercentage, VendorOrCustomerId = (int)salesOrder.VendorOrCustomerId, CommercialRevenueTaxAmount = salesOrder.CommercialRevenueTaxAmount };

                salesInvoice.SalesInvoiceDetails = salesOrder.SalesOrderDetails.Select(x => new SalesInvoiceDetail { Item = x.Item, ItemPrice = x.ItemPrice, ItemUnit = x.ItemUnit, ItemId = x.ItemId, ItemPriceId = x.ItemPriceId, ItemUnitId = x.ItemUnitId, Price = x.Price, Qty = x.Qty, UnitEquivalent = x.UnitEquivalent }).ToList();
                salesInvoice.SalesInvoicePaymentMethods = new List<SalesInvoicePaymentMethod>() {
                    new SalesInvoicePaymentMethod() { PaymentMethodId=1,Amount=0},
                new SalesInvoicePaymentMethod() { PaymentMethodId=2,Amount=0},
                new SalesInvoicePaymentMethod() { PaymentMethodId=3,Amount=0},
                new SalesInvoicePaymentMethod() { PaymentMethodId=4,Amount=0}};
            }
            else if (id != null)
            {
                //if (OrderId >0) {
                //    var OrderSystemPageId = db.SystemPages.Where(s => s.TableName == "Order" && s.IsDeleted == false && s.IsActive == true).Select(s => s.Id).FirstOrDefault();
                //    var OrderSalesInvoice = db.SalesInvoices.Where(a => a.IsActive == true && a.IsDeleted == false && a.SystemPageId == OrderSystemPageId && a.SelectedId == id).FirstOrDefault();
                //    ViewBag.IsLinkedWithOrderSalesInvoice = OrderSalesInvoice != null ? true : false;
                //}
                salesInvoice = await db.SalesInvoices.FindAsync(id);
                if (salesInvoice == null)
                    return HttpNotFound();

                ViewBag.Next = QueryHelper.Next((int)id, "SalesInvoice");
                ViewBag.Previous = QueryHelper.Previous((int)id, "SalesInvoice");
                ViewBag.Last = QueryHelper.GetLast("SalesInvoice");
                ViewBag.First = QueryHelper.GetFirst("SalesInvoice");

                int sysPageId = QueryHelper.SourcePageId("SalesInvoice");
                JournalEntry journal = db.JournalEntries.FirstOrDefault(j => j.SourceId == id && j.SourcePageId == sysPageId);
                ViewBag.Journal = journal;
                ViewBag.Journal.DocumentNumber = journal != null ? journal.DocumentNumber.Replace("-", "") : "";
            }
            var cashBoxes = await cashboxReposistory.UserCashboxes(userId, salesInvoice.DepartmentId).ToListAsync();

            ViewBag.InstallmentActualPaymentCashBoxId = new SelectList(cashBoxes, "Id", "ArName");

            ViewBag.VendorOrCustomerId = new SelectList(db.Customers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", salesInvoice.VendorOrCustomerId);

            ViewBag.CustomerRepId = new SelectList(db.CustomerReps.Where(a => a.IsActive == true).OrderBy(a => a.IsDefault).Select(b => new
            {
                b.Employee.Id,
                ArName = b.Employee.Code + " - " + b.Employee.ArName
            }), "Id", "ArName", salesInvoice.CustomerRepId);

            ViewBag.CostCenterId = new SelectList(db.CostCenters.Where(a => (a.IsActive == true && a.IsDeleted == false && a.TypeId == 2)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", salesInvoice.CostCenterId);
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
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", salesInvoice.DepartmentId);
            ViewBag.WarehouseId = new SelectList(warehouseRepository.UserWarehouses(userId, salesInvoice.DepartmentId), "Id", "ArName", salesInvoice.WarehouseId);

            ViewBag.VoucherDate = salesInvoice.VoucherDate.ToString("yyyy-MM-ddTHH:mm");

            if (salesInvoice.IsLinked == true)
            {
                var purchaseInvoices = await db.PurchaseInvoices.Where(x => x.SelectedId == salesInvoice.Id && x.SystemPageId == 58).Select(x => new { x.DocumentNumber, x.Department.ArName }).FirstOrDefaultAsync();
                ViewBag.PurchaseInvoicesAlert = $"فاتورة البيع مرتبطة بفاتورة شراء رقم {purchaseInvoices.DocumentNumber} فرع {purchaseInvoices.ArName}";
            }
            ViewBag.PaymentType = new SelectList(new List<dynamic>
                {
                    new { Id=1, ArName="نقدى"},
                    new { Id = 2, ArName = "آجل" },
                new { Id=3, ArName="متعدد"}}, "Id", "ArName", salesInvoice.PaymentType);

            ViewBag.IssueMethodId = new SelectList(new List<dynamic>
                {
                    new { Id=1, ArName="صرف مباشر من المخزن"},
                    new { Id = 2, ArName = "سحب على المكشوف" }
            }, "Id", "ArName", salesInvoice.IssueMethodId);

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل فاتورة البيع",
                EnAction = "AddEdit",
                ControllerName = "SalesInvoice",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = salesInvoice.Id,
                CodeOrDocNo = salesInvoice.DocumentNumber
            });

            return View(salesInvoice);
        }

        // POST: SalesInvoice/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult AddEdit(SalesInvoice salesInvoice, ICollection<PurchaseSaleSerialNumber> serialNumbers, int? OrderId, string Status, bool? IsPos/*, string WaitersName*/,string DocName,int? DocId)
        {
            var chassisValidationMessage = ValidateVehicleChassisNumbers(salesInvoice.SalesInvoiceDetails, salesInvoice.Id);
            var vehicleStockValidationMessage = ValidateVehicleStockSelection(salesInvoice.SalesInvoiceDetails);
            if (!string.IsNullOrEmpty(vehicleStockValidationMessage))
                return Json(new { isValid = false, message = vehicleStockValidationMessage }, JsonRequestBehavior.AllowGet);
            if (!string.IsNullOrEmpty(chassisValidationMessage))
                return Json(new { isValid = false, message = chassisValidationMessage }, JsonRequestBehavior.AllowGet);
            bool IsUpdated = false;
            var systemSetting = db.SystemSettings.FirstOrDefault();
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var user = db.ERPUsers.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == userId).FirstOrDefault();
            
            var ProjectName = System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"];
            salesInvoice.UserId = userId;
            int? cashierUserId = null;
            int? posId = null, shiftId = null;
            if (IsPos == false)
            {
                posId = null;
                cashierUserId = null;

                shiftId = null;
            }
            else
            {
                if (user.IsWaiter == true)
                {
                    var poswaiter = db.PosWaiters.Where(a => a.WaiterId == user.Id).FirstOrDefault();
                    if (poswaiter != null)
                    {
                        posId = poswaiter.PosId;
                        var pos = db.Pos.Where(p => p.Id == posId).FirstOrDefault();
                        if (pos != null)
                        {
                            cashierUserId = userId;
                            shiftId = pos.CurrentShiftId;
                        }
                    }
                }
                else
                {
                    var pos = db.Pos.Where(p => p.CurrentCashierUserId == userId && p.PosStatusId == 2).FirstOrDefault();
                    if (pos != null)
                    {
                        posId = pos.Id;
                        cashierUserId = userId;
                        shiftId = pos.CurrentShiftId;
                    }
                }
            }

            if (System.Web.Configuration.WebConfigurationManager.AppSettings["MiniPos"] == "true")
            {
                shiftId = salesInvoice.ShiftId;
            }
            if (posId == null)
            {
                if (ProjectName == "Genoise")
                {
                    if (salesInvoice.DepartmentId != 28)
                    {
                        return Json(new { success = "false", errors = "NotCashier" });
                    }
                }


            }
            if (ModelState.IsValid)
            {
                var id = salesInvoice.Id;
                salesInvoice.IsDeleted = false;

                ///*--- Document Coding ---*/
                //var DocumentCoding = "";
                //SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
                //if (systemSetting.DocumentCoding == true)
                //{
                //    DocumentCoding = salesInvoice.DocumentNumber;
                //    //var FixedPart = "";
                //    //var Separator = "";
                //    //if (systemSetting.IsFixedPart == true)
                //    //{
                //    //    FixedPart = systemSetting.FixedPart;
                //    //}
                //    //if (systemSetting.IsSeparator == true)
                //    //{
                //    //    Separator = systemSetting.Separator;
                //    //}
                //    //var NoOfDigits = systemSetting.NoOfDigits;
                //    //DateTime utcNow = DateTime.UtcNow;
                //    //TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                //    //DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
                //    //var Year = cTime.Year.ToString().Remove(0, 2);
                //    //var Month = cTime.Month;
                //    //Month = Month < 10 ? int.Parse("0" + Month) : Month;
                //    //var DepartmentNo = db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == salesInvoice.DepartmentId).FirstOrDefault().Code;
                //    //var RepNo = salesInvoice.CustomerRepId > 0 ? db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == salesInvoice.CustomerRepId).FirstOrDefault().Code : "0";
                //    //RepNo = int.Parse(RepNo) < 10 ? "0" + RepNo : RepNo;
                //    ////--------------- Year & Month Check ---------------------------//
                //    //var OldMonth = "";
                //    //var OldYear = "";
                //    //var lastCode = db.SalesInvoices.Where(a => a.DepartmentId == salesInvoice.DepartmentId).OrderByDescending(a => a.Id).FirstOrDefault().DocumentNumber;

                //    //if ((systemSetting.Separator != null && lastCode.Contains(systemSetting.Separator)) && (systemSetting.FixedPart != null && (lastCode.Contains(systemSetting.FixedPart))))
                //    //{
                //    //    // si12-56-42-45
                //    //    OldMonth = lastCode.Split(char.Parse(systemSetting.Separator))[2];
                //    //    OldYear = lastCode.Split(char.Parse(systemSetting.Separator))[1];
                //    //}
                //    //else if ((systemSetting.FixedPart != null && lastCode.Contains(systemSetting.FixedPart)) && (systemSetting.Separator != null && !lastCode.Contains(systemSetting.Separator)))
                //    //{// si12564245
                //    //    OldMonth = lastCode.Substring(lastCode.LastIndexOf(systemSetting.FixedPart) + systemSetting.FixedPart.Length).Substring(4, 2);
                //    //    OldYear = lastCode.Substring(lastCode.LastIndexOf(systemSetting.FixedPart) + systemSetting.FixedPart.Length).Substring(2, 2);
                //    //}
                //    //else if ((systemSetting.Separator != null && lastCode.Contains(systemSetting.Separator)) && (systemSetting.FixedPart != null && !(lastCode.Contains(systemSetting.FixedPart))))
                //    //{
                //    //    // 12-56-42-45
                //    //    OldMonth = lastCode.Split(char.Parse(systemSetting.Separator))[2];
                //    //    OldYear = lastCode.Split(char.Parse(systemSetting.Separator))[1];
                //    //}
                //    //else
                //    //{
                //    //    // 12564245
                //    //    OldMonth = lastCode.Substring(4, 2);
                //    //    OldYear = lastCode.Substring(2, 2);
                //    //}

                //    ////-----------------------------------------------------------------//
                //    //var DocNo = "";
                //    //if (salesInvoice.DocumentNumber.Length > (int)NoOfDigits)
                //    //{
                //    //    DocNo = salesInvoice.DocumentNumber.Substring(salesInvoice.DocumentNumber.Length - (int)NoOfDigits);// "";
                //    //}
                //    //else
                //    //{
                //    //    DocNo = salesInvoice.DocumentNumber;
                //    //}
                //    ////check if New Month && New Year To sTart DocNo From 1
                //    //if ((OldYear != Year && int.Parse(OldMonth) != Month) || (OldYear == Year && int.Parse(OldMonth) != Month))
                //    //{
                //    //    DocNo = "1";
                //    //}
                //    //else
                //    //{
                //    //    DocNo = salesInvoice.DocumentNumber;
                //    //}
                //    //var diff = NoOfDigits - DocNo.Length; //salesInvoice.DocumentNumber.Length;
                //    //if (diff > 0)
                //    //{
                //    //    for (var i = 0; i < diff; i++)
                //    //    {
                //    //        // DocNo = DocNo;//salesInvoice.DocumentNumber;
                //    //        DocNo = DocNo.Insert(0, "0");
                //    //        salesInvoice.DocumentNumber = DocNo;
                //    //    }
                //    //}
                //    //DocNo = DocNo.Substring(DocNo.Length - (int)systemSetting.NoOfDigits);
                //    //DocumentCoding = FixedPart + DepartmentNo + Separator + Year + Separator + Month + Separator + RepNo + Separator + DocNo;
                //}
                //DocumentCoding = DocumentCoding.Length > 0 ? DocumentCoding : null;
                /*-------**************** End Of Document Coding *****************--------*/

                //  var salesInvoicePrintQueueId = 0;

                if (salesInvoice.Id > 0)
                {
                    var InabilityToEditSalesAndPurchaseInvoicesAndReturns = systemSetting.InabilityToEditSalesAndPurchaseInvoicesAndReturns;
                    if (InabilityToEditSalesAndPurchaseInvoicesAndReturns == true)
                    {
                        return Json(new { success = "Cannot Be Edited" });
                    }

                    IsUpdated = true;
                    if (salesInvoice.SystemPageId == 2077 && salesInvoice.SelectedId != null) // Order Table
                    {
                        return Json(new { success = "IsLinkedWithOrderSalesInvoice" });

                    }
                    if (db.SalesInvoices.Find(salesInvoice.Id).IsPosted == true)
                    {
                        return Json(new { success = "false" });
                    }
                    // Handle datetime zone hours
                    foreach (var detail in salesInvoice.SalesInvoiceDetails)
                    {
                        if (detail.ExpireDate.HasValue)
                            detail.ExpireDate = detail.ExpireDate.Value.AddHours(6);
                    }
                    MyXML.xPathName = "ItemsDistributions";
                    var SalesInvoiceItemDistributions = MyXML.GetXML(salesInvoice.SalesInvoiceItemDistributions);
                    MyXML.xPathName = "Details";
                    var SalesInvoiceDetails = MyXML.GetXML(salesInvoice.SalesInvoiceDetails);
                    MyXML.xPathName = "PaymentMethods";
                    var InvoicePaymentMethods = MyXML.GetXML(salesInvoice.SalesInvoicePaymentMethods);
                    MyXML.xPathName = "SerialNumbers";
                    var SerialNumbersXML = MyXML.GetXML(serialNumbers);
                    MyXML.xPathName = "SalesOrders";
                    var SalesOrdersXml = MyXML.GetXML(salesInvoice.SalesOrdersSalesInvoices);
                    MyXML.xPathName = "PurchaseRequests";
                    var PurchaseRequestsXml = MyXML.GetXML(salesInvoice.SalesInvoicePurchaseRequests);
                    db.SalesInvoice_Update(salesInvoice.Id, salesInvoice.DocumentNumber, salesInvoice.BranchId, salesInvoice.WarehouseId, salesInvoice.DepartmentId, salesInvoice.VoucherDate, salesInvoice.VendorOrCustomerId, salesInvoice.CurrencyId, salesInvoice.CurrencyEquivalent, salesInvoice.Total, salesInvoice.TotalItemsDiscount, salesInvoice.SalesTaxes, salesInvoice.TotalAfterTaxes, salesInvoice.VoucherDiscountValue, salesInvoice.VoucherDiscountPercentage, salesInvoice.NetTotal, salesInvoice.Paid, salesInvoice.ValidityPeriod, salesInvoice.DeliveryPeriod, salesInvoice.CostCenterId, salesInvoice.CurrentQuantity, salesInvoice.DestinationWarehouseId, salesInvoice.SystemPageId, salesInvoice.SelectedId, salesInvoice.TotalCostPrice, salesInvoice.TotalItemDirectExpenses, salesInvoice.CommercialRevenueTaxAmount, salesInvoice.IsDelivered, salesInvoice.IsAccepted, salesInvoice.IsLinked, salesInvoice.IsCompleted, salesInvoice.IsPosted, userId, salesInvoice.IsActive, salesInvoice.IsDeleted, salesInvoice.AutoCreated, salesInvoice.Notes, salesInvoice.Image, salesInvoice.UpdatedId, salesInvoice.CarNumber, salesInvoice.DeliveredTo, salesInvoice.InstallmentAdvance, salesInvoice.TotalInstallmentBeforeProfit, salesInvoice.TotalInstallmentAfterProfit, salesInvoice.InsallmentProfitPercentage, salesInvoice.InstallmentProfitAmount, salesInvoice.InstallmentPaymentAmount, salesInvoice.InstallmentPaymentCount, salesInvoice.TotalAfterTotalInstallment, salesInvoice.FirstInstallmentPaymentDueDate, salesInvoice.IsInstallmentPlan, salesInvoice.DaysBetweenInstallments, SalesInvoiceDetails, InvoicePaymentMethods, SerialNumbersXML, SalesOrdersXml, salesInvoice.ServiceFees, salesInvoice.CustomerType, PurchaseRequestsXml, posId, cashierUserId, shiftId, false, false, salesInvoice.CustomerRepId, salesInvoice.WarehouseItemDistribution, salesInvoice.RefundableInsurance, salesInvoice.DueDate, salesInvoice.PosCustomerId, salesInvoice.IsCollectedByCashier, salesInvoice.TableId, salesInvoice.DeliveryCost, salesInvoice.DeliveryDate, salesInvoice.DeliveryStartDate, salesInvoice.DeliveryEndDate, salesInvoice.DriverId, salesInvoice.CarId, salesInvoice.PaymentType, salesInvoice.IssueMethodId, salesInvoice.SmokingTax/*, WaitersName*/, salesInvoice.WaiterId);
                    SyncVehicleStockForSalesInvoice(salesInvoice.Id);
                    Notification.GetNotification("SalesInvoice", "Edit", "AddEdit", id, null, "فواتير البيع");
                    //if (systemSetting.UsePrintingAssistantProgram == true)
                    //{
                    //    var salesInvoicePrintQueue = new SalesInvoicePrintQueue();
                    //    salesInvoicePrintQueue.SalesInvoiceId = id;
                    //    salesInvoicePrintQueue.PrintOnKitchen = true;
                    //    db.SalesInvoicePrintQueues.Add(salesInvoicePrintQueue);
                    //    db.SaveChanges();
                    //    salesInvoicePrintQueueId = salesInvoicePrintQueue.Id;
                    //}
                    if (systemSetting.UsePrintingAssistantProgram == true)
                    {
                        AddToSalesInvoicePrintQueue(salesInvoice.Id, Status, IsUpdated);
                    }

                }
                else
                {

                    salesInvoice.IsActive = true;
                    if (salesInvoice.SelectedId != null)
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

                        salesInvoice.VoucherDate = cTime;
                    }
                    // Handle datetime zone hours
                    foreach (var detail in salesInvoice.SalesInvoiceDetails)
                    {
                        if (detail.ExpireDate.HasValue)
                            detail.ExpireDate = detail.ExpireDate.Value.AddHours(6);
                    }
                    //MyXML.xPathName = "ItemsDistributions";
                    //var SalesInvoiceItemDistributions = MyXML.GetXML(salesInvoice.SalesInvoiceItemDistributions);
                    MyXML.xPathName = "Details";
                    var SalesInvoiceDetails = MyXML.GetXML(salesInvoice.SalesInvoiceDetails);
                    MyXML.xPathName = "PaymentMethods";
                    var InvoicePaymentMethods = MyXML.GetXML(salesInvoice.SalesInvoicePaymentMethods);
                    MyXML.xPathName = "SerialNumbers";
                    var SerialNumbersXML = MyXML.GetXML(serialNumbers);
                    MyXML.xPathName = "SalesOrders";
                    var SalesOrdersXml = MyXML.GetXML(salesInvoice.SalesOrdersSalesInvoices);
                    MyXML.xPathName = "PurchaseRequests";
                    var PurchaseRequestsXml = MyXML.GetXML(salesInvoice.SalesInvoicePurchaseRequests);
                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    if (OrderId > 0)
                    {
                        salesInvoice.SystemPageId = db.SystemPages.Where(s => s.TableName == "Order" && s.IsDeleted == false && s.IsActive == true).Select(s => s.Id).FirstOrDefault();
                        salesInvoice.SelectedId = OrderId;
                    }
                    salesInvoice.WaiterId = userId;
                    db.SalesInvoice_Insert(idResult, salesInvoice.BranchId, salesInvoice.WarehouseId, salesInvoice.DepartmentId, salesInvoice.VoucherDate, salesInvoice.VendorOrCustomerId, salesInvoice.CurrencyId, salesInvoice.CurrencyEquivalent, salesInvoice.Total, salesInvoice.TotalItemsDiscount, salesInvoice.SalesTaxes, salesInvoice.TotalAfterTaxes, salesInvoice.VoucherDiscountValue, salesInvoice.VoucherDiscountPercentage, salesInvoice.NetTotal, salesInvoice.Paid, salesInvoice.ValidityPeriod, salesInvoice.DeliveryPeriod, salesInvoice.CostCenterId, salesInvoice.CurrentQuantity, salesInvoice.DestinationWarehouseId, salesInvoice.SystemPageId, salesInvoice.SelectedId, salesInvoice.TotalCostPrice, salesInvoice.TotalItemDirectExpenses, salesInvoice.CommercialRevenueTaxAmount, salesInvoice.IsDelivered, salesInvoice.IsAccepted, salesInvoice.IsLinked, salesInvoice.IsCompleted, false, userId, salesInvoice.IsActive, salesInvoice.IsDeleted, salesInvoice.AutoCreated, salesInvoice.Notes, salesInvoice.Image, salesInvoice.UpdatedId, salesInvoice.CarNumber, salesInvoice.DeliveredTo, salesInvoice.InstallmentAdvance, salesInvoice.TotalInstallmentBeforeProfit, salesInvoice.TotalInstallmentAfterProfit, salesInvoice.InsallmentProfitPercentage, salesInvoice.InstallmentProfitAmount, salesInvoice.InstallmentPaymentAmount, salesInvoice.InstallmentPaymentCount, salesInvoice.TotalAfterTotalInstallment, salesInvoice.FirstInstallmentPaymentDueDate, salesInvoice.IsInstallmentPlan, salesInvoice.DaysBetweenInstallments, SalesInvoiceDetails, InvoicePaymentMethods, SerialNumbersXML, SalesOrdersXml, salesInvoice.ServiceFees, salesInvoice.CustomerType, PurchaseRequestsXml, posId, cashierUserId, shiftId, false, false, salesInvoice.CustomerRepId, salesInvoice.WarehouseItemDistribution, salesInvoice.RefundableInsurance, salesInvoice.DueDate, salesInvoice.PosCustomerId, salesInvoice.IsCollectedByCashier, salesInvoice.TableId, salesInvoice.DeliveryCost, salesInvoice.DeliveryDate, salesInvoice.DeliveryStartDate, salesInvoice.DeliveryEndDate, salesInvoice.DriverId, salesInvoice.CarId, salesInvoice.PaymentType, salesInvoice.IssueMethodId, salesInvoice.SmokingTax/*, WaitersName*/, salesInvoice.WaiterId);
                    id = (int)idResult.Value;
                    if (systemSetting.UsePrintingAssistantProgram == true)
                    {
                        AddToSalesInvoicePrintQueue(id, Status, IsUpdated);
                    }

                    //if (systemSetting.UsePrintingAssistantProgram == true)
                    //{
                    //    var salesInvoicePrintQueue = new SalesInvoicePrintQueue();
                    //    salesInvoicePrintQueue.SalesInvoiceId = id;
                    //    salesInvoicePrintQueue.PrintOnKitchen = true;
                    //    db.SalesInvoicePrintQueues.Add(salesInvoicePrintQueue);
                    //    db.SaveChanges();
                    //    salesInvoicePrintQueueId = salesInvoicePrintQueue.Id;
                    //}
                    SyncVehicleStockForSalesInvoice((int)idResult.Value);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("SalesInvoice", "Add", "AddEdit", id, null, "فواتير البيع");
                }

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = salesInvoice.Id > 0 ? "تعديل  فاتورة بيع " : "اضافة   فاتورة بيع",
                    EnAction = "AddEdit",
                    ControllerName = "SalesInvoice",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = salesInvoice.Id > 0 ? salesInvoice.Id : db.SalesInvoices.Max(i => i.Id),
                    CodeOrDocNo = salesInvoice.DocumentNumber
                });

                //try
                //{
                //    var displayFactoryPrice = db.UserPrivileges.Where(u => u.PageAction.EnName == "DisplayFactoryPrice" && u.PageAction.Action == "DisplayFactoryPrice" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefault();
                //    CashierInvoice_Report rpt = new CashierInvoice_Report(displayFactoryPrice == true ? true : false);
                //    SalesInvoice si = db.SalesInvoices.Find(id);
                //    rpt.Parameters["Id"].Value = id;
                //    rpt.Parameters["DepId"].Value = si.DepartmentId;
                //    rpt.Parameters["DocNum"].Value = si.DocumentNumber;
                //    rpt.Parameters["CashierName"].Value = si.ERPUser.Name;
                //    decimal change = Math.Round(((si.Paid ?? 0) - (si.TotalAfterTaxes ?? 0)), 2);
                //    rpt.Parameters["ClientChange"].Value = change > 0 ? change : 0;

                //    string[] salesMan = si.SalesOrdersSalesInvoices.Select(x => x.SalesOrder.ERPUser.Name).Distinct().ToArray();

                //    rpt.Parameters["SalesManName"].Value = string.Join(", ", salesMan);

                //    rpt.RequestParameters = false;

                //    rpt.CreateDocument();
                //    PrintToolBase tool = new PrintToolBase(rpt.PrintingSystem);
                //    tool.Print();
                //}

                //catch(Exception ex)
                //{
                //    return Json(new { success = "true", id ,printingError=ex});
                //}



                //-- To Set IsLinked = true --//
                //if((DocName!=null||DocName.Length>0)&&(DocId!=null||DocId > 0))
                    if ((DocName != null) && (DocId != null))
                    {
                        db.Database.ExecuteSqlCommand($"update {DocName} set IsLinked = 1 where Id = {DocId}");
                     db.SaveChanges();
                }
                return Json(new { success = "true", id, ShiftId = shiftId/*, salesInvoicePrintQueueId = salesInvoicePrintQueueId */});
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

            return Json(new { success = "false", errors });
        }

        [SkipERPAuthorize]
        public JsonResult GetSerialNumber(int? invoiceId, int? itemId)
        {

            // var SerialNumbers = JsonConvert.SerializeObject(db.PurchaseSaleSerialNumbers.Where(s => s.SelectedId == invoiceId && s.ItemId == itemId && s.PageSourceId == db.SystemPages.FirstOrDefault(d => d.TableName == "SalesInvoice").Id).Select(d => new { d.ItemId, d.SerialNumber }));

            var SerialNumbers = db.PurchaseSaleSerialNumbers.Where(s => s.SelectedId == invoiceId && s.ItemId == itemId && s.PageSourceId == db.SystemPages.FirstOrDefault(d => d.TableName == "SalesInvoice").Id).Select(d => new { d.ItemId, d.SerialNumber }).ToList();

            return Json(SerialNumbers, JsonRequestBehavior.AllowGet);
        }

        // GET
        public ActionResult AddEditSerialNumber(int? id, int itemId)
        {
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

            ViewBag.BranchId = new SelectList(db.Branches.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", salesInvoice.BranchId);
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", salesInvoice.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", salesInvoice.WarehouseId);

            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", salesInvoice.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId).Select(b => new
                {
                    Id = b.WareHouseId,
                    ArName = b.Warehouse.Code + " - " + b.Warehouse.ArName
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

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult AddEditSerialNumber(int SalesInvoiceId, int itemId, ICollection<PurchaseSaleSerialNumber> serialNumbers)
        {
            if (ModelState.IsValid)
            {
                if (SalesInvoiceId == 0)
                {
                    return Json("false");
                }
                if (SalesInvoiceId > 0)
                {
                    if (db.SalesInvoices.Find(SalesInvoiceId).IsPosted == true)
                    {
                        return Json("false");
                    }
                    MyXML.xPathName = "SerialNumbers";
                    var SerialNumbersXML = MyXML.GetXML(serialNumbers);
                    db.AddEditSerialNumber(SalesInvoiceId, itemId, QueryHelper.SourcePageId("SalesInvoice"), SerialNumbersXML);

                    Notification.GetNotification("SalesInvoice", "AddEditSerialNumber", "AddEditSerialNumber", SalesInvoiceId, null, "  سيريال فواتير البيع");
                }

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = SalesInvoiceId > 0 ? "تعديل سيريال فاتورة بيع " : "تعديل سيريال فاتورة بيع",
                    EnAction = "AddEditSerialNumber",
                    ControllerName = "SalesInvoice",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = SalesInvoiceId,
                    CodeOrDocNo = db.SalesInvoices.Where(p => p.Id == SalesInvoiceId).FirstOrDefault().DocumentNumber
                });

                return Json(new { success = "true", id = SalesInvoiceId });
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

            return Json(errors);
        }


        [SkipERPAuthorize]
        public ActionResult SalesInvoiceInstallmentDetails(int id)
        {
            return PartialView(db.SalesInvoiceInstallmentDetails.Where(x => x.SalesInvoiceId == id));
        }

        [SkipERPAuthorize]
        public ActionResult SalesInvoiceInstallmentActualPayments(int id)
        {
            return PartialView(db.SalesInvoiceInstallmentActualPayments.Where(x => x.SalesInvoiceId == id));
        }

        [HttpPost]
        [SkipERPAuthorize]
        public ActionResult InsertInstallmentActualPayment(SalesInvoiceInstallmentActualPayment installmentActualPayment)
        {
            db.SalesInvoiceInstallmentActualPayment_Insert(installmentActualPayment.SalesInvoiceId, installmentActualPayment.Date, installmentActualPayment.Amount, installmentActualPayment.CashboxId);
            return Json(InstallmentAndJournalDetails(installmentActualPayment.SalesInvoiceId));
        }

        [HttpPost]
        [SkipERPAuthorize]
        public ActionResult DeleteInstallmentActualPayment(int id, int salesInvoiceId)
        {
            db.SalesInvoiceInstallmentActualPayment_Delete(id);
            return Json(InstallmentAndJournalDetails(salesInvoiceId));
        }

        [SkipERPAuthorize]
        private dynamic InstallmentAndJournalDetails(int salesInvoiceId)
        {
            return new
            {
                InstallmentActualPayments = db.SalesInvoiceInstallmentActualPayments.Where(x => x.SalesInvoiceId == salesInvoiceId).Select(x => new { x.Id, x.Amount, x.CashBox.ArName, x.Date, x.SalesInvoiceId }),
                InstallmentDetails = db.SalesInvoiceInstallmentDetails.Where(x => x.SalesInvoiceId == salesInvoiceId).Select(x => new { x.DueDate, x.Id, x.InstallmentAmount, x.Notes, x.Paid, x.Remaining, x.SalesInvoiceId }),
                JournalEntryDetails = db.JournalEntryDetails.Where(x => (x.Debit != 0 || x.Credit != 0) && x.JournalEntryId == db.JournalEntries.Where(j => j.SourceId == salesInvoiceId && j.SourcePageId == db.SystemPages.Where(s => s.TableName == "SalesInvoice").Select(s => s.Id).FirstOrDefault()).Select(j => j.Id).FirstOrDefault()).Select(x => new { x.Id, x.AccountId, x.ChartOfAccount.ArName, x.Credit, x.Debit })
            };
        }

        // POST: SalesInvoice/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            var systemSetting = db.SystemSettings.FirstOrDefault();
            var InabilityToEditSalesAndPurchaseInvoicesAndReturns = systemSetting.InabilityToEditSalesAndPurchaseInvoicesAndReturns;
            if (InabilityToEditSalesAndPurchaseInvoicesAndReturns == true)
            {
                return Content("Cannot Be Deleted");
            }
            //-- Check if this Sales Invoice Exist In Cash Receipt Voucher
            var check = db.CheckSalesInvoiceExistInCashReceiptVoucher(id).FirstOrDefault();
            if (check > 0)
            {
                return Content("False");
            }
            else
            {
                SalesInvoice SalesInvoice = db.SalesInvoices.Find(id);
                if (SalesInvoice.SystemPageId == 2077 && SalesInvoice.SelectedId != null) // Order Table
                {
                    return Content("IsLinkedWithOrderSalesInvoice");

                }
                if (SalesInvoice.IsPosted == true)
                {
                    return Content("false");
                }
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.SalesInvoice_Delete(id, userId);
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = " حذف فاتورة بيع",
                    EnAction = "AddEdit",
                    ControllerName = "SalesInvoice",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = SalesInvoice.DocumentNumber
                });

                ////-------------------- Notification-------------------------////
                Notification.GetNotification("SalesInvoice", "Delete", "Delete", id, null, "فواتير البيع");

                return Content("true");
            }

        }
        // get last price of item occur in invoices
        [SkipERPAuthorize]
        public JsonResult SetLastPrice(int itemId)
        {
            var lastPrice = db.GetLastItemPrice(itemId).FirstOrDefault();
            return Json(lastPrice, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult SetBankAccounts(int bankId)
        {
            var BankAccountsList = db.GetBankAccountByBankId(bankId).ToList();
            return Json(BankAccountsList, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult SetDocNum(int id/*, int? wareHouseId, int? RepId, DateTime date*/, DateTime? VoucherDate)
        {
            bool IsExistInDocumentsCoding = false;
            var noOfDigits = (int?)0;
            var YearFormat = (int?)0;
            var CodingTypeId = (int?)0;
            var IsZerosFills = (bool?)null;
            var newDocNo = "";
            var GeneratedDocNo = "";
            var lastObj = db.SalesInvoices.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.SalesInvoices.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Month == VoucherDate.Value.Month && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.SalesInvoices.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "SalesInvoice");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);

            //SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            //var NoOfDigits = systemSetting.NoOfDigits /*== null ? i.ToString().Length : systemSetting.NoOfDigits*/;
            //DateTime utcNow = date; //DateTime.UtcNow;
            //TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            //DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
            //var Year = int.Parse(cTime.Year.ToString().Remove(0, 2));
            //var CompleteYear = int.Parse(cTime.Year.ToString());
            //var Month = cTime.Month;
            //Month = Month < 10 ? int.Parse("0" + Month) : Month;
            //var last = db.SalesInvoices.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.WarehouseId == wareHouseId && a.VoucherDate.Year == CompleteYear && a.VoucherDate.Month == Month).OrderByDescending(a => a.Id).FirstOrDefault();  //QueryHelper.DocLastNum(id, "SalesInvoice");
            //double docNo = 0;
            //if (last != null)
            //{
            //    if (last.DocumentNumber.Length > NoOfDigits)
            //    {
            //        docNo = double.Parse(last.DocumentNumber.Substring(last.DocumentNumber.Length - (int)NoOfDigits));
            //    }
            //    else
            //    {
            //        docNo = double.Parse(last.DocumentNumber);
            //    }
            //}
            //double i = (docNo) + 1;
            ////--------------- Document Coding --------------//
            //var DocumentCoding = "";
            //if (systemSetting.DocumentCoding == true)
            //{
            //    var FixedPart = "";
            //    var Separator = "";
            //    if (systemSetting.IsFixedPart == true)
            //    {
            //        FixedPart = systemSetting.FixedPart;
            //    }
            //    if (systemSetting.IsSeparator == true)
            //    {
            //        Separator = systemSetting.Separator;
            //    }
            //    var DepartmentNo = db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == id).FirstOrDefault().Code;
            //    DepartmentNo = int.Parse(DepartmentNo) < 10 ? "0" + DepartmentNo : DepartmentNo;
            //    var WareHouseNo = db.Warehouses.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == wareHouseId).FirstOrDefault().Code;
            //    WareHouseNo = int.Parse(WareHouseNo) < 10 ? "0" + WareHouseNo : WareHouseNo;

            //    var RepNo = RepId > 0 ? db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == RepId).FirstOrDefault().Code : "0";
            //    RepNo = int.Parse(RepNo) < 10 ? "0" + RepNo : RepNo;
            //    var DocNo = i.ToString();
            //    var diff = NoOfDigits - DocNo.Length; //salesInvoice.DocumentNumber.Length;
            //    var ii = i.ToString();
            //    if (diff > 0)
            //    {
            //        for (var a = 0; a < diff; a++)
            //        {
            //            DocNo = DocNo.Insert(0, "0");
            //            ii = DocNo;
            //        }
            //    }
            //    DocNo = DocNo.Substring(DocNo.Length - (int)NoOfDigits);
            //    // DocumentCoding = FixedPart + DepartmentNo + Separator + Year + Separator + Month + Separator + RepNo + Separator + DocNo;
            //    DocumentCoding = FixedPart + DepartmentNo + Separator + Year + Separator + Month + Separator + RepNo + Separator + WareHouseNo + Separator + DocNo;
            //    //i = double.Parse(DocumentCoding);
            //    return Json(DocumentCoding, JsonRequestBehavior.AllowGet);
            //}
            ////-------------------- End Of Document Coding ------------------//
            //return Json(i, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public async Task<JsonResult> GetChangeItemPricePrivilege()
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            UserRepository userRepository = new UserRepository(db);
            // return Json(await userRepository.HasActionPrivilege(userId, "ChangeItemPrice", "ChangeItemPrice"), JsonRequestBehavior.AllowGet);

            int roleId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("RoleId").Value);
            var CanChangeItemPrice = userId == 1 ? true : await db.UserPrivileges.Where(u => u.PageAction.PageId == 58 && u.PageAction.EnName == "ChangeItemPrice" && u.PageAction.Action == "ChangeItemPrice" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefaultAsync();
            CanChangeItemPrice = CanChangeItemPrice ?? await db.RolePrivileges.Where(u => u.PageAction.PageId == 58 && u.PageAction.EnName == "ChangeItemPrice" && u.PageAction.Action == "ChangeItemPrice" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefaultAsync();
            return Json(CanChangeItemPrice, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetCurrentInstallmentAmount(int invoiceId)
        {
            var balance = db.SalesInvoicePaymentMethods.Where(a => a.SalesInvoiceId == invoiceId && a.PaymentMethodId == 2).Select(a => a.Amount).FirstOrDefault();
            return Json(balance, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult SetSession(string val)
        {
            Session["distribute"] = val;
            return Json(val, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetItemPrice(int customerId, int itemId, int departmentId)
        {
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
        public ActionResult LinkToPurchaseInvoice()
        {
            var purchaseRequest = db.PurchaseRequests.Where(x => x.IsDeleted == false && x.IsApproved == true && x.SalesInvoicePurchaseRequests.Count == 0).OrderByDescending(x => x.Id).ToList();
            return PartialView(purchaseRequest);
        }

        [SkipERPAuthorize]
        [HttpPost]
        public ActionResult PurchaseInvoiceDetails(List<int?> purchaseReqeustIds)
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

            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            ViewBag.AllowDiscountPerItemInSalesInvoice = systemSetting.AllowDiscountPerItemInSalesInvoice;
            ViewBag.UseExpiryDateForItems = systemSetting.UseExpiryDateForItems;
            var view = RenderRazorViewToString("PurchaseInvoiceDetails", db.PurchaseRequestDetails.Where(x => purchaseReqeustIds.Contains(x.MainDocId)).ToList());
            return Json(new { view, VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm") }, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public ActionResult LinkToSalesQuotation()
        {
            var SalesQuotation = db.SalesQuotations.Where(x => x.IsDeleted == false && x.IsActive == true&&x.IsLinked!=true).OrderByDescending(x => x.Id).ToList();
            return PartialView(SalesQuotation);
        }

        [SkipERPAuthorize]
        [HttpPost]
        public ActionResult SalesQuotationDetails(int? id)
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

            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            ViewBag.AllowDiscountPerItemInSalesInvoice = systemSetting.AllowDiscountPerItemInSalesInvoice;
            ViewBag.UseExpiryDateForItems = systemSetting.UseExpiryDateForItems;
            var view = RenderRazorViewToString("SalesQuotationDetails", db.SalesQuotationDetails.Where(x => x.MainDocId == id).ToList());
            var salesQuotation = db.SalesQuotations.Where(a => a.Id == id).Select(a => new { a.VoucherDiscountPercentage, a.VoucherDiscountValue, a.SalesTaxes });
            return Json(new { view, VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm"), salesQuotation }, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public ActionResult LinkToSalesInvoice()
        {
            var SalesInvoice = db.SalesInvoices.Where(x => x.IsDeleted == false && x.IsLinked != true && x.PosId == null).OrderByDescending(x => x.Id).ToList();
            return PartialView(SalesInvoice);
        }

        [SkipERPAuthorize]
        [HttpPost]
        public ActionResult SalesInvoiceDetails(int? id)
        {
            //------ Time Zone Depends On Currency --------//
            var Currency = db.Currencies.Where(a => a.IsActive == true && a.IsDeleted == false && a.IsDefault == true).FirstOrDefault();
            var CurrencyCode = Currency != null ? Currency.Code : "";
            TimeZoneInfo info;
            if (CurrencyCode == "SAR")
            {
                info = TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time");// +1H from Egypt Standard Time
            }
            else
            {
                info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            }
            DateTime utcNow = DateTime.UtcNow;
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
            //----------------- End of Time Zone Depends On Currency --------------------//

            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            ViewBag.AllowDiscountPerItemInSalesInvoice = systemSetting.AllowDiscountPerItemInSalesInvoice;
            ViewBag.UseExpiryDateForItems = systemSetting.UseExpiryDateForItems;
            var view = RenderRazorViewToString("SalesInvoiceDetails", db.SalesInvoiceDetails.Where(x => x.MainDocId == id).ToList());
            var salesInvoice = db.SalesInvoices.Where(a => a.Id == id).Select(a => new { a.VoucherDiscountPercentage, a.VoucherDiscountValue, a.SalesTaxes });
            return Json(new { view, VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm"), salesInvoice }, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public ActionResult LinkToSalesOrder()
        {
            var SalesOrder = db.SalesOrders.Where(x => x.IsDeleted == false && x.IsActive == true && x.IsLinked != true).OrderByDescending(x => x.Id).ToList();
            return PartialView(SalesOrder);
        }

        [SkipERPAuthorize]
        [HttpPost]
        public ActionResult SalesOrderDetails(int? id)
        {
            //------ Time Zone Depends On Currency --------//
            var Currency = db.Currencies.Where(a => a.IsActive == true && a.IsDeleted == false && a.IsDefault == true).FirstOrDefault();
            var CurrencyCode = Currency != null ? Currency.Code : "";
            TimeZoneInfo info;
            if (CurrencyCode == "SAR")
            {
                info = TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time");// +1H from Egypt Standard Time
            }
            else
            {
                info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            }
            DateTime utcNow = DateTime.UtcNow;
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
            //----------------- End of Time Zone Depends On Currency --------------------//

            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            ViewBag.AllowDiscountPerItemInSalesInvoice = systemSetting.AllowDiscountPerItemInSalesInvoice;
            ViewBag.UseExpiryDateForItems = systemSetting.UseExpiryDateForItems;
            var view = RenderRazorViewToString("SalesOrderDetails", db.SalesOrderDetails.Where(x => x.MainDocId == id).ToList());
            var salesOrder = db.SalesOrders.Where(a => a.Id == id).Select(a => new { a.VoucherDiscountPercentage, a.VoucherDiscountValue, a.SalesTaxes });
            return Json(new { view, VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm"), salesOrder }, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public ActionResult LinkToPurchaseReturn()
        {
            var PurchaseReturn = db.PurchaseReturns.Where(x => x.IsDeleted == false && x.IsLinked != true).OrderByDescending(x => x.Id).ToList();
            return PartialView(PurchaseReturn);
        }

        [SkipERPAuthorize]
        [HttpPost]
        public ActionResult PurchaseReturnDetails(int? id)
        {
            //------ Time Zone Depends On Currency --------//
            var Currency = db.Currencies.Where(a => a.IsActive == true && a.IsDeleted == false && a.IsDefault == true).FirstOrDefault();
            var CurrencyCode = Currency != null ? Currency.Code : "";
            TimeZoneInfo info;
            if (CurrencyCode == "SAR")
            {
                info = TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time");// +1H from Egypt Standard Time
            }
            else
            {
                info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            }
            DateTime utcNow = DateTime.UtcNow;
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
            //----------------- End of Time Zone Depends On Currency --------------------//

            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            ViewBag.AllowDiscountPerItemInSalesInvoice = systemSetting.AllowDiscountPerItemInSalesInvoice;
            ViewBag.UseExpiryDateForItems = systemSetting.UseExpiryDateForItems;
            var view = RenderRazorViewToString("PurchaseReturnDetails", db.PurchaseReturnsDetails.Where(x => x.MainDocId == id).ToList());
            var purchaseReturn = db.PurchaseReturns.Where(a => a.Id == id).Select(a => new { a.VoucherDiscountPercentage, a.VoucherDiscountValue, a.SalesTaxes });
            return Json(new { view, VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm"), purchaseReturn }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Synchronization()
        {
            var NotSyncedVouchers = db.SalesInvoices.Where(a => a.IsSynced != true).ToList();
            return View(NotSyncedVouchers);
        }
        [HttpPost]
        public ActionResult SynchronizationData(List<int> ids)
        {
            var UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            List<SalesInvoice> salesInvoices = new List<SalesInvoice>();
            string databaseName = "DB_A396C8_MyERP", datasource = "sql5060.site4now.net", userId = "DB_A396C8_MyERP_admin", password = "MyERP@123";
            string connectionString = "data source=" + datasource + ";initial catalog=" + databaseName + ";Integrated Security=false;multipleactiveresultsets=True;user id=" + userId + ";password=" + password;
            MySoftERPEntity onlineDb = new MySoftERPEntity();
            onlineDb.Database.Connection.ConnectionString = connectionString;
            onlineDb.Database.Connection.Open();
            var test = onlineDb.ERPUsers.ToList();
            if (ids.Count() > 0)
            {
                foreach (var id in ids)
                {
                    if (id > 0)
                    {
                        var salesInvoice = db.SalesInvoices.Find(id);
                        if (salesInvoice != null)
                        {
                            List<SalesInvoiceDetail> salesInvoiceDetails = new List<SalesInvoiceDetail>();
                            foreach (var item in salesInvoice.SalesInvoiceDetails)
                            {
                                var detail = new SalesInvoiceDetail();
                                detail.ItemId = item.ItemId;
                                detail.Qty = item.Qty;
                                detail.ItemPriceId = item.ItemPriceId;
                                detail.Price = item.Price;
                                detail.ItemUnitId = item.ItemUnitId;
                                detail.UnitEquivalent = item.UnitEquivalent;
                                detail.CurrencyId = item.CurrencyId;
                                detail.CurrencyEquivalent = item.CurrencyEquivalent;
                                detail.DiscountPerc = item.DiscountPerc;
                                detail.DiscountValue = item.DiscountValue;
                                detail.WareHouseId = item.WareHouseId;
                                detail.WarrantyPeriod = item.WarrantyPeriod;
                                detail.WarrantyStart = item.WarrantyStart;
                                detail.ExpireDate = item.ExpireDate;
                                detail.PatchId = item.PatchId;
                                detail.RemainQty = item.RemainQty;
                                detail.CostPrice = item.CostPrice;
                                detail.ItemDirectExpenses = item.ItemDirectExpenses;
                                detail.UpdatedId = item.UpdatedId;
                                detail.ExtraTypeId = item.ExtraTypeId;
                                detail.AccessoryPrice = item.AccessoryPrice;
                                detail.Notes = item.Notes;
                                detail.IsDeleted = item.IsDeleted;
                                detail.MainDocId = 0;
                                salesInvoiceDetails.Add(detail);
                            }
                            List<SalesInvoicePaymentMethod> salesInvoicePaymentMethods = new List<SalesInvoicePaymentMethod>();
                            foreach (var item in salesInvoice.SalesInvoicePaymentMethods)
                            {
                                var paymentMethod = new SalesInvoicePaymentMethod();
                                paymentMethod.Amount = item.Amount;
                                paymentMethod.BankAccountId = item.BankAccountId;
                                paymentMethod.BankId = item.BankId;
                                paymentMethod.CashBoxId = item.CashBoxId;
                                paymentMethod.PaidAmount = item.PaidAmount;
                                paymentMethod.PaidDate = item.PaidDate;
                                paymentMethod.PaymentMethodId = item.PaymentMethodId;
                                paymentMethod.SalesInvoiceId = item.SalesInvoiceId;
                                paymentMethod.SystemPageId = item.SystemPageId;
                                salesInvoicePaymentMethods.Add(paymentMethod);
                            }
                            List<SalesInvoiceItemDistribution> salesInvoiceItemDistributions = new List<SalesInvoiceItemDistribution>();
                            foreach (var item in salesInvoice.SalesInvoiceItemDistributions)
                            {
                                var itemDistribution = new SalesInvoiceItemDistribution();
                                itemDistribution.ItemId = item.ItemId;
                                itemDistribution.Qty = item.Qty;
                                itemDistribution.SalesInvoiceId = 0;
                                itemDistribution.WarehouseId = item.WarehouseId;
                                itemDistribution.IsDeleted = item.IsDeleted;
                                salesInvoiceItemDistributions.Add(itemDistribution);
                            }
                            List<SalesOrdersSalesInvoice> salesOrdersSalesInvoices = new List<SalesOrdersSalesInvoice>();
                            foreach (var item in salesInvoice.SalesOrdersSalesInvoices)
                            {
                                var salesOrder = new SalesOrdersSalesInvoice();
                                salesOrder.SalesInvoiceId = 0;
                                salesOrder.SalesOrderId = item.SalesOrderId;
                                salesOrdersSalesInvoices.Add(salesOrder);
                            }
                            List<SalesInvoicePurchaseRequest> salesInvoicePurchaseRequests = new List<SalesInvoicePurchaseRequest>();
                            foreach (var item in salesInvoice.SalesInvoicePurchaseRequests)
                            {
                                var purchaseRequest = new SalesInvoicePurchaseRequest();
                                purchaseRequest.PurchaseRequestId = item.PurchaseRequestId;
                                purchaseRequest.SalesInvoiceId = 0;
                                salesInvoicePurchaseRequests.Add(purchaseRequest);
                            }
                            var pageSourceId = db.SystemPages.Where(a => a.ControllerName == "SalesInvoice").FirstOrDefault().Id;
                            ICollection<PurchaseSaleSerialNumber> purchaseSaleSerialNumbers = db.PurchaseSaleSerialNumbers.Where(a => a.PageSourceId == pageSourceId && a.SelectedId == salesInvoice.Id).ToList();
                            //List<PurchaseSaleSerialNumber> serialNumbers = new List<PurchaseSaleSerialNumber>();
                            //foreach (var item in purchaseSaleSerialNumbers)
                            //{
                            //    var serialNumber = new PurchaseSaleSerialNumber();
                            //    serialNumber.ItemId = item.ItemId;
                            //    serialNumber.PageSourceId = pageSourceId;
                            //    serialNumber.SelectedId = 0;
                            //    serialNumber.SerialNumber = item.SerialNumber;
                            //    serialNumber.IsActive = item.IsActive;
                            //    serialNumber.IsDeleted = item.IsDeleted;
                            //    serialNumbers.Add(serialNumber);
                            //}

                            //MyXML.xPathName = "ItemsDistributions";
                            //var SalesInvoiceItemDistributions = MyXML.GetXML(salesInvoiceItemDistributions);

                            MyXML.xPathName = "PaymentMethods";
                            var InvoicePaymentMethods = MyXML.GetXML(salesInvoicePaymentMethods);
                            //MyXML.xPathName = "SerialNumbers";
                            //var SerialNumbersXML = MyXML.GetXML(serialNumbers);
                            MyXML.xPathName = "SalesOrders";
                            var SalesOrdersXml = MyXML.GetXML(salesOrdersSalesInvoices);
                            MyXML.xPathName = "PurchaseRequests";
                            var PurchaseRequestsXml = MyXML.GetXML(salesInvoicePurchaseRequests);
                            MyXML.xPathName = "Details";
                            var SalesInvoiceDetails = MyXML.GetXML(salesInvoiceDetails);
                            var idResult = new ObjectParameter("Id", typeof(Int32));
                            onlineDb.SalesInvoice_Insert(idResult, salesInvoice.BranchId, salesInvoice.WarehouseId, salesInvoice.DepartmentId, salesInvoice.VoucherDate, salesInvoice.VendorOrCustomerId, salesInvoice.CurrencyId, salesInvoice.CurrencyEquivalent, salesInvoice.Total, salesInvoice.TotalItemsDiscount, salesInvoice.SalesTaxes, salesInvoice.TotalAfterTaxes, salesInvoice.VoucherDiscountValue, salesInvoice.VoucherDiscountPercentage, salesInvoice.NetTotal, salesInvoice.Paid, salesInvoice.ValidityPeriod, salesInvoice.DeliveryPeriod, salesInvoice.CostCenterId, salesInvoice.CurrentQuantity, salesInvoice.DestinationWarehouseId, salesInvoice.SystemPageId, salesInvoice.SelectedId, salesInvoice.TotalCostPrice, salesInvoice.TotalItemDirectExpenses, salesInvoice.CommercialRevenueTaxAmount, salesInvoice.IsDelivered, salesInvoice.IsAccepted, salesInvoice.IsLinked, salesInvoice.IsCompleted, false, UserId, salesInvoice.IsActive, salesInvoice.IsDeleted, salesInvoice.AutoCreated, salesInvoice.Notes, salesInvoice.Image, salesInvoice.UpdatedId, salesInvoice.CarNumber, salesInvoice.DeliveredTo, salesInvoice.InstallmentAdvance, salesInvoice.TotalInstallmentBeforeProfit, salesInvoice.TotalInstallmentAfterProfit, salesInvoice.InsallmentProfitPercentage, salesInvoice.InstallmentProfitAmount, salesInvoice.InstallmentPaymentAmount, salesInvoice.InstallmentPaymentCount, salesInvoice.TotalAfterTotalInstallment, salesInvoice.FirstInstallmentPaymentDueDate, salesInvoice.IsInstallmentPlan, salesInvoice.DaysBetweenInstallments, SalesInvoiceDetails, InvoicePaymentMethods, "", SalesOrdersXml, salesInvoice.ServiceFees, salesInvoice.CustomerType, PurchaseRequestsXml, null, null, null, false, false, salesInvoice.CustomerRepId, salesInvoice.WarehouseItemDistribution, salesInvoice.RefundableInsurance, salesInvoice.DueDate, salesInvoice.PosCustomerId, salesInvoice.IsCollectedByCashier, salesInvoice.TableId, salesInvoice.DeliveryCost, salesInvoice.DeliveryDate, salesInvoice.DeliveryStartDate, salesInvoice.DeliveryEndDate, salesInvoice.DriverId, salesInvoice.CarId, salesInvoice.PaymentType, salesInvoice.IssueMethodId, salesInvoice.SmokingTax, salesInvoice.WaiterId);
                            salesInvoice.IsSynced = true;
                            salesInvoice.IsUpdateSynced = true;
                            db.Entry(salesInvoice).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                    }
                }
                onlineDb.Database.Connection.Close();
                onlineDb.Database.Connection.Dispose();

            }
            return Json(new { success = "true", ids });
        }

        [SkipERPAuthorize]
        public JsonResult GetAvailableVehicleStock(int itemId)
        {
            if (!VehicleStockTableExists())
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);

            string sql = @"SELECT vs.Id, vs.ItemId, vs.ChassisNo, vs.EngineNo, vs.CarTypeId, vs.CarModelId, vs.CarColorId, vs.ManufacturingYear, vs.PlateNo, vs.VehicleNotes,
COALESCE(ct.ArName, '') AS CarTypeName, COALESCE(cm.ArName, '') AS CarModelName, COALESCE(cc.ArName, '') AS CarColorName,
vs.PurchaseDate, vs.PurchaseCost, COALESCE(w.ArName, '') AS WarehouseName
FROM dbo.VehicleStock vs
LEFT JOIN dbo.CarType ct ON ct.Id = vs.CarTypeId
LEFT JOIN dbo.CarModel cm ON cm.Id = vs.CarModelId
LEFT JOIN dbo.CarColor cc ON cc.Id = vs.CarColorId
LEFT JOIN dbo.Warehouse w ON w.Id = vs.WarehouseId
WHERE vs.IsDeleted = 0 AND vs.VehicleStatusId = 1 AND vs.ItemId = @ItemId
ORDER BY vs.PurchaseDate DESC, vs.Id DESC";

            var data = db.Database.SqlQuery<VehicleStockLookupRow>(sql, new SqlParameter("@ItemId", itemId)).ToList();
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        [ERPAuthorize]
        public JsonResult AddToSalesInvoicePrintQueue(int? SalesInvoiceId, string Status, bool? IsUpdated)
        {

            try
            {
                var salesInvoicePrintQueue = new SalesInvoicePrintQueue();
                salesInvoicePrintQueue.SalesInvoiceId = SalesInvoiceId;
                salesInvoicePrintQueue.IsUpdated = IsUpdated;
                if (Status == "SendToKitchen")
                {
                    salesInvoicePrintQueue.PrintInvoice = false;
                    salesInvoicePrintQueue.PrintOnKitchen = true;
                }
                else if (Status == "Save")
                {
                    salesInvoicePrintQueue.PrintInvoice = false;
                    salesInvoicePrintQueue.PrintOnKitchen = true;
                }
                else if (Status == "SaveAndPrint")
                {
                    salesInvoicePrintQueue.PrintInvoice = true;
                    salesInvoicePrintQueue.PrintOnKitchen = true;
                }
                else if (Status == "PrintInvoice")
                {
                    salesInvoicePrintQueue.PrintInvoice = true;
                    salesInvoicePrintQueue.PrintOnKitchen = false;
                }
                else if (Status == "Pay")
                {
                    salesInvoicePrintQueue.PrintInvoice = true;
                    salesInvoicePrintQueue.PrintOnKitchen = false;
                }
                db.SalesInvoicePrintQueues.Add(salesInvoicePrintQueue);
                db.SaveChanges();
                return Json("success", JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }


        private class VehicleStockLookupRow
        {
            public int Id { get; set; }
            public int ItemId { get; set; }
            public string ChassisNo { get; set; }
            public string EngineNo { get; set; }
            public int? CarTypeId { get; set; }
            public int? CarModelId { get; set; }
            public int? CarColorId { get; set; }
            public int? ManufacturingYear { get; set; }
            public string PlateNo { get; set; }
            public string VehicleNotes { get; set; }
            public string CarTypeName { get; set; }
            public string CarModelName { get; set; }
            public string CarColorName { get; set; }
            public DateTime? PurchaseDate { get; set; }
            public decimal? PurchaseCost { get; set; }
            public string WarehouseName { get; set; }
        }

        private bool VehicleStockTableExists()
        {
            return db.Database.SqlQuery<int>("SELECT CASE WHEN OBJECT_ID('dbo.VehicleStock','U') IS NULL THEN 0 ELSE 1 END").FirstOrDefault() == 1;
        }

        private string ValidateVehicleStockSelection(ICollection<SalesInvoiceDetail> details)
        {
            if (!VehicleStockTableExists() || details == null) return null;
            foreach (var detail in details)
            {
                var hasVehicleData = !string.IsNullOrWhiteSpace(detail.ChassisNo) || detail.VehicleStockId.HasValue || detail.CarTypeId.HasValue || detail.CarModelId.HasValue || detail.CarColorId.HasValue || !string.IsNullOrWhiteSpace(detail.EngineNo) || detail.ManufacturingYear.HasValue || !string.IsNullOrWhiteSpace(detail.PlateNo);
                if (!hasVehicleData) continue;

                if (!detail.VehicleStockId.HasValue)
                    return "يجب اختيار السيارة من مخزون السيارات المتاح.";
                if (detail.Qty > 1)
                    return "لا يمكن بيع أكثر من سيارة واحدة بنفس السطر عند اختيار سيارة من المخزون.";

                string sql = @"SELECT COUNT(1) FROM dbo.VehicleStock WHERE Id = @Id AND IsDeleted = 0 AND VehicleStatusId = 1 AND ItemId = @ItemId";
                var valid = db.Database.SqlQuery<int>(sql, new SqlParameter("@Id", detail.VehicleStockId.Value), new SqlParameter("@ItemId", detail.ItemId)).FirstOrDefault() > 0;
                if (!valid)
                    return "السيارة المختارة غير متاحة بالمخزون أو لا تطابق الصنف.";
            }
            return null;
        }

        private void SyncVehicleStockForSalesInvoice(int salesInvoiceId)
        {
            if (!VehicleStockTableExists()) return;
            var invoice = db.SalesInvoices.FirstOrDefault(x => x.Id == salesInvoiceId);
            if (invoice == null) return;
           // var details = db.SalesInvoiceDetails.Where(x => x.MainDocId == salesInvoiceId && !x.IsDeleted && x.VehicleStockId != null).ToList();

            var details = db.SalesInvoiceDetails
    .Where(x => x.MainDocId == salesInvoiceId && !x.IsDeleted)
    .AsEnumerable()
    .Where(x => x.VehicleStockId != null)
    .ToList();

            foreach (var detail in details)
            {
                string sql = @"UPDATE dbo.VehicleStock
SET VehicleStatusId = 3, SalesInvoiceId = @SalesInvoiceId, SalesInvoiceDetailId = @SalesInvoiceDetailId, SalesDate = @SalesDate, SalePrice = @SalePrice,
    UpdatedDate = GETDATE(), ItemId = @ItemId, EngineNo = @EngineNo, CarTypeId=@CarTypeId, CarModelId=@CarModelId, CarColorId=@CarColorId,
    ManufacturingYear=@ManufacturingYear, PlateNo=@PlateNo, VehicleNotes=@VehicleNotes
WHERE Id = @VehicleStockId AND IsDeleted = 0 AND VehicleStatusId = 1";
                db.Database.ExecuteSqlCommand(sql,
                    new SqlParameter("@SalesInvoiceId", salesInvoiceId),
                    new SqlParameter("@SalesInvoiceDetailId", detail.Id),
                    new SqlParameter("@SalesDate", (object)invoice.VoucherDate ?? DBNull.Value),
                    new SqlParameter("@SalePrice", (object)detail.Price),
                    new SqlParameter("@ItemId", detail.ItemId),
                    new SqlParameter("@EngineNo", (object)detail.EngineNo ?? DBNull.Value),
                    new SqlParameter("@CarTypeId", (object)detail.CarTypeId ?? DBNull.Value),
                    new SqlParameter("@CarModelId", (object)detail.CarModelId ?? DBNull.Value),
                    new SqlParameter("@CarColorId", (object)detail.CarColorId ?? DBNull.Value),
                    new SqlParameter("@ManufacturingYear", (object)detail.ManufacturingYear ?? DBNull.Value),
                    new SqlParameter("@PlateNo", (object)detail.PlateNo ?? DBNull.Value),
                    new SqlParameter("@VehicleNotes", (object)detail.VehicleNotes ?? DBNull.Value),
                    new SqlParameter("@VehicleStockId", detail.VehicleStockId.Value));
            }
        }

        private void FillVehicleLookupData(string session)
        {
            ViewBag.CarTypesJson = JsonConvert.SerializeObject(db.CarTypes.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = session == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }).ToList());
            ViewBag.CarModelsJson = JsonConvert.SerializeObject(db.CarModels.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                b.CarTypeId,
                ArName = session == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }).ToList());
            ViewBag.CarColorsJson = JsonConvert.SerializeObject(db.CarColors.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = session == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }).ToList());
        }
        private string ValidateVehicleChassisNumbers(ICollection<SalesInvoiceDetail> details, int invoiceId)
        {
            if (details == null) return null;
            var normalizedInInvoice = new HashSet<string>();
            foreach (var detail in details)
            {
                var hasVehicleData = detail.CarTypeId.HasValue || detail.CarModelId.HasValue || detail.CarColorId.HasValue || !string.IsNullOrWhiteSpace(detail.EngineNo) || detail.ManufacturingYear.HasValue || !string.IsNullOrWhiteSpace(detail.PlateNo);
                var chassis = (detail.ChassisNo ?? "").Trim();
                if (hasVehicleData && string.IsNullOrWhiteSpace(chassis))
                    return "رقم الشاسيه مطلوب عند إدخال بيانات سيارة.";
                if (!string.IsNullOrWhiteSpace(chassis))
                {
                    var normalized = Regex.Replace(chassis.ToLower(), "\\s+", "");
                    if (normalizedInInvoice.Contains(normalized))
                        return "رقم الشاسيه مكرر داخل نفس الفاتورة.";
                    normalizedInInvoice.Add(normalized);
                    var existsInSales = db.SalesInvoiceDetails.Where(x => !x.IsDeleted && x.MainDocId != invoiceId && x.ChassisNo != null)
                        .Select(x => x.ChassisNo).ToList().Any(x => Regex.Replace(x.ToLower(), "\\s+", "") == normalized);
                    if (existsInSales)
                        return "رقم الشاسيه مستخدم مسبقاً في فاتورة بيع أخرى.";
                    var existsInPurchase = db.PurchaseInvoiceDetails.Where(x => !x.IsDeleted && x.ChassisNo != null)
                        .Select(x => x.ChassisNo).ToList().Any(x => Regex.Replace(x.ToLower(), "\\s+", "") == normalized);
                    if (!existsInPurchase)
                        return "رقم الشاسيه غير موجود في فواتير المشتريات.";
                }
            }
            return null;
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
